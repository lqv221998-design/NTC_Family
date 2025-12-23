using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NTC.FamilyManager.Models;
using NTC.FamilyManager.Base;
using Autodesk.Revit.UI;

namespace NTC.FamilyManager.ViewModels
{
    public class FamilyExplorerViewModel : ViewModelBase
    {
        private ObservableCollection<FamilyItem> _families;
        private List<FamilyItem> _allFamilies = new List<FamilyItem>();
        private string _searchText;
        private bool _isLoading;
        private readonly IFamilyService _familyService;
        private readonly RevitRequestHandler _revitHandler;
        private readonly ExternalEvent _externalEvent;

        public FamilyExplorerViewModel(RevitRequestHandler handler, ExternalEvent exEvent)
        {
            _revitHandler = handler;
            _externalEvent = exEvent;
            
            Families = new ObservableCollection<FamilyItem>();
            _familyService = new LocalFamilyService();
            LoadFamiliesCommand = new RelayCommand(async _ => await LoadFamiliesAsync());
            LoadToRevitCommand = new RelayCommand(p => LoadToRevit(p as FamilyItem));
            
            // Tự động load dữ liệu khi khởi tạo
            _ = LoadFamiliesAsync();
        }

        public ICommand LoadFamiliesCommand { get; }
        public ICommand LoadToRevitCommand { get; }

        public ObservableCollection<FamilyItem> Families
        {
            get => _families;
            set => SetProperty(ref _families, value);
        }

        public string SearchText
        {
            get => _searchText;
            set 
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                Families = new ObservableCollection<FamilyItem>(_allFamilies);
            }
            else
            {
                var filtered = _allFamilies.Where(f => f.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                Families = new ObservableCollection<FamilyItem>(filtered);
            }
        }

        private void LoadToRevit(FamilyItem item)
        {
            if (item == null) return;
            
            _revitHandler.FamilyPath = item.DownloadUrl;
            _externalEvent.Raise();
        }

        public async Task LoadFamiliesAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                string path = @"D:\NTC_ONEDRIVER_BACKCUP\OneDrive - newtecons.vn\Documents\NTC_Family\2021\Doors";
                var items = await _familyService.GetFamiliesAsync(path);
                
                _allFamilies = items;
                ApplyFilter();
            }
            catch (Exception)
            {
                // Handle error
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
