"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import {
  getNotificationsApi,
  markNotificationReadApi,
  markAllNotificationsReadApi,
  type NotificationPagedDto,
} from "../../../lib/apiClient";

type NotifType = "system" | "account" | "schedule" | "service" | "security" | "reminder" | "appointment";
type PriorityType = "high" | "medium" | "low";

const TYPE_CONFIG: Record<NotifType, { label: string; iconPath: string; iconBg: string; iconColor: string; borderColor: string }> = {
  system: {
    label: "Hệ thống",
    iconBg: "bg-slate-100",
    iconColor: "text-slate-600",
    borderColor: "border-slate-400",
    iconPath: "M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z",
  },
  account: {
    label: "Tài khoản",
    iconBg: "bg-red-50",
    iconColor: "text-primary",
    borderColor: "border-red-400",
    iconPath: "M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z",
  },
  schedule: {
    label: "Lịch làm việc",
    iconBg: "bg-amber-50",
    iconColor: "text-amber-600",
    borderColor: "border-amber-400",
    iconPath: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5",
  },
  service: {
    label: "Dịch vụ",
    iconBg: "bg-sky-50",
    iconColor: "text-secondary",
    borderColor: "border-sky-400",
    iconPath: "M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.83-5.83m0 0a2.95 2.95 0 11-4.174-4.172 2.95 2.95 0 014.174 4.172zm-7.42 7.42l9.39-9.39",
  },
  security: {
    label: "Bảo mật",
    iconBg: "bg-rose-50",
    iconColor: "text-rose-600",
    borderColor: "border-rose-500",
    iconPath: "M12 9v3.75m0-10.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.75c0 5.592 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.75h-.152c-3.196 0-6.1-1.25-8.25-3.286zm0 13.036h.008v.008H12v-.008z",
  },
  reminder: {
    label: "Nhắc nhở",
    iconBg: "bg-violet-50",
    iconColor: "text-violet-600",
    borderColor: "border-violet-400",
    iconPath: "M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0",
  },
  appointment: {
    label: "Lịch hẹn",
    iconBg: "bg-sky-50",
    iconColor: "text-sky-600",
    borderColor: "border-sky-400",
    iconPath: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5",
  },
};

const FALLBACK_TYPE: typeof TYPE_CONFIG[NotifType] = TYPE_CONFIG.system;

const PRIORITY_CONFIG: Record<PriorityType, { label: string; badgeClass: string; dotClass: string }> = {
  high:   { label: "Cao",   badgeClass: "bg-red-50 text-red-700 border border-red-100",      dotClass: "bg-red-500"    },
  medium: { label: "Vừa",   badgeClass: "bg-amber-50 text-amber-700 border border-amber-100", dotClass: "bg-amber-500" },
  low:    { label: "Thấp",  badgeClass: "bg-slate-100 text-slate-500 border border-slate-200", dotClass: "bg-slate-400" },
};

const FALLBACK_PRIORITY: typeof PRIORITY_CONFIG[PriorityType] = PRIORITY_CONFIG.low;

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

function getPageNumbers(current: number, total: number): (number | "...")[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  if (current <= 4) return [1, 2, 3, 4, 5, "...", total];
  if (current >= total - 3) return [1, "...", total - 4, total - 3, total - 2, total - 1, total];
  return [1, "...", current - 1, current, current + 1, "...", total];
}

function timeAgo(ts: string): string {
  const diff = Math.floor((Date.now() - new Date(ts).getTime()) / 1000);
  if (diff < 60) return "Vừa xong";
  if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
  if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
  return `${Math.floor(diff / 86400)} ngày trước`;
}

