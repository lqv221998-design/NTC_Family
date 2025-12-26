using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NTC.FamilyManager.Infrastructure.Caching
{
    public class ThumbnailCacheManager
    {
        private readonly string _cacheDir;

        public ThumbnailCacheManager()
        {
            _cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NTC_Family",
                "Thumbnails"
            );

            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
        }

        public string GetCachePath(string rfaPath)
        {
            FileInfo fi = new FileInfo(rfaPath);
            // Key based on Path and LastWriteTime to detect changes
            string key = $"{fi.FullName}_{fi.LastWriteTimeUtc.Ticks}";
            string fileName = GetHash(key) + ".png";
            return Path.Combine(_cacheDir, fileName);
        }

        public bool Exists(string cachePath)
        {
            return File.Exists(cachePath);
        }

        public void Clear()
        {
            try
            {
                foreach (var file in Directory.GetFiles(_cacheDir))
                {
                    File.Delete(file);
                }
            }
            catch { }
        }

        private string GetHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < 8; i++) // Chỉ cần 8 byte đầu là đủ định danh ngắn gọn
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
