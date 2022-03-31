using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using Xunit;
using MCOM.Models;
using MCOM.Archiving.Functions;
using MCOM.Services;
using MCOM.Business.PostFeedBack;

namespace MCOM.Tests
{
    public class PostFeedbackJobTest
    {
        #region Tests

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task When_The_Function_Runs_Ok(int itemCount)
        {
            // Mock function parameters
            var (mockFunctionContext, timerInfo) = GetFunctionParams();

            // Init list of clod messages
            var messages = new List<CloudQueueMessage>();

            var i = 0;
            while (i < itemCount)
            {
                var strQueueItem = JsonConvert.SerializeObject(new QueueItem()
                {
                    ClientUrl = "https://test.com",
                    Content = new FeedbackItem() { DocumentId = $"DocumentId-{i}", DriveId = $"DriveId-{i}" },
                    Source = "test"
                });
                messages.Add(new CloudQueueMessage(strQueueItem));
                i++;
            }
              
            var response = new HttpResponseMessage()
            {
                Content = new StringContent("Test response"),
                StatusCode = System.Net.HttpStatusCode.OK
            };

            // Getting mock config variables
            var (mockQueueService, mockBusinessService) = MockConfiguration(messages, response);

            // Build azure function
            var postFeedbackJob = new PostFeedbackJob(mockQueueService.Object, mockBusinessService.Object);

            //Run function
            await postFeedbackJob.RunAsync(timerInfo, mockFunctionContext.Object);

            // Asserts
            mockQueueService.Verify(x => x.GetCloudQueue(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            mockQueueService.Verify(x => x.PostFeedbackAsync(It.IsAny<QueueItem>()), Times.Exactly(itemCount));
        }

        [Fact]
        public async Task When_The_QueItem_Is_Not_Well_Formed()
        {
            try
            {
                // Mock function parameters
                var (mockFunctionContext, timerInfo) = GetFunctionParams();

                // Mock return values
                var queueItem = JsonConvert.SerializeObject(new // WE ARE SENDING HERE A WRONG OBJECT
                {
                    Test = "https://test.com",
                    Testing = "Test"
                });
                var messages = new List<CloudQueueMessage>()
                {
                    new CloudQueueMessage(queueItem)
                };
                var response = new HttpResponseMessage()
                {
                    Content = new StringContent("Test response"),
                    StatusCode = System.Net.HttpStatusCode.OK
                };

                // Getting mock config variables
                var (mockQueueService, mockBusinessService) = MockConfiguration(messages, response);

                // Build azure function
                var postFeedbackJob = new PostFeedbackJob(mockQueueService.Object, mockBusinessService.Object);

                //Run function
                await postFeedbackJob.RunAsync(timerInfo, mockFunctionContext.Object);
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
                // Mock function parameters
                var (mockFunctionContext, timerInfo) = GetFunctionParams();

                // Mock return values
                var queueItem = JsonConvert.SerializeObject(new // WE ARE SENDING HERE A WRONG OBJECT
                {
                    ClientUrl = "https://test.com",
                    Content = "test", 
                    Item = new FeedbackItem() { DocumentId = "00000000-0000-0000-0000-000000000000", DriveId = "00000000-0000-0000-0000-000000000000" }
                });
                var messages = new List<CloudQueueMessage>()
                {
                    new CloudQueueMessage(queueItem)
                };
                var response = new HttpResponseMessage()
                {
                    Content = new StringContent("Test response"),
                    StatusCode = System.Net.HttpStatusCode.OK
                };

                // Getting mock config variables
                var (mockQueueService, mockBusinessService) = MockConfiguration(messages, response);

                // Build azure function
                var postFeedbackJob = new PostFeedbackJob(mockQueueService.Object, mockBusinessService.Object);

                //Run function
                await postFeedbackJob.RunAsync(timerInfo, mockFunctionContext.Object);
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
                // Mock function parameters
                var (mockFunctionContext, timerInfo) = GetFunctionParams();

                // Mock return values
                var queueItem = JsonConvert.SerializeObject(new QueueItem()
                {
                    ClientUrl = "https://test.com",
                    Content = new FeedbackItem() { DocumentId = "DocumentId", DriveId = "DriveId" }
                });
                var messages = new List<CloudQueueMessage>()
                {
                    new CloudQueueMessage(queueItem)
                };
                var response = new HttpResponseMessage()
                {
                    Content = new StringContent("Test response"),
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };

                // Getting mock config variables
                var (mockQueueService, mockBusinessService) = MockConfiguration(messages, response);

                // Build azure function
                var postFeedbackJob = new PostFeedbackJob(mockQueueService.Object, mockBusinessService.Object);

                //Run function
                await postFeedbackJob.RunAsync(timerInfo, mockFunctionContext.Object);
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

        /// <summary>
        /// Mocking services and configuration
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="responseMessage"></param>
        /// <returns></returns>
        private static (Mock<IQueueService>, Mock<IPostFeedBackBusiness>) MockConfiguration(List<CloudQueueMessage> messages, HttpResponseMessage responseMessage)
        {
            // Mock variables
            var mockQueueService = new Mock<IQueueService>();
            var mockPostFeedBackBusiness = new Mock<IPostFeedBackBusiness>();
            var mockQueue = new Mock<CloudQueue>(new Uri("https://test.com"));

            // Mock queue service
            mockQueueService.SetupAllProperties();
            mockQueueService.Setup(x => x.GetCloudQueue(It.IsAny<string>(), It.IsAny<string>())).Returns(mockQueue.Object);
            mockQueue.Setup(x => x.GetMessagesAsync(It.IsAny<int>())).ReturnsAsync(messages);
            mockQueueService.Setup(x => x.PostFeedbackAsync(It.IsAny<QueueItem>())).ReturnsAsync(responseMessage);

            // Mock business service
            mockPostFeedBackBusiness.SetupAllProperties();
            mockPostFeedBackBusiness.Setup(x => x.GetQueueItem(It.IsAny<QueueItem>())).Returns(new QueueItem());

            return (mockQueueService, mockPostFeedBackBusiness);
        }

        #endregion
    }
}
