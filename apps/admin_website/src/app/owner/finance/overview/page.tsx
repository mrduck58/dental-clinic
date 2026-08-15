"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import {
  getFinanceOverviewApi,
  type FinanceOverviewDto,
} from "../../../../lib/apiClient";

const fmt = (n: number) => new Intl.NumberFormat("vi-VN").format(Math.round(n)) + " đ";
const formatCompact = (val: number) => {
  if (Math.abs(val) >= 1_000_000_000) return (val / 1_000_000_000).toFixed(1).replace(".0", "") + " tỷ";
  if (Math.abs(val) >= 1_000_000) return Math.round(val / 1_000_000) + " tr";
  if (Math.abs(val) >= 1_000) return Math.round(val / 1_000) + "k";
  return String(val);
};
const formatDate = (iso: string) => new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
const toISODate = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

type PeriodPreset = "today" | "week" | "month" | "quarter" | "year" | "custom";
const PERIOD_OPTIONS: { value: PeriodPreset; label: string }[] = [
  { value: "today", label: "Hôm nay" },
  { value: "week", label: "Tuần này" },
  { value: "month", label: "Tháng này" },
  { value: "quarter", label: "Quý này" },
  { value: "year", label: "Năm nay" },
  { value: "custom", label: "Tùy chọn" },
];

function presetRange(preset: PeriodPreset, today: Date): { from: Date; to: Date } {
  const startOfWeek = new Date(today);
  const dayOfWeek = (today.getDay() + 6) % 7;
  startOfWeek.setDate(today.getDate() - dayOfWeek);
  switch (preset) {
    case "today": return { from: today, to: today };
    case "week": return { from: startOfWeek, to: today };
    case "month": return { from: new Date(today.getFullYear(), today.getMonth(), 1), to: today };
    case "quarter": {
      const qStart = Math.floor(today.getMonth() / 3) * 3;
      return { from: new Date(today.getFullYear(), qStart, 1), to: today };
    }
    case "year": return { from: new Date(today.getFullYear(), 0, 1), to: today };
    default: return { from: today, to: today };
  }
}

const STATUS_CFG: Record<string, { label: string; cls: string; dot: string }> = {
  Unpaid: { label: "Chưa thu", cls: "bg-amber-50 text-amber-700 border border-amber-200", dot: "bg-amber-500" },
  Paid: { label: "Đã thu", cls: "bg-emerald-50 text-emerald-700 border border-emerald-200", dot: "bg-emerald-500" },
  Refunded: { label: "Hoàn tiền", cls: "bg-slate-100 text-slate-600 border border-slate-250", dot: "bg-slate-500" },
};

/** invertColor: dùng cho chi phí — tăng là xấu (đỏ), giảm là tốt (xanh), ngược với doanh thu/lợi nhuận. */
function GrowthBadge({ percent, invertColor = false }: { percent: number; invertColor?: boolean }) {
  const up = percent > 0;
  const down = percent < 0;
  const good = invertColor ? down : up;
  const bad = invertColor ? up : down;
  return (
    <span className={`text-[12px] font-black inline-flex items-center gap-1 ${good ? "text-emerald-600" : bad ? "text-rose-600" : "text-slate-400"}`}>
      {up ? "▲" : down ? "▼" : "–"} {Math.abs(percent).toFixed(1)}% so với kỳ trước
    </span>
  );
}

