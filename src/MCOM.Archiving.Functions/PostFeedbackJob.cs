using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using MCOM.Business.PostFeedBack;

namespace MCOM.Archiving.Functions
{
    public class PostFeedbackJob
    {
        private IQueueService _queueService;
        private IPostFeedBackBusiness _postFeedBackBusiness;

        public PostFeedbackJob(IQueueService queueService, IPostFeedBackBusiness postFeedBackBusiness)
        {
            _queueService = queueService;
            _postFeedBackBusiness = postFeedBackBusiness;
        }

        [Function("PostFeedbackJob")]
        public async Task RunAsync([TimerTrigger("0 */15 * * * *")] TimerInfo timer, FunctionContext context)
        {
            var logger = context.GetLogger("PostFeedbackJob");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
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
                        try
                        {
                            // Build client and send request
                            var queueItem = JsonConvert.DeserializeObject<QueueItem>(message.AsString);

                            // Validate object
                            if (queueItem.ClientUrl == null || queueItem.Content == null || queueItem.Source == null)
                            {
                                Global.Log.LogError(new NullReferenceException(), $"Error: Error when deserializing the object into a QueueItem");
                                throw new Exception("Error when deserializing the object into a QueueItem");
                            }

                            // Validate null guid
                            if (queueItem.Content.DocumentId.Equals("00000000-0000-0000-0000-000000000000"))
                            {
                                Global.Log.LogError(new NullReferenceException(), "Error: Got null guid");
                                throw new Exception("Got null guid");
                            }

                            // Run business layer and transform queue item
                            queueItem = _postFeedBackBusiness.GetQueueItem(queueItem);

                            // Send the http request
                            var response = await _queueService.PostFeedbackAsync(queueItem);
                            var responseContent = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode)
                            {
                                Global.Log.LogInformation($"Success: {responseContent}");
                            }
                            else
                            {
                                Global.Log.LogError(new NullReferenceException(), $"Error: {response.ReasonPhrase}. Message: {responseContent}");
                                //throw new Exception(response.ReasonPhrase);
                            }
                        }
                        catch (Exception ex)
                        {
                            // log to analytics
                            Global.Log.LogError(ex, $"Exception trying to call destination endpoint: {ex.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    // log to analytics
                    Global.Log.LogError(e, $"Exception: {e.Message}");
                    throw;
                }
            }
        }
    }
}
