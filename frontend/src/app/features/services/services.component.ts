import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { Textarea } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { ServicesService } from '../../core/services.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import { UsersService } from '../../core/users.service';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { ListingImage, ServiceInput, ServiceListing, UserProfile } from '../../core/models';

const emptyDraft = (): ServiceInput => ({
  title: '',
  description: '',
  category: '',
  cost: 0,
  contactInfo: '',
  location: '',
  offers: '',
});

/** A file picked in the dialog but not yet uploaded (with its local preview URL). */
type PendingImage = { file: File; url: string };

@Component({
  selector: 'app-services',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    IconFieldModule,
    InputIconModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    SkeletonModule,
    TableModule,
    TagModule,
    Textarea,
    ToastModule,
    TooltipModule,
    TranslatePipe,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss',
})
export class ServicesComponent implements OnInit {
  private readonly servicesService = inject(ServicesService);
  private readonly usersService = inject(UsersService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  readonly i18n = inject(I18nService);

  /** Search box as a reactive control: valueChanges → debounce → distinct → fetch. */
  readonly searchControl = new FormControl('', { nonNullable: true });

  /** Every reload funnels through here so switchMap cancels stale in-flight requests. */
  private readonly reload$ = new Subject<void>();

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly services = signal<ServiceListing[]>([]);
  readonly total = signal(0);
  readonly categories = signal<string[]>([]);
  readonly users = signal<UserProfile[]>([]);

  readonly search = signal('');
  readonly category = signal<string | null>(null);

  // Server-side pagination/sorting, mirrored from the PrimeNG lazy events.
  readonly first = signal(0);
  readonly pageSize = signal(8);
  readonly sortField = signal<string | null>(null);
  readonly sortOrder = signal(1); // 1 = asc, -1 = desc

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);

  // Gallery: images already stored on the API plus files picked in the dialog.
  readonly maxImages = 10;
  readonly existingImages = signal<ListingImage[]>([]);
  readonly pendingImages = signal<PendingImage[]>([]);

  draft: ServiceInput = emptyDraft();

