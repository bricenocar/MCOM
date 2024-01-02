import * as React from 'react';
import styles from './TaxonomyPanelContents.module.scss';
import {
  IBasePickerStyleProps,
  IBasePickerStyles,
  IPickerItemProps,
  IStyleFunctionOrObject,
  ISuggestionItemProps,
  Label,
  Selection,
} from 'office-ui-fabric-react';
import {
  ITermInfo,
  ITermSetInfo,
  ITermStoreInfo
} from '@pnp/sp/taxonomy';
import { useForceUpdate } from '@uifabric/react-hooks';
import { ModernTermPicker } from '../modernTermPicker/ModernTermPicker';
import { IReadonlyTheme } from "@microsoft/sp-component-base";
import { IModernTermPickerProps } from '../modernTermPicker/ModernTermPicker.types';
import { Optional } from '../ModernTaxonomyPicker';
import { TaxonomyTree } from '../taxonomyTree/TaxonomyTree';
import { SPTaxonomyService } from '../../../services/SPTaxonomyService';
import { LanguageService } from '../../../../services/languageService';

export interface ITaxonomyPanelContentsProps {
  allowMultipleSelections?: boolean;
  pageSize: number;
  selectedPanelOptions: ITermInfo[];
  setSelectedPanelOptions: React.Dispatch<React.SetStateAction<ITermInfo[]>>;
  onResolveSuggestions: (filter: string, selectedItems?: ITermInfo[]) => ITermInfo[] | PromiseLike<ITermInfo[]>;
  taxonomyService: SPTaxonomyService;
  anchorTermInfo: ITermInfo;
  termSetInfo: ITermSetInfo;
  extraAnchorTermIds: string; // Custom property
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
  hideDeprecatedTerms: boolean; // Custom property
}

export function TaxonomyPanelContents(props: ITaxonomyPanelContentsProps): React.ReactElement<ITaxonomyPanelContentsProps> {
  const [terms, setTerms] = React.useState<ITermInfo[]>(props.selectedPanelOptions?.length > 0 ? [...props.selectedPanelOptions] : []);

  const languageService = LanguageService.getInstance();
  const forceUpdate = useForceUpdate();

  const selection = React.useMemo(() => {
    const s = new Selection({
      onSelectionChanged: () => {
        props.setSelectedPanelOptions((prevOptions) => [...selection.getSelection()]);
        forceUpdate();
      }, getKey: (term: any) => term.id // eslint-disable-line @typescript-eslint/no-explicit-any
    });
    s.setItems(terms);
    for (const selectedOption of props.selectedPanelOptions) {
      if (s.canSelectItem) {
        s.setKeySelected(selectedOption.id.toString(), true, true);
      }
    }
    return s;
  }, [terms]);

  const onPickerChange = (items?: ITermInfo[]): void => {
    const itemsToAdd = items.filter((item) => terms.every((term) => term.id !== item.id));
    setTerms((prevTerms) => [...prevTerms, ...itemsToAdd]);
    selection.setItems([...selection.getItems(), ...itemsToAdd], true);
    for (const item of items) {
      if (selection.canSelectItem(item)) {
        selection.setKeySelected(item.id.toString(), true, false);
      }
    }
  };

  const termPickerStyles: IStyleFunctionOrObject<IBasePickerStyleProps, IBasePickerStyles> = { root: { paddingTop: 4, paddingBottom: 4, paddingRight: 4, minheight: 34 }, input: { minheight: 34 }, text: { minheight: 34, borderStyle: 'none', borderWidth: '0px' } };

  return (
    <div className={styles.taxonomyPanelContents}>
      <div className={styles.taxonomyTreeSelector}>
        <div>
          <ModernTermPicker
            {...props.termPickerProps}
            removeButtonAriaLabel={languageService.getResource('ModernTaxonomyPickerRemoveButtonText')}
            onResolveSuggestions={props.termPickerProps?.onResolveSuggestions ?? props.onResolveSuggestions}
            itemLimit={props.allowMultipleSelections ? undefined : 1}
            selectedItems={props.selectedPanelOptions}
            styles={props.termPickerProps?.styles ?? termPickerStyles}
            onChange={onPickerChange}
            getTextFromItem={props.getTextFromItem}
            pickerSuggestionsProps={props.termPickerProps?.pickerSuggestionsProps ?? { noResultsFoundText: languageService.getResource('ModernTaxonomyPickerNoResultsFound') }}
            inputProps={props.termPickerProps?.inputProps ?? {
              'aria-label': props.placeHolder || languageService.getResource('ModernTaxonomyPickerDefaultPlaceHolder'),
              placeholder: props.placeHolder || languageService.getResource('ModernTaxonomyPickerDefaultPlaceHolder')
            }}
            onRenderSuggestionsItem={props.termPickerProps?.onRenderSuggestionsItem ?? props.onRenderSuggestionsItem}
            onRenderItem={props.onRenderItem}
            themeVariant={props.themeVariant}
          />
        </div>
      </div>
      <Label className={styles.taxonomyTreeLabel}>{props.allowMultipleSelections ? languageService.getResource('ModernTaxonomyPickerTreeTitleMulti') : languageService.getResource('ModernTaxonomyPickerTreeTitleSingle')}</Label>
      <TaxonomyTree
        anchorTermInfo={props.anchorTermInfo}
        languageTag={props.languageTag}
        taxonomyService={props.taxonomyService}
        pageSize={props.pageSize}
        selection={selection}
        setTerms={setTerms}
        termSetInfo={props.termSetInfo}
        extraAnchorTermIds={props.extraAnchorTermIds}
        termStoreInfo={props.termStoreInfo}
        terms={terms}
        allowMultipleSelections={props.allowMultipleSelections}
        onRenderActionButton={props.onRenderActionButton}
        hideDeprecatedTerms={props.hideDeprecatedTerms}
        showIcons={false}
        allowSelectingChildren={props.allowSelectingChildren}
      />
    </div>
  );
}

