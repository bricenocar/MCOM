import * as React from 'react';
import type { ITermpickerProps } from './ITermpickerProps';
import { ITermInfo } from '@pnp/sp/taxonomy';
import { ModernTaxonomyPicker } from '../controls/modernTaxonomyPicker';
import { MessageBar, MessageBarType, Shimmer, ShimmerElementType } from '@fluentui/react';

export function Termpicker({ taxonomyService, termSetId, anchorTermId, extraAnchorTermIds, label, panelTitle, onChange, allowMultipleSelections, initialValues, error, placeHolder, disabled, iconColor, iconSize, errorBorderColor, inputHeight, pageSize, hideDeprecatedTerms, checkService, validSiteUrl, validAnchorTermId, validTermSetId, validExtraAnchorTermIds }: ITermpickerProps): JSX.Element {

  const [initialLoadCompleted, setInitialLoadCompleted] = React.useState(false);

  const onLoadCompleted = (initialLoadCompleted: boolean): void => {
    setInitialLoadCompleted(initialLoadCompleted);
  }

  const onModernTaxonomyPickerChange = (terms: ITermInfo[]): void => {
    onChange(terms);
  }

  /*const messageComponent = () => {
    let message = '';
    if (!checkService) {
      message = 'Error when trying to reach the SPO service!';
    } else if (!validSiteUrl) {
      message = 'Invalid SiteUrl value!';
    }
    else if (!validTermSetId) {
      message = 'Invalid TermSetId value!';
    }
    else if (!validAnchorTermId) {
      message = 'Invalid AnchorTermId value!';
    }
    else if (!validExtraAnchorTermIds) {
      message = 'Invalid ExtraAnchorTermIds value!';
    }
    else {
      return null;
    }

    return (
      <MessageBar
        messageBarType={MessageBarType.error}
        isMultiline={false}
        dismissButtonAriaLabel="Close"
      >
        {message}
      </MessageBar>
    );
  }*/

  const shimmerElements = [
    { type: ShimmerElementType.line, width: '96%', height: inputHeight },
    { type: ShimmerElementType.gap, width: '2%' },
    { type: ShimmerElementType.line, width: '2%', height: (inputHeight * 70) / 100 },
  ];

  // const showMessageComponent = messageComponent();

  

  // Render modern taxonomy picker component
  if (checkService && taxonomyService) {
    return (
      <div>
        <ModernTaxonomyPicker
          allowMultipleSelections={allowMultipleSelections}
          termSetId={termSetId}
          anchorTermId={anchorTermId}
          extraAnchorTermIds={extraAnchorTermIds}
          panelTitle={panelTitle ? panelTitle : 'Select Term'}
          label={label}
          onChange={onModernTaxonomyPickerChange}
          onLoadCompleted={onLoadCompleted}
          taxonomyService={taxonomyService}
          initialValues={initialValues}
          placeHolder={placeHolder}
          disabled={disabled}
          inputHeight={inputHeight}
          error={error}
          errorBorderColor={errorBorderColor}
          iconColor={iconColor}
          iconSize={iconSize}
          pageSize={pageSize}
          hideDeprecatedTerms={hideDeprecatedTerms}
        />

        {!initialLoadCompleted && <Shimmer width={'99%'} shimmerElements={shimmerElements} />}
      </div>
    );
  }else{
    return (<div></div>);
  }

  // Render message component
  /*
  if (showMessageComponent) {
    return (showMessageComponent);
  }*/
}
