Check all 3 backup files under configFiles folder in the root of the project:
-backup_node_modules: Read the comments and make the changes in the js files
-backup_pcf-scripts_webPackConfig: replace the webpack config with the backup file. ¨
The webpack config is in the following path:  \node_modules\pcf-scripts\webpackConfig.js
-backup_tsconfig: Make sure the tsconfig.json and this are the same

Build and Deploy Solution:
-npm run build -- --buildMode production

-Get the publisher properties. The org Url is the environment Url:
Example:
https://{envId}.crm4.dynamics.com/api/data/v9.2/publishers?$select=uniquename,customizationprefix

Result:
{"@odata.etag":"W/\"0000000\"","uniquename":"mcom","customizationprefix":"mcom","publisherid":"4ab5ea78-f958-ed11-9562-002248000000"},

-The following command is to create config files. Run this inside the solutions folder (Within a PCF Component Folder, for ex: TermPickerControl)
pac solution init --publisher-name mcom --publisher-prefix mcom

-The following command must be run from the solutions folder where the .cdsproj file is located.
pac solution add-reference --path ../../

The result will be: Project reference successfully added to Dataverse solution project.

-Rename the generated solutions.cdsproj to the {controlname}.cdsproj
-Go to Solution.xml under the newly created path src/Other and rename from solutions to the controlname:
Example:
<UniqueName>TermPickerControl</UniqueName>
    <LocalizedNames>
        <!-- Localized Solution Name in language code -->
        <LocalizedName description="TermPickerControl" languagecode="1033" />
    </LocalizedNames>

-The following command will build the package in release mode (Managed Solution). For Unmanaged Solution remove '--configuration Release'
dotnet build --configuration Release

-The solution package is created under solutions\bin\release (For Unmanaged solution under solutions\bin\debug)
-Upload the package to PowerApps solutions (Import solution)

Hope you have a nice day!!! Enjoy!!!


Juan Briceno