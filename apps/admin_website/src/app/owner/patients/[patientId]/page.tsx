"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter, useParams } from "next/navigation";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import { getPatientDetailApi, type PatientDetailDto } from "../../../../lib/apiClient";

const STATUS_CFG: Record<string, { label: string; badge: string; dot: string }> = {
  Pending:        { label: "Chờ xác nhận",  badge: "bg-amber-50 text-amber-700 border-amber-200",   dot: "bg-amber-500"  },
  Confirmed:      { label: "Đã xác nhận",   badge: "bg-sky-50 text-sky-700 border-sky-200",         dot: "bg-sky-500"    },
  CheckedIn:      { label: "Đã check-in",   badge: "bg-teal-50 text-teal-700 border-teal-200",      dot: "bg-teal-500"   },
  InProgress:     { label: "Đang khám",     badge: "bg-violet-50 text-violet-700 border-violet-200",dot: "bg-violet-500" },
  PendingPayment: { label: "Chờ thanh toán",badge: "bg-orange-50 text-orange-700 border-orange-200",dot: "bg-orange-500" },
  Completed:      { label: "Đã hoàn thành", badge: "bg-green-50 text-green-700 border-green-200",   dot: "bg-green-500"  },
  Cancelled:      { label: "Đã hủy",        badge: "bg-slate-100 text-slate-500 border-slate-200",  dot: "bg-slate-400"  },
  NoShow:         { label: "Không đến",     badge: "bg-rose-50 text-rose-700 border-rose-200",      dot: "bg-rose-500"   },
};

const PAYMENT_CFG: Record<string, { label: string; badge: string }> = {
  Paid:     { label: "Đã thanh toán",  badge: "bg-green-50 text-green-700 border-green-200" },
  Unpaid:   { label: "Chưa thanh toán",badge: "bg-amber-50 text-amber-700 border-amber-200" },
  Refunded: { label: "Đã hoàn tiền",   badge: "bg-slate-100 text-slate-500 border-slate-200" },
};

function formatDateTime(iso: string): { date: string; time: string } {
  const d = new Date(iso);
  const date = `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
  const time = `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
  return { date, time };
}

function formatDob(iso: string | null): string {
  if (!iso) return "—";
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
}

function calcAge(iso: string | null): number | null {
  if (!iso) return null;
  const dob = new Date(iso);
  const now = new Date();
  let age = now.getFullYear() - dob.getFullYear();
  const beforeBirthday = now.getMonth() < dob.getMonth() || (now.getMonth() === dob.getMonth() && now.getDate() < dob.getDate());
  if (beforeBirthday) age--;
  return age;
}

function formatCurrency(val: number): string {
  return new Intl.NumberFormat("vi-VN").format(val) + " đ";
}

function getInitials(name: string): string {
  return name.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase();
}

