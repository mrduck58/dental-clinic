<div align="center">

# 🦷 Dental Clinic System

**Hệ thống quản lý phòng khám nha khoa toàn diện**

Website giới thiệu · Web vận hành nội bộ · Mobile App bệnh nhân

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js)](https://nextjs.org/)
[![Flutter](https://img.shields.io/badge/Flutter-3.22-02569B?logo=flutter)](https://flutter.dev/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)

</div>

---

## 📋 Tổng quan dự án

Hệ thống được chia thành **4 phân hệ** hoạt động độc lập, giao tiếp qua REST API, triển khai qua Docker:

| Phân hệ | Công nghệ | Mô tả | Cổng |
| :--- | :---: | :--- | :---: |
| `apps/api` | .NET 9 | Backend API (Clean Architecture) | `localhost/api` |
| `apps/clinic_website` | Next.js 16 | Website giới thiệu, quảng cáo phòng khám | `localhost/` |
| `apps/admin_website` | Next.js 16 | Hệ thống vận hành nội bộ (lịch hẹn, bệnh án, kho) | `localhost/admin` |
| `apps/mobile_app` | Flutter 3 | App đặt lịch, xem hồ sơ, thanh toán, hỏi đáp AI | — |

---

## 🗂️ Cấu trúc thư mục

```
dental-clinic/
├── apps/
│   ├── api/                   # Backend .NET 9 (Clean Architecture)
│   │   └── src/
│   │       ├── Domain/        # Entities, Enums, Interfaces
│   │       ├── Application/   # UseCases, DTOs, Validators
│   │       ├── Infrastructure/# EF Core, Repositories, External Services
│   │       └── Presentation/  # Controllers, Middlewares
│   ├── clinic_website/        # Next.js — Web giới thiệu phòng khám
│   ├── admin_website/         # Next.js — Web vận hành nội bộ
│   └── mobile_app/            # Flutter — App bệnh nhân
│       └── lib/
│           ├── app/           # GoRouter, App widget gốc, MainShell, SettingsManager
│           ├── core/          # Constants (API, màu), Network (Dio client), Utils
│           └── features/      # Feature-First: appointment, auth, booking, home,
│                              #   payment, profile — mỗi feature có data/ + presentation/
├── nginx/
│   └── nginx.conf             # Reverse proxy định tuyến traffic
├── docs/                      # Tài liệu kỹ thuật cho team
├── .github/                   # PR Template, Issue Templates
├── docker-compose.yml
└── .env.example
```

---

## ⚙️ Yêu cầu hệ thống

| Công cụ | Phiên bản |
| :--- | :--- |
| Git | 2.40+ |
| Docker Desktop | Mới nhất |
| .NET SDK | 9.0 |
| Node.js | 20 LTS |
| Flutter SDK | 3.22+ (Stable) |

---

## 🚀 Khởi chạy nhanh

```bash
# 1. Clone dự án
git clone https://github.com/mrduck58/dental-clinic.git
cd dental-clinic

# 2. Tạo file cấu hình môi trường
copy .env.example .env
# → Mở file .env và điền các thông số (Supabase URL, JWT Secret...)

# 3. Khởi chạy toàn bộ hệ thống
docker compose up --build
```

### Sau khi khởi chạy, truy cập tại:

| Địa chỉ | Mô tả |
| :--- | :--- |
| http://localhost | Website giới thiệu phòng khám |
| http://localhost/admin | Hệ thống vận hành nội bộ |
| http://localhost/api | Backend REST API |

> 💡 **Chạy riêng từng phân hệ?** Xem hướng dẫn chi tiết tại [`docs/setup-guide.md`](docs/setup-guide.md)

---

## 📚 Tài liệu kỹ thuật

| Tài liệu | Nội dung |
| :--- | :--- |
| [`docs/setup-guide.md`](docs/setup-guide.md) | Hướng dẫn thiết lập môi trường phát triển |
| [`docs/architecture.md`](docs/architecture.md) | Kiến trúc hệ thống, luồng phụ thuộc, bảo mật |
| [`docs/database.md`](docs/database.md) | Schema cơ sở dữ liệu, ERD, quy tắc EF Core |
| [`docs/api-endpoints.md`](docs/api-endpoints.md) | Đặc tả REST API endpoints & payload mẫu |
| [`docs/git-workflow.md`](docs/git-workflow.md) | Quy trình Git, cách đặt tên commit, quy trình PR |

---

## 🌿 Quy trình làm việc với Git

```bash
# Tạo nhánh tính năng mới từ develop
git checkout develop
git pull origin develop
git checkout -b feature/ten-tinh-nang

# Sau khi hoàn thành, tạo Pull Request vào develop
# Xem chi tiết tại docs/git-workflow.md
```

**Cấu trúc nhánh:** `main` (Production) ← `develop` (Integration) ← `feature/*` / `bugfix/*`

---

## 🤝 Đóng góp (Contributing)

1. Đọc tài liệu trong thư mục `docs/` trước khi bắt đầu code.
2. Tạo Issue để thảo luận tính năng hoặc báo lỗi trước khi mở PR.
3. Mỗi PR cần qua review của ít nhất 1 thành viên khác.
4. Tuân thủ chuẩn Conventional Commits khi viết commit message.