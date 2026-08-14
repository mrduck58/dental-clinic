import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Bật standalone output để Dockerfile có thể build image gọn nhẹ
  // Xem: https://nextjs.org/docs/app/api-reference/config/next-config-js/output
  output: 'standalone',

  // Root của repo có một package-lock.json rỗng (dùng cho Docker). Turbopack thấy nhiều lockfile nên
  // suy luận workspace root là dental-clinic/ thay vì thư mục app này — module resolution vỡ và mọi
  // page trả 500 (ComponentMod.handler is not a function). admin_website đã trúng lỗi này rồi.
  // Vercel clone toàn bộ repo nên lockfile ở root vẫn có mặt kể cả khi Root Directory trỏ vào đây.
  turbopack: {
    root: __dirname,
  },
  async redirects() {
    return [
      {
        source: '/',
        destination: '/home',
        permanent: true,
      },
    ];
  },
};

export default nextConfig;
