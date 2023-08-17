using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MCOM.Services;
using MCOM.Models;
using MCOM.Utilities;
using MCOM.Models.ScanOnDemand;
using Newtonsoft.Json;
using System.Xml.Schema;
using System.Xml;

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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext context)
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

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // var fileData = JsonConvert.DeserializeObject<ScanRequestPayload>(requestBody);

            var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{fileData.BlobFilePath}");

            // Replace special characters
            var fileName = StringUtilities.RemoveSpecialChars(fileData.FileName);

            // Get file from staging area
            var blobCLient = _blobService.GetBlobClient(fileUri);
            var blobContainerClient = _blobService.GetBlobContainerClient(blobCLient.BlobContainerName);
            var stream = await _blobService.OpenReadAsync(blobCLient);



            response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("The template is valid!");

            return response;
        }
    }
}





// Code
// Get file content (template)


// Get file extension


// Switch based on file format


try
{
    using var streamReader = new StreamReader("JsonFile.json");
    using var jsonTextReader = new JsonTextReader(streamReader);
    while (jsonTextReader.Read()) { }
    Console.WriteLine("Done");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

var booksSettings = new XmlReaderSettings();
//booksSettings.Schemas.Add("http://schemas.dev.office.com/PnP/2022/09/ProvisioningSchema", "ProvisioningSchema-2022-09.xsd"); In case XSD Implementation
booksSettings.ValidationType = ValidationType.Schema;
booksSettings.ValidationEventHandler += settingsValidationEventHandler; // In case XSD Implementation

var books = XmlReader.Create("ProvisioningSchema-2022-09-FullSample-01.xml", booksSettings);

while (books.Read()) { }

Console.Write("Done!");

static void settingsValidationEventHandler(object? sender, ValidationEventArgs e)
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
}
