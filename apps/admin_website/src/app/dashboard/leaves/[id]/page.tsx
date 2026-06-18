"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";

// ── Types ─────────────────────────────────────────────────────────────────────

type LeaveStatus = "Pending" | "Approved" | "Rejected";

type LeaveType =
  | "Nghỉ phép năm"
  | "Nghỉ ốm"
  | "Nghỉ thai sản"
  | "Nghỉ không lương"
  | "Nghỉ họp / Đào tạo";

interface LeaveRequest {
  id: string;
  code: string;
  employeeName: string;
  employeeAvatar: string;
  department: string;
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  daysCount: number;
  reason: string;
  status: LeaveStatus;
  submittedAt: string;
  reviewedAt?: string;
  reviewerNote?: string;
}

// ── Mock data (shared with list page) ────────────────────────────────────────

const MOCK_LEAVES: LeaveRequest[] = [
  {
    id: "1",
    code: "NP-001",
    employeeName: "Nguyễn Thị Lan",
    employeeAvatar: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=150&q=80",
    department: "Lễ tân",
    leaveType: "Nghỉ phép năm",
    startDate: "2026-06-20",
    endDate: "2026-06-22",
    daysCount: 3,
    reason: "Có việc gia đình cần giải quyết, xin nghỉ 3 ngày để về quê.",
    status: "Pending",
    submittedAt: "2026-06-10T08:30:00",
  },
  {
    id: "2",
    code: "NP-002",
    employeeName: "Trần Văn Hùng",
    employeeAvatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=150&q=80",
    department: "Bác sĩ",
    leaveType: "Nghỉ ốm",
    startDate: "2026-06-15",
    endDate: "2026-06-15",
    daysCount: 1,
    reason: "Bị cảm sốt, sức khỏe không đảm bảo để làm việc.",
    status: "Approved",
    submittedAt: "2026-06-14T07:00:00",
    reviewedAt: "2026-06-14T08:00:00",
    reviewerNote: "Đã duyệt. Nghỉ dưỡng sức, hẹn gặp lại tuần sau.",
  },
  {
    id: "3",
    code: "NP-003",
    employeeName: "Lê Thị Mai",
    employeeAvatar: "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=150&q=80",
    department: "Kế toán",
    leaveType: "Nghỉ họp / Đào tạo",
    startDate: "2026-06-25",
    endDate: "2026-06-26",
    daysCount: 2,
    reason: "Tham gia khóa đào tạo kế toán mới tại TP.HCM do công ty tổ chức.",
    status: "Pending",
    submittedAt: "2026-06-11T09:15:00",
  },
  {
    id: "4",
    code: "NP-004",
    employeeName: "Phạm Đức Anh",
    employeeAvatar: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=150&q=80",
    department: "Bác sĩ",
    leaveType: "Nghỉ không lương",
    startDate: "2026-07-01",
    endDate: "2026-07-05",
    daysCount: 5,
    reason: "Có việc riêng quan trọng cần xin nghỉ không lương 5 ngày.",
    status: "Rejected",
    submittedAt: "2026-06-09T10:00:00",
    reviewedAt: "2026-06-09T14:00:00",
    reviewerNote: "Từ chối do lịch làm việc cao điểm, thiếu nhân sự thay thế. Vui lòng chọn thời gian khác.",
  },
  {
    id: "5",
    code: "NP-005",
    employeeName: "Hoàng Thị Hương",
    employeeAvatar: "https://images.unsplash.com/photo-1489424731084-a5d8b219a5bb?auto=format&fit=crop&w=150&q=80",
    department: "Lễ tân",
    leaveType: "Nghỉ thai sản",
    startDate: "2026-06-30",
    endDate: "2026-10-30",
    daysCount: 90,
    reason: "Nghỉ thai sản theo quy định.",
    status: "Approved",
    submittedAt: "2026-06-05T08:00:00",
    reviewedAt: "2026-06-05T10:00:00",
  },
  {
    id: "6",
    code: "NP-006",
    employeeName: "Vũ Minh Tuấn",
    employeeAvatar: "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?auto=format&fit=crop&w=150&q=80",
    department: "Bác sĩ",
    leaveType: "Nghỉ phép năm",
    startDate: "2026-07-10",
    endDate: "2026-07-12",
    daysCount: 3,
    reason: "Kế hoạch đi du lịch cuối năm đã lên từ trước.",
    status: "Pending",
    submittedAt: "2026-06-12T11:00:00",
  },
  {
    id: "7",
    code: "NP-007",
    employeeName: "Đặng Thị Ngọc",
    employeeAvatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=150&q=80",
    department: "Kế toán",
    leaveType: "Nghỉ ốm",
    startDate: "2026-06-18",
    endDate: "2026-06-19",
    daysCount: 2,
    reason: "Khám và điều trị răng theo chỉ định bác sĩ.",
    status: "Approved",
    submittedAt: "2026-06-17T07:30:00",
    reviewedAt: "2026-06-17T08:00:00",
  },
];

