# NTC SharePoint Family Manager (Revit Add-in)

Dự án Revit Add-in chuyên nghiệp dành cho tập đoàn **Newtecons**, giúp quản lý và tải Family trực tiếp từ SharePoint thông qua Microsoft Graph API.

## 🚀 Tính năng chính
- **Multi-targeting:** Hỗ trợ toàn bộ phiên bản Revit từ 2020 đến 2025.
- **Xác thực Doanh nghiệp:** Chỉ cho phép người dùng có email `@newtecons.vn` đăng nhập.
- **Tốc độ tối ưu:** Hệ thống bóc tách metadata từ SharePoint giúp tìm kiếm Family cực nhanh.
- **Giao diện hiện đại:** Thiết kế theo thương hiệu Newtecons, hỗ trợ Responsive và Dark Mode.

## 🛠 Công nghệ sử dụng
- **Ngôn ngữ:** C# / .NET (hỗ trợ cả .NET Framework 4.8 và .NET 8.0).
- **Mô hình:** MVVM (Model-View-ViewModel) tiêu chuẩn Enterprise.
- **API:** 
  - Microsoft Identity Client (MSAL) cho đăng nhập.
  - Microsoft Graph SDK cho tương tác SharePoint.
  - Revit API (Nice3point NuGet).
- **UI:** MahApps.Metro & Material Design In XAML.

## 📂 Cấu trúc thư mục
- `Cmds/`: Chứa các lệnh thực thi External Command của Revit.
- `ViewModels/`: Logic xử lý dữ liệu cho giao diện.
- `Views/`: Thiết kế giao diện (XAML).
- `Base/`: Các lớp nền tảng (ViewModelBase, AssemblyLoader).
- `Lib/`: Các thư viện DLL nội bộ phục vụ dự án.
- `Sample_SharePoint_Library/`: Tài liệu hướng dẫn cấu hình SharePoint.

## 🔨 Hướng dẫn Build
Dự án sử dụng SDK-style, bạn có thể build trực tiếp bằng `dotnet CLI`:

```bash
# Build cho Revit 2023
dotnet build -c D2023

# Build cho Revit 2025 (.NET 8)
dotnet build -c D2025
```

## 📝 Liên hệ hỗ trợ
- **Bộ phận:** BIM Department - Newtecons.
- **Email:** bim@newtecons.vn
