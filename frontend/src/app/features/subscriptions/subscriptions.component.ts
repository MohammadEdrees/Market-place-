import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { EMPTY, catchError } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { SubscriptionService } from '../../core/subscription.service';
import { UsersService } from '../../core/users.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/t.pipe';
import type {
  BillingCycle,
  Subscription,
  SubscriptionCreateInput,
  SubscriptionPlan,
  SubscriptionStatus,
  UserProfile,
} from '../../core/models';

/** Dialog draft — dates are held as `Date` and serialized to ISO on save. */
interface SubDraft {
  /** `null` until an account is picked (create mode only). */
  userId: number | null;
  plan: SubscriptionPlan;
  price: number;
  billingCycle: BillingCycle;
  status: SubscriptionStatus;
  startsAt: Date | null;
  endsAt: Date | null;
  autoRenew: boolean;
}

/** Roles the API accepts for subscription management (`Access.IsAdmin`). */
const MANAGE_ROLES = ['SuperAdmin', 'Admin', 'Manager'];

const emptyDraft = (): SubDraft => ({
  userId: null,
  plan: 'Basic',
  price: 9.99,
  billingCycle: 'Monthly',
  status: 'Active',
  startsAt: new Date(),
  endsAt: null,
  autoRenew: true,
});

/** Offers readable option lists without translating the raw API values. */
type Opt = { label: string; value: string };

