import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressBarModule } from 'primeng/progressbar';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ProductService } from '../../core/product.service';
import type { Product, ProductInput } from '../../core/models';

const emptyDraft = (): ProductInput => ({ name: '', sku: '', category: '', price: 0, stock: 0 });

@Component({
  selector: 'app-products',
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
    ProgressBarModule,
    SelectModule,
    SkeletonModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss',
})
export class ProductsComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly loading = signal(true);
  readonly products = signal<Product[]>([]);
  readonly categories = signal<string[]>([]);

  readonly search = signal('');
  readonly category = signal<string | null>(null);

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);

  draft: ProductInput = emptyDraft();

  readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const category = this.category();
    return this.products().filter(
      (p) =>
        (!term || p.name.toLowerCase().includes(term) || p.sku.toLowerCase().includes(term)) &&
        (!category || p.category === category),
    );
  });

  readonly title = computed(() =>
    this.editingId() ? 'Edit product' : 'New product',
  );

  ngOnInit(): void {
    this.reload();
    this.productService.categories().subscribe((c) => this.categories.set(c));
  }

  reload(): void {
    this.loading.set(true);
    this.productService.list().subscribe({
      next: (products) => {
        this.products.set(products);
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
    this.category.set(null);
  }

  openNew(): void {
    this.editingId.set(null);
    this.draft = emptyDraft();
    this.dialogVisible.set(true);
  }

  openEdit(product: Product): void {
    this.editingId.set(product.id);
    this.draft = {
      name: product.name,
      sku: product.sku,
      category: product.category,
      price: product.price,
      stock: product.stock,
    };
    this.dialogVisible.set(true);
  }

  save(): void {
    const draft = this.draft;
    if (!draft.name.trim() || !draft.sku.trim() || !draft.category.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Missing fields',
        detail: 'Name, SKU and category are required.',
      });
      return;
    }

    this.saving.set(true);
    const id = this.editingId();
    const request$ = id ? this.productService.update(id, draft) : this.productService.create(draft);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: id ? 'Product updated' : 'Product created',
          detail: draft.name,
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

  confirmDelete(product: Product): void {
    this.confirmationService.confirm({
      header: 'Delete product',
      message: `Remove <strong>${product.name}</strong> from the catalogue?`,
      icon: 'pi pi-trash',
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.productService.remove(product.id).subscribe({
          next: () => {
            this.reload();
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: product.name,
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

  statusSeverity(status: string): 'success' | 'warn' | 'danger' {
    switch (status) {
      case 'Active':
        return 'success';
      case 'Low stock':
        return 'warn';
      default:
        return 'danger';
    }
  }

  stockPercent(product: Product): number {
    return Math.min(100, Math.round((product.stock / 200) * 100));
  }

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    return first?.[0] ?? error?.title ?? fallback;
  }
}
