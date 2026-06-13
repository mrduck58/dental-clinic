"use client";

import { useState } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";

const ALL_NOTIFICATIONS = [
  { id: 1,  type: "appointment", title: "Bệnh nhân mới đặt lịch",      body: "Nguyễn Văn An đặt lịch nhổ răng khôn vào 14:00 hôm nay · Phòng 1",                          time: "5 phút trước",  date: "13/06/2026", read: false },
  { id: 2,  type: "reminder",    title: "Nhắc nhở tái khám",           body: "Trần Thị Bích cần tái khám sau trám răng số 6, hạn 15/06/2026",                              time: "30 phút trước", date: "13/06/2026", read: false },
  { id: 3,  type: "system",      title: "Lịch làm việc tuần tới",      body: "Ca sáng Thứ Hai 16/06 đã được phân công · Phòng 2 · 07:30–12:00",                            time: "2 giờ trước",   date: "13/06/2026", read: false },
  { id: 4,  type: "appointment", title: "Bệnh nhân huỷ lịch",          body: "Phạm Minh Cường huỷ lịch kiểm tra định kỳ 09:00 ngày mai · Lý do: bận công việc",           time: "3 giờ trước",   date: "13/06/2026", read: true  },
  { id: 5,  type: "reminder",    title: "Hoàn thiện hồ sơ điều trị",   body: "Cần hoàn thiện hồ sơ điều trị cho Lê Thu Hà và Hoàng Văn Đức trước 17:00 hôm nay",          time: "4 giờ trước",   date: "13/06/2026", read: true  },
  { id: 6,  type: "system",      title: "Cập nhật phần mềm",           body: "Hệ thống sẽ bảo trì từ 22:00–23:00 tối nay · Vui lòng hoàn thành công việc trước thời gian này", time: "Hôm qua",    date: "12/06/2026", read: true  },
  { id: 7,  type: "appointment", title: "Bệnh nhân đến sớm",           body: "Nguyễn Thị Mai đã check-in lúc 07:15, sớm hơn lịch hẹn 15 phút",                            time: "Hôm qua",       date: "12/06/2026", read: true  },
  { id: 8,  type: "reminder",    title: "Kết quả xét nghiệm",          body: "Kết quả X-quang bệnh nhân Trần Văn Bình đã sẵn sàng · Vui lòng xem xét trước buổi khám",    time: "2 ngày trước",  date: "11/06/2026", read: true  },
];

const FILTERS = [
  { key: "all",         label: "Tất cả"      },
  { key: "unread",      label: "Chưa đọc"    },
  { key: "appointment", label: "Lịch hẹn"    },
  { key: "reminder",    label: "Nhắc nhở"    },
  { key: "system",      label: "Hệ thống"    },
];

const TYPE_CFG: Record<string, { bg: string; color: string; badge: string; path: string }> = {
  appointment: {
    bg: "bg-sky-50", color: "text-sky-600", badge: "bg-sky-50 text-sky-700 border-sky-100",
    path: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5",
  },
  reminder: {
    bg: "bg-amber-50", color: "text-amber-600", badge: "bg-amber-50 text-amber-700 border-amber-100",
    path: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z",
  },
  system: {
    bg: "bg-violet-50", color: "text-violet-600", badge: "bg-violet-50 text-violet-700 border-violet-100",
    path: "M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 011.37.49l1.296 2.247a1.125 1.125 0 01-.26 1.431l-1.003.827c-.293.24-.438.613-.431.992a6.759 6.759 0 010 .255c-.007.378.138.75.43.99l1.005.828c.424.35.534.954.26 1.43l-1.298 2.247a1.125 1.125 0 01-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.57 6.57 0 01-.22.128c-.331.183-.581.495-.644.869l-.213 1.28c-.09.543-.56.941-1.11.941h-2.594c-.55 0-1.02-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 01-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 01-1.369-.49l-1.297-2.247a1.125 1.125 0 01.26-1.431l1.004-.827c.292-.24.437-.613.43-.992a6.932 6.932 0 010-.255c.007-.378-.138-.75-.43-.99l-1.004-.828a1.125 1.125 0 01-.26-1.43l1.297-2.247a1.125 1.125 0 011.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.087.22-.128.332-.183.582-.495.644-.869l.214-1.281z M15 12a3 3 0 11-6 0 3 3 0 016 0z",
  },
};

const TYPE_LABEL: Record<string, string> = { appointment: "Lịch hẹn", reminder: "Nhắc nhở", system: "Hệ thống" };

