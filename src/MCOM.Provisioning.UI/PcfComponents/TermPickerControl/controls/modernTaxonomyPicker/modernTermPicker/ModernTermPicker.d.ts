import React from 'react';
import { BasePicker } from 'office-ui-fabric-react/lib/components/pickers/BasePicker';
import { IModernTermPickerProps } from './ModernTermPicker.types';
import { ISuggestionItemProps } from 'office-ui-fabric-react/lib/components/pickers/Suggestions/SuggestionsItem.types';
import { ITermInfo } from '@pnp/sp/taxonomy';
export declare class ModernTermPickerBase extends BasePicker<ITermInfo, IModernTermPickerProps> {
    static defaultProps: {
        onRenderItem: (props: any) => JSX.Element;
        onRenderSuggestionsItem: (props: ITermInfo, itemProps: ISuggestionItemProps<ITermInfo>) => JSX.Element;
    };
    constructor(props: IModernTermPickerProps);
}
export declare const ModernTermPicker: React.FunctionComponent<IModernTermPickerProps>;
