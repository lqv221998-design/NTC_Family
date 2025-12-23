using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Models;

namespace NTC.FamilyManager.Core.Interfaces
{
    public interface IFamilyProcessorService
    {
        /// <summary>
        /// Sự kiện báo cáo tiến độ xử lý (0-100) và message đi kèm.
        /// </summary>
        event Action<int, string> ProgressChanged;

        /// <summary>
        /// Xử lý toàn bộ Family trong một thư mục.
        /// </summary>
        Task<List<FamilyProcessingResult>> ProcessFolderAsync(string sourcePath, string targetRootPath);

        /// <summary>
        /// Xử lý một file Family đơn lẻ.
        /// </summary>
        Task<FamilyProcessingResult> ProcessFileAsync(string filePath, string targetRootPath);
    }
}
