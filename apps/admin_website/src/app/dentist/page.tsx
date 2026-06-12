"use client";

import Link from "next/link";
import DentistSidebar from "../../components/shared/DentistSidebar";
import DentistPageHeader from "../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../hooks/useRequireDentist";

const TODAY_SHIFT = { shift: "Ca sáng", time: "07:30 – 12:00", room: "Phòng 1", checkedIn: true, checkedInAt: "07:28" };

const QUICK_PATIENTS = [
  { id: "P002", name: "Trần Thị Bích",   reason: "Trám răng số 6",                  time: "08:30", status: "in_progress" },
  { id: "P003", name: "Phạm Minh Cường", reason: "Kiểm tra định kỳ, lấy cao răng",  time: "09:00", status: "waiting" },
  { id: "P004", name: "Lê Thu Hà",       reason: "Tẩy trắng răng Zoom Advanced",    time: "09:30", status: "waiting" },
  { id: "P005", name: "Hoàng Văn Đức",   reason: "Cấy ghép Implant răng số 4",      time: "10:00", status: "waiting" },
];

const STATUS_CFG = {
  waiting:     { label: "Đang chờ",  cls: "bg-amber-50 text-amber-700 border border-amber-100"  },
  in_progress: { label: "Đang khám", cls: "bg-sky-50 text-sky-700 border border-sky-100"       },
  done:        { label: "Hoàn thành",cls: "bg-green-50 text-green-700 border border-green-100" },
};

export default function DentistOverviewPage() {
  useRequireDentist();

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="overview" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Tổng Quan"
          subtitle="Xin chào, Bác sĩ Thảo"
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {/* Stats */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            {[
              { label: "Bệnh nhân hôm nay", value: "8", sub: "Ca sáng + chiều",    icon: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z", bg: "bg-sky-50",   color: "text-secondary", dot: false },
              { label: "Đang chờ khám",     value: "5", sub: "Cần xử lý",          icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z",                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          bg: "bg-amber-50", color: "text-amber-600", dot: true, dotColor: "bg-amber-400" },
              { label: "Đã hoàn thành",     value: "2", sub: "Hôm nay",            icon: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z",                                                                                                                                                                                                                                                                                                                                                                                                                                                                              bg: "bg-green-50", color: "text-green-600", dot: false },
              { label: "Ca tuần này",        value: "5", sub: "Sáng: 3 · Chiều: 2", icon: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5", bg: "bg-red-50",   color: "text-primary",   dot: false },
            ].map((s) => (
              <div key={s.label} className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
                <div>
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">{s.label}</span>
                  <div className="flex items-center gap-2 mt-1">
                    <span className="text-3xl font-black text-slate-900 leading-none">{s.value}</span>
                    {s.dot && (
                      <span className="relative flex h-3 w-3 mb-0.5">
                        <span className={`animate-ping absolute inline-flex h-full w-full rounded-full ${(s as { dotColor?: string }).dotColor} opacity-75`} />
                        <span className={`relative inline-flex rounded-full h-3 w-3 ${(s as { dotColor?: string }).dotColor}`} />
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
            {/* Ca làm việc hôm nay */}
            <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-4">
              <div className="flex items-center justify-between">
                <h2 className="text-[15px] font-black text-slate-900">Ca làm việc hôm nay</h2>
                <Link href="/dentist/schedule" className="text-[12.5px] font-bold text-primary hover:underline">Xem lịch →</Link>
              </div>
              <div className="bg-gradient-to-br from-red-50 to-rose-50 border border-red-100 rounded-xl p-4 flex flex-col gap-3">
                <div className="flex items-center justify-between">
                  <span className="text-[13px] font-black text-slate-800">{TODAY_SHIFT.shift}</span>
                  <span className="px-2.5 py-1 bg-green-100 text-green-700 text-[11.5px] font-black rounded-full">Đang hoạt động</span>
                </div>
                <div className="flex items-center gap-2 text-[13px] text-slate-600 font-semibold">
                  <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  {TODAY_SHIFT.time}
                </div>
                <div className="flex items-center gap-2 text-[13px] text-slate-600 font-semibold">
                  <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75" /></svg>
                  {TODAY_SHIFT.room}
                </div>
                <div className="pt-1 border-t border-red-100 flex items-center gap-2 text-[12.5px] text-green-700 font-bold">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  Đã vào ca lúc {TODAY_SHIFT.checkedInAt}
                </div>
              </div>
              <div className="bg-slate-50 rounded-xl p-4 flex flex-col gap-2">
                <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider">Ca chiều</span>
                <span className="text-[13px] font-semibold text-slate-500">Không có lịch</span>
              </div>
            </div>

            {/* Bệnh nhân sắp tới */}
            <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-4">
              <div className="flex items-center justify-between">
                <h2 className="text-[15px] font-black text-slate-900">Bệnh nhân sắp tới</h2>
                <Link href="/dentist/patients" className="text-[12.5px] font-bold text-primary hover:underline">Xem tất cả →</Link>
              </div>
              <ul className="flex flex-col divide-y divide-slate-100">
                {QUICK_PATIENTS.map((p) => {
                  const s = STATUS_CFG[p.status as keyof typeof STATUS_CFG];
                  return (
                    <li key={p.id} className="py-3 flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-sky-50 text-secondary flex items-center justify-center font-black text-[11px] shrink-0 border border-sky-100">
                        {p.name.split(" ").slice(-1)[0][0]}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="text-[13.5px] font-bold text-slate-900 truncate">{p.name}</div>
                        <div className="text-[12px] text-slate-400 font-semibold truncate">{p.reason}</div>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <span className="text-[12px] font-mono font-bold text-slate-500">{p.time}</span>
                        <span className={`px-2 py-0.5 rounded-full text-[11px] font-black whitespace-nowrap ${s.cls}`}>{s.label}</span>
                      </div>
                      <Link href={`/dentist/patients/${p.id}`} className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-primary transition-all">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" /></svg>
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
