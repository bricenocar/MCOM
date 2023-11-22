import * as React from 'react';
import { IPickerItemProps, ISuggestionItemProps } from 'office-ui-fabric-react';
import { ITermInfo, ITermSetInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
import { IReadonlyTheme } from "@microsoft/sp-component-base";
import { IModernTermPickerProps } from '../modernTermPicker/ModernTermPicker.types';
import { Optional } from '../ModernTaxonomyPicker';
import { SPTaxonomyService } from '../../../services/SPTaxonomyService';
export interface ITaxonomyPanelContentsProps {
    allowMultipleSelections?: boolean;
    pageSize: number;
    selectedPanelOptions: ITermInfo[];
    setSelectedPanelOptions: React.Dispatch<React.SetStateAction<ITermInfo[]>>;
    onResolveSuggestions: (filter: string, selectedItems?: ITermInfo[]) => ITermInfo[] | PromiseLike<ITermInfo[]>;
    taxonomyService: SPTaxonomyService;
    anchorTermInfo: ITermInfo;
    termSetInfo: ITermSetInfo;
    termStoreInfo: ITermStoreInfo;
    placeHolder: string;
    onRenderSuggestionsItem?: (props: ITermInfo, itemProps: ISuggestionItemProps<ITermInfo>) => JSX.Element;
    onRenderItem?: (props: IPickerItemProps<ITermInfo>) => JSX.Element;
    getTextFromItem: (item: ITermInfo, currentValue?: string) => string;
    languageTag: string;
    themeVariant?: IReadonlyTheme;
    termPickerProps?: Optional<IModernTermPickerProps, 'onResolveSuggestions'>;
    onRenderActionButton?: (termStoreInfo: ITermStoreInfo, termSetInfo: ITermSetInfo, termInfo: ITermInfo, updateTaxonomyTreeViewCallback?: (newTermItems?: ITermInfo[], updatedTermItems?: ITermInfo[], deletedTermItems?: ITermInfo[]) => void) => JSX.Element;
    allowSelectingChildren?: boolean;
}
export declare function TaxonomyPanelContents(props: ITaxonomyPanelContentsProps): React.ReactElement<ITaxonomyPanelContentsProps>;
