"use client";

import { useState, useEffect, useCallback, useMemo, useRef } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import { Toast, useToast } from "../../../components/shared/Toast";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getStaffAppointmentsApi,
  checkInAppointmentApi,
  markNoShowAppointmentApi,
  getStaffScheduleApi,
  createWalkInAppointmentApi,
  sendPatientEmailVerificationApi,
  createPatientAccountApi,
  getServicesApi,
  searchPatientsApi,
  getFollowUpDueApi,
  undoCheckInAppointmentApi,
  undoNoShowAppointmentApi,
  type StaffAppointmentDto,
  type StaffScheduleResponse,
  type ServiceDto,
  type PatientSearchResultDto,
  type FollowUpDueDto,
} from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";

/* ─── constants ─────────────────────────────────────────── */

// Lưới giờ khớp với GetStaffScheduleHandler (backend). Mỗi ô chỉ khả dụng nếu
// bác sĩ có ca bao trùm khung giờ đó (tra theo `slot.time`).
const TIMES_MORNING   = ["08:00","08:30","09:00","09:30","10:00","10:30","11:00","11:30"];
const TIMES_AFTERNOON = ["13:30","14:00","14:30","15:00","15:30","16:00","16:30","17:00"];
const TIMES_EVENING   = ["17:30","18:00","18:30","19:00","19:30","20:00","20:30","21:00"];

// Các trạng thái sau khi đã check-in — bệnh nhân được coi là đã đến khám.
const ARRIVED_STATUSES = ["CheckedIn", "InProgress", "PendingPayment", "Completed"];

// Nhãn + màu cho banner trạng thái ở panel chi tiết khi lịch đã được xử lý.
const CHECK_ICON = "M4.5 12.75l6 6 9-13.5";
const BAN_ICON   = "M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636";
const PROCESSED_STATUS: Record<string, { label: string; cls: string; icon: string }> = {
  CheckedIn:      { label: "Đã check-in",    cls: "bg-emerald-50 text-emerald-700 border-emerald-200", icon: CHECK_ICON },
  InProgress:     { label: "Đang khám",      cls: "bg-violet-50 text-violet-700 border-violet-200",    icon: CHECK_ICON },
  PendingPayment: { label: "Chờ thanh toán", cls: "bg-sky-50 text-sky-700 border-sky-200",             icon: CHECK_ICON },
  Completed:      { label: "Hoàn thành",     cls: "bg-green-50 text-green-700 border-green-200",        icon: CHECK_ICON },
  NoShow:         { label: "Vắng mặt",       cls: "bg-amber-50 text-amber-700 border-amber-200",        icon: BAN_ICON   },
};

/* ─── style helpers ──────────────────────────────────────── */
const selectCls = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8";
const inputCls  = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";

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

/* ─── Create Patient Account Tab ─────────────────────────── */

