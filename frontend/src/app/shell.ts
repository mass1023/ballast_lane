import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="border-b border-gray-200 bg-white">
      <div class="mx-auto flex max-w-5xl items-center justify-between gap-4 px-4 py-3">
        <a routerLink="/releases" class="font-semibold text-gray-900">Record Label Catalog</a>
        <div class="flex items-center gap-4 text-sm">
          <span class="hidden text-gray-600 sm:inline">{{ auth.username() }}</span>
          <button type="button" class="btn-secondary" (click)="auth.logout()">Log out</button>
        </div>
      </div>
    </header>

    <main class="mx-auto max-w-5xl px-4 py-6">
      <router-outlet />
    </main>
  `,
})
export class Shell {
  protected readonly auth = inject(AuthService);
}
