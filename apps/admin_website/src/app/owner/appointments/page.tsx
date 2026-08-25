"use client";

import { useState, useEffect, useMemo, useRef } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import PatientDetailModal from "../../../components/shared/PatientDetailModal";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import { getStaffAppointmentsApi, type StaffAppointmentDto } from "../../../lib/apiClient";
import { supabase } from "../../../lib/supabaseClient";

/* ─── Trạng thái lịch hẹn ─────────────────────────────────────────────────
   Khóa trùng đúng tên enum AppointmentStatus phía API (Pending, Confirmed,
   CheckedIn, InProgress, PendingPayment, Completed, Cancelled, NoShow). */

interface StatusCfg {
  label: string;
  badge: string;
  dot: string;
}

const STATUS_CFG: Record<string, StatusCfg> = {
  Pending:        { label: "Chờ xác nhận",   badge: "bg-amber-50 text-amber-700 border-amber-200",       dot: "bg-amber-500"   },
  Confirmed:      { label: "Đã xác nhận",    badge: "bg-sky-50 text-sky-700 border-sky-200",             dot: "bg-sky-500"     },
  CheckedIn:      { label: "Đã check-in",    badge: "bg-emerald-50 text-emerald-700 border-emerald-200", dot: "bg-emerald-500" },
  InProgress:     { label: "Đang khám",      badge: "bg-violet-50 text-violet-700 border-violet-200",    dot: "bg-violet-500"  },
  PendingPayment: { label: "Chờ thanh toán", badge: "bg-orange-50 text-orange-700 border-orange-200",    dot: "bg-orange-500"  },
  Completed:      { label: "Đã khám xong",   badge: "bg-green-50 text-green-700 border-green-200",       dot: "bg-green-500"   },
  Cancelled:      { label: "Đã hủy",         badge: "bg-slate-100 text-slate-500 border-slate-200",      dot: "bg-slate-400"   },
  NoShow:         { label: "Vắng mặt",       badge: "bg-rose-50 text-rose-700 border-rose-200",          dot: "bg-rose-500"    },
};

/** Trạng thái lạ (API thêm giá trị mới) vẫn hiện được, không nuốt mất dòng dữ liệu. */
const cfgOf = (status: string): StatusCfg =>
  STATUS_CFG[status] ?? { label: status, badge: "bg-slate-100 text-slate-500 border-slate-200", dot: "bg-slate-400" };

const STATUS_ORDER = Object.keys(STATUS_CFG);

/** Sentinel cho ô lọc dịch vụ — lịch hẹn chưa gán dịch vụ (serviceName = null). */
const NO_SERVICE = "__none__";

/* ─── Helpers ─────────────────────────────────────────────────────────────── */

const pad = (n: number) => String(n).padStart(2, "0");

const todayIso = () => {
  const d = new Date();
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};

const fmtTime = (iso: string) => {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? "—" : `${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? "—" : `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
};

const initialsOf = (name: string) =>
  name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();

/* ─── Ô thống kê kiêm nút lọc nhanh theo trạng thái ────────────────────────── */

function StatTile({ label, value, tone, active, onClick }: {
  label: string;
  value: number;
  tone: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={`text-left bg-white rounded-2xl border shadow-sm p-4 flex flex-col gap-1 transition-all cursor-pointer hover:shadow-md ${
        active ? "border-primary ring-2 ring-primary/30" : "border-slate-200/70"
      }`}
    >
      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{label}</span>
      <span className={`text-2xl font-black ${tone}`}>{value}</span>
    </button>
  );
}

/* ─── Trang ───────────────────────────────────────────────────────────────── */

