#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows.Input;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Mvvm;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Services.Auth;
using NTC.FamilyManager.Infrastructure.Revit;
using NTC.FamilyManager.Services.Family;
#endregion

namespace NTC.FamilyManager.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly RevitRequestHandler _revitHandler;
        private readonly ExternalEvent _externalEvent;

        public FamilyExplorerViewModel ExplorerVM { get; }
        public FamilyCuratorViewModel CuratorVM { get; }

        public MainViewModel(UIDocument uiDoc)
        {
            UiDoc = uiDoc;
            Doc = UiDoc.Document;

            // Bật Mock Mode để phát triển UI
            AuthService.IsMockMode = true;
            _authService = new AuthService();
            
            _revitHandler = new RevitRequestHandler();
            _externalEvent = ExternalEvent.Create(_revitHandler);

            ExplorerVM = new FamilyExplorerViewModel(_revitHandler, _externalEvent);
            
            var curatorService = new FamilyCuratorService(_revitHandler, _externalEvent);
            CuratorVM = new FamilyCuratorViewModel(curatorService, _revitHandler);

            LoginCommand = new RelayCommand(_ => _ = LoginAction());
            
            StatusMessage = "Vui lòng đăng nhập để tiếp tục.";
        }

        public ICommand LoginCommand { get; }

        private async Task LoginAction()
        {
            StatusMessage = "Đang kết nối...";
            
            try
            {
                bool success = await _authService.LoginAsync();
                
                if (success)
                {
                    UserName = _authService.UserName;
                    UserEmail = _authService.UserEmail;
                    IsLoggedIn = true;
                    StatusMessage = "Đăng nhập thành công!";

                    // Sau khi đăng nhập, load dữ liệu
                    _ = ExplorerVM.LoadFamiliesAsync();
                }
                else
                {
                    StatusMessage = "Đăng nhập thất bại hoặc bị hủy.";
                    IsLoggedIn = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi xác thực: " + ex.Message;
                IsLoggedIn = false;
                
                if (ex.Message.Contains("DiagnosticSource") || ex.Message.Contains("Meter"))
                {
                    StatusMessage = "Lỗi thư viện hệ thống. Vui lòng khởi động lại Revit.";
                }
            }
        }

        #region Public Properties

        public UIDocument UiDoc { get; }
        public Document Doc { get; }

        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _userEmail;
        public string UserEmail
        {
            get => _userEmail;
            set => SetProperty(ref _userEmail, value);
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion
    }
}
