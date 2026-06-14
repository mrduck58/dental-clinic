"use client";

import { useState, useMemo } from "react";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";

type ActionType = "login" | "create" | "edit" | "delete" | "export" | "view" | "permission";
type ModuleType = "account" | "service" | "post" | "schedule" | "system";
type StatusType = "success" | "failed" | "warning";

interface ActivityLog {
  id: string;
  timestamp: string;
  user: { name: string; role: string; initials: string; colorClass: string };
  action: ActionType;
  module: ModuleType;
  description: string;
  ip: string;
  status: StatusType;
}

const ACTION_CONFIG: Record<ActionType, { label: string; badgeClass: string; dot: string; iconPath: string }> = {
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
};

const MODULE_LABELS: Record<ModuleType, string> = {
  account: "Tài khoản",
  service: "Dịch vụ",
  post: "Bài viết",
  schedule: "Lịch làm việc",
  system: "Hệ thống",
};

const MODULE_COLORS: Record<ModuleType, string> = {
  account: "bg-red-50 text-primary",
  service: "bg-sky-50 text-secondary",
  post: "bg-emerald-50 text-emerald-700",
  schedule: "bg-amber-50 text-amber-700",
  system: "bg-slate-100 text-slate-600",
};

const STATUS_CONFIG: Record<StatusType, { label: string; badgeClass: string; dotClass: string }> = {
  success: { label: "Thành công", badgeClass: "bg-green-50 text-green-700 border border-green-100", dotClass: "bg-green-500" },
  failed: { label: "Thất bại", badgeClass: "bg-red-50 text-red-700 border border-red-100", dotClass: "bg-red-500" },
  warning: { label: "Cảnh báo", badgeClass: "bg-amber-50 text-amber-700 border border-amber-100", dotClass: "bg-amber-500" },
};

