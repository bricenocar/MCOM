import { Guid } from '@microsoft/sp-core-library';
import { ITermInfo, ITermSetInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
import '@pnp/sp/taxonomy';
import { IWebInfo } from '@pnp/sp/webs';
export declare class SPTaxonomyService {
    private sp;
    private siteUrl;
    constructor(siteUrl: string);
    getWebInfo: () => Promise<IWebInfo>;
    getTerms: (termSetId: Guid, parentTermId?: Guid, skiptoken?: string, hideDeprecatedTerms?: boolean, pageSize?: number) => Promise<{
        value: ITermInfo[];
        skiptoken: string;
    }>;
    getTermsV2: (termSetId: Guid, parentTermId?: Guid, skiptoken?: string, hideDeprecatedTerms?: boolean, pageSize?: number) => Promise<{
        value: ITermInfo[];
        skiptoken: string;
    }>;
    getTermById: (termSetId: Guid, termId: Guid) => Promise<ITermInfo>;
    getTermByIdV2: (termSetId: Guid, termId: Guid) => Promise<ITermInfo>;
    searchTerm: (termSetId: Guid, label: string, languageTag: string, parentTermId?: Guid, allowSelectingChildren?: boolean, stringMatchOption?: 'ExactMatch' | 'StartsWith') => Promise<ITermInfo[]>;
    searchTermV2: (termSetId: Guid, label: string, languageTag: string, parentTermId?: Guid, allowSelectingChildren?: boolean, stringMatchOption?: 'ExactMatch' | 'StartsWith') => Promise<ITermInfo[]>;
    getTermSetInfo: (termSetId: Guid) => Promise<ITermSetInfo | undefined>;
    getTermSetInfoV2: (termSetId: Guid) => Promise<ITermSetInfo | undefined>;
    getTermStoreInfoV2: () => Promise<ITermStoreInfo | undefined>;
    getTermStoreInfo: () => Promise<ITermStoreInfo | undefined>;
}
