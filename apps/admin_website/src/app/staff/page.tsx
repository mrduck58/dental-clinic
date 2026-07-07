"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import StaffSidebar from "../../components/shared/StaffSidebar";
import StaffPageHeader from "../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../hooks/useRequireStaff";
import {
  getUser,
  getStaffDashboardStatsApi,
  getStaffDashboardTodayAppointmentsApi,
  getStaffDashboardPendingInvoicesApi,
  StaffDashboardStatsDto,
  StaffDashboardTodayAppointmentDto,
  StaffDashboardPendingInvoiceDto,
} from "../../lib/apiClient";

const STATUS_CFG: Record<string, { label: string; cls: string }> = {
  Confirmed:  { label: "Xác nhận",  cls: "bg-sky-50 text-sky-700 border border-sky-100"       },
  CheckedIn:  { label: "Check-in",  cls: "bg-green-50 text-green-700 border border-green-100" },
  InProgress: { label: "Đang khám", cls: "bg-violet-50 text-violet-700 border border-violet-100" },
};

function formatTime(dateStr: string): string {
  const d = new Date(dateStr);
  return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
}

export default function StaffOverviewPage() {
  useRequireStaff();

  const [stats, setStats] = useState<StaffDashboardStatsDto | null>(null);
  const [upcoming, setUpcoming] = useState<StaffDashboardTodayAppointmentDto[]>([]);
  const [pendingInvoices, setPendingInvoices] = useState<StaffDashboardPendingInvoiceDto[]>([]);

  const staffName = getUser()?.fullName || "Nhân viên";

  useEffect(() => {
    let cancelled = false;
    getStaffDashboardStatsApi()
      .then((res) => !cancelled && setStats(res))
      .catch((err) => console.error("Không thể tải chỉ số tổng quan:", err));
    getStaffDashboardTodayAppointmentsApi(5)
      .then((res) => !cancelled && setUpcoming(res))
      .catch((err) => console.error("Không thể tải lịch hẹn hôm nay:", err));
    getStaffDashboardPendingInvoicesApi(3)
      .then((res) => !cancelled && setPendingInvoices(res))
      .catch((err) => console.error("Không thể tải hóa đơn chờ thanh toán:", err));
    return () => {
      cancelled = true;
    };
  }, []);

  const statCards = [
    {
      label: "Lịch hẹn hôm nay",
      value: stats?.appointmentsTodayCount ?? "—",
      sub: "Đã xác nhận",
      icon: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5",
      bg: "bg-sky-50",
      color: "text-secondary",
    },
    {
      label: "Chờ check-in",
      value: stats?.waitingCheckInCount ?? "—",
      sub: "Cần xử lý",
      icon: "M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z",
      bg: "bg-amber-50",
      color: "text-amber-600",
      dot: true,
      dotColor: "bg-amber-400",
    },
    {
      label: "Đang trong phòng",
      value: stats?.inProgressCount ?? "—",
      sub: "Đang điều trị",
      icon: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z",
      bg: "bg-violet-50",
      color: "text-violet-600",
    },
    {
      label: "Chờ thanh toán",
      value: stats?.pendingInvoicesCount ?? "—",
      sub: "Hóa đơn pending",
      icon: "M9 14.25l6-6m4.5-3.493V21.75l-3.75-1.5-3.75 1.5-3.75-1.5-3.75 1.5V4.757c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0111.186 0c1.1.128 1.907 1.077 1.907 2.185zM9.75 9h.008v.008H9.75V9zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm4.125 4.5h.008v.008h-.008V13.5zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z",
      bg: "bg-red-50",
      color: "text-primary",
      dot: true,
      dotColor: "bg-red-400",
    },
  ];

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="overview" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader title="Tổng Quan" subtitle={`Xin chào, ${staffName}`} />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {/* Stats */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            {statCards.map((s) => (
              <div key={s.label} className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
                <div>
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">{s.label}</span>
                  <div className="flex items-center gap-2 mt-1">
                    <span className="text-3xl font-black text-slate-900 leading-none">{s.value}</span>
                    {s.dot && (
                      <span className="relative flex h-3 w-3 mb-0.5">
                        <span className={`animate-ping absolute inline-flex h-full w-full rounded-full ${s.dotColor} opacity-75`} />
                        <span className={`relative inline-flex rounded-full h-3 w-3 ${s.dotColor}`} />
                      </span>
                    )}
                  </div>
                  <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">{s.sub}</span>
                </div>
                <div className={`w-12 h-12 rounded-xl ${s.bg} ${s.color} flex items-center justify-center shrink-0`}>
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d={s.icon} />
                  </svg>
                </div>
              </div>
            ))}
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-5 gap-5">
            {/* Upcoming appointments */}
            <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-4">
              <div className="flex items-center justify-between">
                <h2 className="text-[15px] font-black text-slate-900">Lịch hẹn hôm nay</h2>
                <Link href="/staff/appointments" className="text-[12.5px] font-bold text-primary hover:underline">Xem tất cả →</Link>
              </div>
              <ul className="flex flex-col divide-y divide-slate-100">
                {upcoming.length === 0 ? (
                  <li className="py-6 text-center text-[13px] text-slate-400 font-semibold">Không có lịch hẹn nào cần theo dõi.</li>
                ) : (
                  upcoming.map((a) => {
                    const s = STATUS_CFG[a.status] ?? { label: a.status, cls: "bg-slate-100 text-slate-500 border border-slate-200" };
                    return (
                      <li key={a.id} className="py-3 flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-sky-50 text-secondary flex items-center justify-center font-black text-[11px] shrink-0 border border-sky-100">
                          {a.patientName.trim().split(/\s+/).slice(-1)[0][0]}
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="text-[13.5px] font-bold text-slate-900 truncate">{a.patientName}</div>
                          <div className="text-[12px] text-slate-400 font-semibold truncate">{a.serviceName ?? "—"} · {a.dentistName}</div>
                        </div>
                        <div className="flex items-center gap-2 shrink-0">
                          <span className="text-[12px] font-mono font-bold text-slate-500">{formatTime(a.appointmentDate)}</span>
                          <span className={`px-2 py-0.5 rounded-full text-[11px] font-black whitespace-nowrap ${s.cls}`}>{s.label}</span>
                        </div>
                      </li>
                    );
                  })
                )}
              </ul>
            </div>

            {/* Pending invoices */}
            <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-4">
              <div className="flex items-center justify-between">
                <h2 className="text-[15px] font-black text-slate-900">Chờ thanh toán</h2>
                <Link href="/staff/invoices" className="text-[12.5px] font-bold text-primary hover:underline">Xem tất cả →</Link>
              </div>
              <div className="flex flex-col gap-3">
                {pendingInvoices.length === 0 ? (
                  <p className="text-[13px] text-slate-400 font-semibold text-center py-4">Không có hóa đơn nào chờ thanh toán.</p>
                ) : (
                  pendingInvoices.map((inv) => (
                    <div key={inv.id} className="flex items-center gap-3 p-3 bg-red-50/40 border border-red-100/60 rounded-xl">
                      <div className="w-8 h-8 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M9 14.25l6-6m4.5-3.493V21.75l-3.75-1.5-3.75 1.5-3.75-1.5-3.75 1.5V4.757c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0111.186 0c1.1.128 1.907 1.077 1.907 2.185z" />
                        </svg>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="text-[13px] font-bold text-slate-900 truncate">{inv.patientName}</div>
                        <div className="text-[11.5px] text-slate-400 font-semibold">{inv.serviceName ?? "—"}</div>
                      </div>
                      <span className="text-[13px] font-black text-primary shrink-0">{inv.amount.toLocaleString("vi-VN")}đ</span>
                    </div>
                  ))
                )}
              </div>

              {/* Quick actions */}
              <div className="mt-auto pt-2 border-t border-slate-100 grid grid-cols-2 gap-2">
                <Link href="/staff/checkin"
                  className="flex items-center justify-center gap-1.5 px-3 py-2.5 bg-emerald-50 text-emerald-700 rounded-xl text-[12.5px] font-bold hover:bg-emerald-100 transition-colors border border-emerald-100">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
                  Check-in
                </Link>
                <Link href="/staff/queue"
                  className="flex items-center justify-center gap-1.5 px-3 py-2.5 bg-violet-50 text-violet-700 rounded-xl text-[12.5px] font-bold hover:bg-violet-100 transition-colors border border-violet-100">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M3.75 5.25h16.5m-16.5 4.5h16.5m-16.5 4.5h16.5m-16.5 4.5h16.5" /></svg>
                  Hàng đợi
                </Link>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
