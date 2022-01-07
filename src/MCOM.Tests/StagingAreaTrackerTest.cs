using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using Moq;
using Xunit;
using MCOM.Archiving.Functions;
using MCOM.Models;
using MCOM.Services;

namespace MCOM.Tests
{
    public class StagingAreaTrackerTest
    {
        #region Tests

        [Fact]
        public async Task When_The_Request_Runs_Ok()
        {
            // Function parameters
            var (mockFunctionContext, timerInfo) = GetFunctionParams();

            // Input values
            var json = "{\"driveid\":\"test\", \"documentId\":\"testId\", \"Source\":\"test\", \"documentIdField\":\"testField\", \"testField\":\"testData\", \"TempFileLocation\":\"test/test/test\"}";

            var drive = new Drive()
            {
                WebUrl = "https://test.com",
                SharePointIds = new SharepointIds()
                {
                    ListId = "{5BA7AEA7-1E2A-42AB-9DEA-2759BFD3F0B5}"
                }
            };

            // Generate stream file mocking the file coming from SharePoint
            using FileStream stream = System.IO.File.OpenWrite(@"Files/StagingAreaTrackerTest/small.txt");

            // Mock configuration
            var (mockBlobService, mockGraphService, mockSharePointService, mockAzureService) = MockConfiguration(json, drive, 1, stream);

            // Build azure function
            var stagingAreaTracker = new StagingAreaTracker(mockGraphService.Object, mockBlobService.Object, mockSharePointService.Object, mockAzureService.Object);

            // Run function
            await stagingAreaTracker.RunAsync(timerInfo, mockFunctionContext.Object);

            // Get error message
            var firstLog = stagingAreaTracker._logList.FirstOrDefault();
            var lastLog = stagingAreaTracker._logList.LastOrDefault();

            Assert.Equal(LogLevel.Information, firstLog.LogLevel);
            Assert.Equal("The file testId has been deleted successfully from staging area (output)", firstLog.Message);
            Assert.Equal(LogLevel.Information, lastLog.LogLevel);
            Assert.Equal("The file  has been deleted successfully from container (files)", lastLog.Message);
        }

        [Fact]
        public async Task When_The_SharePoint_ListItem_Is_Null_But_Runs_Ok()
        {
            // Function parameters
            var (mockFunctionContext, timerInfo) = GetFunctionParams();

            // Input values
            var json = "{\"driveid\":\"test\", \"documentId\":\"testId\", \"Source\":\"test\", \"documentIdField\":\"testField\", \"testField\":\"testData\", \"TempFileLocation\":\"test/test/test\"}";

            var drive = new Drive()
            {
                WebUrl = "https://test.com",
                SharePointIds = new SharepointIds()
                {
                    ListId = "{5BA7AEA7-1E2A-42AB-9DEA-2759BFD3F0B5}"
                }
            };

            // Generate stream file mocking the file coming from SharePoint
            using FileStream stream = System.IO.File.OpenWrite(@"Files/StagingAreaTrackerTest/small.txt");

            // Mock configuration
            var (mockBlobService, mockGraphService, mockSharePointService, mockAzureService) = MockConfiguration(json, drive, 0, stream);

            // Build azure function
            var stagingAreaTracker = new StagingAreaTracker(mockGraphService.Object, mockBlobService.Object, mockSharePointService.Object, mockAzureService.Object);

            // Run function
            await stagingAreaTracker.RunAsync(timerInfo, mockFunctionContext.Object);

            // Get error message
            var firstLog = stagingAreaTracker._logList.First();
            var secondLog = stagingAreaTracker._logList.ElementAt(1);
            var thirdLog = stagingAreaTracker._logList.ElementAt(2);
            var lastLog = stagingAreaTracker._logList.Last();

            Assert.Equal(LogLevel.Warning, firstLog.LogLevel);
            Assert.Equal("The ListItem for blob testId does not exist in SharePoint.", firstLog.Message);
            Assert.Equal(LogLevel.Information, secondLog.LogLevel);
            Assert.Equal("The file testId has been uploaded successfully (UploadSmallFile). Deleting from staging area output and files", secondLog.Message);
            Assert.Equal(LogLevel.Information, thirdLog.LogLevel);
            Assert.Equal("The file testId has been deleted successfully from staging area (output)", thirdLog.Message);
            Assert.Equal(LogLevel.Information, lastLog.LogLevel);
            Assert.Equal("The file  has been deleted successfully from container (files)", lastLog.Message);
        }

