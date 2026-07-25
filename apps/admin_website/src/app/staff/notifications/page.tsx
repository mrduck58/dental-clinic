"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import Link from "next/link";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getNotificationsApi,
  markNotificationReadApi,
  markAllNotificationsReadApi,
  deleteNotificationApi,
  type NotificationDto,
} from "../../../lib/apiClient";

const FILTERS = [
  { key: "all",         label: "Tất cả"        },
  { key: "unread",      label: "Chưa đọc"      },
  { key: "appointment", label: "Đặt lịch"      },
  { key: "invoice",     label: "Hóa đơn"       },
  { key: "schedule",    label: "Lịch làm việc" },
  { key: "reminder",    label: "Nhắc nhở"      },
  { key: "system",      label: "Hệ thống"      },
];

function getNotifHref(relatedEntityType: string | null, relatedEntityId: string | null): string | null {
  if (!relatedEntityType || !relatedEntityId) return null;
  if (relatedEntityType === "Appointment") return `/staff/appointments`;
  if (relatedEntityType === "Invoice") return `/staff/invoices`;
  return null;
}

const TYPE_CFG: Record<string, { bg: string; color: string; badge: string; path: string }> = {
  appointment: {
    bg: "bg-sky-50", color: "text-sky-600", badge: "bg-sky-50 text-sky-700 border-sky-100",
    path: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5",
  },
  schedule: {
    bg: "bg-amber-50", color: "text-amber-600", badge: "bg-amber-50 text-amber-700 border-amber-100",
    path: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5",
  },
  reminder: {
    bg: "bg-violet-50", color: "text-violet-600", badge: "bg-violet-50 text-violet-700 border-violet-100",
    path: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z",
  },
  system: {
    bg: "bg-slate-100", color: "text-slate-600", badge: "bg-slate-100 text-slate-600 border-slate-200",
    path: "M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z",
  },
  account: {
    bg: "bg-red-50", color: "text-red-600", badge: "bg-red-50 text-red-700 border-red-100",
    path: "M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z",
  },
  invoice: {
    bg: "bg-emerald-50", color: "text-emerald-600", badge: "bg-emerald-50 text-emerald-700 border-emerald-100",
    path: "M2.25 18.75a60.07 60.07 0 0115.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 013 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 00-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 01-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 003 15h-.75M15 10.5a3 3 0 11-6 0 3 3 0 016 0zm3 0h.008v.008H18V10.5zm-12 0h.008v.008H6V10.5z",
  },
};

const FALLBACK_CFG = TYPE_CFG.system;

const TYPE_LABEL: Record<string, string> = {
  schedule: "Lịch làm việc",
  reminder: "Nhắc nhở",
  system:   "Hệ thống",
  account:  "Tài khoản",
  service:  "Dịch vụ",
  security: "Bảo mật",
  appointment: "Lịch hẹn",
  invoice: "Hóa đơn",
};

function timeAgo(ts: string): string {
  const diff = Math.floor((Date.now() - new Date(ts).getTime()) / 1000);
  if (diff < 60) return "Vừa xong";
  if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
  if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
  if (diff < 172800) return "Hôm qua";
  return `${Math.floor(diff / 86400)} ngày trước`;
}