export default function NotificationsPage() {
  useRequireDentist();

  const [notes,  setNotes]  = useState(ALL_NOTIFICATIONS);
  const [filter, setFilter] = useState("all");

  const unread  = notes.filter(n => !n.read).length;
  const markAll = () => setNotes(p => p.map(n => ({ ...n, read: true })));
  const markOne = (id: number) => setNotes(p => p.map(n => n.id === id ? { ...n, read: true } : n));
  const remove  = (id: number) => setNotes(p => p.filter(n => n.id !== id));

  const visible = notes.filter(n => {
    if (filter === "unread")      return !n.read;
    if (filter === "appointment") return n.type === "appointment";
    if (filter === "reminder")    return n.type === "reminder";
    if (filter === "system")      return n.type === "system";
    return true;
  });

  const grouped = visible.reduce<Record<string, typeof visible>>((acc, n) => {
    (acc[n.date] ??= []).push(n); return acc;
  }, {});

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="" />
      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader title="Thông báo" subtitle="Tất cả thông báo của bạn" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">

          {/* Toolbar */}
          <div className="flex items-center justify-between flex-wrap gap-3">
            <div className="flex items-center gap-2 flex-wrap">
              {FILTERS.map(f => (
                <button key={f.key} onClick={() => setFilter(f.key)}
                  className={`px-3.5 py-1.5 text-[13px] font-bold rounded-xl border transition-all cursor-pointer ${
                    filter === f.key
                      ? "bg-primary text-white border-primary shadow-sm shadow-primary/20"
                      : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary hover:bg-red-50"
                  }`}>
                  {f.label}
                  {f.key === "unread" && unread > 0 && (
                    <span className={`ml-1.5 px-1.5 py-0.5 rounded-full text-[10px] font-black ${filter === "unread" ? "bg-white/25 text-white" : "bg-primary/10 text-primary"}`}>{unread}</span>
                  )}
                </button>
              ))}
            </div>
            {unread > 0 && (
              <button onClick={markAll}
                className="flex items-center gap-1.5 px-3.5 py-1.5 text-[13px] font-bold text-slate-500 bg-white border border-slate-200 rounded-xl hover:border-primary/40 hover:text-primary hover:bg-red-50 transition-all cursor-pointer">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                Đánh dấu đọc tất cả
              </button>
            )}
          </div>

          {/* List */}
          {visible.length === 0 ? (
            <div className="flex-1 flex flex-col items-center justify-center gap-3 py-24 text-center">
              <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                <svg className="w-7 h-7 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" /></svg>
              </div>
              <p className="text-[14px] font-bold text-slate-400">Không có thông báo nào</p>
            </div>
          ) : (
            <div className="flex flex-col gap-6">
              {Object.entries(grouped).map(([date, items]) => (
                <div key={date} className="flex flex-col gap-2">
                  <div className="flex items-center gap-3">
                    <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider">{date}</span>
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>

                  <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden divide-y divide-slate-100">
                    {items.map(n => {
                      const cfg = TYPE_CFG[n.type] ?? TYPE_CFG.system;
                      return (
                        <div key={n.id}
                          className={`flex items-start gap-4 px-5 py-4 group transition-colors ${n.read ? "hover:bg-slate-50/60" : "bg-red-50/30 hover:bg-red-50/50"}`}>
                          <div className={`w-9 h-9 rounded-xl ${cfg.bg} ${cfg.color} flex items-center justify-center shrink-0 mt-0.5`}>
                            <svg className="w-4.5 h-4.5" style={{width:18,height:18}} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d={cfg.path} />
                            </svg>
                          </div>

                          <div className="flex-1 min-w-0">
                            <div className="flex items-start gap-2 flex-wrap">
                              <span className={`text-[13.5px] font-black leading-snug ${n.read ? "text-slate-700" : "text-slate-900"}`}>{n.title}</span>
                              <span className={`px-2 py-0.5 text-[10.5px] font-black rounded-full border ${cfg.badge} shrink-0`}>{TYPE_LABEL[n.type]}</span>
                              {!n.read && <span className="w-2 h-2 rounded-full bg-primary shrink-0 mt-1" />}
                            </div>
                            <p className="text-[13px] text-slate-500 font-medium mt-1 leading-relaxed">{n.body}</p>
                            <span className="text-[11.5px] text-slate-400 font-semibold mt-1.5 block">{n.time}</span>
                          </div>

                          <div className="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
                            {!n.read && (
                              <button onClick={() => markOne(n.id)} title="Đánh dấu đã đọc"
                                className="p-1.5 rounded-lg hover:bg-green-50 text-slate-300 hover:text-green-600 transition-all cursor-pointer">
                                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                              </button>
                            )}
                            <button onClick={() => remove(n.id)} title="Xoá thông báo"
                              className="p-1.5 rounded-lg hover:bg-red-50 text-slate-300 hover:text-primary transition-all cursor-pointer">
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
