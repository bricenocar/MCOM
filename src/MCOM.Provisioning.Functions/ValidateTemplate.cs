using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MCOM.Services;
using MCOM.Models;
using MCOM.Utilities;
using MCOM.Models.Provisioning;
using Newtonsoft.Json;
using System.Xml.Schema;
using System.Xml;
using Azure.Storage.Blobs;

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
            var logger = context.GetLogger("ValidateTemplate");
            HttpResponseData response = null;

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                string msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(msg);
                return response;
            }

            // Init blob service client
            _blobService.GetBlobServiceClient();

            // Get request data
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var fileData = JsonConvert.DeserializeObject<ProvisioningRequestPayload>(requestBody);

            if(string.IsNullOrEmpty(fileData?.BlobFilePath) || string.IsNullOrEmpty(fileData?.FileName))
            {
                // Throw error message missing values
            }

            // Get template uri
            var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{fileData?.BlobFilePath}");

            // Replace special characters
            var fileName = StringUtilities.RemoveSpecialChars(fileData?.FileName);

            // Get file from staging area
            var blobClient = _blobService.GetBlobClient(fileUri);
            var blobContainerClient = _blobService.GetBlobContainerClient(blobClient.BlobContainerName);
            var blobContainerProperties = await _blobService.GetBlobContainerPropertiesAsync(blobContainerClient);

            var fileContent = await _blobService.GetBlobDataAsync(blobClient);
            var fileStream = await _blobService.GetBlobStreamAsync(blobClient);

            // Check file extension
            var fileExtention = Path.GetExtension(fileName);

            switch (fileExtention.ToLower())
            {
                case "json":

                    try
                    {
                        using var fileStreamReader = new StreamReader(fileStream);
                        using var jsonFileTextReader = new JsonTextReader(fileStreamReader);
                        while (jsonFileTextReader.Read()) { }
                        // Log information OK...
                    }
                    catch (Exception ex)
                    {
                        // Log Exception to Logs and keep going...
                    }

                    break;

                case "xml":

                    var booksSettings = new XmlReaderSettings();
                    // booksSettings.Schemas.Add("http://schemas.dev.office.com/PnP/2022/09/ProvisioningSchema", "ProvisioningSchema-2022-09.xsd"); In case XSD Implementation
                    // booksSettings.ValidationType = ValidationType.Schema;
                    // booksSettings.ValidationEventHandler += settingsValidationEventHandler; // In case XSD Implementation

                    var books = XmlReader.Create(fileStream, booksSettings);

                    while (books.Read()) { }

                    Console.Write("Done!");

                    break;

                default:
                    // Throw exception???
                    break;
            }

            response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("The template is valid!");

            return response;
        }

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
    }
}
