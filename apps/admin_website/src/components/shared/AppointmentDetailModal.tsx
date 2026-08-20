"use client";

import { useEffect } from "react";
import { createPortal } from "react-dom";
import type { StaffAppointmentDto } from "../../lib/apiClient";

const STATUS_LABEL: Record<string, { label: string; badge: string }> = {
  Pending:         { label: "Chờ xác nhận", badge: "bg-amber-50 text-amber-700 border border-amber-200" },
  Confirmed:       { label: "Đã xác nhận",  badge: "bg-sky-50 text-sky-700 border border-sky-200" },
  CheckedIn:       { label: "Check-in",     badge: "bg-emerald-50 text-emerald-700 border border-emerald-200" },
  InProgress:      { label: "Đang khám",    badge: "bg-violet-50 text-violet-700 border border-violet-200" },
  PendingPayment:  { label: "Chờ thanh toán", badge: "bg-orange-50 text-orange-700 border border-orange-200" },
  Completed:       { label: "Hoàn thành",   badge: "bg-green-50 text-green-700 border border-green-200" },
  Cancelled:       { label: "Đã hủy",       badge: "bg-slate-100 text-slate-500 border border-slate-200" },
  NoShow:          { label: "Vắng mặt",     badge: "bg-amber-50 text-amber-700 border border-amber-200" },
};

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2,"0")}/${String(d.getMonth()+1).padStart(2,"0")}/${d.getFullYear()}`;
};
const fmtTime = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
};

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{label}</span>
      <span className="text-[13.5px] font-bold text-slate-800">{value}</span>
    </div>
  );
}

/**
 * Xem nhanh chi tiết một lịch hẹn từ danh sách — lễ tân bấm vào thẻ thay vì phải mở trang khác
 * chỉ để đối chiếu thông tin (ai đặt hộ, đặt bằng tài khoản nào, triệu chứng...).
 */
export default function AppointmentDetailModal({
  appointment,
  onClose,
}: {
  appointment: StaffAppointmentDto;
  onClose: () => void;
}) {
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  if (typeof document === "undefined") return null;

  const status = STATUS_LABEL[appointment.status] ?? { label: appointment.status, badge: "bg-slate-100 text-slate-500 border border-slate-200" };
  // Chỉ khác chủ tài khoản khi bệnh nhân là người thân được đặt hộ (có quan hệ ghi nhận).
  const bookedByOther = !!appointment.patientRelationship;

  return createPortal(
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-[9998] bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 sm:p-6"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] flex flex-col overflow-hidden"
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div className="px-5 sm:px-6 py-4 border-b border-slate-100 flex items-start justify-between gap-4 shrink-0">
          <div className="min-w-0">
            <h3 className="text-[16px] font-black text-slate-900">Chi tiết lịch hẹn</h3>
            <p className="text-[12.5px] font-semibold text-slate-500 mt-0.5 truncate">#{appointment.appointmentCode}</p>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black whitespace-nowrap ${status.badge}`}>
              {status.label}
            </span>
            <button onClick={onClose} className="w-8 h-8 rounded-xl text-slate-400 hover:bg-slate-100 hover:text-slate-600 flex items-center justify-center shrink-0 cursor-pointer transition-colors">
              <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="flex-1 min-h-0 overflow-y-auto px-5 sm:px-6 py-5 flex flex-col gap-5">
          <div className="grid grid-cols-2 gap-4">
            <Field label="Bệnh nhân" value={
              <>
                {appointment.patientName}
                {appointment.patientRelationship && (
                  <span className="ml-1.5 px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10.5px] font-black align-middle">
                    {appointment.patientRelationship}
                  </span>
                )}
              </>
            } />
            <Field label="Số điện thoại" value={appointment.patientPhone ?? "—"} />
            <Field label="Dịch vụ" value={appointment.serviceName ?? "Khám tổng quát"} />
            <Field label="Bác sĩ" value={appointment.dentistName} />
            <Field label="Ngày hẹn" value={fmtDate(appointment.appointmentDate)} />
            <Field label="Giờ hẹn" value={fmtTime(appointment.appointmentDate)} />
            <Field label="Nguồn đặt" value={appointment.origin === "Online" ? "Đặt online" : "Lập tại quầy"} />
            <Field label="Gửi lúc" value={`${fmtDate(appointment.createdAt)} ${fmtTime(appointment.createdAt)}`} />
          </div>

          <div className="border-t border-slate-100 pt-4 flex flex-col gap-3">
            <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">
              {bookedByOther ? "Người đặt hộ (chủ tài khoản)" : "Chủ tài khoản"}
            </span>
            <div className="flex items-center gap-3 bg-slate-50 border border-slate-100 rounded-xl px-4 py-3">
              <div className="w-9 h-9 rounded-lg bg-white border border-slate-200 flex items-center justify-center font-black text-[12px] text-slate-500 shrink-0">
                {appointment.accountHolderName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
              </div>
              <div className="min-w-0">
                <p className="text-[13.5px] font-black text-slate-800 truncate">{appointment.accountHolderName}</p>
                <p className="text-[12px] font-semibold text-slate-500 truncate">{appointment.accountHolderEmail ?? "Không có email"}</p>
              </div>
            </div>
          </div>

          {appointment.symptoms && (
            <div className="flex flex-col gap-1.5">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Triệu chứng</span>
              <p className="text-[13px] font-semibold text-amber-700 bg-amber-50 border border-amber-100 px-3.5 py-2.5 rounded-xl">
                {appointment.symptoms}
              </p>
            </div>
          )}

          {appointment.checkedInAt && (
            <Field label="Check-in lúc" value={`${fmtDate(appointment.checkedInAt)} ${fmtTime(appointment.checkedInAt)}`} />
          )}
        </div>
      </div>
    </div>,
    document.body
  );
}
