using Azure;
using Azure.Identity;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using SelectPdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MCOM.ScanOnDemand.Functions
{
    public class UpdateDummyDocuments
    {
        //private readonly ILogger _logger;
        private IBlobService _blobService;
        private IGraphService _graphService;
        private ISharePointService _sharePointService;
        private IAzureService _azureService;

        public UpdateDummyDocuments(ILoggerFactory loggerFactory, IGraphService graphService, IBlobService blobService, ISharePointService sharePointService, IAzureService azureService)
        {
            //_logger = loggerFactory.CreateLogger<UpdateDummyDocuments>();
            _graphService = graphService;
            _blobService = blobService;
            _sharePointService = sharePointService;
            _azureService = azureService;
        }

        [Function("UpdateDummyDocuments")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *")] MyInfo myTimer, FunctionContext context)
        {
            var logger = context.GetLogger("UpdateDummyDocuments");
            HtmlToPdf converter;

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
                GlobalProperties.LicenseKey = Global.SelectPDFLicense;// pones esta: 9t3H1sTDx9bPxc/WxMTYxtbFx9jHxNjPz8/P
                converter = new HtmlToPdf();
                converter.Options.WebPageWidth = 1024;
                converter.Options.WebPageHeight = 0;
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                throw;
            }

            Activity.Current?.AddTag("MCOMOperation", "UpdateDummyDocuments");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "UpdateDummyDocuments", "ScanOnDemand"))
            {
                try
                {
                    // Get New dummy document from azure storage
                    // Init blob service client                    
                    _blobService.GetBlobServiceClient();

                    var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/dummytemp/DummyDocument.html");

                    // Get file from staging area
                    var blobCLient = _blobService.GetBlobClient(fileUri);
                    var dummyDocStream = await _blobService.OpenReadAsync(blobCLient);

                    // Read dummy document content
                    var content = string.Empty;
                    using var sr = new StreamReader(dummyDocStream);
                    if (sr != null)
                    {
                        content = sr.ReadToEnd();
                    }
                    else
                    {
                        throw new Exception("Could not find dummy document template in storage account");
                    }
                   
                    // Try to find dummy files in SharePoint based on field Physical Record
                    // // Query: if Is Physical Record is True and Physical Record Status is Not Scanned and Extension of the file is pdf..we are doing replacement in SPO
                    var props = Global.DummyDocumentProperties.Split(',');
                    var sharePointUrl = new Uri(Global.SharePointUrl);
                    var accessToken = await _azureService.GetAzureServiceTokenAsync(sharePointUrl);
                    using var clientContext = _sharePointService.GetClientContext(Global.SharePointUrl, accessToken.Token);
                    var resultTable = _sharePointService.SearchItems(clientContext, Global.DummyDocumentQuery, props, Global.DummyDocumentQueryQuantity, new Guid(Global.DummyDocumentSearchResultId));
                    Global.Log.LogInformation($"Found: {resultTable.ResultRows.Count()} in result table. Query executed: {Global.DummyDocumentQuery}");
                    foreach (var resultRow in resultTable.ResultRows)
                    {
                        Models.Search.SearchResult searchResult = null;
                        try
                        {
                            Global.Log.LogInformation($"Validating field: LRMPhysicalRecordOWSBOOL");
                            if (!resultRow.TryGetValue("LRMPhysicalRecordOWSBOOL", out var physicalRecord))
                            {
                                resultRow.TryGetValue("LRMIsPhysicalRecordOWSBOOL", out physicalRecord);
                            }

                            if (!resultRow.TryGetValue("LRMPhysicalRecordStatusOWSTEXT", out var physicalRecordStatus))
                            {
                                resultRow.TryGetValue("LRMPhysicalRecordStatusOWSTEXT", out physicalRecordStatus);
                            }

                            // Get values from search result
                            searchResult = new Models.Search.SearchResult()
                            {
                                Name = resultRow["Title"] == null ? "" : resultRow["Title"].ToString(),
                                SiteId = resultRow["SiteId"] == null ? "" : resultRow["SiteId"].ToString(),
                                WebId = resultRow["WebId"] == null ? "" : resultRow["WebId"].ToString(),
                                ListId = resultRow["ListID"] == null ? "" : resultRow["ListID"].ToString(),
                                ListItemId = resultRow["ListItemId"] == null ? "" : resultRow["ListItemId"].ToString(),
                                OriginalPath = resultRow["OriginalPath"] == null ? "" : resultRow["OriginalPath"].ToString(),
                                SPWebUrl = resultRow["SPWebUrl"] == null ? "" : resultRow["SPWebUrl"].ToString(),
                                PhysicalRecord = physicalRecord == null ? false : physicalRecord.Equals("1"), // resultRow["LRMPhysicalRecordOWSBOOL"].ToString().Equals("1") ? true : false,
                                PhysicalRecordStatus = physicalRecordStatus == null ? "" : physicalRecordStatus.ToString(),
                                FileExtension = resultRow["FileExtension"] == null ? "" : resultRow["FileExtension"].ToString(),
                            };

                            Global.Log.LogInformation($"Processing document: {searchResult.Name} with Id: {searchResult.ListItemId}");

                            // Generate PDF from url
                            var client = new HttpClient();

                            // Clear accept header to avoid validation issues
                            client.DefaultRequestHeaders.Accept.Clear();

                            Global.Log.LogInformation($"Preparing PDF generator");
                            var pdfGenerator = new PDFProperties()
                            {
                                Html = GetUrl(content, searchResult.SPWebUrl, searchResult.Name, searchResult.SiteId, searchResult.WebId, searchResult.ListId, searchResult.ListItemId)
                            };
                            var stringContent = new StringContent(JsonConvert.SerializeObject(pdfGenerator), Encoding.UTF8, "application/json");

                            //// Send post to build PDF
                            Global.Log.LogInformation($"Generating PDF");
                            var response = await client.PostAsync(Global.GeneratePDFURL, stringContent);
                            using var pdf = await response.Content.ReadAsStreamAsync();
                            Global.Log.LogInformation($"PDF generated");

                            // Check if the file has a retention label applied
                            Global.Log.LogInformation($"Validating retention label");
                            bool retentionLabelValidated = false;
                            using (var siteContext = _sharePointService.GetClientContext(searchResult.SPWebUrl, accessToken.Token))
                            {
                                retentionLabelValidated = _sharePointService.ValidateItemRetentionLabel(siteContext, searchResult.ListId, searchResult.ListItemId);
                            };

                            if(retentionLabelValidated)
                            {
                                // Replace SharePoint file with new pdf
                                var currentDriveItem = await _graphService.ReplaceSharePointFileContentAsync(Global.SharePointDomain,
                                    searchResult.SiteId,
                                    searchResult.WebId,
                                    searchResult.ListId,
                                    searchResult.ListItemId,
                                    pdf);

                                //// Update item with status scanned
                                if (currentDriveItem != null)
                                {
                                    var fileMetadata = new Dictionary<string, object>
                                {
                                    { "LRMDummyUpdated", true }
                                };
                                    await _graphService.SetMetadataByGraphAsync(fileMetadata, searchResult.SiteId, searchResult.ListId, searchResult.ListItemId);

                                    Global.Log.LogInformation($"Setting metadata 'LRMDummyUpdated' eq true");
                                }
                            }                            
                        }
                        catch (Exception ex)
                        {
                            Global.Log.LogError(ex, "Error when updating document" + searchResult == null ? "" : searchResult.Name + "Failed. {ex}");
                        }
                    }
                }
                catch (AuthenticationFailedException e)
                {
                    Global.Log.LogError(e, $"Authentication Failed. {e.Message}");
                    throw;
                }
                catch (RequestFailedException rEx)
                {
                    if (rEx.ErrorCode.Equals("BlobAlreadyExists"))
                    {
                        Global.Log.LogError(rEx, "File already exists in staging area. Overwrite existing file is set to {OverwriteSetting}.", Global.BlobOverwriteExistingFile);
                    }
                    else if (rEx.ErrorCode.Equals("ContainerNotFound"))
                    {
                        Global.Log.LogError(rEx, "The path to staging area ({SourceSystem}) was not found.", "scanrequest");
                    }
                    else
                    {
                        Global.Log.LogError(rEx, "A request failed exception occured. {ErrorMessage}", rEx.Message);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error message:{ErrorMessage}. StackTrace: {ErrorStackTrace}", ex.Message, ex.StackTrace);
                    throw;
                }
            }
        }

        private string GetUrl(string content, string sitePath, string name, string siteId, string webId, string listId, string listItemId)
        {
            return content.Replace("{{placeholder}}", string.Format("{0}/SitePages/ScanOnDemand.aspx?iid={1}&name={2}&sid={3}&wid={4}&lid={5}", sitePath, listItemId, name, siteId, webId, listId));
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
    internal class PDFProperties
    {
        public string? Url { get; set; }
        public string? Html { get; set; }
        public string? Base_url { get; set; }
        public string? Page_size { get; set; }
        public string? Page_orientation { get; set; }
        public string? Web_page_width { get; set; }
        public string? Web_page_height { get; set; }
    }
}
