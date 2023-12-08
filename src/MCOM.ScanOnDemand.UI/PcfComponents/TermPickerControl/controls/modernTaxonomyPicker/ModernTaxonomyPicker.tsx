import { IReadonlyTheme } from '@microsoft/sp-component-base';
import { Guid } from '@microsoft/sp-core-library';

import {
  ITermInfo,
  ITermSetInfo,
  ITermStoreInfo
} from '@pnp/sp/taxonomy';
import { useId } from '@uifabric/react-hooks';
import {
  DefaultButton, IButtonStyles, IconButton, PrimaryButton
} from 'office-ui-fabric-react/lib/Button';
import { IIconProps } from 'office-ui-fabric-react/lib/components/Icon';
import { Label } from 'office-ui-fabric-react/lib/Label';
import {
  Panel,
  PanelType
} from 'office-ui-fabric-react/lib/Panel';
import {
  IBasePickerStyleProps,
  IBasePickerStyles,
  ISuggestionItemProps
} from 'office-ui-fabric-react/lib/Pickers';
import {
  IStackTokens,
  Stack
} from 'office-ui-fabric-react/lib/Stack';
import { ITooltipHostStyles, TooltipHost } from 'office-ui-fabric-react/lib/Tooltip';
import { IStyleFunctionOrObject } from 'office-ui-fabric-react/lib/Utilities';
import * as React from 'react';
import { SPTaxonomyService } from '../../services/SPTaxonomyService';
import styles from './ModernTaxonomyPicker.module.scss';
import { ModernTermPicker } from './modernTermPicker/ModernTermPicker';
import { IModernTermPickerProps, ITermItemProps } from './modernTermPicker/ModernTermPicker.types';
import { TaxonomyPanelContents } from './taxonomyPanelContents';
import { TermItem } from './termItem/TermItem';
import { TermItemSuggestion } from './termItem/TermItemSuggestion';
import { Shimmer, ShimmerElementType } from '@fluentui/react';
import { LanguageService } from '../../../services/languageService';

export type Optional<T, K extends keyof T> = Pick<Partial<T>, K> & Omit<T, K>;

export interface IModernTaxonomyPickerProps {
  allowMultipleSelections?: boolean;
  isPathRendered?: boolean;
  termSetId: string;
  anchorTermId?: string;
  extraAnchorTermIds?: string; // Custom property
  panelTitle: string;
  label: string;
  error: boolean; // Custom property
  errorBorderColor: string; // Custom property
  iconColor: string; // Custom property
  iconSize: number; // Custom property
  inputHeight: number; // Custom property
  pageSize: number; // Custom property
  hideDeprecatedTerms: boolean; // Custom property
  initialValues?: Optional<ITermInfo, "childrenCount" | "createdDateTime" | "lastModifiedDateTime" | "descriptions" | "customSortOrder" | "properties" | "localProperties" | "isDeprecated" | "isAvailableForTagging" | "topicRequested">[]; // Custom property
  disabled?: boolean;
  required?: boolean;
  onChange?: (newValue?: ITermInfo[]) => void;
  onRenderItem?: (itemProps: ITermItemProps) => JSX.Element;
  onRenderSuggestionsItem?: (term: ITermInfo, itemProps: ISuggestionItemProps<ITermInfo>) => JSX.Element;
  placeHolder?: string;
  customPanelWidth?: number;
  themeVariant?: IReadonlyTheme;
  termPickerProps?: Optional<IModernTermPickerProps, 'onResolveSuggestions'>;
  isLightDismiss?: boolean;
  isBlocking?: boolean;
  onRenderActionButton?: (termStoreInfo: ITermStoreInfo, termSetInfo: ITermSetInfo, termInfo?: ITermInfo) => JSX.Element;
  allowSelectingChildren?: boolean;
  taxonomyService: SPTaxonomyService;
}

