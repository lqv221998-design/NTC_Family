using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace NTC.FamilyManager.Infrastructure.Revit
{
    public enum RevitRequestType
    {
        None,
        LoadFamily,
        ExtractMetadata
    }

    public class RevitRequestHandler : IExternalEventHandler
    {
        public RevitRequestType RequestType { get; set; } = RevitRequestType.None;
        public string FamilyPath { get; set; }
        public string ExtractedCategory { get; private set; }
        public string ExtractedThumbnailPath { get; private set; }
        private volatile bool _isFinished = true;
        public bool IsFinished 
        { 
            get => _isFinished; 
            private set => _isFinished = value; 
        }

        private System.Threading.Tasks.TaskCompletionSource<bool> _tcs;
        private readonly object _lock = new object();

        public System.Threading.Tasks.Task<bool> Raise(RevitRequestType type, string path)
        {
            lock (_lock)
            {
                RequestType = type;
                FamilyPath = path;
                _tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                IsFinished = false;
                return _tcs.Task;
            }
        }

        public void Execute(UIApplication app)
        {
            app.Application.FailuresProcessing += OnFailuresProcessing;
            try
            {
                switch (RequestType)
                {
                    case RevitRequestType.LoadFamily:
                        ExecuteLoadFamily(app);
                        break;
                    case RevitRequestType.ExtractMetadata:
                        ExecuteExtractMetadata(app);
                        break;
                }
                _tcs?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _tcs?.TrySetException(ex);
            }
            finally
            {
                app.Application.FailuresProcessing -= OnFailuresProcessing;
                IsFinished = true;
                RequestType = RevitRequestType.None;
            }
        }

        private void OnFailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor failuresAccessor = e.GetFailuresAccessor();
            var failList = failuresAccessor.GetFailureMessages();
            if (failList.Count == 0) return;

            foreach (var failure in failList)
            {
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
                else
                {
                    e.SetProcessingResult(FailureProcessingResult.ProceedWithRollBack);
                }
            }
        }

        private void ExecuteLoadFamily(UIApplication app)
        {
            if (string.IsNullOrEmpty(FamilyPath)) return;
            Document doc = app.ActiveUIDocument.Document;

            using (Transaction trans = new Transaction(doc, "Load Family"))
            {
                trans.Start();
                doc.LoadFamily(FamilyPath, out _);
                trans.Commit();
            }
        }

        private void ExecuteExtractMetadata(UIApplication app)
        {
            if (string.IsNullOrEmpty(FamilyPath)) return;
            
            try
            {
                // 1. Cố gắng trích xuất Category bằng PartAtom (siêu tốc, không mở file)
                string tempXml = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");
                try 
                {
                    app.Application.ExtractPartAtomFromFamilyFile(FamilyPath, tempXml);
                    if (File.Exists(tempXml))
                    {
                        string xmlContent = File.ReadAllText(tempXml);
                        var match = System.Text.RegularExpressions.Regex.Match(xmlContent, @"<category>(.*?)</category>");
                        if (match.Success)
                        {
                            ExtractedCategory = match.Groups[1].Value;
                        }
                        File.Delete(tempXml);
                    }
                }
                catch { /* Fallback to opening file if PartAtom fails */ }

                // 2. Mở file để lấy Thumbnail (chỉ khi thực sự cần thiết)
                // Dùng OpenOptions để chặn các cảnh báo
                OpenOptions opt = new OpenOptions();
                ModelPath mPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(FamilyPath);
                
                Document familyDoc = app.Application.OpenDocumentFile(mPath, opt);
                if (familyDoc != null)
                {
                    try
                    {
                        if (familyDoc.IsFamilyDocument)
                        {
                            if (string.IsNullOrEmpty(ExtractedCategory))
                                ExtractedCategory = familyDoc.OwnerFamily?.FamilyCategory?.Name ?? "General";
                        
                            Export3DThumbnail(familyDoc);
                        }
                    }
                    finally
                    {
                        familyDoc.Close(false);
                    }
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ExtractedCategory)) ExtractedCategory = "Unknown";
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private void Export3DThumbnail(Document familyDoc)
        {
            try
            {
                // Tìm View 3D mặc định
                View3D view3D = new FilteredElementCollector(familyDoc)
                    .OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .FirstOrDefault(v => !v.IsTemplate);

                if (view3D == null) return;

                string folder = Path.GetDirectoryName(FamilyPath);
                string fileName = Path.GetFileNameWithoutExtension(FamilyPath);
                string thumbPath = Path.Combine(folder, fileName + "_3D");

                ImageExportOptions opt = new ImageExportOptions
                {
                    FilePath = thumbPath,
                    ImageResolution = ImageResolution.DPI_150, // Giảm DPI để tăng tốc
                    ExportRange = ExportRange.SetOfViews,
                    HLRandWFViewsFileType = ImageFileType.PNG,
                    PixelSize = 512, // Giảm size thumbnail
                    ShouldCreateWebSite = false
                };
                opt.SetViewsAndSheets(new List<ElementId> { view3D.Id });

                familyDoc.ExportImage(opt);
                ExtractedThumbnailPath = thumbPath + ".png";
            }
            catch { /* Ignore if fails */ }
        }

        public string GetName() => "NTC Family Manager Handler";
    }
}
