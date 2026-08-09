"use client";

import { useCallback, useState } from "react";

export type ToastType = "success" | "error" | "info";

export interface ToastState {
  message: string;
  type: ToastType;
}

/**
 * Toast dùng chung cho các trang vận hành.
 *
 * Giao diện giữ nguyên đúng mẫu đang dùng ở admin/rooms và staff/appointments — tách ra đây vì
 * cùng một khối markup đang bị chép lại ở khoảng chục trang, mỗi nơi lệch nhau một chút.
 *
 * Dùng thay cho alert(): alert() chặn cả trình duyệt, nuốt mất thao tác đang dở và trông như lỗi
 * hệ thống chứ không phải phản hồi của ứng dụng.
 */
export function useToast(autoHideMs = 4000) {
  const [toast, setToast] = useState<ToastState | null>(null);

  const showToast = useCallback((message: string, type: ToastType = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), autoHideMs);
  }, [autoHideMs]);

  return { toast, showToast };
}

export function Toast({ toast }: { toast: ToastState | null }) {
  if (!toast) return null;

  const tone =
    toast.type === "success" ? "bg-emerald-900 border-emerald-800"
    : toast.type === "error" ? "bg-red-900 border-red-800"
    : "bg-slate-900 border-slate-800";

  const icon = toast.type === "success" ? "✓" : toast.type === "error" ? "⚠" : "ℹ";

  return (
    <div
      role="status"
      aria-live="polite"
      className={`fixed top-6 right-6 z-[9999] px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border font-bold text-[14.5px] max-w-md text-white ${tone}`}
    >
      <span className="text-lg">{icon}</span>
      <span>{toast.message}</span>
    </div>
  );
}
