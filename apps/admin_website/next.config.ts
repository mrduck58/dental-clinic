import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'standalone',
  // KHÔNG dùng basePath — route (/admin, /staff, /dentist, /owner, /auth/login...) giữ
  // nguyên, khớp đúng cấu trúc thư mục src/app/{admin,staff,dentist,owner,auth}/... Chỉ
  // dùng assetPrefix để file JS/CSS tĩnh (/_next/static/...) có tiền tố /admin riêng —
  // tránh trùng với /_next/static/... của clinic_website (2 app khác nhau, cùng chạy sau
  // 1 domain). Nginx đã có sẵn location /admin trỏ về app này nên bắt luôn cả phần này,
  // không cần thêm route riêng.
  assetPrefix: '/admin',
};

export default nextConfig;
