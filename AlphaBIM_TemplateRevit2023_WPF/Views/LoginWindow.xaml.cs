using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using MahApps.Metro.Controls;

namespace NTC.FamilyManager.Views
{
    public partial class LoginWindow : MetroWindow
    {
        public string AuthCode { get; private set; }
        private readonly string _startUrl;
        private readonly string _redirectUri;

        public LoginWindow(string startUrl, string redirectUri)
        {
            InitializeComponent();
            _startUrl = startUrl;
            _redirectUri = redirectUri;
            
            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cấu hình UserDataFolder riêng biệt cho Revit Plugin
                // Nếu không cấu hình, WebView2 sẽ cố tạo trong thư mục cài đặt Revit (Program Files) và thất bại
                string userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NTC_FamilyManager", 
                    "WebView2");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
                webView.Source = new Uri(_startUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi tạo trình duyệt: " + ex.Message + "\n" + ex.StackTrace);
                this.Close();
            }
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            string url = webView.Source.ToString();
            if (url.StartsWith(_redirectUri))
            {
                // Parse code from URL
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                AuthCode = query.Get("code");
                
                if (!string.IsNullOrEmpty(AuthCode))
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }
    }
}
