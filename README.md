# 🦷 Dental Clinic System

Hệ thống quản lý phòng khám nha khoa

## Cấu trúc dự án
- `apps/api` — Backend .NET 9
- `apps/admin_website` — Web vận hành nội bộ (Next.js)
- `apps/clinic_website` — Website công khai (Next.js)
- `apps/mobile_app` — Mobile app (Flutter)

## Yêu cầu
- Docker Desktop
- Git

## Cách chạy
1. Copy file môi trường: `cp .env.example .env`
2. Điền thông tin vào file `.env`
3. Chạy: `docker compose up --build`

## Truy cập
- Website: http://localhost
- Admin: http://localhost/admin
- API: http://localhost/api