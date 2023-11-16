using Azure;
using Azure.Messaging.ServiceBus;
using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace MCOM.Functions
{
    public class PreProcessRequest
    {
        private IBlobService _blobService;
        private ISharePointService _sharePointService;
        private readonly IDataBaseService _dataBaseService;
        private readonly ILogger<PreProcessRequest> _logger;

        public PreProcessRequest(ILogger<PreProcessRequest> logger, IBlobService blobService, ISharePointService sharePointService, IDataBaseService dataBaseService)
        {
            _logger = logger;
            _blobService = blobService;
            _sharePointService = sharePointService;
            _dataBaseService = dataBaseService;
        }

        [Function(nameof(PreProcessRequest))]
        [ServiceBusOutput("preprocessed", Connection = "ServiceBusConnectionString")]
        public async Task<string> Run([ServiceBusTrigger("created", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                throw;
            }

            var response = string.Empty;
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "CreateWorkload", "Provisioning"))
            {               
                try
                {
                    // Parse the JSON request body to get parameters
                    var body = message.Body.ToString();
                    WorkloadCreationRequestPayload workloadData = JsonConvert.DeserializeObject<WorkloadCreationRequestPayload>(body);
                    response = JsonConvert.SerializeObject(workloadData, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                }
                catch (JsonException jsonEx)
                {
                    Global.Log.LogError(jsonEx, "Error deserializing the request body. Error: {ErrorMessage}", jsonEx.Message);
                    throw;
                }
            }

            return response;
        }
    }
}
