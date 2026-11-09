import { AbstractControl } from '@angular/forms';

export function fieldError(control: AbstractControl, label: string): string | null {
  if (!control.touched || !control.errors) {
    return null;
  }

  const errors = control.errors;
  if (errors['required']) return `${label} is required.`;
  if (errors['minlength']) return `${label} must be at least ${errors['minlength'].requiredLength} characters.`;
  if (errors['maxlength']) return `${label} can't be longer than ${errors['maxlength'].requiredLength} characters.`;
  if (errors['min']) return `${label} must be ${errors['min'].min} or later.`;
  if (errors['max']) return `${label} must be ${errors['max'].max} or earlier.`;
  return `${label} is not valid.`;
}
