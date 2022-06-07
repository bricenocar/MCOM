using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MCOM.Archiving.Functions;
using MCOM.Models.UnitTesting;
using MCOM.Services;
using MCOM.Utilities;
using MCOM.Tests.Utilities;

namespace MCOM.Tests
{
    public class PostFileTest
    {
        public PostFileTest()
        {
            // Set Environment Variables
            Environment.SetEnvironmentVariable("MandatoryMetadataFields", "fileName,source");
            Environment.SetEnvironmentVariable("BlobStorageAccountName", "test");
        }

        #region Tests

        [Fact]
        public async Task When_The_Request_Works()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var fields = new Dictionary<string, string>()
            {
                { "FileName", "ok" },
                { "Source", "test" },
                // { "documentId", "{078279D5-FB6B-455F-B5D3-0B42AAE045B3}" }
            };

            // Set files to be sent
            var files = new List<FakeFile>()
            {
                new FakeFile
                {
                    Name = "test.txt",
                    Data = File.ReadAllText(@"Files/PostFileTest/ok.txt"),
                    Param = "file",
                    ContentType = "multipart/form-data"
                }
            };

            // Build request
            var request = CreateHttpRequestData(fields, files);

            // Mock configuration
            var mockBlobService = MockConfiguration();

            // Build azure function
            var postFile = new PostFile(mockBlobService.Object);

            // Run function
            var response = await postFile.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();
                        
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("FileName", bodyContent);
            Assert.Contains("Source", bodyContent);
            Assert.Contains("documentId", bodyContent);
        }

        [Fact]
        public async Task When_More_Than_One_File_Is_Sent()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var fields = new Dictionary<string, string>()
            {
                { "FileName", "ok" },
                { "Source", "test" }
            };

            // Set files to be sent
            var files = new List<FakeFile>()
            {
                new FakeFile
                {
                    Name = "test.txt",
                    Data = File.ReadAllText(@"Files/PostFileTest/ok.txt"),
                    Param = "file",
                    ContentType = "multipart/form-data"
                },
                new FakeFile
                {
                    Name = "test2.txt",
                    Data = File.ReadAllText(@"Files/PostFileTest/ok.txt"),
                    Param = "file2",
                    ContentType = "multipart/form-data"
                }
            };

            // Build request            
            var request = CreateHttpRequestData(fields, files);

            // Mock configuration
            var mockBlobService = MockConfiguration();

            // Build azure function
            var postFile = new PostFile(mockBlobService.Object);

            // Run function
            var response = await postFile.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();
                        
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("You can only add 1 file per request with the key: 'File' ", bodyContent);
        }

        [Fact]
        public async Task When_A_File_Is_Sent_But_No_File_Parameter()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var fields = new Dictionary<string, string>()
            {
                { "FileName", "ok" },
                { "Source", "test" }
            };

            // Set files to be sent
            var files = new List<FakeFile>()
            {
                new FakeFile
                {
                    Name = "test.txt",
                    Data = File.ReadAllText(@"Files/PostFileTest/ok.txt"),
                    Param = "error", // Error: Sending wrong file param!
                    ContentType = "multipart/form-data"
                }
            };

            // Build request
            var request = CreateHttpRequestData(fields, files);

            // Mock configuration
            var mockBlobService = MockConfiguration();

            // Build azure function
            var postFile = new PostFile(mockBlobService.Object);

            // Run function
            var response = await postFile.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();
                       
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("You must send at least 1 file per request with the key: 'File'.", bodyContent);
        }

        [Fact]
        public async Task When_A_File_Is_Sent_But_Empty()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var fields = new Dictionary<string, string>()
            {
                { "FileName", "ok" },
                { "Source", "test" }
            };

            // Set files to be sent
            var files = new List<FakeFile>()
            {
                new FakeFile
                {
                    Name = "test.txt",
                    Data = File.ReadAllText(@"Files/PostFileTest/empty.txt"), //ERROR: Empty file    
                    Param = "file",
                    ContentType = "multipart/form-data"
                }
            };

            // Build request
            var request = CreateHttpRequestData(fields, files);

            // Mock configuration
            var mockBlobService = MockConfiguration();

            // Build azure function
            var postFile = new PostFile(mockBlobService.Object);

            // Run function
            var response = await postFile.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();
                        
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("The file you sent has 0 bytes and has no content. Please upload a file that is not empty", bodyContent);
        }

        [Fact]
        public async Task When_A_File_Is_Sent_But_Missing_Required_Params()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var fields = new Dictionary<string, string>()
            {
                { "FileName", "ok" } // ERROR: Missing required params (Source)
            };

            // Set files to be sent
            var files = new List<FakeFile>()
            {
                new FakeFile
                {
                    Name = "test.txt",
                    Data = File.ReadAllText(@"Files/PostFileTest/ok.txt"),
                    Param = "file",
                    ContentType = "multipart/form-data"
                }
            };

            // Build request
            var request = CreateHttpRequestData(fields, files);

            // Mock configuration
            var mockBlobService = MockConfiguration();

            // Build azure function
            var postFile = new PostFile(mockBlobService.Object);

            // Run function
            var response = await postFile.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();
                        
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Missing metadata for [Source]", bodyContent);
        }

        #endregion

        #region Private Methods

        private static Mock<BlobService> MockConfiguration()
        {
            // Mock configuration
            var mockBlobService = new Mock<BlobService>();
            var mockBlobClient = new Mock<BlobClient>(new Uri("https://test.com"), new BlobClientOptions());
            mockBlobService.Setup(b => b.GetBlobClient(It.IsAny<Uri>())).Returns(mockBlobClient.Object);

            return mockBlobService;
        }

        private static HttpRequestData CreateHttpRequestData(Dictionary<string, string> fields, List<FakeFile> files)
        {
            var strBody = StringUtilities.BuildMultiPartForm(fields, files);
            var body = new MemoryStream(Encoding.ASCII.GetBytes(strBody.ToString()));

            // Build request           
            var context = new Mock<FunctionContext>();
            return new FakeHttpRequestData(
                context.Object,
                new Uri("https://test.com"),
                body);
        }

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

        #endregion
    }
}
