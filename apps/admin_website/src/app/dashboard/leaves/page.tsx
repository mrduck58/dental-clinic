"use client";

import { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import Sidebar from "../../../components/shared/Sidebar";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";

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
  startDate: string; // "YYYY-MM-DD"
  endDate: string;   // "YYYY-MM-DD"
  daysCount: number;
  reason: string;
  status: LeaveStatus;
  submittedAt: string;
  reviewedAt?: string;
  reviewerNote?: string;
}

// ── Mock data ─────────────────────────────────────────────────────────────────

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

// ── Status Badge ──────────────────────────────────────────────────────────────

const STATUS_STYLES: Record<LeaveStatus, { label: string; className: string }> = {
  Pending: {
    label: "Đang chờ",
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

export default function LeavesPage() {
  useRequireAdmin();

  const [leaves, setLeaves] = useState<LeaveRequest[]>(MOCK_LEAVES);
  const [isFetching] = useState(false);

  // Filters
  const [statusFilter, setStatusFilter] = useState<string>("All");
  const [searchQuery, setSearchQuery] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");

  // Pagination
  const [currentPage, setCurrentPage] = useState(1);
  const PAGE_SIZE = 5;

  // Toast
  const [toast, setToast] = useState<{ show: boolean; message: string } | null>(null);

  // Statistics
  const stats = useMemo(() => {
    return {
      total: leaves.length,
      pending: leaves.filter((l) => l.status === "Pending").length,
      approved: leaves.filter((l) => l.status === "Approved").length,
      rejected: leaves.filter((l) => l.status === "Rejected").length,
    };
  }, [leaves]);

  // Filtered leaves
  const filteredLeaves = useMemo(() => {
    return leaves.filter((leave) => {
      const matchesStatus = statusFilter === "All" || leave.status === statusFilter;
      const matchesSearch =
        leave.employeeName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        leave.code.toLowerCase().includes(searchQuery.toLowerCase()) ||
        leave.reason.toLowerCase().includes(searchQuery.toLowerCase());

      const leaveStart = new Date(leave.startDate);
      const matchesDateFrom = !dateFrom || leaveStart >= new Date(dateFrom);
      const matchesDateTo = !dateTo || leaveStart <= new Date(dateTo);

      return matchesStatus && matchesSearch && matchesDateFrom && matchesDateTo;
    });
  }, [leaves, statusFilter, searchQuery, dateFrom, dateTo]);

  // Paginated leaves
  const totalPages = Math.ceil(filteredLeaves.length / PAGE_SIZE);
  const paginatedLeaves = useMemo(() => {
    const start = (currentPage - 1) * PAGE_SIZE;
    return filteredLeaves.slice(start, start + PAGE_SIZE);
  }, [filteredLeaves, currentPage]);

  // Reset page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [statusFilter, searchQuery, dateFrom, dateTo]);

  const showToast = (message: string) => {
    setToast({ show: true, message });
    setTimeout(() => setToast(null), 4000);
  };

  const clearFilters = () => {
    setStatusFilter("All");
    setSearchQuery("");
    setDateFrom("");
    setDateTo("");
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <Sidebar activeMenu="leaves" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Đơn Xin Nghỉ Phép</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Quản lý và phê duyệt đơn xin nghỉ phép của nhân viên.
            </p>
          </div>

          {/* Notification Bell */}
          <button className="relative p-2.5 rounded-full bg-slate-100 text-slate-600 hover:bg-red-50 hover:text-primary transition-all animate-ring-hover cursor-pointer">
            <svg className="w-5.5 h-5.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
            {/* Badge */}
            {stats.pending > 0 && (
              <span className="absolute top-1.5 right-1.5 w-3 h-3 bg-primary rounded-full border-2 border-white"></span>
            )}
          </button>
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
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
                  className="text-slate-300 hover:text-slate-500 shrink-0 cursor-pointer ml-1"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>
          )}

          {/* STATS GRID */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng số đơn</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.total}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Tất cả đơn nghỉ</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-amber-400/60 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Đang chờ</span>
                <span className="text-3xl font-black text-amber-600 block mt-1">{stats.pending}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Chờ phê duyệt</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-500 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-green-400/60 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Đã duyệt</span>
                <span className="text-3xl font-black text-green-600 block mt-1">{stats.approved}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Đơn đã chấp thuận</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-green-50 text-green-500 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-red-400/60 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Từ chối</span>
                <span className="text-3xl font-black text-red-600 block mt-1">{stats.rejected}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Đơn không chấp thuận</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-red-500 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
          </div>

          {/* FILTER TOOLBAR */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col lg:flex-row items-stretch lg:items-center gap-4 shrink-0">
            {/* Search */}
            <div className="relative flex-1 min-w-0">
              <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Tìm theo tên nhân viên, mã đơn, lý do..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-9.5 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
              />
            </div>

            {/* Status filter */}
            <div className="relative">
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="w-full lg:w-44 px-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer"
              >
                <option value="All">Tất cả trạng thái</option>
                <option value="Pending">Đang chờ</option>
                <option value="Approved">Đã duyệt</option>
                <option value="Rejected">Từ chối</option>
              </select>
              <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                </svg>
              </span>
            </div>

            {/* Date range */}
            <div className="flex items-center gap-2">
              <div className="relative">
                <span className="absolute inset-y-0 left-3 flex items-center text-slate-400 pointer-events-none">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                  </svg>
                </span>
                <input
                  type="date"
                  value={dateFrom}
                  onChange={(e) => setDateFrom(e.target.value)}
                  placeholder="Từ ngày"
                  className="pl-9 pr-3 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 w-36"
                />
              </div>
              <span className="text-slate-400 font-bold text-[14px]">-</span>
              <div className="relative">
                <input
                  type="date"
                  value={dateTo}
                  onChange={(e) => setDateTo(e.target.value)}
                  placeholder="Đến ngày"
                  className="pl-3 pr-3 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 w-36"
                />
              </div>
            </div>

            {/* Clear filters */}
            {(statusFilter !== "All" || searchQuery || dateFrom || dateTo) && (
              <button
                onClick={clearFilters}
                className="flex items-center gap-1.5 px-3 py-2 text-[13px] font-bold text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg transition-all cursor-pointer whitespace-nowrap"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
                Xóa lọc
              </button>
            )}
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13px] sm:text-[14px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 select-none">
                    <th className="px-5 py-4">Mã đơn</th>
                    <th className="px-5 py-4">Nhân viên</th>
                    <th className="px-5 py-4">Loại nghỉ</th>
                    <th className="px-5 py-4">Ngày bắt đầu</th>
                    <th className="px-5 py-4">Ngày kết thúc</th>
                    <th className="px-5 py-4 text-center">Số ngày</th>
                    <th className="px-5 py-4">Lý do</th>
                    <th className="px-5 py-4 text-center">Trạng thái</th>
                    <th className="px-5 py-4 text-center">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-150/70 font-semibold text-slate-600">
                  {isFetching ? (
                    <tr>
                      <td colSpan={9} className="px-5 py-10 text-center text-slate-400 font-bold">
                        Đang tải danh sách đơn nghỉ...
                      </td>
                    </tr>
                  ) : paginatedLeaves.length > 0 ? (
                    paginatedLeaves.map((leave) => {
                      const status = STATUS_STYLES[leave.status];
                      const typeColor = LEAVE_TYPE_COLORS[leave.leaveType];

                      return (
                        <tr key={leave.id} className="hover:bg-slate-50/30 transition-colors">
                          {/* Code */}
                          <td className="px-5 py-4">
                            <span className="font-black text-primary text-[13px]">{leave.code}</span>
                          </td>

                          {/* Employee */}
                          <td className="px-5 py-4">
                            <div className="flex items-center gap-3">
                              <img
                                src={leave.employeeAvatar}
                                alt={leave.employeeName}
                                className="w-9 h-9 rounded-full object-cover border border-slate-200 shrink-0 shadow-sm"
                              />
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 truncate">{leave.employeeName}</div>
                                <div className="text-[11px] text-slate-400 font-semibold truncate">{leave.department}</div>
                              </div>
                            </div>
                          </td>

                          {/* Leave Type */}
                          <td className="px-5 py-4">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${typeColor}`}>
                              {leave.leaveType}
                            </span>
                          </td>

                          {/* Start Date */}
                          <td className="px-5 py-4 font-bold text-slate-700 text-[13px]">
                            {formatDate(leave.startDate)}
                          </td>

                          {/* End Date */}
                          <td className="px-5 py-4 font-bold text-slate-700 text-[13px]">
                            {formatDate(leave.endDate)}
                          </td>

                          {/* Days Count */}
                          <td className="px-5 py-4 text-center">
                            <span className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-slate-100 text-slate-700 font-black text-[13px]">
                              {leave.daysCount}
                            </span>
                          </td>

                          {/* Reason */}
                          <td className="px-5 py-4 max-w-[200px]">
                            <p className="text-[12px] text-slate-500 font-semibold leading-relaxed line-clamp-2">
                              {leave.reason}
                            </p>
                          </td>

                          {/* Status Badge */}
                          <td className="px-5 py-4 text-center">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${status.className}`}>
                              {status.label}
                            </span>
                          </td>

                          {/* Actions */}
                          <td className="px-5 py-4 text-center">
                            <Link
                              href={`/dashboard/leaves/${leave.id}`}
                              title="Xem chi tiết"
                              className="p-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg transition-all inline-block cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                              </svg>
                            </Link>
                          </td>
                        </tr>
                      );
                    })
                  ) : (
                    <tr>
                      <td colSpan={9} className="px-5 py-10 text-center text-slate-400 font-bold">
                        {leaves.length === 0
                          ? "Chưa có đơn nghỉ phép nào."
                          : "Không tìm thấy đơn nghỉ nào khớp với bộ lọc."}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Result count & Pagination */}
            {!isFetching && filteredLeaves.length > 0 && (
              <div className="border-t border-slate-100 px-5 py-3 flex flex-col sm:flex-row items-center justify-between gap-3">
                <span className="text-[12px] text-slate-400 font-semibold">
                  Hiển thị <span className="font-black text-slate-600">{(currentPage - 1) * PAGE_SIZE + 1}–{Math.min(currentPage * PAGE_SIZE, filteredLeaves.length)}</span> trong{" "}
                  <span className="font-black text-slate-600">{filteredLeaves.length}</span> đơn nghỉ
                </span>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      disabled={currentPage === 1}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                      </svg>
                    </button>

                    {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                      <button
                        key={page}
                        onClick={() => setCurrentPage(page)}
                        className={`w-9 h-9 text-[13px] font-bold rounded-xl transition-all cursor-pointer ${
                          page === currentPage
                            ? "bg-primary text-white shadow-md shadow-primary/20"
                            : "text-slate-500 hover:bg-slate-100"
                        }`}
                      >
                        {page}
                      </button>
                    ))}

                    <button
                      onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                      disabled={currentPage === totalPages}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                      </svg>
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </main>

    </div>
  );
}
