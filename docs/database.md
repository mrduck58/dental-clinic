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

---

## 🆕 Cập nhật gần đây (2026-07-19) — Nhóm bảng phục vụ mobile app

> Lưu ý: các bảng số 1–7 ở trên là tài liệu thiết kế ban đầu, đã không còn khớp hoàn toàn với schema
> thật hiện tại (VD: `MedicalRecords` đã được tách thành `Diagnoses`/`TreatmentPlans`/`Prescriptions`
> qua các migration sau này). Phần dưới đây chỉ ghi lại các thay đổi schema thật đã thêm trong đợt
> này — tham khảo `apps/api/src/Infrastructure/Persistence/Migrations/` để biết trạng thái chính xác.

### Bảng `DentistReviews` (mới — migration `AddDentistReviews`)
Đánh giá của bệnh nhân dành cho nha sĩ, thay cho hệ thống review mock trong bộ nhớ ở mobile app cũ.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `Id` | Guid | PK | Khóa chính |
| `DentistId` | Guid | FK → `Dentists`, Not Null | Nha sĩ được đánh giá |
| `PatientId` | Guid | FK → `Patients`, Not Null | Bệnh nhân đánh giá |
| `Rating` | int | Not Null (1–5) | Số sao |
| `Comment` | varchar(2000) | Not Null | Nội dung đánh giá |
| `TagsCsv` | varchar(500) | Nullable | Nhãn nổi bật do bệnh nhân chọn, lưu dạng CSV (VD: `"Không đau,Chuyên nghiệp"`) — expose qua property `Tags` (list, không map cột) |
| `CreatedAt` / `UpdatedAt` | DateTimeOffset | Not Null | |

Ràng buộc: unique index `(DentistId, PatientId)` — mỗi bệnh nhân chỉ có 1 đánh giá/nha sĩ (gửi lại
sẽ `UPDATE`, không `INSERT` mới). Validate ở tầng handler: chỉ được tạo nếu bệnh nhân đã có buổi
khám `Completed`/`PendingPayment` với nha sĩ đó.

### Bảng `PrescriptionItems` — 3 cột mới (migration `AddPrescriptionItemReminderFields`)
Bổ sung dữ liệu có cấu trúc để mobile sinh lịch nhắc uống thuốc thật, thay vì hiển thị lịch giả cố định.

| Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `TimesPerDay` | int | Nullable | Số lần uống/ngày — bác sĩ nhập ở trang kê đơn (admin_website). Để trống nếu tần suất không cố định (VD: "khi đau") |
| `DurationDays` | int | Nullable | Số ngày dùng thuốc |
| `StartDate` | date | Nullable | Ngày bắt đầu uống, mặc định = ngày kê đơn |

Chỉ khi **cả 3 cột đều có giá trị** thì `GetMedicationRemindersHandler` mới sinh nhắc nhở cho dòng
thuốc đó (xem `docs/api-endpoints.md` mục 8) — tránh suy đoán lịch từ dữ liệu thiếu.

---

## 🛠️ Quy tắc viết Code tầng Domain & Persistence
1. **Sử dụng EF Core Entity Configurations:** Tách biệt cấu hình Fluent API ra khỏi `DbContext`. Mỗi Entity có một file cấu hình riêng kế thừa `IEntityTypeConfiguration<T>` đặt tại tầng `Infrastructure/Persistence/Configurations`.
2. **Khóa ngoại & Navigation Properties:** Phải khai báo tường minh cả trường Id và đối tượng tham chiếu. Ví dụ:
   ```csharp
   public Guid PatientId { get; set; }
   public Patient Patient { get; set; } = null!;
   ```
3. **Mã hóa mật khẩu:** Mật khẩu của `User` phải được hash bằng thuật toán an toàn (ví dụ: BCrypt) trước khi lưu vào database. Tuyệt đối không lưu mật khẩu dạng plain text.
