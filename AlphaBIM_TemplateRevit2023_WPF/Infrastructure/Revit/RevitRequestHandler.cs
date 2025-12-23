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
        public bool IsFinished { get; private set; } = true;

        public void Raise(RevitRequestType type, string path)
        {
            RequestType = type;
            FamilyPath = path;
            IsFinished = false;
        }

        public void Execute(UIApplication app)
        {
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
            }
            finally
            {
                IsFinished = true;
                RequestType = RevitRequestType.None;
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
                // Open family file invisibly
                Document familyDoc = app.Application.OpenDocumentFile(FamilyPath);
                if (familyDoc != null && familyDoc.IsFamilyDocument)
                {
                    ExtractedCategory = familyDoc.OwnerFamily.FamilyCategory.Name;
                    
                    // Lấy Thumbnail 3D thực tế
                    Export3DThumbnail(familyDoc);

                    familyDoc.Close(false);
                }
            }
            catch (Exception)
            {
                ExtractedCategory = "Unknown";
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
            }
            catch { /* Ignore if fails */ }
        }

        public string GetName() => "NTC Family Manager Handler";
    }
}
