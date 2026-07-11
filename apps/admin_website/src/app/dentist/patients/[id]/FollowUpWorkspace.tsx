"use client";

import { useState, useEffect, useMemo } from "react";
import {
  getExaminationApi,
  getPatientTreatmentPlansApi,
  setFollowUpReminderApi,
  clearFollowUpReminderApi,
  type ExaminationDto,
  type TreatmentPlanDto,
} from "../../../../lib/apiClient";

interface FollowUpWorkspaceProps {
  appointmentId: string;
}

// Các mốc hẹn tái khám nhanh
const PRESETS: { label: string; days?: number; months?: number }[] = [
  { label: "Sau 1 ngày", days: 1 },
  { label: "Sau 3 ngày", days: 3 },
  { label: "Sau 7 ngày", days: 7 },
  { label: "Sau 14 ngày", days: 14 },
  { label: "Sau 1 tháng", months: 1 },
  { label: "Sau 2 tháng", months: 2 },
  { label: "Sau 3 tháng", months: 3 },
  { label: "Sau 6 tháng", months: 6 },
];

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
};

const dayKey = (iso: string) => {
  const d = new Date(iso);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
};

const toIsoDate = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

const addInterval = (days?: number, months?: number): Date => {
  const d = new Date();
  if (days) d.setDate(d.getDate() + days);
  if (months) d.setMonth(d.getMonth() + months);
  return d;
};

function CardHeader({ title, icon, color, action }: {
  title: string; icon: string; color: string; action?: React.ReactNode;
}) {
  return (
    <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
      <svg className={`w-5 h-5 ${color}`} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
      </svg>
      <span className="text-[14px] font-black text-slate-900">{title}</span>
      {action && <div className="ml-auto">{action}</div>}
    </div>
  );
}

