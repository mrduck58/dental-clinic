"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import DentistSidebar from "../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../components/shared/DentistPageHeader";
import AiSummaryText from "../../../../components/shared/AiSummaryText";
import TreatmentWorkspace from "./TreatmentWorkspace";
import PrescriptionWorkspace from "./PrescriptionWorkspace";
import FollowUpWorkspace from "./FollowUpWorkspace";
import MaterialWorkspace from "./MaterialWorkspace";
import { useRequireDentist } from "../../../../hooks/useRequireDentist";
import {
  getExaminationApi,
  startTreatmentApi,
  endTreatmentApi,
  createDiagnosisApi,
  updateDiagnosisApi,
  deleteDiagnosisApi,
  getPatientMedicalHistoryApi,
  getPatientAiSummaryApi,
  getTreatmentSuggestionApi,
  type ExaminationDto,
  type PatientHistorySummaryDto,
  type TreatmentSuggestionDto,
  type DiagnosisDto,
  type PatientMedicalHistoryDto,
} from "../../../../lib/apiClient";

type TreatmentStatus = "pending" | "in_progress" | "done";

type TabId = "diagnosis" | "treatment" | "prescription" | "followup" | "materials";

const TABS: { id: TabId; label: string; icon: string; color: string; activeColor: string }[] = [
  { id: "diagnosis",    label: "Chuẩn đoán",  icon: "M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z",                    color: "text-amber-600",   activeColor: "bg-amber-50 border-amber-200 text-amber-700" },
  { id: "treatment",   label: "Liệu trình",   icon: "M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z", color: "text-red-600",    activeColor: "bg-red-50 border-red-200 text-red-700" },
  { id: "prescription", label: "Đơn thuốc",  icon: "M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5", color: "text-violet-600", activeColor: "bg-violet-50 border-violet-200 text-violet-700" },
  { id: "followup",    label: "Tái khám",   icon: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z", color: "text-green-600",   activeColor: "bg-green-50 border-green-200 text-green-700" },
  { id: "materials",   label: "Vật tư",      icon: "M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z", color: "text-orange-600", activeColor: "bg-orange-50 border-orange-200 text-orange-700" },
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

// Các trường của phiếu khám răng miệng (khớp DiagnosisFields ở apiClient)
const EMPTY_EXAM_FORM = {
  description: "",
  gumCondition: "", oralMucosaCondition: "", gumBleeding: "", painOnChewing: "",
  teethCount: "", decayedTeeth: "", wornOrBrokenTeeth: "", looseTeeth: "",
  tartar: "", plaque: "", badBreath: "",
  tmjSymptoms: "", occlusion: "", occlusionDeviation: "",
  conclusion: "",
};
type ExamForm = typeof EMPTY_EXAM_FORM;

function ExamSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-2.5">{title}</div>
      {children}
    </div>
  );
}

function ExamField({ label, placeholder, value, onChange, disabled }: {
  label: string;
  placeholder: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div>
      <label className="text-[12px] font-bold text-slate-500 mb-1 block">{label}</label>
      <input
        type="text"
        value={value}
        onChange={e => onChange(e.target.value)}
        placeholder={placeholder}
        disabled={disabled}
        className="w-full px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-medium text-slate-700 placeholder:text-slate-300 disabled:opacity-60 disabled:cursor-not-allowed"
      />
    </div>
  );
}

