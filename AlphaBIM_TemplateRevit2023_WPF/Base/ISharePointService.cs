using System.Collections.Generic;
using System.Threading.Tasks;
using NTC.FamilyManager.Models;

namespace NTC.FamilyManager.Base
{
    public interface ISharePointService
    {
        Task<List<FamilyItem>> FetchFamiliesAsync(string siteId, string driveId, string folderPath = "");
        Task<string> DownloadFamilyAsync(FamilyItem item, string localPath);
        Task<byte[]> GetThumbnailAsync(FamilyItem item);
    }
}
