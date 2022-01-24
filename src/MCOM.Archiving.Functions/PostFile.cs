using Azure;
using Azure.Storage.Blobs;
using HttpMultipartParser;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MCOM.Archiving.Functions
{
    public class PostFile
    {
        private readonly IBlobService _blobService;

        public PostFile(IBlobService blobService)
        {
            _blobService = blobService;
        }

        [Function("PostFile")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("PostFile");

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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "PostFile");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "PostFile", "Archiving"))
            {
                FilePart file;
                HttpResponseData response = null;
                IReadOnlyList<ParameterPart> formdata;
                try
                {
                    var parsedFormBody = await MultipartFormDataParser.ParseAsync(req.Body);

                    // Read file metadata from request
                    formdata = parsedFormBody.Parameters;

                    // Validate if file exists in request          
                    if (parsedFormBody.Files.Count > 1)
                    {
                        Global.Log.LogError(e, "Too many files ({FileCount}) included in the POST request", parsedFormBody.Files.Count);
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString("You can only add 1 file per request with the key: 'File' ");
                        return response;
                    }

                    // Get file from request
                    file = parsedFormBody.Files.FirstOrDefault(f => string.Equals(f.Name, "file", StringComparison.OrdinalIgnoreCase));

                    // Validate if file is null
                    if (file == null)
                    {
                        Global.Log.LogError(e, "File not included");
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString("You must send at least 1 file per request with the key: 'File'.");
                        return response;
                    }

                    using var fileData = new MemoryStream();
                    await file.Data.CopyToAsync(fileData);
                    fileData.Position = 0;

                    var fileContent = new StringBuilder();
                    using (var reader = new StreamReader(fileData))
                    {
                        while (reader.Peek() >= 0)
                            fileContent.AppendLine(await reader.ReadLineAsync());
                    }

                    // Validate if file is empty
                    if (fileContent.Length == 0)
                    {
                        Global.Log.LogError(e, "File has no content");
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString("The file you sent has 0 bytes and has no content. Please upload a file that is not empty");
                        return response;
                    }
                }
                catch (Exception e)
                {
                    Global.Log.LogError(e, "Error getting file. Errormessage: {ErrorMessage}", e.Message);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(e.Message);
                    return response;
                }

                // Collect metadata from multipart request
                var metadata = new PostFileData<string, string>();

                // Check in case documentId already exists
                var tempDocumentId = formdata.FirstOrDefault(f => f.Name.Equals("documentid", StringComparison.OrdinalIgnoreCase));
                var documentId = tempDocumentId != null ?
                    new Guid(tempDocumentId.Data) :
                    Guid.NewGuid();

                try
                {
                    // Add metadata values from form data
                    foreach (var keyValuePair in formdata)
                    {
                        if (keyValuePair.Name.Equals("Filename", StringComparison.InvariantCultureIgnoreCase))
                        {
                            metadata.FileName = keyValuePair.Data;
                        }
                        metadata.Add(keyValuePair.Name, keyValuePair.Data);
                    }

                    // Generate a unique ID to return to client
                    if (tempDocumentId == null)
                    {
                        metadata.Add("documentId", documentId.ToString());
                    }

                    Global.Log.LogInformation("Id generated for file: {DocumentId}", documentId.ToString());
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(e, "An error has ocurred when trying to extract properties from file: {DocumentId}. Error message:{ErrorMessage}. StackTrace: {ErrorStackTrace}", documentId.ToString(), ex.Message, ex.StackTrace);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString("Invalid form data. Check that the content type is multipart/form-data");
                    return response;
                }

                // Validate input fields (mandatory)
                try
                {
                    if (!metadata.ValidateInput())
                    {
                        Global.Log.LogError(e, "Mandatory metadata is missing ({missingMetadata}) for DocumentId: {DocumentId}", metadata.MissingMetadata, documentId.ToString());
                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.WriteString($"Missing metadata for [{metadata.MissingMetadata}]");
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(e, "An error has ocurred when trying to validate properties from DocumentId: {DocumentId}. Error message:{ErrorMessage}. StackTrace: {ErrorStackTrace}", documentId.ToString(), ex.Message, ex.StackTrace);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString("Missing mandatory metadata.");
                    return response;
                }

                // Tag the request log (first log event) with the Source system
                System.Diagnostics.Activity.Current?.AddTag("SourceSystem", metadata.SourceSystem);

                var jsonMetadata = JsonConvert.SerializeObject(metadata);

                Global.Log.LogInformation("Metadata received from user: {RequestBody} for DocumentId: {DocumentId}", jsonMetadata, documentId.ToString());

                var options = new BlobClientOptions();
                options.Diagnostics.IsLoggingEnabled = Global.BlobIsLoggingEnabled;
                options.Diagnostics.IsTelemetryEnabled = Global.BlobIsTelemetryEnabled;
                options.Diagnostics.IsDistributedTracingEnabled = Global.BlobIsDistributedTracingEnabled;
                options.Retry.MaxRetries = Global.BlobMaxRetries;

                try
                {
                    var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{metadata.SourceSystem}/files/{documentId}");
                    var metadataUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{metadata.SourceSystem}/metadata/{documentId}.json");

                    // Get blob client
                    var blobClient = _blobService.GetBlobClient(metadataUri);
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonMetadata)))
                    {
                        await blobClient.UploadAsync(stream, Global.BlobOverwriteExistingFile);
                    }

                    Global.Log.LogInformation("Successfully uploaded metadata file without errors. DocumentId: {DocumentId}", documentId.ToString());

                    // Get blob client by overwriting the last blob client
                    blobClient = _blobService.GetBlobClient(fileUri);

                    // Copy file stream
                    using (var stream = file.Data)
                    {
                        await blobClient.UploadAsync(stream, Global.BlobOverwriteExistingFile);
                    }

                    Global.Log.LogInformation("Successfully uploaded content file without errors. DocumentId: {DocumentId}", documentId.ToString());
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
                        Global.Log.LogError(rEx, "The path to staging area ({SourceSystem}) was not found.", metadata.SourceSystem);
                        response = req.CreateResponse(HttpStatusCode.FailedDependency);
                        response.WriteString($"ErrorCode: {rEx.ErrorCode}. ErrorMessage: {rEx.Message}");
                        return response;
                    }
                    else
                    {
                        Global.Log.LogError(, "A request failed exception occured. {ErrorMessage}", rEx.Message);
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

/**
 * Reference Documentation
 * https://codingcanvas.com/different-ways-of-uploading-files-using-http-based-apis-part-3/ 
 * https://blog.rasmustc.com/multipart-data-with-azure-functions-httptriggers/
 */

