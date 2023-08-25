using System.Net;
using System.Xml;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Services;
using MCOM.Models;
using MCOM.Utilities;
using MCOM.Models.Provisioning;

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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
        {
            HttpResponseData response;
            var logger = context.GetLogger("ValidateTemplate");            

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                var msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("false");
                return response;
            }

            try
            {
                // Get request data
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var fileData = JsonConvert.DeserializeObject<ProvisioningRequestPayload>(requestBody);

                if (string.IsNullOrEmpty(fileData?.BlobFilePath) || string.IsNullOrEmpty(fileData?.FileName))
                {
                    var msg = "Missing request body params (BlobFilePath, FileName).";
                    Global.Log.LogError(msg + "Error: {ErrorMessage}", msg);
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString("false");
                    return response;
                }

                // Get template uri
                var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{fileData?.BlobFilePath}/{fileData?.FileName}");

                // Replace special characters
                var fileName = StringUtilities.RemoveSpecialChars(fileData?.FileName);

                // Init blob service client
                _blobService.GetBlobServiceClient();

                // Get file from staging area
                var blobClient = _blobService.GetBlobClient(fileUri);
                var fileStream = await _blobService.GetBlobStreamAsync(blobClient);

                // Check file extension
                var fileExtention = Path.GetExtension(fileName);

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
                            response = req.CreateResponse(HttpStatusCode.NotAcceptable);
                            response.Headers.Add("Content-Type", "application/json");
                            response.WriteString("false");
                            return response;
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
                        catch(Exception ex)
                        {
                            Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                            response = req.CreateResponse(HttpStatusCode.NotAcceptable);
                            response.Headers.Add("Content-Type", "application/json");
                            response.WriteString("false");
                            return response;
                        }

                        break;

                    default:
                        var msg = "Wrong file format. It has to be either xml or json files.";
                        Global.Log.LogError(msg + "Error: {ErrorMessage}", msg);
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.Headers.Add("Content-Type", "application/json");
                        response.WriteString("false");
                        return response;
                }

                response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("true");

                return response;
            }
            catch (Exception ex)
            {                 
                Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("false");
                return response;
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
