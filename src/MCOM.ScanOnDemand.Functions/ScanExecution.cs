using System;
using System.IO;
using MCOM.Models;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MCOM.ScanOnDemand.Functions
{
    public class ScanExecution
    {
        private readonly ILogger _logger;

        public ScanExecution(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ScanExecution>();
        }

        [Function("ScanExecution")]
        public void Run([BlobTrigger("scanexecutions/{name}", Connection = "BlobStorageConnectionString")] string data, string name, FunctionContext context)
        {
            var logger = context.GetLogger("ScanExecution");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "ArchiveFile");

            // Convert data
            var fileData = JsonConvert.DeserializeObject<ArchiveFileData<string, object>>(data);

            // Validate input
            fileData.ValidateInput();

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "ArchiveFile", "Archiving"))
            {

            }
        }
    }
}
