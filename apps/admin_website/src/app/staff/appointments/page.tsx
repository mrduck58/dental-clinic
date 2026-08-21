"use client";

import { useState, useEffect, useCallback } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import { Toast, useToast } from "../../../components/shared/Toast";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import RescheduleAppointmentModal from "../../../components/shared/RescheduleAppointmentModal";
import AppointmentDetailModal from "../../../components/shared/AppointmentDetailModal";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getStaffAppointmentsApi,
  confirmAppointmentApi,
  cancelAppointmentApi,
  getCancellationReasonsApi,
  getStaffAppointmentChangeRequestsApi,
  approveAppointmentChangeRequestApi,
  rejectAppointmentChangeRequestApi,
  type StaffAppointmentDto,
  type CancellationReasonOption,
  type AppointmentChangeRequestDto,
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

/**
 * Lọc theo NGÀY HẸN của đơn (không phải ngày gửi đơn). Mặc định để trống = xem tất cả: đơn đặt
 * online rải ra nhiều ngày tới, khóa sẵn vào một ngày sẽ giấu mất phần lớn việc đang chờ xử lý.
 */
function DateFilterBar({
  value,
  onChange,
  countLabel,
}: {
  value: string;
  onChange: (next: string) => void;
  countLabel: string;
}) {
  const now = new Date();
  const todayStr = `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,"0")}-${String(now.getDate()).padStart(2,"0")}`;

  return (
    <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm px-4 py-3 flex flex-wrap items-center gap-2.5">
      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Lọc theo ngày hẹn</span>
      <input
        type="date"
        value={value}
        onChange={e => onChange(e.target.value)}
        className="px-3 py-1.5 text-[12.5px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-700"
      />
      <button
        onClick={() => onChange(todayStr)}
        className={`px-3 py-1.5 rounded-lg text-[12px] font-bold border cursor-pointer transition-all ${
          value === todayStr
            ? "bg-primary text-white border-primary"
            : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
        }`}>
        Hôm nay
      </button>
      {value && (
        <button
          onClick={() => onChange("")}
          className="px-3 py-1.5 rounded-lg text-[12px] font-bold border bg-white text-slate-500 border-slate-200 hover:bg-slate-50 cursor-pointer transition-all flex items-center gap-1.5">
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
          Bỏ lọc
        </button>
      )}
      <span className="ml-auto text-[12px] font-bold text-slate-400">{countLabel}</span>
    </div>
  );
}

/* ─── Online requests tab ────────────────────────────────── */

type ProcessedEntry = { appt: StaffAppointmentDto; action: "confirmed" | "cancelled" };

