"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import AdminPageHeader from "../../../../components/shared/AdminPageHeader";
import Pagination from "../../../../components/shared/Pagination";
import { SortableTh, Th, type SortDir } from "../../../../components/shared/TableHeader";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import { getActivityLogsApi, type ActivityLogItemDto } from "../../../../lib/apiClient";
import { supabase } from "../../../../lib/supabaseClient";

const ACTION_CONFIG: Record<string, { label: string; badgeClass: string; iconPath: string }> = {
  login: {
    label: "Đăng nhập",
    badgeClass: "bg-green-50 text-green-700 border border-green-100",
    iconPath: "M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9",
  },
  create: {
    label: "Tạo mới",
    badgeClass: "bg-sky-50 text-sky-700 border border-sky-100",
    iconPath: "M12 4.5v15m7.5-7.5h-15",
  },
  edit: {
    label: "Chỉnh sửa",
    badgeClass: "bg-amber-50 text-amber-700 border border-amber-100",
    iconPath: "M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z",
  },
  delete: {
    label: "Xóa",
    badgeClass: "bg-red-50 text-red-700 border border-red-100",
    iconPath: "M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0",
  },
  view: {
    label: "Xem",
    badgeClass: "bg-slate-100 text-slate-600 border border-slate-200",
    iconPath: "M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z",
  },
};
const FALLBACK_ACTION = { label: "Khác", badgeClass: "bg-slate-100 text-slate-600 border border-slate-200", iconPath: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" };

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

function getTodayIso() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

export default function SystemLogPage() {
  useRequireAdmin();

  const [logs, setLogs] = useState<ActivityLogItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [pageSize, setPageSize] = useState(10);
  const [currentPage, setCurrentPage] = useState(1);
  const [newCount, setNewCount] = useState(0);
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  const [stats, setStats] = useState({ todayTotal: 0, todaySuccess: 0, warnings: 0 });

  const currentPageRef = useRef(currentPage);
  useEffect(() => { currentPageRef.current = currentPage; }, [currentPage]);

  const resetPage = () => setCurrentPage(1);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getActivityLogsApi({
        module:    "system",
        status:    statusFilter !== "all" ? statusFilter : undefined,
        search:    searchQuery || undefined,
        // Mốc nửa đêm giờ VN — API tự mở rộng endDate đến hết ngày đó
        startDate: dateFrom ? `${dateFrom}T00:00:00+07:00` : undefined,
        endDate:   dateTo ? `${dateTo}T00:00:00+07:00` : undefined,
        page:      currentPage,
        pageSize,
        sortDir,
      });
      setLogs(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi không xác định");
    } finally {
      setLoading(false);
    }
  }, [statusFilter, searchQuery, dateFrom, dateTo, currentPage, pageSize, sortDir]);

  const fetchStats = useCallback(async () => {
    const today = getTodayIso();
    try {
      const [todayData, warningData] = await Promise.all([
        getActivityLogsApi({ module: "system", startDate: `${today}T00:00:00+07:00`, endDate: `${today}T00:00:00+07:00`, pageSize: 100 }),
        getActivityLogsApi({ module: "system", status: "warning", pageSize: 1 }),
      ]);
      const failedData = await getActivityLogsApi({ module: "system", status: "failed", pageSize: 1 });
      setStats({
        todayTotal: todayData.totalCount,
        todaySuccess: todayData.items.filter((l) => l.status === "success").length,
        warnings: warningData.totalCount + failedData.totalCount,
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
      .channel("system-log-realtime")
      .on(
        "postgres_changes",
        { event: "INSERT", schema: "public", table: "ActivityLogs" },
        () => {
          if (currentPageRef.current === 1) {
            fetchLogs();
            fetchStats();
            setNewCount(0);
          } else {
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
        module:    "system",
        status:    statusFilter !== "all" ? statusFilter : undefined,
        search:    searchQuery || undefined,
        startDate: dateFrom ? `${dateFrom}T00:00:00+07:00` : undefined,
        endDate:   dateTo ? `${dateTo}T00:00:00+07:00` : undefined,
        pageSize: 100,
        page: 1,
      });
      const headers = "ID,Thoi gian,Nguoi dung,Vai tro,Hanh dong,Mo ta,Dia chi IP,Trang thai\n";
      const rows = all.items
        .map((log) => {
          const { date, time } = formatTimestamp(log.createdAt);
          const actionLabel = ACTION_CONFIG[log.action]?.label ?? log.action;
          const statusLabel = STATUS_CONFIG[log.status]?.label ?? log.status;
          return `"${log.id}","${date} ${time}","${log.userName}","${log.userRole}","${actionLabel}","${log.description}","${log.ipAddress ?? ""}","${statusLabel}"`;
        })
        .join("\n");
      const csv = "data:text/csv;charset=utf-8,﻿" + encodeURIComponent(headers + rows);
      const link = document.createElement("a");
      link.setAttribute("href", csv);
      link.setAttribute("download", "SystemLog.csv");
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch {
      // silently ignore export errors
    }
  };

  const selectClass =
    "w-full px-4 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="history-system" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <AdminPageHeader
          title="System Log"
          subtitle="Các sự kiện ở cấp hệ thống, không gắn với một nghiệp vụ cụ thể"
          right={
            <span className="flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-green-50 border border-green-100 text-[10.5px] font-extrabold text-green-600 uppercase tracking-wider">
              <span className="relative flex h-1.5 w-1.5">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-green-500"></span>
              </span>
              Realtime
            </span>
          }
        />

        {/* NEW ACTIVITY BANNER */}
        {newCount > 0 && (
          <div className="sticky top-20 z-10 mx-8 mt-0">
            <button
              onClick={handleGoToLatest}
              className="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-primary text-white text-[13px] font-bold rounded-b-2xl shadow-lg hover:bg-primary/90 transition-all animate-fade-in cursor-pointer"
            >
              <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 19.5v-15m0 0l-6.75 6.75M12 4.5l6.75 6.75" />
              </svg>
              {newCount} sự kiện mới — Nhấn để xem mới nhất
            </button>
          </div>
        )}

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Sự kiện hôm nay</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.todayTotal}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Ghi nhận ở cấp hệ thống</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-slate-100 text-slate-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9.348 14.652a3.75 3.75 0 010-5.304m5.304 0a3.75 3.75 0 010 5.304m-7.425 2.121a6.75 6.75 0 010-9.546m9.546 0a6.75 6.75 0 010 9.546M12 12h.008v.008H12V12z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Thành công</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.todaySuccess}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Sự kiện hôm nay</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-green-50 text-green-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Cảnh báo & Lỗi</span>
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
          </div>

          {/* TOOLBAR */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3 shrink-0">
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 flex-wrap">
              <div className="relative flex-1 min-w-[200px]">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm mô tả, địa chỉ IP..."
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); resetPage(); }}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
                />
              </div>

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
              <table className="w-full text-left border-collapse text-[13px] min-w-[820px]">
                <thead>
                  <tr className="bg-slate-50/70 border-b border-slate-200/80 select-none">
                    <SortableTh
                      column="createdAt"
                      label="Thời gian"
                      sortKey="createdAt"
                      sortDir={sortDir}
                      onSort={() => setSortDir((d) => (d === "asc" ? "desc" : "asc"))}
                      className="px-5 w-[145px]"
                    />
                    <Th className="px-5 w-[195px]">Người dùng</Th>
                    <Th className="px-5 w-[135px]">Loại thao tác</Th>
                    <Th className="px-5">Mô tả chi tiết</Th>
                    <Th className="px-5 w-[130px]">Địa chỉ IP</Th>
                    <Th className="px-5 w-[120px]" align="center">Trạng thái</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {loading ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-16 text-center">
                        <div className="flex flex-col items-center gap-3">
                          <div className="w-8 h-8 rounded-full border-2 border-primary border-t-transparent animate-spin" />
                          <div className="text-[13px] font-semibold text-slate-400">Đang tải dữ liệu...</div>
                        </div>
                      </td>
                    </tr>
                  ) : error ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-16 text-center">
                        <div className="text-[14px] font-bold text-red-500">{error}</div>
                      </td>
                    </tr>
                  ) : logs.length > 0 ? (
                    logs.map((log) => {
                      const actionCfg = ACTION_CONFIG[log.action] ?? FALLBACK_ACTION;
                      const statusCfg = STATUS_CONFIG[log.status] ?? STATUS_CONFIG.warning;
                      const roleColor = getRoleColor(log.userRole);
                      const initials = getInitials(log.userName);
                      const { date, time } = formatTimestamp(log.createdAt);
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
                      <td colSpan={6} className="px-6 py-16 text-center">
                        <div className="flex flex-col items-center gap-3">
                          <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center">
                            <svg className="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                            </svg>
                          </div>
                          <div className="text-[14px] font-bold text-slate-500">Không tìm thấy sự kiện hệ thống nào phù hợp.</div>
                          <div className="text-[12.5px] text-slate-400 font-semibold">Thử điều chỉnh bộ lọc hoặc từ khóa tìm kiếm.</div>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {!loading && totalCount > 0 && (
              <div className="px-5 py-3.5 border-t border-slate-100 bg-slate-50/30 flex flex-col sm:flex-row items-center gap-3">
                <div className="flex items-center gap-1.5 text-[11.5px] text-slate-400 font-semibold whitespace-nowrap shrink-0">
                  <span className="relative flex h-2 w-2">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
                  </span>
                  Thời gian thực
                </div>
                <Pagination
                  currentPage={currentPage}
                  totalCount={totalCount}
                  pageSize={pageSize}
                  onPageChange={setCurrentPage}
                  itemLabel="kết quả"
                  className="flex-1"
                />
              </div>
            )}
          </div>

        </div>
      </main>
    </div>
  );
}
