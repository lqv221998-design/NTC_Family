using System;
using NTC.FamilyManager.Core.Mvvm;

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

    public class FamilyProcessingResult : ViewModelBase
    {
        private string _originalPath;
        private string _newPath;
        private string _familyName;
        private string _category;
        private string _discipline;
        private string _version;
        private ProcessingStatus _status;
        private string _message;
        private string _thumbnailPath;
        private DateTime _processedTime;

        public string OriginalPath { get => _originalPath; set => SetProperty(ref _originalPath, value); }
        public string NewPath { get => _newPath; set => SetProperty(ref _newPath, value); }
        public string FamilyName { get => _familyName; set => SetProperty(ref _familyName, value); }
        public string Category { get => _category; set => SetProperty(ref _category, value); }
        public string Discipline { get => _discipline; set => SetProperty(ref _discipline, value); }
        public string Version { get => _version; set => SetProperty(ref _version, value); }
        public ProcessingStatus Status { get => _status; set => SetProperty(ref _status, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public string ThumbnailPath { get => _thumbnailPath; set => SetProperty(ref _thumbnailPath, value); }
        public DateTime ProcessedTime { get => _processedTime; set => SetProperty(ref _processedTime, value); }

        public FamilyProcessingResult()
        {
            Status = ProcessingStatus.Pending;
            ProcessedTime = DateTime.Now;
        }
    }
}
