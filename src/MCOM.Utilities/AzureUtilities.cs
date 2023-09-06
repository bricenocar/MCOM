using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace MCOM.Utilities
{
    public class AzureUtilities
    {
        public static async Task<AccessToken> GetAzureServiceTokenAsync(string resourceId)
        {
            var cred = new DefaultAzureCredential(); //GetDefaultCredential();
            return await cred.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { resourceId + "/.default" }) { }
            );
        }

        public static DefaultAzureCredential GetDefaultCredential()
        {
            // For using Managed Identity when deployed in Azure and VS for testing purposes
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions 
            { 
                ExcludeAzurePowerShellCredential = true, 
                ExcludeEnvironmentCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeAzureCliCredential = true,

                ExcludeManagedIdentityCredential = false,

#if DEBUG
                ExcludeVisualStudioCodeCredential = false, // Test purposes
                ExcludeVisualStudioCredential = false, // Test purposes
#endif          
            });
        }
    }
}
