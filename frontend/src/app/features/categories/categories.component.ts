import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectButtonModule } from 'primeng/selectbutton';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { CategoryService } from '../../core/category.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import type { Category, CategoryKind } from '../../core/models';

const emptyDraft = (): { name: string } => ({ name: '' });

@Component({
  selector: 'app-categories',
  imports: [
    DatePipe,
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    InputTextModule,
    SelectButtonModule,
    SkeletonModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule,
    TranslatePipe,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  readonly i18n = inject(I18nService);

  /** Every reload funnels through here so switchMap cancels stale in-flight requests. */
  private readonly reload$ = new Subject<void>();

  /** Which pool is shown (and used when creating): "Products" | "Services". */
  readonly kind = signal<CategoryKind>('Product');
  readonly kindOptions = computed<{ label: string; value: CategoryKind }[]>(() => [
    { label: this.i18n.t('nav.products'), value: 'Product' },
    { label: this.i18n.t('nav.services'), value: 'Service' },
  ]);

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly categories = signal<Category[]>([]);

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  /** `null` while creating; the category id when renaming. */
  readonly editingId = signal<number | null>(null);

  draft: { name: string } = emptyDraft();

  /** Category writes are admin-only (the API answers 403 for other roles). */
  readonly canManage = computed(() =>
    ['SuperAdmin', 'Admin'].includes(this.auth.user()?.role ?? ''),
  );

  readonly title = computed(() =>
    this.editingId() ? this.i18n.t('categories.rename') : this.i18n.t('categories.add'),
  );

  ngOnInit(): void {
    // Single fetch pipeline: switchMap cancels the previous request as soon as a newer
    // kind/reload trigger arrives, so responses can never arrive out of order.
    this.reload$
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          return this.categoryService.list(this.kind()).pipe(
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
      .subscribe((categories) => {
        this.categories.set(categories);
        this.loadedOnce.set(true);
        this.loading.set(false);
      });

    this.reload();
  }

  /** Asks the fetch pipeline for the current kind (cancels any in-flight request). */
  reload(): void {
    this.reload$.next();
  }

  /** Kind switcher above the table: refetches and drives the kind used when creating. */
  onKind(value: CategoryKind | null): void {
    if (!value || value === this.kind()) {
      return;
    }
    this.kind.set(value);
    this.reload();
  }

  /** Opens the "Add category" dialog with a fresh draft (admins only). */
  openCreate(): void {
    this.editingId.set(null);
    this.draft = emptyDraft();
    this.dialogVisible.set(true);
  }

  openEdit(category: Category): void {
    this.editingId.set(category.id);
    this.draft = { name: category.name };
    this.dialogVisible.set(true);
  }

  /** Creates (`POST`, current kind) or renames (`PUT`, cascades server-side) the category. */
  save(): void {
    const name = this.draft.name.trim();
    if (!name) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('toast.missingFields'),
        detail: this.i18n.t('validation.categoryNameRequired'),
      });
      return;
    }

    const editing = this.editingId();
    this.saving.set(true);
    const request =
      editing != null
        ? this.categoryService.update(editing, { name })
        : this.categoryService.create({ name, kind: this.kind() });

    request.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: editing != null ? this.i18n.t('toast.categoryRenamed') : this.i18n.t('toast.categoryCreated'),
          detail: `${saved.name} · ${this.i18n.label('kind', saved.kind)}`,
        });
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: editing != null
            ? this.i18n.t('toast.couldNotRenameCategory')
            : this.i18n.t('toast.couldNotCreateCategory'),
          detail: this.apiError(err, this.i18n.t('toast.apiRejected')),
        });
      },
    });
  }

  confirmDelete(category: Category): void {
    this.confirmationService.confirm({
      header: this.i18n.t('categories.deleteHeader'),
      message: this.i18n.t('categories.deleteMessage', { name: category.name }),
      icon: 'pi pi-trash',
      acceptButtonProps: { label: this.i18n.t('common.delete'), severity: 'danger' },
      rejectButtonProps: { label: this.i18n.t('common.cancel'), severity: 'secondary', outlined: true },
      accept: () => {
        this.categoryService.remove(category.id).subscribe({
          next: () => {
            this.reload();
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('toast.deleted'),
              detail: category.name,
            });
          },
          // 409 = listings still use it: surface the API's problem-details "detail" (with the count).
          error: (err) =>
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('toast.deleteFailed'),
              detail: this.apiError(err, this.i18n.t('toast.apiRejected')),
            }),
        });
      },
    });
  }

  kindSeverity(kind: CategoryKind): 'info' | 'warn' {
    return kind === 'Product' ? 'info' : 'warn';
  }

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; detail?: string; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    return this.i18n.apiMessage(first?.[0] ?? error?.detail ?? error?.title ?? fallback);
  }
}
