using System;

namespace NTC.FamilyManager.Core.Models
{
    public enum ProcessingStatus
    {
        Pending,
        Processing,
        Succeeded,
        Failed,
        Skipped
    }

    public class FamilyProcessingResult
    {
        public string OriginalPath { get; set; }
        public string NewPath { get; set; }
        public string FamilyName { get; set; }
        public string Category { get; set; }
        public string Discipline { get; set; }
        public ProcessingStatus Status { get; set; }
        public string Message { get; set; }
        public string ThumbnailPath { get; set; }
        public DateTime ProcessedTime { get; set; }

        public FamilyProcessingResult()
        {
            Status = ProcessingStatus.Pending;
            ProcessedTime = DateTime.Now;
        }
    }
}
