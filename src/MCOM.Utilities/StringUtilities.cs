using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Models.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MCOM.Utilities
{
    public class StringUtilities
    {
        public static string RemoveSpecialChars(string str)
        {
            try
            {
                // Create  a string array and add the special characters you want to remove
                var chars = new string[] { "~", "#", "%", "&", "*", ":", "<", ">", "?", "/", "\\", "{", "|", "}", "\"" };

                if (!str.IsNullOrEmpty())
                {
                    // Iterate the number of times based on the String array length.
                    for (int i = 0; i < chars.Length; i++)
                    {
                        if (str.Contains(chars[i]))
                        {
                            str = str.Replace(chars[i], "");
                        }
                    }
                }
                return str.Trim();
            }
            catch (Exception)
            {
                Global.Log.LogError("Error removing special characters from string {String}", str);
                return str.Trim();
            }            
        }

        public static string GetFullUrl(string url)
        {
            try
            {
                var baseUrl = Global.SharePointUrl;
                url = NormalizeSiteAlias(url);
                var fullUrl = $"{baseUrl}/sites/{url}";
                return fullUrl;
            }
            catch (Exception)
            {
                Global.Log.LogError("Error getting full url for site alias {SiteAlias}", url);
                throw;
            }            
        }


        public static bool ValidateAliasText(string text)
        {
            const string unallowedCharacters = "[&,!@;:#¤`´~¨='%<>/\\\\\"\\$\\*\\^\\+\\|\\{\\}\\[\\]\\(\\)\\?\\s]";
            return !Regex.IsMatch(text, unallowedCharacters);
        }

        // / <summary>
        // / Normalize the site alias
        // / </summary>
        // / <param name="alias">Site alias</param>
        // / <returns>Normalized site alias</returns>
        public static string NormalizeSiteAlias(string alias)
        {
            alias = RemoveUnallowedCharacters(alias);
            alias = ReplaceAccentedCharactersWithLatin(alias);
            return alias;
        }

        public static string RemoveUnallowedCharacters(string str)
        {
            const string unallowedCharacters = "[&,!@;:#¤`´~¨='%<>/\\\\\"\\$\\*\\^\\+\\|\\{\\}\\[\\]\\(\\)\\?\\s]";
            var regex = new Regex(unallowedCharacters);
            return regex.Replace(str, "");
        }

        public static string ReplaceAccentedCharactersWithLatin(string str)
        {
            const string a = "[äåàáâãæ]";
            var regex = new Regex(a, RegexOptions.IgnoreCase);
            str = regex.Replace(str, "a");

            const string e = "[èéêëēĕėęě]";
            regex = new Regex(e, RegexOptions.IgnoreCase);
            str = regex.Replace(str, "e");

            const string i = "[ìíîïĩīĭįı]";
            regex = new Regex(i, RegexOptions.IgnoreCase);
            str = regex.Replace(str, "i");

            const string o = "[öòóôõø]";
            regex = new Regex(o, RegexOptions.IgnoreCase);
            str = regex.Replace(str, "o");

            const string u = "[üùúû]";
            regex = new Regex(u, RegexOptions.IgnoreCase);
            str = regex.Replace(str, "u");

            const string c = "[çċčćĉ]";
            regex = new Regex(c, RegexOptions.IgnoreCase);
            str = regex.Replace(str, "c");

            const string d = "[ðďđđ]";
            regex = new Regex(d, RegexOptions.IgnoreCase);
            str = regex.Replace(str, "d");

            return str;
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

        public static Dictionary<string, string> MergeDictionaries(params Dictionary<string, string>[] dictionaries)
        {
            // Use Union to merge dictionaries, and ToDictionary to convert the result to a dictionary
            return dictionaries.Aggregate((dict1, dict2) => dict1.Union(dict2).ToDictionary(x => x.Key, x => x.Value));
        }
    }
}
