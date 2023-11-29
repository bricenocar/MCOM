using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MCOM.Models
{
    public static class Global
    {
        public static ILogger Log { get; set; }
        public static bool BlobIsLoggingEnabled { get; set; }
        public static bool BlobIsTelemetryEnabled { get; set; }
        public static bool BlobIsDistributedTracingEnabled { get; set; }
        public static int BlobMaxRetries { get; set; }
        public static string BlobStorageAccountName { get; set; }
        public static bool BlobOverwriteExistingFile { get; set; }
        public static List<string> MandatoryMetadataFields { get; set; }
        public static string SharePointUrl { get; set; }
        public static string SharePointDomain { get; set; }
        public static string SelectPDFLicense { get; set; }
        public static string ProvisioningBlobStorageAccountName { get; set; }
        public static string AppInsightsAppId { get; set; }
        public static bool AppInsightsStrcuturedLogs { get; set; }
        public static string GeneratePDFURL { get; set; }
        public static string DummyDocumentQuery { get; set; }
        public static int DummyDocumentQueryQuantity { get; set; }
        public static string DummyDocumentProperties { get; set; }
        public static string DummyDocumentSearchResultId { get; set; }
        public static string TenantId { get; set; }
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string Scope { get; set; }
        public static string ValidateUrlFunctionUrl { get; set; }
    }
}
