import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'standalone',
  // Khi chạy sau nginx tại prefix /admin, Next.js cần biết basePath
  // để tự động thêm vào tất cả <Link>, router.push(), và đường dẫn asset
  basePath: process.env.NEXT_BASE_PATH ?? '',
  assetPrefix: process.env.NEXT_BASE_PATH ?? '',
};

export default nextConfig;
