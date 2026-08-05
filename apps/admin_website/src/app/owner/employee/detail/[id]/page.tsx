"use client";

import React, { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import OwnerSidebar from "../../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../../hooks/useRequireOwner";
import { getWeekScheduleApi, getStaffByIdApi, type StaffDto, type ScheduleEntryDto } from "../../../../../lib/apiClient";
import { periodOfShift } from "../../../../../lib/shifts";
import { ROLE_LABELS, ROLE_BADGE_CLASSES, type UiRole } from "../../../../../lib/roles";

// ── Mock review data — TODO: replace with real reviews API ────────────────
const MOCK_RATING = 4.8;
const MOCK_REVIEWS = [
  { id: "1", patient: "Nguyễn V.A.", date: "10/06/2026", rating: 5, comment: "Nha sĩ rất tận tâm, giải thích rõ ràng từng bước điều trị. Rất hài lòng với kết quả!" },
  { id: "2", patient: "Trần T.B.", date: "08/06/2026", rating: 4, comment: "Tay nghề tốt, ít đau. Thái độ phục vụ thân thiện và chuyên nghiệp, sẽ quay lại." },
  { id: "3", patient: "Lê Văn C.", date: "05/06/2026", rating: 5, comment: "Phòng khám sạch sẽ, nha sĩ giỏi và nhiệt tình. Đã giới thiệu cho nhiều người thân!" },
  { id: "4", patient: "Phạm T.D.", date: "01/06/2026", rating: 3, comment: "Chờ khá lâu nhưng nha sĩ điều trị tốt và cẩn thận." },
  { id: "5", patient: "Hoàng V.E.", date: "28/05/2026", rating: 5, comment: "Rất chuyên nghiệp! Nha sĩ giải thích kỹ từng vấn đề, tôi rất an tâm điều trị." },
  { id: "6", patient: "Vũ T.F.", date: "25/05/2026", rating: 4, comment: "Kỹ thuật tốt, giá cả hợp lý. Sẽ giới thiệu nha sĩ cho người thân." },
  { id: "7", patient: "Đặng T.G.", date: "20/05/2026", rating: 2, comment: "Hơi vội, không giải thích nhiều. Mong nha sĩ dành thêm thời gian cho bệnh nhân." },
];

// ── Constants ──────────────────────────────────────────────────────────────

const MONTH_NAMES = [
  "Tháng 1","Tháng 2","Tháng 3","Tháng 4","Tháng 5","Tháng 6",
  "Tháng 7","Tháng 8","Tháng 9","Tháng 10","Tháng 11","Tháng 12",
];
const DAY_HEADERS = ["T2","T3","T4","T5","T6","T7","CN"];

const STATUS_LABELS: Record<string, string> = {
  Active: "Đang làm việc", "On Leave": "Nghỉ phép", Inactive: "Đã nghỉ việc",
};
const STATUS_COLORS: Record<string, string> = {
  Active: "bg-green-50 text-green-700 border-green-200",
  "On Leave": "bg-amber-50 text-amber-700 border-amber-200",
  Inactive: "bg-red-50 text-red-600 border-red-200",
};

// ── Helpers ────────────────────────────────────────────────────────────────

function fmt(s: string | null | undefined): string {
  if (!s) return "—";
  const [y, m, d] = s.split("-");
  return `${d}/${m}/${y}`;
}

function calcSeniority(startDate: string | null | undefined): string {
  if (!startDate) return "—";
  const start = new Date(startDate);
  const now = new Date();
  let years = now.getFullYear() - start.getFullYear();
  let months = now.getMonth() - start.getMonth();
  if (months < 0) { years--; months += 12; }
  if (years === 0) return `${months} tháng`;
  if (months === 0) return `${years} năm`;
  return `${years} năm ${months} tháng`;
}

function calcAge(dob: string | null | undefined): string {
  if (!dob) return "—";
  const b = new Date(dob), n = new Date();
  let age = n.getFullYear() - b.getFullYear();
  if (n.getMonth() < b.getMonth() || (n.getMonth() === b.getMonth() && n.getDate() < b.getDate())) age--;
  return `${age} tuổi`;
}

function getWeekStartsForMonth(year: number, month: number): string[] {
  const first = new Date(year, month, 1);
  const last  = new Date(year, month + 1, 0);
  const mon   = new Date(first);
  const dow   = first.getDay();
  mon.setDate(first.getDate() - (dow === 0 ? 6 : dow - 1));
  const weeks: string[] = [];
  const cur = new Date(mon);
  while (cur <= last) {
    weeks.push(cur.toISOString().split("T")[0]);
    cur.setDate(cur.getDate() + 7);
  }
  return weeks;
}

function buildCalendarDays(year: number, month: number): Array<{ date: string; inMonth: boolean }> {
  const first = new Date(year, month, 1);
  const last  = new Date(year, month + 1, 0);
  const days: Array<{ date: string; inMonth: boolean }> = [];

  const firstDow = first.getDay();
  const pad = firstDow === 0 ? 6 : firstDow - 1;
  const prevLast = new Date(year, month, 0).getDate();
  for (let i = pad - 1; i >= 0; i--) {
    days.push({ date: new Date(year, month - 1, prevLast - i).toISOString().split("T")[0], inMonth: false });
  }
  for (let i = 1; i <= last.getDate(); i++) {
    days.push({ date: new Date(year, month, i).toISOString().split("T")[0], inMonth: true });
  }
  const rem = days.length % 7;
  if (rem !== 0) {
    for (let i = 1; i <= 7 - rem; i++) {
      days.push({ date: new Date(year, month + 1, i).toISOString().split("T")[0], inMonth: false });
    }
  }
  return days;
}

// ── Sub-components ─────────────────────────────────────────────────────────

function FieldRow({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <div className="flex gap-3 py-2.5 border-b border-slate-50 last:border-0">
      <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider w-40 shrink-0 pt-0.5">{label}</span>
      <span className="text-[13.5px] font-semibold text-slate-800 flex-1">{value ?? "—"}</span>
    </div>
  );
}

function Stars({ rating, size = 12 }: { rating: number; size?: number }) {
  return (
    <span className="inline-flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map(i => (
        <svg key={i} style={{ width: size, height: size }} viewBox="0 0 24 24"
          fill={i <= Math.round(rating) ? "#f59e0b" : "#e2e8f0"}>
          <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
        </svg>
      ))}
    </span>
  );
}

