import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

const fallback = 'Something went wrong. Please try again.';

export function serverErrors(error: unknown): string[] {
  if (!(error instanceof HttpErrorResponse)) {
    return [fallback];
  }
  if (error.status === 0) {
    return ["Can't reach the server. Check that the API is running."];
  }

  const problem = error.error as ProblemDetails | null;
  const fieldErrors = Object.values(problem?.errors ?? {}).flat();
  return fieldErrors.length > 0 ? fieldErrors : [problem?.title ?? fallback];
}
