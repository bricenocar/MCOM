using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Utilities;

namespace MCOM.Provisioning.Functions
{
    public class GetOptionalValues
    {      
        public GetOptionalValues()
        {
           
        }

        [Function("GetOptionalValues")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetOptionalValues");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }

            try
            {
                // Temporary static code to build the templates
                var availableTemplates = new List<GetOptionalValuesPayload>()
                {
                    new GetOptionalValuesPayload()
                    {
                        Id = "1",
                        Title = "SharePoint Collaboration site with Teams",
                        Description = "A Sharepoint Team Site is a collaborative workspace in Microsoft Sharepoint that allows teams to work together and share information. It is designed to work together and share information.It is designed to provide a central location where team members can store,organize, and collaborate on documents, lists,calendars and other types of content.",
                        Icon = "SharePoint"
                    },
                    new GetOptionalValuesPayload()
                    {
                        Id = "2",
                        Title = "Teams",
                        Description = "Teams is a collaboration platform that allows users to communicate, collaborate, and share content in real-time through chat and instant messaging, audio  and video calls, screen sharing and co-authoring.It is integrated with other Microsoft 365 apps and there is always a SharePoint connected for file storage. By choosing this option be aware that you get connected SharePoint site. ",
                        Icon = "Teams"
                    },
                    new GetOptionalValuesPayload()
                    {
                        Id = "3",
                        Title = "SharePoint Collaboration site",
                        Description = "A Sharepoint Team Site is a collaborative workspace in Microsoft Sharepoint that allows teams to work together and share information. It is designed to work together and share information.It is designed to provide a central location where team members can store,organize, and collaborate on documents, lists,calendars and other types of content.",
                        Icon = "SharePoint"
                    },
                    new GetOptionalValuesPayload()
                    {
                        Id = "4",
                        Title = "SharePoint Communication site",
                        Description = "A SharePoint Communications Site is designed to be visually appealing and easy to navigate and for sharing information with a wide audience. It is typically used for creating and sharing content such as news,announcements, events, and other types of information that are relevant to a large group of people. NOTE: For SharePoint Communication Site, there is no associated group, so the users will be added into the SharePoint Groups.",
                        Icon = "SharePoint"
                    },
                    new GetOptionalValuesPayload()
                    {
                        Id = "5",
                        Title = "Viva engage (Yammer)",
                        Description = "A Yammer Community Group is a specific group within the Yammer platform that is focused on a particular topic, interest, or function within the organisation. Members of a Yammer Community Group can post updates, ask questions, share ideas, and collaborate with others who share similar interests or work on similar projects.",
                        Icon = "Teams"
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
