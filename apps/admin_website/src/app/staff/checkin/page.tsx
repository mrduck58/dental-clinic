"use client";

import { useState } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";

const PENDING = [
  { id: "A001", name: "Nguyễn Văn An",   phone: "0901 234 567", service: "Nhổ răng khôn",        time: "08:30", dentist: "BS. Thảo", room: "Phòng 1", gender: "Nam" as const, dob: "15/03/1990", note: ""                  },
  { id: "A004", name: "Lê Thu Hà",       phone: "0934 567 890", service: "Tẩy trắng răng Zoom",  time: "10:00", dentist: "BS. Linh", room: "Phòng 3", gender: "Nữ"  as const, dob: "22/07/1995", note: ""                  },
  { id: "A005", name: "Hoàng Văn Đức",   phone: "0945 678 901", service: "Cấy ghép Implant",     time: "10:30", dentist: "BS. Minh", room: "Phòng 2", gender: "Nam" as const, dob: "08/11/1985", note: "Dị ứng penicillin"  },
  { id: "A007", name: "Đỗ Quang Huy",    phone: "0967 890 123", service: "Lấy cao răng",         time: "13:30", dentist: "BS. Thảo", room: "Phòng 1", gender: "Nam" as const, dob: "30/01/2000", note: ""                  },
  { id: "A008", name: "Nguyễn Thị Mai",  phone: "0978 901 234", service: "Chỉnh nha Invisalign", time: "14:00", dentist: "BS. Minh", room: "Phòng 2", gender: "Nữ"  as const, dob: "12/09/1998", note: "Lần khám đầu tiên" },
];

export default function CheckinPage() {
  useRequireStaff();

  const [search,    setSearch]    = useState("");
  const [selected,  setSelected]  = useState<string | null>(null);
  const [checkedIn, setCheckedIn] = useState<Set<string>>(new Set());
  const [confirmAt, setConfirmAt] = useState<Record<string, string>>({});

  const waiting  = PENDING.filter(p => !checkedIn.has(p.id));
  const filtered = waiting.filter(p =>
    p.name.toLowerCase().includes(search.toLowerCase()) || p.phone.includes(search)
  );
  const patient = PENDING.find(p => p.id === selected);

  const doCheckin = (id: string) => {
    const now = new Date();
    const t = now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit", hour12: false });
    setCheckedIn(prev => new Set([...prev, id]));
    setConfirmAt(prev => ({ ...prev, [id]: t }));
    setSelected(null);
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="checkin" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader
          title="Check-in Bệnh Nhân"
          subtitle="Xác nhận bệnh nhân đến khám"
          right={
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">{waiting.length} chờ</span>
              <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">{checkedIn.size} đã check-in</span>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto">
          <div className="flex gap-6">

            {/* Left: search + list */}
            <div className="w-96 flex flex-col gap-4 shrink-0">
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

              {/* Waiting list */}
              {filtered.length > 0 ? (
                <div className="flex flex-col gap-3">
                  <div className="flex items-center gap-3">
                    <span className="text-[13px] font-black text-slate-600 uppercase tracking-wider">Đang chờ check-in</span>
                    <span className="text-[12px] font-bold text-slate-400">{filtered.length} bệnh nhân</span>
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>
                  <div className="flex flex-col gap-2.5">
                    {filtered.map((p, idx) => {
                      const initials = p.name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
                      const isActive = selected === p.id;
                      return (
                        <button key={p.id} onClick={() => setSelected(p.id)}
                          className={`flex rounded-2xl border overflow-hidden w-full text-left transition-all hover:shadow-md ${
                            isActive ? "bg-white border-primary shadow-md shadow-primary/10" : "bg-white border-slate-200/70 hover:-translate-y-px"
                          }`}>
                          {/* Status bar */}
                          <div className={`w-1.5 shrink-0 ${isActive ? "bg-primary" : "bg-amber-400"}`} />
                          <div className="flex items-center gap-4 px-4 py-3.5 flex-1 min-w-0">
                            <div className="flex flex-col items-center w-12 shrink-0">
                              <span className="text-[17px] font-black text-slate-900 font-mono leading-none">{p.time}</span>
                              <span className="text-[11px] font-bold text-slate-400 mt-1">#{idx + 1}</span>
                            </div>
                            <div className="w-px h-10 bg-slate-100 shrink-0" />
                            <div className={`w-10 h-10 rounded-xl flex items-center justify-center font-black text-[12px] shrink-0 ${
                              p.gender === "Nữ" ? "bg-rose-50 text-rose-600 border border-rose-100" : "bg-sky-50 text-sky-700 border border-sky-100"
                            }`}>
                              {initials}
                            </div>
                            <div className="flex-1 min-w-0">
                              <div className="text-[14px] font-black text-slate-900 truncate">{p.name}</div>
                              <div className="text-[12px] text-slate-500 font-semibold truncate">{p.service}</div>
                              <div className="text-[11.5px] text-slate-400 font-medium font-mono">{p.phone}</div>
                            </div>
                          </div>
                        </button>
                      );
                    })}
                  </div>
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
                      const p = PENDING.find(x => x.id === id);
                      if (!p) return null;
                      return (
                        <div key={id} className="flex rounded-2xl border border-emerald-100 bg-emerald-50/60 overflow-hidden">
                          <div className="w-1.5 shrink-0 bg-emerald-400" />
                          <div className="flex items-center gap-3 px-4 py-3 flex-1 min-w-0">
                            <div className="w-7 h-7 rounded-full bg-emerald-100 flex items-center justify-center shrink-0">
                              <svg className="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                            </div>
                            <div className="flex-1 min-w-0">
                              <div className="text-[13px] font-bold text-emerald-900 truncate">{p.name}</div>
                              <div className="text-[11.5px] text-emerald-600 font-semibold">Check-in lúc {confirmAt[id]}</div>
                            </div>
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
                    <div className={`w-16 h-16 rounded-2xl border-2 flex items-center justify-center font-black text-2xl shrink-0 ${
                      patient.gender === "Nữ" ? "bg-rose-50 border-rose-100 text-rose-600" : "bg-sky-50 border-sky-100 text-sky-700"
                    }`}>
                      {patient.name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                    </div>
                    <div>
                      <h2 className="text-[20px] font-black text-slate-900">{patient.name}</h2>
                      <div className="flex items-center gap-3 mt-1 text-[13px] text-slate-500 font-semibold flex-wrap">
                        <span>{patient.phone}</span>
                        <span>·</span>
                        <span>{patient.gender}</span>
                        <span>·</span>
                        <span>Sinh ngày {patient.dob}</span>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    {[
                      { label: "Giờ hẹn",  value: patient.time,    icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"          },
                      { label: "Dịch vụ",  value: patient.service, icon: "M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" },
                      { label: "Bác sĩ",   value: patient.dentist, icon: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" },
                      { label: "Phòng",    value: patient.room,    icon: "M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75" },
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

                  {patient.note && (
                    <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-100 rounded-xl">
                      <svg className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                      <div>
                        <div className="text-[11.5px] font-extrabold text-amber-700 uppercase tracking-wider">Lưu ý</div>
                        <div className="text-[13.5px] font-semibold text-amber-800 mt-0.5">{patient.note}</div>
                      </div>
                    </div>
                  )}

                  <button onClick={() => doCheckin(patient.id)}
                    className="flex items-center justify-center gap-2 w-full py-4 bg-emerald-500 hover:bg-emerald-600 text-white rounded-xl text-[15px] font-black shadow-sm shadow-emerald-200 transition-all cursor-pointer">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
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
