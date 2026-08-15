"use client";

import { useState, useEffect, useCallback } from "react";
import { createPortal } from "react-dom";
import {
  getStaffScheduleApi,
  rescheduleAppointmentApi,
  type StaffAppointmentDto,
  type StaffScheduleResponse,
} from "../../lib/apiClient";

/* ─── lưới giờ ───────────────────────────────────────────── */

// Khớp với GetStaffScheduleHandler (backend). Mỗi ô chỉ khả dụng nếu bác sĩ có ca bao trùm khung giờ.
const TIME_GROUPS: { label: string; times: string[] }[] = [
  { label: "Buổi sáng",  times: ["08:00","08:30","09:00","09:30","10:00","10:30","11:00","11:30"] },
  { label: "Buổi chiều", times: ["13:30","14:00","14:30","15:00","15:30","16:00","16:30","17:00"] },
  { label: "Buổi tối",   times: ["17:30","18:00","18:30","19:00","19:30","20:00","20:30","21:00"] },
];

const toDateInput = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

const fmtDateVi = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2,"0")}/${String(d.getMonth()+1).padStart(2,"0")}/${d.getFullYear()}`;
};

const fmtTime = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
};

/**
 * Giờ Việt Nam (UTC+7) → ISO UTC. Date.UTC coi các thành phần là giờ VN rồi trừ đi offset, nên
 * kết quả không phụ thuộc múi giờ của máy lễ tân — cùng cách làm với form đặt lịch tại quầy.
 */
const vnToUtcIso = (dateStr: string, time: string) => {
  const [y, m, d] = dateStr.split("-").map(Number);
  const [h, min]  = time.split(":").map(Number);
  return new Date(Date.UTC(y, m - 1, d, h, min, 0) - 7 * 60 * 60 * 1000).toISOString();
};

/* ─── component ──────────────────────────────────────────── */

/**
 * Đổi lịch hẹn cho lễ tân: chọn ngày, rồi chọn ô trống trong lưới bác sĩ × khung giờ của đúng
 * ngày đó. Dùng chung một lưới với trang đặt lịch tại quầy để lễ tân không phải học hai cách đọc
 * lịch khác nhau.
 *
 * Lịch được SỬA TẠI CHỖ (giữ nguyên mã lịch hẹn) chứ không hủy-rồi-tạo-mới — xem
 * RescheduleAppointmentHandler ở backend.
 */
export default function RescheduleAppointmentModal({
  appointment,
  onClose,
  onDone,
}: {
  appointment: StaffAppointmentDto;
  onClose: () => void;
  /** Gọi sau khi dời thành công — trang cha tự tải lại danh sách và hiện thông báo. */
  onDone: (info: { date: string; time: string; dentistName: string }) => void;
}) {
  const todayStr = toDateInput(new Date());
  const currentDateStr = toDateInput(new Date(appointment.appointmentDate));
  const currentTime = fmtTime(appointment.appointmentDate);

  // Lịch đã qua thì mặc định mở ở hôm nay — không ai dời lịch về quá khứ.
  const [date, setDate] = useState(currentDateStr < todayStr ? todayStr : currentDateStr);
  const [schedule, setSchedule] = useState<StaffScheduleResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [selected, setSelected] = useState<{ dentistId: string; dentistName: string; time: string } | null>(null);
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError(null);
    setSelected(null);
    try {
      setSchedule(await getStaffScheduleApi(date));
    } catch (e) {
      setSchedule(null);
      setLoadError(e instanceof Error ? e.message : "Không tải được lịch trống");
    } finally {
      setLoading(false);
    }
  }, [date]);

  useEffect(() => { void load(); }, [load]);

  const handleSubmit = async () => {
    if (!selected) return;
    setSaveError(null);
    setSaving(true);
    try {
      await rescheduleAppointmentApi(
        appointment.appointmentId,
        vnToUtcIso(date, selected.time),
        { dentistId: selected.dentistId, reason: reason.trim() || undefined },
      );
      onDone({ date, time: selected.time, dentistName: selected.dentistName });
    } catch (e) {
      setSaveError(e instanceof Error ? e.message : "Dời lịch thất bại");
    } finally {
      setSaving(false);
    }
  };

  if (typeof document === "undefined") return null;

  const dentists = schedule?.dentists ?? [];

  return createPortal(
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-[9998] bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 sm:p-6"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-2xl shadow-xl w-full max-w-3xl max-h-[92vh] flex flex-col overflow-hidden"
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div className="px-5 sm:px-6 py-4 border-b border-slate-100 flex items-start justify-between gap-4 shrink-0">
          <div className="min-w-0">
            <h3 className="text-[16px] font-black text-slate-900">Đổi lịch hẹn</h3>
            <p className="text-[12.5px] font-semibold text-slate-500 mt-0.5 truncate">
              {appointment.patientName} · hiện đang hẹn {fmtDateVi(appointment.appointmentDate)} lúc {currentTime} · {appointment.dentistName}
            </p>
          </div>
          <button onClick={onClose} className="w-8 h-8 rounded-xl text-slate-400 hover:bg-slate-100 hover:text-slate-600 flex items-center justify-center shrink-0 cursor-pointer transition-colors">
            <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 min-h-0 overflow-y-auto px-5 sm:px-6 py-5 flex flex-col gap-4">
          <div className="flex flex-col sm:flex-row sm:items-end gap-3">
            <div className="flex flex-col gap-1.5">
              <label className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider">Ngày hẹn mới</label>
              <input
                type="date"
                value={date}
                min={todayStr}
                onChange={e => setDate(e.target.value)}
                className="px-3.5 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700"
              />
            </div>
            <div className="flex items-center gap-2.5 sm:gap-3 text-[11.5px] font-bold flex-wrap pb-1">
              <span className="flex items-center gap-1.5 text-emerald-600"><span className="w-3 h-3 rounded bg-emerald-50 border border-emerald-200 inline-block" />Trống</span>
              <span className="flex items-center gap-1.5 text-primary"><span className="w-3 h-3 rounded bg-red-50 border border-primary inline-block" />Đang chọn</span>
              <span className="flex items-center gap-1.5 text-slate-400"><span className="w-3 h-3 rounded bg-slate-100 border border-slate-200 inline-block" />Đã đặt</span>
              <span className="flex items-center gap-1.5 text-sky-600"><span className="w-3 h-3 rounded bg-sky-50 border border-sky-300 inline-block" />Lịch hiện tại</span>
            </div>
          </div>

          {loading ? (
            <div className="py-14 flex items-center justify-center">
              <div className="w-6 h-6 border-2 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : loadError ? (
            <div className="py-10 flex flex-col items-center gap-3">
              <p className="text-[13px] font-semibold text-red-500">{loadError}</p>
              <button onClick={load} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">Thử lại</button>
            </div>
          ) : dentists.length === 0 ? (
            <div className="py-12 text-center">
              <p className="text-[13.5px] font-bold text-slate-500">Không có bác sĩ nào làm việc ngày này.</p>
              <p className="text-[12.5px] font-semibold text-slate-400 mt-1">Chọn ngày khác hoặc phân ca cho bác sĩ ở trang Lịch làm việc.</p>
            </div>
          ) : (
            <div className="border border-slate-200 rounded-xl overflow-x-auto">
              <table className="w-full text-[12px] min-w-[520px]">
                <thead>
                  <tr>
                    <th className="px-3 py-2.5 text-left text-[11px] font-extrabold text-slate-400 uppercase tracking-wider bg-slate-50 sticky left-0">Giờ</th>
                    {dentists.map(d => (
                      <th key={d.dentistId} className="px-2 py-2.5 text-center bg-slate-50">
                        <div className="text-[12px] font-black text-slate-700 truncate max-w-[120px] mx-auto">{d.name}</div>
                        <div className="text-[10.5px] font-bold text-slate-400">{d.room}</div>
                      </th>
                    ))}
                  </tr>
                </thead>
                {TIME_GROUPS.map(group => {
                  // Chỉ hiện buổi có ít nhất một bác sĩ làm việc — bảng rỗng chỉ tổ làm dài trang.
                  const visible = group.times.filter(t =>
                    dentists.some(d => d.slots.some(s => s.time === t)));
                  if (visible.length === 0) return null;

                  return (
                    <tbody key={group.label}>
                      <tr>
                        <td colSpan={dentists.length + 1}
                          className="px-3 py-1.5 text-[11px] font-black text-slate-500 uppercase tracking-wider bg-slate-50/70 border-y border-slate-100">
                          {group.label}
                        </td>
                      </tr>
                      {visible.map(time => (
                        <tr key={time} className="border-t border-slate-50">
                          <td className="px-3 py-1.5 font-mono font-black text-slate-700 tabular-nums sticky left-0 bg-white">{time}</td>
                          {dentists.map(dentist => {
                            const slot = dentist.slots.find(s => s.time === time);
                            const isSel = selected?.dentistId === dentist.dentistId && selected?.time === time;
                            // Ô của chính lịch đang sửa — đánh dấu để lễ tân biết mình đang đứng ở đâu,
                            // nếu không nó chỉ hiện "đã đặt" và trông như bị người khác chiếm chỗ.
                            const isCurrent = date === currentDateStr
                              && time === currentTime
                              && dentist.name === appointment.dentistName;

                            return (
                              <td key={dentist.dentistId} className="px-2 py-1.5 text-center">
                                {!slot ? (
                                  <span className="inline-block px-2.5 py-1.5 text-slate-300 text-[11px] font-bold w-full">—</span>
                                ) : isCurrent ? (
                                  <span className="inline-block w-full px-2 py-1.5 rounded-lg border border-sky-300 bg-sky-50 text-sky-700 text-[11px] font-bold">
                                    Lịch hiện tại
                                  </span>
                                ) : slot.isBooked ? (
                                  <span className="inline-block px-2.5 py-1.5 bg-slate-100 text-slate-500 border border-slate-200 rounded-lg text-[11px] font-bold max-w-[90px] truncate w-full">
                                    {(slot.patientName ?? "Đã đặt").split(" ").slice(-1)[0]}
                                  </span>
                                ) : slot.isPast ? (
                                  <span className="inline-block w-full px-2 py-1.5 rounded-lg border border-slate-200 bg-slate-50 text-slate-300 text-[11px] font-bold cursor-not-allowed">
                                    Đã qua giờ
                                  </span>
                                ) : (
                                  <button
                                    onClick={() => setSelected(isSel ? null : { dentistId: dentist.dentistId, dentistName: dentist.name, time })}
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
                  );
                })}
              </table>
            </div>
          )}

          <div className="flex flex-col gap-1.5">
            <label className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider">
              Lý do đổi lịch <span className="normal-case font-bold text-slate-300">(không bắt buộc — sẽ hiện trong thông báo gửi bệnh nhân)</span>
            </label>
            <input
              value={reason}
              onChange={e => setReason(e.target.value)}
              placeholder="Ví dụ: bác sĩ bận ca mổ, bệnh nhân gọi điện xin đổi giờ"
              className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
            />
          </div>

          {saveError && (
            <div className="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-[12.5px] font-semibold">
              {saveError}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-5 sm:px-6 py-4 bg-slate-50/60 border-t border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shrink-0">
          <p className="text-[12.5px] font-bold text-slate-500 min-w-0">
            {selected
              ? <>Giờ mới: <span className="text-slate-900 font-black">{selected.time}</span> ngày {date.split("-").reverse().join("/")} · {selected.dentistName}</>
              : "Chọn một ô trống trong lưới để đổi sang giờ đó."}
          </p>
          <div className="flex items-center gap-2.5 shrink-0">
            <button
              onClick={onClose}
              disabled={saving}
              className="px-4 py-2.5 text-[13px] font-bold text-slate-500 border border-slate-200 bg-white rounded-xl hover:bg-slate-50 disabled:opacity-50 transition-colors cursor-pointer"
            >
              Hủy
            </button>
            <button
              onClick={handleSubmit}
              disabled={!selected || saving}
              className="flex items-center gap-2 px-5 py-2.5 text-[13px] font-black text-white bg-primary hover:bg-red-600 rounded-xl disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
            >
              {saving && <span className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />}
              Xác nhận đổi lịch
            </button>
          </div>
        </div>
      </div>
    </div>,
    document.body
  );
}
