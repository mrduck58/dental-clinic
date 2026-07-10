"use client";

import { useState, useEffect, useCallback } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getWaitingQueueApi,
  transferQueuePatientApi,
  type WaitingQueueResponse,
  type RoomQueueDto,
} from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";

const DENTIST_COLOR: Record<string, { bg: string; border: string; text: string }> = {
  sky:     { bg: "bg-sky-50",     border: "border-sky-100",     text: "text-sky-700" },
  violet:  { bg: "bg-violet-50",  border: "border-violet-100",  text: "text-violet-700" },
  rose:    { bg: "bg-rose-50",    border: "border-rose-100",    text: "text-rose-700" },
  amber:   { bg: "bg-amber-50",   border: "border-amber-100",   text: "text-amber-700" },
  slate:   { bg: "bg-slate-50",   border: "border-slate-100",   text: "text-slate-700" },
};

const initialsOf = (name: string) =>
  name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();

const fmtTime = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getHours()).padStart(2,"0")}:${String(d.getMinutes()).padStart(2,"0")}`;
};

type DragState = { appointmentId: string; fromRoom: string | null } | null;

function RoomQueueCard({ room, canDrag, drag, onDragStart, onDragEnd, onDrop, transferringId }: {
  room: RoomQueueDto;
  canDrag: boolean;
  drag: DragState;
  onDragStart: (appointmentId: string, fromRoom: string | null) => void;
  onDragEnd: () => void;
  onDrop: (appointmentId: string, roomName: string) => void;
  transferringId: string | null;
}) {
  const [isOver, setIsOver] = useState(false);

  const waitingCount = room.patients.filter(p => p.status === "CheckedIn").length;
  const inProgressCount = room.patients.length - waitingCount;
  // Chỉ ghi tên bác sĩ trên từng bệnh nhân khi phòng có nhiều hơn một bác sĩ trong ngày.
  const showDentistPerPatient = room.dentists.length > 1;

  // Chỉ nhận bệnh nhân khi phòng có bác sĩ đang trong ca — API chuyển lịch hẹn cho
  // chính bác sĩ đó, không có ai trực thì không xác định được người phụ trách.
  const hasDentistOnShift = room.dentists.some(d => d.isOnShiftNow);
  const canAcceptDrop = drag !== null && room.roomName !== null &&
    room.roomName !== drag.fromRoom && hasDentistOnShift;

  const handleDragOver = (e: React.DragEvent) => {
    if (!canAcceptDrop) return;
    e.preventDefault();               // báo cho trình duyệt biết đây là vùng thả hợp lệ
    e.dataTransfer.dropEffect = "move";
    setIsOver(true);
  };

  const handleDrop = (e: React.DragEvent) => {
    if (!canAcceptDrop || room.roomName === null) return;
    e.preventDefault();
    setIsOver(false);
    onDrop(e.dataTransfer.getData("text/plain"), room.roomName);
  };

  return (
    <div
      onDragOver={handleDragOver}
      onDragLeave={() => setIsOver(false)}
      onDrop={handleDrop}
      className={`bg-white rounded-2xl border shadow-sm overflow-hidden transition-all ${
        isOver && canAcceptDrop ? "border-primary ring-2 ring-primary/40"
        : canAcceptDrop ? "border-primary/40 border-dashed"
        : drag !== null ? "border-slate-200/70 opacity-60"
        : "border-slate-200/70"
      }`}
    >
      {/* Header: phòng + bác sĩ trực kèm giờ ca làm */}
      <div className="px-5 py-4 bg-slate-50 border-b border-slate-100">
        <div className="flex items-start justify-between gap-3">
          <div className="text-[15px] font-black text-slate-900">{room.roomName ?? "Chưa xếp phòng"}</div>
          <div className="flex items-center gap-1.5 shrink-0">
            {inProgressCount > 0 && (
              <span className="px-2.5 py-1 bg-emerald-100 text-emerald-700 rounded-lg text-[11.5px] font-black">
                {inProgressCount} đang khám
              </span>
            )}
            <span className="px-2.5 py-1 bg-amber-100 text-amber-700 rounded-lg text-[11.5px] font-black">
              {waitingCount} chờ
            </span>
          </div>
        </div>

        <div className="mt-3 flex flex-col gap-1.5">
          {room.dentists.length === 0 ? (
            <div className="text-[12px] text-slate-400 font-semibold">Không có bác sĩ trực</div>
          ) : (
            room.dentists.map(dentist => {
              const color = DENTIST_COLOR[dentist.dentistColor] ?? DENTIST_COLOR.slate;
              return (
                <div key={dentist.dentistId} className="flex items-center gap-2.5">
                  <div className={`w-8 h-8 rounded-lg ${color.bg} border ${color.border} flex items-center justify-center font-black text-[11px] ${color.text} shrink-0`}>
                    {initialsOf(dentist.dentistName)}
                  </div>
                  <div className="min-w-0">
                    <div className={`text-[13px] font-black ${color.text} truncate`}>{dentist.dentistName}</div>
                    <div className="flex flex-wrap gap-1 mt-0.5">
                      {dentist.shifts.length === 0 ? (
                        <span className="text-[11px] font-semibold text-slate-400">Chưa phân ca</span>
                      ) : (
                        dentist.shifts.map(shift => (
                          <span key={shift} className="px-1.5 py-0.5 bg-white border border-slate-200 rounded-md text-[11px] font-bold text-slate-600">
                            {shift}
                          </span>
                        ))
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>

      {/* Hàng đợi: người đang khám đứng đầu, rồi tới người chờ theo thứ tự check-in */}
      <div className="divide-y divide-slate-100">
        {room.patients.length === 0 ? (
          <div className="py-10 text-center text-[13px] text-slate-400 font-semibold">
            Chưa có bệnh nhân chờ
          </div>
        ) : (
          room.patients.map(patient => {
            const isInProgress = patient.status === "InProgress";
            // Chỉ người đang chờ mới kéo được — người đang khám đã ngồi trên ghế của phòng này.
            const isDraggable = canDrag && !isInProgress;
            const isTransferring = transferringId === patient.appointmentId;
            const isDragged = drag?.appointmentId === patient.appointmentId;

            return (
              <div
                key={patient.appointmentId}
                draggable={isDraggable}
                onDragStart={e => {
                  e.dataTransfer.setData("text/plain", patient.appointmentId);
                  e.dataTransfer.effectAllowed = "move";
                  onDragStart(patient.appointmentId, room.roomName);
                }}
                onDragEnd={onDragEnd}
                className={`px-5 py-4 transition-opacity ${isInProgress ? "bg-emerald-50/50" : ""} ${
                  isDraggable ? "cursor-grab active:cursor-grabbing" : ""
                } ${isDragged ? "opacity-40" : ""} ${isTransferring ? "opacity-50 pointer-events-none" : ""}`}
              >
                <div className="flex items-center gap-4">
                  {/* Số thứ tự — theo thứ tự check-in */}
                  <div className={`w-8 h-8 rounded-lg flex items-center justify-center font-black text-[13px] shrink-0 ${
                    isInProgress ? "bg-emerald-500 text-white" : "bg-amber-400 text-white"
                  }`}>
                    {isInProgress ? (
                      <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                        <path d="M6.3 2.8A1 1 0 004.8 3.7v12.6a1 1 0 001.5.9l10.2-6.3a1 1 0 000-1.8L6.3 2.8z" />
                      </svg>
                    ) : patient.queueNumber}
                  </div>

                  {/* Avatar */}
                  <div className="w-10 h-10 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[11px] text-sky-700 shrink-0">
                    {initialsOf(patient.patientName)}
                  </div>

                  {/* Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-[14px] font-black text-slate-900">{patient.patientName}</span>
                      <span className="text-[12px] font-bold text-slate-400">
                        {patient.checkedInAt
                          ? `Check-in ${fmtTime(patient.checkedInAt)}`
                          : `Hẹn ${fmtTime(patient.appointmentDate)}`}
                      </span>
                    </div>
                    <div className="flex items-center gap-2 mt-0.5 flex-wrap">
                      <span className="text-[12px] text-slate-500 font-semibold">{patient.serviceName ?? "Khám tổng quát"}</span>
                      {showDentistPerPatient && (
                        <span className="text-[11.5px] text-slate-400 font-semibold">· {patient.dentistName}</span>
                      )}
                      {patient.waitMinutes > 0 && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-amber-50 text-amber-600 border border-amber-100 rounded-lg text-[11px] font-black">
                          đã chờ {patient.waitMinutes}p
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
                    <span className={`px-2.5 py-1 rounded-lg text-[11.5px] font-black border ${
                      isInProgress
                        ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                        : "bg-amber-50 text-amber-700 border-amber-200"
                    }`}>
                      {isInProgress ? "Đang khám" : "Đang chờ"}
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
  const [selectedDate, setSelectedDate] = useState(queueTodayIso());
  const [drag, setDrag] = useState<DragState>(null);
  const [transferringId, setTransferringId] = useState<string | null>(null);

  // Chuyển phòng = giao bệnh nhân cho bác sĩ đang trong ca trực ở đó, nên chỉ làm được
  // với hàng đợi của hôm nay; ngày quá khứ không có khái niệm "đang trong ca".
  const isToday = selectedDate === queueTodayIso();

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

  const handleDropPatient = async (appointmentId: string, roomName: string) => {
    setDrag(null);
    setTransferringId(appointmentId);
    try {
      await transferQueuePatientApi(appointmentId, roomName);
      await loadQueue();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Không thể chuyển bệnh nhân sang phòng khác");
    } finally {
      setTransferringId(null);
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
          subtitle={
            isToday
              ? `Danh sách bệnh nhân theo phòng khám · ${today} · Kéo bệnh nhân đang chờ sang phòng khác để đổi bác sĩ`
              : `Danh sách bệnh nhân theo phòng khám · ${today}`
          }
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              {queueData && (
                <>
                  <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">
                    {queueData.totalInProgress} đang khám
                  </span>
                  <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">
                    {queueData.totalWaiting} chờ
                  </span>
                </>
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
          ) : queueData && queueData.rooms.length > 0 ? (
            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-5">
              {queueData.rooms.map(room => (
                <RoomQueueCard
                  key={room.roomName ?? "no-room"}
                  room={room}
                  canDrag={isToday}
                  drag={drag}
                  onDragStart={(appointmentId, fromRoom) => setDrag({ appointmentId, fromRoom })}
                  onDragEnd={() => setDrag(null)}
                  onDrop={handleDropPatient}
                  transferringId={transferringId}
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
              <p className="text-[14px] font-bold text-slate-500">Không có bác sĩ nào làm việc trong ngày này</p>
              <p className="text-[12.5px] text-slate-400 font-semibold">Hàng đợi hiển thị theo phòng của bác sĩ có ca làm việc</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
