using System;
using System.IO;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Infrastructure.Caching;
using NTC.FamilyManager.Infrastructure.Thumbnails;

namespace NTC.FamilyManager.Services.Thumbnails
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly ThumbnailCacheManager _cacheManager;
        private readonly RawBinaryThumbnailExtractor _binaryExtractor;
        private readonly OleThumbnailExtractor _oleExtractor;

        public ThumbnailService()
        {
            _cacheManager = new ThumbnailCacheManager();
            _binaryExtractor = new RawBinaryThumbnailExtractor();
            _oleExtractor = new OleThumbnailExtractor();
        }

        public async Task<byte[]> GetThumbnailDataAsync(string rfaPath)
        {
            if (string.IsNullOrEmpty(rfaPath) || !File.Exists(rfaPath)) return null;

            try
            {
                // 1. Trích xuất bằng Binary ETL (High Speed) - Ưu tiên trực tiếp từ memory
                byte[] pngBytes = await _binaryExtractor.ExtractPngBytesAsync(rfaPath);
                if (pngBytes != null && pngBytes.Length > 0)
                {
                    return pngBytes;
                }

                // 2. Fallback to OLE Extractor (Memory Mode)
                var oleBytes = _oleExtractor.ExtractThumbnailBytes(rfaPath);
                if (oleBytes != null) return oleBytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThumbnailService Error] {ex.Message}");
            }

            return null;
        }

        public void ClearCache()
        {
            _cacheManager.Clear();
        }
    }
}
