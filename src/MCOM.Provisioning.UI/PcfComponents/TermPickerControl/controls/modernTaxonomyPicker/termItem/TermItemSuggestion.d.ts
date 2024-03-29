/// <reference types="react" />
import { ISuggestionItemProps } from 'office-ui-fabric-react/lib/Pickers';
import { Guid } from '@microsoft/sp-core-library';
import { ITermInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
export interface ITermItemSuggestionProps<T> extends ISuggestionItemProps<T> {
    term: ITermInfo;
    languageTag?: string;
    termStoreInfo?: ITermStoreInfo;
    onLoadParentLabel?: (termId: Guid) => Promise<string>;
}
export declare function TermItemSuggestion(props: ITermItemSuggestionProps<ITermInfo>): JSX.Element;
