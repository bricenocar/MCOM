using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MCOM.Services;
using MCOM.Data.DBContexts;

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

                    // DB Context
                    s.AddDbContext<GovernanceDBContext>(options => options.UseSqlServer(context.Configuration["GOVERNANCEDB_CONNECTIONSTRING"]));
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddApplicationInsights(context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
                }).Build();

            host.Run();
        }
    }
}