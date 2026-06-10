# 🗄️ Thiết kế Cơ sở Dữ liệu (Database Schema)

Hệ thống quản lý phòng khám nha khoa sử dụng cơ sở dữ liệu quan hệ (SQL Server hoặc PostgreSQL). Tài liệu này đặc tả cấu trúc bảng, mối quan hệ và các trường thông tin chính.

---

## 🗺️ Sơ đồ mối quan hệ thực thể (ERD Summary)

Các mối quan hệ cốt lõi:
- Một **User** có thể có một hồ sơ **Patient** hoặc **Dentist** tương ứng (mối quan hệ 1-1 hoặc kế thừa).
- Một **Patient** có nhiều **Appointments** (Lịch hẹn).
- Một **Dentist** có nhiều **Appointments** (Lịch hẹn).
- Một **Appointment** có thể sinh ra tối đa một **Invoice** (Hóa đơn) và một **MedicalRecord** (Kết quả khám).
- Một **Patient** có nhiều **MedicalRecords** (Lịch sử bệnh án).

---

## 🗃️ Đặc tả các bảng (Table Specifications)

### 1. Bảng `Users` (Người dùng)
Quản lý tài khoản đăng nhập và thông tin định danh cho toàn bộ hệ thống.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid / UUID | PK | Khóa chính |
| `Username` | VARCHAR(50) | Unique, Not Null | Tên đăng nhập |
| `Email` | VARCHAR(100) | Unique, Not Null | Địa chỉ email |
| `PasswordHash` | VARCHAR(255) | Not Null | Mật khẩu đã mã hóa |
| `Role` | VARCHAR(20) | Not Null | Vai trò: `Admin`, `Dentist`, `Receptionist`, `Patient` |
| `PhoneNumber` | VARCHAR(15) | Nullable | Số điện thoại |
| `IsActive` | BOOLEAN | Default True | Trạng thái tài khoản |
| `CreatedAt` | DateTimeOffset | Not Null | Thời điểm tạo tài khoản |

---

### 2. Bảng `Patients` (Bệnh nhân)
Lưu thông tin cá nhân của bệnh nhân (kết nối 1-1 với tài khoản User nếu họ sử dụng Mobile App).

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid / UUID | PK | Khóa chính |
| `UserId` | Guid / UUID | FK, Nullable | Liên kết tới bảng `Users` |
| `FullName` | NVARCHAR(100) | Not Null | Họ và tên đầy đủ |
| `DateOfBirth` | Date | Not Null | Ngày tháng năm sinh |
| `Gender` | VARCHAR(10) | Not Null | Giới tính: `Male`, `Female`, `Other` |
| `Address` | NVARCHAR(255) | Nullable | Địa chỉ thường trú |
| `MedicalHistory` | NVARCHAR(MAX) | Nullable | Tiền sử bệnh lý (dị ứng thuốc, tim mạch...) |

---

### 3. Bảng `Dentists` (Nha sĩ)
Lưu thông tin hồ sơ của các bác sĩ nha khoa tại phòng khám.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid / UUID | PK | Khóa chính |
| `UserId` | Guid / UUID | FK, Not Null | Liên kết tới tài khoản `Users` |
| `FullName` | NVARCHAR(100) | Not Null | Họ và tên nha sĩ |
| `Specialization`| NVARCHAR(100) | Not Null | Chuyên môn (Chỉnh nha, Cấy ghép...) |
| `ExperienceYears`| INT | Not Null | Số năm kinh nghiệm |
| `Biography` | NVARCHAR(MAX) | Nullable | Giới thiệu bản thân (hiển thị trên web) |

---

