"use client";

import React, { useEffect, useMemo, useState } from "react";
import AdminSidebar from "../components/shared/AdminSidebar";
import NotificationBell from "../components/shared/NotificationBell";
import { useRequireAdmin } from "../hooks/useRequireAdmin";
import {
  DashboardRange,
  DashboardStatsDto,
  AppointmentTrendDto,
  AppointmentTrendPointDto,
  ServiceDistributionDto,
  DashboardTodayAppointmentsDto,
  DashboardWeeklyScheduleDto,
  DashboardRecentFeedbackDto,
  getDashboardStatsApi,
  getAppointmentTrendApi,
  getServiceDistributionApi,
  getDashboardTodayAppointmentsApi,
  getDashboardWeeklyScheduleApi,
  getDashboardRecentFeedbackApi,
} from "../lib/apiClient";

// ── Formatting helpers ───────────────────────────────────────────────────────

function formatCompactVnd(amount: number): string {
  if (amount >= 1_000_000_000) return `${(amount / 1_000_000_000).toFixed(2).replace(/\.?0+$/, "")}B`;
  if (amount >= 1_000_000) return `${(amount / 1_000_000).toFixed(1).replace(/\.0$/, "")}M`;
  if (amount >= 1_000) return `${(amount / 1_000).toFixed(1).replace(/\.0$/, "")}K`;
  return amount.toLocaleString("vi-VN");
}

function trendBadge(percent: number | undefined) {
  const p = percent ?? 0;
  const isUp = p >= 0;
  return {
    text: `${isUp ? "↑" : "↓"} ${Math.abs(p).toFixed(1)}%`,
    color: isUp ? "text-green-600" : "text-primary",
    bg: isUp ? "bg-green-50" : "bg-red-50",
  };
}

const VN_WEEKDAYS = ["CN", "T2", "T3", "T4", "T5", "T6", "T7"];

function vnWeekdayLabel(dateStr: string): string {
  return VN_WEEKDAYS[new Date(dateStr).getDay()];
}

function dayOfMonth(dateStr: string): string {
  return String(new Date(dateStr).getDate()).padStart(2, "0");
}

function formatTime(dateStr: string): string {
  const d = new Date(dateStr);
  return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
}

function isPeriodCurrent(point: AppointmentTrendPointDto): boolean {
  const now = Date.now();
  return now >= new Date(point.periodStart).getTime() && now < new Date(point.periodEnd).getTime();
}

function barLabel(point: AppointmentTrendPointDto, range: DashboardRange, index: number): string {
  const start = new Date(point.periodStart);
  if (range === "week") return vnWeekdayLabel(point.periodStart);
  if (range === "month") return `Tuần ${index + 1}`;
  return `Th${start.getMonth() + 1}`;
}

const STATUS_BADGES: Record<string, { label: string; className: string }> = {
  Pending: { label: "Đang chờ", className: "bg-amber-50 text-amber-600" },
  Confirmed: { label: "Đang chờ", className: "bg-amber-50 text-amber-600" },
  CheckedIn: { label: "Đang chờ", className: "bg-amber-50 text-amber-600" },
  InProgress: { label: "Đang khám", className: "bg-red-50 text-primary" },
  PendingPayment: { label: "Chờ thanh toán", className: "bg-amber-50 text-amber-600" },
  Completed: { label: "Đã hoàn thành", className: "bg-green-50 text-green-600" },
  Cancelled: { label: "Đã hủy", className: "bg-slate-100 text-slate-500" },
};

const DONUT_COLORS = ["rgb(220 38 38)", "rgb(2 132 199)", "rgb(245 158 11)", "rgb(34 197 94)", "#8b5cf6", "#06b6d4"];
const OTHER_COLOR = "#94a3b8";

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return (parts[parts.length - 1]?.[0] ?? "?").toUpperCase();
}

function Avatar({ url, name, className }: { url: string | null; name: string; className: string }) {
  if (url) {
    return <img src={url} alt={name} className={`${className} object-cover`} />;
  }
  return (
    <div className={`${className} flex items-center justify-center bg-slate-200 text-slate-600 font-bold shrink-0`}>
      {initials(name)}
    </div>
  );
}

