import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './models';

/** Turns an API error into one sentence the user can act on. */
export function problemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ProblemDetails | null;
    if (problem?.errors) {
      return Object.values(problem.errors).flat().join(' ');
    }
    if (problem?.detail) {
      return problem.detail;
    }
    if (error.status === 0) {
      return 'The server cannot be reached. Check that the API is running.';
    }
  }
  return 'Something went wrong. Try again.';
}
