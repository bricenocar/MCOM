using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MCOM.ScanOnDemand.Functions
{
    public class PostScanRequest
    {
        private readonly IBlobService _blobService;

        public PostScanRequest(IBlobService blobService)
        {
            _blobService = blobService;
        }

        [Function("PostScanRequest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("PostScanRequest");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                string msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(msg);
                return response;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "PostScanRequest");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "PostScanRequest", "ScanOnDemand"))
            {
                // Get the request object
                HttpResponseData response = null;
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<ScanRequestPayload>(requestBody);

                // Do some logic here based on the data coming from the request...

                // Give a new order number           
                data.OrderNumber = Guid.NewGuid();
                data.Status = "Delivered"; // TODO ENUM
                data.IsPhysical = true;

                var jsonMetadata = JsonConvert.SerializeObject(data);

                try
                {
                    Global.Log.LogInformation("Proceed to save the metadata file into scanrequests container");

                    var metadataUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/scanrequests/metadata/{data.OrderNumber}.json");

                    // Get blob client
                    var blobClient = _blobService.GetBlobClient(metadataUri);
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonMetadata)))
                    {
                        await blobClient.UploadAsync(stream, Global.BlobOverwriteExistingFile);
                    }

                    Global.Log.LogInformation("Successfully uploaded metadata file without errors. DocumentId: {DocumentId}", data.OrderNumber);    
                }
                catch (RequestFailedException rEx)
                {
                    if (rEx.ErrorCode.Equals("BlobAlreadyExists"))
                    {
                        Global.Log.LogError(rEx, "File already exists in staging area. Overwrite existing file is set to {OverwriteSetting}.", Global.BlobOverwriteExistingFile);
                        response = req.CreateResponse(HttpStatusCode.Conflict);
                        response.WriteString("Settings dictate that existing files cannot be overwritten and this file already exists in the staging area. Message:" + rEx.Message);
                        return response;
                    }
                    else if (rEx.ErrorCode.Equals("ContainerNotFound"))
                    {
                        Global.Log.LogError(rEx, "The path to staging area ({SourceSystem}) was not found.", "scanrequest");
                        response = req.CreateResponse(HttpStatusCode.FailedDependency);
                        response.WriteString($"ErrorCode: {rEx.ErrorCode}. ErrorMessage: {rEx.Message}");
                        return response;
                    }
                    else
                    {
                        Global.Log.LogError(rEx, "A request failed exception occured. {ErrorMessage}", rEx.Message);
                        response = req.CreateResponse(HttpStatusCode.InternalServerError);
                        response.WriteString($"ErrorCode: {rEx.ErrorCode}. ErrorMessage: {rEx.Message}");
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error message:{ErrorMessage}. StackTrace: {ErrorStackTrace}", ex.Message, ex.StackTrace);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString($"Error Message: {ex.Message}. StackTrace: {ex.StackTrace}");
                    return response;
                }

                response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteString(jsonMetadata);
                return response;
            }
        }
    }
}
