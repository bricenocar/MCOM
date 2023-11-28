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
    inputHeight: number;
    disabled: boolean;
    error: boolean;
    errorBorderColor: string;
    iconColor: string;
    iconSize: number;
    pageSize: number;
    onChange: (terms: ITermInfo[]) => void;
}