function CreatePatientAccountTab({
  onAccountCreated,
  onGoToWalkin,
}: {
  onAccountCreated: (patient: PatientSearchResultDto) => void;
  onGoToWalkin?: () => void;
}) {
  const [form, setForm] = useState({
    name: "",
    phone: "",
    email: "",
    dob: "",
    gender: "Nam",
  });
  const [verifyCode, setVerifyCode] = useState("");
  const [verifySentTo, setVerifySentTo] = useState<string | null>(null);
  const [sendingCode, setSendingCode] = useState(false);
  const [countdown, setCountdown] = useState(0);
  const [saving, setSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [phoneError, setPhoneError] = useState<string | null>(null);
  const [dobError, setDobError] = useState<string | null>(null);
  const [emailError, setEmailError] = useState<string | null>(null);
  const { toast, showToast } = useToast();
  const dobPickerRef = useRef<HTMLInputElement>(null);

  // Danh sách bệnh nhân chưa có tài khoản — bấm để điền nhanh form thay vì gõ tay lại từ đầu.
  const [accountlessList, setAccountlessList] = useState<PatientSearchResultDto[]>([]);
  const [listLoading, setListLoading] = useState(true);
  const [listSearch, setListSearch] = useState("");

  useEffect(() => {
    let cancelled = false;
    setListLoading(true);
    const timer = setTimeout(() => {
      void searchPatientsApi(listSearch.trim(), 20, true)
        .then(rows => { if (!cancelled) setAccountlessList(rows); })
        .catch(() => { if (!cancelled) setAccountlessList([]); })
        .finally(() => { if (!cancelled) setListLoading(false); });
    }, 300);
    return () => { cancelled = true; clearTimeout(timer); };
  }, [listSearch]);

  const pickFromAccountlessList = (p: PatientSearchResultDto) => {
    const [y, m, d] = (p.dateOfBirth || "").split("-");
    const gender = ["Nam", "Nữ", "Khác"].includes(p.gender) ? p.gender : "Nam";
    setForm(prev => ({
      ...prev,
      name: p.fullName,
      phone: (p.phoneNumber ?? "").replace(/\D/g, "").slice(0, 11),
      dob: d && m && y ? `${d}/${m}/${y}` : "",
      gender,
    }));
    setPhoneError(null);
    setDobError(null);
  };

  // Đếm ngược gửi lại mã OTP
  useEffect(() => {
    if (countdown <= 0) return;
    const timer = setInterval(() => setCountdown(c => c - 1), 1000);
    return () => clearInterval(timer);
  }, [countdown]);

  const handleDobChange = (raw: string) => {
    const d = raw.replace(/\D/g, '').slice(0, 8);
    const formatted =
      d.length > 4 ? `${d.slice(0, 2)}/${d.slice(2, 4)}/${d.slice(4)}` :
      d.length > 2 ? `${d.slice(0, 2)}/${d.slice(2)}` :
      d;
    setForm(p => ({ ...p, dob: formatted }));
    setDobError(null);
  };

  const dobIso = (() => {
    const d = form.dob.replace(/\D/g, '');
    return d.length === 8 ? `${d.slice(4, 8)}-${d.slice(2, 4)}-${d.slice(0, 2)}` : "";
  })();

  const todayIso = (() => {
    const t = new Date();
    return `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, '0')}-${String(t.getDate()).padStart(2, '0')}`;
  })();

  const openDobPicker = () => {
    const el = dobPickerRef.current;
    if (!el) return;
    try {
      if (typeof el.showPicker === "function") el.showPicker();
      else el.click();
    } catch {
      el.click();
    }
  };

  const handleDobPicked = (iso: string) => {
    if (!iso) return;
    const [y, m, d] = iso.split("-");
    setForm(p => ({ ...p, dob: `${d}/${m}/${y}` }));
    setDobError(null);
  };

  const handlePhoneChange = (val: string) => {
    const digits = val.replace(/\D/g, '').slice(0, 11);
    setForm(p => ({ ...p, phone: digits }));
    setPhoneError(null);
  };

  const validatePhone = (val: string) => {
    if (val.length === 0) return false;
    if (val.length !== 10 && val.length !== 11) {
      setPhoneError(`Số điện thoại phải có 10 hoặc 11 chữ số (đang nhập ${val.length} số)`);
      return false;
    }
    setPhoneError(null);
    return true;
  };

  const validateEmail = (val: string) => {
    if (!val.trim()) {
      setEmailError("Vui lòng nhập địa chỉ email của bệnh nhân");
      return false;
    }
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!regex.test(val.trim())) {
      setEmailError("Email không đúng định dạng (ví dụ: benhnhan@gmail.com)");
      return false;
    }
    setEmailError(null);
    return true;
  };

  const validateDob = (val: string) => {
    const d = val.replace(/\D/g, '');
    if (d.length === 0) return true;
    if (d.length !== 8) {
      setDobError('Chưa đủ 8 chữ số, nhập theo định dạng dd/mm/yyyy');
      return false;
    }
    const day = +d.slice(0, 2), mon = +d.slice(2, 4), yr = +d.slice(4, 8);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    if (mon < 1 || mon > 12 || day < 1 || day > new Date(yr, mon, 0).getDate() || yr < 1900 || yr > today.getFullYear()) {
      setDobError('Ngày sinh không hợp lệ (năm phải từ 1900 đến nay)');
      return false;
    }
    if (new Date(yr, mon - 1, day) >= today) {
      setDobError('Ngày sinh không được là hôm nay hoặc tương lai');
      return false;
    }
    setDobError(null);
    return true;
  };

  const handleSendCode = async () => {
    const email = form.email.trim();
    if (!validateEmail(email)) return;

    setSendingCode(true);
    setErrorMessage(null);
    try {
      await sendPatientEmailVerificationApi(email);
      setVerifySentTo(email);
      setVerifyCode("");
      setCountdown(60);
      showToast(`Đã gửi mã xác thực 6 số tới email: ${email}`);
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Gửi mã xác thực thất bại";
      setErrorMessage(msg);
      showToast(msg, "error");
    } finally {
      setSendingCode(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    // Validate họ tên
    if (!form.name.trim()) {
      setErrorMessage("Vui lòng nhập họ và tên bệnh nhân");
      return;
    }

    // Validate phone
    const phoneDigits = form.phone.replace(/\D/g, '');
    if (phoneDigits.length !== 10 && phoneDigits.length !== 11) {
      setPhoneError('Số điện thoại phải có 10 hoặc 11 chữ số');
      return;
    }

    // Validate email
    if (!validateEmail(form.email)) return;

    // Validate mã xác thực email
    const code = verifyCode.trim();
    if (!code || code.length !== 6) {
      setErrorMessage("Vui lòng gửi mã xác thực và nhập đúng 6 chữ số từ email bệnh nhân");
      return;
    }

    if (verifySentTo !== form.email.trim()) {
      setErrorMessage("Địa chỉ email đã thay đổi sau khi gửi mã. Vui lòng bấm 'Gửi lại mã' cho email mới.");
      return;
    }

    // Validate ngày sinh
    let isoDate: string | undefined = undefined;
    if (form.dob.trim()) {
      if (!validateDob(form.dob)) return;
      const dobDigits = form.dob.replace(/\D/g, '');
      const dd = +dobDigits.slice(0, 2);
      const mm = +dobDigits.slice(2, 4);
      const yyyy = +dobDigits.slice(4, 8);
      isoDate = `${yyyy}-${String(mm).padStart(2, '0')}-${String(dd).padStart(2, '0')}`;
    }

    try {
      setSaving(true);
      const result = await createPatientAccountApi({
        fullName: form.name.trim(),
        email: form.email.trim(),
        phoneNumber: phoneDigits,
        dateOfBirth: isoDate,
        gender: form.gender,
        verificationCode: code,
      });

      const patientData: PatientSearchResultDto = {
        id: result.patientId,
        fullName: result.fullName || form.name.trim(),
        phoneNumber: phoneDigits,
        dateOfBirth: isoDate || "",
        gender: form.gender,
        hasAccount: true,
      };

      onAccountCreated(patientData);
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Tạo tài khoản bệnh nhân thất bại";
      setErrorMessage(msg);
      showToast(msg, "error");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="flex flex-col lg:flex-row gap-6 flex-1 min-h-0">
      <Toast toast={toast} />

      {/* Main form card */}
      <div className="flex-1 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 sm:p-8 flex flex-col overflow-y-auto min-h-0">
        <div className="flex items-center justify-between gap-4 border-b border-slate-100 pb-5 mb-6">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center font-bold shrink-0">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.765z" />
              </svg>
            </div>
            <div>
              <h3 className="text-[17px] font-black text-slate-900">Tạo tài khoản bệnh nhân</h3>
              <p className="text-[12.5px] text-slate-500 font-semibold">
                Cấp tài khoản đăng nhập để bệnh nhân xem hồ sơ & tự đặt lịch trên app
              </p>
            </div>
          </div>
          {onGoToWalkin && (
            <button
              type="button"
              onClick={onGoToWalkin}
              className="text-[12.5px] font-bold text-slate-400 hover:text-slate-700 cursor-pointer flex items-center gap-1 transition-colors"
            >
              Bỏ qua sang đặt lịch
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
              </svg>
            </button>
          )}
        </div>

        {errorMessage && (
          <div className="mb-6 p-4 rounded-xl bg-red-50 border border-red-200 text-red-700 text-[13px] font-semibold flex items-start gap-2.5">
            <svg className="w-5 h-5 shrink-0 text-red-500 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
            </svg>
            <span>{errorMessage}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-6 flex-1">
          {/* Section 1: Personal info */}
          <div className="flex flex-col gap-4">
            <div className="flex items-center gap-2">
              <span className="w-6 h-6 rounded-full bg-slate-100 text-slate-600 text-[11.5px] font-black flex items-center justify-center">1</span>
              <h4 className="text-[13px] font-black text-slate-800 uppercase tracking-wider">Thông tin cá nhân</h4>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Họ và tên *</label>
                <input
                  required
                  value={form.name}
                  onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                  placeholder="Nguyễn Văn A"
                  className={inputCls}
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Số điện thoại *</label>
                <input
                  required
                  value={form.phone}
                  onChange={e => handlePhoneChange(e.target.value)}
                  onBlur={() => validatePhone(form.phone)}
                  placeholder="0912345678"
                  inputMode="numeric"
                  className={`${inputCls} ${phoneError ? "border-red-400 bg-red-50 focus:border-red-400 focus:ring-red-200" : ""}`}
                />
                {phoneError && <p className="text-[11.5px] font-semibold text-red-500">{phoneError}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ngày sinh *</label>
                <div className="relative">
                  <input
                    required
                    value={form.dob}
                    onChange={e => handleDobChange(e.target.value)}
                    onBlur={() => validateDob(form.dob)}
                    placeholder="dd/mm/yyyy"
                    inputMode="numeric"
                    className={`${inputCls} pr-10 ${dobError ? "border-red-400 bg-red-50 focus:border-red-400 focus:ring-red-200" : ""}`}
                  />
                  <button
                    type="button"
                    onClick={openDobPicker}
                    title="Chọn từ lịch"
                    className="absolute inset-y-0 right-0 w-9 flex items-center justify-center text-slate-400 hover:text-primary cursor-pointer"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                    </svg>
                  </button>
                  <input
                    ref={dobPickerRef}
                    type="date"
                    max={todayIso}
                    value={dobIso}
                    onChange={e => handleDobPicked(e.target.value)}
                    tabIndex={-1}
                    aria-hidden="true"
                    className="absolute bottom-1 right-3 w-px h-px opacity-0 pointer-events-none"
                  />
                </div>
                {dobError && <p className="text-[11.5px] font-semibold text-red-500">{dobError}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Giới tính *</label>
                <div className="relative">
                  <select
                    required
                    value={form.gender}
                    onChange={e => setForm(p => ({ ...p, gender: e.target.value }))}
                    className={selectCls}
                  >
                    <option>Nam</option>
                    <option>Nữ</option>
                    <option>Khác</option>
                  </select>
                  <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Section 2: Email & Verification */}
          <div className="flex flex-col gap-4 pt-2 border-t border-slate-100">
            <div className="flex items-center gap-2">
              <span className="w-6 h-6 rounded-full bg-slate-100 text-slate-600 text-[11.5px] font-black flex items-center justify-center">2</span>
              <h4 className="text-[13px] font-black text-slate-800 uppercase tracking-wider">Xác thực Email & Cấp tài khoản</h4>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">
                  Email bệnh nhân * <span className="text-slate-400 normal-case font-bold">(để nhận tài khoản & mật khẩu)</span>
                </label>
                <div className="relative">
                  <input
                    required
                    type="email"
                    value={form.email}
                    onChange={e => {
                      setForm(p => ({ ...p, email: e.target.value }));
                      setEmailError(null);
                    }}
                    onBlur={() => form.email.trim() && validateEmail(form.email)}
                    placeholder="benhnhan@gmail.com"
                    className={`${inputCls} ${emailError ? "border-red-400 bg-red-50 focus:border-red-400 focus:ring-red-200" : ""}`}
                  />
                </div>
                {emailError && <p className="text-[11.5px] font-semibold text-red-500">{emailError}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">
                  Mã xác thực OTP (6 số) *
                </label>
                <div className="flex gap-2">
                  <input
                    required
                    value={verifyCode}
                    onChange={e => setVerifyCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                    placeholder="Nhập mã 6 số"
                    inputMode="numeric"
                    className={`${inputCls} font-mono tracking-widest text-center font-bold flex-1`}
                  />
                  <button
                    type="button"
                    disabled={sendingCode || countdown > 0 || !form.email.trim()}
                    onClick={handleSendCode}
                    className="px-4 py-2.5 rounded-xl bg-slate-800 text-white text-[13px] font-bold whitespace-nowrap cursor-pointer hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed transition-all shrink-0 flex items-center gap-1.5"
                  >
                    {sendingCode ? (
                      <>
                        <div className="w-3.5 h-3.5 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                        <span>Đang gửi...</span>
                      </>
                    ) : countdown > 0 ? (
                      <span>Gửi lại ({countdown}s)</span>
                    ) : (
                      <span>Gửi mã OTP</span>
                    )}
                  </button>
                </div>
              </div>
            </div>

            <div className="p-3.5 rounded-xl bg-sky-50/80 border border-sky-100 text-sky-800 text-[12px] font-semibold flex items-center gap-2.5">
              <svg className="w-4 h-4 text-sky-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
              </svg>
              <span>
                {verifySentTo === form.email.trim()
                  ? `Đã gửi mã xác thực tới ${verifySentTo}. Nhờ bệnh nhân mở hộp thư và đọc lại mã 6 số.`
                  : "Bấm nút \"Gửi mã OTP\" để gửi mã 6 số tới email bệnh nhân, sau đó nhập mã xác nhận trước khi lưu."}
              </span>
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex flex-col sm:flex-row items-center gap-3 pt-4 border-t border-slate-100 mt-auto">
            <button
              type="submit"
              disabled={saving}
              className="flex-1 w-full sm:w-auto flex items-center justify-center gap-2.5 py-3.5 px-6 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer shadow-sm shadow-primary/25"
            >
              {saving ? (
                <>
                  <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                  <span>Đang tạo tài khoản...</span>
                </>
              ) : (
                <>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.765z" />
                  </svg>
                  <span>Lưu tài khoản & Sang đặt lịch</span>
                  <svg className="w-4 h-4 ml-1" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                  </svg>
                </>
              )}
            </button>
            {onGoToWalkin && (
              <button
                type="button"
                onClick={onGoToWalkin}
                className="w-full sm:w-auto px-5 py-3.5 bg-slate-100 text-slate-600 hover:bg-slate-200 border border-slate-200 rounded-xl text-[13.5px] font-bold cursor-pointer transition-all"
              >
                Hủy
              </button>
            )}
          </div>
        </form>
      </div>

      {/* Danh sách bệnh nhân chưa có tài khoản (Desktop right side) */}
      <div className="w-full lg:w-80 shrink-0 flex flex-col gap-4 min-h-0">
        <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-4 flex-1 min-h-0">
          <div>
            <h4 className="text-[14px] font-black text-slate-900 flex items-center gap-2">
              <svg className="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
              </svg>
              Bệnh nhân chưa có tài khoản
            </h4>
            <p className="text-[11.5px] text-slate-400 font-semibold mt-1">Bấm chọn một bệnh nhân để điền nhanh thông tin vào form bên trái.</p>
          </div>

          <div className="relative shrink-0">
            <span className="absolute inset-y-0 left-3 flex items-center text-slate-400 pointer-events-none">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input
              value={listSearch}
              onChange={e => setListSearch(e.target.value)}
              placeholder="Tìm theo tên, số điện thoại..."
              className="w-full pl-9 pr-3 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
            />
          </div>

          <div className="flex-1 overflow-y-auto -mx-1.5 px-1.5 flex flex-col gap-2 min-h-0">
            {listLoading ? (
              <div className="py-8 text-center text-slate-400 font-semibold text-[12.5px] animate-pulse">Đang tải...</div>
            ) : accountlessList.length === 0 ? (
              <div className="py-8 text-center text-slate-400 font-semibold text-[12.5px]">
                {listSearch.trim() ? "Không tìm thấy bệnh nhân phù hợp." : "Chưa có bệnh nhân nào chưa có tài khoản."}
              </div>
            ) : accountlessList.map(p => (
              <button
                key={p.id}
                type="button"
                onClick={() => pickFromAccountlessList(p)}
                className="text-left p-3 rounded-xl border border-slate-200 hover:border-primary hover:bg-red-50/10 transition-all cursor-pointer shrink-0"
              >
                <div className="font-extrabold text-slate-900 text-[13px] truncate">{p.fullName}</div>
                <div className="text-[11.5px] text-slate-400 font-semibold mt-0.5">
                  {p.phoneNumber || "Chưa có SĐT"}{p.dateOfBirth ? ` · ${fmtDate(p.dateOfBirth)}` : ""}
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

/* ─── Walk-in tab ─────────────────────────────────────────── */

const SLOT_COLORS = [
  "bg-sky-50 text-sky-700 border-sky-200",
  "bg-violet-50 text-violet-700 border-violet-200",
  "bg-rose-50 text-rose-700 border-rose-200",
  "bg-amber-50 text-amber-700 border-amber-200",
  "bg-emerald-50 text-emerald-700 border-emerald-200",
];

function WalkinTab({
  selectedPatient,
  onClearSelectedPatient,
  onGoToCreateAccount,
  followUpFromAppointmentId,
  followUpServiceId,
  onClearFollowUp,
}: {
  selectedPatient?: PatientSearchResultDto | null;
  onClearSelectedPatient?: () => void;
  onGoToCreateAccount?: () => void;
  /** Có giá trị khi bệnh nhân được chọn từ tab Tái khám — gửi kèm khi đặt lịch để bác sĩ
   * vẫn thấy lại liệu trình cũ, dù staff chọn giờ/bác sĩ khác với buổi khám trước. */
  followUpFromAppointmentId?: string | null;
  followUpServiceId?: string | null;
  onClearFollowUp?: () => void;
}) {
  const [schedule,  setSchedule]  = useState<StaffScheduleResponse | null>(null);
  const [services,  setServices]  = useState<ServiceDto[]>([]);
  // Ngày của lưới lịch trống. Trước đây khóa cứng ở hôm nay, nên bệnh nhân đang đứng tại quầy muốn
  // hẹn hôm sau thì lễ tân không đặt được — phải bảo họ tự đặt trên app.
  const [gridDate,  setGridDate]  = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  });
  const [loading,   setLoading]   = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [selected,  setSelected]  = useState<{
    dentistId: string; dentistName: string; room: string; time: string;
  } | null>(null);
  const [form,      setForm]      = useState({ name: "", phone: "", dob: "", gender: "Nam", serviceId: "", note: "" });
  const { toast } = useToast();
  const [saving,    setSaving]    = useState(false);
  const [saved,     setSaved]     = useState(false);
  const [bookError, setBookError] = useState<string | null>(null);
  const [phoneError, setPhoneError] = useState<string | null>(null);
  const [dobError,   setDobError]   = useState<string | null>(null);

  // Tra cứu bệnh nhân cũ để điền nhanh. `linked` giữ hồ sơ đã chọn: gửi kèm patientId khi
  // đặt lịch để backend tái dùng đúng hồ sơ đó thay vì dò lại theo số điện thoại.
  const [lookup,      setLookup]      = useState("");
  const [results,     setResults]     = useState<PatientSearchResultDto[]>([]);
  const [searching,   setSearching]   = useState(false);
  const [linked,      setLinked]      = useState<PatientSearchResultDto | null>(null);
  const [phoneFamilyMembers, setPhoneFamilyMembers] = useState<PatientSearchResultDto[]>([]);

  // Tự động nhận bệnh nhân vừa tạo từ tab Tạo tài khoản
  useEffect(() => {
    if (selectedPatient) {
      const [y, m, d] = (selectedPatient.dateOfBirth || "").split("-");
      const gender = ["Nam", "Nữ", "Khác"].includes(selectedPatient.gender) ? selectedPatient.gender : "Nam";
      setForm(prev => ({
        ...prev,
        name:   selectedPatient.fullName,
        phone:  (selectedPatient.phoneNumber ?? "").replace(/\D/g, "").slice(0, 11),
        dob:    d && m && y ? `${d}/${m}/${y}` : "",
        gender,
      }));
      setLinked(selectedPatient);
      setLookup("");
      setResults([]);
      setPhoneFamilyMembers([]);
      setPhoneError(null);
      setDobError(null);
    }
  }, [selectedPatient]);

  // Giữ đúng dịch vụ của buổi khám trước khi bệnh nhân đến từ tab Tái khám — staff vẫn đổi được nếu cần.
  useEffect(() => {
    if (followUpFromAppointmentId && followUpServiceId) {
      setForm(prev => ({ ...prev, serviceId: followUpServiceId }));
    }
  }, [followUpFromAppointmentId, followUpServiceId]);

  useEffect(() => {
    const term = lookup.trim();
    if (term.length < 2) { setResults([]); setSearching(false); return; }

    let cancelled = false;
    setSearching(true);
    const timer = setTimeout(() => {
      void searchPatientsApi(term)
        .then(rows => { if (!cancelled) setResults(rows); })
        .catch(() => { if (!cancelled) setResults([]); })
        .finally(() => { if (!cancelled) setSearching(false); });
    }, 300);

    return () => { cancelled = true; clearTimeout(timer); };
  }, [lookup]);

  // Tự động gợi ý thành viên gia đình khi nhập SĐT đủ 10 chữ số
  useEffect(() => {
    const p = form.phone.replace(/\D/g, "");
    if (p.length < 10 || linked) {
      setPhoneFamilyMembers([]);
      return;
    }

    let cancelled = false;
    const timer = setTimeout(() => {
      void searchPatientsApi(p, 10)
        .then(rows => {
          if (!cancelled) {
            const matched = rows.filter(r => (r.phoneNumber ?? "").replace(/\D/g, "") === p || !!r.primaryPatientName);
            setPhoneFamilyMembers(matched.length > 0 ? matched : rows);
          }
        })
        .catch(() => {
          if (!cancelled) setPhoneFamilyMembers([]);
        });
    }, 300);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [form.phone, linked]);

  const pickPatient = (p: PatientSearchResultDto) => {
    const [y, m, d] = (p.dateOfBirth || "").split("-");
    const gender = ["Nam", "Nữ", "Khác"].includes(p.gender) ? p.gender : "Khác";
    setForm(prev => ({
      ...prev,
      name:   p.fullName,
      phone:  (p.phoneNumber ?? "").replace(/\D/g, "").slice(0, 11),
      dob:    d && m && y ? `${d}/${m}/${y}` : "",
      gender,
    }));
    setLinked(p);
    setLookup("");
    setResults([]);
    setPhoneFamilyMembers([]);
    setPhoneError(null);
    setDobError(null);
  };

  const unlinkPatient = () => {
    setLinked(null);
    setPhoneFamilyMembers([]);
    onClearSelectedPatient?.();
    onClearFollowUp?.();
    setForm(prev => ({ ...prev, name: "", phone: "", dob: "", gender: "Nam" }));
  };

  const dobPickerRef = useRef<HTMLInputElement>(null);

  // Chỉ giữ chữ số rồi tự chèn "/" — staff gõ "01012000" ra "01/01/2000", không thể sai định dạng.
  const handleDobChange = (raw: string) => {
    const d = raw.replace(/\D/g, '').slice(0, 8);
    const formatted =
      d.length > 4 ? `${d.slice(0, 2)}/${d.slice(2, 4)}/${d.slice(4)}` :
      d.length > 2 ? `${d.slice(0, 2)}/${d.slice(2)}` :
      d;
    setForm(p => ({ ...p, dob: formatted }));
    setDobError(null);
  };

  // "dd/mm/yyyy" → "yyyy-mm-dd" cho <input type="date">; rỗng nếu chưa đủ 8 chữ số.
  const dobIso = (() => {
    const d = form.dob.replace(/\D/g, '');
    return d.length === 8 ? `${d.slice(4, 8)}-${d.slice(2, 4)}-${d.slice(0, 2)}` : "";
  })();

  const todayIso = (() => {
    const t = new Date();
    return `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, '0')}-${String(t.getDate()).padStart(2, '0')}`;
  })();

  const openDobPicker = () => {
    const el = dobPickerRef.current;
    if (!el) return;
    // showPicker() mở lịch mà không cần hiện input. Nó ném lỗi nếu input bị coi là không
    // render hoặc thiếu user-activation, nên luôn có đường lui sang click().
    try {
      if (typeof el.showPicker === "function") el.showPicker();
      else el.click();
    } catch {
      el.click();
    }
  };

  const handleDobPicked = (iso: string) => {
    if (!iso) return;
    const [y, m, d] = iso.split("-");
    setForm(p => ({ ...p, dob: `${d}/${m}/${y}` }));
    setDobError(null);
  };

  const handlePhoneChange = (val: string) => {
    const digits = val.replace(/\D/g, '').slice(0, 11);
    setForm(p => ({ ...p, phone: digits }));
    setPhoneError(null);
  };

  const validatePhone = (val: string) => {
    if (val.length === 0) return;
    if (val.length !== 10 && val.length !== 11)
      setPhoneError(`Số điện thoại phải có 10 hoặc 11 chữ số (đang nhập ${val.length} số)`);
  };

  const validateDob = (val: string) => {
    const d = val.replace(/\D/g, '');
    if (d.length === 0) return;
    if (d.length !== 8) { setDobError('Chưa đủ 8 chữ số, nhập theo định dạng dd/mm/yyyy'); return; }
    const day = +d.slice(0, 2), mon = +d.slice(2, 4), yr = +d.slice(4, 8);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    if (mon < 1 || mon > 12 || day < 1 || day > new Date(yr, mon, 0).getDate() || yr < 1900 || yr > today.getFullYear()) {
      setDobError('Ngày sinh không hợp lệ (năm phải từ 1900 đến nay)'); return;
    }
    if (new Date(yr, mon - 1, day) >= today) { setDobError('Ngày sinh không được là hôm nay hoặc tương lai'); return; }
    setDobError(null);
  };

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setLoadError(null);
      // Đổi ngày thì ô đang chọn không còn ý nghĩa — giữ lại sẽ đặt nhầm sang ngày mới.
      setSelected(null);
      const [sched, svcs] = await Promise.all([
        getStaffScheduleApi(gridDate),
        getServicesApi({ status: "active" }),
      ]);
      setSchedule(sched);
      setServices(svcs);
      setForm(p => ({ ...p, serviceId: p.serviceId || svcs[0]?.id || "" }));
    } catch (e) {
      setLoadError(e instanceof Error ? e.message : "Không thể tải dữ liệu");
    } finally {
      setLoading(false);
    }
  }, [gridDate]);

  useEffect(() => {
    void load();
    const channel = supabase
      .channel(`staff-walkin-grid-${gridDate}`)
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        void load();
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, [load, gridDate]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selected) return;
    setBookError(null);

    // Validate số điện thoại: 10 hoặc 11 chữ số
    const phoneDigits = form.phone.replace(/\D/g, '');
    if (phoneDigits.length !== 10 && phoneDigits.length !== 11) {
      setPhoneError('Số điện thoại phải có 10 hoặc 11 chữ số');
      return;
    }

    // Validate ngày sinh
    const dobDigits = form.dob.replace(/\D/g, '');
    if (dobDigits.length !== 8) {
      setDobError('Vui lòng nhập đúng định dạng dd/mm/yyyy');
      return;
    }
    const dd   = +dobDigits.slice(0, 2);
    const mm   = +dobDigits.slice(2, 4);
    const yyyy = +dobDigits.slice(4, 8);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    if (
      mm < 1 || mm > 12 ||
      dd < 1 || dd > new Date(yyyy, mm, 0).getDate() ||
      yyyy < 1900 || yyyy > today.getFullYear()
    ) {
      setDobError('Ngày sinh không hợp lệ (năm phải từ 1900 đến nay)');
      return;
    }
    if (new Date(yyyy, mm - 1, dd) >= today) {
      setDobError('Ngày sinh không được là hôm nay hoặc tương lai');
      return;
    }
    const isoDate = `${yyyy}-${String(mm).padStart(2, '0')}-${String(dd).padStart(2, '0')}`;

    // Chuyển giờ Việt Nam (UTC+7) sang UTC
    // Dùng Date.UTC để treat components là giờ VN, rồi trừ offset +7h.
    // Ngày lấy từ lưới đang xem (gridDate), không phải hôm nay — nếu không thì mọi lịch đặt cho
    // ngày khác đều bị ghi nhầm về hôm nay mà không báo lỗi gì.
    const [gy, gm, gd] = gridDate.split("-").map(Number);
    const [h, m] = selected.time.split(":").map(Number);
    const vnMs  = Date.UTC(gy, gm - 1, gd, h, m, 0);
    const utcDate = new Date(vnMs - 7 * 60 * 60 * 1000);

    try {
      setSaving(true);
      await createWalkInAppointmentApi({
        dentistId:       selected.dentistId,
        appointmentDate: utcDate.toISOString(),
        patientName:     form.name,
        patientPhone:    phoneDigits,
        dateOfBirth:     isoDate,
        gender:          form.gender,
        serviceId:       form.serviceId || undefined,
        symptoms:        form.note || undefined,
        patientId:       linked?.id,
        followUpFromAppointmentId: followUpFromAppointmentId ?? undefined,
      });

      // Cập nhật lưới ngay lập tức, không chờ API refresh
      const bookedDentistId = selected.dentistId;
      const bookedTime      = selected.time;
      const bookedName      = form.name;
      setSchedule(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          dentists: prev.dentists.map(d => {
            if (d.dentistId !== bookedDentistId) return d;
            const slots = d.slots.map(s =>
              s.time === bookedTime ? { ...s, isBooked: true, patientName: bookedName } : s);
            return { ...d, slots };
          }),
        };
      });

      setSaved(true);
      setSelected(null);
      onClearSelectedPatient?.();
      onClearFollowUp?.();
      setTimeout(() => {
        setSaved(false);
        setForm(p => ({ name: "", phone: "", dob: "", gender: "Nam", serviceId: p.serviceId, note: "" }));
        setPhoneError(null);
        setDobError(null);
        setLinked(null);
        setPhoneFamilyMembers([]);
        setLookup("");
      }, 2000);
    } catch (e) {
      setBookError(e instanceof Error ? e.message : "Đặt lịch thất bại");
    } finally {
      setSaving(false);
    }
  };

  const colCount = (schedule?.dentists.length ?? 0) + 1;

  const renderSlotRow = (time: string) =>
    schedule?.dentists.map((dentist) => {
      const slot     = dentist.slots.find(s => s.time === time);
      const isSel    = selected?.dentistId === dentist.dentistId && selected?.time === time;
      return (
        <td key={dentist.dentistId} className="px-2 py-1.5 text-center">
          {!slot ? (
            // Bác sĩ không có ca bao trùm khung giờ này
            <span className="inline-block px-2.5 py-1.5 text-slate-300 text-[11px] font-bold w-full">—</span>
          ) : slot.isBooked ? (
            <span className="inline-block px-2.5 py-1.5 bg-slate-100 text-slate-500 border border-slate-200 rounded-lg text-[11px] font-bold max-w-[90px] truncate w-full">
              {(slot.patientName ?? "").split(" ").slice(-1)[0]}
            </span>
          ) : slot?.isPast ? (
            <span className="inline-block w-full px-2 py-1.5 rounded-lg border border-slate-200 bg-slate-50 text-slate-300 text-[11px] font-bold cursor-not-allowed">
              Đã qua giờ
            </span>
          ) : (
            <button
              onClick={() => setSelected(isSel ? null : { dentistId: dentist.dentistId, dentistName: dentist.name, room: dentist.room, time })}
              className={`w-full px-2 py-1.5 rounded-lg border text-[11px] font-bold transition-all cursor-pointer ${
                isSel
                  ? "bg-red-50 border-primary text-primary shadow-sm"
                  : "bg-emerald-50 border-emerald-200 text-emerald-600 hover:border-emerald-400 hover:shadow-sm"
              }`}>
              {isSel ? "✓ Đang chọn" : "Trống"}
            </button>
          )}
        </td>
      );
    });

  return (
    <div className="flex flex-col lg:flex-row gap-6 flex-1 min-h-0">
      <Toast toast={toast} />

      {/* Availability grid */}
      <div className="flex-1 flex flex-col gap-4 min-w-0">
        <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col flex-1 min-h-[380px] lg:min-h-0">
          <div className="px-4 sm:px-5 py-3.5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 shrink-0">
            <div className="flex items-center gap-3 flex-wrap">
              <h3 className="text-[14px] font-black text-slate-900">Lịch trống</h3>
              <input
                type="date"
                value={gridDate}
                min={todayIso}
                onChange={e => setGridDate(e.target.value)}
                className="px-3 py-1.5 text-[12.5px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-700"
              />
              <span className={`px-2 py-1 rounded-lg border text-[11px] font-black ${getDateBadgeColor(gridDate)}`}>
                {formatDateLabel(gridDate)}
              </span>
            </div>
            <div className="flex items-center gap-2.5 sm:gap-3 text-[11.5px] sm:text-[12px] font-bold flex-wrap">
              <span className="flex items-center gap-1.5 text-slate-400"><span className="w-3 h-3 rounded bg-slate-100 border border-slate-200 inline-block" />Đã đặt</span>
              <span className="flex items-center gap-1.5 text-emerald-600"><span className="w-3 h-3 rounded bg-emerald-50 border border-emerald-200 inline-block" />Trống</span>
              <span className="flex items-center gap-1.5 text-primary"><span className="w-3 h-3 rounded bg-red-50 border border-primary inline-block" />Đang chọn</span>
              <span className="flex items-center gap-1.5 text-slate-300"><span className="w-3 h-3 rounded bg-slate-50 border border-slate-200 inline-block" />Đã qua giờ</span>
            </div>
          </div>

          {loading ? (
            <div className="p-8 flex items-center justify-center">
              <div className="w-6 h-6 border-2 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : loadError ? (
            <div className="p-6 text-center text-[13px] text-red-500 font-semibold">{loadError}</div>
          ) : (schedule?.dentists.length ?? 0) === 0 ? (
            // Chọn được ngày khác thì gặp được ngày không ai trực — bảng trống trơn không nói lên
            // điều đó, lễ tân sẽ tưởng trang bị lỗi.
            <div className="p-10 flex flex-col items-center gap-2 text-center">
              <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center">
                <svg className="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
              </div>
              <p className="text-[13.5px] font-bold text-slate-500">Không có bác sĩ nào làm việc {formatDateLabel(gridDate).toLowerCase()}.</p>
              <p className="text-[12.5px] font-semibold text-slate-400">Chọn ngày khác, hoặc phân ca cho bác sĩ ở trang Lịch làm việc.</p>
            </div>
          ) : (
            // Bảng chiếm hết chỗ trống còn lại của thẻ và tự cuộn, nhờ đó thead ghim được ở
            // đỉnh và form bên phải luôn nằm trong màn hình khi xem ca tối.
            <div className="flex-1 min-h-0 overflow-x-auto overflow-y-auto">
              <table className="w-full text-[12px] min-w-[520px]">
                <thead>
                  {/* border-collapse (mặc định của preflight) nuốt mất border của th sticky khi
                      cuộn, nên dùng inset shadow để vẽ đường kẻ dưới header. */}
                  <tr>
                    <th className="sticky top-0 z-20 bg-slate-50 shadow-[inset_0_-1px_0_0_#e2e8f0] px-4 py-2.5 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider w-20">Giờ</th>
                    {schedule?.dentists.map((d, i) => (
                      <th key={d.dentistId} className="sticky top-0 z-20 bg-slate-50 shadow-[inset_0_-1px_0_0_#e2e8f0] px-3 py-2.5 text-center font-extrabold text-[11px] uppercase tracking-wider">
                        <div className="inline-flex flex-col items-center gap-0.5">
                          <span className={`px-2.5 py-1 rounded-lg border text-[11.5px] font-black ${SLOT_COLORS[i % SLOT_COLORS.length]}`}>{d.name}</span>
                          <span className="text-slate-400 font-semibold text-[10.5px]">{d.room}</span>
                        </div>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  <tr><td colSpan={colCount} className="px-4 py-1.5 bg-amber-50/40 border-y border-amber-100/60">
                    <span className="text-[11px] font-extrabold text-amber-600 uppercase tracking-wider flex items-center gap-1.5">
                      <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z" /></svg>
                      Ca sáng
                    </span>
                  </td></tr>
                  {TIMES_MORNING.map((time) => (
                    <tr key={time} className="border-b border-slate-50 hover:bg-slate-50/30 transition-colors">
                      <td className="px-4 py-2 font-mono font-black text-slate-600 text-[12.5px] shrink-0">{time}</td>
                      {renderSlotRow(time)}
                    </tr>
                  ))}
                  <tr><td colSpan={colCount} className="px-4 py-1.5 bg-indigo-50/40 border-y border-indigo-100/60">
                    <span className="text-[11px] font-extrabold text-indigo-600 uppercase tracking-wider flex items-center gap-1.5">
                      <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" /></svg>
                      Ca chiều
                    </span>
                  </td></tr>
                  {TIMES_AFTERNOON.map((time) => (
                    <tr key={time} className="border-b border-slate-50 hover:bg-slate-50/30 transition-colors">
                      <td className="px-4 py-2 font-mono font-black text-slate-600 text-[12.5px]">{time}</td>
                      {renderSlotRow(time)}
                    </tr>
                  ))}
                  <tr><td colSpan={colCount} className="px-4 py-1.5 bg-violet-50/40 border-y border-violet-100/60">
                    <span className="text-[11px] font-extrabold text-violet-600 uppercase tracking-wider flex items-center gap-1.5">
                      <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" /></svg>
                      Ca tối
                    </span>
                  </td></tr>
                  {TIMES_EVENING.map((time) => (
                    <tr key={time} className="border-b border-slate-50 hover:bg-slate-50/30 transition-colors">
                      <td className="px-4 py-2 font-mono font-black text-slate-600 text-[12.5px]">{time}</td>
                      {renderSlotRow(time)}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Walk-in form */}
      <div className="w-full lg:w-80 shrink-0 min-h-0">
        <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col gap-5 h-full overflow-y-auto">
          <h3 className="text-[15px] font-black text-slate-900">Đặt lịch tại quầy</h3>

          {saved ? (
            <div className="flex items-center gap-3 bg-green-50 border border-green-100 text-green-700 px-4 py-3 rounded-xl text-[13px] font-bold">
              <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
              Đã đặt lịch — bệnh nhân đã vào hàng đợi
            </div>
          ) : (
            <>
              {selected ? (
                <div className="flex items-center gap-3 p-3.5 bg-red-50 border border-primary/20 rounded-xl">
                  <div className="w-8 h-8 rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
                    <svg className="w-4 h-4 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  </div>
                  <div className="min-w-0">
                    <div className="text-[13px] font-black text-slate-900">{selected.time} · {selected.dentistName}</div>
                    {/* Ngày phải hiện ở đây: lưới đổi được sang ngày khác nên chỉ nhìn giờ là không
                        đủ để biết mình đang đặt cho hôm nào. */}
                    <div className="text-[12px] text-slate-500 font-semibold truncate">
                      {formatDateLabel(gridDate)} · {selected.room}
                    </div>
                  </div>
                  <button onClick={() => setSelected(null)} className="ml-auto text-slate-300 hover:text-slate-500 cursor-pointer">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                  </button>
                </div>
              ) : (
                <div className="flex items-center gap-2 p-3.5 bg-slate-50 border border-slate-200 border-dashed rounded-xl text-slate-400">
                  <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.042 21.672L13.684 16.6m0 0l-2.51 2.225.569-9.47 5.227 7.917-3.286-.672zm-7.518-.267A8.25 8.25 0 1120.25 10.5M8.288 14.212A5.25 5.25 0 1117.25 10.5" /></svg>
                  <span className="text-[12.5px] font-semibold">Chọn ô trống trên lịch</span>
                </div>
              )}

              {followUpFromAppointmentId && (
                <div className="flex items-center gap-2.5 p-3 bg-indigo-50 border border-indigo-200 rounded-xl">
                  <div className="w-8 h-8 rounded-xl bg-indigo-100 flex items-center justify-center shrink-0 text-indigo-700">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992V4.356m-.001 0v4.99m0-4.99l-3.181 3.183a8.25 8.25 0 00-11.667 0L3.75 9.348m0 0V4.356m0 4.992h4.99M3.75 14.652h4.992m-4.992 0v4.992m0-4.992l3.181 3.183a8.25 8.25 0 0011.667 0l2.416-2.415m0 0h-4.99m4.99 0v4.992" /></svg>
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="text-[12.5px] font-black text-indigo-900">Đang check-in tái khám</div>
                    <div className="text-[11px] text-indigo-700 font-semibold">Đặt lịch xong, bác sĩ sẽ thấy lại liệu trình cũ của bệnh nhân.</div>
                  </div>
                  <button type="button" onClick={onClearFollowUp} className="ml-auto text-slate-400 hover:text-red-500 cursor-pointer shrink-0" title="Bỏ liên kết, đặt như lịch vãng lai thường">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                  </button>
                </div>
              )}

              {bookError && (
                <div className="px-4 py-2.5 bg-red-50 border border-red-100 text-red-600 text-[12.5px] font-semibold rounded-xl">{bookError}</div>
              )}

              {/* Tra cứu bệnh nhân cũ */}
              {linked ? (
                <div className="flex items-center gap-2.5 p-3 bg-emerald-50 border border-emerald-200 rounded-xl">
                  <div className="w-8 h-8 rounded-xl bg-emerald-100 flex items-center justify-center shrink-0 text-emerald-700">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-1.5 flex-wrap">
                      <span className="text-[12.5px] font-black text-emerald-900 truncate">{linked.fullName}</span>
                      {linked.relationship && (
                        <span className="px-1.5 py-0.2 bg-indigo-100 text-indigo-800 text-[10px] font-extrabold rounded">{linked.relationship}</span>
                      )}
                    </div>
                    <div className="text-[11px] text-emerald-700 font-semibold flex items-center gap-1.5 mt-0.5 flex-wrap">
                      <span>{linked.phoneNumber ?? "—"}</span>
                      {linked.hasAccount && (
                        <span className="px-1.5 py-0.2 bg-emerald-200/70 text-emerald-800 text-[10px] font-extrabold rounded">Có tài khoản</span>
                      )}
                      {linked.primaryPatientName && (
                        <span className="text-[10.5px] text-emerald-700">· TK: {linked.primaryPatientName}</span>
                      )}
                    </div>
                  </div>
                  <button type="button" onClick={unlinkPatient} className="ml-auto text-slate-400 hover:text-red-500 cursor-pointer shrink-0" title="Bỏ chọn, nhập bệnh nhân khác">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                  </button>
                </div>
              ) : (
                <div className="flex flex-col gap-1.5">
                  <div className="flex items-center justify-between">
                    <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Tìm bệnh nhân cũ</label>
                    {onGoToCreateAccount && (
                      <button
                        type="button"
                        onClick={onGoToCreateAccount}
                        className="text-[11.5px] font-bold text-primary hover:underline cursor-pointer flex items-center gap-1"
                      >
                        + Tạo tài khoản mới
                      </button>
                    )}
                  </div>
                  <div className="relative">
                    <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                    </span>
                    <input value={lookup} onChange={e => setLookup(e.target.value)}
                      placeholder="Tên hoặc số điện thoại..."
                      className={`${inputCls} pl-10`} />
                    {searching && (
                      <span className="absolute inset-y-0 right-3 flex items-center">
                        <span className="w-3.5 h-3.5 border-2 border-slate-200 border-t-slate-400 rounded-full animate-spin" />
                      </span>
                    )}
                  </div>

                  {lookup.trim().length >= 2 && !searching && results.length === 0 && (
                    <p className="text-[11.5px] font-semibold text-slate-400">Không có hồ sơ khớp — điền thủ công bên dưới.</p>
                  )}

                  {results.length > 0 && (
                    <div className="flex flex-col gap-1 max-h-56 overflow-y-auto rounded-xl border border-slate-200 bg-white p-1">
                      {results.map(p => (
                        <button key={p.id} type="button" onClick={() => pickPatient(p)}
                          className="flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-left hover:bg-sky-50 transition-colors cursor-pointer">
                          <div className="w-8 h-8 rounded-lg bg-slate-100 text-slate-600 border border-slate-200 flex items-center justify-center font-black text-[11px] shrink-0">
                            {p.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-1.5 flex-wrap">
                              <span className="text-[12.5px] font-bold text-slate-800 truncate">{p.fullName}</span>
                              {p.relationship && (
                                <span className="text-[10px] font-bold px-1.5 py-0.2 rounded bg-indigo-50 text-indigo-700 border border-indigo-100">
                                  {p.relationship}
                                </span>
                              )}
                            </div>
                            <div className="text-[11px] text-slate-400 font-mono flex items-center gap-1 flex-wrap">
                              <span>{p.phoneNumber ?? "—"}</span>
                              {p.primaryPatientName && (
                                <span className="text-slate-400 text-[10.5px]">· TK: {p.primaryPatientName}</span>
                              )}
                            </div>
                          </div>
                          {p.hasAccount ? (
                            <span className="text-[10px] font-black px-1.5 py-0.5 rounded-md bg-emerald-50 text-emerald-600 border border-emerald-100 shrink-0">
                              Có TK
                            </span>
                          ) : p.primaryPatientId ? (
                            <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-md bg-violet-50 text-violet-600 border border-violet-100 shrink-0">
                              Thành viên
                            </span>
                          ) : null}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}

              <form onSubmit={handleSave} className="flex flex-col gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Họ và tên *</label>
                  <input required value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                    placeholder="Nguyễn Văn A" className={inputCls} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Số điện thoại *</label>
                  <input
                    required
                    value={form.phone}
                    onChange={e => handlePhoneChange(e.target.value)}
                    onBlur={() => validatePhone(form.phone)}
                    placeholder="0912345678"
                    inputMode="numeric"
                    className={`${inputCls} ${phoneError ? "border-red-400 bg-red-50 focus:border-red-400 focus:ring-red-200" : ""}`}
                  />
                  {phoneError && <p className="text-[11.5px] font-semibold text-red-500">{phoneError}</p>}

                  {/* Gợi ý thành viên gia đình khi SĐT đã có hồ sơ */}
                  {phoneFamilyMembers.length > 0 && !linked && (
                    <div className="p-2.5 bg-sky-50/90 border border-sky-200 rounded-xl flex flex-col gap-1.5 mt-1 animate-in fade-in duration-200">
                      <div className="text-[11.5px] font-bold text-sky-900 flex items-center gap-1.5">
                        <svg className="w-3.5 h-3.5 text-sky-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" /></svg>
                        SĐT này có hồ sơ gia đình:
                      </div>
                      <div className="flex flex-wrap gap-1.5">
                        {phoneFamilyMembers.map(m => (
                          <button
                            key={m.id}
                            type="button"
                            onClick={() => pickPatient(m)}
                            className="px-2.5 py-1 bg-white border border-sky-200 hover:border-sky-400 text-sky-900 rounded-lg text-[11.5px] font-bold transition-all flex items-center gap-1 shadow-xs cursor-pointer hover:bg-sky-50"
                          >
                            <span>{m.fullName}</span>
                            {m.relationship ? (
                              <span className="text-[10px] text-sky-600 font-medium">({m.relationship})</span>
                            ) : m.hasAccount ? (
                              <span className="text-[10px] text-emerald-600 font-medium">(Chủ TK)</span>
                            ) : null}
                          </button>
                        ))}
                      </div>
                      <div className="text-[10.5px] text-sky-700/80 italic">
                        Bấm để chọn đúng người, hoặc tiếp tục gõ tên mới bên trên nếu là thành viên mới.
                      </div>
                    </div>
                  )}
                </div>
                <div className="flex gap-3">
                  <div className="flex flex-col gap-1.5 flex-1 min-w-0">
                    <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ngày sinh *</label>
                    <div className="relative">
                      <input
                        required
                        value={form.dob}
                        onChange={e => handleDobChange(e.target.value)}
                        onBlur={() => validateDob(form.dob)}
                        placeholder="dd/mm/yyyy"
                        inputMode="numeric"
                        className={`${inputCls} pr-10 ${dobError ? "border-red-400 bg-red-50 focus:border-red-400 focus:ring-red-200" : ""}`}
                      />
                      <button type="button" onClick={openDobPicker} title="Chọn từ lịch"
                        className="absolute inset-y-0 right-0 w-9 flex items-center justify-center text-slate-400 hover:text-primary cursor-pointer">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                      </button>
                      {/* Input lịch native: ẩn khỏi layout nhưng vẫn neo dưới nút để popup mở đúng chỗ. */}
                      <input
                        ref={dobPickerRef}
                        type="date"
                        max={todayIso}
                        value={dobIso}
                        onChange={e => handleDobPicked(e.target.value)}
                        tabIndex={-1}
                        aria-hidden="true"
                        className="absolute bottom-1 right-3 w-px h-px opacity-0 pointer-events-none"
                      />
                    </div>
                    {dobError && <p className="text-[11.5px] font-semibold text-red-500">{dobError}</p>}
                  </div>
                  <div className="flex flex-col gap-1.5 w-28 shrink-0">
                    <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Giới tính *</label>
                    <div className="relative">
                      <select required value={form.gender} onChange={e => setForm(p => ({ ...p, gender: e.target.value }))} className={selectCls}>
                        <option>Nam</option>
                        <option>Nữ</option>
                        <option>Khác</option>
                      </select>
                      <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                      </span>
                    </div>
                  </div>
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Dịch vụ *</label>
                  <div className="relative">
                    <select required value={form.serviceId} onChange={e => setForm(p => ({ ...p, serviceId: e.target.value }))} className={selectCls}>
                      {services.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                    </select>
                    <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                    </span>
                  </div>
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                  <textarea rows={2} value={form.note} onChange={e => setForm(p => ({ ...p, note: e.target.value }))}
                    placeholder="Yêu cầu đặc biệt..."
                    className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none" />
                </div>
                <button type="submit" disabled={!selected || saving}
                  className="flex items-center justify-center gap-2 w-full py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer shadow-sm shadow-primary/25">
                  {saving ? (
                    <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                  )}
                  {saving ? "Đang đặt..." : "Xác nhận đặt lịch"}
                </button>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

/* ─── Follow-up due tab (bệnh nhân chờ tái khám) ──────────── */

function FollowUpDueTab({ dueList, onPickForWalkin }: {
  dueList: FollowUpDueDto[];
  onPickForWalkin: (patient: FollowUpDueDto) => void;
}) {
  const [search, setSearch] = useState("");

  const todayKey = (() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  })();

  const dueBadge = (followUpDate: string | null) => {
    if (!followUpDate) return { label: "Đang giữa liệu trình", cls: "bg-indigo-50 text-indigo-700 border-indigo-200" };
    const key = followUpDate.split("T")[0];
    if (key < todayKey) return { label: "Quá hẹn", cls: "bg-red-50 text-red-600 border-red-200" };
    if (key === todayKey) return { label: "Đến hẹn hôm nay", cls: "bg-emerald-50 text-emerald-700 border-emerald-200" };
    return { label: "Sắp tới", cls: "bg-slate-50 text-slate-500 border-slate-200" };
  };

  const filtered = dueList.filter(p =>
    !search.trim() ||
    p.patientName.toLowerCase().includes(search.toLowerCase()) ||
    (p.patientPhone ?? "").includes(search)
  );

  return (
    <div className="flex-1 min-h-0 flex flex-col gap-4">
      {/* Search + note */}
      <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/70 shadow-sm shrink-0 flex flex-col gap-3">
        <div className="flex items-center gap-3">
          <div className="relative flex-1">
            <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input
              type="text"
              placeholder="Tìm tên bệnh nhân, số điện thoại..."
              value={search}
              onChange={e => setSearch(e.target.value)}
              className={inputCls + " pl-10"}
            />
          </div>
          <span className="text-[12.5px] font-bold text-slate-400 shrink-0">{filtered.length} bệnh nhân chờ tái khám</span>
        </div>
        <p className="text-[12px] font-semibold text-slate-400">
          Bệnh nhân còn liệu trình <strong>đang thực hiện</strong> sau buổi khám trước — không cần đặt lịch lại.
          Khi bệnh nhân đến, bấm <strong>Check-in tái khám</strong>: thông tin được chuyển sang tab
          <strong> Đặt lịch tại quầy</strong> để chọn giờ/bác sĩ còn ca trống, bác sĩ khám vẫn sẽ thấy lại
          toàn bộ liệu trình đang điều trị. Nếu bệnh nhân tự đặt lịch mới thì check-in ở tab thường như
          một lần khám riêng.
        </p>
      </div>

      {/* List */}
      <div className="flex-1 min-h-0 overflow-y-auto flex flex-col gap-2.5 pr-1">
        {filtered.length === 0 ? (
          <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-2 py-16">
            <svg className="w-9 h-9 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
            </svg>
            <span className="text-[13px] font-bold text-slate-400">Không có bệnh nhân nào đang chờ tái khám.</span>
          </div>
        ) : (
          filtered.map(p => {
            const badge = dueBadge(p.followUpDate);
            const initials = p.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();
            return (
              <div key={p.originalAppointmentId} className="flex rounded-2xl border border-slate-200/70 bg-white shadow-sm overflow-hidden hover:shadow-md transition-all">
                <div className={`w-1.5 shrink-0 ${badge.label === "Quá hẹn" ? "bg-red-400" : badge.label === "Đến hẹn hôm nay" ? "bg-emerald-400" : "bg-slate-200"}`} />
                <div className="flex items-center gap-4 px-5 py-4 flex-1 min-w-0">
                  <div className={`w-11 h-11 rounded-lg flex items-center justify-center font-black text-[13px] border shrink-0 ${
                    p.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"
                  }`}>
                    {initials}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-[14.5px] font-black text-slate-900">{p.patientName}</span>
                      <span className={`px-2 py-0.5 rounded-lg text-[10.5px] font-black border ${badge.cls}`}>{badge.label}</span>
                    </div>
                    <div className="text-[12.5px] font-semibold text-slate-500 mt-0.5">
                      {p.followUpDate && (
                        <>Hẹn tái khám: <span className="font-black text-slate-700">{fmtDate(p.followUpDate)}</span>{" · "}</>
                      )}
                      Buổi gần nhất: {fmtDate(p.originalAppointmentDate)}{p.serviceName ? ` (${p.serviceName})` : ""}
                    </div>
                    {p.activePlans.length > 0 && (
                      <div className="text-[12px] font-semibold text-indigo-600 mt-0.5">
                        Đang điều trị: {p.activePlans.join(", ")}
                      </div>
                    )}
                    <div className="text-[12px] text-slate-400 font-medium mt-0.5">
                      <span className="font-mono">{p.patientPhone ?? "—"}</span>
                      {" · "}{p.dentistName}
                      {p.followUpNote && <span className="italic"> · {p.followUpNote}</span>}
                    </div>
                  </div>
                  <button
                    onClick={() => onPickForWalkin(p)}
                    className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-[13px] font-bold bg-primary text-white hover:bg-red-600 transition-all shrink-0 cursor-pointer shadow-sm shadow-primary/20"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                    </svg>
                    Check-in tái khám
                  </button>
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}

/* ─── Main page ──────────────────────────────────────────── */

export default function CheckinPage() {
  useRequireStaff();

  const [tab, setTab] = useState<"checkin" | "walkin" | "create-account" | "followup">("checkin");
  const [walkinPatient, setWalkinPatient] = useState<PatientSearchResultDto | null>(null);
  // Bệnh nhân được chọn từ tab Tái khám: mang theo buổi hẹn gốc để đặt lịch tại quầy vẫn gắn
  // được về đúng liệu trình cũ (xem WalkinTab.followUpFromAppointmentId).
  const [walkinFollowUp, setWalkinFollowUp] = useState<{ originalAppointmentId: string; serviceId?: string | null } | null>(null);
  const [followUpDue, setFollowUpDue] = useState<FollowUpDueDto[]>([]);
  const [search,    setSearch]    = useState("");
  const [selected,  setSelected]  = useState<string | null>(null);
  const [appointments, setAppointments] = useState<StaffAppointmentDto[]>([]);
  const [loadingId, setLoadingId] = useState<string | null>(null);
  // Phân biệt thao tác đang chạy để hiện spinner đúng nút (check-in, ghi nhận vắng hay hoàn tác).
  const [busyKind,  setBusyKind]  = useState<"checkin" | "noshow" | "undo" | null>(null);
  // Bước xác nhận trong app trước khi ghi nhận vắng (thay cho hộp thoại confirm mặc định).
  const [confirmingNoShow, setConfirmingNoShow] = useState(false);
  // Tương tự cho việc gỡ check-in: hậu quả khác nhau tùy nguồn lịch nên phải cho đọc trước khi bấm.
  const [confirmingUndo, setConfirmingUndo] = useState(false);
  const { toast, showToast } = useToast();

  // Date filter - default to today (local timezone)
  const [dateFilter, setDateFilter] = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  });

  // Tải toàn bộ lịch hẹn của ngày đã chọn (mọi trạng thái): danh sách chờ dựng từ các
  // lịch "Confirmed", còn nhóm đã xử lý (đã check-in / vắng mặt) lấy thẳng từ backend nên
  // không mất khi chuyển trang hay tải lại.
  const loadAppointments = useCallback(async () => {
    const [data, due] = await Promise.all([
      getStaffAppointmentsApi({ date: dateFilter }),
      getFollowUpDueApi().catch(() => []),
    ]);
    setAppointments(data);
    setFollowUpDue(due);
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

  // Đổi bệnh nhân thì đóng các bước xác nhận đang mở dở.
  useEffect(() => { setConfirmingNoShow(false); setConfirmingUndo(false); }, [selected]);

  const waiting = appointments.filter(p => p.status === "Confirmed");

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

  // Nhóm đã xử lý — nguồn từ backend nên tồn tại qua điều hướng. Áp cùng bộ lọc tìm kiếm.
  const matchesSearch = (p: StaffAppointmentDto) =>
    !search.trim() ||
    p.patientName.toLowerCase().includes(search.toLowerCase()) ||
    (p.patientPhone ?? "").includes(search);
  const arrived = appointments
    .filter(p => ARRIVED_STATUSES.includes(p.status) && matchesSearch(p))
    .sort((a, b) => (b.checkedInAt ?? b.appointmentDate).localeCompare(a.checkedInAt ?? a.appointmentDate));
  const absentee = appointments
    .filter(p => p.status === "NoShow" && matchesSearch(p))
    .sort((a, b) => a.appointmentDate.localeCompare(b.appointmentDate));

  const patient = appointments.find(p => p.appointmentId === selected);

  const doCheckin = async (appt: StaffAppointmentDto) => {
    setLoadingId(appt.appointmentId);
    setBusyKind("checkin");
    try {
      await checkInAppointmentApi(appt.appointmentId);
      await loadAppointments();
      setSelected(null);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Check-in thất bại. Vui lòng thử lại.", "error");
    } finally {
      setLoadingId(null);
      setBusyKind(null);
    }
  };

  const doMarkNoShow = async (appt: StaffAppointmentDto) => {
    setLoadingId(appt.appointmentId);
    setBusyKind("noshow");
    try {
      await markNoShowAppointmentApi(appt.appointmentId);
      await loadAppointments();
      setSelected(null);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Ghi nhận vắng thất bại. Vui lòng thử lại.", "error");
    } finally {
      setLoadingId(null);
      setBusyKind(null);
    }
  };

  const doUndoCheckin = async (appt: StaffAppointmentDto) => {
    setLoadingId(appt.appointmentId);
    setBusyKind("undo");
    try {
      await undoCheckInAppointmentApi(appt.appointmentId);
      await loadAppointments();
      setConfirmingUndo(false);
      setSelected(null);
      showToast(`Đã hoàn tác check-in. Lịch của ${appt.patientName} quay về danh sách chờ check-in.`);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Hoàn tác check-in thất bại. Vui lòng thử lại.", "error");
    } finally {
      setLoadingId(null);
      setBusyKind(null);
    }
  };

  const doUndoNoShow = async (appt: StaffAppointmentDto) => {
    setLoadingId(appt.appointmentId);
    setBusyKind("undo");
    try {
      await undoNoShowAppointmentApi(appt.appointmentId);
      await loadAppointments();
      setConfirmingUndo(false);
      setSelected(null);
      showToast(`Đã hoàn tác vắng mặt. Lịch của ${appt.patientName} quay về danh sách chờ check-in.`);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Hoàn tác vắng mặt thất bại. Vui lòng thử lại.", "error");
    } finally {
      setLoadingId(null);
      setBusyKind(null);
    }
  };

  const totalWaiting = groupedByDate.reduce((sum, g) => sum + g.patients.length, 0);

  const renderPatientDetailCard = (p: StaffAppointmentDto) => (
    <div className="bg-white rounded-2xl border border-slate-200/70 shadow-md p-5 sm:p-7 flex flex-col gap-5">
      <div className="flex items-start gap-4">
        <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl border-2 bg-sky-50 border-sky-100 text-sky-700 flex items-center justify-center font-black text-xl sm:text-2xl shrink-0">
          {p.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 flex-wrap">
            <h2 className="text-[18px] sm:text-[20px] font-black text-slate-900 truncate">{p.patientName}</h2>
            {p.patientRelationship && (
              <span className="px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10.5px] font-black">{p.patientRelationship}</span>
            )}
          </div>
          <div className="flex items-center gap-2 sm:gap-3 mt-1 text-[12.5px] sm:text-[13px] text-slate-500 font-semibold flex-wrap">
            <span>{p.patientPhone ?? "—"}</span>
            <span className={`px-2.5 py-0.5 sm:py-1 text-[11px] font-bold rounded-lg border ${getDateBadgeColor(p.appointmentDate.split("T")[0])}`}>
              {formatDateLabel(p.appointmentDate.split("T")[0])}
            </span>
          </div>
        </div>
      </div>

      <div className="flex items-center gap-3 bg-slate-50 border border-slate-100 rounded-xl px-4 py-3">
        <div className="w-9 h-9 rounded-lg bg-white border border-slate-200 flex items-center justify-center font-black text-[12px] text-slate-500 shrink-0">
          {p.accountHolderName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
        </div>
        <div className="min-w-0">
          <div className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider">
            {p.patientRelationship ? "Người đặt hộ (chủ tài khoản)" : "Chủ tài khoản"}
          </div>
          <p className="text-[13px] font-black text-slate-800 truncate">{p.accountHolderName}</p>
          <p className="text-[12px] font-semibold text-slate-500 truncate">{p.accountHolderEmail ?? "Không có email"}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {[
          { label: "Ngày hẹn",  value: fmtDate(p.appointmentDate), icon: "M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" },
          { label: "Giờ hẹn",  value: fmtTime(p.appointmentDate), icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" },
          { label: "Dịch vụ",  value: p.serviceName ?? "Khám tổng quát", icon: "M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" },
          { label: "Bác sĩ",   value: p.dentistName, icon: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" },
          { label: "Mã lịch hẹn", value: p.appointmentCode, icon: "M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75m-.75 3h.75" },
        ].map(item => (
          <div key={item.label} className="flex items-center gap-3 p-3.5 sm:p-4 bg-slate-50 rounded-xl border border-slate-100 min-w-0">
            <div className="w-8 h-8 rounded-xl bg-white border border-slate-200 flex items-center justify-center shrink-0">
              <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d={item.icon} />
              </svg>
            </div>
            <div className="min-w-0 flex-1">
              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider truncate">{item.label}</div>
              <div className="text-[13px] sm:text-[13.5px] font-bold text-slate-800 mt-0.5 truncate">{item.value}</div>
            </div>
          </div>
        ))}
      </div>

      {p.symptoms && (
        <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-100 rounded-xl">
          <svg className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
          <div>
            <div className="text-[11.5px] font-extrabold text-amber-700 uppercase tracking-wider">Triệu chứng</div>
            <div className="text-[13.5px] font-semibold text-amber-800 mt-0.5">{p.symptoms}</div>
          </div>
        </div>
      )}

      {p.status === "Confirmed" ? (
        confirmingNoShow ? (
          <div className="flex flex-col gap-3.5 p-4 sm:p-5 bg-amber-50 border border-amber-200 rounded-2xl">
            <div className="flex items-start gap-3">
              <div className="w-9 h-9 rounded-xl bg-amber-100 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
              </div>
              <div>
                <div className="text-[14px] font-black text-amber-900">Ghi nhận bệnh nhân vắng mặt?</div>
                <div className="text-[12.5px] font-semibold text-amber-700 mt-0.5">
                  <span className="font-black">{p.patientName}</span> sẽ được đưa khỏi danh sách chờ.
                </div>
              </div>
            </div>
            <div className="flex gap-3">
              <button onClick={() => doMarkNoShow(p)}
                disabled={loadingId === p.appointmentId}
                className="flex-1 flex items-center justify-center gap-2 py-3 bg-amber-500 hover:bg-amber-600 disabled:opacity-50 text-white rounded-xl text-[14px] font-black shadow-sm shadow-amber-200 transition-all cursor-pointer">
                {loadingId === p.appointmentId && busyKind === "noshow" ? (
                  <span className="w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                ) : (
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={BAN_ICON} /></svg>
                )}
                Xác nhận vắng mặt
              </button>
              <button onClick={() => setConfirmingNoShow(false)}
                disabled={loadingId === p.appointmentId}
                className="px-5 py-3 bg-white text-slate-500 border border-slate-200 rounded-xl text-[14px] font-bold cursor-pointer hover:bg-slate-50 disabled:opacity-50 transition-all">
                Huỷ
              </button>
            </div>
          </div>
        ) : (
          <div className="flex flex-col sm:flex-row gap-3">
            <button onClick={() => doCheckin(p)}
              disabled={loadingId === p.appointmentId}
              className="flex-1 flex items-center justify-center gap-2 py-3.5 sm:py-4 bg-emerald-500 hover:bg-emerald-600 disabled:opacity-50 text-white rounded-xl text-[14.5px] sm:text-[15px] font-black shadow-sm shadow-emerald-200 transition-all cursor-pointer">
              {loadingId === p.appointmentId && busyKind === "checkin" ? (
                <span className="w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin" />
              ) : (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
              )}
              Xác nhận Check-in
            </button>
            <button onClick={() => setConfirmingNoShow(true)}
              disabled={loadingId === p.appointmentId}
              className="flex items-center justify-center gap-2 px-5 py-3.5 sm:py-4 bg-white hover:bg-amber-50 border border-amber-300 text-amber-700 disabled:opacity-50 rounded-xl text-[14.5px] sm:text-[15px] font-black transition-all cursor-pointer whitespace-nowrap">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={BAN_ICON} /></svg>
              Ghi nhận vắng
            </button>
          </div>
        )
      ) : (
        (() => {
          const cfg = PROCESSED_STATUS[p.status] ?? PROCESSED_STATUS.CheckedIn;
          // Chỉ gỡ được khi buổi khám CHƯA có thật: hoặc bác sĩ chưa gọi vào phòng (CheckedIn),
          // hoặc bệnh nhân chưa hề đến (NoShow). Từ InProgress trở đi đã có bệnh án/hóa đơn treo
          // vào lịch — server cũng chặn, đây chỉ là không mời gọi vô ích.
          const isNoShow = p.status === "NoShow";
          const canUndo = p.status === "CheckedIn" || isNoShow;
          const isWalkIn = p.origin === "WalkIn";

          if (canUndo && confirmingUndo) return (
            <div className="flex flex-col gap-3.5 p-4 sm:p-5 bg-slate-50 border border-slate-300 rounded-2xl">
              <div className="flex items-start gap-3">
                <div className="w-9 h-9 rounded-xl bg-slate-200 flex items-center justify-center shrink-0">
                  <svg className="w-5 h-5 text-slate-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 15L3 9m0 0l6-6M3 9h12a6 6 0 010 12h-3" /></svg>
                </div>
                <div>
                  <div className="text-[14px] font-black text-slate-900">
                    {isNoShow ? "Hoàn tác ghi nhận vắng mặt?" : "Hoàn tác check-in?"}
                  </div>
                  <div className="text-[12.5px] font-semibold text-slate-600 mt-1 leading-relaxed">
                    <span className="font-black">{p.patientName}</span>{" "}
                    {isNoShow
                      ? "sẽ quay lại danh sách chờ check-in, như thể chưa từng bị ghi nhận vắng mặt."
                      : "sẽ rời hàng đợi và quay về danh sách chờ check-in."}
                  </div>
                </div>
              </div>
              <div className="flex gap-3">
                <button onClick={() => (isNoShow ? doUndoNoShow(p) : doUndoCheckin(p))}
                  disabled={loadingId === p.appointmentId}
                  className="flex-1 flex items-center justify-center gap-2 py-3 bg-slate-700 hover:bg-slate-800 disabled:opacity-50 text-white rounded-xl text-[14px] font-black shadow-sm shadow-slate-200 transition-all cursor-pointer">
                  {loadingId === p.appointmentId && busyKind === "undo" ? (
                    <span className="w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                  ) : (
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 15L3 9m0 0l6-6M3 9h12a6 6 0 010 12h-3" /></svg>
                  )}
                  {isNoShow ? "Xác nhận hoàn tác" : "Xác nhận hoàn tác"}
                </button>
                <button onClick={() => setConfirmingUndo(false)}
                  disabled={loadingId === p.appointmentId}
                  className="px-5 py-3 bg-white text-slate-500 border border-slate-200 rounded-xl text-[14px] font-bold cursor-pointer hover:bg-slate-50 disabled:opacity-50 transition-all">
                  Giữ nguyên
                </button>
              </div>
            </div>
          );

          return (
            <div className="flex flex-col gap-3">
              <div className={`flex items-center gap-3 px-5 py-4 rounded-2xl border ${cfg.cls}`}>
                <div className="w-9 h-9 rounded-xl bg-white/70 flex items-center justify-center shrink-0">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={cfg.icon} /></svg>
                </div>
                <div className="min-w-0">
                  <div className="text-[14px] font-black">{cfg.label}</div>
                  {p.status !== "NoShow" && p.checkedInAt && (
                    <div className="text-[12.5px] font-semibold opacity-80">Check-in lúc {fmtTime(p.checkedInAt)}</div>
                  )}
                </div>
                {isWalkIn && (
                  <span className="ml-auto shrink-0 px-2.5 py-1 rounded-lg bg-white/70 text-[11px] font-black uppercase tracking-wider">
                    Tại quầy
                  </span>
                )}
              </div>
              {canUndo && (
                <button onClick={() => setConfirmingUndo(true)}
                  disabled={loadingId === p.appointmentId}
                  className="flex items-center justify-center gap-2 px-5 py-3 bg-white hover:bg-slate-50 border border-slate-300 text-slate-600 disabled:opacity-50 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 15L3 9m0 0l6-6M3 9h12a6 6 0 010 12h-3" /></svg>
                  {isNoShow ? "Hoàn tác vắng mặt" : "Hoàn tác check-in"}
                </button>
              )}
            </div>
          );
        })()
      )}
    </div>
  );

  return (
    // h-screen + overflow-hidden: trang không bao giờ có thanh cuộn ngoài cùng.
    // Mọi vùng cần cuộn đều tự cuộn bên trong (bảng lịch, danh sách chờ, form).
    // Chuỗi `min-h-0` bên dưới là bắt buộc: flex item mặc định có min-height:auto
    // nên sẽ nở theo nội dung và phá vỡ giới hạn chiều cao của cha.
    <div className="animate-fade-in flex min-h-screen lg:h-screen lg:overflow-hidden bg-slate-50 font-sans text-slate-800">
      <Toast toast={toast} />
      <StaffSidebar activeMenu="checkin" />
      <main className="flex-1 flex flex-col min-w-0 min-h-0">
        <StaffPageHeader
          title="Check-in Bệnh Nhân"
          subtitle="Xác nhận bệnh nhân đến khám, tạo tài khoản và đặt lịch hẹn tại quầy"
          right={
            tab === "checkin" ? (
              <div className="flex items-center gap-2 text-[12.5px] font-bold">
                <span className="px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">{totalWaiting} chờ</span>
                <span className="px-2.5 py-1.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-xl">{arrived.length} đã check-in</span>
                {absentee.length > 0 && (
                  <span className="px-2.5 py-1.5 bg-slate-100 text-slate-600 border border-slate-200 rounded-xl">{absentee.length} vắng</span>
                )}
              </div>
            ) : null
          }
        />

        <div className="p-4 sm:p-8 flex-1 min-h-0 overflow-y-auto lg:overflow-hidden flex flex-col gap-5">
          {/* Tabs */}
          <div className="flex gap-2 shrink-0 overflow-x-auto pb-1 max-w-full flex-nowrap">
            {([
              { key: "checkin",        label: "Check-in bệnh nhân" },
              { key: "walkin",         label: "Đặt lịch tại quầy"  },
              { key: "create-account", label: "Tạo tài khoản bệnh nhân" },
              { key: "followup",       label: "Tái khám"           },
            ] as const).map(t => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`flex items-center gap-2 px-5 py-2 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer border ${
                  tab === t.key ? "bg-primary text-white border-primary shadow-sm shadow-primary/20" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                }`}>
                {t.key === "create-account" && (
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                )}
                {t.label}
                {t.key === "checkin" && totalWaiting > 0 && (
                  <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${tab === t.key ? "bg-white/25 text-white" : "bg-amber-100 text-amber-700"}`}>
                    {totalWaiting}
                  </span>
                )}
                {t.key === "followup" && followUpDue.length > 0 && (
                  <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${tab === t.key ? "bg-white/25 text-white" : "bg-indigo-100 text-indigo-700"}`}>
                    {followUpDue.length}
                  </span>
                )}
              </button>
            ))}
          </div>

          {tab === "create-account" && (
            <CreatePatientAccountTab
              onAccountCreated={(patient) => {
                setWalkinPatient(patient);
                setTab("walkin");
                showToast(`Đã tạo tài khoản cho ${patient.fullName}! Vui lòng chọn khung giờ để hoàn tất.`);
              }}
              onGoToWalkin={() => setTab("walkin")}
            />
          )}

          {tab === "walkin" && (
            <WalkinTab
              selectedPatient={walkinPatient}
              onClearSelectedPatient={() => setWalkinPatient(null)}
              onGoToCreateAccount={() => setTab("create-account")}
              followUpFromAppointmentId={walkinFollowUp?.originalAppointmentId}
              followUpServiceId={walkinFollowUp?.serviceId}
              onClearFollowUp={() => setWalkinFollowUp(null)}
            />
          )}

          {tab === "followup" && (
            <FollowUpDueTab
              dueList={followUpDue}
              onPickForWalkin={(p) => {
                setWalkinPatient({
                  id: p.patientId,
                  fullName: p.patientName,
                  phoneNumber: p.patientPhone,
                  dateOfBirth: p.patientDateOfBirth ?? "",
                  gender: p.gender ?? "Khác",
                  hasAccount: false,
                });
                setWalkinFollowUp({ originalAppointmentId: p.originalAppointmentId, serviceId: p.prefillServiceId });
                setTab("walkin");
              }}
            />
          )}

          {tab === "checkin" && (
          <div className="flex flex-col lg:flex-row gap-6 flex-1 min-h-0">

            {/* Left: date filter + search + list */}
            <div className="w-full lg:w-96 flex flex-col gap-4 shrink-0 min-h-0">
              {/* Date filter */}
              <div className="bg-white px-4 py-3 rounded-2xl border border-slate-200/70 shadow-sm shrink-0">
                <label className="block text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-2">Lọc theo ngày</label>
                <input
                  type="date"
                  value={dateFilter}
                  onChange={e => setDateFilter(e.target.value)}
                  className="w-full px-3 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700"
                />
              </div>

              {/* Search */}
              <div className="bg-white px-4 py-3 rounded-2xl border border-slate-200/70 shadow-sm shrink-0">
                <div className="relative">
                  <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                  </span>
                  <input value={search} onChange={e => setSearch(e.target.value)}
                    placeholder="Tìm tên hoặc số điện thoại..."
                    className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400" />
                </div>
              </div>

              {/* Vùng cuộn duy nhất của cột trái: danh sách chờ + danh sách đã check-in */}
              <div className="flex-1 min-h-0 overflow-y-auto flex flex-col gap-4 pr-1">

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
                          <div key={p.appointmentId} className="flex flex-col gap-2">
                            <button
                              type="button"
                              onClick={() => setSelected(isActive ? null : p.appointmentId)}
                              className={`flex rounded-2xl border overflow-hidden w-full text-left transition-all hover:shadow-md cursor-pointer ${
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
                                  <div className="flex items-center gap-1.5 min-w-0">
                                    {p.patientRelationship && (
                                      <span className="shrink-0 px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500 text-[10px] font-black">{p.patientRelationship}</span>
                                    )}
                                    <div className="min-w-0 flex-1 text-[12px] text-slate-500 font-semibold truncate">{p.serviceName ?? "Khám tổng quát"}</div>
                                  </div>
                                  <div className="text-[11.5px] text-slate-400 font-medium font-mono">{p.patientPhone ?? "—"}</div>
                                </div>
                              </div>
                            </button>

                            {/* Mobile inline detail panel - ngay dưới thẻ bệnh nhân */}
                            {isActive && (
                              <div className="lg:hidden animate-fade-in">
                                {renderPatientDetailCard(p)}
                              </div>
                            )}
                          </div>
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

              {/* Đã xử lý — đã check-in (nguồn từ backend, không mất khi rời trang) */}
              {arrived.length > 0 && (
                <div className="flex flex-col gap-3 mt-2">
                  <div className="flex items-center gap-3">
                    <span className="text-[13px] font-black text-emerald-600 uppercase tracking-wider">Đã check-in</span>
                    <span className="text-[12px] font-bold text-slate-400">{arrived.length}</span>
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>
                  <div className="flex flex-col gap-2">
                    {arrived.map(p => {
                      const isActive = selected === p.appointmentId;
                      return (
                        <div key={p.appointmentId} className="flex flex-col gap-2">
                          <button onClick={() => setSelected(isActive ? null : p.appointmentId)}
                            className={`flex rounded-2xl border overflow-hidden w-full text-left cursor-pointer transition-all hover:shadow-md ${
                              isActive ? "border-primary shadow-md shadow-primary/10 bg-white" : "border-emerald-100 bg-emerald-50/60 hover:-translate-y-px"
                            }`}>
                            <div className={`w-1.5 shrink-0 ${isActive ? "bg-primary" : "bg-emerald-400"}`} />
                            <div className="flex items-center gap-3 px-4 py-3 flex-1 min-w-0">
                              <div className="w-7 h-7 rounded-full bg-emerald-100 flex items-center justify-center shrink-0">
                                <svg className="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                              </div>
                              <div className="flex-1 min-w-0">
                                <div className="text-[13px] font-bold text-emerald-900 truncate">{p.patientName}</div>
                                <div className="flex items-center gap-1.5 min-w-0">
                                  {p.patientRelationship && (
                                    <span className="shrink-0 px-1.5 py-0.5 rounded-full bg-white/70 text-emerald-700 text-[9.5px] font-black">{p.patientRelationship}</span>
                                  )}
                                  <div className="min-w-0 flex-1 text-[11.5px] text-emerald-600 font-semibold truncate">
                                    {p.checkedInAt ? `Check-in lúc ${fmtTime(p.checkedInAt)}` : "Đã check-in"}
                                  </div>
                                </div>
                              </div>
                              <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md border ${getDateBadgeColor(p.appointmentDate.split("T")[0])}`}>
                                {formatDateLabel(p.appointmentDate.split("T")[0])}
                              </span>
                            </div>
                          </button>

                          {/* Mobile inline detail panel */}
                          {isActive && (
                            <div className="lg:hidden animate-fade-in">
                              {renderPatientDetailCard(p)}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* Đã xử lý — vắng mặt (nguồn từ backend, không mất khi rời trang) */}
              {absentee.length > 0 && (
                <div className="flex flex-col gap-3 mt-2">
                  <div className="flex items-center gap-3">
                    <span className="text-[13px] font-black text-amber-600 uppercase tracking-wider">Vắng mặt</span>
                    <span className="text-[12px] font-bold text-slate-400">{absentee.length}</span>
                    <div className="flex-1 h-px bg-slate-200" />
                  </div>
                  <div className="flex flex-col gap-2">
                    {absentee.map(p => {
                      const isActive = selected === p.appointmentId;
                      return (
                        <div key={p.appointmentId} className="flex flex-col gap-2">
                          <button onClick={() => setSelected(isActive ? null : p.appointmentId)}
                            className={`flex rounded-2xl border overflow-hidden w-full text-left cursor-pointer transition-all hover:shadow-md ${
                              isActive ? "border-primary shadow-md shadow-primary/10 bg-white" : "border-amber-100 bg-amber-50/60 hover:-translate-y-px"
                            }`}>
                            <div className={`w-1.5 shrink-0 ${isActive ? "bg-primary" : "bg-amber-400"}`} />
                            <div className="flex items-center gap-3 px-4 py-3 flex-1 min-w-0">
                              <div className="w-7 h-7 rounded-full bg-amber-100 flex items-center justify-center shrink-0">
                                <svg className="w-3.5 h-3.5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" /></svg>
                              </div>
                              <div className="flex-1 min-w-0">
                                <div className="text-[13px] font-bold text-amber-900 truncate">{p.patientName}</div>
                                <div className="flex items-center gap-1.5 min-w-0">
                                  {p.patientRelationship && (
                                    <span className="shrink-0 px-1.5 py-0.5 rounded-full bg-white/70 text-amber-700 text-[9.5px] font-black">{p.patientRelationship}</span>
                                  )}
                                  <div className="min-w-0 flex-1 text-[11.5px] text-amber-600 font-semibold truncate">Vắng mặt · hẹn {fmtTime(p.appointmentDate)}</div>
                                </div>
                              </div>
                              <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md border ${getDateBadgeColor(p.appointmentDate.split("T")[0])}`}>
                                {formatDateLabel(p.appointmentDate.split("T")[0])}
                              </span>
                            </div>
                          </button>

                          {/* Mobile inline detail panel */}
                          {isActive && (
                            <div className="lg:hidden animate-fade-in">
                              {renderPatientDetailCard(p)}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              </div>
            </div>

            {/* Right: detail panel (Desktop only) */}
            <div className="hidden lg:block flex-1 min-h-0 overflow-y-auto">
              {patient ? (
                renderPatientDetailCard(patient)
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
          )}
        </div>
      </main>
    </div>
  );
}
