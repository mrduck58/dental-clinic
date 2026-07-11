"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import {
  getExaminationApi,
  getPatientTreatmentPlansApi,
  getMedicinesApi,
  createPrescriptionApi,
  addPrescriptionItemApi,
  deletePrescriptionItemApi,
  type ExaminationDto,
  type TreatmentPlanDto,
  type MedicineDto,
  type PrescriptionDto,
} from "../../../../lib/apiClient";

interface PrescriptionWorkspaceProps {
  appointmentId: string;
}

const COMMON_MEDS = [
  "Amoxicillin 500mg", "Amoxicillin + Clavulanate 875mg", "Metronidazole 250mg",
  "Ibuprofen 400mg", "Paracetamol 500mg", "Diclofenac 50mg",
  "Chlorhexidine 0.12%", "Povidone-Iodine 10%", "Nước muối sinh lý 0.9%",
  "Prednisolone 5mg", "Dexamethasone 0.5mg", "Tramadol 50mg",
];
const DOSAGES     = ["1/2 viên", "1 viên", "2 viên", "3 viên", "5ml", "10ml", "15ml", "1 gói", "1 ống", "2 lần súc miệng"];
const FREQUENCIES = ["1 lần/ngày", "2 lần/ngày", "3 lần/ngày", "4 lần/ngày", "Khi đau", "Trước ăn", "Sau ăn", "Sáng & tối"];
const DURATIONS   = ["3 ngày", "5 ngày", "7 ngày", "10 ngày", "14 ngày", "1 tháng"];
const QUICK_NOTES = ["Uống sau ăn", "Uống trước ăn 30 phút", "Uống với nhiều nước", "Súc miệng 30 giây rồi nhổ", "Bôi vào vùng điều trị"];

const KHAC = "Khác";

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
};

