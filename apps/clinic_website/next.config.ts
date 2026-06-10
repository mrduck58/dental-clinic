import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Bật standalone output để Dockerfile có thể build image gọn nhẹ
  // Xem: https://nextjs.org/docs/app/api-reference/config/next-config-js/output
  output: 'standalone',
};

export default nextConfig;
