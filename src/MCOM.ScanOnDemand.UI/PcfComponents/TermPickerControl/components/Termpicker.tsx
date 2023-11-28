import * as React from 'react';
import type { ITermpickerProps } from './ITermpickerProps';
import { ITermInfo } from '@pnp/sp/taxonomy';
import { ModernTaxonomyPicker } from '../controls/modernTaxonomyPicker';

import "@pnp/sp/webs";

// export default class Termpicker extends React.Component<ITermpickerProps, ITermpickerState> {
export function Termpicker({ taxonomyService, termSetId, label, panelTitle, onChange, allowMultipleSelections, initialValues, error, placeHolder, disabled, iconColor, iconSize, errorBorderColor, inputHeight, pageSize }: ITermpickerProps): JSX.Element {

  const [initialLoadCompleted, setInitialLoadCompleted] = React.useState(false);

  const onLoadCompleted = (initialLoadCompleted: boolean): void => {
    setInitialLoadCompleted(initialLoadCompleted);
  }

  const onModernTaxonomyPickerChange = (terms: ITermInfo[]): void => {
    onChange(terms);
  }

  // Render
  if (taxonomyService) {
    return (
      <ModernTaxonomyPicker
        allowMultipleSelections={allowMultipleSelections}
        termSetId={termSetId}
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
      />
    );
  }
  if (!initialLoadCompleted) {
    return (
      <div>{'Loading control...'}</div>
    );
  }
}
