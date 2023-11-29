import { ITermInfo } from "@pnp/sp/taxonomy";
import { Optional } from "../controls/modernTaxonomyPicker";
export declare const getTermValuesArray: (input: string) => Optional<ITermInfo, "childrenCount" | "createdDateTime" | "lastModifiedDateTime" | "descriptions" | "customSortOrder" | "properties" | "localProperties" | "isDeprecated" | "isAvailableForTagging" | "topicRequested">[];
export declare const validTermValues: (inputString: any) => boolean;
