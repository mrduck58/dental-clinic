"use client";

import { useState, useEffect, useRef } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getMyLeaveRequestsApi,
  createLeaveRequestApi,
  type LeaveRequestDto,
  type MyLeaveStatsDto,
} from "../../../lib/apiClient";

const LEAVE_TYPES: { value: string; label: string }[] = [
  { value: "Annual",    label: "Phép năm" },
  { value: "Sick",      label: "Nghỉ ốm" },
  { value: "Unpaid",    label: "Nghỉ không lương" },
  { value: "Maternity", label: "Nghỉ thai sản / Nghỉ phụ sản" },
  { value: "Training",  label: "Nghỉ họp / Đào tạo" },
];

const LEAVE_LABEL: Record<string, string> = {
  Annual:    "Phép năm",
  Sick:      "Nghỉ ốm",
  Unpaid:    "Nghỉ không lương",
  Maternity: "Nghỉ thai sản / Nghỉ phụ sản",
  Training:  "Nghỉ họp / Đào tạo",
};

const STATUS_CFG: Record<string, { label: string; cls: string; icon: string }> = {
  Pending:   { label: "Chờ duyệt", cls: "bg-amber-50 text-amber-700 border border-amber-100",  icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" },
  Approved:  { label: "Đã duyệt",  cls: "bg-green-50 text-green-700 border border-green-100",  icon: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
  Rejected:  { label: "Từ chối",   cls: "bg-red-50 text-red-700 border border-red-100",        icon: "M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
  Cancelled: { label: "Đã hủy",    cls: "bg-slate-100 text-slate-500 border border-slate-200", icon: "M6 18L18 6M6 6l12 12" },
};

// ── DD/MM/YYYY custom date input ──────────────────────────────────────────────

function DateInput({ value, onChange, minDate, today }: {
  value: string;    // YYYY-MM-DD or ""
  onChange: (v: string) => void;
  minDate?: string; // YYYY-MM-DD
  today?: string;   // YYYY-MM-DD — dùng để phân biệt lỗi quá khứ vs trước ngày bắt đầu
}) {
  const mmRef = useRef<HTMLInputElement>(null);
  const yyRef = useRef<HTMLInputElement>(null);

  const [day,  setDay]  = useState(value ? value.slice(8, 10) : "");
  const [mon,  setMon]  = useState(value ? value.slice(5, 7)  : "");
  const [year, setYear] = useState(value ? value.slice(0, 4)  : "");
  const [errMsg, setErrMsg] = useState("");

  // Reset display when parent clears value (e.g., form reset)
  useEffect(() => {
    if (!value) { setDay(""); setMon(""); setYear(""); setErrMsg(""); }
  }, [value]);

  const tryEmit = (d: string, m: string, y: string) => {
    if (d.length === 2 && m.length === 2 && y.length === 4) {
      const iso = `${y}-${m}-${d}`;
      const date = new Date(iso);
      const valid = !isNaN(date.getTime())
        && date.getDate()      === +d
        && date.getMonth() + 1 === +m
        && date.getFullYear()  === +y;
      if (valid && minDate && iso < minDate) {
        // Phân biệt: quá khứ (trước hôm nay) hay trước ngày bắt đầu
        const msg = today && iso < today
          ? "Không thể chọn ngày trong quá khứ."
          : "Ngày kết thúc không được sớm hơn ngày bắt đầu.";
        setErrMsg(msg);
        onChange("");
        return;
      }
      setErrMsg("");
      onChange(valid ? iso : "");
    } else {
      setErrMsg("");
      onChange("");
    }
  };

  const base = "bg-transparent outline-none text-center text-[13.5px] font-semibold text-slate-700 placeholder:text-slate-400";

  return (
    <div className="flex flex-col gap-1">
      <div className={`flex items-center px-4 py-2.5 rounded-xl transition-all
        ${errMsg
          ? "bg-red-50 border border-red-300 focus-within:ring-1 focus-within:ring-red-300"
          : "bg-slate-50 border border-slate-200 focus-within:bg-white focus-within:border-primary focus-within:ring-1 focus-within:ring-primary"
        }`}>
        <input type="text" inputMode="numeric" value={day} placeholder="DD" maxLength={2}
          onChange={(e) => {
            const v = e.target.value.replace(/\D/g, "").slice(0, 2);
            setDay(v);
            if (v.length === 2) mmRef.current?.focus();
            tryEmit(v, mon, year);
          }}
          className={`w-7 ${base}`}
        />
        <span className="text-slate-400 font-bold mx-0.5 select-none">/</span>
        <input ref={mmRef} type="text" inputMode="numeric" value={mon} placeholder="MM" maxLength={2}
          onChange={(e) => {
            const v = e.target.value.replace(/\D/g, "").slice(0, 2);
            setMon(v);
            if (v.length === 2) yyRef.current?.focus();
            tryEmit(day, v, year);
          }}
          className={`w-7 ${base}`}
        />
        <span className="text-slate-400 font-bold mx-0.5 select-none">/</span>
        <input ref={yyRef} type="text" inputMode="numeric" value={year} placeholder="YYYY" maxLength={4}
          onChange={(e) => {
            const v = e.target.value.replace(/\D/g, "").slice(0, 4);
            setYear(v);
            tryEmit(day, mon, v);
          }}
          className={`w-12 ${base}`}
        />
      </div>
      {errMsg && (
        <span className="text-[11.5px] text-red-500 font-semibold flex items-center gap-1">
          <svg className="w-3 h-3 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
          </svg>
          {errMsg}
        </span>
      )}
    </div>
  );
}

export default function StaffLeavePage() {
  useRequireStaff();

  const [type, setType]     = useState(LEAVE_TYPES[0].value);
  const [fromDate, setFrom] = useState("");
  const [toDate, setTo]     = useState("");
  const [reason, setReason] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [history, setHistory] = useState<LeaveRequestDto[]>([]);
  const [stats, setStats] = useState<MyLeaveStatsDto>({
    totalAnnualDays: 12, usedAnnualDays: 0, remainingAnnualDays: 12,
    pendingCount: 0, approvedThisYear: 0,
  });
  const [isLoading, setIsLoading] = useState(true);

  const today = new Date().toISOString().split("T")[0];

  const loadData = async () => {
    try {
      const data = await getMyLeaveRequestsApi();
      setHistory(data.requests);
      setStats(data.stats);
    } catch {
      // tải thất bại — giữ list rỗng, không crash
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { loadData(); }, []);

  const handleFromChange = (val: string) => {
    setFrom(val);
    if (toDate && toDate < val) setTo("");
  };

  const totalDays = fromDate && toDate
    ? Math.max(0, Math.ceil((new Date(toDate).getTime() - new Date(fromDate).getTime()) / 86400000) + 1)
    : 0;

  const showError = (msg: string) => {
    setError(msg);
    setTimeout(() => setError(""), 4000);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fromDate) return showError("Vui lòng nhập ngày bắt đầu hợp lệ (DD/MM/YYYY).");
    if (!toDate)   return showError("Vui lòng nhập ngày kết thúc hợp lệ (DD/MM/YYYY).");
    if (!reason.trim()) return showError("Vui lòng nhập lý do xin nghỉ.");

    setIsSubmitting(true);
    try {
      await createLeaveRequestApi({
        leaveType: type,
        startDate: fromDate,
        endDate:   toDate,
        reason:    reason.trim(),
      });
      setError("");
      setSubmitted(true);
      setFrom(""); setTo(""); setReason("");
      setTimeout(() => setSubmitted(false), 4000);
      await loadData(); // làm mới danh sách và thống kê từ server
    } catch (err) {
      showError(err instanceof Error ? err.message : "Gửi đơn xin nghỉ thất bại. Vui lòng thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  };

  // "YYYY-MM-DD" hoặc ISO datetime → "DD/MM/YYYY"
  const fmtDate = (s: string) => {
    const date = s.includes("T") ? s.split("T")[0] : s;
    const [y, m, d] = date.split("-");
    return `${d}/${m}/${y}`;
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="leave" />

      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader title="Đơn Xin Nghỉ" subtitle="Tạo và theo dõi đơn xin nghỉ phép" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* STATS */}
          <div className="grid grid-cols-3 gap-5 shrink-0">
            {[
              { label: "Phép năm còn lại", value: `${stats.remainingAnnualDays}`, sub: `/ ${stats.totalAnnualDays} ngày năm ${new Date().getFullYear()}`, color: "text-primary", bg: "bg-red-50" },
              { label: "Đã nghỉ năm nay",  value: `${stats.usedAnnualDays}`,      sub: "ngày đã được duyệt", color: "text-green-700", bg: "bg-green-50" },
              { label: "Đang chờ duyệt",   value: `${stats.pendingCount}`,         sub: "đơn cần xử lý",     color: "text-amber-700", bg: "bg-amber-50" },
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

              {error && (
                <div className="flex items-center gap-3 bg-red-50 border border-red-100 text-red-600 px-4 py-3 rounded-xl text-[13px] font-bold">
                  <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                  {error}
                </div>
              )}

              <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                {/* Leave type */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Loại nghỉ phép</label>
                  <div className="relative">
                    <select value={type} onChange={(e) => setType(e.target.value)}
                      className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8">
                      {LEAVE_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
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
                    <DateInput value={fromDate} onChange={handleFromChange} minDate={today} today={today} />
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Đến ngày</label>
                    <DateInput value={toDate} onChange={setTo} minDate={fromDate || today} today={today} />
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

                <button type="submit" disabled={isSubmitting}
                  className="flex items-center justify-center gap-2 w-full py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 transition-all cursor-pointer shadow-sm shadow-primary/25 disabled:opacity-60 disabled:cursor-not-allowed">
                  {isSubmitting ? (
                    <>
                      <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/></svg>
                      Đang gửi...
                    </>
                  ) : (
                    <>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" /></svg>
                      Gửi đơn xin nghỉ
                    </>
                  )}
                </button>
              </form>
            </div>

            {/* HISTORY */}
            <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200/60 shadow-sm flex flex-col overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100">
                <h2 className="text-[15px] font-black text-slate-900">Lịch sử đơn xin nghỉ</h2>
              </div>
              <ul className="flex-1 divide-y divide-slate-100 overflow-y-auto">
                {isLoading ? (
                  <li className="flex items-center justify-center py-12 text-slate-400 text-[13px] font-semibold gap-2">
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/></svg>
                    Đang tải...
                  </li>
                ) : history.length === 0 ? (
                  <li className="flex flex-col items-center justify-center py-12 gap-2 text-slate-400">
                    <svg className="w-10 h-10" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" /></svg>
                    <span className="text-[13px] font-semibold">Chưa có đơn xin nghỉ nào</span>
                  </li>
                ) : history.map((req) => {
                  const s = STATUS_CFG[req.status] ?? STATUS_CFG["Pending"];
                  return (
                    <li key={req.id} className="px-6 py-4 flex flex-col gap-2 hover:bg-slate-50/50 transition-colors">
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex flex-col gap-0.5">
                          <span className="text-[14px] font-black text-slate-900">{LEAVE_LABEL[req.leaveType] ?? req.leaveType}</span>
                          <span className="text-[12.5px] font-semibold text-slate-500">
                            {fmtDate(req.startDate)}{req.startDate !== req.endDate ? ` → ${fmtDate(req.endDate)}` : ""} · <span className="font-bold text-slate-700">{req.daysCount} ngày</span>
                          </span>
                        </div>
                        <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black whitespace-nowrap shrink-0 ${s.cls}`}>
                          <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={s.icon} /></svg>
                          {s.label}
                        </span>
                      </div>
                      <p className="text-[12.5px] text-slate-500 font-medium leading-relaxed">{req.reason}</p>
                      {req.reviewerNote && (
                        <div className="flex items-start gap-1.5 text-[12px] text-sky-700 font-semibold bg-sky-50 px-3 py-1.5 rounded-lg">
                          <svg className="w-3.5 h-3.5 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                          {req.reviewerNote}
                        </div>
                      )}
                      <span className="text-[11.5px] text-slate-400 font-semibold">Nộp ngày {fmtDate(req.createdAt)}</span>
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
