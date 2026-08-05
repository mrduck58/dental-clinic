"use client";

import React, { useState, useEffect, useCallback, useMemo } from "react";
import { createPortal } from "react-dom";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import {
  getPayrollPeriodApi,
  getPayrollYearlyApi,
  payPayrollApi,
  unpayPayrollApi,
  payAllPayrollApi,
  type PayrollPeriodDto,
  type PayrollItemDto,
  type PayrollFailureDto,
  type PayrollYearlyDto,
} from "../../../lib/apiClient";
import * as XLSX from "xlsx";

const PAGE_SIZE = 10;

/**
 * Màu biểu đồ — thang một tông theo đỏ chủ đạo của web (--color-primary = red 600),
 * đậm = đã chi, nhạt = còn phải chi. Cặp này đạt cả 4 tiêu chí của thang tuần tự:
 * sáng→tối đơn điệu, chênh lệch độ sáng đủ lớn, cùng một tông (lệch 5°),
 * và đầu nhạt vẫn đạt 2.77:1 so với nền trắng.
 */
const CHART_PAID = "#dc2626";
const CHART_REMAINING = "#f87171";
const CHART_GRID = "#e2e8f0";
const CHART_AXIS_TEXT = "#94a3b8";

type SortKey =
  | "name" | "department" | "base" | "allowance"
  | "deduction" | "net" | "status" | "paidAt";
type SortDir = "asc" | "desc";
type Align = "left" | "right" | "center";

/**
 * Đưa nội dung ra thẳng <body>.
 * Bắt buộc phải có: khung trang ngoài cùng mang class `animate-fade-in`, mà keyframes
 * fadeIn kết thúc bằng `transform: translateY(0)` với fill-mode `forwards` — transform
 * còn lại vĩnh viễn nên div đó trở thành containing block của mọi `position: fixed`
 * bên trong. Hệ quả: modal/toast neo vào khung trang và trôi theo thanh cuộn thay vì
 * đứng yên giữa màn hình. Render ngoài body là cách thoát khỏi containing block đó.
 */
function Portal({ children }: { children: React.ReactNode }) {
  if (typeof document === "undefined") return null; // lượt render phía server
  return createPortal(children, document.body);
}

const TH_CLASS = "px-4 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider";
// Viết đủ chữ, không ghép chuỗi — Tailwind quét mã nguồn theo văn bản tĩnh
const ALIGN_CLASS: Record<Align, string> = {
  left: "text-left", right: "text-right", center: "text-center",
};

