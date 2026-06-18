"use client";

import { useState } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";

type LeaveStatus = "pending" | "approved" | "rejected";

interface LeaveRequest {
  id: string;
  type: string;
  from: string;
  to: string;
  days: number;
  reason: string;
  status: LeaveStatus;
  submittedAt: string;
  note?: string;
}

const LEAVE_TYPES = ["Phép năm", "Nghỉ ốm", "Nghỉ việc riêng", "Nghỉ không lương", "Nghỉ thai sản / Nghỉ phụ sản"];

const MOCK_HISTORY: LeaveRequest[] = [
  { id: "L001", type: "Phép năm",       from: "2026-05-01", to: "2026-05-02", days: 2, reason: "Nghỉ lễ Giải phóng miền Nam và ngày Quốc tế Lao động",                       status: "approved", submittedAt: "2026-04-25" },
  { id: "L002", type: "Nghỉ việc riêng", from: "2026-04-15", to: "2026-04-15", days: 1, reason: "Có việc gia đình đột xuất cần giải quyết",                                    status: "approved", submittedAt: "2026-04-14", note: "Đã bố trí bác sĩ trực thay" },
  { id: "L003", type: "Nghỉ ốm",        from: "2026-03-10", to: "2026-03-11", days: 2, reason: "Bị cảm cúm, sốt cao, có giấy xác nhận của bác sĩ điều trị",                   status: "approved", submittedAt: "2026-03-10" },
  { id: "L004", type: "Phép năm",       from: "2026-06-20", to: "2026-06-20", days: 1, reason: "Giải quyết thủ tục hành chính cá nhân",                                        status: "pending",  submittedAt: "2026-06-12" },
];

