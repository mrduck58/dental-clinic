"use client";

import { useCallback, useEffect, useState } from "react";
import { createPortal } from "react-dom";

export interface ConfirmOptions {
  /** Câu hỏi chính, viết ngắn gọn: "Xóa dịch vụ khỏi liệu trình?" */
  title: string;
  /** Hệ quả của hành động — nói rõ cái gì mất đi, để người dùng quyết định có cơ sở. */
  message?: string;
  /** Nhãn nút đồng ý (mặc định "Xác nhận"). Nên đặt theo hành động: "Xóa dịch vụ", "Hủy lịch hẹn". */
  confirmLabel?: string;
  cancelLabel?: string;
  /** danger = hành động phá hủy (đỏ, mặc định) · primary = hành động thường (xanh). */
  tone?: "danger" | "primary";
}

interface ConfirmState extends ConfirmOptions {
  resolve: (confirmed: boolean) => void;
}

/**
 * Popup xác nhận dùng chung cho các trang vận hành.
 *
 * Dùng thay cho confirm() của trình duyệt: confirm() chặn cả tab, hiện tên miền như cảnh báo
 * bảo mật, không đổi được chữ trên nút nên người dùng phải đọc kỹ mới biết "OK" là xóa hay hủy,
 * và không theo giao diện của ứng dụng.
 *
 * Cách dùng — giống cặp useToast/Toast:
 *
 *   const { confirm, confirmState, closeConfirm } = useConfirm();
 *
 *   const handleDelete = async () => {
 *     const ok = await confirm({ title: "Xóa mục này?", confirmLabel: "Xóa" });
 *     if (!ok) return;
 *     ...
 *   };
 *
 *   <ConfirmDialog state={confirmState} onClose={closeConfirm} />
 */
export function useConfirm() {
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null);

  const confirm = useCallback(
    (options: ConfirmOptions) =>
      new Promise<boolean>(resolve => setConfirmState({ ...options, resolve })),
    []
  );

  // Trả kết quả cho lời gọi confirm() đang chờ, rồi đóng popup. Gọi resolve NGOÀI hàm cập nhật
  // state (updater phải thuần túy — StrictMode gọi nó hai lần ở môi trường dev).
  const closeConfirm = useCallback((confirmed: boolean) => {
    confirmState?.resolve(confirmed);
    setConfirmState(null);
  }, [confirmState]);

  return { confirm, confirmState, closeConfirm };
}

export function ConfirmDialog({ state, onClose }: {
  state: ConfirmState | null;
  onClose: (confirmed: boolean) => void;
}) {
  // Esc = hủy, đúng thói quen của mọi hộp thoại.
  useEffect(() => {
    if (!state) return;
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(false); };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [state, onClose]);

  if (!state || typeof document === "undefined") return null;

  const danger = (state.tone ?? "danger") === "danger";

  return createPortal(
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-[9998] bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-6"
      onClick={() => onClose(false)}
    >
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm overflow-hidden" onClick={e => e.stopPropagation()}>
        <div className="p-6 flex gap-4">
          <div className={`w-11 h-11 rounded-xl flex items-center justify-center shrink-0 ${danger ? "bg-red-50 text-red-600" : "bg-sky-50 text-sky-600"}`}>
            <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d={danger
                  ? "M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z"
                  : "M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z"}
              />
            </svg>
          </div>
          <div className="min-w-0">
            <div className="text-[15px] font-black text-slate-900">{state.title}</div>
            {state.message && (
              <div className="text-[13px] font-semibold text-slate-500 mt-1.5 leading-relaxed">{state.message}</div>
            )}
          </div>
        </div>
        <div className="px-6 py-4 bg-slate-50/60 border-t border-slate-100 flex items-center justify-end gap-2.5">
          <button
            onClick={() => onClose(false)}
            className="px-4 py-2.5 text-[13px] font-bold text-slate-500 border border-slate-200 bg-white rounded-xl hover:bg-slate-50 transition-colors cursor-pointer"
          >
            {state.cancelLabel ?? "Hủy"}
          </button>
          <button
            autoFocus
            onClick={() => onClose(true)}
            className={`px-4 py-2.5 text-[13px] font-black text-white rounded-xl transition-colors cursor-pointer ${danger ? "bg-red-600 hover:bg-red-700" : "bg-primary hover:bg-red-600"}`}
          >
            {state.confirmLabel ?? "Xác nhận"}
          </button>
        </div>
      </div>
    </div>,
    document.body
  );
}
