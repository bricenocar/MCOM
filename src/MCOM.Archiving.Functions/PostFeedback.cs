using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using MCOM.Services;
using MCOM.Models;
using MCOM.Models.Azure;
using MCOM.Utilities;
using MCOM.Business.PostFeedBack;

namespace MCOM.Archiving.Functions
{
    public class PostFeedback
    {
        private IQueueService _queueService;
        private IPostFeedBackBusiness _postFeedBackBusiness;

        public PostFeedback(IQueueService queueService, IPostFeedBackBusiness postFeedBackBusiness)
        {
            _queueService = queueService;
            _postFeedBackBusiness = postFeedBackBusiness;
        }

        [Function("PostFeedback")]
        public async Task RunAsync([QueueTrigger("feedbackqueue", Connection = "QueueConnectionString")] string queueItem, FunctionContext context)
        {
            var logger = context.GetLogger("PostFeedback");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            Global.Log.LogInformation("The data coming is: {PostFeedBack}", queueItem);

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "PostFeedback");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "PostFeedback", "Archiving"))
            {
                try
                {
                    // Build client and send request
                    var queueObject = JsonConvert.DeserializeObject<QueueItem>(queueItem);

                    // Validate object
                    if (queueObject.ClientUrl == null || queueObject.Content == null || queueObject.Source == null)
                    {
                        Global.Log.LogError(new NullReferenceException(), $"Error: Error when deserializing the object into a QueueItem");
                        throw new Exception("Error when deserializing the object into a QueueItem");
                    }

                    // Validate null guid
                    if (queueObject.Content.DocumentId.Equals("00000000-0000-0000-0000-000000000000"))
                    {
                        Global.Log.LogError(new NullReferenceException(), "Error: Got null guid");
                        throw new Exception("Got null guid");
                    }

                    // Run business layer and transform queue object
                    queueObject = _postFeedBackBusiness.GetQueueItem(queueObject);

                    // Send the http request
                    var response = await _queueService.PostFeedbackAsync(queueObject);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Global.Log.LogInformation("Successfully sent response to feedback url: {ClientUrl}. DocumentId: {DocumentId}", queueObject.ClientUrl, queueObject.Content.DocumentId);
                    }
                    else
                    {
                        Global.Log.LogError(new NullReferenceException(), "Error trying to send feedback response. Message: {ErrorMessage}. DocumentId: {DocumentId}.", responseContent, queueObject.Content.DocumentId);
                        throw new Exception(response.ReasonPhrase);
                    }
                }
                catch (Exception e)
                {
                    // log to analytics
                    Global.Log.LogError(e, "Exception: {ErrorMessage}", e.Message);
                    throw;
                }
            }
        }
    }
}
