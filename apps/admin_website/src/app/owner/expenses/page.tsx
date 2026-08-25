"use client";

import { useState, useEffect, useCallback, useMemo, useRef } from "react";
import { createPortal } from "react-dom";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import Pagination from "../../../components/shared/Pagination";
import { SortableTh, Th } from "../../../components/shared/TableHeader";
import {
  getExpensesApi,
  getExpenseSummaryApi,
  getExpenseChartsApi,
  createExpenseApi,
  updateExpenseApi,
  deleteExpenseApi,
  generateRecurringExpensesApi,
  getSupplyImportsInRangeApi,
  type ExpenseDto,
  type ExpenseSummaryDto,
  type ExpenseChartsDto,
  type ExpenseCategory,
  type RecurrenceFrequency,
  type SupplyTransactionDto,
} from "../../../lib/apiClient";

const fmt = (n: number) => new Intl.NumberFormat("vi-VN").format(Math.round(n)) + " đ";
const formatCompact = (val: number) => {
  if (Math.abs(val) >= 1_000_000_000) return (val / 1_000_000_000).toFixed(1).replace(".0", "") + " tỷ";
  if (Math.abs(val) >= 1_000_000) return Math.round(val / 1_000_000) + " tr";
  if (Math.abs(val) >= 1_000) return Math.round(val / 1_000) + "k";
  return String(val);
};
const formatDate = (iso: string) => new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
const toISODate = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
const fmtMoneyInput = (n: number) => (n ? n.toLocaleString("vi-VN") : "");
const parseMoneyInput = (s: string) => Number(s.replace(/[^\d]/g, "")) || 0;

const CATEGORY_LABELS: Record<ExpenseCategory, string> = {
  Medicine: "Thuốc", Equipment: "Thiết bị", Rent: "Thuê mặt bằng", Utilities: "Điện nước",
  Marketing: "Marketing", Maintenance: "Bảo trì", Software: "Phần mềm", Other: "Khác",
};
const CATEGORY_OPTIONS = Object.keys(CATEGORY_LABELS) as ExpenseCategory[];
const FREQUENCY_LABELS: Record<RecurrenceFrequency, string> = { Monthly: "Hàng tháng", Quarterly: "Hàng quý", Yearly: "Hàng năm" };

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

function Portal({ children }: { children: React.ReactNode }) {
  if (typeof document === "undefined") return null;
  return createPortal(children, document.body);
}

type SortKey = "date" | "amount" | "category";
type SortDir = "asc" | "desc";
const PAGE_SIZE_DEFAULT = 15;

type SupplySortKey = "date" | "item" | "quantity" | "unitPrice" | "total";
const SUPPLY_PAGE_SIZE_DEFAULT = 15;

interface ExpenseFormState {
  category: ExpenseCategory;
  description: string;
  amount: number;
  date: string;
  note: string;
  isRecurring: boolean;
  frequency: RecurrenceFrequency;
}

const emptyForm = (): ExpenseFormState => ({
  category: "Other",
  description: "",
  amount: 0,
  date: toISODate(new Date()),
  note: "",
  isRecurring: false,
  frequency: "Monthly",
});

