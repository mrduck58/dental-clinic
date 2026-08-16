"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import {
  getRevenueSummaryApi,
  getRevenueTransactionsApi,
  getRevenueChartsApi,
  getPublicDentistsApi,
  getServicesApi,
  type RevenueSummaryDto,
  type RevenueTransactionDto,
  type RevenueChartsDto,
  type PublicDentistDto,
  type ServiceDto,
} from "../../../lib/apiClient";
import Pagination from "../../../components/shared/Pagination";
import { SortableTh, Th } from "../../../components/shared/TableHeader";

const fmt = (n: number) => new Intl.NumberFormat("vi-VN").format(Math.round(n)) + " đ";

const formatCompact = (val: number) => {
  if (Math.abs(val) >= 1_000_000_000) return (val / 1_000_000_000).toFixed(1).replace(".0", "") + " tỷ";
  if (Math.abs(val) >= 1_000_000) return Math.round(val / 1_000_000) + " tr";
  if (Math.abs(val) >= 1_000) return Math.round(val / 1_000) + "k";
  return String(val);
};

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });

const toISODate = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

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
  const dayOfWeek = (today.getDay() + 6) % 7; // Thứ 2 là đầu tuần
  startOfWeek.setDate(today.getDate() - dayOfWeek);

  switch (preset) {
    case "today":
      return { from: today, to: today };
    case "week":
      return { from: startOfWeek, to: today };
    case "month":
      return { from: new Date(today.getFullYear(), today.getMonth(), 1), to: today };
    case "quarter": {
      const quarterStartMonth = Math.floor(today.getMonth() / 3) * 3;
      return { from: new Date(today.getFullYear(), quarterStartMonth, 1), to: today };
    }
    case "year":
      return { from: new Date(today.getFullYear(), 0, 1), to: today };
    default:
      return { from: today, to: today };
  }
}

const STATUS_CFG: Record<string, { label: string; cls: string; dot: string }> = {
  Unpaid: { label: "Chưa thu", cls: "bg-amber-50 text-amber-700 border border-amber-200", dot: "bg-amber-500" },
  Paid: { label: "Đã thu", cls: "bg-emerald-50 text-emerald-700 border border-emerald-200", dot: "bg-emerald-500" },
  Refunded: { label: "Hoàn tiền", cls: "bg-slate-100 text-slate-600 border border-slate-250", dot: "bg-slate-500" },
};

type SortKey = "date" | "amount" | "patient" | "dentist";
type SortDir = "asc" | "desc";

const PAGE_SIZE = 15;

