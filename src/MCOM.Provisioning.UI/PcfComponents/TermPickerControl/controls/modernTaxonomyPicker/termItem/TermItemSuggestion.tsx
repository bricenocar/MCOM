import React, { useEffect } from 'react';
import { ISuggestionItemProps } from 'office-ui-fabric-react/lib/Pickers';
import styles from './TermItemSuggestions.module.scss';
import { Guid } from '@microsoft/sp-core-library';
import { ITermInfo, ITermStoreInfo } from '@pnp/sp/taxonomy';
import { LanguageService } from '../../../../services/languageService';

export interface ITermItemSuggestionProps<T> extends ISuggestionItemProps<T> {
  term: ITermInfo;
  languageTag?: string;
  termStoreInfo?: ITermStoreInfo;
  onLoadParentLabel?: (termId: Guid) => Promise<string>;
}

export function TermItemSuggestion(props: ITermItemSuggestionProps<ITermInfo>): JSX.Element {
  const [parentLabel, setParentLabel] = React.useState<string>("");

  const languageService = LanguageService.getInstance();

  useEffect(() => {
    if (props.onLoadParentLabel) {
      props.onLoadParentLabel(Guid.parse(props.term.id.toString()))
        .then((localParentInfo) => {
          setParentLabel(localParentInfo);
        })
        .catch(() => {
          // no-op;
        });
    }
  }, []);

  let labels: {
    name: string;
    isDefault: boolean;
    languageTag: string;
  }[];

  if (props.languageTag && props.termStoreInfo) {
    labels = props.term.labels.filter((name) => name.languageTag === props.languageTag && name.isDefault);
    if (labels.length === 0) {
      labels = props.term.labels.filter((name) => name.languageTag === props.termStoreInfo.defaultLanguageTag && name.isDefault);
    }
  }
  else {
    labels = props.term.labels.filter((name) => name.isDefault);
  }

  return (
    <div className={styles.termSuggestionContainer} title={labels[0]?.name}>
      {labels[0]?.name}
      {parentLabel !== "" && <div>
        <span className={styles.termSuggestionPath}>{`${languageService.getResource('ModernTaxonomyPickerSuggestionInLabel')} ${parentLabel}`}</span>
      </div>}
    </div>
  );
}
