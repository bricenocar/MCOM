using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCOM.Services;

namespace MCOM.Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                    // Adding services to DI
                    s.AddScoped<IQueueService, QueueService>();
                    s.AddScoped<IGraphService, GraphService>();
                    s.AddScoped<IBlobService, BlobService>();
                    s.AddScoped<IAppInsightsService, AppInsightsService>();
                    s.AddScoped<ISharePointService, SharePointService>();
                    s.AddScoped<IAzureService, AzureService>();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddApplicationInsights(context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
                }).Build();

            host.Run();
        }
    }
}