const STATUS_CFG: Record<LeaveStatus, { label: string; cls: string; icon: string }> = {
  pending:  { label: "Chờ duyệt", cls: "bg-amber-50 text-amber-700 border border-amber-100", icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" },
  approved: { label: "Đã duyệt",  cls: "bg-green-50 text-green-700 border border-green-100", icon: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
  rejected: { label: "Từ chối",   cls: "bg-red-50 text-red-700 border border-red-100",       icon: "M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
};

export default function DentistLeavePage() {
  useRequireDentist();

  const [type, setType]     = useState(LEAVE_TYPES[0]);
  const [fromDate, setFrom] = useState("");
  const [toDate, setTo]     = useState("");
  const [reason, setReason] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [history, setHistory] = useState<LeaveRequest[]>(MOCK_HISTORY);

  const totalDays = fromDate && toDate
    ? Math.max(0, Math.ceil((new Date(toDate).getTime() - new Date(fromDate).getTime()) / 86400000) + 1)
    : 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!fromDate || !toDate || !reason.trim()) return;
    const newReq: LeaveRequest = {
      id: `L${Date.now()}`, type, from: fromDate, to: toDate, days: totalDays,
      reason: reason.trim(), status: "pending", submittedAt: "2026-06-12",
    };
    setHistory([newReq, ...history]);
    setSubmitted(true);
    setFrom(""); setTo(""); setReason("");
    setTimeout(() => setSubmitted(false), 4000);
  };

  const fmtDate = (s: string) => {
    const [y, m, d] = s.split("-");
    return `${d}/${m}/${y}`;
  };

  const pendingCount = history.filter((h) => h.status === "pending").length;
  const approvedDays = history.filter((h) => h.status === "approved").reduce((sum, h) => sum + h.days, 0);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="leave" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader title="Đơn Xin Nghỉ" subtitle="Tạo và theo dõi đơn xin nghỉ phép" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* STATS */}
          <div className="grid grid-cols-3 gap-5 shrink-0">
            {[
              { label: "Phép năm còn lại", value: "10", sub: "/ 12 ngày năm 2026", color: "text-primary", bg: "bg-red-50" },
              { label: "Đã nghỉ năm nay",  value: `${approvedDays}`, sub: "ngày đã được duyệt", color: "text-green-700", bg: "bg-green-50" },
              { label: "Đang chờ duyệt",   value: `${pendingCount}`, sub: "đơn cần xử lý", color: "text-amber-700", bg: "bg-amber-50" },
            ].map((s) => (
              <div key={s.label} className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between">
                <div>
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">{s.label}</span>
                  <span className={`text-3xl font-black ${s.color} block mt-1`}>{s.value}</span>
                  <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">{s.sub}</span>
                </div>
                <div className={`w-12 h-12 rounded-xl ${s.bg} flex items-center justify-center shrink-0`}>
                  <svg className={`w-6 h-6 ${s.color}`} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                  </svg>
                </div>
              </div>
            ))}
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-5 gap-5">

            {/* FORM */}
            <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-5">
              <h2 className="text-[15px] font-black text-slate-900">Tạo đơn xin nghỉ mới</h2>

              {submitted && (
                <div className="flex items-center gap-3 bg-green-50 border border-green-100 text-green-700 px-4 py-3 rounded-xl text-[13px] font-bold">
                  <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  Đơn đã gửi thành công. Chờ quản lý duyệt.
                </div>
              )}

              <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                {/* Leave type */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Loại nghỉ phép</label>
                  <div className="relative">
                    <select value={type} onChange={(e) => setType(e.target.value)}
                      className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8">
                      {LEAVE_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                    </select>
                    <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                    </span>
                  </div>
                </div>

                {/* Date range */}
                <div className="grid grid-cols-2 gap-3">
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Từ ngày</label>
                    <input type="date" value={fromDate} onChange={(e) => setFrom(e.target.value)} required
                      className="px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700" />
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Đến ngày</label>
                    <input type="date" value={toDate} min={fromDate} onChange={(e) => setTo(e.target.value)} required
                      className="px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700" />
                  </div>
                </div>

                {totalDays > 0 && (
                  <div className="px-3 py-2 bg-sky-50 border border-sky-100 rounded-xl text-[13px] font-bold text-sky-700">
                    Tổng: {totalDays} ngày nghỉ
                  </div>
                )}

                {/* Reason */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Lý do</label>
                  <textarea value={reason} onChange={(e) => setReason(e.target.value)} required rows={4}
                    placeholder="Nêu rõ lý do xin nghỉ..."
                    className="px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none" />
                </div>

                <button type="submit"
                  className="flex items-center justify-center gap-2 w-full py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 transition-all cursor-pointer shadow-sm shadow-primary/25">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" /></svg>
                  Gửi đơn xin nghỉ
                </button>
              </form>
            </div>

            {/* HISTORY */}
            <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200/60 shadow-sm flex flex-col overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100">
                <h2 className="text-[15px] font-black text-slate-900">Lịch sử đơn xin nghỉ</h2>
              </div>
              <ul className="flex-1 divide-y divide-slate-100 overflow-y-auto">
                {history.map((req) => {
                  const s = STATUS_CFG[req.status];
                  return (
                    <li key={req.id} className="px-6 py-4 flex flex-col gap-2 hover:bg-slate-50/50 transition-colors">
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex flex-col gap-0.5">
                          <span className="text-[14px] font-black text-slate-900">{req.type}</span>
                          <span className="text-[12.5px] font-semibold text-slate-500">
                            {fmtDate(req.from)}{req.from !== req.to ? ` → ${fmtDate(req.to)}` : ""} · <span className="font-bold text-slate-700">{req.days} ngày</span>
                          </span>
                        </div>
                        <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black whitespace-nowrap shrink-0 ${s.cls}`}>
                          <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={s.icon} /></svg>
                          {s.label}
                        </span>
                      </div>
                      <p className="text-[12.5px] text-slate-500 font-medium leading-relaxed">{req.reason}</p>
                      {req.note && (
                        <div className="flex items-start gap-1.5 text-[12px] text-sky-700 font-semibold bg-sky-50 px-3 py-1.5 rounded-lg">
                          <svg className="w-3.5 h-3.5 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                          {req.note}
                        </div>
                      )}
                      <span className="text-[11.5px] text-slate-400 font-semibold">Nộp ngày {fmtDate(req.submittedAt)}</span>
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
