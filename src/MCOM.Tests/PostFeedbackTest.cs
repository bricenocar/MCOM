using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using MCOM.Models.Azure;
using MCOM.Archiving.Functions;
using MCOM.Services;
using MCOM.Business.PostFeedBack;

namespace MCOM.Tests
{
    public class PostFeedbackTest
    {
        #region Tests

        [Fact]
        public async Task When_The_Function_Runs_Ok()
        {
            // Mock return values
            var queueItem = JsonConvert.SerializeObject(new QueueItem()
            {
                ClientUrl = "https://test.com",
                Content = new FeedbackItem() { DocumentId = $"DocumentId", DriveId = $"DriveId" },
                Source = "test"
            });
            var response = new HttpResponseMessage()
            {
                Content = new StringContent("Test response"),
                StatusCode = System.Net.HttpStatusCode.OK
            };

            // Mock function parameters
            var mockFunctionContext = GetFunctionParams();

            // Getting mock config variables
            var (mockQueueService, mockBusinessService) = MockConfiguration(response);

            // Build azure function
            var postFeedback = new PostFeedback(mockQueueService.Object, mockBusinessService.Object);

            // Run function
            await postFeedback.RunAsync(queueItem, mockFunctionContext.Object);
        }

        [Fact]
        public async Task When_The_QueItem_Is_Not_Well_Formed()
        {
            try
            {
                // Mock return values
                var queueItem = JsonConvert.SerializeObject(new // WE ARE SENDING HERE A WRONG OBJECT
                {
                    Test = "https://test.com",
                    Testing = "Test"
                });
                var response = new HttpResponseMessage()
                {
                    Content = new StringContent("Test response"),
                    StatusCode = System.Net.HttpStatusCode.OK
                };

                // Mock function parameters
                var mockFunctionContext = GetFunctionParams();

                // Getting mock config variables
                var (mockQueueService, mockBusinessService) = MockConfiguration(response);

                // Build azure function
                var postFeedback = new PostFeedback(mockQueueService.Object, mockBusinessService.Object);

                // Run function
                await postFeedback.RunAsync(queueItem, mockFunctionContext.Object);
            }
            catch (Exception ex)
            {
                Assert.Equal("Error when deserializing the object into a QueueItem", ex.Message);
            }
        }

        [Fact]
        public async Task When_The_QueItem_Value_Is_Null()
        {
            try
            {
                // Mock return values
                var queueItem = JsonConvert.SerializeObject(new QueueItem() // WE ARE SENDING HERE A WRONG OBJECT
                {
                    ClientUrl = "https://test.com",
                    Content = new FeedbackItem() { DocumentId = "00000000-0000-0000-0000-000000000000", DriveId = "00000000-0000-0000-0000-000000000000" },
                    Source = "test"
                });
                var response = new HttpResponseMessage()
                {
                    Content = new StringContent("Test response"),
                    StatusCode = System.Net.HttpStatusCode.OK
                };

                // Mock function parameters
                var mockFunctionContext = GetFunctionParams();

                // Getting mock config variables
                var (mockQueueService, mockBusinessService) = MockConfiguration(response);

                // Build azure function
                var postFeedback = new PostFeedback(mockQueueService.Object, mockBusinessService.Object);

                // Run function
                await postFeedback.RunAsync(queueItem, mockFunctionContext.Object);
            }
            catch (Exception ex)
            {
                Assert.Equal("Got null guid", ex.Message);
            }
        }

        [Fact]
        public async Task When_The_Http_Response_Is_Not_Ok()
        {
            try
            {
                // Mock return values
                var queueItem = JsonConvert.SerializeObject(new QueueItem()
                {
                    ClientUrl = "https://test.com",
                    Content = new FeedbackItem() { DocumentId = "DocumentId", DriveId = "DriveId" },
                    Source = "test"
                });
                var response = new HttpResponseMessage()
                {
                    Content = new StringContent("Test response"),
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };

                // Mock function parameters
                var mockFunctionContext = GetFunctionParams();

                // Getting mock config variables
                var (mockQueueService, mockBusinessService) = MockConfiguration(response);

                // Build azure function
                var postFeedback = new PostFeedback(mockQueueService.Object, mockBusinessService.Object);

                // Run function
                await postFeedback.RunAsync(queueItem, mockFunctionContext.Object);
            }
            catch (Exception ex)
            {
                Assert.Equal("Bad Request", ex.Message);
            }
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

        /// <summary>
        /// Mocking services and configuration
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="responseMessage"></param>
        /// <returns></returns>
        private static (Mock<IQueueService>, Mock<IPostFeedBackBusiness>) MockConfiguration(HttpResponseMessage responseMessage)
        {
            // Mock variables
            var mockQueueService = new Mock<IQueueService>();
            var mockPostFeedBackBusiness = new Mock<IPostFeedBackBusiness>();

            // Mock queue service
            mockQueueService.SetupAllProperties();
            mockQueueService.Setup(x => x.PostFeedbackAsync(It.IsAny<QueueItem>())).ReturnsAsync(responseMessage);

            // Mock business service
            var queueItem = new QueueItem()
            {
                ClientUrl = "https://test.com",
                Content = new FeedbackItem() { DocumentId = "test", DriveId = "test" },
                Source = "test"
            };
            mockPostFeedBackBusiness.SetupAllProperties();
            mockPostFeedBackBusiness.Setup(x => x.GetQueueItem(It.IsAny<QueueItem>())).Returns(queueItem);

            return (mockQueueService, mockPostFeedBackBusiness);
        }

        #endregion
    }
}