export default function FollowUpWorkspace({ appointmentId }: FollowUpWorkspaceProps) {
  const [examination, setExamination] = useState<ExaminationDto | null>(null);
  const [plans, setPlans] = useState<TreatmentPlanDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);
  const showToast = (message: string, type: "success" | "error" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  // Form hẹn tái khám
  const [selectedPreset, setSelectedPreset] = useState<number | null>(null);
  const [customValue, setCustomValue] = useState("");
  const [customUnit, setCustomUnit] = useState<"days" | "months">("months");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  const isInProgress = examination?.status === "InProgress";

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const exam = await getExaminationApi(appointmentId);
        if (cancelled) return;
        setExamination(exam);
        setNote(exam.followUpNote ?? "");
        setError(null);

        const planList = await getPatientTreatmentPlansApi(exam.patient.id).catch(() => []);
        if (!cancelled) setPlans(planList);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Không thể tải thông tin bệnh nhân");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [appointmentId]);

  // Quá trình điều trị của ngày hẹn này
  const todayProgress = useMemo(() => {
    if (!examination) return [];
    const key = dayKey(examination.appointmentDate);
    return plans
      .flatMap(p => p.stepProgress.map(sp => ({ ...sp, serviceName: p.serviceName })))
      .filter(sp => dayKey(sp.date) === key)
      .sort((a, b) => a.stepNumber - b.stepNumber);
  }, [plans, examination]);

  // Ngày tái khám được chọn (preset hoặc tùy chỉnh)
  const selectedDate = useMemo(() => {
    if (selectedPreset !== null) {
      const p = PRESETS[selectedPreset];
      return addInterval(p.days, p.months);
    }
    const n = parseInt(customValue);
    if (n > 0) return addInterval(customUnit === "days" ? n : undefined, customUnit === "months" ? n : undefined);
    return null;
  }, [selectedPreset, customValue, customUnit]);

  const handleSave = async () => {
    if (!selectedDate) return;
    try {
      setSaving(true);
      const result = await setFollowUpReminderApi(appointmentId, {
        followUpDate: toIsoDate(selectedDate),
        note: note.trim() || undefined,
      });
      setExamination(prev => prev ? { ...prev, followUpDate: result.followUpDate, followUpNote: result.followUpNote } : prev);
      showToast("Đã lưu lịch hẹn tái khám");
      setSelectedPreset(null);
      setCustomValue("");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể lưu lịch hẹn", "error");
    } finally {
      setSaving(false);
    }
  };

  const handleClear = async () => {
    try {
      setSaving(true);
      await clearFollowUpReminderApi(appointmentId);
      setExamination(prev => prev ? { ...prev, followUpDate: null, followUpNote: null } : prev);
      setNote("");
      showToast("Đã hủy lịch hẹn tái khám");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể hủy lịch hẹn", "error");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center py-24">
        <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !examination) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-4 py-24">
        <p className="text-[14px] font-semibold text-red-500">{error ?? "Đã xảy ra lỗi hệ thống."}</p>
      </div>
    );
  }

  const pt = examination.patient;
  const initials = pt.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
  const prescriptionItems = examination.prescription?.items ?? [];

  return (
    <div>
      <div className="grid gap-6 items-start" style={{ gridTemplateColumns: "2fr 1fr" }}>
        {/* ══════════ LEFT 2/3: PATIENT INFO ══════════ */}
        <div className="flex flex-col gap-6">
          {/* Thông tin bệnh nhân */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-sky-600"
              title="Thông tin bệnh nhân"
              icon="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"
            />
            <div className="p-5">
              <div className="flex items-start gap-4">
                <div className={`w-16 h-16 rounded-2xl flex items-center justify-center font-black text-xl border-2 shrink-0 ${pt.gender === "Nữ" ? "bg-rose-50 text-rose-500 border-rose-100" : "bg-sky-50 text-sky-600 border-sky-100"}`}>
                  {initials}
                </div>
                <div className="flex-1 grid grid-cols-3 gap-3">
                  {[
                    ["Họ tên", pt.fullName],
                    ["Ngày sinh", pt.dateOfBirth ? `${new Date(pt.dateOfBirth).toLocaleDateString("vi-VN")} (${new Date().getFullYear() - new Date(pt.dateOfBirth).getFullYear()} tuổi)` : "—"],
                    ["Giới tính", pt.gender ?? "—"],
                    ["Điện thoại", pt.phoneNumber ?? "—"],
                    ["Email", pt.email ?? "—"],
                    ["Địa chỉ", pt.address ?? "—"],
                  ].map(([label, value]) => (
                    <div key={label}>
                      <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{label}</div>
                      <div className="text-[13px] font-bold text-slate-800 mt-0.5 break-words">{value}</div>
                    </div>
                  ))}
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4 pt-4 mt-4 border-t border-slate-100">
                <div className="bg-sky-50 rounded-xl p-4">
                  <div className="text-[11px] font-extrabold text-sky-600 uppercase tracking-wider mb-1.5">Dịch vụ đăng ký</div>
                  <div className="text-[14px] font-bold text-slate-800">{examination.serviceName ?? "Khám tổng quát"}</div>
                </div>
                <div className="bg-amber-50 rounded-xl p-4">
                  <div className="text-[11px] font-extrabold text-amber-600 uppercase tracking-wider mb-1.5">Lý do đến khám</div>
                  <div className="text-[14px] font-bold text-slate-800">{examination.symptoms ?? "Không có"}</div>
                </div>
              </div>
            </div>
          </div>

          {/* Chẩn đoán */}
          {examination.diagnoses.length > 0 && (
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <CardHeader
                color="text-amber-600"
                title="Chẩn đoán"
                icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
              />
              <div className="p-5 flex flex-col gap-3">
                {examination.diagnoses.map(d => (
                  <div key={d.id} className="border border-amber-100 bg-amber-50/50 rounded-xl p-4">
                    <div className="text-[14px] font-bold text-slate-800">{d.description}</div>
                    {d.conclusion && (
                      <div className="text-[12px] font-medium text-slate-600 mt-1">
                        <span className="font-extrabold text-slate-400 uppercase text-[10px] tracking-wider">Kết luận: </span>
                        {d.conclusion}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Quá trình điều trị hôm nay */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-sky-600"
              title={`Quá trình điều trị ngày ${fmtDate(examination.appointmentDate)}`}
              icon="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"
            />
            <div className="px-5 py-2">
              {todayProgress.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-6">
                  Chưa ghi nhận quá trình điều trị nào trong buổi khám này.
                </p>
              ) : (
                <div className="divide-y divide-slate-100">
                  {todayProgress.map((row, idx) => (
                    <div key={idx} className="grid gap-3 py-3 items-start" style={{ gridTemplateColumns: "1fr 150px" }}>
                      <div>
                        <div className="text-[13.5px] font-bold text-slate-800">
                          {row.serviceName} <span className="text-slate-400">→</span> {row.stepName}
                        </div>
                        <div className="text-[12px] font-semibold text-sky-600 mt-0.5">
                          {row.stepNumber > 0 ? `${row.stepNumber}. ` : ""}{row.stepName} ({row.percent}%)
                        </div>
                        {row.note && <div className="text-[12px] italic text-slate-500 mt-0.5">{row.note}</div>}
                      </div>
                      <span className="text-[13px] font-semibold text-slate-600">{row.dentistName}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Đơn thuốc */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-violet-600"
              title="Đơn thuốc"
              icon="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5"
              action={
                <span className={`text-[11px] font-bold px-2.5 py-1 rounded-lg ${prescriptionItems.length > 0 ? "bg-violet-50 text-violet-700 border border-violet-200" : "bg-slate-100 text-slate-400"}`}>
                  {prescriptionItems.length} thuốc
                </span>
              }
            />
            <div className="px-5 py-2">
              {prescriptionItems.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-6">Chưa có đơn thuốc.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  {prescriptionItems.map((item, i) => (
                    <div key={item.id} className="py-3 flex items-start gap-3">
                      <div className="w-6 h-6 rounded-full bg-violet-100 text-violet-700 flex items-center justify-center text-[11px] font-black shrink-0 mt-0.5">{i + 1}</div>
                      <div className="flex-1 min-w-0">
                        <div className="text-[13px] font-black text-slate-900 leading-snug">{item.medicineName}</div>
                        <div className="text-[12px] font-semibold text-slate-500">
                          {item.dosage} × {item.quantity} {item.unit} · {item.usage}
                        </div>
                        {item.notes && <div className="text-[11.5px] italic text-slate-400 mt-0.5">{item.notes}</div>}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* ══════════ RIGHT 1/3: HẸN TÁI KHÁM ══════════ */}
        <div className="flex flex-col gap-6">
          {/* Lịch hẹn hiện tại */}
          {examination.followUpDate && (
            <div className="bg-white rounded-2xl border border-emerald-200 shadow-sm overflow-hidden">
              <CardHeader
                color="text-emerald-600"
                title="Đã hẹn tái khám"
                icon="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                action={
                  isInProgress && (
                    <button
                      onClick={() => void handleClear()}
                      disabled={saving}
                      className="text-[12px] font-bold text-red-500 hover:text-red-700 cursor-pointer disabled:opacity-50"
                    >
                      Hủy hẹn
                    </button>
                  )
                }
              />
              <div className="p-5">
                <div className="text-[20px] font-black text-emerald-700">{fmtDate(examination.followUpDate)}</div>
                {examination.followUpNote && (
                  <p className="text-[12.5px] font-semibold text-slate-500 mt-1.5">{examination.followUpNote}</p>
                )}
                <p className="text-[11.5px] font-semibold text-slate-400 mt-2">
                  Bệnh nhân sẽ nhận thông báo khi bác sĩ bấm &quot;Kết thúc điều trị&quot;.
                </p>
              </div>
            </div>
          )}

          {/* Đặt lịch hẹn tái khám */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-green-600"
              title={examination.followUpDate ? "Đổi lịch tái khám" : "Hẹn tái khám"}
              icon="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5"
            />
            <div className="p-5 flex flex-col gap-4">
              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Khám lại sau</label>
                <div className="grid grid-cols-2 gap-2 mt-1.5">
                  {PRESETS.map((p, i) => (
                    <button
                      key={p.label}
                      onClick={() => { setSelectedPreset(i); setCustomValue(""); }}
                      disabled={!isInProgress}
                      className={`px-3 py-2.5 rounded-xl border text-[12.5px] font-bold transition-all cursor-pointer disabled:cursor-not-allowed disabled:opacity-60 ${
                        selectedPreset === i
                          ? "bg-green-50 border-green-400 text-green-700"
                          : "bg-white border-slate-200 text-slate-600 hover:border-green-300 hover:bg-green-50/50"
                      }`}
                    >
                      {p.label}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Hoặc tùy chỉnh</label>
                <div className="flex gap-2 mt-1.5">
                  <span className="flex items-center px-3 text-[13px] font-bold text-slate-500 bg-slate-50 border border-slate-200 rounded-xl">Sau</span>
                  <input
                    type="number" min={1} placeholder="Số..."
                    value={customValue}
                    onChange={e => { setCustomValue(e.target.value); setSelectedPreset(null); }}
                    disabled={!isInProgress}
                    className="w-20 px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 disabled:opacity-60"
                  />
                  <select
                    value={customUnit}
                    onChange={e => setCustomUnit(e.target.value as "days" | "months")}
                    disabled={!isInProgress}
                    className="flex-1 px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 cursor-pointer disabled:opacity-60"
                  >
                    <option value="days">ngày</option>
                    <option value="months">tháng</option>
                  </select>
                </div>
              </div>

              {selectedDate && (
                <div className="bg-green-50 border border-green-200 rounded-xl px-4 py-3">
                  <div className="text-[11px] font-extrabold text-green-600 uppercase tracking-wider">Ngày tái khám dự kiến</div>
                  <div className="text-[17px] font-black text-green-700 mt-0.5">
                    {selectedDate.toLocaleDateString("vi-VN", { weekday: "long", day: "2-digit", month: "2-digit", year: "numeric" })}
                  </div>
                </div>
              )}

              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                <textarea
                  value={note}
                  onChange={e => setNote(e.target.value)}
                  rows={2}
                  placeholder="VD: Tái khám kiểm tra mão sứ..."
                  disabled={!isInProgress}
                  className="w-full mt-1.5 px-3.5 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 resize-none disabled:opacity-60"
                />
              </div>

              <button
                onClick={() => void handleSave()}
                disabled={!isInProgress || !selectedDate || saving}
                title={isInProgress ? undefined : "Chỉ hẹn tái khám được khi buổi hẹn đang khám"}
                className="w-full py-3 bg-green-600 text-white text-[14px] font-black rounded-xl hover:bg-green-700 transition-all shadow-sm shadow-green-500/25 disabled:bg-slate-200 disabled:text-slate-400 disabled:shadow-none disabled:cursor-not-allowed cursor-pointer"
              >
                {saving ? "Đang lưu..." : "Lưu lịch hẹn tái khám"}
              </button>

              <p className="text-[11.5px] font-semibold text-slate-400 leading-relaxed">
                Không đặt lịch hẹn mới cho bệnh nhân — chỉ lưu ngày cần khám lại. Khi bác sĩ bấm{" "}
                <span className="font-black text-slate-500">&quot;Kết thúc điều trị&quot;</span>, hệ thống sẽ gửi thông báo nhắc tái khám cho bệnh nhân.
              </p>
              {!isInProgress && (
                <p className="text-[11.5px] font-semibold text-amber-600 text-center">
                  Buổi hẹn chưa ở trạng thái đang khám — chỉ xem.
                </p>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Toast */}
      {toast && (
        <div className={`fixed bottom-6 right-6 z-[60] px-5 py-3.5 rounded-xl shadow-lg text-[13.5px] font-bold text-white ${toast.type === "success" ? "bg-emerald-600" : "bg-red-600"}`}>
          {toast.message}
        </div>
      )}
    </div>
  );
}
