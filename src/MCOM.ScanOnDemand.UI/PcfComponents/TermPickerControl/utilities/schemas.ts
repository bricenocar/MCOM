// Example in case we need a more complex object
export const outPutSchema2 = {
    "$schema": "http://json-schema.org/draft-04/schema#",
    "type": "array",
    "items":
    {
        "type": "object",
        "properties": {
            "id": {
                "type": "integer"
            },
            "childrenCount": {
                "type": "number"
            },
            "createdDateTime": {
                "type": "string"
            },
            "lastModifiedDateTime": {
                "type": "string"
            },
            "set": {
                "type": "object",
                "properties": {
                    "childrenCount": {
                        "type": "number"
                    },
                    "createdDateTime": {
                        "type": "string"
                    },
                    "description": {
                        "type": "string"
                    },
                    "groupId": {
                        "type": "string"
                    },
                    "id": {
                        "type": "string"
                    }
                }
            }
        }
    }
};

// Example in case we need a more complex object
export const outPutSchema1 = {
    "$schema": "http://json-schema.org/draft-04/schema#",
    "type": "array",
    "items":
    {
        "type": "string",
    }
};

export const stringOutPutSchema = {
    "$schema": "http://json-schema.org/draft-04/schema#",
    "type": "string"
};