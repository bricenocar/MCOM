using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace MCOM.Utilities
{
    public class QueueConfiguration
    {
        public virtual CloudQueue Queue { get; set; }

        public QueueConfiguration(string connString, string queueName)
        {
            // Retrieve storage account from connection string
            var storageAccount = CloudStorageAccount.Parse(connString);

            // Create the queue client
            var queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue
            Queue = queueClient.GetQueueReference(queueName);
        }
    }
}
