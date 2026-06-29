"use client";

import React, { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import { createAccountApi, createStaffAccountApi } from "../../../../lib/apiClient";

interface Prefill {
  staffId: string;
  fullName: string;
  email: string;
  role: string;
  phoneNumber: string;
  profilePictureUrl?: string | null;
}

const ROLE_LABELS: Record<string, string> = {
  Admin: "Quản trị viên", Dentist: "Nha sĩ / Bác sĩ CK", Staff: "Lễ tân / Trợ lý", Owner: "Chủ phòng khám",
};
const ROLE_BADGES: Record<string, string> = {
  Admin: "bg-purple-50 text-purple-700 border-purple-200",
  Dentist: "bg-sky-50 text-sky-700 border-sky-200",
  Staff: "bg-green-50 text-green-700 border-green-200",
  Owner: "bg-amber-50 text-amber-700 border-amber-200",
};

export default function CreateAccountPage() {
  useRequireAdmin();
  const router = useRouter();

  const [prefill, setPrefill] = useState<Prefill | null>(null);

  // New-account form
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [role, setRole] = useState("Staff");

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const raw = sessionStorage.getItem("createAccountPrefill");
    if (raw) {
      const data: Prefill = JSON.parse(raw);
      setPrefill(data);
      sessionStorage.removeItem("createAccountPrefill");
    }
  }, []);

  const validateNew = () => {
    const e: Record<string, string> = {};
    if (!fullName.trim()) e.fullName = "Họ và tên không được để trống";
    if (!email.trim()) {
      e.email = "Email không được để trống";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      e.email = "Email không đúng định dạng";
    }
    if (!phoneNumber.trim()) {
      e.phoneNumber = "Số điện thoại không được để trống";
    } else if (!/^\d{9,11}$/.test(phoneNumber.replace(/\s+/g, ""))) {
      e.phoneNumber = "Số điện thoại phải chứa từ 9 đến 11 chữ số";
    }
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!prefill && !validateNew()) return;

    setIsSubmitting(true);
    try {
      if (prefill) {
        await createStaffAccountApi(prefill.staffId);
      } else {
        await createAccountApi({ fullName: fullName.trim(), email: email.trim(), phoneNumber: phoneNumber.trim(), role });
      }
      sessionStorage.setItem("permSuccessMsg", `Tài khoản cho ${prefill?.fullName || fullName} đã được tạo và gửi qua email.`);
      router.push("/admin/permissions");
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Đã xảy ra lỗi không xác định" });
      setIsSubmitting(false);
    }
  };

  const inp = (field: string) =>
    `w-full px-4 py-3 text-[14px] bg-white border rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold ${errors[field] ? "border-red-400 bg-red-50/30" : "border-slate-200"
    }`;
  const lbl = "text-[12px] font-extrabold text-slate-500 uppercase tracking-wider mb-1.5 block";

  const initials = prefill?.fullName
    ? prefill.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
    : null;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="permissions" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <button
              onClick={() => router.back()}
              className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </button>
            <div>
              <h1 className="text-xl font-extrabold text-slate-900 tracking-tight">
                {prefill ? `Tạo tài khoản cho ${prefill.fullName}` : "Thêm tài khoản mới"}
              </h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
                Hệ thống sẽ tạo mật khẩu ngẫu nhiên và gửi qua email.
              </p>
            </div>
          </div>
          <NotificationBell />
        </header>

        {/* Content */}
        <div className="flex-1 p-8 flex justify-center">
          <div className="w-full max-w-2xl">

            {errors.api && (
              <div className="mb-6 bg-red-50 border border-red-200 text-red-700 p-4 rounded-2xl text-[13.5px] font-bold flex items-start gap-2.5">
                <svg className="w-5 h-5 text-red-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
                {errors.api}
              </div>
            )}

            <form onSubmit={handleSubmit} className="flex flex-col gap-6">

              {prefill ? (
                /* ── Staff pre-fill mode ── */
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 flex flex-col gap-5">
                  <div className="flex items-center gap-2 pb-1">
                    <div className="w-1 h-4 bg-primary rounded-full" />
                    <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin nhân viên</span>
                  </div>

                  {/* Avatar + name */}
                  <div className="flex items-center gap-4">
                    {prefill.profilePictureUrl ? (
                      <img src={prefill.profilePictureUrl} alt={prefill.fullName}
                        className="w-16 h-16 rounded-2xl object-cover border border-slate-200 shadow-sm shrink-0" />
                    ) : (
                      <div className="w-16 h-16 rounded-2xl bg-primary/10 text-primary font-black text-xl flex items-center justify-center shrink-0">
                        {initials}
                      </div>
                    )}
                    <div>
                      <div className="text-[17px] font-black text-slate-900">{prefill.fullName}</div>
                      <span className={`mt-1.5 inline-flex px-2.5 py-0.5 rounded-full text-[11px] font-black border ${ROLE_BADGES[prefill.role] || "bg-slate-50 border-slate-200 text-slate-600"}`}>
                        {ROLE_LABELS[prefill.role] || prefill.role}
                      </span>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className={lbl}>Email</label>
                      <div className="w-full px-4 py-3 text-[14px] bg-slate-50 border border-slate-200 rounded-xl font-semibold text-slate-600">
                        {prefill.email}
                      </div>
                    </div>
                    <div>
                      <label className={lbl}>Số điện thoại</label>
                      <div className="w-full px-4 py-3 text-[14px] bg-slate-50 border border-slate-200 rounded-xl font-semibold text-slate-600">
                        {prefill.phoneNumber || "—"}
                      </div>
                    </div>
                  </div>

                  <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 flex items-start gap-3">
                    <svg className="w-5 h-5 text-amber-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z" />
                    </svg>
                    <div>
                      <div className="text-[13px] font-black text-amber-800">Mật khẩu tự động</div>
                      <p className="text-[12px] text-amber-700 font-semibold mt-0.5 leading-relaxed">
                        Hệ thống sẽ tạo mật khẩu ngẫu nhiên và gửi đến{" "}
                        <span className="font-black text-amber-900">{prefill.email}</span>.
                      </p>
                    </div>
                  </div>
                </div>
              ) : (
                /* ── New account mode ── */
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4">
                  <div className="flex items-center gap-2 pb-1">
                    <div className="w-1 h-4 bg-primary rounded-full" />
                    <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin tài khoản</span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="md:col-span-2">
                      <label className={lbl}>Họ và tên <span className="text-red-500">*</span></label>
                      <input type="text" value={fullName} onChange={(e) => { setFullName(e.target.value); if (errors.fullName) setErrors((p) => ({ ...p, fullName: "" })); }}
                        placeholder="Nguyễn Văn A" className={inp("fullName")} />
                      {errors.fullName && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.fullName}</p>}
                    </div>

                    <div>
                      <label className={lbl}>Email <span className="text-red-500">*</span></label>
                      <input type="email" value={email} onChange={(e) => { setEmail(e.target.value); if (errors.email) setErrors((p) => ({ ...p, email: "" })); }}
                        placeholder="nhanvien@dentalclinic.com" className={inp("email")} />
                      {errors.email && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.email}</p>}
                    </div>

                    <div>
                      <label className={lbl}>Số điện thoại <span className="text-red-500">*</span></label>
                      <input type="text" value={phoneNumber} onChange={(e) => { setPhoneNumber(e.target.value); if (errors.phoneNumber) setErrors((p) => ({ ...p, phoneNumber: "" })); }}
                        placeholder="0987654321" className={inp("phoneNumber")} />
                      {errors.phoneNumber && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.phoneNumber}</p>}
                    </div>

                    <div>
                      <label className={lbl}>Vai trò <span className="text-red-500">*</span></label>
                      <div className="relative">
                        <select value={role} onChange={(e) => setRole(e.target.value)}
                          className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold appearance-none pr-8 cursor-pointer">
                          <option value="Staff">Lễ tân / Trợ lý</option>
                          <option value="Dentist">Nha sĩ / Bác sĩ chuyên khoa</option>
                          <option value="Admin">Quản trị viên (Admin)</option>
                          <option value="Owner">Chủ phòng khám</option>
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                          </svg>
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 flex items-start gap-3">
                    <svg className="w-5 h-5 text-amber-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z" />
                    </svg>
                    <div>
                      <div className="text-[13px] font-black text-amber-800">Mật khẩu tự động</div>
                      <p className="text-[12px] text-amber-700 font-semibold mt-0.5 leading-relaxed">
                        Hệ thống sẽ tạo mật khẩu ngẫu nhiên và gửi đến{" "}
                        <span className="font-black text-amber-900">{email || "email của nhân viên"}</span>.
                      </p>
                    </div>
                  </div>
                </div>
              )}

              {/* Actions */}
              <div className="flex items-center justify-end gap-3">
                <button type="button" onClick={() => router.back()} disabled={isSubmitting}
                  className="px-6 py-3 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-white border border-slate-200 rounded-xl transition-all cursor-pointer disabled:opacity-50 shadow-sm">
                  Hủy bỏ
                </button>
                <button type="submit" disabled={isSubmitting}
                  className="px-6 py-3 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all cursor-pointer flex items-center gap-2 min-w-[160px] justify-center disabled:opacity-50">
                  {isSubmitting ? (
                    <><svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>Đang tạo...</>
                  ) : (
                    <>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                      </svg>
                      Tạo tài khoản & Gửi email
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      </main>
    </div>
  );
}
