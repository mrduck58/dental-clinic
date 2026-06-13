"use client";

import { useState } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";

type QueueStatus = "waiting" | "inprogress" | "done";

interface QueueItem {
  id: string; name: string; service: string; time: string;
  dentist: string; room: string; waitMin: number; status: QueueStatus; note?: string;
}

const INITIAL_QUEUE: QueueItem[] = [
  { id: "Q001", name: "Nguyễn Văn An",   service: "Nhổ răng khôn",       time: "08:30", dentist: "BS. Thảo", room: "Phòng 1", waitMin: 12, status: "inprogress" },
  { id: "Q002", name: "Trần Thị Bích",   service: "Trám răng số 6",      time: "09:00", dentist: "BS. Minh", room: "Phòng 2", waitMin: 5,  status: "inprogress" },
  { id: "Q003", name: "Phạm Minh Cường", service: "Kiểm tra định kỳ",    time: "09:30", dentist: "BS. Thảo", room: "Phòng 1", waitMin: 28, status: "waiting"    },
  { id: "Q004", name: "Lê Thu Hà",       service: "Tẩy trắng răng Zoom", time: "10:00", dentist: "BS. Linh", room: "Phòng 3", waitMin: 45, status: "waiting"    },
  { id: "Q005", name: "Hoàng Văn Đức",   service: "Cấy ghép Implant",    time: "10:30", dentist: "BS. Minh", room: "Phòng 2", waitMin: 60, status: "waiting",   note: "Dị ứng penicillin" },
  { id: "Q006", name: "Vũ Thị Ngọc",     service: "Bọc răng sứ",         time: "08:00", dentist: "BS. Linh", room: "Phòng 3", waitMin: 0,  status: "done"       },
  { id: "Q007", name: "Đỗ Quang Huy",    service: "Lấy cao răng",        time: "07:30", dentist: "BS. Thảo", room: "Phòng 1", waitMin: 0,  status: "done"       },
];

