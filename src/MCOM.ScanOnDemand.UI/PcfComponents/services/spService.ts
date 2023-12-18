export class SPService {
    private static instance: SPService | null = null;
    private spoServiceUrl: string | null = null;

    private constructor() {
        // Private constructor to enforce singleton pattern
    }

    public static initialize(spoServiceUrl?: string): void {
        if (!SPService.instance) {
            SPService.instance = new SPService();
        }

        // Use the provided spoServiceUrl if available
        if (spoServiceUrl) {
            SPService.instance.spoServiceUrl = spoServiceUrl;
        }
    }

    public static getInstance(): SPService {
        if (!SPService.instance || !SPService.instance.spoServiceUrl) {
            throw new Error("SPService is not initialized. Call SPService.initialize() first.");
        }
        return SPService.instance;
    }

    public getSpoServiceUrl(): string | null {
        return this.spoServiceUrl;
    }

    public async serviceStatusCheck(): Promise<boolean> {
        return await this.getSPData('', true);
    }

    public async getSPData(body: string, serviceCheck = false): Promise<any | undefined> {
        try {
            // Check if service check
            const serviceCheckParam = serviceCheck ? '&statusCheck=true' : '';

            // Build headers
            const headers = new Headers();
            headers.append("Accept", "text/html");
            headers.append("Content-Type", "text/plain");

            // Get response and json object
            const response = await fetch(`${this.spoServiceUrl}${serviceCheckParam}`, {
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
                    console.error('Error getting data from the service. Returning null.');
                }
            }
        } catch (ex) {
            if (console) {
                console.error(ex);
                console.error('Error trying to reach the getSPO service.');
            }
        }

        return null;
    }
}
