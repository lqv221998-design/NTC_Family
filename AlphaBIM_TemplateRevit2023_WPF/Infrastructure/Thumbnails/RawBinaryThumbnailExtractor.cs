using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NTC.FamilyManager.Infrastructure.Thumbnails
{
    public class RawBinaryThumbnailExtractor
    {
        // PNG Header Signature: 89 50 4E 47 0D 0A 1A 0A
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47 };

        public async Task<byte[]> ExtractPngBytesAsync(string rfaPath)
        {
            try
            {
                if (!File.Exists(rfaPath)) return null;

                using (var fs = new FileStream(rfaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Revit thường để Preview ở đầu file (OLE Container). 
                    // Ta chỉ quét 30MB đầu tiên để đảm bảo tốc độ.
                    long lengthToRead = Math.Min(fs.Length, 30 * 1024 * 1024);
                    byte[] buffer = new byte[lengthToRead];
                    await fs.ReadAsync(buffer, 0, (int)lengthToRead);

                    int index = FindSignature(buffer, PngSignature);
                    if (index != -1)
                    {
                        // Tìm thấy "89 50 4E 47", ta lấy từ đó đến hết hoặc đến khi gặp ký hiệu kết thúc PNG (IEND)
                        // Tuy nhiên đơn giản nhất là lấy một đoạn đủ lớn, WPF Image sẽ tự xử lý nếu dư byte rác phía sau
                        // Hoặc chính xác hơn là tìm IEND: 49 45 4E 44 AE 42 60 82
                        int endIndex = FindSignature(buffer, new byte[] { 0x49, 0x45, 0x4E, 0x44 }, index);
                        
                        int pngLength;
                        if (endIndex != -1)
                        {
                            pngLength = (endIndex + 8) - index;
                        }
                        else
                        {
                            // Nếu không tìm thấy IEND, lấy phần còn lại của buffer đã đọc
                            pngLength = buffer.Length - index;
                        }

                        byte[] pngBytes = new byte[pngLength];
                        Buffer.BlockCopy(buffer, index, pngBytes, 0, pngLength);
                        return pngBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Binary ETL Error] {ex.Message}");
            }
            return null;
        }

        private int FindSignature(byte[] buffer, byte[] signature, int startIndex = 0)
        {
            for (int i = startIndex; i <= buffer.Length - signature.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < signature.Length; j++)
                {
                    if (buffer[i + j] != signature[j])
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
