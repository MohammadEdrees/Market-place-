import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
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
import { OrdersService } from '../../core/orders.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { MarketOrder } from '../../core/models';

const STATUSES = ['Processing', 'Confirmed', 'Completed', 'Cancelled', 'Reserved', 'Refunded'] as const;

@Component({
  selector: 'app-orders',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
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
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss',
})
export class OrdersComponent implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly messageService = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);
  readonly i18n = inject(I18nService);

  /** Search box as a reactive control: valueChanges → debounce → distinct → fetch. */
  readonly searchControl = new FormControl('', { nonNullable: true });

  /** Every reload funnels through here so switchMap cancels stale in-flight requests. */
  private readonly reload$ = new Subject<void>();

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly orders = signal<MarketOrder[]>([]);
  readonly total = signal(0);

  readonly search = signal('');
  readonly kind = signal<string | null>(null);
  readonly status = signal<string | null>(null);

  // Server-side pagination/sorting, mirrored from the PrimeNG lazy events.
  readonly first = signal(0);
  readonly pageSize = signal(8);
  readonly sortField = signal<string | null>(null);
  readonly sortOrder = signal(-1); // orders default to newest first

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly active = signal<MarketOrder | null>(null);
  readonly newStatus = signal<string>('');

  readonly kindOptions = computed(() => [
    { label: this.i18n.t('orders.purchases'), value: 'Product' },
    { label: this.i18n.t('orders.reservations'), value: 'Service' },
  ]);
  readonly statusOptions = computed(() =>
    STATUSES.map((s) => ({ label: this.i18n.label('status', s), value: s })),
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
          return this.ordersService
            .list({
              search: this.search() || undefined,
              kind: this.kind() ?? undefined,
              status: this.status() ?? undefined,
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
        this.orders.set(res.items);
        this.total.set(res.total);
        this.loadedOnce.set(true);
        this.loading.set(false);
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
    this.sortOrder.set(event.sortOrder ?? -1);
    this.reload();
  }

  onKind(value: string | null): void {
    this.kind.set(value);
    this.reloadFirstPage();
  }

  onStatus(value: string | null): void {
    this.status.set(value);
    this.reloadFirstPage();
  }

  clearFilters(): void {
    this.kind.set(null);
    this.status.set(null);
    this.search.set('');
    // Suppress the valueChanges pipeline — the manual reload below covers it.
    this.searchControl.setValue('', { emitEvent: false });
    this.reloadFirstPage();
  }

  private reloadFirstPage(): void {
    this.first.set(0);
    this.reload();
  }

  openStatus(order: MarketOrder): void {
    this.active.set(order);
    this.newStatus.set(order.status);
    this.dialogVisible.set(true);
  }

  saveStatus(): void {
    const order = this.active();
    if (!order || this.newStatus() === order.status) {
      this.dialogVisible.set(false);
      return;
    }

    this.saving.set(true);
    this.ordersService.updateStatus(order.id, this.newStatus()).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('toast.statusUpdated'),
          detail: this.i18n.t('toast.statusUpdatedDetail', {
            id: order.id,
            status: this.i18n.label('status', this.newStatus()),
          }),
        });
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('toast.updateFailed'),
          detail: this.apiError(err, this.i18n.t('toast.statusForbidden')),
        });
      },
    });
  }

  kindSeverity(kind: string): 'info' | 'warn' {
    return kind === 'Service' ? 'warn' : 'info';
  }

  statusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'Processing':
      case 'Confirmed':
        return 'info';
      case 'Reserved':
        return 'warn';
      case 'Cancelled':
      case 'Refunded':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    return this.i18n.apiMessage(first?.[0] ?? error?.title ?? fallback);
  }
}
