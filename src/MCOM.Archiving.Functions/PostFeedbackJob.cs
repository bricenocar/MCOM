using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;

namespace MCOM.Archiving.Functions
{
    public class PostFeedbackJob
    {
        private IQueueService _queueService;

        public PostFeedbackJob(IQueueService queueService)
        {
            _queueService = queueService;
        }

        [Function("PostFeedbackJob")]
        public async Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo timer, FunctionContext context)
        {
            var logger = context.GetLogger("PostFeedbackJob");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError("Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "PostFeedbackJob");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "PostFeedbackJob", "Archiving"))
            {
                try
                {
                    // Getting env variables
                    var connString = Environment.GetEnvironmentVariable("QueueConnectionString");
                    var queueName = Environment.GetEnvironmentVariable("QueuePoisonName");

                    // Get the queue
                    var queue = _queueService.GetCloudQueue(connString, queueName);

                    // Get messages
                    var messages = await queue.GetMessagesAsync(32);

                    // Loop trhough messages and send the request to the client
                    foreach (var message in messages)
                    {
                        // Build client and send request
                        var queueObject = JsonConvert.DeserializeObject<QueueItem>(message.AsString);

                        // Validate object
                        if (queueObject.ResponseUrl == null || queueObject.Item == null)
                        {
                            Global.Log.LogError($"Error: Error when deserializing the object into a QueueItem");
                            throw new Exception("Error when deserializing the object into a QueueItem");
                        }

                        // Validate null guid
                        if (queueObject.Item.DocumentId.Equals("00000000-0000-0000-0000-000000000000"))
                        {
                            Global.Log.LogError("Error: Got null guid");
                            throw new Exception("Got null guid");
                        }

                        var response = await _queueService.PostFeedbackAsync(queueObject);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            Global.Log.LogInformation($"Success: {responseContent}");
                        }
                        else
                        {
                            Global.Log.LogError($"Error: {response.ReasonPhrase}. Message: {responseContent}");
                            //throw new Exception(response.ReasonPhrase);
                        }
                    }
                }
                catch (Exception e)
                {
                    // log to analytics
                    Global.Log.LogError($"Exception: {e.Message}");
                    throw;
                }
            }
        }
    }
}
