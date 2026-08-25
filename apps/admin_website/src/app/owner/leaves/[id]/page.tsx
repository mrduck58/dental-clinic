"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import {
  getLeaveRequestByIdApi,
  getLeaveRequestImpactApi,
  approveLeaveRequestApi,
  rejectLeaveRequestApi,
  type LeaveRequestDto,
  type LeaveImpactDto,
} from "../../../../lib/apiClient";
import { shiftLabel } from "../../../../lib/shifts";

// ── Helpers ───────────────────────────────────────────────────────────────────

const LEAVE_LABEL: Record<string, string> = {
  Annual:    "Phép năm",
  Sick:      "Nghỉ ốm",
  Maternity: "Nghỉ thai sản",
  Unpaid:    "Nghỉ không lương",
  Training:  "Nghỉ họp / Đào tạo",
};

const LEAVE_TYPE_COLORS: Record<string, string> = {
  Annual:    "bg-sky-50 text-sky-600 border-sky-200",
  Sick:      "bg-rose-50 text-rose-600 border-rose-200",
  Maternity: "bg-pink-50 text-pink-600 border-pink-200",
  Unpaid:    "bg-slate-100 text-slate-600 border-slate-200",
  Training:  "bg-violet-50 text-violet-600 border-violet-200",
};

const STATUS_STYLES: Record<string, { label: string; className: string; icon: string }> = {
  Pending:   { label: "Đang chờ duyệt", className: "bg-amber-50 text-amber-600 border-amber-200",  icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" },
  Approved:  { label: "Đã duyệt",        className: "bg-green-50 text-green-600 border-green-200",  icon: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
  Rejected:  { label: "Từ chối",         className: "bg-red-50 text-red-600 border-red-200",        icon: "M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
  Cancelled: { label: "Đã hủy",          className: "bg-slate-100 text-slate-500 border-slate-200", icon: "M6 18L18 6M6 6l12 12" },
};

const AVATAR_COLORS = [
  "bg-sky-500", "bg-violet-500", "bg-pink-500",
  "bg-amber-500", "bg-green-500", "bg-rose-500", "bg-teal-500",
];

const getAvatarColor = (name: string) =>
  AVATAR_COLORS[name.charCodeAt(0) % AVATAR_COLORS.length];

const getInitials = (name: string) =>
  name.split(" ").slice(-2).map((n) => n[0] ?? "").join("").toUpperCase();

const fmtDate = (s: string) => {
  const part = s.includes("T") ? s.split("T")[0] : s;
  const [y, m, d] = part.split("-");
  return `${d}/${m}/${y}`;
};

const fmtDateTime = (s: string) =>
  new Date(s).toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });

const WEEKDAY_LABEL = ["Chủ nhật", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7"];

// Thứ trong tuần của một chuỗi "YYYY-MM-DD" — tự tách chuỗi thay vì new Date(s) để khỏi lệch
// múi giờ (chuỗi ngày trần bị trình duyệt hiểu là UTC).
const weekdayOf = (s: string) => {
  const [y, m, d] = s.split("-").map(Number);
  return WEEKDAY_LABEL[new Date(y, m - 1, d).getDay()];
};

// Thứ Hai của tuần chứa ngày này — dùng để mở thẳng màn hình xếp lịch đúng tuần bị thủng.
const mondayOf = (s: string) => {
  const [y, m, d] = s.split("-").map(Number);
  const date = new Date(y, m - 1, d);
  date.setDate(date.getDate() - ((date.getDay() + 6) % 7));
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
};

const ROLE_LABEL: Record<string, string> = {
  dentist:   "Bác sĩ",
  assistant: "Phụ tá",
  staff:     "Nhân viên",
};

// Nhãn ca cho chip: "08:00-10:00" → "08:00–10:00" — luôn đủ giờ:phút, nhất quán với mọi ca khác.
const compactShift = (id: string) => {
  const m = id.match(/^(\d{1,2}):(\d{2})-(\d{1,2}):(\d{2})$/);
  if (!m) return shiftLabel(id);
  return `${m[1]}:${m[2]}–${m[3]}:${m[4]}`;
};

// Một ngày thường chỉ có 1 phòng nhưng nhiều ca — gom theo phòng+vai trò để không lặp lại
// tên phòng ở từng dòng ca.
type ImpactShift = LeaveImpactDto["days"][number]["shifts"][number];

const groupShiftsByRoom = (shifts: ImpactShift[]) => {
  const groups = new Map<string, { room: string; role: string; shifts: ImpactShift[] }>();
  shifts.forEach((s) => {
    const key = `${s.room}|${s.role}`;
    const group = groups.get(key) ?? { room: s.room, role: s.role, shifts: [] };
    group.shifts.push(s);
    groups.set(key, group);
  });
  return [...groups.values()];
};

// Đơn nộp lúc nào → duyệt lúc nào, quy ra "mất bao lâu mới xử lý".
const fmtElapsed = (fromIso: string, toIso: string): string | null => {
  const ms = new Date(toIso).getTime() - new Date(fromIso).getTime();
  if (!Number.isFinite(ms) || ms < 0) return null;

  const mins = Math.floor(ms / 60000);
  if (mins < 1) return "dưới 1 phút";

  const days  = Math.floor(mins / 1440);
  const hours = Math.floor((mins % 1440) / 60);
  const rest  = mins % 60;

  if (days > 0)  return hours > 0 ? `${days} ngày ${hours} giờ` : `${days} ngày`;
  if (hours > 0) return rest  > 0 ? `${hours} giờ ${rest} phút` : `${hours} giờ`;
  return `${rest} phút`;
};

// ── Page ──────────────────────────────────────────────────────────────────────

export default function LeaveDetailPage() {
  useRequireOwner();
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;

  const [leave, setLeave]           = useState<LeaveRequestDto | null>(null);
  const [isLoading, setIsLoading]   = useState(true);
  const [notFound, setNotFound]     = useState(false);
  const [isActing, setIsActing]     = useState(false);
  const [rejectNote, setRejectNote] = useState("");
  const [toast, setToast]           = useState<{ message: string; ok: boolean } | null>(null);

  // Ảnh hưởng của đơn nghỉ tới lịch làm việc — tải song song với chi tiết đơn.
  const [impact, setImpact]               = useState<LeaveImpactDto | null>(null);
  const [isImpactLoading, setImpactLoading] = useState(true);
  const [impactError, setImpactError]     = useState<string | null>(null);

  const [confirmApproveOpen, setConfirmApproveOpen] = useState(false);
  // Kết quả sau khi duyệt: các ca đã bị gỡ khỏi lịch nên endpoint impact sẽ trả về rỗng,
  // phải giữ lại con số ở client để còn báo Owner biết cần bổ sung bao nhiêu ca và ở tuần nào.
  const [approveResult, setApproveResult] = useState<{
    removedShiftCount: number;
    affectedDayCount: number;
    appointmentCount: number;
    weekStart: string | null;
  } | null>(null);

  // Không tự đặt trạng thái "đang tải" ở đây để effect nạp lần đầu không setState đồng bộ trong
  // thân effect (cùng lý do với loadRooms ở màn hình xếp lịch) — nút thử lại tự đặt trước khi gọi.
  const loadImpact = useCallback(() => {
    return getLeaveRequestImpactApi(id)
      .then((dto) => { setImpact(dto); setImpactError(null); })
      .catch((err) => setImpactError(err instanceof Error ? err.message : "Không tải được ảnh hưởng của đơn nghỉ."))
      .finally(() => setImpactLoading(false));
  }, [id]);

  const retryLoadImpact = () => { setImpactLoading(true); setImpactError(null); loadImpact(); };

  useEffect(() => {
    getLeaveRequestByIdApi(id)
      .then(setLeave)
      .catch(() => setNotFound(true))
      .finally(() => setIsLoading(false));
  }, [id]);

  useEffect(() => { loadImpact(); }, [loadImpact]);

  const showToast = (message: string, ok = true) => {
    setToast({ message, ok });
    setTimeout(() => setToast(null), 6000);
  };

  const handleApprove = async () => {
    if (!leave) return;
    setConfirmApproveOpen(false);
    setIsActing(true);
    try {
      const result = await approveLeaveRequestApi(leave.id);
      setLeave(result.request);
      setApproveResult({
        removedShiftCount: result.removedShiftCount,
        affectedDayCount: result.affectedDayCount,
        appointmentCount: result.affectedAppointmentCount,
        weekStart: result.affectedDates.length > 0 ? mondayOf(result.affectedDates[0]) : null,
      });
      await loadImpact();
      showToast(
        result.removedShiftCount > 0
          ? `Đã duyệt đơn và gỡ ${result.removedShiftCount} ca làm việc trong ${result.affectedDayCount} ngày. Lịch đang trống, cần bổ sung người.`
          : "Đơn xin nghỉ đã được duyệt. Không có ca làm việc nào bị ảnh hưởng."
      );
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Duyệt đơn thất bại.", false);
    } finally {
      setIsActing(false);
    }
  };

  const handleReject = async () => {
    if (!leave) return;
    setIsActing(true);
    try {
      const updated = await rejectLeaveRequestApi(leave.id, rejectNote.trim() || undefined);
      setLeave(updated);
      setRejectNote("");
      showToast("Đã từ chối đơn xin nghỉ.");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Từ chối đơn thất bại.", false);
    } finally {
      setIsActing(false);
    }
  };

  // ── Loading ────────────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <OwnerSidebar activeMenu="leaves" />
        <main className="flex-1 flex flex-col min-w-0">
          <PageHeader />
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <div className="w-10 h-10 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto" />
              <p className="mt-3 text-[14px] font-semibold text-slate-400">Đang tải...</p>
            </div>
          </div>
        </main>
      </div>
    );
  }

  // ── Not found ──────────────────────────────────────────────────────────────
  if (notFound || !leave) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <OwnerSidebar activeMenu="leaves" />
        <main className="flex-1 flex flex-col min-w-0">
          <PageHeader />
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <svg className="w-16 h-16 text-slate-300 mx-auto" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
              </svg>
              <p className="mt-4 text-[16px] font-bold text-slate-500">Không tìm thấy đơn nghỉ phép</p>
              <Link href="/owner/leaves"
                className="mt-4 inline-flex items-center gap-2 px-4 py-2 bg-primary text-white text-[14px] font-bold rounded-xl transition-all">
                Quay lại danh sách
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  const status    = STATUS_STYLES[leave.status] ?? STATUS_STYLES["Pending"];
  const typeColor = LEAVE_TYPE_COLORS[leave.leaveType] ?? "bg-slate-100 text-slate-600 border-slate-200";
  const typeLabel = LEAVE_LABEL[leave.leaveType] ?? leave.leaveType;
  const shortId   = leave.id.slice(0, 8).toUpperCase();
  const isPending  = leave.status === "Pending";
  const isApproved = leave.status === "Approved";
  // Có gì đó để hiển thị (ca trùng và/hoặc lịch hẹn) vs. thật sự có ca sẽ bị gỡ khi duyệt —
  // bác sĩ có thể có lịch hẹn trong ngày mà tuần đó chưa được xếp ca nào.
  const hasImpact      = !!impact && impact.days.length > 0;
  const hasShiftImpact = !!impact && impact.affectedShiftCount > 0;

  const elapsed = leave.reviewedAt ? fmtElapsed(leave.createdAt, leave.reviewedAt) : null;
  // Tuần cần bổ sung lịch: lấy từ kết quả duyệt nếu vừa bấm duyệt trong phiên này, còn khi tải lại
  // trang thì suy từ ngày bắt đầu nghỉ (các ca đã bị xóa nên không dò lại được từ impact).
  const resultWeekStart = approveResult?.weekStart ?? mondayOf(leave.startDate);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="leaves" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <OwnerPageHeader
          left={
            <Link href="/owner/leaves"
              className="p-2 text-slate-400 hover:text-primary hover:bg-slate-100 rounded-xl transition-all cursor-pointer">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
          }
          title="Chi tiết đơn xin nghỉ phép"
          subtitle="Xem thông tin và phê duyệt đơn nghỉ."
        />

        {/* TOAST */}
        {toast && (
          <div className="fixed top-6 right-6 z-[100] animate-fade-in">
            <div className={`bg-white rounded-2xl shadow-xl p-4 flex items-center gap-3 max-w-sm border ${toast.ok ? "border-green-200" : "border-red-200"}`}>
              <div className={`w-9 h-9 rounded-full flex items-center justify-center shrink-0 ${toast.ok ? "bg-green-100" : "bg-red-100"}`}>
                <svg className={`w-5 h-5 ${toast.ok ? "text-green-600" : "text-red-600"}`} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d={toast.ok
                    ? "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                    : "M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z"} />
                </svg>
              </div>
              <span className="text-[13px] font-black text-slate-900">{toast.message}</span>
              <button onClick={() => setToast(null)} className="text-slate-300 hover:text-slate-500 shrink-0 cursor-pointer ml-1">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
        )}

        {/* CONTENT */}
        <div className="p-8 flex-1 overflow-y-auto">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

            {/* ── LEFT: Thông tin đơn nghỉ ── */}
            <div className="lg:col-span-2 flex flex-col gap-5">

              {/* Card: Thông tin đơn */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between">
                  <h2 className="text-[15px] font-black text-slate-900">Thông tin đơn nghỉ phép</h2>
                  <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-[12px] font-black border ${status.className}`}>
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d={status.icon} />
                    </svg>
                    {status.label}
                  </span>
                </div>

                <div className="p-6">
                  <div className="grid grid-cols-2 gap-y-6 gap-x-8">
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Mã tham chiếu</span>
                      <span className="font-black text-primary text-[14px] font-mono">{shortId}</span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Loại nghỉ</span>
                      <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border ${typeColor}`}>
                        {typeLabel}
                      </span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày bắt đầu</span>
                      <span className="font-bold text-slate-700 text-[14px]">{fmtDate(leave.startDate)}</span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày kết thúc</span>
                      <span className="font-bold text-slate-700 text-[14px]">{fmtDate(leave.endDate)}</span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Số ngày nghỉ</span>
                      <span className="font-black text-slate-900 text-[18px]">{leave.daysCount} <span className="text-[13px] font-semibold text-slate-400">ngày</span></span>
                    </div>
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1.5">Ngày nộp đơn</span>
                      <span className="font-bold text-slate-700 text-[14px]">{fmtDateTime(leave.createdAt)}</span>
                    </div>
                  </div>

                  <div className="mt-6 pt-5 border-t border-slate-100">
                    <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-2">Lý do xin nghỉ</span>
                    <p className="text-[14px] text-slate-700 font-semibold leading-relaxed bg-slate-50 rounded-xl p-4">
                      {leave.reason}
                    </p>
                  </div>
                </div>
              </div>

              {/* Card: Ảnh hưởng tới lịch làm việc */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between gap-3">
                  <div>
                    <h2 className="text-[15px] font-black text-slate-900">Ảnh hưởng tới lịch làm việc</h2>
                    <p className="text-[12px] font-semibold text-slate-400 mt-0.5">
                      Các ca đã xếp cho {leave.userFullName} trùng vào khoảng ngày xin nghỉ.
                    </p>
                  </div>
                  {isPending && hasImpact && (
                    <span className="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-[11.5px] font-black border bg-amber-50 text-amber-600 border-amber-200">
                      <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                      </svg>
                      Cần xem trước khi duyệt
                    </span>
                  )}
                </div>

                <div className="p-6">
                  {isImpactLoading ? (
                    <div className="flex items-center gap-3 py-4">
                      <div className="w-5 h-5 border-2 border-primary border-t-transparent rounded-full animate-spin" />
                      <span className="text-[13px] font-semibold text-slate-400">Đang đối chiếu với lịch làm việc...</span>
                    </div>
                  ) : impactError ? (
                    <div className="flex items-center justify-between gap-3 p-4 rounded-xl bg-red-50 border border-red-100">
                      <span className="text-[13px] font-semibold text-red-700">{impactError}</span>
                      <button onClick={retryLoadImpact}
                        className="shrink-0 px-3 py-1.5 text-[12px] font-black text-red-600 bg-white border border-red-200 rounded-lg hover:bg-red-100 transition-all cursor-pointer">
                        Thử lại
                      </button>
                    </div>
                  ) : !hasImpact ? (
                    <div className="flex items-center gap-3 p-4 rounded-xl bg-slate-50 border border-slate-100">
                      <svg className="w-5 h-5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      <span className="text-[13px] font-semibold text-slate-500">
                        {leave.status === "Approved"
                          ? "Không còn ca nào của nhân sự này trong khoảng ngày nghỉ."
                          : "Không có ca làm việc nào đã xếp trùng vào khoảng ngày xin nghỉ."}
                      </span>
                    </div>
                  ) : (
                    <>
                      {/* Tổng quan — 3 con số nằm gọn trên một dòng */}
                      <div className="flex flex-wrap gap-2.5 mb-4">
                        <div className="flex-1 min-w-[130px] flex items-center gap-3 px-3.5 py-2.5 rounded-xl bg-rose-50 border border-rose-100">
                          <span className="text-[24px] font-black text-rose-700 leading-none">{impact!.affectedShiftCount}</span>
                          <span className="text-[11.5px] font-extrabold text-rose-500 leading-tight">
                            {isPending ? <>ca sẽ<br />bị gỡ</> : <>ca<br />trùng lịch</>}
                          </span>
                        </div>
                        <div className="flex-1 min-w-[130px] flex items-center gap-3 px-3.5 py-2.5 rounded-xl bg-amber-50 border border-amber-100">
                          <span className="text-[24px] font-black text-amber-700 leading-none">{impact!.affectedDayCount}</span>
                          <span className="text-[11.5px] font-extrabold text-amber-500 leading-tight">ngày<br />trùng lịch</span>
                        </div>
                        <div className="flex-1 min-w-[130px] flex items-center gap-3 px-3.5 py-2.5 rounded-xl bg-sky-50 border border-sky-100">
                          <span className="text-[24px] font-black text-sky-700 leading-none">{impact!.affectedAppointmentCount}</span>
                          <span className="text-[11.5px] font-extrabold text-sky-500 leading-tight">lịch hẹn<br />đã đặt</span>
                        </div>
                      </div>

                      {isPending && (
                        <p className="mb-4 text-[12.5px] font-semibold text-slate-500 leading-relaxed">
                          {hasShiftImpact
                            ? <>Duyệt đơn → các ca dưới đây bị gỡ, ô lịch trở thành trống.</>
                            : <>Không có ca nào đã xếp, lịch làm việc giữ nguyên.</>}
                          {impact!.affectedAppointmentCount > 0 && (
                            <> Lịch hẹn <span className="font-black text-slate-700">không</span> tự hủy — cần tự dời hoặc đổi bác sĩ.</>
                          )}
                        </p>
                      )}

                      {/* Mỗi ngày một khối: ca hiện dạng chip theo phòng */}
                      <div className="flex flex-col gap-2">
                        {impact!.days.map((day) => (
                          <div key={day.date} className="rounded-xl border border-slate-200 px-3.5 py-3 flex flex-col gap-2.5">
                            <div className="flex items-center justify-between gap-3">
                              <span className="text-[13px] font-black text-slate-800">
                                {weekdayOf(day.date)} · {fmtDate(day.date)}
                              </span>
                              <div className="flex items-center gap-1.5 shrink-0">
                                {day.shifts.length > 0 && (
                                  <span className="text-[11px] font-black text-rose-600 bg-rose-50 border border-rose-100 px-2 py-0.5 rounded-full">
                                    {day.shifts.length} ca
                                  </span>
                                )}
                                {day.appointmentCount > 0 && (
                                  <span className="text-[11px] font-black text-sky-600 bg-sky-50 border border-sky-100 px-2 py-0.5 rounded-full">
                                    {day.appointmentCount} lịch hẹn
                                  </span>
                                )}
                              </div>
                            </div>

                            {groupShiftsByRoom(day.shifts).map((group) => (
                              <div key={`${day.date}-${group.room}-${group.role}`} className="flex flex-wrap items-center gap-1.5">
                                <span className="px-2 py-1 rounded-lg bg-slate-100 border border-slate-200 text-[11px] font-black text-slate-600">
                                  {group.room || "—"} · {ROLE_LABEL[group.role] ?? group.role}
                                </span>
                                {/* Gạch ngang = "sẽ bị gỡ nếu duyệt". Đơn đã từ chối thì các ca này
                                    vẫn còn nguyên trong lịch nên hiện trung tính, không gạch. */}
                                {group.shifts.map((s) => (
                                  <span key={s.scheduleId}
                                    className={`px-2 py-1 rounded-lg text-[11.5px] font-black font-mono ${
                                      isPending
                                        ? "bg-rose-50 border border-rose-100 text-rose-600 line-through decoration-rose-300"
                                        : "bg-slate-50 border border-slate-200 text-slate-600"
                                    }`}>
                                    {compactShift(s.shift)}
                                  </span>
                                ))}
                              </div>
                            ))}

                            {day.shifts.length === 0 && (
                              <span className="text-[12px] font-semibold text-slate-400">
                                Không có ca nào đã xếp trong ngày này.
                              </span>
                            )}

                            {day.appointmentTimes.length > 0 && (
                              <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1 pt-0.5 border-t border-slate-100">
                                <span className="text-[11px] font-extrabold text-sky-600 uppercase tracking-wide pt-1.5">Giờ hẹn</span>
                                <span className="text-[12px] font-black text-slate-600 font-mono pt-1.5">
                                  {day.appointmentTimes.join("  ·  ")}
                                </span>
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </>
                  )}
                </div>
              </div>

            </div>

            {/* ── RIGHT: Nhân viên + Hành động + Kết quả phê duyệt ── */}
            <div className="flex flex-col gap-5">

              {/* Card: Thông tin nhân viên */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                  <h2 className="text-[15px] font-black text-slate-900">Nhân viên</h2>
                </div>
                <div className="p-6 flex flex-col items-center text-center gap-3">
                  <div className={`w-20 h-20 rounded-full ${getAvatarColor(leave.userFullName)} flex items-center justify-center text-white text-2xl font-black shadow-md`}>
                    {getInitials(leave.userFullName)}
                  </div>
                  <div>
                    <h3 className="text-[16px] font-extrabold text-slate-900">{leave.userFullName}</h3>
                    <p className="mt-1 text-[13px] text-slate-500 font-semibold">{leave.department ?? "—"}</p>
                  </div>
                  <Link href="/owner/leaves"
                    className="mt-1 text-[12.5px] text-primary font-bold hover:underline cursor-pointer">
                    ← Xem tất cả đơn
                  </Link>
                </div>
              </div>

              {/* Card: Hành động — chỉ khi Pending */}
              {leave.status === "Pending" && (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                    <h2 className="text-[15px] font-black text-slate-900">Hành động</h2>
                  </div>
                  <div className="p-6 flex flex-col gap-4">
                    <div>
                      <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide block mb-2">
                        Ghi chú <span className="text-slate-300 normal-case font-semibold">(tuỳ chọn khi duyệt, bắt buộc khi từ chối)</span>
                      </label>
                      <textarea
                        rows={3}
                        value={rejectNote}
                        onChange={(e) => setRejectNote(e.target.value)}
                        placeholder="Nhập lý do hoặc ghi chú..."
                        className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none"
                      />
                    </div>

                    <div className="flex flex-col gap-2.5">
                      {/* Duyệt — luôn hỏi lại vì thao tác này xóa luôn ca đã xếp trong lịch */}
                      <button onClick={() => setConfirmApproveOpen(true)} disabled={isActing || isImpactLoading}
                        className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-green-600 hover:bg-green-700 disabled:opacity-60 disabled:cursor-not-allowed text-white text-[14px] font-extrabold rounded-xl shadow-sm shadow-green-600/20 transition-all cursor-pointer">
                        {isActing ? (
                          <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/>
                          </svg>
                        ) : (
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                        )}
                        Duyệt đơn
                      </button>

                      {/* Từ chối */}
                      <button onClick={handleReject} disabled={isActing || !rejectNote.trim()}
                        className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-red-50 border-2 border-red-200 text-red-600 hover:bg-red-100 hover:border-red-300 disabled:opacity-50 disabled:cursor-not-allowed text-[14px] font-extrabold rounded-xl transition-all cursor-pointer">
                        {isActing ? (
                          <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3v4a8 8 0 00-8 8h4z"/>
                          </svg>
                        ) : (
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                        )}
                        Từ chối
                      </button>

                      {!rejectNote.trim() && (
                        <p className="text-[11.5px] text-slate-400 font-semibold text-center">
                          Nhập lý do để kích hoạt nút Từ chối
                        </p>
                      )}

                      {hasShiftImpact && (
                        <p className="text-[11.5px] text-amber-600 font-bold text-center leading-relaxed">
                          Duyệt đơn sẽ gỡ {impact!.affectedShiftCount} ca khỏi lịch làm việc
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              )}

              {/* Card: Kết quả phê duyệt (nếu đã xử lý) — nằm dưới cùng cột phải, ngay chỗ
                  card Hành động vừa biến mất sau khi đơn được xử lý */}
              {leave.reviewedAt && (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                    <h2 className="text-[15px] font-black text-slate-900">Kết quả phê duyệt</h2>
                  </div>

                  <div className="p-6 flex flex-col gap-5">
                    {/* Trạng thái nổi bật */}
                    <div className={`rounded-xl p-4 border flex items-center gap-3 ${
                      isApproved ? "bg-green-50 border-green-200" : "bg-red-50 border-red-200"
                    }`}>
                      <div className={`w-11 h-11 rounded-full flex items-center justify-center shrink-0 ${
                        isApproved ? "bg-green-100" : "bg-red-100"
                      }`}>
                        <svg className={`w-6 h-6 ${isApproved ? "text-green-600" : "text-red-600"}`}
                          fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d={isApproved
                            ? "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                            : "M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z"} />
                        </svg>
                      </div>
                      <div className="min-w-0">
                        <span className={`block text-[15px] font-black ${isApproved ? "text-green-800" : "text-red-800"}`}>
                          {isApproved ? "Đã duyệt đơn" : "Đã từ chối đơn"}
                        </span>
                        <span className={`text-[12px] font-bold ${isApproved ? "text-green-600" : "text-red-600"}`}>
                          {fmtDateTime(leave.reviewedAt)}
                        </span>
                      </div>
                    </div>

                    {/* Tiến trình xử lý */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-3">
                        Tiến trình xử lý
                      </span>
                      <div className="flex flex-col gap-0">
                        <div className="flex gap-3">
                          <div className="flex flex-col items-center shrink-0">
                            <span className="w-2.5 h-2.5 rounded-full bg-slate-300 mt-1.5" />
                            <span className="w-px flex-1 bg-slate-200 my-1" />
                          </div>
                          <div className="pb-3">
                            <span className="block text-[12.5px] font-black text-slate-700">Nhân viên nộp đơn</span>
                            <span className="text-[12px] font-semibold text-slate-400">{fmtDateTime(leave.createdAt)}</span>
                          </div>
                        </div>
                        <div className="flex gap-3">
                          <div className="flex flex-col items-center shrink-0">
                            <span className={`w-2.5 h-2.5 rounded-full mt-1.5 ${isApproved ? "bg-green-500" : "bg-red-500"}`} />
                          </div>
                          <div>
                            <span className="block text-[12.5px] font-black text-slate-700">
                              Chủ phòng khám {isApproved ? "duyệt" : "từ chối"}
                            </span>
                            <span className="text-[12px] font-semibold text-slate-400">{fmtDateTime(leave.reviewedAt)}</span>
                          </div>
                        </div>
                      </div>
                      {elapsed && (
                        <p className="mt-3 text-[12px] font-bold text-slate-500 bg-slate-50 rounded-lg px-3 py-2">
                          Thời gian xử lý: <span className="text-slate-700 font-black">{elapsed}</span>
                        </p>
                      )}
                    </div>

                    {/* Tóm tắt đơn */}
                    <div className="grid grid-cols-2 gap-2.5">
                      <div className="rounded-xl bg-slate-50 border border-slate-100 px-3 py-2.5">
                        <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Khoảng nghỉ</span>
                        <span className="text-[13px] font-black text-slate-700">
                          {fmtDate(leave.startDate)} – {fmtDate(leave.endDate)}
                        </span>
                      </div>
                      <div className="rounded-xl bg-slate-50 border border-slate-100 px-3 py-2.5">
                        <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Loại nghỉ</span>
                        <span className="text-[13px] font-black text-slate-700">{typeLabel}</span>
                      </div>
                    </div>

                    {/* Ghi chú của người duyệt */}
                    {leave.reviewerNote && (
                      <div>
                        <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-2">
                          {isApproved ? "Ghi chú từ quản lý" : "Lý do từ chối"}
                        </span>
                        <div className={`p-3.5 rounded-xl text-[13px] font-semibold leading-relaxed ${
                          isApproved
                            ? "bg-green-50 text-green-800 border border-green-100"
                            : "bg-red-50 text-red-800 border border-red-100"
                        }`}>
                          {leave.reviewerNote}
                        </div>
                      </div>
                    )}

                    {/* Hệ quả lên lịch làm việc */}
                    <div>
                      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-2">
                        Lịch làm việc
                      </span>
                      {isApproved ? (
                        <div className="rounded-xl bg-amber-50 border border-amber-200 p-3.5">
                          <p className="text-[12.5px] font-black text-amber-800 leading-relaxed">
                            {approveResult && approveResult.removedShiftCount > 0
                              ? `Đã gỡ ${approveResult.removedShiftCount} ca trong ${approveResult.affectedDayCount} ngày khỏi lịch.`
                              : "Các ca trùng khoảng nghỉ đã được gỡ khỏi lịch."}
                          </p>
                          <p className="text-[12px] font-semibold text-amber-700 mt-1 leading-relaxed">
                            Cần phân công người thay thế. Thông báo nhắc bổ sung đã được gửi cho chủ phòng khám.
                          </p>
                          <Link href={`/owner/schedule/edit?week=${resultWeekStart}`}
                            className="mt-3 w-full inline-flex items-center justify-center gap-1.5 px-3.5 py-2.5 bg-amber-500 hover:bg-amber-600 text-white text-[12.5px] font-extrabold rounded-xl transition-all cursor-pointer">
                            Bổ sung lịch tuần {fmtDate(resultWeekStart).slice(0, 5)}
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                            </svg>
                          </Link>
                        </div>
                      ) : (
                        <div className="rounded-xl bg-slate-50 border border-slate-200 px-3.5 py-3">
                          <p className="text-[12.5px] font-semibold text-slate-600 leading-relaxed">
                            Giữ nguyên — đơn bị từ chối nên không ca nào bị gỡ.
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* MODAL: xác nhận duyệt đơn — nói rõ hệ quả lên lịch làm việc trước khi xóa ca */}
        {confirmApproveOpen && (
          <div className="fixed inset-0 z-[110] flex items-center justify-center bg-slate-900/40 p-4 animate-fade-in">
            <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-green-100 flex items-center justify-center shrink-0">
                  <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <h3 className="text-[15.5px] font-black text-slate-900">Xác nhận duyệt đơn xin nghỉ</h3>
              </div>

              <div className="p-6 flex flex-col gap-4">
                <p className="text-[13.5px] font-semibold text-slate-600 leading-relaxed">
                  Duyệt đơn nghỉ của <span className="font-black text-slate-900">{leave.userFullName}</span> từ{" "}
                  <span className="font-black text-slate-900">{fmtDate(leave.startDate)}</span> đến{" "}
                  <span className="font-black text-slate-900">{fmtDate(leave.endDate)}</span>.
                </p>

                {hasShiftImpact ? (
                  <div className="p-4 rounded-xl bg-amber-50 border border-amber-200 flex flex-col gap-2">
                    <p className="text-[13px] font-black text-amber-800">
                      {impact!.affectedShiftCount} ca trong {impact!.affectedDayCount} ngày sẽ bị gỡ khỏi lịch làm việc.
                    </p>
                    <p className="text-[12.5px] font-semibold text-amber-700 leading-relaxed">
                      Các ô lịch đó trở thành trống và bạn sẽ nhận thông báo nhắc bổ sung người thay thế.
                      {impact!.affectedAppointmentCount > 0 && (
                        <> Ngoài ra có <span className="font-black">{impact!.affectedAppointmentCount} lịch hẹn</span> đã đặt
                        trong những ngày này — hệ thống không tự hủy, bạn cần tự dời hoặc đổi bác sĩ.</>
                      )}
                    </p>
                  </div>
                ) : impactError ? (
                  <div className="p-4 rounded-xl bg-red-50 border border-red-200">
                    <p className="text-[12.5px] font-semibold text-red-700 leading-relaxed">
                      Chưa đối chiếu được với lịch làm việc ({impactError}). Nếu vẫn duyệt, hệ thống vẫn tự gỡ các ca
                      trùng nhưng bạn sẽ không biết trước là những ca nào.
                    </p>
                  </div>
                ) : (
                  <div className="p-4 rounded-xl bg-slate-50 border border-slate-200">
                    <p className="text-[12.5px] font-semibold text-slate-600 leading-relaxed">
                      Không có ca làm việc nào đã xếp trùng vào khoảng ngày này, lịch làm việc sẽ giữ nguyên.
                      {!!impact && impact.affectedAppointmentCount > 0 && (
                        <> Nhưng có <span className="font-black text-slate-800">{impact.affectedAppointmentCount} lịch hẹn</span> đã đặt
                        trong những ngày này — hệ thống không tự hủy, bạn cần tự dời hoặc đổi bác sĩ.</>
                      )}
                    </p>
                  </div>
                )}

                <div className="flex gap-2.5 pt-1">
                  <button onClick={() => setConfirmApproveOpen(false)} disabled={isActing}
                    className="flex-1 px-4 py-2.5 bg-slate-100 hover:bg-slate-200 disabled:opacity-60 text-slate-600 text-[13.5px] font-extrabold rounded-xl transition-all cursor-pointer">
                    Huỷ
                  </button>
                  <button onClick={handleApprove} disabled={isActing}
                    className="flex-1 px-4 py-2.5 bg-green-600 hover:bg-green-700 disabled:opacity-60 text-white text-[13.5px] font-extrabold rounded-xl transition-all cursor-pointer">
                    {isActing ? "Đang duyệt..." : "Xác nhận duyệt"}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

function PageHeader() {
  return (
    <OwnerPageHeader
      left={
        <Link href="/owner/leaves"
          className="flex items-center gap-2 text-slate-500 hover:text-primary transition-all cursor-pointer">
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
          </svg>
          <span className="text-[14px] font-bold">Quay lại</span>
        </Link>
      }
      title="Chi tiết đơn xin nghỉ phép"
    />
  );
}
