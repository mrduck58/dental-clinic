"use client";

import { useState } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireOwner } from "../../../hooks/useRequireOwner";

/* ─── constants ─────────────────────────────────────────── */

const DENTISTS = ["BS. Thảo", "BS. Minh", "BS. Linh", "BS. Hùng"];
const ROOMS: Record<string, string> = {
  "BS. Thảo": "Phòng 1", "BS. Minh": "Phòng 2", "BS. Linh": "Phòng 3", "BS. Hùng": "Phòng 4",
};
const SERVICES = [
  "Kiểm tra định kỳ","Nhổ răng khôn","Trám răng composite","Lấy cao răng",
  "Tẩy trắng răng Zoom","Bọc răng sứ","Cấy ghép Implant","Chỉnh nha","Điều trị tủy","Phục hình tháo lắp",
];
const TIMES_MORNING   = ["08:00","08:30","09:00","09:30","10:00","10:30","11:00","11:30"];
const TIMES_AFTERNOON = ["13:00","13:30","14:00","14:30","15:00","15:30","16:00","16:30"];
const ALL_TIMES = [...TIMES_MORNING, ...TIMES_AFTERNOON];

/* Slots already booked today — key: "dentist-time" */
const INIT_OCCUPIED: Record<string, string> = {
  "BS. Thảo-08:30": "Nguyễn Văn An",
  "BS. Thảo-09:30": "Phạm Minh Cường",
  "BS. Minh-09:00":  "Trần Thị Bích",
  "BS. Minh-10:30":  "Hoàng Văn Đức",
  "BS. Linh-10:00":  "Lê Thu Hà",
  "BS. Linh-11:00":  "Vũ Thị Ngọc",
  "BS. Minh-13:30":  "Đỗ Quang Huy",
  "BS. Minh-14:00":  "Nguyễn Thị Mai",
};

/* ─── online requests ────────────────────────────────────── */

type ReqStatus = "pending" | "confirmed" | "rejected";
interface OnlineRequest {
  id: string; name: string; phone: string; service: string;
  reqDate: string; reqTime: string; note: string; createdAt: string;
  status: ReqStatus; rejectReason?: string;
  assignedDentist?: string; confirmedTime?: string;
}

const INIT_REQUESTS: OnlineRequest[] = [
  { id: "REQ001", name: "Nguyễn Thị Kim",  phone: "0989 123 456", service: "Kiểm tra định kỳ", reqDate: "13/06/2026", reqTime: "14:30", note: "",                        createdAt: "10/06/2026", status: "pending" },
  { id: "REQ002", name: "Trần Minh Khoa",  phone: "0976 234 567", service: "Nhổ răng khôn",    reqDate: "14/06/2026", reqTime: "09:00", note: "Đã đau hàm được 3 ngày",  createdAt: "12/06/2026", status: "pending" },
  { id: "REQ003", name: "Lý Thị Hoa",      phone: "0965 345 678", service: "Lấy cao răng",     reqDate: "15/06/2026", reqTime: "10:30", note: "",                        createdAt: "12/06/2026", status: "pending" },
  { id: "REQ004", name: "Bùi Văn Sơn",     phone: "0954 456 789", service: "Bọc răng sứ",      reqDate: "16/06/2026", reqTime: "08:30", note: "Cần tư vấn màu sắc",     createdAt: "13/06/2026", status: "pending" },
  { id: "REQ005", name: "Mai Thị Thanh",   phone: "0943 567 890", service: "Chỉnh nha",        reqDate: "17/06/2026", reqTime: "14:00", note: "",                        createdAt: "13/06/2026", status: "confirmed", assignedDentist: "BS. Minh", confirmedTime: "14:00" },
  { id: "REQ006", name: "Võ Thành Nam",    phone: "0932 678 901", service: "Tẩy trắng răng",   reqDate: "13/06/2026", reqTime: "09:30", note: "Lần đầu",                 createdAt: "11/06/2026", status: "rejected",  rejectReason: "Lịch bác sĩ đã đầy ngày đó" },
];

/* ─── style helpers ──────────────────────────────────────── */
const selectCls = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8";
const inputCls  = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";