### 4. Bảng `Appointments` (Lịch hẹn)
Quản lý lịch hẹn khám chữa bệnh của bệnh nhân với bác sĩ.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid / UUID | PK | Khóa chính |
| `PatientId` | Guid / UUID | FK, Not Null | Liên kết tới `Patients` |
| `DentistId` | Guid / UUID | FK, Not Null | Liên kết tới `Dentists` |
| `AppointmentDate`| DateTimeOffset| Not Null | Ngày giờ hẹn |
| `Status` | VARCHAR(20) | Not Null | Trạng thái: `Pending`, `Confirmed`, `Completed`, `Cancelled` |
| `Notes` | NVARCHAR(500) | Nullable | Ghi chú từ bệnh nhân hoặc lễ tân khi đặt lịch |
| `CreatedAt` | DateTimeOffset | Not Null | Thời điểm đăng ký lịch hẹn |

---

### 5. Bảng `MedicalRecords` (Hồ sơ bệnh án)
Ghi nhận kết quả khám bệnh, chẩn đoán và phương án điều trị của nha sĩ dành cho bệnh nhân.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid / UUID | PK | Khóa chính |
| `PatientId` | Guid / UUID | FK, Not Null | Liên kết tới `Patients` |
| `DentistId` | Guid / UUID | FK, Not Null | Liên kết tới `Dentists` |
| `AppointmentId`| Guid / UUID | FK, Nullable | Liên kết lịch hẹn gốc |
| `Diagnosis` | NVARCHAR(500) | Not Null | Chẩn đoán của bác sĩ |
| `TreatmentPlan`| NVARCHAR(MAX) | Not Null | Kế hoạch điều trị chi tiết |
| `Notes` | NVARCHAR(MAX) | Nullable | Ghi chú thêm (đơn thuốc, lời dặn bác sĩ...) |
| `VisitDate` | Date | Not Null | Ngày thực hiện khám bệnh |

---

### 6. Bảng `Invoices` (Hóa đơn)
Lưu trữ thông tin tài chính, thanh toán của các ca điều trị tại phòng khám.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid / UUID | PK | Khóa chính |
| `AppointmentId`| Guid / UUID | FK, Not Null | Liên kết lịch hẹn liên quan |
| `TotalAmount` | DECIMAL(18,2) | Not Null | Tổng số tiền thanh toán |
| `Status` | VARCHAR(20) | Not Null | Trạng thái: `Unpaid`, `Paid`, `Refunded` |
| `PaymentMethod`| VARCHAR(20) | Not Null | Hình thức: `Cash`, `BankTransfer`, `OnlinePayment` |
| `PaymentDate` | DateTimeOffset| Nullable | Thời gian thực hiện thanh toán |

---

### 7. Bảng `Inventory` (Kho vật tư y tế)
Quản lý các vật liệu tiêu hao, công cụ dụng cụ nha khoa dùng nội bộ trong phòng khám.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid / UUID | PK | Khóa chính |
| `ItemName` | NVARCHAR(100) | Not Null, Unique | Tên vật tư (ví dụ: Thuốc tê, Chỉ nha khoa) |
| `Quantity` | INT | Not Null | Số lượng hiện tại trong kho |
| `Unit` | NVARCHAR(20) | Not Null | Đơn vị tính (Hộp, Chiếc, Ống...) |
| `MinimumStock` | INT | Not Null | Ngưỡng báo động tồn kho tối thiểu |
| `LastUpdated` | DateTimeOffset | Not Null | Thời điểm cập nhật kho gần nhất |

---

## 🛠️ Quy tắc viết Code tầng Domain & Persistence
1. **Sử dụng EF Core Entity Configurations:** Tách biệt cấu hình Fluent API ra khỏi `DbContext`. Mỗi Entity có một file cấu hình riêng kế thừa `IEntityTypeConfiguration<T>` đặt tại tầng `Infrastructure/Persistence/Configurations`.
2. **Khóa ngoại & Navigation Properties:** Phải khai báo tường minh cả trường Id và đối tượng tham chiếu. Ví dụ:
   ```csharp
   public Guid PatientId { get; set; }
   public Patient Patient { get; set; } = null!;
   ```
3. **Mã hóa mật khẩu:** Mật khẩu của `User` phải được hash bằng thuật toán an toàn (ví dụ: BCrypt) trước khi lưu vào database. Tuyệt đối không lưu mật khẩu dạng plain text.
