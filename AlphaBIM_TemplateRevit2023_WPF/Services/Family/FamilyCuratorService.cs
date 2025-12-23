using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Core.Models;
using NTC.FamilyManager.Infrastructure.Revit;

namespace NTC.FamilyManager.Services.Family
{
    public class FamilyCuratorService : IFamilyCuratorService
    {
        private readonly RevitRequestHandler _revitHandler;

        public FamilyCuratorService(RevitRequestHandler revitHandler)
        {
            _revitHandler = revitHandler;
        }

        public async Task<FamilyProcessingResult> AnalyzeFamilyAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            // 1. Yêu cầu Revit trích xuất Category (Chạy đồng bộ qua ExternalEvent)
            _revitHandler.Raise(RevitRequestType.ExtractMetadata, filePath);
            
            // Chờ Revit xử lý xong (Vì đây là ExternalEvent, chúng ta cần đợi IsFinished)
            // Trong thực tế, UI sẽ handle việc chờ này, nhưng ở tầng Service ta có thể check loop
            while (!_revitHandler.IsFinished)
            {
                await Task.Delay(100);
            }

            string category = _revitHandler.ExtractedCategory;
            string discipline = MapCategoryToDiscipline(category);
            string cleanName = Path.GetFileNameWithoutExtension(filePath);
            
            // Xóa tiền tố cũ nếu có
            cleanName = Regex.Replace(cleanName, @"^NTC_[^_]+_[^_]+_", "");

            string proposedName = $"NTC_{discipline}_{category}_{cleanName}";

            return new FamilyProcessingResult
            {
                OriginalPath = filePath,
                FamilyName = proposedName,
                Category = category,
                Discipline = discipline,
                Status = ProcessingStatus.Pending
            };
        }

        public async Task<bool> CommitStandardizationAsync(FamilyProcessingResult proposal)
        {
            try
            {
                string targetDir = Path.Combine(
                    Path.GetDirectoryName(proposal.OriginalPath), 
                    "Standardized", 
                    proposal.Discipline, 
                    proposal.Category);

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string targetPath = Path.Combine(targetDir, proposal.FamilyName + ".rfa");

                // Thực hiện di chuyển file RFA
                File.Move(proposal.OriginalPath, targetPath);

                // Đồng bộ Thumbnail (nếu có file _3D.png được tạo từ RevitRequestHandler)
                string oldThumb = Path.Combine(Path.GetDirectoryName(proposal.OriginalPath), Path.GetFileNameWithoutExtension(proposal.OriginalPath) + "_3D.png");
                if (File.Exists(oldThumb))
                {
                    string newThumb = Path.Combine(targetDir, proposal.FamilyName + ".png");
                    if (File.Exists(newThumb)) File.Delete(newThumb);
                    File.Move(oldThumb, newThumb);
                }

                proposal.NewPath = targetPath;
                proposal.Status = ProcessingStatus.Succeeded;
                return true;
            }
            catch (Exception ex)
            {
                proposal.Status = ProcessingStatus.Failed;
                proposal.Message = ex.Message;
                return false;
            }
        }

        public Task<bool> CheckDuplicatesAsync(FamilyProcessingResult proposal)
        {
            // Logic kiểm tra trùng lặp trong thư mục Standardized
            return Task.FromResult(false); 
        }

        private string MapCategoryToDiscipline(string category)
        {
            var arcCategories = new[] { "Doors", "Windows", "Walls", "Floors", "Roofs", "Stairs" };
            var mepCategories = new[] { "Pipes", "Ducts", "Electrical Fixtures", "Mechanical Equipment" };
            var strCategories = new[] { "Structural Columns", "Structural Foundations", "Structural Framing" };

            if (arcCategories.Any(c => category.Contains(c))) return "ARC";
            if (mepCategories.Any(c => category.Contains(c))) return "MEP";
            if (strCategories.Any(c => category.Contains(c))) return "STR";

            return "GEN"; // General / Generic
        }
    }
}
