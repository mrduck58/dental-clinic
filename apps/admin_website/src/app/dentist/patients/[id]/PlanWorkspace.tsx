"use client";

import { useState, useMemo, useEffect } from "react";
import ToothArchDiagram, { TOOTH_COLOR, type ToothState as TState } from "../../../../components/shared/ToothArchDiagram";
import DentistPageHeader from "../../../../components/shared/DentistPageHeader";
import {
  getServicesApi,
  getMedicinesApi,
  type DentistPatientDto,
  type ServiceDto,
  type MedicineDto
} from "../../../../lib/apiClient";

const STORAGE_KEY = "dental_clinic_treatment_plans";

interface Phase {
  id: string;
  name: string;
  services: { serviceId: string; tooth?: string }[];
  isCompleted: boolean;
  prescription: { id: string; medicineName: string; dosage: string; quantity: number; unit: string }[];
  followUpMonth?: number;
  followUpReason?: string;
}

interface PatientPlanData {
  diagnosis: string;
  clinicalNotes: string;
  teeth: TState;
  planType: "ngan_han" | "dai_han";
  selectedShortTermServiceId?: string;
  phases: Phase[];
  currentStep?: 1 | 2;
}

interface PlanWorkspaceProps {
  patient: DentistPatientDto;
  onBack?: () => void;
}

