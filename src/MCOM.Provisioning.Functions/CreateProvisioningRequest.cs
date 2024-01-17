using Azure.Messaging.ServiceBus;
using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace MCOM.Provisioning.Functions
{
    public class CreateProvisioningRequest
    {
        private readonly ILogger _logger;

        public CreateProvisioningRequest(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateProvisioningRequest>();
        }

        [Function(nameof(CreateProvisioningRequest))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception e)
            {
                string msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                throw new ArgumentException(msg, e);
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "CreateProvisioningRequest");

            HttpResponseData? response = null;
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "CreateProvisioningRequest", "Provisioning"))
            {
                try
                {
                    // Get body from request
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    if (requestBody == null)
                    {
                        throw new InvalidRequestException("body", "There is no body present");
                    }

                    // Print request
                    Global.Log.LogInformation($"Body: {requestBody}");

                    // Deserialize the request body
                    WorkloadCreationRequestPayload? workloadData = JsonConvert.DeserializeObject<WorkloadCreationRequestPayload>(requestBody, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });

                    // Validate request before continuiung
                    ValidateRequestPayload(workloadData);

                    // Get Service Bus connection string from environment variables
                    var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");

                    // Create a Service Bus client
                    await using (ServiceBusClient client = new ServiceBusClient(serviceBusConnectionString))
                    {
                        // Create a sender for the queue
                        ServiceBusSender sender = client.CreateSender("requested");

                        // Format message before sending it to the queue
                        var messageBody = JsonConvert.SerializeObject(workloadData, Formatting.Indented, new JsonSerializerSettings
                        {                            
                            NullValueHandling = NullValueHandling.Ignore,
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        });

                        // Create a message and send it to the queue
                        ServiceBusMessage message = new ServiceBusMessage(messageBody);
                        await sender.SendMessageAsync(message);
                    }

                    // Respond to http request
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = "Request received",
                        Status = "Success"
                    }));
                    return response;
                }
                catch (JsonException jsonEx)
                {
                    Global.Log.LogError(jsonEx, "Error deserializing the request body. Error: {ErrorMessage}", jsonEx.Message);
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = string.Format("Error deserializing the request body. Error: {ErrorMessage}", jsonEx.Message),
                        Status = "Error"
                    }));
                    return response;
                }
                catch (InvalidRequestException invEx)
                {
                    Global.Log.LogError(invEx, "Invalid request. Error: {ErrorMessage}", invEx.Message);
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = string.Format("Invalid request. Error: {ErrorMessage}", invEx.Message),
                        Status = "Error"
                    }));
                    return response;
                }
            }
        }

        /// <summary>
        /// Validate the request payload
        private static void ValidateRequestPayload(WorkloadCreationRequestPayload workloadData)
        {
            // Validate the request
            if (workloadData == null || workloadData.Request == null)
            {
                throw new InvalidRequestException("body", "There is no body present");
            }

            if (workloadData.Site == null || workloadData.Site.SiteConfig == null)
            {
                throw new InvalidRequestException("There is no site object or site config present in the request");
            }

            if (workloadData.Request.WorkloadId == 0)
            {
                throw new InvalidRequestException("Workload id is null or empty");
            }

            if (workloadData.Site.SiteConfig.SiteType == SiteType.CommunicationSite &&
                workloadData.Site.SiteUsers.Owners.Count == 0)
            {
                throw new InvalidRequestException("There communication site requires a site owner");
            }

            if (workloadData.Team != null && workloadData.Site.GroupUsers.Owners.Count == 0)
            {
                throw new InvalidRequestException("There are no group owners present in the request. It is mandatory when creating Teams");
            }

            // Validate the quantity of owners and members based on requirements (configurable)
        }
    }
}
