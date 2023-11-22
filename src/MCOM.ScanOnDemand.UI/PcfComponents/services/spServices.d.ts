import { SPFI } from '@pnp/sp';
export declare const getSPFI: (siteUrl: string) => Promise<SPFI | undefined>;
export declare const getSPData: (url: string) => Promise<any | undefined>;
