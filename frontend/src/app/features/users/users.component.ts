import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
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
import { Textarea } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { ProductService } from '../../core/product.service';
import { UsersService } from '../../core/users.service';
import type { TableLazyLoadEvent } from 'primeng/table';
import type { Product, UserCreateInput, UserProfile, UserUpdateInput } from '../../core/models';

const emptyDraft = (): UserUpdateInput => ({ name: '', phone: '', location: '', bio: '' });

const emptyCreateDraft = (): UserCreateInput => ({
  name: '',
  email: '',
  password: '',
  role: 'Viewer',
  phone: '',
  location: '',
  bio: '',
});

@Component({
  selector: 'app-users',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    CurrencyPipe,
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
  private readonly productService = inject(ProductService);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  /** Search box as a reactive control: valueChanges → debounce → distinct → fetch. */
  readonly searchControl = new FormControl('', { nonNullable: true });

  /** Every reload funnels through here so switchMap cancels stale in-flight requests. */
  private readonly reload$ = new Subject<void>();

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

  // Who is being edited: admins may edit anyone, everyone else only themselves
  // (the API enforces this — PUT /api/users/{id} is admins only).
  readonly editingSelf = signal(true);
  private editingId = 0;
  editEmail = '';
  editRole = 'Viewer';
  /** Optional password reset; blank keeps the current password. */
  editPassword = '';

  // Products owned by the profile being viewed/edited (relevant for providers).
  readonly relatedProducts = signal<Product[]>([]);
  readonly relatedIsProvider = signal(false);
  /** Show the products section for providers (even when they have none yet). */
  readonly showRelatedProducts = computed(() => this.relatedIsProvider() || this.relatedProducts().length > 0);
  private relatedFor = 0;

  // Staged profile-picture change (applied together with the text fields on save).
  readonly currentAvatar = signal<string | null>(null);
  readonly avatarPreview = signal<string | null>(null);
  /** Display source: a staged local pick, else the stored picture (empty → initials bubble). */
  readonly avatarDisplay = computed(() => this.avatarPreview() ?? this.currentAvatar() ?? '');
  private avatarFile: File | null = null;
  private avatarToRemove = false;

  // Admin-created accounts (POST /api/users).
  readonly createVisible = signal(false);
  readonly creating = signal(false);
  createDraft: UserCreateInput = emptyCreateDraft();

  readonly roleOptions = ['SuperAdmin', 'Admin', 'Manager', 'Viewer', 'Provider', 'Client'];
  readonly typeOptions = ['Dashboard', 'Mobile'];

  /** SuperAdmin, Admin and Manager may create and edit accounts (the API returns 403 otherwise). */
  readonly canManageUsers = computed(() =>
    ['SuperAdmin', 'Admin', 'Manager'].includes(this.auth.user()?.role ?? ''),
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
          return this.usersService
            .list({
              search: this.search() || undefined,
              role: this.role() ?? undefined,
              type: this.type() ?? undefined,
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
                  summary: 'API unreachable',
                  detail: 'Start the .NET API on localhost:5240.',
                });
                return EMPTY;
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        this.users.set(res.items);
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
    this.sortOrder.set(event.sortOrder ?? 1);
    this.reload();
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
    this.role.set(null);
    this.type.set(null);
    this.search.set('');
    // Suppress the valueChanges pipeline — the manual reload below covers it.
    this.searchControl.setValue('', { emitEvent: false });
    this.reloadFirstPage();
  }

  private reloadFirstPage(): void {
    this.first.set(0);
    this.reload();
  }

  /** True for the signed-in user's own row (self-service edits go through `PUT /api/users/me`). */
  isSelf(user: UserProfile): boolean {
    return user.id === this.auth.user()?.id;
  }

  openView(user: UserProfile): void {
    this.viewing.set(user);
    this.loadRelatedProducts(user);
    this.viewVisible.set(true);
  }

  openEdit(user: UserProfile): void {
    this.editingId = user.id;
    this.editingSelf.set(this.isSelf(user));
    this.editEmail = user.email;
    this.editRole = user.role;
    this.editPassword = '';
    this.draft = {
      name: user.name,
      phone: user.phone ?? '',
      location: user.location ?? '',
      bio: user.bio ?? '',
    };
    this.resetAvatarEditor(user.imagePath ?? null);
    this.loadRelatedProducts(user);
    this.editVisible.set(true);
  }

  /** Loads the products owned by the profile being viewed (shown in both dialogs). */
  private loadRelatedProducts(user: UserProfile): void {
    this.relatedFor = user.id;
    this.relatedProducts.set([]);
    this.relatedIsProvider.set(user.role === 'Provider');
    this.productService.list({ sellerId: user.id, pageSize: 100 }).subscribe({
      next: (res) => {
        if (this.relatedFor === user.id) {
          this.relatedProducts.set(res.items ?? []);
        }
      },
      error: () => {
        if (this.relatedFor === user.id) {
          this.relatedProducts.set([]);
        }
      },
    });
  }

  private resetAvatarEditor(current: string | null): void {
    if (this.avatarPreview()) {
      URL.revokeObjectURL(this.avatarPreview()!);
    }
    this.currentAvatar.set(current);
    this.avatarPreview.set(null);
    this.avatarFile = null;
    this.avatarToRemove = false;
  }

  /** Stages a newly chosen picture (local preview only — uploaded when the dialog is saved). */
  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    if (!file) {
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.messageService.add({ severity: 'warn', summary: 'Image too large', detail: 'Maximum size is 5 MB.' });
      return;
    }
    if (!file.type.startsWith('image/')) {
      this.messageService.add({ severity: 'warn', summary: 'Not an image', detail: 'Choose a PNG, JPG, WEBP or GIF.' });
      return;
    }

    if (this.avatarPreview()) {
      URL.revokeObjectURL(this.avatarPreview()!);
    }
    this.avatarFile = file;
    this.avatarToRemove = false;
    this.avatarPreview.set(URL.createObjectURL(file));
  }

  /** Drops the picture from the preview; the removal itself runs on save. */
  removeAvatarImage(): void {
    if (this.avatarPreview()) {
      URL.revokeObjectURL(this.avatarPreview()!);
    }
    this.avatarPreview.set(null);
    this.avatarFile = null;
    if (this.currentAvatar()) {
      this.avatarToRemove = true;
    }
    this.currentAvatar.set(null);
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

    const adminEdit = !this.editingSelf();
    if (adminEdit) {
      if (!this.editEmail.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.editEmail.trim())) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Missing fields',
          detail: 'A valid email address is required.',
        });
        return;
      }
      if (this.editPassword && this.editPassword.length < 6) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Password too short',
          detail: 'Use at least 6 characters.',
        });
        return;
      }
    }

    this.saving.set(true);
    if (adminEdit) {
      // Admin edit of another account: PUT /api/users/{id} (profile + email/role + optional reset).
      this.usersService
        .updateUser(this.editingId, {
          name: draft.name!.trim(),
          email: this.editEmail.trim(),
          role: this.editRole,
          phone: draft.phone,
          location: draft.location,
          bio: draft.bio,
          password: this.editPassword || null,
        })
        .subscribe({
          next: (updated) => this.applyAvatarChange(updated, 'User updated'),
          error: (err) => {
            this.saving.set(false);
            this.messageService.add({
              severity: 'error',
              summary: 'Could not edit user',
              detail: this.apiError(err, 'The API rejected the request.'),
            });
          },
        });
      return;
    }

    this.usersService.updateMe(draft).subscribe({
      next: (updated) => this.applyAvatarChange(updated),
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

  /** Applies the staged picture change (upload / delete) after the text fields saved. */
  private applyAvatarChange(updated: UserProfile, summary = 'Profile updated'): void {
    const finish = () => {
      this.saving.set(false);
      this.editVisible.set(false);
      this.viewVisible.set(false);
      this.resetAvatarEditor(null);
      this.reload();
      this.messageService.add({
        severity: 'success',
        summary,
        detail: updated.name,
      });
    };
    const failed = (err: unknown) => {
      this.saving.set(false);
      this.messageService.add({
        severity: 'error',
        summary: 'Picture change failed',
        detail: this.apiError(err, 'Your details were saved, but the picture change was rejected.'),
      });
    };

    if (this.avatarFile) {
      const file = this.avatarFile;
      this.avatarFile = null;
      this.usersService.uploadAvatar(updated.id, file).subscribe({ next: finish, error: failed });
    } else if (this.avatarToRemove) {
      this.avatarToRemove = false;
      this.usersService.removeAvatar(updated.id).subscribe({ next: finish, error: failed });
    } else {
      finish();
    }
  }

  /** Opens the "Add user" dialog with a fresh draft (admins/Managers only). */
  openCreate(): void {
    this.createDraft = emptyCreateDraft();
    this.createVisible.set(true);
  }

  /** Creates the account server-side; the platform (Dashboard/Mobile) is derived from the role. */
  saveNew(): void {
    const draft = this.createDraft;
    if (!draft.name.trim() || !draft.email.trim() || !draft.password) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Missing fields',
        detail: 'Name, email and password are required.',
      });
      return;
    }
    if (draft.password.length < 6) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Password too short',
        detail: 'Use at least 6 characters.',
      });
      return;
    }

    this.creating.set(true);
    this.usersService.create(draft).subscribe({
      next: (created) => {
        this.creating.set(false);
        this.createVisible.set(false);
        this.reload();
        this.messageService.add({
          severity: 'success',
          summary: 'User created',
          detail: `${created.name} · ${created.role} · ${created.type}`,
        });
      },
      error: (err) => {
        this.creating.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Could not create user',
          detail: this.apiError(err, 'The API rejected the request.'),
        });
      },
    });
  }

  /** Preview text for the platform that will be assigned on create. */
  derivedType(role: string): string {
    return role === 'Provider' || role === 'Client' ? 'Mobile' : 'Dashboard';
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