export default function OwnerRevenuePage() {
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

  const [summary, setSummary] = useState<RevenueSummaryDto | null>(null);
  const [summaryLoading, setSummaryLoading] = useState(true);
  const [charts, setCharts] = useState<RevenueChartsDto | null>(null);
  const [chartsLoading, setChartsLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [dentists, setDentists] = useState<PublicDentistDto[]>([]);
  const [services, setServices] = useState<ServiceDto[]>([]);

  const [searchQuery, setSearchQuery] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [dentistFilter, setDentistFilter] = useState("");
  const [serviceFilter, setServiceFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [methodFilter, setMethodFilter] = useState("");

  const [sortKey, setSortKey] = useState<SortKey>("date");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const [page, setPage] = useState(1);

  const [items, setItems] = useState<RevenueTransactionDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [listLoading, setListLoading] = useState(true);

  // Danh sách bác sĩ/dịch vụ cho dropdown lọc — tải một lần khi vào trang
  useEffect(() => {
    getPublicDentistsApi().then(setDentists).catch(() => setDentists([]));
    getServicesApi().then(setServices).catch(() => setServices([]));
  }, []);

  // Gõ tìm kiếm không gọi API ngay
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearchTerm(searchQuery.trim());
      setPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  useEffect(() => { setPage(1); }, [fromISO, toISO, dentistFilter, serviceFilter, statusFilter, methodFilter]);

  const reloadSummary = useCallback(async () => {
    setSummaryLoading(true);
    try {
      setSummary(await getRevenueSummaryApi(fromISO, toISO));
      setErrorMsg(null);
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : "Không thể tải tổng quan doanh thu");
    } finally {
      setSummaryLoading(false);
    }
  }, [fromISO, toISO]);

  const reloadCharts = useCallback(async () => {
    setChartsLoading(true);
    try {
      setCharts(await getRevenueChartsApi(fromISO, toISO));
    } catch {
      setCharts(null);
    } finally {
      setChartsLoading(false);
    }
  }, [fromISO, toISO]);

  const reloadTransactions = useCallback(async () => {
    setListLoading(true);
    try {
      const data = await getRevenueTransactionsApi({
        from: fromISO,
        to: toISO,
        dentistId: dentistFilter || undefined,
        serviceName: serviceFilter || undefined,
        status: statusFilter || undefined,
        paymentMethod: methodFilter || undefined,
        search: searchTerm || undefined,
        page,
        pageSize: PAGE_SIZE,
        sortBy: sortKey,
        sortDir,
      });
      setItems(data.items);
      setTotalCount(data.totalCount);
      setErrorMsg(null);
    } catch (err) {
      setItems([]);
      setTotalCount(0);
      setErrorMsg(err instanceof Error ? err.message : "Không thể tải danh sách giao dịch");
    } finally {
      setListLoading(false);
    }
  }, [fromISO, toISO, dentistFilter, serviceFilter, statusFilter, methodFilter, searchTerm, page, sortKey, sortDir]);

  useEffect(() => { reloadSummary(); }, [reloadSummary]);
  useEffect(() => { reloadCharts(); }, [reloadCharts]);
  useEffect(() => { reloadTransactions(); }, [reloadTransactions]);

  const toggleSort = (column: SortKey) => {
    if (sortKey === column) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(column);
      setSortDir(column === "patient" || column === "dentist" ? "asc" : "desc");
    }
    setPage(1);
  };

  const isFiltered = Boolean(dentistFilter || serviceFilter || statusFilter || methodFilter || searchTerm);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="revenue" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader
          title="Doanh Thu"
          subtitle="Tổng/đã thu/chưa thu/hoàn tiền theo kỳ, danh sách giao dịch và biểu đồ theo dịch vụ/bác sĩ."
        />

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
                <button
                  key={opt.value}
                  onClick={() => setPreset(opt.value)}
                  className={`px-3.5 py-2 text-[12.5px] font-black cursor-pointer transition-colors whitespace-nowrap ${
                    preset === opt.value ? "bg-primary text-white" : "bg-white text-slate-500 hover:bg-slate-50"
                  }`}
                >
                  {opt.label}
                </button>
              ))}
            </div>

            {preset === "custom" && (
              <div className="flex items-center gap-2.5 flex-wrap">
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">Từ ngày</span>
                <input type="date" value={customFrom} max={customTo}
                  onChange={(e) => setCustomFrom(e.target.value)}
                  className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">Đến ngày</span>
                <input type="date" value={customTo} min={customFrom}
                  onChange={(e) => setCustomTo(e.target.value)}
                  className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />
              </div>
            )}

            <span className="text-[12px] text-slate-400 font-semibold ml-auto whitespace-nowrap">
              {formatDate(fromISO)} — {formatDate(toISO)}
            </span>
          </div>

          {/* KPI Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-5">
            <div className="bg-primary p-5 rounded-2xl shadow-md shadow-primary/20">
              <span className="text-[11px] font-extrabold text-red-100 uppercase tracking-wider block">Tổng doanh thu</span>
              <span className="text-xl font-black text-white mt-1.5 block">
                {summaryLoading ? "…" : fmt(summary?.totalBilled ?? 0)}
              </span>
              <span className="text-[11.5px] font-semibold text-red-100 mt-0.5 block">Tổng đã lập hóa đơn trong kỳ</span>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Đã thu</span>
              <span className="text-xl font-black text-emerald-600 mt-1.5 block">
                {summaryLoading ? "…" : fmt(summary?.totalCollected ?? 0)}
              </span>
              <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">Tiền mặt/chuyển khoản đã nhận</span>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Chưa thu</span>
              <span className="text-xl font-black text-amber-600 mt-1.5 block">
                {summaryLoading ? "…" : fmt(summary?.totalUncollected ?? 0)}
              </span>
              <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">Còn nợ / chưa thanh toán</span>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Hoàn tiền</span>
              <span className="text-xl font-black text-slate-600 mt-1.5 block">
                {summaryLoading ? "…" : fmt(summary?.totalRefunded ?? 0)}
              </span>
              <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">Đã hoàn cho bệnh nhân</span>
            </div>
          </div>

          {/* Biểu đồ theo dịch vụ / theo bác sĩ */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
            <RevenueBarPanel
              title="Doanh thu theo dịch vụ"
              loading={chartsLoading}
              data={(charts?.byService ?? []).map((s) => ({ label: s.serviceName, amount: s.amount }))}
            />
            <RevenueBarPanel
              title="Doanh thu theo bác sĩ"
              loading={chartsLoading}
              data={(charts?.byDentist ?? []).map((d) => ({ label: d.dentistName, amount: d.amount }))}
            />
          </div>

          {/* Filters & Table */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
            <div className="p-4 flex flex-col gap-3.5 border-b border-slate-100">
              <div className="flex flex-wrap items-center gap-3">
                <div className="relative flex-1 min-w-[220px]">
                  <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                  </span>
                  <input
                    type="text"
                    placeholder="Tìm theo tên bệnh nhân, mã hóa đơn..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                  />
                </div>

                <select value={dentistFilter} onChange={(e) => setDentistFilter(e.target.value)}
                  className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                  <option value="">Tất cả bác sĩ</option>
                  {dentists.map((d) => <option key={d.id} value={d.id}>{d.fullName}</option>)}
                </select>

                <select value={serviceFilter} onChange={(e) => setServiceFilter(e.target.value)}
                  className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                  <option value="">Tất cả dịch vụ</option>
                  {services.map((s) => <option key={s.id} value={s.name}>{s.name}</option>)}
                </select>

                <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}
                  className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                  <option value="">Tất cả trạng thái</option>
                  <option value="Unpaid">Chưa thu</option>
                  <option value="Paid">Đã thu</option>
                  <option value="Refunded">Hoàn tiền</option>
                </select>

                <select value={methodFilter} onChange={(e) => setMethodFilter(e.target.value)}
                  className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                  <option value="">Mọi phương thức</option>
                  <option value="Cash">Tiền mặt</option>
                  <option value="BankTransfer">Chuyển khoản</option>
                  <option value="OnlinePayment">Thanh toán online</option>
                </select>

                {isFiltered && (
                  <button
                    onClick={() => { setSearchQuery(""); setDentistFilter(""); setServiceFilter(""); setStatusFilter(""); setMethodFilter(""); }}
                    className="text-[12.5px] font-bold text-slate-400 hover:text-primary cursor-pointer whitespace-nowrap"
                  >
                    Xóa lọc
                  </button>
                )}
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    <Th className="px-6">Mã hóa đơn</Th>
                    <SortableTh column="patient" label="Bệnh nhân" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} className="px-6" />
                    <Th className="px-6">Dịch vụ</Th>
                    <SortableTh column="dentist" label="Bác sĩ" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} className="px-6" />
                    <SortableTh column="date" label="Ngày" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} className="px-6" />
                    <Th className="px-6">Phương thức</Th>
                    <SortableTh column="amount" label="Số tiền" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" className="px-6" />
                    <Th className="px-6" align="center">Trạng thái</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {listLoading ? (
                    <tr>
                      <td colSpan={8} className="px-6 py-12 text-center text-slate-400 font-semibold animate-pulse">
                        Đang tải danh sách giao dịch...
                      </td>
                    </tr>
                  ) : items.length === 0 ? (
                    <tr>
                      <td colSpan={8} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">
                        Không có giao dịch nào khớp với bộ lọc hiện tại.
                      </td>
                    </tr>
                  ) : items.map((item) => {
                    const cfg = STATUS_CFG[item.status] ?? STATUS_CFG.Unpaid;
                    return (
                      <tr key={item.invoiceId} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-6 py-4 font-bold text-slate-600">{item.invoiceNumber}</td>
                        <td className="px-6 py-4 font-extrabold text-slate-900">{item.patientName}</td>
                        <td className="px-6 py-4 font-semibold text-slate-600">{item.serviceSummary}</td>
                        <td className="px-6 py-4 font-semibold text-slate-600">{item.dentistName}</td>
                        <td className="px-6 py-4 font-semibold text-slate-500">{formatDate(item.date)}</td>
                        <td className="px-6 py-4 font-semibold text-slate-500">{item.paymentMethod}</td>
                        <td className="px-6 py-4 text-right">
                          <div className="font-black text-slate-900">{fmt(item.amount)}</div>
                          {item.remainingAmount > 0 && (
                            <div className="text-[10.5px] text-amber-600 font-bold mt-0.5 whitespace-nowrap">
                              Còn thiếu {fmt(item.remainingAmount)} (chưa xuất HĐ)
                            </div>
                          )}
                        </td>
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

            {!listLoading && totalCount > 0 && (
              <div className="border-t border-slate-100 px-5 py-3.5 bg-slate-50/25">
                <Pagination
                  currentPage={page}
                  totalCount={totalCount}
                  pageSize={PAGE_SIZE}
                  onPageChange={setPage}
                  itemLabel="giao dịch"
                />
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

// ── Biểu đồ cột ngang top 10, vẽ tay bằng div (không có thư viện biểu đồ) ────

function RevenueBarPanel({
  title, loading, data,
}: {
  title: string;
  loading: boolean;
  data: { label: string; amount: number }[];
}) {
  const maxAmount = Math.max(...data.map((d) => d.amount), 1);

  return (
    <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
      <span className="text-[13.5px] font-extrabold text-slate-800 block mb-4">{title}</span>
      {loading ? (
        <div className="py-10 text-center text-slate-400 font-semibold text-[13px] animate-pulse">Đang tải...</div>
      ) : data.length === 0 ? (
        <div className="py-10 text-center text-slate-400 font-semibold text-[13px]">Chưa có dữ liệu trong kỳ.</div>
      ) : (
        <div className="space-y-2.5">
          {data.map((d) => (
            <div key={d.label} className="flex items-center gap-3">
              <span className="w-28 shrink-0 text-[12px] font-bold text-slate-600 truncate" title={d.label}>{d.label}</span>
              <div className="flex-1 h-5 bg-slate-100 rounded-md overflow-hidden">
                <div
                  className="h-full bg-primary rounded-md transition-all duration-500"
                  style={{ width: `${Math.max(2, (d.amount / maxAmount) * 100)}%` }}
                />
              </div>
              <span className="w-16 shrink-0 text-right text-[12px] font-black text-slate-700 tabular-nums">
                {formatCompact(d.amount)}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
