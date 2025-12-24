using System;
using System.IO;
using OpenMcdf;

namespace NTC.FamilyManager.Services.Thumbnails
{
    public class OleThumbnailExtractor
    {
        public bool TryExtractThumbnail(string rfaPath, string outputPath)
        {
            try
            {
                if (!File.Exists(rfaPath)) return false;

                // Sử dụng FileStream với FileShare.ReadWrite để vừa hỗ trợ Unicode vừa tránh Lock file
                using (FileStream fs = new FileStream(rfaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (CompoundFile cf = new CompoundFile(fs))
                {
                    CFStream previewStream = null;
                    cf.RootStorage.VisitEntries(entry =>
                    {
                        if (entry.IsStream && entry.Name.StartsWith("RevitPreview"))
                        {
                            previewStream = entry as CFStream;
                        }
                    }, false);

                    if (previewStream != null)
                    {
                        var data = previewStream.GetData();
                        if (data != null && data.Length > 8)
                        {
                            // Kiểm tra chữ ký PNG: 89 50 4E 47
                            if (data[0] == 0x89 && data[1] == 0x50 && 
                                data[2] == 0x4E && data[3] == 0x47) 
                            { 
                                File.WriteAllBytes(outputPath, data);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OLE ERROR] {Path.GetFileName(rfaPath)}: {ex.Message}");
            }
            return false;
        }
    }
}
