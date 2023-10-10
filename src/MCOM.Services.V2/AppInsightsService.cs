using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Rest;
using MCOM.Extensions;
using MCOM.Models;
using MCOM.Models.AppInsights;
using MCOM.Utilities;

namespace MCOM.Services
{
    public interface IAppInsightsService
    {
        Task<ApplicationInsightsDataClient> GetApplicationInsightsDataClientAsync();
        Task<List<AppInsightsEvent>> GetCustomEventsByDocumentId(string operation, string documentId);
    }

    public class AppInsightsService : IAppInsightsService
    {
        private AccessToken AppInsightsToken { get; set; }
        private ApplicationInsightsDataClient ApplicationInsightsDataClient { get; set; }


        public async Task<ApplicationInsightsDataClient> GetApplicationInsightsDataClientAsync()
        {
            if (ApplicationInsightsDataClient == null || AppInsightsToken.ExpiresOn >= DateTime.Now)
            {
                AppInsightsToken = await AzureUtilities.GetAzureServiceTokenAsync("https://api.applicationinsights.io/");
                var serviceClientCredentials = new MCOMServiceClientCredentials(AppInsightsToken);
                ApplicationInsightsDataClient = new ApplicationInsightsDataClient(serviceClientCredentials);
            }

            return ApplicationInsightsDataClient;
        }

        public async Task<List<AppInsightsEvent>> GetCustomEventsByDocumentId(string operation, string documentId)
        {
            ApplicationInsightsDataClient = await GetApplicationInsightsDataClientAsync();

            // Init events list
            var events = new List<AppInsightsEvent>();

            try
            {
                // Check if structured logs is enabled, otherwise query the Message property.
                var query = $"traces" +
                    $"| where customDimensions[\"MCOMOperationTrace\"] in (\"" + operation + "\") and customDimensions[\"DocumentId\"] in (\"" + documentId + "\")" +
                    $"| project timestamp, appName, operation_Name, message, customDimensions[\"DocumentId\"], customDimensions[\"LogLevel\"],  ingestion_time()" +
                    $"| order by timestamp desc" +
                    $"| take 10";

                // Get the query results
                var queryResults = ApplicationInsightsDataClient.Query.Execute(Global.AppInsightsAppId, query);

                // Check if there is no data in response
                if (!queryResults.Tables.Any() || !queryResults.Tables.First().Rows.Any())
                {
                    events.Add(new AppInsightsEvent()
                    {
                        Message = "There is no information found for the specified document id yet. Try again later",
                        EventDate = DateTime.Now,
                        LogLevel = "EmptyData",
                    });
                }

                for (var i = 0; i < queryResults.Tables[0].Rows.Count; i++)
                {
                    var logEntry = queryResults.Tables[0].Rows[i];
                    var eventDate = Convert.ToDateTime(logEntry[0].ToString());

                    events.Add(new AppInsightsEvent()
                    {
                        Message = logEntry[3].ToString(),
                        EventDate = eventDate.TryParseToLocalTime(),
                        LogLevel = logEntry[5].ToString(),
                    });
                }
            }
            catch (Exception e)
            {
                events.Add(new AppInsightsEvent()
                {
                    Message = $"Error running query. ErrorMessage: {e.Message}",
                    EventDate = DateTime.Now,
                    LogLevel = "Exception",
                });
            }

            return events;
        }


        /// <summary>
        /// Class used to get client credentials. The request will add a bearer token on the fly
        /// </summary>
        public class MCOMServiceClientCredentials : ServiceClientCredentials
        {
            private const string _bearerTokenType = "Bearer";
            private readonly AccessToken _token;

            public MCOMServiceClientCredentials(AccessToken token)
            {
                _token = token;
            }

            public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_bearerTokenType, _token.Token);
                await base.ProcessHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
