import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { OrdersService } from '../../core/orders.service';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { MarketOrder } from '../../core/models';

const STATUSES = ['Processing', 'Confirmed', 'Completed', 'Cancelled', 'Reserved', 'Refunded'] as const;

@Component({
  selector: 'app-orders',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
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
  ],
  providers: [MessageService],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss',
})
export class OrdersComponent implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly messageService = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);

  /** Keystrokes are debounced before they trigger an API round-trip. */
  private readonly searchInput$ = new Subject<string>();

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

  readonly kindOptions = [
    { label: 'Purchases', value: 'Product' },
    { label: 'Reservations', value: 'Service' },
  ];
  readonly statusOptions = STATUSES.map((s) => ({ label: s, value: s }));

  ngOnInit(): void {
    this.reload();
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reloadFirstPage());
  }

  /** Fetches the current page from the API using filters + lazy sort/page state. */
  reload(): void {
    this.loading.set(true);
    const pageSize = this.pageSize();
    this.ordersService
      .list({
        search: this.search() || undefined,
        kind: this.kind() ?? undefined,
        status: this.status() ?? undefined,
        page: Math.floor(this.first() / pageSize) + 1,
        pageSize,
        sortBy: this.sortField() ?? undefined,
        sortDir: this.sortOrder() === -1 ? 'desc' : 'asc',
      })
      .subscribe({
        next: (res) => {
          this.orders.set(res.items);
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
    this.sortOrder.set(event.sortOrder ?? -1);
    this.reload();
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.searchInput$.next(value);
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
    this.search.set('');
    this.kind.set(null);
    this.status.set(null);
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
          summary: 'Status updated',
          detail: `Order #${order.id} → ${this.newStatus()}`,
        });
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Update failed',
          detail: this.apiError(err, 'Only admins or the listing owner can change this status.'),
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
    return first?.[0] ?? error?.title ?? fallback;
  }
}
