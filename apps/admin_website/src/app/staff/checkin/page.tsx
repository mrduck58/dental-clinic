"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getStaffAppointmentsApi,
  checkInAppointmentApi,
  type StaffAppointmentDto,
} from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";

// Helper functions - defined before useMemo to avoid ReferenceError
const fmtTime = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
};

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2,"0")}/${String(d.getMonth() + 1).padStart(2,"0")}/${d.getFullYear()}`;
};

const formatDateLabel = (dateStr: string) => {
  const now = new Date();
  const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  const tomorrow = new Date(now.getTime() + 86400000);
  const tomorrowStr = `${tomorrow.getFullYear()}-${String(tomorrow.getMonth() + 1).padStart(2, "0")}-${String(tomorrow.getDate()).padStart(2, "0")}`;
  if (dateStr === today) return "Hôm nay";
  if (dateStr === tomorrowStr) return "Ngày mai";
  const [y, m, d] = dateStr.split("-");
  return `Ngày ${d}/${m}/${y}`;
};

const getDateBadgeColor = (dateStr: string) => {
  const now = new Date();
  const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  if (dateStr === today) return "bg-emerald-50 text-emerald-700 border-emerald-200";
  return "bg-slate-50 text-slate-600 border-slate-200";
};

export default function CheckinPage() {
  useRequireStaff();

  const [search,    setSearch]    = useState("");
  const [selected,  setSelected]  = useState<string | null>(null);
  const [confirmed, setConfirmed] = useState<StaffAppointmentDto[]>([]);
  const [checkedIn, setCheckedIn] = useState<Set<string>>(new Set());
  const [confirmAt, setConfirmAt] = useState<Record<string, string>>({});
  const [loadingId, setLoadingId] = useState<string | null>(null);

  // Date filter - default to today (local timezone)
  const [dateFilter, setDateFilter] = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  });

  // Load confirmed appointments
  const loadAppointments = useCallback(async () => {
    const data = await getStaffAppointmentsApi({ date: dateFilter, status: "Confirmed" });
    setConfirmed(data);
  }, [dateFilter]);

  useEffect(() => {
    void loadAppointments();
    const channel = supabase
      .channel("staff-checkin-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void loadAppointments();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [loadAppointments]);

  const waiting = confirmed.filter(p => !checkedIn.has(p.appointmentId));

  // Group appointments by date and sort by date (earliest first)
  const groupedByDate = useMemo(() => {
    const groups: Record<string, StaffAppointmentDto[]> = {};
    for (const p of waiting) {
      const date = p.appointmentDate.split("T")[0];
      if (!groups[date]) groups[date] = [];
      groups[date].push(p);
    }
    return Object.entries(groups)
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([date, patients]) => ({
        date,
        label: formatDateLabel(date),
        patients: patients.sort((a, b) =>
          new Date(a.appointmentDate).getTime() - new Date(b.appointmentDate).getTime()
        ),
      }));
  }, [waiting]);

  // Filter by search
  const filteredGroups = useMemo(() => {
    if (!search.trim()) return groupedByDate;
    return groupedByDate
      .map(g => ({
        ...g,
        patients: g.patients.filter(p =>
          p.patientName.toLowerCase().includes(search.toLowerCase()) ||
          (p.patientPhone ?? "").includes(search)
        ),
      }))
      .filter(g => g.patients.length > 0);
  }, [groupedByDate, search]);

  const patient = confirmed.find(p => p.appointmentId === selected);

  const doCheckin = async (appt: StaffAppointmentDto) => {
    setLoadingId(appt.appointmentId);
    try {
      await checkInAppointmentApi(appt.appointmentId);
      const now = new Date();
      const t = `${String(now.getHours()).padStart(2,"0")}:${String(now.getMinutes()).padStart(2,"0")}`;
      setCheckedIn(prev => new Set([...prev, appt.appointmentId]));
      setConfirmAt(prev => ({ ...prev, [appt.appointmentId]: t }));
      setSelected(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Check-in thất bại";
      alert(`${message}. Vui lòng thử lại.`);
    } finally {
      setLoadingId(null);
    }
  };

  const totalWaiting = groupedByDate.reduce((sum, g) => sum + g.patients.length, 0);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="checkin" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader
          title="Check-in Bệnh Nhân"
          subtitle="Xác nhận bệnh nhân đến khám"
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">{totalWaiting} chờ</span>
              <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">{checkedIn.size} đã check-in</span>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto">
          <div className="flex gap-6">

            {/* Left: date filter + search + list */}
            <div className="w-96 flex flex-col gap-4 shrink-0">
              {/* Date filter */}
              <div className="bg-white px-4 py-3 rounded-2xl border border-slate-200/70 shadow-sm">
                <label className="block text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-2">Lọc theo ngày</label>
                <input
                  type="date"
                  value={dateFilter}
                  onChange={e => setDateFilter(e.target.value)}
                  className="w-full px-3 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700"
                />
              </div>

              {/* Search */}
              <div className="bg-white px-4 py-3 rounded-2xl border border-slate-200/70 shadow-sm">
                <div className="relative">
                  <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                  </span>
                  <input value={search} onChange={e => setSearch(e.target.value)}
                    placeholder="Tìm tên hoặc số điện thoại..."
                    className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400" />
                </div>
              </div>

              {/* Grouped waiting list */}
              {filteredGroups.length > 0 ? (
                <div className="flex flex-col gap-4">
                  <div className="flex items-center gap-3">
                    <span className="text-[13px] font-black text-slate-600 uppercase tracking-wider">Đang chờ check-in</span>
                    <span className="text-[12px] font-bold text-slate-400">{totalWaiting} bệnh nhân</span>
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>

                  {/* Date groups */}
                  {filteredGroups.map(group => (
                    <div key={group.date} className="flex flex-col gap-2.5">
                      {/* Date header */}
                      <div className="flex items-center gap-2 px-1">
                        <span className={`px-2.5 py-1 text-[11.5px] font-black rounded-lg border ${getDateBadgeColor(group.date)}`}>
                          {group.label}
                        </span>
                        <span className="text-[11px] text-slate-400 font-semibold">
                          {group.patients.length} bệnh nhân
                        </span>
                        <div className="flex-1 h-px bg-slate-100" />
                      </div>

                      {/* Patient cards */}
                      {group.patients.map((p, idx) => {
                        const initials = p.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
                        const isActive = selected === p.appointmentId;
                        const apptTime = fmtTime(p.appointmentDate);
                        return (
                          <button key={p.appointmentId} onClick={() => setSelected(p.appointmentId)}
                            className={`flex rounded-2xl border overflow-hidden w-full text-left transition-all hover:shadow-md ${
                              isActive ? "bg-white border-primary shadow-md shadow-primary/10" : "bg-white border-slate-200/70 hover:-translate-y-px"
                            }`}>
                            <div className={`w-1.5 shrink-0 ${isActive ? "bg-primary" : "bg-amber-400"}`} />
                            <div className="flex items-center gap-4 px-4 py-3.5 flex-1 min-w-0">
                              <div className="flex flex-col items-center w-12 shrink-0">
                                <span className="text-[17px] font-black text-slate-900 font-mono leading-none">{apptTime}</span>
                                <span className="text-[11px] font-bold text-slate-400 mt-1">#{idx + 1}</span>
                              </div>
                              <div className="w-px h-10 bg-slate-100 shrink-0" />
                              <div className="w-10 h-10 rounded-xl flex items-center justify-center font-black text-[12px] shrink-0 bg-sky-50 text-sky-700 border border-sky-100">
                                {initials}
                              </div>
                              <div className="flex-1 min-w-0">
                                <div className="text-[14px] font-black text-slate-900 truncate">{p.patientName}</div>
                                <div className="text-[12px] text-slate-500 font-semibold truncate">{p.serviceName ?? "Khám tổng quát"}</div>
                                <div className="text-[11.5px] text-slate-400 font-medium font-mono">{p.patientPhone ?? "—"}</div>
                              </div>
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  ))}
                </div>
              ) : (
                <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-14">
                  <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center">
                    <svg className="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                  </div>
                  <p className="text-[13px] font-bold text-slate-500">{search ? "Không tìm thấy kết quả" : "Tất cả đã check-in"}</p>
                </div>
              )}

              {/* Checked-in section */}
              {checkedIn.size > 0 && (
                <div className="flex flex-col gap-3 mt-2">
                  <div className="flex items-center gap-3">
                    <span className="text-[13px] font-black text-emerald-600 uppercase tracking-wider">Đã check-in</span>
                    <span className="text-[12px] font-bold text-slate-400">{checkedIn.size}</span>
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>
                  <div className="flex flex-col gap-2">
                    {[...checkedIn].map(id => {
                      const p = confirmed.find(x => x.appointmentId === id);
                      if (!p) return null;
                      return (
                        <div key={id} className="flex rounded-2xl border border-emerald-100 bg-emerald-50/60 overflow-hidden">
                          <div className="w-1.5 shrink-0 bg-emerald-400" />
                          <div className="flex items-center gap-3 px-4 py-3 flex-1 min-w-0">
                            <div className="w-7 h-7 rounded-full bg-emerald-100 flex items-center justify-center shrink-0">
                              <svg className="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                            </div>
                            <div className="flex-1 min-w-0">
                              <div className="text-[13px] font-bold text-emerald-900 truncate">{p.patientName}</div>
                              <div className="text-[11.5px] text-emerald-600 font-semibold">Check-in lúc {confirmAt[id]}</div>
                            </div>
                            <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md border ${getDateBadgeColor(p.appointmentDate.split("T")[0])}`}>
                              {formatDateLabel(p.appointmentDate.split("T")[0])}
                            </span>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>

            {/* Right: detail panel */}
            <div className="flex-1">
              {patient ? (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-7 flex flex-col gap-5">
                  <div className="flex items-start gap-4">
                    <div className="w-16 h-16 rounded-2xl border-2 bg-sky-50 border-sky-100 text-sky-700 flex items-center justify-center font-black text-2xl shrink-0">
                      {patient.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                    </div>
                    <div>
                      <h2 className="text-[20px] font-black text-slate-900">{patient.patientName}</h2>
                      <div className="flex items-center gap-3 mt-1 text-[13px] text-slate-500 font-semibold flex-wrap">
                        <span>{patient.patientPhone ?? "—"}</span>
                        <span className={`px-2.5 py-1 text-[11px] font-bold rounded-lg border ${getDateBadgeColor(patient.appointmentDate.split("T")[0])}`}>
                          {formatDateLabel(patient.appointmentDate.split("T")[0])}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    {[
                      { label: "Ngày hẹn",  value: fmtDate(patient.appointmentDate), icon: "M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" },
                      { label: "Giờ hẹn",  value: fmtTime(patient.appointmentDate), icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"          },
                      { label: "Dịch vụ",  value: patient.serviceName ?? "Khám tổng quát", icon: "M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" },
                      { label: "Bác sĩ",   value: patient.dentistName, icon: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" },
                      { label: "Mã lịch hẹn", value: patient.appointmentCode, icon: "M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75m-.75 3h.75" },
                    ].map(item => (
                      <div key={item.label} className="flex items-center gap-3 p-4 bg-slate-50 rounded-xl border border-slate-100">
                        <div className="w-8 h-8 rounded-xl bg-white border border-slate-200 flex items-center justify-center shrink-0">
                          <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d={item.icon} />
                          </svg>
                        </div>
                        <div>
                          <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{item.label}</div>
                          <div className="text-[13.5px] font-bold text-slate-800 mt-0.5">{item.value}</div>
                        </div>
                      </div>
                    ))}
                  </div>

                  {patient.symptoms && (
                    <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-100 rounded-xl">
                      <svg className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                      <div>
                        <div className="text-[11.5px] font-extrabold text-amber-700 uppercase tracking-wider">Triệu chứng</div>
                        <div className="text-[13.5px] font-semibold text-amber-800 mt-0.5">{patient.symptoms}</div>
                      </div>
                    </div>
                  )}

                  <button onClick={() => doCheckin(patient)}
                    disabled={loadingId === patient.appointmentId}
                    className="flex items-center justify-center gap-2 w-full py-4 bg-emerald-500 hover:bg-emerald-600 disabled:opacity-50 text-white rounded-xl text-[15px] font-black shadow-sm shadow-emerald-200 transition-all cursor-pointer">
                    {loadingId === patient.appointmentId ? (
                      <span className="w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                    ) : (
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
                    )}
                    Xác nhận Check-in
                  </button>
                </div>
              ) : (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm h-full min-h-[400px] flex flex-col items-center justify-center gap-3">
                  <div className="w-16 h-16 rounded-full bg-slate-100 flex items-center justify-center">
                    <svg className="w-8 h-8 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" /></svg>
                  </div>
                  <p className="text-[14px] font-bold text-slate-400">Chọn bệnh nhân bên trái để xem chi tiết</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
