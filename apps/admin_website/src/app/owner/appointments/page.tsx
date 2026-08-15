"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import Pagination from "../../../components/shared/Pagination";
import { SortableTh, Th, type SortDir } from "../../../components/shared/TableHeader";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import { getAppointmentsPagedApi, type StaffAppointmentDto } from "../../../lib/apiClient";
import * as XLSX from "xlsx";

/* ─── status config (khớp enum AppointmentStatus thật của backend) ───────── */

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

const STATUS_FILTER_OPTIONS: Array<{ value: string; label: string }> = [
  { value: "Completed,PendingPayment,InProgress", label: "Đã khám & đang khám" },
  { value: "", label: "Tất cả trạng thái" },
  { value: "InProgress", label: "Đang khám" },
  { value: "Completed", label: "Đã hoàn thành" },
  { value: "PendingPayment", label: "Chờ thanh toán" },
  { value: "CheckedIn", label: "Đã check-in" },
  { value: "Confirmed", label: "Đã xác nhận" },
  { value: "Pending", label: "Chờ xác nhận" },
  { value: "Cancelled", label: "Đã hủy" },
  { value: "NoShow", label: "Không đến" },
];

const PAGE_SIZE_OPTIONS = [10, 20, 50];

const selectCls = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";
const inputCls  = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700";

function formatDateTime(iso: string): { date: string; time: string } {
  const d = new Date(iso);
  const date = `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
  const time = `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
  return { date, time };
}

function getInitials(name: string): string {
  return name.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase();
}

type SortKey = "date";

