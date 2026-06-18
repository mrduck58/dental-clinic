"use client";

import { useState } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";

type AttendanceStatus = "present" | "absent" | "leave" | "none";

interface DaySchedule {
  date: string;
  dayLabel: string;
  dateLabel: string;
  shift: "morning" | "afternoon" | "both" | "none";
  room: string;
  attendance: AttendanceStatus;
  checkIn?: string;
  checkOut?: string;
}

const WEEK: DaySchedule[] = [
  { date: "2026-06-08", dayLabel: "Thứ 2", dateLabel: "08/06", shift: "morning",   room: "Phòng 1", attendance: "present", checkIn: "07:29", checkOut: "12:05" },
  { date: "2026-06-09", dayLabel: "Thứ 3", dateLabel: "09/06", shift: "afternoon",  room: "Phòng 2", attendance: "present", checkIn: "13:01", checkOut: "17:32" },
  { date: "2026-06-10", dayLabel: "Thứ 4", dateLabel: "10/06", shift: "morning",   room: "Phòng 1", attendance: "present", checkIn: "07:31", checkOut: "12:02" },
  { date: "2026-06-11", dayLabel: "Thứ 5", dateLabel: "11/06", shift: "none",      room: "",        attendance: "none" },
  { date: "2026-06-12", dayLabel: "Thứ 6", dateLabel: "12/06", shift: "morning",   room: "Phòng 1", attendance: "present", checkIn: "07:28", checkOut: undefined },
  { date: "2026-06-13", dayLabel: "Thứ 7", dateLabel: "13/06", shift: "afternoon", room: "Phòng 3", attendance: "none" },
  { date: "2026-06-14", dayLabel: "CN",    dateLabel: "14/06", shift: "none",      room: "",        attendance: "none" },
];

const ATTENDANCE_HISTORY = [
  { month: "Tháng 6/2026", present: 10, absent: 0, leave: 1, total: 11, rate: "91%" },
  { month: "Tháng 5/2026", present: 22, absent: 0, leave: 0, total: 22, rate: "100%" },
  { month: "Tháng 4/2026", present: 20, absent: 1, leave: 1, total: 22, rate: "91%" },
];

