"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import DentistSidebar from "../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../components/shared/DentistPageHeader";
import ToothArchDiagram, { TOOTH_COLOR, TOOTH_LEGEND, UPPER_TEETH, LOWER_TEETH, ARCH_H, type ToothStatus as TS, type ToothState as TState } from "../../../../components/shared/ToothArchDiagram";
import { useRequireDentist } from "../../../../hooks/useRequireDentist";
import {
  getExaminationApi,
  startTreatmentApi,
  endTreatmentApi,
  createDiagnosisApi,
  updateDiagnosisApi,
  deleteDiagnosisApi,
  addPrescriptionItemApi,
  deletePrescriptionItemApi,
  createFollowUpApi,
  getFollowUpsApi,
  deleteFollowUpApi,
  getMedicinesApi,
  deleteTreatmentPlanApi,
  getTreatmentCourseApi,
  getPatientMedicalHistoryApi,
  type ExaminationDto,
  type DiagnosisDto,
  type TreatmentPlanDto,
  type PrescriptionDto,
  type FollowUpAppointmentDto,
  type MedicineDto,
  type TreatmentCourseDto,
  type PatientMedicalHistoryDto,
} from "../../../../lib/apiClient";

type ToothStatus = TS;
type ToothState = TState;
type TreatmentStatus = "pending" | "in_progress" | "done";

type TabId = "diagnosis" | "treatment" | "prescription" | "followup";