export default function PlanWorkspace({ patient, onBack }: PlanWorkspaceProps) {
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [medicines, setMedicines] = useState<MedicineDto[]>([]);

  // Workspace State
  const [diagnosis, setDiagnosis] = useState("");
  const [clinicalNotes, setClinicalNotes] = useState("");
  const [teeth, setTeeth] = useState<TState>({});
  const [planType, setPlanType] = useState<"ngan_han" | "dai_han">("ngan_han");
  const [selectedShortTermServiceId, setSelectedShortTermServiceId] = useState("");
  const [phases, setPhases] = useState<Phase[]>([]);
  const [selectedPhaseId, setSelectedPhaseId] = useState<string>("");
  const [currentStep, setCurrentStep] = useState<1 | 2>(1);

  // Phase manipulation
  const [editingPhaseId, setEditingPhaseId] = useState<string | null>(null);
  const [editingPhaseName, setEditingPhaseName] = useState("");

  // Prescriptions form
  const [showAddMed, setShowAddMed] = useState(false);
  const [medName, setMedName] = useState("");
  const [medDosage, setMedDosage] = useState("");
  const [medQty, setMedQty] = useState(1);
  const [medUnit, setMedUnit] = useState("viên");

  // Notifications
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);

  const showToast = (message: string, type: "success" | "error" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 3000);
  };

  useEffect(() => {
    getServicesApi().then(setServices).catch(() => {});
    getMedicinesApi().then(setMedicines).catch(() => {});
  }, []);

  // Load saved plan from localStorage
  useEffect(() => {
    try {
      const allSaved = JSON.parse(localStorage.getItem(STORAGE_KEY) || "{}");
      const saved = allSaved[patient.appointmentId] as PatientPlanData | undefined;
      if (saved) {
        setDiagnosis(saved.diagnosis || "");
        setClinicalNotes(saved.clinicalNotes || "");
        setTeeth(saved.teeth || {});
        setPlanType(saved.planType || "ngan_han");
        setSelectedShortTermServiceId(saved.selectedShortTermServiceId || "");
        setPhases(saved.phases || []);
        setCurrentStep(saved.currentStep || (saved.diagnosis ? 2 : 1));
        if (saved.phases && saved.phases.length > 0) {
          setSelectedPhaseId(saved.phases[0].id);
        }
      } else {
        // Reset defaults
        setDiagnosis("");
        setClinicalNotes("");
        setTeeth({});
        setPlanType("ngan_han");
        setSelectedShortTermServiceId("");
        setCurrentStep(1);
        const initialPhases = [
          {
            id: "p1",
            name: "Giai đoạn 1: Chữa Tủy & Khống Chế Viêm",
            services: [],
            isCompleted: false,
            prescription: [],
            followUpMonth: 1,
            followUpReason: "Kiểm tra tủy & theo dõi tiến trình"
          },
          {
            id: "p2",
            name: "Giai đoạn 2: Phục Hồi & Tái Tạo",
            services: [],
            isCompleted: false,
            prescription: [],
            followUpMonth: 2,
            followUpReason: "Thực hiện phục hình răng"
          }
        ];
        setPhases(initialPhases);
        setSelectedPhaseId(initialPhases[0].id);
      }
    } catch {
      // ignore parsing error
    }
  }, [patient]);

  // Save active plan to localStorage
  const handleSaveAll = (stepOverride?: 1 | 2) => {
    try {
      const allSaved = JSON.parse(localStorage.getItem(STORAGE_KEY) || "{}");
      const nextStep = stepOverride || currentStep;
      const data: PatientPlanData = {
        diagnosis,
        clinicalNotes,
        teeth,
        planType,
        selectedShortTermServiceId,
        phases,
        currentStep: nextStep
      };
      allSaved[patient.appointmentId] = data;
      localStorage.setItem(STORAGE_KEY, JSON.stringify(allSaved));
      showToast("Đã lưu phác đồ điều trị thành công!", "success");
    } catch {
      showToast("Không thể lưu phác đồ điều trị", "error");
    }
  };

  const handleFinishExamAndProceed = () => {
    if (!diagnosis.trim()) {
      showToast("Vui lòng nhập chuẩn đoán chính trước khi tiếp tục", "error");
      return;
    }
    setCurrentStep(2);
    handleSaveAll(2);
    showToast("Đã lưu kết quả khám. Chuyển sang lập kế hoạch điều trị!");
  };

  const handleAddPhase = () => {
    const nextIdx = phases.length + 1;
    const newId = "p_" + Date.now();
    const newPhase: Phase = {
      id: newId,
      name: `Giai đoạn ${nextIdx}: Giai đoạn mới`,
      services: [],
      isCompleted: false,
      prescription: [],
      followUpMonth: 1,
      followUpReason: "Tái khám theo dõi"
    };
    setPhases([...phases, newPhase]);
    setSelectedPhaseId(newId);
    showToast("Đã thêm giai đoạn mới");
  };

  const handleRemovePhase = (id: string) => {
    const updated = phases.filter(p => p.id !== id);
    setPhases(updated);
    if (selectedPhaseId === id && updated.length > 0) {
      setSelectedPhaseId(updated[0].id);
    }
    showToast("Đã xóa giai đoạn");
  };

  const handleStartEditPhase = (phase: Phase) => {
    setEditingPhaseId(phase.id);
    setEditingPhaseName(phase.name.split(": ").slice(1).join(": ") || phase.name);
  };

  const handleSavePhaseName = (phaseId: string) => {
    setPhases(phases.map((p, idx) => {
      if (p.id === phaseId) {
        return {
          ...p,
          name: `Giai đoạn ${idx + 1}: ${editingPhaseName}`
        };
      }
      return p;
    }));
    setEditingPhaseId(null);
    showToast("Đã cập nhật tên giai đoạn");
  };

  const handleAddServiceToPhase = (phaseId: string, serviceId: string) => {
    if (!serviceId) return;
    setPhases(phases.map(p => {
      if (p.id === phaseId) {
        return {
          ...p,
          services: [...p.services, { serviceId, tooth: "" }]
        };
      }
      return p;
    }));
    showToast("Đã chỉ định dịch vụ vào giai đoạn");
  };

  const handleUpdateServiceInPhase = (phaseId: string, serviceIdx: number, updatedFields: Partial<{ serviceId: string; tooth: string }>) => {
    setPhases(phases.map(p => {
      if (p.id === phaseId) {
        const copy = [...p.services];
        copy[serviceIdx] = { ...copy[serviceIdx], ...updatedFields };
        return { ...p, services: copy };
      }
      return p;
    }));
  };

  const handleRemoveServiceFromPhase = (phaseId: string, idx: number) => {
    setPhases(phases.map(p => {
      if (p.id === phaseId) {
        const copy = [...p.services];
        copy.splice(idx, 1);
        return { ...p, services: copy };
      }
      return p;
    }));
    showToast("Đã xóa dịch vụ khỏi giai đoạn");
  };

  const handleAddMedication = (phaseId: string) => {
    if (!medName.trim()) return;
    setPhases(phases.map(p => {
      if (p.id === phaseId) {
        return {
          ...p,
          prescription: [
            ...p.prescription,
            {
              id: "med_" + Date.now(),
              medicineName: medName,
              dosage: medDosage,
              quantity: medQty,
              unit: medUnit
            }
          ]
        };
      }
      return p;
    }));
    setMedName("");
    setMedDosage("");
    setMedQty(1);
    setShowAddMed(false);
    showToast("Đã thêm thuốc vào giai đoạn");
  };

  const handleRemoveMedication = (phaseId: string, medId: string) => {
    setPhases(phases.map(p => {
      if (p.id === phaseId) {
        return {
          ...p,
          prescription: p.prescription.filter(m => m.id !== medId)
        };
      }
      return p;
    }));
    showToast("Đã xóa thuốc");
  };

  const selectedPhaseIndex = useMemo(() => {
    const idx = phases.findIndex(p => p.id === selectedPhaseId);
    return idx === -1 ? 0 : idx;
  }, [phases, selectedPhaseId]);

  const patientHeaderSubtitle = (
    <div className="flex items-center mt-1">
      <span className="text-[12.5px] font-black text-red-650">
        Tiền sử bệnh lý: Huyết áp cao, dị ứng penicillin
      </span>
    </div>
  );

  return (
    <div className="flex-1 h-full min-h-0 flex flex-col overflow-hidden">
      {/* Page Header (with back button & patient profile title if in sidebar mode) */}
      {onBack && (
        <DentistPageHeader
          title={patient.patientName}
          subtitle={patientHeaderSubtitle}
          left={
            <div className="flex items-center gap-2.5">
              <button
                onClick={onBack}
                className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-700 transition-all shrink-0 cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                </svg>
              </button>
              <div className={`w-9 h-9 rounded-full flex items-center justify-center font-black text-[12px] border shrink-0 ${patient.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"}`}>
                {patient.patientName.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase()}
              </div>
            </div>
          }
        />
      )}

      {/* Save bar if in tab mode (which has no onBack callback) */}
      {!onBack && (
        <div className="bg-white px-8 py-3.5 border-b border-slate-200 flex justify-between items-center shrink-0">
          <div className="flex items-center gap-3">
            <span className="text-[13px] font-black text-slate-400 uppercase tracking-wide">Lập phác đồ điều trị</span>
            {patientHeaderSubtitle}
          </div>
        </div>
      )}

      {toast && (
        <div className={`fixed top-24 right-8 z-50 px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border animate-fade-in font-bold text-[14px] ${
          toast.type === "success" ? "bg-emerald-900 text-white border-emerald-800" : "bg-red-900 text-white border-red-800"
        }`}>
          <span>{toast.type === "success" ? "✓" : "⚠"}</span>
          <span>{toast.message}</span>
        </div>
      )}

      {/* 3 columns grid — strictly occupies remaining space */}
      <div className="flex-1 min-h-0 p-4 grid grid-cols-1 xl:grid-cols-3 gap-4 relative">
        
        {/* ──────── COLUMN 1: KHÁM & CHUẨN ĐOÁN ──────── */}
        <div className="bg-white rounded-xl border border-slate-200/70 shadow-sm flex flex-col h-full overflow-hidden">
          <div className="px-5 py-3 border-b border-slate-100 flex items-center gap-2 bg-slate-50/50 shrink-0">
            <svg className="w-5 h-5 text-teal-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <span className="text-[13.5px] font-black text-slate-800 uppercase tracking-wide">Khám & Chuẩn đoán</span>
          </div>
          
          <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-3 min-h-0">
            {/* Tooth diagram AT THE TOP */}
            <div className="flex flex-col gap-1.5 bg-slate-50 border border-slate-200 rounded-xl p-3.5 shrink-0">
              <div className="flex justify-between items-center mb-1">
                <span className="text-[11.5px] font-black text-slate-500 uppercase tracking-wide">Sơ đồ răng (Click trực tiếp răng để chỉ định):</span>
                <span className="text-[10px] font-extrabold text-red-600 bg-red-50 px-2 py-0.5 rounded-md">Cần điều trị</span>
              </div>
              <ToothArchDiagram
                teeth={teeth}
                selected={new Set()}
                onToothClick={num => {
                  setTeeth(prev => {
                    const isDecay = prev[num] === "decay";
                    return {
                      ...prev,
                      [num]: isDecay ? "normal" : "decay"
                    };
                  });
                }}
                showLegend={false}
              />
              
              {/* Selected teeth list */}
              {Object.entries(teeth).filter(([_, status]) => status === "decay").length > 0 && (
                <div className="flex items-center gap-2 flex-wrap mt-2 pt-2 border-t border-slate-200/50">
                  <span className="text-[10.5px] font-black text-slate-400">Răng chỉ định điều trị:</span>
                  <span className="text-[11.5px] font-black text-red-600 bg-red-50 border border-red-200 px-2.5 py-0.5 rounded-md shadow-3xs">
                    {Object.entries(teeth)
                      .filter(([_, status]) => status === "decay")
                      .map(([num]) => num)
                      .join(", ")}
                  </span>
                </div>
              )}
            </div>

            {/* Diagnosis */}
            <div className="flex flex-col gap-1 shrink-0">
              <label className="text-[11px] font-black text-slate-400 uppercase tracking-wide">Chuẩn đoán chính:</label>
              <textarea
                value={diagnosis}
                onChange={e => setDiagnosis(e.target.value)}
                placeholder="VD: K04.7 - Áp xe quanh chóp răng..."
                className="px-3 py-2 text-[12.5px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 h-14 resize-none"
              />
            </div>

            {/* Clinical Notes */}
            <div className="flex flex-col gap-1 shrink-0">
              <label className="text-[11px] font-black text-slate-400 uppercase tracking-wide">Ghi chú lâm sàng:</label>
              <textarea
                value={clinicalNotes}
                onChange={e => setClinicalNotes(e.target.value)}
                placeholder="VD: Sưng đau vùng răng hàm dưới..."
                className="px-3 py-2 text-[12.5px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 h-20 resize-none"
              />
            </div>

            {/* X-Ray uploads */}
            <div className="flex flex-col gap-1 shrink-0">
              <div className="flex justify-between items-center">
                <label className="text-[11px] font-black text-slate-400 uppercase tracking-wide">Hình ảnh X-Quang:</label>
              </div>
              <div className="flex gap-2 items-center flex-wrap">
                <div className="w-14 h-14 rounded-lg border border-dashed border-slate-200 flex flex-col items-center justify-center bg-slate-50/50 hover:bg-slate-100 hover:border-slate-300 transition-all cursor-pointer">
                  <span className="text-[16px] text-slate-400">+</span>
                </div>
                <div className="w-14 h-14 rounded-lg border border-slate-200 overflow-hidden relative group">
                  <img
                    src="https://images.unsplash.com/photo-1598256989800-fe5f95da9787?w=80&auto=format&fit=crop&q=60"
                    alt="Xray 1"
                    className="w-full h-full object-cover"
                  />
                </div>
                <div className="w-14 h-14 rounded-lg border border-slate-200 overflow-hidden relative group">
                  <img
                    src="https://images.unsplash.com/photo-1579684385127-1ef15d508118?w=80&auto=format&fit=crop&q=60"
                    alt="Xray 2"
                    className="w-full h-full object-cover"
                  />
                </div>
              </div>
            </div>

            {/* Save Diagnostic and Proceed to Step 2 Button */}
            {currentStep === 1 ? (
              <button
                onClick={handleFinishExamAndProceed}
                className="w-full bg-slate-900 hover:bg-slate-950 text-white transition-all py-3 rounded-lg text-[12.5px] font-black flex items-center justify-center gap-2 mt-2 shadow-md cursor-pointer shrink-0"
              >
                Lưu khám xong & Sang kế hoạch điều trị
              </button>
            ) : (
              <button
                onClick={() => {
                  setCurrentStep(1);
                  showToast("Đã chuyển về Bước 1 để chỉnh sửa chuẩn đoán");
                }}
                className="w-full bg-slate-100 hover:bg-slate-200 text-slate-700 transition-all py-2.5 rounded-lg text-[12px] font-bold flex items-center justify-center gap-2 mt-2 border border-slate-200 cursor-pointer shrink-0"
              >
                Sửa khám chuẩn đoán
              </button>
            )}
          </div>
        </div>

        {/* ──────── COLUMN 2: KẾ HOẠCH ĐIỀU TRỊ ──────── */}
        <div className="bg-white rounded-xl border border-slate-200/70 shadow-sm flex flex-col h-full overflow-hidden relative">
          {/* Overlay blocking if in Step 1 */}
          {currentStep === 1 && (
            <div className="absolute inset-0 bg-white/70 backdrop-blur-xs z-10 flex flex-col items-center justify-center text-center p-6 rounded-xl">
              <div className="w-12 h-12 rounded-full bg-indigo-50 flex items-center justify-center text-indigo-500 mb-3 shadow-inner">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
              </div>
              <span className="text-[14px] font-black text-slate-800">Kế hoạch điều trị khóa</span>
              <span className="text-[12px] text-slate-400 font-semibold mt-1.5 max-w-[200px]">
                Vui lòng hoàn thành bước "Khám & Chuẩn đoán" để tiếp tục lập phác đồ.
              </span>
            </div>
          )}

          <div className="px-5 py-3 border-b border-slate-100 flex items-center justify-between bg-slate-50/50 shrink-0">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25" />
              </svg>
              <span className="text-[13.5px] font-black text-slate-800 uppercase tracking-wide">Kế hoạch điều trị</span>
            </div>

            {/* Ngắn hạn vs Dài hạn toggler */}
            <div className="flex bg-slate-100 p-0.5 rounded-lg border border-slate-200">
              <button
                onClick={() => setPlanType("ngan_han")}
                className={`px-3 py-1 rounded-md text-[10.5px] font-extrabold transition-all cursor-pointer ${
                  planType === "ngan_han" ? "bg-primary text-white shadow-sm" : "text-slate-500 hover:text-slate-850"
                }`}
              >
                NGẮN HẠN
              </button>
              <button
                onClick={() => setPlanType("dai_han")}
                className={`px-3 py-1 rounded-md text-[10.5px] font-extrabold transition-all cursor-pointer ${
                  planType === "dai_han" ? "bg-primary text-white shadow-sm" : "text-slate-500 hover:text-slate-850"
                }`}
              >
                DÀI HẠN
              </button>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-3 min-h-0">
            {planType === "ngan_han" ? (
              /* ── Short Term Plan View ── */
              <div className="flex flex-col gap-4 py-4 shrink-0">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-black text-slate-400 uppercase tracking-wide">Chọn dịch vụ trực tiếp:</label>
                  <select
                    value={selectedShortTermServiceId}
                    onChange={e => setSelectedShortTermServiceId(e.target.value)}
                    className="w-full px-4 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-650 cursor-pointer"
                  >
                    <option value="">-- Chọn dịch vụ --</option>
                    {services.map(s => (
                      <option key={s.id} value={s.id}>
                        {s.name} - {(s.price || 0).toLocaleString("vi-VN")}đ
                      </option>
                    ))}
                  </select>
                </div>
                <p className="text-[12px] text-slate-400 font-semibold leading-relaxed">
                  * Lưu ý: Phác đồ ngắn hạn áp dụng cho các thủ thuật, điều trị nhanh trong ngày, không chia làm nhiều đợt.
                </p>
              </div>
            ) : (
              /* ── Long Term Plan (Phased) View ── */
              <div className="flex flex-col gap-3 flex-1 min-h-0">
                {/* Header Actions */}
                <div className="flex justify-between items-center shrink-0">
                  <span className="text-[11.5px] font-black text-slate-400 uppercase tracking-wide">Các giai đoạn ({phases.length}):</span>
                  <button
                    onClick={handleAddPhase}
                    className="px-3 py-1.5 bg-indigo-50 border border-indigo-200 hover:bg-indigo-100 rounded-lg text-[10.5px] font-black text-indigo-700 transition-all cursor-pointer"
                  >
                    + Tạo phase
                  </button>
                </div>

                {/* Phase timeline list */}
                <div className="flex-1 overflow-y-auto flex flex-col gap-3 min-h-0 pr-1">
                  {phases.map((p, idx) => {
                    const isCompleted = p.isCompleted;
                    const isSelected = p.id === selectedPhaseId;

                    return (
                      <div
                        key={p.id}
                        onClick={() => setSelectedPhaseId(p.id)}
                        className={`rounded-lg border p-3 flex flex-col gap-2 relative transition-all cursor-pointer ${
                          isSelected
                            ? "bg-indigo-50/20 border-indigo-500 shadow-sm ring-1 ring-indigo-500"
                            : "bg-slate-50/30 border-slate-200 hover:border-slate-350"
                        }`}
                      >
                        {/* Phase Badge Index */}
                        <div className="absolute top-3 left-3 w-5 h-5 rounded-full flex items-center justify-center font-bold text-[10px] shrink-0 bg-slate-200 text-slate-600">
                          {idx + 1}
                        </div>

                        <div className="pl-6 flex flex-col gap-1.5">
                          {/* Title & Edit */}
                          <div className="flex items-start justify-between gap-3">
                            {editingPhaseId === p.id ? (
                              <div className="flex-1 flex gap-2 items-center" onClick={e => e.stopPropagation()}>
                                <input
                                  type="text"
                                  value={editingPhaseName}
                                  onChange={e => setEditingPhaseName(e.target.value)}
                                  className="flex-1 px-2.5 py-0.5 text-[12px] border border-slate-200 rounded-lg font-bold bg-white focus:outline-none"
                                />
                                <button
                                  onClick={() => handleSavePhaseName(p.id)}
                                  className="text-emerald-600 text-[11px] font-bold"
                                >
                                  Lưu
                                </button>
                              </div>
                            ) : (
                              <div className="flex flex-col gap-0.5">
                                <span className="text-[13px] font-black text-slate-900 leading-tight">
                                  {p.name}
                                </span>
                                <span className={`self-start inline-block px-1.5 py-0.5 rounded-md text-[9px] font-extrabold mt-0.5 uppercase ${
                                  isCompleted
                                    ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                                    : "bg-indigo-50 text-indigo-700 border border-indigo-200"
                                }`}>
                                  {isCompleted ? "Hoàn thành" : "Hoạt động"}
                                </span>
                              </div>
                            )}

                            {editingPhaseId !== p.id && (
                              <div className="flex gap-1.5 shrink-0" onClick={e => e.stopPropagation()}>
                                <button
                                  onClick={() => handleStartEditPhase(p)}
                                  className="p-1 hover:bg-slate-100 rounded-lg text-slate-400 hover:text-slate-700 transition-all cursor-pointer"
                                >
                                  {/* SVG Edit Icon */}
                                  <svg className="w-3.5 h-3.5 text-slate-400 hover:text-slate-700" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                                  </svg>
                                </button>
                                {phases.length > 1 && (
                                  <button
                                    onClick={() => handleRemovePhase(p.id)}
                                    className="p-1 hover:bg-red-50 rounded-lg text-slate-400 hover:text-red-500 transition-all cursor-pointer"
                                  >
                                    {/* SVG Trash Icon */}
                                    <svg className="w-3.5 h-3.5 text-slate-400 hover:text-red-650" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                                    </svg>
                                  </button>
                                )}
                              </div>
                            )}
                          </div>

                          <div className="flex flex-col gap-2.5 mt-1">
                            {/* Assigned Services */}
                            <div className="flex flex-col gap-1">
                              {p.services.length === 0 ? (
                                <p className="text-[11.5px] text-slate-400 italic font-semibold">Chưa gán dịch vụ.</p>
                              ) : (
                                <div className="flex flex-col gap-1.5">
                                  {p.services.map((item, sIdx) => {
                                    return (
                                      <div key={sIdx} className="flex flex-col gap-1 bg-white p-2 rounded-lg border border-slate-150 shadow-2xs" onClick={e => e.stopPropagation()}>
                                        <div className="flex justify-between items-center gap-2">
                                          {/* Service editor select */}
                                          <select
                                            value={item.serviceId}
                                            onChange={e => handleUpdateServiceInPhase(p.id, sIdx, { serviceId: e.target.value })}
                                            className="flex-1 text-[12px] font-bold text-slate-700 bg-transparent border-0 border-b border-dashed border-slate-200 focus:border-primary focus:ring-0 px-0.5 py-0.5 cursor-pointer"
                                          >
                                            {services.map(x => (
                                              <option key={x.id} value={x.id}>
                                                {x.name}
                                              </option>
                                            ))}
                                          </select>
                                          <button
                                            onClick={() => handleRemoveServiceFromPhase(p.id, sIdx)}
                                            className="text-red-400 hover:text-red-650 font-bold text-[14px] cursor-pointer"
                                          >
                                            ×
                                          </button>
                                        </div>

                                        {/* Tooth input config for this service */}
                                        <div className="flex items-center gap-1.5 mt-0.5">
                                          <span className="text-[9.5px] text-slate-400 font-bold uppercase">Răng chỉ định:</span>
                                          <input
                                            type="text"
                                            value={item.tooth || ""}
                                            onChange={e => handleUpdateServiceInPhase(p.id, sIdx, { tooth: e.target.value })}
                                            placeholder="VD: R36, R37..."
                                            className="w-16 px-1.5 py-0.5 text-[11px] bg-slate-50 border border-slate-200 rounded-md focus:outline-none"
                                          />
                                        </div>
                                      </div>
                                    );
                                  })}
                                </div>
                              )}
                            </div>

                            {/* Add Service dropdown */}
                            <div className="flex flex-col gap-0.5 bg-slate-50 border border-slate-150 p-2 rounded-lg" onClick={e => e.stopPropagation()}>
                              <span className="text-[9.5px] font-bold text-slate-400 uppercase">Thêm dịch vụ & răng:</span>
                              <div className="flex gap-2">
                                <select
                                  onChange={e => {
                                    handleAddServiceToPhase(p.id, e.target.value);
                                    e.target.value = "";
                                  }}
                                  className="flex-1 px-2.5 py-1 text-[11px] bg-white border border-slate-200 rounded-lg cursor-pointer"
                                >
                                  <option value="">-- Chọn dịch vụ --</option>
                                  {services.map(s => (
                                    <option key={s.id} value={s.id}>
                                      {s.name}
                                    </option>
                                  ))}
                                </select>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>

                {/* Save Entire Treatment Plan Button here in Column 2 */}
                <button
                  onClick={() => handleSaveAll()}
                  className="w-full bg-slate-900 hover:bg-slate-950 text-white transition-all py-3 rounded-lg text-[12.5px] font-black tracking-wide shadow-md flex items-center justify-center gap-2 mt-1.5 cursor-pointer shrink-0"
                >
                  Lưu Toàn Bộ Lịch Trình
                </button>
              </div>
            )}
          </div>
        </div>

        {/* ──────── COLUMN 3: CHỈ ĐỊNH ĐIỀU TRỊ ──────── */}
        <div className="bg-white rounded-xl border border-slate-200/70 shadow-sm flex flex-col h-full overflow-hidden relative">
          {/* Overlay blocking if in Step 1 */}
          {currentStep === 1 && (
            <div className="absolute inset-0 bg-white/70 backdrop-blur-xs z-10 flex flex-col items-center justify-center text-center p-6 rounded-xl">
              <div className="w-12 h-12 rounded-full bg-indigo-50 flex items-center justify-center text-indigo-500 mb-3 shadow-inner">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
              </div>
              <span className="text-[14px] font-black text-slate-800">Chỉ định điều trị khóa</span>
              <span className="text-[12px] text-slate-400 font-semibold mt-1.5 max-w-[200px]">
                Vui lòng hoàn thành bước "Khám & Chuẩn đoán" để tiếp tục kê đơn thuốc.
              </span>
            </div>
          )}

          <div className="px-5 py-3 border-b border-slate-100 flex items-center justify-between bg-slate-50/50 shrink-0">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-indigo-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12" />
              </svg>
              <span className="text-[13.5px] font-black text-slate-800 uppercase tracking-wide">Chỉ định điều trị</span>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-3 min-h-0">
            {/* Active phase indicator */}
            {planType === "dai_han" && phases[selectedPhaseIndex] && (
              <div className="bg-indigo-50 border border-indigo-100 rounded-lg p-2.5 text-center shrink-0">
                <span className="text-[11.5px] font-bold text-indigo-700">
                  Đang chỉ định cho: <span className="font-extrabold underline">{phases[selectedPhaseIndex].name}</span>
                </span>
              </div>
            )}

            {/* Prescription Section */}
            <div className="flex flex-col gap-2.5 flex-1 min-h-0">
              <div className="flex justify-between items-center border-b border-slate-100 pb-1.5 shrink-0">
                <span className="text-[11.5px] font-black text-slate-900 uppercase tracking-wide">
                  Đơn thuốc {planType === "dai_han" && phases[selectedPhaseIndex] ? `(GD ${selectedPhaseIndex + 1})` : ""}:
                </span>
                <button
                  onClick={() => {
                    if (planType === "dai_han" && !phases[selectedPhaseIndex]) {
                      showToast("Vui lòng kích hoạt/thêm phase trước", "error");
                      return;
                    }
                    setShowAddMed(!showAddMed);
                  }}
                  className="flex items-center gap-1 text-[11px] font-black text-teal-600 hover:text-teal-700"
                >
                  Thêm thuốc
                </button>
              </div>

              {/* Add medicine subform */}
              {showAddMed && (
                <div className="bg-slate-50 border border-slate-200 rounded-lg p-3.5 flex flex-col gap-2 shrink-0">
                  <span className="text-[10.5px] font-black text-slate-800">Thêm thuốc mới</span>
                  <div className="flex flex-col gap-1.5">
                    <select
                      value={medName}
                      onChange={e => {
                        setMedName(e.target.value);
                        const selectedMed = medicines.find(m => m.name === e.target.value);
                        if (selectedMed) {
                          setMedUnit(selectedMed.unit || "viên");
                        }
                      }}
                      className="px-3 py-1.5 text-[11.5px] bg-white border border-slate-200 rounded-lg"
                    >
                      <option value="">-- Chọn thuốc hoặc tự nhập --</option>
                      {medicines.map(m => (
                        <option key={m.id} value={m.name}>
                          {m.name} ({m.unit})
                        </option>
                      ))}
                    </select>
                    <input
                      type="text"
                      placeholder="Hoặc tự nhập tên thuốc..."
                      value={medName}
                      onChange={e => setMedName(e.target.value)}
                      className="px-3 py-1.5 text-[11.5px] bg-white border border-slate-200 rounded-lg"
                    />
                    <div className="grid grid-cols-2 gap-2">
                      <input
                        type="text"
                        placeholder="Liều dùng..."
                        value={medDosage}
                        onChange={e => setMedDosage(e.target.value)}
                        className="px-3 py-1.5 text-[11.5px] bg-white border border-slate-200 rounded-lg"
                      />
                      <div className="flex gap-2 items-center">
                        <input
                          type="number"
                          min={1}
                          value={medQty}
                          onChange={e => setMedQty(Number(e.target.value))}
                          className="w-16 px-3 py-1.5 text-[11.5px] bg-white border border-slate-200 rounded-lg"
                        />
                        <span className="text-[11.5px] text-slate-500 font-bold">{medUnit}</span>
                      </div>
                    </div>
                  </div>
                  <div className="flex justify-end gap-2 mt-0.5">
                    <button
                      onClick={() => setShowAddMed(false)}
                      className="px-2.5 py-1 text-slate-400 hover:text-slate-600 text-[10.5px] font-bold"
                    >
                      Hủy
                    </button>
                    <button
                      onClick={() => {
                        const targetId = planType === "dai_han" ? phases[selectedPhaseIndex].id : "short_term";
                        handleAddMedication(targetId);
                      }}
                      className="px-3 py-1 bg-primary text-white rounded-lg text-[10.5px] font-black"
                    >
                      Đồng ý
                    </button>
                  </div>
                </div>
              )}

              {/* Display medicines list */}
              <div className="flex-1 overflow-y-auto flex flex-col gap-2 pr-1">
                {planType === "dai_han" ? (
                  phases[selectedPhaseIndex] && phases[selectedPhaseIndex].prescription.length === 0 ? (
                    <p className="text-[11.5px] text-slate-400 italic font-semibold">Chưa kê đơn thuốc cho phase này.</p>
                  ) : (
                    phases[selectedPhaseIndex] && phases[selectedPhaseIndex].prescription.map(item => (
                      <div key={item.id} className="flex justify-between items-center bg-slate-50/50 p-2.5 rounded-lg border border-slate-150 shrink-0">
                        <div className="flex flex-col">
                          <span className="text-[12px] font-bold text-slate-800">{item.medicineName}</span>
                          <span className="text-[9.5px] text-slate-400 mt-0.5">{item.dosage} · Số lượng: {item.quantity} {item.unit}</span>
                        </div>
                        <button
                          onClick={() => handleRemoveMedication(phases[selectedPhaseIndex].id, item.id)}
                          className="p-1 hover:bg-red-50 text-red-500 rounded-lg text-[13px]"
                        >
                          🗑
                        </button>
                      </div>
                    ))
                  )
                ) : (
                  <p className="text-[11.5px] text-slate-400 italic font-semibold">Kê đơn trực tiếp cho bệnh nhân.</p>
                )}
              </div>
            </div>

            {/* Follow-up Section */}
            <div className="flex flex-col gap-2 border-t border-slate-100 pt-3 shrink-0">
              <span className="text-[11.5px] font-black text-slate-900 uppercase tracking-wide">Lịch tái khám:</span>

              {planType === "dai_han" && phases[selectedPhaseIndex] ? (
                <div className="flex flex-col gap-2.5 bg-slate-50/50 border border-slate-150 rounded-lg p-3">
                  <div className="flex flex-col gap-1">
                    <span className="text-[9.5px] font-bold text-slate-400 uppercase">Tái khám sau (Tháng):</span>
                    <select
                      value={phases[selectedPhaseIndex].followUpMonth || 1}
                      onChange={e => {
                        const m = Number(e.target.value);
                        setPhases(phases.map((p, idx) => {
                          if (idx === selectedPhaseIndex) {
                            return { ...p, followUpMonth: m };
                          }
                          return p;
                        }));
                      }}
                      className="px-3 py-1.5 text-[11.5px] bg-white border border-slate-200 rounded-lg cursor-pointer font-bold text-slate-700"
                    >
                      <option value={1}>Sau 1 tháng (Tháng 1)</option>
                      <option value={2}>Sau 2 tháng (Tháng 2)</option>
                      <option value={3}>Sau 3 tháng (Tháng 3)</option>
                      <option value={6}>Sau 6 tháng (Tháng 6)</option>
                      <option value={12}>Sau 12 tháng (Tháng 12)</option>
                    </select>
                  </div>

                  <div className="flex flex-col gap-1">
                    <span className="text-[9.5px] font-bold text-slate-400 uppercase">Lý do tái khám:</span>
                    <input
                      type="text"
                      value={phases[selectedPhaseIndex].followUpReason || ""}
                      onChange={e => {
                        const text = e.target.value;
                        setPhases(phases.map((p, idx) => {
                          if (idx === selectedPhaseIndex) {
                            return { ...p, followUpReason: text };
                          }
                          return p;
                        }));
                      }}
                      placeholder="VD: Kiểm tra tủy, cắt chỉ..."
                      className="px-3 py-1.5 text-[11.5px] bg-white border border-slate-200 rounded-lg text-slate-700"
                    />
                  </div>
                </div>
              ) : (
                <p className="text-[11.5px] text-slate-400 italic font-semibold">Tái khám theo buổi đặt hẹn thông thường.</p>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Progress Stepper */}
      <div className="bg-white border-t border-slate-200 px-6 py-2 flex justify-between items-center shrink-0">
        <span className="text-[11px] font-black text-slate-400 uppercase tracking-wider">Tiến trình điều trị tổng thể</span>
        <div className="flex items-center gap-6 max-w-md w-full">
          <div className="flex items-center gap-1.5">
            <div className={`w-5 h-5 rounded-full flex items-center justify-center font-black text-[10px] ${
              currentStep >= 1 ? "bg-emerald-500 text-white" : "bg-slate-200 text-slate-500"
            }`}>
              1
            </div>
            <span className={`text-[11.5px] font-black ${
              currentStep >= 1 ? "text-emerald-600" : "text-slate-400"
            }`}>Khám</span>
          </div>
          <div className={`flex-1 h-0.5 shrink-0 ${currentStep >= 2 ? "bg-emerald-500" : "bg-slate-200"}`} />
          <div className="flex items-center gap-1.5">
            <div className={`w-5 h-5 rounded-full flex items-center justify-center font-black text-[10px] ${
              currentStep >= 2 ? "bg-indigo-600 text-white" : "bg-slate-200 text-slate-500"
            }`}>
              2
            </div>
            <span className={`text-[11.5px] font-black ${
              currentStep >= 2 ? "text-indigo-600" : "text-slate-400"
            }`}>Liệu trình</span>
          </div>
          <div className="flex-1 h-0.5 bg-slate-200 shrink-0" />
          <div className="flex items-center gap-1.5">
            <div className="w-5 h-5 rounded-full bg-slate-200 text-slate-500 flex items-center justify-center font-black text-[10px]">
              3
            </div>
            <span className="text-[11.5px] font-black text-slate-400">Thực hiện</span>
          </div>
        </div>
      </div>
    </div>
  );
}
