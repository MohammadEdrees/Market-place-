import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
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
import { OrdersService } from '../../core/orders.service';
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

  readonly loading = signal(true);
  readonly orders = signal<MarketOrder[]>([]);

  readonly search = signal('');
  readonly kind = signal<string | null>(null);
  readonly status = signal<string | null>(null);

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly active = signal<MarketOrder | null>(null);
  readonly newStatus = signal<string>('');

  readonly kindOptions = [
    { label: 'Purchases', value: 'Product' },
    { label: 'Reservations', value: 'Service' },
  ];
  readonly statusOptions = STATUSES.map((s) => ({ label: s, value: s }));

  readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const kind = this.kind();
    const status = this.status();
    return this.orders().filter(
      (o) =>
        (!term ||
          o.customer.toLowerCase().includes(term) ||
          o.product.toLowerCase().includes(term) ||
          o.category.toLowerCase().includes(term)) &&
        (!kind || o.kind === kind) &&
        (!status || o.status === status),
    );
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.ordersService.list().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'API unreachable',
          detail: 'Start the .NET API on localhost:5240.',
        });
      },
    });
  }

  clearFilters(): void {
    this.search.set('');
    this.kind.set(null);
    this.status.set(null);
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
