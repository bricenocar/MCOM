// Terms output schema. TODO...
export const termsOutPutSchema  = {
    '$schema': 'http://json-schema.org/draft-04/schema#',
    'type': 'object',
    'properties': {
        'terms': {
            'type': 'array',
            'items': {
                'type': 'object',
                'properties': {
                    'id': {
                        'type': 'string'
                    },
                    'labels': {
                        'type': 'array',
                        'items': {
                            'type': 'object',
                            'properties': {
                                'isDefault': {
                                    'type': 'boolean'
                                },
                                'languageTag': {
                                    'type': 'string'
                                },
                                'name': {
                                    'type': 'string'
                                }
                            }
                        }
                    },
                    'languageTag': {
                        'type': 'string'
                    }
                }
            }
        }
    }
};

// String output schema
export const stringOutPutSchema  = {
    '$schema': 'http://json-schema.org/draft-04/schema#',
    'type': 'string'
};