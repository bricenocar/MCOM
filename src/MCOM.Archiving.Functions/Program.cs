using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCOM.Services;

namespace MCOM.Archiving.Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s => {
                    s.AddScoped<IQueueService, QueueService>();
                    s.AddScoped<IGraphService, GraphService>();
                    s.AddScoped<IBlobService, BlobService>();
                    s.AddScoped<IAppInsightsService, AppInsightsService>();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                    builder.AddApplicationInsights(context.Configuration["APPINSIGHTS_CONNECTION_STRING"]);
                }).Build();

            host.Run();
        }
    }
}