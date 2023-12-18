export declare const termsOutPutSchema: {
    $schema: string;
    type: string;
    properties: {
        terms: {
            type: string;
            items: {
                type: string;
                properties: {
                    id: {
                        type: string;
                    };
                    labels: {
                        type: string;
                        items: {
                            type: string;
                            properties: {
                                isDefault: {
                                    type: string;
                                };
                                languageTag: {
                                    type: string;
                                };
                                name: {
                                    type: string;
                                };
                            };
                        };
                    };
                    languageTag: {
                        type: string;
                    };
                };
            };
        };
    };
};
export declare const stringOutPutSchema: {
    $schema: string;
    type: string;
};