export default function OwnerAppointmentsPage() {
  useRequireOwner();
  const router = useRouter();

  const [items, setItems] = useState<StaffAppointmentDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState(STATUS_FILTER_OPTIONS[0].value);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");

  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [sortKey] = useState<SortKey>("date");
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  // Gõ tìm kiếm không gọi API ngay — debounce 350ms
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearchTerm(searchQuery.trim());
      setCurrentPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  const fetchAppointments = useCallback(() => {
    setIsLoading(true);
    getAppointmentsPagedApi({
      startDate: dateFrom || undefined,
      endDate:   dateTo || undefined,
      status:    statusFilter || undefined,
      search:    searchTerm || undefined,
      page:      currentPage,
      pageSize,
      sortDir,
    })
      .then((res) => {
        setItems(res.items);
        setTotalCount(res.totalCount);
        setErrorMsg(null);
      })
      .catch((err) => setErrorMsg(err instanceof Error ? err.message : "Không thể tải danh sách ca khám"))
      .finally(() => setIsLoading(false));
  }, [dateFrom, dateTo, statusFilter, searchTerm, currentPage, pageSize, sortDir]);

  useEffect(() => { fetchAppointments(); }, [fetchAppointments]);

  const handleSort = () => {
    setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    setCurrentPage(1);
  };

  const handleExportExcel = () => {
    const headers = ["Mã lịch hẹn", "Bệnh nhân", "SĐT", "Nha sĩ", "Dịch vụ", "Ngày hẹn", "Giờ hẹn", "Trạng thái"];
    const rows = items.map((a) => {
      const { date, time } = formatDateTime(a.appointmentDate);
      return [
        a.appointmentCode, a.patientName, a.patientPhone || "—", a.dentistName,
        a.serviceName || "—", date, time, STATUS_CFG[a.status]?.label ?? a.status,
      ];
    });
    const ws = XLSX.utils.aoa_to_sheet([["DANH SÁCH CA KHÁM & ĐIỀU TRỊ"], headers, ...rows]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, "CaKham");
    XLSX.writeFile(wb, "Ca_Kham_Va_Dieu_Tri.xlsx");
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="appointments" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader
          title="Ca khám & điều trị"
          subtitle="Xem toàn bộ lịch hẹn đã khám, đang khám và sắp tới của bệnh nhân"
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {errorMsg && (
            <div className="bg-rose-50 border border-rose-100 text-rose-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold">
              {errorMsg}
            </div>
          )}

          {/* FILTER TOOLBAR */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4 shrink-0">
            <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3.5 flex-wrap">
              {/* Search */}
              <div className="relative flex-1 min-w-[220px]">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm theo tên/SĐT bệnh nhân, tên nha sĩ..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>

              {/* Status filter */}
              <div className="relative md:w-56">
                <select
                  value={statusFilter}
                  onChange={(e) => { setStatusFilter(e.target.value); setCurrentPage(1); }}
                  className={selectCls}
                >
                  {STATUS_FILTER_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Date range */}
              <div className="flex items-center gap-2 md:w-auto">
                <input
                  type="date"
                  value={dateFrom}
                  onChange={(e) => { setDateFrom(e.target.value); setCurrentPage(1); }}
                  className={`${inputCls} md:w-40`}
                  title="Từ ngày"
                />
                <span className="text-slate-300 font-bold shrink-0">–</span>
                <input
                  type="date"
                  value={dateTo}
                  onChange={(e) => { setDateTo(e.target.value); setCurrentPage(1); }}
                  className={`${inputCls} md:w-40`}
                  title="Đến ngày"
                />
              </div>
            </div>

            {/* Row 2 */}
            <div className="flex items-center justify-between gap-3 flex-wrap border-t border-slate-100 pt-3">
              <div className="flex items-center gap-2.5">
                <span className="text-[12.5px] text-slate-400 font-semibold">Hiển thị</span>
                <div className="relative">
                  <select
                    value={pageSize}
                    onChange={(e) => { setPageSize(Number(e.target.value)); setCurrentPage(1); }}
                    className="pl-3 pr-7 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-bold text-slate-650 appearance-none cursor-pointer"
                  >
                    {PAGE_SIZE_OPTIONS.map((n) => <option key={n} value={n}>{n}</option>)}
                  </select>
                  <span className="absolute inset-y-0 right-2 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </span>
                </div>
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">/ trang</span>
              </div>

              <button
                onClick={handleExportExcel}
                disabled={items.length === 0}
                className="flex items-center gap-1.5 px-4.5 py-2 bg-white border border-slate-250 hover:bg-slate-50 disabled:opacity-50 text-slate-600 rounded-xl text-[13px] font-bold transition-all shadow-sm cursor-pointer"
              >
                <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 8.25H7.5a2.25 2.25 0 0 0-2.25 2.25v9a2.25 2.25 0 0 0 2.25 2.25h10a2.25 2.25 0 0 0 2.25-2.25V10.5A2.25 2.25 0 0 0 17.5 8.25H16M9 8.25V4.5A2.25 2.25 0 0 1 11.25 2.25h1.5A2.25 2.25 0 0 1 15 4.5v3.75m-6 0h6m-6 5.25h6m-6 3h6" />
                </svg>
                Xuất file Excel
              </button>
            </div>
          </div>

          {/* TABLE CONTAINER */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex-1 flex flex-col">
            <div className="overflow-x-auto flex-1">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    <Th className="px-6">Mã lịch hẹn</Th>
                    <Th className="px-6">Bệnh nhân</Th>
                    <Th className="px-6">Nha sĩ</Th>
                    <Th className="px-6">Dịch vụ</Th>
                    <SortableTh column="date" label="Ngày giờ hẹn" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <Th className="px-6" align="center">Trạng thái</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoading ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-16 text-center font-bold text-slate-400 animate-pulse">
                        Đang tải danh sách ca khám...
                      </td>
                    </tr>
                  ) : items.length > 0 ? (
                    items.map((a) => {
                      const status = STATUS_CFG[a.status] ?? { label: a.status, badge: "bg-slate-100 text-slate-500 border-slate-200", dot: "bg-slate-400" };
                      const { date, time } = formatDateTime(a.appointmentDate);
                      return (
                        <tr
                          key={a.appointmentId}
                          onClick={() => router.push(`/owner/patients/${a.patientId}`)}
                          className="hover:bg-slate-50/50 transition-colors cursor-pointer group"
                        >
                          <td className="px-6 py-4">
                            <span className="font-black text-primary text-[13px]">{a.appointmentCode}</span>
                          </td>
                          <td className="px-6 py-4">
                            <div className="flex items-center gap-3">
                              <div className="w-9 h-9 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-black text-[12px] shrink-0">
                                {getInitials(a.patientName)}
                              </div>
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 truncate group-hover:text-primary transition-colors">{a.patientName}</div>
                                <div className="text-[11px] text-slate-400 font-semibold font-mono truncate">{a.patientPhone ?? "—"}</div>
                              </div>
                            </div>
                          </td>
                          <td className="px-6 py-4 font-bold text-slate-700">{a.dentistName}</td>
                          <td className="px-6 py-4 font-semibold text-slate-600">{a.serviceName ?? "—"}</td>
                          <td className="px-6 py-4">
                            <div className="font-bold text-slate-700">{date}</div>
                            <div className="text-[11px] text-slate-400 font-semibold font-mono">{time}</div>
                          </td>
                          <td className="px-6 py-4 text-center">
                            <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-black border ${status.badge}`}>
                              <span className={`w-1.5 h-1.5 rounded-full ${status.dot} ${a.status === "InProgress" ? "animate-pulse" : ""}`} />
                              {status.label}
                            </span>
                          </td>
                        </tr>
                      );
                    })
                  ) : (
                    <tr>
                      <td colSpan={6} className="px-6 py-16 text-center">
                        <div className="flex flex-col items-center gap-2">
                          <svg className="w-9 h-9 text-slate-355" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
                          </svg>
                          <div className="font-extrabold text-[14px] text-slate-500">Không tìm thấy ca khám phù hợp.</div>
                          <div className="text-[12px] text-slate-400 font-semibold">Thử thay đổi từ khóa hoặc bộ lọc.</div>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {!isLoading && totalCount > 0 && (
              <div className="border-t border-slate-100 px-5 py-3.5 bg-slate-50/25">
                <Pagination
                  currentPage={currentPage}
                  totalCount={totalCount}
                  pageSize={pageSize}
                  onPageChange={setCurrentPage}
                  itemLabel="ca khám"
                />
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
