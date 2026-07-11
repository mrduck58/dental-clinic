"use client";

import { useState, useMemo, useEffect, useCallback } from "react";
import Link from "next/link";
import DentistSidebar from "../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../../hooks/useRequireDentist";
import { getDentistPatientsApi, type DentistPatientDto, type DentistPatientsResponse } from "../../../../lib/apiClient";
import { supabase } from "../../../../lib/supabaseClient";

type PatientStatus = "waiting" | "in_progress" | "done";

const STATUS_MAP: Record<string, PatientStatus> = {
  "Confirmed":      "waiting",
  "CheckedIn":      "waiting",
  "InProgress":     "in_progress",
  "PendingPayment": "done",
  "Completed":      "done",
};

const STATUS_CFG: Record<PatientStatus, { label: string; bar: string; badge: string; dot: string }> = {
  waiting:     { label: "Đang chờ",   bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",   dot: "bg-amber-500"  },
  in_progress: { label: "Đang khám",  bar: "bg-sky-400",    badge: "bg-sky-50 text-sky-700 border border-sky-200",         dot: "bg-sky-500"    },
  done:        { label: "Hoàn thành", bar: "bg-emerald-400",badge: "bg-emerald-50 text-emerald-700 border border-emerald-200", dot: "bg-emerald-500" },
};

function PatientRow({ p, idx }: { p: DentistPatientDto; idx: number }) {
  const status = STATUS_MAP[p.status] ?? "waiting";
  const s = STATUS_CFG[status];
  const initials = p.patientName.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase();

  const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
  };

  const time = fmtTime(p.appointmentDate);
  const shift = new Date(p.appointmentDate).getHours() < 12 ? "morning" : "afternoon";

  return (
    <div className="flex rounded-2xl border overflow-hidden transition-all hover:shadow-md bg-white border-slate-200/70 hover:-translate-y-px">
      {/* Status accent bar */}
      <div className={`w-1.5 shrink-0 ${s.bar}`} />

      <div className="flex items-center gap-5 px-5 py-4 flex-1 min-w-0">

        {/* Time + order */}
        <div className="flex flex-col items-center w-14 shrink-0">
          <span className="text-[19px] font-black text-slate-900 font-mono leading-none tabular-nums">{time}</span>
          <span className="text-[11px] font-bold text-slate-400 mt-1">#{idx + 1}</span>
        </div>

        {/* Divider */}
        <div className="w-px h-12 bg-slate-100 shrink-0" />

        {/* Avatar */}
        <div className={`w-11 h-11 rounded-xl flex items-center justify-center font-black text-[13px] shrink-0 ${
          p.gender === "Nữ" ? "bg-rose-50 text-rose-600 border border-rose-100" : "bg-sky-50 text-sky-700 border border-sky-100"
        }`}>
          {initials}
        </div>

        {/* Main info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-[15px] font-black leading-tight text-slate-400">{p.patientName}</span>
            {p.isNew && (
              <span className="px-1.5 py-0.5 bg-violet-100 text-violet-700 text-[10px] font-black rounded-md tracking-wide">MỚI</span>
            )}
            {p.isFollowUpVisit && (
              <span className="px-1.5 py-0.5 bg-indigo-100 text-indigo-700 text-[10px] font-black rounded-md tracking-wide">TÁI KHÁM</span>
            )}
            <span className="text-[12px] text-slate-400 font-semibold">{p.age} tuổi · {p.gender}</span>
          </div>
          <div className="flex items-center gap-1.5 mt-1">
            <svg className="w-3.5 h-3.5 text-slate-300 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
            </svg>
            <span className="text-[13.5px] font-semibold truncate text-slate-400">{p.symptoms ?? p.serviceName ?? "Khám tổng quát"}</span>
          </div>
          <div className="text-[12px] text-slate-400 font-medium mt-0.5 font-mono">{p.phone ?? "—"}</div>
        </div>

        {/* Status badge */}
        <div className="shrink-0 flex flex-col items-end gap-2">
          <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[12px] font-black whitespace-nowrap ${s.badge}`}>
            <span className={`w-1.5 h-1.5 rounded-full ${s.dot}`} />
            {s.label}
          </span>
          <span className={`text-[11px] font-bold px-2 py-0.5 rounded-lg ${shift === "morning" ? "bg-red-50 text-primary" : "bg-indigo-50 text-indigo-600"}`}>
            {shift === "morning" ? "Ca sáng" : "Ca chiều"}
          </span>
        </div>

        {/* Action button */}
        <Link
          href={`/dentist/patients/${p.appointmentId}`}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-[13px] font-bold transition-all shrink-0 bg-slate-100 text-slate-500 hover:bg-slate-200"
        >
          Xem hồ sơ
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
          </svg>
        </Link>
      </div>
    </div>
  );
}

function ShiftSection({ label, icon, patients }: {
  label: string; icon: string; patients: DentistPatientDto[];
}) {
  if (patients.length === 0) return null;
  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
          </svg>
          <span className="text-[13px] font-black text-slate-600 uppercase tracking-wider">{label}</span>
        </div>
        <span className="text-[12px] font-bold text-slate-400">{patients.length} bệnh nhân</span>
        <div className="flex-1 h-px bg-slate-200" />
      </div>
      <div className="flex flex-col gap-2.5">
        {patients.map((p, idx) => <PatientRow key={p.appointmentId} p={p} idx={idx} />)}
      </div>
    </div>
  );
}

export default function CompletedPatientsPage() {
  useRequireDentist();

  const [response, setResponse] = useState<DentistPatientsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [shiftFilter, setShiftFilter] = useState("all");

  const today = useMemo(() => {
    const d = new Date();
    return d.toLocaleDateString("vi-VN", { weekday: "long", day: "2-digit", month: "long", year: "numeric" });
  }, []);

  const loadPatients = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getDentistPatientsApi();
      setResponse(data);
      setError(null);
    } catch {
      setError("Không thể tải danh sách bệnh nhân");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadPatients();
    const channel = supabase
      .channel("dentist-patients-completed-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void loadPatients();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [loadPatients]);

  const patients = response?.patients ?? [];

  // Chỉ hiển thị bệnh nhân đã hoàn thành
  const filtered = useMemo(() => {
    return patients.filter((p) => {
      const q = search.toLowerCase();
      const matchSearch = q === "" || p.patientName.toLowerCase().includes(q) || (p.symptoms ?? "").toLowerCase().includes(q) || (p.phone ?? "").includes(q);
      const status = STATUS_MAP[p.status] ?? "waiting";
      const matchStatus = status === "done";
      const shift = new Date(p.appointmentDate).getHours() < 12 ? "morning" : "afternoon";
      const matchShift = shiftFilter === "all" || shift === shiftFilter;
      return matchSearch && matchStatus && matchShift;
    });
  }, [patients, search, shiftFilter]);

  const doneCount = patients.filter(p => (STATUS_MAP[p.status] ?? "waiting") === "done").length;

  const morning = filtered.filter(p => new Date(p.appointmentDate).getHours() < 12);
  const afternoon = filtered.filter(p => new Date(p.appointmentDate).getHours() >= 12);

  const selectCls = "px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 appearance-none cursor-pointer pr-8";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Hoàn Thành"
          subtitle={today}
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              <Link
                href="/dentist/patients"
                className="px-3 py-1.5 bg-primary/10 text-primary border border-primary/20 rounded-xl hover:bg-primary/20 transition-colors"
              >
                Bắt đầu khám
              </Link>
              <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">{doneCount} hoàn thành</span>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">

          {/* Search + Shift Filter */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/70 shadow-sm flex flex-col sm:flex-row gap-3">
            <div className="relative flex-1">
              <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
              </span>
              <input type="text" placeholder="Tìm tên, lý do khám, số điện thoại..."
                value={search} onChange={(e) => setSearch(e.target.value)}
                className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400" />
            </div>
            <div className="relative">
              <select value={shiftFilter} onChange={(e) => setShiftFilter(e.target.value)} className={selectCls}>
                <option value="all">Cả 2 ca</option>
                <option value="morning">Ca sáng</option>
                <option value="afternoon">Ca chiều</option>
              </select>
              <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
            </div>
          </div>

          {/* Loading state */}
          {loading && (
            <div className="flex items-center justify-center py-20">
              <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          )}

          {/* Error state */}
          {error && !loading && (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
              <p className="text-[14px] font-semibold text-red-500">{error}</p>
              <button onClick={() => void loadPatients()} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">
                Thử lại
              </button>
            </div>
          )}

          {/* Patient sections */}
          {!loading && !error && filtered.length > 0 ? (
            <div className="flex flex-col gap-7">
              <ShiftSection
                label="Ca sáng"
                icon="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z"
                patients={morning}
              />
              <ShiftSection
                label="Ca chiều"
                icon="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z"
                patients={afternoon}
              />
            </div>
          ) : !loading && !error && (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
              <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
              </div>
              <p className="text-[14px] font-bold text-slate-500">Chưa có bệnh nhân hoàn thành.</p>
              <p className="text-[13px] text-slate-400">Nhấn <Link href="/dentist/patients" className="text-primary font-semibold hover:underline">vào đây</Link> để xem bệnh nhân cần khám.</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
