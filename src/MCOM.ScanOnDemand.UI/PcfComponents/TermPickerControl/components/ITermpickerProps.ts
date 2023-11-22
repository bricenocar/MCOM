import { ITermInfo } from "@pnp/sp/taxonomy";
import { SPTaxonomyService } from "../services/SPTaxonomyService";

export interface ITermpickerProps {
  taxonomyService: SPTaxonomyService;
  termSetId: string;
  label: string;
  panelTitle: string;
  allowMultipleSelections: boolean;
  onChange: (terms: ITermInfo[]) => void;
}
