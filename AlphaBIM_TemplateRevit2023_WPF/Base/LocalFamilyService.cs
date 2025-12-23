using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NTC.FamilyManager.Models;

namespace NTC.FamilyManager.Base
{
    public interface IFamilyService
    {
        Task<List<FamilyItem>> GetFamiliesAsync(string folderPath);
    }

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
                        Id = file, // Use full path as ID for local
                        Name = Path.GetFileNameWithoutExtension(file),
                        DownloadUrl = file, // Local path
                        LastModified = fileInfo.LastWriteTime,
                        Category = "Doors", // Hardcoded for now based on folder structure or from folder name
                        RevitVersion = "2021" // Hardcoded for now based on folder structure
                    };

                    // Try to find thumbnail (same name as rfa)
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
