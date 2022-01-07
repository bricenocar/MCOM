using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using Xunit;
using Newtonsoft.Json;
using MCOM.Functions;
using MCOM.Models;
using MCOM.Services;

namespace MCOM.Tests
{
    public class ArchiveFileTest
    {
        public ArchiveFileTest()
        {
            // Set Environment Variables
            Environment.SetEnvironmentVariable("MandatoryMetadataFields", "fileName,source");
            Environment.SetEnvironmentVariable("BlobStorageAccountName", "test");
        }

        #region Tests

        [Theory]
        [InlineData(@"Files/ArchiveFileTest/ok_small.txt")]
        [InlineData(@"Files/ArchiveFileTest/ok_large.txt")]
        public async Task When_The_Function_Runs_Ok(string filePath)
        {
            // Get function params
            var mockFunctionContext = GetFunctionParams();

            // Mock configuration
            var (mockBlobService, mockGraphService) = MockConfiguration(filePath);

            // Function property
            var data = await System.IO.File.ReadAllTextAsync(@"Files/ArchiveFileTest/ok.json");

            // Build azure function
            var archiveFile = new ArchiveFile(mockGraphService.Object, mockBlobService.Object);

            // Run function
            var feedBackItem = await archiveFile.Run(data, "test.json", mockFunctionContext.Object);

            // Build test item to compare against function response
            var testFeedBackItem = new QueueItem()
            {
                ResponseUrl = "https://test.com",
                Item = new FeedbackItem() { DocumentId = "test", DriveId = "test" }
            };

            // Convert to Json to compare objects
            var testFeedBackItemJson = JsonConvert.SerializeObject(testFeedBackItem);
            var feedBackItemJson = JsonConvert.SerializeObject(feedBackItem);

            // Asserts
            Assert.True(testFeedBackItemJson.Equals(feedBackItemJson));
        }

        [Fact]
        public async Task When_The_File_Format_Is_Not_Json()
        {
            // Get function params
            var mockFunctionContext = GetFunctionParams();

            // Mock configuration
            var (mockBlobService, mockGraphService) = MockConfiguration(@"Files/ArchiveFileTest/ok_small.txt");

            // Function property
            var data = await System.IO.File.ReadAllTextAsync(@"Files/ArchiveFileTest/ok.json");

            // Build azure function
            var archiveFile = new ArchiveFile(mockGraphService.Object, mockBlobService.Object);

            // Run function
            var feedBackItem = await archiveFile.Run(data, "test.txt", mockFunctionContext.Object); // Sending wrong format (txt)!

            // Asserts
            Assert.Null(feedBackItem);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get function params. These are just default to run the Azure Function
        /// </summary>
        /// <returns></returns>
        private static Mock<FunctionContext> GetFunctionParams()
        {
            // Mock function parameters
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var mockFunctionContext = new Mock<FunctionContext>();
            mockFunctionContext.SetupProperty(c => c.InstanceServices, serviceProvider);

            return mockFunctionContext;
        }

        private static (Mock<BlobService>, Mock<GraphService>) MockConfiguration(string filePath)
        {
            // Mock configuration
            var mockBlobService = new Mock<BlobService>();
            var mockBlobClient = new Mock<BlobClient>(new Uri("https://test.com"), new BlobClientOptions());
            var mockBlobServiceClient = new Mock<BlobServiceClient>("UseDevelopmentStorage=true");
            var stream = System.IO.File.OpenRead(filePath);
            mockBlobService.SetupAllProperties();
            mockBlobService.Object.BlobServiceClient = mockBlobServiceClient.Object;
            mockBlobService.Setup(b => b.GetBlobClient(It.IsAny<Uri>())).Returns(mockBlobClient.Object);
            mockBlobService.Setup(b => b.OpenReadAsync(It.IsAny<BlobClient>())).ReturnsAsync(stream);

            var mockGraphService = new Mock<GraphService>();
            var mockGraphServiceClient = new Mock<GraphServiceClient>(new HttpClient(), "https://test.com");
            mockGraphService.SetupAllProperties();
            mockGraphService.Object.GraphServiceClient = mockGraphServiceClient.Object;

            var driveItem = new DriveItem();
            var result = new UploadResult<DriveItem>()
            {
                ItemResponse = driveItem
            };
            
            mockGraphService.Setup(g => g.UploadDriveItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>())).ReturnsAsync(driveItem);
            mockGraphService.Setup(g => g.SetMetadataAsync(It.IsAny<ArchiveFileData<string, object>>(), It.IsAny<DriveItem>()));
            mockGraphService.Setup(g => g.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(result);

            return (mockBlobService, mockGraphService);
        }

        #endregion
    }
}