export default function DentistSchedulePage() {
  useRequireDentist();
  const [checkedOut, setCheckedOut] = useState(false);
  const today = WEEK[4]; // Thứ 6 12/06

  const shiftLabel = (s: DaySchedule["shift"]) => {
    if (s === "morning") return "Ca sáng · 07:30–12:00";
    if (s === "afternoon") return "Ca chiều · 13:00–17:30";
    if (s === "both") return "Cả ngày";
    return null;
  };

  const attendanceBadge = (a: AttendanceStatus) => {
    if (a === "present") return <span className="text-[11px] font-black px-2 py-0.5 rounded-full bg-green-50 text-green-700 border border-green-100">Có mặt</span>;
    if (a === "absent")  return <span className="text-[11px] font-black px-2 py-0.5 rounded-full bg-red-50 text-primary border border-red-100">Vắng</span>;
    if (a === "leave")   return <span className="text-[11px] font-black px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-100">Nghỉ phép</span>;
    return null;
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="schedule" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader title="Lịch Làm Việc & Chấm Công" subtitle="Tuần 24 · 08/06 – 14/06/2026" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* ATTENDANCE CARD TODAY */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-5">
            <div className="flex flex-col gap-1.5">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Chấm công hôm nay — Thứ 6, 12/06/2026</span>
              <span className="text-[17px] font-black text-slate-900">Ca sáng · 07:30 – 12:00 · Phòng 1</span>
              <div className="flex items-center gap-4 mt-1 text-[13px] font-semibold">
                <span className="flex items-center gap-1.5 text-green-700">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" /></svg>
                  Vào ca: <span className="font-black">07:28</span>
                </span>
                {checkedOut ? (
                  <span className="flex items-center gap-1.5 text-slate-600">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" /></svg>
                    Ra ca: <span className="font-black">12:01</span>
                  </span>
                ) : (
                  <span className="text-slate-400">Ra ca: chưa ghi nhận</span>
                )}
              </div>
            </div>
            {!checkedOut ? (
              <button
                onClick={() => setCheckedOut(true)}
                className="flex items-center gap-2 px-5 py-2.5 bg-slate-800 hover:bg-slate-700 text-white text-[13px] font-bold rounded-xl transition-all cursor-pointer shadow-sm whitespace-nowrap"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
                </svg>
                Ghi nhận ra ca
              </button>
            ) : (
              <div className="flex items-center gap-2 px-4 py-2.5 bg-green-50 border border-green-100 text-green-700 text-[13px] font-bold rounded-xl">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                Đã hoàn thành ca
              </div>
            )}
          </div>

          {/* WEEKLY CALENDAR */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
              <h2 className="text-[15px] font-black text-slate-900">Lịch tuần 24</h2>
              <div className="flex items-center gap-3 text-[12.5px] font-semibold">
                <span className="flex items-center gap-1.5"><span className="w-2.5 h-2.5 rounded-full bg-primary inline-block" />Ca sáng</span>
                <span className="flex items-center gap-1.5"><span className="w-2.5 h-2.5 rounded-full bg-secondary inline-block" />Ca chiều</span>
                <span className="flex items-center gap-1.5"><span className="w-2.5 h-2.5 rounded-full bg-slate-300 inline-block" />Nghỉ</span>
              </div>
            </div>
            <div className="grid grid-cols-7 divide-x divide-slate-100">
              {WEEK.map((day) => {
                const isToday = day.date === "2026-06-12";
                return (
                  <div key={day.date} className={`p-4 flex flex-col gap-2 min-h-[140px] ${isToday ? "bg-red-50/40" : ""}`}>
                    <div className="flex flex-col items-center gap-0.5">
                      <span className={`text-[11px] font-extrabold uppercase tracking-wider ${isToday ? "text-primary" : "text-slate-400"}`}>{day.dayLabel}</span>
                      <span className={`text-[15px] font-black ${isToday ? "text-primary" : "text-slate-700"}`}>{day.dateLabel.split("/")[0]}</span>
                      {isToday && <span className="w-1.5 h-1.5 rounded-full bg-primary" />}
                    </div>
                    {day.shift !== "none" ? (
                      <div className={`rounded-xl px-2 py-2 flex flex-col gap-1 ${day.shift === "morning" ? "bg-red-50 border border-red-100" : day.shift === "afternoon" ? "bg-sky-50 border border-sky-100" : "bg-violet-50 border border-violet-100"}`}>
                        <span className={`text-[10.5px] font-black ${day.shift === "morning" ? "text-primary" : day.shift === "afternoon" ? "text-secondary" : "text-violet-700"}`}>
                          {day.shift === "morning" ? "Ca sáng" : day.shift === "afternoon" ? "Ca chiều" : "Cả ngày"}
                        </span>
                        <span className="text-[10px] font-semibold text-slate-500">{day.room}</span>
                        {day.attendance === "present" && (
                          <span className="text-[9.5px] font-black text-green-600">✓ {day.checkIn}{day.checkOut ? ` – ${day.checkOut}` : " →"}</span>
                        )}
                      </div>
                    ) : (
                      <div className="rounded-xl px-2 py-2 bg-slate-50 border border-slate-100 flex items-center justify-center">
                        <span className="text-[10.5px] font-semibold text-slate-400">Nghỉ</span>
                      </div>
                    )}
                    <div className="mt-auto">{attendanceBadge(day.attendance)}</div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* ATTENDANCE STATS */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100">
              <h2 className="text-[15px] font-black text-slate-900">Thống kê chuyên cần</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-[13px]">
                <thead>
                  <tr className="bg-slate-50/70 text-slate-400 text-[11px] font-extrabold uppercase tracking-wider border-b border-slate-100">
                    <th className="px-6 py-3.5 text-left">Tháng</th>
                    <th className="px-6 py-3.5 text-center">Có mặt</th>
                    <th className="px-6 py-3.5 text-center">Nghỉ phép</th>
                    <th className="px-6 py-3.5 text-center">Vắng</th>
                    <th className="px-6 py-3.5 text-center">Tổng ca</th>
                    <th className="px-6 py-3.5 text-center">Tỷ lệ</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {ATTENDANCE_HISTORY.map((r) => (
                    <tr key={r.month} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-6 py-3.5 font-bold text-slate-800">{r.month}</td>
                      <td className="px-6 py-3.5 text-center"><span className="font-black text-green-700">{r.present}</span></td>
                      <td className="px-6 py-3.5 text-center"><span className="font-bold text-amber-600">{r.leave}</span></td>
                      <td className="px-6 py-3.5 text-center"><span className="font-bold text-red-500">{r.absent}</span></td>
                      <td className="px-6 py-3.5 text-center font-semibold text-slate-600">{r.total}</td>
                      <td className="px-6 py-3.5 text-center">
                        <span className={`px-2.5 py-1 rounded-full text-[11.5px] font-black ${r.rate === "100%" ? "bg-green-50 text-green-700 border border-green-100" : "bg-amber-50 text-amber-700 border border-amber-100"}`}>{r.rate}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

        </div>
      </main>
    </div>
  );
}
