"use client";

import { useState, useEffect, useCallback } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import {
  getOwnerRevenueReportApi,
  type OwnerRevenueReportDto,
  type OwnerRevenueIncomeItemDto,
  type OwnerRevenueExpenseItemDto,
} from "../../../lib/apiClient";
import Pagination from "../../../components/shared/Pagination";
import { SortableTh, toggleSortState, type SortDir } from "../../../components/shared/TableHeader";

const fmt = (n: number) => new Intl.NumberFormat("vi-VN").format(Math.round(n)) + " đ";

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });

const toISODate = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

type IncomeSortKey = "patient" | "invoice" | "date" | "status" | "amount";
type ExpenseSortKey = "description" | "person" | "type" | "date" | "amount";

export default function OwnerRevenuePage() {
  useRequireOwner();

  const today = new Date();
  const [fromDate, setFromDate] = useState(toISODate(new Date(today.getFullYear(), today.getMonth(), 1)));
  const [toDate, setToDate] = useState(toISODate(today));
  const [data, setData] = useState<OwnerRevenueReportDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [listTab, setListTab] = useState<"income" | "expense">("income");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [incomeSortKey, setIncomeSortKey] = useState<IncomeSortKey>("date");
  const [incomeSortDir, setIncomeSortDir] = useState<SortDir>("desc");
  const [expenseSortKey, setExpenseSortKey] = useState<ExpenseSortKey>("date");
  const [expenseSortDir, setExpenseSortDir] = useState<SortDir>("desc");

  const reload = useCallback(async (from: string, to: string) => {
    setLoading(true);
    try {
      setData(await getOwnerRevenueReportApi(from, to));
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể tải báo cáo doanh thu");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { reload(fromDate, toDate); }, [fromDate, toDate, reload]);
  useEffect(() => { setPage(1); }, [listTab, fromDate, toDate, pageSize, search]);

  const netProfit = data ? data.totalIncome - data.totalStockExpense - data.totalPayrollExpense : 0;

  const q = search.trim().toLowerCase();
  const filteredIncome: OwnerRevenueIncomeItemDto[] = data
    ? data.incomeItems.filter(i =>
        !q ||
        (i.patientName ?? "").toLowerCase().includes(q) ||
        (i.invoiceNumber ?? "").toLowerCase().includes(q))
    : [];
  const filteredExpense: OwnerRevenueExpenseItemDto[] = data
    ? data.expenseItems.filter(e =>
        !q ||
        (e.description ?? "").toLowerCase().includes(q) ||
        (e.subDescription ?? "").toLowerCase().includes(q))
    : [];

  const totalCount = listTab === "income" ? filteredIncome.length : filteredExpense.length;

  const sortedIncome = [...filteredIncome].sort((a, b) => {
    const dir = incomeSortDir === "asc" ? 1 : -1;
    const value = (it: OwnerRevenueIncomeItemDto): string | number => {
      switch (incomeSortKey) {
        case "patient": return (it.patientName ?? "").toLowerCase();
        case "invoice": return (it.invoiceNumber ?? "").toLowerCase();
        case "date":    return new Date(it.date).getTime();
        case "status":  return 0; // mọi khoản thu hiển thị đều đã thanh toán, không phân biệt được
        case "amount":  return it.amount;
      }
    };
    const va = value(a);
    const vb = value(b);
    if (typeof va === "string" && typeof vb === "string") return va.localeCompare(vb, "vi") * dir;
    return ((va as number) - (vb as number)) * dir;
  });

  const sortedExpense = [...filteredExpense].sort((a, b) => {
    const dir = expenseSortDir === "asc" ? 1 : -1;
    const value = (it: OwnerRevenueExpenseItemDto): string | number => {
      switch (expenseSortKey) {
        case "description": return (it.description ?? "").toLowerCase();
        case "person":      return (it.subDescription ?? "").toLowerCase();
        case "type":        return it.category === "supply" ? "vật tư" : "lương";
        case "date":        return new Date(it.date).getTime();
        case "amount":      return it.amount;
      }
    };
    const va = value(a);
    const vb = value(b);
    if (typeof va === "string" && typeof vb === "string") return va.localeCompare(vb, "vi") * dir;
    return ((va as number) - (vb as number)) * dir;
  });

  const toggleIncomeSort = (column: IncomeSortKey) => {
    const next = toggleSortState({ key: incomeSortKey, dir: incomeSortDir }, column, (col) => col === "date" || col === "amount");
    setIncomeSortKey(next.key);
    setIncomeSortDir(next.dir);
    setPage(1);
  };

  const toggleExpenseSort = (column: ExpenseSortKey) => {
    const next = toggleSortState({ key: expenseSortKey, dir: expenseSortDir }, column, (col) => col === "date" || col === "amount");
    setExpenseSortKey(next.key);
    setExpenseSortDir(next.dir);
    setPage(1);
  };

  const pagedIncome = sortedIncome.slice((page - 1) * pageSize, page * pageSize);
  const pagedExpense = sortedExpense.slice((page - 1) * pageSize, page * pageSize);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="revenue" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader
          title="Doanh Thu"
          subtitle="Chi tiết từng khoản thu từ bệnh nhân và chi phí vật tư / lương nhân viên."
        />

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          {error && (
            <div className="flex items-center justify-between gap-4 px-5 py-3.5 bg-red-50 border border-red-200 rounded-xl">
              <span className="text-[13px] font-bold text-red-700">{error}</span>
              <button onClick={() => setError(null)} className="text-red-400 hover:text-red-600 cursor-pointer">
                ✕
              </button>
            </div>
          )}

          {loading || !data ? (
            <div className="flex items-center justify-center py-24">
              <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : (
            <>
              {/* KPI Cards */}
              <div className="grid grid-cols-1 md:grid-cols-4 gap-5">
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng thu từ bệnh nhân</span>
                  <span className="text-xl font-black text-slate-900 mt-1.5 block">{fmt(data.totalIncome)}</span>
                  <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">{data.incomeItems.length} hóa đơn</span>
                </div>

                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng chi mua vật tư</span>
                  <span className="text-xl font-black text-slate-900 mt-1.5 block">{fmt(data.totalStockExpense)}</span>
                  <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">
                    {data.expenseItems.filter(e => e.category === "supply").length} lần nhập
                  </span>
                </div>

                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng chi lương nhân viên</span>
                  <span className="text-xl font-black text-slate-900 mt-1.5 block">{fmt(data.totalPayrollExpense)}</span>
                  <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">
                    {data.expenseItems.filter(e => e.category === "payroll").length} bảng lương
                  </span>
                </div>

                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
                  <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Lợi nhuận ròng</span>
                  <span className={`text-xl font-black mt-1.5 block ${netProfit >= 0 ? "text-emerald-600" : "text-red-600"}`}>{fmt(netProfit)}</span>
                  <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">{netProfit >= 0 ? "Có lãi" : "Đang lỗ"} trong kỳ</span>
                </div>
              </div>

              {/* Danh sách thu/chi — chuyển bằng tag, có tìm kiếm + phân trang */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
                <div className="p-4 flex flex-col gap-4 border-b border-slate-100">
                  <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3.5 flex-wrap">
                    {/* Search */}
                    <div className="relative flex-1 min-w-[240px]">
                      <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                      </span>
                      <input
                        type="text"
                        placeholder={listTab === "income" ? "Tìm theo tên bệnh nhân, mã hóa đơn..." : "Tìm theo nội dung chi..."}
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                      />
                    </div>

                    {/* Tag: Thu / Chi */}
                    <div className="flex items-center gap-1 bg-slate-50 border border-slate-200 rounded-xl p-1 shadow-sm shrink-0">
                      <button onClick={() => setListTab("income")}
                        className={`px-4 py-1.5 rounded-lg text-[13.5px] font-extrabold transition-all cursor-pointer ${
                          listTab === "income" ? "bg-primary text-white shadow-sm" : "text-slate-500 hover:text-slate-800 hover:bg-slate-200/40"
                        }`}>
                        Thu ({data.incomeItems.length})
                      </button>
                      <button onClick={() => setListTab("expense")}
                        className={`px-4 py-1.5 rounded-lg text-[13.5px] font-extrabold transition-all cursor-pointer ${
                          listTab === "expense" ? "bg-primary text-white shadow-sm" : "text-slate-500 hover:text-slate-800 hover:bg-slate-200/40"
                        }`}>
                        Chi ({data.expenseItems.length})
                      </button>
                    </div>

                    <span className="text-[12.5px] font-black shrink-0" style={{ color: listTab === "income" ? "#059669" : "#dc2626" }}>
                      {listTab === "income" ? `+${fmt(data.totalIncome)}` : `-${fmt(data.totalStockExpense + data.totalPayrollExpense)}`}
                    </span>
                  </div>

                  {/* Hiển thị X / trang + lọc theo khoảng ngày — dùng chung cho cả Thu và Chi */}
                  <div className="flex flex-wrap items-center gap-2.5">
                    <span className="text-[12.5px] text-slate-400 font-semibold">Hiển thị</span>
                    <div className="relative">
                      <select value={pageSize} onChange={e => setPageSize(Number(e.target.value))}
                        className="pl-3 pr-7 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-bold text-slate-650 appearance-none cursor-pointer">
                        {PAGE_SIZE_OPTIONS.map(n => <option key={n} value={n}>{n}</option>)}
                      </select>
                      <span className="absolute inset-y-0 right-2 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                      </span>
                    </div>
                    <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">/ trang</span>

                    <span className="w-px h-5 bg-slate-200 mx-1 shrink-0" />

                    <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">Từ ngày</span>
                    <input
                      type="date"
                      value={fromDate}
                      max={toDate}
                      onChange={e => setFromDate(e.target.value)}
                      className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-bold text-slate-650 cursor-pointer"
                    />
                    <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">Đến ngày</span>
                    <input
                      type="date"
                      value={toDate}
                      min={fromDate}
                      onChange={e => setToDate(e.target.value)}
                      className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-bold text-slate-650 cursor-pointer"
                    />
                  </div>
                </div>

                {/* Table */}
                <div className="overflow-x-auto">
                  {listTab === "income" ? (
                    <table className="w-full text-[13.5px] text-left border-collapse">
                      <thead>
                        <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                          <SortableTh column="patient" label="Bệnh nhân" sortKey={incomeSortKey} sortDir={incomeSortDir} onSort={toggleIncomeSort} className="px-6" />
                          <SortableTh column="invoice" label="Mã hóa đơn" sortKey={incomeSortKey} sortDir={incomeSortDir} onSort={toggleIncomeSort} className="px-6" />
                          <SortableTh column="date" label="Ngày thanh toán" sortKey={incomeSortKey} sortDir={incomeSortDir} onSort={toggleIncomeSort} className="px-6" />
                          <SortableTh column="status" label="Trạng thái" sortKey={incomeSortKey} sortDir={incomeSortDir} onSort={toggleIncomeSort} align="center" className="px-6" />
                          <SortableTh column="amount" label="Số tiền" sortKey={incomeSortKey} sortDir={incomeSortDir} onSort={toggleIncomeSort} align="right" className="px-6" />
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {pagedIncome.length === 0 ? (
                          <tr>
                            <td colSpan={5} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">
                              Không có khoản thu nào khớp với bộ lọc hiện tại.
                            </td>
                          </tr>
                        ) : pagedIncome.map(item => (
                          <tr key={item.invoiceId} className="hover:bg-slate-50/50 transition-colors">
                            <td className="px-6 py-4 font-extrabold text-slate-900">{item.patientName}</td>
                            <td className="px-6 py-4 font-bold text-slate-600">{item.invoiceNumber}</td>
                            <td className="px-6 py-4 font-semibold text-slate-500">{formatDate(item.date)}</td>
                            <td className="px-6 py-4 text-center">
                              <span className="inline-block px-2.5 py-1 rounded-full text-[11px] font-extrabold bg-emerald-50 text-emerald-600">
                                Đã thanh toán
                              </span>
                            </td>
                            <td className="px-6 py-4 text-right font-black text-emerald-600">+{fmt(item.amount)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  ) : (
                    <table className="w-full text-[13.5px] text-left border-collapse">
                      <thead>
                        <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                          <SortableTh column="description" label="Nội dung" sortKey={expenseSortKey} sortDir={expenseSortDir} onSort={toggleExpenseSort} className="px-6" />
                          <SortableTh column="person" label="Người đã nhập hàng" sortKey={expenseSortKey} sortDir={expenseSortDir} onSort={toggleExpenseSort} className="px-6" />
                          <SortableTh column="type" label="Loại" sortKey={expenseSortKey} sortDir={expenseSortDir} onSort={toggleExpenseSort} className="px-6" />
                          <SortableTh column="date" label="Ngày" sortKey={expenseSortKey} sortDir={expenseSortDir} onSort={toggleExpenseSort} className="px-6" />
                          <SortableTh column="amount" label="Số tiền" sortKey={expenseSortKey} sortDir={expenseSortDir} onSort={toggleExpenseSort} align="right" className="px-6" />
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {pagedExpense.length === 0 ? (
                          <tr>
                            <td colSpan={5} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">
                              Không có khoản chi nào khớp với bộ lọc hiện tại.
                            </td>
                          </tr>
                        ) : pagedExpense.map(item => (
                          <tr key={item.id} className="hover:bg-slate-50/50 transition-colors">
                            <td className="px-6 py-4 font-extrabold text-slate-900">{item.description}</td>
                            <td className="px-6 py-4 font-semibold text-slate-600">{item.subDescription}</td>
                            <td className="px-6 py-4">
                              <span className={`inline-block px-2.5 py-1 rounded-full text-[11px] font-extrabold ${
                                item.category === "supply" ? "bg-orange-50 text-orange-600" : "bg-violet-50 text-violet-600"
                              }`}>
                                {item.category === "supply" ? "Vật tư" : "Lương"}
                              </span>
                            </td>
                            <td className="px-6 py-4 font-semibold text-slate-500">{formatDate(item.date)}</td>
                            <td className="px-6 py-4 text-right font-black text-primary">-{fmt(item.amount)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>

                {/* Pagination */}
                {totalCount > 0 && (
                  <div className="border-t border-slate-100 px-5 py-3.5 bg-slate-50/25">
                    <Pagination
                      currentPage={page}
                      totalCount={totalCount}
                      pageSize={pageSize}
                      onPageChange={setPage}
                      itemLabel="kết quả"
                    />
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </main>
    </div>
  );
}
