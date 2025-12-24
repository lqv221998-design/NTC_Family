using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Core.Mvvm;
using NTC.FamilyManager.Core.Models;
using NTC.FamilyManager.Infrastructure.Revit;
using Autodesk.Revit.UI;

namespace NTC.FamilyManager.ViewModels
{
    public class FamilyCuratorViewModel : ViewModelBase
    {
        private readonly IFamilyCuratorService _curatorService;
        private readonly RevitRequestHandler _revitHandler;
        private ObservableCollection<FamilyProcessingResult> _proposals;
        private bool _isProcessing;
        private string _statusMessage;
        private double _progressValue;
        private readonly Dispatcher _uiDispatcher;

        public FamilyCuratorViewModel(IFamilyCuratorService curatorService, RevitRequestHandler revitHandler)
        {
            _curatorService = curatorService;
            _revitHandler = revitHandler;
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            
            Proposals = new ObservableCollection<FamilyProcessingResult>();

            SelectFilesCommand = new RelayCommand(_ => SelectFiles());
            SelectFolderCommand = new RelayCommand(_ => SelectFolder());
            CommitAllCommand = new RelayCommand(async _ => await CommitAllAsync());
            CommitSelectedCommand = new RelayCommand(async p => await CommitSelectedAsync(p as FamilyProcessingResult));
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

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public ICommand SelectFilesCommand { get; }
        public ICommand SelectFolderCommand { get; }
        public ICommand CommitAllCommand { get; }
        public ICommand CommitSelectedCommand { get; }
        public ICommand RemoveItemCommand { get; }

        private void SelectFiles()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Revit Family (*.rfa)|*.rfa"
            };

            if (dialog.ShowDialog() == true)
            {
                _ = ProcessFilesAsync(dialog.FileNames);
            }
        }

        private void SelectFolder()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Chọn thư mục"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = Path.GetDirectoryName(dialog.FileName);
                if (Directory.Exists(folderPath))
                {
                    var files = Directory.GetFiles(folderPath, "*.rfa", SearchOption.AllDirectories);
                    _ = ProcessFilesAsync(files);
                }
            }
        }

        private async Task ProcessFilesAsync(string[] files)
        {
            if (files == null || files.Length == 0) return;

            IsProcessing = true;
            ProgressValue = 0;
            int total = files.Length;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    string file = files[i];
                    string fileName = Path.GetFileName(file);
                    StatusMessage = $"Đang phân tích ({i + 1}/{total}): {fileName}...";
                    
                    try 
                    {
                        var proposal = await _curatorService.AnalyzeFamilyAsync(file);
                        if (proposal != null)
                        {
                            _uiDispatcher.Invoke(() => Proposals.Add(proposal));
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Lỗi tại file {fileName}: {ex.Message}";
                        System.Diagnostics.Debug.WriteLine($"Failed to process {file}: {ex.Message}");
                        await Task.Delay(1000); 
                    }

                    ProgressValue = ((double)(i + 1) / total) * 100;
                }
                StatusMessage = $"Hoàn tất phân tích {total} file.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task CommitSelectedAsync(FamilyProcessingResult proposal)
        {
            if (proposal == null) return;
            
            bool success = await _curatorService.CommitStandardizationAsync(proposal);
            if (success)
            {
                _uiDispatcher.Invoke(() => Proposals.Remove(proposal));
                StatusMessage = $"Đã chuẩn hóa: {proposal.FamilyName}";
            }
        }

        private async Task CommitAllAsync()
        {
            if (Proposals == null || Proposals.Count == 0)
            {
                StatusMessage = "Không có gì để chuẩn hóa!";
                return;
            }

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục đích cho Thư viện Family (Phân loại theo Discipline/Category)";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    IsProcessing = true;
                    int count = 0;

                    try
                    {
                        var itemsToCommit = Proposals.ToList();
                        foreach (var proposal in itemsToCommit)
                        {
                            StatusMessage = $"Đang lưu: {proposal.FamilyName}...";
                            
                            if (await _curatorService.CommitStandardizationAsync(proposal, selectedPath))
                            {
                                _uiDispatcher.Invoke(() => Proposals.Remove(proposal));
                                count++;
                            }
                        }
                        StatusMessage = $"Đã chuẩn hóa thành công {count} file vào: {selectedPath}";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Lỗi khi lưu: {ex.Message}";
                    }
                    finally
                    {
                        IsProcessing = false;
                    }
                }
            }
        }
    }
}
