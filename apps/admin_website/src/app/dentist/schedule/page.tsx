"use client";

import { useState, useEffect, useMemo, useCallback } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";
import { getMyScheduleApi, type ScheduleEntryDto } from "../../../lib/apiClient";
import { SHIFTS, shiftLabel, periodOfShift, type ShiftPeriod } from "../../../lib/shifts";

const DAY_LABELS = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN"];

// Thứ tự các ca trong ngày để sắp xếp theo thời gian
const SHIFT_ORDER: Record<string, number> = Object.fromEntries(SHIFTS.map((s, i) => [s.id, i]));

// Màu theo buổi
const PERIOD_STYLE: Record<ShiftPeriod, { chip: string; dot: string; text: string }> = {
  "Buổi sáng":  { chip: "bg-amber-50 border-amber-100",  dot: "bg-amber-400",  text: "text-amber-700" },
  "Buổi chiều": { chip: "bg-sky-50 border-sky-100",      dot: "bg-sky-400",    text: "text-sky-700" },
  "Buổi tối":   { chip: "bg-violet-50 border-violet-100", dot: "bg-violet-400", text: "text-violet-700" },
};

const pad = (n: number) => String(n).padStart(2, "0");
const formatDateKey = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const fmtDayMonth = (d: Date) => `${pad(d.getDate())}/${pad(d.getMonth() + 1)}`;

const getThisWeekMonday = (): Date => {
  const today = new Date();
  const day = today.getDay();
  const diff = today.getDate() - day + (day === 0 ? -6 : 1);
  const monday = new Date(today);
  monday.setDate(diff);
  monday.setHours(0, 0, 0, 0);
  return monday;
};

const getWeekDates = (monday: Date): Date[] =>
  Array.from({ length: 7 }, (_, i) => {
    const d = new Date(monday);
    d.setDate(monday.getDate() + i);
    return d;
  });

