import { SPFI } from '@pnp/sp';
export declare const getSPFI: (siteUrl: string) => Promise<SPFI | undefined>;
export declare const serviceStatusCheck: () => Promise<boolean>;
export declare const getSPData: (body: string, serviceCheck?: boolean) => Promise<any | undefined>;
