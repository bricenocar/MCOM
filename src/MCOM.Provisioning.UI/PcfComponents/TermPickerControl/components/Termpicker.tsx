import * as React from 'react';
import type { ITermpickerProps } from './ITermpickerProps';
import { ITermInfo } from '@pnp/sp/taxonomy';
import { ModernTaxonomyPicker } from '../controls/modernTaxonomyPicker';
import { MessageBar, MessageBarType } from '@fluentui/react';

export function Termpicker({ taxonomyService, termSetId, anchorTermId, extraAnchorTermIds, label, panelTitle, onChange, allowMultipleSelections, allowSelectingChildren, initialValues, error, placeHolder, disabled, iconColor, iconSize, errorBorderColor, inputHeight, pageSize, hideDeprecatedTerms, checkService, validApiUrl, validSiteUrl, validAnchorTermId, validTermSetId, validExtraAnchorTermIds }: ITermpickerProps): JSX.Element {

  const onModernTaxonomyPickerChange = (terms: ITermInfo[]): void => {
    onChange(terms);
  }

  const messageComponent = () => {
    let message = '';
    if (!checkService) {
      message = 'Error when trying to reach the SPO service!';
    } else if (!validApiUrl) {
      message = 'Invalid ApiUrl value!';
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
  }

  // Build message component
  const showMessageComponent = messageComponent();

  // Render message component
  if (showMessageComponent) {
    return (showMessageComponent);
  }

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
          allowSelectingChildren={allowSelectingChildren}
        />
      </div>
    );
  }
}
