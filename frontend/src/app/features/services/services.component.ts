import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { ServicesService } from '../../core/services.service';
import { UsersService } from '../../core/users.service';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { ServiceInput, ServiceListing, UserProfile } from '../../core/models';

const emptyDraft = (): ServiceInput => ({
  title: '',
  description: '',
  category: '',
  cost: 0,
  contactInfo: '',
  location: '',
  offers: '',
});

@Component({
  selector: 'app-services',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
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

  /** Keystrokes are debounced before they trigger an API round-trip. */
  private readonly searchInput$ = new Subject<string>();

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

  draft: ServiceInput = emptyDraft();

  /** Providers and dashboard admins may list/manage services; others get 403 from the API. */
  readonly canList = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager', 'Provider'].includes(this.auth.user()?.role ?? ''),
  );

  readonly title = computed(() => (this.editingId() ? 'Edit service' : 'New service'));

  ngOnInit(): void {
    this.reload();
    this.servicesService.categories().subscribe({
      next: (c) => this.categories.set(c),
      error: () => {},
    });
    // Provider names for the table; non-admins only receive the public provider directory.
    this.usersService.list({ pageSize: 100 }).subscribe({
      next: (r) => this.users.set(r.items),
      error: () => {},
    });
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reloadFirstPage());
  }

  /** Fetches the current page from the API using filters + lazy sort/page state. */
  reload(): void {
    this.loading.set(true);
    const pageSize = this.pageSize();
    this.servicesService
      .list({
        search: this.search() || undefined,
        category: this.category() ?? undefined,
        page: Math.floor(this.first() / pageSize) + 1,
        pageSize,
        sortBy: this.sortField() ?? undefined,
        sortDir: this.sortOrder() === -1 ? 'desc' : 'asc',
      })
      .subscribe({
        next: (res) => {
          this.services.set(res.items);
          this.total.set(res.total);
          this.loadedOnce.set(true);
          this.loading.set(false);
        },
        error: () => {
          this.loadedOnce.set(true);
          this.loading.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'API unreachable',
            detail: 'Start the .NET API on localhost:5240.',
          });
        },
      });
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

  onSearch(value: string): void {
    this.search.set(value);
    this.searchInput$.next(value);
  }

  onCategory(value: string | null): void {
    this.category.set(value);
    this.reloadFirstPage();
  }

  clearFilters(): void {
    this.search.set('');
    this.category.set(null);
    this.reloadFirstPage();
  }

  private reloadFirstPage(): void {
    this.first.set(0);
    this.reload();
  }

  openNew(): void {
    this.editingId.set(null);
    this.draft = emptyDraft();
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
    this.dialogVisible.set(true);
  }

  save(): void {
    const draft = this.draft;
    if (!draft.title.trim() || !draft.category.trim() || !draft.contactInfo.trim() || !draft.location.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Missing fields',
        detail: 'Title, category, contact info and location are required.',
      });
      return;
    }

    this.saving.set(true);
    const id = this.editingId();
    const request$ = id ? this.servicesService.update(id, draft) : this.servicesService.create(draft);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: id ? 'Service updated' : 'Service created',
          detail: draft.title,
        });
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Save failed',
          detail: this.apiError(err, 'The API rejected the request.'),
        });
      },
    });
  }

  confirmDelete(service: ServiceListing): void {
    this.confirmationService.confirm({
      header: 'Delete service',
      message: `Remove <strong>${service.title}</strong> from the marketplace?`,
      icon: 'pi pi-trash',
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.servicesService.remove(service.id).subscribe({
          next: () => {
            this.reload();
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: service.title,
            });
          },
          error: () =>
            this.messageService.add({
              severity: 'error',
              summary: 'Delete failed',
              detail: 'The API rejected the request.',
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
    return first?.[0] ?? error?.title ?? fallback;
  }
}