function capitalize(s: string) {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

interface Stats {
  total: number;
  unread: number;
  highPriorityUnread: number;
  securityCount: number;
}

export default function NotificationsPage() {
  useRequireAdmin();

  const [data, setData] = useState<NotificationPagedDto | null>(null);
  const [stats, setStats] = useState<Stats>({ total: 0, unread: 0, highPriorityUnread: 0, securityCount: 0 });
  const [loading, setLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [typeFilter, setTypeFilter] = useState("all");
  const [priorityFilter, setPriorityFilter] = useState("all");
  const [readFilter, setReadFilter] = useState("all");
  const [pageSize, setPageSize] = useState(10);
  const [currentPage, setCurrentPage] = useState(1);

  const resetPage = () => setCurrentPage(1);

  const fetchListRef = useRef<() => Promise<void>>(async () => {});
  const fetchStatsRef = useRef<() => Promise<void>>(async () => {});

  const fetchList = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getNotificationsApi({
        type:     typeFilter !== "all" ? capitalize(typeFilter) : undefined,
        priority: priorityFilter !== "all" ? capitalize(priorityFilter) : undefined,
        isRead:   readFilter === "all" ? undefined : readFilter === "read",
        search:   searchQuery || undefined,
        page:     currentPage,
        pageSize,
      });
      setData(res);
    } catch { /* silently ignore fetch errors */ }
    finally { setLoading(false); }
  }, [typeFilter, priorityFilter, readFilter, searchQuery, currentPage, pageSize]);

  const fetchStats = useCallback(async () => {
    try {
      const [baseRes, highRes, secRes] = await Promise.all([
        getNotificationsApi({ pageSize: 1 }),
        getNotificationsApi({ priority: "High", isRead: false, pageSize: 1 }),
        getNotificationsApi({ type: "Security", pageSize: 1 }),
      ]);
      setStats({
        total: baseRes.totalCount,
        unread: baseRes.unreadCount,
        highPriorityUnread: highRes.totalCount,
        securityCount: secRes.totalCount,
      });
    } catch { /* silently ignore */ }
  }, []);

  useEffect(() => { fetchListRef.current = fetchList; }, [fetchList]);
  useEffect(() => { fetchStatsRef.current = fetchStats; }, [fetchStats]);

  useEffect(() => { fetchListRef.current(); }, [fetchList]);
  useEffect(() => { fetchStatsRef.current(); }, []);

  const refresh = useCallback(() => {
    fetchListRef.current();
    fetchStatsRef.current();
  }, []);

  const markRead = async (id: string) => {
    try {
      await markNotificationReadApi(id);
      refresh();
    } catch { /* ignore */ }
  };

  const markAllRead = async () => {
    try {
      await markAllNotificationsReadApi();
      refresh();
    } catch { /* ignore */ }
  };

  const unreadCount = stats.unread;
  const totalPages = data?.totalPages ?? 1;
  const safePage = Math.min(currentPage, Math.max(1, totalPages));
  const startIndex = (safePage - 1) * pageSize;
  const pageNumbers = getPageNumbers(safePage, totalPages);
  const pagedNotifs = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const selectClass =
    "w-full px-4 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="notifications" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-16 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex flex-col">
            <h1 className="text-[18px] font-black text-slate-900 leading-tight">Thông Báo</h1>
            <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">Theo dõi các cảnh báo và thông báo từ hệ thống</p>
          </div>
          <div className="flex items-center gap-3">
            <NotificationBell />
          </div>
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng thông báo</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.total}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Toàn bộ hệ thống</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-sky-50 text-secondary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Chưa đọc</span>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-3xl font-black text-slate-900 leading-none">{unreadCount}</span>
                  {unreadCount > 0 && (
                    <span className="relative flex h-3 w-3 mb-0.5">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-3 w-3 bg-primary"></span>
                    </span>
                  )}
                </div>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Cần xem xét</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 9v.906a2.25 2.25 0 01-1.183 1.981l-6.478 3.488M2.25 9v.906a2.25 2.25 0 001.183 1.981l6.478 3.488m8.839 2.51l-4.66-2.51m0 0l-1.023-.55a2.25 2.25 0 00-2.134 0l-1.022.55m0 0l-4.661 2.51m16.5 1.615a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V8.844a2.25 2.25 0 011.183-1.98l7.5-4.04a2.25 2.25 0 012.134 0l7.5 4.04a2.25 2.25 0 011.183 1.98V19.5z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Ưu tiên cao</span>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-3xl font-black text-slate-900 leading-none">{stats.highPriorityUnread}</span>
                  {stats.highPriorityUnread > 0 && (
                    <span className="relative flex h-3 w-3 mb-0.5">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-3 w-3 bg-amber-500"></span>
                    </span>
                  )}
                </div>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Chưa đọc, cần xử lý</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Bảo mật</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.securityCount}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Cảnh báo bảo mật</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m0-10.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.75c0 5.592 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.75h-.152c-3.196 0-6.1-1.25-8.25-3.286zm0 13.036h.008v.008H12v-.008z" />
                </svg>
              </div>
            </div>
          </div>

          {/* TOOLBAR */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3 shrink-0">
            {/* Row 1: Search + Filters */}
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
                  placeholder="Tìm tiêu đề, nội dung thông báo..."
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); resetPage(); }}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
                />
              </div>

              {/* Type */}
              <div className="relative sm:w-44">
                <select value={typeFilter} onChange={(e) => { setTypeFilter(e.target.value); resetPage(); }} className={selectClass}>
                  <option value="all">Tất cả loại</option>
                  <option value="system">Hệ thống</option>
                  <option value="account">Tài khoản</option>
                  <option value="schedule">Lịch làm việc</option>
                  <option value="service">Dịch vụ</option>
                  <option value="security">Bảo mật</option>
                  <option value="reminder">Nhắc nhở</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>

              {/* Priority */}
              <div className="relative sm:w-40">
                <select value={priorityFilter} onChange={(e) => { setPriorityFilter(e.target.value); resetPage(); }} className={selectClass}>
                  <option value="all">Tất cả mức độ</option>
                  <option value="high">Ưu tiên cao</option>
                  <option value="medium">Ưu tiên vừa</option>
                  <option value="low">Ưu tiên thấp</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>

              {/* Read status */}
              <div className="relative sm:w-40">
                <select value={readFilter} onChange={(e) => { setReadFilter(e.target.value); resetPage(); }} className={selectClass}>
                  <option value="all">Tất cả trạng thái</option>
                  <option value="unread">Chưa đọc</option>
                  <option value="read">Đã đọc</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>
            </div>

            {/* Row 2: Page size + count + mark-all */}
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
              {unreadCount > 0 && (
                <button
                  onClick={markAllRead}
                  className="flex items-center gap-1.5 px-3 py-1.5 text-[12.5px] font-bold text-slate-500 hover:text-primary hover:bg-red-50 border border-slate-200 hover:border-primary/20 rounded-lg transition-all cursor-pointer"
                >
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                  </svg>
                  Đánh dấu đã đọc tất cả
                </button>
              )}
            </div>
          </div>

          {/* NOTIFICATION LIST */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
            {loading ? (
              <div className="flex items-center justify-center py-20">
                <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
              </div>
            ) : pagedNotifs.length > 0 ? (
              <ul className="divide-y divide-slate-100">
                {pagedNotifs.map((notif) => {
                  const typeKey = notif.type.toLowerCase() as NotifType;
                  const typeCfg = TYPE_CONFIG[typeKey] ?? FALLBACK_TYPE;
                  const prioKey = notif.priority.toLowerCase() as PriorityType;
                  const priCfg = PRIORITY_CONFIG[prioKey] ?? FALLBACK_PRIORITY;
                  return (
                    <li
                      key={notif.id}
                      className={`flex items-start gap-4 px-6 py-4 transition-colors ${
                        !notif.isRead
                          ? "bg-red-50/20 hover:bg-red-50/30"
                          : "hover:bg-slate-50/50"
                      }`}
                    >
                      {/* Unread indicator bar */}
                      <div className={`self-stretch w-0.5 rounded-full shrink-0 mt-1 ${!notif.isRead ? typeCfg.borderColor.replace("border-", "bg-") : "bg-transparent"}`} />

                      {/* Icon */}
                      <div className={`w-10 h-10 rounded-xl ${typeCfg.iconBg} ${typeCfg.iconColor} flex items-center justify-center shrink-0 mt-0.5`}>
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d={typeCfg.iconPath} />
                        </svg>
                      </div>

                      {/* Content */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between gap-3 flex-wrap">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className={`text-[14px] font-black leading-snug ${!notif.isRead ? "text-slate-900" : "text-slate-700"}`}>
                              {notif.title}
                            </span>
                            {!notif.isRead && (
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10.5px] font-black bg-primary text-white">
                                MỚI
                              </span>
                            )}
                          </div>
                          <span className="text-[12px] text-slate-400 font-semibold whitespace-nowrap shrink-0">{timeAgo(notif.createdAt)}</span>
                        </div>

                        <p className={`mt-1 text-[13px] leading-relaxed ${!notif.isRead ? "text-slate-600 font-medium" : "text-slate-500"}`}>
                          {notif.body}
                        </p>

                        <div className="mt-2.5 flex items-center gap-2 flex-wrap">
                          {/* Type badge */}
                          <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-bold ${typeCfg.iconBg} ${typeCfg.iconColor}`}>
                            <svg className="w-3 h-3 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d={typeCfg.iconPath} />
                            </svg>
                            {typeCfg.label}
                          </span>

                          {/* Priority badge */}
                          <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-bold ${priCfg.badgeClass}`}>
                            <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${priCfg.dotClass}`}></span>
                            {priCfg.label}
                          </span>
                        </div>
                      </div>

                      {/* Mark read button */}
                      {!notif.isRead && (
                        <button
                          onClick={() => markRead(notif.id)}
                          title="Đánh dấu đã đọc"
                          className="shrink-0 mt-1 p-2 rounded-full text-slate-400 hover:text-primary hover:bg-red-50 transition-all cursor-pointer"
                        >
                          <svg style={{width:"1.125rem",height:"1.125rem"}} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                          </svg>
                        </button>
                      )}
                    </li>
                  );
                })}
              </ul>
            ) : (
              <div className="flex flex-col items-center gap-3 px-6 py-20">
                <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                  <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                  </svg>
                </div>
                <div className="text-[14px] font-bold text-slate-500">Không tìm thấy thông báo nào phù hợp.</div>
                <div className="text-[12.5px] text-slate-400 font-semibold">Thử điều chỉnh bộ lọc hoặc từ khóa tìm kiếm.</div>
              </div>
            )}

            {/* FOOTER — pagination */}
            {totalCount > 0 && !loading && (
              <div className="px-5 py-3.5 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 bg-slate-50/30">
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">
                  {startIndex + 1}–{Math.min(startIndex + pageSize, totalCount)} trong{" "}
                  <span className="font-bold text-slate-600">{totalCount}</span> thông báo
                </span>

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
                      <span key={`e-${i}`} className="w-8 h-8 flex items-center justify-center text-[13px] text-slate-400 font-bold select-none">
                        ···
                      </span>
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
