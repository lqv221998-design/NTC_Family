namespace NTC.FamilyManager.Core.Models
{
    public class ThumbnailResult
    {
        public string LocalPath { get; set; }
        public byte[] RawData { get; set; }
        public bool IsFromCache { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success => string.IsNullOrEmpty(ErrorMessage) && RawData != null && RawData.Length > 0;
    }
}
