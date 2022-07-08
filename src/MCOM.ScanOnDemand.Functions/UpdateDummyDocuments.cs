using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MCOM.ScanOnDemand.Functions
{
    public class UpdateDummyDocuments
    {
        private readonly ILogger _logger;

        public UpdateDummyDocuments(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateDummyDocuments>();
        }

        [Function("UpdateDummyDocuments")]
        public void Run([TimerTrigger("0 */5 * * * *")] MyInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
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
