using System.Collections.Generic;

namespace MCOM.Models
{
    public class ArchiveFileData<TKey, TValue> : Dictionary<string, object>
    {
        public ArchiveFileData() : base() { }
        public string FileName { get; set; }
        public string Source { get; set; }
        public string FileTitle { get; set; }
        public string BlobFilePath { get; set; }
        public string DriveID { get; set; }
        public string DocumentId { get; set; }
        public string DocumentIdField { get; set; }
        public string FeedBackUrl { get; set; }
        public bool UseCSOM { get; set; }
        public bool SetMetadata { get; set; }
        
        public Dictionary<string, object> FileMetadata;

        public void ValidateInput()
        {
            UseCSOM = false;
            SetMetadata = false;

            if(FileMetadata is null)
            {
                FileMetadata = new Dictionary<string, object>();
            }
            foreach (KeyValuePair<string, object> pair in this)
            {
                switch (pair.Key.ToLower())
                {
                    case "source":
                        Source = pair.Value.ToString();
                        break;
                    case "filename":
                        FileName = pair.Value.ToString();
                        break;                    
                    case "tempfilelocation":
                        BlobFilePath = pair.Value.ToString();
                        break;
                    case "driveid":
                        DriveID = pair.Value.ToString();
                        break;
                    case "usecsom":
                        UseCSOM = (bool)pair.Value;
                        break;
                    case "documentid":
                        DocumentId =pair.Value.ToString();
                        break;
                    case "lrmhpecmrecordid": // Workaround for issue in ADF
                        DocumentId = pair.Value.ToString();
                        FileMetadata.Add(pair.Key, pair.Value.ToString());
                        break;
                    case "feedbackurl":
                        FeedBackUrl = pair.Value.ToString();
                        break;
                    case "documentidfield":
                        DocumentIdField = pair.Value.ToString();
                        break;
                    default:
                        SetMetadata = true;                        
                        FileMetadata.Add(pair.Key, pair.Value.ToString());
                        break;
                }             
            }
        }        
    }
}
