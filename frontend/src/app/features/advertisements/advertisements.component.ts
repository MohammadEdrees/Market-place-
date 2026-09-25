import { DatePipe } from '@angular/common';
import { Component, DestroyRef, ElementRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { AdvertisementService } from '../../core/advertisement.service';
import type { Advertisement, AdvertisementInput } from '../../core/models';

/** Dialog draft — dates are handled as `Date` and serialized to ISO on save. */
interface AdDraft {
  title: string;
  subtitle: string;
  targetUrl: string;
  sortOrder: number;
  isActive: boolean;
  startsAt: Date | null;
  endsAt: Date | null;
}

/** A file picked in the dialog but not yet uploaded (with its local preview URL). */
type PendingImage = { file: File; url: string };

const ALLOWED_IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/webp', 'image/gif'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

const emptyDraft = (): AdDraft => ({
  title: '',
  subtitle: '',
  targetUrl: '',
  sortOrder: 0,
  isActive: true,
  startsAt: null,
  endsAt: null,
});

/** Row → request body: `PUT` needs the full advertisement, not just the flipped flag. */
function toInput(ad: Advertisement): AdvertisementInput {
  return {
    title: ad.title,
    subtitle: ad.subtitle ?? null,
    targetUrl: ad.targetUrl ?? null,
    isActive: ad.isActive,
    startsAt: ad.startsAt ?? null,
    endsAt: ad.endsAt ?? null,
    sortOrder: ad.sortOrder,
  };
}

const toDate = (value: string | null): Date | null => {
  if (!value) {
    return null;
  }
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
};

@Component({
  selector: 'app-advertisements',
  imports: [
    DatePipe,
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DatePickerModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    SkeletonModule,
    TableModule,
    TagModule,
    ToastModule,
    ToggleSwitchModule,
    TooltipModule,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './advertisements.component.html',
  styleUrl: './advertisements.component.scss',
})
export class AdvertisementsComponent implements OnInit {
  private readonly advertisementService = inject(AdvertisementService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  /** Hidden file input behind the row "Replace image" buttons. */
  private readonly replaceInput = viewChild<ElementRef<HTMLInputElement>>('replaceInput');

  /** Every reload funnels through here so switchMap cancels stale in-flight requests. */
  private readonly reload$ = new Subject<void>();

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly ads = signal<Advertisement[]>([]);

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  /** `null` while creating; the ad id when editing. */
  readonly editingId = signal<number | null>(null);
  draft: AdDraft = emptyDraft();

  // Image handling: a file picked in the dialog, and row replacements in flight.
  readonly pendingImage = signal<PendingImage | null>(null);
  readonly existingImage = signal<string | null>(null);
  /** Local previews keyed by ad id — shown while the replacement uploads. */
  readonly previews = signal<Record<number, string>>({});
  readonly uploadingId = signal<number | null>(null);
  readonly togglingId = signal<number | null>(null);
  private uploadTarget: Advertisement | null = null;

  /** Advertisement writes are admin-only (the API answers 403 for other roles). */
  readonly canManage = computed(() =>
    ['SuperAdmin', 'Admin'].includes(this.auth.user()?.role ?? ''),
  );

  readonly title = computed(() => (this.editingId() ? 'Edit advertisement' : 'New advertisement'));

  ngOnInit(): void {
    // Single fetch pipeline: switchMap cancels the previous request as soon as a newer
    // trigger arrives, so responses can never arrive out of order.
    this.reload$
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          return this.advertisementService.list().pipe(
            catchError(() => {
              this.loadedOnce.set(true);
              this.loading.set(false);
              this.messageService.add({
                severity: 'error',
                summary: 'API unreachable',
                detail: 'Start the .NET API on localhost:5240.',
              });
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((ads) => {
        this.ads.set(ads);
        this.loadedOnce.set(true);
        this.loading.set(false);
      });

    this.reload();
  }

  /** Asks the fetch pipeline for the full list (cancels any in-flight request). */
  reload(): void {
    this.reload$.next();
  }

  openCreate(): void {
    this.editingId.set(null);
    this.draft = emptyDraft();
    this.pendingImage.set(null);
    this.existingImage.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(ad: Advertisement): void {
    this.editingId.set(ad.id);
    this.draft = {
      title: ad.title,
      subtitle: ad.subtitle ?? '',
      targetUrl: ad.targetUrl ?? '',
      sortOrder: ad.sortOrder,
      isActive: ad.isActive,
      startsAt: toDate(ad.startsAt),
      endsAt: toDate(ad.endsAt),
    };
    this.pendingImage.set(null);
    this.existingImage.set(ad.imagePath);
    this.dialogVisible.set(true);
  }

  /** Creates (`POST`) or updates (`PUT`) the ad, then uploads a dialog-picked image if any. */
  save(): void {
    const title = this.draft.title.trim();
    if (!title) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Missing fields',
        detail: 'Advertisement title is required.',
      });
      return;
    }

    const { startsAt, endsAt } = this.draft;
    if (startsAt && endsAt && endsAt.getTime() <= startsAt.getTime()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Check the schedule',
        detail: 'Ends must be after starts.',
      });
      return;
    }

    const input: AdvertisementInput = {
      title,
      subtitle: this.draft.subtitle.trim() || null,
      targetUrl: this.draft.targetUrl.trim() || null,
      isActive: this.draft.isActive,
      startsAt: startsAt ? startsAt.toISOString() : null,
      endsAt: endsAt ? endsAt.toISOString() : null,
      sortOrder: Number(this.draft.sortOrder) || 0,
    };

    const editing = this.editingId();
    this.saving.set(true);
    const request$ =
      editing != null
        ? this.advertisementService.update(editing, input)
        : this.advertisementService.create(input);

    request$.subscribe({
      next: (saved) => this.uploadDialogImage(saved, editing),
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: editing != null ? 'Could not save advertisement' : 'Could not create advertisement',
          detail: this.apiError(err, 'The API rejected the request.'),
        });
      },
    });
  }

  /** Uploads the dialog-picked image once the ad exists (create and edit alike). */
  private uploadDialogImage(saved: Advertisement, editing: number | null): void {
    const pending = this.pendingImage();
    if (!pending) {
      this.finishSave(saved, editing);
      return;
    }

    this.advertisementService.uploadImage(saved.id, pending.file).subscribe({
      next: (withImage) => {
        this.clearPendingImage();
        this.finishSave(withImage, editing);
      },
      error: (err) => {
        this.clearPendingImage();
        this.saving.set(false);
        this.reload();
        this.messageService.add({
          severity: 'error',
          summary: 'Image upload failed',
          detail: this.apiError(err, 'The advertisement was saved, but its image was not.'),
        });
      },
    });
  }

  private finishSave(saved: Advertisement, editing: number | null): void {
    this.saving.set(false);
    this.dialogVisible.set(false);
    this.reload();
    this.messageService.add({
      severity: 'success',
      summary: editing != null ? 'Advertisement updated' : 'Advertisement created',
      detail: saved.title,
    });
  }

  /* ---------- Dialog image ---------- */

  onDialogFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    if (!file) {
      return;
    }
    const problem = this.imageProblem(file);
    if (problem) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid image', detail: problem });
      return;
    }
    this.clearPendingImage();
    this.pendingImage.set({ file, url: URL.createObjectURL(file) });
  }

  clearPendingImage(): void {
    const pending = this.pendingImage();
    if (pending) {
      URL.revokeObjectURL(pending.url);
    }
    this.pendingImage.set(null);
  }

  /* ---------- Row image replacement (uploads immediately) ---------- */

  /** Opens the hidden file picker for one row; the upload starts as soon as a file is picked. */
  replaceImage(ad: Advertisement): void {
    if (this.uploadingId() != null) {
      return;
    }
    const input = this.replaceInput();
    if (!input) {
      return;
    }
    this.uploadTarget = ad;
    input.nativeElement.click();
  }

  onRowFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    const target = this.uploadTarget;
    if (!file || !target) {
      return;
    }
    const problem = this.imageProblem(file);
    if (problem) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid image', detail: problem });
      return;
    }

    // Local preview while the multipart upload runs; the response carries the stored URL.
    const url = URL.createObjectURL(file);
    this.previews.update((map) => ({ ...map, [target.id]: url }));
    this.uploadingId.set(target.id);

    this.advertisementService.uploadImage(target.id, file).subscribe({
      next: (updated) => {
        this.clearPreview(updated.id);
        this.uploadingId.set(null);
        this.ads.update((list) => list.map((ad) => (ad.id === updated.id ? updated : ad)));
        this.messageService.add({
          severity: 'success',
          summary: 'Image updated',
          detail: updated.title,
        });
      },
      error: (err) => {
        this.clearPreview(target.id);
        this.uploadingId.set(null);
        this.messageService.add({
          severity: 'error',
          summary: 'Image upload failed',
          detail: this.apiError(err, 'The API rejected the image.'),
        });
      },
    });
  }

  private clearPreview(id: number): void {
    this.previews.update((map) => {
      const url = map[id];
      if (url) {
        URL.revokeObjectURL(url);
      }
      const next = { ...map };
      delete next[id];
      return next;
    });
  }

  /** Preview taken while uploading, otherwise the stored image (may be null). */
  imageSrc(ad: Advertisement): string | null {
    return this.previews()[ad.id] ?? ad.imagePath;
  }

  /* ---------- Row enable/disable ---------- */

  /** Flips `isActive` with a full `PUT`; rolls back to the server list on failure. */
  toggleActive(ad: Advertisement, isActive: boolean): void {
    if (this.togglingId() != null || isActive === ad.isActive) {
      return;
    }
    this.togglingId.set(ad.id);
    this.advertisementService.update(ad.id, { ...toInput(ad), isActive }).subscribe({
      next: (updated) => {
        this.togglingId.set(null);
        this.ads.update((list) => list.map((item) => (item.id === updated.id ? updated : item)));
        this.messageService.add({
          severity: 'success',
          summary: isActive ? 'Advertisement enabled' : 'Advertisement disabled',
          detail: updated.title,
        });
      },
      error: (err) => {
        this.togglingId.set(null);
        this.reload();
        this.messageService.add({
          severity: 'error',
          summary: 'Could not update advertisement',
          detail: this.apiError(err, 'Only admins can manage advertisements.'),
        });
      },
    });
  }

  /* ---------- Delete ---------- */

  confirmDelete(ad: Advertisement): void {
    this.confirmationService.confirm({
      header: 'Delete advertisement',
      message: `Remove <strong>${ad.title}</strong>? Its image is deleted too.`,
      icon: 'pi pi-trash',
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.advertisementService.remove(ad.id).subscribe({
          next: () => {
            this.reload();
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: ad.title,
            });
          },
          error: (err) =>
            this.messageService.add({
              severity: 'error',
              summary: 'Delete failed',
              detail: this.apiError(err, 'The API rejected the request.'),
            }),
        });
      },
    });
  }

  /* ---------- Display helpers ---------- */

  /**
   * Client-side status chip — mirrors the server's `activeNow`:
   * off → "Off"; starts in the future → "Scheduled"; ends in the past → "Expired"; else "Live".
   */
  statusLabel(ad: Advertisement): 'Live' | 'Scheduled' | 'Expired' | 'Off' {
    if (!ad.isActive) {
      return 'Off';
    }
    const now = Date.now();
    const starts = ad.startsAt ? Date.parse(ad.startsAt) : null;
    const ends = ad.endsAt ? Date.parse(ad.endsAt) : null;
    if (starts != null && !Number.isNaN(starts) && starts > now) {
      return 'Scheduled';
    }
    if (ends != null && !Number.isNaN(ends) && ends < now) {
      return 'Expired';
    }
    return 'Live';
  }

  statusSeverity(ad: Advertisement): 'success' | 'info' | 'danger' | 'secondary' {
    switch (this.statusLabel(ad)) {
      case 'Live':
        return 'success';
      case 'Scheduled':
        return 'info';
      case 'Expired':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  openImage(path: string): void {
    window.open(path, '_blank', 'noopener');
  }

  private imageProblem(file: File): string | null {
    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      return 'PNG, JPG, WEBP or GIF only.';
    }
    if (file.size > MAX_IMAGE_BYTES) {
      return 'Images must be 5 MB or smaller.';
    }
    return null;
  }

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; detail?: string; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    return first?.[0] ?? error?.detail ?? error?.title ?? fallback;
  }
}
