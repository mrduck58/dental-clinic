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
  getServicesApi,
  searchPatientsApi,
  getFollowUpDueApi,
  checkInFollowUpApi,
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

/* ─── Walk-in tab ─────────────────────────────────────────── */

const SLOT_COLORS = [
  "bg-sky-50 text-sky-700 border-sky-200",
  "bg-violet-50 text-violet-700 border-violet-200",
  "bg-rose-50 text-rose-700 border-rose-200",
  "bg-amber-50 text-amber-700 border-amber-200",
  "bg-emerald-50 text-emerald-700 border-emerald-200",
];

function WalkinTab() {
  const [schedule,  setSchedule]  = useState<StaffScheduleResponse | null>(null);
  const [services,  setServices]  = useState<ServiceDto[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [selected,  setSelected]  = useState<{
    dentistId: string; dentistName: string; room: string; time: string;
  } | null>(null);
  const [form,      setForm]      = useState({ name: "", phone: "", email: "", dob: "", gender: "Nam", serviceId: "", note: "" });
  // Xác thực email trước khi cấp tài khoản: bệnh nhân mở hộp thư, đọc mã cho lễ tân nhập lại.
  // Không có bước này thì gõ nhầm một ký tự là mật khẩu bay tới hộp thư người lạ.
  const [verifyCode, setVerifyCode] = useState("");
  const [verifySentTo, setVerifySentTo] = useState<string | null>(null);
  const [sendingCode, setSendingCode] = useState(false);
  const { toast, showToast } = useToast();
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

  useEffect(() => {
    const term = lookup.trim();
    if (term.length < 2) { setResults([]); setSearching(false); return; }

    // `cancelled` phải sống ở scope của effect, không phải trong callback setTimeout,
    // để lần gõ sau bỏ qua kết quả của request trước.
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

  const pickPatient = (p: PatientSearchResultDto) => {
    const [y, m, d] = p.dateOfBirth.split("-");
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
    setPhoneError(null);
    setDobError(null);
  };

  const unlinkPatient = () => {
    setLinked(null);
    setForm(prev => ({ ...prev, name: "", phone: "", email: "", dob: "", gender: "Nam" }));
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
      setLoadError(null);
      const [sched, svcs] = await Promise.all([
        getStaffScheduleApi(),
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
  }, []);

  useEffect(() => { void load(); }, [load]);

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
    // Dùng Date.UTC để treat components là giờ VN, rồi trừ offset +7h
    const now = new Date();
    const [h, m] = selected.time.split(":").map(Number);
    const vnMs  = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate(), h, m, 0);
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
        // Chỉ gửi khi đây là bệnh nhân MỚI: hồ sơ đã tồn tại thì backend bỏ qua, không lập tài khoản lần hai.
        patientEmail:    linked ? undefined : (form.email.trim() || undefined),
        // Chỉ gửi mã khi nó thuộc về đúng email đang nhập — sửa email sau khi gửi mã thì mã cũ
        // không còn đúng địa chỉ nào, gửi lên sẽ tạo tài khoản cho email chưa xác thực.
        emailVerificationCode:
          !linked && verifySentTo === form.email.trim() ? verifyCode.trim() || undefined : undefined,
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
      setTimeout(() => {
        setSaved(false);
        setForm(p => ({ name: "", phone: "", email: "", dob: "", gender: "Nam", serviceId: p.serviceId, note: "" }));
        setVerifyCode("");
        setVerifySentTo(null);
        setPhoneError(null);
        setDobError(null);
        setLinked(null);
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
    <div className="flex gap-6 flex-1 min-h-0">
      <Toast toast={toast} />

      {/* Availability grid */}
      <div className="flex-1 flex flex-col gap-4 min-w-0 min-h-0">
        <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col flex-1 min-h-0">
          <div className="px-5 py-3.5 border-b border-slate-100 flex items-center justify-between shrink-0">
            <h3 className="text-[14px] font-black text-slate-900">Lịch trống hôm nay</h3>
            <div className="flex items-center gap-3 text-[12px] font-bold">
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
          ) : (
            // Bảng chiếm hết chỗ trống còn lại của thẻ và tự cuộn, nhờ đó thead ghim được ở
            // đỉnh và form bên phải luôn nằm trong màn hình khi xem ca tối.
            <div className="flex-1 min-h-0 overflow-auto">
              <table className="w-full text-[12px]">
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
      <div className="w-80 shrink-0 min-h-0">
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
                  <div>
                    <div className="text-[13px] font-black text-slate-900">{selected.time} · {selected.dentistName}</div>
                    <div className="text-[12px] text-slate-500 font-semibold">{selected.room}</div>
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

              {bookError && (
                <div className="px-4 py-2.5 bg-red-50 border border-red-100 text-red-600 text-[12.5px] font-semibold rounded-xl">{bookError}</div>
              )}

              {/* Tra cứu bệnh nhân cũ */}
              {linked ? (
                <div className="flex items-center gap-2.5 p-3 bg-sky-50 border border-sky-200 rounded-xl">
                  <div className="w-8 h-8 rounded-xl bg-sky-100 flex items-center justify-center shrink-0 text-sky-700">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  </div>
                  <div className="min-w-0">
                    <div className="text-[12.5px] font-black text-sky-900 truncate">{linked.fullName}</div>
                    <div className="text-[11.5px] text-sky-600 font-semibold">Dùng lại hồ sơ đã có</div>
                  </div>
                  <button type="button" onClick={unlinkPatient} className="ml-auto text-sky-400 hover:text-sky-700 cursor-pointer shrink-0" title="Bỏ liên kết, nhập bệnh nhân mới">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                  </button>
                </div>
              ) : (
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Tìm bệnh nhân cũ</label>
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
                            <div className="text-[12.5px] font-bold text-slate-800 truncate">{p.fullName}</div>
                            <div className="text-[11px] text-slate-400 font-mono">{p.phoneNumber ?? "—"}</div>
                          </div>
                          {p.hasAccount && (
                            <span className="text-[10px] font-black px-1.5 py-0.5 rounded-md bg-emerald-50 text-emerald-600 border border-emerald-100 shrink-0">
                              Có TK
                            </span>
                          )}
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
                </div>
                {/* Chỉ hỏi email khi đây là bệnh nhân MỚI — hồ sơ đã tra cứu ra thì họ đã có sẵn
                    tài khoản (hoặc đã từ chối cung cấp email), hỏi lại chỉ làm rối lễ tân. */}
                {!linked && (
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">
                      Email <span className="text-slate-400 normal-case font-bold">(để lập tài khoản cho bệnh nhân)</span>
                    </label>
                    <input
                      type="email"
                      value={form.email}
                      onChange={e => setForm(p => ({ ...p, email: e.target.value }))}
                      placeholder="benhnhan@gmail.com"
                      className={inputCls}
                    />
                    <p className="text-[11.5px] font-semibold text-slate-400">
                      Có email thì hệ thống gửi mật khẩu đăng nhập để bệnh nhân tự đặt lịch lần sau.
                      Bỏ trống vẫn khám được bình thường.
                    </p>

                    {form.email.trim() !== "" && (
                      <div className="mt-2 rounded-xl border border-slate-200 bg-slate-50/70 p-3 flex flex-col gap-2">
                        <div className="flex items-center gap-2">
                          <input
                            value={verifyCode}
                            onChange={e => setVerifyCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                            placeholder="Mã 6 số"
                            inputMode="numeric"
                            className={`${inputCls} flex-1`}
                          />
                          <button
                            type="button"
                            disabled={sendingCode}
                            onClick={async () => {
                              const email = form.email.trim();
                              setSendingCode(true);
                              try {
                                await sendPatientEmailVerificationApi(email);
                                setVerifySentTo(email);
                                setVerifyCode("");
                                showToast(`Đã gửi mã xác thực tới ${email}. Nhờ bệnh nhân mở hộp thư và đọc mã.`);
                              } catch (err) {
                                showToast(
                                  err instanceof Error ? err.message : "Gửi mã xác thực thất bại.",
                                  "error");
                              } finally {
                                setSendingCode(false);
                              }
                            }}
                            className="px-4 py-3 rounded-xl bg-slate-800 text-white text-[13px] font-bold whitespace-nowrap cursor-pointer hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                          >
                            {sendingCode ? "Đang gửi…" : "Gửi mã"}
                          </button>
                        </div>
                        <p className="text-[11.5px] font-semibold text-slate-500">
                          {verifySentTo === form.email.trim()
                            ? "Đã gửi mã — nhờ bệnh nhân đọc lại mã trong hộp thư."
                            : "Bấm “Gửi mã” rồi nhờ bệnh nhân đọc mã trong hộp thư. Chưa xác thực thì vẫn khám được, chỉ chưa có tài khoản."}
                        </p>
                      </div>
                    )}
                  </div>
                )}
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

function FollowUpDueTab({ dueList, onCheckedIn }: {
  dueList: FollowUpDueDto[];
  onCheckedIn: () => Promise<void>;
}) {
  const [search, setSearch] = useState("");
  const [busyId, setBusyId] = useState<string | null>(null);
  const [message, setMessage] = useState<{ text: string; ok: boolean } | null>(null);

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

  const doCheckIn = async (p: FollowUpDueDto) => {
    setBusyId(p.originalAppointmentId);
    setMessage(null);
    try {
      await checkInFollowUpApi(p.originalAppointmentId);
      setMessage({ text: `Đã check-in tái khám cho ${p.patientName} — bệnh nhân đã vào hàng đợi của ${p.dentistName}.`, ok: true });
      await onCheckedIn();
    } catch (err) {
      setMessage({ text: err instanceof Error ? err.message : "Check-in tái khám thất bại.", ok: false });
    } finally {
      setBusyId(null);
    }
  };

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
          Khi bệnh nhân đến, bấm <strong>Check-in tái khám</strong>: bệnh nhân vào thẳng hàng đợi của bác sĩ cũ
          và bác sĩ sẽ thấy lại toàn bộ liệu trình đang điều trị. Nếu bệnh nhân tự đặt lịch mới thì check-in
          ở tab thường như một lần khám riêng.
        </p>
        {message && (
          <div className={`px-4 py-2.5 rounded-xl text-[13px] font-bold border ${message.ok ? "bg-emerald-50 text-emerald-700 border-emerald-200" : "bg-red-50 text-red-600 border-red-200"}`}>
            {message.text}
          </div>
        )}
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
                    onClick={() => void doCheckIn(p)}
                    disabled={busyId !== null}
                    className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-[13px] font-bold bg-primary text-white hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed transition-all shrink-0 cursor-pointer shadow-sm shadow-primary/20"
                  >
                    {busyId === p.originalAppointmentId ? (
                      <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                      </svg>
                    ) : (
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                      </svg>
                    )}
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

  const [tab,       setTab]       = useState<"checkin"|"walkin"|"followup">("checkin");
  const [followUpDue, setFollowUpDue] = useState<FollowUpDueDto[]>([]);
  const [search,    setSearch]    = useState("");
  const [selected,  setSelected]  = useState<string | null>(null);
  const [appointments, setAppointments] = useState<StaffAppointmentDto[]>([]);
  const [loadingId, setLoadingId] = useState<string | null>(null);
  // Phân biệt thao tác đang chạy để hiện spinner đúng nút (check-in hay ghi nhận vắng).
  const [busyKind,  setBusyKind]  = useState<"checkin" | "noshow" | null>(null);
  // Bước xác nhận trong app trước khi ghi nhận vắng (thay cho hộp thoại confirm mặc định).
  const [confirmingNoShow, setConfirmingNoShow] = useState(false);
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

  // Đổi bệnh nhân thì đóng bước xác nhận vắng đang mở dở.
  useEffect(() => { setConfirmingNoShow(false); }, [selected]);

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

  const totalWaiting = groupedByDate.reduce((sum, g) => sum + g.patients.length, 0);

  return (
    // h-screen + overflow-hidden: trang không bao giờ có thanh cuộn ngoài cùng.
    // Mọi vùng cần cuộn đều tự cuộn bên trong (bảng lịch, danh sách chờ, form).
    // Chuỗi `min-h-0` bên dưới là bắt buộc: flex item mặc định có min-height:auto
    // nên sẽ nở theo nội dung và phá vỡ giới hạn chiều cao của cha.
    <div className="animate-fade-in flex h-screen overflow-hidden bg-slate-50 font-sans text-slate-800">
      <Toast toast={toast} />
      <StaffSidebar activeMenu="checkin" />
      <main className="flex-1 flex flex-col min-w-0 min-h-0">
        <StaffPageHeader
          title="Check-in Bệnh Nhân"
          subtitle="Xác nhận bệnh nhân đến khám và tạo lịch hẹn tại quầy"
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

        <div className="p-8 flex-1 min-h-0 overflow-hidden flex flex-col gap-5">
          {/* Tabs */}
          <div className="flex gap-2 shrink-0">
            {([
              { key: "checkin",  label: "Check-in bệnh nhân" },
              { key: "walkin",   label: "Đặt lịch tại quầy"  },
              { key: "followup", label: "Tái khám"           },
            ] as const).map(t => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`flex items-center gap-2 px-5 py-2 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer border ${
                  tab === t.key ? "bg-primary text-white border-primary shadow-sm shadow-primary/20" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                }`}>
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

          {tab === "walkin" && <WalkinTab />}

          {tab === "followup" && <FollowUpDueTab dueList={followUpDue} onCheckedIn={loadAppointments} />}

          {tab === "checkin" && (
          <div className="flex gap-6 flex-1 min-h-0">

            {/* Left: date filter + search + list */}
            <div className="w-96 flex flex-col gap-4 shrink-0 min-h-0">
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
                          <button key={p.appointmentId} onClick={() => setSelected(p.appointmentId)}
                            className={`flex rounded-2xl border overflow-hidden w-full text-left transition-all hover:shadow-md ${
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
                                <div className="text-[12px] text-slate-500 font-semibold truncate">{p.serviceName ?? "Khám tổng quát"}</div>
                                <div className="text-[11.5px] text-slate-400 font-medium font-mono">{p.patientPhone ?? "—"}</div>
                              </div>
                            </div>
                          </button>
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
                    {arrived.map(p => (
                      <button key={p.appointmentId} onClick={() => setSelected(p.appointmentId)}
                        className={`flex rounded-2xl border overflow-hidden w-full text-left cursor-pointer transition-all hover:shadow-md ${
                          selected === p.appointmentId ? "border-primary shadow-md shadow-primary/10 bg-white" : "border-emerald-100 bg-emerald-50/60 hover:-translate-y-px"
                        }`}>
                        <div className={`w-1.5 shrink-0 ${selected === p.appointmentId ? "bg-primary" : "bg-emerald-400"}`} />
                        <div className="flex items-center gap-3 px-4 py-3 flex-1 min-w-0">
                          <div className="w-7 h-7 rounded-full bg-emerald-100 flex items-center justify-center shrink-0">
                            <svg className="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                          </div>
                          <div className="flex-1 min-w-0">
                            <div className="text-[13px] font-bold text-emerald-900 truncate">{p.patientName}</div>
                            <div className="text-[11.5px] text-emerald-600 font-semibold">
                              {p.checkedInAt ? `Check-in lúc ${fmtTime(p.checkedInAt)}` : "Đã check-in"}
                            </div>
                          </div>
                          <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md border ${getDateBadgeColor(p.appointmentDate.split("T")[0])}`}>
                            {formatDateLabel(p.appointmentDate.split("T")[0])}
                          </span>
                        </div>
                      </button>
                    ))}
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
                    {absentee.map(p => (
                      <button key={p.appointmentId} onClick={() => setSelected(p.appointmentId)}
                        className={`flex rounded-2xl border overflow-hidden w-full text-left cursor-pointer transition-all hover:shadow-md ${
                          selected === p.appointmentId ? "border-primary shadow-md shadow-primary/10 bg-white" : "border-amber-100 bg-amber-50/60 hover:-translate-y-px"
                        }`}>
                        <div className={`w-1.5 shrink-0 ${selected === p.appointmentId ? "bg-primary" : "bg-amber-400"}`} />
                        <div className="flex items-center gap-3 px-4 py-3 flex-1 min-w-0">
                          <div className="w-7 h-7 rounded-full bg-amber-100 flex items-center justify-center shrink-0">
                            <svg className="w-3.5 h-3.5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" /></svg>
                          </div>
                          <div className="flex-1 min-w-0">
                            <div className="text-[13px] font-bold text-amber-900 truncate">{p.patientName}</div>
                            <div className="text-[11.5px] text-amber-600 font-semibold">Vắng mặt · hẹn {fmtTime(p.appointmentDate)}</div>
                          </div>
                          <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md border ${getDateBadgeColor(p.appointmentDate.split("T")[0])}`}>
                            {formatDateLabel(p.appointmentDate.split("T")[0])}
                          </span>
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              )}

              </div>
            </div>

            {/* Right: detail panel */}
            <div className="flex-1 min-h-0 overflow-y-auto">
              {patient ? (
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-7 flex flex-col gap-5">
                  <div className="flex items-start gap-4">
                    <div className="w-16 h-16 rounded-2xl border-2 bg-sky-50 border-sky-100 text-sky-700 flex items-center justify-center font-black text-2xl shrink-0">
                      {patient.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                    </div>
                    <div>
                      <h2 className="text-[20px] font-black text-slate-900">{patient.patientName}</h2>
                      <div className="flex items-center gap-3 mt-1 text-[13px] text-slate-500 font-semibold flex-wrap">
                        <span>{patient.patientPhone ?? "—"}</span>
                        <span className={`px-2.5 py-1 text-[11px] font-bold rounded-lg border ${getDateBadgeColor(patient.appointmentDate.split("T")[0])}`}>
                          {formatDateLabel(patient.appointmentDate.split("T")[0])}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    {[
                      { label: "Ngày hẹn",  value: fmtDate(patient.appointmentDate), icon: "M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" },
                      { label: "Giờ hẹn",  value: fmtTime(patient.appointmentDate), icon: "M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z"          },
                      { label: "Dịch vụ",  value: patient.serviceName ?? "Khám tổng quát", icon: "M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" },
                      { label: "Bác sĩ",   value: patient.dentistName, icon: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" },
                      { label: "Mã lịch hẹn", value: patient.appointmentCode, icon: "M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75m-.75 3h.75" },
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

                  {patient.symptoms && (
                    <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-100 rounded-xl">
                      <svg className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                      <div>
                        <div className="text-[11.5px] font-extrabold text-amber-700 uppercase tracking-wider">Triệu chứng</div>
                        <div className="text-[13.5px] font-semibold text-amber-800 mt-0.5">{patient.symptoms}</div>
                      </div>
                    </div>
                  )}

                  {patient.status === "Confirmed" ? (
                    confirmingNoShow ? (
                      /* Xác nhận vắng mặt trong app — thay cho hộp thoại confirm mặc định */
                      <div className="flex flex-col gap-3.5 p-5 bg-amber-50 border border-amber-200 rounded-2xl">
                        <div className="flex items-start gap-3">
                          <div className="w-9 h-9 rounded-xl bg-amber-100 flex items-center justify-center shrink-0">
                            <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                          </div>
                          <div>
                            <div className="text-[14px] font-black text-amber-900">Ghi nhận bệnh nhân vắng mặt?</div>
                            <div className="text-[12.5px] font-semibold text-amber-700 mt-0.5">
                              <span className="font-black">{patient.patientName}</span> sẽ được đưa khỏi danh sách chờ.
                            </div>
                          </div>
                        </div>
                        <div className="flex gap-3">
                          <button onClick={() => doMarkNoShow(patient)}
                            disabled={loadingId === patient.appointmentId}
                            className="flex-1 flex items-center justify-center gap-2 py-3 bg-amber-500 hover:bg-amber-600 disabled:opacity-50 text-white rounded-xl text-[14px] font-black shadow-sm shadow-amber-200 transition-all cursor-pointer">
                            {loadingId === patient.appointmentId && busyKind === "noshow" ? (
                              <span className="w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                            ) : (
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={BAN_ICON} /></svg>
                            )}
                            Xác nhận vắng mặt
                          </button>
                          <button onClick={() => setConfirmingNoShow(false)}
                            disabled={loadingId === patient.appointmentId}
                            className="px-5 py-3 bg-white text-slate-500 border border-slate-200 rounded-xl text-[14px] font-bold cursor-pointer hover:bg-slate-50 disabled:opacity-50 transition-all">
                            Huỷ
                          </button>
                        </div>
                      </div>
                    ) : (
                      <div className="flex gap-3">
                        <button onClick={() => doCheckin(patient)}
                          disabled={loadingId === patient.appointmentId}
                          className="flex-1 flex items-center justify-center gap-2 py-4 bg-emerald-500 hover:bg-emerald-600 disabled:opacity-50 text-white rounded-xl text-[15px] font-black shadow-sm shadow-emerald-200 transition-all cursor-pointer">
                          {loadingId === patient.appointmentId && busyKind === "checkin" ? (
                            <span className="w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                          ) : (
                            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
                          )}
                          Xác nhận Check-in
                        </button>
                        <button onClick={() => setConfirmingNoShow(true)}
                          disabled={loadingId === patient.appointmentId}
                          className="flex items-center justify-center gap-2 px-5 py-4 bg-white hover:bg-amber-50 border border-amber-300 text-amber-700 disabled:opacity-50 rounded-xl text-[15px] font-black transition-all cursor-pointer whitespace-nowrap">
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={BAN_ICON} /></svg>
                          Ghi nhận vắng
                        </button>
                      </div>
                    )
                  ) : (
                    /* Lịch đã xử lý — hiện trạng thái, không còn nút thao tác */
                    (() => {
                      const cfg = PROCESSED_STATUS[patient.status] ?? PROCESSED_STATUS.CheckedIn;
                      return (
                        <div className={`flex items-center gap-3 px-5 py-4 rounded-2xl border ${cfg.cls}`}>
                          <div className="w-9 h-9 rounded-xl bg-white/70 flex items-center justify-center shrink-0">
                            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={cfg.icon} /></svg>
                          </div>
                          <div>
                            <div className="text-[14px] font-black">{cfg.label}</div>
                            {patient.status !== "NoShow" && patient.checkedInAt && (
                              <div className="text-[12.5px] font-semibold opacity-80">Check-in lúc {fmtTime(patient.checkedInAt)}</div>
                            )}
                          </div>
                        </div>
                      );
                    })()
                  )}
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
          )}
        </div>
      </main>
    </div>
  );
}
