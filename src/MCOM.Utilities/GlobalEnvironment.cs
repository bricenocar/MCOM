using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MCOM.Models;

namespace MCOM.Utilities
{
    public static class GlobalEnvironment
    {
        public static void SetEnvironmentVariables(ILogger log)
        {
            Global.Log = log;            
            Global.BlobStorageAccountName = Environment.GetEnvironmentVariable("BlobStorageAccountName");            
            Global.BlobMaxRetries = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobMaxRetries")) ? Convert.ToInt32(Environment.GetEnvironmentVariable("BlobMaxRetries")) : 5;
            Global.BlobOverwriteExistingFile = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobOverwriteExistingFile")) && Convert.ToBoolean(Environment.GetEnvironmentVariable("BlobOverwriteExistingFile"));
            Global.BlobIsLoggingEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobIsLoggingEnabled")) && Convert.ToBoolean(Environment.GetEnvironmentVariable("BlobIsLoggingEnabled"));
            Global.BlobIsTelemetryEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobIsTelemetryEnabled")) && Convert.ToBoolean(Environment.GetEnvironmentVariable("BlobIsTelemetryEnabled"));
            Global.BlobIsDistributedTracingEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobIsDistributedTracingEnabled")) && Convert.ToBoolean(Environment.GetEnvironmentVariable("BlobIsDistributedTracingEnabled"));
            Global.AppInsightsStrcuturedLogs = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AppInsightsStrcuturedLogs")) && Convert.ToBoolean(Environment.GetEnvironmentVariable("AppInsightsStrcuturedLogs"));
            Global.AppInsightsAppId = Environment.GetEnvironmentVariable("AppInsightsAppId");
           
            // SharePoint related
            Global.SharePointUrl = Environment.GetEnvironmentVariable("SharePointUrl");
            Global.SharePointDomain = Environment.GetEnvironmentVariable("SharePointDomain");
            Global.SelectPDFLicense = Environment.GetEnvironmentVariable("SelectPDFLicense");
            Global.GeneratePDFURL = Environment.GetEnvironmentVariable("GeneratePDFURL");
            Global.DummyDocumentQuery = Environment.GetEnvironmentVariable("DummyDocumentQuery");
            Global.DummyDocumentProperties = Environment.GetEnvironmentVariable("DummyDocumentProperties");            
            Global.DummyDocumentQueryQuantity = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DummyDocumentQueryQuantity")) ? Int32.Parse(Environment.GetEnvironmentVariable("DummyDocumentQueryQuantity")) : 100;            

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MandatoryMetadataFields")))
            {
                var values = Environment.GetEnvironmentVariable("MandatoryMetadataFields");
                char[] delimiters = { ',', ';' };
                Global.MandatoryMetadataFields = new List<string>(values.Split(delimiters, StringSplitOptions.RemoveEmptyEntries));
            }
        }
    }
}
