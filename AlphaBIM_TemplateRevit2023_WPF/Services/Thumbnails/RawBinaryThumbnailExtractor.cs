using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NTC.FamilyManager.Services.Thumbnails
{
    public class RawBinaryThumbnailExtractor
    {
        // PNG Signature: 89 50 4E 47 0D 0A 1A 0A
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        // PNG IEND Chunk: 49 45 4E 44
        private static readonly byte[] PngEndChunk = { 0x49, 0x45, 0x4E, 0x44 };

        public bool ExtractThumbnail(string rfaPath, string outputCachePath)
        {
            try
            {
                if (!File.Exists(rfaPath)) return false;

                // Nếu cache đã tồn tại và mới hơn file gốc thì dùng luôn
                if (File.Exists(outputCachePath))
                {
                    var rfaTime = File.GetLastWriteTimeUtc(rfaPath);
                    var cacheTime = File.GetLastWriteTimeUtc(outputCachePath);
                    if (cacheTime >= rfaTime) return true;
                }

                // Đọc file với buffer lớn để quét nhanh
                using (var fs = new FileStream(rfaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Revit file thường lưu preview ở phần đầu hoặc header stream.
                    // Tuy nhiên, để chắc chắn, ta quét toàn bộ nhưng tối ưu bằng buffer.
                    // Giới hạn quét: 30MB đầu tiên (thường preview nằm ở đầu OLE)
                    long scanLimit = Math.Min(fs.Length, 30 * 1024 * 1024); 
                    byte[] buffer = new byte[81920]; // 80KB buffer
                    int bytesRead;
                    long totalRead = 0;

                    long pngStart = -1;
                    
                    while (totalRead < scanLimit && (bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Tìm PNG Signature
                        int foundIndex = FindSequence(buffer, 0, bytesRead, PngSignature);
                        if (foundIndex != -1)
                        {
                            pngStart = totalRead + foundIndex;
                            break;
                        }
                        totalRead += bytesRead;
                        // Backtrack để tránh việc signature bị cắt đôi giữa 2 buffer
                        long backtrack = PngSignature.Length;
                        if (fs.Position > backtrack)
                        {
                            fs.Seek(-backtrack, SeekOrigin.Current);
                            totalRead -= backtrack;
                        }
                    }

                    if (pngStart != -1)
                    {
                        return ExtractBytes(fs, pngStart, outputCachePath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RawBinary ERROR] {ex.Message}");
            }
            return false;
        }

        private bool ExtractBytes(FileStream fs, long startPos, string outputPath)
        {
            try
            {
                fs.Seek(startPos, SeekOrigin.Begin);
                using (var outFile = new FileStream(outputPath, FileMode.Create))
                {
                    // Copy cho đến khi gặp IEND
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    bool endFound = false;

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        int endIdx = FindSequence(buffer, 0, bytesRead, PngEndChunk);
                        if (endIdx != -1)
                        {
                            // Ghi phần còn lại bao gồm IEND + 4 byte CRC
                            int writeLen = endIdx + PngEndChunk.Length + 4; 
                            if (writeLen > bytesRead) writeLen = bytesRead; // Safety header check
                            
                            outFile.Write(buffer, 0, writeLen);
                            endFound = true;
                            break;
                        }
                        outFile.Write(buffer, 0, bytesRead);
                    }
                    
                    if (!endFound) return false; // File ảnh bị cắt cụt
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private int FindSequence(byte[] buffer, int offset, int count, byte[] sequence)
        {
            if (count < sequence.Length) return -1;
            
            for (int i = offset; i <= offset + count - sequence.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (buffer[i + j] != sequence[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }
}
