"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import {
  getFinanceOverviewApi,
  getRevenueChartsApi,
  getRevenueTransactionsApi,
  getExpenseChartsApi,
  getExpensesApi,
} from "../../../../lib/apiClient";
import * as XLSX from "xlsx";
import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";

const fmt = (n: number) => new Intl.NumberFormat("vi-VN").format(Math.round(n)) + " đ";
const formatDate = (iso: string) => new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
const toISODate = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

// Số dòng chi tiết tối đa lấy về cho báo cáo "chi tiết giao dịch" — đủ cho hầu hết các kỳ báo cáo
// thực tế; nếu kỳ có nhiều hơn, báo cáo sẽ ghi rõ số dòng bị cắt bớt thay vì âm thầm thiếu dữ liệu.
const MAX_DETAIL_ROWS = 1000;

interface ReportResult {
  title: string;
  columns: string[];
  rows: string[][];
  truncatedNote?: string;
}

type ReportKey =
  | "summary" | "revenue-by-service" | "revenue-by-dentist"
  | "expense-by-category" | "revenue-detail" | "expense-detail";

const REPORT_OPTIONS: { value: ReportKey; label: string }[] = [
  { value: "summary", label: "Tổng hợp: Doanh thu / Chi phí / Lương / Lợi nhuận" },
  { value: "revenue-by-service", label: "Doanh thu theo dịch vụ" },
  { value: "revenue-by-dentist", label: "Doanh thu theo bác sĩ" },
  { value: "expense-by-category", label: "Chi phí theo danh mục" },
  { value: "revenue-detail", label: "Chi tiết giao dịch doanh thu" },
  { value: "expense-detail", label: "Chi tiết chi phí" },
];

async function loadReport(type: ReportKey, from: string, to: string): Promise<ReportResult> {
  switch (type) {
    case "summary": {
      const d = await getFinanceOverviewApi(from, to);
      return {
        title: "Báo cáo tổng hợp",
        columns: ["Chỉ tiêu", "Số tiền", "So với kỳ trước"],
        rows: [
          ["Tổng doanh thu", fmt(d.totalRevenue), `${d.revenueGrowthPercent >= 0 ? "+" : ""}${d.revenueGrowthPercent.toFixed(1)}%`],
          ["Tổng chi phí", fmt(d.totalExpense), `${d.expenseGrowthPercent >= 0 ? "+" : ""}${d.expenseGrowthPercent.toFixed(1)}%`],
          ["Tổng lương", fmt(d.totalPayroll), "—"],
          ["Lợi nhuận", fmt(d.profit), `${d.profitGrowthPercent >= 0 ? "+" : ""}${d.profitGrowthPercent.toFixed(1)}%`],
        ],
      };
    }
    case "revenue-by-service": {
      const d = await getRevenueChartsApi(from, to);
      return {
        title: "Doanh thu theo dịch vụ",
        columns: ["Dịch vụ", "Doanh thu đã thu"],
        rows: d.byService.map((s) => [s.serviceName, fmt(s.amount)]),
      };
    }
    case "revenue-by-dentist": {
      const d = await getRevenueChartsApi(from, to);
      return {
        title: "Doanh thu theo bác sĩ",
        columns: ["Bác sĩ", "Doanh thu đã thu"],
        rows: d.byDentist.map((x) => [x.dentistName, fmt(x.amount)]),
      };
    }
    case "expense-by-category": {
      const d = await getExpenseChartsApi(from, to);
      return {
        title: "Chi phí theo danh mục",
        columns: ["Danh mục", "Số tiền"],
        rows: d.byCategory.map((c) => [c.categoryLabel, fmt(c.amount)]),
      };
    }
    case "revenue-detail": {
      const d = await getRevenueTransactionsApi({ from, to, page: 1, pageSize: MAX_DETAIL_ROWS, sortBy: "date", sortDir: "desc" });
      return {
        title: "Chi tiết giao dịch doanh thu",
        columns: ["Mã hóa đơn", "Bệnh nhân", "Dịch vụ", "Bác sĩ", "Ngày", "Phương thức", "Số tiền", "Còn thiếu (chưa xuất HĐ)", "Trạng thái"],
        rows: d.items.map((t) => [t.invoiceNumber, t.patientName, t.serviceSummary, t.dentistName, formatDate(t.date), t.paymentMethod, fmt(t.amount), t.remainingAmount > 0 ? fmt(t.remainingAmount) : "—", t.status]),
        truncatedNote: d.totalCount > MAX_DETAIL_ROWS
          ? `Kỳ này có ${d.totalCount} giao dịch — báo cáo chỉ lấy ${MAX_DETAIL_ROWS} giao dịch mới nhất.`
          : undefined,
      };
    }
    case "expense-detail": {
      const d = await getExpensesApi({ from, to, page: 1, pageSize: MAX_DETAIL_ROWS, sortBy: "date", sortDir: "desc" });
      return {
        title: "Chi tiết chi phí",
        columns: ["Ngày", "Nội dung", "Danh mục", "Số tiền", "Định kỳ"],
        rows: d.items.map((e) => [formatDate(e.date), e.description, e.category, fmt(e.amount), e.isRecurring ? "Có" : "Không"]),
        truncatedNote: d.totalCount > MAX_DETAIL_ROWS
          ? `Kỳ này có ${d.totalCount} khoản chi phí — báo cáo chỉ lấy ${MAX_DETAIL_ROWS} khoản mới nhất.`
          : undefined,
      };
    }
  }
}