// ── Helpers ───────────────────────────────────────────────────────────────────

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
};

const formatDateTime = (dateStr: string) => {
  return new Date(dateStr).toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const STATUS_STYLES: Record<LeaveStatus, { label: string; className: string }> = {
  Pending: {
    label: "Đang chờ duyệt",
    className: "bg-amber-50 text-amber-600 border-amber-200",
  },
  Approved: {
    label: "Đã duyệt",
    className: "bg-green-50 text-green-600 border-green-200",
  },
  Rejected: {
    label: "Từ chối",
    className: "bg-red-50 text-red-600 border-red-200",
  },
};

const LEAVE_TYPE_COLORS: Record<LeaveType, string> = {
  "Nghỉ phép năm": "bg-sky-50 text-sky-600 border-sky-200",
  "Nghỉ ốm": "bg-rose-50 text-rose-600 border-rose-200",
  "Nghỉ thai sản": "bg-pink-50 text-pink-600 border-pink-200",
  "Nghỉ không lương": "bg-slate-100 text-slate-600 border-slate-200",
  "Nghỉ họp / Đào tạo": "bg-violet-50 text-violet-600 border-violet-200",
};

// ── Page Component ────────────────────────────────────────────────────────────

export default function LeaveDetailPage() {
  useRequireAdmin();
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;

  const [leave, setLeave] = useState<LeaveRequest | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [reviewerNote, setReviewerNote] = useState("");
  const [toast, setToast] = useState<{ show: boolean; message: string } | null>(null);

  useEffect(() => {
    const found = MOCK_LEAVES.find((l) => l.id === id);
    setTimeout(() => {
      setLeave(found ?? null);
      setIsLoading(false);
    }, 300);
  }, [id]);

  const showToast = (message: string) => {
    setToast({ show: true, message });
    setTimeout(() => setToast(null), 4000);
  };

  const handleApprove = () => {
    if (!leave) return;
    setIsSubmitting(true);
    setTimeout(() => {
      setLeave({
        ...leave,
        status: "Approved",
        reviewedAt: new Date().toISOString(),
        reviewerNote,
      });
      setIsSubmitting(false);
      showToast(`Đơn ${leave.code} đã được duyệt!`);
    }, 600);
  };

  const handleReject = () => {
    if (!leave || !reviewerNote.trim()) return;
    setIsSubmitting(true);
    setTimeout(() => {
      setLeave({
        ...leave,
        status: "Rejected",
        reviewedAt: new Date().toISOString(),
        reviewerNote,
      });
      setIsSubmitting(false);
      showToast(`Đơn ${leave.code} đã bị từ chối.`);
    }, 600);
  };

  if (isLoading) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="leaves" />
        <main className="flex-1 flex flex-col min-w-0">
          <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center shrink-0 font-sans shadow-sm shadow-slate-100/50">
            <Link
              href="/dashboard/leaves"
              className="flex items-center gap-2 text-slate-500 hover:text-primary transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
              <span className="text-[14px] font-bold">Quay lại</span>
            </Link>
          </header>
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <div className="w-10 h-10 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto"></div>
              <p className="mt-3 text-[14px] font-semibold text-slate-400">Đang tải...</p>
            </div>
          </div>
        </main>
      </div>
    );
  }

  if (!leave) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="leaves" />
        <main className="flex-1 flex flex-col min-w-0">
          <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center shrink-0 font-sans shadow-sm shadow-slate-100/50">
            <Link
              href="/dashboard/leaves"
              className="flex items-center gap-2 text-slate-500 hover:text-primary transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
              <span className="text-[14px] font-bold">Quay lại</span>
            </Link>
          </header>
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <svg className="w-16 h-16 text-slate-300 mx-auto" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
              </svg>
              <p className="mt-4 text-[16px] font-bold text-slate-500">Không tìm thấy đơn nghỉ phép</p>
              <Link
                href="/dashboard/leaves"
                className="mt-4 inline-flex items-center gap-2 px-4 py-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-bold rounded-xl transition-all cursor-pointer"
              >
                Quay lại danh sách
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  const status = STATUS_STYLES[leave.status];

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="leaves" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <Link
              href="/dashboard/leaves"
              className="flex items-center gap-2 text-slate-500 hover:text-primary transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
            <div>
              <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chi tiết đơn xin nghỉ phép</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Xem thông tin chi tiết và phê duyệt đơn nghỉ.</p>
            </div>
          </div>

          {/* Notification Bell */}
          <button className="relative p-2.5 rounded-full bg-slate-100 text-slate-600 hover:bg-red-50 hover:text-primary transition-all animate-ring-hover cursor-pointer">
            <svg className="w-5.5 h-5.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
          </button>
        </header>

        {/* TOAST */}
        {toast?.show && (
          <div className="fixed top-6 right-6 z-[100] animate-fade-in">
            <div className="bg-white border border-green-200 rounded-2xl shadow-xl shadow-slate-200/60 p-4 flex items-center gap-3 max-w-sm">
              <div className="w-9 h-9 rounded-full bg-green-100 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <span className="text-[13px] font-black text-slate-900">{toast.message}</span>
              <button
                onClick={() => setToast(null)}
                className="text-slate-300 hover:text-slate-500 shrink-0 ml-1 cursor-pointer"
              >
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
            {/* LEFT: Thông tin đơn nghỉ */}
            <div className="lg:col-span-2 space-y-5">
              {/* Card 1: Thông tin đơn nghỉ */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                  <h2 className="text-[15px] font-black text-slate-900">Thông tin đơn nghỉ phép</h2>
                </div>
                <div className="p-6">
                  <div className="grid grid-cols-2 gap-y-5 gap-x-8">
                    {/* Mã đơn */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Mã đơn</span>
                      <span className="font-black text-primary text-[15px]">{leave.code}</span>
                    </div>

                    {/* Trạng thái */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Trạng thái</span>
                      <span className={`inline-flex items-center px-3 py-1 rounded-full text-[12px] font-black border ${status.className}`}>
                        {status.label}
                      </span>
                    </div>

                    {/* Ngày tạo */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày tạo</span>
                      <span className="font-bold text-slate-700">{formatDateTime(leave.submittedAt)}</span>
                    </div>

                    {/* Loại nghỉ */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Loại nghỉ</span>
                      <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[12px] font-black border ${LEAVE_TYPE_COLORS[leave.leaveType]}`}>
                        {leave.leaveType}
                      </span>
                    </div>

                    {/* Ngày bắt đầu */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày bắt đầu</span>
                      <span className="font-bold text-slate-700">{formatDate(leave.startDate)}</span>
                    </div>

                    {/* Ngày kết thúc */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày kết thúc</span>
                      <span className="font-bold text-slate-700">{formatDate(leave.endDate)}</span>
                    </div>

                    {/* Số ngày nghỉ */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Số ngày nghỉ</span>
                      <span className="font-black text-slate-900 text-[15px]">{leave.daysCount} ngày</span>
                    </div>
                  </div>

                  {/* Lý do */}
                  <div className="mt-5 pt-5 border-t border-slate-100">
                    <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Lý do</span>
                    <p className="text-[14px] text-slate-700 font-semibold leading-relaxed">{leave.reason}</p>
                  </div>
                </div>
              </div>

              {/* Card 2: Thông tin phê duyệt (nếu đã xử lý) */}
              {leave.reviewedAt && (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                    <h2 className="text-[15px] font-black text-slate-900">Thông tin phê duyệt</h2>
                  </div>
                  <div className="p-6">
                    <div className="grid grid-cols-2 gap-y-5 gap-x-8">
                      <div>
                        <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">
                          {leave.status === "Approved" ? "Ngày duyệt" : "Ngày từ chối"}
                        </span>
                        <span className="font-bold text-slate-700">{formatDateTime(leave.reviewedAt)}</span>
                      </div>
                      <div>
                        <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">
                          {leave.status === "Approved" ? "Người duyệt" : "Người từ chối"}
                        </span>
                        <span className="font-bold text-slate-700">Admin</span>
                      </div>
                    </div>
                    {leave.reviewerNote && (
                      <div className="mt-5 pt-5 border-t border-slate-100">
                        <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">
                          {leave.status === "Approved" ? "Ghi chú duyệt" : "Lý do từ chối"}
                        </span>
                        <p className="text-[14px] text-slate-700 font-semibold leading-relaxed">{leave.reviewerNote}</p>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* RIGHT: Thông tin nhân viên */}
            <div className="space-y-5">
              {/* Card: Thông tin nhân viên */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                  <h2 className="text-[15px] font-black text-slate-900">Thông tin nhân viên</h2>
                </div>
                <div className="p-6">
                  <div className="flex flex-col items-center text-center">
                    <img
                      src={leave.employeeAvatar}
                      alt={leave.employeeName}
                      className="w-20 h-20 rounded-full object-cover border-3 border-slate-200 shadow-md"
                    />
                    <h3 className="mt-4 text-[16px] font-extrabold text-slate-900">{leave.employeeName}</h3>
                    <span className="mt-1 text-[13px] text-slate-500 font-semibold">{leave.department}</span>
                  </div>
                </div>
              </div>

              {/* Card: Hành động (chỉ khi đang chờ) */}
              {leave.status === "Pending" && (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                    <h2 className="text-[15px] font-black text-slate-900">Hành động</h2>
                  </div>
                  <div className="p-6 flex flex-col gap-3">
                    {/* Ghi chú */}
                    <div>
                      <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide mb-1.5 block">
                        Ghi chú phê duyệt
                      </label>
                      <textarea
                        rows={3}
                        value={reviewerNote}
                        onChange={(e) => setReviewerNote(e.target.value)}
                        placeholder="Nhập ghi chú (tùy chọn)..."
                        className="w-full px-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-800 resize-none placeholder:text-slate-400"
                      />
                    </div>

                    {/* Buttons */}
                    <div className="flex flex-col gap-2.5 pt-1">
                      <button
                        onClick={handleApprove}
                        disabled={isSubmitting}
                        className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-green-500 hover:bg-green-600 disabled:opacity-60 disabled:cursor-not-allowed text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-green-200 hover:shadow-green-300 transition-all cursor-pointer"
                      >
                        {isSubmitting ? (
                          <>
                            <svg className="w-4 h-4 animate-spin" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                            </svg>
                            Đang xử lý...
                          </>
                        ) : (
                          <>
                            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            Duyệt đơn
                          </>
                        )}
                      </button>
                      <button
                        onClick={handleReject}
                        disabled={isSubmitting || !reviewerNote.trim()}
                        className="w-full flex items-center justify-center gap-2 px-4 py-3 text-[14px] font-extrabold rounded-xl border-2 border-red-200 text-red-500 hover:bg-red-50 hover:border-red-300 disabled:opacity-50 disabled:cursor-not-allowed transition-all cursor-pointer"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        Từ chối
                      </button>
                      {!reviewerNote.trim() && (
                        <p className="text-[11px] text-red-500 font-semibold text-center">
                          Vui lòng nhập lý do từ chối
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