function toDateLabel(ts: string): string {
  return new Date(ts).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

export default function NotificationsPage() {
  useRequireStaff();

  const [notes, setNotes]       = useState<NotificationDto[]>([]);
  const [unreadCount, setUnread] = useState(0);
  const [loading, setLoading]   = useState(false);
  const [filter, setFilter]     = useState("all");

  const fetchRef = useRef<() => Promise<void>>(async () => {});

  const loadNotes = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getNotificationsApi({
        type:   filter !== "all" && filter !== "unread" ? filter.charAt(0).toUpperCase() + filter.slice(1) : undefined,
        isRead: filter === "unread" ? false : undefined,
        pageSize: 50,
        page: 1,
      });
      setNotes(res.items);
      setUnread(res.unreadCount);
    } catch { /* ignore */ }
    finally { setLoading(false); }
  }, [filter]);

  useEffect(() => { fetchRef.current = loadNotes; }, [loadNotes]);
  useEffect(() => { fetchRef.current(); }, [loadNotes]);

  const markAll = async () => {
    try {
      await markAllNotificationsReadApi();
      fetchRef.current();
    } catch { /* ignore */ }
  };

  const markOne = async (id: string) => {
    try {
      await markNotificationReadApi(id);
      setNotes(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
      setUnread(c => Math.max(0, c - 1));
    } catch { /* ignore */ }
  };

  const remove = async (id: string) => {
    setNotes(prev => prev.filter(n => n.id !== id));
    try {
      await deleteNotificationApi(id);
    } catch {
      fetchRef.current();
    }
  };

  const grouped = notes.reduce<Record<string, NotificationDto[]>>((acc, n) => {
    const date = toDateLabel(n.createdAt);
    (acc[date] ??= []).push(n);
    return acc;
  }, {});

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="notifications" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader title="Thông báo" subtitle="Tất cả thông báo của bạn" />

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
                  {f.key === "unread" && unreadCount > 0 && (
                    <span className={`ml-1.5 px-1.5 py-0.5 rounded-full text-[10px] font-black ${filter === "unread" ? "bg-white/25 text-white" : "bg-primary/10 text-primary"}`}>{unreadCount}</span>
                  )}
                </button>
              ))}
            </div>
            {unreadCount > 0 && (
              <button onClick={markAll}
                className="flex items-center gap-1.5 px-3.5 py-1.5 text-[13px] font-bold text-slate-500 bg-white border border-slate-200 rounded-xl hover:border-primary/40 hover:text-primary hover:bg-red-50 transition-all cursor-pointer">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                Đánh dấu đọc tất cả
              </button>
            )}
          </div>

          {/* List */}
          {loading ? (
            <div className="flex items-center justify-center py-24">
              <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
            </div>
          ) : notes.length === 0 ? (
            <div className="flex-1 flex flex-col items-center justify-center gap-3 py-24 text-center">
              <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                <svg className="w-7 h-7 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                </svg>
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
                      const typeKey = n.type.toLowerCase();
                      const cfg = TYPE_CFG[typeKey] ?? FALLBACK_CFG;
                      const label = TYPE_LABEL[typeKey] ?? n.type;
                      const href = getNotifHref(n.relatedEntityType, n.relatedEntityId);
                      const innerContent = (
                        <>
                          <div className={`w-9 h-9 rounded-xl ${cfg.bg} ${cfg.color} flex items-center justify-center shrink-0 mt-0.5`}>
                            <svg style={{width:18,height:18}} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d={cfg.path} />
                            </svg>
                          </div>
                          <div className="flex-1 min-w-0">
                            <div className="flex items-start gap-2 flex-wrap">
                              <span className={`text-[13.5px] font-black leading-snug ${n.isRead ? "text-slate-700" : "text-slate-900"}`}>{n.title}</span>
                              <span className={`px-2 py-0.5 text-[10.5px] font-black rounded-full border ${cfg.badge} shrink-0`}>{label}</span>
                              {!n.isRead && <span className="w-2 h-2 rounded-full bg-primary shrink-0 mt-1" />}
                            </div>
                            <p className="text-[13px] text-slate-500 font-medium mt-1 leading-relaxed">{n.body}</p>
                            <div className="flex items-center gap-2 mt-1.5">
                              <span className="text-[11.5px] text-slate-400 font-semibold">{timeAgo(n.createdAt)}</span>
                              {href && <span className="text-[11.5px] font-bold text-primary">· Xem chi tiết →</span>}
                            </div>
                          </div>
                        </>
                      );
                      return (
                        <div key={n.id}
                          className={`flex items-start gap-4 px-5 py-4 group transition-colors ${n.isRead ? "hover:bg-slate-50/60" : "bg-red-50/30 hover:bg-red-50/50"}`}>
                          {href ? (
                            <Link href={href} onClick={() => { if (!n.isRead) markOne(n.id); }}
                              className="flex items-start gap-4 flex-1 min-w-0 cursor-pointer">
                              {innerContent}
                            </Link>
                          ) : (
                            <div className="flex items-start gap-4 flex-1 min-w-0">
                              {innerContent}
                            </div>
                          )}

                          <div className="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
                            {!n.isRead && (
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
