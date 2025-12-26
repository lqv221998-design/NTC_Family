using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NTC.FamilyManager.Core.Interfaces;
using NTC.FamilyManager.Core.Models;
using NTC.FamilyManager.Infrastructure.Revit;
using NTC.FamilyManager.Services.Naming;
using NTC.FamilyManager.Services.Thumbnails;
using NTC.FamilyManager.Infrastructure.Utilities;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace NTC.FamilyManager.Services.Family
{
    public class FamilyCuratorService : IFamilyCuratorService
    {
        private readonly RevitRequestHandler _revitHandler;
        private readonly ExternalEvent _externalEvent;
        private readonly SmartNameGenerator _smartNamer;
        private readonly OleMetadataReader _oleReader;
        private readonly IThumbnailService _thumbnailService;

        public FamilyCuratorService(RevitRequestHandler revitHandler, ExternalEvent externalEvent, IThumbnailService thumbnailService = null)
        {
            _revitHandler = revitHandler;
            _externalEvent = externalEvent;
            _smartNamer = new SmartNameGenerator();
            _oleReader = new OleMetadataReader();
            _thumbnailService = thumbnailService ?? new ThumbnailService();
        }

        public async Task<FamilyProcessingResult> AnalyzeFamilyAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            string proposedFamilyName = null;
            string category = null;
            string discipline = null;
            bool thumbnailExtracted = false;
            string tempThumbPath = null;

            try 
            {
                var oleData = _oleReader.ReadMetadata(filePath);
                string revitVersion = oleData.Version ?? "2023";
                
                var namingResult = _smartNamer.SuggestName(filePath, oleData.Category, revitVersion);
                proposedFamilyName = namingResult.ProposedName;
                category = namingResult.Category ?? oleData.Category;
                discipline = namingResult.Discipline ?? oleData.Discipline;

                byte[] thumbData = await _thumbnailService.GetThumbnailDataAsync(filePath);
                thumbnailExtracted = thumbData != null;
                return new FamilyProcessingResult
                {
                    OriginalPath = filePath,
                    FamilyName = proposedFamilyName,
                    Category = category ?? "Generic Models",
                    Discipline = discipline ?? "Kiến trúc",
                    Version = revitVersion,
                    ThumbnailData = thumbData,
                    Status = ProcessingStatus.Pending,
                    Message = thumbData == null ? "NTC Scan (Direct)" : "NTC Scan (Direct + Memory)"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] OLE Failed: {ex.Message}");
            }

            var revitTask = _revitHandler.Raise(RevitRequestType.ExtractMetadata, filePath);
            _externalEvent.Raise();
            await revitTask; 
            
            category = _revitHandler.ExtractedCategory ?? "Generic Models";
            string version = _revitHandler.ExtractedVersion ?? "2023";
            discipline = MapCategoryToDiscipline(category);

            if (!thumbnailExtracted)
            {
                 tempThumbPath = _revitHandler.ExtractedThumbnailPath;
            }

            string cleanName = Path.GetFileNameWithoutExtension(filePath).Replace(" ", "_").Replace("-", "_").ToLower();
            string catShort = category.Replace(" ", "").ToLower();
            proposedFamilyName = $"NTC_{catShort}_{cleanName}_v{version}";

            return new FamilyProcessingResult
            {
                OriginalPath = filePath,
                FamilyName = proposedFamilyName,
                Category = category,
                Discipline = discipline ?? "Kiến trúc",
                Version = version,
                ThumbnailPath = tempThumbPath,
                Status = ProcessingStatus.Pending,
                Message = "Analyzed (Revit Fallback)"
            };
        }

        public async Task<bool> CommitStandardizationAsync(FamilyProcessingResult proposal, string destinationRoot = null)
        {
            if (proposal == null) return false;
            
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string ver = proposal.Version ?? "2023";
                    string disc = (proposal.Discipline ?? "Kiến trúc").ToLower().Replace(" ", "_");
                    string cat = (proposal.Category ?? "Generic Models").ToLower().Replace(" ", "_");
                    string familyName = proposal.FamilyName;
                    string targetDir;

                    if (string.IsNullOrEmpty(destinationRoot))
                        targetDir = Path.Combine(Path.GetDirectoryName(proposal.OriginalPath), "Standardized_Library", ver, disc, cat);
                    else
                        targetDir = Path.Combine(destinationRoot, ver, disc, cat);

                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    string targetPath = Path.Combine(targetDir, familyName + ".rfa");
                    File.Copy(proposal.OriginalPath, targetPath, true);
                    
                    if (new FileInfo(targetPath).Length > 0)
                    {
                        try { File.Delete(proposal.OriginalPath); } catch { }
                    }



                    proposal.NewPath = targetPath;
                    proposal.Status = ProcessingStatus.Succeeded;
                    proposal.Message = $"Đã lưu chuẩn V4.2 vào thư mục {disc}/{cat}";
                    return true;
                }
                catch (IOException) when (i < 2)
                {
                    await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    proposal.Status = ProcessingStatus.Failed;
                    proposal.Message = "Lỗi: " + ex.Message;
                    return false;
                }
            }
            return false;
        }



        public Task<bool> CheckDuplicatesAsync(FamilyProcessingResult proposal)
        {
            return Task.FromResult(false); 
        }

        private string MapCategoryToDiscipline(string category)
        {
            if (string.IsNullOrEmpty(category)) return "Kiến trúc";
            var arcCategories = new[] { "Doors", "Windows", "Walls", "Floors", "Roofs", "Stairs", "Furniture", "Casework", "Curtain" };
            var mepCategories = new[] { "Pipes", "Ducts", "Electrical", "Mechanical", "Plumbing", "Lighting", "Fire", "Sprinkler" };
            var strCategories = new[] { "Structural", "Foundation", "Framing", "Rebar", "Connection" };
            string catLower = category.ToLower();
            if (arcCategories.Any(c => catLower.Contains(c.ToLower()))) return "Kiến trúc";
            if (mepCategories.Any(c => catLower.Contains(c.ToLower()))) return "MEP";
            if (strCategories.Any(c => catLower.Contains(c.ToLower()))) return "Kết cấu";
            return "Kiến trúc"; 
        }
    }
}
