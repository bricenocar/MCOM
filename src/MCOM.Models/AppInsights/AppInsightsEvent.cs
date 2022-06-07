using System;

namespace MCOM.Models.AppInsights
{
    public class AppInsightsEvent
    {
        public string Message { get; set; }
        public DateTime EventDate { get; set; }
        public string LogLevel { get; set; }
    }
}
