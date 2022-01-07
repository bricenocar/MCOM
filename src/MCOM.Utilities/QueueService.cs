using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using MCOM.Models;

namespace MCOM.Utilities
{
    public class QueueService
    {
        public virtual async Task<HttpResponseMessage> PostFeedbackAsync(QueueItem queueMessage)
        {
            // Send request to endpoint
            using var client = new HttpClient();
            var responseUri = new Uri(queueMessage.ResponseUrl);
            client.BaseAddress = new Uri($"{responseUri.Scheme}://{responseUri.Host}");
            var requestContent = new StringContent(JsonConvert.SerializeObject(queueMessage.Item, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));
            var response = await client.PostAsync(responseUri.PathAndQuery, requestContent);
            return response;
        }
    }
}
