namespace MCOM.Provisioning.Workflow.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Extensions.Workflows;
    using Microsoft.Azure.WebJobs;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using MCOM.Provisioning.Workflow.Utils;
    using Microsoft.Extensions.Logging;


    /// <summary>
    /// Represents the PrepareProvisioningTemplate flow invoked function.
    /// </summary>
    public class PrepareProvisioningTemplate
    {
        private readonly ILogger<PrepareProvisioningTemplate> logger;

        public PrepareProvisioningTemplate(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<PrepareProvisioningTemplate>();
        }

        /// <summary>
        /// Prepares the pnp provisioning template based on data from the provisioning request
        /// </summary>
        /// <param name="templateInfo">The metadata coming from provisioning request</param>        
        [FunctionName("PrepareProvisioningTemplate")]
        public Task<string> Run([WorkflowActionTrigger] string workloadMetadata, string template)
        {
            var result = string.Empty;
            try
            {
                JsonExtractor jsonExtractor = new JsonExtractor();
                Dictionary<string, object> extractedAttributes = jsonExtractor.ExtractJsonAttributes(jsonString);
                // Get the value of the property "Type" from the JSON string
                string type = (string)extractedAttributes["Type"];
                
                foreach (var kvp in extractedAttributes)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }


            return Task.FromResult(result);
        }        
    }
}