const MOCK_LOGS: ActivityLog[] = [
  { id: "L001", timestamp: "2026-06-12T08:32:14", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "login", module: "system", description: "Đăng nhập hệ thống thành công từ trình duyệt Chrome", ip: "192.168.1.10", status: "success" },
  { id: "L002", timestamp: "2026-06-12T08:45:02", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "create", module: "service", description: "Tạo dịch vụ mới: \"Tẩy trắng răng Zoom Advanced\"", ip: "192.168.1.10", status: "success" },
  { id: "L003", timestamp: "2026-06-12T09:11:55", user: { name: "Lê Phương Thảo", role: "Bác sĩ", initials: "PT", colorClass: "bg-sky-50 text-secondary" }, action: "login", module: "system", description: "Đăng nhập hệ thống từ thiết bị di động (iOS Safari)", ip: "192.168.1.25", status: "success" },
  { id: "L004", timestamp: "2026-06-12T09:30:20", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "edit", module: "service", description: "Cập nhật giá dịch vụ \"Cấy ghép Implant\" từ 15.000.000đ → 16.500.000đ", ip: "192.168.1.10", status: "success" },
  { id: "L005", timestamp: "2026-06-12T10:05:43", user: { name: "Trần Thị Hương", role: "Lễ tân", initials: "TH", colorClass: "bg-green-50 text-green-700" }, action: "login", module: "system", description: "Đăng nhập thất bại — sai mật khẩu (lần 1/3)", ip: "192.168.1.30", status: "failed" },
  { id: "L006", timestamp: "2026-06-12T10:06:10", user: { name: "Trần Thị Hương", role: "Lễ tân", initials: "TH", colorClass: "bg-green-50 text-green-700" }, action: "login", module: "system", description: "Đăng nhập hệ thống thành công", ip: "192.168.1.30", status: "success" },
  { id: "L007", timestamp: "2026-06-12T10:22:37", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "permission", module: "account", description: "Cập nhật quyền hạn \"Lê Phương Thảo\" — bật quyền Quản lý lịch hẹn", ip: "192.168.1.10", status: "success" },
  { id: "L008", timestamp: "2026-06-12T11:00:15", user: { name: "Phạm Văn Bình", role: "Kế toán", initials: "PB", colorClass: "bg-amber-50 text-amber-700" }, action: "view", module: "service", description: "Xem báo cáo doanh thu tháng 6/2026", ip: "192.168.1.42", status: "success" },
  { id: "L009", timestamp: "2026-06-12T11:15:28", user: { name: "Phạm Văn Bình", role: "Kế toán", initials: "PB", colorClass: "bg-amber-50 text-amber-700" }, action: "export", module: "schedule", description: "Xuất file CSV lịch làm việc tuần 24/2026", ip: "192.168.1.42", status: "success" },
  { id: "L010", timestamp: "2026-06-12T13:30:05", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "create", module: "schedule", description: "Tạo lịch làm việc tuần 25 (16/06 – 22/06/2026) cho 8 nhân sự", ip: "192.168.1.10", status: "success" },
  { id: "L011", timestamp: "2026-06-12T13:52:40", user: { name: "Lê Phương Thảo", role: "Bác sĩ", initials: "PT", colorClass: "bg-sky-50 text-secondary" }, action: "create", module: "post", description: "Tạo bài viết: \"Hướng dẫn chăm sóc răng sau khi cấy Implant\"", ip: "192.168.1.25", status: "success" },
  { id: "L012", timestamp: "2026-06-12T14:20:11", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "create", module: "account", description: "Tạo tài khoản mới: \"Trần Quốc Bảo\" (Bác sĩ) — email xác nhận đã gửi", ip: "192.168.1.10", status: "success" },
  { id: "L013", timestamp: "2026-06-12T14:45:30", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "edit", module: "post", description: "Chỉnh sửa bài viết #P023 — cập nhật ảnh thumbnail và mô tả SEO", ip: "192.168.1.10", status: "success" },
  { id: "L014", timestamp: "2026-06-12T15:10:22", user: { name: "Trần Thị Hương", role: "Lễ tân", initials: "TH", colorClass: "bg-green-50 text-green-700" }, action: "view", module: "schedule", description: "Xem lịch làm việc tuần 24 — ca sáng nhân viên hành chính", ip: "192.168.1.30", status: "success" },
  { id: "L015", timestamp: "2026-06-12T15:30:00", user: { name: "Phạm Văn Bình", role: "Kế toán", initials: "PB", colorClass: "bg-amber-50 text-amber-700" }, action: "delete", module: "service", description: "Xóa chương trình khuyến mãi đã hết hạn: \"Giảm 20% tháng 5/2026\"", ip: "192.168.1.42", status: "warning" },
  { id: "L016", timestamp: "2026-06-11T17:05:18", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "edit", module: "schedule", description: "Chỉnh sửa lịch làm việc tuần 24 — đổi ca chiều ngày Thứ Năm", ip: "192.168.1.10", status: "success" },
  { id: "L017", timestamp: "2026-06-11T16:40:55", user: { name: "Lê Phương Thảo", role: "Bác sĩ", initials: "PT", colorClass: "bg-sky-50 text-secondary" }, action: "edit", module: "post", description: "Cập nhật bài viết: \"Lợi ích của việc chỉnh nha sớm cho trẻ\"", ip: "192.168.1.25", status: "success" },
  { id: "L018", timestamp: "2026-06-11T14:22:33", user: { name: "Nguyễn Minh Đức", role: "Admin", initials: "NĐ", colorClass: "bg-red-50 text-primary" }, action: "export", module: "account", description: "Xuất danh sách toàn bộ tài khoản nhân sự hệ thống (CSV)", ip: "192.168.1.10", status: "success" },
  { id: "L019", timestamp: "2026-06-11T11:55:12", user: { name: "Trần Thị Hương", role: "Lễ tân", initials: "TH", colorClass: "bg-green-50 text-green-700" }, action: "login", module: "system", description: "Đăng nhập từ IP không quen thuộc: 103.45.67.89 (ngoài mạng nội bộ)", ip: "103.45.67.89", status: "warning" },
  { id: "L020", timestamp: "2026-06-11T09:30:45", user: { name: "Phạm Văn Bình", role: "Kế toán", initials: "PB", colorClass: "bg-amber-50 text-amber-700" }, action: "login", module: "system", description: "Đăng nhập hệ thống thành công từ trình duyệt Firefox", ip: "192.168.1.42", status: "success" },
];

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

