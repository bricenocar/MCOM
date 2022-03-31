using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MCOM.Utilities
{
    public class HttpClientUtilities
    {
        public static async Task<HttpResponseMessage> PostAsync(string clientUrl, object content, Dictionary<string, string> headers = null)
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

            // Add headers
            foreach(var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            // Get response from the request
            var response = await client.PostAsync(responseUri.PathAndQuery, requestContent);

            return response;
        }
    }
}