export default function DentistSchedulePage() {
  useRequireDentist();

  const [monday, setMonday] = useState<Date>(getThisWeekMonday);
  const [entries, setEntries] = useState<ScheduleEntryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const weekDates = useMemo(() => getWeekDates(monday), [monday]);
  const todayKey = formatDateKey(new Date());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getMyScheduleApi(formatDateKey(monday));
      setEntries(data);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể tải lịch làm việc");
    } finally {
      setLoading(false);
    }
  }, [monday]);

  useEffect(() => { void load(); }, [load]);

  // Gom ca theo ngày, sắp xếp theo thời gian trong ngày
  const byDate = useMemo(() => {
    const m: Record<string, ScheduleEntryDto[]> = {};
    for (const e of entries) (m[e.date] ??= []).push(e);
    for (const k in m) m[k].sort((a, b) => (SHIFT_ORDER[a.shift] ?? 99) - (SHIFT_ORDER[b.shift] ?? 99));
    return m;
  }, [entries]);

  // Tổng kết tuần theo buổi
  const summary = useMemo(() => {
    const c = { total: 0, morning: 0, afternoon: 0, evening: 0 };
    for (const e of entries) {
      if (e.isHoliday) continue;
      c.total++;
      const p = periodOfShift(e.shift);
      if (p === "Buổi sáng") c.morning++;
      else if (p === "Buổi chiều") c.afternoon++;
      else c.evening++;
    }
    return c;
  }, [entries]);

  const weekLabel = `${fmtDayMonth(weekDates[0])} – ${fmtDayMonth(weekDates[6])}/${weekDates[6].getFullYear()}`;

  const goPrev = () => { const d = new Date(monday); d.setDate(monday.getDate() - 7); setMonday(d); };
  const goNext = () => { const d = new Date(monday); d.setDate(monday.getDate() + 7); setMonday(d); };
  const goThis = () => setMonday(getThisWeekMonday());

  const isThisWeek = formatDateKey(monday) === formatDateKey(getThisWeekMonday());

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="schedule" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader title="Lịch Làm Việc" subtitle="Xem lịch ca làm việc của bạn theo tuần" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* WEEK NAV + SUMMARY */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm px-6 py-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <button onClick={goPrev} className="w-9 h-9 rounded-lg border border-slate-200 flex items-center justify-center text-slate-500 hover:bg-slate-50 cursor-pointer">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
              </button>
              <div className="text-center min-w-[150px]">
                <div className="text-[15px] font-black text-slate-900">{weekLabel}</div>
                <div className="text-[11.5px] font-semibold text-slate-400">{isThisWeek ? "Tuần này" : "Tuần khác"}</div>
              </div>
              <button onClick={goNext} className="w-9 h-9 rounded-lg border border-slate-200 flex items-center justify-center text-slate-500 hover:bg-slate-50 cursor-pointer">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" /></svg>
              </button>
              {!isThisWeek && (
                <button onClick={goThis} className="ml-2 px-3 py-1.5 text-[12.5px] font-bold text-primary border border-primary/30 rounded-lg hover:bg-primary/5 cursor-pointer">
                  Về tuần này
                </button>
              )}
            </div>

            <div className="flex items-center gap-2.5 text-[12.5px] font-bold">
              <span className="px-3 py-1.5 bg-slate-100 text-slate-600 rounded-lg">{summary.total} ca / tuần</span>
              <span className="px-3 py-1.5 bg-amber-50 text-amber-700 border border-amber-100 rounded-lg">Sáng: {summary.morning}</span>
              <span className="px-3 py-1.5 bg-sky-50 text-sky-700 border border-sky-100 rounded-lg">Chiều: {summary.afternoon}</span>
              <span className="px-3 py-1.5 bg-violet-50 text-violet-700 border border-violet-100 rounded-lg">Tối: {summary.evening}</span>
            </div>
          </div>

          {/* WEEK GRID */}
          {loading ? (
            <div className="flex items-center justify-center py-24">
              <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : error ? (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
              <p className="text-[14px] font-semibold text-red-500">{error}</p>
              <button onClick={() => void load()} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-lg cursor-pointer">Thử lại</button>
            </div>
          ) : (
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-7 divide-y lg:divide-y-0 lg:divide-x divide-slate-100">
                {weekDates.map((d, i) => {
                  const key = formatDateKey(d);
                  const dayEntries = byDate[key] ?? [];
                  const isToday = key === todayKey;
                  const isHoliday = dayEntries.some(e => e.isHoliday);
                  const shifts = dayEntries.filter(e => !e.isHoliday);

                  return (
                    <div key={key} className={`p-4 flex flex-col gap-2.5 min-h-[180px] ${isToday ? "bg-red-50/40" : ""}`}>
                      <div className="flex flex-col items-center gap-0.5">
                        <span className={`text-[11px] font-extrabold uppercase tracking-wider ${isToday ? "text-primary" : "text-slate-400"}`}>{DAY_LABELS[i]}</span>
                        <span className={`text-[16px] font-black ${isToday ? "text-primary" : "text-slate-700"}`}>{pad(d.getDate())}</span>
                        {isToday && <span className="text-[10px] font-black text-primary">Hôm nay</span>}
                      </div>

                      {isHoliday ? (
                        <div className="rounded-xl px-2.5 py-3 bg-rose-50 border border-rose-100 flex items-center justify-center">
                          <span className="text-[11px] font-black text-rose-600">Nghỉ lễ</span>
                        </div>
                      ) : shifts.length > 0 ? (
                        <div className="flex flex-col gap-1.5">
                          {shifts.map(e => {
                            const period = periodOfShift(e.shift);
                            const st = PERIOD_STYLE[period];
                            return (
                              <div key={e.id} className={`rounded-xl px-2.5 py-2 border flex flex-col gap-1 ${st.chip}`}>
                                <div className="flex items-center gap-1.5">
                                  <span className={`w-1.5 h-1.5 rounded-full ${st.dot}`} />
                                  <span className={`text-[10px] font-black uppercase tracking-wide ${st.text}`}>{period}</span>
                                </div>
                                <span className="text-[12px] font-black text-slate-800 font-mono">{shiftLabel(e.shift)}</span>
                                {e.room && <span className="text-[10.5px] font-semibold text-slate-500">{e.room}</span>}
                              </div>
                            );
                          })}
                        </div>
                      ) : (
                        <div className="rounded-xl px-2.5 py-3 bg-slate-50 border border-slate-100 flex items-center justify-center mt-auto">
                          <span className="text-[11px] font-semibold text-slate-400">Nghỉ</span>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          )}

        </div>
      </main>
    </div>
  );
}
