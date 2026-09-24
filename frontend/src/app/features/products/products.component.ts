import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { ProductService } from '../../core/product.service';
import { UsersService } from '../../core/users.service';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { Product, ProductInput, UserProfile } from '../../core/models';

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
  private readonly usersService = inject(UsersService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  /** Keystrokes are debounced before they trigger an API round-trip. */
  private readonly searchInput$ = new Subject<string>();

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly products = signal<Product[]>([]);
  readonly total = signal(0);
  readonly categories = signal<string[]>([]);
  readonly sellers = signal<UserProfile[]>([]);

  readonly search = signal('');
  readonly category = signal<string | null>(null);

  // Server-side pagination/sorting, mirrored from the PrimeNG lazy events.
  readonly first = signal(0);
  readonly pageSize = signal(8);
  readonly sortField = signal<string | null>(null);
  readonly sortOrder = signal(1); // 1 = asc, -1 = desc

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);

  draft: ProductInput = emptyDraft();

  /** Providers and dashboard admins may list/manage products; others get 403 from the API. */
  readonly canList = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager', 'Provider'].includes(this.auth.user()?.role ?? ''),
  );

  readonly title = computed(() =>
    this.editingId() ? 'Edit product' : 'New product',
  );

  ngOnInit(): void {
    this.reload();
    this.productService.categories().subscribe({ next: (c) => this.categories.set(c), error: () => {} });
    // Owner names for the Seller column; non-admins only receive the public provider directory.
    this.usersService.list({ pageSize: 100 }).subscribe({
      next: (r) => this.sellers.set(r.items),
      error: () => {},
    });
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reloadFirstPage());
  }

  /** Fetches the current page from the API using filters + lazy sort/page state. */
  reload(): void {
    this.loading.set(true);
    const pageSize = this.pageSize();
    this.productService
      .list({
        search: this.search() || undefined,
        category: this.category() ?? undefined,
        page: Math.floor(this.first() / pageSize) + 1,
        pageSize,
        sortBy: this.sortField() ?? undefined,
        sortDir: this.sortOrder() === -1 ? 'desc' : 'asc',
      })
      .subscribe({
        next: (res) => {
          this.products.set(res.items);
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
    this.sortOrder.set(event.sortOrder ?? 1);
    this.reload();
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.searchInput$.next(value);
  }

  onCategory(value: string | null): void {
    this.category.set(value);
    this.reloadFirstPage();
  }

  clearFilters(): void {
    this.search.set('');
    this.category.set(null);
    this.reloadFirstPage();
  }

  private reloadFirstPage(): void {
    this.first.set(0);
    this.reload();
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

  /** Display name of the listing's owner; `Platform` for unowned demo items. */
  sellerName(sellerId?: number | null): string {
    if (sellerId == null) {
      return 'Platform';
    }
    return this.sellers().find((u) => u.id === sellerId)?.name ?? `#${sellerId}`;
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
