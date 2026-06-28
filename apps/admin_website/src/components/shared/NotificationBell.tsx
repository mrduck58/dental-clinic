"use client";

import { useState, useRef, useEffect } from "react";
import Link from "next/link";

const MOCK_NOTIFICATIONS = [
  { id: 1, type: "appointment", title: "Bệnh nhân mới đặt lịch", body: "Nguyễn Văn An · Nhổ răng khôn · 14:00 hôm nay", time: "5 phút trước", read: false },
  { id: 2, type: "reminder",    title: "Nhắc nhở tái khám",      body: "Trần Thị Bích cần tái khám sau trám răng số 6",  time: "30 phút trước", read: false },
  { id: 3, type: "system",      title: "Lịch làm việc tuần tới", body: "Ca sáng Thứ Hai đã được phân công · Phòng 2",    time: "2 giờ trước",   read: false },
  { id: 4, type: "appointment", title: "Bệnh nhân huỷ lịch",     body: "Phạm Minh Cường huỷ lịch 09:00 ngày mai",       time: "3 giờ trước",   read: true  },
  { id: 5, type: "reminder",    title: "Kiểm tra hồ sơ",         body: "Cần hoàn thiện hồ sơ điều trị cho 2 bệnh nhân",  time: "Hôm qua",       read: true  },
];

const TYPE_ICON: Record<string, { path: string; bg: string; color: string }> = {
  appointment: {
    bg: "bg-sky-50", color: "text-sky-600",
    path: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5",
  },
  reminder: {
    bg: "bg-amber-50", color: "text-amber-600",
    path: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z",
  },
  system: {
    bg: "bg-violet-50", color: "text-violet-600",
    path: "M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 011.37.49l1.296 2.247a1.125 1.125 0 01-.26 1.431l-1.003.827c-.293.24-.438.613-.431.992a6.759 6.759 0 010 .255c-.007.378.138.75.43.99l1.005.828c.424.35.534.954.26 1.43l-1.298 2.247a1.125 1.125 0 01-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.57 6.57 0 01-.22.128c-.331.183-.581.495-.644.869l-.213 1.28c-.09.543-.56.941-1.11.941h-2.594c-.55 0-1.02-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 01-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 01-1.369-.49l-1.297-2.247a1.125 1.125 0 01.26-1.431l1.004-.827c.292-.24.437-.613.43-.992a6.932 6.932 0 010-.255c.007-.378-.138-.75-.43-.99l-1.004-.828a1.125 1.125 0 01-.26-1.43l1.297-2.247a1.125 1.125 0 011.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.087.22-.128.332-.183.582-.495.644-.869l.214-1.281z M15 12a3 3 0 11-6 0 3 3 0 016 0z",
  },
};

interface NotificationBellProps { href?: string }

export default function NotificationBell({ href = "/admin/notifications" }: NotificationBellProps) {
  const [open, setOpen]   = useState(false);
  const [notes, setNotes] = useState(MOCK_NOTIFICATIONS);
  const ref               = useRef<HTMLDivElement>(null);

  const unread = notes.filter(n => !n.read).length;

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const markAll  = () => setNotes(p => p.map(n => ({ ...n, read: true })));
  const markOne  = (id: number) => setNotes(p => p.map(n => n.id === id ? { ...n, read: true } : n));

  return (
    <div ref={ref} className="relative shrink-0">
      <button
        onClick={() => setOpen(o => !o)}
        className="relative p-2.5 rounded-full bg-slate-100 text-slate-600 hover:bg-red-50 hover:text-primary transition-all cursor-pointer"
        aria-label="Thông báo"
      >
        <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
        </svg>
        {unread > 0 && (
          <span className="absolute top-1 right-1 min-w-[16px] h-4 px-0.5 bg-primary rounded-full border-2 border-white flex items-center justify-center text-[9px] font-black text-white leading-none">
            {unread > 9 ? "9+" : unread}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-2 w-80 bg-white rounded-2xl border border-slate-200 shadow-xl shadow-slate-200/60 z-50 overflow-hidden animate-fade-in">
          {/* Header */}
          <div className="px-4 py-3 border-b border-slate-100 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="text-[14px] font-black text-slate-900">Thông báo</span>
              {unread > 0 && (
                <span className="px-2 py-0.5 bg-primary/10 text-primary text-[11px] font-black rounded-full">{unread} mới</span>
              )}
            </div>
            {unread > 0 && (
              <button onClick={markAll} className="text-[12px] font-bold text-slate-400 hover:text-primary transition-colors cursor-pointer">
                Đọc tất cả
              </button>
            )}
          </div>

          {/* List */}
          <div className="max-h-[360px] overflow-y-auto divide-y divide-slate-100">
            {notes.map(n => {
              const ic = TYPE_ICON[n.type] ?? TYPE_ICON.system;
              return (
                <button key={n.id} onClick={() => markOne(n.id)}
                  className={`w-full text-left px-4 py-3.5 flex items-start gap-3 hover:bg-slate-50 transition-colors cursor-pointer ${n.read ? "opacity-60" : ""}`}>
                  <div className={`w-8 h-8 rounded-xl ${ic.bg} ${ic.color} flex items-center justify-center shrink-0 mt-0.5`}>
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d={ic.path} />
                    </svg>
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <span className="text-[13px] font-black text-slate-900 leading-snug">{n.title}</span>
                      {!n.read && <span className="w-2 h-2 rounded-full bg-primary shrink-0 mt-1.5" />}
                    </div>
                    <p className="text-[12px] text-slate-500 font-medium mt-0.5 leading-snug line-clamp-2">{n.body}</p>
                    <span className="text-[11px] text-slate-400 font-semibold mt-1 block">{n.time}</span>
                  </div>
                </button>
              );
            })}
          </div>

          {/* Footer */}
          <div className="px-4 py-2.5 border-t border-slate-100 bg-slate-50/60">
            <Link href={href} onClick={() => setOpen(false)}
              className="flex items-center justify-center gap-1.5 w-full text-[12.5px] font-bold text-primary hover:text-red-600 transition-colors py-0.5 cursor-pointer">
              Xem tất cả thông báo
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" /></svg>
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
