"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getWaitingQueueApi,
  transferQueuePatientApi,
  reorderQueuePatientApi,
  type WaitingQueueResponse,
  type RoomQueueDto,
  type QueueDentistDto,
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

// Thời gian chờ hiển thị dạng giờ + phút: 80 → "1h20p", 60 → "1h", 45 → "45p".
const fmtWait = (mins: number) => {
  const h = Math.floor(mins / 60);
  const m = mins % 60;
  return h > 0 ? `${h}h${m > 0 ? `${String(m).padStart(2, "0")}p` : ""}` : `${m}p`;
};

type DragState = { appointmentId: string; fromRoom: string | null } | null;

// Hai hàm dưới cập nhật hàng đợi NGAY trên client để thao tác phản hồi tức thì, thay vì đợi
// hai lượt gọi mạng (ghi + tải lại) mới thấy kết quả. Backend vẫn là nơi chốt: sai thì trả về
// nguyên trạng, còn giá trị nào chỉ backend tính được thì tải lại ngầm để chỉnh sau.

/// Đổi chỗ hai bệnh nhân cạnh nhau. Backend chỉ hoán vị trí hiển thị (QueueOrder) và giữ nguyên
/// số thứ tự của mỗi người, nên kết quả cục bộ trùng khớp hoàn toàn — không cần tải lại.
const swapPatientsLocally = (
  data: WaitingQueueResponse, aId: string, bId: string,
): WaitingQueueResponse => ({
  ...data,
  rooms: data.rooms.map(room => {
    const ia = room.patients.findIndex(p => p.appointmentId === aId);
    const ib = room.patients.findIndex(p => p.appointmentId === bId);
    if (ia < 0 || ib < 0) return room;
    const patients = [...room.patients];
    [patients[ia], patients[ib]] = [patients[ib], patients[ia]];
    return { ...room, patients };
  }),
});

/// Chuyển bệnh nhân sang phòng khác: rời phòng cũ, xuống CUỐI phòng mới (người đang khám vẫn
/// đứng đầu vì backend xếp theo trạng thái trước). Số thứ tự mới do backend đánh nên giữ tạm
/// số cũ, lượt tải lại ngầm ngay sau đó sẽ chỉnh đúng.
const movePatientLocally = (
  data: WaitingQueueResponse, appointmentId: string, toRoom: string, dentistName?: string,
): WaitingQueueResponse => {
  const found = data.rooms.flatMap(r => r.patients).find(p => p.appointmentId === appointmentId);
  if (!found) return data;
  const moved = { ...found, dentistName: dentistName ?? found.dentistName };
  return {
    ...data,
    rooms: data.rooms.map(room => {
      const without = room.patients.filter(p => p.appointmentId !== appointmentId);
      return room.roomName === toRoom
        ? { ...room, patients: [...without, moved] }
        : { ...room, patients: without };
    }),
  };
};

