"use client";

import { useState, useEffect } from "react";
import NotificationBell from "./NotificationBell";

interface StaffPageHeaderProps {
  title: string;
  subtitle?: string;
  right?: React.ReactNode;
}

const DAYS = ["Chủ Nhật","Thứ Hai","Thứ Ba","Thứ Tư","Thứ Năm","Thứ Sáu","Thứ Bảy"];

export default function StaffPageHeader({ title, subtitle, right }: StaffPageHeaderProps) {
  const [time, setTime] = useState<string | null>(null);
  const [date, setDate] = useState<string | null>(null);

  useEffect(() => {
    const tick = () => {
      const now = new Date();
      setTime(now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false }));
      const d = now.getDate().toString().padStart(2, "0");
      const m = (now.getMonth() + 1).toString().padStart(2, "0");
      setDate(`${DAYS[now.getDay()]} · ${d}/${m}/${now.getFullYear()}`);
    };
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, []);

  return (
    <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-4 sm:px-8 h-16 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
      <div className="flex items-center gap-3 min-w-0">
        {/* Nút mở sidebar trên Mobile/Tablet */}
        <button
          type="button"
          onClick={() => window.dispatchEvent(new CustomEvent("toggle-sidebar"))}
          className="lg:hidden p-2 -ml-2 rounded-xl text-slate-600 hover:text-slate-900 hover:bg-slate-100 transition-all shrink-0"
          aria-label="Mở menu điều hướng"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
          </svg>
        </button>

        <div className="min-w-0">
          <h1 className="text-[18px] font-black text-slate-900 leading-tight truncate">{title}</h1>
          {subtitle && <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5 truncate">{subtitle}</p>}
        </div>
      </div>
      <div className="flex items-center gap-2 sm:gap-2.5 shrink-0">
        {right}
        {date !== null && (
          <div className="hidden md:flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-xl">
            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
            </svg>
            <span className="text-[13px] font-semibold text-slate-700">{date}</span>
          </div>
        )}
        {time !== null && (
          <div className="hidden sm:flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-xl">
            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span className="text-[13px] font-mono font-black text-slate-700 tabular-nums">{time}</span>
          </div>
        )}
        <NotificationBell href="/staff/notifications" />
      </div>
    </header>
  );
}
