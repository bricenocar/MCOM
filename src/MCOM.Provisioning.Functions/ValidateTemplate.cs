using System.Net;
using System.Xml;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Services;
using MCOM.Models;
using MCOM.Utilities;

namespace MCOM.Provisioning.Functions
{
    public class ValidateTemplate
    {
        private readonly IBlobService _blobService;

        public ValidateTemplate(IBlobService blobService)
        {
            _blobService = blobService;
        }

        [Function("ValidateTemplate")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, [FromBody] string blobTemplatePath, FunctionContext context)
        {            
            var logger = context.GetLogger("ValidateTemplate");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception ex)
            {
                var msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(ex, msg + "Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }

            try
            {
                if (string.IsNullOrEmpty(blobTemplatePath))
                {
                    var msg = "The template path is empty.";
                    Global.Log.LogError(msg + "Error: {ErrorMessage}", msg);
                    return HttpUtilities.HttpResponse(req, HttpStatusCode.BadRequest, "false");
                }

                // Get template uri
                var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{blobTemplatePath}");                

                // Init blob service client
                _blobService.GetBlobServiceClient();

                // Get file from staging area
                var blobClient = _blobService.GetBlobClient(fileUri);
                var fileStream = await _blobService.GetBlobStreamAsync(blobClient);

                // Check file extension
                var fileExtention = Path.GetExtension(blobTemplatePath);

                switch (fileExtention.ToLower())
                {
                    case ".json":

                        try
                        {
                            using var fileStreamReader = new StreamReader(fileStream);
                            using var jsonFileTextReader = new JsonTextReader(fileStreamReader);
                            while (jsonFileTextReader.Read()) { }
                        }
                        catch (Exception ex)
                        {
                            Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                            return HttpUtilities.HttpResponse(req, HttpStatusCode.UnprocessableEntity, "false");
                        }

                        break;

                    case ".xml":

                        var readerSettings = new XmlReaderSettings();
                        /* Start - In case of xsd validation is going to be implemented */
                        // readerSettings.Schemas.Add("http://schemas.dev.office.com/PnP/2022/09/ProvisioningSchema", "ProvisioningSchema-2022-09.xsd");
                        // readerSettings.ValidationType = ValidationType.Schema;
                        // readerSettings.ValidationEventHandler += settingsValidationEventHandler;
                        /* End - In case of xsd validation is going to be implemented */

                        var xmlReader = XmlReader.Create(fileStream, readerSettings);

                        try
                        {
                            while (xmlReader.Read()) { }
                        }
                        catch (Exception ex)
                        {
                            Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                            return HttpUtilities.HttpResponse(req, HttpStatusCode.UnprocessableEntity, "false");
                        }

                        break;

                    default:
                        var msg = "Wrong file format. It has to be either xml or json files.";
                        Global.Log.LogError(msg + "Error: {ErrorMessage}", msg);
                        return HttpUtilities.HttpResponse(req, HttpStatusCode.BadRequest, "false");
                }

                return HttpUtilities.HttpResponse(req, HttpStatusCode.OK, "true");
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);                
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }
        }

        #region In case of xsd validation is going to be implemented
        // In case og XSD validation
        /*static void settingsValidationEventHandler(object? sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                Console.Write("WARNING: ");
                Console.WriteLine(e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                Console.Write("ERROR: ");
                Console.WriteLine(e.Message);
            }
        }*/
        #endregion
    }
}
