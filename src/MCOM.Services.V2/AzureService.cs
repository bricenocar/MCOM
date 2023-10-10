using System;
using System.Threading.Tasks;
using Azure.Core;
using MCOM.Utilities;


namespace MCOM.Services
{
    public interface IAzureService
    {
        Task<AccessToken> GetAzureServiceTokenAsync(Uri uri);
    }

    public class AzureService : IAzureService
    {
        public virtual async Task<AccessToken> GetAzureServiceTokenAsync(Uri uri)
        {
            return await AzureUtilities.GetAzureServiceTokenAsync(uri.Scheme + Uri.SchemeDelimiter + uri.Host);
        }
    }
}
