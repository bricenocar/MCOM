using System;
using System.Collections.Generic;
using System.Text;
using MCOM.Models.UnitTesting;

namespace MCOM.Utilities
{
    public class StringUtilities
    {
        public static string RemoveSpecialChars(string str)
        {
            // Create  a string array and add the special characters you want to remove
            var chars = new string[] { "~", "#", "%", "&", "*", ":", "<", ">", "?", "/", "\\", "{", "|", "}", "\"" };

            // Iterate the number of times based on the String array length.
            for (int i = 0; i < chars.Length; i++)
            {
                if (str.Contains(chars[i]))
                {
                    str = str.Replace(chars[i], "");
                }
            }

            return str.Trim();
        }

        public static bool IsGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        public static StringBuilder BuildMultiPartForm(Dictionary<string, string> fields, List<FakeFile> files)
        {
            // Init var
            var stringBuilder = new StringBuilder();

            // Set boundary
            var boundary = "----------" + DateTime.Now.Ticks.ToString("x");
            // The first boundary
            var boundaryTemplate = "--" + boundary + "\r\n";
            // The last boundary.
            var trailer = "\r\n--" + boundary + "--\r\n";

            // form-data, properly formatted
            var formdataTemplate = "Content-Dis-data; name=\"{0}\"\r\n\r\n{1}";
            // form-data file upload, properly formatted
            var fileheaderTemplate = "Content-Dis-data; name=\"{0}\"; filename=\"{1}\";\r\nContent-Type: {2}\r\n\r\n";

            // Added to track if we need a CRLF or not
            bool bNeedsCRLF = false;

            if (fields != null)
            {
                foreach (string key in fields.Keys)
                {
                    // Append in case is needed
                    if (bNeedsCRLF)
                        stringBuilder.Append("\r\n");

                    // Append the boundary
                    stringBuilder.Append(boundaryTemplate);

                    // Append the key
                    stringBuilder.Append(string.Format(formdataTemplate, key, fields[key]));
                    bNeedsCRLF = true;
                }
            }

            if (files != null)
            {
                foreach (var file in files)
                {
                    // Append in case is needed
                    if (bNeedsCRLF)
                        stringBuilder.Append("\r\n");

                    // Append the boundary
                    stringBuilder.Append(boundaryTemplate);

                    // Append file
                    stringBuilder.Append(string.Format(fileheaderTemplate, file?.Param, file?.Name, file?.ContentType));

                    // Append the file data
                    stringBuilder.Append(file?.Data);
                }
            }

            stringBuilder.Append(trailer);

            return stringBuilder;
        }
    }
}
