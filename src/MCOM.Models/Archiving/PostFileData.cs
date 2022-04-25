using System.Collections.Generic;

namespace MCOM.Models.Archiving
{
    public class PostFileData<TKey, TValue> : Dictionary<string, string>
    {
        private List<string> _missingFields;

        public PostFileData() : base() { }

        public bool ValidateInput()
        {
            _missingFields = new List<string>();

            if (TryGetValue("Source", out var sourceValue)) SourceSystem = sourceValue;
            else _missingFields.Add("Source");

            foreach (var metadata in Global.MandatoryMetadataFields)
            {
                if (!metadata.ToLower().Equals("filename") && !metadata.ToLower().Equals("source"))
                {
                    if (!TryGetValue(metadata, out _)) _missingFields.Add(metadata);
                }
            }

            if (_missingFields.Count > 0)
            {
                return false;
            }

            return true;
        }

        public string FileName { get; set; }

        public string SourceSystem { get; set; }

        public string MissingMetadata
        {
            get
            {
                if (_missingFields != null)
                {
                    return string.Join(", ", _missingFields);
                }

                return "";
            }
        }
    }
}