export default function PatientDetailPage() {
  useRequireDentist();
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  const editMode = searchParams.get("edit") === "1";

  const [examination, setExamination] = useState<ExaminationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabId>("diagnosis");

  // Phiếu khám răng miệng — gom các trường vào một object cho gọn
  const [form, setForm] = useState<ExamForm>(EMPTY_EXAM_FORM);
  const [diagSaved, setDiagSaved] = useState(false);
  const setField = (key: keyof ExamForm, value: string) => setForm(f => ({ ...f, [key]: value }));

  // Patient medical history
  const [medicalHistory, setMedicalHistory] = useState<PatientMedicalHistoryDto[]>([]);

  // Tóm tắt hồ sơ bằng AI — tạo theo yêu cầu (không tự động gọi khi vào trang)
  const [aiSummary, setAiSummary] = useState<PatientHistorySummaryDto | null>(null);
  const [aiSummaryLoading, setAiSummaryLoading] = useState(false);
  const [aiSummaryError, setAiSummaryError] = useState<string | null>(null);

  const loadAiSummary = useCallback(async (force = false) => {
    setAiSummaryLoading(true);
    setAiSummaryError(null);
    try {
      const data = await getPatientAiSummaryApi(id, force);
      setAiSummary(data);
    } catch (err) {
      setAiSummaryError(err instanceof Error ? err.message : "Không thể tạo tóm tắt AI");
    } finally {
      setAiSummaryLoading(false);
    }
  }, [id]);

  // Gợi ý điều trị bằng AI — dựa trên phiếu khám ĐÃ LƯU của buổi khám này + lịch sử khám trước đây.
  // Không cache: chẩn đoán có thể được bác sĩ sửa nhiều lần, mỗi lần bấm tạo lại theo dữ liệu mới nhất.
  const [treatmentSuggestion, setTreatmentSuggestion] = useState<TreatmentSuggestionDto | null>(null);
  const [treatmentSuggestionLoading, setTreatmentSuggestionLoading] = useState(false);
  const [treatmentSuggestionError, setTreatmentSuggestionError] = useState<string | null>(null);

  const loadTreatmentSuggestion = useCallback(async () => {
    setTreatmentSuggestionLoading(true);
    setTreatmentSuggestionError(null);
    try {
      const data = await getTreatmentSuggestionApi(id);
      setTreatmentSuggestion(data);
    } catch (err) {
      setTreatmentSuggestionError(err instanceof Error ? err.message : "Không thể tạo gợi ý điều trị");
    } finally {
      setTreatmentSuggestionLoading(false);
    }
  }, [id]);

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
      setError(null);

      // Đổ phiếu khám đã lưu (nếu có) vào form
      const d = data.diagnoses[0];
      if (d) {
        setForm({
          description: d.description ?? "",
          gumCondition: d.gumCondition ?? "",
          oralMucosaCondition: d.oralMucosaCondition ?? "",
          gumBleeding: d.gumBleeding ?? "",
          painOnChewing: d.painOnChewing ?? "",
          teethCount: d.teethCount ?? "",
          decayedTeeth: d.decayedTeeth ?? "",
          wornOrBrokenTeeth: d.wornOrBrokenTeeth ?? "",
          looseTeeth: d.looseTeeth ?? "",
          tartar: d.tartar ?? "",
          plaque: d.plaque ?? "",
          badBreath: d.badBreath ?? "",
          tmjSymptoms: d.tmjSymptoms ?? "",
          occlusion: d.occlusion ?? "",
          occlusionDeviation: d.occlusionDeviation ?? "",
          conclusion: d.conclusion ?? "",
        });
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
      // Hiển thị toàn bộ lịch sử khám (gồm cả buổi vừa kết thúc) để đồng bộ với tab liệu trình.
      // Buổi đang khám (InProgress) vốn không nằm trong medical-history nên không bị trùng.
      setMedicalHistory(history);
    } catch {
      // Silently fail for medical history
    }
  }, [examination?.patient?.id, id]);

  useEffect(() => {
    void loadExamination();
  }, [loadExamination]);

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
      const t = (v: string) => (v.trim() ? v.trim() : undefined);
      const diagnosisData = {
        description: form.description.trim(),
        gumCondition: t(form.gumCondition),
        oralMucosaCondition: t(form.oralMucosaCondition),
        gumBleeding: t(form.gumBleeding),
        painOnChewing: t(form.painOnChewing),
        teethCount: t(form.teethCount),
        decayedTeeth: t(form.decayedTeeth),
        wornOrBrokenTeeth: t(form.wornOrBrokenTeeth),
        looseTeeth: t(form.looseTeeth),
        tartar: t(form.tartar),
        plaque: t(form.plaque),
        badBreath: t(form.badBreath),
        tmjSymptoms: t(form.tmjSymptoms),
        occlusion: t(form.occlusion),
        occlusionDeviation: t(form.occlusionDeviation),
        conclusion: t(form.conclusion),
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
      // Phiếu khám vừa đổi — gợi ý cũ (nếu có) không còn khớp dữ liệu mới, xoá để bác sĩ tạo lại.
      setTreatmentSuggestion(null);
      setTreatmentSuggestionError(null);
      showToast("Đã lưu phiếu khám!", "success");

      // Tải lại để đồng bộ với dữ liệu server
      await loadExamination();
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Lưu thất bại", "error");
    }
  };


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
  // Đơn đã kết thúc: cho phép sửa khi mở ở chế độ chỉnh sửa (?edit=1).
  const isFinished = examination.status === "PendingPayment" || examination.status === "Completed";
  const canEdit = isInProgress || (editMode && isFinished);

