"use client";

import { useState, useMemo, useEffect, useCallback } from "react";
import Link from "next/link";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";
import { getDentistPatientsApi, type DentistPatientDto, type DentistPatientsResponse } from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";

const todayIso = () => {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
};

type PatientStatus = "waiting" | "in_progress" | "done";

const STATUS_MAP: Record<string, PatientStatus> = {
  "Confirmed":      "waiting",
  "CheckedIn":      "waiting",
  "InProgress":     "in_progress",
  "PendingPayment": "done",
  "Completed":      "done",
};

const STATUS_FILTERS = [
  { key: "all", label: "Tất cả", statuses: ["waiting", "in_progress", "done"] as PatientStatus[], color: "bg-slate-100 text-slate-600 border-slate-200" },
  { key: "waiting", label: "Chờ khám", statuses: ["waiting"] as PatientStatus[], color: "bg-amber-50 text-amber-700 border-amber-200" },
  { key: "in_progress", label: "Đang khám", statuses: ["in_progress"] as PatientStatus[], color: "bg-sky-50 text-sky-700 border-sky-200" },
  { key: "done", label: "Hoàn thành", statuses: ["done"] as PatientStatus[], color: "bg-emerald-50 text-emerald-700 border-emerald-200" },
];

const STATUS_CFG: Record<PatientStatus, { label: string; bar: string; badge: string; dot: string }> = {
  waiting:     { label: "Đang chờ",   bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",   dot: "bg-amber-500"  },
  in_progress: { label: "Đang khám",  bar: "bg-sky-400",    badge: "bg-sky-50 text-sky-700 border border-sky-200",         dot: "bg-sky-500"    },
  done:        { label: "Hoàn thành", bar: "bg-emerald-400",badge: "bg-emerald-50 text-emerald-700 border border-emerald-200", dot: "bg-emerald-500" },
};

function PatientRow({ p, idx, isTopWaiting }: { p: DentistPatientDto; idx: number; isTopWaiting?: boolean }) {
  const status = STATUS_MAP[p.status] ?? "waiting";
  const s = STATUS_CFG[status];
  const isActive = status === "in_progress";
  const isDone   = status === "done";
  const isWaiting = status === "waiting";
  const initials = p.patientName.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase();

  const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
  };

  const fmtWait = (mins?: number) => {
    if (!mins || mins <= 0) return null;
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return h > 0 ? `${h}h${m > 0 ? `${String(m).padStart(2, "0")}p` : ""}` : `${m}p`;
  };

  const apptTime = fmtTime(p.appointmentDate);
  const checkInTime = p.checkedInAt ? fmtTime(p.checkedInAt) : null;
  const shift = new Date(p.appointmentDate).getHours() < 12 ? "morning" : "afternoon";
  const waitDuration = fmtWait(p.waitMinutes);

  return (
    <div className={`flex rounded-2xl border overflow-hidden transition-all hover:shadow-md ${
      isActive
        ? "bg-white border-sky-300 shadow-sm shadow-sky-100 ring-2 ring-sky-100"
        : isTopWaiting
          ? "bg-white border-amber-300 shadow-sm shadow-amber-100/60 ring-2 ring-amber-100"
          : isDone
            ? "bg-white border-slate-200/60 opacity-85 hover:opacity-100"
            : "bg-white border-slate-200/70 hover:-translate-y-px"
    }`}>
      {/* Status accent bar */}
      <div className={`w-1.5 shrink-0 ${s.bar}`} />

      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 sm:px-5 sm:py-4 flex-1 min-w-0">
        <div className="flex items-center gap-3 sm:gap-4 flex-1 min-w-0">
          {/* Queue number + Time */}
          <div className="flex flex-col items-center w-12 sm:w-14 shrink-0">
            <span className="text-[17px] sm:text-[19px] font-black text-slate-900 font-mono leading-none tabular-nums">
              {apptTime}
            </span>
            <span className={`text-[11px] font-black px-2 py-0.5 rounded-md mt-1.5 tabular-nums ${
              isActive
                ? "bg-sky-100 text-sky-700"
                : isTopWaiting
                  ? "bg-amber-100 text-amber-800 ring-1 ring-amber-300/60"
                  : isDone
                    ? "bg-slate-100 text-slate-500"
                    : "bg-amber-50 text-amber-700 border border-amber-200/60"
            }`}>
              #{p.queueNumber || idx + 1}
            </span>
          </div>

          {/* Divider */}
          <div className="w-px h-10 sm:h-12 bg-slate-100 shrink-0" />

          {/* Avatar */}
          <div className={`w-10 h-10 sm:w-11 sm:h-11 rounded-xl flex items-center justify-center font-black text-[13px] shrink-0 ${
            p.gender === "Nữ" ? "bg-rose-50 text-rose-600 border border-rose-100" : "bg-sky-50 text-sky-700 border border-sky-100"
          }`}>
            {initials}
          </div>

          {/* Main info */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className={`text-[15px] font-black leading-tight ${isDone ? "text-slate-400" : "text-slate-900"}`}>{p.patientName}</span>
              {isTopWaiting && (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-amber-500 text-white text-[10px] font-black rounded-md tracking-wide shadow-sm shadow-amber-500/30 animate-pulse">
                  ĐẦU HÀNG ĐỢI
                </span>
              )}
              {p.isNew && (
                <span className="px-1.5 py-0.5 bg-violet-100 text-violet-700 text-[10px] font-black rounded-md tracking-wide">MỚI</span>
              )}
              {p.isFollowUpVisit && (
                <span className="px-1.5 py-0.5 bg-indigo-100 text-indigo-700 text-[10px] font-black rounded-md tracking-wide">TÁI KHÁM</span>
              )}
              <span className="text-[12px] text-slate-400 font-semibold">{p.age} tuổi · {p.gender}</span>
            </div>

            <div className="flex items-center gap-2 mt-1 flex-wrap">
              <span className={`text-[13px] sm:text-[13.5px] font-semibold truncate ${isDone ? "text-slate-400" : "text-slate-700"}`}>
                {p.symptoms ?? p.serviceName ?? "Khám tổng quát"}
              </span>
              {checkInTime && isWaiting && (
                <span className="text-[11.5px] font-bold text-slate-400">
                  · Check-in {checkInTime}
                </span>
              )}
              {waitDuration && isWaiting && (
                <span className="inline-flex items-center px-2 py-0.5 bg-amber-50 text-amber-700 border border-amber-100 rounded-md text-[11px] font-black">
                  đã chờ {waitDuration}
                </span>
              )}
            </div>
            <div className="text-[12px] text-slate-400 font-medium mt-0.5 font-mono">{p.phone ?? "—"}</div>
          </div>
        </div>

        {/* Status + Action buttons */}
        <div className="flex items-center justify-between sm:justify-end gap-2.5 shrink-0 border-t sm:border-t-0 pt-3 sm:pt-0 border-slate-100 w-full sm:w-auto">
          {/* Status badge */}
          <div className="flex items-center gap-1.5">
            <span className={`inline-flex items-center gap-1.5 px-2.5 sm:px-3 py-1.5 rounded-xl text-[11.5px] sm:text-[12px] font-black whitespace-nowrap ${s.badge}`}>
              <span className={`w-1.5 h-1.5 rounded-full ${s.dot} ${isActive ? "animate-pulse" : ""}`} />
              {s.label}
            </span>
            <span className={`text-[11px] font-bold px-2 py-1 rounded-lg ${shift === "morning" ? "bg-red-50 text-primary" : "bg-indigo-50 text-indigo-600"}`}>
              {shift === "morning" ? "Ca sáng" : "Ca chiều"}
            </span>
          </div>

          {/* Action buttons */}
          <div className="flex items-center gap-2">
            {isDone && (
              <Link
                href={`/dentist/patients/${p.appointmentId}?edit=1`}
                className="flex items-center gap-1.5 px-3 py-2 rounded-xl text-[12.5px] sm:text-[13px] font-bold transition-all bg-amber-50 text-amber-700 border border-amber-200 hover:bg-amber-100 cursor-pointer"
              >
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
                </svg>
                Sửa
              </Link>
            )}
            <Link
              href={`/dentist/patients/${p.appointmentId}`}
              className={`flex items-center gap-1.5 sm:gap-2 px-3.5 sm:px-4 py-2 rounded-xl text-[12.5px] sm:text-[13px] font-bold transition-all whitespace-nowrap cursor-pointer ${
                isActive
                  ? "bg-sky-600 text-white hover:bg-sky-700 shadow-sm shadow-sky-600/25"
                  : isTopWaiting
                    ? "bg-primary text-white hover:bg-red-600 shadow-sm shadow-primary/30"
                    : isDone
                      ? "bg-slate-100 text-slate-500 hover:bg-slate-200"
                      : "bg-red-50 text-primary border border-primary/20 hover:bg-primary hover:text-white"
              }`}
            >
              {isActive ? "Tiếp tục khám" : isTopWaiting ? "Khám ngay" : isDone ? "Xem hồ sơ" : "Chi tiết"}
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
              </svg>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function DentistPatientsPage() {
  useRequireDentist();

  const [response, setResponse] = useState<DentistPatientsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [shiftFilter, setShiftFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");

  const [selectedDate, setSelectedDate] = useState(todayIso());

  const dateLabel = useMemo(
    () => new Date(selectedDate + "T00:00:00").toLocaleDateString("vi-VN", { weekday: "long", day: "2-digit", month: "long", year: "numeric" }),
    [selectedDate]
  );

  const loadPatients = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getDentistPatientsApi(selectedDate);
      setResponse(data);
      setError(null);
    } catch {
      setError("Không thể tải danh sách bệnh nhân");
    } finally {
      setLoading(false);
    }
  }, [selectedDate]);

  useEffect(() => {
    void loadPatients();
    const channel = supabase
      .channel("dentist-patients-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void loadPatients();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [loadPatients]);

  const patients = response?.patients ?? [];

  const filtered = useMemo(() => {
    const filter = STATUS_FILTERS.find(f => f.key === statusFilter)!;
    return patients.filter((p) => {
      const q = search.toLowerCase();
      const matchSearch = q === "" || p.patientName.toLowerCase().includes(q) || (p.symptoms ?? "").toLowerCase().includes(q) || (p.phone ?? "").includes(q);
      const status = STATUS_MAP[p.status] ?? "waiting";
      const matchStatus = filter.statuses.includes(status);
      const shift = new Date(p.appointmentDate).getHours() < 12 ? "morning" : "afternoon";
      const matchShift = shiftFilter === "all" || shift === shiftFilter;
      return matchSearch && matchStatus && matchShift;
    });
  }, [patients, search, shiftFilter, statusFilter]);

  const waiting = patients.filter(p => (STATUS_MAP[p.status] ?? "waiting") === "waiting").length;
  const active = patients.filter(p => (STATUS_MAP[p.status] ?? "waiting") === "in_progress").length;
  const done = patients.filter(p => (STATUS_MAP[p.status] ?? "waiting") === "done").length;

  const firstWaitingId = useMemo(() => {
    return filtered.find(p => (STATUS_MAP[p.status] ?? "waiting") === "waiting")?.appointmentId;
  }, [filtered]);

  const selectCls = "px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 appearance-none cursor-pointer pr-8";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Bệnh Nhân"
          subtitle={dateLabel}
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">{waiting} chờ</span>
              <span className="px-2.5 py-1.5 bg-sky-50 text-sky-700 border border-sky-200 rounded-xl">{active} đang khám</span>
              <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">{done} hoàn thành</span>
              <div className="flex items-center gap-1.5 pl-1">
                <input
                  type="date"
                  value={selectedDate}
                  onChange={e => setSelectedDate(e.target.value || todayIso())}
                  className="px-3 py-1.5 text-[12.5px] font-bold bg-white border border-slate-200 rounded-xl text-slate-700 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none cursor-pointer"
                />
                {selectedDate !== todayIso() && (
                  <button onClick={() => setSelectedDate(todayIso())}
                    className="px-2.5 py-1.5 rounded-xl border border-primary/30 bg-primary/5 text-primary hover:bg-primary/10 transition-all cursor-pointer">
                    Hôm nay
                  </button>
                )}
              </div>
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

          {/* Status Filter Buttons */}
          <div className="flex gap-2">
            {STATUS_FILTERS.map((filter) => {
              const count = patients.filter(p => {
                const status = STATUS_MAP[p.status] ?? "waiting";
                return filter.statuses.includes(status);
              }).length;
              const isActive = statusFilter === filter.key;
              return (
                <button
                  key={filter.key}
                  onClick={() => setStatusFilter(filter.key)}
                  className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-[13px] font-bold transition-all cursor-pointer ${
                    isActive
                      ? `${filter.color} border shadow-sm`
                      : "bg-white text-slate-500 border border-slate-200 hover:border-slate-300"
                  }`}
                >
                  {filter.label}
                  <span className={`px-2 py-0.5 rounded-lg text-[11px] ${
                    isActive ? "bg-white/50" : "bg-slate-100"
                  }`}>
                    {count}
                  </span>
                </button>
              );
            })}
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

          {/* Patient queue list */}
          {!loading && !error && filtered.length > 0 ? (
            <div className="flex flex-col gap-3">
              <div className="flex items-center justify-between px-1">
                <span className="text-[13px] font-black text-slate-600 uppercase tracking-wider flex items-center gap-2">
                  <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
                  </svg>
                  Hàng Đợi Khám ({filtered.length} bệnh nhân)
                </span>
                <span className="text-[12px] font-bold text-slate-400">
                  Thứ tự theo hàng đợi thực tế
                </span>
              </div>
              <div className="flex flex-col gap-2.5">
                {filtered.map((p, idx) => (
                  <PatientRow
                    key={p.appointmentId}
                    p={p}
                    idx={idx}
                    isTopWaiting={p.appointmentId === firstWaitingId}
                  />
                ))}
              </div>
            </div>
          ) : !loading && !error && (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
              <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
              </div>
              <p className="text-[14px] font-bold text-slate-500">Không tìm thấy bệnh nhân phù hợp.</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
