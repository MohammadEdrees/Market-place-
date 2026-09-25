import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Textarea } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { RolesService } from '../../core/roles.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import type { Role, RoleInput } from '../../core/models';

const emptyDraft = (): RoleInput => ({ name: '', description: '' });

@Component({
  selector: 'app-roles',
  imports: [
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    InputTextModule,
    SkeletonModule,
    TableModule,
    Textarea,
    ToastModule,
    TooltipModule,
    TranslatePipe,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss',
})
export class RolesComponent implements OnInit {
  private readonly rolesService = inject(RolesService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  readonly i18n = inject(I18nService);

  /** Every reload funnels through here so switchMap cancels stale in-flight requests. */
  private readonly reload$ = new Subject<void>();

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly roles = signal<Role[]>([]);

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  /** `null` while creating; the role id when editing. */
  readonly editingId = signal<number | null>(null);

  draft: RoleInput = emptyDraft();

  /** SuperAdmin, Admin and Manager may manage roles (the API returns 403 otherwise). */
  readonly canManageRoles = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager'].includes(this.auth.user()?.role ?? ''),
  );

  readonly title = computed(() =>
    this.editingId() ? this.i18n.t('roles.edit') : this.i18n.t('roles.add'),
  );

  ngOnInit(): void {
    // Single fetch pipeline: switchMap cancels the previous request as soon as a newer
    // trigger arrives, so responses can never arrive out of order.
    this.reload$
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          return this.rolesService.list().pipe(
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
      .subscribe((roles) => {
        this.roles.set(roles);
        this.loadedOnce.set(true);
        this.loading.set(false);
      });

    this.reload();
  }

  /** Asks the fetch pipeline for the full list (cancels any in-flight request). */
  reload(): void {
    this.reload$.next();
  }

  /** Opens the "Add role" dialog with a fresh draft (admins/Managers only). */
  openCreate(): void {
    this.editingId.set(null);
    this.draft = emptyDraft();
    this.dialogVisible.set(true);
  }

  openEdit(role: Role): void {
    this.editingId.set(role.id);
    this.draft = { name: role.name, description: role.description ?? '' };
    this.dialogVisible.set(true);
  }

  /** Creates (`POST`) or renames (`PUT`) the role server-side. */
  save(): void {
    const name = this.draft.name.trim();
    if (!name) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('toast.missingFields'),
        detail: this.i18n.t('validation.roleNameRequired'),
      });
      return;
    }

    const input: RoleInput = { name, description: this.draft.description?.trim() || null };
    const editing = this.editingId();

    this.saving.set(true);
    const request =
      editing != null ? this.rolesService.update(editing, input) : this.rolesService.create(input);

    request.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: editing != null ? this.i18n.t('toast.roleUpdated') : this.i18n.t('toast.roleCreated'),
          detail: this.i18n.t('toast.roleSavedDetail', { name: saved.name, count: saved.userCount }),
        });
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: editing != null
            ? this.i18n.t('toast.couldNotEditRole')
            : this.i18n.t('toast.couldNotCreateRole'),
          detail: this.apiError(err, this.i18n.t('toast.apiRejected')),
        });
      },
    });
  }

  confirmDelete(role: Role): void {
    this.confirmationService.confirm({
      header: this.i18n.t('roles.deleteHeader'),
      message: this.i18n.t('roles.deleteMessage', { name: role.name }),
      icon: 'pi pi-trash',
      acceptButtonProps: { label: this.i18n.t('common.delete'), severity: 'danger' },
      rejectButtonProps: { label: this.i18n.t('common.cancel'), severity: 'secondary', outlined: true },
      accept: () => {
        this.rolesService.remove(role.id).subscribe({
          next: () => {
            this.reload();
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('toast.deleted'),
              detail: role.name,
            });
          },
          // 409 = role in use: surface the API's "Reassign the N account(s)…" detail.
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

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; detail?: string; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    return this.i18n.apiMessage(first?.[0] ?? error?.detail ?? error?.title ?? fallback);
  }
}
