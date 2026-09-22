import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

import { AuthService } from './core/auth.service';

interface NavItem {
  label: string;
  icon: string;
  link?: string;
  soon?: boolean;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AvatarModule, ButtonModule, TooltipModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);

  readonly auth = inject(AuthService);
  readonly darkMode = signal(false);
  readonly pageTitle = signal('Dashboard');
  readonly apiUrl = 'localhost:5240';

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
    { label: 'Dashboard', icon: 'pi pi-home', link: '/dashboard' },
    { label: 'Products', icon: 'pi pi-box', link: '/products' },
  ];

  readonly workspaceNav: NavItem[] = [
    { label: 'Reports', icon: 'pi pi-chart-bar', soon: true },
    { label: 'Team', icon: 'pi pi-users', soon: true },
    { label: 'Settings', icon: 'pi pi-cog', soon: true },
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
        this.pageTitle.set(url.includes('products') ? 'Products' : 'Dashboard');
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
