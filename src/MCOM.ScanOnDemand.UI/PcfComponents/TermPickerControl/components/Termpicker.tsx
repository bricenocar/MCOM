import * as React from 'react';
import type { ITermpickerProps } from './ITermpickerProps';
import { ITermInfo } from '@pnp/sp/taxonomy';
import { ModernTaxonomyPicker } from '../controls/modernTaxonomyPicker';

import "@pnp/sp/webs";

// export default class Termpicker extends React.Component<ITermpickerProps, ITermpickerState> {
export function Termpicker({ taxonomyService, termSetId, label, panelTitle, onChange, allowMultipleSelections }: ITermpickerProps): JSX.Element {

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
        taxonomyService={taxonomyService}
      />
    );
  } else {
    return (
      <div>{'Loading control...'}</div>
    );
  }
}
