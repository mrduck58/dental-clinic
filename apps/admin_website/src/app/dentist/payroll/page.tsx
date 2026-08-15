"use client";

import { useState, useEffect, useCallback } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";
import {
  getMyPayrollPeriodApi,
  getMyPayrollYearlyApi,
  type MyPayrollPeriodDto,
  type MyPayrollYearlyDto,
} from "../../../lib/apiClient";

const CHART_BAR = "#0ea5e9";
const CHART_GRID = "#e2e8f0";
const CHART_AXIS_TEXT = "#94a3b8";

const STATUS_LABELS: Record<string, string> = {
  NotCreated: "Chưa tạo kỳ lương",
  Draft: "Đang tạm tính",
  Calculated: "Đã tính, chờ duyệt",
  Approved: "Đã duyệt, chờ chi trả",
  Paid: "Đã chi trả",
};

const STATUS_BADGE: Record<string, string> = {
  NotCreated: "bg-slate-50 text-slate-500 border border-slate-200",
  Draft: "bg-slate-100 text-slate-600 border border-slate-250",
  Calculated: "bg-blue-50 text-blue-700 border border-blue-200",
  Approved: "bg-indigo-50 text-indigo-700 border border-indigo-200",
  Paid: "bg-green-50 text-green-700 border border-green-200",
};

const STATUS_DOT: Record<string, string> = {
  NotCreated: "bg-slate-400",
  Draft: "bg-slate-500",
  Calculated: "bg-blue-500",
  Approved: "bg-indigo-500",
  Paid: "bg-green-500",
};

function formatCurrency(val: number): string {
  return new Intl.NumberFormat("vi-VN").format(val) + " đ";
}

function formatCompact(val: number): string {
  if (Math.abs(val) >= 1_000_000_000) return (val / 1_000_000_000).toFixed(1).replace(".0", "") + " tỷ";
  if (Math.abs(val) >= 1_000_000) return Math.round(val / 1_000_000) + " tr";
  if (Math.abs(val) >= 1_000) return Math.round(val / 1_000) + "k";
  return String(val);
}

function formatDate(iso: string | null): string {
  return iso ? new Date(iso).toLocaleDateString("vi-VN") : "—";
}

function formatDays(val: number): string {
  return Number.isInteger(val) ? String(val) : val.toFixed(1);
}

