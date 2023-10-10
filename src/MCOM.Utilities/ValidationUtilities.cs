using System;

namespace MCOM.Utilities
{
    public class ValidationUtilities
    {
        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        
        public static bool isValidOwnersQuantity(string[] array)
        {
            if (array.Length < 2 || array.Length > 5)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class UnavailableUrlException : Exception
    {
        public string Url { get; set; }
        public UnavailableUrlException(string url)
            : base($"The url: {url}, is not available for use")
        {
            Url = url;
        }
    }

    public class InvalidRequestException : Exception
    {
        public string InvalidProperty { get; set; }
        public InvalidRequestException(string property, string message = "") 
            : base($"The property: {property}, has an invalid or missing value. {message}")
        {
            InvalidProperty = property;            
        }
    }

    public class SiteCreationException : Exception
    {
        public string Url { get; set; }
        public SiteCreationException(string url, string errorMessage)
            : base($"There was an exception trying to create site with url: {url}. Error message: {errorMessage}")
        {
            Url = url;
        }
    }

    public class TeamCreationException : Exception
    {
        public string GroupId { get; set; }
        public TeamCreationException(string groupId, string errorMessage)
            : base($"There was an exception trying to create team for group: {groupId}. Error message: {errorMessage}")
        {
            GroupId = groupId;            
        }
    }
}
