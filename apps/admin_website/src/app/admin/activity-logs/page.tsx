"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import { getActivityLogsApi, type ActivityLogItemDto } from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";

type ActionType = "login" | "create" | "edit" | "delete" | "export" | "view" | "permission" | "approve" | "reject" | "cancel" | "payment";
type ModuleType = "account" | "service" | "post" | "schedule" | "system" | "appointment" | "room" | "medicine" | "inventory" | "leave" | "invoice" | "feedback" | "promotion";
type StatusType = "success" | "failed" | "warning";

const ACTION_CONFIG: Record<string, { label: string; badgeClass: string; dot: string; iconPath: string }> = {
  login: {
    label: "Đăng nhập",
    badgeClass: "bg-green-50 text-green-700 border border-green-100",
    dot: "bg-green-500",
    iconPath: "M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9",
  },
  create: {
    label: "Tạo mới",
    badgeClass: "bg-sky-50 text-sky-700 border border-sky-100",
    dot: "bg-sky-500",
    iconPath: "M12 4.5v15m7.5-7.5h-15",
  },
  edit: {
    label: "Chỉnh sửa",
    badgeClass: "bg-amber-50 text-amber-700 border border-amber-100",
    dot: "bg-amber-500",
    iconPath: "M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z",
  },
  delete: {
    label: "Xóa",
    badgeClass: "bg-red-50 text-red-700 border border-red-100",
    dot: "bg-red-500",
    iconPath: "M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0",
  },
  export: {
    label: "Xuất dữ liệu",
    badgeClass: "bg-violet-50 text-violet-700 border border-violet-100",
    dot: "bg-violet-500",
    iconPath: "M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3",
  },
  view: {
    label: "Xem",
    badgeClass: "bg-slate-100 text-slate-600 border border-slate-200",
    dot: "bg-slate-400",
    iconPath: "M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z",
  },
  permission: {
    label: "Phân quyền",
    badgeClass: "bg-orange-50 text-orange-700 border border-orange-100",
    dot: "bg-orange-500",
    iconPath: "M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.751A11.956 11.956 0 0112 2.714z",
  },
  approve: {
    label: "Duyệt",
    badgeClass: "bg-emerald-50 text-emerald-700 border border-emerald-100",
    dot: "bg-emerald-500",
    iconPath: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z",
  },
  reject: {
    label: "Từ chối",
    badgeClass: "bg-rose-50 text-rose-700 border border-rose-100",
    dot: "bg-rose-500",
    iconPath: "M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z",
  },
  cancel: {
    label: "Hủy",
    badgeClass: "bg-gray-100 text-gray-600 border border-gray-200",
    dot: "bg-gray-400",
    iconPath: "M6 18L18 6M6 6l12 12",
  },
  payment: {
    label: "Thanh toán",
    badgeClass: "bg-teal-50 text-teal-700 border border-teal-100",
    dot: "bg-teal-500",
    iconPath: "M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5z",
  },
};

const MODULE_LABELS: Record<string, string> = {
  account:     "Tài khoản",
  service:     "Dịch vụ",
  post:        "Bài viết",
  schedule:    "Lịch làm việc",
  system:      "Hệ thống",
  appointment: "Lịch hẹn",
  room:        "Phòng khám",
  medicine:    "Thuốc",
  inventory:   "Kho vật tư",
  leave:       "Nghỉ phép",
  invoice:     "Hóa đơn",
  feedback:    "Phản hồi",
  promotion:   "Khuyến mãi",
};

const MODULE_COLORS: Record<string, string> = {
  account:     "bg-red-50 text-primary",
  service:     "bg-sky-50 text-secondary",
  post:        "bg-emerald-50 text-emerald-700",
  schedule:    "bg-amber-50 text-amber-700",
  system:      "bg-slate-100 text-slate-600",
  appointment: "bg-blue-50 text-blue-700",
  room:        "bg-indigo-50 text-indigo-700",
  medicine:    "bg-teal-50 text-teal-700",
  inventory:   "bg-cyan-50 text-cyan-700",
  leave:       "bg-orange-50 text-orange-700",
  invoice:     "bg-violet-50 text-violet-700",
  feedback:    "bg-rose-50 text-rose-700",
  promotion:   "bg-fuchsia-50 text-fuchsia-700",
};

