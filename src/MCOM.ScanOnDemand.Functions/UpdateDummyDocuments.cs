using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

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
            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                throw;
            }

            Activity.Current?.AddTag("MCOMOperation", "UpdateDummyDocuments");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "UpdateDummyDocuments", "ScanOnDemand"))
            {
                // Get New dummy document from azure storage
                // Init blob service client
                _blobService.GetBlobServiceClient();

                var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/dummytemp/DummyDocument.pdf");

                // Get file from staging area
                var blobCLient = _blobService.GetBlobClient(fileUri);
                var blobContainerClient = _blobService.GetBlobContainerClient(blobCLient.BlobContainerName);
                var dummyDocStream = await _blobService.OpenReadAsync(blobCLient);

                // Try to find dummy files in SharePoint based on field Physical Record                                 
                var sharePointUrl = new Uri(Global.SharePointUrl);
                var accessToken = await _azureService.GetAzureServiceTokenAsync(sharePointUrl);
                using var clientContext = _sharePointService.GetClientContext(Global.SharePointUrl, accessToken.Token);

                // Search
                var resultTable = _sharePointService.SearchItems(clientContext, $"LRMPhysicalRecord:True AND carlos", 1000);
                foreach (var resultRow in resultTable.ResultRows)
                {
                    var searchResult = new Models.Search.SearchResult()
                    {
                        Name = resultRow["Title"].ToString(),
                        SiteId = resultRow["SiteId"].ToString(),
                        WebId = resultRow["WebId"].ToString(),
                        ListId = resultRow["ListID"].ToString(),
                        ListItemId = resultRow["ListItemId"].ToString()
                    };

                    DriveItem currentDriveItem = null;
                    currentDriveItem = await _graphService.ReplaceSharePointFileContentAsync(Global.SharePointDomain,
                        searchResult.SiteId,
                        searchResult.WebId,
                        searchResult.ListId,
                        searchResult.ListItemId,
                        dummyDocStream);

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
}
