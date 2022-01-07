using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MCOM.Utilities
{
    public class HttpClientUtilities
    {
        public static async Task<HttpResponseMessage> PostFeedbackAsync(string clientUrl, object content)
        {
            // Build response url and request content
            var responseUri = new Uri(clientUrl);
            var requestContent = new StringContent(JsonConvert.SerializeObject(content, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            // Build http client
            using var client = new HttpClient();
            client.BaseAddress = new Uri($"{responseUri.Scheme}://{responseUri.Host}");

            // Get response from the request
            var response = await client.PostAsync(responseUri.PathAndQuery, requestContent);

            return response;
        }
    }
}
