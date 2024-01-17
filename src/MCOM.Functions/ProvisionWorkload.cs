using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnP.Core.Services;
using PnP.Framework.Provisioning.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MCOM.Functions
{
    public class ProvisionWorkload
    {
        private readonly ILogger<ProvisionWorkload> _logger;
        private readonly IPnPContextFactory _pnpContextFactory;
        private IMicrosoft365Service _microsoft365Service;
        private IBlobService _blobService;

        public ProvisionWorkload(ILogger<ProvisionWorkload> logger, IPnPContextFactory pnpContextFactory, IMicrosoft365Service microsoft365Service, IBlobService blobService)
        {
            _logger = logger;
            _pnpContextFactory = pnpContextFactory;
            _microsoft365Service = microsoft365Service;
            _blobService = blobService;
        }

        [Function(nameof(ProvisionWorkload))]
        [ServiceBusOutput("provisioned", Connection = "ServiceBusConnectionString")]
        public async Task<string> Run([ServiceBusTrigger("processed", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                throw;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "ProvisionWorkload");
            var workloadData = message.Body.ToString();
            // Print the body of the message to the console
            Global.Log.LogInformation($"Received: {workloadData}");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "ProvisionWorkload", "Provisioning"))
            {
                // Get the message body as a json and convert it to a WorkloadCreationRequestPayload object                
                WorkloadCreationRequestPayload workloadCreationRequestPayload = new WorkloadCreationRequestPayload();
                try
                {
                    workloadCreationRequestPayload = JsonConvert.DeserializeObject<WorkloadCreationRequestPayload>(workloadData);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing JSON.");
                    throw;
                }

                // Get the blob from the storage account
                Stream blobStream = await GetProvisioningTemplateBlobItem(workloadCreationRequestPayload);

                // Validate that the stream is not empty
                if (blobStream != null && blobStream.Length > 0)
                {
                    // Get full URL
                    var urlSufix = string.Empty;
                    var siteType = workloadCreationRequestPayload.Site.SiteConfig.SiteType;
                    if(siteType == SiteType.TeamSite)
                    {
                        urlSufix = workloadCreationRequestPayload.Site.SiteConfig.Alias;
                    } else
                    {
                        urlSufix = workloadCreationRequestPayload.Site.SiteConfig.SiteURL;
                    }

                    // Error if sufix is null or empty
                    if(string.IsNullOrEmpty(urlSufix))
                    {
                        throw new InvalidRequestException("Alias or SiteURL", "The URL is empty or null. Check the input parameters in tue queue");
                    }

                    // Create the PnP Context
                    using (var pnpContext = await _pnpContextFactory.CreateAsync("Default"))
                    {
                        try
                        {
                            // Get the provisioning template from the blob item stream
                            ProvisioningTemplate provisioningTemplate = _microsoft365Service.GetProvisioningTemplate(blobStream);
                            Global.Log.LogInformation($"The provisioning template is found, data: {provisioningTemplate.Id}");

                            // Get optional metadata
                            var optionalMetadata = workloadCreationRequestPayload.Site.SiteMetadata.OptionalMetadata;

                            // Convert optional metadata to data dictionary string string
                            var dictionary = StringUtilities.ConvertToDictionary(optionalMetadata);

                            // Merge dictionaries with template parameters
                            var dynamicParameters = StringUtilities.MergeDictionaries(workloadCreationRequestPayload.Site.SiteMetadata.EIMMetadata, dictionary);

                            // Replace default values with values from the request
                            foreach (var parameter in dynamicParameters)
                            {
                                if (!string.IsNullOrEmpty(parameter.Value))
                                {
                                    // Replace default value in the site field
                                    var siteField = provisioningTemplate.SiteFields.Find(f => f.SchemaXml.Contains(parameter.Key));
                                    if (siteField != null)
                                    {
                                        string newXml = StringUtilities.ReplaceChildXmlNode(siteField.SchemaXml, "Default", parameter.Value);
                                        siteField.SchemaXml = newXml;
                                    }
                                    Global.Log.LogInformation($"Added default value for field {parameter.Key} with value {parameter.Value}");
                                }                                
                            }

                            // Add owners and members in case it is a communication site
                            if(workloadCreationRequestPayload.Site.SiteConfig.SiteType == SiteType.CommunicationSite)
                            {
                                var members = workloadCreationRequestPayload.Site.SiteUsers.Members.Select(m => m.Value.Contains(";") ? m.Value.Split(';')[1].Replace(",", "") : m.Value).ToList();
                                var owners = workloadCreationRequestPayload.Site.SiteUsers.Owners.Select(o => o.Value.Contains(";") ? o.Value.Split(';')[1].Replace(",", "") : o.Value).ToList();
                                await _microsoft365Service.addUsersToUngroupedSiteInTemplate(pnpContext, provisioningTemplate, owners, members);
                            }

                            // Add optional fields to the content type
                            if (provisioningTemplate.ContentTypes != null)
                            {
                                foreach (var contentType in provisioningTemplate.ContentTypes)
                                {
                                    // Find Equinor Document content type
                                    if (contentType.Id == "0x01010021A623C39873404E8BA89587BD4428B401")
                                    {
                                        foreach (var field in optionalMetadata)
                                        {
                                            var siteField = provisioningTemplate.SiteFields.Find(f => f.SchemaXml.Contains(field.InternalName));
                                            if (siteField != null)
                                            {
                                                string id = StringUtilities.GetAttributeFromXmlNode(siteField.SchemaXml, "Field", "ID");
                                                if (!string.IsNullOrEmpty(id))
                                                {
                                                    var fieldRef = new FieldRef(field.InternalName);
                                                    fieldRef.Id = new Guid(id);
                                                    contentType.FieldRefs.Add(fieldRef);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Apply the provisioning template to the target site
                            var result = _microsoft365Service.ApplyProvisioningTemplateAsync(pnpContext, provisioningTemplate, urlSufix);
                        }
                        catch (Exception ex)
                        {
                            if(ex.Message.Contains("duplicate"))
                            {
                                Global.Log.LogCritical(ex, $"There was a duplicated column already provisioned by another solution, retrying to apply provisioning template. Error message: {ex.Message}");
                                throw new SiteCreationException(urlSufix, ex.Message);
                            }
                            else
                            {
                                Global.Log.LogCritical(ex, $"There has been an error trying to provision site. Error message: {ex.Message}");
                                throw new SiteCreationException(urlSufix, ex.Message);
                            }
                        }                        
                    }
                } else
                {
                    throw new NullReferenceException("The provisioning template is empty or could not be retrieved. Check logs for more information");
                }
            }

            return workloadData;
        }

        private async Task<Stream> GetProvisioningTemplateBlobItem(WorkloadCreationRequestPayload workloadCreationRequestPayload)
        {
            // Initialize Stream
            Stream blobStream = null;

            // Get the xml template
            try
            {
                var xmlUrl = workloadCreationRequestPayload.Request.ProvisioningTemplateUrl;
                if (!string.IsNullOrEmpty(xmlUrl))
                {
                    // Get blob item from azure storage
                    _blobService.InitializeBlobServiceClient(Global.ProvisioningBlobStorageAccountName);
                    var blobClient = _blobService.GetBlobClient(new Uri(xmlUrl));
                    
                    // Check if the blob exists
                    Response<bool> existsResponse = await blobClient.ExistsAsync();
                    if (existsResponse != null && existsResponse.Value)
                    {
                        // Download the blob to a stream
                        blobStream = await blobClient.OpenReadAsync();
                    }
                    else
                    {
                        Global.Log.LogCritical(new FileNotFoundException(), "The Provisioning template does not exists.");
                    }
                }
            }
            catch (CredentialUnavailableException credException)
            {
                Global.Log.LogCritical(new UnauthorizedAccessException(), $"The resource does not have permissions to access the blob. Error message: {credException.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Global.Log.LogCritical(new FileNotFoundException(), $"There was an error trying to get the provisioning template. Error message: {ex.Message}");
                throw;
            }

            return blobStream;
        }
    }
}
