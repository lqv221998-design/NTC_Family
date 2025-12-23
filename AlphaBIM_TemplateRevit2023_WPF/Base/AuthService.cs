using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using NTC.FamilyManager.Views;

namespace NTC.FamilyManager.Base
{
    public class AuthService : IAuthService
    {
        // Cho phép IT cấu hình sau này mà không cần sửa code core
        public static string ClientId { get; set; } = "4a1aa350-ad1c-4340-a92c-05574044414e";
        public static string TenantId { get; set; } = "ac5781b0-bb44-456b-93b8-a6c9dd7a9989";
        public static string RedirectUri { get; set; } = "http://localhost";
        
        // Chế độ dành cho Developer hoặc khi chưa có IT cấu hình Azure AD
        public static bool IsMockMode { get; set; } = false;

        private readonly string[] _scopes = { "User.Read", "Files.Read.All", "Sites.Read.All" };
        
        private string _accessToken;
        private string _refreshToken;
        
        public bool IsAuthenticated { get; private set; }
        public string UserName { get; private set; }
        public string UserEmail { get; private set; }

        public async Task<bool> LoginAsync()
        {
            if (IsMockMode)
            {
                UserName = "Developer NTC";
                UserEmail = "dev@newtecons.vn";
                IsAuthenticated = true;
                return true;
            }

            try
            {
                // 1. Try silent login first
                if (await TrySilentLoginAsync()) return true;

                // 2. Interactive Login via WebView2
                string scopesStr = string.Join(" ", _scopes);
                string authUrl = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/authorize?" +
                                $"client_id={ClientId}&response_type=code&redirect_uri={RedirectUri}&" +
                                $"response_mode=query&scope={Uri.EscapeDataString(scopesStr)}";

                var loginWin = new LoginWindow(authUrl, RedirectUri);
                if (loginWin.ShowDialog() == true && !string.IsNullOrEmpty(loginWin.AuthCode))
                {
                    bool success = await ExchangeCodeForTokenAsync(loginWin.AuthCode);
                    if (success)
                    {
                        TokenCacheHelper.SaveToken(_refreshToken);
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi đăng nhập: " + ex.Message);
            }
            return false;
        }

        private async Task<bool> ExchangeCodeForTokenAsync(string code)
        {
            using (var client = new HttpClient())
            {
                var dict = new Dictionary<string, string>
                {
                    {"client_id", ClientId},
                    {"scope", string.Join(" ", _scopes)},
                    {"code", code},
                    {"redirect_uri", RedirectUri},
                    {"grant_type", "authorization_code"}
                };

                var response = await client.PostAsync($"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token", 
                    new FormUrlEncodedContent(dict));
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                    
                    _accessToken = tokenResponse.AccessToken;
                    _refreshToken = tokenResponse.RefreshToken;
                    
                    return await ProcessUserInfoAsync();
                }
            }
            return false;
        }

        private async Task<bool> TrySilentLoginAsync()
        {
            string savedRefreshToken = TokenCacheHelper.LoadToken();
            if (string.IsNullOrEmpty(savedRefreshToken)) return false;

            return await ExchangeRefreshTokenForTokenAsync(savedRefreshToken);
        }

        private async Task<bool> ExchangeRefreshTokenForTokenAsync(string refreshToken)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var dict = new Dictionary<string, string>
                    {
                        {"client_id", ClientId},
                        {"scope", string.Join(" ", _scopes)},
                        {"refresh_token", refreshToken},
                        {"grant_type", "refresh_token"}
                    };

                    var response = await client.PostAsync($"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token", 
                        new FormUrlEncodedContent(dict));
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                        
                        _accessToken = tokenResponse.AccessToken;
                        _refreshToken = tokenResponse.RefreshToken ?? refreshToken;
                        
                        bool success = await ProcessUserInfoAsync();
                        if (success)
                        {
                            TokenCacheHelper.SaveToken(_refreshToken);
                        }
                        return success;
                    }
                }
            }
            catch { }
            return false;
        }

        private async Task<bool> ProcessUserInfoAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    string email = user.userPrincipalName ?? user.mail;
                    if (string.IsNullOrEmpty(email)) return false;

                    if (!email.EndsWith("@newtecons.vn") && !email.EndsWith("@ntc.vn"))
                    {
                        MessageBox.Show("Vui lòng sử dụng tài khoản email của Newtecons.", "Sai Domain", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Logout();
                        return false;
                    }

                    UserEmail = email;
                    UserName = user.displayName;
                    IsAuthenticated = true;
                    return true;
                }
            }
            return false;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            return _accessToken;
        }

        public void Logout()
        {
            _accessToken = null;
            _refreshToken = null;
            UserEmail = null;
            UserName = null;
            IsAuthenticated = false;
            TokenCacheHelper.ClearCache();
        }

        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
