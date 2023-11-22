import { SPFI, spfi, DefaultHeaders, DefaultInit } from '@pnp/sp';
import { BearerToken, DefaultParse, BrowserFetch } from '@pnp/queryable';

// Get the spfi object
export const getSPFI = async (siteUrl: string): Promise<SPFI | undefined> => {

    try {
        // Get response and json object
        const response = await fetch('Url to get the token');
        const json = await response.json();

        if (json && json.Token) {
            // Pass Bearer token to fetch
            return spfi(siteUrl).using(
                BearerToken(json.Token),
                DefaultHeaders(),
                DefaultInit(),
                BrowserFetch(),
                DefaultParse()
            );
        } else {
            if (console) {
                console.log('Error generating the JWT when calling the function.');
            }
        }
    } catch {
        return undefined;
    }
}

// Get the spfi object
export const getSPData = async (url: string): Promise<any | undefined> => {
    try {
        // Get response and json object
        const response = await fetch(url);
        const json = await response.json();

        if (json) {
            // Pass Bearer token to fetch
            return json;
        } else {
            if (console) {
                console.log('Error generating the JWT when calling the function.');
            }
        }
    } catch {
        return undefined;
    }
}