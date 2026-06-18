"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";

type PatientStatus = "waiting" | "in_progress" | "done";

interface Patient {
  id: string; name: string; age: number; gender: "Nam" | "Nữ";
  phone: string; time: string; shift: "morning" | "afternoon";
  reason: string; status: PatientStatus; isNew: boolean;
}

const MOCK_PATIENTS: Patient[] = [
  { id: "P001", name: "Nguyễn Văn An",   age: 34, gender: "Nam", phone: "0912 345 678", time: "08:00", shift: "morning",   reason: "Nhổ răng khôn hàm dưới",              status: "done",        isNew: false },
  { id: "P002", name: "Trần Thị Bích",   age: 28, gender: "Nữ",  phone: "0908 765 432", time: "08:30", shift: "morning",   reason: "Trám răng số 6",                       status: "in_progress", isNew: false },
  { id: "P003", name: "Phạm Minh Cường", age: 45, gender: "Nam", phone: "0934 111 222", time: "09:00", shift: "morning",   reason: "Kiểm tra định kỳ, lấy cao răng",       status: "waiting",     isNew: false },
  { id: "P004", name: "Lê Thu Hà",       age: 22, gender: "Nữ",  phone: "0977 333 444", time: "09:30", shift: "morning",   reason: "Tẩy trắng răng Zoom Advanced",         status: "waiting",     isNew: true  },
  { id: "P005", name: "Hoàng Văn Đức",   age: 58, gender: "Nam", phone: "0901 555 666", time: "10:00", shift: "morning",   reason: "Cấy ghép Implant răng số 4 hàm trên",  status: "waiting",     isNew: false },
  { id: "P006", name: "Nguyễn Thị Lan",  age: 31, gender: "Nữ",  phone: "0945 777 888", time: "10:30", shift: "morning",   reason: "Tái khám sau nhổ răng",                status: "waiting",     isNew: false },
  { id: "P007", name: "Võ Minh Tuấn",    age: 19, gender: "Nam", phone: "0968 999 000", time: "14:00", shift: "afternoon", reason: "Niềng răng mắc cài kim loại",          status: "waiting",     isNew: true  },
  { id: "P008", name: "Đinh Thị Nga",    age: 42, gender: "Nữ",  phone: "0912 111 999", time: "14:30", shift: "afternoon", reason: "Bọc sứ răng cửa",                      status: "waiting",     isNew: false },
];

const STATUS_CFG: Record<PatientStatus, { label: string; bar: string; badge: string; dot: string }> = {
  waiting:     { label: "Đang chờ",   bar: "bg-amber-400",  badge: "bg-amber-50 text-amber-700 border border-amber-200",   dot: "bg-amber-500"  },
  in_progress: { label: "Đang khám",  bar: "bg-sky-400",    badge: "bg-sky-50 text-sky-700 border border-sky-200",         dot: "bg-sky-500"    },
  done:        { label: "Hoàn thành", bar: "bg-emerald-400",badge: "bg-emerald-50 text-emerald-700 border border-emerald-200", dot: "bg-emerald-500" },
};

function PatientRow({ p, idx }: { p: Patient; idx: number }) {
  const s = STATUS_CFG[p.status];
  const isActive = p.status === "in_progress";
  const isDone   = p.status === "done";
  const initials = p.name.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase();

  return (
    <div className={`flex rounded-2xl border overflow-hidden transition-all hover:shadow-md ${
      isActive ? "bg-white border-sky-200 shadow-sm shadow-sky-100/40" : "bg-white border-slate-200/70 hover:-translate-y-px"
    }`}>
      {/* Status accent bar */}
      <div className={`w-1.5 shrink-0 ${s.bar}`} />

      <div className="flex items-center gap-5 px-5 py-4 flex-1 min-w-0">

        {/* Time + order */}
        <div className="flex flex-col items-center w-14 shrink-0">
          <span className="text-[19px] font-black text-slate-900 font-mono leading-none tabular-nums">{p.time}</span>
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
            <span className={`text-[15px] font-black leading-tight ${isDone ? "text-slate-400" : "text-slate-900"}`}>{p.name}</span>
            {p.isNew && (
              <span className="px-1.5 py-0.5 bg-violet-100 text-violet-700 text-[10px] font-black rounded-md tracking-wide">MỚI</span>
            )}
            <span className="text-[12px] text-slate-400 font-semibold">{p.age} tuổi · {p.gender}</span>
          </div>
          <div className="flex items-center gap-1.5 mt-1">
            <svg className="w-3.5 h-3.5 text-slate-300 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
            </svg>
            <span className={`text-[13.5px] font-semibold truncate ${isDone ? "text-slate-400" : "text-slate-700"}`}>{p.reason}</span>
          </div>
          <div className="text-[12px] text-slate-400 font-medium mt-0.5 font-mono">{p.phone}</div>
        </div>

        {/* Status badge */}
        <div className="shrink-0 flex flex-col items-end gap-2">
          <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[12px] font-black whitespace-nowrap ${s.badge}`}>
            <span className={`w-1.5 h-1.5 rounded-full ${s.dot} ${isActive ? "animate-pulse" : ""}`} />
            {s.label}
          </span>
          <span className={`text-[11px] font-bold px-2 py-0.5 rounded-lg ${p.shift === "morning" ? "bg-red-50 text-primary" : "bg-indigo-50 text-indigo-600"}`}>
            {p.shift === "morning" ? "Ca sáng" : "Ca chiều"}
          </span>
        </div>

        {/* Action button */}
        <Link
          href={`/dentist/patients/${p.id}`}
          className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-[13px] font-bold transition-all shrink-0 ${
            isActive
              ? "bg-primary text-white hover:bg-red-600 shadow-sm shadow-primary/25"
              : isDone
                ? "bg-slate-100 text-slate-500 hover:bg-slate-200"
                : "bg-red-50 text-primary border border-primary/20 hover:bg-primary hover:text-white"
          }`}
        >
          {isActive ? "Tiếp tục khám" : isDone ? "Xem hồ sơ" : "Bắt đầu khám"}
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
          </svg>
        </Link>
      </div>
    </div>
  );
}

