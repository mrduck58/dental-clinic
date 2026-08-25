"use client";

import React, { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../components/shared/OwnerPageHeader";
import Pagination from "../../../../components/shared/Pagination";
import { SortableTh, Th, toggleSortState, type SortDir } from "../../../../components/shared/TableHeader";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import { getStaffApi, getWeekScheduleApi, getLeaveRequestsAdminApi, resolveAssetUrl, type StaffDto, type StaffStatsDto, type ScheduleEntryDto } from "../../../../lib/apiClient";
import { ROLE_LABELS, type UiRole } from "../../../../lib/roles";
import * as XLSX from "xlsx";

const STATUS_LABELS: Record<string, string> = {
  Active: "Đang làm việc", "On Leave": "Nghỉ phép", Inactive: "Đã nghỉ việc",
};

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

type SortKey = "name" | "specialty" | "status" | "salary" | "leaveAccrued";
const SORT_DESC_BY_DEFAULT = (key: SortKey) => key === "status" || key === "salary" || key === "leaveAccrued";

function toLocalDateString(d: Date): string {
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

function getMonday(d: Date): string {
  const day = d.getDay();
  const mon = new Date(d);
  mon.setDate(d.getDate() - (day === 0 ? 6 : day - 1));
  return toLocalDateString(mon);
}

export default function OwnerDentistManagementPage() {
  useRequireOwner();
  const router = useRouter();

  const [staffList, setStaffList]       = useState<StaffDto[]>([]);
  const [stats, setStats]               = useState<StaffStatsDto>({ totalDentists: 0, totalEmployees: 0, totalDoctors: 0 });
  const [totalCount, setTotalCount]     = useState(0);
  const [isLoading, setIsLoading]       = useState(true);

  const [searchQuery, setSearchQuery]   = useState("");
  const [specialtyFilter, setSpecialtyFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [currentPage, setCurrentPage]   = useState(1);
  const [pageSize, setPageSize]         = useState(10);
  const [sortKey, setSortKey]           = useState<SortKey>("name");
  const [sortDir, setSortDir]           = useState<SortDir>("asc");

  const [toast, setToast] = useState<{ show: boolean; message: string } | null>(null);
  const [todaySchedule, setTodaySchedule] = useState<ScheduleEntryDto[]>([]);
  const [onLeaveCount, setOnLeaveCount]   = useState(0);
  const [baseTotal, setBaseTotal]         = useState(0);
  const [pendingLeaveCount, setPendingLeaveCount] = useState(0);

  const showToast = (message: string) => {
    setToast({ show: true, message });
    setTimeout(() => setToast(null), 4000);
  };

  useEffect(() => {
    const msg = sessionStorage.getItem("staffSuccessMsg");
    if (msg) { showToast(msg); sessionStorage.removeItem("staffSuccessMsg"); }
  }, []);

  const fetchStaff = useCallback(() => {
    setIsLoading(true);
    getStaffApi({
      search:    searchQuery || undefined,
      role:      "Dentist",
      status:    statusFilter !== "All" ? statusFilter : undefined,
      specialty: specialtyFilter || undefined,
      page:      currentPage,
      pageSize,
      sortBy:    sortKey,
      sortDir,
    })
      .then((res) => {
        setStaffList(res.items);
        setTotalCount(res.totalCount);
        setStats(res.statistics);
      })
      .catch((err) => showToast("Lỗi tải dữ liệu: " + (err instanceof Error ? err.message : "")))
      .finally(() => setIsLoading(false));
  }, [searchQuery, specialtyFilter, statusFilter, currentPage, pageSize, sortKey, sortDir]);

  useEffect(() => { fetchStaff(); }, [fetchStaff]);

  const handleSort = (column: SortKey) => {
    const next = toggleSortState({ key: sortKey, dir: sortDir }, column, SORT_DESC_BY_DEFAULT);
    setSortKey(next.key);
    setSortDir(next.dir);
    setCurrentPage(1);
  };

  useEffect(() => {
    const mon = getMonday(new Date());
    const today = toLocalDateString(new Date());
    getWeekScheduleApi(mon)
      .then(entries => setTodaySchedule(entries.filter(e => e.date === today && !e.isHoliday)))
      .catch(() => {});
    Promise.all([
      getStaffApi({ role: "Dentist", status: "On Leave", page: 1, pageSize: 1 }),
      getStaffApi({ role: "Dentist", status: "Active",   page: 1, pageSize: 1 }),
      getLeaveRequestsAdminApi("Pending").catch(() => []),
    ]).then(([drLeave, drActive, pendingLeaves]) => {
      setOnLeaveCount(drLeave.totalCount);
      setBaseTotal(drActive.totalCount + drLeave.totalCount);
      setPendingLeaveCount(pendingLeaves.length);
    }).catch(() => {});
  }, []);

  const selectClass =
    "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";

  const workingTodayDoctors = new Set(todaySchedule.filter(e => e.role === "dentist").map(e => e.name)).size;
  const offTodayDoctors     = Math.max(0, baseTotal - workingTodayDoctors);

  const ICON_USERS   = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.109A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" /></svg>;
  const ICON_CHECK   = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>;
  const ICON_OFF     = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z" /></svg>;
  const ICON_LEAVE   = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>;

  const statCards = [
    { label: "Tổng nha sĩ",       sub: "Nha sĩ và nha sĩ chuyên khoa", value: baseTotal,           numClass: "text-slate-900",  bg: "bg-slate-50",  iconCls: "text-slate-500",  icon: ICON_USERS  },
    { label: "Làm việc hôm nay",  sub: "Có lịch trong ngày",           value: workingTodayDoctors, numClass: "text-green-700",  bg: "bg-green-50",  iconCls: "text-green-600",  icon: ICON_CHECK  },
    { label: "Nghỉ hôm nay",      sub: "Không có lịch trong ngày",     value: offTodayDoctors,     numClass: "text-amber-700",  bg: "bg-amber-50",  iconCls: "text-amber-600",  icon: ICON_OFF    },
    { label: "Đơn xin nghỉ phép", sub: "Đang chờ phê duyệt",           value: pendingLeaveCount,   numClass: "text-indigo-700", bg: "bg-indigo-50", iconCls: "text-indigo-500", icon: ICON_LEAVE, onClick: () => router.push("/owner/leaves") },
  ];

  const handleExportExcel = () => {
    const headers = ["Họ tên", "Email", "SĐT", "Vai trò", "Chuyên khoa", "Trạng thái"];
    const rows = staffList.map((u) => [
      u.fullName || u.email, u.email,
      u.phoneNumber || "—", ROLE_LABELS[u.role as UiRole] || u.role,
      u.specialty || "—",
      STATUS_LABELS[u.employmentStatus || "Active"],
    ]);
    const ws = XLSX.utils.aoa_to_sheet([
      ["DANH SÁCH - NHA SĨ"],
      headers,
      ...rows,
    ]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, "NhaSi");
    XLSX.writeFile(wb, "NhaSi.xlsx");
    showToast("Đã xuất file Excel thành công.");
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="staff-dentists" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <OwnerPageHeader
          title="Quản Lý Nha Sĩ"
          subtitle="Tra cứu hồ sơ nha sĩ phục vụ vận hành phòng khám."
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {/* TOAST */}
          {toast?.show && (
            <div className="fixed top-6 right-6 z-[100] animate-fade-in">
              <div className="bg-white border border-green-200 rounded-2xl shadow-2xl p-4 flex items-center gap-3 max-w-sm">
                <div className="w-9 h-9 rounded-full bg-green-100 flex items-center justify-center shrink-0">
                  <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <span className="text-[13px] font-black text-slate-900 leading-tight">{toast.message}</span>
              </div>
            </div>
          )}

          {/* STAT CARDS */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 shrink-0">
            {statCards.map((card, i) => (
              <div
                key={i}
                onClick={card.onClick}
                className={`bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between transition-all duration-200 hover-lift ${
                  card.onClick ? "cursor-pointer hover:border-indigo-300 hover:shadow-md" : ""
                }`}
              >
                <div>
                  <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">{card.label}</span>
                  <span className={`text-3xl font-black block mt-1 ${card.numClass}`}>{card.value}</span>
                  <span className="text-[11.5px] text-slate-400 font-semibold block mt-0.5">{card.sub}</span>
                </div>
                <div className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 ${card.bg} ${card.iconCls}`}>
                  {card.icon}
                </div>
              </div>
            ))}
          </div>

          {/* FILTER TOOLBAR */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4 shrink-0">
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
                  placeholder="Tìm theo tên, mã NV, email, số điện thoại..."
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); setCurrentPage(1); }}
                  className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>

              {/* Specialty filter */}
              <div className="relative md:w-52">
                <select
                  value={specialtyFilter}
                  onChange={(e) => { setSpecialtyFilter(e.target.value); setCurrentPage(1); }}
                  className={selectClass}
                >
                  <option value="">Tất cả chuyên khoa</option>
                  <option value="Răng Hàm Mặt">Răng Hàm Mặt</option>
                  <option value="Nha chu">Nha chu</option>
                  <option value="Chỉnh nha / Niềng răng">Chỉnh nha / Niềng răng</option>
                  <option value="Phục hình răng / Implant">Phục hình răng / Implant</option>
                  <option value="Nha khoa thẩm mỹ">Nha khoa thẩm mỹ</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Status filter */}
              <div className="relative md:w-48">
                <select
                  value={statusFilter}
                  onChange={(e) => { setStatusFilter(e.target.value); setCurrentPage(1); }}
                  className={selectClass}
                >
                  <option value="All">Tất cả trạng thái</option>
                  <option value="Active">Đang làm việc</option>
                  <option value="On Leave">Nghỉ phép</option>
                  <option value="Inactive">Đã nghỉ việc</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Add */}
              <button
                onClick={() => router.push("/owner/employee/add-dentist")}
                className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold px-5 py-2.5 rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all hover:translate-y-[-1px] cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm mới
              </button>
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

              <div className="flex items-center gap-2">
                <button
                  onClick={handleExportExcel}
                  className="flex items-center gap-1.5 px-4.5 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-[13px] font-bold transition-all shadow-sm cursor-pointer"
                >
                  <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 8.25H7.5a2.25 2.25 0 0 0-2.25 2.25v9a2.25 2.25 0 0 0 2.25 2.25h10a2.25 2.25 0 0 0 2.25-2.25V10.5A2.25 2.25 0 0 0 17.5 8.25H16M9 8.25V4.5A2.25 2.25 0 0 1 11.25 2.25h1.5A2.25 2.25 0 0 1 15 4.5v3.75m-6 0h6m-6 5.25h6m-6 3h6" />
                  </svg>
                  Xuất file Excel
                </button>
              </div>
            </div>
          </div>

          {/* TABLE CONTAINER */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex-1 flex flex-col">
            <div className="overflow-x-auto flex-1">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    <SortableTh column="name" label="Nhân viên" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <Th className="px-6">Liên hệ</Th>
                    <SortableTh column="specialty" label="Chuyên khoa" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <Th className="px-6">Hình thức</Th>
                    <SortableTh column="salary" label="Lương cơ bản" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <SortableTh column="leaveAccrued" label="Nghỉ phép/tháng" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} align="center" className="px-6" />
                    <SortableTh column="status" label="Trạng thái" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} align="center" className="px-6" />
                    <Th className="px-6" align="center">Thao tác</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoading ? (
                    <tr>
                      <td colSpan={8} className="px-6 py-16 text-center font-bold text-slate-400 animate-pulse">
                        Đang tải danh sách hồ sơ nha sĩ...
                      </td>
                    </tr>
                  ) : staffList.length > 0 ? (
                    staffList.map((item) => {
                      const initials = item.fullName
                        ? item.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
                        : item.email.slice(0, 2).toUpperCase();
                      return (
                        <tr
                          key={item.id}
                          onClick={() => {
                            sessionStorage.setItem("staffDetailData", JSON.stringify(item));
                            router.push(`/owner/employee/detail/${item.id}`);
                          }}
                          className="hover:bg-slate-50/50 transition-colors cursor-pointer group"
                        >
                          <td className="px-6 py-4.5">
                            <div className="flex items-center gap-3">
                              {item.profilePictureUrl ? (
                                <img src={resolveAssetUrl(item.profilePictureUrl)} alt="avatar" className="w-10 h-10 rounded-xl object-cover border border-slate-200" />
                              ) : (
                                <div className="w-10 h-10 rounded-xl bg-slate-100 flex items-center justify-center font-black text-[13px] text-slate-500 shrink-0">
                                  {initials}
                                </div>
                              )}
                              <div>
                                <span className="font-extrabold text-slate-900 block group-hover:text-primary transition-colors leading-tight">
                                  {item.fullName || item.email}
                                </span>
                              </div>
                            </div>
                          </td>
                          <td className="px-6 py-4.5">
                            <span className="font-semibold text-slate-700 block">{item.email}</span>
                            <span className="text-[12px] text-slate-400 font-bold block mt-0.5">{item.phoneNumber || "—"}</span>
                          </td>
                          <td className="px-6 py-4.5">
                            <span className="font-bold text-slate-700 block">{item.specialty || "—"}</span>
                          </td>
                          <td className="px-6 py-4.5">
                            {(() => {
                              const exp = item.yearsOfExperience ?? 5;
                              const isPartTime = exp % 2 === 0;
                              const type = item.employmentType || (isPartTime ? "Part-time" : "Full-time");
                              return (
                                <div className="group relative inline-flex items-center gap-1">
                                  {type === "Full-time" ? (
                                    <svg className="w-5 h-5 text-emerald-500" fill="currentColor" viewBox="0 0 24 24">
                                      <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zm3.3 14.7L11 14V7h1.5v5.8l3.7 2.2-.7 1.7z" />
                                    </svg>
                                  ) : (
                                    <svg className="w-5 h-5 text-sky-500" fill="currentColor" viewBox="0 0 24 24">
                                      <path d="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2zm4.2 14.2L11 13V7h1.5v5.2l4.5 2.7-.8 1.3z" />
                                    </svg>
                                  )}
                                  <span className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-1 px-2 py-1 bg-slate-900 text-white text-[10px] rounded opacity-0 group-hover:opacity-100 transition-opacity duration-150 pointer-events-none whitespace-nowrap z-30 shadow-md font-extrabold">
                                    {type === "Full-time" ? "Toàn thời gian (Full-time)" : "Bán thời gian (Part-time)"}
                                  </span>
                                </div>
                              );
                            })()}
                          </td>
                          <td className="px-6 py-4.5 text-slate-900 font-bold">
                            {(() => {
                              const exp = item.yearsOfExperience ?? 5;
                              const isPartTime = exp % 2 === 0;
                              const type = item.employmentType || (isPartTime ? "Part-time" : "Full-time");

                              if (type === "Part-time") {
                                const rate = item.ratePerShift ?? 150000;
                                const formatted = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(rate);
                                return (
                                  <>
                                    {formatted}
                                    <span className="text-[11px] text-slate-400 font-semibold ml-0.5">/ca</span>
                                  </>
                                );
                              }

                              const baseSalary = item.baseSalary ?? (25000000 + exp * 1500000);
                              const rawUnit = item.salaryUnit || "Theo tháng";
                              const unit = rawUnit.toLowerCase().includes("tháng") ? "tháng" : rawUnit.toLowerCase().includes("ngày") ? "ngày" : rawUnit.toLowerCase().includes("ca") ? "ca" : "giờ";
                              const formatted = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(baseSalary);
                              return (
                                <>
                                  {formatted}
                                  {unit !== "tháng" && (
                                    <span className="text-[11px] text-slate-400 font-semibold ml-0.5">/{unit}</span>
                                  )}
                                </>
                              );
                            })()}
                          </td>
                          <td className="px-6 py-4.5 text-center font-bold text-slate-500">
                            {(() => {
                              if (item.leaveAccrued != null) return `${item.leaveAccrued} ca`;
                              const exp = item.yearsOfExperience ?? 5;
                              const isPartTime = exp % 2 === 0;
                              return !isPartTime ? "24 ca" : "—";
                            })()}
                          </td>
                          <td className="px-6 py-4.5 text-center" onClick={(e) => e.stopPropagation()}>
                            <div className="flex items-center justify-center">
                              <div className="group relative inline-block cursor-pointer">
                                {item.employmentStatus === "Active" ? (
                                  <div className="w-7 h-7 rounded-full bg-green-50 border border-green-200 flex items-center justify-center text-green-600 shadow-sm">
                                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                                    </svg>
                                  </div>
                                ) : item.employmentStatus === "On Leave" ? (
                                  <div className="w-7 h-7 rounded-full bg-amber-50 border border-amber-200 flex items-center justify-center text-amber-500 shadow-sm">
                                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                                    </svg>
                                  </div>
                                ) : (
                                  <div className="w-7 h-7 rounded-full bg-red-50 border border-red-200 flex items-center justify-center text-red-500 shadow-sm">
                                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                                    </svg>
                                  </div>
                                )}
                                <span className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-1 px-2 py-1 bg-slate-900 text-white text-[10px] rounded opacity-0 group-hover:opacity-100 transition-opacity duration-150 pointer-events-none whitespace-nowrap z-30 shadow-md font-extrabold">
                                  {item.employmentStatus === "Active" ? "Đang làm việc" : item.employmentStatus === "On Leave" ? "Nghỉ phép" : "Nghỉ việc"}
                                </span>
                              </div>
                            </div>
                          </td>
                          <td className="px-6 py-4.5 text-center" onClick={(e) => e.stopPropagation()}>
                            <div className="flex items-center justify-center gap-1">
                              <button
                                onClick={() => {
                                  sessionStorage.setItem("staffEditData", JSON.stringify(item));
                                  router.push(`/owner/employee/edit-dentist/${item.id}`);
                                }}
                                title="Chỉnh sửa"
                                className="p-1.5 text-slate-450 hover:text-primary hover:bg-primary-light rounded-lg transition-all cursor-pointer"
                              >
                                <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                                </svg>
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  ) : (
                    <tr>
                      <td colSpan={8} className="px-6 py-16 text-center">
                        <div className="flex flex-col items-center gap-2">
                          <svg className="w-9 h-9 text-slate-355" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
                          </svg>
                          <div className="font-extrabold text-[14px] text-slate-500">Không tìm thấy kết quả phù hợp.</div>
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
                  itemLabel="kết quả"
                />
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
