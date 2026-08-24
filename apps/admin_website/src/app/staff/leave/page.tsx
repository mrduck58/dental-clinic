"use client";

import { useState, useEffect } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import StaffLeaveShiftPicker from "../../../components/shared/StaffLeaveShiftPicker";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getMyLeaveRequestsApi,
  createLeaveRequestApi,
  type LeaveRequestDto,
  type MyLeaveStatsDto,
} from "../../../lib/apiClient";
import { shiftLabel } from "../../../lib/shifts";

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

const selectionKey = (date: string, shiftId: string) => `${date}__${shiftId}`;
const parseSelectionKey = (key: string): { date: string; shiftId: string } => {
  const [date, shiftId] = key.split("__");
  return { date, shiftId };
};

export default function StaffLeavePage() {
  useRequireStaff();

  const [activeTab, setActiveTab] = useState<"create" | "history">("create");
  const [type, setType]     = useState(LEAVE_TYPES[0].value);
  const [selected, setSelected] = useState<Set<string>>(new Set());
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

  const toggleShift = (date: string, shiftId: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      const key = selectionKey(date, shiftId);
      if (next.has(key)) next.delete(key); else next.add(key);
      return next;
    });
  };

  const selectedDayCount = new Set(Array.from(selected).map((k) => parseSelectionKey(k).date)).size;

  const showError = (msg: string) => {
    setError(msg);
    setTimeout(() => setError(""), 4000);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selected.size === 0) return showError("Vui lòng chọn ít nhất một ca muốn nghỉ.");
    if (!reason.trim()) return showError("Vui lòng nhập lý do xin nghỉ.");

    setIsSubmitting(true);
    try {
      await createLeaveRequestApi({
        leaveType: type,
        shifts: Array.from(selected).map(parseSelectionKey),
        reason: reason.trim(),
      });
      setError("");
      setSubmitted(true);
      setSelected(new Set()); setReason("");
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

        <div className="p-4 sm:p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 sm:gap-5 shrink-0">
            {[
              { label: "Phép năm còn lại", value: `${stats.remainingAnnualDays}`, sub: `/ ${stats.totalAnnualDays} ngày năm ${new Date().getFullYear()}`, color: "text-primary", bg: "bg-red-50" },
              { label: "Đã nghỉ năm nay",  value: `${stats.usedAnnualDays}`,      sub: "ngày đã được duyệt", color: "text-green-700", bg: "bg-green-50" },
              { label: "Đang chờ duyệt",   value: `${stats.pendingCount}`,         sub: "đơn cần xử lý",     color: "text-amber-700", bg: "bg-amber-50" },
            ].map((s) => (
              <div key={s.label} className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between">
                <div>
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">{s.label}</span>
                  <span className={`text-2xl sm:text-3xl font-black ${s.color} block mt-1`}>{s.value}</span>
                  <span className="text-[11.5px] sm:text-[12px] text-slate-400 font-semibold block mt-0.5">{s.sub}</span>
                </div>
                <div className={`w-11 h-11 sm:w-12 sm:h-12 rounded-xl ${s.bg} flex items-center justify-center shrink-0`}>
                  <svg className={`w-5 h-5 sm:w-6 sm:h-6 ${s.color}`} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                  </svg>
                </div>
              </div>
            ))}
          </div>

          {/* TABS */}
          <div className="flex gap-1.5 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-1.5 w-fit shrink-0">
            <button type="button" onClick={() => setActiveTab("create")}
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-[13px] font-bold transition-all cursor-pointer ${
                activeTab === "create" ? "bg-primary text-white shadow-sm shadow-primary/25" : "text-slate-500 hover:bg-slate-50"
              }`}>
              Tạo đơn xin nghỉ
            </button>
            <button type="button" onClick={() => setActiveTab("history")}
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-[13px] font-bold transition-all cursor-pointer ${
                activeTab === "history" ? "bg-primary text-white shadow-sm shadow-primary/25" : "text-slate-500 hover:bg-slate-50"
              }`}>
              Lịch sử đơn xin nghỉ
              {history.length > 0 && (
                <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black ${activeTab === "history" ? "bg-white/20 text-white" : "bg-slate-100 text-slate-500"}`}>
                  {history.length}
                </span>
              )}
            </button>
          </div>

          {/* FORM — full width để lịch tuần đủ chỗ hiển thị */}
          {activeTab === "create" && (
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-4 sm:p-6 flex flex-col gap-5">
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

            <form onSubmit={handleSubmit} className="flex flex-col gap-5">
              {/* Leave type — pill group */}
              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Loại nghỉ phép</label>
                <div className="flex flex-wrap gap-2">
                  {LEAVE_TYPES.map((t) => (
                    <button key={t.value} type="button" onClick={() => setType(t.value)}
                      className={`px-3.5 py-2 rounded-xl text-[12.5px] font-bold border transition-all cursor-pointer ${
                        type === t.value
                          ? "bg-primary text-white border-primary shadow-sm shadow-primary/25"
                          : "bg-slate-50 text-slate-600 border-slate-200 hover:border-primary/40 hover:text-primary"
                      }`}>
                      {t.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Shift picker */}
              <div className="flex flex-col gap-1.5">
                <div className="flex items-center justify-between flex-wrap gap-2">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Chọn ca muốn nghỉ</label>
                  {selected.size > 0 && (
                    <span className="px-2.5 py-1 bg-sky-50 border border-sky-100 rounded-lg text-[12px] font-bold text-sky-700">
                      Đã chọn {selected.size} ca · {selectedDayCount} ngày
                    </span>
                  )}
                </div>
                <StaffLeaveShiftPicker selected={selected} onToggle={toggleShift} />
              </div>

              {/* Reason */}
              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Lý do</label>
                <textarea value={reason} onChange={(e) => setReason(e.target.value)} required rows={3}
                  placeholder="Nêu rõ lý do xin nghỉ..."
                  className="px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none" />
              </div>

              <button type="submit" disabled={isSubmitting}
                className="flex items-center justify-center gap-2 w-full sm:w-auto sm:self-end px-8 py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 transition-all cursor-pointer shadow-sm shadow-primary/25 disabled:opacity-60 disabled:cursor-not-allowed">
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
          )}

          {/* HISTORY — lưới thẻ full width */}
          {activeTab === "history" && (
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm flex flex-col overflow-hidden">
            {isLoading ? (
              <div className="flex items-center justify-center py-12 text-slate-400 text-[13px] font-semibold gap-2">
                <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/></svg>
                Đang tải...
              </div>
            ) : history.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 gap-2 text-slate-400">
                <svg className="w-10 h-10" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" /></svg>
                <span className="text-[13px] font-semibold">Chưa có đơn xin nghỉ nào</span>
              </div>
            ) : (
              <div className="p-5 flex flex-col gap-4">
                {history.map((req) => {
                  const s = STATUS_CFG[req.status] ?? STATUS_CFG["Pending"];
                  return (
                    <div key={req.id} className="border border-slate-200/70 rounded-xl p-4 flex flex-col gap-2 hover:border-slate-300 hover:shadow-sm transition-all">
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
                      {req.shifts?.length > 0 && (
                        <div className="flex flex-wrap gap-1.5">
                          {req.shifts.map((sh) => (
                            <span key={`${sh.date}-${sh.shiftId}`} className="px-2 py-0.5 bg-slate-100 text-slate-600 text-[11px] font-bold rounded-md font-mono">
                              {fmtDate(sh.date)} {shiftLabel(sh.shiftId)}
                            </span>
                          ))}
                        </div>
                      )}
                      <p className="text-[12.5px] text-slate-500 font-medium leading-relaxed">{req.reason}</p>
                      {req.reviewerNote && (
                        <div className="flex items-start gap-1.5 text-[12px] text-sky-700 font-semibold bg-sky-50 px-3 py-1.5 rounded-lg">
                          <svg className="w-3.5 h-3.5 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                          {req.reviewerNote}
                        </div>
                      )}
                      <span className="text-[11.5px] text-slate-400 font-semibold">Nộp ngày {fmtDate(req.createdAt)}</span>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
          )}
        </div>
      </main>
    </div>
  );
}
