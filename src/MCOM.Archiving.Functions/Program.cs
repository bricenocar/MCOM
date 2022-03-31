using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCOM.Services;
using MCOM.Business.PostFeedBack;

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
                    s.AddScoped<ISharePointService, SharePointService>();
                    s.AddScoped<IAzureService, AzureService>();
                    s.AddScoped<IPostFeedBackBusiness, PostFeedBackBusiness>();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddApplicationInsights(context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
                }).Build();

            host.Run();
        }
    }
}