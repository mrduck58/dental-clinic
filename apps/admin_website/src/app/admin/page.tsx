"use client";

import React, { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import AdminSidebar from "../../components/shared/AdminSidebar";
import AdminPageHeader from "../../components/shared/AdminPageHeader";
import { useRequireAdmin } from "../../hooks/useRequireAdmin";
import {
  AccountDto,
  ActivityLogItemDto,
  NotificationDto,
  getAccountsApi,
  getStaffApi,
  getRoomsApi,
  getServicesApi,
  getMedicinesApi,
  getDashboardTodayAppointmentsApi,
  getAppointmentTrendApi,
  AppointmentTrendPointDto,
  getActivityLogsApi,
  getNotificationsApi,
} from "../../lib/apiClient";
import { normalizeRole, ROLE_LABELS, type UiRole } from "../../lib/roles";

// ── Formatting helpers ───────────────────────────────────────────────────────

type Range = "week" | "month" | "year";

const VN_WEEKDAYS = ["CN", "T2", "T3", "T4", "T5", "T6", "T7"];

function vnWeekdayLabel(date: Date): string {
  return VN_WEEKDAYS[date.getDay()];
}

function formatDateTime(dateStr: string): string {
  const d = new Date(dateStr);
  return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")} ${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}`;
}

/** Thời gian tương đối ("5 phút trước") — dùng cho tín hiệu "hệ thống còn hoạt động" ở panel Tình Trạng Hệ Thống. */
function formatRelativeTime(dateStr: string): string {
  const diffMs = Date.now() - new Date(dateStr).getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return "Vừa xong";
  if (diffMin < 60) return `${diffMin} phút trước`;
  const diffHour = Math.floor(diffMin / 60);
  if (diffHour < 24) return `${diffHour} giờ trước`;
  return `${Math.floor(diffHour / 24)} ngày trước`;
}

interface Bucket {
  start: Date;
  end: Date;
  label: string;
}

function buildBuckets(range: Range): Bucket[] {
  const now = new Date();
  const buckets: Bucket[] = [];

  if (range === "week") {
    for (let i = 6; i >= 0; i--) {
      const start = new Date(now);
      start.setHours(0, 0, 0, 0);
      start.setDate(start.getDate() - i);
      const end = new Date(start);
      end.setDate(end.getDate() + 1);
      buckets.push({ start, end, label: vnWeekdayLabel(start) });
    }
  } else if (range === "month") {
    for (let i = 3; i >= 0; i--) {
      const end = new Date(now);
      end.setHours(0, 0, 0, 0);
      end.setDate(end.getDate() - i * 7 + 1);
      const start = new Date(end);
      start.setDate(start.getDate() - 7);
      buckets.push({ start, end, label: `Tuần ${4 - i}` });
    }
  } else {
    for (let i = 11; i >= 0; i--) {
      const start = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const end = new Date(now.getFullYear(), now.getMonth() - i + 1, 1);
      buckets.push({ start, end, label: `Th${start.getMonth() + 1}` });
    }
  }

  return buckets;
}

function bucketCounts(dates: string[], buckets: Bucket[]) {
  const times = dates.map((d) => new Date(d).getTime());
  return buckets.map((b) => ({
    label: b.label,
    value: times.filter((t) => t >= b.start.getTime() && t < b.end.getTime()).length,
  }));
}

const STATUS_CONFIG: Record<string, { label: string; badgeClass: string }> = {
  success: { label: "Thành công", badgeClass: "bg-green-50 text-green-700 border border-green-100" },
  failed: { label: "Thất bại", badgeClass: "bg-red-50 text-red-700 border border-red-100" },
  warning: { label: "Cảnh báo", badgeClass: "bg-amber-50 text-amber-700 border border-amber-100" },
};

const PRIORITY_CONFIG: Record<string, { label: string; badgeClass: string }> = {
  high: { label: "Cao", badgeClass: "bg-red-50 text-red-700 border border-red-100" },
  medium: { label: "Vừa", badgeClass: "bg-amber-50 text-amber-700 border border-amber-100" },
  low: { label: "Thấp", badgeClass: "bg-slate-100 text-slate-500 border border-slate-200" },
};

// Cùng bảng màu/nhãn với trang Vai Trò (admin/permissions/roles) để nhất quán trong toàn hệ thống.
const ROLE_CHART_CONFIG: Record<UiRole, { text: string; dot: string }> = {
  Admin: { text: "text-purple-500", dot: "bg-purple-500" },
  Owner: { text: "text-amber-500", dot: "bg-amber-500" },
  Dentist: { text: "text-sky-500", dot: "bg-sky-500" },
  Staff: { text: "text-green-500", dot: "bg-green-500" },
};

// ── Small presentational pieces ──────────────────────────────────────────────

function StatTile({
  label,
  value,
  iconPath,
  size = "md",
}: {
  label: string;
  value: string | number;
  iconPath: string;
  size?: "md" | "lg";
}) {
  const isLg = size === "lg";
  return (
    <div
      className={`bg-slate-50/60 rounded-xl border border-slate-200/60 shadow-sm flex items-center gap-3.5 hover-lift hover:border-primary/40 hover:bg-white transition-colors duration-200 ${
        isLg ? "p-5" : "p-4"
      }`}
    >
      <span
        className={`rounded-xl flex items-center justify-center shrink-0 bg-red-50 text-primary ${
          isLg ? "w-14 h-14" : "w-12 h-12"
        }`}
      >
        <svg className={isLg ? "w-7 h-7" : "w-6 h-6"} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d={iconPath} />
        </svg>
      </span>
      <div className="min-w-0">
        <div className={`font-black text-slate-900 leading-none truncate ${isLg ? "text-[32px]" : "text-[26px]"}`}>{value}</div>
        <div className={`font-bold text-slate-400 mt-1.5 truncate ${isLg ? "text-[13px]" : "text-[12.5px]"}`}>{label}</div>
      </div>
    </div>
  );
}

const GRID_LINES = [0, 25, 50, 75, 100];

function MiniBarChart({ title, desc, items }: { title: string; desc: string; items: { label: string; value: number }[] }) {
  const maxValue = Math.max(1, ...items.map((i) => i.value));
  return (
    <div className="bg-white p-4.5 rounded-xl border border-slate-200/60 flex flex-col min-h-[240px]">
      <h4 className="text-[14px] font-extrabold text-slate-900">{title}</h4>
      <p className="text-[12px] text-slate-400 mt-0.5 font-medium mb-4">{desc}</p>

      <div className="flex-1 relative">
        {/* Gridlines */}
        <div className="absolute inset-0 flex flex-col justify-between pointer-events-none">
          {GRID_LINES.map((g) => (
            <div key={g} className="border-t border-dashed border-slate-100 w-full" />
          ))}
        </div>

        {/* Bars */}
        <div className="relative h-full flex items-end justify-between gap-1.5 px-0.5 pt-5">
          {items.map((item, idx) => (
            <div key={idx} className="flex-1 flex flex-col items-center justify-end gap-1.5 min-w-0 h-full">
              <span className="text-[10.5px] font-black text-slate-500 leading-none">{item.value}</span>
              <div
                className="w-full max-w-[24px] rounded-t-md bg-gradient-to-t from-primary to-red-400 shadow-sm shadow-primary/20 transition-all"
                style={{ height: `${Math.max(3, Math.round((item.value / maxValue) * 100))}%` }}
              />
            </div>
          ))}
        </div>
      </div>

      <div className="flex items-center justify-between gap-1.5 px-0.5 pt-2 mt-1 border-t border-slate-100">
        {items.map((item, idx) => (
          <span key={idx} className="flex-1 text-center text-[10.5px] font-bold text-slate-400 truncate">
            {item.label}
          </span>
        ))}
      </div>
    </div>
  );
}

/** Biểu đồ tròn (donut) phân bố người dùng theo vai trò — đa dạng hoá loại biểu đồ so với 3 bar chart còn lại,
 * vẽ thuần SVG (stroke-dasharray) để khớp quy ước "không dùng thư viện chart" của dự án. */
function RoleDonutChart({ data }: { data: { role: UiRole; value: number }[] }) {
  const total = data.reduce((s, d) => s + d.value, 0);
  const radius = 40;
  const circumference = 2 * Math.PI * radius;
  let offsetAcc = 0;

  return (
    <div className="bg-white p-4.5 rounded-xl border border-slate-200/60 flex flex-col min-h-[240px]">
      <h4 className="text-[14px] font-extrabold text-slate-900">Phân Bố Người Dùng</h4>
      <p className="text-[12px] text-slate-400 mt-0.5 font-medium mb-4">Theo vai trò trong hệ thống</p>

      {total === 0 ? (
        <div className="flex-1 flex items-center justify-center text-[12.5px] text-slate-400 font-semibold">Chưa có dữ liệu</div>
      ) : (
        <div className="flex-1 flex items-center gap-4">
          <svg viewBox="0 0 100 100" className="w-24 h-24 -rotate-90 shrink-0">
            <circle cx="50" cy="50" r={radius} fill="none" stroke="#f1f5f9" strokeWidth="14" />
            {data
              .filter((d) => d.value > 0)
              .map((d) => {
                const dash = (d.value / total) * circumference;
                const el = (
                  <circle
                    key={d.role}
                    cx="50"
                    cy="50"
                    r={radius}
                    fill="none"
                    strokeWidth="14"
                    strokeDasharray={`${dash} ${circumference - dash}`}
                    strokeDashoffset={-offsetAcc}
                    className={ROLE_CHART_CONFIG[d.role].text}
                    stroke="currentColor"
                  />
                );
                offsetAcc += dash;
                return el;
              })}
          </svg>
          <div className="flex flex-col gap-2 min-w-0 flex-1">
            {data.map((d) => (
              <div key={d.role} className="flex items-center justify-between gap-2 text-[12px]">
                <span className="flex items-center gap-1.5 font-bold text-slate-600 truncate">
                  <span className={`w-2 h-2 rounded-full shrink-0 ${ROLE_CHART_CONFIG[d.role].dot}`} />
                  {ROLE_LABELS[d.role]}
                </span>
                <span className="font-black text-slate-900">{d.value}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function QuickAction({ href, label, iconPath }: { href: string; label: string; iconPath: string }) {
  return (
    <Link
      href={href}
      className="flex items-center gap-3 p-3.5 rounded-xl border border-slate-200/60 bg-slate-50/60 hover:bg-white hover:border-primary/40 hover:shadow-sm transition-all duration-200 group"
    >
      <span className="w-10 h-10 rounded-lg bg-white border border-slate-200/60 text-primary flex items-center justify-center shrink-0 group-hover:bg-red-50 group-hover:border-primary/20 transition-colors">
        <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d={iconPath} />
        </svg>
      </span>
      <span className="text-[13px] font-bold text-slate-700 group-hover:text-slate-900 truncate">{label}</span>
    </Link>
  );
}

const QUICK_ACTIONS = [
  {
    href: "/admin/permissions/create-account",
    label: "Tạo tài khoản mới",
    iconPath: "M18 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM3 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 019.374 21c-2.331 0-4.512-.645-6.374-1.766z",
  },
  {
    href: "/admin/permissions",
    label: "Quản lý người dùng",
    iconPath: "M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z",
  },
  {
    href: "/admin/activity-logs",
    label: "Xem nhật ký hệ thống",
    iconPath: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z",
  },
  {
    href: "/admin/ai-analytics",
    label: "Phân tích AI",
    iconPath: "M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z",
  },
  {
    href: "/admin/services/add",
    label: "Thêm dịch vụ",
    iconPath: "M12 9v6m3-3H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z",
  },
  {
    href: "/admin/rooms/create",
    label: "Thêm phòng khám",
    iconPath: "M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3a1.5 1.5 0 011.5-1.5h3a1.5 1.5 0 011.5 1.5v3m-9-10.5h.75m-.75 3h.75m-.75 3h.75m9-6h.75m-.75 3h.75m-.75 3h.75",
  },
];

export default function Dashboard() {
  const authorized = useRequireAdmin();

  const [range, setRange] = useState<Range>("week");
  const [loadError, setLoadError] = useState(false);

  const [accounts, setAccounts] = useState<AccountDto[] | null>(null);
  const [staffStats, setStaffStats] = useState<{ totalDentists: number; totalEmployees: number } | null>(null);
  const [roomsCount, setRoomsCount] = useState<number | null>(null);
  const [servicesCount, setServicesCount] = useState<number | null>(null);
  const [medicinesCount, setMedicinesCount] = useState<number | null>(null);
  const [todayAppointmentsCount, setTodayAppointmentsCount] = useState<number | null>(null);
  const [appointmentTrend, setAppointmentTrend] = useState<AppointmentTrendPointDto[]>([]);
  const [activityLogs, setActivityLogs] = useState<ActivityLogItemDto[]>([]);
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);

  useEffect(() => {
    let cancelled = false;
    Promise.all([
      getAccountsApi(),
      getStaffApi({ pageSize: 1 }),
      getRoomsApi(),
      getServicesApi(),
      getMedicinesApi(),
      getDashboardTodayAppointmentsApi(1, 1),
      getActivityLogsApi({ page: 1, pageSize: 100 }),
      getNotificationsApi({ page: 1, pageSize: 3 }),
    ])
      .then(([acc, staff, rooms, services, medicines, todayAppts, logs, notifs]) => {
        if (cancelled) return;
        setAccounts(acc);
        setStaffStats({ totalDentists: staff.statistics.totalDentists, totalEmployees: staff.statistics.totalEmployees });
        setRoomsCount(rooms.length);
        setServicesCount(services.length);
        setMedicinesCount(medicines.length);
        setTodayAppointmentsCount(todayAppts.totalCount);
        setActivityLogs(logs.items);
        setNotifications(notifs.items);
        setLoadError(false);
      })
      .catch((err) => {
        console.error("Không thể tải dữ liệu tổng quan hệ thống:", err);
        if (!cancelled) setLoadError(true);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  // Biểu đồ lịch hẹn dùng lại API trend sẵn có, đổi theo Tuần/Tháng/Năm
  useEffect(() => {
    let cancelled = false;
    getAppointmentTrendApi(range)
      .then((res) => !cancelled && setAppointmentTrend(res.points))
      .catch((err) => console.error("Không thể tải biểu đồ lịch hẹn:", err));
    return () => {
      cancelled = true;
    };
  }, [range]);

  const buckets = useMemo(() => buildBuckets(range), [range]);

  const appointmentItems = useMemo(
    () => appointmentTrend.map((p, idx) => ({ label: buckets[idx]?.label ?? "", value: p.count })),
    [appointmentTrend, buckets]
  );
  const newUsersItems = useMemo(
    () => bucketCounts((accounts ?? []).map((a) => a.createdAt), buckets),
    [accounts, buckets]
  );
  const activityItems = useMemo(
    () => bucketCounts(activityLogs.map((l) => l.createdAt), buckets),
    [activityLogs, buckets]
  );
  const roleDistribution = useMemo(() => {
    const counts: Record<UiRole, number> = { Admin: 0, Owner: 0, Dentist: 0, Staff: 0 };
    for (const a of accounts ?? []) counts[normalizeRole(a.role)]++;
    return (Object.keys(counts) as UiRole[]).map((role) => ({ role, value: counts[role] }));
  }, [accounts]);

  const lockedAccountsCount = (accounts ?? []).filter((a) => !a.isActive).length;
  const mostRecentLogAt = activityLogs[0]?.createdAt;

  if (!authorized) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="overview" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <AdminPageHeader
          title="Tổng Quan Hệ Thống"
          subtitle="Số liệu vận hành và tình trạng hệ thống."
          right={
            <div className="relative w-64 hidden sm:block">
              <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Tìm kiếm nhanh..."
                className="w-full pl-9 pr-4 py-2 text-[15px] bg-slate-100 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all"
              />
            </div>
          }
        />

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* Cảnh báo hệ thống — nổi bật ở đầu trang, chỉ hiện khi có việc cần chú ý */}
          {lockedAccountsCount > 0 && (
            <div className="flex items-center justify-between gap-4 p-4 rounded-2xl bg-amber-50 border-2 border-amber-200 shadow-sm shadow-amber-100/50">
              <div className="flex items-center gap-3.5 min-w-0">
                <span className="w-11 h-11 rounded-xl bg-amber-100 text-amber-600 flex items-center justify-center shrink-0">
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                  </svg>
                </span>
                <div className="min-w-0">
                  <p className="text-[14px] font-black text-amber-800">Cần chú ý: {lockedAccountsCount} tài khoản đang bị khóa</p>
                  <p className="text-[12.5px] font-semibold text-amber-600">Kiểm tra lại danh sách người dùng để mở khóa nếu cần thiết.</p>
                </div>
              </div>
              <Link
                href="/admin/permissions"
                className="shrink-0 px-4 py-2 rounded-xl bg-amber-600 text-white text-[13px] font-bold hover:bg-amber-700 transition-colors shadow-sm"
              >
                Xem chi tiết
              </Link>
            </div>
          )}

          {/* Chỉ số hệ thống */}
          <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4">
            <div>
              <h3 className="text-[18px] font-extrabold text-slate-900">Chỉ Số Hệ Thống</h3>
              <p className="text-[13.5px] text-slate-400 mt-0.5 font-semibold">Số liệu tổng quan toàn bộ hệ thống tính đến hiện tại.</p>
            </div>

            {/* 2 chỉ số quan trọng nhất được nhấn mạnh hơn (kích thước lớn hơn) */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <StatTile
                size="lg"
                label="Tổng số người dùng"
                value={accounts?.length ?? "—"}
                iconPath="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z"
              />
              <StatTile
                size="lg"
                label="Lịch hẹn hôm nay"
                value={todayAppointmentsCount ?? "—"}
                iconPath="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5"
              />
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <StatTile
                label="Tổng số bác sĩ"
                value={staffStats?.totalDentists ?? "—"}
                iconPath="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5"
              />
              <StatTile
                label="Tổng số nhân viên"
                value={staffStats?.totalEmployees ?? "—"}
                iconPath="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198.007.031c.003.01.005.02.008.029A9.091 9.091 0 0021 18.75m-2.785-5.365A3 3 0 1016.5 9.75M16.5 13.5A3 3 0 0016.5 9.75M9 13.5a3.75 3.75 0 110-7.5 3.75 3.75 0 010 7.5zM2.25 18.75a6.75 6.75 0 0113.5 0M9 13.5c-.394 0-.776-.03-1.147-.09a6.75 6.75 0 00-5.603 5.34"
              />
              <StatTile
                label="Tổng số phòng"
                value={roomsCount ?? "—"}
                iconPath="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3a1.5 1.5 0 011.5-1.5h3a1.5 1.5 0 011.5 1.5v3m-9-10.5h.75m-.75 3h.75m-.75 3h.75m9-6h.75m-.75 3h.75m-.75 3h.75"
              />
              <StatTile
                label="Tổng số dịch vụ"
                value={servicesCount ?? "—"}
                iconPath="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.83-5.83m0 0a2.95 2.95 0 11-4.174-4.172 2.95 2.95 0 014.174 4.172zm-7.42 7.42l9.39-9.39"
              />
            </div>
          </section>

          {/* Quick Actions */}
          <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4">
            <div>
              <h3 className="text-[18px] font-extrabold text-slate-900">Thao Tác Nhanh</h3>
              <p className="text-[13.5px] text-slate-400 mt-0.5 font-semibold">Truy cập nhanh các tác vụ quản trị thường dùng.</p>
            </div>
            <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-3">
              {QUICK_ACTIONS.map((action) => (
                <QuickAction key={action.href} {...action} />
              ))}
            </div>
          </section>

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">

            {/* ── LEFT COLUMN ── */}
            <div className="lg:col-span-8 flex flex-col gap-6 min-w-0">

              {/* Biểu đồ hoạt động */}
              <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Biểu Đồ Hoạt Động</h3>
                    <p className="text-[13.5px] text-slate-400 mt-0.5 font-semibold">Lịch hẹn, người dùng mới và hoạt động ghi nhận.</p>
                  </div>
                  <div className="flex bg-slate-100 p-1 rounded-xl self-start shrink-0">
                    {(["week", "month", "year"] as Range[]).map((r) => (
                      <button
                        key={r}
                        onClick={() => setRange(r)}
                        className={`px-4 py-1.5 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${range === r ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
                      >
                        {r === "week" ? "Theo Tuần" : r === "month" ? "Theo Tháng" : "Theo Năm"}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
                  <MiniBarChart title="Số Lượng Lịch Hẹn" desc="Lịch hẹn theo thời gian" items={appointmentItems} />
                  <MiniBarChart title="Người Dùng Mới" desc="Tài khoản tạo mới" items={newUsersItems} />
                  <MiniBarChart title="Hoạt Động Hệ Thống" desc="Số thao tác ghi nhận" items={activityItems} />
                  <RoleDonutChart data={roleDistribution} />
                </div>
              </section>

              {/* Bảng nhật ký hoạt động gần đây */}
              <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
                <div className="p-4.5 border-b border-slate-100 flex justify-between items-center">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Nhật Ký Hoạt Động Gần Đây</h3>
                    <p className="text-[13.5px] text-slate-400 mt-0.5 font-semibold">Các thao tác mới nhất được ghi nhận trên hệ thống.</p>
                  </div>
                  <Link
                    href="/admin/activity-logs"
                    className="shrink-0 text-[12.5px] font-bold text-primary hover:text-red-700 transition-colors"
                  >
                    Xem tất cả →
                  </Link>
                </div>
                <div className="overflow-x-auto flex-1">
                  <table className="w-full text-left border-collapse text-[13px]">
                    <thead>
                      <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-100">
                        <th className="px-5 py-3">Thời gian</th>
                        <th className="px-5 py-3">Người dùng</th>
                        <th className="px-5 py-3">Hành động</th>
                        <th className="px-5 py-3">Module</th>
                        <th className="px-5 py-3">Trạng thái</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                      {activityLogs.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="px-5 py-6 text-center text-slate-400 font-semibold">
                            Chưa có hoạt động nào được ghi nhận.
                          </td>
                        </tr>
                      ) : (
                        activityLogs.slice(0, 8).map((log) => {
                          const badge = STATUS_CONFIG[log.status] ?? { label: log.status, badgeClass: "bg-slate-100 text-slate-500" };
                          return (
                            <tr key={log.id} className="hover:bg-slate-50/30 transition-colors">
                              <td className="px-5 py-3 font-bold text-slate-950 whitespace-nowrap">{formatDateTime(log.createdAt)}</td>
                              <td className="px-5 py-3 font-bold text-slate-800">{log.userName}</td>
                              <td className="px-5 py-3 text-slate-500 font-medium">{log.description}</td>
                              <td className="px-5 py-3 text-slate-500 font-medium">{log.module}</td>
                              <td className="px-5 py-3">
                                <span className={`inline-flex px-2 py-0.5 rounded-full text-[12px] font-bold ${badge.badgeClass}`}>{badge.label}</span>
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            {/* ── RIGHT COLUMN ── */}
            <div className="lg:col-span-4 flex flex-col min-w-0 self-stretch gap-6">

              {/* Tình trạng hệ thống — thay cho ô "Trạng thái hệ thống" đơn lẻ trước đây, gộp nhiều tín hiệu
                  thực tế đang có (không bịa số liệu health-check chưa tồn tại ở backend). */}
              <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5">
                <h3 className="text-[16px] font-extrabold text-slate-900">Tình Trạng Hệ Thống</h3>

                <div className="flex items-center gap-3 p-3 rounded-xl bg-green-50 border border-green-100">
                  <span className="relative flex w-2.5 h-2.5 shrink-0">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75" />
                    <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-green-500" />
                  </span>
                  <p className="text-[13px] font-bold text-green-700">Hệ thống đang hoạt động bình thường</p>
                </div>

                <div className="flex items-center justify-between px-1">
                  <span className="text-[12.5px] font-semibold text-slate-500">Hoạt động gần nhất</span>
                  <span className="text-[12.5px] font-bold text-slate-800">
                    {mostRecentLogAt ? formatRelativeTime(mostRecentLogAt) : "—"}
                  </span>
                </div>
                <div className="flex items-center justify-between px-1">
                  <span className="text-[12.5px] font-semibold text-slate-500">Tài khoản đang bị khóa</span>
                  <span className={`text-[12.5px] font-bold ${lockedAccountsCount > 0 ? "text-amber-600" : "text-slate-800"}`}>
                    {lockedAccountsCount}
                  </span>
                </div>
              </section>

              {/* Thông báo quản trị — 3 thông báo gần nhất */}
              <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5">
                <div className="flex items-center justify-between">
                  <h3 className="text-[16px] font-extrabold text-slate-900">Thông Báo Quản Trị</h3>
                  <Link
                    href="/admin/notifications"
                    className="text-[12px] font-bold text-primary hover:text-red-700 transition-colors"
                  >
                    Xem tất cả →
                  </Link>
                </div>
                <div className="flex flex-col gap-3">
                  {notifications.length === 0 ? (
                    <p className="text-[13px] text-slate-400 font-semibold">Chưa có thông báo nào.</p>
                  ) : (
                    notifications.map((n) => {
                      const prio = PRIORITY_CONFIG[n.priority?.toLowerCase()] ?? PRIORITY_CONFIG.low;
                      return (
                        <div key={n.id} className="flex flex-col gap-1.5 p-3 rounded-xl bg-slate-50 border border-slate-100 hover:border-slate-200 hover:bg-slate-100/30 transition-all duration-200">
                          <div className="flex items-center justify-between gap-2">
                            <span className="text-[13px] font-bold text-slate-900 truncate">{n.title}</span>
                            <span className={`shrink-0 inline-flex px-1.5 py-0.5 rounded-full text-[10.5px] font-bold ${prio.badgeClass}`}>{prio.label}</span>
                          </div>
                          <p className="text-[12px] text-slate-500 leading-relaxed line-clamp-2">{n.body}</p>
                          <span className="text-[11px] text-slate-400 font-medium">{formatDateTime(n.createdAt)}</span>
                        </div>
                      );
                    })
                  )}
                </div>
              </section>

            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
