"use client";

import { useState, useEffect, useCallback } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getStaffAppointmentsApi,
  confirmAppointmentApi,
  cancelAppointmentApi,
  getCancellationReasonsApi,
  type StaffAppointmentDto,
  type CancellationReasonOption,
} from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";
import { SHIFT_PERIODS, periodOfTime, type ShiftPeriod } from "../../../lib/shifts";

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

type ProcessedEntry = { appt: StaffAppointmentDto; action: "confirmed" | "cancelled" };

function OnlineTab() {
  const [pending,    setPending]    = useState<StaffAppointmentDto[]>([]);
  const [processed,  setProcessed]  = useState<ProcessedEntry[]>([]);
  const [loadingId,  setLoadingId]  = useState<string | null>(null);
  const [error,      setError]      = useState<string | null>(null);
  const [expanding,  setExpanding]  = useState<string | null>(null);
  const [rejectTarget, setRejectTarget] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  // Nhóm lý do lấy từ server (không hardcode) — nhờ đó thống kê "vì sao lịch bị hủy" mới gom được.
  const [reasonOptions, setReasonOptions] = useState<CancellationReasonOption[]>([]);
  const [reasonCode, setReasonCode] = useState("");
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);

  const selectedReason = reasonOptions.find(o => o.code === reasonCode);
  const noteRequired = selectedReason?.requiresNote ?? false;
  const missingNote = noteRequired && rejectReason.trim() === "";
  const canSubmitCancel = reasonCode !== "" && !missingNote;

  const showToast = (message: string, type: "success" | "error" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  useEffect(() => {
    getCancellationReasonsApi()
      .then(options => {
        setReasonOptions(options);
        setReasonCode(options[0]?.code ?? "");
      })
      .catch(() => showToast("Không tải được danh sách lý do hủy.", "error"));
  }, []);

  const load = useCallback(async () => {
    try {
      const data = await getStaffAppointmentsApi({ status: "Pending" });
      setPending(data);
    } catch {
      setError("Không thể tải danh sách đặt lịch.");
    }
  }, []);

  useEffect(() => {
    void load();
    const channel = supabase
      .channel("staff-online-tab")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void load();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [load]);

  const doConfirm = async (appt: StaffAppointmentDto) => {
    setLoadingId(appt.appointmentId);
    try {
      await confirmAppointmentApi(appt.appointmentId);
      setPending(prev => prev.filter(a => a.appointmentId !== appt.appointmentId));
      setProcessed(prev => [{ appt: { ...appt, status: "Confirmed" }, action: "confirmed" }, ...prev]);
      setExpanding(null);
    } catch {
      alert("Xác nhận thất bại. Vui lòng thử lại.");
    } finally {
      setLoadingId(null);
    }
  };

  const doCancel = async (appt: StaffAppointmentDto) => {
    // Nút đã bị vô hiệu hóa khi chưa hợp lệ nên nhánh này không tới được từ UI — giữ lại làm chốt
    // chặn nếu sau này nút được bật ở nơi khác, và để hàm an toàn khi gọi trực tiếp.
    if (!canSubmitCancel) return;

    setLoadingId(appt.appointmentId);
    try {
      await cancelAppointmentApi(appt.appointmentId, reasonCode, rejectReason);
      setPending(prev => prev.filter(a => a.appointmentId !== appt.appointmentId));
      setProcessed(prev => [{ appt: { ...appt, status: "Cancelled" }, action: "cancelled" }, ...prev]);
      setRejectTarget(null);
      setRejectReason("");
      showToast("Đã từ chối lịch hẹn và thông báo cho bệnh nhân.");
    } catch (e) {
      // Hiển thị đúng thông điệp của server (trạng thái không hủy được, đã hủy rồi...) thay vì
      // câu chung chung — người dùng cần biết vì sao mới biết phải làm gì tiếp.
      showToast(e instanceof Error ? e.message : "Hủy lịch thất bại. Vui lòng thử lại.", "error");
    } finally {
      setLoadingId(null);
    }
  };

  const fmtDate = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getDate()).padStart(2,"0")}/${String(d.getMonth()+1).padStart(2,"0")}/${d.getFullYear()}`;
  };
  const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
  };

  if (error) return (
    <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
      <p className="text-[14px] font-semibold text-red-500">{error}</p>
      <button onClick={load} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">Thử lại</button>
    </div>
  );

  return (
    <div className="flex flex-col gap-6">
      {/* Toast — fixed theo viewport, cùng khuôn với các trang khác trong admin */}
      {toast && (
        <div className={`fixed top-6 right-6 z-[9999] px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border font-bold text-[14.5px] max-w-md ${
          toast.type === "success"
            ? "bg-emerald-900 text-white border-emerald-800"
            : "bg-red-900 text-white border-red-800"
        }`}>
          <span className="text-lg">{toast.type === "success" ? "✓" : "⚠"}</span>
          <span>{toast.message}</span>
        </div>
      )}

      {/* Pending */}
      {pending.length > 0 && (
        <div className="flex flex-col gap-3">
          <SectionHeader
            icon="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"
            label="Chờ xác nhận"
            count={pending.length}
          />
          {pending.map(appt => {
            const isConfirming = expanding === appt.appointmentId;
            const isRejecting  = rejectTarget === appt.appointmentId;
            const isLoading    = loadingId === appt.appointmentId;
            const initials     = appt.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
            return (
              <div key={appt.appointmentId} className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
                <div className="flex items-start gap-5 px-6 py-5">
                  <div className="w-11 h-11 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[13px] text-sky-700 shrink-0">
                    {initials}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2.5 flex-wrap">
                      <span className="text-[15px] font-black text-slate-900">{appt.patientName}</span>
                      {appt.patientPhone && <span className="text-[12px] font-medium text-slate-400 font-mono">{appt.patientPhone}</span>}
                      <span className="px-2 py-0.5 bg-sky-50 text-sky-700 border border-sky-100 rounded-full text-[11.5px] font-black">{appt.serviceName ?? "Khám tổng quát"}</span>
                      <span className="text-[11px] text-slate-400 font-mono">#{appt.appointmentCode}</span>
                    </div>
                    <div className="flex items-center gap-2 mt-1.5 text-[13px] text-slate-500 font-semibold">
                      <svg className="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                      <span>Yêu cầu: <strong className="text-slate-700">{fmtDate(appt.appointmentDate)}</strong> lúc <strong className="text-slate-700">{fmtTime(appt.appointmentDate)}</strong></span>
                      <span className="text-slate-300">·</span>
                      <span className="text-slate-400 text-[12px]">Gửi ngày {fmtDate(appt.createdAt)}</span>
                    </div>
                    {appt.symptoms && (
                      <div className="mt-2 flex items-start gap-1.5 text-[12.5px] text-amber-700 bg-amber-50 border border-amber-100 px-3 py-1.5 rounded-lg font-semibold w-fit">
                        <svg className="w-3.5 h-3.5 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                        {appt.symptoms}
                      </div>
                    )}
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <button disabled={isLoading}
                      onClick={() => isConfirming ? setExpanding(null) : (setExpanding(appt.appointmentId), setRejectTarget(null))}
                      className={`flex items-center gap-1.5 px-4 py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border disabled:opacity-50 ${
                        isConfirming ? "bg-primary text-white border-primary" : "bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      Xác nhận
                    </button>
                    <button disabled={isLoading}
                      onClick={() => isRejecting ? setRejectTarget(null) : (setRejectTarget(appt.appointmentId), setExpanding(null), setRejectReason(""))}
                      className={`flex items-center gap-1.5 px-4 py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border disabled:opacity-50 ${
                        isRejecting ? "bg-slate-700 text-white border-slate-700" : "bg-slate-100 text-slate-500 border-slate-200 hover:bg-red-50 hover:text-primary hover:border-red-200"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                      Từ chối
                    </button>
                  </div>
                </div>

                {/* Confirm expand */}
                {isConfirming && (
                  <div className="border-t border-slate-100 bg-slate-50/60 px-6 py-5">
                    <p className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-3">
                      Xác nhận lịch hẹn · {fmtDate(appt.appointmentDate)} lúc {fmtTime(appt.appointmentDate)} · {appt.dentistName}
                    </p>
                    <div className="flex gap-3">
                      <button onClick={() => doConfirm(appt)} disabled={isLoading}
                        className="flex items-center gap-2 px-5 py-2.5 bg-emerald-500 hover:bg-emerald-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm shadow-emerald-200">
                        {isLoading ? <span className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" /> : <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>}
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
                {isRejecting && (
                  <div className="border-t border-slate-100 bg-slate-50/60 px-6 py-5">
                    <p className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-3">Lý do từ chối</p>
                    <select value={reasonCode} onChange={e => setReasonCode(e.target.value)}
                      className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 mb-3 cursor-pointer">
                      {reasonOptions.map(o => (
                        <option key={o.code} value={o.code}>{o.labelVi}</option>
                      ))}
                    </select>
                    <textarea value={rejectReason} onChange={e => setRejectReason(e.target.value)} rows={2}
                      placeholder={noteRequired
                        ? "Nêu rõ lý do để bệnh nhân được biết"
                        : "Ghi chú thêm (không bắt buộc)"}
                      className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none mb-3" />
                    {/* Nút bị vô hiệu hóa phải nói được VÌ SAO, không thì người dùng chỉ thấy nút xám
                        và không biết còn thiếu gì. */}
                    {missingNote && (
                      <p className="text-[12.5px] font-bold text-amber-600 mb-3">
                        Chọn &ldquo;Lý do khác&rdquo; thì cần ghi rõ nội dung để bệnh nhân hiểu.
                      </p>
                    )}
                    <div className="flex gap-3">
                      <button onClick={() => doCancel(appt)} disabled={!canSubmitCancel || isLoading}
                        className="flex items-center gap-2 px-5 py-2.5 bg-primary hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm shadow-primary/25">
                        {isLoading ? <span className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" /> : <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>}
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
            );
          })}
        </div>
      )}

      {/* Processed this session */}
      {processed.length > 0 && (
        <div className="flex flex-col gap-3">
          <SectionHeader
            icon="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            label="Đã xử lý (phiên này)"
            count={processed.length}
          />
          {processed.map(({ appt, action }) => (
            <div key={appt.appointmentId} className={`bg-white rounded-2xl border shadow-sm px-6 py-4 flex items-center gap-5 ${
              action === "confirmed" ? "border-emerald-100" : "border-slate-200/60 opacity-70"
            }`}>
              <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${action === "confirmed" ? "bg-emerald-50" : "bg-slate-100"}`}>
                {action === "confirmed"
                  ? <svg className="text-emerald-600" style={{width:18,height:18}} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  : <svg className="text-slate-400" style={{width:18,height:18}} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2.5 flex-wrap">
                  <span className="text-[14px] font-bold text-slate-900">{appt.patientName}</span>
                  {appt.patientPhone && <span className="text-[12px] text-slate-400 font-mono">{appt.patientPhone}</span>}
                  <span className="px-2 py-0.5 bg-sky-50 text-sky-700 border border-sky-100 rounded-full text-[11.5px] font-black">{appt.serviceName ?? "Khám tổng quát"}</span>
                </div>
                <p className={`text-[12.5px] font-semibold mt-1 ${action === "confirmed" ? "text-emerald-700" : "text-slate-400"}`}>
                  {action === "confirmed"
                    ? `✓ Đã xác nhận · ${fmtDate(appt.appointmentDate)} lúc ${fmtTime(appt.appointmentDate)} · ${appt.dentistName}`
                    : `✗ Đã hủy · ${fmtDate(appt.appointmentDate)} lúc ${fmtTime(appt.appointmentDate)}`}
                </p>
              </div>
              <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black shrink-0 ${
                action === "confirmed" ? "bg-emerald-50 text-emerald-700 border border-emerald-100" : "bg-slate-100 text-slate-500 border border-slate-200"
              }`}>
                <span className={`w-1.5 h-1.5 rounded-full ${action === "confirmed" ? "bg-emerald-500" : "bg-slate-400"}`} />
                {action === "confirmed" ? "Đã xác nhận" : "Đã hủy"}
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
          <p className="text-[14px] font-bold text-slate-500">Không có đơn đặt lịch nào đang chờ.</p>
        </div>
      )}
    </div>
  );
}

/* ─── Confirmed tab ──────────────────────────────────────── */

function ConfirmedTab() {
  const [confirmed, setConfirmed] = useState<StaffAppointmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getStaffAppointmentsApi({ status: "Confirmed" });
      setConfirmed(data);
    } catch {
      setError("Không thể tải danh sách lịch hẹn đã xác nhận.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
    const channel = supabase
      .channel("staff-confirmed-tab")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void load();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [load]);

  const fmtDate = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getDate()).padStart(2,"0")}/${String(d.getMonth()+1).padStart(2,"0")}/${d.getFullYear()}`;
  };
  const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
  };

  if (loading) return (
    <div className="flex items-center justify-center py-20">
      <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
    </div>
  );

  if (error) return (
    <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
      <p className="text-[14px] font-semibold text-red-500">{error}</p>
      <button onClick={load} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">Thử lại</button>
    </div>
  );

  return (
    <div className="flex flex-col gap-6">
      {confirmed.length > 0 ? (
        <div className="flex flex-col gap-3">
          <SectionHeader
            icon="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            label="Lịch hẹn đã xác nhận"
            count={confirmed.length}
          />
          {confirmed.map(appt => {
            const initials = appt.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
            return (
              <div key={appt.appointmentId} className="bg-white rounded-2xl border border-emerald-100 shadow-sm overflow-hidden hover:shadow-md transition-all">
                <div className="flex items-center gap-5 px-6 py-4">
                  <div className="w-11 h-11 rounded-xl bg-emerald-50 border border-emerald-100 flex items-center justify-center font-black text-[13px] text-emerald-700 shrink-0">
                    {initials}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2.5 flex-wrap">
                      <span className="text-[15px] font-black text-slate-900">{appt.patientName}</span>
                      {appt.patientPhone && <span className="text-[12px] font-medium text-slate-400 font-mono">{appt.patientPhone}</span>}
                      <span className="px-2 py-0.5 bg-emerald-50 text-emerald-700 border border-emerald-100 rounded-full text-[11.5px] font-black">{appt.serviceName ?? "Khám tổng quát"}</span>
                      <span className="text-[11px] text-slate-400 font-mono">#{appt.appointmentCode}</span>
                    </div>
                    <div className="flex items-center gap-2 mt-1.5 text-[13px] text-slate-500 font-semibold">
                      <svg className="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                      <span>{fmtDate(appt.appointmentDate)} lúc <strong className="text-slate-700">{fmtTime(appt.appointmentDate)}</strong></span>
                      <span className="text-slate-300">·</span>
                      <span className="text-[12px] text-slate-400">{appt.dentistName}</span>
                    </div>
                  </div>
                  <div className="shrink-0 flex items-center gap-2">
                    <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[12px] font-black bg-emerald-50 text-emerald-700 border border-emerald-100">
                      <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
                      Đã xác nhận
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
          <div className="w-14 h-14 rounded-full bg-emerald-100 flex items-center justify-center">
            <svg className="w-7 h-7 text-emerald-500" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
          </div>
          <p className="text-[14px] font-bold text-slate-500">Không có lịch hẹn nào đã xác nhận.</p>
        </div>
      )}
    </div>
  );
}

/* ─── Main page ──────────────────────────────────────────── */


const STATUS_CFG: Record<string, { label: string; bar: string; badge: string; dot: string }> = {
  waiting:    { label: "Đang chờ",    bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",     dot: "bg-amber-500"  },
  checkedin:  { label: "Check-in",    bar: "bg-emerald-400",badge: "bg-emerald-50 text-emerald-700 border border-emerald-200",dot: "bg-emerald-500"},
  inprogress: { label: "Đang khám",   bar: "bg-violet-400", badge: "bg-violet-50 text-violet-700 border border-violet-200",  dot: "bg-violet-500" },
  done:       { label: "Hoàn thành",  bar: "bg-green-400",  badge: "bg-green-50 text-green-700 border border-green-200",     dot: "bg-green-500"  },
  // API statuses
  pending:    { label: "Chờ xác nhận",bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",     dot: "bg-amber-500"  },
  confirmed:  { label: "Đã xác nhận", bar: "bg-sky-400",    badge: "bg-sky-50 text-sky-700 border border-sky-200",           dot: "bg-sky-500"    },
  completed:  { label: "Hoàn thành",  bar: "bg-green-400",  badge: "bg-green-50 text-green-700 border border-green-200",     dot: "bg-green-500"  },
  cancelled:  { label: "Đã hủy",      bar: "bg-slate-300",  badge: "bg-slate-100 text-slate-500 border border-slate-200",    dot: "bg-slate-400"  },
  noshow:     { label: "Vắng mặt",    bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",     dot: "bg-amber-500"  },
};

// Icon cho tiêu đề mỗi buổi ở tab "Tất cả lịch hôm nay" — mặt trời (sáng), mây (chiều), mặt trăng (tối).
const PERIOD_ICON: Record<ShiftPeriod, string> = {
  "Buổi sáng":  "M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z",
  "Buổi chiều": "M2.25 15a4.5 4.5 0 004.5 4.5H18a3.75 3.75 0 001.332-7.257 3 3 0 00-3.758-3.848 5.25 5.25 0 00-10.233 2.33A4.502 4.502 0 002.25 15z",
  "Buổi tối":   "M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z",
};

export default function AppointmentsPage() {
  useRequireStaff();
  const [tab, setTab] = useState<"online"|"confirmed"|"today">("online");

  // Today's appointments loaded from API for the "today" tab and header badge
  const [todayAppts, setTodayAppts]     = useState<StaffAppointmentDto[]>([]);
  const [pendingCount, setPendingCount] = useState(0);
  const [confirmedCount, setConfirmedCount] = useState(0);

  useEffect(() => {
    const today = new Date();
    const dateStr = `${today.getFullYear()}-${String(today.getMonth()+1).padStart(2,"0")}-${String(today.getDate()).padStart(2,"0")}`;

    const reload = () => {
      void getStaffAppointmentsApi({ date: dateStr }).then(data => setTodayAppts(data));
      void getStaffAppointmentsApi({ status: "Pending" }).then(data => setPendingCount(data.length));
    };

    reload();

    const channel = supabase
      .channel("staff-page-header")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, reload)
      .subscribe();

    return () => { void supabase.removeChannel(channel); };
  }, []);

  // Update confirmedCount when todayAppts changes
  useEffect(() => {
    const confirmed = todayAppts.filter(a => a.status === "Confirmed").length;
    setConfirmedCount(confirmed);
  }, [todayAppts]);

  const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
  };

  // Nhóm lịch hôm nay theo buổi (sáng / chiều / tối) giống trang xếp lịch & lịch bác sĩ.
  const apptsByPeriod = (period: ShiftPeriod) =>
    todayAppts.filter(a => periodOfTime(new Date(a.appointmentDate)) === period);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="appointments" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader
          title="Đặt Lịch & Nhận Đơn"
          subtitle="Xác nhận và theo dõi các đơn đặt lịch online"
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              {pendingCount > 0 && (
                <span className="flex items-center gap-1 px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">
                  <span className="relative flex h-2 w-2"><span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" /><span className="relative inline-flex rounded-full h-2 w-2 bg-amber-500" /></span>
                  {pendingCount} đơn chờ
                </span>
              )}
              {confirmedCount > 0 && (
                <span className="flex items-center gap-1 px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">
                  {confirmedCount} đơn đã xác nhận
                </span>
              )}
              <span className="px-2.5 py-1.5 bg-sky-50 text-sky-700 border border-sky-200 rounded-xl">{todayAppts.length} lịch hôm nay</span>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Tabs */}
          <div className="flex gap-2">
            {([
              { key: "online",  label: "Đơn đặt online",       dot: pendingCount > 0 },
              { key: "confirmed", label: "Đã xác nhận",         dot: confirmedCount > 0 },
              { key: "today",   label: "Tất cả lịch hôm nay",  dot: false },
            ] as const).map(t => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`flex items-center gap-2 px-5 py-2 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer border ${
                  tab === t.key ? "bg-primary text-white border-primary shadow-sm shadow-primary/20" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                }`}>
                {t.label}
                {t.dot && (
                  <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${tab === t.key ? "bg-white/25 text-white" : "bg-emerald-100 text-emerald-700"}`}>
                    {t.key === "confirmed" ? confirmedCount : pendingCount}
                  </span>
                )}
              </button>
            ))}
          </div>

          {tab === "online" && <OnlineTab />}
          {tab === "confirmed" && <ConfirmedTab />}

          {tab === "today" && (
            <div className="flex flex-col gap-7">
              {SHIFT_PERIODS
                .map(period => ({ label: period, items: apptsByPeriod(period), icon: PERIOD_ICON[period] }))
                .filter(g => g.items.length > 0).map(group => (
                <div key={group.label} className="flex flex-col gap-3">
                  <SectionHeader icon={group.icon} label={group.label} count={group.items.length} />
                  <div className="flex flex-col gap-2.5">
                    {group.items.map((a, idx) => {
                      const statusKey = a.status.toLowerCase() as keyof typeof STATUS_CFG;
                      const s = STATUS_CFG[statusKey] ?? STATUS_CFG.waiting;
                      const initials = a.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
                      return (
                        <div key={a.appointmentId} className="flex rounded-2xl border border-slate-200/70 bg-white overflow-hidden hover:shadow-md hover:-translate-y-px transition-all">
                          <div className={`w-1.5 shrink-0 ${s.bar}`} />
                          <div className="flex items-center gap-5 px-5 py-4 flex-1 min-w-0">
                            <div className="flex flex-col items-center w-14 shrink-0">
                              <span className="text-[19px] font-black text-slate-900 font-mono leading-none tabular-nums">{fmtTime(a.appointmentDate)}</span>
                              <span className="text-[11px] font-bold text-slate-400 mt-1">#{idx + 1}</span>
                            </div>
                            <div className="w-px h-12 bg-slate-100 shrink-0" />
                            <div className="w-11 h-11 rounded-xl bg-sky-50 text-sky-700 border border-sky-100 flex items-center justify-center font-black text-[13px] shrink-0">
                              {initials}
                            </div>
                            <div className="flex-1 min-w-0">
                              <div className="text-[15px] font-black text-slate-900 leading-tight">{a.patientName}</div>
                              <div className="text-[13px] font-semibold text-slate-500 mt-0.5">{a.serviceName ?? "Khám tổng quát"}</div>
                              {a.patientPhone && <div className="text-[12px] text-slate-400 font-medium mt-0.5 font-mono">{a.patientPhone}</div>}
                            </div>
                            <div className="shrink-0 flex flex-col items-end gap-2">
                              <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[12px] font-black whitespace-nowrap ${s.badge}`}>
                                <span className={`w-1.5 h-1.5 rounded-full ${s.dot}`} />
                                {s.label}
                              </span>
                              <span className="text-[11.5px] font-bold px-2 py-0.5 rounded-lg bg-slate-50 text-slate-500 border border-slate-100">
                                {a.dentistName}
                              </span>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              ))}
              {todayAppts.length === 0 && (
                <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex items-center justify-center py-16">
                  <p className="text-[14px] font-bold text-slate-400">Không có lịch hẹn nào hôm nay.</p>
                </div>
              )}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
