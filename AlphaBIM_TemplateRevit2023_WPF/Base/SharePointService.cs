using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NTC.FamilyManager.Models;

namespace NTC.FamilyManager.Base
{
    public class SharePointService : ISharePointService
    {
        private readonly IAuthService _authService;
        private readonly HttpClient _httpClient;

        public SharePointService(IAuthService authService)
        {
            _authService = authService;
            _httpClient = new HttpClient();
        }

        public async Task<List<FamilyItem>> FetchFamiliesAsync(string siteId, string driveId, string folderPath = "")
        {
            var families = new List<FamilyItem>();
            string token = await _authService.GetAccessTokenAsync();
            
            if (string.IsNullOrEmpty(token)) return families;

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                // Graph API for files in a drive
                string url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root/children";
                if (!string.IsNullOrEmpty(folderPath))
                {
                    url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{folderPath}:/children";
                }

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);
                    
                    foreach (var item in data.value)
                    {
                        string name = item.name;
                        if (name.EndsWith(".rfa", StringComparison.OrdinalIgnoreCase))
                        {
                            families.Add(new FamilyItem
                            {
                                Id = item.id,
                                Name = name,
                                DownloadUrl = item["@microsoft.graph.downloadUrl"],
                                LastModified = item.lastModifiedDateTime,
                                // TODO: Parse category from folder path or metadata
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle error
            }

            return families;
        }

        public async Task<string> DownloadFamilyAsync(FamilyItem item, string localPath)
        {
            if (string.IsNullOrEmpty(item.DownloadUrl)) return null;

            using (var response = await _httpClient.GetAsync(item.DownloadUrl))
            {
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    System.IO.File.WriteAllBytes(localPath, bytes);
                    return localPath;
                }
            }
            return null;
        }

        public async Task<byte[]> GetThumbnailAsync(FamilyItem item)
        {
            // TODO: Implement thumbnail fetching from Graph API
            return null;
        }
    }
}
