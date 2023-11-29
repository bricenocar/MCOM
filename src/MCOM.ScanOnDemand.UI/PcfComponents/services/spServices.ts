import { SPFI, spfi, DefaultHeaders, DefaultInit } from '@pnp/sp';
import { BearerToken, DefaultParse, BrowserFetch } from '@pnp/queryable';

const spoServiceUrl = 'https://function-mcom-provisioning-inttest.azurewebsites.net/api/GetSPOData?code=nftcNjFYXkjVaJzkJWo4o1uQD9-NCM8hU_cCubUsOkk4AzFu_rQ4fQ==';

// Get the spfi object in case working with jwt and spfi
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

export const serviceStatusCheck = async (): Promise<boolean> => {
    return await getSPData('', true);
}

// Get the spfi object
export const getSPData = async (body: string, serviceCheck = false): Promise<any | undefined> => {
    try {
        // Check if service check
        const serviceCheckParam = serviceCheck ? '&statusCheck=true' : '';

        // Build headers
        const headers = new Headers();
        headers.append("Accept", "text/html");
        headers.append("Content-Type", "text/plain");

        // Get response and json object
        const response = await fetch(`${spoServiceUrl}${serviceCheckParam}`, {
            method: 'POST',
            headers,
            body,
            redirect: 'follow'
        });

        const data = await response.json();
        if (data && response.ok === true) {
            return data;
        } else {
            if (console) {
                console.log('Error getting data from the service. Returning null.');
            }
        }
    } catch (ex) {
        if (console) {
            console.log(ex);
            console.log('Error trying to reach the getSPO service.');
        }
    }

    return null;
}