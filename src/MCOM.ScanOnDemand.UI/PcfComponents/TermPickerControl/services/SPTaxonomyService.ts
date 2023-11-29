import { Guid } from '@microsoft/sp-core-library';
import { SPFI, SPCollection, ISPCollection } from '@pnp/sp';
import { JSONParse } from '@pnp/queryable';
import { ITermInfo, ITermSetInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
import { IWebInfo } from '@pnp/sp/webs';
import { getSPData } from '../../services/spServices';

import '@pnp/sp/taxonomy';

export class SPTaxonomyService {

  private sp: SPFI;
  private siteUrl: string;

  constructor(siteUrl: string) { // sp can passes in case it is available and can be used instead of V2
    this.siteUrl = siteUrl;
    // this.sp = sp; in case is needed...
  }

  // Get web info
  public getWebInfo = async () => {
    const termInfo: IWebInfo = await getSPData(`&url=${this.siteUrl}/_api/web`);
    return termInfo;
  }

  public getTerms = async (termSetId: Guid, parentTermId?: Guid, skiptoken?: string, hideDeprecatedTerms?: boolean, pageSize: number = 50): Promise<{ value: ITermInfo[], skiptoken: string }> => {

    // we need to use local sp context to provide JSONParse behavior
    const localSpfi = this.sp.using(JSONParse());
    try {
      let legacyChildrenTerms: ISPCollection;
      if (parentTermId && parentTermId !== Guid.empty) {
        legacyChildrenTerms = SPCollection(localSpfi.termStore.sets.getById(termSetId.toString()).terms.getById(parentTermId.toString()).concat('/getLegacyChildren'));
      }
      else {
        legacyChildrenTerms = SPCollection(localSpfi.termStore.sets.getById(termSetId.toString()).concat('/getLegacyChildren'));
      }
      legacyChildrenTerms = legacyChildrenTerms.top(pageSize);
      if (hideDeprecatedTerms) {
        legacyChildrenTerms = legacyChildrenTerms.filter('isDeprecated eq false');
      }
      if (skiptoken && skiptoken !== '') {
        legacyChildrenTerms.query.set('$skiptoken', skiptoken);
      }

      // type manipulations as we're getting plain JSON here
      const termsJsonResult = await legacyChildrenTerms() as { '@odata.nextLink': string | undefined, value: ITermInfo[] };
      let newSkiptoken = '';
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

  public getTermsV2 = async (termSetId: Guid, parentTermId?: Guid, skiptoken?: string, hideDeprecatedTerms?: boolean, pageSize: number = 50): Promise<{ value: ITermInfo[], skiptoken: string }> => {

    // we need to use local sp context to provide JSONParse behavior    
    try {
      let url = '';
      if (parentTermId && parentTermId !== Guid.empty) {
        url = `&url=${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId.toString()}/terms/${parentTermId.toString()}/getLegacyChildren?`;
      }
      else {
        url = `&url=${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId.toString()}/getLegacyChildren?`;
      }

      // skip = ?%24top=50&%24filter=isDeprecated+eq+false&%24skiptoken=MjAw
      // no-skip = %24filter=isDeprecated+eq+false&%24top=50
      const urlFormat = (skiptoken && skiptoken !== '') ? '{filter}{skiptoken}' : '{filter}{top}';
      const skipTokenParam = (skiptoken && skiptoken !== '') ? `$skiptoken=${skiptoken}` : '';
      const filterParam = '';//`$filter=isDeprecated+eq+false`;
      const topParam = `$top=${pageSize}`;

      // Replace placeholders
      url = `${url}${urlFormat}`
        .replace('{skiptoken}', skipTokenParam)
        .replace('{top}', topParam)
        .replace('{filter}', filterParam);

      // Build json result
      const termsJsonResult = await getSPData(url) as { '@odata.nextLink': string | undefined, value: ITermInfo[] };
      let newSkiptoken = '';

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

  public getTermById = async (termSetId: Guid, termId: Guid): Promise<ITermInfo> => {
    if (termId === Guid.empty) {
      return undefined;
    }
    try {
      const termInfo = await this.sp.termStore.sets.getById(termSetId.toString()).terms.getById(termId.toString()).expand("parent")();
      return termInfo;
    } catch (error) {
      console.error(`SPTaxonomyService.getTermById:`, error);
      return undefined;
    }
  }

  public getTermByIdV2 = async (termSetId: Guid, termId: Guid): Promise<ITermInfo> => {
    if (termId === Guid.empty) {
      return undefined;
    }
    try {
      const termInfo = await getSPData(`&url=${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId}/terms/${termId}?%24expand=parent`);
      return termInfo;
    } catch (error) {
      console.error(`SPTaxonomyService.getTermById:`, error);
      return undefined;
    }
  }

  public searchTerm = async (
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
      let filteredTerms: any = await this.sp.termStore.searchTerm(query);
      if (allowSelectingChildren === false) {
        const hasParentId = parentTermId !== Guid.empty;
        const set = this.sp.termStore.sets.getById(termSetId.toString());
        const collection = hasParentId ? set.terms.getById(parentTermId.toString()).children : set.children;
        const childrenIds = await collection.select("id")().then(children => children.map(c => c.id));

        filteredTerms = filteredTerms.filter(term => childrenIds.includes(term.id));
      }

      return filteredTerms.value ? filteredTerms.value : filteredTerms;
    } catch (error) {
      console.error(`SPTaxonomyService.searchTerm:`, error);
      return [];
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
      let filteredTerms: any = await getSPData(`&url=${this.siteUrl}/_api/v2.1/termstore/searchTerm(label='${label}',setId='${termSetId}',languageTag='${languageTag}',stringMatchOption='${stringMatchOption}')?%24expand=set`);
      if (allowSelectingChildren === false) {
        const hasParentId = parentTermId !== Guid.empty;
        //const set = this.sp.termStore.sets.getById(termSetId.toString());
        const set = this.sp.termStore.sets.getById(termSetId.toString());
        const collection = hasParentId ? set.terms.getById(parentTermId.toString()).children : set.children;
        const childrenIds = await collection.select("id")().then(children => children.map(c => c.id));

        filteredTerms = filteredTerms.filter(term => childrenIds.includes(term.id));
      }

      return filteredTerms.value ? filteredTerms.value : filteredTerms;
    } catch (error) {
      console.error(`SPTaxonomyService.searchTerm:`, error);
      return [];
    }
  }

  public getTermSetInfo = async (termSetId: Guid): Promise<ITermSetInfo | undefined> => {
    const tsInfo = await this.sp.termStore.sets.getById(termSetId.toString())();
    return tsInfo;
  }

  public getTermSetInfoV2 = async (termSetId: Guid): Promise<ITermSetInfo | undefined> => {
    const tsInfo: ITermSetInfo = await getSPData(`&url=${this.siteUrl}/_api/v2.1/termstore/sets/${termSetId}`);
    return tsInfo;
  }

  public getTermStoreInfoV2 = async (): Promise<ITermStoreInfo | undefined> => {
    const termStoreInfo: ITermStoreInfo = await getSPData(`&url=${this.siteUrl}/_api/v2.1/termstore`);
    return termStoreInfo;
  }

  public getTermStoreInfo = async (): Promise<ITermStoreInfo | undefined> => {
    const termStoreInfo = await this.sp.termStore();
    return termStoreInfo;
  }
}