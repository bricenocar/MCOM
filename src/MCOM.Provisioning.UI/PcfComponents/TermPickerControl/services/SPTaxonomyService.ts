import { Guid } from '@microsoft/sp-core-library';
import { ITermInfo, ITermSetInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
import { IWebInfo } from '@pnp/sp/webs';

import '@pnp/sp/taxonomy';
import { SPService } from '../../services/spService';

export class SPTaxonomyService {

  private siteUrl: string;

  constructor(siteUrl: string) { // sp can passes in case it is available and can be used instead of V2
    this.siteUrl = siteUrl;
  }

  // Get web info
  public getWebInfo = async () => {
    const termInfo: IWebInfo = await SPService.getInstance().getSPData(`${this.siteUrl}/_api/web`);
    return termInfo;
  }

  public getTermsV2 = async (termSetId: Guid, parentTermId?: Guid, extraAnchorTermIds?: string, skiptoken?: string, hideDeprecatedTerms?: boolean, pageSize: number = 50): Promise<{ value: ITermInfo[], skiptoken: string }> => {

    // we need to use local sp context to provide JSONParse behavior    
    try {
      let url = '';
      let extraTermsJsonResult = undefined;

      // If parent term id (Anchor term id)
      if (parentTermId && parentTermId !== Guid.empty) {
        // Check in case of extra terms are added to the tree
        if (extraAnchorTermIds && extraAnchorTermIds !== '') {
          // Split all ids into an array
          const extraAnchorTermIdsList = extraAnchorTermIds.trim().split(',');

          // Init filter by passing the parent term id
          let extraFilter = '';

          // Loop and build filters
          extraAnchorTermIdsList.forEach(etId => {
            extraFilter = (!extraFilter) ? `$filter=id eq '${etId}'` : `${extraFilter} or id eq '${etId}'`;
          });

          let extraUrl = `${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId.toString()}/terms?${extraFilter}`;

          extraTermsJsonResult = await SPService.getInstance().getSPData(extraUrl) as { '@odata.nextLink': string | undefined, value: ITermInfo[] };
        }

        url = `${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId.toString()}/terms/${parentTermId.toString()}/getLegacyChildren?`;
      }
      else {
        url = `${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId.toString()}/getLegacyChildren?`;
      }

      // Default url format. The place of each param matters!
      const urlFormat = (skiptoken && skiptoken !== '') ? '{top}&{filter}&{skiptoken}' : '{top}&{filter}';

      // Build params to be added to the url
      const skipTokenParam = `$skiptoken=${skiptoken}`;
      const filterParam = (hideDeprecatedTerms) ? '$filter=isDeprecated+eq+false' : '';
      const topParam = `$top=${pageSize}`;

      // Replace placeholders
      url = `${url}${urlFormat}`
        .replace('{skiptoken}', skipTokenParam)
        .replace('{top}', topParam)
        .replace('{filter}', filterParam);

      // Build json result
      let termsJsonResult = await SPService.getInstance().getSPData(url) as { '@odata.nextLink': string | undefined, value: ITermInfo[] };
      let newSkiptoken = '';

      // Add extra terms in case this is the case
      if (extraTermsJsonResult && extraTermsJsonResult.value.length > 0) {
        termsJsonResult.value = [...extraTermsJsonResult.value, ...termsJsonResult.value];
      }

      // Add skip token to the tree
      if (termsJsonResult['@odata.nextLink']) {
        const urlParams = new URLSearchParams(termsJsonResult['@odata.nextLink'].split('?')[1]);
        if (urlParams.has('$skiptoken')) {
          newSkiptoken = urlParams.get('$skiptoken');
        }
      }

      return { value: termsJsonResult.value as ITermInfo[], skiptoken: newSkiptoken };
    } catch (error) {
      console.error(`SPTaxonomyService.getTerms:`, error);
      return { value: [], skiptoken: '' };
    }
  }

  public getTermByIdV2 = async (termSetId: Guid, termId: Guid): Promise<ITermInfo> => {
    if (termId === Guid.empty) {
      return undefined;
    }
    try {
      const termInfo = await SPService.getInstance().getSPData(`${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId}/terms/${termId}?%24expand=parent`);
      return termInfo;
    } catch (error) {
      console.error(`SPTaxonomyService.getTermById:`, error);
      return undefined;
    }
  }

  public searchTermV2 = async (
    termSetId: Guid,
    label: string,
    languageTag: string,
    parentTermId?: Guid,
    allowSelectingChildren = true,
    stringMatchOption: 'ExactMatch' | 'StartsWith' = 'StartsWith',
  ): Promise<ITermInfo[]> => {
    try {
      const query = {
        label,
        setId: termSetId.toString(),
        languageTag,
        stringMatchOption,
      };

      if (parentTermId !== Guid.empty) {
        query['parentTermId'] = parentTermId.toString();
      }

      // Get array of filtered items     
      let filteredTerms: any = await SPService.getInstance().getSPData(`${this.siteUrl}/_api/v2.1/termstore/searchTerm(label='${label}',setId='${termSetId}',languageTag='${languageTag}',stringMatchOption='${stringMatchOption}')?%24expand=set`);
      if (allowSelectingChildren === false) {
        const hasParentId = parentTermId !== Guid.empty;
        const terms = hasParentId ? await this.getTermsV2(termSetId, parentTermId, '', '', true, 50) : await this.getTermsV2(termSetId, undefined, '', '', true, 50);
        const childrenIds = terms.value.map(c => c.id);

        filteredTerms = filteredTerms.value.filter(term => childrenIds.includes(term.id));
      }

      return filteredTerms.value ? filteredTerms.value : filteredTerms;
    } catch (error) {
      console.error(`SPTaxonomyService.searchTerm:`, error);
      return [];
    }
  }

  public getTermSetInfoV2 = async (termSetId: Guid): Promise<ITermSetInfo | undefined> => {
    const tsInfo: ITermSetInfo = await SPService.getInstance().getSPData(`${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId}`);
    return tsInfo;
  }

  public getTermStoreInfoV2 = async (): Promise<ITermStoreInfo | undefined> => {
    const termStoreInfo: ITermStoreInfo = await SPService.getInstance().getSPData(`${this.siteUrl}/_api/v2.1/termstore`);
    return termStoreInfo;
  }
}