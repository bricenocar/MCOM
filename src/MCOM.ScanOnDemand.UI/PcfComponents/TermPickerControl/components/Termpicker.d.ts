/// <reference types="react" />
import type { ITermpickerProps } from './ITermpickerProps';
import "@pnp/sp/webs";
export declare function Termpicker({ taxonomyService, termSetId, label, panelTitle, onChange, allowMultipleSelections, initialValues, error, placeHolder, disabled, iconColor, iconSize, errorBorderColor, inputHeight, pageSize, hideDeprecatedTerms, checkService }: ITermpickerProps): JSX.Element;