const DENTIST_COLOR: Record<string, string> = {
  "BS. Thảo": "bg-sky-50 text-sky-700 border-sky-200",
  "BS. Minh":  "bg-violet-50 text-violet-700 border-violet-200",
  "BS. Linh":  "bg-rose-50 text-rose-700 border-rose-200",
  "BS. Hùng":  "bg-amber-50 text-amber-700 border-amber-200",
};

/* ─── sub-components ─────────────────────────────────────── */

function SectionHeader({ icon, label, count }: { icon: string; label: string; count: number }) {
  return (
    <div className="flex items-center gap-3">
      <div className="flex items-center gap-2">
        <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
        </svg>
        <span className="text-[13px] font-black text-slate-600 uppercase tracking-wider">{label}</span>
      </div>
      <span className="text-[12px] font-bold text-slate-400">{count}</span>
      <div className="flex-1 h-px bg-slate-200" />
    </div>
  );
}

/* ─── Online requests tab ────────────────────────────────── */

function OnlineTab() {
  const [requests, setRequests]         = useState<OnlineRequest[]>(INIT_REQUESTS);
  const [expanding, setExpanding]       = useState<string | null>(null);   // confirm expand
  const [rejectTarget, setRejectTarget] = useState<string | null>(null);   // reject expand
  const [rejectReason, setRejectReason] = useState("");
  const [confirmForm, setConfirmForm]   = useState({ dentist: DENTISTS[0], time: "" });

  const pending   = requests.filter(r => r.status === "pending");
  const processed = requests.filter(r => r.status !== "pending");

  const openConfirm = (id: string, reqTime: string) => {
    setExpanding(id);
    setRejectTarget(null);
    setConfirmForm({ dentist: DENTISTS[0], time: reqTime });
  };
  const openReject = (id: string) => {
    setRejectTarget(id);
    setExpanding(null);
    setRejectReason("");
  };

  const doConfirm = (id: string) => {
    setRequests(prev => prev.map(r => r.id !== id ? r : {
      ...r, status: "confirmed",
      assignedDentist: confirmForm.dentist,
      confirmedTime: confirmForm.time,
    }));
    setExpanding(null);
  };
  const doReject = (id: string) => {
    setRequests(prev => prev.map(r => r.id !== id ? r : { ...r, status: "rejected", rejectReason }));
    setRejectTarget(null);
    setRejectReason("");
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Pending */}
      {pending.length > 0 && (
        <div className="flex flex-col gap-3">
          <SectionHeader
            icon="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"
            label="Chờ xác nhận"
            count={pending.length}
          />
          {pending.map(req => (
            <div key={req.id} className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
              {/* Main row */}
              <div className="flex items-start gap-5 px-6 py-5">
                {/* Avatar */}
                <div className="w-11 h-11 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[13px] text-sky-700 shrink-0">
                  {req.name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                </div>

                {/* Info */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2.5 flex-wrap">
                    <span className="text-[15px] font-black text-slate-900">{req.name}</span>
                    <span className="text-[12px] font-medium text-slate-400 font-mono">{req.phone}</span>
                    <span className="px-2 py-0.5 bg-sky-50 text-sky-700 border border-sky-100 rounded-full text-[11.5px] font-black">{req.service}</span>
                  </div>
                  <div className="flex items-center gap-2 mt-1.5 text-[13px] text-slate-500 font-semibold">
                    <svg className="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                    <span>Yêu cầu: <strong className="text-slate-700">{req.reqDate}</strong> lúc <strong className="text-slate-700">{req.reqTime}</strong></span>
                    <span className="text-slate-300">·</span>
                    <span className="text-slate-400 text-[12px]">Gửi ngày {req.createdAt}</span>
                  </div>
                  {req.note && (
                    <div className="mt-2 flex items-start gap-1.5 text-[12.5px] text-amber-700 bg-amber-50 border border-amber-100 px-3 py-1.5 rounded-lg font-semibold w-fit">
                      <svg className="w-3.5 h-3.5 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                      {req.note}
                    </div>
                  )}
                </div>

                {/* Action buttons */}
                <div className="flex items-center gap-2 shrink-0">
                  <button
                    onClick={() => expanding === req.id ? setExpanding(null) : openConfirm(req.id, req.reqTime)}
                    className={`flex items-center gap-1.5 px-4 py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border ${
                      expanding === req.id
                        ? "bg-primary text-white border-primary"
                        : "bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100"
                    }`}>
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                    Xác nhận
                  </button>
                  <button
                    onClick={() => rejectTarget === req.id ? setRejectTarget(null) : openReject(req.id)}
                    className={`flex items-center gap-1.5 px-4 py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border ${
                      rejectTarget === req.id
                        ? "bg-slate-700 text-white border-slate-700"
                        : "bg-slate-100 text-slate-500 border-slate-200 hover:bg-red-50 hover:text-primary hover:border-red-200"
                    }`}>
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                    Từ chối
                  </button>
                </div>
              </div>

              {/* Confirm expand */}
              {expanding === req.id && (
                <div className="border-t border-slate-100 bg-slate-50/60 px-6 py-5">
                  <p className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-3">Phân công lịch hẹn</p>
                  <div className="grid grid-cols-3 gap-3 mb-4">
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Bác sĩ</label>
                      <div className="relative">
                        <select value={confirmForm.dentist}
                          onChange={e => setConfirmForm(p => ({ ...p, dentist: e.target.value }))}
                          className={selectCls}>
                          {DENTISTS.map(d => <option key={d}>{d}</option>)}
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                      </div>
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Phòng</label>
                      <div className="px-4 py-2.5 text-[13.5px] bg-slate-100 border border-slate-200 rounded-xl font-semibold text-slate-500">
                        {ROOMS[confirmForm.dentist]}
                      </div>
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Giờ hẹn</label>
                      <div className="relative">
                        <select value={confirmForm.time}
                          onChange={e => setConfirmForm(p => ({ ...p, time: e.target.value }))}
                          className={selectCls}>
                          <option value="">-- Chọn giờ --</option>
                          {ALL_TIMES.map(t => <option key={t}>{t}</option>)}
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                      </div>
                    </div>
                  </div>
                  <div className="flex gap-3">
                    <button onClick={() => doConfirm(req.id)} disabled={!confirmForm.time}
                      className="flex items-center gap-2 px-5 py-2.5 bg-emerald-500 hover:bg-emerald-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm shadow-emerald-200">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      Xác nhận lịch hẹn
                    </button>
                    <button onClick={() => setExpanding(null)}
                      className="px-5 py-2.5 bg-white text-slate-500 border border-slate-200 rounded-xl text-[13px] font-bold cursor-pointer hover:bg-slate-50 transition-all">
                      Huỷ
                    </button>
                  </div>
                </div>
              )}

              {/* Reject expand */}
              {rejectTarget === req.id && (
                <div className="border-t border-slate-100 bg-slate-50/60 px-6 py-5">
                  <p className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-3">Lý do từ chối</p>
                  <textarea value={rejectReason} onChange={e => setRejectReason(e.target.value)} rows={2}
                    placeholder="Nêu lý do để bệnh nhân được biết (lịch đầy, bác sĩ nghỉ...)"
                    className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none mb-3" />
                  <div className="flex gap-3">
                    <button onClick={() => doReject(req.id)} disabled={!rejectReason.trim()}
                      className="flex items-center gap-2 px-5 py-2.5 bg-primary hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm shadow-primary/25">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                      Xác nhận từ chối
                    </button>
                    <button onClick={() => setRejectTarget(null)}
                      className="px-5 py-2.5 bg-white text-slate-500 border border-slate-200 rounded-xl text-[13px] font-bold cursor-pointer hover:bg-slate-50 transition-all">
                      Huỷ
                    </button>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Processed */}
      {processed.length > 0 && (
        <div className="flex flex-col gap-3">
          <SectionHeader
            icon="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            label="Đã xử lý"
            count={processed.length}
          />
          {processed.map(req => (
            <div key={req.id} className={`bg-white rounded-2xl border shadow-sm px-6 py-4 flex items-center gap-5 ${
              req.status === "confirmed" ? "border-emerald-100" : "border-slate-200/60 opacity-70"
            }`}>
              <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${
                req.status === "confirmed" ? "bg-emerald-50" : "bg-slate-100"
              }`}>
                {req.status === "confirmed" ? (
                  <svg className="w-4.5 h-4.5 text-emerald-600" style={{width:18,height:18}} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                ) : (
                  <svg className="w-4.5 h-4.5 text-slate-400" style={{width:18,height:18}} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                )}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2.5 flex-wrap">
                  <span className="text-[14px] font-bold text-slate-900">{req.name}</span>
                  <span className="text-[12px] text-slate-400 font-mono">{req.phone}</span>
                  <span className="px-2 py-0.5 bg-sky-50 text-sky-700 border border-sky-100 rounded-full text-[11.5px] font-black">{req.service}</span>
                </div>
                {req.status === "confirmed" ? (
                  <p className="text-[12.5px] font-semibold text-emerald-700 mt-1">
                    ✓ Đã xếp lịch · {req.confirmedTime} · {req.assignedDentist} · {ROOMS[req.assignedDentist!]}
                  </p>
                ) : (
                  <p className="text-[12.5px] font-semibold text-slate-400 mt-1">✗ Từ chối · {req.rejectReason}</p>
                )}
              </div>
              <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black shrink-0 ${
                req.status === "confirmed"
                  ? "bg-emerald-50 text-emerald-700 border border-emerald-100"
                  : "bg-slate-100 text-slate-500 border border-slate-200"
              }`}>
                <span className={`w-1.5 h-1.5 rounded-full ${req.status === "confirmed" ? "bg-emerald-500" : "bg-slate-400"}`} />
                {req.status === "confirmed" ? "Đã xác nhận" : "Đã từ chối"}
              </span>
            </div>
          ))}
        </div>
      )}

      {pending.length === 0 && processed.length === 0 && (
        <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
          <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
            <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
          </div>
          <p className="text-[14px] font-bold text-slate-500">Không có đơn đặt lịch nào.</p>
        </div>
      )}
    </div>
  );
}

/* ─── Walk-in tab ─────────────────────────────────────────── */

function WalkinTab() {
  const [occupied, setOccupied] = useState<Record<string, string>>(INIT_OCCUPIED);
  const [selected, setSelected] = useState<{ dentist: string; time: string } | null>(null);
  const [form,     setForm]     = useState({ name: "", phone: "", service: SERVICES[0], note: "" });
  const [saved,    setSaved]    = useState(false);

  const slotKey = (d: string, t: string) => `${d}-${t}`;

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selected) return;
    const key = slotKey(selected.dentist, selected.time);
    setOccupied(prev => ({ ...prev, [key]: form.name }));
    setSaved(true);
    setTimeout(() => {
      setSaved(false);
      setForm({ name: "", phone: "", service: SERVICES[0], note: "" });
      setSelected(null);
    }, 2500);
  };

  return (
    <div className="flex gap-6">
      {/* Availability grid */}
      <div className="flex-1 flex flex-col gap-4 min-w-0">
        <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
          <div className="px-5 py-3.5 border-b border-slate-100 flex items-center justify-between">
            <h3 className="text-[14px] font-black text-slate-900">Lịch trống hôm nay</h3>
            <div className="flex items-center gap-3 text-[12px] font-bold">
              <span className="flex items-center gap-1.5 text-slate-400"><span className="w-3 h-3 rounded bg-slate-100 border border-slate-200 inline-block" />Đã đặt</span>
              <span className="flex items-center gap-1.5 text-emerald-600"><span className="w-3 h-3 rounded bg-emerald-50 border border-emerald-200 inline-block" />Trống</span>
              <span className="flex items-center gap-1.5 text-primary"><span className="w-3 h-3 rounded bg-red-50 border border-primary inline-block" />Đang chọn</span>
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-[12px]">
              <thead>
                <tr className="bg-slate-50/70 border-b border-slate-100">
                  <th className="px-4 py-2.5 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider w-20 shrink-0">Giờ</th>
                  {DENTISTS.map(d => (
                    <th key={d} className="px-3 py-2.5 text-center font-extrabold text-[11px] uppercase tracking-wider">
                      <div className={`inline-flex flex-col items-center gap-0.5`}>
                        <span className={`px-2.5 py-1 rounded-lg border text-[11.5px] font-black ${DENTIST_COLOR[d]}`}>{d}</span>
                        <span className="text-slate-400 font-semibold text-[10.5px]">{ROOMS[d]}</span>
                      </div>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {/* Morning divider */}
                <tr><td colSpan={5} className="px-4 py-1.5 bg-amber-50/40 border-y border-amber-100/60">
                  <span className="text-[11px] font-extrabold text-amber-600 uppercase tracking-wider flex items-center gap-1.5">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z" /></svg>
                    Ca sáng
                  </span>
                </td></tr>
                {TIMES_MORNING.map(time => (
                  <tr key={time} className="border-b border-slate-50 hover:bg-slate-50/30 transition-colors">
                    <td className="px-4 py-2 font-mono font-black text-slate-650 text-[12.5px] shrink-0">{time}</td>
                    {DENTISTS.map(dentist => {
                      const key  = slotKey(dentist, time);
                      const who  = occupied[key];
                      const isSel = selected?.dentist === dentist && selected?.time === time;
                      return (
                        <td key={dentist} className="px-2 py-1.5 text-center">
                          {who ? (
                            <span className="inline-block px-2.5 py-1.5 bg-slate-100 text-slate-500 border border-slate-200 rounded-lg text-[11px] font-bold max-w-[90px] truncate w-full">
                              {who.split(" ").slice(-1)[0]}
                            </span>
                          ) : (
                            <button onClick={() => setSelected(isSel ? null : { dentist, time })}
                              className={`w-full px-2 py-1.5 rounded-lg border text-[11px] font-bold transition-all cursor-pointer ${
                                isSel
                                  ? "bg-red-50 border-primary text-primary shadow-sm"
                                  : "bg-emerald-50 border-emerald-200 text-emerald-600 hover:border-emerald-400 hover:shadow-sm"
                              }`}>
                              {isSel ? "✓ Đang chọn" : "Trống"}
                            </button>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
                {/* Afternoon divider */}
                <tr><td colSpan={5} className="px-4 py-1.5 bg-indigo-50/40 border-y border-indigo-100/60">
                  <span className="text-[11px] font-extrabold text-indigo-600 uppercase tracking-wider flex items-center gap-1.5">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" /></svg>
                    Ca chiều
                  </span>
                </td></tr>
                {TIMES_AFTERNOON.map(time => (
                  <tr key={time} className="border-b border-slate-50 hover:bg-slate-50/30 transition-colors">
                    <td className="px-4 py-2 font-mono font-black text-slate-650 text-[12.5px]">{time}</td>
                    {DENTISTS.map(dentist => {
                      const key  = slotKey(dentist, time);
                      const who  = occupied[key];
                      const isSel = selected?.dentist === dentist && selected?.time === time;
                      return (
                        <td key={dentist} className="px-2 py-1.5 text-center">
                          {who ? (
                            <span className="inline-block px-2.5 py-1.5 bg-slate-100 text-slate-500 border border-slate-200 rounded-lg text-[11px] font-bold max-w-[90px] truncate w-full">
                              {who.split(" ").slice(-1)[0]}
                            </span>
                          ) : (
                            <button onClick={() => setSelected(isSel ? null : { dentist, time })}
                              className={`w-full px-2 py-1.5 rounded-lg border text-[11px] font-bold transition-all cursor-pointer ${
                                isSel
                                  ? "bg-red-50 border-primary text-primary shadow-sm"
                                  : "bg-emerald-50 border-emerald-200 text-emerald-600 hover:border-emerald-400 hover:shadow-sm"
                              }`}>
                              {isSel ? "✓ Đang chọn" : "Trống"}
                            </button>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Walk-in form */}
      <div className="w-80 shrink-0">
        <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-5 sticky top-5">
          <h3 className="text-[15px] font-black text-slate-900">Đặt lịch tại quầy</h3>

          {saved ? (
            <div className="flex items-center gap-3 bg-green-50 border border-green-100 text-green-700 px-4 py-3 rounded-xl text-[13px] font-bold">
              <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
              Đã đặt lịch thành công!
            </div>
          ) : (
            <>
              {/* Selected slot preview */}
              {selected ? (
                <div className="flex items-center gap-3 p-3.5 bg-red-50 border border-primary/20 rounded-xl">
                  <div className="w-8 h-8 rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
                    <svg className="w-4 h-4 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  </div>
                  <div>
                    <div className="text-[13px] font-black text-slate-900">{selected.time} · {selected.dentist}</div>
                    <div className="text-[12px] text-slate-500 font-semibold">{ROOMS[selected.dentist]}</div>
                  </div>
                  <button onClick={() => setSelected(null)} className="ml-auto text-slate-300 hover:text-slate-500 cursor-pointer">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                  </button>
                </div>
              ) : (
                <div className="flex items-center gap-2 p-3.5 bg-slate-50 border border-slate-200 border-dashed rounded-xl text-slate-400">
                  <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.042 21.672L13.684 16.6m0 0l-2.51 2.225.569-9.47 5.227 7.917-3.286-.672zm-7.518-.267A8.25 8.25 0 1120.25 10.5M8.288 14.212A5.25 5.25 0 1117.25 10.5" /></svg>
                  <span className="text-[12.5px] font-semibold">Chọn ô trống trên lịch</span>
                </div>
              )}

              <form onSubmit={handleSave} className="flex flex-col gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Họ và tên *</label>
                  <input required value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                    placeholder="Nguyễn Văn A"
                    className={inputCls} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Số điện thoại *</label>
                  <input required value={form.phone} onChange={e => setForm(p => ({ ...p, phone: e.target.value }))}
                    placeholder="09xx xxx xxx"
                    className={inputCls} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Dịch vụ *</label>
                  <div className="relative">
                    <select value={form.service} onChange={e => setForm(p => ({ ...p, service: e.target.value }))} className={selectCls}>
                      {SERVICES.map(s => <option key={s}>{s}</option>)}
                    </select>
                    <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                  </div>
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                  <textarea rows={2} value={form.note} onChange={e => setForm(p => ({ ...p, note: e.target.value }))}
                    placeholder="Yêu cầu đặc biệt..."
                    className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none" />
                </div>
                <button type="submit" disabled={!selected}
                  className="flex items-center justify-center gap-2 w-full py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer shadow-sm shadow-primary/25">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                  Xác nhận đặt lịch
                </button>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

/* ─── Main page ──────────────────────────────────────────── */

const TODAY_APPOINTMENTS = [
  { id: "A001", name: "Nguyễn Văn An",   phone: "0901 234 567", service: "Nhổ răng khôn",        time: "08:30", dentist: "BS. Thảo", room: "Phòng 1", status: "inprogress", shift: "morning"   },
  { id: "A002", name: "Trần Thị Bích",   phone: "0912 345 678", service: "Trám răng số 6",       time: "09:00", dentist: "BS. Minh", room: "Phòng 2", status: "checkedin",  shift: "morning"   },
  { id: "A003", name: "Phạm Minh Cường", phone: "0923 456 789", service: "Kiểm tra định kỳ",     time: "09:30", dentist: "BS. Thảo", room: "Phòng 1", status: "waiting",    shift: "morning"   },
  { id: "A004", name: "Lê Thu Hà",       phone: "0934 567 890", service: "Tẩy trắng răng Zoom",  time: "10:00", dentist: "BS. Linh", room: "Phòng 3", status: "waiting",    shift: "morning"   },
  { id: "A005", name: "Hoàng Văn Đức",   phone: "0945 678 901", service: "Cấy ghép Implant",     time: "10:30", dentist: "BS. Minh", room: "Phòng 2", status: "waiting",    shift: "morning"   },
  { id: "A006", name: "Đỗ Quang Huy",    phone: "0967 890 123", service: "Lấy cao răng",         time: "13:30", dentist: "BS. Thảo", room: "Phòng 1", status: "waiting",    shift: "afternoon" },
  { id: "A007", name: "Nguyễn Thị Mai",  phone: "0978 901 234", service: "Chỉnh nha Invisalign", time: "14:00", dentist: "BS. Minh", room: "Phòng 2", status: "waiting",    shift: "afternoon" },
];

const STATUS_CFG: Record<string, { label: string; bar: string; badge: string; dot: string }> = {
  waiting:    { label: "Đang chờ",  bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",   dot: "bg-amber-500"  },
  checkedin:  { label: "Check-in",  bar: "bg-emerald-400",badge: "bg-emerald-50 text-emerald-700 border border-emerald-200",dot: "bg-emerald-500"},
  inprogress: { label: "Đang khám", bar: "bg-violet-400", badge: "bg-violet-50 text-violet-700 border border-violet-200", dot: "bg-violet-500" },
  done:       { label: "Hoàn thành",bar: "bg-green-400",  badge: "bg-green-50 text-green-700 border border-green-200",   dot: "bg-green-500"  },
};

export default function OwnerAppointmentsPage() {
  useRequireOwner();
  const [tab, setTab] = useState<"today"|"online"|"walkin">("online");

  const morning   = TODAY_APPOINTMENTS.filter(a => a.shift === "morning");
  const afternoon = TODAY_APPOINTMENTS.filter(a => a.shift === "afternoon");
  const online    = INIT_REQUESTS.filter(r => r.status === "pending").length;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="appointments" />
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Ca khám & điều trị</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Xác nhận đặt lịch online và tạo lịch hẹn tại quầy
            </p>
          </div>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              {online > 0 && (
                <span className="flex items-center gap-1 px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">
                  <span className="relative flex h-2 w-2">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
                    <span className="relative inline-flex rounded-full h-2 w-2 bg-amber-500" />
                  </span>
                  {online} đơn chờ
                </span>
              )}
              <span className="px-2.5 py-1.5 bg-sky-50 text-sky-700 border border-sky-200 rounded-xl">
                {TODAY_APPOINTMENTS.length} lịch hôm nay
              </span>
            </div>
            <NotificationBell />
          </div>
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Tabs */}
          <div className="flex gap-2">
            {([
              { key: "online",  label: "Đơn đặt online",       dot: online > 0 },
              { key: "walkin",  label: "Đặt lịch tại quầy",    dot: false },
              { key: "today",   label: "Tất cả lịch hôm nay",  dot: false },
            ] as const).map(t => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`flex items-center gap-2 px-5 py-2 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer border ${
                  tab === t.key ? "bg-primary text-white border-primary shadow-sm shadow-primary/20" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                }`}>
                {t.label}
                {t.dot && (
                  <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${tab === t.key ? "bg-white/25 text-white" : "bg-amber-100 text-amber-700"}`}>
                    {online}
                  </span>
                )}
              </button>
            ))}
          </div>

          {tab === "online" && <OnlineTab />}

          {tab === "walkin" && <WalkinTab />}

          {tab === "today" && (
            <div className="flex flex-col gap-7">
              {[
                { label: "Ca sáng",  items: morning,   icon: "M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z" },
                { label: "Ca chiều", items: afternoon, icon: "M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" },
              ].filter(g => g.items.length > 0).map(group => (
                <div key={group.label} className="flex flex-col gap-3">
                  <SectionHeader icon={group.icon} label={group.label} count={group.items.length} />
                  <div className="flex flex-col gap-2.5">
                    {group.items.map((a, idx) => {
                      const s = STATUS_CFG[a.status];
                      const initials = a.name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
                      return (
                        <div key={a.id} className="flex rounded-2xl border border-slate-200/70 bg-white overflow-hidden hover:shadow-md hover:-translate-y-px transition-all">
                          <div className={`w-1.5 shrink-0 ${s.bar}`} />
                          <div className="flex items-center gap-5 px-5 py-4 flex-1 min-w-0">
                            <div className="flex flex-col items-center w-14 shrink-0">
                              <span className="text-[19px] font-black text-slate-900 font-mono leading-none tabular-nums">{a.time}</span>
                              <span className="text-[11px] font-bold text-slate-400 mt-1">#{idx + 1}</span>
                            </div>
                            <div className="w-px h-12 bg-slate-100 shrink-0" />
                            <div className="w-11 h-11 rounded-xl bg-sky-50 text-sky-700 border border-sky-100 flex items-center justify-center font-black text-[13px] shrink-0">
                              {initials}
                            </div>
                            <div className="flex-1 min-w-0">
                              <div className="text-[15px] font-black text-slate-900 leading-tight">{a.name}</div>
                              <div className="text-[13px] font-semibold text-slate-500 mt-0.5">{a.service}</div>
                              <div className="text-[12px] text-slate-400 font-medium mt-0.5 font-mono">{a.phone}</div>
                            </div>
                            <div className="shrink-0 flex flex-col items-end gap-2">
                              <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[12px] font-black whitespace-nowrap ${s.badge}`}>
                                <span className={`w-1.5 h-1.5 rounded-full ${s.dot} ${a.status === "inprogress" ? "animate-pulse" : ""}`} />
                                {s.label}
                              </span>
                              <span className="text-[11.5px] font-bold px-2 py-0.5 rounded-lg bg-slate-50 text-slate-500 border border-slate-100">
                                {a.dentist} · {a.room}
                              </span>
                            </div>
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
