# 🚀 Hướng dẫn Thiết lập Dự án (Setup Guide)

Tài liệu này hướng dẫn chi tiết cách thiết lập môi trường phát triển cục bộ (local development) cho dự án Hệ thống Quản lý Phòng khám Nha khoa.

---

## 📋 Yêu cầu Hệ thống (Prerequisites)

Trước khi bắt đầu, hãy đảm bảo bạn đã cài đặt các công cụ sau trên máy tính của mình:

| Công cụ | Phiên bản khuyến nghị | Phân hệ sử dụng | Link tải |
| :--- | :--- | :--- | :--- |
| **Git** | 2.40.0 trở lên | Toàn bộ dự án | [Tải Git](https://git-scm.com/) |
| **Docker Desktop** | Mới nhất (hỗ trợ Compose) | Chạy môi trường DB/Nginx | [Tải Docker](https://www.docker.com/products/docker-desktop/) |
| **.NET SDK** | .NET 9.0 SDK | `apps/api` (Backend) | [Tải .NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) |
| **Node.js** | v20.x LTS | `apps/admin_website`, `apps/clinic_website` | [Tải Node.js](https://nodejs.org/) |
| **Flutter SDK** | 3.22.x trở lên (Channel Stable) | `apps/mobile_app` | [Tải Flutter](https://docs.flutter.dev/get-started/install) |

---

## 🛠️ Các bước thiết lập ban đầu (Initial Setup)

### Bước 1: Clone dự án
Mở terminal và clone dự án về máy:
```bash
git clone <url-repo-cua-ban>
cd dental-clinic
```

### Bước 2: Thiết lập cấu hình biến môi trường
Tạo file cấu hình môi trường `.env` từ file ví dụ:
* Mở terminal ở thư mục gốc của dự án:
```bash
copy .env.example .env
```
* Mở file `.env` vừa tạo và điền các thông số kết nối Database, API Key cho dịch vụ thanh toán và AI.

---

## 💻 Hướng dẫn chạy các phân hệ (Running the Apps)

### Cách 1: Chạy toàn bộ hệ thống bằng Docker (Khuyên dùng khi Dev giao diện/App)
Để khởi chạy nhanh cơ sở dữ liệu và các cổng định tuyến (Nginx), chạy lệnh sau tại thư mục gốc:
```bash
docker compose up --build
```
Hệ thống sẽ tự động build và chạy:
- **Web giới thiệu (Next.js):** [http://localhost](http://localhost)
- **Web vận hành nội bộ (Next.js):** [http://localhost/admin](http://localhost/admin)
- **Backend API (.NET 9):** [http://localhost/api](http://localhost/api)
- **Cơ sở dữ liệu (PostgreSQL/SQL Server):** Cổng mặc định theo cấu hình `.env`

---

### Cách 2: Chạy từng phân hệ thủ công (Local Development)

#### 1. Backend API (`apps/api`)
1. Di chuyển vào thư mục API:
   ```bash
   cd apps/api
   ```
2. Khôi phục các thư viện NuGet:
   ```bash
   dotnet restore
   ```
3. Chạy migrations để khởi tạo database:
   ```bash
   dotnet ef database update
   ```
4. Khởi chạy API server:
   ```bash
   dotnet run
   ```
   *Mặc định API sẽ chạy tại: https://localhost:5001 hoặc http://localhost:5000 (Xem chi tiết tại [api-endpoints.md](file:///c:/DentalClinic/dental-clinic/docs/api-endpoints.md))*

#### 2. Trang Web Giới thiệu (`apps/clinic_website`) & Quản lý (`apps/admin_website`)
*(Các bước tương tự nhau đối với cả hai trang web)*
1. Di chuyển vào thư mục tương ứng:
   ```bash
   cd apps/clinic_website
   # Hoặc cd apps/admin_website
   ```
2. Cài đặt các thư viện Node:
   ```bash
   npm install
   ```
3. Khởi chạy ở chế độ dev:
   ```bash
   npm run dev
   ```
   *Next.js sẽ chạy trang web tại: http://localhost:3000 (clinic) và http://localhost:3001 (admin) nếu chạy độc lập.*

#### 3. Mobile App (`apps/mobile_app`)
1. Di chuyển vào thư mục dự án mobile:
   ```bash
   cd apps/mobile_app
   ```
2. Lấy các gói Flutter dependencies:
   ```bash
   flutter pub get
   ```
3. Kiểm tra thiết bị giả lập hoặc cắm máy thật:
   ```bash
   flutter devices
   ```
4. Khởi chạy ứng dụng:
   ```bash
   flutter run
   ```

**Chạy trên điện thoại Android thật qua cáp USB:** `ApiConstants.baseUrl` trỏ tới `http://localhost:5239/api`
cho điện thoại thật (khác với Android Emulator dùng `10.0.2.2`) — `localhost` trên điện thoại chỉ có
nghĩa khi có cầu nối cổng tới máy tính. Trước khi chạy app, luôn chạy lệnh sau (mỗi lần cắm lại cáp
USB / khởi động lại máy tính / mất kết nối adb thì phải chạy lại, vì mapping này không tự lưu):
```bash
adb reverse tcp:5239 tcp:5239
```
Nếu không thấy `adb` trong PATH, dùng đường dẫn đầy đủ tới `platform-tools/adb.exe` trong thư mục
cài Android SDK (thường ở `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`).

---

## ⚠️ Khắc phục một số lỗi thường gặp (Troubleshooting)

1. **Lỗi kết nối cơ sở dữ liệu:**
   Đảm bảo dịch vụ database trong Docker đã khởi động hoàn toàn trước khi chạy lệnh `dotnet run`. Kiểm tra lại chuỗi kết nối `ConnectionStrings` trong `appsettings.json` hoặc `.env`.
2. **Lỗi phiên bản Node.js:**
   Nếu gặp lỗi liên quan đến cấu hình Next.js hoặc ES Modules, hãy chắc chắn bạn đang dùng Node.js v20 LTS. Bạn có thể sử dụng `nvm use 20` để chuyển đổi phiên bản.
3. **Lỗi Cocoapods trên macOS khi chạy Flutter:**
   Nếu chạy trên iOS gặp lỗi pod, hãy chạy lệnh:
   ```bash
   cd ios && pod install --repo-update && cd ..
   ```
4. **Mobile app báo "Lỗi kết nối quá thời gian" / "Không thể kết nối máy chủ" khi chạy trên điện thoại Android thật qua USB:**
   Hầu như luôn là do thiếu (hoặc mất) tunnel `adb reverse`. Chạy lại:
   ```bash
   adb reverse tcp:5239 tcp:5239
   adb reverse --list   # xác nhận thấy "UsbFfs tcp:5239 tcp:5239"
   ```
   Kiểm tra thêm: backend API (`dotnet run` ở `apps/api`) có đang chạy không (`curl http://localhost:5239/api/dentists` phải trả `200`).
