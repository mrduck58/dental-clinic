"use client";

import { useState, useMemo, useEffect, useCallback } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import PlanWorkspace from "../patients/[id]/PlanWorkspace";
import { useRequireDentist } from "../../../hooks/useRequireDentist";
import {
  getDentistPatientsApi,
  getDentistPastPatientsApi,
  type DentistPatientDto
} from "../../../lib/apiClient";

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

      const merged = Array.from(map.values()).sort((a, b) =>
        new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime()
      );

      setPatients(merged);
      setErrorPatients(null);
    } catch {
      setErrorPatients("Không thể tải danh sách bệnh nhân");
    } finally {
      setLoadingPatients(false);
    }
  }, []);

  // Initial load
  useEffect(() => {
    void loadPatients();
  }, [loadPatients]);

  // Group patients by selected list search
  const filteredPatients = useMemo(() => {
    const q = searchPatient.toLowerCase();
    return patients.filter(p =>
      p.patientName.toLowerCase().includes(q) ||
      (p.phone ?? "").includes(q) ||
      p.appointmentCode.toLowerCase().includes(q)
    );
  }, [patients, searchPatient]);

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
            <div className="bg-white p-6 rounded-2xl border border-slate-200/70 shadow-sm flex flex-col gap-4">
              <span className="text-[15px] font-black text-slate-900">Danh sách bệnh nhân</span>
              <div className="relative">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm tên bệnh nhân, số điện thoại hoặc mã lịch hẹn..."
                  value={searchPatient}
                  onChange={e => setSearchPatient(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
                />
              </div>
            </div>

            {loadingPatients ? (
              <div className="flex items-center justify-center py-20">
                <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
              </div>
            ) : errorPatients ? (
              <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
                <p className="text-[14px] font-semibold text-red-500">{errorPatients}</p>
                <button onClick={() => void loadPatients()} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">
                  Thử lại
                </button>
              </div>
            ) : filteredPatients.length > 0 ? (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {filteredPatients.map(p => {
                  const initials = p.patientName.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase();
                  return (
                    <div key={p.appointmentId} className="bg-white rounded-2xl border border-slate-200/70 p-5 flex flex-col justify-between hover:shadow-md transition-all">
                      <div className="flex gap-4">
                        <div className={`w-12 h-12 rounded-xl flex items-center justify-center font-black text-[14px] border ${
                          p.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"
                        }`}>
                          {initials}
                        </div>
                        <div className="min-w-0 flex-1">
                          <div className="text-[14.5px] font-black text-slate-900 truncate leading-tight">{p.patientName}</div>
                          <div className="text-[12px] text-slate-400 font-semibold mt-1">{p.age} tuổi · {p.gender}</div>
                          <div className="text-[12px] text-slate-400 font-semibold font-mono mt-0.5">{p.phone ?? "—"}</div>
                        </div>
                      </div>
                      <div className="border-t border-slate-100 mt-4 pt-3 flex items-center justify-between gap-2">
                        <span className="text-[11px] text-slate-400 font-bold font-mono">#{p.appointmentCode}</span>
                        <button
                          onClick={() => setSelectedPatient(p)}
                          className="px-3.5 py-2 bg-red-50 text-primary border border-primary/20 hover:bg-primary hover:text-white rounded-xl text-[12.5px] font-black transition-all cursor-pointer"
                        >
                          Lập phác đồ
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            ) : (
              <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
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
