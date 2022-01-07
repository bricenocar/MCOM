using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MCOM.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace MCOM.PerformanceTests
{
    /*------IMPORTANT!--------
    When running the test locally in Visual Studio, create a new "config.local.json" file under Files/PostFileTest
    The content will be the same as config.json but replace the values with the real ones. 
    Get the values for config.locaø.json from another developer in case you don't have them.*/
    public class PostFileTest
    {
        #region Tests

        [Fact]
        public async Task Post_File_Performance_Test()
        {
            // Call API and get response content as string           
            var resultList = await OnPostAsync();

            foreach (var resultItem in resultList)
            {
                // Convert to json              
                dynamic jsonItemValue = JsonConvert.DeserializeObject(resultItem);

                // Get documentId from response
                var strDocumentId = jsonItemValue.documentId;

                // Asserts
                Assert.NotNull(strDocumentId);
                Assert.True(Guid.TryParse(strDocumentId.ToString(), out Guid _));
            }                 
        }

        #endregion

        #region Private methods

        private static async Task<List<string>> OnPostAsync()
        {
            // Init variables
            var resultList = new List<string>();

            // Get config file content
            var configContent = GetConfigFile();
            dynamic config = JsonConvert.DeserializeObject(configContent);

            // Config variables
            var count = Convert.ToInt32(config.PerformanceCount.ToString());
            var url = config.FunctionUrl.ToString();

            // Get authorization token from Azure
            var token = await AuthenticationUtilities.GetAuthenticationTokenAsync(config);

            // Create http instance
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            for (var i = 1; i <= count; i++)
            {
                // Get file and metadata
                var bytes = File.ReadAllBytes(@"Files/PostFileTest/ok.txt");
                var form = new MultipartFormDataContent
                {
                    { new StringContent($"DCFTest_{i}"), "Filename" },
                    { new StringContent("dcf"), "Source" },
                    { new StringContent("testuser"), "archived_user" },
                    { new StringContent("1234"), "attachments" },
                    { new StringContent("2014-05-05"), "bldate" },
                    { new StringContent("3"), "broker" },
                    { new StringContent("332211"), "cargonumber" },
                    { new StringContent("Equinor"), "customer" },
                    { new StringContent("2015-05-05"), "dateexecuted" },
                    { new StringContent("334455"), "dealnumber" },
                    { new StringContent("some place"), "dischargeport" },
                    { new StringContent("CARGO"), "foldertype" },
                    { new StringContent("ABIDJAN"), "loadport" },
                    { new StringContent("AG11IS1;AG12NO5"), "quality" },
                    { new StringContent("CBL/NI/SE"), "responsibleops" },
                    { new StringContent("hello world"), "title" },
                    { new StringContent("'BWGAS/CHARTERING'; GM NGLLPGPOPS; GM SHIPGAS; 'MASTER/FLANDERS HARMONY'; Tor Madsen (tormad); 'STATOIL/TOR MADSEN PERSONAL'"), "to" },
                    { new StringContent("2015-05-05"), "transmissiondate" },
                    { new StringContent("2012 BominDE"), "uniquefolderid" },
                    { new StringContent("#584-1"), "vessel" },
                    { new StringContent("1010007055"), "voyagenumber" },
                    // { new StringContent("58109b3b-1b6f-4120-92c8-a28dab7f3901"), "documentId" },
                    { new ByteArrayContent(bytes, 0, bytes.Length), "file", "test.txt" }
                };              

                // Get response
                var response = await httpClient.PostAsync(url, form);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error: StatucCode: {response.StatusCode}, Message: {responseContent}");

                resultList.Add(responseContent);
            }

            return resultList;
        }

        private static string GetConfigFile()
        {
            // Check if the local file exists. If so then it means that we are running in local environment
            if (File.Exists(@"Files/PostFileTest/config.local.json"))
                return File.ReadAllText(@"Files/PostFileTest/config.local.json");

            return File.ReadAllText(@"Files/PostFileTest/config.json");
        }

        #endregion
    }
}
