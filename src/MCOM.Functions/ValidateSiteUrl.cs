using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnP.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MCOM.Functions
{
    public class ValidateSiteUrl
    {
        private readonly IPnPContextFactory _pnpContextFactory;
        private readonly ILogger _logger;
        private IMicrosoft365Service _microsoft365Service;

        public ValidateSiteUrl(ILoggerFactory loggerFactory, IPnPContextFactory pnpContextFactory, IMicrosoft365Service microsoft365Service)
        {
            _logger = loggerFactory.CreateLogger<ValidateSiteUrl>();
            _pnpContextFactory = pnpContextFactory;
            _microsoft365Service = microsoft365Service;
        }

        [Function("ValidateSiteUrl")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception ex)
            {
                var msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(ex, msg + "Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, ex.Message);
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "ValidateSiteUrl");
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "ValidateSiteUrl", "Provisioning"))
            {
                HttpResponseData response = null;                
                try
                {
                    // Parse query parameters
                    var query = QueryHelpers.ParseQuery(req.Url.Query);

                    // Get parameters from body in case og POST
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);

                    // Get url from query or body
                    string siteUrl = query.Keys.Contains("siteUrl") ? query["siteUrl"] : data?.siteUrl;

                    // Validate group alias before continuing
                    bool valid = StringUtilities.ValidateAliasText(siteUrl);
                    if (!valid)
                    {
                        ValidateGroupResponse responseBody = new ValidateGroupResponse();
                        responseBody.Valid = false;
                        responseBody.Message = "The group email name can't contain symbols other than underscores, dashes, single quotes, and periods (_, -, ', .), and can't start or end with a period.\r\n\r\n";
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.Headers.Add("Content-Type", "application/json");
                        response.WriteString(JsonConvert.SerializeObject(responseBody));
                        return response;
                    }

                    // Create the PnP Context
                    using (var pnpContext = await _pnpContextFactory.CreateAsync("Default"))
                    {
                        ValidateSiteResponse responseBody = new ValidateSiteResponse();
                        try
                        {
                            var fullUrl = StringUtilities.GetFullUrl(siteUrl);
                            await _microsoft365Service.CheckIfSiteExists(pnpContext, fullUrl);
                            responseBody.Valid = true;
                            responseBody.Message = "The site url is available";
                            response = req.CreateResponse(HttpStatusCode.OK);
                        }
                        catch (UnavailableUrlException siteException)
                        {
                            Global.Log.LogWarning(siteException.Message);                           
                            responseBody.Valid = false;
                            responseBody.Message = siteException.Message;
                            response = req.CreateResponse(HttpStatusCode.Conflict);                            
                        } 
                        response.Headers.Add("Content-Type", "application/json");
                        response.WriteString(JsonConvert.SerializeObject(responseBody));
                        return response;                        
                    }                    
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                    return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, ex.Message);
                }
            }
        }
    }

    public class ValidateSiteResponse {
        public bool Valid { get; set; }
        public string Message { get; set; }
    }
}
