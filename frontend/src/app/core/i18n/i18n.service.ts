import { computed, Injectable, signal } from '@angular/core';

import arJson from './ar.json';
import enJson from './en.json';

export type Lang = 'en' | 'ar';

/** Nested translation dictionary (the shape of the en.json / ar.json files). */
type Dict = { [key: string]: string | Dict };

/**
 * Runtime English/Arabic switcher for the dashboard.
 *
 * Both dictionaries are bundled statically (no fetch → no flash of missing
 * keys), the active language lives in a signal so every `t()` call in a
 * template re-evaluates on switch, and the chosen language is persisted to
 * `localStorage['marketplace.lang']`.
 *
 * `setLang()` also flips `<html lang>` and `<html dir>`, which drives the
 * RTL layout for Arabic app-wide.
 */
@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly dicts: Record<Lang, Dict> = { en: enJson, ar: arJson };

  /** Active language. */
  readonly lang = signal<Lang>(readInitialLang());

  /** Text direction for the active language. */
  readonly dir = computed<'ltr' | 'rtl'>(() => (this.lang() === 'ar' ? 'rtl' : 'ltr'));

  constructor() {
    this.applyDocument();
  }

  /**
   * Translate `key` (a dot path such as `nav.products`) in the active
   * language, falling back to English and finally to the key itself (with a
   * console warning so missing keys are easy to spot during development).
   * `{{param}}` placeholders are replaced from `params`.
   */
  t(key: string, params?: Record<string, string | number>): string {
    const raw = lookup(this.dicts[this.lang()], key) ?? lookup(this.dicts.en, key);
    if (raw === undefined) {
      console.warn(`[i18n] missing key: ${key}`);
      return key;
    }
    if (!params) return raw;
    return raw.replace(/\{\{(\w+)\}\}/g, (match, name: string) =>
      name in params ? String(params[name]) : match,
    );
  }

  /**
   * Label for a dynamic value (a status, a role, …): looks up
   * `prefix.<value>` lower-cased and falls back to the raw value, so unknown
   * server-side values still display.
   */
  label(prefix: string, value: string | null | undefined): string {
    if (!value) return '';
    const key = `${prefix}.${value.toLowerCase()}`;
    const raw = lookup(this.dicts[this.lang()], key) ?? lookup(this.dicts.en, key);
    return raw ?? value;
  }

  /**
   * Map a known English API problem-detail message to the active language.
   * Scans the `errors.*` subtree of the English dictionary for an exact
   * match and re-translates it; anything unknown passes through unchanged
   * (the API itself stays English-only — see README).
   */
  apiMessage(detail: string | null | undefined): string {
    if (!detail) return detail ?? '';
    const errors = this.dicts.en['errors'];
    if (typeof errors === 'object' && errors !== null) {
      for (const [key, value] of Object.entries(errors)) {
        if (value === detail) return this.t(`errors.${key}`);
      }
    }
    return detail;
  }

  /** Switch the active language, persist it and re-point `<html>` at it. */
  setLang(lang: Lang): void {
    if (this.lang() === lang) return;
    this.lang.set(lang);
    localStorage.setItem('marketplace.lang', lang);
    this.applyDocument();
  }

  private applyDocument(): void {
    document.documentElement.lang = this.lang();
    document.documentElement.dir = this.dir();
  }
}

function readInitialLang(): Lang {
  const stored = localStorage.getItem('marketplace.lang');
  return stored === 'ar' ? 'ar' : 'en';
}

function lookup(dict: Dict | undefined, key: string): string | undefined {
  let node: string | Dict | undefined = dict;
  for (const part of key.split('.')) {
    if (typeof node !== 'object' || node === null) return undefined;
    node = (node as Dict)[part];
  }
  return typeof node === 'string' ? node : undefined;
}
