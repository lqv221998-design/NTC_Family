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

        public ObservableCollection<string> Disciplines { get; } = new ObservableCollection<string>
        {
            "Kiến trúc", "Kết cấu", "MEP", "Construction"
        };

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
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Revit Family (*.rfa)|*.rfa"
            };

            if (openDialog.ShowDialog() == true)
            {
                ProcessFiles(openDialog.FileNames);
            }
        }

        private void SelectFolder()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Chọn thư mục chứa Family để phân tích";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var files = Directory.GetFiles(folderDialog.SelectedPath, "*.rfa", SearchOption.AllDirectories);
                    ProcessFiles(files);
                }
            }
        }

        private async void ProcessFiles(string[] paths)
        {
            IsProcessing = true;
            try
            {
                foreach (var path in paths)
                {
                    StatusMessage = $"Đang phân tích: {Path.GetFileName(path)}...";
                    var result = await _curatorService.AnalyzeFamilyAsync(path);
                    if (result != null)
                    {
                        Proposals.Add(result);
                    }
                }
                StatusMessage = $"Đã tải xong {paths.Length} file.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task CommitAllAsync()
        {
            if (Proposals == null || Proposals.Count == 0)
            {
                StatusMessage = "Không có file nào để lưu!";
                return;
            }

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục đích để lưu Thư viện Family (Phân loại tự động theo Phiên bản/Dự án/Hạng mục)";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string rootPath = dialog.SelectedPath;
                    IsProcessing = true;
                    ProgressValue = 0;
                    int total = Proposals.Count;
                    int successCount = 0;
                    int failCount = 0;

                    try
                    {
                        // Copy danh sách để tránh lỗi sửa đổi collection khi đang duyệt
                        var items = Proposals.ToList();
                        
                        for (int i = 0; i < items.Count; i++)
                        {
                            var proposal = items[i];
                            StatusMessage = $"Đang xử lý ({i + 1}/{total}): {proposal.FamilyName}...";
                            ProgressValue = (double)(i + 1) / total * 100;

                            bool success = await Task.Run(async () => 
                                await _curatorService.CommitStandardizationAsync(proposal, rootPath));

                            if (success)
                            {
                                successCount++;
                                _uiDispatcher.Invoke(() => Proposals.Remove(proposal));
                            }
                            else
                            {
                                failCount++;
                            }
                        }

                        StatusMessage = $"Hoàn tất! Thành công: {successCount}, Thất bại: {failCount}.";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Lỗi hệ thống khi lưu hàng loạt: {ex.Message}";
                    }
                    finally
                    {
                        IsProcessing = false;
                        ProgressValue = 100;
                    }
                }
            }
        }

        private async Task CommitSelectedAsync(FamilyProcessingResult proposal)
        {
            if (proposal == null) return;

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục lưu cho file này";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    IsProcessing = true;
                    try
                    {
                        bool success = await _curatorService.CommitStandardizationAsync(proposal, dialog.SelectedPath);
                        if (success)
                        {
                            StatusMessage = $"Đã lưu thành công: {proposal.FamilyName}";
                            _uiDispatcher.Invoke(() => Proposals.Remove(proposal));
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Lỗi khi lưu file: {ex.Message}";
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