function OnlineTab() {
  const [pending,        setPending]        = useState<StaffAppointmentDto[]>([]);
  const [changeRequests, setChangeRequests] = useState<AppointmentChangeRequestDto[]>([]);
  const [processed,      setProcessed]      = useState<ProcessedEntry[]>([]);
  const [loadingId,      setLoadingId]      = useState<string | null>(null);
  const [loadingChangeReqId, setLoadingChangeReqId] = useState<string | null>(null);
  const [error,          setError]          = useState<string | null>(null);
  const [expanding,      setExpanding]      = useState<string | null>(null);
  const [rejectTarget,   setRejectTarget]   = useState<string | null>(null);
  const [rejectReason,   setRejectReason]   = useState("");
  const [rejectChangeReqTarget, setRejectChangeReqTarget] = useState<string | null>(null);
  const [rejectChangeReqNote,   setRejectChangeReqNote]   = useState("");
  const [reschedulingAppt, setReschedulingAppt] = useState<StaffAppointmentDto | null>(null);
  // Lịch đang mở xem chi tiết (bấm vào thẻ) — tách riêng khỏi reschedulingAppt vì hai việc độc lập.
  const [detailAppt, setDetailAppt] = useState<StaffAppointmentDto | null>(null);
  // "" = tất cả ngày (mặc định).
  const [dateFilter, setDateFilter] = useState("");
  const [reasonOptions, setReasonOptions] = useState<CancellationReasonOption[]>([]);
  const [reasonCode, setReasonCode] = useState("");
  const { toast, showToast } = useToast();

  const selectedReason = reasonOptions.find(o => o.code === reasonCode);
  const noteRequired = selectedReason?.requiresNote ?? false;
  const missingNote = noteRequired && rejectReason.trim() === "";
  const canSubmitCancel = reasonCode !== "" && !missingNote;

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
      const [apptData, reqData] = await Promise.all([
        getStaffAppointmentsApi({ status: "Pending", date: dateFilter || undefined }),
        getStaffAppointmentChangeRequestsApi({ status: "Pending", date: dateFilter || undefined }),
      ]);
      setPending(apptData);
      setChangeRequests(reqData);
    } catch {
      setError("Không thể tải danh sách đặt lịch.");
    }
  }, [dateFilter]);

  useEffect(() => {
    void load();
    const channel = supabase
      .channel("staff-online-tab")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void load();
      })
      .on("postgres_changes", { event: "*", schema: "public", table: "AppointmentChangeRequests" }, () => {
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
      showToast("Đã xác nhận lịch hẹn và thông báo cho bệnh nhân.");
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Xác nhận thất bại. Vui lòng thử lại.", "error");
    } finally {
      setLoadingId(null);
    }
  };

  const doCancel = async (appt: StaffAppointmentDto) => {
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
      showToast(e instanceof Error ? e.message : "Hủy lịch thất bại. Vui lòng thử lại.", "error");
    } finally {
      setLoadingId(null);
    }
  };

  const doApproveChangeRequest = async (req: AppointmentChangeRequestDto) => {
    setLoadingChangeReqId(req.id);
    try {
      await approveAppointmentChangeRequestApi(req.id);
      setChangeRequests(prev => prev.filter(r => r.id !== req.id));
      showToast(`Đã duyệt yêu cầu ${req.type === "Cancel" ? "hủy" : "dời"} lịch của ${req.patientName}.`);
      void load();
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Duyệt yêu cầu thất bại.", "error");
    } finally {
      setLoadingChangeReqId(null);
    }
  };

  const doRejectChangeRequest = async (req: AppointmentChangeRequestDto) => {
    if (!rejectChangeReqNote.trim()) {
      showToast("Vui lòng nhập lý do từ chối.", "error");
      return;
    }
    setLoadingChangeReqId(req.id);
    try {
      await rejectAppointmentChangeRequestApi(req.id, rejectChangeReqNote.trim());
      setChangeRequests(prev => prev.filter(r => r.id !== req.id));
      setRejectChangeReqTarget(null);
      setRejectChangeReqNote("");
      showToast(`Đã từ chối yêu cầu của ${req.patientName}.`);
    } catch (e) {
      showToast(e instanceof Error ? e.message : "Từ chối yêu cầu thất bại.", "error");
    } finally {
      setLoadingChangeReqId(null);
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

  const totalActionsCount = pending.length + changeRequests.length;

  return (
    <div className="flex flex-col gap-6">
      <Toast toast={toast} />

      {reschedulingAppt && (
        <RescheduleAppointmentModal
          appointment={reschedulingAppt}
          onClose={() => setReschedulingAppt(null)}
          onDone={info => {
            setReschedulingAppt(null);
            void load();
            showToast(
              `Đã đổi lịch của ${reschedulingAppt.patientName} sang ${info.date.split("-").reverse().join("/")} lúc ${info.time} · ${info.dentistName}.`,
            );
          }}
        />
      )}

      {detailAppt && (
        <AppointmentDetailModal appointment={detailAppt} onClose={() => setDetailAppt(null)} />
      )}

      <DateFilterBar
        value={dateFilter}
        onChange={setDateFilter}
        countLabel={`${totalActionsCount} mục cần xử lý`}
      />

      {/* Yêu cầu thay đổi lịch (Hủy / Dời sau 24h) */}
      {changeRequests.length > 0 && (
        <div className="flex flex-col gap-3">
          <SectionHeader
            icon="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99"
            label="Yêu cầu thay đổi lịch (Hủy / Dời sau 24h)"
            count={changeRequests.length}
          />
          {changeRequests.map(req => {
            const isLoading = loadingChangeReqId === req.id;
            const isRejecting = rejectChangeReqTarget === req.id;
            const initials = req.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
            const isCancel = req.type === "Cancel";

            return (
              <div key={req.id} className="bg-white rounded-2xl border border-amber-200/80 shadow-sm overflow-hidden ring-1 ring-amber-400/20">
                <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4 p-4 sm:px-6 sm:py-5">
                  <div className="flex items-start gap-3 sm:gap-4 flex-1 min-w-0">
                    <div className={`w-10 h-10 sm:w-11 sm:h-11 rounded-xl flex items-center justify-center font-black text-[13px] shrink-0 ${
                      isCancel ? "bg-rose-50 border border-rose-100 text-rose-700" : "bg-indigo-50 border border-indigo-100 text-indigo-700"
                    }`}>
                      {initials}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-[15px] font-black text-slate-900">{req.patientName}</span>
                        {req.patientPhone && <span className="text-[12px] font-medium text-slate-400 font-mono">{req.patientPhone}</span>}
                        {isCancel ? (
                          <span className="px-2.5 py-0.5 bg-rose-50 text-rose-700 border border-rose-200 rounded-full text-[11.5px] font-black flex items-center gap-1">
                            <span className="w-1.5 h-1.5 rounded-full bg-rose-500" />
                            Yêu cầu hủy lịch
                          </span>
                        ) : (
                          <span className="px-2.5 py-0.5 bg-indigo-50 text-indigo-700 border border-indigo-200 rounded-full text-[11.5px] font-black flex items-center gap-1">
                            <span className="w-1.5 h-1.5 rounded-full bg-indigo-500" />
                            Yêu cầu dời lịch
                          </span>
                        )}
                        <span className="text-[11px] text-slate-400 font-mono">#{req.appointmentCode}</span>
                      </div>

                      {/* Lịch cũ */}
                      <div className="flex items-center gap-2 mt-1.5 text-[12.5px] sm:text-[13px] text-slate-500 font-semibold flex-wrap">
                        <span>Lịch hiện tại: <strong className="text-slate-700">{fmtDate(req.currentAppointmentDate)}</strong> lúc <strong className="text-slate-700">{fmtTime(req.currentAppointmentDate)}</strong> · {req.currentDentistName}</span>
                        <span className="text-slate-300 hidden sm:inline">·</span>
                        <span className="text-slate-400 text-[11.5px] sm:text-[12px]">Gửi lúc {fmtDate(req.createdAt)} {fmtTime(req.createdAt)}</span>
                      </div>

                      {/* Lịch mới nếu là dời */}
                      {!isCancel && req.desiredDate && (
                        <div className="mt-2 flex items-center gap-2 text-[12.5px] sm:text-[13px] text-indigo-700 bg-indigo-50/80 border border-indigo-100 px-3 py-1.5 rounded-xl font-bold w-fit flex-wrap">
                          <svg className="w-4 h-4 text-indigo-500 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                          </svg>
                          <span>Dời sang: <strong className="text-indigo-900">{fmtDate(req.desiredDate.toString())}</strong> lúc <strong className="text-indigo-900">{req.desiredTimeSlot || fmtTime(req.desiredDate.toString())}</strong></span>
                          {req.desiredDentistName && <span>· BS: <strong className="text-indigo-900">{req.desiredDentistName}</strong></span>}
                        </div>
                      )}

                      {/* Lý do bệnh nhân gửi */}
                      <div className="mt-2 flex items-start gap-1.5 text-[12px] sm:text-[12.5px] text-amber-900 bg-amber-50/90 border border-amber-200/70 px-3 py-1.5 rounded-xl font-medium w-fit">
                        <svg className="w-3.5 h-3.5 mt-0.5 shrink-0 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0012 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018z" />
                        </svg>
                        <span>Lý do bệnh nhân: <strong className="text-amber-950 font-semibold">{req.reason}</strong></span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 shrink-0 border-t sm:border-t-0 pt-3 sm:pt-0 border-slate-100 w-full sm:w-auto justify-end">
                    <button
                      disabled={isLoading}
                      onClick={() => doApproveChangeRequest(req)}
                      className="flex-1 sm:flex-initial flex items-center justify-center gap-1.5 px-3.5 sm:px-4 py-2.5 sm:py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border disabled:opacity-50 whitespace-nowrap bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100 shadow-sm">
                      {isLoading ? (
                        <span className="w-4 h-4 border-2 border-emerald-600/40 border-t-emerald-600 rounded-full animate-spin" />
                      ) : (
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      )}
                      Duyệt
                    </button>
                    <button
                      disabled={isLoading}
                      onClick={() => isRejecting ? setRejectChangeReqTarget(null) : (setRejectChangeReqTarget(req.id), setRejectChangeReqNote(""))}
                      className={`flex-1 sm:flex-initial flex items-center justify-center gap-1.5 px-3.5 sm:px-4 py-2.5 sm:py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border disabled:opacity-50 whitespace-nowrap ${
                        isRejecting ? "bg-slate-700 text-white border-slate-700" : "bg-slate-100 text-slate-600 border-slate-200 hover:bg-rose-50 hover:text-rose-700 hover:border-rose-200"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                      Từ chối
                    </button>
                  </div>
                </div>

                {/* Reject box for Change Request */}
                {isRejecting && (
                  <div className="border-t border-amber-100 bg-amber-50/40 px-4 sm:px-6 py-4">
                    <p className="text-[12.5px] font-extrabold text-slate-600 uppercase tracking-wider mb-2">Lý do từ chối yêu cầu</p>
                    <textarea
                      value={rejectChangeReqNote}
                      onChange={e => setRejectChangeReqNote(e.target.value)}
                      rows={2}
                      placeholder="Nêu rõ lý do từ chối để bệnh nhân nhận được thông báo cụ thể..."
                      className="w-full px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none mb-3"
                    />
                    <div className="flex gap-2.5">
                      <button
                        onClick={() => doRejectChangeRequest(req)}
                        disabled={isLoading || !rejectChangeReqNote.trim()}
                        className="px-4 py-2 bg-rose-600 hover:bg-rose-700 disabled:opacity-40 text-white rounded-xl text-[12.5px] font-black cursor-pointer transition-all shadow-sm">
                        Xác nhận từ chối
                      </button>
                      <button
                        onClick={() => setRejectChangeReqTarget(null)}
                        className="px-4 py-2 bg-white text-slate-500 border border-slate-200 rounded-xl text-[12.5px] font-bold cursor-pointer hover:bg-slate-50 transition-all">
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
                <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4 p-4 sm:px-6 sm:py-5">
                  <div
                    onClick={() => setDetailAppt(appt)}
                    className="flex items-start gap-3 sm:gap-4 flex-1 min-w-0 cursor-pointer rounded-xl -m-1.5 p-1.5 hover:bg-slate-50 transition-colors">
                    <div className="w-10 h-10 sm:w-11 sm:h-11 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[13px] text-sky-700 shrink-0">
                      {initials}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-[15px] font-black text-slate-900">{appt.patientName}</span>
                        {appt.patientRelationship && (
                          <span className="px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10.5px] font-black">{appt.patientRelationship}</span>
                        )}
                        <span className="px-2 py-0.5 bg-sky-50 text-sky-700 border border-sky-100 rounded-full text-[11.5px] font-black">{appt.serviceName ?? "Khám tổng quát"}</span>
                        {appt.status?.toLowerCase() === "rebooking" && (
                          <span className="px-2.5 py-0.5 bg-amber-50 text-amber-800 border border-amber-300 rounded-full text-[11.5px] font-black flex items-center gap-1">
                            <span className="w-1.5 h-1.5 rounded-full bg-amber-500" />
                            Rebooking
                          </span>
                        )}
                        <span className="text-[11px] text-slate-400 font-mono">#{appt.appointmentCode}</span>
                      </div>
                      <div className="flex items-center gap-2 mt-1.5 text-[12.5px] sm:text-[13px] text-slate-500 font-semibold flex-wrap">
                        <span className="flex items-center gap-1">
                          <svg className="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                          Yêu cầu: <strong className="text-slate-700">{fmtDate(appt.appointmentDate)}</strong> lúc <strong className="text-slate-700">{fmtTime(appt.appointmentDate)}</strong>
                        </span>
                        <span className="text-slate-300 hidden sm:inline">·</span>
                        <span className="text-slate-400 text-[11.5px] sm:text-[12px]">Gửi ngày {fmtDate(appt.createdAt)}</span>
                      </div>
                      <div className="flex items-center gap-1 mt-1.5 text-[12px] text-slate-400 font-semibold flex-wrap">
                        <svg className="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" /></svg>
                        Đặt bởi <span className="text-slate-500">{appt.accountHolderName}</span>
                        {appt.accountHolderEmail && <span className="text-slate-300">· {appt.accountHolderEmail}</span>}
                      </div>
                      {appt.symptoms && (
                        <div className="mt-2 flex items-start gap-1.5 text-[12px] sm:text-[12.5px] text-amber-700 bg-amber-50 border border-amber-100 px-3 py-1.5 rounded-lg font-semibold w-fit">
                          <svg className="w-3.5 h-3.5 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                          {appt.symptoms}
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0 border-t sm:border-t-0 pt-3 sm:pt-0 border-slate-100 w-full sm:w-auto justify-end">
                    <button disabled={isLoading}
                      onClick={() => isConfirming ? setExpanding(null) : (setExpanding(appt.appointmentId), setRejectTarget(null))}
                      className={`flex-1 sm:flex-initial flex items-center justify-center gap-1.5 px-3.5 sm:px-4 py-2.5 sm:py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border disabled:opacity-50 whitespace-nowrap ${
                        isConfirming ? "bg-primary text-white border-primary" : "bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      Xác nhận
                    </button>
                    <button disabled={isLoading}
                      onClick={() => { setReschedulingAppt(appt); setExpanding(null); setRejectTarget(null); }}
                      className="flex-1 sm:flex-initial flex items-center justify-center gap-1.5 px-3.5 sm:px-4 py-2.5 sm:py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border disabled:opacity-50 whitespace-nowrap bg-sky-50 text-sky-700 border-sky-200 hover:bg-sky-100">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5M12 12.75h.008v.008H12v-.008z" /></svg>
                      Đổi lịch
                    </button>
                    <button disabled={isLoading}
                      onClick={() => isRejecting ? setRejectTarget(null) : (setRejectTarget(appt.appointmentId), setExpanding(null), setRejectReason(""))}
                      className={`flex-1 sm:flex-initial flex items-center justify-center gap-1.5 px-3.5 sm:px-4 py-2.5 sm:py-2 rounded-xl text-[13px] font-bold cursor-pointer transition-all border disabled:opacity-50 whitespace-nowrap ${
                        isRejecting ? "bg-slate-700 text-white border-slate-700" : "bg-slate-100 text-slate-500 border-slate-200 hover:bg-red-50 hover:text-primary hover:border-red-200"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                      Từ chối
                    </button>
                  </div>
                </div>

                {/* Confirm expand */}
                {isConfirming && (
                  <div className="border-t border-slate-100 bg-slate-50/60 px-4 sm:px-6 py-5">
                    <p className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-3">
                      Xác nhận lịch hẹn · {fmtDate(appt.appointmentDate)} lúc {fmtTime(appt.appointmentDate)} · {appt.dentistName}
                    </p>
                    <div className="flex flex-col sm:flex-row gap-2.5 sm:gap-3">
                      <button onClick={() => doConfirm(appt)} disabled={isLoading}
                        className="flex items-center justify-center gap-2 px-5 py-2.5 bg-emerald-500 hover:bg-emerald-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm shadow-emerald-200 w-full sm:w-auto">
                        {isLoading ? <span className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" /> : <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>}
                        Xác nhận lịch hẹn
                      </button>
                      <button onClick={() => setExpanding(null)}
                        className="px-5 py-2.5 bg-white text-slate-500 border border-slate-200 rounded-xl text-[13px] font-bold cursor-pointer hover:bg-slate-50 transition-all text-center w-full sm:w-auto">
                        Huỷ
                      </button>
                    </div>
                  </div>
                )}

                {/* Reject expand */}
                {isRejecting && (
                  <div className="border-t border-slate-100 bg-slate-50/60 px-4 sm:px-6 py-5">
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
                    {missingNote && (
                      <p className="text-[12.5px] font-bold text-amber-600 mb-3">
                        Chọn &ldquo;Lý do khác&rdquo; thì cần ghi rõ nội dung để bệnh nhân hiểu.
                      </p>
                    )}
                    <div className="flex flex-col sm:flex-row gap-2.5 sm:gap-3">
                      <button onClick={() => doCancel(appt)} disabled={!canSubmitCancel || isLoading}
                        className="flex items-center justify-center gap-2 px-5 py-2.5 bg-primary hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm shadow-primary/25 w-full sm:w-auto">
                        {isLoading ? <span className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" /> : <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>}
                        Xác nhận từ chối
                      </button>
                      <button onClick={() => setRejectTarget(null)}
                        className="px-5 py-2.5 bg-white text-slate-500 border border-slate-200 rounded-xl text-[13px] font-bold cursor-pointer hover:bg-slate-50 transition-all text-center w-full sm:w-auto">
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

      {pending.length === 0 && changeRequests.length === 0 && processed.length === 0 && (
        <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
          <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
            <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
          </div>
          <p className="text-[14px] font-bold text-slate-500">
            {dateFilter
              ? `Không có đơn nào hẹn ngày ${dateFilter.split("-").reverse().join("/")}.`
              : "Không có đơn đặt lịch hay yêu cầu thay đổi nào đang chờ."}
          </p>
          {dateFilter && (
            <button onClick={() => setDateFilter("")}
              className="px-4 py-2 text-[13px] font-bold text-slate-500 border border-slate-200 rounded-xl hover:bg-slate-50 cursor-pointer transition-all">
              Xem tất cả các ngày
            </button>
          )}
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
  // "" = tất cả ngày (mặc định).
  const [dateFilter, setDateFilter] = useState("");
  const [detailAppt, setDetailAppt] = useState<StaffAppointmentDto | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getStaffAppointmentsApi({ status: "Confirmed", date: dateFilter || undefined });
      setConfirmed(data);
    } catch {
      setError("Không thể tải danh sách lịch hẹn đã xác nhận.");
    } finally {
      setLoading(false);
    }
  }, [dateFilter]);

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

  if (error) return (
    <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
      <p className="text-[14px] font-semibold text-red-500">{error}</p>
      <button onClick={load} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">Thử lại</button>
    </div>
  );

  return (
    <div className="flex flex-col gap-6">
      {detailAppt && (
        <AppointmentDetailModal appointment={detailAppt} onClose={() => setDetailAppt(null)} />
      )}

      <DateFilterBar
        value={dateFilter}
        onChange={setDateFilter}
        countLabel={`${confirmed.length} lịch đã xác nhận`}
      />

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
        </div>
      ) : confirmed.length > 0 ? (
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
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 sm:px-6 sm:py-4">
                  <div
                    onClick={() => setDetailAppt(appt)}
                    className="flex items-center gap-3 sm:gap-4 flex-1 min-w-0 cursor-pointer rounded-xl -m-1.5 p-1.5 hover:bg-slate-50 transition-colors">
                    <div className="w-10 h-10 sm:w-11 sm:h-11 rounded-xl bg-emerald-50 border border-emerald-100 flex items-center justify-center font-black text-[13px] text-emerald-700 shrink-0">
                      {initials}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2.5 flex-wrap">
                        <span className="text-[15px] font-black text-slate-900">{appt.patientName}</span>
                        {appt.patientRelationship && (
                          <span className="px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10.5px] font-black">{appt.patientRelationship}</span>
                        )}
                        {appt.patientPhone && <span className="text-[12px] font-medium text-slate-400 font-mono">{appt.patientPhone}</span>}
                        <span className="px-2 py-0.5 bg-emerald-50 text-emerald-700 border border-emerald-100 rounded-full text-[11.5px] font-black">{appt.serviceName ?? "Khám tổng quát"}</span>
                        <span className="text-[11px] text-slate-400 font-mono">#{appt.appointmentCode}</span>
                      </div>
                      <div className="flex items-center gap-2 mt-1.5 text-[12.5px] sm:text-[13px] text-slate-500 font-semibold flex-wrap">
                        <span>{fmtDate(appt.appointmentDate)} lúc <strong className="text-slate-700">{fmtTime(appt.appointmentDate)}</strong></span>
                        <span className="text-slate-300">·</span>
                        <span className="text-[12px] text-slate-400">{appt.dentistName}</span>
                      </div>
                      <div className="flex items-center gap-1 mt-1.5 text-[12px] text-slate-400 font-semibold flex-wrap">
                        <svg className="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" /></svg>
                        Đặt bởi <span className="text-slate-500">{appt.accountHolderName}</span>
                        {appt.accountHolderEmail && <span className="text-slate-300">· {appt.accountHolderEmail}</span>}
                      </div>
                    </div>
                  </div>
                  <div className="shrink-0 flex items-center gap-2 self-end sm:self-center">
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
          <p className="text-[14px] font-bold text-slate-500">
            {dateFilter
              ? `Không có lịch đã xác nhận nào hẹn ngày ${dateFilter.split("-").reverse().join("/")}.`
              : "Không có lịch hẹn nào đã xác nhận."}
          </p>
          {dateFilter && (
            <button onClick={() => setDateFilter("")}
              className="px-4 py-2 text-[13px] font-bold text-slate-500 border border-slate-200 rounded-xl hover:bg-slate-50 cursor-pointer transition-all">
              Xem tất cả các ngày
            </button>
          )}
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
  const [detailAppt, setDetailAppt] = useState<StaffAppointmentDto | null>(null);

  useEffect(() => {
    const today = new Date();
    const dateStr = `${today.getFullYear()}-${String(today.getMonth()+1).padStart(2,"0")}-${String(today.getDate()).padStart(2,"0")}`;

    const reload = () => {
      void getStaffAppointmentsApi({ date: dateStr }).then(data => setTodayAppts(data));
      void Promise.all([
        getStaffAppointmentsApi({ status: "Pending" }),
        getStaffAppointmentChangeRequestsApi({ status: "Pending" }),
      ]).then(([appts, reqs]) => setPendingCount(appts.length + reqs.length));
    };

    reload();

    const channel = supabase
      .channel("staff-page-header")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, reload)
      .on("postgres_changes", { event: "*", schema: "public", table: "AppointmentChangeRequests" }, reload)
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
      {detailAppt && (
        <AppointmentDetailModal appointment={detailAppt} onClose={() => setDetailAppt(null)} />
      )}

      <StaffSidebar activeMenu="appointments" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader
          title="Đặt Lịch & Nhận Đơn"
          subtitle="Xác nhận và theo dõi các đơn đặt lịch online"
          right={
            <div className="flex items-center gap-1.5 sm:gap-2 text-[12px] sm:text-[12.5px] font-bold">
              {pendingCount > 0 && (
                <span className="flex items-center gap-1 px-2 sm:px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl whitespace-nowrap">
                  <span className="relative flex h-2 w-2"><span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" /><span className="relative inline-flex rounded-full h-2 w-2 bg-amber-500" /></span>
                  {pendingCount} chờ
                </span>
              )}
              {confirmedCount > 0 && (
                <span className="hidden sm:flex items-center gap-1 px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl whitespace-nowrap">
                  {confirmedCount} đã xác nhận
                </span>
              )}
              <span className="hidden md:inline-flex px-2.5 py-1.5 bg-sky-50 text-sky-700 border border-sky-200 rounded-xl whitespace-nowrap">{todayAppts.length} lịch hôm nay</span>
            </div>
          }
        />

        <div className="p-4 sm:p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Tabs */}
          <div className="flex gap-2 overflow-x-auto pb-1 max-w-full flex-nowrap shrink-0">
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
                        <div key={a.appointmentId}
                          onClick={() => setDetailAppt(a)}
                          className="flex rounded-2xl border border-slate-200/70 bg-white overflow-hidden hover:shadow-md hover:-translate-y-px transition-all cursor-pointer">
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
                              <div className="flex items-center gap-2 flex-wrap">
                                <span className="text-[15px] font-black text-slate-900 leading-tight">{a.patientName}</span>
                                {a.patientRelationship && (
                                  <span className="px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10.5px] font-black">{a.patientRelationship}</span>
                                )}
                              </div>
                              <div className="text-[13px] font-semibold text-slate-500 mt-0.5">{a.serviceName ?? "Khám tổng quát"}</div>
                              {a.patientPhone && <div className="text-[12px] text-slate-400 font-medium mt-0.5 font-mono">{a.patientPhone}</div>}
                              <div className="text-[11.5px] text-slate-400 font-semibold mt-0.5 truncate">
                                Đặt bởi {a.accountHolderName}{a.accountHolderEmail && ` · ${a.accountHolderEmail}`}
                              </div>
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
