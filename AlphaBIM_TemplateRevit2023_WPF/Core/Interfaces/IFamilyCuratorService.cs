using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Models;

namespace NTC.FamilyManager.Core.Interfaces
{
    public interface IFamilyCuratorService
    {
        /// <summary>
        /// Phân tích file và đưa ra đề xuất Tên/Đường dẫn.
        /// </summary>
        Task<FamilyProcessingResult> AnalyzeFamilyAsync(string filePath);

        /// <summary>
        /// Thực thi việc chuẩn hóa (Rename/Move) sau khi Admin phê duyệt.
        /// </summary>
        Task<bool> CommitStandardizationAsync(FamilyProcessingResult proposal, string destinationRoot = null);

        /// <summary>
        /// Kiểm tra xem tên hoặc metadata đã tồn tại trong thư viện chưa.
        /// </summary>
        Task<bool> CheckDuplicatesAsync(FamilyProcessingResult proposal);
    }
}