  /** Providers and dashboard admins may list/manage services; others get 403 from the API. */
  readonly canList = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager', 'Provider'].includes(this.auth.user()?.role ?? ''),
  );

  readonly title = computed(() =>
    this.editingId() ? this.i18n.t('services.edit') : this.i18n.t('services.new'),
  );

  ngOnInit(): void {
    // Backend search with RxJS: debounce keystrokes, drop duplicates, refetch page 1.
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        map((value) => value.trim()),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((term) => {
        this.search.set(term);
        this.first.set(0);
        this.reload();
      });

    // Single fetch pipeline: switchMap cancels the previous request as soon as a newer
    // page/sort/filter/search trigger arrives, so responses can never arrive out of order.
    this.reload$
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          const pageSize = this.pageSize();
          return this.servicesService
            .list({
              search: this.search() || undefined,
              category: this.category() ?? undefined,
              page: Math.floor(this.first() / pageSize) + 1,
              pageSize,
              sortBy: this.sortField() ?? undefined,
              sortDir: this.sortOrder() === -1 ? 'desc' : 'asc',
            })
            .pipe(
              catchError(() => {
                this.loadedOnce.set(true);
                this.loading.set(false);
                this.messageService.add({
                  severity: 'error',
                  summary: this.i18n.t('toast.apiUnreachable'),
                  detail: this.i18n.t('toast.apiUnreachableDetail'),
                });
                return EMPTY;
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        this.services.set(res.items);
        this.total.set(res.total);
        this.loadedOnce.set(true);
        this.loading.set(false);
      });

    this.servicesService.categories().subscribe({
      next: (c) => this.categories.set(c),
      error: () => {},
    });
    // Provider names for the table; non-admins only receive the public provider directory.
    this.usersService.list({ pageSize: 100 }).subscribe({
      next: (r) => this.users.set(r.items),
      error: () => {},
    });

    this.reload();
  }

  /** Asks the fetch pipeline for the current page (cancels any in-flight request). */
  reload(): void {
    this.reload$.next();
  }

  /** Pagination and column sorting arrive as a single PrimeNG lazy event. */
  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first.set(event.first ?? 0);
    if (event.rows != null) {
      this.pageSize.set(event.rows);
    }
    this.sortField.set(event.sortField != null ? String(event.sortField) : null);
    this.sortOrder.set(event.sortOrder ?? 1);
    this.reload();
  }

  onCategory(value: string | null): void {
    this.category.set(value);
    this.reloadFirstPage();
  }

  clearFilters(): void {
    this.category.set(null);
    this.search.set('');
    // Suppress the valueChanges pipeline — the manual reload below covers it.
    this.searchControl.setValue('', { emitEvent: false });
    this.reloadFirstPage();
  }

  private reloadFirstPage(): void {
    this.first.set(0);
    this.reload();
  }

  openNew(): void {
    this.editingId.set(null);
    this.draft = emptyDraft();
    this.existingImages.set([]);
    this.clearPendingImages();
    this.dialogVisible.set(true);
  }

  openEdit(service: ServiceListing): void {
    this.editingId.set(service.id);
    this.draft = {
      title: service.title,
      description: service.description,
      category: service.category,
      cost: service.cost,
      contactInfo: service.contactInfo,
      location: service.location,
      offers: service.offers,
    };
    this.existingImages.set(service.images ?? []);
    this.clearPendingImages();
    this.dialogVisible.set(true);
  }

  save(): void {
    const draft = this.draft;
    if (!draft.title.trim() || !draft.category.trim() || !draft.contactInfo.trim() || !draft.location.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('toast.missingFields'),
        detail: this.i18n.t('validation.servicesRequired'),
      });
      return;
    }

    if (this.existingImages().length + this.pendingImages().length > this.maxImages) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('toast.tooManyImages'),
        detail: this.i18n.t('toast.maxImagesDetail', { count: this.maxImages }),
      });
      return;
    }

    this.saving.set(true);
    const id = this.editingId();
    const request$ = id ? this.servicesService.update(id, draft) : this.servicesService.create(draft);

    request$.subscribe({
      next: (saved) => this.uploadPendingImages(saved.id, id, draft.title),
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('toast.saveFailed'),
          detail: this.apiError(err, this.i18n.t('toast.apiRejected')),
        });
      },
    });
  }

  /** Uploads dialog-picked files once the listing exists (works for create and edit). */
  private uploadPendingImages(listingId: number, editingId: number | null, label: string): void {
    const pending = this.pendingImages();
    if (pending.length === 0) {
      this.finishSave(editingId, label);
      return;
    }

    this.servicesService.uploadImages(listingId, pending.map((item) => item.file)).subscribe({
      next: () => {
        this.clearPendingImages();
        this.finishSave(editingId, label);
      },
      error: (err) => {
        this.saving.set(false);
        this.reload();
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('toast.imageUploadFailed'),
          detail: this.apiError(err, this.i18n.t('toast.listingSavedImagesNot')),
        });
      },
    });
  }

  private finishSave(editingId: number | null, label: string): void {
    this.saving.set(false);
    this.dialogVisible.set(false);
    this.reload();
    this.messageService.add({
      severity: 'success',
      summary: this.i18n.t(editingId ? 'toast.serviceUpdated' : 'toast.serviceCreated'),
      detail: label,
    });
  }

  /** Appends chosen files to the pending gallery; they upload when the dialog is saved. */
  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (files.length === 0) {
      return;
    }

    const room = this.maxImages - this.existingImages().length - this.pendingImages().length;
    if (files.length > room) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('toast.galleryLimit'),
        detail: this.i18n.t('toast.maxImagesDetail', { count: this.maxImages }),
      });
    }
    const accepted = files.slice(0, Math.max(0, room));
    this.pendingImages.update((list) => [
      ...list,
      ...accepted.map((file) => ({ file, url: URL.createObjectURL(file) })),
    ]);
  }

  /** Drops a not-yet-uploaded file from the dialog preview. */
  removePendingImage(item: PendingImage): void {
    URL.revokeObjectURL(item.url);
    this.pendingImages.update((list) => list.filter((i) => i !== item));
  }

  /** Asks the API to remove a stored image row and delete its file. */
  removeExistingImage(image: ListingImage): void {
    const id = this.editingId();
    if (id == null) {
      return;
    }
    this.servicesService.removeImage(id, image.id).subscribe({
      next: () => this.existingImages.update((list) => list.filter((i) => i.id !== image.id)),
      error: (err) =>
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('toast.couldNotRemoveImage'),
          detail: this.apiError(err, this.i18n.t('toast.apiRejected')),
        }),
    });
  }

  /** Previews the full-size image (served by the API) in a new tab. */
  openImage(path: string): void {
    window.open(path, '_blank', 'noopener');
  }

  private clearPendingImages(): void {
    this.pendingImages().forEach((item) => URL.revokeObjectURL(item.url));
    this.pendingImages.set([]);
  }

  confirmDelete(service: ServiceListing): void {
    this.confirmationService.confirm({
      header: this.i18n.t('services.deleteHeader'),
      message: this.i18n.t('services.deleteMessage', { name: service.title }),
      icon: 'pi pi-trash',
      acceptButtonProps: { label: this.i18n.t('common.delete'), severity: 'danger' },
      rejectButtonProps: {
        label: this.i18n.t('common.cancel'),
        severity: 'secondary',
        outlined: true,
      },
      accept: () => {
        this.servicesService.remove(service.id).subscribe({
          next: () => {
            this.reload();
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('toast.deleted'),
              detail: service.title,
            });
          },
          error: () =>
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('toast.deleteFailed'),
              detail: this.i18n.t('toast.apiRejected'),
            }),
        });
      },
    });
  }

  providerName(providerId: number): string {
    return this.users().find((u) => u.id === providerId)?.name ?? `#${providerId}`;
  }

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    // apiMessage() re-translates known English API messages; unknown text passes through.
    return this.i18n.apiMessage(first?.[0] ?? error?.title ?? fallback);
  }
}
