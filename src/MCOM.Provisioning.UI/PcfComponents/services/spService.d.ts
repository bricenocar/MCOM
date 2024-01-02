export declare class SPService {
    private static instance;
    private spoServiceUrl;
    private constructor();
    static initialize(spoServiceUrl?: string): void;
    static getInstance(): SPService;
    getSpoServiceUrl(): string | null;
    serviceStatusCheck(): Promise<boolean>;
    getSPData(body: string, serviceCheck?: boolean): Promise<any | undefined>;
}
