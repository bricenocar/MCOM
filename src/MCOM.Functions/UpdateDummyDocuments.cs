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
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MCOM.ScanOnDemand.Functions
{
    public class UpdateDummyDocuments
    {
        private readonly ILogger _logger;
        private IBlobService _blobService;
        private IGraphService _graphService;
        private ISharePointService _sharePointService;
        private IAzureService _azureService;

        public UpdateDummyDocuments(ILoggerFactory loggerFactory, IGraphService graphService, IBlobService blobService, ISharePointService sharePointService, IAzureService azureService)
        {
            _logger = loggerFactory.CreateLogger<UpdateDummyDocuments>();
            _graphService = graphService;
            _blobService = blobService;
            _sharePointService = sharePointService;
            _azureService = azureService;
        }

        [Function("UpdateDummyDocuments")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *")] MyInfo myTimer)
        {
            HtmlToPdf converter;
            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
                GlobalProperties.LicenseKey = Global.SelectPDFLicense;
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
                    using var sr = new StreamReader(dummyDocStream);
                    string content = sr.ReadToEnd();

                    // Try to find dummy files in SharePoint based on field Physical Record                                 
                    var sharePointUrl = new Uri(Global.SharePointUrl);
                    var accessToken = await _azureService.GetAzureServiceTokenAsync(sharePointUrl);
                    using var clientContext = _sharePointService.GetClientContext(Global.SharePointUrl, accessToken.Token);
                    var resultTable = _sharePointService.SearchItems(clientContext, Global.DummyDocumentQuery, Global.DummyDocumentQueryQuantity, new Guid("0c668855-6c88-4f5c-a0ad-b474a7055bbf"));
                    
                    foreach (var resultRow in resultTable.ResultRows)
                    {
                        // Get values from search result
                        var searchResult = new Models.Search.SearchResult()
                        {
                            Name = resultRow["Title"].ToString(),
                            SiteId = resultRow["SiteId"].ToString(),
                            WebId = resultRow["WebId"].ToString(),
                            ListId = resultRow["ListID"].ToString(),
                            ListItemId = resultRow["ListItemId"].ToString(),
                            OriginalPath = resultRow["OriginalPath"].ToString()
                        };

                        // Generate PDF from url
                        HttpClient client = new HttpClient();
                        client.DefaultRequestHeaders.Accept.Clear();

                        var pdfGenerator = new PDFProperties()
                        {
                            Html = content
                        };

                        var stringContent = new StringContent(JsonConvert.SerializeObject(pdfGenerator), Encoding.UTF8, "application/json");

                        var response = await client.PostAsync(Global.GeneratePDFURL, stringContent);

                        using (Stream pdf = await response.Content.ReadAsStreamAsync())
                        {
                            // Replace SharePoint file with new pdf
                            DriveItem currentDriveItem = null;
                            currentDriveItem = await _graphService.ReplaceSharePointFileContentAsync(Global.SharePointDomain,
                                searchResult.SiteId,
                                searchResult.WebId,
                                searchResult.ListId,
                                searchResult.ListItemId,
                                pdf);
                        }
                    }
                }
                catch (AuthenticationFailedException e)
                {
                    Global.Log.LogError(e, $"Authentication Failed. {e.Message}");
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
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error message:{ErrorMessage}. StackTrace: {ErrorStackTrace}", ex.Message, ex.StackTrace);                   
                }                
            }
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
