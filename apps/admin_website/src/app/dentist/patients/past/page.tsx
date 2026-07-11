"use client";

import { useState, useMemo, useEffect, useCallback } from "react";
import Link from "next/link";
import DentistSidebar from "../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../../hooks/useRequireDentist";
import { getDentistPastPatientsApi, type DentistPatientDto } from "../../../../lib/apiClient";
import { supabase } from "../../../../lib/supabaseClient";

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

function PatientRow({ p, idx }: { p: DentistPatientDto; idx: number }) {
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
    <div className="flex rounded-2xl border overflow-hidden transition-all hover:shadow-md bg-white border-slate-200/70 hover:-translate-y-px">
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
        <div className={`w-11 h-11 rounded-xl flex items-center justify-center font-black text-[13px] shrink-0 ${
          p.gender === "Nữ" ? "bg-rose-50 text-rose-600 border border-rose-100" : "bg-sky-50 text-sky-700 border border-sky-100"
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
            {p.isFollowUpVisit && (
              <span className="px-1.5 py-0.5 bg-indigo-100 text-indigo-700 text-[10px] font-black rounded-md tracking-wide">TÁI KHÁM</span>
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
          <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[12px] font-black whitespace-nowrap ${s.badge}`}>
            <span className={`w-1.5 h-1.5 rounded-full ${s.dot}`} />
            {s.label}
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

function DateSection({ dateLabel, patients }: { dateLabel: string; patients: DentistPatientDto[] }) {
  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-3">
        <span className="text-[13px] font-black text-slate-600 uppercase tracking-wider">Ngày {dateLabel}</span>
        <span className="text-[12px] font-bold text-slate-400">{patients.length} bệnh nhân</span>
        <div className="flex-1 h-px bg-slate-200" />
      </div>
      <div className="flex flex-col gap-2.5">
        {patients.map((p, idx) => <PatientRow key={p.appointmentId} p={p} idx={idx} />)}
      </div>
    </div>
  );
}

export default function PastPatientsPage() {
  useRequireDentist();

  const [patients, setPatients] = useState<DentistPatientDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  const loadPatients = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getDentistPastPatientsApi();
      setPatients(data);
      setError(null);
    } catch {
      setError("Không thể tải danh sách bệnh nhân đã từng khám");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadPatients();
    const channel = supabase
      .channel("dentist-patients-past-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void loadPatients();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [loadPatients]);

  const filtered = useMemo(() => {
    return patients.filter((p) => {
      const q = search.toLowerCase();
      return q === "" ||
        p.patientName.toLowerCase().includes(q) ||
        (p.symptoms ?? "").toLowerCase().includes(q) ||
        (p.phone ?? "").includes(q) ||
        (p.serviceName ?? "").toLowerCase().includes(q);
    });
  }, [patients, search]);

  // Group patients by date (dd/MM/yyyy)
  const grouped = useMemo(() => {
    const map: Record<string, DentistPatientDto[]> = {};
    for (const p of filtered) {
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
  }, [filtered]);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="past-patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Lịch Sử Khám"
          subtitle="Bệnh nhân đã từng khám"
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">
                Tổng cộng: {patients.length} bệnh nhân
              </span>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Search bar */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/70 shadow-sm flex flex-col sm:flex-row gap-3">
            <div className="relative flex-1">
              <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Tìm tên bệnh nhân, lý do khám, dịch vụ, số điện thoại..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
              />
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

          {/* Grouped patient sections */}
          {!loading && !error && grouped.length > 0 ? (
            <div className="flex flex-col gap-7">
              {grouped.map(([dateKey, list]) => (
                <DateSection key={dateKey} dateLabel={dateKey} patients={list} />
              ))}
            </div>
          ) : !loading && !error && (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
              <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </div>
              <p className="text-[14px] font-bold text-slate-500">Chưa có dữ liệu lịch sử bệnh nhân.</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