export default function OwnerPatientDetailPage() {
  useRequireOwner();
  const router = useRouter();
  const params = useParams();
  const patientId = params?.patientId as string;

  const [patient, setPatient] = useState<PatientDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const reload = useCallback(() => {
    if (!patientId) return;
    setIsLoading(true);
    getPatientDetailApi(patientId)
      .then((data) => { setPatient(data); setErrorMsg(null); })
      .catch((err) => setErrorMsg(err instanceof Error ? err.message : "Không thể tải thông tin bệnh nhân"))
      .finally(() => setIsLoading(false));
  }, [patientId]);

  useEffect(() => { reload(); }, [reload]);

  const age = patient ? calcAge(patient.dateOfBirth) : null;

  const stats = patient ? {
    total: patient.appointments.length,
    completed: patient.appointments.filter((a) => a.status === "Completed").length,
    active: patient.appointments.filter((a) => a.status === "InProgress" || a.status === "CheckedIn").length,
    unpaid: patient.appointments.filter((a) => a.paymentStatus === "Unpaid").length,
  } : null;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="appointments" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader
          title="Chi Tiết Bệnh Nhân"
          subtitle="Thông tin và lịch sử khám, điều trị, thanh toán"
          left={
            <button
              onClick={() => router.push("/owner/appointments")}
              className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </button>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {errorMsg && (
            <div className="bg-rose-50 border border-rose-100 text-rose-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold">
              {errorMsg}
            </div>
          )}

          {isLoading ? (
            <div className="py-16 text-center text-slate-400 font-semibold animate-pulse">Đang tải thông tin bệnh nhân...</div>
          ) : patient ? (
            <>
              {/* Patient info card */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col sm:flex-row items-start gap-5">
                <div className="w-16 h-16 rounded-2xl bg-red-50 text-primary flex items-center justify-center font-black text-[20px] shrink-0">
                  {getInitials(patient.fullName)}
                </div>
                <div className="flex-1 min-w-0 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-x-6 gap-y-3">
                  <div className="sm:col-span-2 lg:col-span-4">
                    <span className="text-[18px] font-black text-slate-900">{patient.fullName}</span>
                    {age !== null && <span className="text-[13px] text-slate-400 font-semibold ml-2">{age} tuổi{patient.gender ? ` · ${patient.gender}` : ""}</span>}
                  </div>
                  <div>
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Số điện thoại</span>
                    <span className="text-[13.5px] font-bold text-slate-700 font-mono">{patient.phone ?? "—"}</span>
                  </div>
                  <div>
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Email</span>
                    <span className="text-[13.5px] font-bold text-slate-700 truncate block">{patient.email ?? "—"}</span>
                  </div>
                  <div>
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Ngày sinh</span>
                    <span className="text-[13.5px] font-bold text-slate-700">{formatDob(patient.dateOfBirth)}</span>
                  </div>
                  <div>
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Địa chỉ</span>
                    <span className="text-[13.5px] font-bold text-slate-700 truncate block">{patient.address ?? "—"}</span>
                  </div>
                </div>
              </div>

              {/* Stat cards */}
              {stats && (
                <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm">
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng lịch hẹn</span>
                    <span className="text-2xl font-black text-slate-900 mt-1 block">{stats.total}</span>
                  </div>
                  <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm">
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Đã hoàn thành</span>
                    <span className="text-2xl font-black text-green-700 mt-1 block">{stats.completed}</span>
                  </div>
                  <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm">
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Đang khám</span>
                    <span className="text-2xl font-black text-violet-700 mt-1 block">{stats.active}</span>
                  </div>
                  <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm">
                    <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Còn nợ thanh toán</span>
                    <span className="text-2xl font-black text-amber-700 mt-1 block">{stats.unpaid}</span>
                  </div>
                </div>
              )}

              {/* History table */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex-1 flex flex-col">
                <div className="px-6 py-4 border-b border-slate-100">
                  <h3 className="text-[14px] font-black text-slate-900">Lịch sử khám & điều trị</h3>
                </div>
                <div className="overflow-x-auto flex-1">
                  <table className="w-full text-[13.5px] text-left border-collapse">
                    <thead>
                      <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                        <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Mã lịch hẹn</th>
                        <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Ngày giờ</th>
                        <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Nha sĩ</th>
                        <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Dịch vụ</th>
                        <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-center">Trạng thái</th>
                        <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-center">Thanh toán</th>
                        <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-right">Số tiền</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {patient.appointments.length > 0 ? (
                        patient.appointments.map((a) => {
                          const status = STATUS_CFG[a.status] ?? { label: a.status, badge: "bg-slate-100 text-slate-500 border-slate-200", dot: "bg-slate-400" };
                          const payment = a.paymentStatus ? PAYMENT_CFG[a.paymentStatus] : null;
                          const { date, time } = formatDateTime(a.appointmentDate);
                          return (
                            <tr key={a.appointmentId} className="hover:bg-slate-50/50 transition-colors">
                              <td className="px-6 py-4">
                                <span className="font-black text-primary text-[13px]">{a.appointmentCode}</span>
                              </td>
                              <td className="px-6 py-4">
                                <div className="font-bold text-slate-700">{date}</div>
                                <div className="text-[11px] text-slate-400 font-semibold font-mono">{time}</div>
                              </td>
                              <td className="px-6 py-4 font-bold text-slate-700">{a.dentistName}</td>
                              <td className="px-6 py-4 font-semibold text-slate-600">{a.serviceName ?? "—"}</td>
                              <td className="px-6 py-4 text-center">
                                <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-black border ${status.badge}`}>
                                  <span className={`w-1.5 h-1.5 rounded-full ${status.dot} ${a.status === "InProgress" ? "animate-pulse" : ""}`} />
                                  {status.label}
                                </span>
                              </td>
                              <td className="px-6 py-4 text-center">
                                {payment ? (
                                  <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${payment.badge}`}>
                                    {payment.label}
                                  </span>
                                ) : (
                                  <span className="text-[11px] text-slate-350 font-semibold">Chưa xuất hóa đơn</span>
                                )}
                              </td>
                              <td className="px-6 py-4 text-right font-black text-slate-900 tabular-nums">
                                {a.totalAmount != null ? formatCurrency(a.totalAmount) : "—"}
                              </td>
                            </tr>
                          );
                        })
                      ) : (
                        <tr>
                          <td colSpan={7} className="px-6 py-16 text-center">
                            <div className="flex flex-col items-center gap-2">
                              <svg className="w-9 h-9 text-slate-355" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                              </svg>
                              <div className="font-extrabold text-[14px] text-slate-500">Bệnh nhân chưa có lịch hẹn nào.</div>
                            </div>
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </>
          ) : null}
        </div>
      </main>
    </div>
  );
}
