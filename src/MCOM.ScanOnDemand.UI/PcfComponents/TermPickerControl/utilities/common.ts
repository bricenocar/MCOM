import { ITermInfo } from "@pnp/sp/taxonomy";
import { Optional } from "../controls/modernTaxonomyPicker";

// Get term values string and convert to a TermInfo array object
export const getTermValuesArray = (input: string) => {
    const resultArray: Optional<ITermInfo, "childrenCount" | "createdDateTime" | "lastModifiedDateTime" | "descriptions" | "customSortOrder" | "properties" | "localProperties" | "isDeprecated" | "isAvailableForTagging" | "topicRequested">[] = [];
    const segments = input.split(';#');

    for (let i = 0; i < segments.length; i++) {
        const currentSegment = segments[i];
        const nextSegment = segments[i + 1];

        if (currentSegment.startsWith('-1') && nextSegment) {
            let [name, id] = nextSegment.split('|');
            name = name.replace('-1;#', '');
            resultArray.push({labels: [{name, isDefault: true, languageTag: "en-US"}], id});
            i++;
        }
    }

    return resultArray;
}

export const validTermValues = (inputString): boolean => {
    const regexPattern = /-1;#([^;|]+)\|([^;#]+)(?=;#|$)/g;
    return regexPattern.test(inputString);
}