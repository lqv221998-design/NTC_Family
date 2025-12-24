using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Threading;

namespace NTC.FamilyManager.Infrastructure.Utilities
{
    public static class ShellThumbnailProvider
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SIZE
        {
            public int cx;
            public int cy;
        }

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItemImageFactory
        {
            void GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] int flags,
                [Out] out IntPtr phbm);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void SHCreateItemFromParsingName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject([In] IntPtr hObject);

        public static BitmapSource GetThumbnail(string filePath, int width, int height)
        {
            BitmapSource result = null;
            Exception threadEx = null;

            Thread staThread = new Thread(() =>
            {
                IntPtr hBitmap = IntPtr.Zero;
                try
                {
                    Guid guid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                    SHCreateItemFromParsingName(filePath, IntPtr.Zero, guid, out IShellItemImageFactory factory);

                    SIZE size = new SIZE { cx = width, cy = height };
                    
                    // SIIGBF_BIGGERSIZEOK (0x1) | SIIGBF_THUMBNAILONLY (0x8) = 0x9
                    // Sử dụng cờ này để bắt buộc lấy Thumbnail, không lấy Icon.
                    factory.GetImage(size, 0x9, out hBitmap);

                    if (hBitmap != IntPtr.Zero)
                    {
                        result = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        result.Freeze(); // Quan trọng để truyền giữa các thread
                    }
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
                finally
                {
                    if (hBitmap != IntPtr.Zero)
                    {
                        DeleteObject(hBitmap);
                    }
                }
            });

            staThread.SetApartmentState(ApartmentState.STA); // BẮT BUỘC CHO SHELL API
            staThread.Start();
            staThread.Join();

            if (threadEx != null)
            {
                 System.Diagnostics.Debug.WriteLine($"[ShellThumbnail STA Error] {threadEx.Message}");
            }

            return result;
        }
    }
}
