using MCOM.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
               .ConfigureFunctionsWorkerDefaults()
               .ConfigureServices((context, s) =>
               {
                   // Adding services to DI               
                   s.AddScoped<IBlobService, BlobService>();
               })
               .ConfigureLogging((context, builder) =>
               {
                   builder.AddApplicationInsights(context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
               }).Build();

host.Run();
