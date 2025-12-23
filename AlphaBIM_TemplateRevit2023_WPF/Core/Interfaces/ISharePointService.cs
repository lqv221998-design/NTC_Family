using System.Collections.Generic;
using System.Threading.Tasks;
using NTC.FamilyManager.Models;

namespace NTC.FamilyManager.Core.Interfaces
{
    public interface ISharePointService
    {
        Task<List<FamilyItem>> FetchFamiliesAsync(string siteId, string driveId, string folderPath = "");
        Task<string> DownloadFamilyAsync(FamilyItem item, string localPath);
        Task<byte[]> GetThumbnailAsync(FamilyItem item);
    }
}