function ShiftSection({ label, icon, patients, emptyMsg }: {
  label: string; icon: string; patients: Patient[]; emptyMsg: string;
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
        {patients.map((p, idx) => <PatientRow key={p.id} p={p} idx={idx} />)}
      </div>
    </div>
  );
}

export default function DentistPatientsPage() {
  useRequireDentist();
  const [search, setSearch]           = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [shiftFilter, setShiftFilter]   = useState("all");

  const filtered = useMemo(() => MOCK_PATIENTS.filter((p) => {
    const q = search.toLowerCase();
    const matchSearch = q === "" || p.name.toLowerCase().includes(q) || p.reason.toLowerCase().includes(q) || p.phone.includes(q);
    const matchStatus = statusFilter === "all" || p.status === statusFilter;
    const matchShift  = shiftFilter  === "all" || p.shift  === shiftFilter;
    return matchSearch && matchStatus && matchShift;
  }), [search, statusFilter, shiftFilter]);

  const total    = MOCK_PATIENTS.length;
  const done     = MOCK_PATIENTS.filter((p) => p.status === "done").length;
  const active   = MOCK_PATIENTS.filter((p) => p.status === "in_progress").length;
  const waiting  = MOCK_PATIENTS.filter((p) => p.status === "waiting").length;
  const progress = Math.round((done / total) * 100);

  const morning   = filtered.filter((p) => p.shift === "morning");
  const afternoon = filtered.filter((p) => p.shift === "afternoon");

  const selectCls = "px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 appearance-none cursor-pointer pr-8";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Bệnh Nhân Hôm Nay"
          subtitle="Thứ Sáu, 12 tháng 6 năm 2026"
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">{waiting} chờ</span>
              <span className="px-2.5 py-1.5 bg-sky-50 text-sky-700 border border-sky-200 rounded-xl">{active} đang khám</span>
              <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">{done} xong</span>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">

          {/* Progress bar */}
          <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm px-6 py-4 flex items-center gap-5">
            <div className="flex flex-col gap-1 shrink-0">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-widest">Tiến độ ca làm</span>
              <span className="text-[24px] font-black text-slate-900 leading-none">{done}<span className="text-[14px] text-slate-400 font-bold">/{total}</span></span>
            </div>
            <div className="flex-1 flex flex-col gap-1.5">
              <div className="h-2.5 bg-slate-100 rounded-full overflow-hidden">
                <div className="h-full bg-gradient-to-r from-primary to-red-400 rounded-full transition-all duration-500" style={{ width: `${progress}%` }} />
              </div>
              <div className="flex items-center gap-4 text-[11.5px] font-semibold">
                <span className="flex items-center gap-1 text-emerald-600"><span className="w-2 h-2 rounded-full bg-emerald-400 inline-block" />{done} hoàn thành</span>
                <span className="flex items-center gap-1 text-sky-600"><span className="w-2 h-2 rounded-full bg-sky-400 animate-pulse inline-block" />{active} đang khám</span>
                <span className="flex items-center gap-1 text-amber-600"><span className="w-2 h-2 rounded-full bg-amber-400 inline-block" />{waiting} đang chờ</span>
              </div>
            </div>
            <div className="text-right shrink-0">
              <span className="text-[28px] font-black text-slate-800 leading-none">{progress}%</span>
              <div className="text-[11px] font-bold text-slate-400 mt-0.5">hoàn thành</div>
            </div>
          </div>

          {/* Search + Filter */}
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
              <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className={selectCls}>
                <option value="all">Tất cả trạng thái</option>
                <option value="waiting">Đang chờ</option>
                <option value="in_progress">Đang khám</option>
                <option value="done">Hoàn thành</option>
              </select>
              <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
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

          {/* Patient sections */}
          {filtered.length > 0 ? (
            <div className="flex flex-col gap-7">
              <ShiftSection
                label="Ca sáng"
                icon="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z"
                patients={morning}
                emptyMsg="Không có bệnh nhân ca sáng"
              />
              <ShiftSection
                label="Ca chiều"
                icon="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z"
                patients={afternoon}
                emptyMsg="Không có bệnh nhân ca chiều"
              />
            </div>
          ) : (
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
