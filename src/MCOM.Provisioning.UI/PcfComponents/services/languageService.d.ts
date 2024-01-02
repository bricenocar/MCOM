export declare class LanguageService {
    private static instance;
    private _locale;
    private _resources;
    private _defaultLocale;
    private constructor();
    static getInstance(locale?: string): LanguageService;
    initializeLocale(locale: string): void;
    getResource(key: string): string;
    getLocale(): string;
    private importLocaleResources;
}