// So sánh theo ngày local (yyyy-MM-dd)
const dayKey = (iso: string) => {
  const d = new Date(iso);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
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

const inputCls = "w-full px-3.5 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";
const labelCls = "text-[11px] font-extrabold text-slate-500 uppercase tracking-wider";

export default function PrescriptionWorkspace({ appointmentId }: PrescriptionWorkspaceProps) {
  const [examination, setExamination] = useState<ExaminationDto | null>(null);
  const [prescription, setPrescription] = useState<PrescriptionDto | null>(null);
  const [plans, setPlans] = useState<TreatmentPlanDto[]>([]);
  const [medicines, setMedicines] = useState<MedicineDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);
  const showToast = (message: string, type: "success" | "error" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  // Form kê đơn
  const [medName, setMedName] = useState("");
  const [customName, setCustomName] = useState("");
  const [dosage, setDosage] = useState(DOSAGES[1]);
  const [frequency, setFrequency] = useState(FREQUENCIES[2]);
  const [duration, setDuration] = useState(DURATIONS[2]);
  const [quantity, setQuantity] = useState(1);
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  const isInProgress = examination?.status === "InProgress";

  const reloadPrescription = useCallback(async () => {
    try {
      const exam = await getExaminationApi(appointmentId);
      setExamination(exam);
      setPrescription(exam.prescription ?? null);
    } catch {
      // giữ dữ liệu cũ nếu lỗi tạm thời
    }
  }, [appointmentId]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const exam = await getExaminationApi(appointmentId);
        if (cancelled) return;
        setExamination(exam);
        setPrescription(exam.prescription ?? null);
        setError(null);

        const [planList, medsList] = await Promise.all([
          getPatientTreatmentPlansApi(exam.patient.id).catch(() => []),
          getMedicinesApi({ status: "active" }).catch(() => []),
        ]);
        if (cancelled) return;
        setPlans(planList);
        setMedicines(medsList);
        setMedName(medsList[0]?.name ?? COMMON_MEDS[0]);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Không thể tải thông tin bệnh nhân");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [appointmentId]);

  const medicineOptions = medicines.length > 0 ? medicines.map(m => m.name) : COMMON_MEDS;

  // Quá trình điều trị của ngày hẹn này
  const todayProgress = useMemo(() => {
    if (!examination) return [];
    const key = dayKey(examination.appointmentDate);
    return plans
      .flatMap(p => p.stepProgress.map(sp => ({ ...sp, serviceName: p.serviceName })))
      .filter(sp => dayKey(sp.date) === key)
      .sort((a, b) => a.stepNumber - b.stepNumber);
  }, [plans, examination]);

  const canAdd = (medName !== KHAC ? medName.trim() !== "" : customName.trim() !== "") && quantity > 0;

  const handleAddItem = async () => {
    if (!canAdd) return;
    const name = medName === KHAC ? customName.trim() : medName;
    const unit = medicines.find(m => m.name === name)?.unit || "viên";
    try {
      setSaving(true);
      let prescriptionId = prescription?.id;
      if (!prescriptionId) {
        const created = await createPrescriptionApi(appointmentId, { notes: "" });
        prescriptionId = created.id;
      }
      await addPrescriptionItemApi({
        prescriptionId,
        medicineName: name,
        dosage,
        quantity,
        unit,
        usage: `${frequency}, ${duration}`,
        notes: note.trim() || undefined,
      });
      showToast("Đã thêm thuốc vào đơn");
      setCustomName("");
      setNote("");
      setQuantity(1);
      await reloadPrescription();
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể thêm thuốc", "error");
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteItem = async (itemId: string) => {
    try {
      await deletePrescriptionItemApi(itemId);
      showToast("Đã xóa thuốc khỏi đơn");
      setPrescription(prev => prev ? { ...prev, items: prev.items.filter(i => i.id !== itemId) } : null);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể xóa thuốc", "error");
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
  const items = prescription?.items ?? [];

  return (
    <div>
      <div className="grid gap-6 items-start" style={{ gridTemplateColumns: "2fr 1fr" }}>
        {/* ══════════ LEFT 2/3: PATIENT INFO + TODAY PROGRESS ══════════ */}
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

          {/* Chẩn đoán buổi khám */}
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
                  <div className="grid gap-3 py-2.5 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider" style={{ gridTemplateColumns: "1fr 150px" }}>
                    <span>Nội dung / điều trị</span>
                    <span>Bác sĩ</span>
                  </div>
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
        </div>

        {/* ══════════ RIGHT 1/3: PRESCRIPTION ══════════ */}
        <div className="flex flex-col gap-6">
          {/* Đơn thuốc hiện tại */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-violet-600"
              title="Đơn thuốc"
              icon="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5"
              action={
                <span className={`text-[11px] font-bold px-2.5 py-1 rounded-lg ${items.length > 0 ? "bg-violet-50 text-violet-700 border border-violet-200" : "bg-slate-100 text-slate-400"}`}>
                  {items.length} thuốc
                </span>
              }
            />
            <div className="px-5 py-2">
              {items.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-6">Đơn thuốc trống.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  {items.map((item, i) => (
                    <div key={item.id} className="py-3 flex items-start gap-3">
                      <div className="w-6 h-6 rounded-full bg-violet-100 text-violet-700 flex items-center justify-center text-[11px] font-black shrink-0 mt-0.5">{i + 1}</div>
                      <div className="flex-1 min-w-0">
                        <div className="text-[13px] font-black text-slate-900 leading-snug">{item.medicineName}</div>
                        <div className="text-[12px] font-semibold text-slate-500">
                          {item.dosage} × {item.quantity} {item.unit} · {item.usage}
                        </div>
                        {item.notes && <div className="text-[11.5px] italic text-slate-400 mt-0.5">{item.notes}</div>}
                      </div>
                      {isInProgress && (
                        <button
                          onClick={() => void handleDeleteItem(item.id)}
                          className="p-1 rounded-lg hover:bg-red-50 text-slate-300 hover:text-red-500 transition-all cursor-pointer shrink-0 mt-0.5"
                          title="Xóa thuốc"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                          </svg>
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Form kê thuốc */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-violet-600"
              title="Kê thuốc"
              icon="M12 4.5v15m7.5-7.5h-15"
            />
            <div className="p-5 flex flex-col gap-3.5">
              <div className="flex flex-col gap-1.5">
                <label className={labelCls}>Tên thuốc</label>
                <select
                  value={medName}
                  onChange={e => setMedName(e.target.value)}
                  disabled={!isInProgress}
                  className={inputCls + " cursor-pointer disabled:cursor-not-allowed disabled:opacity-60"}
                >
                  {medicineOptions.map(m => <option key={m} value={m}>{m}</option>)}
                  <option value={KHAC}>{KHAC}</option>
                </select>
                {medName === KHAC && (
                  <input
                    value={customName}
                    onChange={e => setCustomName(e.target.value)}
                    placeholder="Nhập tên thuốc..."
                    className={inputCls}
                  />
                )}
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="flex flex-col gap-1.5">
                  <label className={labelCls}>Liều dùng</label>
                  <input list="rx-dosages" value={dosage} onChange={e => setDosage(e.target.value)} disabled={!isInProgress} className={inputCls + " disabled:opacity-60"} />
                  <datalist id="rx-dosages">{DOSAGES.map(d => <option key={d} value={d} />)}</datalist>
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className={labelCls}>Số lượng</label>
                  <input
                    type="number" min={1} value={quantity}
                    onChange={e => setQuantity(Math.max(1, Number(e.target.value)))}
                    disabled={!isInProgress}
                    className={inputCls + " disabled:opacity-60"}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className={labelCls}>Tần suất</label>
                  <input list="rx-frequencies" value={frequency} onChange={e => setFrequency(e.target.value)} disabled={!isInProgress} className={inputCls + " disabled:opacity-60"} />
                  <datalist id="rx-frequencies">{FREQUENCIES.map(f => <option key={f} value={f} />)}</datalist>
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className={labelCls}>Thời gian</label>
                  <input list="rx-durations" value={duration} onChange={e => setDuration(e.target.value)} disabled={!isInProgress} className={inputCls + " disabled:opacity-60"} />
                  <datalist id="rx-durations">{DURATIONS.map(d => <option key={d} value={d} />)}</datalist>
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <label className={labelCls}>Hướng dẫn sử dụng</label>
                <div className="flex flex-wrap gap-1.5">
                  {QUICK_NOTES.map(hint => (
                    <button
                      key={hint} type="button"
                      onClick={() => setNote(n => n ? `${n}. ${hint}` : hint)}
                      disabled={!isInProgress}
                      className="px-2 py-1 text-[10.5px] font-semibold bg-slate-100 text-slate-500 rounded-lg hover:bg-violet-100 hover:text-violet-700 transition-all cursor-pointer disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      + {hint}
                    </button>
                  ))}
                </div>
                <textarea
                  value={note}
                  onChange={e => setNote(e.target.value)}
                  rows={2}
                  placeholder="Hoặc nhập hướng dẫn..."
                  disabled={!isInProgress}
                  className={inputCls + " resize-none disabled:opacity-60"}
                />
              </div>

              <button
                onClick={() => void handleAddItem()}
                disabled={!isInProgress || !canAdd || saving}
                title={isInProgress ? undefined : "Chỉ kê đơn được khi buổi hẹn đang khám"}
                className="w-full py-2.5 bg-violet-600 text-white text-[13.5px] font-black rounded-xl hover:bg-violet-700 transition-all shadow-sm shadow-violet-500/25 disabled:bg-slate-200 disabled:text-slate-400 disabled:shadow-none disabled:cursor-not-allowed cursor-pointer"
              >
                {saving ? "Đang lưu..." : "Thêm vào đơn"}
              </button>
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