return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title={patient.fullName}
          subtitle={`${patient.dateOfBirth ? `${new Date().getFullYear() - new Date(patient.dateOfBirth).getFullYear()} tuổi` : "—"} · ${patient.gender ?? "—"} · ${patient.phoneNumber ?? "—"}`}
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
              {isFinished && !editMode && (
                <Link
                  href={`/dentist/patients/${id}?edit=1`}
                  className="flex items-center px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white text-[13px] font-bold rounded-xl transition-all shadow-sm cursor-pointer"
                >
                  Chỉnh sửa
                </Link>
              )}
              {isFinished && editMode && (
                <Link
                  href={`/dentist/patients/${id}`}
                  className="flex items-center px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-[13px] font-bold rounded-xl transition-all shadow-sm cursor-pointer"
                >
                  Xong
                </Link>
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
                <div className="grid gap-6 items-start" style={{ gridTemplateColumns: "1fr 1fr" }}>
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

                    {/* Tóm tắt AI cho bác sĩ */}
                    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                          <svg className="w-5 h-5 text-violet-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                          </svg>
                          <span className="text-[14px] font-black text-slate-900">Tóm tắt AI cho bác sĩ</span>
                        </div>
                        {!aiSummary && (
                          <button
                            onClick={() => void loadAiSummary()}
                            disabled={aiSummaryLoading}
                            className="px-3 py-1.5 rounded-lg bg-primary text-white text-[11.5px] font-bold hover:opacity-90 disabled:opacity-50 transition-all cursor-pointer"
                          >
                            {aiSummaryLoading ? "Đang tạo..." : "Tạo tóm tắt"}
                          </button>
                        )}
                        {aiSummary && (
                          <button
                            onClick={() => void loadAiSummary(true)}
                            disabled={aiSummaryLoading}
                            className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-600 text-[11.5px] font-bold hover:bg-slate-50 disabled:opacity-50 transition-all cursor-pointer"
                          >
                            {aiSummaryLoading ? "Đang tạo..." : "Làm mới"}
                          </button>
                        )}
                      </div>
                      <div className="px-5 py-4 max-h-[300px] overflow-y-auto">
                        {aiSummaryLoading && (
                          <div className="flex items-center justify-center py-6">
                            <div className="w-5 h-5 border-2 border-slate-200 border-t-primary rounded-full animate-spin" />
                          </div>
                        )}
                        {!aiSummaryLoading && aiSummaryError && (
                          <div className="flex flex-col gap-2">
                            <span className="text-[12.5px] font-semibold text-red-600">{aiSummaryError}</span>
                            <button
                              onClick={() => void loadAiSummary()}
                              className="self-start text-[12px] font-bold text-primary hover:underline cursor-pointer"
                            >
                              Thử lại
                            </button>
                          </div>
                        )}
                        {!aiSummaryLoading && !aiSummaryError && !aiSummary && (
                          <span className="text-[12.5px] text-slate-400">
                            Tổng hợp nhanh lịch sử khám trước đây của bệnh nhân để hỗ trợ bác sĩ.
                          </span>
                        )}
                        {!aiSummaryLoading && aiSummary && (
                          <div className="flex flex-col gap-3">
                            {aiSummary.fromCache && (
                              <span className="text-[10.5px] font-semibold text-slate-400">
                                Tóm tắt từ lần tạo trước — bấm &quot;Làm mới&quot; nếu bệnh nhân vừa có lịch khám mới cần cập nhật.
                              </span>
                            )}
                            <AiSummaryText text={aiSummary.summary} />
                            <div className="px-3 py-2 rounded-lg bg-amber-50 border border-amber-200">
                              <span className="text-[11px] font-semibold text-amber-700 leading-snug">{aiSummary.disclaimer}</span>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Gợi ý điều trị bằng AI */}
                    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                          <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 18v-5.25m0 0a6.01 6.01 0 001.5-.189m-1.5.189a6.01 6.01 0 01-1.5-.189m3.75 7.478a12.06 12.06 0 01-4.5 0m3.75 2.383a14.406 14.406 0 01-3 0M14.25 18v-.192c0-.983.658-1.823 1.508-2.316a7.5 7.5 0 10-7.517 0c.85.493 1.509 1.333 1.509 2.316V18" />
                          </svg>
                          <span className="text-[14px] font-black text-slate-900">Gợi ý điều trị</span>
                        </div>
                        {examination.diagnoses[0]?.id && (
                          <>
                            {!treatmentSuggestion && (
                              <button
                                onClick={() => void loadTreatmentSuggestion()}
                                disabled={treatmentSuggestionLoading}
                                className="px-3 py-1.5 rounded-lg bg-emerald-600 text-white text-[11.5px] font-bold hover:opacity-90 disabled:opacity-50 transition-all cursor-pointer"
                              >
                                {treatmentSuggestionLoading ? "Đang tạo..." : "Tạo gợi ý"}
                              </button>
                            )}
                            {treatmentSuggestion && (
                              <button
                                onClick={() => void loadTreatmentSuggestion()}
                                disabled={treatmentSuggestionLoading}
                                className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-600 text-[11.5px] font-bold hover:bg-slate-50 disabled:opacity-50 transition-all cursor-pointer"
                              >
                                {treatmentSuggestionLoading ? "Đang tạo..." : "Làm mới"}
                              </button>
                            )}
                          </>
                        )}
                      </div>
                      <div className="px-5 py-4 max-h-[300px] overflow-y-auto">
                        {!examination.diagnoses[0]?.id && (
                          <span className="text-[12.5px] text-slate-400">
                            Lưu phiếu khám (bên phải) trước để tạo gợi ý điều trị dựa trên chẩn đoán vừa nhập.
                          </span>
                        )}
                        {examination.diagnoses[0]?.id && treatmentSuggestionLoading && (
                          <div className="flex items-center justify-center py-6">
                            <div className="w-5 h-5 border-2 border-slate-200 border-t-emerald-600 rounded-full animate-spin" />
                          </div>
                        )}
                        {examination.diagnoses[0]?.id && !treatmentSuggestionLoading && treatmentSuggestionError && (
                          <div className="flex flex-col gap-2">
                            <span className="text-[12.5px] font-semibold text-red-600">{treatmentSuggestionError}</span>
                            <button
                              onClick={() => void loadTreatmentSuggestion()}
                              className="self-start text-[12px] font-bold text-primary hover:underline cursor-pointer"
                            >
                              Thử lại
                            </button>
                          </div>
                        )}
                        {examination.diagnoses[0]?.id && !treatmentSuggestionLoading && !treatmentSuggestionError && !treatmentSuggestion && (
                          <span className="text-[12.5px] text-slate-400">
                            Dựa trên chẩn đoán vừa lưu và lịch sử điều trị của bệnh nhân để gợi ý hướng xử lý, hỗ trợ bác sĩ tham khảo thêm.
                          </span>
                        )}
                        {!treatmentSuggestionLoading && treatmentSuggestion && (
                          <div className="flex flex-col gap-3">
                            <AiSummaryText text={treatmentSuggestion.suggestion} />
                            <div className="px-3 py-2 rounded-lg bg-amber-50 border border-amber-200">
                              <span className="text-[11px] font-semibold text-amber-700 leading-snug">{treatmentSuggestion.disclaimer}</span>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* RIGHT: Phiếu khám răng miệng */}
                  <div>
                    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2 shrink-0">
                        <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                        <span className="text-[14px] font-black text-slate-900">Thông tin khám răng miệng</span>
                      </div>
                      <div className="p-5 flex flex-col gap-5">

                        <ExamSection title="Tình trạng lợi – niêm mạc">
                          <div className="grid grid-cols-2 gap-3">
                            <ExamField label="Tình trạng lợi" placeholder="vd: Lợi hồng hào, không viêm"
                              value={form.gumCondition} onChange={v => setField("gumCondition", v)} disabled={!canEdit} />
                            <ExamField label="Tình trạng niêm mạc miệng" placeholder="vd: Bình thường, không tổn thương"
                              value={form.oralMucosaCondition} onChange={v => setField("oralMucosaCondition", v)} disabled={!canEdit} />
                            <ExamField label="Chảy máu lợi" placeholder="vd: Có / Không"
                              value={form.gumBleeding} onChange={v => setField("gumBleeding", v)} disabled={!canEdit} />
                            <ExamField label="Đau khi chạm / ăn nhai" placeholder="vd: Có / Không"
                              value={form.painOnChewing} onChange={v => setField("painOnChewing", v)} disabled={!canEdit} />
                          </div>
                        </ExamSection>

                        <ExamSection title="Tình trạng răng">
                          <div className="grid grid-cols-2 gap-3">
                            <ExamField label="Số răng hiện có" placeholder="vd: 28 răng"
                              value={form.teethCount} onChange={v => setField("teethCount", v)} disabled={!canEdit} />
                            <ExamField label="Răng sâu" placeholder="vd: Răng 16, 26 sâu mặt nhai"
                              value={form.decayedTeeth} onChange={v => setField("decayedTeeth", v)} disabled={!canEdit} />
                            <ExamField label="Răng mòn / nứt / vỡ" placeholder="vd: Răng 11 mòn cổ, 21 nứt cạnh cắn"
                              value={form.wornOrBrokenTeeth} onChange={v => setField("wornOrBrokenTeeth", v)} disabled={!canEdit} />
                            <ExamField label="Răng lung lay" placeholder="vd: Răng 31 độ 1"
                              value={form.looseTeeth} onChange={v => setField("looseTeeth", v)} disabled={!canEdit} />
                          </div>
                        </ExamSection>

                        <ExamSection title="Vệ sinh răng miệng">
                          <div className="grid grid-cols-2 gap-3">
                            <ExamField label="Cao răng" placeholder="vd: Ít / Trung bình / Nhiều"
                              value={form.tartar} onChange={v => setField("tartar", v)} disabled={!canEdit} />
                            <ExamField label="Mảng bám" placeholder="vd: Ít / Trung bình / Nhiều"
                              value={form.plaque} onChange={v => setField("plaque", v)} disabled={!canEdit} />
                            <ExamField label="Mùi hôi miệng" placeholder="vd: Có / Không"
                              value={form.badBreath} onChange={v => setField("badBreath", v)} disabled={!canEdit} />
                          </div>
                        </ExamSection>

                        <ExamSection title="Khớp thái dương hàm">
                          <ExamField label="Triệu chứng" placeholder="vd: Không đau, không tiếng kêu khớp"
                            value={form.tmjSymptoms} onChange={v => setField("tmjSymptoms", v)} disabled={!canEdit} />
                        </ExamSection>

                        <ExamSection title="Khớp cắn">
                          <div className="grid grid-cols-2 gap-3">
                            <ExamField label="Khớp cắn" placeholder="vd: Khớp cắn chuẩn / Cắn chéo / Cắn hở / Cắn sâu"
                              value={form.occlusion} onChange={v => setField("occlusion", v)} disabled={!canEdit} />
                            <ExamField label="Sai lệch khớp cắn" placeholder="vd: Không / Có"
                              value={form.occlusionDeviation} onChange={v => setField("occlusionDeviation", v)} disabled={!canEdit} />
                          </div>
                        </ExamSection>

                        <ExamSection title="Chẩn đoán">
                          <textarea
                            value={form.description}
                            onChange={e => setField("description", e.target.value)}
                            placeholder="Nhập chẩn đoán..."
                            rows={2}
                            disabled={!canEdit}
                            className="w-full px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-medium text-slate-700 placeholder:text-slate-300 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                          />
                        </ExamSection>

                        <ExamSection title="Kết quả & kế hoạch điều trị">
                          <textarea
                            value={form.conclusion}
                            onChange={e => setField("conclusion", e.target.value)}
                            placeholder="Nhập kế hoạch điều trị / tư vấn..."
                            rows={3}
                            disabled={!canEdit}
                            className="w-full px-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-medium text-slate-700 placeholder:text-slate-300 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                          />
                        </ExamSection>
                      </div>

                      {/* Footer ghim — nút lưu luôn thấy, không phải kéo xuống */}
                      <div className="px-5 py-3.5 border-t border-slate-100 bg-slate-50/40 flex items-center gap-3 shrink-0">
                        {diagSaved && (
                          <span className="text-[12px] text-green-600 font-bold animate-fade-in flex items-center gap-1">
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                            </svg>
                            Đã lưu
                          </span>
                        )}
                        {!canEdit && (
                          <span className="text-[12px] font-semibold text-amber-600">
                            {isFinished ? "Bật \"Chỉnh sửa\" để sửa phiếu khám." : "Bấm \"Bắt đầu khám\" để nhập phiếu khám."}
                          </span>
                        )}
                        <button
                          onClick={handleSaveDiagnosis}
                          disabled={!canEdit}
                          className="ml-auto flex items-center gap-2 px-5 py-2.5 bg-primary text-white text-[13px] font-black rounded-xl hover:bg-red-600 transition-all shadow-sm cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                          </svg>
                          Lưu phiếu khám
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* ─── TAB: LIỆU TRÌNH ─── */}
              {activeTab === "treatment" && (
                <TreatmentWorkspace appointmentId={id} editMode={editMode && isFinished} />
              )}

              {/* ─── TAB: ĐƠN THUỐC ─── */}
              {activeTab === "prescription" && (
                <PrescriptionWorkspace appointmentId={id} editMode={editMode && isFinished} />
              )}

              {/* ─── TAB: TÁI KHÁM ─── */}
              {activeTab === "followup" && (
                <FollowUpWorkspace appointmentId={id} editMode={editMode && isFinished} />
              )}

              {/* ─── TAB: VẬT TƯ ─── */}
              {activeTab === "materials" && (
                <MaterialWorkspace appointmentId={id} editMode={editMode && isFinished} />
              )}

            </div>{/* end left col */}

          </div>{/* end 2-col wrapper */}
        </div>
      </main>
    </div>
  );
}
