"use client";

import React from "react";
import { type StaffDto } from "../../../../lib/apiClient";

const ROLE_LABELS: Record<string, string> = {
  Admin: "Quản trị viên", Doctor: "Bác sĩ chuyên khoa",
  Dentist: "Nha sĩ", Staff: "Lễ tân / Trợ lý",
};
const STATUS_LABELS: Record<string, string> = {
  Active: "Đang làm việc", "On Leave": "Nghỉ phép", Inactive: "Đã nghỉ việc",
};
const ROLE_BADGES: Record<string, string> = {
  Admin: "bg-purple-50 text-purple-700 border-purple-200",
  Doctor: "bg-emerald-50 text-emerald-700 border-emerald-200",
  Dentist: "bg-sky-50 text-sky-700 border-sky-200",
  Staff: "bg-green-50 text-green-700 border-green-200",
};
const STATUS_BADGES: Record<string, string> = {
  Active: "bg-green-50 text-green-700 border-green-200",
  "On Leave": "bg-amber-50 text-amber-700 border-amber-200",
  Inactive: "bg-red-50 text-red-700 border-red-200",
};

const isDoctorRole = (role: string) => role === "Doctor" || role === "Dentist";

interface Props {
  isOpen: boolean;
  onClose: () => void;
  staff: StaffDto | null;
}

function InfoRow({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{label}</span>
      <span className="text-[13.5px] font-bold text-slate-800">{value ?? "—"}</span>
    </div>
  );
}