export default function OwnerExpensesPage() {
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

  const [summary, setSummary] = useState<ExpenseSummaryDto | null>(null);
  const [summaryLoading, setSummaryLoading] = useState(true);
  const [charts, setCharts] = useState<ExpenseChartsDto | null>(null);
  const [chartsLoading, setChartsLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [sortKey, setSortKey] = useState<SortKey>("date");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_DEFAULT);

  const [items, setItems] = useState<ExpenseDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [listLoading, setListLoading] = useState(true);

  const [isGenerating, setIsGenerating] = useState(false);

  const [formOpen, setFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<ExpenseFormState>(emptyForm());
  const [saving, setSaving] = useState(false);

  const [deleteTarget, setDeleteTarget] = useState<ExpenseDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [supplyImports, setSupplyImports] = useState<SupplyTransactionDto[]>([]);
  const [supplyLoading, setSupplyLoading] = useState(true);
  const [supplySearch, setSupplySearch] = useState("");
  const [supplySortKey, setSupplySortKey] = useState<SupplySortKey>("date");
  const [supplySortDir, setSupplySortDir] = useState<SortDir>("desc");
  const [supplyPage, setSupplyPage] = useState(1);
  const [supplyPageSize, setSupplyPageSize] = useState(SUPPLY_PAGE_SIZE_DEFAULT);
  const supplyTableRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const timer = setTimeout(() => { setSearchTerm(searchQuery.trim()); setPage(1); }, 350);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  useEffect(() => { setPage(1); }, [fromISO, toISO, categoryFilter]);
  useEffect(() => { setSupplyPage(1); }, [fromISO, toISO, supplySearch]);

  const showMessage = (msg: string) => { setErrorMsg(null); setSuccessMsg(msg); setTimeout(() => setSuccessMsg(null), 4000); };
  const showError = (err: unknown, fallback: string) => { setSuccessMsg(null); setErrorMsg(err instanceof Error ? err.message : fallback); };

  const reloadSummary = useCallback(async () => {
    setSummaryLoading(true);
    try { setSummary(await getExpenseSummaryApi(fromISO, toISO)); }
    catch (err) { showError(err, "Không thể tải tổng quan chi phí"); }
    finally { setSummaryLoading(false); }
  }, [fromISO, toISO]);

  const reloadCharts = useCallback(async () => {
    setChartsLoading(true);
    try { setCharts(await getExpenseChartsApi(fromISO, toISO)); }
    catch { setCharts(null); }
    finally { setChartsLoading(false); }
  }, [fromISO, toISO]);

  const reloadList = useCallback(async () => {
    setListLoading(true);
    try {
      const data = await getExpensesApi({
        from: fromISO, to: toISO,
        category: categoryFilter || undefined,
        search: searchTerm || undefined,
        page, pageSize, sortBy: sortKey, sortDir,
      });
      setItems(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      setItems([]); setTotalCount(0);
      showError(err, "Không thể tải danh sách chi phí");
    } finally { setListLoading(false); }
  }, [fromISO, toISO, categoryFilter, searchTerm, page, pageSize, sortKey, sortDir]);

  const reloadSupplyHistory = useCallback(async () => {
    setSupplyLoading(true);
    try { setSupplyImports(await getSupplyImportsInRangeApi(fromISO, toISO)); }
    catch (err) { showError(err, "Không thể tải lịch sử nhập kho"); setSupplyImports([]); }
    finally { setSupplyLoading(false); }
  }, [fromISO, toISO]);

  useEffect(() => { reloadSummary(); }, [reloadSummary]);
  useEffect(() => { reloadCharts(); }, [reloadCharts]);
  useEffect(() => { reloadList(); }, [reloadList]);
  useEffect(() => { reloadSupplyHistory(); }, [reloadSupplyHistory]);

  const reloadAll = useCallback(() => {
    reloadSummary(); reloadCharts(); reloadList(); reloadSupplyHistory();
  }, [reloadSummary, reloadCharts, reloadList, reloadSupplyHistory]);

  const filteredSupplyImports = useMemo(() => {
    const term = supplySearch.trim().toLowerCase();
    const filtered = term
      ? supplyImports.filter((t) => t.itemName.toLowerCase().includes(term) || (t.note ?? "").toLowerCase().includes(term))
      : supplyImports;
    const dir = supplySortDir === "asc" ? 1 : -1;
    return [...filtered].sort((a, b) => {
      switch (supplySortKey) {
        case "item": return a.itemName.localeCompare(b.itemName) * dir;
        case "quantity": return (a.quantity - b.quantity) * dir;
        case "unitPrice": return ((a.unitPrice ?? 0) - (b.unitPrice ?? 0)) * dir;
        case "total": return ((a.unitPrice ?? 0) * a.quantity - (b.unitPrice ?? 0) * b.quantity) * dir;
        default: return (new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()) * dir;
      }
    });
  }, [supplyImports, supplySearch, supplySortKey, supplySortDir]);

  const supplyTotalAmount = useMemo(
    () => filteredSupplyImports.reduce((sum, t) => sum + (t.unitPrice ?? 0) * t.quantity, 0),
    [filteredSupplyImports]
  );

  const pagedSupplyImports = useMemo(() => {
    const start = (supplyPage - 1) * supplyPageSize;
    return filteredSupplyImports.slice(start, start + supplyPageSize);
  }, [filteredSupplyImports, supplyPage, supplyPageSize]);

  const toggleSupplySort = (column: SupplySortKey) => {
    if (supplySortKey === column) setSupplySortDir((d) => (d === "asc" ? "desc" : "asc"));
    else { setSupplySortKey(column); setSupplySortDir(column === "item" ? "asc" : "desc"); }
    setSupplyPage(1);
  };

  const handleSupplyPageSizeChange = (size: number) => {
    setSupplyPageSize(size);
    setSupplyPage(1);
  };

  const scrollToSupplyHistory = () => supplyTableRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });

  const toggleSort = (column: SortKey) => {
    if (sortKey === column) setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    else { setSortKey(column); setSortDir("desc"); }
    setPage(1);
  };

  const handleGenerateRecurring = async () => {
    setIsGenerating(true);
    try {
      const result = await generateRecurringExpensesApi();
      showMessage(result.generatedCount > 0
        ? `Đã sinh ${result.generatedCount} chi phí định kỳ cho kỳ hiện tại.`
        : "Không có chi phí định kỳ nào cần sinh cho kỳ này.");
      reloadAll();
    } catch (err) { showError(err, "Không thể sinh chi phí định kỳ"); }
    finally { setIsGenerating(false); }
  };

  const handlePageSizeChange = (size: number) => {
    setPageSize(size);
    setPage(1);
  };

  const openCreateForm = () => { setEditingId(null); setForm(emptyForm()); setFormOpen(true); };
  const openEditForm = (item: ExpenseDto) => {
    setEditingId(item.id);
    setForm({
      category: item.category,
      description: item.description,
      amount: item.amount,
      date: item.date,
      note: item.note ?? "",
      isRecurring: item.isRecurring,
      frequency: item.frequency ?? "Monthly",
    });
    setFormOpen(true);
  };

  const handleSubmitForm = async () => {
    if (!form.description.trim() || form.amount <= 0) {
      showError(null, "Vui lòng nhập đầy đủ nội dung và số tiền hợp lệ.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        category: form.category,
        description: form.description.trim(),
        amount: form.amount,
        date: form.date,
        note: form.note.trim() || null,
        isRecurring: form.isRecurring,
        frequency: form.isRecurring ? form.frequency : null,
      };
      if (editingId) {
        await updateExpenseApi(editingId, payload);
        showMessage("Đã cập nhật chi phí.");
      } else {
        await createExpenseApi(payload);
        showMessage("Đã thêm chi phí mới.");
      }
      setFormOpen(false);
      reloadAll();
    } catch (err) { showError(err, "Không thể lưu chi phí"); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await deleteExpenseApi(deleteTarget.id);
      showMessage(`Đã xoá chi phí "${deleteTarget.description}".`);
      setDeleteTarget(null);
      reloadAll();
    } catch (err) { showError(err, "Không thể xoá chi phí"); }
    finally { setDeleting(false); }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="expenses" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader title="Chi Phí" subtitle="Quản lý chi phí vận hành, tổng hợp theo danh mục (gồm cả vật tư và lương)." />

        <Portal>
          <div className="fixed top-24 right-8 z-50 w-[min(28rem,calc(100vw-4rem))] space-y-3 pointer-events-none">
            {successMsg && (
              <div className="animate-fade-in pointer-events-auto bg-emerald-50 border border-emerald-200 text-emerald-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-emerald-900/5">
                <span className="flex-1">{successMsg}</span>
              </div>
            )}
            {errorMsg && (
              <div className="animate-fade-in pointer-events-auto bg-rose-50 border border-rose-200 text-rose-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-rose-900/5">
                <span className="flex-1">{errorMsg}</span>
                <button onClick={() => setErrorMsg(null)} className="text-rose-400 hover:text-rose-600 cursor-pointer">✕</button>
              </div>
            )}
          </div>
        </Portal>

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
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
            <div className="bg-primary p-5 rounded-2xl shadow-md shadow-primary/20">
              <span className="text-[11px] font-extrabold text-red-100 uppercase tracking-wider block">Tổng chi phí</span>
              <span className="text-xl font-black text-white mt-1.5 block">{summaryLoading ? "…" : fmt(summary?.totalExpense ?? 0)}</span>
              <span className="text-[11.5px] font-semibold text-red-100 mt-0.5 block">Gồm chi phí khác + vật tư + lương</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Chi phí khác</span>
              <span className="text-xl font-black text-slate-900 mt-1.5 block">{summaryLoading ? "…" : fmt(summary?.totalOther ?? 0)}</span>
              <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">Thuốc/thiết bị/mặt bằng/...</span>
            </div>
            <div onClick={scrollToSupplyHistory}
              className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm cursor-pointer hover:border-orange-300 hover:shadow-md transition-all">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Vật tư</span>
              <span className="text-xl font-black text-orange-600 mt-1.5 block">{summaryLoading ? "…" : fmt(summary?.totalSupply ?? 0)}</span>
              <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">Chi phí mua vào (nhập kho) trong kỳ — bấm để xem lịch sử bên dưới</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Lương</span>
              <span className="text-xl font-black text-violet-600 mt-1.5 block">{summaryLoading ? "…" : fmt(summary?.totalPayroll ?? 0)}</span>
              <span className="text-[11.5px] font-semibold text-slate-400 mt-0.5 block">Kỳ lương rơi vào khoảng lọc</span>
            </div>
          </div>

          {/* Biểu đồ theo danh mục */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
            <span className="text-[13.5px] font-extrabold text-slate-800 block mb-1">Chi phí theo danh mục</span>
            <span className="text-[11.5px] text-slate-400 font-semibold block mb-4">
              Số liệu tính theo chi phí mua vào (thực chi/nhập kho) trong kỳ — khác với giá vốn lúc tiêu hao theo dịch vụ đang hiển thị ở Doanh thu/Tổng quan tài chính.
            </span>
            {chartsLoading ? (
              <div className="py-10 text-center text-slate-400 font-semibold text-[13px] animate-pulse">Đang tải...</div>
            ) : !charts || charts.byCategory.length === 0 ? (
              <div className="py-10 text-center text-slate-400 font-semibold text-[13px]">Chưa có dữ liệu trong kỳ.</div>
            ) : (
              <div className="space-y-2.5">
                {charts.byCategory.map((c) => {
                  const max = Math.max(...charts.byCategory.map((x) => x.amount), 1);
                  const isSupply = c.categoryLabel === "Vật tư";
                  return (
                    <div key={c.categoryLabel} onClick={isSupply ? scrollToSupplyHistory : undefined}
                      className={`flex items-center gap-3 ${isSupply ? "cursor-pointer group" : ""}`}>
                      <span className={`w-32 shrink-0 text-[12px] font-bold text-slate-600 truncate ${isSupply ? "group-hover:text-orange-600" : ""}`} title={c.categoryLabel}>{c.categoryLabel}</span>
                      <div className="flex-1 h-5 bg-slate-100 rounded-md overflow-hidden">
                        <div className={`h-full rounded-md transition-all duration-500 ${isSupply ? "bg-orange-500" : "bg-primary"}`} style={{ width: `${Math.max(2, (c.amount / max) * 100)}%` }} />
                      </div>
                      <span className="w-16 shrink-0 text-right text-[12px] font-black text-slate-700 tabular-nums">{formatCompact(c.amount)}</span>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Danh sách chi phí */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
            <div className="p-4 flex flex-wrap items-center gap-3 border-b border-slate-100">
              <div className="relative flex-1 min-w-[180px]">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input type="text" placeholder="Tìm chi phí..." value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold" />
              </div>
              <div className="ml-auto flex items-center gap-2.5 flex-wrap">
                <select value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}
                  className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                  <option value="">Tất cả danh mục</option>
                  {CATEGORY_OPTIONS.map((c) => <option key={c} value={c}>{CATEGORY_LABELS[c]}</option>)}
                </select>
                <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold whitespace-nowrap">
                  <span>Hiển thị</span>
                  <select value={pageSize} onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                    className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                    {[10, 20, 50].map((n) => (<option key={n} value={n}>{n}</option>))}
                  </select>
                  <span>/ trang</span>
                </div>
                <button onClick={handleGenerateRecurring} disabled={isGenerating}
                  title="Sinh chi phí cho kỳ hiện tại từ các khoản chi phí định kỳ đang bật"
                  className="px-3.5 py-2 bg-blue-50 border border-blue-200 hover:bg-blue-100 disabled:opacity-50 text-blue-700 text-xs font-black rounded-xl cursor-pointer shadow-sm transition-all whitespace-nowrap">
                  {isGenerating ? "Đang sinh..." : "Sinh chi phí định kỳ"}
                </button>
                <button onClick={openCreateForm}
                  className="px-4 py-2 bg-primary hover:bg-primary-hover text-white text-xs font-black rounded-xl cursor-pointer shadow-md shadow-primary/20 transition-all whitespace-nowrap">
                  + Thêm chi phí
                </button>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    <SortableTh column="date" label="Ngày" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} className="px-6" />
                    <Th className="px-6">Nội dung</Th>
                    <SortableTh column="category" label="Danh mục" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} className="px-6" />
                    <Th className="px-6" align="center">Định kỳ</Th>
                    <SortableTh column="amount" label="Số tiền" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" className="px-6" />
                    <Th className="px-6" align="center">Hành động</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {listLoading ? (
                    <tr><td colSpan={6} className="px-6 py-12 text-center text-slate-400 font-semibold animate-pulse">Đang tải danh sách chi phí...</td></tr>
                  ) : items.length === 0 ? (
                    <tr><td colSpan={6} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Không có chi phí nào khớp với bộ lọc hiện tại.</td></tr>
                  ) : items.map((item) => (
                    <tr key={item.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-6 py-4 font-semibold text-slate-500">{formatDate(item.date)}</td>
                      <td className="px-6 py-4">
                        <div className="font-extrabold text-slate-900">{item.description}</div>
                        {item.note && <div className="text-[11px] text-slate-400 font-semibold mt-0.5">{item.note}</div>}
                        {item.recurringSourceId && <div className="text-[10.5px] text-blue-500 font-bold mt-0.5">Sinh từ chi phí định kỳ</div>}
                      </td>
                      <td className="px-6 py-4 font-semibold text-slate-600">{CATEGORY_LABELS[item.category]}</td>
                      <td className="px-6 py-4 text-center">
                        {item.isRecurring ? (
                          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-black bg-blue-50 text-blue-700 border border-blue-200">
                            {FREQUENCY_LABELS[item.frequency ?? "Monthly"]}
                          </span>
                        ) : (
                          <span className="text-slate-300">—</span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right font-black text-slate-900">{fmt(item.amount)}</td>
                      <td className="px-6 py-4 text-center">
                        <div className="flex items-center justify-center gap-2">
                          <button onClick={() => openEditForm(item)}
                            className="px-2.5 py-1.5 rounded-lg text-xs font-black bg-slate-50 hover:bg-slate-100 text-slate-600 border border-slate-200 cursor-pointer transition-all">
                            Sửa
                          </button>
                          <button onClick={() => setDeleteTarget(item)}
                            className="px-2.5 py-1.5 rounded-lg text-xs font-black bg-red-50 hover:bg-red-100 text-primary border border-red-200 cursor-pointer transition-all">
                            Xoá
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {!listLoading && totalCount > 0 && (
              <div className="border-t border-slate-100 px-5 py-3.5 bg-slate-50/25">
                <Pagination currentPage={page} totalCount={totalCount} pageSize={pageSize} onPageChange={setPage} itemLabel="chi phí" />
              </div>
            )}
          </div>

          {/* Lịch sử nhập kho (chi tiết cho khoản "Vật tư" ở trên) */}
          <div ref={supplyTableRef} className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col scroll-mt-6">
            <div className="p-4 border-b border-slate-100">
              <span className="text-[13.5px] font-extrabold text-slate-800 block">Lịch sử nhập kho</span>
              <span className="text-[11.5px] text-slate-400 font-semibold block mt-0.5">
                Chi phí mua vào theo từng lần nhập kho trong kỳ đang lọc — khác với giá vốn lúc tiêu hao theo dịch vụ (xem ở Doanh thu/Tổng quan tài chính).
              </span>
            </div>
            <div className="p-4 flex flex-wrap items-center gap-3 border-b border-slate-100">
              <div className="relative flex-1 min-w-[220px]">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input type="text" placeholder="Tìm theo tên vật tư, ghi chú..." value={supplySearch} onChange={(e) => setSupplySearch(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold" />
              </div>
              <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold whitespace-nowrap">
                <span>Hiển thị</span>
                <select value={supplyPageSize} onChange={(e) => handleSupplyPageSizeChange(Number(e.target.value))}
                  className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                  {[10, 20, 50].map((n) => (<option key={n} value={n}>{n}</option>))}
                </select>
                <span>/ trang</span>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    <SortableTh column="date" label="Ngày" sortKey={supplySortKey} sortDir={supplySortDir} onSort={toggleSupplySort} className="px-6" />
                    <SortableTh column="item" label="Vật tư" sortKey={supplySortKey} sortDir={supplySortDir} onSort={toggleSupplySort} className="px-6" />
                    <SortableTh column="quantity" label="Số lượng" sortKey={supplySortKey} sortDir={supplySortDir} onSort={toggleSupplySort} align="right" className="px-6" />
                    <SortableTh column="unitPrice" label="Đơn giá" sortKey={supplySortKey} sortDir={supplySortDir} onSort={toggleSupplySort} align="right" className="px-6" />
                    <SortableTh column="total" label="Thành tiền" sortKey={supplySortKey} sortDir={supplySortDir} onSort={toggleSupplySort} align="right" className="px-6" />
                    <Th className="px-6">Người nhập</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {supplyLoading ? (
                    <tr><td colSpan={6} className="px-6 py-12 text-center text-slate-400 font-semibold animate-pulse">Đang tải lịch sử nhập kho...</td></tr>
                  ) : pagedSupplyImports.length === 0 ? (
                    <tr><td colSpan={6} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Không có lần nhập kho nào khớp với bộ lọc hiện tại.</td></tr>
                  ) : pagedSupplyImports.map((t) => (
                    <tr key={t.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-6 py-4 font-semibold text-slate-500 whitespace-nowrap">{formatDate(t.createdAt)}</td>
                      <td className="px-6 py-4">
                        <div className="font-extrabold text-slate-900">{t.itemName}</div>
                        {t.note && <div className="text-[11px] text-slate-400 font-semibold mt-0.5">{t.note}</div>}
                      </td>
                      <td className="px-6 py-4 text-right font-semibold text-slate-600">{t.quantity}</td>
                      <td className="px-6 py-4 text-right font-semibold text-slate-600">{fmt(t.unitPrice ?? 0)}</td>
                      <td className="px-6 py-4 text-right font-black text-slate-900">{fmt((t.unitPrice ?? 0) * t.quantity)}</td>
                      <td className="px-6 py-4 font-semibold text-slate-500">{t.createdBy}</td>
                    </tr>
                  ))}
                </tbody>
                {!supplyLoading && pagedSupplyImports.length > 0 && (
                  <tfoot>
                    <tr className="border-t border-slate-200 bg-slate-50/50">
                      <td colSpan={4} className="px-6 py-3 text-right font-black text-slate-500 text-[11px] uppercase tracking-wide">Tổng cộng ({filteredSupplyImports.length} lần nhập)</td>
                      <td className="px-6 py-3 text-right font-black text-orange-600">{fmt(supplyTotalAmount)}</td>
                      <td />
                    </tr>
                  </tfoot>
                )}
              </table>
            </div>

            {!supplyLoading && filteredSupplyImports.length > 0 && (
              <div className="border-t border-slate-100 px-5 py-3.5 bg-slate-50/25">
                <Pagination currentPage={supplyPage} totalCount={filteredSupplyImports.length} pageSize={supplyPageSize} onPageChange={setSupplyPage} itemLabel="lần nhập" />
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Modal thêm/sửa chi phí */}
      {formOpen && (
        <Portal>
          <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4"
            onClick={() => !saving && setFormOpen(false)} role="dialog" aria-modal="true">
            <div className="animate-fade-in bg-white rounded-2xl shadow-2xl w-full max-w-lg p-6 max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
              <h2 className="text-lg font-extrabold text-slate-900">{editingId ? "Sửa chi phí" : "Thêm chi phí"}</h2>

              <div className="mt-4 space-y-4">
                <div>
                  <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Danh mục</label>
                  <select value={form.category} onChange={(e) => setForm((f) => ({ ...f, category: e.target.value as ExpenseCategory }))}
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 text-[13.5px] cursor-pointer">
                    {CATEGORY_OPTIONS.map((c) => <option key={c} value={c}>{CATEGORY_LABELS[c]}</option>)}
                  </select>
                </div>

                <div>
                  <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Nội dung</label>
                  <input type="text" value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                    placeholder="Ví dụ: Tiền thuê mặt bằng tháng 8"
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 text-[13.5px]" />
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Số tiền</label>
                    <input type="text" inputMode="numeric" value={fmtMoneyInput(form.amount)}
                      onChange={(e) => setForm((f) => ({ ...f, amount: parseMoneyInput(e.target.value) }))}
                      placeholder="0"
                      className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 text-[13.5px]" />
                  </div>
                  <div>
                    <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Ngày</label>
                    <input type="date" value={form.date} onChange={(e) => setForm((f) => ({ ...f, date: e.target.value }))}
                      className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 text-[13.5px] cursor-pointer" />
                  </div>
                </div>

                <div>
                  <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Ghi chú (tùy chọn)</label>
                  <input type="text" value={form.note} onChange={(e) => setForm((f) => ({ ...f, note: e.target.value }))}
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 text-[13.5px]" />
                </div>

                <div className="flex items-center gap-3 pt-1">
                  <label className="flex items-center gap-2 cursor-pointer select-none">
                    <input type="checkbox" checked={form.isRecurring}
                      onChange={(e) => setForm((f) => ({ ...f, isRecurring: e.target.checked }))}
                      className="w-4 h-4 accent-primary cursor-pointer" />
                    <span className="text-[13px] font-bold text-slate-600">Chi phí định kỳ</span>
                  </label>
                  {form.isRecurring && (
                    <select value={form.frequency} onChange={(e) => setForm((f) => ({ ...f, frequency: e.target.value as RecurrenceFrequency }))}
                      className="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                      {(Object.keys(FREQUENCY_LABELS) as RecurrenceFrequency[]).map((f) => <option key={f} value={f}>{FREQUENCY_LABELS[f]}</option>)}
                    </select>
                  )}
                </div>
                {form.isRecurring && (
                  <p className="text-[11.5px] text-blue-600 font-semibold bg-blue-50 border border-blue-100 rounded-xl px-3.5 py-2.5">
                    Bản ghi này sẽ là mẫu để sinh chi phí cho các kỳ sau — bấm &quot;Sinh chi phí định kỳ&quot; ở trang danh sách khi vào kỳ mới.
                  </p>
                )}
              </div>

              <div className="flex items-center justify-end gap-3 mt-6">
                <button onClick={() => setFormOpen(false)} disabled={saving}
                  className="px-4 py-2.5 rounded-xl border border-slate-200 bg-white text-slate-600 text-xs font-black hover:bg-slate-50 cursor-pointer transition-all disabled:opacity-50">
                  Hủy
                </button>
                <button onClick={handleSubmitForm} disabled={saving}
                  className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-hover disabled:opacity-50 text-white text-xs font-black cursor-pointer shadow-md shadow-primary/20 transition-all">
                  {saving ? "Đang lưu..." : editingId ? "Lưu thay đổi" : "Thêm chi phí"}
                </button>
              </div>
            </div>
          </div>
        </Portal>
      )}

      {/* Xác nhận xoá */}
      {deleteTarget && (
        <Portal>
          <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4"
            onClick={() => !deleting && setDeleteTarget(null)} role="dialog" aria-modal="true">
            <div className="animate-fade-in bg-white rounded-2xl shadow-2xl w-full max-w-md p-6" onClick={(e) => e.stopPropagation()}>
              <h2 className="text-lg font-extrabold text-slate-900">Xác nhận xoá chi phí</h2>
              <p className="text-[13.5px] text-slate-500 font-semibold mt-1.5 leading-relaxed">
                Xoá <span className="font-black text-slate-700">{deleteTarget.description}</span> ({fmt(deleteTarget.amount)})? Thao tác này không thể hoàn tác.
              </p>
              <div className="flex items-center justify-end gap-3 mt-6">
                <button onClick={() => setDeleteTarget(null)} disabled={deleting}
                  className="px-4 py-2.5 rounded-xl border border-slate-200 bg-white text-slate-600 text-xs font-black hover:bg-slate-50 cursor-pointer transition-all disabled:opacity-50">
                  Hủy
                </button>
                <button onClick={handleDelete} disabled={deleting}
                  className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-hover disabled:opacity-50 text-white text-xs font-black cursor-pointer shadow-md shadow-primary/20 transition-all">
                  {deleting ? "Đang xoá..." : "Xoá"}
                </button>
              </div>
            </div>
          </div>
        </Portal>
      )}

    </div>
  );
}
