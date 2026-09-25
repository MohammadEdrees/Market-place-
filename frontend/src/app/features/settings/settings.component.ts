import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { AuthService } from '../../core/auth.service';
import { BackupService, backupFileName, type RestoreMode } from '../../core/backup.service';
import { I18nService, type Lang } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import type { BackupFile, RestoreReport } from '../../core/models';

/**
 * Settings page: workspace preferences (language) plus the backup & restore tools for
 * dashboard admins. A backup is a JSON snapshot of every table — the API keeps the newest
 * ten under `App_Data/backups` and can merge or replace the live data with one.
 */
@Component({
  selector: 'app-settings',
  imports: [
    DatePipe,
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    SelectModule,
    SelectButtonModule,
    SkeletonModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule,
    TranslatePipe,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit {
  private readonly backupService = inject(BackupService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  /** Backup and restore are admin-only; the API answers 403 for everyone else. */
  readonly isAdmin = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager'].includes(this.auth.user()?.role ?? ''),
  );

  readonly loading = signal(true);
  readonly working = signal(false);
  readonly backups = signal<BackupFile[]>([]);
  readonly lastReport = signal<RestoreReport | null>(null);

  /** Chosen snapshot to restore and how to apply it. */
  readonly restoreMode = signal<RestoreMode>('merge');
  readonly restoreModes = computed<{ label: string; value: RestoreMode }[]>(() => [
    { label: this.i18n.t('settings.modeMerge'), value: 'merge' },
    { label: this.i18n.t('settings.modeReplace'), value: 'replace' },
  ]);
  readonly selectedFile = signal<File | null>(null);

  readonly langOptions: { label: string; value: Lang }[] = [
    { label: 'English', value: 'en' },
    { label: 'العربية', value: 'ar' },
  ];

  ngOnInit(): void {
    this.loadBackups();
  }

  // ------------------------------------------------------------------ backup

  loadBackups(): void {
    if (!this.isAdmin()) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.backupService.list().subscribe({
      next: (files) => {
        this.backups.set(files);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast('error', this.i18n.t('toast.backupFailed'), '');
      },
    });
  }

  /** Creates a snapshot: the API stores a copy and the file is saved to the downloads. */
  createBackup(): void {
    this.working.set(true);
    this.backupService.export().subscribe({
      next: (res) => {
        this.working.set(false);
        const name = backupFileName(res.headers.get('Content-Disposition'));
        this.saveBlob(res.body!, name);
        this.toast('success', this.i18n.t('toast.backupCreated'), name);
        this.loadBackups();
      },
      error: () => {
        this.working.set(false);
        this.toast('error', this.i18n.t('toast.backupFailed'), '');
      },
    });
  }

  downloadBackup(file: BackupFile): void {
    this.backupService.download(file.name).subscribe({
      next: (res) => this.saveBlob(res.body!, file.name),
      error: () => this.toast('error', this.i18n.t('toast.backupFailed'), ''),
    });
  }

  removeBackup(file: BackupFile): void {
    this.confirmationService.confirm({
      header: this.i18n.t('settings.deleteTitle'),
      message: this.i18n.t('settings.deleteMessage', { name: file.name }),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.i18n.t('common.delete'),
      rejectLabel: this.i18n.t('common.cancel'),
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.backupService.remove(file.name).subscribe({
          next: () => {
            this.toast('success', this.i18n.t('toast.backupDeleted'), file.name);
            this.loadBackups();
          },
          error: () => this.toast('error', this.i18n.t('toast.backupDeleteFailed'), ''),
        });
      },
    });
  }

  // ----------------------------------------------------------------- restore

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  restore(): void {
    const file = this.selectedFile();
    if (!file) {
      this.toast('warn', this.i18n.t('settings.pickFirst'), '');
      return;
    }

    const mode = this.restoreMode();
    this.confirmationService.confirm({
      header: this.i18n.t('settings.restoreTitle'),
      message:
        mode === 'replace'
          ? this.i18n.t('settings.restoreReplaceWarning', { name: file.name })
          : this.i18n.t('settings.restoreMergeWarning', { name: file.name }),
      icon: mode === 'replace' ? 'pi pi-exclamation-triangle' : 'pi pi-info-circle',
      acceptLabel: this.i18n.t('settings.restoreAction'),
      rejectLabel: this.i18n.t('common.cancel'),
      acceptButtonStyleClass: mode === 'replace' ? 'p-button-danger' : undefined,
      accept: () => this.runRestore(file, mode),
    });
  }

  private runRestore(file: File, mode: RestoreMode): void {
    this.working.set(true);
    this.backupService.restore(file, mode).subscribe({
      next: (report) => {
        this.working.set(false);
        this.lastReport.set(report);
        this.selectedFile.set(null);
        this.toast(
          'success',
          this.i18n.t('toast.restoreDone'),
          this.i18n.t('settings.restoreSummary', {
            inserted: report.totalInserted,
            updated: report.totalUpdated,
          }),
        );
      },
      error: (err) => {
        this.working.set(false);
        this.toast('error', this.i18n.t('toast.restoreFailed'), err?.error?.detail ?? '');
      },
    });
  }

  // ------------------------------------------------------------------ shared

  /** Human-readable file size (KB/MB) for the backup table. */
  size(bytes: number): string {
    return bytes >= 1024 * 1024
      ? `${(bytes / (1024 * 1024)).toFixed(1)} MB`
      : `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }

  /** One-line row summary of what a snapshot holds, e.g. "24 products · 6 services". */
  countsText(file: BackupFile): string {
    const counts = file.counts ?? {};
    const parts: string[] = [];
    if (counts['products']) parts.push(this.i18n.t('settings.rowsProducts', { count: counts['products'] }));
    if (counts['services']) parts.push(this.i18n.t('settings.rowsServices', { count: counts['services'] }));
    if (counts['orders']) parts.push(this.i18n.t('settings.rowsOrders', { count: counts['orders'] }));
    if (counts['users']) parts.push(this.i18n.t('settings.rowsUsers', { count: counts['users'] }));
    return parts.join(' · ');
  }

  private saveBlob(blob: Blob, name: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = name;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private toast(severity: 'success' | 'error' | 'warn' | 'info', summary: string, detail: string): void {
    this.messageService.add({ severity, summary, detail });
  }
}