const TABS: { id: TabId; label: string; icon: string; color: string; activeColor: string }[] = [
  { id: "diagnosis",    label: "Chuẩn đoán",  icon: "M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z",                    color: "text-amber-600",   activeColor: "bg-amber-50 border-amber-200 text-amber-700" },
  { id: "treatment",   label: "Liệu trình",   icon: "M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z", color: "text-red-600",    activeColor: "bg-red-50 border-red-200 text-red-700" },
  { id: "prescription", label: "Đơn thuốc",  icon: "M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5", color: "text-violet-600", activeColor: "bg-violet-50 border-violet-200 text-violet-700" },
  { id: "followup",    label: "Tái khám",   icon: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z", color: "text-green-600",   activeColor: "bg-green-50 border-green-200 text-green-700" },
];

const TX_STATUS: Record<TreatmentStatus, { label: string; cls: string }> = {
  pending:     { label: "Chờ thực hiện",  cls: "bg-slate-100 text-slate-600 border border-slate-200" },
  in_progress: { label: "Đang thực hiện", cls: "bg-sky-50 text-sky-700 border border-sky-100" },
  done:        { label: "Hoàn thành",     cls: "bg-green-50 text-green-700 border border-green-100" },
};

const STATUS_LABEL: Record<string, { label: string; cls: string }> = {
  Pending:      { label: "Chờ xác nhận", cls: "bg-slate-100 text-slate-600 border border-slate-200" },
  Confirmed:    { label: "Đã xác nhận", cls: "bg-blue-50 text-blue-700 border border-blue-200" },
  CheckedIn:    { label: "Đã check-in", cls: "bg-amber-50 text-amber-700 border border-amber-200" },
  InProgress:   { label: "Đang khám",  cls: "bg-sky-50 text-sky-700 border border-sky-200" },
  PendingPayment: { label: "Chờ thanh toán", cls: "bg-orange-50 text-orange-700 border border-orange-200" },
  Completed:    { label: "Hoàn thành",  cls: "bg-green-50 text-green-700 border border-green-200" },
  Cancelled:    { label: "Đã hủy",     cls: "bg-red-50 text-red-700 border border-red-200" },
};

function SectionHeading({ color, title, icon, action }: {
  color: string; title: string; icon: string; action?: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
      <div className="flex items-center gap-3">
        <div className={`w-8 h-8 rounded-xl ${color} flex items-center justify-center shrink-0`}>
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
          </svg>
        </div>
        <span className="text-[15px] font-black text-slate-900">{title}</span>
      </div>
      {action}
    </div>
  );
}

export default function PatientDetailPage() {
  useRequireDentist();
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const [examination, setExamination] = useState<ExaminationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabId>("diagnosis");

  // Tooth diagram state
  const [teeth, setTeeth] = useState<ToothState>({});
  const [selectedTooth, setSel] = useState<string | null>(null);
  
  // Diagnosis state
  const [complaint, setComplaint] = useState("");
  const [findings, setFindings] = useState("");
  const [diagnosis, setDiagnosis] = useState("");
  const [diagSaved, setDiagSaved] = useState(false);

  // New diagnosis fields
  const [heartRate, setHeartRate] = useState<string>("");
  const [temperature, setTemperature] = useState<string>("");
  const [bloodPressureSystolic, setBloodPressureSystolic] = useState<string>("");
  const [bloodPressureDiastolic, setBloodPressureDiastolic] = useState<string>("");
  const [medicalHistoryInput, setMedicalHistoryInput] = useState<string>("");
  const [allergyHistory, setAllergyHistory] = useState<string>("");
  const [dentalCondition, setDentalCondition] = useState<string>("");
  const [conclusion, setConclusion] = useState<string>("");
  
  // Treatment steps from API
  const [treatmentPlans, setTreatmentPlans] = useState<TreatmentPlanDto[]>([]);

  // Liệu trình dài hạn (course) — chỉ hiển thị; việc tạo nằm ở trang chọn dịch vụ.
  const [course, setCourse] = useState<TreatmentCourseDto | null>(null);

  // Prescription from API
  const [prescription, setPrescription] = useState<PrescriptionDto | null>(null);
  
  // Medicines list from API
  const [medicines, setMedicines] = useState<MedicineDto[]>([]);
  
  // Follow-ups from API
  const [followUps, setFollowUps] = useState<FollowUpAppointmentDto[]>([]);

  // Patient medical history
  const [medicalHistory, setMedicalHistory] = useState<PatientMedicalHistoryDto[]>([]);

  // Toast notification
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" | "info" } | null>(null);

  const showToast = (message: string, type: "success" | "error" | "info" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  // Load examination data
  const loadExamination = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getExaminationApi(id);
      setExamination(data);
      setComplaint(data.symptoms ?? "");
      setTreatmentPlans(data.treatmentPlans ?? []);
      setPrescription(data.prescription ?? null);
      try { setCourse(await getTreatmentCourseApi(id)); } catch { /* ignore */ }
      setError(null);

      // Load diagnosis data into form fields
      const existingDiagnosis = data.diagnoses[0];
      if (existingDiagnosis) {
        setDiagnosis(existingDiagnosis.description ?? "");
        setFindings(existingDiagnosis.notes ?? "");
        setHeartRate(existingDiagnosis.heartRate?.toString() ?? "");
        setTemperature(existingDiagnosis.temperature?.toString() ?? "");
        setBloodPressureSystolic(existingDiagnosis.bloodPressureSystolic?.toString() ?? "");
        setBloodPressureDiastolic(existingDiagnosis.bloodPressureDiastolic?.toString() ?? "");
        setMedicalHistoryInput(existingDiagnosis.medicalHistory ?? "");
        setAllergyHistory(existingDiagnosis.allergyHistory ?? "");
        setDentalCondition(existingDiagnosis.dentalCondition ?? "");
        setConclusion(existingDiagnosis.conclusion ?? "");
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải thông tin khám");
    } finally {
      setLoading(false);
    }
  }, [id]);

  // Load patient medical history
  const loadMedicalHistory = useCallback(async () => {
    if (!examination?.patient?.id) return;
    try {
      const history = await getPatientMedicalHistoryApi(examination.patient.id);
      setMedicalHistory(history);
    } catch {
      // Silently fail for medical history
    }
  }, [examination?.patient?.id]);

  // Load follow-ups
  const loadFollowUps = useCallback(async () => {
    try {
      const data = await getFollowUpsApi(id);
      setFollowUps(data);
    } catch {
      // Silently fail for follow-ups
    }
  }, [id]);

  useEffect(() => {
    void loadExamination();
    void loadFollowUps();
    void getMedicinesApi().then(setMedicines).catch(() => {});
  }, [loadExamination, loadFollowUps]);

  // Load medical history when examination data is available
  useEffect(() => {
    void loadMedicalHistory();
  }, [loadMedicalHistory]);

  // Start treatment if needed
  const handleStartTreatment = async () => {
    try {
      await startTreatmentApi(id);
      showToast("Đã bắt đầu khám!", "success");
      await loadExamination();
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Bắt đầu khám thất bại", "error");
    }
  };

  // End treatment
  const handleEndTreatment = async () => {
    try {
      await endTreatmentApi(id);
      showToast("Đã kết thúc điều trị. Chuyển sang chờ thanh toán!", "success");
      await loadExamination();
      setTimeout(() => router.push("/dentist/patients"), 1500);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Kết thúc điều trị thất bại", "error");
    }
  };

  // Save diagnosis
  const handleSaveDiagnosis = async () => {
    if (!examination) return;
    try {
      const diagnosisData = {
        diagnosisCode: "D01",
        description: diagnosis,
        notes: findings,
        heartRate: heartRate ? parseFloat(heartRate) : undefined,
        temperature: temperature ? parseFloat(temperature) : undefined,
        bloodPressureSystolic: bloodPressureSystolic ? parseFloat(bloodPressureSystolic) : undefined,
        bloodPressureDiastolic: bloodPressureDiastolic ? parseFloat(bloodPressureDiastolic) : undefined,
        medicalHistory: medicalHistoryInput || undefined,
        allergyHistory: allergyHistory || undefined,
        dentalCondition: dentalCondition || undefined,
        conclusion: conclusion || undefined,
      };

      const existingDiagnosisId = examination.diagnoses[0]?.id;

      if (existingDiagnosisId) {
        // Try to update existing diagnosis
        try {
          await updateDiagnosisApi({
            diagnosisId: existingDiagnosisId,
            ...diagnosisData,
          });
        } catch (updateErr) {
          // If update fails (e.g., diagnosis not found), create new one
          console.warn("Update failed, creating new diagnosis:", updateErr);
          await createDiagnosisApi(id, diagnosisData);
        }
      } else {
        // Create new diagnosis
        await createDiagnosisApi(id, diagnosisData);
      }

      setDiagSaved(true);
      showToast("Đã lưu tất cả thông tin khám!", "success");

      // Update form state immediately to reflect saved data
      setDiagnosis(diagnosis);
      setFindings(findings);
      setHeartRate(heartRate);
      setTemperature(temperature);
      setBloodPressureSystolic(bloodPressureSystolic);
      setBloodPressureDiastolic(bloodPressureDiastolic);
      setMedicalHistoryInput(medicalHistoryInput);
      setAllergyHistory(allergyHistory);
      setDentalCondition(dentalCondition);

      // Also reload to sync with server data
      await loadExamination();
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Lưu thất bại", "error");
    }
  };

  // Add prescription item
  const handleAddPrescriptionItem = async () => {
    if (!prescription) return;
    try {
      await addPrescriptionItemApi({
        prescriptionId: prescription.id,
        medicineName: "Thuốc mới",
        dosage: "500mg",
        quantity: 1,
        unit: "viên",
        usage: "Theo chỉ định",
      });
      showToast("Đã thêm thuốc!", "success");
      await loadExamination();
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Thêm thuốc thất bại", "error");
    }
  };

  // Delete prescription item
  const handleDeletePrescriptionItem = async (itemId: string) => {
    try {
      await deletePrescriptionItemApi(itemId);
      showToast("Đã xóa thuốc!", "success");
      // Update state directly instead of reloading the whole page
      setPrescription(prev => prev ? {
        ...prev,
        items: prev.items.filter(i => i.id !== itemId)
      } : null);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Xóa thuốc thất bại", "error");
    }
  };

  // Delete treatment plan
  const handleDeleteTreatmentPlan = async (planId: string) => {
    try {
      await deleteTreatmentPlanApi(planId);
      showToast("Đã xóa liệu trình!", "success");
      // Update state directly instead of reloading the whole page
      setTreatmentPlans(prev => prev.filter(tp => tp.id !== planId));
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Xóa liệu trình thất bại", "error");
    }
  };

  // Create follow-up
  const handleCreateFollowUp = async () => {
    try {
      const nextWeek = new Date();
      nextWeek.setDate(nextWeek.getDate() + 7);
      await createFollowUpApi(id, {
        appointmentDate: nextWeek.toISOString(),
        symptoms: "Tái khám theo dõi",
      });
      showToast("Đã tạo lịch tái khám!", "success");
      await loadFollowUps();
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Tạo lịch tái khám thất bại", "error");
    }
  };

  // Delete follow-up
  const handleDeleteFollowUp = async (followUpId: string) => {
    try {
      await deleteFollowUpApi(followUpId);
      showToast("Đã xóa lịch tái khám!", "success");
      await loadFollowUps();
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Xóa lịch tái khám thất bại", "error");
    }
  };

  const setToothStatus = (tooth: string, status: ToothStatus) => {
    setTeeth((p) => ({ ...p, [tooth]: status }));
    setSel(null);
  };

  const totalCost = treatmentPlans.reduce((sum, tp) => sum + (tp.estimatedCost ?? 0), 0);

  if (loading) {
    return (
      <div className="flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <DentistSidebar activeMenu="patients" />
        <main className="flex-1 flex items-center justify-center">
          <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
        </main>
      </div>
    );
  }

  if (error || !examination) {
    return (
      <div className="flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <DentistSidebar activeMenu="patients" />
        <main className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <p className="text-[14px] font-semibold text-red-500">{error ?? "Không tìm thấy lịch hẹn"}</p>
            <Link href="/dentist/patients" className="mt-4 inline-block px-4 py-2 bg-primary text-white text-[13px] font-bold rounded-xl">
              Quay lại danh sách
            </Link>
          </div>
        </main>
      </div>
    );
  }

  const patient = examination.patient;
  const initials = patient.fullName.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase();
  const statusConfig = STATUS_LABEL[examination.status] ?? { label: examination.status, cls: "bg-slate-100 text-slate-600" };
  const isInProgress = examination.status === "InProgress";
  const isPendingPayment = examination.status === "PendingPayment";

return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title={patient.fullName}
          subtitle={`${patient.dateOfBirth ? new Date(patient.dateOfBirth).getFullYear() : "—"} tuổi · ${patient.gender ?? "—"} · ${patient.phoneNumber ?? "—"}`}
          left={
            <div className="flex items-center gap-2.5">
              <Link href="/dentist/patients" className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-700 transition-all shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
              </Link>
              <div className={`w-9 h-9 rounded-full flex items-center justify-center font-black text-[12px] border shrink-0 ${patient.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"}`}>
                {initials}
              </div>
            </div>
          }
          right={
            <div className="flex items-center gap-2">
              <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[12px] font-black border ${statusConfig.cls}`}>
                {statusConfig.label}
              </span>
              {isInProgress && (
                <button
                  onClick={handleEndTreatment}
                  className="flex items-center gap-2 px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white text-[13px] font-bold rounded-xl transition-all shadow-sm cursor-pointer"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Kết thúc điều trị
                </button>
              )}
              {!isInProgress && examination.status === "CheckedIn" && (
                <button
                  onClick={handleStartTreatment}
                  className="flex items-center gap-2 px-4 py-2 bg-primary hover:bg-red-600 text-white text-[13px] font-bold rounded-xl transition-all shadow-sm cursor-pointer"
                >
                  Bắt đầu khám
                </button>
              )}
            </div>
          }
        />

        {/* TOAST */}
        {toast && (
          <div className={`fixed top-24 right-8 z-50 px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border animate-fade-in font-bold text-[14px] ${toast.type === "success" ? "bg-emerald-900 text-white border-emerald-800" : toast.type === "error" ? "bg-red-900 text-white border-red-800" : "bg-slate-900 text-white border-slate-800"}`}>
            <span>{toast.type === "success" ? "✓" : toast.type === "error" ? "⚠" : "ℹ"}</span>
            <span>{toast.message}</span>
          </div>
        )}

        {/* ─── TAB BAR ─── */}
        <div className="px-8 pt-6 pb-0 flex items-center gap-2">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2.5 px-5 py-2 rounded-xl text-[13px] font-bold border transition-all cursor-pointer ${
                activeTab === tab.id
                  ? `${tab.activeColor} shadow-sm`
                  : "bg-white text-slate-500 border-slate-200 hover:bg-slate-50 hover:border-slate-300"
              }`}
            >
              <svg className={`w-4 h-4 shrink-0 ${activeTab === tab.id ? tab.color : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d={tab.icon} />
              </svg>
              {tab.label}
            </button>
          ))}
        </div>

        {/* CONTENT */}
        <div className="p-8 pt-4 flex-1 overflow-y-auto">

          <div className="grid gap-6" style={{ gridTemplateColumns: "1fr" }}>

            {/* ── clinical sections (based on active tab) ── */}
            <div className="flex flex-col gap-6 min-w-0">

              {/* ─── TAB: CHUẨN ĐOÁN ─── */}
              {activeTab === "diagnosis" && (
                <div className="grid gap-6" style={{ gridTemplateColumns: "2fr 1fr" }}>
                  {/* LEFT: Patient Info + History */}
                  <div className="flex flex-col gap-6">

                    {/* Patient Basic Info Card */}
                    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
                        <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
                        </svg>
                        <span className="text-[14px] font-black text-slate-900">Thông tin bệnh nhân</span>
                      </div>
                      <div className="p-5">
                        <div className="flex items-start gap-4 mb-5">
                          <div className={`w-16 h-16 rounded-2xl flex items-center justify-center font-black text-xl border-2 shrink-0 ${patient.gender === "Nữ" ? "bg-rose-50 text-rose-500 border-rose-100" : "bg-sky-50 text-sky-600 border-sky-100"}`}>
                            {patient.fullName.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase()}
                          </div>
                          <div className="flex-1 grid grid-cols-3 gap-3">
                            <div>
                              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Họ tên</div>
                              <div className="text-[13px] font-bold text-slate-800 mt-0.5">{patient.fullName}</div>
                            </div>
                            <div>
                              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Ngày sinh</div>
                              <div className="text-[13px] font-bold text-slate-800 mt-0.5">
                                {patient.dateOfBirth ? `${new Date(patient.dateOfBirth).toLocaleDateString("vi-VN")} (${new Date().getFullYear() - new Date(patient.dateOfBirth).getFullYear()} tuổi)` : "—"}
                              </div>
                            </div>
                            <div>
                              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Giới tính</div>
                              <div className="text-[13px] font-bold text-slate-800 mt-0.5">{patient.gender ?? "—"}</div>
                            </div>
                            <div>
                              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Điện thoại</div>
                              <div className="text-[13px] font-bold text-slate-800 mt-0.5">{patient.phoneNumber ?? "—"}</div>
                            </div>
                            <div>
                              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Email</div>
                              <div className="text-[13px] font-bold text-slate-800 mt-0.5">{patient.email ?? "—"}</div>
                            </div>
                            <div>
                              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Địa chỉ</div>
                              <div className="text-[13px] font-bold text-slate-800 mt-0.5">{patient.address ?? "—"}</div>
                            </div>
                          </div>
                        </div>

                        {/* Service & Symptoms */}
                        <div className="grid grid-cols-2 gap-4 pt-4 border-t border-slate-100">
                          <div className="bg-sky-50 rounded-xl p-4">
                            <div className="flex items-center gap-2 mb-2">
                              <svg className="w-4 h-4 text-sky-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" />
                              </svg>
                              <span className="text-[11px] font-extrabold text-sky-600 uppercase tracking-wider">Dịch vụ đăng ký</span>
                            </div>
                            <div className="text-[14px] font-bold text-slate-800">{examination.serviceName ?? "Khám tổng quát"}</div>
                          </div>
                          <div className="bg-amber-50 rounded-xl p-4">
                            <div className="flex items-center gap-2 mb-2">
                              <svg className="w-4 h-4 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z" />
                              </svg>
                              <span className="text-[11px] font-extrabold text-amber-600 uppercase tracking-wider">Lý do đến khám</span>
                            </div>
                            <div className="text-[14px] font-bold text-slate-800">{examination.symptoms ?? "Không có"}</div>
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Treatment History */}
                    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
                        <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <span className="text-[14px] font-black text-slate-900">Lịch sử điều trị</span>
                        <span className="ml-auto text-[11px] font-bold px-2.5 py-1 bg-amber-50 text-amber-700 rounded-lg border border-amber-200">
                          {medicalHistory.length} lần khám
                        </span>
                      </div>
                      <div className="p-5 max-h-[300px] overflow-y-auto">
                        {medicalHistory.length === 0 ? (
                          <div className="text-center py-8 text-slate-400">
                            <svg className="w-12 h-12 mx-auto mb-3 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" />
                            </svg>
                            <p className="text-[13px] font-semibold">Chưa có lịch sử khám</p>
                          </div>
                        ) : (
                          <div className="flex flex-col gap-3">
                            {medicalHistory.map((record, index) => (
                              <div key={record.appointmentId} className="border border-slate-100 rounded-xl p-4 hover:bg-slate-50 transition-colors">
                                <div className="flex items-start justify-between mb-3">
                                  <div>
                                    <div className="flex items-center gap-2 mb-1">
                                      <span className="text-[12px] font-mono font-bold text-primary">{record.appointmentCode}</span>
                                      <span className="text-[10px] font-bold px-2 py-0.5 bg-green-50 text-green-700 rounded-full border border-green-200">Hoàn thành</span>
                                    </div>
                                    <div className="text-[12px] text-slate-500">
                                      {new Date(record.appointmentDate).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" })} · {record.dentistName}
                                    </div>
                                  </div>
                                  <div className="text-right">
                                    <div className="text-[11px] font-semibold text-slate-400">{record.serviceName}</div>
                                  </div>
                                </div>

                                {record.symptoms && (
                                  <div className="mb-2">
                                    <div className="text-[10px] font-extrabold text-slate-400 uppercase tracking-wider">Lý do khám</div>
                                    <div className="text-[12px] font-medium text-slate-600 mt-0.5">{record.symptoms}</div>
                                  </div>
                                )}

                                {record.diagnoses.length > 0 && (
                                  <div className="mb-2">
                                    <div className="text-[10px] font-extrabold text-slate-400 uppercase tracking-wider">Chẩn đoán</div>
                                    <div className="text-[12px] font-medium text-slate-600 mt-0.5">{record.diagnoses[0].description}</div>
                                    {record.diagnoses[0].conclusion && (
                                      <div className="text-[11px] text-slate-500 mt-0.5 italic">Kết luận: {record.diagnoses[0].conclusion}</div>
                                    )}
                                  </div>
                                )}

                                {record.treatmentPlans.length > 0 && (
                                  <div className="mb-2">
                                    <div className="text-[10px] font-extrabold text-slate-400 uppercase tracking-wider">Điều trị</div>
                                    <div className="flex flex-wrap gap-1.5 mt-1">
                                      {record.treatmentPlans.map((tp, tpIdx) => (
                                        <span key={tpIdx} className="text-[11px] font-semibold px-2 py-1 bg-sky-50 text-sky-700 rounded-lg border border-sky-100">
                                          {tp.description}
                                        </span>
                                      ))}
                                    </div>
                                  </div>
                                )}

                                {record.prescriptionItems.length > 0 && (
                                  <div>
                                    <div className="text-[10px] font-extrabold text-slate-400 uppercase tracking-wider">Đơn thuốc</div>
                                    <div className="flex flex-wrap gap-1.5 mt-1">
                                      {record.prescriptionItems.map((item, itemIdx) => (
                                        <span key={itemIdx} className="text-[11px] font-semibold px-2 py-1 bg-violet-50 text-violet-700 rounded-lg border border-violet-100">
                                          {item.medicineName}
                                        </span>
                                      ))}
                                    </div>
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* RIGHT: Diagnosis Form */}
                  <div className="flex flex-col gap-6">
                    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
                        <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                        <span className="text-[14px] font-black text-slate-900">Thông tin khám</span>
                      </div>
                      <div className="p-5 flex flex-col gap-4">

                        {/* Vital Signs */}
                        <div>
                          <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-3">Chỉ số sinh tồn</div>
                          <div className="grid grid-cols-2 gap-3">
                            <div>
                              <label className="text-[11px] font-bold text-slate-500 mb-1 block">Nhịp tim (lần/phút)</label>
                              <input
                                type="number"
                                value={heartRate}
                                onChange={(e) => setHeartRate(e.target.value)}
                                placeholder="vd: 80"
                                disabled={!isInProgress}
                                className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 disabled:opacity-60 disabled:cursor-not-allowed"
                              />
                            </div>
                            <div>
                              <label className="text-[11px] font-bold text-slate-500 mb-1 block">Nhiệt độ (°C)</label>
                              <input
                                type="number"
                                value={temperature}
                                onChange={(e) => setTemperature(e.target.value)}
                                placeholder="vd: 37.0"
                                step="0.1"
                                disabled={!isInProgress}
                                className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 disabled:opacity-60 disabled:cursor-not-allowed"
                              />
                            </div>
                            {/* Blood Pressure - 2 column grid */}
                            <div>
                              <label className="text-[11px] font-bold text-slate-500 mb-1 block">Huyết áp tâm thu (mmHg)</label>
                              <input
                                type="number"
                                value={bloodPressureSystolic}
                                onChange={(e) => setBloodPressureSystolic(e.target.value)}
                                placeholder="Tâm thu"
                                disabled={!isInProgress}
                                className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 disabled:opacity-60 disabled:cursor-not-allowed"
                              />
                            </div>
                            <div>
                              <label className="text-[11px] font-bold text-slate-500 mb-1 block">Huyết áp tâm trương (mmHg)</label>
                              <input
                                type="number"
                                value={bloodPressureDiastolic}
                                onChange={(e) => setBloodPressureDiastolic(e.target.value)}
                                placeholder="Tâm trương"
                                disabled={!isInProgress}
                                className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 disabled:opacity-60 disabled:cursor-not-allowed"
                              />
                            </div>
                          </div>
                        </div>

                        {/* Medical History & Allergy History - 2 columns */}
                        <div className="grid grid-cols-2 gap-3">
                          <div>
                            <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-1 block">Tiền sử bệnh lý</label>
                            <textarea
                              value={medicalHistoryInput}
                              onChange={(e) => setMedicalHistoryInput(e.target.value)}
                              placeholder="VD: Tiểu đường, cao huyết áp..."
                              rows={2}
                              disabled={!isInProgress}
                              className="w-full px-3 py-2 text-[12px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                            />
                          </div>
                          <div>
                            <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-1 block">Tiền sử dị ứng</label>
                            <textarea
                              value={allergyHistory}
                              onChange={(e) => setAllergyHistory(e.target.value)}
                              placeholder="VD: Dị ứng penicillin, hải sản..."
                              rows={2}
                              disabled={!isInProgress}
                              className="w-full px-3 py-2 text-[12px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                            />
                          </div>
                        </div>

                        {/* Dental Condition */}
                        <div>
                          <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-1 block">Tình trạng răng</label>
                          <textarea
                            value={dentalCondition}
                            onChange={(e) => setDentalCondition(e.target.value)}
                            placeholder="Mô tả tình trạng răng hiện tại..."
                            rows={2}
                            disabled={!isInProgress}
                            className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                          />
                        </div>

                        {/* Diagnosis */}
                        <div>
                          <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-1 block">Chẩn đoán</label>
                          <textarea
                            value={diagnosis}
                            onChange={(e) => setDiagnosis(e.target.value)}
                            placeholder="Nhập chẩn đoán..."
                            rows={2}
                            disabled={!isInProgress}
                            className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                          />
                        </div>

                        {/* Clinical Findings */}
                        <div>
                          <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-1 block">Kết quả thăm khám lâm sàng</label>
                          <textarea
                            value={findings}
                            onChange={(e) => setFindings(e.target.value)}
                            placeholder="Ghi chép kết quả khám..."
                            rows={2}
                            disabled={!isInProgress}
                            className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-medium text-slate-700 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                          />
                        </div>

                        {/* Save Button */}
                        <div className="flex items-center gap-3 pt-2">
                          {diagSaved && (
                            <span className="text-[12px] text-green-600 font-bold animate-fade-in flex items-center gap-1">
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                              </svg>
                              Đã lưu
                            </span>
                          )}
                          <button
                            onClick={handleSaveDiagnosis}
                            disabled={!isInProgress}
                            className="ml-auto flex items-center gap-2 px-5 py-2.5 bg-primary text-white text-[13px] font-black rounded-xl hover:bg-red-600 transition-all shadow-sm cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                            </svg>
                            Lưu tất cả
                          </button>
                        </div>
                      </div>
                    </div>

                    {/* Tooth Diagram Mini */}
                    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
                        <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" />
                        </svg>
                        <span className="text-[14px] font-black text-slate-900">Sơ đồ răng</span>
                      </div>
                      <div className="p-4">
                        <ToothArchDiagram
                          teeth={teeth}
                          selected={selectedTooth ? new Set([selectedTooth]) : new Set()}
                          onToothClick={(num) => setSel(selectedTooth === num ? null : num)}
                          showLegend
                        />
                        {selectedTooth && (
                          <div className="mt-3 bg-slate-50 border border-slate-200 rounded-xl p-3">
                            <div className="text-[12px] font-black text-slate-700 mb-2">Răng {selectedTooth}</div>
                            <div className="flex gap-1.5 flex-wrap">
                              {(["normal","decay","filled","missing","crown","implant"] as ToothStatus[]).map((s) => (
                                <button key={s} onClick={() => setToothStatus(selectedTooth, s)}
                                  className={`px-2.5 py-1 rounded-lg text-[11px] font-bold border-2 transition-all cursor-pointer ${TOOTH_COLOR[s]}`}>
                                  {TOOTH_LEGEND.find((l) => l.status === s)?.label ?? s}
                                </button>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* ─── TAB: LIỆU TRÌNH ─── */}
              {activeTab === "treatment" && (
                <>
                  {/* Liệu trình dài hạn */}
                  <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                    <SectionHeading color="bg-indigo-50 text-indigo-700" title="Liệu trình dài hạn"
                      icon="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zM3.75 12h.007v.008H3.75V12zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm-.375 5.25h.007v.008H3.75v-.008zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
                    <div className="p-6">
                      {course ? (
                        <div className="flex flex-col gap-3">
                          <div className="flex items-center justify-between flex-wrap gap-2">
                            <div className="flex items-center gap-2">
                              <span className="text-[15px] font-black text-slate-900">{course.name}</span>
                              <span className={`text-[11px] font-black px-2 py-0.5 rounded-lg border ${course.status === "Completed" ? "bg-green-50 text-green-700 border-green-200" : "bg-indigo-50 text-indigo-700 border-indigo-200"}`}>
                                {course.status === "Completed" ? "Đã tất toán" : "Đang điều trị"}
                              </span>
                            </div>
                            <span className="text-[12px] font-semibold text-slate-400">Thanh toán theo nhiều đợt qua các buổi tái khám</span>
                          </div>
                          <div className="grid grid-cols-3 gap-3">
                            {[
                              { label: "Tổng chi phí", value: course.totalCost, cls: "text-slate-900" },
                              { label: "Đã thu", value: course.amountPaid, cls: "text-emerald-600" },
                              { label: "Còn lại", value: course.remainingAmount, cls: "text-orange-600" },
                            ].map(s => (
                              <div key={s.label} className="bg-slate-50 border border-slate-100 rounded-xl px-4 py-3">
                                <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{s.label}</div>
                                <div className={`text-[15px] font-black mt-0.5 ${s.cls}`}>{s.value.toLocaleString("vi-VN")}đ</div>
                              </div>
                            ))}
                          </div>
                        </div>
                      ) : isInProgress ? (
                        <div className="flex flex-col gap-3">
                          <p className="text-[13px] text-slate-500 font-semibold">
                            Dùng cho điều trị lâu dài (niềng răng, implant…): chọn dịch vụ để chốt tổng chi phí, bệnh nhân trả thành nhiều đợt qua các lần tái khám.
                          </p>
                          <Link
                            href={`/dentist/patients/${id}/treatment/new?mode=course`}
                            className="self-start flex items-center gap-2 px-5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-[13px] font-black rounded-xl transition-all shadow-sm cursor-pointer"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                            Tạo liệu trình dài hạn
                          </Link>
                        </div>
                      ) : (
                        <p className="text-[13px] text-slate-400 font-semibold">Không có liệu trình dài hạn.</p>
                      )}
                    </div>
                  </section>

                  {/* Liệu trình điều trị (ngắn hạn) */}
                  <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                    <SectionHeading color="bg-red-50 text-primary" title="Liệu trình điều trị"
                      icon="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z"
                      action={
                        isInProgress && (
                          <Link
                            href={`/dentist/patients/${id}/treatment/new`}
                            className="flex items-center gap-2 px-4 py-2 bg-primary text-white text-[13px] font-bold rounded-xl hover:bg-red-600 transition-all shadow-sm cursor-pointer"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                            Tạo liệu trình
                          </Link>
                        )
                      } />
                    <div className="p-6 flex flex-col gap-4">
                      <div className="text-[13px] font-bold text-slate-600">
                        Tổng chi phí dự kiến: <span className="text-primary font-black text-[15px]">{totalCost.toLocaleString("vi-VN")}đ</span>
                      </div>
                      <div className="flex flex-col divide-y divide-slate-100">
                        {treatmentPlans.length === 0 ? (
                          <p className="text-[13px] text-slate-400 font-semibold text-center py-4">Chưa có dịch vụ nào.</p>
                        ) : (
                          treatmentPlans.map((tp) => {
                            const serviceName = tp.description.split(" - Răng")[0] || tp.description;
                            return (
                              <div key={tp.id} className="flex items-center justify-between py-3">
                                <span className="text-[13px] font-semibold text-slate-700">{serviceName}</span>
                                <div className="flex items-center gap-3">
                                  <span className="text-[13px] font-bold text-primary">{tp.estimatedCost ? tp.estimatedCost.toLocaleString("vi-VN") + "đ" : "Liên hệ"}</span>
                                  {isInProgress && (
                                    <button
                                      onClick={() => handleDeleteTreatmentPlan(tp.id)}
                                      className="p-1 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-all cursor-pointer"
                                    >
                                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                    </button>
                                  )}
                                </div>
                              </div>
                            );
                          })
                        )}
                      </div>
                    </div>
                  </section>
                </>
              )}

              {/* ─── TAB: ĐƠN THUỐC ─── */}
              {activeTab === "prescription" && (
                <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <SectionHeading color="bg-violet-50 text-violet-700" title="Đơn thuốc"
                    icon="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5"
                    action={
                      isInProgress && (
                        <Link
                          href={`/dentist/patients/${id}/prescription/new`}
                          className="flex items-center gap-2 px-4 py-2 bg-primary text-white text-[13px] font-bold rounded-xl hover:bg-red-600 transition-all shadow-sm cursor-pointer"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                          Tạo đơn thuốc
                        </Link>
                      )
                    } />
                  {prescription ? (
                    <div className="p-6 flex flex-col gap-4">
                      <div className="flex flex-col divide-y divide-slate-100">
                        {prescription.items.length === 0 ? (
                          <p className="text-[13px] text-slate-400 font-semibold text-center py-4">Chưa có thuốc nào.</p>
                        ) : (
                          prescription.items.map((item, idx) => (
                            <div key={idx} className="flex items-center justify-between py-3">
                              <div className="flex flex-col">
                                <span className="text-[13px] font-semibold text-slate-700">{item.medicineName}</span>
                                <span className="text-[11px] text-slate-400">{item.dosage} × {item.quantity} {item.unit}</span>
                              </div>
                              {isInProgress && (
                                <button
                                  onClick={() => handleDeletePrescriptionItem(item.id)}
                                  className="p-1 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-all cursor-pointer"
                                >
                                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                </button>
                              )}
                            </div>
                          ))
                        )}
                      </div>
                    </div>
                  ) : (
                    <div className="p-6 text-center text-slate-400 text-[13px]">Chưa có đơn thuốc.</div>
                  )}
                </section>
              )}

              {/* ─── TAB: TÁI KHÁM ─── */}
              {activeTab === "followup" && (
                <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <SectionHeading color="bg-green-50 text-green-700" title="Lịch tái khám"
                    icon="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z"
                    action={
                      isInProgress && (
                        <Link
                          href={`/dentist/patients/${id}/followup/new`}
                          className="flex items-center gap-2 px-4 py-2 bg-primary text-white text-[13px] font-bold rounded-xl hover:bg-red-600 transition-all shadow-sm"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                          Đặt lịch mới
                        </Link>
                      )
                    } />
                  <div className="p-6 flex flex-col gap-3">
                    {followUps.length === 0 && <p className="text-[13px] text-slate-400 font-semibold">Chưa có lịch tái khám.</p>}
                    {followUps.map((h) => (
                      <div key={h.id} className="bg-slate-50 border border-slate-100 rounded-xl px-5 py-4 flex items-start gap-4">
                        <div className="w-10 h-10 rounded-xl bg-green-50 text-green-700 flex items-center justify-center shrink-0">
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                        </div>
                        <div className="flex-1">
                          <div className="flex items-center justify-between gap-3">
                            <span className="text-[14px] font-black text-slate-900">
                              {new Date(h.appointmentDate).toLocaleDateString("vi-VN")}
                            </span>
                            <span className="text-[12px] font-semibold text-slate-400">{h.dentistName}</span>
                          </div>
                          <p className="text-[13px] text-slate-600 font-medium mt-1">{h.symptoms ?? h.notes ?? "Tái khám theo dõi"}</p>
                          <span className={`inline-block mt-2 px-2.5 py-0.5 text-[10px] font-black rounded-full ${h.status === "Confirmed" ? "bg-blue-50 text-blue-700" : h.status === "Completed" ? "bg-green-50 text-green-700" : "bg-amber-50 text-amber-700"}`}>
                            {h.status}
                          </span>
                        </div>
                        {isInProgress && (
                          <button
                            onClick={() => handleDeleteFollowUp(h.id)}
                            className="p-1 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-all cursor-pointer"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                          </button>
                        )}
                      </div>
                    ))}
                  </div>
                </section>
              )}

            </div>{/* end left col */}

          </div>{/* end 2-col wrapper */}
        </div>
      </main>
    </div>
  );
}
