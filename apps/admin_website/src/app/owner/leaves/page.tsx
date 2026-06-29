"use client";

import { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import {
  getLeaveRequestsAdminApi,
  type LeaveRequestDto,
} from "../../../lib/apiClient";

// ── Helpers ───────────────────────────────────────────────────────────────────

type LeaveWithCode = LeaveRequestDto & { code: string };

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

const STATUS_STYLES: Record<string, { label: string; className: string }> = {
  Pending:   { label: "Đang chờ", className: "bg-amber-50 text-amber-600 border-amber-200" },
  Approved:  { label: "Đã duyệt", className: "bg-green-50 text-green-600 border-green-200" },
  Rejected:  { label: "Từ chối",  className: "bg-red-50 text-red-600 border-red-200" },
  Cancelled: { label: "Đã hủy",   className: "bg-slate-100 text-slate-500 border-slate-200" },
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

const fmtDateTime = (s: string) => {
  const dt = new Date(s);
  return dt.toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
};

// ── Page Component ────────────────────────────────────────────────────────────

export default function LeavesPage() {
  useRequireOwner();

  const [leaves, setLeaves] = useState<LeaveWithCode[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Filters
  const [statusFilter, setStatusFilter] = useState("All");
  const [searchQuery, setSearchQuery] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");

  // Pagination
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(5);

  // Toast
  const [toast, setToast] = useState<{ message: string; ok: boolean } | null>(null);

  // ── Data loading ────────────────────────────────────────────────────────────

  const loadData = async () => {
    try {
      const data = await getLeaveRequestsAdminApi();
      const sorted = [...data].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
      const withCodes: LeaveWithCode[] = sorted.map((l, i) => ({
        ...l,
        code: `NP-${String(i + 1).padStart(3, "0")}`,
      }));
      setLeaves(withCodes);
    } catch {
      showToast("Không thể tải danh sách đơn nghỉ.", false);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { loadData(); }, []);

  // ── Derived state ───────────────────────────────────────────────────────────

  const stats = useMemo(() => ({
    total:    leaves.length,
    pending:  leaves.filter((l) => l.status === "Pending").length,
    approved: leaves.filter((l) => l.status === "Approved").length,
    rejected: leaves.filter((l) => l.status === "Rejected" || l.status === "Cancelled").length,
  }), [leaves]);

  const filteredLeaves = useMemo(() => {
    return leaves.filter((leave) => {
      const matchStatus  = statusFilter === "All" || leave.status === statusFilter;
      const q = searchQuery.toLowerCase();
      const matchSearch  = !q ||
        leave.userFullName.toLowerCase().includes(q) ||
        leave.code.toLowerCase().includes(q) ||
        leave.reason.toLowerCase().includes(q);
      const matchFrom    = !dateFrom || leave.startDate >= dateFrom;
      const matchTo      = !dateTo   || leave.startDate <= dateTo;
      return matchStatus && matchSearch && matchFrom && matchTo;
    });
  }, [leaves, statusFilter, searchQuery, dateFrom, dateTo]);

  const totalPages = Math.ceil(filteredLeaves.length / itemsPerPage);
  const paginatedLeaves = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    return filteredLeaves.slice(start, start + itemsPerPage);
  }, [filteredLeaves, currentPage, itemsPerPage]);

  useEffect(() => { setCurrentPage(1); }, [statusFilter, searchQuery, dateFrom, dateTo, itemsPerPage]);

  // ── Actions ─────────────────────────────────────────────────────────────────

  const showToast = (message: string, ok = true) => {
    setToast({ message, ok });
    setTimeout(() => setToast(null), 4000);
  };

  const clearFilters = () => {
    setStatusFilter("All");
    setSearchQuery("");
    setDateFrom("");
    setDateTo("");
  };

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="leaves" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Đơn Xin Nghỉ Phép</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Quản lý và phê duyệt đơn xin nghỉ phép của nhân viên.
            </p>
          </div>
          <button className="relative p-2.5 rounded-full bg-slate-100 text-slate-600 hover:bg-red-50 hover:text-primary transition-all cursor-pointer">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
            {stats.pending > 0 && (
              <span className="absolute top-1.5 right-1.5 w-3 h-3 bg-primary rounded-full border-2 border-white" />
            )}
          </button>
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* TOAST */}
          {toast && (
            <div className="fixed top-6 right-6 z-[100] animate-fade-in">
              <div className={`bg-white rounded-2xl shadow-xl p-4 flex items-center gap-3 max-w-sm border ${toast.ok ? "border-green-200" : "border-red-200"}`}>
                <div className={`w-9 h-9 rounded-full flex items-center justify-center shrink-0 ${toast.ok ? "bg-green-100" : "bg-red-100"}`}>
                  {toast.ok ? (
                    <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  ) : (
                    <svg className="w-5 h-5 text-red-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                    </svg>
                  )}
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

          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            {[
              { label: "Tổng số đơn",  value: stats.total,    sub: "Tất cả đơn nghỉ",     color: "text-slate-900", bg: "bg-red-50",    icon: "M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5", iconColor: "text-primary" },
              { label: "Đang chờ",     value: stats.pending,  sub: "Chờ phê duyệt",        color: "text-amber-600", bg: "bg-amber-50",  icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z",                        iconColor: "text-amber-500" },
              { label: "Đã duyệt",     value: stats.approved, sub: "Đơn đã chấp thuận",    color: "text-green-600", bg: "bg-green-50",  icon: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z",          iconColor: "text-green-500" },
              { label: "Từ chối",      value: stats.rejected, sub: "Đơn không chấp thuận", color: "text-red-600",   bg: "bg-red-50",    icon: "M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z",  iconColor: "text-red-500" },
            ].map((s) => (
              <div key={s.label} className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between">
                <div>
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">{s.label}</span>
                  <span className={`text-3xl font-black ${s.color} block mt-1`}>{s.value}</span>
                  <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">{s.sub}</span>
                </div>
                <div className={`w-12 h-12 rounded-xl ${s.bg} flex items-center justify-center shrink-0`}>
                  <svg className={`w-6 h-6 ${s.iconColor}`} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d={s.icon} />
                  </svg>
                </div>
              </div>
            ))}
          </div>

          {/* FILTER TOOLBAR */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3 shrink-0">
            <div className="flex flex-col lg:flex-row items-stretch lg:items-center gap-4">
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
                  <option value="Cancelled">Đã hủy</option>
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
                  <input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)}
                    className="pl-9 pr-3 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 w-36" />
                </div>
                <span className="text-slate-400 font-bold">-</span>
                <input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)}
                  className="pl-3 pr-3 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 w-36" />
              </div>

              {(statusFilter !== "All" || searchQuery || dateFrom || dateTo) && (
                <button onClick={clearFilters}
                  className="flex items-center gap-1.5 px-3 py-2 text-[13px] font-bold text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg transition-all cursor-pointer whitespace-nowrap">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                  Xóa lọc
                </button>
              )}
            </div>

            {/* Row 2: Items per page + result count */}
            <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
              <span>Hiển thị</span>
              <div className="relative">
                <select value={itemsPerPage} onChange={(e) => setItemsPerPage(Number(e.target.value))}
                  className="appearance-none pl-3 pr-7 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:outline-none font-bold text-slate-600 cursor-pointer">
                  {[5, 10, 20].map((n) => <option key={n} value={n}>{n}</option>)}
                </select>
                <span className="absolute inset-y-0 right-2 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
              <span>/ trang</span>
              <span className="text-slate-300 mx-1">·</span>
              <span>Tìm thấy <span className="text-slate-600 font-bold">{filteredLeaves.length}</span> kết quả</span>
            </div>
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
                <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                  {isLoading ? (
                    <tr>
                      <td colSpan={9} className="px-5 py-14 text-center">
                        <div className="flex items-center justify-center gap-2 text-slate-400 font-bold">
                          <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/>
                          </svg>
                          Đang tải danh sách đơn nghỉ...
                        </div>
                      </td>
                    </tr>
                  ) : paginatedLeaves.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="px-5 py-12 text-center text-slate-400 font-bold">
                        {leaves.length === 0 ? "Chưa có đơn nghỉ phép nào." : "Không tìm thấy đơn nào khớp với bộ lọc."}
                      </td>
                    </tr>
                  ) : (
                    paginatedLeaves.map((leave) => {
                      const status    = STATUS_STYLES[leave.status] ?? STATUS_STYLES["Pending"];
                      const typeColor = LEAVE_TYPE_COLORS[leave.leaveType] ?? "bg-slate-100 text-slate-600 border-slate-200";
                      const typeLabel = LEAVE_LABEL[leave.leaveType] ?? leave.leaveType;

                      return (
                        <tr key={leave.id} className="hover:bg-slate-50/30 transition-colors">
                          <td className="px-5 py-4">
                            <span className="font-black text-primary text-[13px]">{leave.code}</span>
                          </td>
                          <td className="px-5 py-4">
                            <div className="flex items-center gap-3">
                              <div className={`w-9 h-9 rounded-full ${getAvatarColor(leave.userFullName)} flex items-center justify-center shrink-0 text-white text-[11px] font-black`}>
                                {getInitials(leave.userFullName)}
                              </div>
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 truncate">{leave.userFullName}</div>
                                <div className="text-[11px] text-slate-400 font-semibold truncate">{leave.department ?? "—"}</div>
                              </div>
                            </div>
                          </td>
                          <td className="px-5 py-4">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${typeColor}`}>
                              {typeLabel}
                            </span>
                          </td>
                          <td className="px-5 py-4 font-bold text-slate-700 text-[13px]">{fmtDate(leave.startDate)}</td>
                          <td className="px-5 py-4 font-bold text-slate-700 text-[13px]">{fmtDate(leave.endDate)}</td>
                          <td className="px-5 py-4 text-center">
                            <span className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-slate-100 text-slate-700 font-black text-[13px]">
                              {leave.daysCount}
                            </span>
                          </td>
                          <td className="px-5 py-4 max-w-[200px]">
                            <p className="text-[12px] text-slate-500 font-semibold leading-relaxed line-clamp-2">{leave.reason}</p>
                          </td>
                          <td className="px-5 py-4 text-center">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${status.className}`}>
                              {status.label}
                            </span>
                          </td>
                          <td className="px-5 py-4 text-center">
                            <Link
                              href={`/owner/leaves/${leave.id}`}
                              title={leave.status === "Pending" ? "Xem & duyệt đơn" : "Xem chi tiết"}
                              className="relative p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all inline-block cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                              </svg>
                              {leave.status === "Pending" && (
                                <span className="absolute -top-0.5 -right-0.5 w-2.5 h-2.5 bg-amber-400 rounded-full border border-white" />
                              )}
                            </Link>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {!isLoading && filteredLeaves.length > 0 && (
              <div className="border-t border-slate-100 px-5 py-3 flex flex-col sm:flex-row items-center justify-between gap-3">
                <span className="text-[12px] text-slate-400 font-semibold">
                  Hiển thị{" "}
                  <span className="font-black text-slate-600">
                    {(currentPage - 1) * itemsPerPage + 1}–{Math.min(currentPage * itemsPerPage, filteredLeaves.length)}
                  </span>{" "}
                  trong <span className="font-black text-slate-600">{filteredLeaves.length}</span> đơn nghỉ
                </span>
                {totalPages > 1 && (
                  <div className="flex items-center gap-1.5">
                    <button onClick={() => setCurrentPage((p) => Math.max(1, p - 1))} disabled={currentPage === 1}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer transition-all">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                      </svg>
                    </button>
                    {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                      <button key={page} onClick={() => setCurrentPage(page)}
                        className={`w-9 h-9 text-[13px] font-bold rounded-xl cursor-pointer transition-all ${
                          page === currentPage ? "bg-primary text-white shadow-md shadow-primary/20" : "text-slate-500 hover:bg-slate-100"
                        }`}>
                        {page}
                      </button>
                    ))}
                    <button onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer transition-all">
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
