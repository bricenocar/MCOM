using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MCOM.Provisioning.Functions
{
    public class GetJWT
    {
        private readonly ILogger _logger;

        public GetJWT(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetJWT>();
        }

        [Function("GetJWT")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/e78a86b8-aa34-41fe-a537-9392c8870bf0/oauth2/v2.0/token");
            var collection = new List<KeyValuePair<string, string>>
            {
                new("client_id", "43db244a-1e1a-4414-b60c-8279f3f7e6ff"),
                new("client_secret", "MFw8Q~y_zwrQvzrts3ggTBB0PWs64hj.qBy6QabY"),
                new("scope", "api://5e0a5f0a-db01-473c-b448-0e9711ed8f9a/.default"),
                new("grant_type", "client_credentials")
            };
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadAsStringAsync();

            Console.WriteLine(await response.Content.ReadAsStringAsync());

            var responseToReturn = req.CreateResponse(HttpStatusCode.OK);
            responseToReturn.Headers.Add("Content-Type", "application/json");
            responseToReturn.WriteString(token);

            return responseToReturn;
        }
    }
}
