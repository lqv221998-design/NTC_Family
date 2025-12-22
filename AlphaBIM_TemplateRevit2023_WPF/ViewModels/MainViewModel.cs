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
using NTC.FamilyManager.Base;
#endregion

namespace NTC.FamilyManager
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        public MainViewModel(UIDocument uiDoc)
        {
            UiDoc = uiDoc;
            Doc = UiDoc.Document;

            _authService = new AuthService();
            LoginCommand = new RelayCommand(_ => _ = LoginAction());
            
            StatusMessage = "Vui lòng đăng nhập để tiếp tục.";
        }

        public ICommand LoginCommand { get; }

        private async System.Threading.Tasks.Task LoginAction()
        {
            StatusMessage = "Đang khởi tạo trình đăng nhập Microsoft...";
            
            try
            {
                // Gọi LoginAsync mà không cần truyền email trước (Cửa sổ MS sẽ tự hỏi)
                bool success = await _authService.LoginAsync();
                
                if (success)
                {
                    UserName = _authService.UserName;
                    UserEmail = _authService.UserEmail;
                    IsLoggedIn = true;
                    StatusMessage = "Đăng nhập thành công!";

                    // TODO: Khởi tạo dữ liệu SharePoint sau khi đăng nhập thành công
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
                
                // Nếu lỗi liên quan đến nạp DLL, gợi ý người dùng khởi động lại Revit
                if (ex.Message.Contains("DiagnosticSource") || ex.Message.Contains("Meter"))
                {
                    StatusMessage = "Lỗi thư viện hệ thống. Vui lòng khởi động lại Revit.";
                }
            }
        }

        #region Public Properties

        public UIDocument UiDoc { get; }
        public Document Doc { get; }

        // Đã gỡ UserEmailInput theo Image 0

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
