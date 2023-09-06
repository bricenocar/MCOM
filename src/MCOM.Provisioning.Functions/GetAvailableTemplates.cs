using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace MCOM.Provisioning.Functions
{
    public class GetAvailableTemplates
    {

        public GetAvailableTemplates()
        {
            // DI services
        }

        [Function("GetAvailableTemplates")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, [FromQuery] string optionId, FunctionContext context)
        {
            var logger = context.GetLogger("GetAvailableTemplates");

            try
            {
                // COnfigure all envirionment variables
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }

            try
            {
                // Check option id coming from querystring
                if (string.IsNullOrEmpty(optionId))
                {
                    return HttpUtilities.HttpResponse(req, HttpStatusCode.BadRequest, "false");
                }

                // Temporary static code to build the templates
                var availableTemplates = new List<GetAvailableTemplatesPayload>()
                {
                    new GetAvailableTemplatesPayload()
                    {
                        OptionId = optionId,
                        Icon = "",
                        Title = "Standard Template",
                        Description = "<div>" +
                                          "<div>" +
                                            "<h3>Sharepoint Collaboration site:</h3>" +
                                            "<div>" +
                                              "<ul>" +
                                                "<li>Document library</li>" +
                                                "<li>Lists and Pages</li>" +
                                                "<li>OneNote</li>" +
                                              "</ul>" +
                                            "</div>" +
                                          "</div>" +
                                          "<div>" +
                                              "<h3>Teams:</h3>" +
                                            "<div>" +
                                              "<ul>" +
                                                "<li>Channels</li>" +
                                                "<li>Posts</li>" +
                                                "<li>Files</li>" +
                                              "</ul>" +
                                            "</div>" +
                                          "</div>" +
                                        "</div>",
                        FileBlobName = "Json Template",
                        FileBlobPath = "templates/JsonFile.json"
                    },
                    new GetAvailableTemplatesPayload()
                    {
                        OptionId = optionId,
                        Icon = "",
                        Title = "Nice Template",
                         Description = "<div>" +
                                          "<div>" +
                                            "<h3>Sharepoint Collaboration site:</h3>" +
                                            "<div>" +
                                              "<ul>" +
                                                "<li>Document library</li>" +
                                                "<li>Lists and Pages</li>" +
                                                "<li>OneNote</li>" +
                                              "</ul>" +
                                            "</div>" +
                                          "</div>" +
                                          "<div>" +
                                              "<h3>Teams:</h3>" +
                                            "<div>" +
                                              "<ul>" +
                                                "<li>Channels</li>" +
                                                "<li>Posts</li>" +
                                                "<li>Files</li>" +
                                              "</ul>" +
                                            "</div>" +
                                          "</div>" +
                                        "</div>",
                        FileBlobName = "Xml Template",
                        FileBlobPath = "templates/XmlFile.xml"
                    }
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(availableTemplates));

                return response;
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }
        }
    }
}
