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

        public static ChainedTokenCredential GetDefaultCredential()
        {
            // For using Managed Identity when deployed in Azure and Azure CLI for local development purposes
            return new ChainedTokenCredential(                    
                    new ManagedIdentityCredential(),
                    new DefaultAzureCredential(),
                    new AzureCliCredential()                    
                );
        }
    }
}
