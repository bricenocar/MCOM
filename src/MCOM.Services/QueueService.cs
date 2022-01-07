﻿using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using MCOM.Models;
using MCOM.Utilities;

namespace MCOM.Services
{
    public interface IQueueService
    {
        Task<HttpResponseMessage> PostFeedbackAsync(QueueItem queueMessage);
        CloudQueue GetCloudQueue(string connString, string queueName);
    }

    public class QueueService : IQueueService
    {
        public virtual CloudQueue GetCloudQueue(string connString, string queueName)
        {
            // Retrieve storage account from connection string
            var storageAccount = CloudStorageAccount.Parse(connString);

            // Create the queue client
            var queueClient = storageAccount.CreateCloudQueueClient();

            // Return the queue
            return queueClient.GetQueueReference(queueName);
        }

        public virtual async Task<HttpResponseMessage> PostFeedbackAsync(QueueItem queueMessage)
        {
            return await HttpClientUtilities.PostFeedbackAsync(queueMessage.ResponseUrl, queueMessage.Item);
        }
    }
}
