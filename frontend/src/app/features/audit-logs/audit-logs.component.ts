import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs';
import { AuditService } from '../../core/audit.service';
import { AuthService } from '../../core/auth.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { AuditAction, AuditLog } from '../../core/models';

/**
 * Read-only view of the audit trail (`GET /api/auditlogs`).
 *
 * The API writes entries itself — via middleware, after each successful mutation —
 * so this page has no create/edit/delete controls at all. It is the one list in the
 * dashboard that only ever grows until the retention cap prunes it, which is why
 * filtering, sorting and paging are all delegated to the server.
 */
@Component({
  selector: 'app-audit-logs',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    DatePipe,
    DialogModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    SelectModule,
    SkeletonModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule,
    TranslatePipe,
  ],
  providers: [MessageService],
  templateUrl: './audit-logs.component.html',
  styleUrl: './audit-logs.component.scss',
})
export class AuditLogsComponent implements OnInit {
  private readonly auditService = inject(AuditService);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  readonly i18n = inject(I18nService);

  /** Search box as a reactive control: valueChanges → debounce → distinct → fetch. */
  readonly searchControl = new FormControl('', { nonNullable: true });

  /** Every reload funnels through here so switchMap cancels stale in-flight requests. */
  private readonly reload$ = new Subject<void>();

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly entries = signal<AuditLog[]>([]);
  readonly total = signal(0);

  readonly search = signal('');
  readonly action = signal<string | null>(null);
  readonly entity = signal<string | null>(null);

  // Server-side pagination/sorting, mirrored from the PrimeNG lazy events.
  readonly first = signal(0);
  readonly pageSize = signal(8);
  readonly sortField = signal<string | null>(null);
  readonly sortOrder = signal(-1); // 1 = asc, -1 = desc — the trail starts newest-first

  readonly viewVisible = signal(false);
  readonly viewing = signal<AuditLog | null>(null);

  /**
   * The trail is admin-only (`Access.IsAdmin`), so the same three roles that may
   * manage accounts are the ones allowed to read it. A `403` at runtime — a role
   * revoked mid-session — flips `forbidden` and swaps in the same panel.
   */
  readonly isAdmin = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager'].includes(this.auth.user()?.role ?? ''),
  );
  readonly forbidden = signal(false);

  /** Actions the API can record; drives the filter and the translated tags. */
  readonly actions: AuditAction[] = ['create', 'update', 'delete', 'login', 'register', 'restore'];

  /** Route families the API serves; anything unknown would fall back to its raw value. */
  readonly areas = [
    'products',
    'services',
    'orders',
    'categories',
    'advertisements',
    'users',
    'roles',
    'subscriptions',
    'backup',
    'auth',
  ];

  readonly actionOptions = computed(() =>
    this.actions.map((action) => ({ label: this.i18n.label('action', action), value: action })),
  );
  readonly areaOptions = computed(() =>
    this.areas.map((area) => ({ label: this.i18n.label('entity', area), value: area })),
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
          return this.auditService
            .list({
              search: this.search() || undefined,
              action: this.action() ?? undefined,
              entity: this.entity() ?? undefined,
              page: Math.floor(this.first() / pageSize) + 1,
              pageSize,
              sortBy: this.sortField() ?? undefined,
              sortDir: this.sortOrder() === -1 ? 'desc' : 'asc',
            })
            .pipe(
              catchError((err: { status?: number }) => {
                this.loadedOnce.set(true);
                this.loading.set(false);
                // A revoked role must not masquerade as an outage: show the panel instead.
                if (err?.status === 403) {
                  this.forbidden.set(true);
                  return EMPTY;
                }
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
        this.entries.set(res.items);
        this.total.set(res.total);
        this.loadedOnce.set(true);
        this.loading.set(false);
      });

    // Non-admins see the "administrators only" panel — never a doomed 403 request.
    if (this.isAdmin()) {
      this.reload();
    } else {
      this.loadedOnce.set(true);
      this.loading.set(false);
    }
  }

  /** Asks the fetch pipeline for the current page (cancels any in-flight request). */
  reload(): void {
    if (!this.isAdmin() || this.forbidden()) {
      return;
    }
    this.reload$.next();
  }

  /** Pagination and column sorting arrive as a single PrimeNG lazy event. */
  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first.set(event.first ?? 0);
    if (event.rows != null) {
      this.pageSize.set(event.rows);
    }
    // The server whitelists sort keys (`createdAt`, `user`, `action`, `entity`,
    // `duration`, `path`); unknown values fall back to newest-first.
    this.sortField.set(event.sortField != null ? String(event.sortField) : null);
    this.sortOrder.set(event.sortOrder ?? -1);
    this.reload();
  }

  onAction(value: string | null): void {
    this.action.set(value);
    this.reloadFirstPage();
  }

  onArea(value: string | null): void {
    this.entity.set(value);
    this.reloadFirstPage();
  }

  clearFilters(): void {
    this.action.set(null);
    this.entity.set(null);
    this.search.set('');
    // Suppress the valueChanges pipeline — the manual reload below covers it.
    this.searchControl.setValue('', { emitEvent: false });
    this.reloadFirstPage();
  }

  private reloadFirstPage(): void {
    this.first.set(0);
    this.reload();
  }

  openView(entry: AuditLog): void {
    this.viewing.set(entry);
    this.viewVisible.set(true);
  }

  /** Tag colour per action — destructive work reads red at a glance. */
  actionSeverity(action: string): 'danger' | 'warn' | 'info' | 'success' | 'secondary' {
    switch (action) {
      case 'delete':
        return 'danger';
      case 'register':
      case 'restore':
        return 'warn';
      case 'create':
        return 'success';
      case 'update':
        return 'info';
      default:
        return 'secondary';
    }
  }

  /** `#12` when the call targeted one record, nothing for collection-level calls. */
  target(entry: AuditLog): string {
    return entry.entityId != null ? `#${entry.entityId}` : '';
  }
}