        [Fact]
        public async Task When_The_Request_Is_Missing_Source_Param()
        {
            // Function parameters
            var (mockFunctionContext, timerInfo) = GetFunctionParams();

            // Input values (MISSING SOURCE)
            var json = "{\"driveid\":\"test\", \"documentId\":\"testId\", \"documentIdField\":\"testField\", \"testField\":\"testData\", \"TempFileLocation\":\"test/test/test\"}";

            var drive = new Drive()
            {
                WebUrl = "https://test.com",
                SharePointIds = new SharepointIds()
                {
                    ListId = "{5BA7AEA7-1E2A-42AB-9DEA-2759BFD3F0B5}"
                }
            };

            // Generate stream file mocking the file coming from SharePoint
            using FileStream stream = System.IO.File.OpenWrite(@"Files/StagingAreaTrackerTest/small.txt");

            // Mock configuration
            var (mockBlobService, mockGraphService, mockSharePointService, mockAzureService) = MockConfiguration(json, drive, 1, stream);

            // Build azure function
            var stagingAreaTracker = new StagingAreaTracker(mockGraphService.Object, mockBlobService.Object, mockSharePointService.Object, mockAzureService.Object);

            // Run function
            await stagingAreaTracker.RunAsync(timerInfo, mockFunctionContext.Object);

            // Get error message
            var log = stagingAreaTracker._logList.FirstOrDefault();

            Assert.Equal(LogLevel.Critical, log.LogLevel);
            Assert.Equal("The blob 'test' is missing driveId 'test' or DocumentId 'testId' or source ''", log.Message);
        }

        [Fact]
        public async Task When_Drive_Is_Null()
        {
            // Function parameters
            var (mockFunctionContext, timerInfo) = GetFunctionParams();

            // Input values
            var json = "{\"driveid\":\"test\", \"documentId\":\"testId\", \"Source\":\"test\", \"documentIdField\":\"testField\", \"testField\":\"testData\", \"TempFileLocation\":\"test/test/test\"}";

            Drive drive = null;

            // Generate stream file mocking the file coming from SharePoint
            using FileStream stream = System.IO.File.OpenWrite(@"Files/StagingAreaTrackerTest/small.txt");

            // Mock configuration
            var (mockBlobService, mockGraphService, mockSharePointService, mockAzureService) = MockConfiguration(json, drive, 1, stream);

            // Build azure function
            var stagingAreaTracker = new StagingAreaTracker(mockGraphService.Object, mockBlobService.Object, mockSharePointService.Object, mockAzureService.Object);

            // Run function
            await stagingAreaTracker.RunAsync(timerInfo, mockFunctionContext.Object);

            // Get error message
            var log = stagingAreaTracker._logList.FirstOrDefault();

            Assert.Equal(LogLevel.Error, log.LogLevel);
            Assert.Equal("Could not get drive with specified ID. DocumentId: testId", log.Message);
        }        

        #endregion

        #region Private methods

