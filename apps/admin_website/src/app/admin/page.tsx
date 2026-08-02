"use client";

import React, { useEffect, useMemo, useState } from "react";
import AdminSidebar from "../../components/shared/AdminSidebar";
import NotificationBell from "../../components/shared/NotificationBell";
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

// ── Small presentational pieces ──────────────────────────────────────────────

function StatTile({
  label,
  value,
  iconPath,
  accent,
}: {
  label: string;
  value: string | number;
  iconPath: string;
  accent?: "green" | "red";
}) {
  const iconWrap =
    accent === "green"
      ? "bg-green-50 text-green-600"
      : accent === "red"
        ? "bg-red-50 text-red-600"
        : "bg-red-50 text-primary";
  return (
    <div className="bg-slate-50/60 p-4 rounded-xl border border-slate-200/60 shadow-sm flex items-center gap-3.5 hover-lift hover:border-primary/40 hover:bg-white transition-colors duration-200">
      <span className={`w-12 h-12 rounded-xl flex items-center justify-center shrink-0 ${iconWrap}`}>
        <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d={iconPath} />
        </svg>
      </span>
      <div className="min-w-0">
        <div className="text-[26px] font-black text-slate-900 leading-none truncate">{value}</div>
        <div className="text-[12.5px] font-bold text-slate-400 mt-1.5 truncate">{label}</div>
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
      getNotificationsApi({ page: 1, pageSize: 5 }),
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

  const lockedAccountsCount = (accounts ?? []).filter((a) => !a.isActive).length;

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
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Tổng Quan Hệ Thống</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Số liệu vận hành và tình trạng hệ thống.</p>
          </div>
          <div className="flex items-center gap-6">
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
            <NotificationBell />
          </div>
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* Chỉ số hệ thống */}
          <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4">
            <div>
              <h3 className="text-[18px] font-extrabold text-slate-900">Chỉ Số Hệ Thống</h3>
              <p className="text-[13.5px] text-slate-400 mt-0.5 font-semibold">Số liệu tổng quan toàn bộ hệ thống tính đến hiện tại.</p>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <StatTile
                label="Tổng số người dùng"
                value={accounts?.length ?? "—"}
                iconPath="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z"
              />
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
              <StatTile
                label="Tổng số thuốc"
                value={medicinesCount ?? "—"}
                iconPath="M9.75 9.75h.005v.005h-.005v-.005zM9.75 12h.005v.005h-.005V12zm-2.25.005h.005v.01H7.5v-.01zM12 12h.75v.75H12V12zM9 15h.75v.75H9V15zM3.75 6h16.5v14.25H3.75V6z"
              />
              <StatTile
                label="Lịch hẹn hôm nay"
                value={todayAppointmentsCount ?? "—"}
                iconPath="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5"
              />
              <StatTile
                label="Trạng thái hệ thống"
                value={loadError ? "Sự cố" : "Tốt"}
                accent={loadError ? "red" : "green"}
                iconPath="M9.348 14.652a3.75 3.75 0 010-5.304m5.304 0a3.75 3.75 0 010 5.304m-7.425 2.121a6.75 6.75 0 010-9.546m9.546 0a6.75 6.75 0 010 9.546M12 12h.008v.008H12V12z"
              />
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

                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <MiniBarChart title="Số Lượng Lịch Hẹn" desc="Lịch hẹn theo thời gian" items={appointmentItems} />
                  <MiniBarChart title="Người Dùng Mới" desc="Tài khoản tạo mới" items={newUsersItems} />
                  <MiniBarChart title="Hoạt Động Hệ Thống" desc="Số thao tác ghi nhận" items={activityItems} />
                </div>
              </section>

              {/* Bảng nhật ký hoạt động gần đây */}
              <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
                <div className="p-4.5 border-b border-slate-100 flex justify-between items-center">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Nhật Ký Hoạt Động Gần Đây</h3>
                    <p className="text-[13.5px] text-slate-400 mt-0.5 font-semibold">Các thao tác mới nhất được ghi nhận trên hệ thống.</p>
                  </div>
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

              {/* Cảnh báo hệ thống */}
              <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5">
                <h3 className="text-[16px] font-extrabold text-slate-900">Cảnh Báo Hệ Thống</h3>
                {lockedAccountsCount === 0 ? (
                  <div className="flex items-center gap-3 p-3 rounded-xl bg-green-50 border border-green-100">
                    <span className="w-9 h-9 rounded-lg bg-green-100 text-green-600 flex items-center justify-center shrink-0">
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                    </span>
                    <p className="text-[13px] font-bold text-green-700">Không có cảnh báo nào.</p>
                  </div>
                ) : (
                  <div className="flex items-center gap-3 p-3 rounded-xl bg-amber-50 border border-amber-100">
                    <span className="w-9 h-9 rounded-lg bg-amber-100 text-amber-600 flex items-center justify-center shrink-0">
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                      </svg>
                    </span>
                    <p className="text-[13px] font-bold text-amber-700">
                      {lockedAccountsCount} tài khoản đang bị khóa
                    </p>
                  </div>
                )}
              </section>

              {/* Thông báo quản trị */}
              <section className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5">
                <h3 className="text-[16px] font-extrabold text-slate-900">Thông Báo Quản Trị</h3>
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