const ROLE_COLORS: Record<string, string> = {
  Admin:   "bg-red-50 text-primary",
  Owner:   "bg-purple-50 text-purple-700",
  Dentist: "bg-sky-50 text-sky-700",
  Staff:   "bg-green-50 text-green-700",
  Patient: "bg-amber-50 text-amber-700",
};

const STATUS_CONFIG: Record<string, { label: string; badgeClass: string; dotClass: string }> = {
  success: { label: "Thành công", badgeClass: "bg-green-50 text-green-700 border border-green-100", dotClass: "bg-green-500" },
  failed:  { label: "Thất bại",   badgeClass: "bg-red-50 text-red-700 border border-red-100",     dotClass: "bg-red-500"   },
  warning: { label: "Cảnh báo",   badgeClass: "bg-amber-50 text-amber-700 border border-amber-100", dotClass: "bg-amber-500" },
};

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  const last = parts[parts.length - 1]?.[0] ?? "";
  const first = parts[0]?.[0] ?? "";
  return (parts.length > 1 ? last + first : first).toUpperCase();
}

function getRoleColor(role: string): string {
  return ROLE_COLORS[role] ?? "bg-slate-100 text-slate-600";
}

function getPageNumbers(current: number, total: number): (number | "...")[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  if (current <= 4) return [1, 2, 3, 4, 5, "...", total];
  if (current >= total - 3) return [1, "...", total - 4, total - 3, total - 2, total - 1, total];
  return [1, "...", current - 1, current, current + 1, "...", total];
}

function getTodayIso() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

