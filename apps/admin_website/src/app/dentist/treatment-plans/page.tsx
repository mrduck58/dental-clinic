"use client";

import { useState, useMemo, useEffect, useCallback } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import PlanWorkspace from "../patients/[id]/PlanWorkspace";
import { useRequireDentist } from "../../../hooks/useRequireDentist";
import { supabase } from "../../../lib/supabaseClient";
import {
  getDentistPatientsApi,
  getDentistPastPatientsApi,
  type DentistPatientDto
} from "../../../lib/apiClient";

const STATUS_MAP: Record<string, "waiting" | "in_progress" | "done"> = {
  "Confirmed":      "waiting",
  "CheckedIn":      "waiting",
  "InProgress":     "in_progress",
  "PendingPayment": "done",
  "Completed":      "done",
};

const STATUS_CFG = {
  waiting:     { label: "Đang chờ",   bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",   dot: "bg-amber-500"  },
  in_progress: { label: "Đang khám",  bar: "bg-sky-400",    badge: "bg-sky-50 text-sky-700 border border-sky-200",         dot: "bg-sky-500"    },
  done:        { label: "Hoàn thành", bar: "bg-emerald-400",badge: "bg-emerald-50 text-emerald-700 border border-emerald-200", dot: "bg-emerald-500" },
};

interface PatientRowProps {
  p: DentistPatientDto;
  onSelect: (p: DentistPatientDto) => void;
}

function PatientRow({ p, onSelect }: PatientRowProps) {
  const status = STATUS_MAP[p.status] ?? "waiting";
  const s = STATUS_CFG[status];
  const initials = p.patientName.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase();

  const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
  };

  const fmtDate = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getDate()).padStart(2,"0")}/${String(d.getMonth() + 1).padStart(2,"0")}/${d.getFullYear()}`;
  };

  const time = fmtTime(p.appointmentDate);
  const date = fmtDate(p.appointmentDate);

  return (
    <div className="flex rounded-xl border overflow-hidden transition-all hover:shadow-md bg-white border-slate-200/70 hover:-translate-y-px">
      <div className={`w-1.5 shrink-0 ${s.bar}`} />

      <div className="flex items-center gap-5 px-5 py-4 flex-1 min-w-0">
        {/* Time + Date */}
        <div className="flex flex-col items-center w-24 shrink-0">
          <span className="text-[17px] font-black text-slate-900 font-mono leading-none tabular-nums">{time}</span>
          <span className="text-[11px] font-bold text-slate-400 mt-1.5 font-mono">{date}</span>
        </div>

        {/* Divider */}
        <div className="w-px h-12 bg-slate-100 shrink-0" />

        {/* Avatar */}
        <div className={`w-11 h-11 rounded-lg flex items-center justify-center font-black text-[13px] border shrink-0 ${
          p.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"
        }`}>
          {initials}
        </div>

        {/* Main info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-[15px] font-black leading-tight text-slate-700">{p.patientName}</span>
            {p.isNew && (
              <span className="px-1.5 py-0.5 bg-violet-100 text-violet-700 text-[10px] font-black rounded-md tracking-wide">MỚI</span>
            )}
            <span className="text-[12px] text-slate-400 font-semibold">{p.age} tuổi · {p.gender}</span>
          </div>
          <div className="flex items-center gap-1.5 mt-1">
            <svg className="w-3.5 h-3.5 text-slate-300 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
            </svg>
            <span className="text-[13.5px] font-semibold truncate text-slate-500">{p.symptoms ?? p.serviceName ?? "Khám tổng quát"}</span>
          </div>
          <div className="text-[12px] text-slate-400 font-medium mt-0.5 font-mono">{p.phone ?? "—"}</div>
        </div>

        {/* Status badge */}
        <div className="shrink-0 flex flex-col items-end">
          <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[12px] font-black whitespace-nowrap ${s.badge}`}>
            <span className={`w-1.5 h-1.5 rounded-full ${s.dot}`} />
            {s.label}
          </span>
        </div>

        {/* Action button */}
        <button
          onClick={() => onSelect(p)}
          className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-[13px] font-bold transition-all shrink-0 bg-red-50 text-primary border border-primary/20 hover:bg-primary hover:text-white cursor-pointer"
        >
          Lập phác đồ
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
          </svg>
        </button>
      </div>
    </div>
  );
}

interface DateSectionProps {
  dateLabel: string;
  patients: DentistPatientDto[];
  onSelect: (p: DentistPatientDto) => void;
}

function DateSection({ dateLabel, patients, onSelect }: DateSectionProps) {
  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-3">
        <span className="text-[13px] font-black text-slate-600 uppercase tracking-wider">Ngày {dateLabel}</span>
        <span className="text-[12px] font-bold text-slate-400">{patients.length} bệnh nhân</span>
        <div className="flex-1 h-px bg-slate-200" />
      </div>
      <div className="flex flex-col gap-2.5">
        {patients.map((p) => (
          <PatientRow key={p.appointmentId} p={p} onSelect={onSelect} />
        ))}
      </div>
    </div>
  );
}

