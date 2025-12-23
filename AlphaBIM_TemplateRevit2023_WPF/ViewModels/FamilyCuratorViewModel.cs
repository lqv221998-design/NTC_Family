using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Core.Mvvm;
using NTC.FamilyManager.Core.Models;
using NTC.FamilyManager.Infrastructure.Revit;

namespace NTC.FamilyManager.ViewModels
{
    public class FamilyCuratorViewModel : ViewModelBase
    {
        private readonly IFamilyCuratorService _curatorService;
        private readonly RevitRequestHandler _revitHandler;
        private ObservableCollection<FamilyProcessingResult> _proposals;
        private bool _isProcessing;
        private string _statusMessage;

        public FamilyCuratorViewModel(IFamilyCuratorService curatorService, RevitRequestHandler revitHandler)
        {
            _curatorService = curatorService;
            _revitHandler = revitHandler;
            Proposals = new ObservableCollection<FamilyProcessingResult>();

            SelectFolderCommand = new RelayCommand(_ => SelectFolder());
            CommitAllCommand = new RelayCommand(async _ => await CommitAllAsync());
            CommitSelectedCommand = new RelayCommand(async p => await CommitItemAsync(p as FamilyProcessingResult));
            RemoveItemCommand = new RelayCommand(p => Proposals.Remove(p as FamilyProcessingResult));
        }

        public ObservableCollection<FamilyProcessingResult> Proposals
        {
            get => _proposals;
            set => SetProperty(ref _proposals, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SelectFolderCommand { get; }
        public ICommand CommitAllCommand { get; }
        public ICommand CommitSelectedCommand { get; }
        public ICommand RemoveItemCommand { get; }

        private void SelectFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _ = ScanFolderAsync(dialog.SelectedPath);
                }
            }
        }

        private async Task ScanFolderAsync(string path)
        {
            IsProcessing = true;
            StatusMessage = "Đang quét và đề xuất tên chuẩn...";
            
            try
            {
                var files = Directory.GetFiles(path, "*.rfa", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var proposal = await _curatorService.AnalyzeFamilyAsync(file);
                    if (proposal != null)
                    {
                        Proposals.Add(proposal);
                    }
                }
                StatusMessage = $"Đã đề xuất {files.Length} file. Mời Admin kiểm duyệt.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task CommitItemAsync(FamilyProcessingResult proposal)
        {
            if (proposal == null) return;
            
            bool success = await _curatorService.CommitStandardizationAsync(proposal);
            if (success)
            {
                StatusMessage = $"Đã chuẩn hóa: {proposal.FamilyName}";
            }
        }

        private async Task CommitAllAsync()
        {
            IsProcessing = true;
            int count = 0;
            try
            {
                foreach (var proposal in Proposals.ToList())
                {
                    if (await _curatorService.CommitStandardizationAsync(proposal))
                    {
                        Proposals.Remove(proposal);
                        count++;
                    }
                }
                StatusMessage = $"Đã hoàn thành chuẩn hóa {count} file.";
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
