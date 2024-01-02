import { Guid } from '@microsoft/sp-core-library';
import { ITermInfo, ITermSetInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
import { IWebInfo } from '@pnp/sp/webs';
import '@pnp/sp/taxonomy';
export declare class SPTaxonomyService {
    private siteUrl;
    constructor(siteUrl: string);
    getWebInfo: () => Promise<IWebInfo>;
    getTermsV2: (termSetId: Guid, parentTermId?: Guid, extraAnchorTermIds?: string, skiptoken?: string, hideDeprecatedTerms?: boolean, pageSize?: number) => Promise<{
        value: ITermInfo[];
        skiptoken: string;
    }>;
    getTermByIdV2: (termSetId: Guid, termId: Guid) => Promise<ITermInfo>;
    searchTermV2: (termSetId: Guid, label: string, languageTag: string, parentTermId?: Guid, allowSelectingChildren?: boolean, stringMatchOption?: 'ExactMatch' | 'StartsWith') => Promise<ITermInfo[]>;
    getTermSetInfoV2: (termSetId: Guid) => Promise<ITermSetInfo | undefined>;
    getTermStoreInfoV2: () => Promise<ITermStoreInfo | undefined>;
}
