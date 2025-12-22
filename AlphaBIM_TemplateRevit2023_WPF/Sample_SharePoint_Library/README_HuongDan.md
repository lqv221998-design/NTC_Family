# Hướng dẫn cấu trúc thư mục & Metadata (Dành cho Newtecons SharePoint)

Thư mục `Sample_SharePoint_Library` này mô phỏng cách bạn sẽ tổ chức Family trên SharePoint. 

## 1. Cách tổ chức thư mục trên SharePoint
Tuy SharePoint hỗ trợ các Cột (Metadata) mạnh mẽ, nhưng để quản lý thủ công dễ dàng, bạn nên chia thư mục theo **Phiên bản Revit**:

- `Sample_SharePoint_Library/`
    - `2023/` (Thư mục chứa Family bản 2023)
        - `Doors/`
            - `NTC_Cua_Di_4_Canh.rfa`
        - `Furniture/`
            - `NTC_Ghe_Van_Phong.rfa`
    - `2024/` (Thư mục chứa Family bản 2024)
        - `Lighting/`
            - `NTC_Den_Tran.rfa`

## 2. Metadata trên SharePoint là gì? (Lớp thông tin ẩn)

Hãy tưởng tượng thư viện SharePoint của bạn là một **bảng Excel thông minh**. 

- Mỗi file `.rfa` bạn upload lên là một **dòng** trong bảng.
- **Thư mục** (2023, 2024...) chỉ là cách bạn xếp các dòng đó vào các ngăn tủ khác nhau.
- **Metadata (Cột)** là các thông tin bổ sung mà bạn điền vào các cột bên cạnh file đó trên trang web SharePoint.

### Hình ảnh mô phỏng giao diện Web SharePoint:
Khi bạn mở trình duyệt web truy cập SharePoint, bạn sẽ thấy nó hiện ra như thế này:

| Tên File (Hệ thống) | RevitVersion (Cột bạn tự tạo) | FamilyCategory (Cột bạn tự tạo) | FamilyCode |
| :--- | :--- | :--- | :--- |
| 📄 `NTC_Cua_Di_4_Canh.rfa` | **2023** | **Doors** | DR-001 |
| 📄 `NTC_Ghe_Van_Phong.rfa` | **2023** | **Furniture** | FN-012 |
| 📄 `NTC_Den_Tran.rfa` | **2024** | **Lighting** | LT-005 |

### Tại sao phải dùng Metadata thay vì chỉ dùng Thư mục?
1. **Tìm kiếm siêu tốc:** Thay vì Tool phải đi vào từng thư mục mò mẫm, nó chỉ cần hỏi SharePoint: "Cho tôi danh sách các file có cột `RevitVersion = 2023`".
2. **Đa chiều:** Một file chỉ có thể nằm trong 1 thư mục, nhưng nó có thể có nhiều thông tin Metadata (Vừa thuộc 2023, vừa là Doors, vừa là của dự án A).

## 3. Tool AlphaBIM hoạt động như thế nào?
- Khi bạn mở Tool trong Revit, Tool sẽ gửi lệnh qua API để đọc cái **"bảng Excel thông minh"** nói trên.
- Tool sẽ lấy giá trị ở cột `RevitVersion` để hiện vào danh mục bên trái (như hình bạn gửi).
- Tool sẽ lấy giá trị ở cột `FamilyCategory` để hiện vào danh mục phân loại.
## 4. Hướng dẫn tạo cột Metadata (Cực kỳ quan trọng)

Dựa trên hình ảnh bạn vừa gửi, đây là các bước để bạn tạo các cột thông tin mà Tool sẽ đọc:

### Bước 1: Click chọn "+ Add column"
Trên giao diện SharePoint của bạn (ngay bên phải cột *Modified*), hãy nhấn vào chữ **+ Add column**.

### Bước 2: Chọn kiểu dữ liệu "Choice"
Một danh sách hiện ra, bạn hãy chọn dòng **Choice** (để tạo danh sách chọn sẵn cho Version hoặc Category).

### Bước 3: Thiết lập cột "Phiên bản Revit"
- **Name:** Nhập `RevitVersion` (Viết liền không dấu để làm Internal Name).
- **Type:** Choice.
- **Choices:** Nhập danh sách: `2020`, `2021`, `2022`, `2023`, `2024`, `2025`.
- Nhấn **Save**.

### Bước 4: Thiết lập cột "Hạng mục (Category)"
Làm tương tự bước 3:
- **Name:** Nhập `FamilyCategory`.
- **Choices:** Nhập các hạng mục bạn dùng (ví dụ: `Doors`, `Windows`, `Furniture`...).
- Nhấn **Save**.

### Bước 5: Điền thông tin cho file
Sau khi tạo cột xong, bạn chỉ cần click vào file Revit của mình (ví dụ file `Single door_Wood.rfa`), chọn biểu tượng **(i)** (Details) ở góc trên bên phải, sau đó chọn giá trị tương ứng cho các cột vừa tạo.

> [!IMPORTANT]
> **Lưu ý:** Bạn chỉ cần tạo các cột này **MỘT LẦN DUY NHẤT** cho toàn bộ thư viện `NTC_BAN BIM`. Mọi file nằm trong các thư mục con đều sẽ được thừa hưởng các cột này.

## 5. Cách mở quyền cho tất cả thành viên @newtecons.vn

Để mọi người trong công ty có thể dùng Tool tải Family mà bạn không phải thêm tên từng người vào Site, hãy làm theo các bước sau:

### Bước 1: Vào thiết kế phân quyền của Thư viện
1. Tại trang web SharePoint của bạn, nhấn vào biểu tượng **Bánh răng (Settings)** ở góc trên bên phải.
2. Chọn **Library settings** -> chọn **More library settings**.

### Bước 2: Quản lý phân quyền (Permissions)
1. Trong trang cài đặt mới hiện ra, tìm và nhấn vào dòng **Permissions for this document library**.
2. Nhấn vào nút **Grant Permissions** trên thanh công cụ phía trên.

### Bước 3: Thêm nhóm "Mọi người"
1. Trong ô nhập tên, bạn hãy gõ chính xác cụm từ: **`Everyone except external users`** (Đây là nhóm mặc định chứa toàn bộ nhân viên có mail công ty).
2. Nhấn vào **SHOW OPTIONS**.
3. Tại phần *Select a permission level*, hãy chọn **Read** (Chỉ cho phép xem và tải, không cho xóa hay sửa file của bạn).
4. Nhấn **Share**.

> [!TIP]
> **Kết quả:** Bây giờ bất kỳ ai tại Newtecons mở Tool Revit lên đều sẽ thấy và tải được Family, trong khi bạn vẫn giữ được quyền quản lý cao nhất cho Site của mình.
