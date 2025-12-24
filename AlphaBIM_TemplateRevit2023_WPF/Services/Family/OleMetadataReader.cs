using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenMcdf;
using System.Xml.Linq;

namespace NTC.FamilyManager.Services.Family
{
    public class OleMetadataReader
    {
        public class FamilyMetadata
        {
            public string Version { get; set; } = "2023";
            public string Category { get; set; }
            public string Discipline { get; set; }
            public string Author { get; set; }
        }

        public FamilyMetadata ReadMetadata(string filePath)
        {
            var metadata = new FamilyMetadata();
            try
            {
                if (!File.Exists(filePath)) return metadata;

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (CompoundFile cf = new CompoundFile(fs))
                {
                    // 1. Read BasicFileInfo
                    try
                    {
                        var infoStream = cf.RootStorage.GetStream("BasicFileInfo");
                        var data = infoStream.GetData();
                        string content = Encoding.Unicode.GetString(data);
                        
                        // Extract Version
                        var vMatch = Regex.Match(content, @"Autodesk Revit 20\d{2}");
                        if (vMatch.Success)
                        {
                            metadata.Version = vMatch.Value.Replace("Autodesk Revit ", "");
                        }

                        // Extract Category
                        var lines = content.Split(new[] { '\0', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            metadata.Category = lines.LastOrDefault(l => l.Length > 3 && !l.Contains(":") && !l.Contains("\\") && !l.Contains("."));
                        }
                    }
                    catch { /* Stream not found */ }

                    // 2. Read PartAtom
                    try
                    {
                        var atomStream = cf.RootStorage.GetStream("PartAtom");
                        var atomData = atomStream.GetData();
                        string xmlContent = Encoding.UTF8.GetString(atomData);
                        
                        int xmlStart = xmlContent.IndexOf("<?xml");
                        if (xmlStart >= 0)
                        {
                            xmlContent = xmlContent.Substring(xmlStart);
                            var doc = XDocument.Parse(xmlContent);
                            var categoryElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "category");
                            if (categoryElement != null && !string.IsNullOrEmpty(categoryElement.Value))
                            {
                                metadata.Category = categoryElement.Value;
                            }
                        }
                    }
                    catch { /* Stream not found or XML error */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OLE METADATA ERROR] {ex.Message}");
            }

            return metadata;
        }
    }
}
