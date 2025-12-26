using System;
using System.IO;
using System.Linq;
using OpenMcdf;

namespace NTC.FamilyManager.Services.Thumbnails
{
    public class OleThumbnailExtractor
    {
        public byte[] ExtractThumbnailBytes(string rfaPath)
        {
            try
            {
                if (!File.Exists(rfaPath)) return null;

                using (FileStream fs = new FileStream(rfaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (CompoundFile cf = new CompoundFile(fs))
                {
                    CFStream previewStream = null;
<<<<<<< HEAD
                    string[] possibleNames = { "RevitPreview", "RvtPreview", "Preview", "Contents" };
                    
                    cf.RootStorage.VisitEntries(entry =>
=======
                    
                    // Ưu tiên 1: Tìm đích danh RevitPreview4.0 (Chuẩn Revit 2013+)
                    try 
>>>>>>> e676abede778d6f026810cd786f691f51b1d8d5d
                    {
                        previewStream = cf.RootStorage.GetStream("RevitPreview4.0");
                    }
                    catch 
                    {
                        // Không tìm thấy đích danh, chuyển sang quét
                    }

                    // Ưu tiên 2: Quét các stream có tên chứa Preview
                    if (previewStream == null)
                    {
                        string[] possibleNames = { "RevitPreview", "RvtPreview", "Preview" };
                        cf.RootStorage.VisitEntries(entry =>
                        {
                            if (previewStream == null && entry.IsStream && possibleNames.Any(name => entry.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                previewStream = entry as CFStream;
                            }
                        }, false);
                    }

                    if (previewStream != null)
                    {
                        var data = previewStream.GetData();
<<<<<<< HEAD
                        if (data != null && data.Length > 20)
                        {
                            for (int i = 0; i < Math.Min(100, data.Length - 4); i++)
=======
                        if (data != null && data.Length > 8)
                        {
                            // PNG Signature: 89 50 4E 47 0D 0A 1A 0A
                            for (int i = 0; i < Math.Min(100, data.Length - 8); i++)
>>>>>>> e676abede778d6f026810cd786f691f51b1d8d5d
                            {
                                if (data[i] == 0x89 && data[i+1] == 0x50 && data[i+2] == 0x4E && data[i+3] == 0x47)
                                {
                                    byte[] pngData = new byte[data.Length - i];
                                    Buffer.BlockCopy(data, i, pngData, 0, pngData.Length);
                                    return pngData;
                                }
                            }
<<<<<<< HEAD
                            if (data[0] == 0x42 && data[1] == 0x4D) return data;
=======
>>>>>>> e676abede778d6f026810cd786f691f51b1d8d5d
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public bool TryExtractThumbnail(string rfaPath, string outputPath)
        {
            var data = ExtractThumbnailBytes(rfaPath);
            if (data != null)
            {
                File.WriteAllBytes(outputPath, data);
                return true;
            }
            return false;
        }
    }
}
