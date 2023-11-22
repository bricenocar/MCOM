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
        /// <summary>
        /// Send Post Http Request
        /// </summary>
        /// <param name="clientUrl"></param>
        /// <param name="content"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
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

        /// <summary>
        /// SendAsync a Post Http Request with FormUrlEncodedContent encoded body
        /// </summary>
        /// <param name="clientUrl"></param>
        /// <param name="collection"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendAsync(string clientUrl, List<KeyValuePair<string, string>> collection, Dictionary<string, string> headers = null)
        {
            using var httpClient = new HttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, clientUrl);
            if(headers != null)
            {
                foreach (var header in headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }           

            var content = new FormUrlEncodedContent(collection);
            httpRequest.Content = content;

            return await httpClient.SendAsync(httpRequest);
        }

        /// <summary>
        /// SendAsync a Get Http Request
        /// </summary>
        /// <param name="clientUrl"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendAsync(string clientUrl, Dictionary<string, string> headers = null)
        {
            // Get token to make calls agaisnt Office365
            using var httpClient = new HttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, clientUrl);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }

            return await httpClient.SendAsync(httpRequest);
        }
    }
}
