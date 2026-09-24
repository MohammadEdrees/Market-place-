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
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { Textarea } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { AuthService } from '../../core/auth.service';
import { ServicesService } from '../../core/services.service';
import { UsersService } from '../../core/users.service';
import type { ServiceInput, ServiceListing, UserProfile } from '../../core/models';

const emptyDraft = (): ServiceInput => ({
  title: '',
  description: '',
  category: '',
  cost: 0,
  contactInfo: '',
  location: '',
  offers: '',
});

@Component({
  selector: 'app-services',
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
    SelectModule,
    SkeletonModule,
    TableModule,
    TagModule,
    Textarea,
    ToastModule,
    TooltipModule,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss',
})
export class ServicesComponent implements OnInit {
  private readonly servicesService = inject(ServicesService);
  private readonly usersService = inject(UsersService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly services = signal<ServiceListing[]>([]);
  readonly categories = signal<string[]>([]);
  readonly users = signal<UserProfile[]>([]);

  readonly search = signal('');
  readonly category = signal<string | null>(null);

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);

  draft: ServiceInput = emptyDraft();

  /** Providers and dashboard admins may list/manage services; others get 403 from the API. */
  readonly canList = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager', 'Provider'].includes(this.auth.user()?.role ?? ''),
  );

  readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const category = this.category();
    return this.services().filter(
      (s) =>
        (!term ||
          s.title.toLowerCase().includes(term) ||
          s.description.toLowerCase().includes(term) ||
          s.location.toLowerCase().includes(term)) &&
        (!category || s.category === category),
    );
  });

  readonly title = computed(() => (this.editingId() ? 'Edit service' : 'New service'));

  ngOnInit(): void {
    this.reload();
    this.servicesService.categories().subscribe({
      next: (c) => this.categories.set(c),
      error: () => {},
    });
    // Provider names for the table; non-admins only receive the public provider directory.
    this.usersService.list().subscribe({
      next: (u) => this.users.set(u),
      error: () => {},
    });
  }

  reload(): void {
    this.loading.set(true);
    this.servicesService.list().subscribe({
      next: (services) => {
        this.services.set(services);
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

  openEdit(service: ServiceListing): void {
    this.editingId.set(service.id);
    this.draft = {
      title: service.title,
      description: service.description,
      category: service.category,
      cost: service.cost,
      contactInfo: service.contactInfo,
      location: service.location,
      offers: service.offers,
    };
    this.dialogVisible.set(true);
  }

  save(): void {
    const draft = this.draft;
    if (!draft.title.trim() || !draft.category.trim() || !draft.contactInfo.trim() || !draft.location.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Missing fields',
        detail: 'Title, category, contact info and location are required.',
      });
      return;
    }

    this.saving.set(true);
    const id = this.editingId();
    const request$ = id ? this.servicesService.update(id, draft) : this.servicesService.create(draft);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: id ? 'Service updated' : 'Service created',
          detail: draft.title,
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

  confirmDelete(service: ServiceListing): void {
    this.confirmationService.confirm({
      header: 'Delete service',
      message: `Remove <strong>${service.title}</strong> from the marketplace?`,
      icon: 'pi pi-trash',
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => {
        this.servicesService.remove(service.id).subscribe({
          next: () => {
            this.reload();
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: service.title,
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

  providerName(providerId: number): string {
    return this.users().find((u) => u.id === providerId)?.name ?? `#${providerId}`;
  }

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    return first?.[0] ?? error?.title ?? fallback;
  }
}
