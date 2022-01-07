# Azure API Management developer portal

This is a copy of the Microsoft Azure API Management developer portal project in GitHub, we took only the scriptsV2 folder to migrate the developer portal from one instance to the other. Follow these steps to migrate:

- Run `npm install` in the root of the project
```
npm install
```
- Run migrate.js with a valid combination of arguments. For example:
```
node ./migrate ^
--sourceEndpoint "<name.management.azure-api.net>" ^
--sourceId "sourceId" ^
--sourceKey "sourceKey" ^
--destEndpoint "<name.management.azure-api.net>" ^
--destinationId "destinationId" ^
--destinationKey "destinationKey" ^
--publishEndpoint "<name.developer.azure-api.net>"
```


