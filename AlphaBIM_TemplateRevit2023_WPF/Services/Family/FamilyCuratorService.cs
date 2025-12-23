using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Core.Models;
using NTC.FamilyManager.Infrastructure.Revit;
using Autodesk.Revit.UI;

namespace NTC.FamilyManager.Services.Family
{
    public class FamilyCuratorService : IFamilyCuratorService
    {
        private readonly RevitRequestHandler _revitHandler;
        private readonly ExternalEvent _externalEvent;

        public FamilyCuratorService(RevitRequestHandler revitHandler, ExternalEvent externalEvent)
        {
            _revitHandler = revitHandler;
            _externalEvent = externalEvent;
        }

        public async Task<FamilyProcessingResult> AnalyzeFamilyAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            // 1. Gửi yêu cầu tới Revit và await Task (Elite Pattern)
            var revitTask = _revitHandler.Raise(RevitRequestType.ExtractMetadata, filePath);
            _externalEvent.Raise();
            
            try 
            {
                // Timeout 30s cho mỗi file
                if (await Task.WhenAny(revitTask, Task.Delay(30000)) != revitTask)
                {
                    return new FamilyProcessingResult
                    {
                        OriginalPath = filePath,
                        FamilyName = Path.GetFileNameWithoutExtension(filePath),
                        Category = "Timeout",
                        Status = ProcessingStatus.Failed,
                        Message = "Revit không phản hồi sau 30 giây."
                    };
                }

                await revitTask; // Đảm bảo bắt được exception nếu có
            }
            catch (Exception ex)
            {
                return new FamilyProcessingResult
                {
                    OriginalPath = filePath,
                    FamilyName = Path.GetFileNameWithoutExtension(filePath),
                    Category = "Error",
                    Status = ProcessingStatus.Failed,
                    Message = $"Lỗi Revit: {ex.Message}"
                };
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
                ThumbnailPath = _revitHandler.ExtractedThumbnailPath,
                Status = ProcessingStatus.Pending
            };
        }

        public async Task<bool> CommitStandardizationAsync(FamilyProcessingResult proposal)
        {
            // Retry logic (3 lần) để dập tắt lỗi file bị khóa
            for (int i = 0; i < 3; i++)
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

                    // 1. Di chuyển file RFA
                    if (File.Exists(targetPath)) File.Delete(targetPath);
                    File.Move(proposal.OriginalPath, targetPath);

                    // 2. Đồng bộ Thumbnail
                    string oldThumb = Path.Combine(Path.GetDirectoryName(proposal.OriginalPath), Path.GetFileNameWithoutExtension(proposal.OriginalPath) + "_3D.png");
                    if (File.Exists(oldThumb))
                    {
                        string newThumb = Path.Combine(targetDir, proposal.FamilyName + ".png");
                        if (File.Exists(newThumb)) File.Delete(newThumb);
                        File.Move(oldThumb, newThumb);
                    }

                    proposal.NewPath = targetPath;
                    proposal.Status = ProcessingStatus.Succeeded;
                    proposal.Message = "Thành công";
                    return true;
                }
                catch (IOException ex) when (i < 2)
                {
                    // Đợi 1 giây rồi thử lại nếu file bị khóa
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    proposal.Status = ProcessingStatus.Failed;
                    proposal.Message = ex.Message;
                    return false;
                }
            }
            return false;
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