function BarPanel({ title, data }: { title: string; data: { label: string; amount: number }[] }) {
  const max = Math.max(...data.map((d) => d.amount), 1);
  return (
    <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
      <span className="text-[13.5px] font-extrabold text-slate-800 block mb-4">{title}</span>
      {data.length === 0 ? (
        <div className="py-10 text-center text-slate-400 font-semibold text-[13px]">Chưa có dữ liệu trong kỳ.</div>
      ) : (
        <div className="space-y-2.5">
          {data.map((d) => (
            <div key={d.label} className="flex items-center gap-3">
              <span className="w-28 shrink-0 text-[12px] font-bold text-slate-600 truncate" title={d.label}>{d.label}</span>
              <div className="flex-1 h-5 bg-slate-100 rounded-md overflow-hidden">
                <div className="h-full bg-primary rounded-md transition-all duration-500" style={{ width: `${Math.max(2, (d.amount / max) * 100)}%` }} />
              </div>
              <span className="w-16 shrink-0 text-right text-[12px] font-black text-slate-700 tabular-nums">{formatCompact(d.amount)}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default function OwnerFinanceOverviewPage() {
  useRequireOwner();

  const today = useMemo(() => new Date(), []);
  const [preset, setPreset] = useState<PeriodPreset>("month");
  const [customFrom, setCustomFrom] = useState(toISODate(new Date(today.getFullYear(), today.getMonth(), 1)));
  const [customTo, setCustomTo] = useState(toISODate(today));

  const { fromISO, toISO } = useMemo(() => {
    if (preset === "custom") return { fromISO: customFrom, toISO: customTo };
    const { from, to } = presetRange(preset, today);
    return { fromISO: toISODate(from), toISO: toISODate(to) };
  }, [preset, customFrom, customTo, today]);

  const [data, setData] = useState<FinanceOverviewDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      setData(await getFinanceOverviewApi(fromISO, toISO));
      setErrorMsg(null);
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : "Không thể tải tổng quan tài chính");
    } finally { setLoading(false); }
  }, [fromISO, toISO]);

  useEffect(() => { reload(); }, [reload]);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="finance-overview" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader title="Tổng Quan Tài Chính" subtitle="Doanh thu, chi phí, lương và lợi nhuận trong kỳ — tổng hợp từ các màn Doanh thu/Chi phí/Lương." />

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          {errorMsg && (
            <div className="flex items-center justify-between gap-4 px-5 py-3.5 bg-red-50 border border-red-200 rounded-xl">
              <span className="text-[13px] font-bold text-red-700">{errorMsg}</span>
              <button onClick={() => setErrorMsg(null)} className="text-red-400 hover:text-red-600 cursor-pointer">✕</button>
            </div>
          )}

          {/* Bộ lọc thời gian */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-wrap items-center gap-3">
            <div className="inline-flex rounded-xl border border-slate-200 overflow-hidden shrink-0">
              {PERIOD_OPTIONS.map((opt) => (
                <button key={opt.value} onClick={() => setPreset(opt.value)}
                  className={`px-3.5 py-2 text-[12.5px] font-black cursor-pointer transition-colors whitespace-nowrap ${
                    preset === opt.value ? "bg-primary text-white" : "bg-white text-slate-500 hover:bg-slate-50"
                  }`}>
                  {opt.label}
                </button>
              ))}
            </div>
            {preset === "custom" && (
              <div className="flex items-center gap-2.5 flex-wrap">
                <input type="date" value={customFrom} max={customTo} onChange={(e) => setCustomFrom(e.target.value)}
                  className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />
                <span className="text-[12.5px] text-slate-400 font-semibold">—</span>
                <input type="date" value={customTo} min={customFrom} onChange={(e) => setCustomTo(e.target.value)}
                  className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />
              </div>
            )}
          </div>

          {/* KPI */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-5">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng doanh thu</span>
              <span className="text-xl font-black text-slate-900 mt-1.5 block">{loading ? "…" : fmt(data?.totalRevenue ?? 0)}</span>
              {!loading && data && <div className="mt-1"><GrowthBadge percent={data.revenueGrowthPercent} /></div>}
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng chi phí</span>
              <span className="text-xl font-black text-orange-600 mt-1.5 block">{loading ? "…" : fmt(data?.totalExpense ?? 0)}</span>
              {!loading && data && <div className="mt-1"><GrowthBadge percent={data.expenseGrowthPercent} invertColor /></div>}
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng lương</span>
              <span className="text-xl font-black text-violet-600 mt-1.5 block">{loading ? "…" : fmt(data?.totalPayroll ?? 0)}</span>
            </div>
            <div className="bg-primary p-5 rounded-2xl shadow-md shadow-primary/20">
              <span className="text-[11px] font-extrabold text-red-100 uppercase tracking-wider block">Lợi nhuận</span>
              <span className="text-xl font-black text-white mt-1.5 block">{loading ? "…" : fmt(data?.profit ?? 0)}</span>
              {!loading && data && (
                <span className={`text-[12px] font-black inline-flex items-center gap-1 mt-1 ${data.profitGrowthPercent >= 0 ? "text-emerald-200" : "text-red-100"}`}>
                  {data.profitGrowthPercent >= 0 ? "▲" : "▼"} {Math.abs(data.profitGrowthPercent).toFixed(1)}% so với kỳ trước
                </span>
              )}
            </div>
          </div>

          {/* Top dịch vụ / bác sĩ */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
            <BarPanel title="Top dịch vụ" data={(data?.topServices ?? []).map((s) => ({ label: s.serviceName, amount: s.amount }))} />
            <BarPanel title="Top bác sĩ" data={(data?.topDentists ?? []).map((d) => ({ label: d.dentistName, amount: d.amount }))} />
          </div>

          {/* Giao dịch gần đây */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100">
              <span className="text-[13.5px] font-extrabold text-slate-800">Giao dịch gần đây</span>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Mã hóa đơn</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Bệnh nhân</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Dịch vụ</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Ngày</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-right">Số tiền</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-center">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {loading ? (
                    <tr><td colSpan={6} className="px-6 py-10 text-center text-slate-400 font-semibold animate-pulse">Đang tải...</td></tr>
                  ) : !data || data.recentTransactions.length === 0 ? (
                    <tr><td colSpan={6} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Chưa có giao dịch nào trong kỳ.</td></tr>
                  ) : data.recentTransactions.map((t) => {
                    const cfg = STATUS_CFG[t.status] ?? STATUS_CFG.Unpaid;
                    return (
                      <tr key={t.invoiceId} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-6 py-4 font-bold text-slate-600">{t.invoiceNumber}</td>
                        <td className="px-6 py-4 font-extrabold text-slate-900">{t.patientName}</td>
                        <td className="px-6 py-4 font-semibold text-slate-600">{t.serviceSummary}</td>
                        <td className="px-6 py-4 font-semibold text-slate-500">{formatDate(t.date)}</td>
                        <td className="px-6 py-4 text-right font-black text-slate-900">{fmt(t.amount)}</td>
                        <td className="px-6 py-4 text-center">
                          <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-black ${cfg.cls}`}>
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
          </div>
        </div>
      </main>
    </div>
  );
}