export default function ActivityLogsPage() {
  useRequireAdmin();

  const [logs, setLogs] = useState<ActivityLogItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [actionFilter, setActionFilter] = useState("all");
  const [moduleFilter, setModuleFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [pageSize, setPageSize] = useState(10);
  const [currentPage, setCurrentPage] = useState(1);
  const [newCount, setNewCount] = useState(0);

  // Stats (today's data)
  const [stats, setStats] = useState({ todayTotal: 0, todaySuccess: 0, warnings: 0, activeUsers: 0 });

  const currentPageRef = useRef(currentPage);
  useEffect(() => { currentPageRef.current = currentPage; }, [currentPage]);

  const resetPage = () => setCurrentPage(1);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getActivityLogsApi({
        action:    actionFilter !== "all" ? actionFilter : undefined,
        module:    moduleFilter !== "all" ? moduleFilter : undefined,
        status:    statusFilter !== "all" ? statusFilter : undefined,
        search:    searchQuery || undefined,
        startDate: dateFrom ? `${dateFrom}T00:00:00+07:00` : undefined,
        endDate:   dateTo ? `${dateTo}T23:59:59+07:00` : undefined,
        page:      currentPage,
        pageSize,
      });
      setLogs(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi không xác định");
    } finally {
      setLoading(false);
    }
  }, [actionFilter, moduleFilter, statusFilter, searchQuery, dateFrom, dateTo, currentPage, pageSize]);

  const fetchStats = useCallback(async () => {
    const today = getTodayIso();
    try {
      const [todayData, warningData] = await Promise.all([
        getActivityLogsApi({ startDate: `${today}T00:00:00+07:00`, endDate: `${today}T23:59:59+07:00`, pageSize: 1000 }),
        getActivityLogsApi({ status: "failed", pageSize: 1, page: 1 }),
      ]);
      const warningCount1 = warningData.totalCount;
      const warningData2 = await getActivityLogsApi({ status: "warning", pageSize: 1, page: 1 });
      const todayItems = todayData.items;
      const activeUsers = new Set(todayItems.map((l) => l.userId ?? l.userName)).size;
      setStats({
        todayTotal: todayData.totalCount,
        todaySuccess: todayItems.filter((l) => l.status === "success").length,
        warnings: warningCount1 + warningData2.totalCount,
        activeUsers,
      });
    } catch {
      // Stats are optional — don't block the main view
    }
  }, []);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  // Realtime subscription — Supabase INSERT on ActivityLogs
  useEffect(() => {
    const channel = supabase
      .channel("activity-logs-realtime")
      .on(
        "postgres_changes",
        { event: "INSERT", schema: "public", table: "ActivityLogs" },
        () => {
          if (currentPageRef.current === 1) {
            // On page 1: auto-refresh to show newest row at top
            fetchLogs();
            fetchStats();
            setNewCount(0);
          } else {
            // On other pages: show badge so user can navigate back
            setNewCount((c) => c + 1);
            fetchStats();
          }
        }
      )
      .subscribe();

    return () => {
      supabase.removeChannel(channel);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleGoToLatest = () => {
    setCurrentPage(1);
    setNewCount(0);
  };

  const formatTimestamp = (ts: string) => {
    const d = new Date(ts);
    const pad = (n: number) => String(n).padStart(2, "0");
    return {
      date: `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`,
      time: `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`,
    };
  };

  const handleExport = async () => {
    try {
      const all = await getActivityLogsApi({
        action:    actionFilter !== "all" ? actionFilter : undefined,
        module:    moduleFilter !== "all" ? moduleFilter : undefined,
        status:    statusFilter !== "all" ? statusFilter : undefined,
        search:    searchQuery || undefined,
        startDate: dateFrom ? `${dateFrom}T00:00:00+07:00` : undefined,
        endDate:   dateTo ? `${dateTo}T23:59:59+07:00` : undefined,
        pageSize: 10000,
        page: 1,
      });
      const headers = "ID,Thoi gian,Nguoi dung,Vai tro,Hanh dong,Phan he,Mo ta,Dia chi IP,Trang thai\n";
      const rows = all.items
        .map((log) => {
          const { date, time } = formatTimestamp(log.createdAt);
          const actionLabel = ACTION_CONFIG[log.action]?.label ?? log.action;
          const moduleLabel = MODULE_LABELS[log.module] ?? log.module;
          const statusLabel = STATUS_CONFIG[log.status]?.label ?? log.status;
          return `"${log.id}","${date} ${time}","${log.userName}","${log.userRole}","${actionLabel}","${moduleLabel}","${log.description}","${log.ipAddress ?? ""}","${statusLabel}"`;
        })
        .join("\n");
      const csv = "data:text/csv;charset=utf-8,﻿" + encodeURIComponent(headers + rows);
      const link = document.createElement("a");
      link.setAttribute("href", csv);
      link.setAttribute("download", "AuditLog.csv");
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch {
      // silently ignore export errors
    }
  };

  const safePage = Math.min(currentPage, totalPages);
  const startIndex = (safePage - 1) * pageSize;
  const pageNumbers = getPageNumbers(safePage, totalPages);

  const selectClass =
    "w-full px-4 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="history-audit" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-16 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex flex-col">
            <div className="flex items-center gap-2.5">
              <h1 className="text-[18px] font-black text-slate-900 leading-tight">Audit Log</h1>
              <span className="flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-green-50 border border-green-100 text-[10.5px] font-extrabold text-green-600 uppercase tracking-wider">
                <span className="relative flex h-1.5 w-1.5">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-green-500"></span>
                </span>
                Realtime
              </span>
            </div>
            <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">Theo dõi toàn bộ thao tác và sự kiện hệ thống</p>
          </div>
          <NotificationBell />
        </header>

        {/* NEW ACTIVITY BANNER — shown when user is not on page 1 */}
        {newCount > 0 && (
          <div className="sticky top-16 z-10 mx-8 mt-0">
            <button
              onClick={handleGoToLatest}
              className="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-primary text-white text-[13px] font-bold rounded-b-2xl shadow-lg hover:bg-primary/90 transition-all animate-fade-in cursor-pointer"
            >
              <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 19.5v-15m0 0l-6.75 6.75M12 4.5l6.75 6.75" />
              </svg>
              {newCount} hoạt động mới — Nhấn để xem mới nhất
            </button>
          </div>
        )}

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Hoạt động hôm nay</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.todayTotal}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Thao tác được ghi nhận</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-sky-50 text-secondary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 010 3.75H5.625a1.875 1.875 0 010-3.75z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Thành công</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.todaySuccess}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Thao tác hôm nay</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-green-50 text-green-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Cảnh báo &amp; Lỗi</span>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-3xl font-black text-slate-900 leading-none">{stats.warnings}</span>
                  {stats.warnings > 0 && (
                    <span className="relative flex h-3 w-3 mb-0.5">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-3 w-3 bg-amber-500"></span>
                    </span>
                  )}
                </div>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Cần kiểm tra lại</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Người dùng hôm nay</span>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-3xl font-black text-slate-900 leading-none">{stats.activeUsers}</span>
                  <span className="relative flex h-3.5 w-3.5 mb-0.5">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-3.5 w-3.5 bg-green-500"></span>
                  </span>
                </div>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Nhân sự đã đăng nhập</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
              </div>
            </div>
          </div>

          {/* TOOLBAR */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3 shrink-0">
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 flex-wrap">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm tên, mô tả, địa chỉ IP..."
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); resetPage(); }}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
                />
              </div>

              {/* Action */}
              <div className="relative sm:w-44">
                <select value={actionFilter} onChange={(e) => { setActionFilter(e.target.value); resetPage(); }} className={selectClass}>
                  <option value="all">Tất cả thao tác</option>
                  <option value="login">Đăng nhập</option>
                  <option value="create">Tạo mới</option>
                  <option value="edit">Chỉnh sửa</option>
                  <option value="delete">Xóa</option>
                  <option value="approve">Duyệt</option>
                  <option value="reject">Từ chối</option>
                  <option value="cancel">Hủy</option>
                  <option value="payment">Thanh toán</option>
                  <option value="export">Xuất dữ liệu</option>
                  <option value="view">Xem</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>

              {/* Module */}
              <div className="relative sm:w-44">
                <select value={moduleFilter} onChange={(e) => { setModuleFilter(e.target.value); resetPage(); }} className={selectClass}>
                  <option value="all">Tất cả phân hệ</option>
                  <option value="account">Tài khoản</option>
                  <option value="appointment">Lịch hẹn</option>
                  <option value="service">Dịch vụ</option>
                  <option value="post">Bài viết</option>
                  <option value="schedule">Lịch làm việc</option>
                  <option value="room">Phòng khám</option>
                  <option value="medicine">Thuốc</option>
                  <option value="inventory">Kho vật tư</option>
                  <option value="leave">Nghỉ phép</option>
                  <option value="invoice">Hóa đơn</option>
                  <option value="feedback">Phản hồi</option>
                  <option value="promotion">Khuyến mãi</option>
                  <option value="system">Hệ thống</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>

              {/* Status */}
              <div className="relative sm:w-40">
                <select value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value); resetPage(); }} className={selectClass}>
                  <option value="all">Tất cả trạng thái</option>
                  <option value="success">Thành công</option>
                  <option value="warning">Cảnh báo</option>
                  <option value="failed">Thất bại</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>

              {/* Date range */}
              <div className="flex items-center gap-1.5 sm:w-auto">
                <input
                  type="date"
                  value={dateFrom}
                  onChange={(e) => { setDateFrom(e.target.value); resetPage(); }}
                  max={dateTo || undefined}
                  className="px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 cursor-pointer"
                />
                <span className="text-slate-300 font-bold text-[13px]">–</span>
                <input
                  type="date"
                  value={dateTo}
                  onChange={(e) => { setDateTo(e.target.value); resetPage(); }}
                  min={dateFrom || undefined}
                  className="px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 cursor-pointer"
                />
                {(dateFrom || dateTo) && (
                  <button
                    type="button"
                    onClick={() => { setDateFrom(""); setDateTo(""); resetPage(); }}
                    title="Xóa bộ lọc ngày"
                    className="p-1.5 text-slate-400 hover:text-primary hover:bg-red-50 rounded-lg transition-all cursor-pointer shrink-0"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                )}
              </div>
            </div>

            {/* Row 2: Page size + count + export */}
            <div className="flex items-center justify-between gap-3 flex-wrap">
              <div className="flex items-center gap-2.5">
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">Hiển thị</span>
                <div className="relative">
                  <select
                    value={pageSize}
                    onChange={(e) => { setPageSize(Number(e.target.value)); resetPage(); }}
                    className="pl-3 pr-7 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none cursor-pointer"
                  >
                    {PAGE_SIZE_OPTIONS.map((n) => (
                      <option key={n} value={n}>{n}</option>
                    ))}
                  </select>
                  <span className="absolute inset-y-0 right-2 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                  </span>
                </div>
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">mục / trang</span>
                <span className="text-[12.5px] text-slate-300 font-semibold">·</span>
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">
                  <span className="font-bold text-slate-600">{totalCount}</span> kết quả
                </span>
              </div>

              <button
                onClick={handleExport}
                className="flex items-center gap-2 px-4 py-2 bg-white hover:bg-slate-50 text-slate-600 hover:text-slate-900 text-[13px] font-bold border border-slate-200 rounded-xl transition-all shadow-sm cursor-pointer whitespace-nowrap"
              >
                <svg className="w-4 h-4 shrink-0 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
                </svg>
                Xuất CSV
              </button>
            </div>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13px] min-w-[900px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 select-none text-[11px]">
                    <th className="px-5 py-4 w-[145px]">Thời gian</th>
                    <th className="px-5 py-4 w-[195px]">Người dùng</th>
                    <th className="px-5 py-4 w-[135px]">Loại thao tác</th>
                    <th className="px-5 py-4 w-[125px]">Phân hệ</th>
                    <th className="px-5 py-4">Mô tả chi tiết</th>
                    <th className="px-5 py-4 w-[130px]">Địa chỉ IP</th>
                    <th className="px-5 py-4 w-[120px] text-center">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {loading ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-16 text-center">
                        <div className="flex flex-col items-center gap-3">
                          <div className="w-8 h-8 rounded-full border-2 border-primary border-t-transparent animate-spin" />
                          <div className="text-[13px] font-semibold text-slate-400">Đang tải dữ liệu...</div>
                        </div>
                      </td>
                    </tr>
                  ) : error ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-16 text-center">
                        <div className="text-[14px] font-bold text-red-500">{error}</div>
                      </td>
                    </tr>
                  ) : logs.length > 0 ? (
                    logs.map((log) => {
                      const { date, time } = formatTimestamp(log.createdAt);
                      const actionCfg = ACTION_CONFIG[log.action] ?? {
                        label: log.action,
                        badgeClass: "bg-slate-100 text-slate-600 border border-slate-200",
                        dot: "bg-slate-400",
                        iconPath: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z",
                      };
                      const statusCfg = STATUS_CONFIG[log.status] ?? STATUS_CONFIG.warning;
                      const moduleColor = MODULE_COLORS[log.module] ?? "bg-slate-100 text-slate-600";
                      const moduleLabel = MODULE_LABELS[log.module] ?? log.module;
                      const roleColor = getRoleColor(log.userRole);
                      const initials = getInitials(log.userName);
                      const rowBg =
                        log.status === "failed"
                          ? "bg-red-50/30 hover:bg-red-50/50"
                          : log.status === "warning"
                          ? "bg-amber-50/20 hover:bg-amber-50/40"
                          : "hover:bg-slate-50/40";

                      return (
                        <tr key={log.id} className={`transition-colors ${rowBg}`}>
                          <td className="px-5 py-3.5">
                            <div className="text-[12.5px] font-bold text-slate-800">{date}</div>
                            <div className="text-[11.5px] font-mono font-semibold text-slate-400 mt-0.5">{time}</div>
                          </td>

                          <td className="px-5 py-3.5">
                            <div className="flex items-center gap-2.5">
                              <div className={`w-8 h-8 rounded-full ${roleColor} flex items-center justify-center font-black text-[10px] shrink-0 border border-white shadow-sm`}>
                                {initials}
                              </div>
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 text-[13px] truncate">{log.userName}</div>
                                <div className="text-[11px] text-slate-400 font-semibold mt-0.5">{log.userRole}</div>
                              </div>
                            </div>
                          </td>

                          <td className="px-5 py-3.5">
                            <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black whitespace-nowrap ${actionCfg.badgeClass}`}>
                              <svg className="w-3 h-3 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d={actionCfg.iconPath} />
                              </svg>
                              {actionCfg.label}
                            </span>
                          </td>

                          <td className="px-5 py-3.5">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold whitespace-nowrap ${moduleColor}`}>
                              {moduleLabel}
                            </span>
                          </td>

                          <td className="px-5 py-3.5">
                            <span className="text-[13px] text-slate-700 font-medium leading-relaxed">{log.description}</span>
                          </td>

                          <td className="px-5 py-3.5">
                            <span className="font-mono text-[12px] text-slate-500 font-semibold">{log.ipAddress ?? "—"}</span>
                          </td>

                          <td className="px-5 py-3.5 text-center">
                            <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black whitespace-nowrap ${statusCfg.badgeClass}`}>
                              <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${statusCfg.dotClass}`}></span>
                              {statusCfg.label}
                            </span>
                          </td>
                        </tr>
                      );
                    })
                  ) : (
                    <tr>
                      <td colSpan={7} className="px-6 py-16 text-center">
                        <div className="flex flex-col items-center gap-3">
                          <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center">
                            <svg className="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                            </svg>
                          </div>
                          <div className="text-[14px] font-bold text-slate-500">Không tìm thấy hoạt động nào phù hợp.</div>
                          <div className="text-[12.5px] text-slate-400 font-semibold">Thử điều chỉnh bộ lọc hoặc từ khóa tìm kiếm.</div>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {!loading && totalCount > 0 && (
              <div className="px-5 py-3.5 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 bg-slate-50/30">
                <div className="flex items-center gap-3">
                  <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">
                    {startIndex + 1}–{Math.min(startIndex + pageSize, totalCount)} trong{" "}
                    <span className="font-bold text-slate-600">{totalCount}</span> kết quả
                  </span>
                  <span className="text-slate-200">|</span>
                  <div className="flex items-center gap-1.5 text-[11.5px] text-slate-400 font-semibold whitespace-nowrap">
                    <span className="relative flex h-2 w-2">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
                    </span>
                    Thời gian thực
                  </div>
                </div>

                <div className="flex items-center gap-1">
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={safePage === 1}
                    className="w-8 h-8 flex items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-100 hover:text-slate-900 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                  >
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                    </svg>
                  </button>

                  {pageNumbers.map((p, i) =>
                    p === "..." ? (
                      <span key={`ellipsis-${i}`} className="w-8 h-8 flex items-center justify-center text-[13px] text-slate-400 font-bold select-none">···</span>
                    ) : (
                      <button
                        key={p}
                        onClick={() => setCurrentPage(p as number)}
                        className={`w-8 h-8 flex items-center justify-center rounded-lg text-[13px] font-bold transition-all border ${
                          safePage === p
                            ? "bg-primary text-white border-primary shadow-sm"
                            : "border-slate-200 text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                        }`}
                      >
                        {p}
                      </button>
                    )
                  )}

                  <button
                    onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                    disabled={safePage === totalPages}
                    className="w-8 h-8 flex items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-100 hover:text-slate-900 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                  >
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                    </svg>
                  </button>
                </div>
              </div>
            )}
          </div>

        </div>
      </main>
    </div>
  );
}