        private static (Mock<BlobService>, Mock<GraphService>, Mock<SharePointService>, Mock<AzureService>) MockConfiguration(string json, Drive drive, int itemCount, Stream stream)
        {
            // Mock configuration
            var mockSharePointService = new Mock<SharePointService>();
            var mockAzureService = new Mock<AzureService>();
            var mockGraphService = new Mock<GraphService>();
            var mockGraphServiceClient = new Mock<GraphServiceClient>(new HttpClient(), "https://test.com");
            var mockBlobServiceClient = new Mock<BlobServiceClient>("UseDevelopmentStorage=true");
            var mockBlobService = new Mock<BlobService>();
            var mockBlobClient = new Mock<BlobClient>(new Uri("https://test.com"), new BlobClientOptions());
            var blobContainerClientMock = new Mock<BlobContainerClient>();

            // Properties
            var page = Page<BlobItem>.FromValues(new[]
                {
                    BlobsModelFactory.BlobItem("test")
                }, null, Mock.Of<Response>()
            );

            // BLob mocking
            var pageable = AsyncPageable<BlobItem>.FromPages(new[] { page });
            mockBlobService.SetupAllProperties();
            mockBlobServiceClient.SetupAllProperties();
            mockBlobService.Object.BlobServiceClient = mockBlobServiceClient.Object;
            mockBlobService.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(blobContainerClientMock.Object);
            mockBlobService.Setup(b => b.GetBlobs(It.IsAny<BlobContainerClient>())).Returns(pageable);
            mockBlobService.SetupSequence(b => b.GetBlobClient(It.IsAny<BlobContainerClient>(), It.IsAny<string>()))
                .Returns(mockBlobClient.Object)
                .Returns(mockBlobClient.Object);
            mockBlobService.Setup(b => b.GetBlobStreamAsync(It.IsAny<BlobClient>())).ReturnsAsync(stream);
            mockBlobService.Setup(b => b.BlobClientExistsAsync(It.IsAny<BlobClient>())).ReturnsAsync(true);
            mockBlobService.Setup(b => b.GetBlobDataAsync(It.IsAny<BlobClient>())).ReturnsAsync(json);
            mockBlobService.SetupSequence(b => b.DeleteBlobClientIfExistsAsync(It.IsAny<BlobClient>()))
                .ReturnsAsync(true)
                .ReturnsAsync(true);

            var driveItem = new DriveItem();

            // Graph mocking
            mockGraphService.SetupAllProperties();
            mockGraphService.Object.GraphServiceClient = mockGraphServiceClient.Object;
            mockGraphService.Setup(g => g.GetDriveAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(drive);
            mockGraphService.Setup(g => g.UploadDriveItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>())).ReturnsAsync(driveItem);


            var accessToken = new AccessToken();

            // Azure mocking
            mockAzureService.SetupAllProperties();
            mockAzureService.Setup(s => s.GetAzureServiceTokenAsync(It.IsAny<Uri>())).ReturnsAsync(accessToken);

            var mockClientContext = new Mock<ClientContext>(new Uri("https://test.com"));
            ObjectPath objectPath = null;
            var mockObjectPath = new Mock<ObjectPath>();
            var mockList = new Mock<Microsoft.SharePoint.Client.List>(mockClientContext.Object, objectPath);
            var mockListItemCollection = new Mock<ListItemCollection>(mockClientContext.Object, objectPath);
            var listItemCollection = new List<Microsoft.SharePoint.Client.ListItem>();

            // Add list items to collection
            for (int i = 0; i < itemCount; i++)
            {
                listItemCollection.Add(new Microsoft.SharePoint.Client.ListItem(mockClientContext.Object, objectPath));
            }

            // SharePoint mocking
            mockSharePointService.SetupAllProperties();
            mockSharePointService.Setup(s => s.GetClientContext(It.IsAny<string>(), It.IsAny<string>())).Returns(mockClientContext.Object);
            mockSharePointService.Setup(s => s.GetListById(It.IsAny<ClientContext>(), It.IsAny<Guid>())).Returns(mockList.Object);
            mockSharePointService.Setup(s => s.GetListItems(It.IsAny<ClientContext>(), It.IsAny<Microsoft.SharePoint.Client.List>(), It.IsAny<CamlQuery>())).Returns(mockListItemCollection.Object);
            mockSharePointService.Setup(s => s.GetListAsGenericList(It.IsAny<ListItemCollection>())).Returns(listItemCollection);

            return (mockBlobService, mockGraphService, mockSharePointService, mockAzureService);
        }

        private static (Mock<FunctionContext>, TimerInfo) GetFunctionParams()
        {
            // Mock function parameters
            var timerInfo = default(TimerInfo);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var mockFunctionContext = new Mock<FunctionContext>();
            mockFunctionContext.SetupProperty(c => c.InstanceServices, serviceProvider);

            return (mockFunctionContext, timerInfo);
        }

        #endregion
    }
}