export function ModernTaxonomyPicker(props: IModernTaxonomyPickerProps): JSX.Element {

  const languageService = LanguageService.getInstance();
  const languageTag = languageService.getLocale();

  const [panelIsOpen, setPanelIsOpen] = React.useState(false);
  const [loadCompleted, setLoadCompleted] = React.useState(false);
  const [selectedOptions, setSelectedOptions] = React.useState<ITermInfo[]>([]);
  const [selectedPanelOptions, setSelectedPanelOptions] = React.useState<ITermInfo[]>([]);
  const [currentTermStoreInfo, setCurrentTermStoreInfo] = React.useState<ITermStoreInfo>(undefined);
  const [currentTermSetInfo, setCurrentTermSetInfo] = React.useState<ITermSetInfo>(undefined);
  const [currentAnchorTermInfo, setCurrentAnchorTermInfo] = React.useState<ITermInfo>(undefined);
  const [currentLanguageTag, setCurrentLanguageTag] = React.useState<string>('');

  // On load event
  React.useEffect(() => {
    props.taxonomyService.getTermStoreInfoV2()
      .then((termStoreInfo) => {
        setCurrentTermStoreInfo(termStoreInfo);
        setCurrentLanguageTag(languageTag!);
        setSelectedOptions(Array.isArray(props.initialValues) ?
          props.initialValues.map(term => { return { ...term, languageTag: languageTag, termStoreInfo: termStoreInfo } as ITermInfo; }) :
          []);
      })
      .catch((e) => {
        console.error('getTermStoreInfo error: ', e);
        // no-op;
      });
    props.taxonomyService.getTermSetInfoV2(Guid.parse(props.termSetId))
      .then((termSetInfo) => {
        setCurrentTermSetInfo(termSetInfo);
        if (!props.anchorTermId) {
          setLoadCompleted(true);
        }
      })
      .catch((e) => {
        // no-op;
        console.error('getTermSetInfo error: ', e);
      });
    if (props.anchorTermId && props.anchorTermId !== Guid.empty.toString()) {
      props.taxonomyService.getTermByIdV2(Guid.parse(props.termSetId), props.anchorTermId ? Guid.parse(props.anchorTermId) : Guid.empty)
        .then((anchorTermInfo) => {
          setCurrentAnchorTermInfo(anchorTermInfo);
          setLoadCompleted(true);
        })
        .catch((e) => {
          console.error('getTermById error: ', e);
          // no-op;
        });
    }
  }, []);

  // Send changed options to the parent
  React.useEffect(() => {
    if (props.onChange && loadCompleted) {
      props.onChange(selectedOptions);
    }
  }, [selectedOptions]);

  // Set initial values to the termpicker component
  React.useEffect(() => {
    setSelectedOptions(Array.isArray(props.initialValues) ?
      props.initialValues.map(term => { return { ...term, languageTag: languageTag } as ITermInfo; }) :
      []);
  }, [props.initialValues]);

  // In case of any of the term properties change so reload the whole component
  React.useEffect(() => {
    const getTermSetInfoV2 = async () => {
      try {
        const termSetInfo = await props.taxonomyService.getTermSetInfoV2(Guid.parse(props.termSetId));
        setCurrentTermSetInfo(termSetInfo);

        if (props.anchorTermId && props.anchorTermId !== Guid.empty.toString()) {
          const anchorTermInfo = await props.taxonomyService.getTermByIdV2(Guid.parse(props.termSetId), props.anchorTermId ? Guid.parse(props.anchorTermId) : Guid.empty);
          setCurrentAnchorTermInfo(anchorTermInfo);
        } else {
          setCurrentAnchorTermInfo(undefined);
        }

        // Move the isMounted check closer to where you set loadCompleted
      } catch (error) {
        console.error('Error fetching term set info:', error);
      }

      // Hide Shimmer
      setLoadCompleted(true);
    };

    // Show shimmer
    setLoadCompleted(false);

    // Call local function
    getTermSetInfoV2();

  }, [props.termSetId, props.anchorTermId, props.extraAnchorTermIds]);

  function onOpenPanel(): void {
    if (props.disabled === true) {
      return;
    }
    setSelectedPanelOptions(selectedOptions);
    setPanelIsOpen(true);
  }

  function onClosePanel(): void {
    setSelectedPanelOptions([]);
    setPanelIsOpen(false);
  }

  function onApply(): void {
    if (props.isPathRendered) {
      addParentInformationToTerms([...selectedPanelOptions])
        .then((selectedTermsWithPath) => {
          setSelectedOptions(selectedTermsWithPath);
        })
        .catch(() => {
          // no-op;
        });
    }
    else {
      setSelectedOptions([...selectedPanelOptions]);
    }
    onClosePanel();
  }

  async function getParentTree(term: ITermInfo): Promise<ITermInfo | undefined> {
    let currentParent = term.parent;
    if (!currentParent) {
      const fullTerm = await props.taxonomyService.getTermByIdV2(Guid.parse(props.termSetId), Guid.parse(term.id));
      currentParent = fullTerm?.parent;
    }
    if (!currentParent) { // Top-level term reached, no parents.
      return undefined;
    } else {
      currentParent.parent = await getParentTree(currentParent);
      return currentParent;
    }
  }

  async function addParentInformationToTerms(terms: ITermInfo[]): Promise<ITermInfo[]> {
    for (const term of terms) {
      const termParent = await getParentTree(term);
      term.parent = termParent;
    }

    return terms;
  }

  async function onResolveSuggestions(filter: string, selectedItems?: ITermInfo[]): Promise<ITermInfo[]> {
    if (filter === '') {
      return [];
    }

    const filteredTerms = await props.taxonomyService.searchTermV2(Guid.parse(props.termSetId), filter, currentLanguageTag, props.anchorTermId ? Guid.parse(props.anchorTermId) : Guid.empty, props.allowSelectingChildren);
    const filteredTermsWithoutSelectedItems = filteredTerms.filter((term) => {
      if (!selectedItems || selectedItems.length === 0) {
        return true;
      }
      return selectedItems.every((item) => item.id !== term.id);
    });

    const filteredTermsAndAvailable = filteredTermsWithoutSelectedItems
      .filter((term) =>
        term.isAvailableForTagging
          .filter((t) => t.setId === props.termSetId)[0].isAvailable);
    return filteredTermsAndAvailable;
  }

  async function onLoadParentLabel(termId: Guid): Promise<string> {
    const termInfo = await props.taxonomyService.getTermByIdV2(Guid.parse(props.termSetId), termId);
    if (termInfo?.parent) {
      let labelsWithMatchingLanguageTag = termInfo.parent.labels.filter((termLabel) => (termLabel.languageTag === currentLanguageTag));
      if (labelsWithMatchingLanguageTag.length === 0) {
        labelsWithMatchingLanguageTag = termInfo.parent.labels.filter((termLabel) => (termLabel.languageTag === currentTermStoreInfo?.defaultLanguageTag));
      }
      return labelsWithMatchingLanguageTag[0]?.name;
    }
    else {
      let termSetNames = currentTermSetInfo?.localizedNames.filter((name) => name.languageTag === currentLanguageTag);
      if (termSetNames?.length === 0) {
        termSetNames = currentTermSetInfo?.localizedNames.filter((name) => name.languageTag === currentTermStoreInfo?.defaultLanguageTag);
      }
      return termSetNames![0].name;
    }
  }

  function onRenderSuggestionsItem(term: ITermInfo, itemProps: ISuggestionItemProps<ITermInfo>): JSX.Element {
    return (
      <TermItemSuggestion
        onLoadParentLabel={onLoadParentLabel}
        term={term}
        termStoreInfo={currentTermStoreInfo}
        languageTag={currentLanguageTag}
        {...itemProps}
      />
    );
  }

  function getLabelsForCurrentLanguage(item: ITermInfo): {
    name: string;
    isDefault: boolean;
    languageTag: string;
  }[] {
    let labels = item.labels.filter((name) => name?.languageTag?.toLowerCase() === currentLanguageTag && name?.isDefault);
    if (labels.length === 0 && currentTermStoreInfo?.defaultLanguageTag) {
      labels = item.labels.filter((name) => name?.languageTag?.toLowerCase() === currentTermStoreInfo.defaultLanguageTag.toLowerCase() && name?.isDefault);
    }

    return (labels && labels.length > 0) ? labels : item.labels;
  }

  function onRenderItem(itemProps: ITermItemProps): JSX.Element | null {
    const labels = getLabelsForCurrentLanguage(itemProps.item);
    let fullParentPrefixes: string[] = [labels[0].name];

    if (props.isPathRendered) {
      let currentTermProps = itemProps.item;
      while (currentTermProps.parent !== undefined) {
        const currentParentLabels = getLabelsForCurrentLanguage(currentTermProps.parent);
        fullParentPrefixes.push(currentParentLabels[0].name);
        currentTermProps = currentTermProps.parent;
      }
      fullParentPrefixes = fullParentPrefixes.reverse();
    }
    return labels.length > 0 ? (
      <TermItem {...itemProps} languageTag={currentLanguageTag} termStoreInfo={currentTermStoreInfo!} name={fullParentPrefixes.join(":")}>{fullParentPrefixes.join(":")}</TermItem>
    ) : null;
  }

  function onTermPickerChange(itms?: ITermInfo[]): void {
    if (itms && props.isPathRendered) {
      addParentInformationToTerms(itms)
        .then((itmsWithPath) => {
          setSelectedOptions(itmsWithPath || []);
          setSelectedPanelOptions(itmsWithPath || []);
        })
        .catch(() => {
          //no-op;
        });
    }
    else {
      setSelectedOptions(itms || []);
      setSelectedPanelOptions(itms || []);
    }
  }

  function getTextFromItem(termInfo: ITermInfo): string {
    let labelsWithMatchingLanguageTag = termInfo.labels.filter((termLabel) => (termLabel.languageTag === currentLanguageTag));
    if (labelsWithMatchingLanguageTag.length === 0) {
      labelsWithMatchingLanguageTag = termInfo.labels.filter((termLabel) => (termLabel.languageTag === currentTermStoreInfo?.defaultLanguageTag));
    }
    return labelsWithMatchingLanguageTag[0]?.name;
  }

  const calloutProps = { gapSpace: 0 };
  const tooltipId = useId('tooltip');
  const hostStyles: Partial<ITooltipHostStyles> = { root: { display: 'inline-block' } };
  const addTermButtonStyles: IButtonStyles = {
    root: {
      selectors: {
        '.ms-Icon': {
          fontSize: props.iconSize ? `${props.iconSize}px` : '16px',
          color: props.iconColor,
        }
      },
    },
    rootHovered: {
      backgroundColor: 'inherit'
    }, rootPressed: {
      backgroundColor: 'inherit'
    }
  }

  let termPickerStyles: IStyleFunctionOrObject<IBasePickerStyleProps, IBasePickerStyles> = { input: { minheight: 34, height: props.inputHeight ? `${props.inputHeight}px` : '30px' }, text: { minheight: 34 } };
  if (props.error) {
    if (props.errorBorderColor) {
      termPickerStyles = {
        root: {
          selectors: {
            '.ms-BasePicker-text': {
              borderColor: props.errorBorderColor,
            },
            '.ms-BasePicker-text:after': {
              borderColor: props.errorBorderColor,
            }
          },
        },
        input: {
          minheight: 34, height: `${props.inputHeight}px`, borderColor: props.errorBorderColor, borderStyle: 'solid'
        }, text: { minheight: 34 }
      };
    }
  }

  // Buils shimmer elements
  const shimmerElements = [
    { type: ShimmerElementType.line, width: '96%', height: props.inputHeight },
    { type: ShimmerElementType.gap, width: '2%' },
    { type: ShimmerElementType.line, width: '2%', height: (props.inputHeight * 70) / 100 },
  ];

  return (

    <div className={styles.modernTaxonomyPicker}>
      {props.label && <Label required={props.required}>{props.label}</Label>}
      {loadCompleted &&
        <div className={styles.termField}>
          <div className={styles.termFieldInput}>
            <ModernTermPicker
              {...props.termPickerProps}
              removeButtonAriaLabel={languageService.getResource('ModernTaxonomyPickerRemoveButtonText')}
              onResolveSuggestions={props.termPickerProps?.onResolveSuggestions ?? onResolveSuggestions}
              itemLimit={props.allowMultipleSelections ? undefined : 1}
              selectedItems={selectedOptions}
              disabled={props.disabled}
              styles={props.termPickerProps?.styles ?? termPickerStyles}
              onChange={onTermPickerChange}
              getTextFromItem={getTextFromItem}
              pickerSuggestionsProps={props.termPickerProps?.pickerSuggestionsProps ?? { noResultsFoundText: languageService.getResource('ModernTaxonomyPickerNoResultsFound') }}
              inputProps={props.termPickerProps?.inputProps ?? {
                'aria-label': props.placeHolder || languageService.getResource('ModernTaxonomyPickerDefaultPlaceHolder'),
                placeholder: props.placeHolder || languageService.getResource('ModernTaxonomyPickerDefaultPlaceHolder')
              }}
              onRenderSuggestionsItem={props.onRenderSuggestionsItem ?? onRenderSuggestionsItem}
              onRenderItem={props.onRenderItem as any ?? onRenderItem} // FIXING LINT ISSUE as any
              themeVariant={props.themeVariant}
            />
          </div>
          <div className={styles.termFieldButton}>
            <TooltipHost
              content={languageService.getResource('ModernTaxonomyPickerAddTagButtonTooltip')}
              id={tooltipId}
              calloutProps={calloutProps}
              styles={hostStyles}
            >
              <IconButton disabled={props.disabled} styles={addTermButtonStyles} iconProps={{ iconName: 'Tag' } as IIconProps} onClick={onOpenPanel} aria-describedby={tooltipId} />
            </TooltipHost>
          </div>
        </div>
      }

      {!loadCompleted && <Shimmer width={'99%'} shimmerElements={shimmerElements} />}

      <Panel
        isOpen={panelIsOpen}
        hasCloseButton={true}
        closeButtonAriaLabel={languageService.getResource('ModernTaxonomyPickerPanelCloseButtonText')}
        onDismiss={onClosePanel}
        isLightDismiss={props.isLightDismiss}
        isBlocking={props.isBlocking}
        type={props.customPanelWidth ? PanelType.custom : PanelType.medium}
        customWidth={props.customPanelWidth ? `${props.customPanelWidth}px` : undefined}
        headerText={props.panelTitle}
        onRenderFooterContent={() => {
          const horizontalGapStackTokens: IStackTokens = {
            childrenGap: 10,
          };
          return (
            <Stack horizontal disableShrink tokens={horizontalGapStackTokens}>
              <PrimaryButton text={languageService.getResource('ModernTaxonomyPickerApplyButtonText')} value='Apply' onClick={onApply} />
              <DefaultButton text={languageService.getResource('ModernTaxonomyPickerCancelButtonText')} value='Cancel' onClick={onClosePanel} />
            </Stack>
          );
        }}>

        {
          props.termSetId && props.taxonomyService && (
            <div key={props.termSetId} >
              <TaxonomyPanelContents
                allowMultipleSelections={props.allowMultipleSelections}
                onResolveSuggestions={props.termPickerProps?.onResolveSuggestions ?? onResolveSuggestions}
                taxonomyService={props.taxonomyService}
                anchorTermInfo={currentAnchorTermInfo!}
                extraAnchorTermIds={props.extraAnchorTermIds}
                termSetInfo={currentTermSetInfo!}
                termStoreInfo={currentTermStoreInfo!}
                pageSize={props.pageSize || 200}
                selectedPanelOptions={selectedPanelOptions}
                setSelectedPanelOptions={setSelectedPanelOptions}
                placeHolder={props.placeHolder || languageService.getResource('ModernTaxonomyPickerDefaultPlaceHolder')}
                onRenderSuggestionsItem={props.onRenderSuggestionsItem ?? onRenderSuggestionsItem}
                onRenderItem={props.onRenderItem as any ?? onRenderItem} // FIXING LINT ISSUE as any
                getTextFromItem={getTextFromItem}
                languageTag={currentLanguageTag}
                themeVariant={props.themeVariant}
                termPickerProps={props.termPickerProps}
                onRenderActionButton={props.onRenderActionButton}
                allowSelectingChildren={props.allowSelectingChildren}
                hideDeprecatedTerms={props.hideDeprecatedTerms}
              />
            </div>
          )
        }
      </Panel>
    </div >
  );
}