@Component({
  selector: 'app-subscriptions',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DatePickerModule,
    DialogModule,
    IconFieldModule,
    InputIconModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    SkeletonModule,
    TableModule,
    TagModule,
    ToastModule,
    ToggleSwitchModule,
    TooltipModule,
    TranslatePipe,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.scss',
})
export class SubscriptionsComponent implements OnInit {
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly usersService = inject(UsersService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  /** Subscriptions, newest first. */
  readonly subs = signal<Subscription[]>([]);

  /** Accounts offered by the create dialog (loaded on first open). */
  private readonly accounts = signal<UserProfile[]>([]);

  readonly loadedOnce = signal(false);
  readonly loading = signal(false);

  // --- filters (client side: the endpoint returns the whole, small list) ---
  readonly search = signal('');
  readonly planFilter = signal<SubscriptionPlan | null>(null);
  readonly statusFilter = signal<SubscriptionStatus | null>(null);

  // --- dialog ---
  readonly dialogOpen = signal(false);
  readonly editing = signal<Subscription | null>(null);
  readonly saving = signal(false);
  readonly draft = signal<SubDraft>(emptyDraft());

  /** `SuperAdmin` / `Admin` / `Manager` — matches `Access.IsAdmin` on the API. */
  readonly canManage = computed(() => MANAGE_ROLES.includes(this.auth.user()?.role ?? ''));

  readonly planOptions = computed<Opt[]>(() =>
    (['Basic', 'Premium', 'Enterprise'] as SubscriptionPlan[]).map((plan) => ({
      label: this.i18n.label('plan', plan),
      value: plan,
    })),
  );

  readonly cycleOptions = computed<Opt[]>(() =>
    (['Monthly', 'Yearly'] as BillingCycle[]).map((cycle) => ({
      label: this.i18n.label('cycle', cycle),
      value: cycle,
    })),
  );

  readonly statusOptions = computed<Opt[]>(() =>
    (['Active', 'Inactive', 'Expired'] as SubscriptionStatus[]).map((status) => ({
      label: this.i18n.label('status', status),
      value: status,
    })),
  );

  /** Accounts not already carrying a plan are listed first in the picker. */
  readonly accountOptions = computed(() => {
    const taken = new Set(this.subs().map((s) => s.userId));
    return this.accounts()
      .slice()
      .sort((a, b) => Number(taken.has(a.id)) - Number(taken.has(b.id)) || a.name.localeCompare(b.name))
      .map((u) => ({ label: `${u.name} · ${u.email}`, value: u.id }));
  });

  /** Rows after search + plan + status filtering. */
  readonly visible = computed(() => {
    const term = this.search().trim().toLowerCase();
    const plan = this.planFilter();
    const status = this.statusFilter();
    return this.subs().filter(
      (s) =>
        (!plan || s.plan === plan) &&
        (!status || s.status === status) &&
        (!term ||
          s.userName.toLowerCase().includes(term) ||
          s.userEmail.toLowerCase().includes(term) ||
          s.plan.toLowerCase().includes(term)),
    );
  });

  ngOnInit(): void {
    this.reload();
  }

  /** Re-fetch the list; failures surface as an unreachable-API toast. */
  reload(): void {
    this.loading.set(true);
    this.subscriptionService
      .list()
      .pipe(
        catchError(() => {
          // Keep the skeleton (nothing has loaded yet) but stop the spinner and say why.
          this.loading.set(false);
          this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('toast.apiUnreachable'),
            detail: this.i18n.t('toast.apiUnreachableDetail'),
          });
          return EMPTY;
        }),
      )
      .subscribe((rows) => {
        this.subs.set(rows);
        this.loadedOnce.set(true);
        this.loading.set(false);
      });
  }

  onSearch(value: string): void {
    this.search.set(value);
  }

  onPlan(value: SubscriptionPlan | null): void {
    this.planFilter.set(value);
  }

  onStatus(value: SubscriptionStatus | null): void {
    this.statusFilter.set(value);
  }

  clearFilters(): void {
    this.search.set('');
    this.planFilter.set(null);
    this.statusFilter.set(null);
  }

  /** Merge a partial edit into the dialog draft (immutable so signals fire). */
  patchDraft(patch: Partial<SubDraft>): void {
    this.draft.update((current) => ({ ...current, ...patch }));
  }

  openCreate(): void {
    this.editing.set(null);
    this.draft.set(emptyDraft());
    this.dialogOpen.set(true);
    if (this.accounts().length === 0) this.loadAccounts();
  }

  openEdit(row: Subscription): void {
    this.editing.set(row);
    this.draft.set({
      userId: row.userId,
      plan: row.plan,
      price: row.price,
      billingCycle: row.billingCycle,
      status: row.status,
      startsAt: new Date(row.startsAt),
      endsAt: row.endsAt ? new Date(row.endsAt) : null,
      autoRenew: row.autoRenew,
    });
    this.dialogOpen.set(true);
  }

  save(): void {
    const draft = this.draft();
    const creating = this.editing() === null;

    // `userId` is only ever `null` on a fresh draft (create mode); a loaded row
    // always carries its account, so one guard covers both paths and narrows the type.
    if (draft.userId === null) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('validation.accountRequired'),
        detail: this.i18n.t('subscriptions.pickAccount'),
      });
      return;
    }
    if (draft.startsAt && draft.endsAt && draft.endsAt.getTime() <= draft.startsAt.getTime()) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('validation.scheduleOrder'),
        detail: this.i18n.t('subscriptions.endsAfterStarts'),
      });
      return;
    }

    this.saving.set(true);
    const iso = (d: Date | null) => (d ? d.toISOString() : null);

    const request$ = creating
      ? this.subscriptionService.create({
          userId: draft.userId,
          plan: draft.plan,
          price: draft.price,
          billingCycle: draft.billingCycle,
          startsAt: iso(draft.startsAt),
          endsAt: iso(draft.endsAt),
          autoRenew: draft.autoRenew,
        } satisfies SubscriptionCreateInput)
      : this.subscriptionService.update(this.editing()!.id, {
          plan: draft.plan,
          price: draft.price,
          billingCycle: draft.billingCycle,
          status: draft.status,
          endsAt: iso(draft.endsAt),
          autoRenew: draft.autoRenew,
        });

    // Deliberately no `catchError(... EMPTY)`: the `error` callback below is the
    // handler — swallowing the error would leave the dialog stuck on "Saving…".
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogOpen.set(false);
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t(creating ? 'toast.subCreated' : 'toast.subUpdated'),
        });
        this.reload();
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('toast.apiRejected'),
        });
      },
    });
  }

  confirmDelete(row: Subscription): void {
    this.confirmationService.confirm({
      header: this.i18n.t('subscriptions.deleteHeader'),
      message: this.i18n.t('subscriptions.deleteMessage', {
        plan: this.i18n.label('plan', row.plan),
        name: row.userName,
      }),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: this.i18n.t('common.delete'), severity: 'danger' },
      rejectButtonProps: { label: this.i18n.t('common.cancel'), severity: 'secondary', outlined: true },
      accept: () => this.remove(row),
    });
  }

  /** True once a dated plan has run past its end (styled struck-through). */
  isEnded(row: Subscription): boolean {
    return row.endsAt !== null && new Date(row.endsAt).getTime() < Date.now();
  }

  planSeverity(plan: SubscriptionPlan): 'info' | 'success' | 'contrast' {
    return plan === 'Premium' ? 'success' : plan === 'Enterprise' ? 'contrast' : 'info';
  }

  statusSeverity(status: SubscriptionStatus): 'success' | 'warn' | 'danger' {
    return status === 'Active' ? 'success' : status === 'Inactive' ? 'warn' : 'danger';
  }

  private remove(row: Subscription): void {
    this.subscriptionService.remove(row.id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.i18n.t('toast.deleted') });
        this.reload();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.i18n.t('toast.deleteFailed') });
      },
    });
  }

  /** One-shot account list used by the create picker. */
  private loadAccounts(): void {
    this.usersService
      .list({ pageSize: 100, sortBy: 'name', sortDir: 'asc' })
      .pipe(
        catchError(() => {
          this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('toast.apiUnreachable'),
            detail: this.i18n.t('toast.apiUnreachableDetail'),
          });
          return EMPTY;
        }),
      )
      .subscribe((page) => this.accounts.set(page.items));
  }
}
