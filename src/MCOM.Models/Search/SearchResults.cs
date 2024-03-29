﻿namespace MCOM.Models.Search
{
    public class SearchResult
    {
        public string Name { get; set; }
        public string SiteId { get; set; }
        public string WebId { get; set; }
        public string ListId { get; set; }
        public string ListItemId { get; set; }
        public string OriginalPath { get; set; }
        public bool PhysicalRecord { get; set; }
        public string PhysicalRecordStatus { get; set; }
        public string FileExtension { get; set; }
        public string SPWebUrl { get; set; }
    }
}
