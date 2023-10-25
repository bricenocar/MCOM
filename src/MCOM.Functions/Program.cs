using MCOM.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PnP.Core.Auth;
using PnP.Core.Services.Builder.Configuration;
using System;
using System.Security.Cryptography.X509Certificates;

namespace MCOM.Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((hostingContext, services) =>
                {
                    // Adding services to DI
                    services.AddScoped<IQueueService, QueueService>();
                    services.AddScoped<IGraphService, GraphService>();
                    services.AddScoped<IBlobService, BlobService>();
                    services.AddScoped<IAppInsightsService, AppInsightsService>();
                    services.AddScoped<ISharePointService, SharePointService>();
                    services.AddScoped<IMicrosoft365Service, Microsoft365Service>();
                    services.AddScoped<IAzureService, AzureService>();
                    services.AddScoped<IDataBaseService, DataBaseService>();

                    // Add pnp core sdk services config
                    services.AddPnPCore(options =>
                    {
                        var siteUrl = Environment.GetEnvironmentVariable("SharePointUrl");
                        bool isMSI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSI_SECRET"));
                        if (isMSI)
                        {
                            var authProvider = new ManagedIdentityTokenProvider();
                            options.DefaultAuthenticationProvider = authProvider;
                            options.Sites.Add("Default",
                                new PnPCoreSiteOptions
                                {
                                    SiteUrl = siteUrl,
                                    AuthenticationProvider = authProvider
                                }
                            );
                        }
                        else
                        {
                            // local dev
                            string ClientId = Environment.GetEnvironmentVariable("ClientId");
                            string TenantId = Environment.GetEnvironmentVariable("TenantId");
                            string CertificateThumbprint = Environment.GetEnvironmentVariable("CertificateThumbprint");
                            // Configure an authentication provider with certificate (Required for app only)
                            // App-only authentication against SharePoint Online requires certificate based authentication for calling the "classic" SharePoint REST/CSOM APIs. The SharePoint Graph calls can work with clientid+secret, but since PnP Core SDK requires both type of APIs (as not all features are exposed via the Graph APIs) you need to use certificate based auth.
                            var authProvider = new X509CertificateAuthenticationProvider(ClientId,
                                TenantId,
                                StoreName.My,
                                StoreLocation.CurrentUser,
                                CertificateThumbprint);
                            // And set it as default
                            options.DefaultAuthenticationProvider = authProvider;

                            // Add a default configuration with the site configured in app settings
                            options.Sites.Add("Default",
                                new PnPCoreSiteOptions
                                {
                                    SiteUrl = siteUrl,
                                    AuthenticationProvider = authProvider
                                }
                            );
                        }
                    });
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddApplicationInsights(context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
                }).Build();

            host.Run();
        }
    }
}