const STATS_LABELS: Record<DashboardRange, { appointments: { name: string; desc: string }; revenue: { name: string; desc: string }; patientsDesc: string }> = {
  week: {
    appointments: { name: "Lịch hẹn hôm nay", desc: "Lịch khám đã đặt" },
    revenue: { name: "Doanh thu ngày", desc: "VNĐ thu trong hôm nay" },
    patientsDesc: "Đăng ký mới tuần này",
  },
  month: {
    appointments: { name: "Tổng lịch hẹn tháng", desc: "Tổng số ca khám tháng này" },
    revenue: { name: "Doanh thu tháng", desc: "Lũy kế tháng này" },
    patientsDesc: "Đăng ký mới tháng này",
  },
  year: {
    appointments: { name: "Tổng lịch hẹn năm", desc: "Tổng số ca khám năm nay" },
    revenue: { name: "Doanh thu năm", desc: "Lũy kế năm nay" },
    patientsDesc: "Đăng ký mới năm nay",
  },
};

export default function Dashboard() {
  const authorized = useRequireAdmin();

  const [hoveredIndex, setHoveredIndex] = useState<number | null>(null);
  const [range, setRange] = useState<DashboardRange>("week");

  const [stats, setStats] = useState<DashboardStatsDto | null>(null);
  const [trend, setTrend] = useState<AppointmentTrendDto | null>(null);
  const [distribution, setDistribution] = useState<ServiceDistributionDto | null>(null);
  const [todayAppointments, setTodayAppointments] = useState<DashboardTodayAppointmentsDto | null>(null);
  const [weeklySchedule, setWeeklySchedule] = useState<DashboardWeeklyScheduleDto | null>(null);
  const [recentFeedback, setRecentFeedback] = useState<DashboardRecentFeedbackDto | null>(null);

  // Dữ liệu theo range (Tuần/Tháng/Năm)
  useEffect(() => {
    let cancelled = false;
    Promise.all([getDashboardStatsApi(range), getAppointmentTrendApi(range), getServiceDistributionApi(range, 5)])
      .then(([s, t, d]) => {
        if (cancelled) return;
        setStats(s);
        setTrend(t);
        setDistribution(d);
      })
      .catch((err) => console.error("Không thể tải dữ liệu tổng quan:", err));
    return () => {
      cancelled = true;
    };
  }, [range]);

  // Dữ liệu không phụ thuộc range: lịch hẹn hôm nay, lịch vận hành, đánh giá
  useEffect(() => {
    let cancelled = false;
    getDashboardTodayAppointmentsApi(1, 10)
      .then((res) => !cancelled && setTodayAppointments(res))
      .catch((err) => console.error("Không thể tải lịch hẹn hôm nay:", err));
    getDashboardWeeklyScheduleApi()
      .then((res) => !cancelled && setWeeklySchedule(res))
      .catch((err) => console.error("Không thể tải lịch vận hành:", err));
    getDashboardRecentFeedbackApi(3)
      .then((res) => !cancelled && setRecentFeedback(res))
      .catch((err) => console.error("Không thể tải đánh giá khách hàng:", err));
    return () => {
      cancelled = true;
    };
  }, []);

  const labels = STATS_LABELS[range];

  const patientsTrend = trendBadge(stats?.newPatientsTrendPercent);
  const appointmentsTrend = trendBadge(stats?.appointmentsTrendPercent);
  const revenueTrend = trendBadge(stats?.revenueTrendPercent);

  // Nhãn "Lịch hẹn hôm nay" (tab Tuần) phải phản ánh đúng số lịch hẹn HÔM NAY,
  // không phải tổng cả tuần — nên lấy từ todayAppointments thay vì stats.appointmentsCount.
  const appointmentsCountDisplay = range === "week" ? todayAppointments?.totalCount : stats?.appointmentsCount;

  const barItems = useMemo(() => {
    const points = trend?.points ?? [];
    const maxCount = Math.max(1, ...points.map((p) => p.count));
    return points.map((p, idx) => ({
      label: barLabel(p, range, idx),
      value: p.count,
      height: `${Math.max(4, Math.round((p.count / maxCount) * 160))}px`,
      active: isPeriodCurrent(p),
    }));
  }, [trend, range]);

  const barWidthPercent = range === "year" ? "6%" : range === "month" ? "18%" : "10%";

  const segments = useMemo(() => {
    const items = distribution?.items ?? [];
    return items.map((item, idx) => {
      const stroke = item.serviceId === null ? OTHER_COLOR : DONUT_COLORS[idx % DONUT_COLORS.length];
      const offset = -items.slice(0, idx).reduce((sum, prev) => sum + prev.percentage, 0);
      return { name: item.serviceName ?? "Dịch vụ khác", value: item.percentage, stroke, offset };
    });
  }, [distribution]);

  // Chưa xác nhận phiên đăng nhập hợp lệ — không render dashboard (tránh chớp nội dung
  // được bảo vệ trước khi useRequireAdmin kịp điều hướng sang /auth/login).
  if (!authorized) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">

      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <AdminSidebar activeMenu="overview" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">

        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">
              Tổng Quan Vận Hành
            </h1>
          </div>

          {/* Search, Notifications */}
          <div className="flex items-center gap-6">
            {/* Search Input */}
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
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">

            {/* ── LEFT COLUMN (Stats, Bar Chart, Donut Chart & Table) ───────────── */}
            <div className="lg:col-span-8 flex flex-col gap-8 min-w-0">

              {/* ── STATS HEADER & GRID ────────────────────────────────────────── */}
              <section className="bg-white py-3.5 px-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5 shrink-0">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Báo cáo chỉ số chính</h3>
                    <p className="text-[14px] text-slate-400 mt-0.5 font-semibold">Thống kê bệnh nhân, lịch hẹn & doanh thu.</p>
                  </div>

                  {/* Range select buttons */}
                  <div className="flex bg-slate-100 p-1 rounded-xl self-start shrink-0">
                    <button
                      onClick={() => setRange("week")}
                      className={`px-4 py-1.5 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${range === "week" ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
                    >
                      Theo Tuần
                    </button>
                    <button
                      onClick={() => setRange("month")}
                      className={`px-4 py-1.5 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${range === "month" ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
                    >
                      Theo Tháng
                    </button>
                    <button
                      onClick={() => setRange("year")}
                      className={`px-4 py-1.5 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${range === "year" ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
                    >
                      Theo Năm
                    </button>
                  </div>
                </div>

                {/* 3-column Stats Grid */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="bg-slate-50/50 py-3.5 px-4 rounded-xl border border-slate-200/60 shadow-sm hover-lift flex flex-col justify-between hover:border-primary/40 transition-all duration-200">
                    <div className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider mb-1">Bệnh nhân mới</div>
                    <div className="flex items-center justify-between gap-3 my-1">
                      <div className="flex items-baseline gap-1.5">
                        <span className="text-3xl font-black text-slate-900 leading-none">{stats?.newPatientsCount ?? "—"}</span>
                        <span className={`inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded-full text-[11px] font-bold leading-none ${patientsTrend.bg} ${patientsTrend.color}`}>
                          {patientsTrend.text}
                        </span>
                      </div>
                      <span className="w-11 h-11 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                        <svg className="w-6.5 h-6.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198.007.031c.003.01.005.02.008.029A9.091 9.091 0 0021 18.75m-2.785-5.365A3 3 0 1016.5 9.75M16.5 13.5A3 3 0 0016.5 9.75M9 13.5a3.75 3.75 0 110-7.5 3.75 3.75 0 010 7.5zM2.25 18.75a6.75 6.75 0 0113.5 0M9 13.5c-.394 0-.776-.03-1.147-.09a6.75 6.75 0 00-5.603 5.34" />
                        </svg>
                      </span>
                    </div>
                    <p className="text-[12px] text-slate-400 mt-1 font-semibold">{labels.patientsDesc}</p>
                  </div>

                  <div className="bg-slate-50/50 py-3.5 px-4 rounded-xl border border-slate-200/60 shadow-sm hover-lift flex flex-col justify-between hover:border-primary/40 transition-all duration-200">
                    <div className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider mb-1">{labels.appointments.name}</div>
                    <div className="flex items-center justify-between gap-3 my-1">
                      <div className="flex items-baseline gap-1.5">
                        <span className="text-3xl font-black text-slate-900 leading-none">{appointmentsCountDisplay ?? "—"}</span>
                        <span className={`inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded-full text-[11px] font-bold leading-none ${appointmentsTrend.bg} ${appointmentsTrend.color}`}>
                          {appointmentsTrend.text}
                        </span>
                      </div>
                      <span className="w-11 h-11 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                        <svg className="w-6.5 h-6.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                        </svg>
                      </span>
                    </div>
                    <p className="text-[12px] text-slate-400 mt-1 font-semibold">{labels.appointments.desc}</p>
                  </div>

                  <div className="bg-slate-50/50 py-3.5 px-4 rounded-xl border border-slate-200/60 shadow-sm hover-lift flex flex-col justify-between hover:border-primary/40 transition-all duration-200">
                    <div className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider mb-1">{labels.revenue.name}</div>
                    <div className="flex items-center justify-between gap-3 my-1">
                      <div className="flex items-baseline gap-1.5">
                        <span className="text-3xl font-black text-slate-900 leading-none">{stats ? formatCompactVnd(stats.revenueAmount) : "—"}</span>
                        <span className={`inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded-full text-[11px] font-bold leading-none ${revenueTrend.bg} ${revenueTrend.color}`}>
                          {revenueTrend.text}
                        </span>
                      </div>
                      <span className="w-11 h-11 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                        <svg className="w-6.5 h-6.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5z" />
                        </svg>
                      </span>
                    </div>
                    <p className="text-[12px] text-slate-400 mt-1 font-semibold">{labels.revenue.desc}</p>
                  </div>
                </div>
              </section>

              {/* Row 1: Charts song song */}
              <div className="grid grid-cols-1 md:grid-cols-12 gap-6">

                {/* Lịch Hẹn - Bar Chart (Chiếm 7/12) */}
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:col-span-7 justify-between min-h-[320px] hover:border-primary/40 transition-all duration-200">
                  <div className="flex items-center justify-between mb-4">
                    <div>
                      <h3 className="text-[18px] font-extrabold text-slate-900">Thống Kê Lịch Hẹn</h3>
                      <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Số lượng cuộc hẹn khám tại hệ thống</p>
                    </div>
                  </div>

                  <div className="flex-1 flex flex-col justify-end pt-2">
                    <div className="flex items-end justify-between h-[160px] border-b border-slate-100 pb-1 px-2">
                      {barItems.map((item, idx) => (
                        <div
                          key={idx}
                          className="flex flex-col items-center gap-2 group cursor-pointer"
                          style={{ width: barWidthPercent }}
                        >
                          <div className="relative w-full flex justify-center">
                            <span className="absolute bottom-full mb-1 opacity-0 group-hover:opacity-100 bg-slate-900 text-white text-[11px] font-semibold py-0.5 px-1.5 rounded transition-opacity duration-200 pointer-events-none z-10 whitespace-nowrap">
                              {item.value}
                            </span>
                            <div
                              className={`w-full max-w-[28px] rounded-t transition-all ${item.active ? "bg-primary" : "bg-slate-200 group-hover:bg-primary/80"}`}
                              style={{ height: item.height }}
                            ></div>
                          </div>
                          <span className={`text-[11px] font-bold transition-all truncate max-w-full ${item.active ? "text-slate-950 font-black" : "text-slate-400 group-hover:text-slate-900"}`}>
                            {item.label}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Tỷ Lệ Sử Dụng Dịch Vụ - Donut Chart (Chiếm 5/12) */}
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:col-span-5 justify-between min-h-[320px]">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Tỷ Lệ Dịch Vụ</h3>
                    <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Tỷ lệ dịch vụ nha khoa</p>
                  </div>

                  <div className="flex-1 flex flex-col items-center justify-center gap-4 py-1 mt-2">
                    {/* Donut Chart SVG */}
                    <div className="relative w-48 h-48 shrink-0">
                      <svg className="w-full h-full transform -rotate-90 overflow-visible" viewBox="0 0 40 40">
                        <circle cx="20" cy="20" r="13" fill="none" stroke="#f1f5f9" strokeWidth="6" />
                        {segments.map((segment, idx) => (
                          <circle
                            key={idx}
                            cx="20"
                            cy="20"
                            r="13"
                            fill="none"
                            stroke={segment.stroke}
                            strokeWidth="6"
                            strokeDasharray={`${segment.value} ${100 - segment.value}`}
                            strokeDashoffset={segment.offset}
                            className="transition-all duration-300 cursor-pointer origin-center hover:scale-[1.05]"
                            style={{ transformOrigin: "center" }}
                            onMouseEnter={() => setHoveredIndex(idx)}
                            onMouseLeave={() => setHoveredIndex(null)}
                          />
                        ))}
                        {hoveredIndex !== null && segments[hoveredIndex] && (
                          <circle
                            cx="20"
                            cy="20"
                            r="13"
                            fill="none"
                            stroke={segments[hoveredIndex].stroke}
                            strokeWidth="7.5"
                            strokeDasharray={`${segments[hoveredIndex].value} ${100 - segments[hoveredIndex].value}`}
                            strokeDashoffset={segments[hoveredIndex].offset}
                            className="origin-center pointer-events-none transition-all duration-200 scale-[1.05]"
                            style={{ transformOrigin: "center" }}
                          />
                        )}
                      </svg>

                      <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-2 pointer-events-none">
                        {hoveredIndex !== null && segments[hoveredIndex] ? (
                          <>
                            <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide truncate max-w-[70px]">
                              {segments[hoveredIndex].name}
                            </span>
                            <span className="text-lg font-black mt-0.5 leading-none" style={{ color: segments[hoveredIndex].stroke }}>
                              {segments[hoveredIndex].value}%
                            </span>
                          </>
                        ) : (
                          <>
                            <span className="text-[12px] font-bold text-slate-400 uppercase tracking-widest leading-none">Cơ cấu</span>
                            <span className="text-[12px] font-bold text-slate-500 mt-0.5">Dịch vụ</span>
                          </>
                        )}
                      </div>
                    </div>

                    {segments.length === 0 ? (
                      <p className="text-[13px] text-slate-400 font-semibold">Chưa có dữ liệu lịch hẹn trong kỳ này.</p>
                    ) : (
                      <div className="flex flex-wrap items-center justify-center gap-x-4 gap-y-2 w-full text-[13px] font-bold text-slate-500 mt-1">
                        {segments.map((segment, idx) => (
                          <div
                            key={idx}
                            className={`flex items-center gap-1.5 transition-all cursor-pointer ${hoveredIndex === idx ? "text-slate-900 scale-105" : "text-slate-500"}`}
                            onMouseEnter={() => setHoveredIndex(idx)}
                            onMouseLeave={() => setHoveredIndex(null)}
                          >
                            <span className="w-2 h-2 rounded-full shrink-0" style={{ backgroundColor: segment.stroke }}></span>
                            <span className="truncate">{segment.name} ({segment.value}%)</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>

              </div>

              {/* Row 2: Danh Sách Lịch Hẹn Hôm Nay (Full-width) */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
                <div className="p-4.5 border-b border-slate-100 flex justify-between items-center">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Lịch Hẹn Hôm Nay</h3>
                    <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Danh sách các cuộc hẹn đăng ký khám chữa bệnh trong ngày hôm nay</p>
                  </div>
                  <button className="flex items-center gap-1.5 bg-slate-50 hover:bg-slate-100 text-slate-700 hover:text-slate-900 border border-slate-200 text-[12px] font-bold px-3.5 py-1.5 rounded-xl transition-all hover:translate-y-[-1px] cursor-pointer">
                    <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 6h9.75M10.5 6a1.5 1.5 0 11-3 0m3 0a1.5 1.5 0 10-3 0M3.75 6H7.5m3 12h9.75m-9.75 0a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m-3.75 0H7.5m9-6h3.75m-3.75 0a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m-9.75 0h9.75" />
                    </svg>
                    Bộ lọc & Sắp xếp
                  </button>
                </div>

                <div className="overflow-x-auto flex-1">
                  <table className="w-full text-left border-collapse text-[13px]">
                    <thead>
                      <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-100">
                        <th className="px-5 py-3">Giờ khám</th>
                        <th className="px-5 py-3">Họ và tên bệnh nhân</th>
                        <th className="px-5 py-3">Dịch vụ điều trị</th>
                        <th className="px-5 py-3">Trạng thái</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                      {!todayAppointments || todayAppointments.items.length === 0 ? (
                        <tr>
                          <td colSpan={4} className="px-5 py-6 text-center text-slate-400 font-semibold">
                            Không có lịch hẹn nào hôm nay.
                          </td>
                        </tr>
                      ) : (
                        todayAppointments.items.map((item) => {
                          const badge = STATUS_BADGES[item.status] ?? { label: item.status, className: "bg-slate-100 text-slate-500" };
                          return (
                            <tr key={item.id} className="hover:bg-slate-50/30 transition-colors">
                              <td className="px-5 py-3 font-bold text-slate-950">{formatTime(item.appointmentDate)}</td>
                              <td className="px-5 py-3 font-bold text-slate-800">{item.patientName}</td>
                              <td className="px-5 py-3 text-slate-500 font-medium">{item.serviceName ?? "—"}</td>
                              <td className="px-5 py-3">
                                <span className={`inline-flex px-2 py-0.5 rounded-full text-[12px] font-bold ${badge.className}`}>{badge.label}</span>
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>
              </div>

            </div>

            {/* ── RIGHT COLUMN (Calendar & Shifts & Top-rated dentists) ── */}
            <div className="lg:col-span-4 flex flex-col min-w-0 self-stretch">

              {/* Container 1: Lịch vận hành & ca trực */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5 min-h-[500px] shrink-0">
                <div>
                  <h3 className="text-[18px] font-extrabold text-slate-900">Lịch Vận Hành Phòng Khám</h3>
                  <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Thông tin ca làm việc của các bác sĩ</p>
                </div>

                {/* Weekly Horizontal Calendar */}
                <div className="grid grid-cols-7 gap-1.5 text-center">
                  {(weeklySchedule?.week ?? []).map((day) => (
                    <div
                      key={day.date}
                      className={
                        day.isToday
                          ? "flex flex-col items-center py-2.5 rounded-md bg-primary text-white border border-primary shadow-sm shadow-primary/20 cursor-pointer transition-all"
                          : "flex flex-col items-center py-2.5 rounded-md border border-slate-100 cursor-pointer hover:bg-red-50/20 hover:border-primary/40 transition-all duration-200"
                      }
                    >
                      <span className={`text-[12px] font-bold uppercase ${day.isToday ? "text-white/80" : "text-slate-400"}`}>
                        {vnWeekdayLabel(day.date)}
                      </span>
                      <span className={`text-[15px] font-extrabold mt-0.5 ${day.isToday ? "text-white" : "text-slate-700"}`}>
                        {dayOfMonth(day.date)}
                      </span>
                    </div>
                  ))}
                </div>

                {/* Shifts Split: Morning & Afternoon (Compacted) */}
                <div className="flex flex-col gap-4 border-t border-slate-100 pt-4 flex-1 justify-start">

                  {/* Morning Shift (Ca Sáng) */}
                  <div className="flex flex-col gap-2">
                    <div className="flex items-center gap-2">
                      <span className="w-2 h-2 rounded-full bg-primary"></span>
                      <span className="text-[12px] font-extrabold text-slate-900 uppercase tracking-wider">Lịch trực ca sáng (08:00 - 12:00)</span>
                    </div>
                    <div className="flex flex-col gap-2">
                      {(weeklySchedule?.morningShift ?? []).length === 0 ? (
                        <p className="text-[12px] text-slate-400 font-semibold px-1">Không có bác sĩ trực ca sáng.</p>
                      ) : (
                        weeklySchedule!.morningShift.map((s, idx) => (
                          <div key={idx} className="flex items-center justify-between p-2 rounded-xl bg-slate-50 border border-slate-200/60 hover:border-primary/40 hover:bg-red-50/10 cursor-pointer transition-all duration-200">
                            <div className="flex items-center gap-2">
                              <Avatar url={s.profilePictureUrl} name={s.staffName} className="w-6 h-6 rounded-full border border-slate-200" />
                              <div>
                                <div className="text-[13px] font-bold text-slate-900">{s.staffName}</div>
                                <div className="text-[11px] text-slate-400 font-semibold">{s.specialization ?? s.room}</div>
                              </div>
                            </div>
                            <span className={`inline-flex items-center gap-1 text-[11px] font-bold px-1.5 py-0.5 rounded-full ${s.isBusy ? "text-amber-500 bg-amber-50" : "text-green-600 bg-green-50"}`}>
                              {s.isBusy ? "Đang khám" : "Đang rảnh"}
                            </span>
                          </div>
                        ))
                      )}
                    </div>
                  </div>

                  {/* Afternoon Shift (Ca Chiều) */}
                  <div className="flex flex-col gap-2">
                    <div className="flex items-center gap-2">
                      <span className="w-2 h-2 rounded-full bg-secondary"></span>
                      <span className="text-[12px] font-extrabold text-slate-900 uppercase tracking-wider">Lịch trực ca chiều (13:30 - 17:30)</span>
                    </div>
                    <div className="flex flex-col gap-2">
                      {(weeklySchedule?.afternoonShift ?? []).length === 0 ? (
                        <p className="text-[12px] text-slate-400 font-semibold px-1">Không có bác sĩ trực ca chiều.</p>
                      ) : (
                        weeklySchedule!.afternoonShift.map((s, idx) => (
                          <div key={idx} className="flex items-center justify-between p-2 rounded-xl bg-slate-50 border border-slate-200/60 hover:border-primary/40 hover:bg-red-50/10 cursor-pointer transition-all duration-200">
                            <div className="flex items-center gap-2">
                              <Avatar url={s.profilePictureUrl} name={s.staffName} className="w-6 h-6 rounded-full border border-slate-200" />
                              <div>
                                <div className="text-[13px] font-bold text-slate-900">{s.staffName}</div>
                                <div className="text-[11px] text-slate-400 font-semibold">{s.specialization ?? s.room}</div>
                              </div>
                            </div>
                            <span className={`inline-flex items-center gap-1 text-[11px] font-bold px-1.5 py-0.5 rounded-full ${s.isBusy ? "text-amber-500 bg-amber-50" : "text-green-600 bg-green-50"}`}>
                              {s.isBusy ? "Đang khám" : "Đang rảnh"}
                            </span>
                          </div>
                        ))
                      )}
                    </div>
                  </div>

                </div>
              </div>

              {/* Container 2: Đánh giá từ khách hàng (Độc lập) */}
              <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5 mt-6 shrink-0">
                <div className="flex items-center justify-between">
                  <span className="text-[16px] font-extrabold text-slate-900 tracking-wider">Đánh giá từ khách hàng</span>
                  <span className="text-[12px] text-amber-500 font-bold flex items-center gap-0.5">
                    ★ Đánh giá {recentFeedback ? recentFeedback.averageRating.toFixed(1) : "—"}/5
                  </span>
                </div>
                <div className="flex flex-col gap-3">
                  {!recentFeedback || recentFeedback.items.length === 0 ? (
                    <p className="text-[13px] text-slate-400 font-semibold">Chưa có đánh giá nổi bật nào.</p>
                  ) : (
                    recentFeedback.items.map((review) => (
                      <div key={review.id} className="flex flex-col gap-2 p-3 rounded-xl bg-slate-50 border border-slate-100 hover:border-slate-200 hover:bg-slate-100/30 transition-all duration-200">
                        <div className="flex items-center gap-2">
                          <Avatar url={null} name={review.customerName} className="w-7 h-7 rounded-full border border-slate-200" />
                          <div className="min-w-0">
                            <div className="text-[12px] font-bold text-slate-900 truncate">{review.customerName}</div>
                            <div className="text-[10px] text-slate-400 font-medium">
                              {new Date(review.createdAt).toLocaleDateString("vi-VN")}
                            </div>
                          </div>
                        </div>
                        <div className="flex items-center gap-0.5 text-amber-400 text-[10px]">
                          {"⭐".repeat(review.rating)}
                        </div>
                        <p className="text-[12px] text-slate-600 italic leading-relaxed">&ldquo;{review.comment}&rdquo;</p>
                      </div>
                    ))
                  )}
                </div>
              </div>

            </div>

          </div>
        </div>
      </main>
    </div>
  );
}
