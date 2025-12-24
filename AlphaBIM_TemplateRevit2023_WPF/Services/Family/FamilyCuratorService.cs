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
using Autodesk.Revit.UI;

namespace NTC.FamilyManager.Services.Family
{
    public class FamilyCuratorService : IFamilyCuratorService
    {
        private readonly RevitRequestHandler _revitHandler;
        private readonly ExternalEvent _externalEvent;
        private readonly SmartNameGenerator _smartNamer;
        private readonly OleThumbnailExtractor _oleExtractor;

        public FamilyCuratorService(RevitRequestHandler revitHandler, ExternalEvent externalEvent)
        {
            _revitHandler = revitHandler;
            _externalEvent = externalEvent;
            _smartNamer = new SmartNameGenerator();
            _oleExtractor = new OleThumbnailExtractor();
        }

        public async Task<FamilyProcessingResult> AnalyzeFamilyAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            string tempThumbPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(filePath) + "_3D.png");
            string proposedFamilyName = null;
            string category = null;
            string discipline = null;
            bool thumbnailExtracted = false;

            // --- PHASE 1: INTELLIGENT SPEED (JSON & OLE) ---
            try 
            {
                // 1.1 Try Smart Naming (JSON)
                var namingResult = _smartNamer.SuggestName(filePath);
                if (!string.IsNullOrEmpty(namingResult.ProposedName))
                {
                    proposedFamilyName = namingResult.ProposedName;
                    category = namingResult.Category;
                    discipline = namingResult.Discipline;
                }

                // 1.2 Try Fast Thumbnail (OLE)
                if (_oleExtractor.TryExtractThumbnail(filePath, tempThumbPath))
                {
                    thumbnailExtracted = true;
                }

                // If we have both Name and Thumbnail, we can SKIP Revit API completely!
                if (!string.IsNullOrEmpty(proposedFamilyName) && thumbnailExtracted)
                {
                     return new FamilyProcessingResult
                    {
                        OriginalPath = filePath,
                        FamilyName = proposedFamilyName,
                        Category = category,
                        Discipline = discipline,
                        ThumbnailPath = tempThumbPath,
                        Status = ProcessingStatus.Pending,
                        Message = "AI Detection (Fast Mode)"
                    };
                }
            }
            catch (Exception ex)
            {
                // Log warning but DO NOT STOP. Fallback to Revit.
                System.Diagnostics.Debug.WriteLine($"[WARNING] Fast Mode Failed for {filePath}: {ex.Message}");
            }

            // --- PHASE 2: REVIT FALLBACK (SLOW PATH) ---

            // Nếu thiếu thông tin, mới gọi Revit
            var revitTask = _revitHandler.Raise(RevitRequestType.ExtractMetadata, filePath);
            _externalEvent.Raise();
            
            try 
            {
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

                await revitTask; 
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

            if (string.IsNullOrEmpty(category))
            {
                category = _revitHandler.ExtractedCategory ?? "Generic Models";
                discipline = MapCategoryToDiscipline(category);
            }

            if (!thumbnailExtracted)
            {
                 tempThumbPath = _revitHandler.ExtractedThumbnailPath;
            }

            // Ensure discipline is never null
            if (string.IsNullOrEmpty(discipline)) discipline = "GEN";

            // Generate name if SmartName failed
            if (string.IsNullOrEmpty(proposedFamilyName))
            {
                string cleanName = Path.GetFileNameWithoutExtension(filePath);
                // Remove existing NTC prefix if any to avoid NTC_NTC_...
                cleanName = Regex.Replace(cleanName, @"^NTC_[^_]+_[^_]+_", "", RegexOptions.IgnoreCase);
                proposedFamilyName = $"NTC_{discipline}_{category.Replace(" ", "")}_{cleanName}";
            }

            return new FamilyProcessingResult
            {
                OriginalPath = filePath,
                FamilyName = proposedFamilyName,
                Category = category,
                Discipline = discipline,
                ThumbnailPath = tempThumbPath,
                Status = ProcessingStatus.Pending,
                Message = "Revit Analyzed (Fallback)"
            };
        }

        public async Task<bool> CommitStandardizationAsync(FamilyProcessingResult proposal, string destinationRoot = null)
        {
            // ... (rest of method stays the same, checking for nulls)
            if (proposal == null) return false;
            
            // Retry logic (3 lần) để dập tắt lỗi file bị khóa
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string targetDir;
                    string disc = proposal.Discipline ?? "GEN";
                    string cat = proposal.Category ?? "GenericModels";

                    if (string.IsNullOrEmpty(destinationRoot))
                    {
                        targetDir = Path.Combine(
                            Path.GetDirectoryName(proposal.OriginalPath), 
                            "Standardized", 
                            disc, 
                            cat);
                    }
                    else
                    {
                        targetDir = Path.Combine(destinationRoot, disc, cat);
                    }

                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    string familyName = proposal.FamilyName ?? Path.GetFileNameWithoutExtension(proposal.OriginalPath);
                    string targetPath = Path.Combine(targetDir, familyName + ".rfa");

                    // 1. Di chuyển file RFA
                    if (File.Exists(targetPath)) File.Delete(targetPath);
                    File.Move(proposal.OriginalPath, targetPath);

                    // 2. Đồng bộ Thumbnail
                    string oldThumb = proposal.ThumbnailPath; 
                    if (!string.IsNullOrEmpty(oldThumb) && File.Exists(oldThumb))
                    {
                        string newThumb = Path.Combine(targetDir, familyName + ".png");
                        if (File.Exists(newThumb)) File.Delete(newThumb);
                        File.Copy(oldThumb, newThumb); 
                    }

                    proposal.NewPath = targetPath;
                    proposal.Status = ProcessingStatus.Succeeded;
                    proposal.Message = "Thành công";
                    return true;
                }
                catch (IOException) when (i < 2)
                {
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
            return Task.FromResult(false); 
        }

        private string MapCategoryToDiscipline(string category)
        {
            if (string.IsNullOrEmpty(category)) return "GEN";

            var arcCategories = new[] { "Doors", "Windows", "Walls", "Floors", "Roofs", "Stairs", "Furniture", "Casework", "Curtain" };
            var mepCategories = new[] { "Pipes", "Ducts", "Electrical", "Mechanical", "Plumbing", "Lighting", "Fire", "Sprinkler" };
            var strCategories = new[] { "Structural", "Foundation", "Framing", "Rebar", "Connection" };

            string catLower = category.ToLower();

            if (arcCategories.Any(c => catLower.Contains(c.ToLower()))) return "ARC";
            if (mepCategories.Any(c => catLower.Contains(c.ToLower()))) return "MEP";
            if (strCategories.Any(c => catLower.Contains(c.ToLower()))) return "STR";

            return "GEN"; 
        }
    }
}