export default function OwnerFinanceReportsPage() {
  useRequireOwner();

  const today = useMemo(() => new Date(), []);
  const [reportType, setReportType] = useState<ReportKey>("summary");
  const [fromISO, setFromISO] = useState(toISODate(new Date(today.getFullYear(), today.getMonth(), 1)));
  const [toISO, setToISO] = useState(toISODate(today));

  const [result, setResult] = useState<ReportResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      setResult(await loadReport(reportType, fromISO, toISO));
      setErrorMsg(null);
    } catch (err) {
      setResult(null);
      setErrorMsg(err instanceof Error ? err.message : "Không thể tải báo cáo");
    } finally { setLoading(false); }
  }, [reportType, fromISO, toISO]);

  useEffect(() => { reload(); }, [reload]);

  const fileBaseName = () => `${result?.title.replace(/[^a-zA-Z0-9À-ỹ]+/g, "_") ?? "BaoCao"}_${fromISO}_${toISO}`;

  const handleExportExcel = () => {
    if (!result) return;
    const worksheet = XLSX.utils.aoa_to_sheet([result.columns, ...result.rows]);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "BaoCao");
    XLSX.writeFile(workbook, `${fileBaseName()}.xlsx`);
  };

  const handleExportPdf = () => {
    if (!result) return;
    const doc = new jsPDF();
    doc.setFontSize(13);
    doc.text(result.title, 14, 15);
    doc.setFontSize(10);
    doc.text(`Kỳ: ${formatDate(fromISO)} — ${formatDate(toISO)}`, 14, 22);
    autoTable(doc, {
      head: [result.columns],
      body: result.rows,
      startY: 28,
      styles: { font: "helvetica", fontSize: 9 },
      headStyles: { fillColor: [220, 38, 38] },
    });
    doc.save(`${fileBaseName()}.pdf`);
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="finance-reports" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader title="Báo Cáo" subtitle="Chọn loại báo cáo và khoảng thời gian, xuất Excel hoặc PDF." />

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          {errorMsg && (
            <div className="flex items-center justify-between gap-4 px-5 py-3.5 bg-red-50 border border-red-200 rounded-xl">
              <span className="text-[13px] font-bold text-red-700">{errorMsg}</span>
              <button onClick={() => setErrorMsg(null)} className="text-red-400 hover:text-red-600 cursor-pointer">✕</button>
            </div>
          )}

          {/* Chọn loại báo cáo + khoảng thời gian */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-wrap items-center gap-3">
            <select value={reportType} onChange={(e) => setReportType(e.target.value as ReportKey)}
              className="px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 text-[13.5px] cursor-pointer min-w-[280px]">
              {REPORT_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
            </select>

            <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">Từ ngày</span>
            <input type="date" value={fromISO} max={toISO} onChange={(e) => setFromISO(e.target.value)}
              className="px-2.5 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />
            <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">Đến ngày</span>
            <input type="date" value={toISO} min={fromISO} onChange={(e) => setToISO(e.target.value)}
              className="px-2.5 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />

            <div className="ml-auto flex items-center gap-2.5">
              <button onClick={handleExportExcel} disabled={!result || result.rows.length === 0}
                className="flex items-center gap-2 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-xs font-bold transition-all cursor-pointer shadow-sm disabled:opacity-50">
                <svg className="w-4 h-4 text-emerald-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" /></svg>
                Excel
              </button>
              <button onClick={handleExportPdf} disabled={!result || result.rows.length === 0}
                className="flex items-center gap-2 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-xs font-bold transition-all cursor-pointer shadow-sm disabled:opacity-50">
                <svg className="w-4 h-4 text-rose-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" /></svg>
                PDF
              </button>
            </div>
          </div>

          {result?.truncatedNote && (
            <div className="bg-amber-50 border border-amber-100 text-amber-700 px-5 py-3 rounded-2xl text-[13px] font-bold">
              {result.truncatedNote}
            </div>
          )}

          {/* Bảng xem trước */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100">
              <span className="text-[13.5px] font-extrabold text-slate-800">{result?.title ?? "Báo cáo"}</span>
              <span className="text-[12px] text-slate-400 font-semibold ml-2">{formatDate(fromISO)} — {formatDate(toISO)}</span>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    {result?.columns.map((c) => (
                      <th key={c} className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">{c}</th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {loading ? (
                    <tr><td colSpan={result?.columns.length ?? 1} className="px-6 py-12 text-center text-slate-400 font-semibold animate-pulse">Đang tải báo cáo...</td></tr>
                  ) : !result || result.rows.length === 0 ? (
                    <tr><td colSpan={result?.columns.length ?? 1} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Không có dữ liệu trong kỳ đã chọn.</td></tr>
                  ) : result.rows.map((row, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/50 transition-colors">
                      {row.map((cell, cellIdx) => (
                        <td key={cellIdx} className={`px-6 py-3.5 font-semibold ${cellIdx === 0 ? "text-slate-900 font-bold" : "text-slate-600"}`}>{cell}</td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
