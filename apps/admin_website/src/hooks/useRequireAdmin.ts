"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { getToken, getUser } from "../lib/apiClient";

/** @returns true chỉ khi đã xác nhận phiên đăng nhập hợp lệ với vai trò Admin — dùng để
 * chặn trang không render nội dung trước khi kịp điều hướng sang /auth/login (tránh
 * "chớp" nội dung được bảo vệ với người chưa đăng nhập). */
export function useRequireAdmin(): boolean {
  const router = useRouter();
  const [mounted, setMounted] = useState(false);
  const [authorized, setAuthorized] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!mounted) return;
    const token = getToken();
    const user = getUser();
    if (!token || !user) { router.replace("/auth/login"); return; }
    if (user.role !== "Admin") { router.replace("/auth/login"); return; }
    setAuthorized(true);
  }, [mounted, router]);

  return authorized;
}
