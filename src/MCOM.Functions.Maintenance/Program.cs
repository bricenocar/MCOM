using MCOM.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MCOM.ScanOnDemand.Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, s) =>
                {
                    // Adding services to DI               
                    s.AddScoped<IBlobService, BlobService>();
                    s.AddScoped<IGraphService, GraphService>();
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