using System;

namespace NTC.FamilyManager.Models
{
    public class FamilyItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string RevitVersion { get; set; }
        public string ThumbnailUrl { get; set; }
        public string DownloadUrl { get; set; }
        public int Likes { get; set; }
        public int Downloads { get; set; }
        public DateTime LastModified { get; set; }
    }
}
