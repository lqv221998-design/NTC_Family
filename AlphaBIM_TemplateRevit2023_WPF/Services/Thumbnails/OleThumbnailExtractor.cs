using System;
using System.IO;
using System.Linq;
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

                using (FileStream fs = new FileStream(rfaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (CompoundFile cf = new CompoundFile(fs))
                {
                    CFStream previewStream = null;
                    
                    // Thử tìm các stream phổ biến chứa ảnh preview
                    string[] possibleNames = { "RevitPreview", "RvtPreview", "Preview", "Contents" };
                    
                    cf.RootStorage.VisitEntries(entry =>
                    {
                        if (entry.IsStream && possibleNames.Any(name => entry.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            previewStream = entry as CFStream;
                        }
                    }, false);

                    if (previewStream != null)
                    {
                        var data = previewStream.GetData();
                        if (data != null && data.Length > 20) // Skip header nếu có
                        {
                            // 1. Kiểm tra PNG: 89 50 4E 47
                            for (int i = 0; i < Math.Min(100, data.Length - 4); i++)
                            {
                                if (data[i] == 0x89 && data[i+1] == 0x50 && data[i+2] == 0x4E && data[i+3] == 0x47)
                                {
                                    byte[] pngData = new byte[data.Length - i];
                                    Buffer.BlockCopy(data, i, pngData, 0, pngData.Length);
                                    File.WriteAllBytes(outputPath, pngData);
                                    return true;
                                }
                            }

                            // 2. Kiểm tra BMP (BM): 42 4D (Nếu không tìm thấy PNG)
                            if (data[0] == 0x42 && data[1] == 0x4D)
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
                System.Diagnostics.Debug.WriteLine($"[OLE THUMB ERROR] {Path.GetFileName(rfaPath)}: {ex.Message}");
            }
            return false;
        }
    }
}
