using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using MCOM.Models.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static System.Formats.Asn1.AsnWriter;

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
        /// SendAsync a Post Http Request with FormUrlEncodedContent encoded body
        /// </summary>
        /// <param name="clientUrl"></param>
        /// <param name="content"></param>
        /// <param name="mediaType"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendAsync(string clientUrl, string content, string mediaType, Dictionary<string, string> headers = null)
        {
            using var httpClient = new HttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, clientUrl);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }
                        
            httpRequest.Content = new StringContent(content, Encoding.UTF8, mediaType);

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
            // Get token to make calls against Office365
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

        /// <summary>
        /// Get Bearer token
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="scope"></param>
        /// <param name="grantType"></param>
        /// <param name="authUrl"></param>
        /// <returns>Token deserialized</returns>
        public static async Task<BearerToken> GetTokenAsync(string clientId, string clientSecret, string scope, string grantType, string authUrl)
        {
            var collection = new List<KeyValuePair<string, string>>
            {
                new("client_id", clientId),
                new("client_secret", clientSecret),
                new("scope", scope),
                new("grant_type", grantType)
            };

            // Get AD token
            var httpAzureADResponse = await SendAsync(authUrl, collection);

            httpAzureADResponse.EnsureSuccessStatusCode();

            // Get token object
            var tokenObject = await httpAzureADResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<BearerToken>(tokenObject);
        }
    }
}
