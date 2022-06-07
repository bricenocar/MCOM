using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using MCOM.Utilities;
using MCOM.Models;
using MCOM.Services;

namespace MCOM.Functions
{
    public class GetManagedMetadata
    {
        private ISharePointService _sharePointService;
        private IAzureService _azureService;

        public GetManagedMetadata(ISharePointService sharePointService, IAzureService azureService)
        {
            _sharePointService = sharePointService;
            _azureService = azureService;
        }

        [Function("GetManagedMetadata")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetManagedMetadata");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "An error occured when setting environmental variables", e);
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                resp.WriteString("Config values missing or bad formatted in app config");
                return resp;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "GetManagedMetadata");

            // Parse query parameters
            var query = QueryHelpers.ParseQuery(req.Url.Query);

            // Get request from body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string responseMessage = "";

            // Get dynamic data
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Get query params
            string webUrl = query.Keys.Contains("webUrl") ? query["webUrl"] : data?.webUrl;
            string termGroup = query.Keys.Contains("termGroup") ? query["termGroup"] : data?.termGroup;
            string termSetName = query.Keys.Contains("termSetName") ? query["termSetName"] : data?.termSetName;
            string termName = query.Keys.Contains("termName") ? query["termName"] : data?.termName;

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetManagedMetadata", "Archiving"))
            {
                HttpResponseData response = null;

                if (string.IsNullOrEmpty(webUrl))
                {
                    Global.Log.LogError(new NullReferenceException(), "Parameter 'webUrl' is expected but not received in body");
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString("Parameter 'web' is expected but not received in body");
                    return response;
                }

                try
                {
                    // Get token using managed identity
                    var fileUri = new Uri(webUrl);
                    var accessToken = await _azureService.GetAzureServiceTokenAsync(fileUri);

                    // Get SharePoint context with access token                   
                    var clientContext = _sharePointService.GetClientContext(webUrl, accessToken.Token);
                    var taxonomySession = _sharePointService.GetTaxonomySession(clientContext);
                    var termStore = _sharePointService.GetDefaultSiteCollectionTermStore(taxonomySession);

                    // Load data from SharePoint
                    _sharePointService.Load(clientContext, termStore, store => store.Groups.Include(group => group.TermSets, groups => groups.Name));
                    _sharePointService.ExecuteQuery(clientContext);

                    var termsResult = new List<Models.Taxonomy.Term>();
                    var groups = termStore.Groups.Where(g => g.Name.Equals(termGroup));
                    if (!groups.Any())
                    {
                        Global.Log.LogError(new NullReferenceException(), "No groups found with the specified name");
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString("No groups found with the specified name");
                        return response;
                    }

                    foreach (var group in groups)
                    {
                        //var termSets = group.TermSets.Where(ts => ts.Name.Equals(termSetName));
                        var termSets = _sharePointService.GetTermSets(group, termSetName);
                        if (!termSets.Any())
                        {
                            Global.Log.LogError(new NullReferenceException(), "No termset found with the specified name");
                            response = req.CreateResponse(HttpStatusCode.BadRequest);
                            response.WriteString("No termset found with the specified name");
                            return response;
                        }

                        foreach (var termSet in termSets)
                        {
                            if (!string.IsNullOrEmpty(termName))
                            {
                                var label = _sharePointService.GetLabelMatchInformation(clientContext, termName);
                                var myTerms = _sharePointService.GetTerms(termSet, label);

                                _sharePointService.Load(clientContext, myTerms);
                                _sharePointService.ExecuteQuery(clientContext);

                                foreach (var term in myTerms)
                                {
                                    termsResult.Add(new Models.Taxonomy.Term()
                                    {
                                        Label = term.Name,
                                        TermId = term.Id.ToString(),
                                        ComposedString = $"{term.Name}|{term.Id}"
                                    });
                                }

                                responseMessage = JsonConvert.SerializeObject(termsResult.First());
                            }
                            else
                            {
                                var allTermsInTermSet = _sharePointService.GetAllTerms(termSet);

                                _sharePointService.Load(clientContext, allTermsInTermSet);
                                _sharePointService.ExecuteQuery(clientContext);

                                foreach (var term in allTermsInTermSet)
                                {
                                    termsResult.Add(new Models.Taxonomy.Term()
                                    {
                                        Label = term.Name,
                                        TermId = term.Id.ToString(),
                                        ComposedString = $"{term.Name}|{term.Id}"
                                    });
                                }

                                responseMessage = JsonConvert.SerializeObject(termsResult);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Unhandled exception found: {ErrorMessage}", ex.Message);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(ex.Message);
                    return response;
                }

                response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteString(responseMessage);
                return response;
            }
        }
    }
}

