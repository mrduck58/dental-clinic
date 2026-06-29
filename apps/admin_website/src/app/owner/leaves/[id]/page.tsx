"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import {
  getLeaveRequestByIdApi,
  approveLeaveRequestApi,
  rejectLeaveRequestApi,
  type LeaveRequestDto,
} from "../../../../lib/apiClient";

// ── Helpers ───────────────────────────────────────────────────────────────────

const LEAVE_LABEL: Record<string, string> = {
  Annual:    "Phép năm",
  Sick:      "Nghỉ ốm",
  Maternity: "Nghỉ thai sản",
  Unpaid:    "Nghỉ không lương",
  Training:  "Nghỉ họp / Đào tạo",
};

const LEAVE_TYPE_COLORS: Record<string, string> = {
  Annual:    "bg-sky-50 text-sky-600 border-sky-200",
  Sick:      "bg-rose-50 text-rose-600 border-rose-200",
  Maternity: "bg-pink-50 text-pink-600 border-pink-200",
  Unpaid:    "bg-slate-100 text-slate-600 border-slate-200",
  Training:  "bg-violet-50 text-violet-600 border-violet-200",
};

const STATUS_STYLES: Record<string, { label: string; className: string; icon: string }> = {
  Pending:   { label: "Đang chờ duyệt", className: "bg-amber-50 text-amber-600 border-amber-200",  icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" },
  Approved:  { label: "Đã duyệt",        className: "bg-green-50 text-green-600 border-green-200",  icon: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
  Rejected:  { label: "Từ chối",         className: "bg-red-50 text-red-600 border-red-200",        icon: "M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
  Cancelled: { label: "Đã hủy",          className: "bg-slate-100 text-slate-500 border-slate-200", icon: "M6 18L18 6M6 6l12 12" },
};

const AVATAR_COLORS = [
  "bg-sky-500", "bg-violet-500", "bg-pink-500",
  "bg-amber-500", "bg-green-500", "bg-rose-500", "bg-teal-500",
];

const getAvatarColor = (name: string) =>
  AVATAR_COLORS[name.charCodeAt(0) % AVATAR_COLORS.length];

const getInitials = (name: string) =>
  name.split(" ").slice(-2).map((n) => n[0] ?? "").join("").toUpperCase();

const fmtDate = (s: string) => {
  const part = s.includes("T") ? s.split("T")[0] : s;
  const [y, m, d] = part.split("-");
  return `${d}/${m}/${y}`;
};

const fmtDateTime = (s: string) =>
  new Date(s).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });

// ── Page ──────────────────────────────────────────────────────────────────────

export default function LeaveDetailPage() {
  useRequireOwner();
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;

  const [leave, setLeave]           = useState<LeaveRequestDto | null>(null);
  const [isLoading, setIsLoading]   = useState(true);
  const [notFound, setNotFound]     = useState(false);
  const [isActing, setIsActing]     = useState(false);
  const [rejectNote, setRejectNote] = useState("");
  const [toast, setToast]           = useState<{ message: string; ok: boolean } | null>(null);

  useEffect(() => {
    getLeaveRequestByIdApi(id)
      .then(setLeave)
      .catch(() => setNotFound(true))
      .finally(() => setIsLoading(false));
  }, [id]);

  const showToast = (message: string, ok = true) => {
    setToast({ message, ok });
    setTimeout(() => setToast(null), 4000);
  };

  const handleApprove = async () => {
    if (!leave) return;
    setIsActing(true);
    try {
      const updated = await approveLeaveRequestApi(leave.id);
      setLeave(updated);
      showToast("Đơn xin nghỉ đã được duyệt thành công.");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Duyệt đơn thất bại.", false);
    } finally {
      setIsActing(false);
    }
  };

  const handleReject = async () => {
    if (!leave) return;
    setIsActing(true);
    try {
      const updated = await rejectLeaveRequestApi(leave.id, rejectNote.trim() || undefined);
      setLeave(updated);
      setRejectNote("");
      showToast("Đã từ chối đơn xin nghỉ.");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Từ chối đơn thất bại.", false);
    } finally {
      setIsActing(false);
    }
  };

  // ── Loading ────────────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <OwnerSidebar activeMenu="leaves" />
        <main className="flex-1 flex flex-col min-w-0">
          <PageHeader />
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <div className="w-10 h-10 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto" />
              <p className="mt-3 text-[14px] font-semibold text-slate-400">Đang tải...</p>
            </div>
          </div>
        </main>
      </div>
    );
  }

  // ── Not found ──────────────────────────────────────────────────────────────
  if (notFound || !leave) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <OwnerSidebar activeMenu="leaves" />
        <main className="flex-1 flex flex-col min-w-0">
          <PageHeader />
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <svg className="w-16 h-16 text-slate-300 mx-auto" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
              </svg>
              <p className="mt-4 text-[16px] font-bold text-slate-500">Không tìm thấy đơn nghỉ phép</p>
              <Link href="/owner/leaves"
                className="mt-4 inline-flex items-center gap-2 px-4 py-2 bg-primary text-white text-[14px] font-bold rounded-xl transition-all">
                Quay lại danh sách
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  const status    = STATUS_STYLES[leave.status] ?? STATUS_STYLES["Pending"];
  const typeColor = LEAVE_TYPE_COLORS[leave.leaveType] ?? "bg-slate-100 text-slate-600 border-slate-200";
  const typeLabel = LEAVE_LABEL[leave.leaveType] ?? leave.leaveType;
  const shortId   = leave.id.slice(0, 8).toUpperCase();

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="leaves" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <Link href="/owner/leaves"
              className="p-2 text-slate-400 hover:text-primary hover:bg-slate-100 rounded-xl transition-all cursor-pointer">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
            <div>
              <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chi tiết đơn xin nghỉ phép</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Xem thông tin và phê duyệt đơn nghỉ.</p>
            </div>
          </div>
          <button className="relative p-2.5 rounded-full bg-slate-100 text-slate-600 hover:bg-red-50 hover:text-primary transition-all cursor-pointer">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
          </button>
        </header>

        {/* TOAST */}
        {toast && (
          <div className="fixed top-6 right-6 z-[100] animate-fade-in">
            <div className={`bg-white rounded-2xl shadow-xl p-4 flex items-center gap-3 max-w-sm border ${toast.ok ? "border-green-200" : "border-red-200"}`}>
              <div className={`w-9 h-9 rounded-full flex items-center justify-center shrink-0 ${toast.ok ? "bg-green-100" : "bg-red-100"}`}>
                <svg className={`w-5 h-5 ${toast.ok ? "text-green-600" : "text-red-600"}`} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d={toast.ok
                    ? "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                    : "M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z"} />
                </svg>
              </div>
              <span className="text-[13px] font-black text-slate-900">{toast.message}</span>
              <button onClick={() => setToast(null)} className="text-slate-300 hover:text-slate-500 shrink-0 cursor-pointer ml-1">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
        )}

        {/* CONTENT */}
        <div className="p-8 flex-1 overflow-y-auto">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

            {/* ── LEFT: Thông tin đơn nghỉ ── */}
            <div className="lg:col-span-2 flex flex-col gap-5">

              {/* Card: Thông tin đơn */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between">
                  <h2 className="text-[15px] font-black text-slate-900">Thông tin đơn nghỉ phép</h2>
                  <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-[12px] font-black border ${status.className}`}>
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d={status.icon} />
                    </svg>
                    {status.label}
                  </span>
                </div>

                <div className="p-6">
                  <div className="grid grid-cols-2 gap-y-6 gap-x-8">
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Mã tham chiếu</span>
                      <span className="font-black text-primary text-[14px] font-mono">{shortId}</span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Loại nghỉ</span>
                      <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border ${typeColor}`}>
                        {typeLabel}
                      </span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày bắt đầu</span>
                      <span className="font-bold text-slate-700 text-[14px]">{fmtDate(leave.startDate)}</span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày kết thúc</span>
                      <span className="font-bold text-slate-700 text-[14px]">{fmtDate(leave.endDate)}</span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Số ngày nghỉ</span>
                      <span className="font-black text-slate-900 text-[18px]">{leave.daysCount} <span className="text-[13px] font-semibold text-slate-400">ngày</span></span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày nộp đơn</span>
                      <span className="font-bold text-slate-700 text-[14px]">{fmtDateTime(leave.createdAt)}</span>
                    </div>
                  </div>

                  <div className="mt-6 pt-5 border-t border-slate-100">
                    <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-2">Lý do xin nghỉ</span>
                    <p className="text-[14px] text-slate-700 font-semibold leading-relaxed bg-slate-50 rounded-xl p-4">
                      {leave.reason}
                    </p>
                  </div>
                </div>
              </div>

              {/* Card: Thông tin phê duyệt (nếu đã xử lý) */}
              {leave.reviewedAt && (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                    <h2 className="text-[15px] font-black text-slate-900">Kết quả phê duyệt</h2>
                  </div>
                  <div className="p-6 flex flex-col gap-5">
                    <div className="grid grid-cols-2 gap-x-8">
                      <div>
                        <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">
                          {leave.status === "Approved" ? "Ngày duyệt" : "Ngày từ chối"}
                        </span>
                        <span className="font-bold text-slate-700">{fmtDateTime(leave.reviewedAt)}</span>
                      </div>
                    </div>

                    {leave.reviewerNote && (
                      <div>
                        <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-2">
                          {leave.status === "Approved" ? "Ghi chú từ quản lý" : "Lý do từ chối"}
                        </span>
                        <div className={`p-4 rounded-xl text-[13.5px] font-semibold leading-relaxed ${
                          leave.status === "Approved"
                            ? "bg-green-50 text-green-800 border border-green-100"
                            : "bg-red-50 text-red-800 border border-red-100"
                        }`}>
                          {leave.reviewerNote}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* ── RIGHT: Nhân viên + Hành động ── */}
            <div className="flex flex-col gap-5">

              {/* Card: Thông tin nhân viên */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                  <h2 className="text-[15px] font-black text-slate-900">Nhân viên</h2>
                </div>
                <div className="p-6 flex flex-col items-center text-center gap-3">
                  <div className={`w-20 h-20 rounded-full ${getAvatarColor(leave.userFullName)} flex items-center justify-center text-white text-2xl font-black shadow-md`}>
                    {getInitials(leave.userFullName)}
                  </div>
                  <div>
                    <h3 className="text-[16px] font-extrabold text-slate-900">{leave.userFullName}</h3>
                    <p className="mt-1 text-[13px] text-slate-500 font-semibold">{leave.department ?? "—"}</p>
                  </div>
                  <Link href="/owner/leaves"
                    className="mt-1 text-[12.5px] text-primary font-bold hover:underline cursor-pointer">
                    ← Xem tất cả đơn
                  </Link>
                </div>
              </div>

              {/* Card: Hành động — chỉ khi Pending */}
              {leave.status === "Pending" && (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                    <h2 className="text-[15px] font-black text-slate-900">Hành động</h2>
                  </div>
                  <div className="p-6 flex flex-col gap-4">
                    <div>
                      <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide block mb-2">
                        Ghi chú <span className="text-slate-300 normal-case font-semibold">(tuỳ chọn khi duyệt, bắt buộc khi từ chối)</span>
                      </label>
                      <textarea
                        rows={3}
                        value={rejectNote}
                        onChange={(e) => setRejectNote(e.target.value)}
                        placeholder="Nhập lý do hoặc ghi chú..."
                        className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none"
                      />
                    </div>

                    <div className="flex flex-col gap-2.5">
                      {/* Duyệt */}
                      <button onClick={handleApprove} disabled={isActing}
                        className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-green-600 hover:bg-green-700 disabled:opacity-60 disabled:cursor-not-allowed text-white text-[14px] font-extrabold rounded-xl shadow-sm shadow-green-600/20 transition-all cursor-pointer">
                        {isActing ? (
                          <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/>
                          </svg>
                        ) : (
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                        )}
                        Duyệt đơn
                      </button>

                      {/* Từ chối */}
                      <button onClick={handleReject} disabled={isActing || !rejectNote.trim()}
                        className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-red-50 border-2 border-red-200 text-red-600 hover:bg-red-100 hover:border-red-300 disabled:opacity-50 disabled:cursor-not-allowed text-[14px] font-extrabold rounded-xl transition-all cursor-pointer">
                        {isActing ? (
                          <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/>
                          </svg>
                        ) : (
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                        )}
                        Từ chối
                      </button>

                      {!rejectNote.trim() && (
                        <p className="text-[11.5px] text-slate-400 font-semibold text-center">
                          Nhập lý do để kích hoạt nút Từ chối
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

function PageHeader() {
  return (
    <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center shrink-0 shadow-sm shadow-slate-100/50">
      <Link href="/owner/leaves"
        className="flex items-center gap-2 text-slate-500 hover:text-primary transition-all cursor-pointer">
        <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
        </svg>
        <span className="text-[14px] font-bold">Quay lại</span>
      </Link>
    </header>
  );
}
