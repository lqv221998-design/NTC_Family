using System.Threading.Tasks;

namespace NTC.FamilyManager.Core.Interfaces
{
    public interface IThumbnailService
    {
        /// <summary>
        /// Lấy dữ liệu ảnh thumbnail (PNG/BMP) của file RFA trực tiếp từ bộ nhớ.
        /// </summary>
        /// <param name="rfaPath">Đường dẫn tệp Revit Family.</param>
        /// <returns>Mảng byte dữ liệu ảnh, hoặc null nếu thất bại.</returns>
        Task<byte[]> GetThumbnailDataAsync(string rfaPath);

        /// <summary>
        /// Dọn dẹp toàn bộ cache ảnh (nếu còn dùng).
        /// </summary>
        void ClearCache();
    }
}
