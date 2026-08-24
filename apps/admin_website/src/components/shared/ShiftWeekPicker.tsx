"use client";

import { useState, useEffect, useMemo, useCallback } from "react";
import { getMyScheduleApi, type ScheduleEntryDto } from "../../lib/apiClient";
import { SHIFTS, shiftLabel, periodOfShift, type ShiftPeriod } from "../../lib/shifts";

const DAY_LABELS = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN"];

const SHIFT_ORDER: Record<string, number> = Object.fromEntries(SHIFTS.map((s, i) => [s.id, i]));

const PERIOD_STYLE: Record<ShiftPeriod, { chip: string; dot: string; text: string; chipSelected: string }> = {
  "Buổi sáng":  { chip: "bg-amber-50 border-amber-100",   dot: "bg-amber-400",  text: "text-amber-700", chipSelected: "bg-amber-500 border-amber-500" },
  "Buổi chiều": { chip: "bg-sky-50 border-sky-100",       dot: "bg-sky-400",    text: "text-sky-700",   chipSelected: "bg-sky-500 border-sky-500" },
  "Buổi tối":   { chip: "bg-violet-50 border-violet-100", dot: "bg-violet-400", text: "text-violet-700", chipSelected: "bg-violet-500 border-violet-500" },
};

const pad = (n: number) => String(n).padStart(2, "0");
const formatDateKey = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const fmtDayMonth = (d: Date) => `${pad(d.getDate())}/${pad(d.getMonth() + 1)}`;
const selectionKey = (date: string, shiftId: string) => `${date}__${shiftId}`;

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

interface ShiftWeekPickerProps {
  selected: Set<string>;
  onToggle: (date: string, shiftId: string) => void;
}

/// Lưới chọn ca theo tuần dùng cho form "Đơn xin nghỉ" — cùng bố cục với trang "Lịch Làm Việc"
/// (dentist/schedule), nhưng mỗi thẻ ca là nút bấm chọn/bỏ chọn thay vì chỉ hiển thị.
export default function ShiftWeekPicker({ selected, onToggle }: ShiftWeekPickerProps) {
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

  const byDate = useMemo(() => {
    const m: Record<string, ScheduleEntryDto[]> = {};
    for (const e of entries) (m[e.date] ??= []).push(e);
    for (const k in m) m[k].sort((a, b) => (SHIFT_ORDER[a.shift] ?? 99) - (SHIFT_ORDER[b.shift] ?? 99));
    return m;
  }, [entries]);

  const weekLabel = `${fmtDayMonth(weekDates[0])} – ${fmtDayMonth(weekDates[6])}/${weekDates[6].getFullYear()}`;
  const isThisWeek = formatDateKey(monday) === formatDateKey(getThisWeekMonday());

  const goPrev = () => { const d = new Date(monday); d.setDate(monday.getDate() - 7); setMonday(d); };
  const goNext = () => { const d = new Date(monday); d.setDate(monday.getDate() + 7); setMonday(d); };
  const goThis = () => setMonday(getThisWeekMonday());

  return (
    <div className="flex flex-col gap-3">
      {/* WEEK NAV */}
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <button type="button" onClick={goPrev} className="w-8 h-8 rounded-lg border border-slate-200 flex items-center justify-center text-slate-500 hover:bg-slate-50 cursor-pointer">
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
          </button>
          <div className="text-center min-w-[130px]">
            <div className="text-[13.5px] font-black text-slate-900">{weekLabel}</div>
            <div className="text-[10.5px] font-semibold text-slate-400">{isThisWeek ? "Tuần này" : "Tuần khác"}</div>
          </div>
          <button type="button" onClick={goNext} className="w-8 h-8 rounded-lg border border-slate-200 flex items-center justify-center text-slate-500 hover:bg-slate-50 cursor-pointer">
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" /></svg>
          </button>
        </div>
        {!isThisWeek && (
          <button type="button" onClick={goThis} className="px-2.5 py-1 text-[11.5px] font-bold text-primary border border-primary/30 rounded-lg hover:bg-primary/5 cursor-pointer">
            Về tuần này
          </button>
        )}
      </div>

      {/* WEEK GRID */}
      {loading ? (
        <div className="flex items-center justify-center py-10">
          <div className="w-6 h-6 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
        </div>
      ) : error ? (
        <div className="bg-slate-50 rounded-xl border border-slate-200/70 flex flex-col items-center gap-2 py-8">
          <p className="text-[12.5px] font-semibold text-red-500">{error}</p>
          <button type="button" onClick={() => void load()} className="px-3 py-1.5 text-[12px] font-bold bg-primary text-white rounded-lg cursor-pointer">Thử lại</button>
        </div>
      ) : (
        <div className="rounded-xl border border-slate-200/70 overflow-hidden">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-7 divide-y lg:divide-y-0 lg:divide-x divide-slate-100">
            {weekDates.map((d, i) => {
              const key = formatDateKey(d);
              const dayEntries = byDate[key] ?? [];
              const isToday = key === todayKey;
              const isHoliday = dayEntries.some(e => e.isHoliday);
              const shifts = dayEntries.filter(e => !e.isHoliday);

              return (
                <div key={key} className={`p-2.5 flex flex-col gap-2 min-h-[130px] ${isToday ? "bg-red-50/40" : ""}`}>
                  <div className="flex flex-col items-center gap-0.5">
                    <span className={`text-[10px] font-extrabold uppercase tracking-wider ${isToday ? "text-primary" : "text-slate-400"}`}>{DAY_LABELS[i]}</span>
                    <span className={`text-[13.5px] font-black ${isToday ? "text-primary" : "text-slate-700"}`}>{pad(d.getDate())}</span>
                  </div>

                  {isHoliday ? (
                    <div className="rounded-lg px-2 py-2 bg-rose-50 border border-rose-100 flex items-center justify-center">
                      <span className="text-[10px] font-black text-rose-600">Nghỉ lễ</span>
                    </div>
                  ) : shifts.length > 0 ? (
                    <div className="flex flex-col gap-1">
                      {shifts.map(e => {
                        const period = periodOfShift(e.shift);
                        const st = PERIOD_STYLE[period];
                        const isSelected = selected.has(selectionKey(key, e.shift));
                        return (
                          <button
                            type="button"
                            key={e.id}
                            onClick={() => onToggle(key, e.shift)}
                            className={`rounded-lg px-2 py-1.5 border flex flex-col gap-0.5 text-left transition-all cursor-pointer ${
                              isSelected ? `${st.chipSelected} shadow-sm` : `${st.chip} hover:brightness-95`
                            }`}
                          >
                            <div className="flex items-center gap-1">
                              <span className={`w-1.5 h-1.5 rounded-full ${isSelected ? "bg-white" : st.dot}`} />
                              <span className={`text-[9px] font-black uppercase tracking-wide ${isSelected ? "text-white" : st.text}`}>{period}</span>
                              {isSelected && (
                                <svg className="w-3 h-3 ml-auto text-white" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                                </svg>
                              )}
                            </div>
                            <span className={`text-[11px] font-black font-mono ${isSelected ? "text-white" : "text-slate-800"}`}>{shiftLabel(e.shift)}</span>
                          </button>
                        );
                      })}
                    </div>
                  ) : (
                    <div className="rounded-lg px-2 py-2 bg-slate-50 border border-slate-100 flex items-center justify-center mt-auto">
                      <span className="text-[10px] font-semibold text-slate-400">Không có ca</span>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