export default function StaffDetailModal({ isOpen, onClose, staff }: Props) {
  if (!isOpen || !staff) return null;

  const initials = staff.fullName
    ? staff.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
    : staff.username.slice(0, 2).toUpperCase();

  const isDoctor = isDoctorRole(staff.role);
  const status = staff.employmentStatus || "Active";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto animate-fade-in">
      <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-lg shadow-2xl my-8 flex flex-col overflow-hidden">

        {/* Top band */}
        <div className={`px-6 pt-6 pb-5 ${isDoctor ? "bg-sky-50" : "bg-red-50/40"}`}>
          <div className="flex items-start justify-between mb-4">
            <span className={`text-[11.5px] font-black uppercase tracking-widest ${isDoctor ? "text-secondary" : "text-primary"}`}>
              {isDoctor ? "Hồ sơ bác sĩ" : "Hồ sơ nhân viên"}
            </span>
            <button onClick={onClose} className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-white/60 transition-all cursor-pointer">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div className="flex items-center gap-4">
            {staff.profilePictureUrl ? (
              <img src={staff.profilePictureUrl} alt={staff.fullName || staff.username}
                className="w-16 h-16 rounded-2xl object-cover border-2 border-white shadow-md shrink-0" />
            ) : (
              <div className={`w-16 h-16 rounded-2xl flex items-center justify-center font-black text-xl shadow-inner shrink-0 ${isDoctor ? "bg-sky-100 text-sky-700" : "bg-red-100 text-primary"}`}>
                {initials}
              </div>
            )}
            <div className="min-w-0">
              <h3 className="text-[18px] font-black text-slate-900 leading-tight truncate">
                {staff.fullName || staff.username}
              </h3>
              <div className="flex flex-wrap items-center gap-2 mt-1.5">
                <span className={`px-2.5 py-0.5 rounded-full text-[11px] font-black border ${ROLE_BADGES[staff.role] || "bg-slate-50 border-slate-200 text-slate-600"}`}>
                  {ROLE_LABELS[staff.role] || staff.role}
                </span>
                <span className={`px-2.5 py-0.5 rounded-full text-[11px] font-black border ${STATUS_BADGES[status]}`}>
                  {STATUS_LABELS[status]}
                </span>
                {staff.hasAccount ? (
                  <span className="px-2.5 py-0.5 rounded-full text-[11px] font-black border bg-green-50 text-green-700 border-green-200 flex items-center gap-1">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    Đã có tài khoản
                  </span>
                ) : (
                  <span className="px-2.5 py-0.5 rounded-full text-[11px] font-black border bg-amber-50 text-amber-700 border-amber-200 flex items-center gap-1">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                    </svg>
                    Chưa có tài khoản
                  </span>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Body */}
        <div className="px-6 py-5 flex flex-col gap-5 overflow-y-auto max-h-[420px]">

          {/* Contact */}
          <div>
            <div className="flex items-center gap-2 mb-3">
              <div className="w-1 h-4 bg-primary rounded-full" />
              <span className="text-[11px] font-black text-slate-500 uppercase tracking-widest">Thông tin liên hệ</span>
            </div>
            <div className="grid grid-cols-2 gap-x-6 gap-y-3">
              <InfoRow label="Email" value={staff.email} />
              <InfoRow label="Số điện thoại" value={staff.phoneNumber} />
            </div>
          </div>

          {/* Doctor-specific fields */}
          {isDoctor ? (
            <div>
              <div className="flex items-center gap-2 mb-3">
                <div className="w-1 h-4 bg-secondary rounded-full" />
                <span className="text-[11px] font-black text-slate-500 uppercase tracking-widest">Thông tin chuyên môn</span>
              </div>
              <div className="grid grid-cols-2 gap-x-6 gap-y-3">
                <InfoRow label="Chuyên khoa" value={staff.specialty} />
                <InfoRow label="Số GPHNN" value={staff.licenseNumber} />
                <InfoRow label="Kinh nghiệm" value={staff.yearsOfExperience != null ? `${staff.yearsOfExperience} năm` : null} />
                <InfoRow label="Ngày tạo hồ sơ" value={new Date(staff.createdAt).toLocaleDateString("vi-VN")} />
              </div>
            </div>
          ) : (
            <div>
              <div className="flex items-center gap-2 mb-3">
                <div className="w-1 h-4 bg-green-500 rounded-full" />
                <span className="text-[11px] font-black text-slate-500 uppercase tracking-widest">Thông tin công việc</span>
              </div>
              <div className="grid grid-cols-2 gap-x-6 gap-y-3">
                <InfoRow label="Mã nhân viên" value={staff.employeeId} />
                <InfoRow label="Phòng ban" value={staff.department} />
                <InfoRow label="Ngày tạo hồ sơ" value={new Date(staff.createdAt).toLocaleDateString("vi-VN")} />
              </div>
            </div>
          )}

          {/* Account info */}
          <div className="bg-slate-50 rounded-xl p-3.5 border border-slate-100 flex items-center gap-3">
            <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${staff.hasAccount ? "bg-green-100 text-green-700" : "bg-amber-100 text-amber-600"}`}>
              {staff.hasAccount ? (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z" />
                </svg>
              ) : (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
              )}
            </div>
            <div className="min-w-0">
              {staff.hasAccount ? (
                <>
                  <div className="text-[13px] font-black text-slate-800">Đã có tài khoản đăng nhập</div>
                  <div className="text-[11.5px] text-slate-400 font-semibold mt-0.5">Username: <span className="font-black text-slate-600 font-mono">{staff.username}</span></div>
                </>
              ) : (
                <>
                  <div className="text-[13px] font-black text-amber-700">Chưa có tài khoản đăng nhập</div>
                  <div className="text-[11.5px] text-amber-600 font-semibold mt-0.5">Tạo tài khoản tại trang Tài khoản & Phân quyền.</div>
                </>
              )}
            </div>
          </div>

          {/* Professional notes */}
          {staff.professionalNotes && (
            <div>
              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-1.5">Ghi chú</div>
              <p className="text-[13px] text-slate-700 font-semibold leading-relaxed bg-slate-50 rounded-xl p-3.5 border border-slate-100">
                {staff.professionalNotes}
              </p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-100 flex justify-end">
          <button onClick={onClose} className="px-5 py-2.5 text-[14px] font-bold text-slate-600 hover:text-slate-900 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer">
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}
