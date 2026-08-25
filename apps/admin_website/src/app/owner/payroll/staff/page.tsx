"use client";

import React, { useState, useEffect, useCallback, useMemo } from "react";
import { createPortal } from "react-dom";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../components/shared/OwnerPageHeader";
import Pagination from "../../../../components/shared/Pagination";
import { SortableTh, Th } from "../../../../components/shared/TableHeader";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import {
  getPayrollPeriodApi,
  payPayrollApi,
  unpayPayrollApi,
  payAllPayrollApi,
  createPayrollPeriodApi,
  calculatePayrollPeriodApi,
  approvePayrollPeriodApi,
  setPayrollBonusApi,
  type PayrollPeriodDto,
  type PayrollItemDto,
  type PayrollFailureDto,
} from "../../../../lib/apiClient";
import * as XLSX from "xlsx";

const PAGE_SIZE_DEFAULT = 10;

type SortKey =
  | "name" | "base" | "allowance"
  | "deduction" | "bonus" | "net" | "status" | "paidAt";
type SortDir = "asc" | "desc";

// Thứ tự vòng đời kỳ lương, dùng để sắp xếp cột Trạng thái theo tiến độ thay vì bảng chữ cái
const STATUS_ORDER: Record<string, number> = { NotCreated: 0, Draft: 1, Calculated: 2, Approved: 3, Paid: 4 };

const STATUS_BADGE: Record<string, { label: string; dot: string; cls: string }> = {
  NotCreated: { label: "Chưa tạo", dot: "bg-slate-400", cls: "bg-slate-50 text-slate-500 border border-slate-200" },
  Draft: { label: "Nháp", dot: "bg-slate-500", cls: "bg-slate-100 text-slate-600 border border-slate-250" },
  Calculated: { label: "Đã tính", dot: "bg-blue-500", cls: "bg-blue-50 text-blue-700 border border-blue-200" },
  Approved: { label: "Đã duyệt", dot: "bg-indigo-500", cls: "bg-indigo-50 text-indigo-700 border border-indigo-200" },
  Paid: { label: "Đã trả", dot: "bg-green-500", cls: "bg-green-50 text-green-700 border border-green-200" },
};

// Ô nhập tiền: hiển thị có dấu phân cách (5.000.000), parse về số.
const fmtMoneyInput = (n: number) => (n ? n.toLocaleString("vi-VN") : "");
const parseMoneyInput = (s: string) => Number(s.replace(/[^\d]/g, "")) || 0;

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

