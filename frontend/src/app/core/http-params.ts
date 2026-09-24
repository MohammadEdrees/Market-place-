import { HttpParams } from '@angular/common/http';

/**
 * Converts a plain query object to `HttpParams`, skipping `null`, `undefined`
 * and empty-string values so optional filters stay out of the URL.
 */
export function toParams(
  query: Record<string, string | number | boolean | null | undefined>,
): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value != null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
