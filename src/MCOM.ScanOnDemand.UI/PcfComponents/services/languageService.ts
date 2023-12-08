import { locales } from "../utilities/locales";

export class LanguageService {
  private static instance: LanguageService;
  private _locale: string;
  private _resources;
  private _defaultLocale = 'en-us'; // English as default language

  private constructor(locale: string) {
    // Use the specified locale or default to English
    this._locale = locale || this._defaultLocale;
  }

  public static getInstance(locale?: string): LanguageService {
    if (!LanguageService.instance) {
      LanguageService.instance = new LanguageService(locale);
    }
    return LanguageService.instance;
  }

  public initializeLocale(locale: string): void {
    try {
      // Load resources for the specified locale
      this._resources = this.importLocaleResources(locale) || this.importLocaleResources(this._defaultLocale);
      // Update the locale
      this._locale = locale;
    } catch (error) {
      console.error(`Error loading resources for locale ${locale}:`, error);
      // Fallback to English or any default language
      this._resources = this.importLocaleResources(this._defaultLocale);
    }
  }

  public getResource(key: string): string {
    if (this._resources && key in this._resources) {
      return this._resources[key];
    } else {
      console.error(`Resource key not found: ${key}`);
      return key; // Fallback to the key itself
    }
  }

  public getLocale(): string {
    return this._locale;
  }

  private importLocaleResources = (loc: string) => {
    // Replace _ with - if it's the case and normalize
    const normalizedLoc = loc.replace('_', '-').toLowerCase();
    if (locales[normalizedLoc]) {
      return locales[normalizedLoc];
    } else {
      // Handle the case where the locale is not found
      console.error(`Locale not found: ${loc}`);
      return null;
    }
  };
}