export default function TreatmentPlansPage() {
  useRequireDentist();

  // Patients list
  const [patients, setPatients] = useState<DentistPatientDto[]>([]);
  const [loadingPatients, setLoadingPatients] = useState(true);
  const [errorPatients, setErrorPatients] = useState<string | null>(null);

  // Selected patient for planning
  const [selectedPatient, setSelectedPatient] = useState<DentistPatientDto | null>(null);
  const [searchPatient, setSearchPatient] = useState("");

  // Load all patients (Today + Past)
  const loadPatients = useCallback(async () => {
    try {
      setLoadingPatients(true);
      const [todayRes, pastRes] = await Promise.all([
        getDentistPatientsApi().catch(() => ({ patients: [] })),
        getDentistPastPatientsApi().catch(() => [])
      ]);

      const todayList = todayRes.patients || [];
      const pastList = pastRes || [];

      // Deduplicate by appointmentId
      const map = new Map<string, DentistPatientDto>();
      pastList.forEach(p => map.set(p.appointmentId, p));
      todayList.forEach(p => map.set(p.appointmentId, p));

      const merged = Array.from(map.values());
      setPatients(merged);
      setErrorPatients(null);
    } catch {
      setErrorPatients("Không thể tải danh sách bệnh nhân");
    } finally {
      setLoadingPatients(false);
    }
  }, []);

  // Initial load & supabase listener
  useEffect(() => {
    void loadPatients();
    const channel = supabase
      .channel("dentist-treatment-plans-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void loadPatients();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [loadPatients]);

  // Filter patients by search query
  const filteredPatients = useMemo(() => {
    const q = searchPatient.toLowerCase();
    return patients.filter(p =>
      p.patientName.toLowerCase().includes(q) ||
      (p.phone ?? "").includes(q) ||
      p.appointmentCode.toLowerCase().includes(q) ||
      (p.symptoms ?? "").toLowerCase().includes(q) ||
      (p.serviceName ?? "").toLowerCase().includes(q)
    );
  }, [patients, searchPatient]);

  // Group patients by date (dd/MM/yyyy)
  const groupedPatients = useMemo(() => {
    const map: Record<string, DentistPatientDto[]> = {};
    for (const p of filteredPatients) {
      const d = new Date(p.appointmentDate);
      const key = `${String(d.getDate()).padStart(2,"0")}/${String(d.getMonth() + 1).padStart(2,"0")}/${d.getFullYear()}`;
      if (!map[key]) map[key] = [];
      map[key].push(p);
    }
    return Object.entries(map).sort((a, b) => {
      const [dayA, monthA, yearA] = a[0].split("/").map(Number);
      const [dayB, monthB, yearB] = b[0].split("/").map(Number);
      return new Date(yearB, monthB - 1, dayB).getTime() - new Date(yearA, monthA - 1, dayA).getTime();
    });
  }, [filteredPatients]);

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="treatment-plans" />

      <main className="flex-1 flex flex-col min-w-0">
        {!selectedPatient && (
          <DentistPageHeader
            title="Phác Đồ Điều Trị"
            subtitle="Chọn bệnh nhân để thiết lập phác đồ"
          />
        )}

        {!selectedPatient ? (
          /* ──────── LIST VIEW ──────── */
          <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
            <div className="bg-white px-5 py-4 rounded-xl border border-slate-200/70 shadow-sm flex flex-col gap-4">
              <span className="text-[15px] font-black text-slate-900">Danh sách bệnh nhân</span>
              <div className="relative">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm tên bệnh nhân, lý do khám, dịch vụ, số điện thoại..."
                  value={searchPatient}
                  onChange={e => setSearchPatient(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
                />
              </div>
            </div>

            {loadingPatients ? (
              <div className="flex items-center justify-center py-20">
                <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
              </div>
            ) : errorPatients ? (
              <div className="bg-white rounded-xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
                <p className="text-[14px] font-semibold text-red-500">{errorPatients}</p>
                <button onClick={() => void loadPatients()} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-lg cursor-pointer">
                  Thử lại
                </button>
              </div>
            ) : groupedPatients.length > 0 ? (
              <div className="flex flex-col gap-7">
                {groupedPatients.map(([dateKey, list]) => (
                  <DateSection
                    key={dateKey}
                    dateLabel={dateKey}
                    patients={list}
                    onSelect={setSelectedPatient}
                  />
                ))}
              </div>
            ) : (
              <div className="bg-white rounded-xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
                <p className="text-[14px] font-bold text-slate-500">Không tìm thấy bệnh nhân nào.</p>
              </div>
            )}
          </div>
        ) : (
          /* ──────── 3-COLUMN WORKSPACE VIEW ──────── */
          <PlanWorkspace patient={selectedPatient} onBack={() => setSelectedPatient(null)} />
        )}
      </main>
    </div>
  );
}