// ── Page ───────────────────────────────────────────────────────────────────

export default function StaffDetailPage() {
  useRequireOwner();
  const router = useRouter();
  const params = useParams();
  const id = params?.id as string;

  const [staff, setStaff] = useState<StaffDto | null>(null);
  const [isLoadingStaff, setIsLoadingStaff] = useState(true);
  const [errorStaff, setErrorStaff] = useState<string | null>(null);
  const [monthSchedule, setMonthSchedule] = useState<ScheduleEntryDto[]>([]);
  const [isLoadingSchedule, setIsLoadingSchedule] = useState(true);
  const [starFilter, setStarFilter] = useState<number | null>(null);

  const today      = new Date();
  const todayStr   = today.toISOString().split("T")[0];
  const [viewYear,  setViewYear]  = useState(today.getFullYear());
  const [viewMonth, setViewMonth] = useState(today.getMonth());

  const isDentist = staff?.role === "Dentist";

  useEffect(() => {
    const raw = sessionStorage.getItem("staffDetailData");
    if (raw) {
      setStaff(JSON.parse(raw) as StaffDto);
      setIsLoadingStaff(false);
      sessionStorage.removeItem("staffDetailData");
    } else if (id) {
      setIsLoadingStaff(true);
      getStaffByIdApi(id)
        .then((res) => {
          setStaff(res);
          setErrorStaff(null);
        })
        .catch((err) => {
          setErrorStaff(err instanceof Error ? err.message : "Không thể tải thông tin nhân viên");
        })
        .finally(() => {
          setIsLoadingStaff(false);
        });
    } else {
      setIsLoadingStaff(false);
    }
  }, [id]);

  useEffect(() => {
    setIsLoadingSchedule(true);
    const weeks = getWeekStartsForMonth(viewYear, viewMonth);
    Promise.all(weeks.map(ws => getWeekScheduleApi(ws)))
      .then(r => setMonthSchedule(r.flat()))
      .catch(() => setMonthSchedule([]))
      .finally(() => setIsLoadingSchedule(false));
  }, [viewYear, viewMonth]);

  const navigateMonth = (dir: 1 | -1) => {
    let m = viewMonth + dir, y = viewYear;
    if (m < 0) { m = 11; y--; }
    if (m > 11) { m = 0; y++; }
    setViewMonth(m); setViewYear(y);
  };

  // ── Loading state ──────────────────────────────────────────────────────
  if (isLoadingStaff) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <OwnerSidebar activeMenu="staff" />
        <main className="flex-1 flex flex-col items-center justify-center gap-4">
          <div className="w-12 h-12 rounded-full border-4 border-primary border-t-transparent animate-spin" />
          <p className="text-slate-500 font-bold text-[14px]">Đang tải hồ sơ nhân sự...</p>
        </main>
      </div>
    );
  }

  // ── Empty / Error state ──────────────────────────────────────────────────
  if (errorStaff || !staff) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <OwnerSidebar activeMenu="staff" />
        <main className="flex-1 flex flex-col items-center justify-center gap-4">
          <svg className="w-14 h-14 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
          </svg>
          <p className="text-slate-500 font-bold text-[15px]">{errorStaff || "Không tìm thấy dữ liệu."}</p>
          <button onClick={() => router.push("/owner/employee")} className="px-6 py-3 bg-primary text-white font-extrabold rounded-xl cursor-pointer">
            Quay lại danh sách
          </button>
        </main>
      </div>
    );
  }

  // ── Derived values ─────────────────────────────────────────────────────

  const monthPrefix    = `${viewYear}-${String(viewMonth + 1).padStart(2, "0")}`;
  const staffSchedule  = monthSchedule.filter(e =>
    e.name === staff.fullName && !e.isHoliday && e.date.startsWith(monthPrefix)
  );
  const morningCount   = staffSchedule.filter(e => periodOfShift(e.shift) === "Buổi sáng").length;
  const afternoonCount = staffSchedule.filter(e => periodOfShift(e.shift) === "Buổi chiều").length;
  const eveningCount   = staffSchedule.filter(e => periodOfShift(e.shift) === "Buổi tối").length;
  const totalShifts    = staffSchedule.length;
  const calDays        = buildCalendarDays(viewYear, viewMonth);

  const empStatus = staff.employmentStatus || "Active";
  const initials  = staff.fullName
    ? staff.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()
    : staff.email.slice(0, 2).toUpperCase();

  const exp = staff.yearsOfExperience ?? 5;
  const isPartTime = exp % 2 === 0 && isDentist;
  const isShift = !isPartTime && (staff.role === "Staff" && (staff.position?.toLowerCase().includes("lễ tân") || staff.position?.toLowerCase().includes("tiếp đón")));
  const employmentType = staff.employmentType || (isPartTime ? "Part-time" : isShift ? "Shift-based" : "Full-time");
  const baseSalary = staff.baseSalary ?? (isDentist ? (25000000 + exp * 1500000) : (10000000 + (staff.fullName?.length || 5) * 200000));
  const salaryUnit = staff.salaryUnit || (isPartTime ? "Theo ngày" : isShift ? "Theo ca" : "Theo tháng");
  const leaveAccrued = staff.leaveAccrued ?? (isDentist ? 1.5 : 1);
  const leaveMax = staff.leaveAccrued ? Math.ceil(staff.leaveAccrued * 2) : (isDentist ? 3 : 2);

  // ── Reviews ────────────────────────────────────────────────────────────
  const starCounts = [5, 4, 3, 2, 1].map(s => ({
    star: s,
    count: MOCK_REVIEWS.filter(r => r.rating === s).length,
  }));
  const filteredReviews = starFilter === null
    ? MOCK_REVIEWS
    : MOCK_REVIEWS.filter(r => r.rating === starFilter);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="staff" />

      <main className="flex-1 flex flex-col min-w-0">

        {/* ── Top Header ── */}
        <OwnerPageHeader
          left={
            <button onClick={() => router.push("/owner/employee")} className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-all cursor-pointer">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </button>
          }
          title="Chi Tiết Nhân Sự"
          subtitle={<>Hồ sơ và thông tin làm việc của {isDentist ? "nha sĩ" : "nhân viên"}</>}
          right={
            <button
              onClick={() => {
                sessionStorage.setItem("staffEditData", JSON.stringify(staff));
                router.push(isDentist
                  ? `/owner/employee/edit-dentist/${staff.id}`
                  : `/owner/employee/edit-staff/${staff.id}`
                );
              }}
              className="flex items-center gap-2 px-4 py-2.5 bg-white hover:bg-slate-50 border border-slate-200 text-slate-700 text-[13.5px] font-extrabold rounded-xl transition-all cursor-pointer shadow-sm"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
              </svg>
              Chỉnh sửa
            </button>
          }
        />

        <div className="flex-1 p-8 flex justify-center overflow-y-auto">
          <div className="w-full max-w-5xl flex flex-col gap-6">

            {/* ── Profile Header ── */}
            <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
              <div className="flex items-start gap-5">
                {staff.profilePictureUrl ? (
                  <img src={staff.profilePictureUrl} alt={staff.fullName || ""} className="w-20 h-20 rounded-2xl object-cover border border-slate-200 shadow-sm shrink-0" />
                ) : (
                  <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-primary/10 to-primary/5 border border-primary/10 flex items-center justify-center shrink-0 select-none">
                    <span className="text-2xl font-black text-primary">{initials}</span>
                  </div>
                )}
                <div className="flex-1 min-w-0">
                  <div className="flex flex-wrap items-center gap-2 mb-1">
                    <h2 className="text-2xl font-black text-slate-900 tracking-tight">{staff.fullName || "—"}</h2>
                    {staff.employeeId && (
                      <span className="font-mono text-[13px] font-black text-primary bg-red-50 border border-red-100 px-2 py-0.5 rounded-lg">{staff.employeeId}</span>
                    )}
                  </div>
                  <div className="flex flex-wrap items-center gap-2 mb-3">
                    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border ${ROLE_BADGE_CLASSES[staff.role as UiRole] || "bg-slate-50 border-slate-200 text-slate-600"}`}>
                      {ROLE_LABELS[staff.role as UiRole] || staff.role}
                    </span>
                    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border ${STATUS_COLORS[empStatus]}`}>
                      {STATUS_LABELS[empStatus]}
                    </span>
                    {!staff.hasAccount && (
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border bg-amber-50 text-amber-700 border-amber-200">Chưa có tài khoản</span>
                    )}
                    {staff.hasAccount && !staff.isActive && (
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border bg-red-50 text-red-600 border-red-200">Tài khoản đang khóa</span>
                    )}
                  </div>
                  <div className="flex flex-wrap items-center gap-4 text-[13px] text-slate-500 font-semibold">
                    {staff.email && (
                      <span className="flex items-center gap-1.5">
                        <svg className="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                        </svg>
                        {staff.email}
                      </span>
                    )}
                    {staff.phoneNumber && (
                      <span className="flex items-center gap-1.5">
                        <svg className="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 6.75c0 8.284 6.716 15 15 15h2.25a2.25 2.25 0 002.25-2.25v-1.372c0-.516-.351-.966-.852-1.091l-4.423-1.106c-.44-.11-.902.055-1.173.417l-.97 1.293c-.282.376-.769.542-1.21.38a12.035 12.035 0 01-7.143-7.143c-.162-.441.004-.928.38-1.21l1.293-.97c.363-.271.527-.734.417-1.173L6.963 3.102a1.125 1.125 0 00-1.091-.852H4.5A2.25 2.25 0 002.25 4.5v2.25z" />
                        </svg>
                        {staff.phoneNumber}
                      </span>
                    )}
                    {staff.address && (
                      <span className="flex items-center gap-1.5">
                        <svg className="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1115 0z" />
                        </svg>
                        {staff.address}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* ── Stat Cards ── */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {isDentist ? (
                <div className="bg-white rounded-2xl border border-amber-100 shadow-sm p-4 flex flex-col gap-1.5">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Đánh giá</span>
                  <div className="flex items-baseline gap-1.5">
                    <span className="text-xl font-black text-amber-500">{MOCK_RATING}</span>
                    <svg className="w-4 h-4 text-amber-400 mb-0.5" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                    </svg>
                    <span className="text-[11px] text-slate-400 font-semibold">/ 5.0</span>
                  </div>
                  <span className="text-[11.5px] text-slate-400 font-semibold">{MOCK_REVIEWS.length} đánh giá</span>
                </div>
              ) : (
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-4 flex flex-col gap-1">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Thâm niên</span>
                  <span className="text-xl font-black text-slate-900">{calcSeniority(staff.startDate)}</span>
                  <span className="text-[11.5px] text-slate-400 font-semibold">Từ {fmt(staff.startDate)}</span>
                </div>
              )}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-4 flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Ca tháng này</span>
                <span className="text-xl font-black text-slate-900">{isLoadingSchedule ? "..." : totalShifts}</span>
                <span className="text-[11.5px] text-slate-400 font-semibold">Ca làm việc</span>
              </div>
              <div className="bg-white rounded-2xl border border-sky-100 shadow-sm p-4 flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Ca sáng</span>
                <span className="text-xl font-black text-sky-500">{isLoadingSchedule ? "..." : morningCount}</span>
                <span className="text-[11.5px] text-slate-400 font-semibold">Tháng hiện tại</span>
              </div>
              <div className="bg-white rounded-2xl border border-amber-100 shadow-sm p-4 flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Ca chiều</span>
                <span className="text-xl font-black text-amber-500">{isLoadingSchedule ? "..." : afternoonCount}</span>
                <span className="text-[11.5px] text-slate-400 font-semibold">Tháng hiện tại</span>
              </div>
              <div className="bg-white rounded-2xl border border-violet-100 shadow-sm p-4 flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Ca tối</span>
                <span className="text-xl font-black text-violet-500">{isLoadingSchedule ? "..." : eveningCount}</span>
                <span className="text-[11.5px] text-slate-400 font-semibold">Tháng hiện tại</span>
              </div>
            </div>

            {/* ── Three-column layout ── */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">

              {/* ── Left 2/3: info cards ── */}
              <div className="lg:col-span-2 flex flex-col gap-6">

                {/* Thông tin cơ bản */}
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
                  <div className="flex items-center gap-2 mb-4">
                    <div className="w-1 h-4 bg-primary rounded-full" />
                    <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin cơ bản</span>
                  </div>
                  <FieldRow label="Họ và tên" value={staff.fullName} />
                  <FieldRow label="Giới tính" value={staff.gender} />
                  <FieldRow label="Ngày sinh" value={staff.dateOfBirth ? `${fmt(staff.dateOfBirth)} · ${calcAge(staff.dateOfBirth)}` : null} />
                  <FieldRow label="Số điện thoại" value={staff.phoneNumber} />
                  <FieldRow label="Email" value={staff.email} />
                  <FieldRow label="Địa chỉ" value={staff.address} />
                  <FieldRow label="Tạo hồ sơ ngày" value={fmt(staff.createdAt?.split("T")[0])} />
                </div>

                {/* Dentist: chuyên môn */}
                {isDentist && (
                  <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
                    <div className="flex items-center gap-2 mb-4">
                      <div className="w-1 h-4 bg-secondary rounded-full" />
                      <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin chuyên môn</span>
                    </div>
                    <FieldRow label="Chuyên khoa" value={staff.specialty} />
                    <FieldRow label="Số CCHN" value={staff.licenseNumber} />
                    <FieldRow label="Dịch vụ phụ trách" value={staff.servicesHandled} />
                    <FieldRow label="Ngày bắt đầu làm" value={fmt(staff.startDate)} />
                    <FieldRow label="Ngày cấp CC" value={fmt(staff.certificateIssuedDate)} />
                    <FieldRow label="Nơi cấp CC" value={staff.certificateIssuedBy} />
                    <FieldRow label="Số năm kinh nghiệm" value={staff.yearsOfExperience != null ? `${staff.yearsOfExperience} năm` : null} />
                    <FieldRow label="Trình độ học vấn" value={staff.education} />
                    {staff.bio && (
                      <div className="pt-3 mt-1">
                        <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block mb-2">Giới thiệu</span>
                        <p className="text-[13.5px] font-semibold text-slate-700 leading-relaxed bg-slate-50 p-4 rounded-xl border border-slate-100">{staff.bio}</p>
                      </div>
                    )}
                  </div>
                )}

                {/* Staff: công việc */}
                {!isDentist && (
                  <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
                    <div className="flex items-center gap-2 mb-4">
                      <div className="w-1 h-4 bg-primary rounded-full" />
                      <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin công việc</span>
                    </div>
                    <FieldRow label="Chức vụ" value={staff.position} />
                    <FieldRow label="Bộ phận" value={staff.department} />
                    <FieldRow label="Ngày vào làm" value={fmt(staff.startDate)} />
                    <FieldRow label="Trạng thái" value={STATUS_LABELS[empStatus]} />
                    {staff.bio && (
                      <div className="pt-3 mt-1">
                        <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block mb-2">Mô tả công việc</span>
                        <p className="text-[13.5px] font-semibold text-slate-700 leading-relaxed bg-slate-50 p-4 rounded-xl border border-slate-100">{staff.bio}</p>
                      </div>
                    )}
                  </div>
                )}

                {/* Remuneration & Leaves */}
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
                  <div className="flex items-center gap-2 mb-4">
                    <div className="w-1 h-4 bg-primary rounded-full" />
                    <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin đãi ngộ & chế độ lương phép</span>
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-6 text-[13.5px]">
                    <div>
                      <span className="block text-slate-400 font-semibold">Hình thức làm việc:</span>
                      <span className="font-extrabold text-slate-900 block mt-1">
                        {employmentType === "Full-time"
                          ? "Toàn thời gian"
                          : employmentType === "Part-time"
                          ? "Bán thời gian"
                          : "Theo ca"}
                      </span>
                    </div>

                    <div>
                      <span className="block text-slate-400 font-semibold">Lương cơ bản:</span>
                      <span className="font-extrabold text-red-600 block mt-1">
                        {new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(baseSalary)}
                        <span className="text-xs text-slate-400 font-semibold"> / {salaryUnit.replace("Theo ", "")}</span>
                      </span>
                    </div>

                    <div>
                      <span className="block text-slate-400 font-semibold">Chu kỳ tính lương:</span>
                      <span className="font-extrabold text-slate-900 block mt-1">{salaryUnit}</span>
                    </div>

                    {employmentType === "Full-time" && (
                      <div className="sm:col-span-3 grid grid-cols-1 sm:grid-cols-2 gap-4 border-t border-slate-100 pt-4">
                        <div className="flex justify-between items-center bg-teal-50/40 rounded-xl p-3 border border-teal-100/40">
                          <span className="text-slate-500 font-bold">Phép tích lũy thêm hàng tháng:</span>
                          <span className="font-extrabold text-teal-800 text-[14.5px]">{leaveAccrued} ngày</span>
                        </div>
                        <div className="flex justify-between items-center bg-teal-50/40 rounded-xl p-3 border border-teal-100/40">
                          <span className="text-slate-550 font-bold">Phép tối đa được nghỉ / tháng:</span>
                          <span className="font-extrabold text-teal-800 text-[14.5px]">{leaveMax} ngày</span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* ── Right 1/3 ── */}
              <div className="lg:col-span-1 flex flex-col gap-5">

                {/* Tài khoản hệ thống */}
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-5">
                  <div className="flex items-center gap-2 mb-3.5">
                    <div className="w-1 h-4 bg-purple-400 rounded-full" />
                    <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Tài khoản hệ thống</span>
                  </div>
                  <div className="flex items-center justify-between py-2 border-b border-slate-50">
                    <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider">Trạng thái</span>
                    <span className={`text-[11px] font-black px-2 py-0.5 rounded-full border ${staff.hasAccount ? (staff.isActive ? "bg-green-50 text-green-700 border-green-200" : "bg-red-50 text-red-600 border-red-200") : "bg-amber-50 text-amber-700 border-amber-200"}`}>
                      {!staff.hasAccount ? "Chưa có TK" : staff.isActive ? "Kích hoạt" : "Bị khóa"}
                    </span>
                  </div>
                  {staff.hasAccount && (
                    <div className="flex items-center justify-between py-2 border-b border-slate-50">
                      <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider">Username</span>
                      <span className="text-[12px] font-mono font-bold text-slate-700">{staff.username || "—"}</span>
                    </div>
                  )}
                  <div className="flex items-center justify-between py-2">
                    <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider">Tạo hồ sơ</span>
                    <span className="text-[12px] font-semibold text-slate-600">{fmt(staff.createdAt?.split("T")[0])}</span>
                  </div>
                </div>

                {/* ── Compact Monthly Calendar ── */}
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-5">
                  {/* Header */}
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-1.5">
                      <div className="w-1 h-3.5 bg-primary rounded-full" />
                      <span className="text-[10.5px] font-black text-slate-400 uppercase tracking-widest">Lịch làm việc</span>
                    </div>
                    <div className="flex items-center gap-1">
                      <button onClick={() => navigateMonth(-1)} className="p-1 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                        </svg>
                      </button>
                      <span className="text-[12px] font-extrabold text-slate-700 w-28 text-center">
                        {MONTH_NAMES[viewMonth]}, {viewYear}
                      </span>
                      <button onClick={() => navigateMonth(1)} className="p-1 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                        </svg>
                      </button>
                    </div>
                  </div>

                  {(viewYear !== today.getFullYear() || viewMonth !== today.getMonth()) && (
                    <button
                      onClick={() => { setViewYear(today.getFullYear()); setViewMonth(today.getMonth()); }}
                      className="w-full mb-2.5 py-1 text-[11px] font-black text-primary bg-red-50 hover:bg-red-100 border border-red-100 rounded-lg transition-all cursor-pointer"
                    >
                      Về tháng hiện tại
                    </button>
                  )}

                  {/* Day headers */}
                  <div className="grid grid-cols-7 gap-1 mb-1">
                    {DAY_HEADERS.map(d => (
                      <div key={d} className="text-center text-[9.5px] font-extrabold text-slate-400 uppercase tracking-wider py-1">{d}</div>
                    ))}
                  </div>

                  {/* Grid */}
                  {isLoadingSchedule ? (
                    <div className="py-8 text-center text-[12px] text-slate-400 font-semibold">Đang tải...</div>
                  ) : (
                      <div className="grid grid-cols-7 gap-1 text-[12px] font-bold text-slate-800">
                      {calDays.map(({ date, inMonth }) => {
                        const entries      = staffSchedule.filter(e => e.date === date);
                        const hasMorning   = entries.some(e => periodOfShift(e.shift) === "Buổi sáng");
                        const hasAfternoon = entries.some(e => periodOfShift(e.shift) === "Buổi chiều");
                        const hasEvening   = entries.some(e => periodOfShift(e.shift) === "Buổi tối");
                        const isToday      = date === todayStr;
                        const dayNum       = parseInt(date.split("-")[2]);
                        return (
                          <div
                            key={date}
                            className={`h-9 rounded-lg flex flex-col items-center justify-center relative select-none border transition-all ${
                              !inMonth
                                ? "bg-slate-50 border-transparent text-slate-300 opacity-40"
                                : isToday
                                ? "bg-red-50/20 text-primary border-primary border-2 shadow-sm font-extrabold"
                                : "bg-white border-slate-100 hover:border-slate-200 cursor-pointer text-slate-800"
                            }`}
                          >
                            <span>{dayNum}</span>
                            {inMonth && (
                              <div className="absolute bottom-1 flex gap-0.5 justify-center">
                                {hasMorning && <span className="w-1.5 h-1.5 rounded-full bg-green-500" />}
                                {hasAfternoon && <span className="w-1.5 h-1.5 rounded-full bg-sky-500" />}
                                {hasEvening && <span className="w-1.5 h-1.5 rounded-full bg-violet-500" />}
                                {!hasMorning && !hasAfternoon && !hasEvening && <span className="w-1.5 h-1.5 rounded-full bg-slate-300" />}
                              </div>
                            )}
                          </div>
                        );
                      })}
                    </div>
                  )}

                  {/* Legend */}
                  <div className="flex items-center gap-3 mt-3 pt-3 border-t border-slate-50 flex-wrap">
                    <div className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-green-500 inline-block" />
                      <span className="text-[10.5px] font-bold text-slate-400">Ca sáng</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-sky-500 inline-block" />
                      <span className="text-[10.5px] font-bold text-slate-400">Ca chiều</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-slate-300 inline-block" />
                      <span className="text-[10.5px] font-bold text-slate-400">Nghỉ</span>
                    </div>
                    <div className="flex items-center gap-1.5 ml-auto">
                      <span className="w-4 h-4 rounded bg-red-50/20 border-primary border-2 flex items-center justify-center text-[9px] text-primary font-extrabold shadow-sm">
                        {today.getDate()}
                      </span>
                      <span className="text-[10.5px] font-bold text-slate-400">Hôm nay</span>
                    </div>
                  </div>

                  {!isLoadingSchedule && totalShifts === 0 && (
                    <p className="text-center text-[11.5px] text-slate-400 font-semibold mt-2">Chưa có lịch trong tháng này.</p>
                  )}
                </div>

                {/* ── Dentist: Reviews ── */}
                {isDentist && (
                  <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-5">
                    {/* Header */}
                    <div className="flex items-center justify-between mb-3">
                      <div className="flex items-center gap-1.5">
                        <div className="w-1 h-3.5 bg-amber-400 rounded-full" />
                        <span className="text-[10.5px] font-black text-slate-400 uppercase tracking-widest">Đánh giá bệnh nhân</span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Stars rating={MOCK_RATING} size={13} />
                        <span className="text-[13px] font-black text-amber-500">{MOCK_RATING}</span>
                      </div>
                    </div>

                    {/* Star filter buttons */}
                    <div className="flex flex-wrap gap-1.5 mb-3">
                      <button
                        onClick={() => setStarFilter(null)}
                        className={`flex items-center gap-1 px-2.5 py-1 rounded-lg text-[11px] font-black border transition-all cursor-pointer ${starFilter === null ? "bg-slate-800 text-white border-slate-800" : "bg-white text-slate-500 border-slate-200 hover:border-slate-300"}`}
                      >
                        Tất cả
                        <span className={`px-1.5 py-0.5 rounded text-[10px] font-black ${starFilter === null ? "bg-white/20 text-white" : "bg-slate-100 text-slate-500"}`}>
                          {MOCK_REVIEWS.length}
                        </span>
                      </button>
                      {starCounts.map(({ star, count }) => (
                        <button
                          key={star}
                          onClick={() => count > 0 && setStarFilter(starFilter === star ? null : star)}
                          disabled={count === 0}
                          className={`flex items-center gap-1 px-2.5 py-1 rounded-lg text-[11px] font-black border transition-all ${count === 0 ? "opacity-40 cursor-not-allowed bg-white text-slate-400 border-slate-100" : "cursor-pointer"} ${starFilter === star ? "bg-amber-500 text-white border-amber-500" : count > 0 ? "bg-white text-slate-600 border-slate-200 hover:border-amber-300 hover:text-amber-600" : ""}`}
                        >
                          <svg className="w-3 h-3" viewBox="0 0 24 24" fill={starFilter === star ? "white" : "#f59e0b"}>
                            <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                          </svg>
                          {star}
                          <span className={`px-1.5 py-0.5 rounded text-[10px] font-black ${starFilter === star ? "bg-white/20 text-white" : "bg-slate-100 text-slate-500"}`}>
                            {count}
                          </span>
                        </button>
                      ))}
                    </div>

                    {/* Review list */}
                    <div className="flex flex-col divide-y divide-slate-50 max-h-80 overflow-y-auto pr-1">
                      {filteredReviews.length === 0 ? (
                        <p className="py-6 text-center text-[12px] text-slate-400 font-semibold">Không có đánh giá nào.</p>
                      ) : (
                        filteredReviews.map(review => (
                          <div key={review.id} className="py-3 flex flex-col gap-2">
                            <div className="flex items-center justify-between gap-2">
                              <div className="flex items-center gap-2 min-w-0">
                                <div className="w-7 h-7 rounded-full bg-gradient-to-br from-slate-200 to-slate-100 flex items-center justify-center shrink-0">
                                  <span className="text-[10px] font-black text-slate-500">{review.patient.slice(0, 2).toUpperCase()}</span>
                                </div>
                                <span className="text-[12px] font-extrabold text-slate-700 truncate">{review.patient}</span>
                              </div>
                              <div className="flex items-center gap-1.5 shrink-0">
                                <Stars rating={review.rating} size={11} />
                                <span className="text-[10.5px] text-slate-400 font-semibold">{review.date}</span>
                              </div>
                            </div>
                            <p className="text-[12.5px] text-slate-600 font-semibold leading-relaxed line-clamp-2">{review.comment}</p>
                          </div>
                        ))
                      )}
                    </div>
                  </div>
                )}

              </div>
            </div>

          </div>
        </div>
      </main>
    </div>
  );
}
