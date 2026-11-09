import { Component, input } from '@angular/core';

@Component({
  selector: 'app-error-box',
  template: `
    @if (errors().length > 0) {
      <div class="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">
        @if (errors().length === 1) {
          {{ errors()[0] }}
        } @else {
          <ul class="list-disc space-y-0.5 pl-4">
            @for (error of errors(); track error) {
              <li>{{ error }}</li>
            }
          </ul>
        }
      </div>
    }
  `,
})
export class ErrorBox {
  readonly errors = input<string[]>([]);
}
