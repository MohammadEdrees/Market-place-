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
import { Textarea } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { UsersService } from '../../core/users.service';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { UserProfile, UserUpdateInput } from '../../core/models';

const emptyDraft = (): UserUpdateInput => ({ name: '', phone: '', location: '', bio: '' });

@Component({
  selector: 'app-users',
  imports: [
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
    Textarea,
    ToastModule,
    TooltipModule,
  ],
  providers: [MessageService],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  /** Keystrokes are debounced before they trigger an API round-trip. */
  private readonly searchInput$ = new Subject<string>();

  readonly loadedOnce = signal(false);
  readonly loading = signal(true);
  readonly users = signal<UserProfile[]>([]);
  readonly total = signal(0);

  readonly search = signal('');
  readonly role = signal<string | null>(null);
  readonly type = signal<string | null>(null);

  // Server-side pagination/sorting, mirrored from the PrimeNG lazy events.
  readonly first = signal(0);
  readonly pageSize = signal(8);
  readonly sortField = signal<string | null>(null);
  readonly sortOrder = signal(1); // 1 = asc, -1 = desc

  readonly viewVisible = signal(false);
  readonly viewing = signal<UserProfile | null>(null);

  readonly editVisible = signal(false);
  readonly saving = signal(false);
  draft: UserUpdateInput = emptyDraft();

  readonly roleOptions = ['SuperAdmin', 'Admin', 'Manager', 'Viewer', 'Provider', 'Client'];
  readonly typeOptions = ['Dashboard', 'Mobile'];

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
    this.usersService
      .list({
        search: this.search() || undefined,
        role: this.role() ?? undefined,
        type: this.type() ?? undefined,
        page: Math.floor(this.first() / pageSize) + 1,
        pageSize,
        sortBy: this.sortField() ?? undefined,
        sortDir: this.sortOrder() === -1 ? 'desc' : 'asc',
      })
      .subscribe({
        next: (res) => {
          this.users.set(res.items);
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

  onRole(value: string | null): void {
    this.role.set(value);
    this.reloadFirstPage();
  }

  onType(value: string | null): void {
    this.type.set(value);
    this.reloadFirstPage();
  }

  clearFilters(): void {
    this.search.set('');
    this.role.set(null);
    this.type.set(null);
    this.reloadFirstPage();
  }

  private reloadFirstPage(): void {
    this.first.set(0);
    this.reload();
  }

  /** Only your own profile can be edited (the API only accepts `PUT /api/users/me`). */
  isSelf(user: UserProfile): boolean {
    return user.id === this.auth.user()?.id;
  }

  openView(user: UserProfile): void {
    this.viewing.set(user);
    this.viewVisible.set(true);
  }

  openEdit(user: UserProfile): void {
    this.draft = {
      name: user.name,
      phone: user.phone ?? '',
      location: user.location ?? '',
      bio: user.bio ?? '',
    };
    this.editVisible.set(true);
  }

  save(): void {
    const draft = this.draft;
    if (!draft.name?.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Missing fields',
        detail: 'Display name is required.',
      });
      return;
    }

    this.saving.set(true);
    this.usersService.updateMe(draft).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.editVisible.set(false);
        this.viewVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: 'Profile updated',
          detail: updated.name,
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

  initials(name: string): string {
    return name
      .split(/\s+/)
      .map((part) => part[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  roleSeverity(role: string): 'danger' | 'warn' | 'info' | 'success' | 'secondary' {
    switch (role) {
      case 'SuperAdmin':
        return 'danger';
      case 'Admin':
        return 'warn';
      case 'Manager':
        return 'info';
      case 'Provider':
        return 'success';
      case 'Viewer':
        return 'secondary';
      default:
        return 'info';
    }
  }

  typeSeverity(type: string): 'info' | 'success' {
    return type === 'Mobile' ? 'success' : 'info';
  }

  private apiError(err: unknown, fallback: string): string {
    const error = (err as { error?: { errors?: Record<string, string[]>; title?: string } })?.error;
    const first = error?.errors ? Object.values(error.errors)[0] : undefined;
    return first?.[0] ?? error?.title ?? fallback;
  }
}
