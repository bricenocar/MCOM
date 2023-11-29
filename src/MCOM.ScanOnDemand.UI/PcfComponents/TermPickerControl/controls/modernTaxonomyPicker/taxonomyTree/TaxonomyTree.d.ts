import * as React from 'react';
import { Selection } from 'office-ui-fabric-react';
import { IReadonlyTheme } from '@microsoft/sp-component-base';
import { ITermInfo, ITermSetInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
import { SPTaxonomyService } from '../../../services/SPTaxonomyService';
export interface ITaxonomyTreeProps {
    allowMultipleSelections?: boolean;
    pageSize: number;
    taxonomyService: SPTaxonomyService;
    anchorTermInfo?: ITermInfo;
    termSetInfo: ITermSetInfo;
    termStoreInfo: ITermStoreInfo;
    languageTag: string;
    themeVariant?: IReadonlyTheme;
    onRenderActionButton?: (termStoreInfo: ITermStoreInfo, termSetInfo: ITermSetInfo, termInfo: ITermInfo, updateTaxonomyTreeViewCallback?: (newTermItems?: ITermInfo[], updatedTermItems?: ITermInfo[], deletedTermItems?: ITermInfo[]) => void) => JSX.Element;
    terms: ITermInfo[];
    setTerms: React.Dispatch<React.SetStateAction<ITermInfo[]>>;
    selection?: Selection<any>;
    hideDeprecatedTerms?: boolean;
    showIcons?: boolean;
    allowSelectingChildren?: boolean;
}
export declare function TaxonomyTree(props: ITaxonomyTreeProps): React.ReactElement<ITaxonomyTreeProps>;