export default function OwnerAppointmentsPage() {
  useRequireOwner();

  const [appointments, setAppointments] = useState<StaffAppointmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // "" = xem tất cả các ngày. Chỉ ô NGÀY lọc phía server (tham số date của API);
  // các bộ lọc còn lại chạy trên client để số liệu thống kê theo trạng thái luôn
  // phản ánh đủ toàn bộ ngày đang xem, không bị chính bộ lọc trạng thái cắt cụt.
  const [dateFilter, setDateFilter] = useState(todayIso());
  const [statusFilter, setStatusFilter] = useState("All");
  const [dentistFilter, setDentistFilter] = useState("All");
  const [serviceFilter, setServiceFilter] = useState("All");
  const [search, setSearch] = useState("");

  const [selectedPatient, setSelectedPatient] = useState<{ id: string; name: string; phone: string | null } | null>(null);

  const inFlight = useRef(false);
  // Tăng lên để buộc nạp lại cùng một ngày (bấm "Làm mới", hoặc realtime báo có thay đổi).
  const [reloadNonce, setReloadNonce] = useState(0);

  // Cờ đang tải chỉ tắt trong callback của promise, không bật đồng bộ trong thân effect: lần nạp
  // đầu đã có loading=true sẵn, còn đổi ngày / bấm làm mới thì chính hành động đó bật cờ. Nhờ vậy
  // lượt làm mới do realtime kích hoạt cập nhật tại chỗ, bảng không nháy trắng.
  useEffect(() => {
    let cancelled = false;
    inFlight.current = true;
    getStaffAppointmentsApi(dateFilter ? { date: dateFilter } : undefined)
      .then(data => { if (!cancelled) { setAppointments(data); setError(null); } })
      .catch(e => {
        if (!cancelled) setError(e instanceof Error ? e.message : "Không thể tải danh sách lịch hẹn");
      })
      .finally(() => {
        inFlight.current = false;
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, [dateFilter, reloadNonce]);

  /** Đổi ngày xem — bật cờ đang tải rồi để effect ở trên nạp lại theo ngày mới. */
  const changeDate = (value: string) => {
    setLoading(true);
    setDateFilter(value);
  };

  const reload = () => {
    setLoading(true);
    setReloadNonce(n => n + 1);
  };

  // Trạng thái đổi liên tục trong ngày (check-in, bắt đầu khám, khám xong) nên bảng tự
  // làm mới khi bảng Appointments có thay đổi, thay vì bắt chủ phòng khám bấm F5.
  useEffect(() => {
    const channel = supabase
      .channel("owner-appointments-page")
      .on("postgres_changes", { event: "*", schema: "public", table: "Appointments" }, () => {
        if (!inFlight.current) setReloadNonce(n => n + 1);
      })
      .subscribe();
    return () => { void supabase.removeChannel(channel); };
  }, []);

  /* ── Danh sách lựa chọn cho ô lọc, dựng từ chính dữ liệu đang có ────────── */

  const dentistOptions = useMemo(
    () => [...new Set(appointments.map(a => a.dentistName))].sort((a, b) => a.localeCompare(b, "vi")),
    [appointments]);

  const serviceOptions = useMemo(
    () => [...new Set(appointments.map(a => a.serviceName).filter((s): s is string => !!s))]
      .sort((a, b) => a.localeCompare(b, "vi")),
    [appointments]);

  const hasUnassignedService = useMemo(
    () => appointments.some(a => !a.serviceName), [appointments]);

  /* ── Thống kê theo trạng thái (trước khi áp bộ lọc client) ──────────────── */

  const counts = useMemo(() => {
    const c: Record<string, number> = {};
    appointments.forEach(a => { c[a.status] = (c[a.status] ?? 0) + 1; });
    return c;
  }, [appointments]);

  /* ── Lọc + sắp xếp ─────────────────────────────────────────────────────── */

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();

    const rows = appointments.filter(a => {
      if (statusFilter !== "All" && a.status !== statusFilter) return false;
      if (dentistFilter !== "All" && a.dentistName !== dentistFilter) return false;
      if (serviceFilter === NO_SERVICE) {
        if (a.serviceName) return false;
      } else if (serviceFilter !== "All" && a.serviceName !== serviceFilter) {
        return false;
      }
      if (q) {
        const haystack = [a.patientName, a.patientPhone ?? "", a.appointmentCode, a.symptoms ?? ""]
          .join(" ").toLowerCase();
        if (!haystack.includes(q)) return false;
      }
      return true;
    });

    // Xem một ngày cụ thể: xếp theo giờ hẹn tăng dần để đọc như dòng thời gian của phòng khám.
    // Xem tất cả các ngày: mới nhất lên đầu.
    return rows.sort((a, b) => {
      const ta = new Date(a.appointmentDate).getTime();
      const tb = new Date(b.appointmentDate).getTime();
      return dateFilter ? ta - tb : tb - ta;
    });
  }, [appointments, statusFilter, dentistFilter, serviceFilter, search, dateFilter]);

  const hasActiveFilter =
    statusFilter !== "All" || dentistFilter !== "All" || serviceFilter !== "All" || search.trim() !== "";

  const clearFilters = () => {
    setStatusFilter("All");
    setDentistFilter("All");
    setServiceFilter("All");
    setSearch("");
  };

  const toggleStatus = (status: string) =>
    setStatusFilter(prev => (prev === status ? "All" : status));

  const selectClass =
    "px-3 py-2 text-[12.5px] font-bold bg-white border border-slate-200 rounded-xl text-slate-700 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none cursor-pointer";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="appointments" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader
          title="Ca khám & điều trị"
          subtitle={
            dateFilter
              ? `Toàn bộ bệnh nhân trong ngày ${fmtDate(dateFilter)} và trạng thái khám hiện tại`
              : "Toàn bộ bệnh nhân của mọi ngày và trạng thái khám hiện tại"
          }
          right={
            <button
              onClick={reload}
              className="hidden sm:flex items-center gap-1.5 px-3 py-1.5 text-[12.5px] font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:border-primary hover:text-primary transition-all cursor-pointer"
            >
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992V4.356m-.001 0v4.99m0-4.99l-3.181 3.183a8.25 8.25 0 00-11.667 0L3.75 9.348m0 0V4.356m0 4.992h4.99M3.75 14.652h4.992m-4.992 0v4.992m0-4.992l3.181 3.183a8.25 8.25 0 0011.667 0l2.416-2.415m0 0h-4.99m4.99 0v4.992" />
              </svg>
              Làm mới
            </button>
          }
        />

        <div className="p-4 sm:p-8 flex-1 overflow-y-auto flex flex-col gap-5">

          {/* ── Thống kê nhanh, bấm để lọc theo trạng thái ── */}
          <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-5 gap-4">
            <StatTile
              label="Tổng lịch hẹn" value={appointments.length} tone="text-slate-900"
              active={statusFilter === "All"} onClick={() => setStatusFilter("All")}
            />
            <StatTile
              label="Đang khám" value={counts.InProgress ?? 0} tone="text-violet-600"
              active={statusFilter === "InProgress"} onClick={() => toggleStatus("InProgress")}
            />
            <StatTile
              label="Đã khám xong" value={counts.Completed ?? 0} tone="text-green-600"
              active={statusFilter === "Completed"} onClick={() => toggleStatus("Completed")}
            />
            <StatTile
              label="Đang chờ khám" value={counts.CheckedIn ?? 0} tone="text-emerald-600"
              active={statusFilter === "CheckedIn"} onClick={() => toggleStatus("CheckedIn")}
            />
            <StatTile
              label="Chờ xác nhận" value={counts.Pending ?? 0} tone="text-amber-600"
              active={statusFilter === "Pending"} onClick={() => toggleStatus("Pending")}
            />
          </div>

          {/* ── Bộ lọc ── */}
          <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm p-4 flex flex-wrap items-center gap-2.5">
            {/* Ngày */}
            <div className="flex items-center gap-1.5">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Ngày</span>
              <input
                type="date"
                value={dateFilter}
                onChange={e => changeDate(e.target.value)}
                className={selectClass}
              />
              <button
                onClick={() => changeDate(todayIso())}
                disabled={dateFilter === todayIso()}
                className="px-2.5 py-2 text-[12.5px] font-bold rounded-xl border border-primary/30 bg-primary/5 text-primary hover:bg-primary/10 disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer"
              >
                Hôm nay
              </button>
              <button
                onClick={() => changeDate("")}
                disabled={dateFilter === ""}
                className="px-2.5 py-2 text-[12.5px] font-bold rounded-xl border border-slate-200 text-slate-600 hover:border-primary hover:text-primary disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer"
              >
                Tất cả ngày
              </button>
            </div>

            <div className="h-6 w-px bg-slate-200 hidden sm:block" />

            {/* Trạng thái */}
            <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className={selectClass}>
              <option value="All">Tất cả trạng thái</option>
              {STATUS_ORDER.map(s => (
                <option key={s} value={s}>{STATUS_CFG[s].label} ({counts[s] ?? 0})</option>
              ))}
            </select>

            {/* Bác sĩ */}
            <select value={dentistFilter} onChange={e => setDentistFilter(e.target.value)} className={selectClass}>
              <option value="All">Tất cả bác sĩ</option>
              {dentistOptions.map(d => <option key={d} value={d}>{d}</option>)}
            </select>

            {/* Dịch vụ */}
            <select value={serviceFilter} onChange={e => setServiceFilter(e.target.value)} className={selectClass}>
              <option value="All">Tất cả dịch vụ</option>
              {serviceOptions.map(s => <option key={s} value={s}>{s}</option>)}
              {hasUnassignedService && <option value={NO_SERVICE}>Chưa gán dịch vụ</option>}
            </select>

            {/* Tìm kiếm */}
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Tìm tên bệnh nhân, SĐT, mã lịch hẹn..."
              className="flex-1 min-w-[200px] px-3.5 py-2 text-[12.5px] font-semibold bg-slate-100/70 border border-transparent rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all"
            />

            {hasActiveFilter && (
              <button
                onClick={clearFilters}
                className="px-3 py-2 text-[12.5px] font-bold text-slate-500 bg-slate-100 hover:bg-slate-200 rounded-xl transition-all cursor-pointer"
              >
                Xóa lọc
              </button>
            )}
          </div>

          {/* ── Bảng ── */}
          <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden flex-1">
            {loading ? (
              <div className="flex items-center justify-center py-24">
                <div className="w-8 h-8 border-[3px] border-primary/20 border-t-primary rounded-full animate-spin" />
              </div>
            ) : error ? (
              <div className="flex flex-col items-center gap-3 py-24">
                <div className="w-14 h-14 rounded-full bg-red-50 flex items-center justify-center">
                  <svg className="w-7 h-7 text-red-500" fill="none" stroke="currentColor" strokeWidth="1.8" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                  </svg>
                </div>
                <p className="text-[14px] font-black text-slate-700">{error}</p>
                <button
                  onClick={reload}
                  className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl hover:opacity-90 transition-all cursor-pointer"
                >
                  Thử lại
                </button>
              </div>
            ) : filtered.length === 0 ? (
              <div className="flex flex-col items-center gap-2.5 py-24 px-6 text-center">
                <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                  <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                  </svg>
                </div>
                <p className="text-[14px] font-black text-slate-700">
                  {appointments.length === 0 ? "Không có dữ liệu" : "Không có lịch hẹn khớp bộ lọc"}
                </p>
                <p className="text-[12.5px] font-semibold text-slate-400 max-w-md">
                  {appointments.length === 0
                    ? dateFilter
                      ? `Chưa có lịch hẹn nào trong ngày ${fmtDate(dateFilter)}.`
                      : "Hệ thống chưa có lịch hẹn nào."
                    : "Thử bỏ bớt điều kiện lọc hoặc đổi sang ngày khác."}
                </p>
                {hasActiveFilter && appointments.length > 0 && (
                  <button
                    onClick={clearFilters}
                    className="mt-1 px-4 py-2 text-[12.5px] font-black text-primary bg-primary/5 border border-primary/30 rounded-xl hover:bg-primary/10 transition-all cursor-pointer"
                  >
                    Xóa lọc
                  </button>
                )}
              </div>
            ) : (
              <>
                <div className="px-5 py-3 border-b border-slate-100 flex items-center justify-between gap-3">
                  <span className="text-[12.5px] font-bold text-slate-500">
                    Hiển thị <span className="text-slate-900 font-black">{filtered.length}</span>
                    {filtered.length !== appointments.length && <> / {appointments.length}</>} lịch hẹn
                  </span>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse min-w-[900px]">
                    <thead>
                      <tr className="bg-slate-50/70 border-b border-slate-200">
                        {["Mã lịch hẹn", "Bệnh nhân", "Dịch vụ", "Bác sĩ", "Giờ hẹn", "Check-in", "Trạng thái"].map(h => (
                          <th key={h} className="px-5 py-3 text-[11px] font-black text-slate-400 uppercase tracking-wider whitespace-nowrap">
                            {h}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {filtered.map(a => {
                        const cfg = cfgOf(a.status);
                        return (
                          <tr
                          key={a.appointmentId}
                          onClick={() => setSelectedPatient({ id: a.patientId, name: a.patientName, phone: a.patientPhone })}
                          className="hover:bg-slate-50/60 transition-colors cursor-pointer"
                        >
                            <td className="px-5 py-3.5">
                              <span className="text-[12px] font-mono font-bold text-slate-500">{a.appointmentCode}</span>
                            </td>

                            <td className="px-5 py-3.5">
                              <div className="flex items-center gap-3 min-w-0">
                                <div className="w-9 h-9 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[11px] text-sky-700 shrink-0">
                                  {initialsOf(a.patientName)}
                                </div>
                                <div className="min-w-0">
                                  <div className="text-[13.5px] font-black text-slate-900 truncate">{a.patientName}</div>
                                  <div className="text-[11.5px] font-semibold text-slate-400">
                                    {a.patientPhone ?? "Chưa có SĐT"}
                                  </div>
                                </div>
                              </div>
                            </td>

                            <td className="px-5 py-3.5 max-w-[230px]">
                              <div className={`text-[13px] font-bold truncate ${a.serviceName ? "text-slate-700" : "text-slate-400"}`}>
                                {a.serviceName ?? "Chưa gán dịch vụ"}
                              </div>
                              {a.symptoms && (
                                <div className="text-[11.5px] font-semibold text-amber-600 truncate mt-0.5" title={a.symptoms}>
                                  {a.symptoms}
                                </div>
                              )}
                            </td>

                            <td className="px-5 py-3.5">
                              <span className="text-[13px] font-bold text-slate-700 whitespace-nowrap">{a.dentistName}</span>
                            </td>

                            <td className="px-5 py-3.5 whitespace-nowrap">
                              <div className="text-[13.5px] font-black text-slate-900">{fmtTime(a.appointmentDate)}</div>
                              <div className="text-[11.5px] font-semibold text-slate-400">{fmtDate(a.appointmentDate)}</div>
                            </td>

                            <td className="px-5 py-3.5 whitespace-nowrap">
                              {a.checkedInAt ? (
                                <span className="text-[13px] font-bold text-emerald-600">{fmtTime(a.checkedInAt)}</span>
                              ) : (
                                <span className="text-[12.5px] font-semibold text-slate-300">Chưa check-in</span>
                              )}
                            </td>

                            <td className="px-5 py-3.5">
                              <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[11.5px] font-black border whitespace-nowrap ${cfg.badge}`}>
                                <span className={`w-1.5 h-1.5 rounded-full ${cfg.dot}`} />
                                {cfg.label}
                              </span>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>
        </div>
      </main>

      {selectedPatient && (
        <PatientDetailModal
          patientId={selectedPatient.id}
          patientName={selectedPatient.name}
          patientPhone={selectedPatient.phone}
          onClose={() => setSelectedPatient(null)}
        />
      )}
    </div>
  );
}
