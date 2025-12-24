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

                // Revit files are OLE Compound Files
                using (CompoundFile cf = new CompoundFile(rfaPath))
                {
                    // Revit stores preview in a stream named "RevitPreview4.0" or similar
                    // We Iterate root storage to find any stream starting with RevitPreview
                    CFStream previewStream = null;
                    
                    cf.RootStorage.VisitEntries(entry =>
                    {
                        if (entry.IsStream && entry.Name.StartsWith("RevitPreview"))
                        {
                            previewStream = entry as CFStream;
                        }
                    }, false); // Non-recursive visit of root

                    if (previewStream != null)
                    {
                        var data = previewStream.GetData();
                        if (data != null && data.Length > 0)
                        {
                            // The stream is a PNG image (usually) - checks signature
                            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
                            if (data.Length > 8 && 
                                data[0] == 0x89 && data[1] == 0x50 && 
                                data[2] == 0x4E && data[3] == 0x47) 
                            { 
                                File.WriteAllBytes(outputPath, data);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                // File locked by Revit or other process
                System.Diagnostics.Debug.WriteLine($"[OLE LOCK] File invalid or locked: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                // Format error
                System.Diagnostics.Debug.WriteLine($"[OLE ERROR] Extraction failed: {ex.Message}");
            }
            
            return false;
        }
    }
}
