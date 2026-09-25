import { inject, Pipe, PipeTransform } from '@angular/core';

import { I18nService } from './i18n.service';

/**
 * Template pipe for `I18nService.t`: `{{ 'nav.products' | t }}` or with
 * placeholders `{{ 'toast.updated' | t: { name: product.name } }}`.
 *
 * Deliberately **impure**: it re-evaluates on every change-detection cycle so
 * an active language switch updates the view immediately without threading
 * the language through every binding. The lookups are dictionary walks over
 * bundled JSON, which is cheap for this app's binding count.
 */
@Pipe({ name: 't', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(I18nService);

  transform(key: string, params?: Record<string, string | number>): string {
    return this.i18n.t(key, params);
  }
}
