"use client";

import { useState, useMemo } from "react";
import { SHIFTS, type ShiftPeriod } from "../../lib/shifts";

const DAY_LABELS = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN"];

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

interface StaffLeaveShiftPickerProps {
  selected: Set<string>;
  onToggle: (date: string, shiftId: string) => void;
}

/// Lưới chọn ca theo tuần cho đơn xin nghỉ của Staff. Khác với ShiftWeekPicker (dentist) — Staff
/// không có lịch làm việc theo ca nào để tra cứu, nên ở đây luôn hiện đủ 6 ca cho mọi ngày để chọn,
/// không gọi API lấy lịch thật.
export default function StaffLeaveShiftPicker({ selected, onToggle }: StaffLeaveShiftPickerProps) {
  const [monday, setMonday] = useState<Date>(getThisWeekMonday);

  const weekDates = useMemo(() => getWeekDates(monday), [monday]);
  const todayKey = formatDateKey(new Date());
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
      <div className="rounded-xl border border-slate-200/70 overflow-hidden">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-7 divide-y lg:divide-y-0 lg:divide-x divide-slate-100">
          {weekDates.map((d, i) => {
            const key = formatDateKey(d);
            const isToday = key === todayKey;

            return (
              <div key={key} className={`p-2.5 flex flex-col gap-2 ${isToday ? "bg-red-50/40" : ""}`}>
                <div className="flex flex-col items-center gap-0.5">
                  <span className={`text-[10px] font-extrabold uppercase tracking-wider ${isToday ? "text-primary" : "text-slate-400"}`}>{DAY_LABELS[i]}</span>
                  <span className={`text-[13.5px] font-black ${isToday ? "text-primary" : "text-slate-700"}`}>{pad(d.getDate())}</span>
                </div>

                <div className="flex flex-col gap-1">
                  {SHIFTS.map((shift) => {
                    const st = PERIOD_STYLE[shift.period];
                    const isSelected = selected.has(selectionKey(key, shift.id));
                    return (
                      <button
                        type="button"
                        key={shift.id}
                        onClick={() => onToggle(key, shift.id)}
                        className={`rounded-lg px-2 py-1.5 border flex flex-col gap-0.5 text-left transition-all cursor-pointer ${
                          isSelected ? `${st.chipSelected} shadow-sm` : `${st.chip} hover:brightness-95`
                        }`}
                      >
                        <div className="flex items-center gap-1">
                          <span className={`w-1.5 h-1.5 rounded-full ${isSelected ? "bg-white" : st.dot}`} />
                          <span className={`text-[9px] font-black uppercase tracking-wide ${isSelected ? "text-white" : st.text}`}>{shift.period}</span>
                          {isSelected && (
                            <svg className="w-3 h-3 ml-auto text-white" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                            </svg>
                          )}
                        </div>
                        <span className={`text-[11px] font-black font-mono ${isSelected ? "text-white" : "text-slate-800"}`}>{shift.label}</span>
                      </button>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