export default function DentistPayrollPage() {
  useRequireDentist();

  const [selectedMonth, setSelectedMonth] = useState(new Date().getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());
  const [period, setPeriod] = useState<MyPayrollPeriodDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [yearlyYear, setYearlyYear] = useState(new Date().getFullYear());
  const [yearly, setYearly] = useState<MyPayrollYearlyDto | null>(null);
  const [yearlyLoading, setYearlyLoading] = useState(true);
  const [hoverMonth, setHoverMonth] = useState<number | null>(null);

  const reload = useCallback(() => {
    setIsLoading(true);
    getMyPayrollPeriodApi({ year: selectedYear, month: selectedMonth })
      .then((data) => { setPeriod(data); setErrorMsg(null); })
      .catch((err) => setErrorMsg(err instanceof Error ? err.message : "Không thể tải bảng lương"))
      .finally(() => setIsLoading(false));
  }, [selectedYear, selectedMonth]);

  useEffect(() => { reload(); }, [reload]);

  useEffect(() => {
    setYearlyLoading(true);
    getMyPayrollYearlyApi(yearlyYear)
      .then(setYearly)
      .catch(() => setYearly(null))
      .finally(() => setYearlyLoading(false));
  }, [yearlyYear]);

  const item = period?.item ?? null;
  const change = item ? item.netSalary - item.previousNetSalary : 0;
  const changePercent = item && item.previousNetSalary > 0 ? (change / item.previousNetSalary) * 100 : 0;

  const previousMonthLabel =
    selectedMonth === 1 ? `12/${selectedYear - 1}` : `${selectedMonth - 1}/${selectedYear}`;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="payroll" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Bảng Lương Của Tôi"
          subtitle="Xem chi tiết lương và lịch sử chi trả hàng tháng"
        />

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          {/* Filters */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-3 flex-wrap">
            <div className="relative">
              <select
                value={selectedMonth}
                onChange={(e) => setSelectedMonth(Number(e.target.value))}
                className="px-4 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none transition-all font-semibold text-slate-600 text-[13px] pr-8 appearance-none cursor-pointer"
              >
                {Array.from({ length: 12 }, (_, i) => (
                  <option key={i + 1} value={i + 1}>Tháng {i + 1}</option>
                ))}
              </select>
              <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                </svg>
              </span>
            </div>
            <div className="relative">
              <select
                value={selectedYear}
                onChange={(e) => setSelectedYear(Number(e.target.value))}
                className="px-4 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none transition-all font-semibold text-slate-600 text-[13px] pr-8 appearance-none cursor-pointer"
              >
                {[2025, 2026, 2027].map((yr) => (
                  <option key={yr} value={yr}>Năm {yr}</option>
                ))}
              </select>
              <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                </svg>
              </span>
            </div>
          </div>

          {errorMsg && (
            <div className="bg-rose-50 border border-rose-100 text-rose-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold">
              {errorMsg}
            </div>
          )}

          {isLoading ? (
            <div className="py-16 text-center text-slate-400 font-semibold animate-pulse">Đang tải bảng lương...</div>
          ) : item ? (
            <>
              {!item.hasSalaryConfigured && (
                <div className="bg-amber-50 border border-amber-100 text-amber-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-center gap-2">
                  <svg className="w-4 h-4 text-amber-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                  <span>Bạn chưa được thiết lập lương cơ bản trong hồ sơ — liên hệ chủ phòng khám để cập nhật.</span>
                </div>
              )}

              {/* KPI cards */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Lương cơ bản</span>
                  <span className="text-2xl font-black text-slate-900 mt-1 block">{formatCurrency(item.baseSalary)}</span>
                </div>
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Phụ cấp</span>
                  <span className="text-2xl font-black text-emerald-600 mt-1 block">+{formatCurrency(item.allowance)}</span>
                </div>
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Khấu trừ</span>
                  <span className="text-2xl font-black text-rose-500 mt-1 block">-{formatCurrency(item.deduction)}</span>
                  <span className="text-[11px] text-slate-400 font-semibold block mt-0.5">
                    {item.leaveDays > 0 ? (
                      <>Nghỉ {formatDays(item.leaveDays)}/{formatDays(item.allowedLeaveDays)} ngày phép{" "}
                        {item.exceededDays > 0 && (
                          <span className="text-rose-400 font-extrabold">(vượt {formatDays(item.exceededDays)})</span>
                        )}
                      </>
                    ) : "Chưa nghỉ phép"}
                  </span>
                </div>
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Thưởng</span>
                  <span className="text-2xl font-black text-emerald-600 mt-1 block">+{formatCurrency(item.bonus)}</span>
                </div>
                <div className="bg-primary p-5 rounded-2xl shadow-md shadow-primary/20">
                  <span className="text-[11px] font-extrabold text-red-100 uppercase tracking-wider block">Thực nhận</span>
                  <span className="text-2xl font-black text-white mt-1 block">{formatCurrency(item.netSalary)}</span>
                  {item.previousNetSalary > 0 && (
                    <span className={`text-[11px] font-bold block mt-0.5 ${change > 0 ? "text-emerald-200" : change < 0 ? "text-red-100" : "text-red-100"}`}>
                      {change > 0 ? "▲" : change < 0 ? "▼" : "–"} {formatCurrency(Math.abs(change))} ({Math.abs(changePercent).toFixed(1)}%) so với tháng {previousMonthLabel}
                    </span>
                  )}
                </div>
              </div>

              {/* Status card */}
              <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between flex-wrap gap-3">
                <div className="flex items-center gap-3">
                  <span
                    className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-[12px] font-black ${STATUS_BADGE[item.status] ?? STATUS_BADGE.NotCreated}`}
                  >
                    <span className={`w-1.5 h-1.5 rounded-full ${STATUS_DOT[item.status] ?? STATUS_DOT.NotCreated}`} />
                    {STATUS_LABELS[item.status] ?? item.status}
                  </span>
                  {item.status === "Paid" && (
                    <span className="text-[12.5px] text-slate-500 font-semibold">Ngày chi trả: <span className="font-bold text-slate-700">{formatDate(item.paidAt)}</span></span>
                  )}
                </div>
                <span className="text-[12px] text-slate-400 font-semibold">Kỳ lương tháng {selectedMonth}/{selectedYear}</span>
              </div>

              {(item.status === "NotCreated" || item.status === "Draft" || item.status === "Calculated") && (
                <div className="bg-blue-50 border border-blue-100 text-blue-700 px-5 py-3 rounded-2xl text-[13px] font-semibold flex items-center gap-2">
                  <svg className="w-4 h-4 text-blue-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                  <span>Số liệu bên trên chỉ là tạm tính, có thể thay đổi cho đến khi phòng khám duyệt kỳ lương.</span>
                </div>
              )}
            </>
          ) : (
            <div className="py-16 text-center text-slate-400 font-semibold">Không có dữ liệu lương cho kỳ này.</div>
          )}

          {/* Yearly trend */}
          <section className="rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden bg-white">
            <div className="flex items-center justify-between gap-4 px-6 py-5 border-b border-slate-100 flex-wrap">
              <div>
                <span className="text-[15px] font-extrabold text-slate-900 block">Diễn biến lương 12 tháng</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Thực nhận của tôi qua từng tháng trong năm.</span>
              </div>
              <div className="relative">
                <select
                  value={yearlyYear}
                  onChange={(e) => setYearlyYear(Number(e.target.value))}
                  className="px-4 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none transition-all font-semibold text-slate-600 text-[13px] pr-8 appearance-none cursor-pointer"
                >
                  {[2025, 2026, 2027].map((yr) => (
                    <option key={yr} value={yr}>Năm {yr}</option>
                  ))}
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
            </div>

            <div className="p-5 space-y-4">
              {yearlyLoading ? (
                <div className="py-14 text-center text-slate-400 font-semibold animate-pulse">Đang tải diễn biến lương...</div>
              ) : yearly ? (
                <>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
                    <div className="bg-slate-50/70 border border-slate-100 rounded-xl px-4 py-3">
                      <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng thực nhận cả năm</span>
                      <span className="text-[16px] font-black text-slate-900 mt-1 block">{formatCurrency(yearly.totalNet)}</span>
                    </div>
                    <div className="bg-slate-50/70 border border-slate-100 rounded-xl px-4 py-3">
                      <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Trung bình / tháng</span>
                      <span className="text-[16px] font-black text-slate-900 mt-1 block">{formatCurrency(Math.round(yearly.totalNet / 12))}</span>
                    </div>
                    <div className="bg-slate-50/70 border border-slate-100 rounded-xl px-4 py-3">
                      <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Đã chi trả</span>
                      <span className="text-[16px] font-black text-slate-900 mt-1 block">{yearly.paidCount}/12 tháng</span>
                    </div>
                  </div>

                  <MyPayrollChart data={yearly} hoverMonth={hoverMonth} onHover={setHoverMonth} />
                </>
              ) : (
                <div className="py-10 text-center text-slate-400 font-semibold">Không thể tải diễn biến lương theo năm.</div>
              )}
            </div>
          </section>
        </div>
      </main>
    </div>
  );
}

function MyPayrollChart({
  data, hoverMonth, onHover,
}: {
  data: MyPayrollYearlyDto;
  hoverMonth: number | null;
  onHover: (m: number | null) => void;
}) {
  const W = 760, H = 260;
  const padL = 62, padR = 12, padT = 16, padB = 30;
  const plotW = W - padL - padR;
  const plotH = H - padT - padB;
  const band = plotW / 12;
  const barW = Math.min(24, band - 14);

  const maxValue = Math.max(...data.months.map((m) => m.netSalary), 1_000_000);
  const niceMax = (() => {
    const pow = Math.pow(10, Math.floor(Math.log10(maxValue)));
    const n = maxValue / pow;
    const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
    return step * pow;
  })();

  const y = (v: number) => padT + plotH - (v / niceMax) * plotH;
  const ticks = [0, 0.25, 0.5, 0.75, 1].map((t) => t * niceMax);
  const hovered = hoverMonth ? data.months.find((m) => m.month === hoverMonth) ?? null : null;
  const tooltipCenterPercent = hovered ? ((padL + (hovered.month - 0.5) * band) / W) * 100 : 0;

  return (
    <div className="relative">
      <svg viewBox={`0 0 ${W} ${H}`} className="w-full h-auto" role="img" aria-label={`Diễn biến lương 12 tháng năm ${data.year}`}>
        {ticks.map((t) => (
          <g key={t}>
            <line x1={padL} x2={W - padR} y1={y(t)} y2={y(t)} stroke={CHART_GRID} strokeWidth={1} />
            <text x={padL - 10} y={y(t) + 4} textAnchor="end" fontSize={11} fill={CHART_AXIS_TEXT} fontWeight={600} style={{ fontVariantNumeric: "tabular-nums" }}>
              {t === 0 ? "0" : formatCompact(t)}
            </text>
          </g>
        ))}

        {data.months.map((m, i) => {
          const x = padL + i * band + (band - barW) / 2;
          const isHover = hoverMonth === m.month;
          const barH = (m.netSalary / niceMax) * plotH;
          const baseY = padT + plotH;

          return (
            <g key={m.month}>
              {barH > 0 && (
                <rect
                  x={x} y={baseY - barH} width={barW} height={barH} rx={4}
                  fill={m.status === "Paid" ? CHART_BAR : "#bae6fd"}
                  opacity={hoverMonth === null || isHover ? 1 : 0.45}
                />
              )}
              <rect
                x={padL + i * band} y={padT} width={band} height={plotH} fill="transparent"
                onMouseEnter={() => onHover(m.month)}
                onMouseLeave={() => onHover(null)}
              />
              <text x={padL + i * band + band / 2} y={H - 10} textAnchor="middle" fontSize={11}
                fill={isHover ? "#334155" : CHART_AXIS_TEXT} fontWeight={isHover ? 800 : 600}>
                T{m.month}
              </text>
            </g>
          );
        })}
        <line x1={padL} x2={W - padR} y1={padT + plotH} y2={padT + plotH} stroke="#cbd5e1" strokeWidth={1} />
      </svg>

      <div className="flex items-center gap-5 mt-2 pl-1">
        <span className="inline-flex items-center gap-2 text-[11.5px] font-bold text-slate-500">
          <span className="w-3 h-3 rounded-sm" style={{ background: CHART_BAR }} /> Đã chi trả
        </span>
        <span className="inline-flex items-center gap-2 text-[11.5px] font-bold text-slate-500">
          <span className="w-3 h-3 rounded-sm" style={{ background: "#bae6fd" }} /> Chờ chi trả
        </span>
      </div>

      {hovered && (
        <div
          className="absolute -top-1 z-10 pointer-events-none bg-white border border-slate-200 rounded-xl shadow-xl px-3.5 py-2.5 w-[190px] max-w-[calc(100%-0.5rem)]"
          style={{
            left: `${tooltipCenterPercent}%`,
            transform: `translateX(${tooltipCenterPercent < 22 ? "0%" : tooltipCenterPercent > 78 ? "-100%" : "-50%"})`,
          }}
        >
          <span className="text-[12px] font-black text-slate-900 block">Tháng {hovered.month}/{data.year}</span>
          <div className="mt-1.5 space-y-1">
            <div className="flex items-center justify-between gap-4">
              <span className="text-[11px] font-bold text-slate-500">Thực nhận</span>
              <span className="text-[11.5px] font-black text-slate-900 tabular-nums">{formatCurrency(hovered.netSalary)}</span>
            </div>
            <div className="flex items-center justify-between gap-4">
              <span className="text-[11px] font-bold text-slate-500">Trạng thái</span>
              <span className="text-[11.5px] font-bold text-slate-600">{STATUS_LABELS[hovered.status] ?? hovered.status}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
