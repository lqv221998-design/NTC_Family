using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NTC.FamilyManager.Services.Auth.Helpers
{
    public static class TokenCacheHelper
    {
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NTC_FamilyManager",
            "token_cache.dat");

        public static void SaveToken(string token)
        {
            try
            {
                var directory = Path.GetDirectoryName(CacheFilePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                byte[] data = Encoding.UTF8.GetBytes(token);
                byte[] encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(CacheFilePath, encryptedData);
            }
            catch { /* Ignore storage errors in this phase */ }
        }

        public static string LoadToken()
        {
            try
            {
                if (!File.Exists(CacheFilePath)) return null;

                byte[] encryptedData = File.ReadAllBytes(CacheFilePath);
                byte[] data = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCache()
        {
            if (File.Exists(CacheFilePath))
            {
                try { File.Delete(CacheFilePath); } catch { }
            }
        }
    }
}
