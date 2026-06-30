"use client";

import { useState, useEffect, useCallback } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getWaitingQueueApi,
  completeTreatmentApi,
  type WaitingQueueResponse,
  type DentistQueueDto,
  type QueuePatientDto,
} from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";

const DENTIST_COLOR: Record<string, { bg: string; border: string; text: string }> = {
  sky:     { bg: "bg-sky-50",     border: "border-sky-100",     text: "text-sky-700" },
  violet:  { bg: "bg-violet-50",  border: "border-violet-100",  text: "text-violet-700" },
  rose:    { bg: "bg-rose-50",    border: "border-rose-100",    text: "text-rose-700" },
  amber:   { bg: "bg-amber-50",   border: "border-amber-100",   text: "text-amber-700" },
  slate:   { bg: "bg-slate-50",   border: "border-slate-100",   text: "text-slate-700" },
};

const DENTIST_ROOM: Record<string, string> = {
  "BS. Thảo": "Phòng 1",
  "BS. Minh": "Phòng 2",
  "BS. Linh": "Phòng 3",
  "BS. Hùng": "Phòng 4",
  "BS. Lê Minh Tuấn": "Phòng 1",
  "BS. Nguyễn Văn Hùng": "Phòng 2",
};

function DentistQueueCard({ dentist, onComplete, loadingId }: {
  dentist: DentistQueueDto;
  onComplete: (id: string) => void;
  loadingId: string | null;
}) {
  const color = DENTIST_COLOR[dentist.dentistColor] ?? DENTIST_COLOR.slate;
  const waitingPatients = dentist.patients.filter(p => p.status === "CheckedIn");

  const fmtTime = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
      {/* Header */}
      <div className={`px-5 py-4 ${color.bg} border-b ${color.border}`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className={`w-10 h-10 rounded-xl ${color.bg} border ${color.border} flex items-center justify-center font-black text-[13px] ${color.text}`}>
              {dentist.dentistName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
            </div>
            <div>
              {(dentist.roomName ?? DENTIST_ROOM[dentist.dentistName]) && (
                <div className="text-[11px] font-bold text-slate-500 mb-0.5">{dentist.roomName ?? DENTIST_ROOM[dentist.dentistName]}</div>
              )}
              <div className={`text-[14px] font-black ${color.text}`}>{dentist.dentistName}</div>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <span className="px-2.5 py-1 bg-amber-100 text-amber-700 rounded-lg text-[11.5px] font-black">
              {waitingPatients.length} chờ
            </span>
          </div>
        </div>
      </div>

      {/* Patient list */}
      <div className="divide-y divide-slate-100">
        {waitingPatients.length === 0 ? (
          <div className="py-10 text-center text-[13px] text-slate-400 font-semibold">
            Chưa có bệnh nhân chờ
          </div>
        ) : (
          waitingPatients.map(patient => {
            const initials = patient.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
            const isLoading = loadingId === patient.appointmentId;

            return (
              <div key={patient.appointmentId} className="px-5 py-4">
                <div className="flex items-center gap-4">
                  {/* Status bar */}
                  <div className="w-1.5 h-12 rounded-full bg-amber-400 shrink-0" />

                  {/* Avatar */}
                  <div className="w-10 h-10 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[11px] text-sky-700 shrink-0">
                    {initials}
                  </div>

                  {/* Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-[14px] font-black text-slate-900">{patient.patientName}</span>
                      <span className="text-[12px] font-mono font-bold text-slate-400">{fmtTime(patient.appointmentDate)}</span>
                    </div>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className="text-[12px] text-slate-500 font-semibold">{patient.serviceName ?? "Khám tổng quát"}</span>
                      {patient.waitMinutes > 0 && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-amber-50 text-amber-600 border border-amber-100 rounded-lg text-[11px] font-black">
                          ~{patient.waitMinutes}p
                        </span>
                      )}
                    </div>
                    {patient.symptoms && (
                      <div className="mt-1.5 text-[11.5px] text-amber-600 font-semibold bg-amber-50 px-2 py-1 rounded-lg inline-block">
                        {patient.symptoms}
                      </div>
                    )}
                  </div>

                  {/* Status badge */}
                  <div className="flex items-center gap-2 shrink-0">
                    <span className="px-2.5 py-1 rounded-lg text-[11.5px] font-black border bg-amber-50 text-amber-700 border-amber-200">
                      Đang chờ
                    </span>
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}

const queueTodayIso = () => {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
};

export default function QueuePage() {
  useRequireStaff();
  const [queueData, setQueueData] = useState<WaitingQueueResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [loadingId, setLoadingId] = useState<string | null>(null);
  const [selectedDate, setSelectedDate] = useState(queueTodayIso());

  const loadQueue = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getWaitingQueueApi(selectedDate);
      setQueueData(data);
      setError(null);
    } catch {
      setError("Không thể tải hàng đợi");
    } finally {
      setLoading(false);
    }
  }, [selectedDate]);

  useEffect(() => {
    void loadQueue();
    const channel = supabase
      .channel("staff-queue-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void loadQueue();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [loadQueue]);

  const handleComplete = async (id: string) => {
    setLoadingId(id);
    try {
      await completeTreatmentApi(id);
      await loadQueue();
    } catch {
      alert("Hoàn thành khám thất bại. Vui lòng thử lại.");
    } finally {
      setLoadingId(null);
    }
  };

  const today = queueData?.date
    ? new Date(queueData.date).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" })
    : new Date().toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="queue" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader
          title="Hàng Đợi"
          subtitle={`Danh sách bệnh nhân theo phòng khám · ${today}`}
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              {queueData && (
                <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">
                  {queueData.totalWaiting} chờ
                </span>
              )}
              <div className="flex items-center gap-1.5 pl-1">
                <input
                  type="date"
                  value={selectedDate}
                  onChange={e => setSelectedDate(e.target.value || queueTodayIso())}
                  className="px-3 py-1.5 text-[12.5px] font-bold bg-white border border-slate-200 rounded-xl text-slate-700 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none cursor-pointer"
                />
                {selectedDate !== queueTodayIso() && (
                  <button onClick={() => setSelectedDate(queueTodayIso())}
                    className="px-2.5 py-1.5 rounded-xl border border-primary/30 bg-primary/5 text-primary hover:bg-primary/10 transition-all cursor-pointer">
                    Hôm nay
                  </button>
                )}
              </div>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto">
          {loading ? (
            <div className="flex items-center justify-center py-20">
              <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : error ? (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
              <p className="text-[14px] font-semibold text-red-500">{error}</p>
              <button onClick={loadQueue} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">
                Thử lại
              </button>
            </div>
          ) : queueData && queueData.dentists.length > 0 ? (
            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-5">
              {queueData.dentists.map(dentist => (
                <DentistQueueCard
                  key={dentist.dentistId}
                  dentist={dentist}
                  onComplete={handleComplete}
                  loadingId={loadingId}
                />
              ))}
            </div>
          ) : (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
              <div className="w-16 h-16 rounded-full bg-slate-100 flex items-center justify-center">
                <svg className="w-8 h-8 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
                </svg>
              </div>
              <p className="text-[14px] font-bold text-slate-500">Chưa có bệnh nhân nào trong hàng đợi</p>
              <p className="text-[12.5px] text-slate-400 font-semibold">Bệnh nhân sẽ xuất hiện sau khi check-in</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
