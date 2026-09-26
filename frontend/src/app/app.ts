import { Component, computed, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { filter } from 'rxjs';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';

import { AuthService } from './core/auth.service';
import { I18nService, Lang } from './core/i18n/i18n.service';
import { TranslatePipe } from './core/i18n/t.pipe';

interface NavItem {
  /** Translation key (see `nav.*` in core/i18n/en.json), not display text. */
  labelKey: string;
  icon: string;
  link?: string;
  soon?: boolean;
}

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    AvatarModule,
    ButtonModule,
    SelectModule,
    TooltipModule,
    TranslatePipe,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);

  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly darkMode = signal(false);
  readonly apiUrl = 'localhost:5240';

  /** Translation key for the current route segment (see `nav.*` / `page.login`). */
  private readonly pageKey = signal('nav.dashboard');

  /** Current route title, re-translated whenever the language changes. */
  readonly pageTitle = computed(() => this.i18n.t(this.pageKey()));

  /** Language choices for the topbar switcher — endonyms, never translated. */
  readonly langOptions: { label: string; value: Lang }[] = [
    { label: 'English', value: 'en' },
    { label: 'العربية', value: 'ar' },
  ];

  /** The login page renders full-screen; everything else gets the sidebar/topbar chrome. */
  readonly showShell = signal(!this.router.url.startsWith('/login'));

  /** Initials for the topbar avatar, derived from the signed-in user's name. */
  readonly initials = computed(() => {
    const name = this.auth.user()?.name ?? 'MW';
    return name
      .split(/\s+/)
      .map((part) => part[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  });

  readonly primaryNav: NavItem[] = [
    { labelKey: 'nav.dashboard', icon: 'pi pi-home', link: '/dashboard' },
    { labelKey: 'nav.products', icon: 'pi pi-box', link: '/products' },
    { labelKey: 'nav.services', icon: 'pi pi-wrench', link: '/services' },
    { labelKey: 'nav.orders', icon: 'pi pi-shopping-cart', link: '/orders' },
    { labelKey: 'nav.categories', icon: 'pi pi-tags', link: '/categories' },
    { labelKey: 'nav.advertisements', icon: 'pi pi-megaphone', link: '/advertisements' },
    { labelKey: 'nav.users', icon: 'pi pi-users', link: '/users' },
    { labelKey: 'nav.roles', icon: 'pi pi-key', link: '/roles' },
    { labelKey: 'nav.subscriptions', icon: 'pi pi-credit-card', link: '/subscriptions' },
    { labelKey: 'nav.settings', icon: 'pi pi-cog', link: '/settings' },
  ];

  readonly workspaceNav: NavItem[] = [
    { labelKey: 'nav.reports', icon: 'pi pi-chart-bar', soon: true },
    { labelKey: 'nav.team', icon: 'pi pi-id-card', soon: true },
  ];

  constructor() {
    if (localStorage.getItem('marketplace.darkMode') === 'true') {
      this.darkMode.set(true);
      document.documentElement.classList.add('dark-mode');
    }

    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((e) => {
        const url = e.urlAfterRedirects;
        this.showShell.set(!url.startsWith('/login'));
        const segment = url.split('?')[0].split('/').filter(Boolean)[0] ?? '';
        const keys: Record<string, string> = {
          login: 'page.login',
          dashboard: 'nav.dashboard',
          products: 'nav.products',
          services: 'nav.services',
          orders: 'nav.orders',
          categories: 'nav.categories',
          advertisements: 'nav.advertisements',
          users: 'nav.users',
          roles: 'nav.roles',
          subscriptions: 'nav.subscriptions',
          settings: 'page.settings',
        };
        // Store the *key*; pageTitle re-translates it on every switch.
        this.pageKey.set(keys[segment] ?? 'nav.dashboard');
      });

    // Browser tab title: refreshed on navigation (pageTitle) and on every
    // language switch, beating the router's default static title strategy.
    effect(() => {
      document.title = `${this.pageTitle()} · Market Workplace`;
    });
  }

  toggleDarkMode(): void {
    const next = !this.darkMode();
    this.darkMode.set(next);
    document.documentElement.classList.toggle('dark-mode', next);
    localStorage.setItem('marketplace.darkMode', String(next));
  }

  logout(): void {
    this.auth.logout();
  }
}
