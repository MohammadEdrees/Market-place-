import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Chart, registerables, type ChartOptions } from 'chart.js';
import { ChartModule } from 'primeng/chart';
import { ProgressBarModule } from 'primeng/progressbar';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DashboardService } from '../../core/dashboard.service';
import { ProductService } from '../../core/product.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import type { DashboardResponse, Product } from '../../core/models';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DatePipe, ChartModule, ProgressBarModule, SkeletonModule, TableModule, TagModule, TranslatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly productService = inject(ProductService);
  readonly i18n = inject(I18nService);

  readonly loading = signal(true);
  readonly data = signal<DashboardResponse | null>(null);
  readonly products = signal<Product[]>([]);

  readonly inventory = computed(() => {
    const all = this.products();
    return {
      total: all.length,
      low: this.data()?.lowStockCount ?? 0,
      out: all.filter((p) => p.stock === 0).length,
      healthyPercent: all.length
        ? Math.round(((all.length - (this.data()?.lowStockCount ?? 0)) / all.length) * 100)
        : 100,
    };
  });

  readonly lineData = computed(() => {
    const trend = this.data()?.revenueTrend ?? [];
    return {
      labels: trend.map((p) => p.label),
      datasets: [
        {
          label: this.i18n.t('dashboard.chartRevenue'),
          data: trend.map((p) => p.revenue),
          borderColor: '#6366f1',
          backgroundColor: 'rgba(99, 102, 241, 0.18)',
          fill: true,
          tension: 0.4,
          pointRadius: 0,
          pointHoverRadius: 5,
          yAxisID: 'y',
        },
        {
          label: this.i18n.t('dashboard.chartOrders'),
          data: trend.map((p) => p.orders),
          borderColor: '#12b76a',
          backgroundColor: 'transparent',
          borderDash: [6, 4],
          tension: 0.4,
          pointRadius: 0,
          pointHoverRadius: 5,
          yAxisID: 'y1',
        },
      ],
    };
  });

  readonly lineOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: { labels: { usePointStyle: true, boxWidth: 8 } },
      tooltip: {
        callbacks: {
          // Resolved per tooltip so the active language is always current.
          label: (item) =>
            item.dataset.label === this.i18n.t('dashboard.chartRevenue')
              ? ` ${this.i18n.t('dashboard.chartRevenue')}: $${Number(item.parsed.y).toLocaleString()}`
              : ` ${this.i18n.t('dashboard.chartOrders')}: ${item.parsed.y}`,
        },
      },
    },
    scales: {
      x: { grid: { display: false }, ticks: { maxRotation: 0 } },
      y: {
        position: 'left',
        grid: { color: 'rgba(102, 112, 133, 0.15)' },
        ticks: { callback: (value) => `$${Number(value).toLocaleString()}` },
      },
      y1: { position: 'right', grid: { drawOnChartArea: false } },
    },
  };

  readonly doughnutData = computed(() => {
    const shares = this.data()?.salesByCategory ?? [];
    const palette = ['#6366f1', '#12b76a', '#f79009', '#f04438', '#0ba5ec', '#7a5af8'];
    return {
      labels: shares.map((s) => s.label),
      datasets: [
        {
          data: shares.map((s) => s.value),
          backgroundColor: palette,
          borderWidth: 0,
          hoverOffset: 6,
        },
      ],
    };
  });

  readonly doughnutOptions: ChartOptions<'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '68%',
    plugins: {
      legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 8, padding: 14 } },
      tooltip: {
        callbacks: { label: (item) => ` ${item.label}: $${Number(item.parsed).toLocaleString()}` },
      },
    },
  };

  /** Best-sellers ranking as a horizontal bar chart (all-time revenue). */
  readonly topProductsData = computed(() => {
    const top = this.data()?.topProducts ?? [];
    return {
      labels: top.map((p) => p.name),
      datasets: [
        {
          label: this.i18n.t('dashboard.chartRevenue'),
          data: top.map((p) => p.revenue),
          backgroundColor: 'rgba(99, 102, 241, 0.85)',
          hoverBackgroundColor: '#6366f1',
          borderRadius: 6,
          barThickness: 18,
        },
      ],
    };
  });

  readonly topProductsOptions: ChartOptions<'bar'> = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: { label: (item) => ` $${Number(item.parsed.x).toLocaleString()}` },
      },
    },
    scales: {
      x: {
        grid: { color: 'rgba(102, 112, 133, 0.15)' },
        ticks: { callback: (value) => `$${Number(value).toLocaleString()}` },
      },
      y: { grid: { display: false } },
    },
  };

  /** Order status breakdown, largest first (server already sorts it). */
  readonly statusBreakdown = computed(() => this.data()?.ordersByStatus ?? []);

  /** Audience figures; empty placeholders keep the panel stable before data arrives. */
  readonly audience = computed(() => {
    const stats = this.data()?.userStats;
    return [
      { key: 'total', label: this.i18n.t('dashboard.totalAccounts'), value: stats?.total ?? 0, icon: 'users' },
      { key: 'clients', label: this.i18n.t('dashboard.clients'), value: stats?.clients ?? 0, icon: 'user' },
      { key: 'providers', label: this.i18n.t('dashboard.providers'), value: stats?.providers ?? 0, icon: 'briefcase' },
      { key: 'new', label: this.i18n.t('dashboard.newUsers'), value: stats?.newLast30 ?? 0, icon: 'user-plus' },
    ];
  });

  ngOnInit(): void {
    this.dashboardService.getDashboard().subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    // Full catalogue for the inventory widget (unpaginated by nature, capped at the API max of 100).
    this.productService.list({ pageSize: 100 }).subscribe((res) => this.products.set(res.items));
  }

  statusSeverity(status: string): 'success' | 'warn' | 'danger' | 'info' {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'Processing':
        return 'warn';
      case 'Refunded':
        return 'danger';
      default:
        return 'info';
    }
  }

  /** Bar colour per order status, mirroring the tag severities above. */
  statusColor(status: string): string {
    switch (status) {
      case 'Completed':
        return '#12b76a';
      case 'Processing':
        return '#f79009';
      case 'Refunded':
      case 'Cancelled':
        return '#f04438';
      case 'Reserved':
        return '#7a5af8';
      case 'Confirmed':
        return '#0ba5ec';
      default:
        return '#667085';
    }
  }
}
