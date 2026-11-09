import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { serverErrors } from '../core/api-errors';
import { fieldError } from '../core/field-error';
import { ErrorBox } from '../shared/error-box';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, ErrorBox],
  template: `
    <div class="flex min-h-dvh items-center justify-center px-4 py-12">
      <div class="w-full max-w-sm">
        <h1 class="text-center text-2xl font-semibold text-gray-900">Create an account</h1>
        <p class="mt-1 text-center text-sm text-gray-600">You'll be logged in right away</p>

        <form
          [formGroup]="form"
          (ngSubmit)="submit()"
          novalidate
          class="mt-8 space-y-5 rounded-lg border border-gray-200 bg-white p-6"
        >
          <app-error-box [errors]="errors()" />

          <div>
            <label for="username" class="label">Username</label>
            <input id="username" formControlName="username" class="input" autocomplete="username" />
            @if (fieldError(form.controls.username, 'Username'); as message) {
              <p class="field-error">{{ message }}</p>
            } @else {
              <p class="mt-1 text-xs text-gray-500">3 to 50 characters.</p>
            }
          </div>

          <div>
            <label for="password" class="label">Password</label>
            <div class="relative">
              <input
                id="password"
                [type]="showPassword() ? 'text' : 'password'"
                formControlName="password"
                class="input pr-16"
                autocomplete="new-password"
              />
              <button
                type="button"
                class="absolute inset-y-0 right-0 px-3 text-xs font-medium text-gray-600 hover:text-gray-900"
                [attr.aria-label]="showPassword() ? 'Hide password' : 'Show password'"
                (click)="showPassword.set(!showPassword())"
              >
                {{ showPassword() ? 'Hide' : 'Show' }}
              </button>
            </div>
            @if (fieldError(form.controls.password, 'Password'); as message) {
              <p class="field-error">{{ message }}</p>
            } @else {
              <p class="mt-1 text-xs text-gray-500">At least 5 characters.</p>
            }
          </div>

          <button type="submit" class="btn-primary w-full" [disabled]="submitting()">
            {{ submitting() ? 'Creating account…' : 'Create account' }}
          </button>
        </form>

        <p class="mt-6 text-center text-sm text-gray-600">
          Already have an account?
          <a routerLink="/login" class="btn-link">Log in</a>
        </p>
      </div>
    </div>
  `,
})
export class Register {
  private authService = inject(AuthService);
  private router = inject(Router);

  protected readonly form = new FormGroup({
    username: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3), Validators.maxLength(50)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    }),
  });

  protected readonly showPassword = signal(false);
  protected readonly submitting = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly fieldError = fieldError;

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { username, password } = this.form.getRawValue();

    this.submitting.set(true);
    this.errors.set([]);
    this.authService.register({ username: username.trim(), password }).subscribe({
      next: () => this.router.navigate(['/releases']),
      error: (error) => {
        this.submitting.set(false);
        this.errors.set(serverErrors(error));
      },
    });
  }
}