function RoomQueueCard({ room, canDrag, drag, onDragStart, onDragEnd, onDrop, onReorder, transferringId, reorderingId }: {
  room: RoomQueueDto;
  canDrag: boolean;
  drag: DragState;
  onDragStart: (appointmentId: string, fromRoom: string | null) => void;
  onDragEnd: () => void;
  onDrop: (appointmentId: string, roomName: string) => void;
  onReorder: (appointmentId: string, swapWithAppointmentId: string) => void;
  transferringId: string | null;
  reorderingId: string | null;
}) {
  // Danh sách người ĐANG CHỜ theo đúng thứ tự hiển thị — dùng để tìm người liền kề khi đẩy lên/xuống.
  const waiting = room.patients.filter(p => p.status === "CheckedIn");
  const [isOver, setIsOver] = useState(false);

  const waitingCount = room.patients.filter(p => p.status === "CheckedIn").length;
  const inProgressCount = room.patients.length - waitingCount;
  // Chỉ ghi tên bác sĩ trên từng bệnh nhân khi phòng có nhiều hơn một bác sĩ trong ngày.
  const showDentistPerPatient = room.dentists.length > 1;

  // Nhận bệnh nhân khi phòng có bác sĩ đang trực HOẶC sắp vào ca (giao trước gần giờ giao ca).
  // Không có ai thì không xác định được người phụ trách nên không cho thả.
  const hasAssignableDentist = room.dentists.some(d => d.isOnShiftNow || d.isOnShiftSoon);
  const canAcceptDrop = drag !== null && room.roomName !== null &&
    room.roomName !== drag.fromRoom && hasAssignableDentist;

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
                    <div className="flex items-center gap-1.5">
                      <span className={`text-[13px] font-black ${color.text} truncate`}>{dentist.dentistName}</span>
                      {dentist.isOnShiftNow ? (
                        <span className="px-1.5 py-0.5 bg-emerald-100 text-emerald-700 rounded-md text-[10px] font-black shrink-0">Đang trực</span>
                      ) : dentist.isOnShiftSoon ? (
                        <span className="px-1.5 py-0.5 bg-amber-100 text-amber-700 rounded-md text-[10px] font-black shrink-0">Sắp vào ca</span>
                      ) : null}
                    </div>
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
            const isReordering = reorderingId === patient.appointmentId;

            // Người liền kề trong hàng chờ để đẩy lên/xuống một bậc (chỉ trong nhóm đang chờ).
            const wIdx = isInProgress ? -1 : waiting.findIndex(w => w.appointmentId === patient.appointmentId);
            const upId = wIdx > 0 ? waiting[wIdx - 1].appointmentId : null;
            const downId = wIdx >= 0 && wIdx < waiting.length - 1 ? waiting[wIdx + 1].appointmentId : null;
            const canReorder = canDrag && !isInProgress;

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
                } ${isDragged ? "opacity-40" : ""} ${isTransferring || isReordering ? "opacity-50 pointer-events-none" : ""}`}
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div className="flex items-center gap-3 sm:gap-4 flex-1 min-w-0">
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
                            đã chờ {fmtWait(patient.waitMinutes)}
                          </span>
                        )}
                      </div>
                      {patient.symptoms && (
                        <div className="mt-1.5 text-[11.5px] text-amber-600 font-semibold bg-amber-50 px-2 py-1 rounded-lg inline-block">
                          {patient.symptoms}
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Actions & Status */}
                  <div className="flex items-center justify-between sm:justify-end gap-2 shrink-0 border-t sm:border-t-0 pt-2 sm:pt-0 border-slate-100 pl-11 sm:pl-0">
                    {/* Nút đẩy lên / xuống một bậc — chỉ cho người đang chờ, xem hàng đợi hôm nay */}
                    {canReorder && (
                      <div className="flex items-center gap-1 shrink-0">
                        <button
                          onClick={e => { e.stopPropagation(); if (upId) onReorder(patient.appointmentId, upId); }}
                          disabled={!upId || isReordering}
                          title="Đẩy lên trước một người"
                          className="w-6 h-6 flex items-center justify-center rounded-md border border-slate-200 text-slate-500 hover:text-primary hover:border-primary disabled:opacity-30 disabled:cursor-not-allowed transition-all cursor-pointer"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 15.75l7.5-7.5 7.5 7.5" /></svg>
                        </button>
                        <button
                          onClick={e => { e.stopPropagation(); if (downId) onReorder(patient.appointmentId, downId); }}
                          disabled={!downId || isReordering}
                          title="Đẩy xuống sau một người"
                          className="w-6 h-6 flex items-center justify-center rounded-md border border-slate-200 text-slate-500 hover:text-primary hover:border-primary disabled:opacity-30 disabled:cursor-not-allowed transition-all cursor-pointer"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                        </button>
                      </div>
                    )}

                    {/* Status badge */}
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
  const [reorderingId, setReorderingId] = useState<string | null>(null);
  // Khi phòng đích có ≥2 bác sĩ chọn được (đang trực + sắp vào ca), hỏi lễ tân giao cho ai.
  const [pendingPick, setPendingPick] = useState<{
    appointmentId: string;
    patientName: string;
    roomName: string;
    candidates: QueueDentistDto[];
  } | null>(null);

  // Chuyển phòng = giao bệnh nhân cho bác sĩ đang trong ca trực ở đó, nên chỉ làm được
  // với hàng đợi của hôm nay; ngày quá khứ không có khái niệm "đang trong ca".
  const isToday = selectedDate === queueTodayIso();

  const inFlight = useRef(false);

  // silent = cập nhật tại chỗ, không thay cả bảng bằng spinner. Chỉ lần tải đầu và khi đổi
  // ngày mới cần spinner; các lần làm mới sau một thao tác hoặc theo định kỳ thì không, nếu
  // không mỗi lần chuyển phòng bảng lại nháy trắng như đang tải lại từ đầu.
  const loadQueue = useCallback(async (silent = false) => {
    inFlight.current = true;
    if (!silent) setLoading(true);
    try {
      const data = await getWaitingQueueApi(selectedDate);
      setQueueData(data);
      setError(null);
    } catch {
      setError("Không thể tải hàng đợi");
    } finally {
      inFlight.current = false;
      if (!silent) setLoading(false);
    }
  }, [selectedDate]);

  // Làm mới do nền kích hoạt (realtime, poll định kỳ). Bỏ qua nếu đang có lượt tải chạy dở:
  // sau khi tự chuyển phòng, realtime bắn thêm một event cho đúng thay đổi đó — gọi API lần
  // hai là vô ích. Lượt tải mình chủ động gọi thì luôn chạy, không bị bỏ qua.
  const refreshInBackground = useCallback(() => {
    if (inFlight.current) return;
    void loadQueue(true);
  }, [loadQueue]);

  useEffect(() => {
    void loadQueue();
    const channel = supabase
      .channel("staff-queue-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        refreshInBackground();
      })
      .subscribe();
    // Trạng thái "đang trực"/"sắp vào ca" của từng phòng đổi theo giờ, không theo thay đổi
    // lịch hẹn — nếu không có ai đụng vào Appointments trong lúc trang mở lâu, dữ liệu ca trực
    // sẽ cũ dần và cho phép kéo-thả vào phòng đã hết bác sĩ trực (backend sẽ từ chối lúc thả).
    // Poll định kỳ để giữ nó luôn khớp với giờ thực tế.
    const shiftPoll = setInterval(refreshInBackground, 60_000);
    return () => { void supabase.removeChannel(channel); clearInterval(shiftPoll); };
  }, [loadQueue, refreshInBackground]);

  const doTransfer = async (appointmentId: string, roomName: string, dentistId?: string) => {
    setTransferringId(appointmentId);
    const snapshot = queueData;
    const dentistName = queueData?.rooms
      .find(r => r.roomName === roomName)?.dentists
      .find(d => d.dentistId === dentistId)?.dentistName;
    setQueueData(prev => prev && movePatientLocally(prev, appointmentId, roomName, dentistName));
    try {
      await transferQueuePatientApi(appointmentId, roomName, dentistId);
      // Số thứ tự ở phòng mới do backend đánh — tải lại ngầm để chỉnh, bảng không nháy.
      await loadQueue(true);
    } catch (e) {
      alert(e instanceof Error ? e.message : "Không thể chuyển bệnh nhân sang phòng khác");
      // Backend là nơi chốt bác sĩ nào nhận được bệnh nhân. Bị từ chối thì trả về nguyên trạng
      // rồi tải lại, để UI thôi cho phép thả vào phòng đó và lễ tân không thử lại vô ích.
      setQueueData(snapshot);
      await loadQueue(true);
    } finally {
      setTransferringId(null);
    }
  };

  const handleDropPatient = (appointmentId: string, roomName: string) => {
    setDrag(null);
    const room = queueData?.rooms.find(r => r.roomName === roomName);
    // Bác sĩ có thể nhận bệnh nhân lúc này = đang trực hoặc sắp vào ca.
    const candidates = room?.dentists.filter(d => d.isOnShiftNow || d.isOnShiftSoon) ?? [];

    // Nhiều người (gần giờ giao ca) → hỏi lễ tân giao cho ai; một người → giao thẳng.
    if (candidates.length >= 2) {
      const patient = room?.patients.find(p => p.appointmentId === appointmentId);
      setPendingPick({
        appointmentId,
        patientName: patient?.patientName ?? "bệnh nhân",
        roomName,
        candidates,
      });
      return;
    }
    void doTransfer(appointmentId, roomName, candidates[0]?.dentistId);
  };

  const handlePickDentist = (dentistId: string) => {
    if (!pendingPick) return;
    const { appointmentId, roomName } = pendingPick;
    setPendingPick(null);
    void doTransfer(appointmentId, roomName, dentistId);
  };

  const handleReorder = async (appointmentId: string, swapWithAppointmentId: string) => {
    setReorderingId(appointmentId);
    const snapshot = queueData;
    setQueueData(prev => prev && swapPatientsLocally(prev, appointmentId, swapWithAppointmentId));
    try {
      // Kết quả cục bộ khớp hoàn toàn với backend nên KHÔNG tải lại — tiết kiệm hẳn một lượt GET.
      await reorderQueuePatientApi(appointmentId, swapWithAppointmentId);
    } catch (e) {
      alert(e instanceof Error ? e.message : "Không thể đổi thứ tự hàng đợi");
      setQueueData(snapshot);
    } finally {
      setReorderingId(null);
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

        <div className="p-4 sm:p-8 flex-1 overflow-y-auto">
          {loading ? (
            <div className="flex items-center justify-center py-20">
              <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : error ? (
            <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-16">
              <p className="text-[14px] font-semibold text-red-500">{error}</p>
              <button onClick={() => void loadQueue()} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">
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
                  onReorder={handleReorder}
                  transferringId={transferringId}
                  reorderingId={reorderingId}
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

      {pendingPick && (
        <DentistPickerModal
          patientName={pendingPick.patientName}
          roomName={pendingPick.roomName}
          candidates={pendingPick.candidates}
          onPick={handlePickDentist}
          onClose={() => setPendingPick(null)}
        />
      )}
    </div>
  );
}

/** Hỏi lễ tân giao bệnh nhân cho ai khi phòng đích có cả bác sĩ đang trực lẫn bác sĩ sắp vào ca. */
function DentistPickerModal({ patientName, roomName, candidates, onPick, onClose }: {
  patientName: string;
  roomName: string;
  candidates: QueueDentistDto[];
  onPick: (dentistId: string) => void;
  onClose: () => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4" onClick={onClose}>
      <div
        className="bg-white rounded-2xl shadow-xl border border-slate-200/70 w-full max-w-md overflow-hidden"
        onClick={e => e.stopPropagation()}
      >
        <div className="px-6 py-5 border-b border-slate-100">
          <div className="text-[15px] font-black text-slate-900">Chọn bác sĩ khám</div>
          <div className="mt-1 text-[12.5px] text-slate-500 font-semibold">
            Giao <span className="font-black text-slate-700">{patientName}</span> tại {roomName} cho:
          </div>
        </div>

        <div className="p-3 flex flex-col gap-2">
          {candidates.map(dentist => {
            const color = DENTIST_COLOR[dentist.dentistColor] ?? DENTIST_COLOR.slate;
            return (
              <button
                key={dentist.dentistId}
                onClick={() => onPick(dentist.dentistId)}
                className="flex items-center gap-3 p-3 rounded-xl border border-slate-200 hover:border-primary hover:bg-primary/5 transition-all cursor-pointer text-left"
              >
                <div className={`w-10 h-10 rounded-xl ${color.bg} border ${color.border} flex items-center justify-center font-black text-[12px] ${color.text} shrink-0`}>
                  {initialsOf(dentist.dentistName)}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13.5px] font-black text-slate-900 truncate">{dentist.dentistName}</div>
                  <div className="text-[11.5px] text-slate-400 font-semibold truncate">
                    {dentist.shifts.join(" · ") || "Chưa phân ca"}
                  </div>
                </div>
                {dentist.isOnShiftNow ? (
                  <span className="px-2.5 py-1 bg-emerald-100 text-emerald-700 rounded-lg text-[11px] font-black shrink-0">
                    Đang trực
                  </span>
                ) : (
                  <span className="px-2.5 py-1 bg-amber-100 text-amber-700 rounded-lg text-[11px] font-black shrink-0">
                    Sắp vào ca
                  </span>
                )}
              </button>
            );
          })}
        </div>

        <div className="px-6 py-4 border-t border-slate-100 flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 text-[13px] font-bold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-xl transition-all cursor-pointer"
          >
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}