function getPageNumbers(current: number, total: number): (number | "...")[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  if (current <= 4) return [1, 2, 3, 4, 5, "...", total];
  if (current >= total - 3) return [1, "...", total - 4, total - 3, total - 2, total - 1, total];
  return [1, "...", current - 1, current, current + 1, "...", total];
}

export default function ActivityLogsPage() {
  useRequireAdmin();

  const [searchQuery, setSearchQuery] = useState("");
  const [actionFilter, setActionFilter] = useState("all");
  const [moduleFilter, setModuleFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [pageSize, setPageSize] = useState(10);
  const [currentPage, setCurrentPage] = useState(1);

  const resetPage = () => setCurrentPage(1);

  const filteredLogs = useMemo(() => {
    return MOCK_LOGS.filter((log) => {
      const q = searchQuery.toLowerCase();
      const matchesSearch =
        q === "" ||
        log.user.name.toLowerCase().includes(q) ||
        log.description.toLowerCase().includes(q) ||
        log.ip.includes(q);
      const matchesAction = actionFilter === "all" || log.action === actionFilter;
      const matchesModule = moduleFilter === "all" || log.module === moduleFilter;
      const matchesStatus = statusFilter === "all" || log.status === statusFilter;
      return matchesSearch && matchesAction && matchesModule && matchesStatus;
    });
  }, [searchQuery, actionFilter, moduleFilter, statusFilter]);

  const totalPages = Math.max(1, Math.ceil(filteredLogs.length / pageSize));
  const safePage = Math.min(currentPage, totalPages);
  const startIndex = (safePage - 1) * pageSize;
  const pagedLogs = filteredLogs.slice(startIndex, startIndex + pageSize);
  const pageNumbers = getPageNumbers(safePage, totalPages);

  const stats = useMemo(() => {
    const today = MOCK_LOGS.filter((l) => l.timestamp.startsWith("2026-06-12"));
    const uniqueUsers = new Set(today.map((l) => l.user.name)).size;
    return {
      todayTotal: today.length,
      todaySuccess: today.filter((l) => l.status === "success").length,
      warnings: MOCK_LOGS.filter((l) => l.status === "warning" || l.status === "failed").length,
      activeUsers: uniqueUsers,
    };
  }, []);

  const formatTimestamp = (ts: string) => {
    const d = new Date(ts);
    const pad = (n: number) => String(n).padStart(2, "0");
    return {
      date: `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`,
      time: `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`,
    };
  };

  const handleExport = () => {
    const headers = "ID,Thoi gian,Nguoi dung,Vai tro,Hanh dong,Phan he,Mo ta,Dia chi IP,Trang thai\n";
    const rows = filteredLogs
      .map((log) => {
        const { date, time } = formatTimestamp(log.timestamp);
        return `"${log.id}","${date} ${time}","${log.user.name}","${log.user.role}","${ACTION_CONFIG[log.action].label}","${MODULE_LABELS[log.module]}","${log.description}","${log.ip}","${STATUS_CONFIG[log.status].label}"`;
      })
      .join("\n");
    const csv = "data:text/csv;charset=utf-8,﻿" + encodeURIComponent(headers + rows);
    const link = document.createElement("a");
    link.setAttribute("href", csv);
    link.setAttribute("download", "LichSuHoatDong.csv");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const selectClass =
    "w-full px-4 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="history" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-16 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex flex-col">
            <h1 className="text-[18px] font-black text-slate-900 leading-tight">Lịch Sử Hoạt Động</h1>
            <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">Theo dõi toàn bộ thao tác và sự kiện hệ thống</p>
          </div>
          <NotificationBell />
        </header>

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
                  placeholder="Tìm tên, mô tả, địa chỉ IP..."
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); resetPage(); }}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
                />
              </div>

              {/* Action type */}
              <div className="relative sm:w-44">
                <select value={actionFilter} onChange={(e) => { setActionFilter(e.target.value); resetPage(); }} className={selectClass}>
                  <option value="all">Tất cả thao tác</option>
                  <option value="login">Đăng nhập</option>
                  <option value="create">Tạo mới</option>
                  <option value="edit">Chỉnh sửa</option>
                  <option value="delete">Xóa</option>
                  <option value="export">Xuất dữ liệu</option>
                  <option value="view">Xem</option>
                  <option value="permission">Phân quyền</option>
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
                  <option value="service">Dịch vụ</option>
                  <option value="post">Bài viết</option>
                  <option value="schedule">Lịch làm việc</option>
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
                  <span className="font-bold text-slate-600">{filteredLogs.length}</span> kết quả
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
                  {pagedLogs.length > 0 ? (
                    pagedLogs.map((log) => {
                      const { date, time } = formatTimestamp(log.timestamp);
                      const actionCfg = ACTION_CONFIG[log.action];
                      const statusCfg = STATUS_CONFIG[log.status];
                      const rowBg =
                        log.status === "failed"
                          ? "bg-red-50/30 hover:bg-red-50/50"
                          : log.status === "warning"
                          ? "bg-amber-50/20 hover:bg-amber-50/40"
                          : "hover:bg-slate-50/40";

                      return (
                        <tr key={log.id} className={`transition-colors ${rowBg}`}>

                          {/* Timestamp */}
                          <td className="px-5 py-3.5">
                            <div className="text-[12.5px] font-bold text-slate-800">{date}</div>
                            <div className="text-[11.5px] font-mono font-semibold text-slate-400 mt-0.5">{time}</div>
                          </td>

                          {/* User */}
                          <td className="px-5 py-3.5">
                            <div className="flex items-center gap-2.5">
                              <div className={`w-8 h-8 rounded-full ${log.user.colorClass} flex items-center justify-center font-black text-[10px] shrink-0 border border-white shadow-sm`}>
                                {log.user.initials}
                              </div>
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 text-[13px] truncate">{log.user.name}</div>
                                <div className="text-[11px] text-slate-400 font-semibold mt-0.5">{log.user.role}</div>
                              </div>
                            </div>
                          </td>

                          {/* Action */}
                          <td className="px-5 py-3.5">
                            <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black whitespace-nowrap ${actionCfg.badgeClass}`}>
                              <svg className="w-3 h-3 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d={actionCfg.iconPath} />
                              </svg>
                              {actionCfg.label}
                            </span>
                          </td>

                          {/* Module */}
                          <td className="px-5 py-3.5">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold whitespace-nowrap ${MODULE_COLORS[log.module]}`}>
                              {MODULE_LABELS[log.module]}
                            </span>
                          </td>

                          {/* Description */}
                          <td className="px-5 py-3.5">
                            <span className="text-[13px] text-slate-700 font-medium leading-relaxed">{log.description}</span>
                          </td>

                          {/* IP */}
                          <td className="px-5 py-3.5">
                            <span className="font-mono text-[12px] text-slate-500 font-semibold">{log.ip}</span>
                          </td>

                          {/* Status */}
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

            {filteredLogs.length > 0 && (
              <div className="px-5 py-3.5 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 bg-slate-50/30">
                {/* Left: range + real-time dot */}
                <div className="flex items-center gap-3">
                  <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">
                    {startIndex + 1}–{Math.min(startIndex + pageSize, filteredLogs.length)} trong{" "}
                    <span className="font-bold text-slate-600">{filteredLogs.length}</span> kết quả
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

                {/* Right: pagination */}
                <div className="flex items-center gap-1">
                  {/* Prev */}
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={safePage === 1}
                    className="w-8 h-8 flex items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-100 hover:text-slate-900 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                  >
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                    </svg>
                  </button>

                  {/* Page numbers */}
                  {pageNumbers.map((p, i) =>
                    p === "..." ? (
                      <span key={`ellipsis-${i}`} className="w-8 h-8 flex items-center justify-center text-[13px] text-slate-400 font-bold select-none">
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

                  {/* Next */}
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