export default function OwnerPayrollPage() {
  useRequireOwner();

  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [deptFilter, setDeptFilter] = useState("All");
  const [period, setPeriod] = useState<PayrollPeriodDto | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [failures, setFailures] = useState<PayrollFailureDto[]>([]);
  const [busyUserId, setBusyUserId] = useState<string | null>(null);
  const [isPayingAll, setIsPayingAll] = useState(false);

  const [selectedMonth, setSelectedMonth] = useState(new Date().getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());

  const [sortKey, setSortKey] = useState<SortKey>("net");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const [page, setPage] = useState(1);

  // Hộp thoại xác nhận chi trả hàng loạt
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmTotals, setConfirmTotals] = useState<{ count: number; amount: number } | null>(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  // Báo cáo lương theo năm (tải lười, chỉ khi mở)
  const [showYearly, setShowYearly] = useState(false);
  const [yearlyYear, setYearlyYear] = useState(new Date().getFullYear());
  const [yearly, setYearly] = useState<PayrollYearlyDto | null>(null);
  const [yearlyLoading, setYearlyLoading] = useState(false);
  const [yearlyError, setYearlyError] = useState<string | null>(null);
  const [yearlyView, setYearlyView] = useState<"chart" | "table">("chart");
  const [hoverMonth, setHoverMonth] = useState<number | null>(null);

  // Gõ tìm kiếm không gọi API ngay — mỗi lần gõ là một request tới /api/payrolls
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearchTerm(searchQuery.trim());
      setPage(1); // kết quả mới thì xem từ trang đầu
    }, 350);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  const reload = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await getPayrollPeriodApi({
        year: selectedYear,
        month: selectedMonth,
        search: searchTerm || undefined,
        // Lựa chọn "Nha sĩ" lọc theo vai trò, các lựa chọn còn lại lọc theo bộ phận
        role: deptFilter === "Dentist" ? "Dentist,Doctor" : undefined,
        department: deptFilter === "All" || deptFilter === "Dentist" ? undefined : deptFilter,
      });
      setPeriod(data);
      setErrorMsg(null);
    } catch (err) {
      console.error("Failed to load payroll period", err);
      setPeriod(null);
      setErrorMsg(err instanceof Error ? err.message : "Không thể tải bảng lương");
    } finally {
      setIsLoading(false);
    }
  }, [selectedMonth, selectedYear, searchTerm, deptFilter]);

  useEffect(() => {
    reload();
  }, [reload]);

  // Modal mở: đóng bằng Esc và khoá cuộn nền để không cuộn nhầm trang phía sau
  useEffect(() => {
    if (!confirmOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") setConfirmOpen(false);
    };
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [confirmOpen]);

  const items = useMemo(() => period?.items ?? [], [period]);
  const summary = period?.summary;

  const formatCurrency = (val: number) => new Intl.NumberFormat("vi-VN").format(val) + " đ";

  const formatCompact = (val: number) => {
    if (Math.abs(val) >= 1_000_000_000) return (val / 1_000_000_000).toFixed(1).replace(".0", "") + " tỷ";
    if (Math.abs(val) >= 1_000_000) return Math.round(val / 1_000_000) + " tr";
    if (Math.abs(val) >= 1_000) return Math.round(val / 1_000) + "k";
    return String(val);
  };

  const formatDate = (iso: string | null) => (iso ? new Date(iso).toLocaleDateString("vi-VN") : "—");

  const formatDays = (val: number) => (Number.isInteger(val) ? String(val) : val.toFixed(1));

  const showMessage = (msg: string) => {
    setErrorMsg(null);
    setSuccessMsg(msg);
    setTimeout(() => setSuccessMsg(null), 4000);
  };

  const showError = (err: unknown, fallback: string) => {
    setSuccessMsg(null);
    setErrorMsg(err instanceof Error ? err.message : fallback);
  };

  // ── Sắp xếp ────────────────────────────────────────────────────────────────

  const changeOf = (it: PayrollItemDto) => it.netSalary - it.previousNetSalary;

  const sortedItems = useMemo(() => {
    const dir = sortDir === "asc" ? 1 : -1;
    const value = (it: PayrollItemDto): string | number => {
      switch (sortKey) {
        case "name":       return (it.fullName || it.email).toLowerCase();
        case "department": return (it.department ?? "").toLowerCase();
        case "base":       return it.baseSalary;
        case "allowance":  return it.allowance;
        case "deduction":  return it.deduction;
        case "net":        return it.netSalary;
        case "status":     return it.status === "Paid" ? 1 : 0;
        case "paidAt":     return it.paidAt ? new Date(it.paidAt).getTime() : 0;
      }
    };
    return [...items].sort((a, b) => {
      const va = value(a);
      const vb = value(b);
      if (typeof va === "string" && typeof vb === "string") return va.localeCompare(vb, "vi") * dir;
      return ((va as number) - (vb as number)) * dir;
    });
  }, [items, sortKey, sortDir]);

  const toggleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      // Cột chữ mặc định A→Z, cột số mặc định lớn→nhỏ
      setSortDir(key === "name" || key === "department" ? "asc" : "desc");
    }
    setPage(1);
  };

  // ── Phân trang ─────────────────────────────────────────────────────────────

  const totalPages = Math.max(1, Math.ceil(sortedItems.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pagedItems = sortedItems.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);
  const firstIndex = sortedItems.length === 0 ? 0 : (currentPage - 1) * PAGE_SIZE + 1;
  const lastIndex = Math.min(currentPage * PAGE_SIZE, sortedItems.length);

  // ── Hành động ──────────────────────────────────────────────────────────────

  const handleTogglePay = async (userId: string, name: string, isPaid: boolean) => {
    setBusyUserId(userId);
    try {
      if (isPaid) {
        await unpayPayrollApi({ year: selectedYear, month: selectedMonth, userId });
        showMessage(`Đã hoàn tác chi trả lương cho nhân viên ${name}!`);
      } else {
        await payPayrollApi({ year: selectedYear, month: selectedMonth, userId });
        showMessage(`Đã chi trả lương tháng ${selectedMonth}/${selectedYear} cho nhân viên ${name}!`);
      }
      setFailures([]);
      await reload();
    } catch (err) {
      showError(err, "Không thể cập nhật trạng thái thanh toán");
    } finally {
      setBusyUserId(null);
    }
  };

  const openConfirmPayAll = async () => {
    setConfirmOpen(true);
    setConfirmTotals(null);
    if (!isFiltered) {
      setConfirmTotals({ count: pendingCount, amount: totalRemaining });
      return;
    }
    // Đang lọc: số trên bảng không phải số sẽ chi, nên hỏi lại server toàn kỳ
    setConfirmLoading(true);
    try {
      const full = await getPayrollPeriodApi({ year: selectedYear, month: selectedMonth });
      setConfirmTotals({
        count: full.summary.pendingCount,
        amount: full.summary.totalNet - full.summary.totalPaid,
      });
    } catch {
      setConfirmTotals(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  const handlePayAll = async () => {
    setConfirmOpen(false);
    setIsPayingAll(true);
    try {
      const result = await payAllPayrollApi({ year: selectedYear, month: selectedMonth });
      setFailures(result.failures);
      if (result.paidCount > 0) {
        showMessage(
          `Đã chi trả lương tháng ${selectedMonth}/${selectedYear} cho ${result.paidCount} nhân sự, tổng ${formatCurrency(result.totalPaid)}.`
        );
      } else if (result.failures.length === 0) {
        showMessage(`Tháng ${selectedMonth}/${selectedYear} không còn nhân sự nào cần chi trả.`);
      }
      await reload();
    } catch (err) {
      showError(err, "Không thể chi trả lương toàn bộ nhân sự");
    } finally {
      setIsPayingAll(false);
    }
  };

  const handleExport = () => {
    const data = sortedItems.map((item, idx) => ({
      "STT": idx + 1,
      "Mã nhân viên": item.employeeId ?? "Chưa cấp",
      "Họ và tên": item.fullName || item.email,
      "Số điện thoại": item.phoneNumber ?? "—",
      "Bộ phận": item.department ?? "—",
      "Chức vụ": item.position ?? "—",
      "Lương cơ bản": item.baseSalary,
      "Phụ cấp": item.allowance,
      "Nghỉ thực tế (ngày)": item.leaveDays,
      "Định mức phép (ngày)": item.allowedLeaveDays,
      "Vượt phép (ngày)": item.exceededDays,
      "Khấu trừ": item.deduction,
      "Thực nhận (Net)": item.netSalary,
      "Thực nhận tháng trước": item.previousNetSalary,
      "Chênh lệch": changeOf(item),
      "Trạng thái": item.status === "Paid" ? "Đã thanh toán" : "Chưa thanh toán",
      "Ngày chi trả": formatDate(item.paidAt),
    }));

    const worksheet = XLSX.utils.json_to_sheet(data);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "BangLuong");
    XLSX.writeFile(workbook, `Bang_Luong_Nhan_Vien_Thang_${selectedMonth}_Nam_${selectedYear}.xlsx`);
  };

  // ── Báo cáo năm ────────────────────────────────────────────────────────────

  const toggleYearly = () => {
    const opening = !showYearly;
    setShowYearly(opening);
    // Khu vực này nằm cuối trang: mở ra thì đưa luôn vào tầm mắt
    if (opening) {
      setTimeout(
        () => document.getElementById("bao-cao-nam")?.scrollIntoView({ behavior: "smooth", block: "start" }),
        80
      );
    }
  };

  const loadYearly = useCallback(async (year: number) => {
    setYearlyLoading(true);
    setYearlyError(null);
    try {
      setYearly(await getPayrollYearlyApi(year));
    } catch (err) {
      setYearly(null);
      setYearlyError(err instanceof Error ? err.message : "Không thể tải báo cáo lương theo năm");
    } finally {
      setYearlyLoading(false);
    }
  }, []);

  useEffect(() => {
    if (showYearly) loadYearly(yearlyYear);
  }, [showYearly, yearlyYear, loadYearly]);

  const totalNet = summary?.totalNet ?? 0;
  const totalPaid = summary?.totalPaid ?? 0;
  const totalRemaining = totalNet - totalPaid;
  const paidCount = summary?.paidCount ?? 0;
  const pendingCount = summary?.pendingCount ?? 0;
  const totalStaff = summary?.totalStaff ?? 0;
  const paidPercent = totalStaff > 0 ? Math.round((paidCount / totalStaff) * 100) : 0;
  const isFiltered = searchTerm !== "" || deptFilter !== "All";
  const previousTotalNet = summary?.previousTotalNet ?? 0;
  const totalChange = totalNet - previousTotalNet;
  const totalChangePercent = previousTotalNet > 0 ? (totalChange / previousTotalNet) * 100 : 0;

  // Kỳ liền trước (tháng 1 lùi về tháng 12 năm ngoái)
  const previousMonthLabel =
    selectedMonth === 1 ? `12/${selectedYear - 1}` : `${selectedMonth - 1}/${selectedYear}`;
  const increasedCount = items.filter((i) => i.previousNetSalary > 0 && i.netSalary > i.previousNetSalary).length;
  const decreasedCount = items.filter((i) => i.previousNetSalary > 0 && i.netSalary < i.previousNetSalary).length;
  const newcomerCount = items.filter((i) => i.previousNetSalary === 0 && i.netSalary > 0).length;

  // Hàm dựng ô tiêu đề, KHÔNG phải component — tạo component trong lúc render sẽ khiến
  // React coi mỗi lần render là một loại component mới và remount cả cụm.
  const sortTh = (column: SortKey, label: string, align: Align = "left") => {
    const active = sortKey === column;
    return (
      <th className={`${TH_CLASS} ${ALIGN_CLASS[align]}`}>
        <button
          onClick={() => toggleSort(column)}
          className={`inline-flex items-center gap-0.5 uppercase cursor-pointer hover:text-slate-600 transition-colors ${
            active ? "text-slate-600" : ""
          }`}
          title={`Sắp xếp theo ${label}`}
        >
          {label}
          <span className={`inline-block ml-1 text-[9px] leading-none ${active ? "text-primary" : "text-slate-300"}`}>
            {active ? (sortDir === "asc" ? "▲" : "▼") : "⇅"}
          </span>
        </button>
      </th>
    );
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="payroll" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <OwnerPageHeader
          title="Bảng Lương Nhân Viên"
          subtitle="Quản lý đãi ngộ, phê duyệt bảng thanh toán lương hàng tháng."
        />

        {/* Toast nổi bên trên nội dung — không chiếm chỗ trong luồng nên trang không bị
            đẩy xuống lúc hiện và không nhảy lên lúc thông báo biến mất */}
        <Portal>
        <div className="fixed top-24 right-8 z-50 w-[min(28rem,calc(100vw-4rem))] space-y-3 pointer-events-none">
          {successMsg && (
            <div className="animate-fade-in pointer-events-auto bg-emerald-50 border border-emerald-200 text-emerald-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-emerald-900/5">
              <span className="mt-px">✓</span>
              <span className="flex-1">{successMsg}</span>
            </div>
          )}

          {errorMsg && (
            <div className="animate-fade-in pointer-events-auto bg-rose-50 border border-rose-200 text-rose-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-rose-900/5">
              <span className="mt-px">!</span>
              <span className="flex-1">{errorMsg}</span>
              <button
                onClick={() => setErrorMsg(null)}
                aria-label="Đóng thông báo"
                className="text-rose-400 hover:text-rose-600 font-black cursor-pointer leading-none"
              >
                ×
              </button>
            </div>
          )}

          {/* Danh sách nhân sự chi trả không thành công — tách riêng khỏi thông báo thành công */}
          {failures.length > 0 && (
            <div className="animate-fade-in pointer-events-auto bg-rose-50 border border-rose-200 text-rose-700 px-5 py-3 rounded-2xl shadow-lg shadow-rose-900/5">
              <div className="flex items-start gap-2">
                <span className="mt-px">✕</span>
                <span className="flex-1 text-[13.5px] font-black">
                  {failures.length} nhân sự chi trả không thành công
                </span>
                <button
                  onClick={() => setFailures([])}
                  aria-label="Đóng danh sách lỗi"
                  className="text-rose-400 hover:text-rose-600 font-black cursor-pointer leading-none"
                >
                  ×
                </button>
              </div>
              <ul className="mt-2 space-y-1 max-h-52 overflow-y-auto">
                {failures.map((f) => (
                  <li key={f.userId} className="text-[12px] font-semibold text-rose-600 leading-snug">
                    <span className="font-black">{f.fullName}</span>: {f.reason}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
        </Portal>

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          {summary && summary.missingSalaryCount > 0 && (
            <div className="bg-amber-50 border border-amber-100 text-amber-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-center gap-2">
              <span>⚠</span> {summary.missingSalaryCount} nhân sự chưa được thiết lập lương cơ bản trong hồ sơ — cập nhật ở trang Quản lý nhân sự trước khi chi trả.
            </div>
          )}

          {/* KPI Summary Block */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider block">
                Tổng quỹ lương tháng {selectedMonth}/{selectedYear}
              </span>
              <span className="text-2xl font-black text-slate-900 mt-1 block">{formatCurrency(totalNet)}</span>
              <span className="text-[11px] text-slate-400 font-semibold block mt-0.5">
                Của {totalStaff} bác sĩ &amp; nhân viên {isFiltered ? "khớp bộ lọc hiện tại" : "của phòng khám"}
              </span>

              <div className="mt-3 pt-3 border-t border-slate-100 space-y-1.5">
                <div className="flex items-baseline justify-between gap-2">
                  <span className="text-[11.5px] font-bold text-slate-500">
                    Đã chi trả <span className="text-slate-400 font-semibold">({paidCount} người)</span>
                  </span>
                  <span className="text-[13px] font-black text-emerald-600 whitespace-nowrap">{formatCurrency(totalPaid)}</span>
                </div>
                <div className="flex items-baseline justify-between gap-2">
                  <span className="text-[11.5px] font-bold text-slate-500">
                    Còn phải chi <span className="text-slate-400 font-semibold">({pendingCount} người)</span>
                  </span>
                  <span className="text-[13px] font-black text-amber-600 whitespace-nowrap">{formatCurrency(totalRemaining)}</span>
                </div>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Tiến độ giải ngân</span>
              <span className="text-2xl font-black text-slate-900 mt-1 block">
                {paidCount}<span className="text-slate-300">/</span>{totalStaff}{" "}
                <span className="text-sm font-semibold text-slate-400">nhân sự đã nhận lương</span>
              </span>

              <div className="mt-3 h-2 w-full rounded-full bg-slate-100 overflow-hidden">
                <div
                  className="h-full rounded-full bg-emerald-500 transition-all duration-500"
                  style={{ width: `${paidPercent}%` }}
                />
              </div>
              <div className="flex items-center justify-between gap-2 mt-2">
                <span className="text-[11px] text-slate-400 font-semibold">
                  {pendingCount > 0 ? `Còn ${pendingCount} nhân sự chờ chi trả` : "Đã chi trả xong toàn bộ"}
                </span>
                <span className="text-[11px] font-black text-slate-500">{paidPercent}%</span>
              </div>
              {(summary?.missingSalaryCount ?? 0) > 0 && (
                <span className="text-[11px] text-amber-600 font-bold block mt-1">
                  Trong đó {summary?.missingSalaryCount} nhân sự chưa thiết lập lương nên chưa chi trả được
                </span>
              )}
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider block">
                So với tháng {previousMonthLabel}
              </span>

              {previousTotalNet > 0 ? (
                <>
                  <div className="flex items-baseline gap-2 mt-1 flex-wrap">
                    <span className={`text-2xl font-black ${totalChange > 0 ? "text-rose-600" : totalChange < 0 ? "text-emerald-600" : "text-slate-900"}`}>
                      {totalChange > 0 ? "▲" : totalChange < 0 ? "▼" : ""} {formatCurrency(Math.abs(totalChange))}
                    </span>
                    <span className="text-[13px] font-black text-slate-400">
                      {totalChange > 0 ? "+" : totalChange < 0 ? "−" : ""}{Math.abs(totalChangePercent).toFixed(1)}%
                    </span>
                  </div>
                  <span className="text-[11px] text-slate-400 font-semibold block mt-0.5">
                    Quỹ tháng {previousMonthLabel}: {formatCurrency(previousTotalNet)}
                  </span>

                  <div className="mt-3 pt-3 border-t border-slate-100 space-y-1.5">
                    <div className="flex items-baseline justify-between gap-2">
                      <span className="text-[11.5px] font-bold text-slate-500">Nhân sự tăng lương</span>
                      <span className="text-[13px] font-black text-rose-500 whitespace-nowrap">{increasedCount} người</span>
                    </div>
                    <div className="flex items-baseline justify-between gap-2">
                      <span className="text-[11.5px] font-bold text-slate-500">
                        Nhân sự giảm lương <span className="text-slate-400 font-semibold">(nghỉ phép, khấu trừ)</span>
                      </span>
                      <span className="text-[13px] font-black text-emerald-600 whitespace-nowrap">{decreasedCount} người</span>
                    </div>
                    {newcomerCount > 0 && (
                      <div className="flex items-baseline justify-between gap-2">
                        <span className="text-[11.5px] font-bold text-slate-500">Chưa có lương kỳ trước</span>
                        <span className="text-[13px] font-black text-slate-500 whitespace-nowrap">{newcomerCount} người</span>
                      </div>
                    )}
                  </div>
                </>
              ) : (
                <div className="mt-2">
                  <span className="text-[15px] font-black text-slate-400 block">Chưa có dữ liệu kỳ trước</span>
                  <span className="text-[11px] text-slate-400 font-semibold block mt-1 leading-relaxed">
                    Tháng {previousMonthLabel} chưa có nhân sự nào được thiết lập lương nên không so sánh được.
                  </span>
                </div>
              )}
            </div>
          </div>

          {/* Filters & Actions Bar */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-3 w-full sm:w-auto flex-1">
              {/* Search */}
              <div className="relative flex-1 max-w-md">
                <svg className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
                <input
                  type="text"
                  placeholder="Tìm nhân viên (tên, mã)..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 text-[13.5px] placeholder:text-slate-400"
                />
              </div>

              {/* Department Selector */}
              <div className="relative">
                <select
                  value={deptFilter}
                  onChange={(e) => { setDeptFilter(e.target.value); setPage(1); }}
                  className="px-4 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none transition-all font-semibold text-slate-600 text-[13px] pr-8 appearance-none cursor-pointer"
                >
                  <option value="All">Tất cả bộ phận</option>
                  <option value="Đón tiếp & CSKH">Đón tiếp &amp; CSKH</option>
                  <option value="Phụ tá lâm sàng">Phụ tá lâm sàng</option>
                  <option value="Dentist">Nha sĩ</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Month Selector */}
              <div className="relative">
                <select
                  value={selectedMonth}
                  onChange={(e) => { setSelectedMonth(Number(e.target.value)); setPage(1); }}
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

              {/* Year Selector */}
              <div className="relative">
                <select
                  value={selectedYear}
                  onChange={(e) => { setSelectedYear(Number(e.target.value)); setPage(1); }}
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

            <div className="flex items-center gap-3 shrink-0">
              <button
                onClick={handleExport}
                disabled={sortedItems.length === 0}
                className="flex items-center gap-2 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-xs font-bold transition-all cursor-pointer shadow-sm disabled:opacity-50"
              >
                📊 Xuất File Excel
              </button>

              <button
                onClick={openConfirmPayAll}
                disabled={isPayingAll || isLoading || totalStaff === 0 || paidCount === totalStaff}
                title={
                  isFiltered
                    ? "Chi trả cho toàn bộ nhân sự chưa thanh toán của kỳ, không chỉ những người đang lọc"
                    : `Chi trả cho ${pendingCount} nhân sự còn lại của tháng ${selectedMonth}/${selectedYear}`
                }
                className="px-4 py-2 bg-primary hover:bg-primary-hover disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-black rounded-xl cursor-pointer shadow-md shadow-primary/20 transition-all whitespace-nowrap"
              >
                {isPayingAll
                  ? "Đang xử lý..."
                  : paidCount === totalStaff && totalStaff > 0
                    ? "Đã chi trả xong"
                    : `Thanh Toán Tất Cả${pendingCount > 0 ? ` (${pendingCount})` : ""}`}
              </button>
            </div>
          </div>

          {/* Table Container */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px]">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/80">
                    <th className={`${TH_CLASS} text-center w-12`}>STT</th>
                    {sortTh("name", "Nhân viên")}
                    {sortTh("department", "Bộ phận / Chức vụ")}
                    {sortTh("base", "Lương cơ bản", "right")}
                    {sortTh("allowance", "Phụ cấp", "right")}
                    {sortTh("deduction", "Khấu trừ", "right")}
                    {sortTh("net", "Thực nhận", "right")}
                    {sortTh("status", "Trạng thái", "center")}
                    {sortTh("paidAt", "Ngày trả", "center")}
                    <th className={`${TH_CLASS} text-center`}>Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoading ? (
                    <tr>
                      <td colSpan={10} className="px-5 py-12 text-center text-slate-400 font-semibold animate-pulse">
                        Đang tải danh sách lương nhân sự...
                      </td>
                    </tr>
                  ) : pagedItems.length === 0 ? (
                    <tr>
                      <td colSpan={10} className="px-5 py-12 text-center text-slate-400 font-semibold">
                        Không tìm thấy thông tin lương phù hợp.
                      </td>
                    </tr>
                  ) : (
                    pagedItems.map((item, idx) => {
                      return (
                        <tr key={item.userId} className="hover:bg-slate-50/50 transition-colors">
                          <td className="px-4 py-4 text-center text-slate-400 font-bold tabular-nums">
                            {(currentPage - 1) * PAGE_SIZE + idx + 1}
                          </td>
                          <td className="px-4 py-4">
                            <div className="flex items-center gap-3">
                              <div className="w-9 h-9 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-black text-xs shrink-0">
                                {item.fullName
                                  ? item.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
                                  : item.email.slice(0, 2).toUpperCase()}
                              </div>
                              <div>
                                <span className="font-bold text-slate-900 block">{item.fullName || item.email}</span>
                                <span className="text-[11px] font-mono text-slate-400 font-extrabold">{item.employeeId ?? "Chưa cấp ID"}</span>
                              </div>
                            </div>
                          </td>
                          <td className="px-4 py-4">
                            <div className="font-semibold text-slate-700">{item.department ?? "—"}</div>
                            <div className="text-[11px] text-slate-400 font-semibold mt-0.5">{item.position ?? item.role}</div>
                          </td>
                          <td className="px-4 py-4 text-right font-bold text-slate-700 tabular-nums">
                            {item.hasSalaryConfigured ? (
                              formatCurrency(item.baseSalary)
                            ) : (
                              <span className="text-amber-600 text-[11.5px] font-bold">Chưa thiết lập</span>
                            )}
                          </td>
                          <td className="px-4 py-4 text-right text-emerald-600 font-bold tabular-nums">+{formatCurrency(item.allowance)}</td>
                          <td className="px-4 py-4 text-right">
                            <div className="text-rose-500 font-bold tabular-nums">-{formatCurrency(item.deduction)}</div>
                            <div className="text-[10px] text-slate-400 font-bold mt-0.5 block whitespace-nowrap">
                              {item.leaveDays > 0 ? (
                                <span>
                                  Nghỉ {formatDays(item.leaveDays)}n/{formatDays(item.allowedLeaveDays)}n phép{" "}
                                  {item.exceededDays > 0 && (
                                    <span className="text-rose-400 font-extrabold">(vượt {formatDays(item.exceededDays)})</span>
                                  )}
                                </span>
                              ) : (
                                <span>Chưa nghỉ phép</span>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-4 text-right font-black text-slate-900 tabular-nums">{formatCurrency(item.netSalary)}</td>
                          <td className="px-4 py-4 text-center">
                            <span
                              className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-black ${
                                item.status === "Paid"
                                  ? "bg-green-50 text-green-700 border border-green-200"
                                  : "bg-amber-50 text-amber-700 border border-amber-200"
                              }`}
                            >
                              <span className={`w-1.5 h-1.5 rounded-full ${item.status === "Paid" ? "bg-green-500" : "bg-amber-500"}`} />
                              {item.status === "Paid" ? "Đã trả" : "Chờ duyệt"}
                            </span>
                          </td>
                          <td className="px-4 py-4 text-center text-slate-500 font-semibold font-mono text-xs">
                            {formatDate(item.paidAt)}
                          </td>
                          <td className="px-4 py-4 text-center">
                            <button
                              onClick={() => handleTogglePay(item.userId, item.fullName || item.email, item.status === "Paid")}
                              disabled={busyUserId === item.userId || (item.status !== "Paid" && item.netSalary <= 0)}
                              title={
                                item.status !== "Paid" && item.netSalary <= 0
                                  ? "Nhân sự chưa được thiết lập lương trong hồ sơ"
                                  : undefined
                              }
                              className={`px-3 py-1.5 rounded-lg text-xs font-black transition-all cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed ${
                                item.status === "Paid"
                                  ? "bg-red-50 hover:bg-red-100 text-primary border border-red-200"
                                  : "bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border border-emerald-200"
                              }`}
                            >
                              {busyUserId === item.userId ? "..." : item.status === "Paid" ? "Hoàn tác" : "Chi Trả"}
                            </button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {!isLoading && sortedItems.length > 0 && (
              <div className="flex flex-col sm:flex-row items-center justify-between gap-3 px-5 py-3.5 border-t border-slate-100 bg-slate-50/50">
                <span className="text-[12px] text-slate-500 font-bold">
                  Hiển thị <span className="text-slate-700">{firstIndex}–{lastIndex}</span> trên{" "}
                  <span className="text-slate-700">{sortedItems.length}</span> nhân sự
                </span>

                <div className="flex items-center gap-1.5">
                  <button
                    onClick={() => setPage(currentPage - 1)}
                    disabled={currentPage === 1}
                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white text-[12px] font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer transition-all"
                  >
                    ‹ Trước
                  </button>

                  {Array.from({ length: totalPages }, (_, i) => i + 1)
                    .filter((p) => p === 1 || p === totalPages || Math.abs(p - currentPage) <= 1)
                    .map((p, i, arr) => (
                      <React.Fragment key={p}>
                        {i > 0 && arr[i - 1] !== p - 1 && <span className="text-slate-300 font-bold px-1">…</span>}
                        <button
                          onClick={() => setPage(p)}
                          className={`min-w-[2rem] px-2 py-1.5 rounded-lg text-[12px] font-black cursor-pointer transition-all ${
                            p === currentPage
                              ? "bg-primary text-white shadow-sm shadow-primary/20"
                              : "border border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
                          }`}
                        >
                          {p}
                        </button>
                      </React.Fragment>
                    ))}

                  <button
                    onClick={() => setPage(currentPage + 1)}
                    disabled={currentPage === totalPages}
                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white text-[12px] font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer transition-all"
                  >
                    Sau ›
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* ── Báo cáo quỹ lương theo năm ─────────────────────────────────── */}
          <section
            id="bao-cao-nam"
            className="rounded-2xl border-2 border-primary/20 shadow-lg shadow-primary/10 overflow-hidden bg-white"
          >
            <button
              onClick={toggleYearly}
              aria-expanded={showYearly}
              aria-controls="bao-cao-nam-noi-dung"
              className="w-full flex items-center justify-between gap-4 px-6 py-5 cursor-pointer text-left bg-gradient-to-r from-primary to-primary-hover hover:from-primary-hover hover:to-primary-hover transition-colors"
            >
              <div className="flex items-center gap-4 min-w-0">
                {/* Biểu tượng cột — nhắc lại đúng hình dạng của biểu đồ bên trong.
                    Trên nền đỏ phải dùng sắc trắng, cột màu đỏ sẽ chìm mất. */}
                <span className="w-11 h-11 rounded-xl bg-white/15 flex items-end justify-center gap-[3px] p-2.5 shrink-0">
                  <span className="w-1.5 rounded-sm bg-white/50" style={{ height: "40%" }} />
                  <span className="w-1.5 rounded-sm bg-white/80" style={{ height: "75%" }} />
                  <span className="w-1.5 rounded-sm bg-white" style={{ height: "100%" }} />
                </span>
                <div className="min-w-0">
                  <span className="text-[15px] font-extrabold text-white block">Báo cáo quỹ lương theo năm</span>
                  <span className="text-[12px] text-red-100 font-semibold block leading-snug">
                    Diễn biến 12 tháng: tổng quỹ, phần đã chi và phần còn phải chi.
                  </span>
                </div>
              </div>

              <span className="flex items-center gap-2.5 shrink-0">
                <span className="hidden sm:inline text-[12px] font-black text-white uppercase tracking-wide">
                  {showYearly ? "Thu gọn" : "Xem báo cáo"}
                </span>
                <span className={`w-8 h-8 rounded-full bg-white/20 text-white flex items-center justify-center text-lg leading-none transition-transform ${showYearly ? "rotate-180" : ""}`}>
                  ⌄
                </span>
              </span>
            </button>

            {showYearly && (
              <div id="bao-cao-nam-noi-dung" className="p-5 space-y-5">
                {/* Thanh điều khiển */}
                <div className="flex flex-wrap items-center justify-between gap-3">
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

                  <div className="inline-flex rounded-xl border border-slate-200 overflow-hidden">
                    {(["chart", "table"] as const).map((v) => (
                      <button
                        key={v}
                        onClick={() => setYearlyView(v)}
                        className={`px-3.5 py-2 text-[12px] font-black cursor-pointer transition-colors ${
                          yearlyView === v ? "bg-primary text-white" : "bg-white text-slate-500 hover:bg-slate-50"
                        }`}
                      >
                        {v === "chart" ? "Biểu đồ" : "Bảng"}
                      </button>
                    ))}
                  </div>
                </div>

                {yearlyLoading ? (
                  <div className="py-16 text-center text-slate-400 font-semibold animate-pulse">Đang tải báo cáo năm...</div>
                ) : yearlyError ? (
                  <div className="py-10 text-center text-rose-600 font-bold text-[13px]">{yearlyError}</div>
                ) : yearly ? (
                  <>
                    {/* Con số dẫn dắt của cả năm, đứng trước biểu đồ */}
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                      <div className="rounded-xl px-5 py-4 bg-primary shadow-md shadow-primary/20 lg:row-span-1">
                        <span className="text-[10.5px] font-extrabold text-red-100 uppercase tracking-wider block leading-tight">
                          Tổng quỹ lương năm {yearly.year}
                        </span>
                        <span className="text-[26px] font-black text-white mt-1.5 block leading-none">
                          {formatCurrency(yearly.totalNet)}
                        </span>
                        <span className="text-[11px] text-red-100 font-semibold block mt-2">
                          Đã chi {formatCurrency(yearly.totalPaid)} · còn{" "}
                          {formatCurrency(yearly.totalNet - yearly.totalPaid)}
                        </span>
                      </div>

                      <div className="lg:col-span-2 grid grid-cols-1 sm:grid-cols-3 gap-4">
                        {[
                          { label: "Trung bình mỗi tháng", value: formatCurrency(yearly.averageMonthlyNet) },
                          { label: "Tháng chi cao nhất", value: `Tháng ${yearly.peakMonth}` },
                          { label: "Tổng khấu trừ cả năm", value: formatCurrency(yearly.totalDeduction) },
                        ].map((s) => (
                          <div key={s.label} className="bg-slate-50/70 border border-slate-100 rounded-xl px-4 py-3 flex flex-col justify-center">
                            <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block leading-tight">{s.label}</span>
                            <span className="text-[15px] font-black text-slate-900 mt-1 block">{s.value}</span>
                          </div>
                        ))}
                      </div>
                    </div>

                    {yearlyView === "chart" ? (
                      <YearlyChart
                        data={yearly}
                        hoverMonth={hoverMonth}
                        onHover={setHoverMonth}
                        formatCurrency={formatCurrency}
                        formatCompact={formatCompact}
                      />
                    ) : (
                      <div className="overflow-x-auto">
                        <table className="w-full text-[13px]">
                          <thead>
                            <tr className="border-b border-slate-150 bg-slate-50/80">
                              <th className={`${TH_CLASS} text-left`}>Tháng</th>
                              <th className={`${TH_CLASS} text-center`}>Nhân sự</th>
                              <th className={`${TH_CLASS} text-center`}>Đã trả</th>
                              <th className={`${TH_CLASS} text-right`}>Tổng quỹ</th>
                              <th className={`${TH_CLASS} text-right`}>Đã chi</th>
                              <th className={`${TH_CLASS} text-right`}>Còn phải chi</th>
                              <th className={`${TH_CLASS} text-right`}>Khấu trừ</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-slate-100">
                            {yearly.months.map((m) => (
                              <tr key={m.month} className="hover:bg-slate-50/50">
                                <td className="px-4 py-2.5 font-bold text-slate-700">Tháng {m.month}</td>
                                <td className="px-4 py-2.5 text-center text-slate-500 font-semibold tabular-nums">{m.staffCount}</td>
                                <td className="px-4 py-2.5 text-center text-slate-500 font-semibold tabular-nums">{m.paidCount}</td>
                                <td className="px-4 py-2.5 text-right font-black text-slate-900 tabular-nums">{formatCurrency(m.totalNet)}</td>
                                <td className="px-4 py-2.5 text-right font-bold text-slate-600 tabular-nums">{formatCurrency(m.totalPaid)}</td>
                                <td className="px-4 py-2.5 text-right font-bold text-slate-600 tabular-nums">{formatCurrency(m.totalNet - m.totalPaid)}</td>
                                <td className="px-4 py-2.5 text-right font-bold text-rose-500 tabular-nums">{formatCurrency(m.totalDeduction)}</td>
                              </tr>
                            ))}
                          </tbody>
                          <tfoot>
                            <tr className="border-t-2 border-slate-200 bg-slate-50/80">
                              <td className="px-4 py-3 font-black text-slate-700" colSpan={3}>Cả năm {yearly.year}</td>
                              <td className="px-4 py-3 text-right font-black text-slate-900 tabular-nums">{formatCurrency(yearly.totalNet)}</td>
                              <td className="px-4 py-3 text-right font-black text-slate-700 tabular-nums">{formatCurrency(yearly.totalPaid)}</td>
                              <td className="px-4 py-3 text-right font-black text-slate-700 tabular-nums">{formatCurrency(yearly.totalNet - yearly.totalPaid)}</td>
                              <td className="px-4 py-3 text-right font-black text-rose-500 tabular-nums">{formatCurrency(yearly.totalDeduction)}</td>
                            </tr>
                          </tfoot>
                        </table>
                      </div>
                    )}

                    <p className="text-[11px] text-slate-400 font-semibold leading-relaxed">
                      Tháng đã chi trả lấy đúng số đã chốt; tháng chưa chi trả được tính lại theo hồ sơ lương
                      và đơn nghỉ hiện tại, nên có thể còn thay đổi.
                    </p>
                  </>
                ) : null}
              </div>
            )}
          </section>
        </div>
      </main>

      {/* ── Hộp thoại xác nhận chi trả toàn bộ ───────────────────────────────── */}
      {confirmOpen && (
        <Portal>
        <div
          className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4"
          onClick={() => setConfirmOpen(false)}
          role="dialog"
          aria-modal="true"
          aria-label="Xác nhận chi trả toàn bộ"
        >
          <div
            className="animate-fade-in bg-white rounded-2xl shadow-2xl w-full max-w-lg p-6 max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="text-lg font-extrabold text-slate-900">Xác nhận chi trả toàn bộ</h2>
            <p className="text-[13.5px] text-slate-500 font-semibold mt-1.5 leading-relaxed">
              Thao tác này chi trả lương <span className="font-black text-slate-700">tháng {selectedMonth}/{selectedYear}</span> cho
              tất cả nhân sự chưa thanh toán của kỳ và <span className="font-black text-slate-700">không thể hoàn tác hàng loạt</span> —
              muốn hủy phải hoàn tác từng người.
            </p>

            <div className="mt-4 rounded-xl border border-slate-200 bg-slate-50/70 p-4">
              {confirmLoading ? (
                <span className="text-[13px] text-slate-400 font-bold animate-pulse">Đang tính số liệu toàn kỳ...</span>
              ) : confirmTotals ? (
                <div className="space-y-2">
                  <div className="flex items-baseline justify-between gap-3">
                    <span className="text-[12.5px] font-bold text-slate-500">Số nhân sự sẽ được chi trả</span>
                    <span className="text-[15px] font-black text-slate-900">{confirmTotals.count} người</span>
                  </div>
                  <div className="flex items-baseline justify-between gap-3">
                    <span className="text-[12.5px] font-bold text-slate-500">Tổng số tiền dự kiến</span>
                    <span className="text-[15px] font-black text-slate-900">{formatCurrency(confirmTotals.amount)}</span>
                  </div>
                </div>
              ) : (
                <span className="text-[13px] text-slate-500 font-bold">
                  Không lấy được số liệu tổng hợp — thao tác vẫn áp dụng cho toàn bộ nhân sự chưa thanh toán của kỳ.
                </span>
              )}
            </div>

            {isFiltered && (
              <p className="text-[12px] text-amber-700 font-bold mt-3 bg-amber-50 border border-amber-100 rounded-xl px-3.5 py-2.5">
                Bạn đang lọc danh sách. Số liệu trên là của <span className="underline">toàn bộ kỳ</span>, không phải phần đang hiển thị.
              </p>
            )}

            {(summary?.missingSalaryCount ?? 0) > 0 && (
              <p className="text-[12px] text-slate-500 font-semibold mt-3">
                Nhân sự chưa thiết lập lương sẽ bị bỏ qua và liệt kê riêng sau khi chạy xong.
              </p>
            )}

            <div className="flex items-center justify-end gap-3 mt-6">
              <button
                onClick={() => setConfirmOpen(false)}
                className="px-4 py-2.5 rounded-xl border border-slate-200 bg-white text-slate-600 text-xs font-black hover:bg-slate-50 cursor-pointer transition-all"
              >
                Hủy
              </button>
              <button
                onClick={handlePayAll}
                disabled={confirmLoading}
                className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-hover disabled:opacity-50 text-white text-xs font-black cursor-pointer shadow-md shadow-primary/20 transition-all"
              >
                Xác nhận chi trả
              </button>
            </div>
          </div>
        </div>
        </Portal>
      )}
    </div>
  );
}

// ── Biểu đồ cột chồng 12 tháng ─────────────────────────────────────────────
// Cột = tổng quỹ lương của tháng, chia hai phần: đã chi trả và còn phải chi.
// Vẽ tay bằng SVG vì dự án không có thư viện biểu đồ nào.

function YearlyChart({
  data, hoverMonth, onHover, formatCurrency, formatCompact,
}: {
  data: PayrollYearlyDto;
  hoverMonth: number | null;
  onHover: (m: number | null) => void;
  formatCurrency: (v: number) => string;
  formatCompact: (v: number) => string;
}) {
  const W = 760, H = 300;
  const padL = 62, padR = 12, padT = 16, padB = 30;
  const plotW = W - padL - padR;
  const plotH = H - padT - padB;
  const band = plotW / 12;
  const barW = Math.min(24, band - 14);

  // Sàn 1 triệu để năm chưa có dữ liệu không sinh ra nhãn trục kiểu "0.25"
  const maxValue = Math.max(...data.months.map((m) => m.totalNet), 1_000_000);
  // Làm tròn trục lên số đẹp (1/2/5 × 10^n) để nhãn trục dễ đọc
  const niceMax = (() => {
    const pow = Math.pow(10, Math.floor(Math.log10(maxValue)));
    const n = maxValue / pow;
    const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
    return step * pow;
  })();

  const y = (v: number) => padT + plotH - (v / niceMax) * plotH;
  const ticks = [0, 0.25, 0.5, 0.75, 1].map((t) => t * niceMax);

  const hovered = hoverMonth ? data.months.find((m) => m.month === hoverMonth) ?? null : null;
  // Tâm cột đang trỏ, quy ra % chiều rộng khung (SVG co giãn nên phải tính theo tỉ lệ)
  const tooltipCenterPercent = hovered ? ((padL + (hovered.month - 0.5) * band) / W) * 100 : 0;

  return (
    <div className="relative">
      <svg viewBox={`0 0 ${W} ${H}`} className="w-full h-auto" role="img"
        aria-label={`Biểu đồ quỹ lương 12 tháng năm ${data.year}`}>
        {/* Lưới + nhãn trục dọc */}
        {ticks.map((t) => (
          <g key={t}>
            <line x1={padL} x2={W - padR} y1={y(t)} y2={y(t)} stroke={CHART_GRID} strokeWidth={1} />
            <text x={padL - 10} y={y(t) + 4} textAnchor="end" fontSize={11} fill={CHART_AXIS_TEXT}
              fontWeight={600} style={{ fontVariantNumeric: "tabular-nums" }}>
              {t === 0 ? "0" : formatCompact(t)}
            </text>
          </g>
        ))}

        {data.months.map((m, i) => {
          const x = padL + i * band + (band - barW) / 2;
          const isHover = hoverMonth === m.month;
          const totalH = (m.totalNet / niceMax) * plotH;
          const paidH = (m.totalPaid / niceMax) * plotH;
          const remainH = Math.max(0, totalH - paidH);
          const baseY = padT + plotH;
          // Khe 2px màu nền tách hai đoạn của cột chồng
          const gap = paidH > 0 && remainH > 2 ? 2 : 0;

          return (
            <g key={m.month}>
              {/* Đoạn "còn phải chi" nằm trên, bo tròn đầu cột */}
              {remainH > 0 && (
                <path
                  d={roundedTopRect(x, baseY - totalH, barW, remainH - gap > 0 ? remainH - gap : remainH, 4)}
                  fill={CHART_REMAINING}
                  opacity={hoverMonth === null || isHover ? 1 : 0.45}
                />
              )}
              {/* Đoạn "đã chi trả" đứng trên đường cơ sở */}
              {paidH > 0 && (
                <path
                  d={remainH > 0
                    ? `M${x},${baseY - paidH} h${barW} v${paidH} h${-barW} Z`
                    : roundedTopRect(x, baseY - paidH, barW, paidH, 4)}
                  fill={CHART_PAID}
                  opacity={hoverMonth === null || isHover ? 1 : 0.45}
                />
              )}

              {/* Vùng bắt hover rộng hơn cột để dễ trỏ */}
              <rect
                x={padL + i * band} y={padT} width={band} height={plotH}
                fill="transparent"
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

        {/* Đường cơ sở */}
        <line x1={padL} x2={W - padR} y1={padT + plotH} y2={padT + plotH} stroke="#cbd5e1" strokeWidth={1} />
      </svg>

      {/* Chú giải — luôn có vì biểu đồ có 2 thành phần */}
      <div className="flex items-center gap-5 mt-2 pl-1">
        <span className="inline-flex items-center gap-2 text-[11.5px] font-bold text-slate-500">
          <span className="w-3 h-3 rounded-sm" style={{ background: CHART_PAID }} /> Đã chi trả
        </span>
        <span className="inline-flex items-center gap-2 text-[11.5px] font-bold text-slate-500">
          <span className="w-3 h-3 rounded-sm" style={{ background: CHART_REMAINING }} /> Còn phải chi
        </span>
      </div>

      {/* Tooltip — sát mép trái/phải thì neo theo mép đó thay vì căn giữa,
          nếu không các tháng đầu và cuối (nhất là T12) sẽ bị tràn ra ngoài khung. */}
      {hovered && (
        <div
          className="absolute -top-1 z-10 pointer-events-none bg-white border border-slate-200 rounded-xl shadow-xl px-3.5 py-2.5 w-[210px] max-w-[calc(100%-0.5rem)]"
          style={{
            left: `${tooltipCenterPercent}%`,
            transform: `translateX(${
              tooltipCenterPercent < 22 ? "0%" : tooltipCenterPercent > 78 ? "-100%" : "-50%"
            })`,
          }}
        >
          <span className="text-[12px] font-black text-slate-900 block">Tháng {hovered.month}/{data.year}</span>
          <div className="mt-1.5 space-y-1">
            <Row label="Tổng quỹ" value={formatCurrency(hovered.totalNet)} bold />
            <Row label="Đã chi trả" value={formatCurrency(hovered.totalPaid)} dot={CHART_PAID} />
            <Row label="Còn phải chi" value={formatCurrency(hovered.totalNet - hovered.totalPaid)} dot={CHART_REMAINING} />
            <Row label="Nhân sự đã trả" value={`${hovered.paidCount}/${hovered.staffCount}`} />
          </div>
        </div>
      )}
    </div>
  );
}

function Row({ label, value, dot, bold }: { label: string; value: string; dot?: string; bold?: boolean }) {
  return (
    <div className="flex items-center justify-between gap-4 whitespace-nowrap">
      <span className="inline-flex items-center gap-1.5 text-[11px] font-bold text-slate-500">
        {dot && <span className="w-2 h-2 rounded-sm shrink-0" style={{ background: dot }} />}
        {label}
      </span>
      <span className={`text-[11.5px] tabular-nums ${bold ? "font-black text-slate-900" : "font-bold text-slate-600"}`}>
        {value}
      </span>
    </div>
  );
}

/** Hình chữ nhật bo tròn ở đầu cột, vuông góc ở chân cột (đường cơ sở). */
function roundedTopRect(x: number, yTop: number, w: number, h: number, r: number) {
  const radius = Math.min(r, h, w / 2);
  return `M${x},${yTop + h} V${yTop + radius} Q${x},${yTop} ${x + radius},${yTop} H${x + w - radius} Q${x + w},${yTop} ${x + w},${yTop + radius} V${yTop + h} Z`;
}
