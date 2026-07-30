"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { createPortal } from "react-dom";
import {
  getExaminationApi,
  getPatientMedicalHistoryApi,
  getPatientTreatmentPlansApi,
  getServicesApi,
  getServiceProceduresApi,
  createTreatmentPlanApi,
  updateTreatmentPlanApi,
  deleteTreatmentPlanApi,
  addTreatmentPlanProgressApi,
  updateTreatmentPlanProgressApi,
  reorderTreatmentPlanProgressApi,
  deleteTreatmentPlanProgressApi,
  type ExaminationDto,
  type PatientMedicalHistoryDto,
  type TreatmentPlanDto,
  type ServiceDto,
  type TreatmentProcedureDto,
} from "../../../../lib/apiClient";

interface TreatmentWorkspaceProps {
  appointmentId: string;
  /** Có onBack → hiển thị header riêng (dùng ở trang Phác đồ điều trị); không có → chế độ nhúng trong tab. */
  onBack?: () => void;
  /** Cho phép sửa dù buổi hẹn đã kết thúc (chế độ chỉnh sửa đơn hoàn thành). */
  editMode?: boolean;
}

const fmtMoney = (n: number) => n.toLocaleString("vi-VN") + "đ";

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
};

const fmtWarranty = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
};

const PLAN_STATUS: Record<string, { label: string; cls: string }> = {
  Planned:    { label: "Chờ thực hiện",  cls: "bg-slate-100 text-slate-600 border-slate-200" },
  InProgress: { label: "Đang thực hiện", cls: "bg-sky-50 text-sky-700 border-sky-200" },
  Completed:  { label: "Hoàn thành",     cls: "bg-emerald-50 text-emerald-700 border-emerald-200" },
  Cancelled:  { label: "Đã hủy",         cls: "bg-red-50 text-red-600 border-red-200" },
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

export default function TreatmentWorkspace({ appointmentId, onBack, editMode = false }: TreatmentWorkspaceProps) {
  const [examination, setExamination] = useState<ExaminationDto | null>(null);
  const [medicalHistory, setMedicalHistory] = useState<PatientMedicalHistoryDto[]>([]);
  const [plans, setPlans] = useState<TreatmentPlanDto[]>([]);
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [proceduresCache, setProceduresCache] = useState<Record<string, TreatmentProcedureDto[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);
  const showToast = (message: string, type: "success" | "error" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  const canEdit = examination?.status === "InProgress" || editMode;

  // ── Modal: thêm dịch vụ ────────────────────────────────────────────────────
  const [showAddService, setShowAddService] = useState(false);
  const [serviceSearch, setServiceSearch] = useState("");
  const [selService, setSelService] = useState<ServiceDto | null>(null);
  const [addQuantity, setAddQuantity] = useState(1);
  const [addTeeth, setAddTeeth] = useState("");
  const [addNotes, setAddNotes] = useState("");
  const [savingService, setSavingService] = useState(false);

  // ── Modal: thêm quá trình ──────────────────────────────────────────────────
  const [showAddProgress, setShowAddProgress] = useState(false);
  const [progressPlanId, setProgressPlanId] = useState("");
  const [progressSteps, setProgressSteps] = useState<TreatmentProcedureDto[]>([]);
  const [loadingSteps, setLoadingSteps] = useState(false);
  const [progressStepNumber, setProgressStepNumber] = useState<number | "">("");
  const [progressStepName, setProgressStepName] = useState("");
  const [progressPercent, setProgressPercent] = useState<number>(0);
  const [progressDate, setProgressDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [progressNote, setProgressNote] = useState("");
  const [savingProgress, setSavingProgress] = useState(false);
  // Bước ngoài quy trình chuẩn — bác sĩ tự nhập tên
  const [progressCustom, setProgressCustom] = useState(false);

  // ── Modal: sửa mục đã ghi trong nhật ký ────────────────────────────────────
  const [editEntry, setEditEntry] = useState<{ planId: string; entryIndex: number; stepName: string } | null>(null);
  const [editStepName, setEditStepName] = useState("");
  const [editPercent, setEditPercent] = useState<number>(0);
  const [editNote, setEditNote] = useState("");
  const [savingEdit, setSavingEdit] = useState(false);

  // ── Kéo-thả sắp xếp nhật ký điều trị (chỉ trong cùng một liệu trình) ────────
  const [dragKey, setDragKey] = useState<string | null>(null);      // `${planId}:${entryIndex}`
  const [dragOverKey, setDragOverKey] = useState<string | null>(null);

  const loadPlans = useCallback(async (patientId: string) => {
    try {
      setPlans(await getPatientTreatmentPlansApi(patientId));
    } catch {
      // giữ danh sách cũ nếu lỗi tạm thời
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const exam = await getExaminationApi(appointmentId);
        if (cancelled) return;
        setExamination(exam);
        setError(null);

        const patientId = exam.patient.id;
        const [history, planList, serviceList] = await Promise.all([
          getPatientMedicalHistoryApi(patientId).catch(() => []),
          getPatientTreatmentPlansApi(patientId).catch(() => []),
          getServicesApi({ status: "Active" }).catch(() => []),
        ]);
        if (cancelled) return;
        setMedicalHistory(history);
        setPlans(planList);
        setServices(serviceList);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Không thể tải thông tin bệnh nhân");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [appointmentId]);

  const loadProcedures = useCallback(async (serviceId: string) => {
    if (proceduresCache[serviceId]) return proceduresCache[serviceId];
    const steps = await getServiceProceduresApi(serviceId).catch(() => []);
    setProceduresCache(prev => ({ ...prev, [serviceId]: steps }));
    return steps;
  }, [proceduresCache]);

  // Buổi tái khám (staff check-in từ tab Tái khám) → hiển thị liệu trình của CHUỖI đơn được tái khám
  // (buổi gốc + các buổi tái khám trước trong chuỗi + buổi này) — không hiển thị dịch vụ của các lần khám khác.
  // Buổi khám thường → chỉ hiển thị liệu trình lập trong chính buổi này.
  const isFollowUpVisit = examination?.isFollowUpVisit ?? false;
  const chainIds = useMemo(() => {
    const ids = new Set(examination?.relatedAppointmentIds ?? []);
    ids.add(appointmentId);
    return ids;
  }, [examination?.relatedAppointmentIds, appointmentId]);
  const visiblePlans = useMemo(
    () => plans.filter(p => p.appointmentId != null && chainIds.has(p.appointmentId)),
    [plans, chainIds]
  );

  // Nhật ký điều trị: gộp stepProgress của các liệu trình đang hiển thị theo ĐÚNG thứ tự đã lưu
  // (không sort theo ngày để bác sĩ tự kéo-thả sắp xếp). entryIndex = vị trí gốc trong mảng của liệu trình.
  const progressRows = useMemo(() =>
    visiblePlans
      .flatMap(p => p.stepProgress.map((sp, entryIndex) => ({ ...sp, planId: p.id, entryIndex, serviceName: p.serviceName }))),
    [visiblePlans]
  );

  const activePlans = useMemo(() => visiblePlans.filter(p => p.status !== "Cancelled"), [visiblePlans]);
  const totalCost = useMemo(() => activePlans.reduce((sum, p) => sum + p.totalCost, 0), [activePlans]);

  // Các liệu trình đang thực hiện từ chuỗi đơn trước (hiện trong banner tái khám)
  const continuingPlans = useMemo(
    () => visiblePlans.filter(p => p.status === "InProgress" && p.appointmentId !== appointmentId),
    [visiblePlans, appointmentId]
  );

  const filteredServices = useMemo(() => {
    const q = serviceSearch.toLowerCase();
    return services.filter(s => s.name.toLowerCase().includes(q));
  }, [services, serviceSearch]);

  // ── Handlers ───────────────────────────────────────────────────────────────

  const handleAddService = async () => {
    if (!selService || !examination) return;
    try {
      setSavingService(true);
      await createTreatmentPlanApi(appointmentId, {
        serviceId: selService.id,
        quantity: Math.max(1, addQuantity),
        teeth: addTeeth.trim() || undefined,
        notes: addNotes.trim() || undefined,
      });
      showToast("Đã thêm dịch vụ vào liệu trình");
      setShowAddService(false);
      setSelService(null);
      setServiceSearch("");
      setAddQuantity(1);
      setAddTeeth("");
      setAddNotes("");
      await loadPlans(examination.patient.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể thêm dịch vụ", "error");
    } finally {
      setSavingService(false);
    }
  };

  const handleChangeStatus = async (plan: TreatmentPlanDto, status: string) => {
    // Hủy = loại dịch vụ khỏi liệu trình và khỏi tổng chi phí → chặn khi đã xuất hóa đơn.
    if (status === "Cancelled" && plan.isInvoiced) {
      showToast("Dịch vụ đã xuất hóa đơn — không thể hủy.", "error");
      return;
    }
    try {
      await updateTreatmentPlanApi({
        treatmentPlanId: plan.id,
        unitPrice: plan.unitPrice,
        quantity: plan.quantity,
        teeth: plan.teeth ?? undefined,
        notes: plan.notes ?? undefined,
        warrantyUntil: plan.warrantyUntil ?? undefined,
        status,
      });
      showToast("Đã cập nhật trạng thái");
      if (examination) await loadPlans(examination.patient.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể cập nhật trạng thái", "error");
    }
  };

  const handleDeletePlan = async (plan: TreatmentPlanDto) => {
    if (plan.isInvoiced) {
      showToast("Dịch vụ đã xuất hóa đơn — không thể xóa khỏi liệu trình.", "error");
      return;
    }
    if (!confirm(`Xóa dịch vụ "${plan.serviceName}" khỏi liệu trình?`)) return;
    try {
      await deleteTreatmentPlanApi(plan.id);
      showToast("Đã xóa dịch vụ");
      if (examination) await loadPlans(examination.patient.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể xóa dịch vụ", "error");
    }
  };

  const handleOpenProgress = async (planId?: string) => {
    const target = planId ?? activePlans[0]?.id ?? "";
    setProgressPlanId(target);
    setProgressStepNumber("");
    setProgressStepName("");
    setProgressPercent(0);
    setProgressDate(new Date().toISOString().slice(0, 10));
    setProgressNote("");
    setProgressCustom(false);
    setShowAddProgress(true);
    const plan = plans.find(p => p.id === target);
    if (plan) {
      setLoadingSteps(true);
      setProgressSteps(await loadProcedures(plan.serviceId));
      setLoadingSteps(false);
    } else {
      setProgressSteps([]);
    }
  };

  const handleProgressPlanChange = async (planId: string) => {
    setProgressPlanId(planId);
    setProgressStepNumber("");
    setProgressStepName("");
    setProgressPercent(0);
    setProgressCustom(false);
    const plan = plans.find(p => p.id === planId);
    if (plan) {
      setLoadingSteps(true);
      setProgressSteps(await loadProcedures(plan.serviceId));
      setLoadingSteps(false);
    } else {
      setProgressSteps([]);
    }
  };

  const handleAddProgress = async () => {
    if (!progressPlanId || !progressStepName.trim()) return;
    try {
      setSavingProgress(true);
      await addTreatmentPlanProgressApi(progressPlanId, {
        stepNumber: progressStepNumber === "" ? 0 : progressStepNumber,
        stepName: progressStepName.trim(),
        percent: progressPercent,
        date: progressDate || undefined,
        note: progressNote.trim() || undefined,
      });
      showToast("Đã ghi nhận quá trình điều trị");
      setShowAddProgress(false);
      if (examination) await loadPlans(examination.patient.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể ghi nhận quá trình", "error");
    } finally {
      setSavingProgress(false);
    }
  };

  const handleOpenEdit = (row: { planId: string; entryIndex: number; stepName: string; percent: number; note: string | null }) => {
    setEditEntry({ planId: row.planId, entryIndex: row.entryIndex, stepName: row.stepName });
    setEditStepName(row.stepName);
    setEditPercent(row.percent);
    setEditNote(row.note ?? "");
  };

  const handleSaveEdit = async () => {
    if (!editEntry) return;
    if (!editStepName.trim()) {
      showToast("Vui lòng nhập tên bước điều trị", "error");
      return;
    }
    try {
      setSavingEdit(true);
      await updateTreatmentPlanProgressApi(editEntry.planId, {
        entryIndex: editEntry.entryIndex,
        percent: editPercent,
        note: editNote.trim() || undefined,
        stepName: editStepName.trim(),
      });
      showToast("Đã cập nhật quá trình điều trị");
      setEditEntry(null);
      if (examination) await loadPlans(examination.patient.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể sửa quá trình", "error");
    } finally {
      setSavingEdit(false);
    }
  };

  const handleReorderDrop = async (src: string, target: { planId: string; entryIndex: number }) => {
    setDragKey(null);
    setDragOverKey(null);
    if (!src) return;
    const [srcPlanId, srcIdxStr] = src.split(":");
    const from = Number(srcIdxStr);
    // Chỉ đổi thứ tự trong cùng một liệu trình (mỗi liệu trình lưu nhật ký riêng)
    if (srcPlanId !== target.planId || from === target.entryIndex || Number.isNaN(from)) return;
    const plan = plans.find(p => p.id === target.planId);
    if (!plan) return;
    const n = plan.stepProgress.length;
    const order = Array.from({ length: n }, (_, i) => i);
    const [moved] = order.splice(from, 1);
    order.splice(target.entryIndex, 0, moved);
    try {
      await reorderTreatmentPlanProgressApi(target.planId, order);
      showToast("Đã đổi thứ tự quá trình điều trị");
      if (examination) await loadPlans(examination.patient.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể đổi thứ tự", "error");
    }
  };

  const handleDeleteProgress = async (row: { planId: string; entryIndex: number; stepName: string }) => {
    if (!confirm(`Xóa mục "${row.stepName}" khỏi quá trình điều trị?`)) return;
    try {
      await deleteTreatmentPlanProgressApi(row.planId, row.entryIndex);
      showToast("Đã xóa mục quá trình điều trị");
      if (examination) await loadPlans(examination.patient.id);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể xóa quá trình", "error");
    }
  };

  // ── Render ─────────────────────────────────────────────────────────────────

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
        {onBack && (
          <button onClick={onBack} className="px-5 py-2.5 bg-primary text-white text-[13px] font-bold rounded-xl cursor-pointer">
            Quay lại danh sách
          </button>
        )}
      </div>
    );
  }

  const pt = examination.patient;
  const initials = pt.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();

  return (
    <div className={onBack ? "flex-1 overflow-y-auto" : ""}>
      {/* Header (chỉ ở chế độ standalone — trang Phác đồ điều trị) */}
      {onBack && (
        <div className="sticky top-0 z-20 bg-white/95 backdrop-blur border-b border-slate-200/70 px-8 h-16 flex items-center gap-4">
          <button
            onClick={onBack}
            className="w-9 h-9 rounded-lg border border-slate-200 flex items-center justify-center text-slate-500 hover:bg-slate-50 cursor-pointer"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
            </svg>
          </button>
          <div className="flex-1 min-w-0">
            <div className="text-[15px] font-black text-slate-900 truncate">Liệu trình điều trị — {pt.fullName}</div>
            <div className="text-[12px] font-semibold text-slate-400">{examination.appointmentCode} · {fmtDate(examination.appointmentDate)}</div>
          </div>
          {!canEdit && (
            <span className="text-[12px] font-bold px-3 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-lg">
              Buổi hẹn chưa ở trạng thái đang khám — chỉ xem
            </span>
          )}
        </div>
      )}

      <div className={`grid gap-6 items-start ${onBack ? "p-8" : ""}`} style={{ gridTemplateColumns: "1fr 2fr" }}>
        {/* ══════════ LEFT 1/3: PATIENT INFO ══════════ */}
        <div className="flex flex-col gap-6">
          {/* Thông tin bệnh nhân */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-sky-600"
              title="Thông tin bệnh nhân"
              icon="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"
            />
            <div className="p-5">
              <div className="flex items-center gap-4 mb-4">
                <div className={`w-14 h-14 rounded-2xl flex items-center justify-center font-black text-lg border-2 shrink-0 ${pt.gender === "Nữ" ? "bg-rose-50 text-rose-500 border-rose-100" : "bg-sky-50 text-sky-600 border-sky-100"}`}>
                  {initials}
                </div>
                <div>
                  <div className="text-[15px] font-black text-slate-900">{pt.fullName}</div>
                  <div className="text-[12px] font-semibold text-slate-400">
                    {pt.dateOfBirth ? `${new Date(pt.dateOfBirth).toLocaleDateString("vi-VN")} (${new Date().getFullYear() - new Date(pt.dateOfBirth).getFullYear()} tuổi)` : "—"} · {pt.gender ?? "—"}
                  </div>
                </div>
              </div>
              <div className="grid grid-cols-1 gap-3">
                {[
                  ["Điện thoại", pt.phoneNumber ?? "—"],
                  ["Email", pt.email ?? "—"],
                  ["Địa chỉ", pt.address ?? "—"],
                ].map(([label, value]) => (
                  <div key={label} className="flex items-baseline justify-between gap-3">
                    <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider shrink-0">{label}</span>
                    <span className="text-[13px] font-bold text-slate-800 text-right break-all">{value}</span>
                  </div>
                ))}
              </div>
              <div className="grid grid-cols-1 gap-3 pt-4 mt-4 border-t border-slate-100">
                <div className="bg-sky-50 rounded-xl p-3.5">
                  <div className="text-[11px] font-extrabold text-sky-600 uppercase tracking-wider mb-1">Dịch vụ đăng ký</div>
                  <div className="text-[13.5px] font-bold text-slate-800">{examination.serviceName ?? "Khám tổng quát"}</div>
                </div>
                <div className="bg-amber-50 rounded-xl p-3.5">
                  <div className="text-[11px] font-extrabold text-amber-600 uppercase tracking-wider mb-1">Lý do đến khám</div>
                  <div className="text-[13.5px] font-bold text-slate-800">{examination.symptoms ?? "Không có"}</div>
                </div>
              </div>
            </div>
          </div>

          {/* Chẩn đoán của bác sĩ */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-amber-600"
              title="Chẩn đoán"
              icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
            <div className="p-5">
              {examination.diagnoses.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-4">Chưa có chẩn đoán ở buổi khám này.</p>
              ) : (
                <div className="flex flex-col gap-4">
                  {examination.diagnoses.map(d => (
                    <div key={d.id} className="border border-amber-100 bg-amber-50/50 rounded-xl p-4">
                      <div className="text-[14px] font-bold text-slate-800">{d.description}</div>
                      {[
                        ["Răng sâu", d.decayedTeeth],
                        ["Răng mòn / nứt / vỡ", d.wornOrBrokenTeeth],
                        ["Răng lung lay", d.looseTeeth],
                        ["Tình trạng lợi", d.gumCondition],
                        ["Tiền sử dị ứng", d.allergyHistory],
                      ].filter(([, v]) => v).map(([label, v]) => (
                        <div key={label} className="text-[12px] font-medium text-slate-600 mt-1.5">
                          <span className="font-extrabold text-slate-400 uppercase text-[10px] tracking-wider">{label}: </span>
                          {v}
                        </div>
                      ))}
                      {d.conclusion && (
                        <div className="text-[12px] font-medium text-slate-600 mt-1">
                          <span className="font-extrabold text-slate-400 uppercase text-[10px] tracking-wider">Kết quả & kế hoạch: </span>
                          {d.conclusion}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Lịch sử điều trị */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-violet-600"
              title="Lịch sử điều trị"
              icon="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"
              action={
                <span className="text-[11px] font-bold px-2.5 py-1 bg-violet-50 text-violet-700 rounded-lg border border-violet-200">
                  {medicalHistory.length} lần khám
                </span>
              }
            />
            <div className="p-5 max-h-[340px] overflow-y-auto">
              {medicalHistory.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-4">Chưa có lịch sử khám.</p>
              ) : (
                <div className="flex flex-col gap-3">
                  {medicalHistory.map(record => (
                    <div key={record.appointmentId} className="border border-slate-100 rounded-xl p-3.5">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-[11px] font-mono font-bold text-primary">{record.appointmentCode}</span>
                        <span className="text-[11px] text-slate-400 font-semibold">{fmtDate(record.appointmentDate)}</span>
                      </div>
                      <div className="text-[12px] font-semibold text-slate-600">{record.serviceName} · {record.dentistName}</div>
                      {record.diagnoses[0] && (
                        <div className="text-[12px] text-slate-500 mt-1">{record.diagnoses[0].description}</div>
                      )}
                      {record.treatmentPlans.length > 0 && (
                        <div className="flex flex-wrap gap-1.5 mt-1.5">
                          {record.treatmentPlans.map((tp, i) => (
                            <span key={i} className="text-[11px] font-semibold px-2 py-0.5 bg-sky-50 text-sky-700 rounded-lg border border-sky-100">
                              {tp.description}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* ══════════ RIGHT 2/3: TREATMENT ══════════ */}
        <div className="flex flex-col gap-6">
          {/* Banner tái khám: buổi hẹn do staff check-in từ tab Tái khám */}
          {isFollowUpVisit && (
            <div className="bg-indigo-50 border border-indigo-200 rounded-2xl px-5 py-4 flex items-start gap-3">
              <svg className="w-5 h-5 text-indigo-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99" />
              </svg>
              <div>
                <div className="text-[13.5px] font-black text-indigo-800">
                  Bệnh nhân đến tái khám
                </div>
                <div className="text-[12.5px] font-semibold text-indigo-600 mt-0.5">
                  {continuingPlans.length > 0
                    ? `${continuingPlans.map(p => p.serviceName).join(", ")} đang thực hiện. Ghi nhận bước tiếp theo ở mục "Quá trình điều trị" bên dưới.`
                    : "Xem lại liệu trình và quá trình điều trị trước đó ở các mục bên dưới."}
                </div>
              </div>
            </div>
          )}

          {/* Quá trình điều trị */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-sky-600"
              title="Quá trình điều trị"
              icon="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"
            />
            <div className="px-5 py-2">
              {progressRows.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-6">Chưa ghi nhận quá trình điều trị nào.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  <div className="grid gap-3 py-2.5 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider items-center" style={{ gridTemplateColumns: "24px 90px 1fr 160px 68px" }}>
                    <span />
                    <span>Ngày</span>
                    <span>Nội dung / điều trị</span>
                    <span>Bác sĩ</span>
                    <span />
                  </div>
                  {progressRows.map((row) => {
                    const rowKey = `${row.planId}:${row.entryIndex}`;
                    return (
                    <div
                      key={rowKey}
                      draggable={canEdit}
                      onDragStart={e => {
                        if (!canEdit) { e.preventDefault(); return; }
                        e.dataTransfer.setData("text/plain", rowKey);
                        e.dataTransfer.effectAllowed = "move";
                        setDragKey(rowKey);
                      }}
                      onDragEnd={() => { setDragKey(null); setDragOverKey(null); }}
                      onDragOver={e => {
                        if (!canEdit) return;
                        e.preventDefault(); // bắt buộc để onDrop có thể fire
                        e.dataTransfer.dropEffect = "move";
                        // Chỉ tô sáng khi cùng liệu trình (dùng cho hiển thị)
                        if (dragKey && dragKey.split(":")[0] === row.planId) setDragOverKey(rowKey);
                      }}
                      onDragLeave={() => setDragOverKey(k => (k === rowKey ? null : k))}
                      onDrop={e => {
                        e.preventDefault();
                        const src = e.dataTransfer.getData("text/plain");
                        void handleReorderDrop(src, { planId: row.planId, entryIndex: row.entryIndex });
                      }}
                      className={`grid gap-3 py-3 items-start transition-colors ${dragKey === rowKey ? "opacity-40" : ""} ${dragOverKey === rowKey ? "bg-sky-50" : ""}`}
                      style={{ gridTemplateColumns: "24px 90px 1fr 160px 68px" }}
                    >
                      <span
                        title={canEdit ? "Kéo để đổi thứ tự" : undefined}
                        className={`flex items-center justify-center pt-0.5 text-slate-300 ${canEdit ? "cursor-grab active:cursor-grabbing hover:text-slate-500" : "opacity-40"}`}
                      >
                        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                          <path d="M9 5a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM9 12a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM9 19a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM18 5a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM18 12a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM18 19a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0z" />
                        </svg>
                      </span>
                      <span className="text-[13px] font-bold text-slate-600 font-mono">{fmtDate(row.date)}</span>
                      <div>
                        <div className="text-[13.5px] font-bold text-slate-800">
                          {row.serviceName} <span className="text-slate-400">→</span>{" "}
                          <span className="text-sky-600">
                            {row.stepNumber > 0 ? `${row.stepNumber}. ` : ""}{row.stepName} ({row.percent}%)
                          </span>
                        </div>
                        {row.note && <div className="text-[12px] italic text-slate-500 mt-0.5">{row.note}</div>}
                      </div>
                      <span className="text-[13px] font-semibold text-slate-600">{row.dentistName}</span>
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => handleOpenEdit(row)}
                          disabled={!canEdit}
                          title={canEdit ? "Sửa tiến độ" : "Chỉ sửa được khi buổi khám đang diễn ra"}
                          className="w-7 h-7 rounded-lg flex items-center justify-center text-slate-300 hover:text-sky-600 hover:bg-sky-50 disabled:hover:bg-transparent disabled:hover:text-slate-200 disabled:text-slate-200 disabled:cursor-not-allowed cursor-pointer"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
                          </svg>
                        </button>
                        <button
                          onClick={() => void handleDeleteProgress(row)}
                          disabled={!canEdit}
                          title={canEdit ? "Xóa mục này" : "Chỉ xóa được khi buổi khám đang diễn ra"}
                          className="w-7 h-7 rounded-lg flex items-center justify-center text-slate-300 hover:text-red-500 hover:bg-red-50 disabled:hover:bg-transparent disabled:hover:text-slate-200 disabled:text-slate-200 disabled:cursor-not-allowed cursor-pointer"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                          </svg>
                        </button>
                      </div>
                    </div>
                    );
                  })}
                </div>
              )}
              <div className="py-3 border-t border-slate-100 flex justify-center">
                <button
                  onClick={() => void handleOpenProgress()}
                  disabled={activePlans.length === 0 || !canEdit}
                  title={canEdit ? undefined : "Bấm \"Bắt đầu khám\" để ghi nhận quá trình điều trị"}
                  className="flex items-center gap-1.5 text-[13px] font-bold text-primary hover:text-red-700 disabled:text-slate-300 disabled:cursor-not-allowed cursor-pointer"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  Thêm quá trình
                </button>
              </div>
            </div>
          </div>

          {/* Thủ thuật / Dịch vụ */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-primary"
              title="Thủ thuật / Dịch vụ"
              icon="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25z"
              action={
                <button
                  onClick={() => setShowAddService(true)}
                  disabled={!canEdit}
                  title={canEdit ? undefined : "Chỉ thêm được khi buổi hẹn đang khám"}
                  className="flex items-center gap-1.5 px-3 py-1.5 text-[12.5px] font-bold bg-primary text-white rounded-lg hover:bg-red-600 disabled:bg-slate-200 disabled:text-slate-400 disabled:cursor-not-allowed cursor-pointer"
                >
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  Thêm dịch vụ
                </button>
              }
            />
            <div className="px-5 py-2">
              {visiblePlans.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-6">Chưa có dịch vụ nào trong liệu trình.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  <div className="grid gap-3 py-2.5 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider items-center" style={{ gridTemplateColumns: "28px 1fr 130px 110px 40px 110px 32px" }}>
                    <span>#</span>
                    <span>Thủ thuật</span>
                    <span>Trạng thái</span>
                    <span className="text-right">Giá</span>
                    <span className="text-right">SL</span>
                    <span className="text-right">Thành tiền</span>
                    <span />
                  </div>
                  {visiblePlans.map((plan, idx) => {
                    const st = PLAN_STATUS[plan.status] ?? PLAN_STATUS.Planned;
                    return (
                      <div key={plan.id} className="py-3">
                        <div className="grid gap-3 items-center" style={{ gridTemplateColumns: "28px 1fr 130px 110px 40px 110px 32px" }}>
                          <span className="text-[13px] font-bold text-slate-400">{idx + 1}</span>
                          <span className="text-[14px] font-black text-slate-800">{plan.serviceName}</span>
                          <select
                            value={plan.status}
                            onChange={e => void handleChangeStatus(plan, e.target.value)}
                            className={`text-[11.5px] font-black px-2 py-1.5 rounded-lg border cursor-pointer focus:outline-none ${st.cls}`}
                          >
                            {Object.entries(PLAN_STATUS)
                              // Dịch vụ đã xuất hóa đơn không được hủy (trừ khi đang ở trạng thái đã hủy sẵn)
                              .filter(([value]) => value !== "Cancelled" || !plan.isInvoiced || plan.status === "Cancelled")
                              .map(([value, cfg]) => (
                                <option key={value} value={value}>{cfg.label}</option>
                              ))}
                          </select>
                          <span className="text-[13.5px] font-bold text-slate-700 text-right tabular-nums">{fmtMoney(plan.unitPrice)}</span>
                          <span className="text-[13.5px] font-bold text-slate-700 text-right tabular-nums">{plan.quantity}</span>
                          <span className="text-[14px] font-black text-slate-900 text-right tabular-nums">{fmtMoney(plan.totalCost)}</span>
                          <button
                            onClick={() => void handleDeletePlan(plan)}
                            disabled={plan.isInvoiced}
                            className="w-7 h-7 rounded-lg flex items-center justify-center text-slate-300 hover:text-red-500 hover:bg-red-50 disabled:text-slate-200 disabled:hover:text-slate-200 disabled:hover:bg-transparent disabled:cursor-not-allowed cursor-pointer"
                            title={plan.isInvoiced ? "Dịch vụ đã xuất hóa đơn — không thể xóa" : "Xóa dịch vụ"}
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                            </svg>
                          </button>
                        </div>
                        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 mt-1.5 pl-10 text-[12px] font-semibold text-slate-500">
                          <span>▸ Bác sĩ: {plan.dentistName}</span>
                          {plan.isInvoiced && (
                            <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-lg text-[11px] font-bold">
                              <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                              </svg>
                              Đã xuất hóa đơn — không thể xóa
                            </span>
                          )}
                          {plan.teeth && <span className="text-sky-600">▸ Răng: {plan.teeth}</span>}
                          {plan.warrantyUntil && (
                            <span className="text-emerald-600">🛡 BH đến {fmtWarranty(plan.warrantyUntil)}</span>
                          )}
                          {plan.amountPaid > 0 && (
                            <span className="text-violet-600">Đã thu: {fmtMoney(plan.amountPaid)}</span>
                          )}
                          {plan.notes && <span className="italic">▸ {plan.notes}</span>}
                        </div>
                      </div>
                    );
                  })}
                  {/* Tổng cộng */}
                  <div className="grid gap-3 py-3.5 items-center" style={{ gridTemplateColumns: "28px 1fr 130px 110px 40px 110px 32px" }}>
                    <span />
                    <span className="text-[13px] font-black text-slate-500 uppercase tracking-wider">Tổng cộng</span>
                    <span /><span /><span />
                    <span className="text-[16px] font-black text-primary text-right tabular-nums">{fmtMoney(totalCost)}</span>
                    <span />
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ══════════ MODAL: THÊM DỊCH VỤ ══════════ */}
      {showAddService && createPortal(
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-6" onClick={() => setShowAddService(false)}>
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[85vh] overflow-y-auto" onClick={e => e.stopPropagation()}>
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
              <span className="text-[15px] font-black text-slate-900">Thêm dịch vụ vào liệu trình</span>
              <button onClick={() => setShowAddService(false)} className="text-slate-400 hover:text-slate-600 cursor-pointer">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <div className="p-6 flex flex-col gap-4">
              <input
                type="text"
                placeholder="Tìm dịch vụ..."
                value={serviceSearch}
                onChange={e => setServiceSearch(e.target.value)}
                className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700"
              />
              <div className="max-h-56 overflow-y-auto border border-slate-100 rounded-xl divide-y divide-slate-50">
                {filteredServices.length === 0 ? (
                  <p className="text-[13px] font-semibold text-slate-400 text-center py-6">Không tìm thấy dịch vụ.</p>
                ) : filteredServices.map(s => (
                  <button
                    key={s.id}
                    onClick={() => setSelService(s)}
                    className={`w-full flex items-center justify-between px-4 py-3 text-left transition-colors cursor-pointer ${selService?.id === s.id ? "bg-primary/5" : "hover:bg-slate-50"}`}
                  >
                    <div>
                      <div className="text-[13.5px] font-bold text-slate-800">{s.name}</div>
                      <div className="text-[11.5px] font-semibold text-slate-400">{s.durationMinutes} phút</div>
                    </div>
                    <span className="text-[13.5px] font-black text-slate-900 tabular-nums">{fmtMoney(s.price)}</span>
                  </button>
                ))}
              </div>

              {selService && (
                <div className="bg-emerald-50 border border-emerald-200 rounded-xl p-4 flex flex-col gap-3">
                  <div className="flex items-center justify-between">
                    <span className="text-[13.5px] font-black text-emerald-800">{selService.name}</span>
                    <span className="text-[13.5px] font-black text-emerald-700 tabular-nums">{fmtMoney(selService.price)}</span>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Số lượng</label>
                      <input
                        type="number" inputMode="numeric" min={1} value={addQuantity || ""}
                        onChange={e => setAddQuantity(e.target.value === "" ? 0 : Math.max(0, Math.floor(Number(e.target.value) || 0)))}
                        onBlur={() => setAddQuantity(q => (q < 1 ? 1 : q))}
                        className="w-full mt-1 px-3 py-2 text-[13px] bg-white border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                      />
                    </div>
                    <div>
                      <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Răng</label>
                      <input
                        type="text" placeholder="VD: 21, 36" value={addTeeth}
                        onChange={e => setAddTeeth(e.target.value)}
                        className="w-full mt-1 px-3 py-2 text-[13px] bg-white border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                      />
                    </div>
                  </div>
                  <textarea
                    placeholder="Ghi chú (tùy chọn)..."
                    value={addNotes}
                    onChange={e => setAddNotes(e.target.value)}
                    rows={2}
                    className="w-full px-3 py-2 text-[13px] bg-white border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-semibold resize-none"
                  />
                </div>
              )}

              <button
                onClick={() => void handleAddService()}
                disabled={!selService || savingService}
                className="w-full py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 disabled:bg-slate-200 disabled:text-slate-400 disabled:cursor-not-allowed cursor-pointer"
              >
                {savingService ? "Đang lưu..." : "Thêm vào liệu trình"}
              </button>
            </div>
          </div>
        </div>,
        document.body
      )}

      {/* ══════════ MODAL: THÊM QUÁ TRÌNH ══════════ */}
      {showAddProgress && createPortal(
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-6" onClick={() => setShowAddProgress(false)}>
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[85vh] overflow-y-auto" onClick={e => e.stopPropagation()}>
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
              <span className="text-[15px] font-black text-slate-900">Ghi nhận quá trình điều trị</span>
              <button onClick={() => setShowAddProgress(false)} className="text-slate-400 hover:text-slate-600 cursor-pointer">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <div className="p-6 flex flex-col gap-4">
              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Dịch vụ / liệu trình</label>
                <select
                  value={progressPlanId}
                  onChange={e => void handleProgressPlanChange(e.target.value)}
                  className="w-full mt-1 px-3 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold cursor-pointer"
                >
                  {activePlans.map(p => (
                    <option key={p.id} value={p.id}>
                      {p.serviceName}{p.teeth ? ` — Răng ${p.teeth}` : ""}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Bước quy trình</label>
                {loadingSteps ? (
                  <p className="text-[12.5px] font-semibold text-slate-400 mt-2">Đang tải quy trình...</p>
                ) : progressSteps.length > 0 ? (
                  <div className="mt-1.5 flex flex-col gap-1.5">
                    {progressSteps.map(step => (
                      <button
                        key={step.id}
                        onClick={() => {
                          setProgressCustom(false);
                          setProgressStepNumber(step.stepNumber);
                          setProgressStepName(step.name);
                        }}
                        className={`flex items-center px-3.5 py-2.5 rounded-lg border text-left transition-colors cursor-pointer ${
                          !progressCustom && progressStepNumber === step.stepNumber
                            ? "bg-sky-50 border-sky-300 text-sky-800"
                            : "bg-white border-slate-200 text-slate-700 hover:bg-slate-50"
                        }`}
                      >
                        <span className="text-[13px] font-bold">{step.stepNumber}. {step.name}</span>
                      </button>
                    ))}

                    {/* Bước ngoài quy trình chuẩn — không phải ca nào cũng theo đúng quy trình */}
                    <button
                      onClick={() => {
                        setProgressCustom(true);
                        setProgressStepNumber("");
                        setProgressStepName("");
                      }}
                      className={`flex items-center gap-2 px-3.5 py-2.5 rounded-lg border border-dashed text-left transition-colors cursor-pointer ${
                        progressCustom
                          ? "bg-violet-50 border-violet-300 text-violet-800"
                          : "bg-white border-slate-300 text-slate-500 hover:bg-slate-50"
                      }`}
                    >
                      <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                      </svg>
                      <span className="text-[13px] font-bold">Khác — tự nhập bước điều trị</span>
                    </button>

                    {progressCustom && (
                      <input
                        type="text" autoFocus
                        placeholder="Nhập nội dung điều trị (VD: Xử lý viêm lợi phát sinh)"
                        value={progressStepName}
                        onChange={e => setProgressStepName(e.target.value)}
                        className="w-full px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                      />
                    )}
                  </div>
                ) : (
                  <input
                    type="text" placeholder="Tên bước (dịch vụ chưa có quy trình)"
                    value={progressStepName}
                    onChange={e => setProgressStepName(e.target.value)}
                    className="w-full mt-1.5 px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                  />
                )}
              </div>

              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Tiến độ bước này (%)</label>
                <div className="relative mt-1.5">
                  <input
                    type="number" min={0} max={100} placeholder="VD: 30"
                    value={progressPercent || ""}
                    onChange={e => setProgressPercent(Math.min(100, Math.max(0, Number(e.target.value))))}
                    className="w-full px-3 py-2.5 pr-8 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                  />
                  <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-slate-400">%</span>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Ngày thực hiện</label>
                  <input
                    type="date" value={progressDate}
                    onChange={e => setProgressDate(e.target.value)}
                    className="w-full mt-1 px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                  />
                </div>
                <div>
                  <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                  <input
                    type="text" placeholder="Tùy chọn..." value={progressNote}
                    onChange={e => setProgressNote(e.target.value)}
                    className="w-full mt-1 px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-semibold"
                  />
                </div>
              </div>

              <button
                onClick={() => void handleAddProgress()}
                disabled={!progressPlanId || !progressStepName.trim() || savingProgress}
                className="w-full py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 disabled:bg-slate-200 disabled:text-slate-400 disabled:cursor-not-allowed cursor-pointer"
              >
                {savingProgress ? "Đang lưu..." : "Ghi nhận"}
              </button>
            </div>
          </div>
        </div>,
        document.body
      )}

      {/* ══════════ MODAL: SỬA TIẾN ĐỘ ĐÃ GHI ══════════ */}
      {editEntry && createPortal(
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-6" onClick={() => setEditEntry(null)}>
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md" onClick={e => e.stopPropagation()}>
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
              <span className="text-[15px] font-black text-slate-900">Sửa tiến độ</span>
              <button onClick={() => setEditEntry(null)} className="text-slate-400 hover:text-slate-600 cursor-pointer">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <div className="p-6 flex flex-col gap-4">
              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Bước điều trị</label>
                <input
                  type="text" placeholder="Tên bước..." value={editStepName}
                  onChange={e => setEditStepName(e.target.value)}
                  className="w-full mt-1.5 px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                />
              </div>

              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Tiến độ hoàn thành (%)</label>
                <div className="relative mt-1.5">
                  <input
                    type="number" min={0} max={100} autoFocus
                    value={editPercent || ""}
                    onChange={e => setEditPercent(Math.min(100, Math.max(0, Number(e.target.value))))}
                    className="w-full px-3 py-2.5 pr-8 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-bold"
                  />
                  <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[13px] font-bold text-slate-400">%</span>
                </div>
              </div>

              <div>
                <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                <input
                  type="text" placeholder="Tùy chọn..." value={editNote}
                  onChange={e => setEditNote(e.target.value)}
                  className="w-full mt-1.5 px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-semibold"
                />
              </div>

              <button
                onClick={() => void handleSaveEdit()}
                disabled={savingEdit}
                className="w-full py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 disabled:bg-slate-200 disabled:text-slate-400 disabled:cursor-not-allowed cursor-pointer"
              >
                {savingEdit ? "Đang lưu..." : "Lưu thay đổi"}
              </button>
            </div>
          </div>
        </div>,
        document.body
      )}

      {/* Toast */}
      {toast && (
        <div className={`fixed bottom-6 right-6 z-[60] px-5 py-3.5 rounded-xl shadow-lg text-[13.5px] font-bold text-white ${toast.type === "success" ? "bg-emerald-600" : "bg-red-600"}`}>
          {toast.message}
        </div>
      )}
    </div>
  );
}
