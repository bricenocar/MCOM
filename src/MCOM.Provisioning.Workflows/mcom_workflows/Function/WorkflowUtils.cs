namespace MCOM.Provisioning.Workflow.Utils
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    public class JsonExtractor
    {
        public Dictionary<string, object> ExtractJsonAttributes(string jsonString)
        {
            // Initialize a dictionary to store the extracted attributes
            var extractedAttributes = new Dictionary<string, object>();

            try
            {
                // Parse the JSON string into a JToken
                JToken jsonToken = JToken.Parse(jsonString);

                // Recursively traverse the JSON structure to extract attributes
                ExtractAttributes(jsonToken, "", extractedAttributes);
            }
            catch (JsonReaderException ex)
            {
                // Handle JSON parsing errors here
                Console.WriteLine("JSON parsing error: " + ex.Message);
            }

            return extractedAttributes;
        }

        private void ExtractAttributes(JToken token, string currentPath, Dictionary<string, object> extractedAttributes)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty property in token.Children<JProperty>())
                    {
                        // Recursively traverse object properties
                        ExtractAttributes(property.Value, $"{currentPath}.{property.Name}", extractedAttributes);
                    }
                    break;
                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken arrayItem in token.Children())
                    {
                        // Recursively traverse array items
                        ExtractAttributes(arrayItem, $"{currentPath}[{index}]", extractedAttributes);
                        index++;
                    }
                    break;
                default:
                    // For leaf nodes, store the attribute and its value
                    extractedAttributes[currentPath] = ((JValue)token).Value;
                    break;
            }
        }
    }
}