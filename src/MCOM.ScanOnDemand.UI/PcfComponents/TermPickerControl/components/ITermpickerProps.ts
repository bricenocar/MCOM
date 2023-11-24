import { ITermInfo } from "@pnp/sp/taxonomy";
import { SPTaxonomyService } from "../services/SPTaxonomyService";
import { Optional } from "../controls/modernTaxonomyPicker";

export interface ITermpickerProps {
  taxonomyService: SPTaxonomyService;
  termSetId: string;
  label: string;
  panelTitle: string;
  allowMultipleSelections: boolean;
  initialValues: Optional<ITermInfo, "childrenCount" | "createdDateTime" | "lastModifiedDateTime" | "descriptions" | "customSortOrder" | "properties" | "localProperties" | "isDeprecated" | "isAvailableForTagging" | "topicRequested">[];
  placeHolder: string;
  disabled: boolean;
  error: boolean;
  errorColor: string;
  iconColor: string;
  onChange: (terms: ITermInfo[]) => void;
}