const COL_CFG: Record<QueueStatus, { label: string; bar: string; badge: string; dot: string; header: string; emptyBorder: string }> = {
  waiting:    { label: "Đang chờ",  bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",   dot: "bg-amber-500",  header: "text-amber-700",  emptyBorder: "border-amber-200"  },
  inprogress: { label: "Đang khám", bar: "bg-violet-400", badge: "bg-violet-50 text-violet-700 border border-violet-200",dot: "bg-violet-500", header: "text-violet-700", emptyBorder: "border-violet-200" },
  done:       { label: "Hoàn thành",bar: "bg-emerald-400",badge: "bg-emerald-50 text-emerald-700 border border-emerald-200",dot: "bg-emerald-500",header: "text-emerald-700",emptyBorder: "border-emerald-200"},
};

const DENTIST_BADGE: Record<string, string> = {
  "BS. Thảo": "bg-sky-50 text-sky-700 border-sky-100",
  "BS. Minh": "bg-violet-50 text-violet-700 border-violet-100",
  "BS. Linh": "bg-rose-50 text-rose-700 border-rose-100",
  "BS. Hùng": "bg-amber-50 text-amber-700 border-amber-100",
};

const NEXT_STATUS: Partial<Record<QueueStatus, QueueStatus>> = { waiting: "inprogress", inprogress: "done" };
const NEXT_LABEL:  Record<QueueStatus, string> = { waiting: "Bắt đầu khám →", inprogress: "Hoàn thành →", done: "" };

export default function QueuePage() {
  useRequireStaff();
  const [queue, setQueue] = useState<QueueItem[]>(INITIAL_QUEUE);

  const advance = (id: string) => setQueue(prev => prev.map(item => {
    if (item.id !== id) return item;
    const next = NEXT_STATUS[item.status];
    return next ? { ...item, status: next, waitMin: 0 } : item;
  }));

  const revert = (id: string) => setQueue(prev => prev.map(item => {
    if (item.id !== id) return item;
    const prev2: QueueStatus = item.status === "inprogress" ? "waiting" : item.status === "done" ? "inprogress" : "waiting";
    return { ...item, status: prev2 };
  }));

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="queue" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader
          title="Hàng Đợi"
          subtitle="Theo dõi tiến trình khám bệnh theo thời gian thực"
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">{queue.filter(q => q.status === "waiting").length} chờ</span>
              <span className="px-2.5 py-1.5 bg-violet-50 text-violet-700 border border-violet-200 rounded-xl">{queue.filter(q => q.status === "inprogress").length} đang khám</span>
              <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">{queue.filter(q => q.status === "done").length} xong</span>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto">
          {/* Progress bar */}
          <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm px-6 py-4 flex items-center gap-5 mb-5">
            <div className="flex flex-col gap-1 shrink-0">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-widest">Tiến độ hôm nay</span>
              <span className="text-[24px] font-black text-slate-900 leading-none">
                {queue.filter(q => q.status === "done").length}<span className="text-[14px] text-slate-400 font-bold">/{queue.length}</span>
              </span>
            </div>
            <div className="flex-1 flex flex-col gap-1.5">
              <div className="h-2.5 bg-slate-100 rounded-full overflow-hidden">
                <div className="h-full bg-gradient-to-r from-primary to-red-400 rounded-full transition-all duration-500"
                  style={{ width: `${Math.round(queue.filter(q => q.status === "done").length / queue.length * 100)}%` }} />
              </div>
              <div className="flex items-center gap-4 text-[11.5px] font-semibold">
                <span className="flex items-center gap-1 text-emerald-600"><span className="w-2 h-2 rounded-full bg-emerald-400 inline-block" />{queue.filter(q => q.status === "done").length} hoàn thành</span>
                <span className="flex items-center gap-1 text-violet-600"><span className="w-2 h-2 rounded-full bg-violet-400 animate-pulse inline-block" />{queue.filter(q => q.status === "inprogress").length} đang khám</span>
                <span className="flex items-center gap-1 text-amber-600"><span className="w-2 h-2 rounded-full bg-amber-400 inline-block" />{queue.filter(q => q.status === "waiting").length} đang chờ</span>
              </div>
            </div>
            <div className="text-right shrink-0">
              <span className="text-[28px] font-black text-slate-800 leading-none">{Math.round(queue.filter(q => q.status === "done").length / queue.length * 100)}%</span>
              <div className="text-[11px] font-bold text-slate-400 mt-0.5">hoàn thành</div>
            </div>
          </div>

          {/* Kanban columns */}
          <div className="grid grid-cols-3 gap-5">
            {(["waiting","inprogress","done"] as QueueStatus[]).map(col => {
              const cfg   = COL_CFG[col];
              const items = queue.filter(q => q.status === col);
              return (
                <div key={col} className="flex flex-col gap-3">
                  {/* Column header */}
                  <div className="flex items-center gap-3">
                    <div className="flex items-center gap-2">
                      <span className={`w-2.5 h-2.5 rounded-full ${cfg.bar} ${col === "inprogress" ? "animate-pulse" : ""}`} />
                      <span className={`text-[13px] font-black uppercase tracking-wider ${cfg.header}`}>{cfg.label}</span>
                    </div>
                    <span className={`px-2 py-0.5 rounded-full text-[11px] font-black ${cfg.badge}`}>{items.length}</span>
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>

                  {/* Cards */}
                  {items.length === 0 ? (
                    <div className={`rounded-2xl border-2 border-dashed ${cfg.emptyBorder} p-8 text-center text-[12.5px] font-semibold text-slate-400`}>
                      Không có bệnh nhân
                    </div>
                  ) : (
                    <div className="flex flex-col gap-2.5">
                      {items.map(item => {
                        const initials = item.name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
                        return (
                          <div key={item.id} className={`flex rounded-2xl border bg-white overflow-hidden shadow-sm transition-all ${
                            col === "done" ? "opacity-70 border-slate-200/60" : col === "inprogress" ? "border-violet-200 shadow-violet-100/40" : "border-slate-200/70 hover:shadow-md"
                          }`}>
                            <div className={`w-1.5 shrink-0 ${cfg.bar}`} />
                            <div className="flex flex-col gap-3 px-4 py-4 flex-1">
                              {/* Header row */}
                              <div className="flex items-start justify-between gap-2">
                                <div className="flex items-center gap-3">
                                  <div className="w-9 h-9 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[11px] text-sky-700 shrink-0">
                                    {initials}
                                  </div>
                                  <div>
                                    <div className="text-[14px] font-black text-slate-900 leading-tight">{item.name}</div>
                                    <div className="text-[12px] text-slate-500 font-semibold mt-0.5 truncate">{item.service}</div>
                                  </div>
                                </div>
                                <span className="text-[13px] font-mono font-black text-slate-500 shrink-0">{item.time}</span>
                              </div>

                              {/* Dentist + room */}
                              <div className="flex items-center gap-2 flex-wrap">
                                <span className={`px-2 py-0.5 rounded-lg border text-[11.5px] font-black ${DENTIST_BADGE[item.dentist] ?? "bg-slate-50 text-slate-600 border-slate-100"}`}>
                                  {item.dentist}
                                </span>
                                <span className="px-2 py-0.5 rounded-lg bg-slate-50 text-slate-500 border border-slate-100 text-[11.5px] font-semibold">
                                  {item.room}
                                </span>
                                {col === "waiting" && item.waitMin > 0 && (
                                  <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-lg bg-amber-50 text-amber-600 border border-amber-100 text-[11px] font-black">
                                    ~{item.waitMin} phút
                                  </span>
                                )}
                              </div>

                              {/* Note */}
                              {item.note && (
                                <div className="flex items-start gap-2 px-3 py-2 bg-amber-50 border border-amber-100 rounded-xl">
                                  <svg className="w-3.5 h-3.5 text-amber-600 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                                  <span className="text-[11.5px] font-semibold text-amber-800">{item.note}</span>
                                </div>
                              )}

                              {/* Actions */}
                              {col !== "done" ? (
                                <div className="flex items-center gap-2 pt-1 border-t border-slate-100">
                                  <button onClick={() => advance(item.id)}
                                    className={`flex-1 flex items-center justify-center gap-1.5 py-2 rounded-xl text-[12.5px] font-bold cursor-pointer transition-all ${
                                      col === "waiting"
                                        ? "bg-violet-50 text-violet-700 hover:bg-violet-100 border border-violet-100"
                                        : "bg-emerald-50 text-emerald-700 hover:bg-emerald-100 border border-emerald-100"
                                    }`}>
                                    {NEXT_LABEL[col]}
                                  </button>
                                  <button onClick={() => revert(item.id)}
                                    className="px-3 py-2 rounded-xl text-[12.5px] font-bold text-slate-400 hover:text-slate-600 hover:bg-slate-100 border border-slate-100 cursor-pointer transition-all">
                                    ←
                                  </button>
                                </div>
                              ) : (
                                <div className="flex items-center gap-1.5 text-[12px] font-bold text-emerald-600 pt-1 border-t border-slate-100">
                                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                  Đã hoàn thành
                                </div>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      </main>
    </div>
  );
}
