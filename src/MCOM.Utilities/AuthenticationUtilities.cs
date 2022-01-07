using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using MCOM.Models;

namespace MCOM.Utilities
{
    public class AuthenticationUtilities
    {
        public static async Task<string> GetAuthenticationTokenAsync(dynamic config)
        {            
            var isUsingClientSecret = AppUsesClientSecret(config);
            var authConfig = AuthenticationConfig.BuildAuthenticationConfig(config);

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

            if (isUsingClientSecret)
            {
                app = ConfidentialClientApplicationBuilder.Create(authConfig.ClientId)
                                   .WithClientSecret(authConfig.ClientSecret)
                                   .WithAuthority(new Uri(authConfig.Authority))
                                   .Build();
            }
            else
            {
                var certificate = ReadCertificate(authConfig.CertificateName);
                app = ConfidentialClientApplicationBuilder.Create(authConfig.ClientId)
                    .WithCertificate(certificate)
                    .WithAuthority(new Uri(authConfig.Authority))
                    .Build();
            }

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            var scopes = new string[] { $"{authConfig.ApiUrl}.default" };

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                throw;
            }

            return result.AccessToken;
        }

        /// <summary>
        /// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
        /// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
        /// </summary>
        /// <param name="config">Configuration from appsettings.json</param>
        /// <returns></returns>
        private static bool AppUsesClientSecret(dynamic config)
        {
            if (!string.IsNullOrWhiteSpace(config.ClientSecret.ToString()))
            {
                return true;
            }

            else if (!string.IsNullOrWhiteSpace(config.CertificateName.ToString()))
            {
                return false;
            }

            else
                throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
        }

        private static X509Certificate2 ReadCertificate(string certificateName)
        {
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", "certificateName");
            }
            CertificateDescription certificateDescription = CertificateDescription.FromStoreWithDistinguishedName(certificateName);
            DefaultCertificateLoader defaultCertificateLoader = new DefaultCertificateLoader();
            defaultCertificateLoader.LoadIfNeeded(certificateDescription);
            return certificateDescription.Certificate;
        }
    }
}
