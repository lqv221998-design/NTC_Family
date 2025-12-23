using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Models;

namespace NTC.FamilyManager.Services.Family
{
    public class LocalFamilyService : IFamilyService
    {
        public async Task<List<FamilyItem>> GetFamiliesAsync(string folderPath)
        {
            return await Task.Run(() =>
            {
                var families = new List<FamilyItem>();
                if (!Directory.Exists(folderPath)) return families;

                var rfaFiles = Directory.GetFiles(folderPath, "*.rfa", SearchOption.AllDirectories);

                foreach (var file in rfaFiles)
                {
                    var fileInfo = new FileInfo(file);
                    var item = new FamilyItem
                    {
                        Id = file,
                        Name = Path.GetFileNameWithoutExtension(file),
                        DownloadUrl = file,
                        LastModified = fileInfo.LastWriteTime,
                        Category = "Doors",
                        RevitVersion = "2021"
                    };

                    string thumbnailPath = Path.ChangeExtension(file, ".png");
                    if (!File.Exists(thumbnailPath))
                        thumbnailPath = Path.ChangeExtension(file, ".jpg");

                    if (File.Exists(thumbnailPath))
                    {
                        item.ThumbnailUrl = thumbnailPath;
                    }

                    families.Add(item);
                }

                return families;
            });
        }
    }
}
