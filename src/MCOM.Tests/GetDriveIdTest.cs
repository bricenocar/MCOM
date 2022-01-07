using System;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using Xunit;
using MCOM.Functions;
using MCOM.Services;
using MCOM.Tests.Utilities;

namespace MCOM.Tests
{
    public class GetDriveIdTest
    {
        #region Tests

        [Fact]
        public async Task When_The_Function_Runs_Ok()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var parameters = new Dictionary<string, string>()
            {
                { "libraryName", "test" },
                { "siteUrl", "https://test.com" }
            };

            // Build request
            var request = CreateHttpRequestData(parameters);

            // Mock configuration
            var graphService = MockConfiguration("test");

            // Build azure function
            var getDriveId = new GetDriveId(graphService.Object);

            // Run function
            var response = await getDriveId.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("DriveId", bodyContent);
        }

        [Fact]
        public async Task When_The_Function_Is_Missing_QueryString_Params()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var parameters = new Dictionary<string, string>()
            {
                 { "libraryName", "test" } // Missing siteUrl param
            };

            // Build request
            var request = CreateHttpRequestData(parameters);

            // Mock configuration
            var graphService = MockConfiguration("test");

            // Build azure function
            var getDriveId = new GetDriveId(graphService.Object);

            // Run function
            var response = await getDriveId.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Request is missing mandatory parameter siteUrl or libraryName", bodyContent);
        }

        [Fact]
        public async Task When_The_Function_Cannot_Find_Library_Name()
        {
            // Function parameters
            var mockFunctionContext = GetFunctionParams();

            // Set fields to be sent in the request
            var parameters = new Dictionary<string, string>()
            {
                { "libraryName", "test" },
                { "siteUrl", "https://test.com" }
            };

            // Build request
            var request = CreateHttpRequestData(parameters);

            // Mock configuration
            var graphService = MockConfiguration("error");

            // Build azure function
            var getDriveId = new GetDriveId(graphService.Object);

            // Run function
            var response = await getDriveId.Run(request, mockFunctionContext.Object);

            // Get body content
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(response.Body).ReadToEndAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal($"Library not found at {parameters["libraryName"]}", bodyContent);
        }

        #endregion

        #region Private Methods

        private static Mock<GraphService> MockConfiguration(string name)
        {
            // Mock configuration
            var mockGraphService = new Mock<GraphService>();
            var mockGraphServiceClient = new Mock<GraphServiceClient>(new HttpClient(), "https://test.com");
            var siteDrivesCollectionPage = new SiteDrivesCollectionPage()
            {
                new Drive()
                {
                    Name = name
                }
            };

            mockGraphService.SetupAllProperties();
            mockGraphService.Object.GraphServiceClient = mockGraphServiceClient.Object;
            mockGraphService.Setup(g => g.GetDriveCollectionPageAsync(It.IsAny<Uri>())).ReturnsAsync(siteDrivesCollectionPage);

            return mockGraphService;
        }

        private static HttpRequestData CreateHttpRequestData(Dictionary<string, string> parameters)
        {
            var url = "https://test.com?";

            foreach (var param in parameters)
            {
                url += $"{param.Key}={param.Value}&";
            }

            // Build request
            var body = new MemoryStream(Encoding.ASCII.GetBytes("{ \"test\": true }"));
            var context = new Mock<FunctionContext>();
            return new FakeHttpRequestData(
                context.Object,
                new Uri(url),
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