export default function OwnerPayrollStaffPage() {
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
  const [isCreatingPeriod, setIsCreatingPeriod] = useState(false);
  const [isCalculating, setIsCalculating] = useState(false);
  const [isApproving, setIsApproving] = useState(false);
  const [bonusDraft, setBonusDraft] = useState<Record<string, number>>({});
  const [savingBonusUserId, setSavingBonusUserId] = useState<string | null>(null);

  const [selectedMonth, setSelectedMonth] = useState(new Date().getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());

  const [sortKey, setSortKey] = useState<SortKey>("net");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_DEFAULT);

  // Hộp thoại xác nhận chi trả hàng loạt
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmTotals, setConfirmTotals] = useState<{ count: number; amount: number } | null>(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

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
        // Trang này chỉ dành riêng cho nhân viên — lọc theo vai trò
        role: "Staff",
        department: deptFilter === "All" ? undefined : deptFilter,
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

  const formatDate = (iso: string | null) => (iso ? new Date(iso).toLocaleDateString("vi-VN") : "—");

  const formatShifts = (val: number) => (Number.isInteger(val) ? String(val) : val.toFixed(1));

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
        case "base":       return it.baseSalary;
        case "allowance":  return it.allowance;
        case "deduction":  return it.deduction;
        case "bonus":      return it.bonus;
        case "net":        return it.netSalary;
        case "status":     return STATUS_ORDER[it.status] ?? 0;
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
      setSortDir(key === "name" ? "asc" : "desc");
    }
    setPage(1);
  };

  // ── Phân trang ─────────────────────────────────────────────────────────────

  const totalPages = Math.max(1, Math.ceil(sortedItems.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const pagedItems = sortedItems.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  const handlePageSizeChange = (size: number) => {
    setPageSize(size);
    setPage(1);
  };

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
      setConfirmTotals({ count: approvedCount, amount: approvedAmount });
      return;
    }
    // Đang lọc: số trên bảng không phải số sẽ chi, nên hỏi lại server toàn kỳ
    setConfirmLoading(true);
    try {
      const full = await getPayrollPeriodApi({ year: selectedYear, month: selectedMonth });
      setConfirmTotals({
        count: full.summary.approvedCount,
        amount: full.items.filter((i) => i.status === "Approved").reduce((sum, i) => sum + i.netSalary, 0),
      });
    } catch {
      setConfirmTotals(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  // ── Vòng đời kỳ lương: Tạo → Tính → Duyệt ───────────────────────────────────

  const handleCreatePeriod = async () => {
    setIsCreatingPeriod(true);
    try {
      const result = await createPayrollPeriodApi({ year: selectedYear, month: selectedMonth });
      showMessage(
        result.affectedCount > 0
          ? `Đã tạo kỳ lương nháp cho ${result.affectedCount} nhân sự.`
          : "Không có nhân sự nào cần tạo kỳ lương mới."
      );
      await reload();
    } catch (err) {
      showError(err, "Không thể tạo kỳ lương");
    } finally {
      setIsCreatingPeriod(false);
    }
  };

  const handleCalculatePeriod = async () => {
    setIsCalculating(true);
    try {
      const result = await calculatePayrollPeriodApi({ year: selectedYear, month: selectedMonth });
      showMessage(
        result.affectedCount > 0
          ? `Đã tính lương cho ${result.affectedCount} nhân sự.`
          : "Không có kỳ nháp nào cần tính lương."
      );
      await reload();
    } catch (err) {
      showError(err, "Không thể tính lương kỳ này");
    } finally {
      setIsCalculating(false);
    }
  };

  const handleApprovePeriod = async () => {
    setIsApproving(true);
    try {
      const result = await approvePayrollPeriodApi({ year: selectedYear, month: selectedMonth });
      showMessage(
        result.affectedCount > 0
          ? `Đã duyệt kỳ lương cho ${result.affectedCount} nhân sự.`
          : "Không có kỳ nào chờ duyệt."
      );
      await reload();
    } catch (err) {
      showError(err, "Không thể duyệt kỳ lương");
    } finally {
      setIsApproving(false);
    }
  };

  const handleBonusCommit = async (item: PayrollItemDto) => {
    const val = bonusDraft[item.userId];
    if (val === undefined || val === item.bonus) return;
    setSavingBonusUserId(item.userId);
    try {
      await setPayrollBonusApi({ year: selectedYear, month: selectedMonth, userId: item.userId, bonus: val });
      setBonusDraft((prev) => {
        const next = { ...prev };
        delete next[item.userId];
        return next;
      });
      await reload();
    } catch (err) {
      showError(err, "Không thể cập nhật thưởng");
    } finally {
      setSavingBonusUserId(null);
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
      "Lương cơ bản": item.baseSalary,
      "Phụ cấp": item.allowance,
      "Nghỉ thực tế (ca)": item.leaveShifts,
      "Định mức phép (ca)": item.allowedLeaveShifts,
      "Vượt phép (ca)": item.exceededShifts,
      "Khấu trừ": item.deduction,
      "Thưởng": item.bonus,
      "Thực nhận (Net)": item.netSalary,
      "Thực nhận tháng trước": item.previousNetSalary,
      "Chênh lệch": changeOf(item),
      "Trạng thái": STATUS_BADGE[item.status]?.label ?? item.status,
      "Ngày chi trả": formatDate(item.paidAt),
    }));

    const worksheet = XLSX.utils.json_to_sheet(data);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "NhanVien");
    XLSX.writeFile(workbook, `Bang_Luong_Nhan_Vien_Thang_${selectedMonth}_Nam_${selectedYear}.xlsx`);
  };

  const totalNet = summary?.totalNet ?? 0;
  const totalPaid = summary?.totalPaid ?? 0;
  const totalRemaining = totalNet - totalPaid;
  const paidCount = summary?.paidCount ?? 0;
  const pendingCount = summary?.pendingCount ?? 0;
  const totalStaff = summary?.totalStaff ?? 0;
  const notCreatedCount = summary?.notCreatedCount ?? 0;
  const draftCount = summary?.draftCount ?? 0;
  const calculatedCount = summary?.calculatedCount ?? 0;
  const approvedCount = summary?.approvedCount ?? 0;
  const approvedAmount = useMemo(
    () => items.filter((i) => i.status === "Approved").reduce((sum, i) => sum + i.netSalary, 0),
    [items]
  );
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

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="payroll-staff" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <OwnerPageHeader
          title="Bảng Lương Nhân Viên"
          subtitle="Quản lý đãi ngộ và chi trả lương hàng tháng cho nhân viên."
        />

        {/* Toast nổi bên trên nội dung — không chiếm chỗ trong luồng nên trang không bị
            đẩy xuống lúc hiện và không nhảy lên lúc thông báo biến mất */}
        <Portal>
        <div className="fixed top-24 right-8 z-50 w-[min(28rem,calc(100vw-4rem))] space-y-3 pointer-events-none">
          {successMsg && (
            <div className="animate-fade-in pointer-events-auto bg-emerald-50 border border-emerald-200 text-emerald-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-emerald-900/5">
              <svg className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
              <span className="flex-1">{successMsg}</span>
            </div>
          )}

          {errorMsg && (
            <div className="animate-fade-in pointer-events-auto bg-rose-50 border border-rose-200 text-rose-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-rose-900/5">
              <svg className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" /></svg>
              <span className="flex-1">{errorMsg}</span>
              <button
                onClick={() => setErrorMsg(null)}
                aria-label="Đóng thông báo"
                className="text-rose-400 hover:text-rose-600 p-0.5 cursor-pointer"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          )}

          {/* Danh sách nhân sự chi trả không thành công — tách riêng khỏi thông báo thành công */}
          {failures.length > 0 && (
            <div className="animate-fade-in pointer-events-auto bg-rose-50 border border-rose-200 text-rose-700 px-5 py-3 rounded-2xl shadow-lg shadow-rose-900/5">
              <div className="flex items-start gap-2">
                <svg className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                <span className="flex-1 text-[13.5px] font-black">
                  {failures.length} nhân sự chi trả không thành công
                </span>
                <button
                  onClick={() => setFailures([])}
                  aria-label="Đóng danh sách lỗi"
                  className="text-rose-400 hover:text-rose-600 p-0.5 cursor-pointer"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
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
              <svg className="w-4 h-4 text-amber-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
              <span>{summary.missingSalaryCount} nhân sự chưa được thiết lập lương cơ bản trong hồ sơ — cập nhật ở trang Quản lý nhân sự trước khi chi trả.</span>
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
                Của {totalStaff} nhân viên {isFiltered ? "khớp bộ lọc hiện tại" : "của phòng khám"}
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
                    <span className={`text-2xl font-black inline-flex items-center gap-1.5 ${totalChange > 0 ? "text-rose-600" : totalChange < 0 ? "text-emerald-600" : "text-slate-900"}`}>
                      {totalChange > 0 ? (
                        <svg className="w-5 h-5 text-rose-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 19.5l15-15m0 0H8.25m11.25 0v11.25" /></svg>
                      ) : totalChange < 0 ? (
                        <svg className="w-5 h-5 text-emerald-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 4.5l15 15m0 0V8.25m0 11.25H8.25" /></svg>
                      ) : null}
                      {formatCurrency(Math.abs(totalChange))}
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
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5">
            <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
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
                <svg className="w-4 h-4 text-emerald-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" /></svg>
                Xuất File Excel
              </button>

              <button
                onClick={openConfirmPayAll}
                disabled={isPayingAll || isLoading || approvedCount === 0}
                title={
                  approvedCount === 0
                    ? "Chưa có kỳ lương nào ở trạng thái Đã duyệt để chi trả"
                    : isFiltered
                      ? "Chi trả cho toàn bộ nhân sự đã duyệt của kỳ, không chỉ những người đang lọc"
                      : `Chi trả cho ${approvedCount} nhân sự đã duyệt của tháng ${selectedMonth}/${selectedYear}`
                }
                className="px-4 py-2 bg-primary hover:bg-primary-hover disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-black rounded-xl cursor-pointer shadow-md shadow-primary/20 transition-all whitespace-nowrap"
              >
                {isPayingAll
                  ? "Đang xử lý..."
                  : approvedCount === 0
                    ? "Không có kỳ chờ chi trả"
                    : `Thanh Toán Tất Cả (${approvedCount})`}
              </button>
            </div>
            </div>

            {/* Page Size Selector — hàng dưới, tách riêng khỏi bộ lọc bộ phận/tháng/năm */}
            <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
              <span>Hiển thị</span>
              <select
                value={pageSize}
                onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer"
              >
                {[10, 20, 50].map((n) => (<option key={n} value={n}>{n}</option>))}
              </select>
              <span>/ trang</span>
            </div>
          </div>

          {/* Quy trình duyệt lương: Tạo → Tính → Duyệt → Chi trả */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col lg:flex-row items-start lg:items-center justify-between gap-4">
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider mr-1">
                Quy trình kỳ {selectedMonth}/{selectedYear}
              </span>
              {(
                [
                  ["NotCreated", notCreatedCount],
                  ["Draft", draftCount],
                  ["Calculated", calculatedCount],
                  ["Approved", approvedCount],
                  ["Paid", paidCount],
                ] as const
              ).map(([key, count]) => (
                <span
                  key={key}
                  className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-black ${STATUS_BADGE[key].cls}`}
                >
                  <span className={`w-1.5 h-1.5 rounded-full ${STATUS_BADGE[key].dot}`} />
                  {STATUS_BADGE[key].label}: {count}
                </span>
              ))}
            </div>

            <div className="flex items-center gap-2 shrink-0">
              <button
                onClick={handleCreatePeriod}
                disabled={isCreatingPeriod || isLoading}
                title="Tạo kỳ lương nháp cho toàn bộ nhân sự chưa có bản ghi trong kỳ (áp dụng cho cả nha sĩ và nhân viên)"
                className="px-3.5 py-2 bg-white border border-slate-250 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed text-slate-600 text-xs font-black rounded-xl cursor-pointer shadow-sm transition-all whitespace-nowrap"
              >
                {isCreatingPeriod ? "Đang tạo..." : `Tạo kỳ lương${notCreatedCount > 0 ? ` (${notCreatedCount})` : ""}`}
              </button>
              <button
                onClick={handleCalculatePeriod}
                disabled={isCalculating || isLoading || draftCount === 0}
                title="Tính lại số liệu và chốt các kỳ đang Nháp sang Đã tính (áp dụng cho toàn bộ nhân sự, không riêng danh sách đang lọc)"
                className="px-3.5 py-2 bg-blue-50 border border-blue-200 hover:bg-blue-100 disabled:opacity-50 disabled:cursor-not-allowed text-blue-700 text-xs font-black rounded-xl cursor-pointer shadow-sm transition-all whitespace-nowrap"
              >
                {isCalculating ? "Đang tính..." : `Tính lương${draftCount > 0 ? ` (${draftCount})` : ""}`}
              </button>
              <button
                onClick={handleApprovePeriod}
                disabled={isApproving || isLoading || calculatedCount === 0}
                title="Duyệt các kỳ Đã tính sang Đã duyệt, đủ điều kiện chi trả (áp dụng cho toàn bộ nhân sự, không riêng danh sách đang lọc)"
                className="px-3.5 py-2 bg-indigo-50 border border-indigo-200 hover:bg-indigo-100 disabled:opacity-50 disabled:cursor-not-allowed text-indigo-700 text-xs font-black rounded-xl cursor-pointer shadow-sm transition-all whitespace-nowrap"
              >
                {isApproving ? "Đang duyệt..." : `Duyệt kỳ lương${calculatedCount > 0 ? ` (${calculatedCount})` : ""}`}
              </button>
            </div>
          </div>

          {/* Table Container */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px]">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/80">
                    <Th className="px-4" align="center">STT</Th>
                    <SortableTh column="name" label="Nhân viên" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} className="px-4" />
                    <SortableTh column="base" label="Lương cơ bản" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" className="px-4" />
                    <SortableTh column="allowance" label="Phụ cấp" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" className="px-4" />
                    <SortableTh column="deduction" label="Khấu trừ" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" className="px-4" />
                    <SortableTh column="bonus" label="Thưởng" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" className="px-4" />
                    <SortableTh column="net" label="Thực nhận" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="right" className="px-4" />
                    <SortableTh column="status" label="Trạng thái" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="center" className="px-4" />
                    <SortableTh column="paidAt" label="Ngày trả" sortKey={sortKey} sortDir={sortDir} onSort={toggleSort} align="center" className="px-4" />
                    <Th className="px-4" align="center">Hành động</Th>
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
                            {(currentPage - 1) * pageSize + idx + 1}
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
                          <td className="px-4 py-4 text-right font-bold text-slate-700 tabular-nums">
                            {item.hasSalaryConfigured ? (
                              <>
                                {formatCurrency(item.baseSalary)}
                                {item.employmentType && item.employmentType !== "Full-time" && (
                                  <div className="text-[10px] text-slate-400 font-bold mt-0.5">
                                    {formatShifts(item.requiredShifts)} ca
                                  </div>
                                )}
                              </>
                            ) : (
                              <span className="text-amber-600 text-[11.5px] font-bold">Chưa thiết lập</span>
                            )}
                          </td>
                          <td className="px-4 py-4 text-right text-emerald-600 font-bold tabular-nums">+{formatCurrency(item.allowance)}</td>
                          <td className="px-4 py-4 text-right">
                            <div className="text-rose-500 font-bold tabular-nums">-{formatCurrency(item.deduction)}</div>
                            <div className="text-[10px] text-slate-400 font-bold mt-0.5 block whitespace-nowrap">
                              {item.employmentType && item.employmentType !== "Full-time" ? (
                                <span>Theo ca — không áp dụng phép</span>
                              ) : item.leaveShifts > 0 ? (
                                <span>
                                  Nghỉ {formatShifts(item.leaveShifts)}c/{formatShifts(item.allowedLeaveShifts)}c phép{" "}
                                  {item.exceededShifts > 0 && (
                                    <span className="text-rose-400 font-extrabold">(vượt {formatShifts(item.exceededShifts)})</span>
                                  )}
                                </span>
                              ) : (
                                <span>Chưa nghỉ phép</span>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-4 text-right">
                            {item.status === "Draft" ? (
                              <input
                                type="text"
                                inputMode="numeric"
                                value={fmtMoneyInput(bonusDraft[item.userId] ?? item.bonus)}
                                onChange={(e) =>
                                  setBonusDraft((prev) => ({ ...prev, [item.userId]: parseMoneyInput(e.target.value) }))
                                }
                                onBlur={() => handleBonusCommit(item)}
                                disabled={savingBonusUserId === item.userId}
                                placeholder="0"
                                className="w-28 text-right px-2 py-1 bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 text-[13px] disabled:opacity-50"
                              />
                            ) : (
                              <span className="font-bold text-slate-700 tabular-nums">{formatCurrency(item.bonus)}</span>
                            )}
                          </td>
                          <td className="px-4 py-4 text-right font-black text-slate-900 tabular-nums">{formatCurrency(item.netSalary)}</td>
                          <td className="px-4 py-4 text-center">
                            <span
                              className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-black whitespace-nowrap ${STATUS_BADGE[item.status]?.cls ?? STATUS_BADGE.NotCreated.cls}`}
                            >
                              <span className={`w-1.5 h-1.5 rounded-full ${STATUS_BADGE[item.status]?.dot ?? STATUS_BADGE.NotCreated.dot}`} />
                              {STATUS_BADGE[item.status]?.label ?? item.status}
                            </span>
                          </td>
                          <td className="px-4 py-4 text-center text-slate-500 font-semibold font-mono text-xs">
                            {formatDate(item.paidAt)}
                          </td>
                          <td className="px-4 py-4 text-center">
                            {item.status === "Approved" || item.status === "Paid" ? (
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
                            ) : (
                              <span className="text-[11px] text-slate-400 font-bold">
                                {item.status === "NotCreated"
                                  ? "Chưa tạo kỳ"
                                  : item.status === "Draft"
                                    ? "Chưa tính lương"
                                    : "Chờ duyệt"}
                              </span>
                            )}
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
              <div className="px-5 py-3.5 border-t border-slate-100 bg-slate-50/50">
                <Pagination
                  currentPage={currentPage}
                  totalCount={sortedItems.length}
                  pageSize={pageSize}
                  onPageChange={setPage}
                  itemLabel="nhân sự"
                />
              </div>
            )}
          </div>
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
              tất cả nhân sự đã duyệt của kỳ và <span className="font-black text-slate-700">không thể hoàn tác hàng loạt</span> —
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
                  Không lấy được số liệu tổng hợp — thao tác vẫn áp dụng cho toàn bộ nhân sự đã duyệt của kỳ.
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
