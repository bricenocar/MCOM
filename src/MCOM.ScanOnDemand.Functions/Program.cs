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
                .ConfigureServices((context, services) =>
                {
                    // DB Context
                    services.AddDbContext<GovernanceDBContext>(options => options.UseSqlServer(context.Configuration["GOVERNANCEDB_CONNECTIONSTRING"]));

                    // Adding services to DI               
                    services.AddScoped<IBlobService, BlobService>();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddApplicationInsights(context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
                }).Build();

            host.Run();
        }
    }
}