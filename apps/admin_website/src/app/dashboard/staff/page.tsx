"use client";

import React, { useState, useEffect } from "react";
import Sidebar from "../../../components/shared/Sidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import {
  getStaffApi,
  type StaffDto,
  type StaffStatsDto,
} from "../../../lib/apiClient";
import AddStaffModal from "./components/AddStaffModal";
import EditStaffModal from "./components/EditStaffModal";
import * as XLSX from "xlsx";

const ROLE_LABELS: Record<string, string> = {
  Admin: "Quản trị viên",
  Doctor: "Bác sĩ",
  Dentist: "Nha sĩ",
  Staff: "Lễ tân / Trợ lý",
};

const ROLE_BADGES: Record<string, string> = {
  Admin: "bg-purple-50 text-purple-700 border-purple-100",
  Doctor: "bg-emerald-50 text-emerald-700 border-emerald-100",
  Dentist: "bg-sky-50 text-secondary border-sky-100",
  Staff: "bg-green-50 text-green-700 border-green-100",
};

const STATUS_LABELS: Record<string, string> = {
  Active: "Đang làm việc",
  "On Leave": "Nghỉ phép",
  Inactive: "Đã nghỉ việc",
};

const STATUS_BADGES: Record<string, string> = {
  Active: "bg-green-50 text-green-700 border-green-200",
  "On Leave": "bg-amber-50 text-amber-700 border-amber-200",
  Inactive: "bg-red-50 text-red-700 border-red-200",
};

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

export default function StaffManagementPage() {
  useRequireAdmin();

  // State
  const [staffList, setStaffList] = useState<StaffDto[]>([]);
  const [stats, setStats] = useState<StaffStatsDto>({
    totalDentists: 0,
    totalEmployees: 0,
    totalDoctors: 0,
  });
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);

  // Filters
  const [searchQuery, setSearchQuery] = useState("");
  const [roleFilter, setRoleFilter] = useState("All");
  const [statusFilter, setStatusFilter] = useState("All");
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [selectedStaff, setSelectedStaff] = useState<StaffDto | null>(null);

  // Toast
  const [toast, setToast] = useState<{ show: boolean; message: string } | null>(null);

  const showToast = (message: string) => {
    setToast({ show: true, message });
    setTimeout(() => setToast(null), 4000);
  };

  const fetchStaff = () => {
    setIsLoading(true);
    getStaffApi({
      search: searchQuery || undefined,
      role: roleFilter !== "All" ? roleFilter : undefined,
      status: statusFilter !== "All" ? statusFilter : undefined,
      page: currentPage,
      pageSize,
    })
      .then((res) => {
        setStaffList(res.items);
        setTotalCount(res.totalCount);
        setStats(res.statistics);
      })
      .catch((err) => {
        showToast("Lỗi khi tải dữ liệu: " + (err instanceof Error ? err.message : ""));
      })
      .finally(() => {
        setIsLoading(false);
      });
  };

  // Fetch data on filter or page change
  useEffect(() => {
    fetchStaff();
  }, [searchQuery, roleFilter, statusFilter, currentPage, pageSize]);

  // Reset to page 1 when filter changes
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
    setCurrentPage(1);
  };

  const handleRoleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setRoleFilter(e.target.value);
    setCurrentPage(1);
  };

  const handleStatusChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setStatusFilter(e.target.value);
    setCurrentPage(1);
  };



  // Export Excel client-side (formatted "thống kê văn phòng")
  const handleExportExcel = () => {
    const statsData = [
      ["BÁO CÁO THỐNG KÊ NHÂN SỰ PHÒNG KHÁM"],
      ["Ngày xuất bản:", new Date().toLocaleDateString("vi-VN")],
      [],
      ["Tổng số nhân sự:", stats.totalEmployees],
      ["Trong đó - Bác sĩ:", stats.totalDoctors],
      ["Trong đó - Nha sĩ:", stats.totalDentists],
      ["Trong đó - Bộ phận khác:", stats.totalEmployees - stats.totalDoctors - stats.totalDentists],
      [],
      ["DANH SÁCH CHI TIẾT NHÂN VIÊN"],
      ["Mã NV", "Họ tên", "Email", "Số điện thoại", "Vai trò", "Bộ phận", "Trạng thái", "Ngày tạo"]
    ];

    const staffRows = staffList.map(u => [
      u.employeeId || "—",
      u.fullName || u.username,
      u.email,
      u.phoneNumber || "—",
      ROLE_LABELS[u.role] || u.role,
      u.department || "Chưa xếp bộ phận",
      STATUS_LABELS[u.employmentStatus || "Active"],
      u.createdAt ? new Date(u.createdAt).toLocaleDateString("vi-VN") : "—"
    ]);

    const finalAOA = [...statsData, ...staffRows];
    
    const ws = XLSX.utils.aoa_to_sheet(finalAOA);
    
    // Autofit columns
    const maxLens = finalAOA[9].map((_, colIdx) => {
      return Math.max(
        ...finalAOA.slice(9).map(row => (row[colIdx] ? String(row[colIdx]).length : 0)),
        finalAOA[9][colIdx] ? String(finalAOA[9][colIdx]).length : 0
      );
    });
    ws["!cols"] = maxLens.map(len => ({ wch: Math.max(len + 3, 12) }));

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, "Thống kê nhân sự");
    XLSX.writeFile(wb, "DanhSachNhanVien.xlsx");
    showToast("Đã xuất danh sách nhân viên ra file Excel (.xlsx).");
  };

  const selectClass =
    "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <Sidebar activeMenu="staff" />

      <main className="flex-1 flex flex-col min-w-0">
        
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Quản Lý Nhân Viên</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Tra cứu, thêm mới, phân quyền và cập nhật hồ sơ nhân sự phòng khám.
            </p>
          </div>
          <NotificationBell />
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* TOAST NOTIFICATION */}
          {toast?.show && (
            <div className="fixed top-6 right-6 z-[100] animate-fade-in">
              <div className="bg-white border border-green-250 rounded-2xl shadow-2xl p-4 flex items-center gap-3 max-w-sm">
                <div className="w-9 h-9 rounded-full bg-green-100 flex items-center justify-center shrink-0">
                  <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <span className="text-[13px] font-black text-slate-900 leading-tight">{toast.message}</span>
              </div>
            </div>
          )}

                  {/* STATISTICS GRID */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5 shrink-0">
            {/* Total Employees */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng số nhân sự</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.totalEmployees}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Bác sĩ, nha sĩ, lễ tân, trợ lý</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50/50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.109A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
                </svg>
              </div>
            </div>

            {/* Total Dentists */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-secondary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Nha sĩ</span>
                <span className="text-3xl font-black text-secondary block mt-1">{stats.totalDentists}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Nha sĩ điều trị lâm sàng</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-sky-50 text-secondary flex items-center justify-center shrink-0">
                <span className="text-xl">🩺</span>
              </div>
            </div>

            {/* Total Doctors */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-emerald-450/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Bác sĩ chuyên khoa</span>
                <span className="text-3xl font-black text-emerald-600 block mt-1">{stats.totalDoctors}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Bác sĩ khám & tư vấn</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-650 flex items-center justify-center shrink-0">
                <span className="text-xl">🧑‍⚕️</span>
              </div>
            </div>
          </div>

          {/* FILTER TOOLBAR */}
          <div className="bg-white p-4.5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4 shrink-0">
            {/* Row 1: Search + Dropdowns */}
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
                  onChange={handleSearchChange}
                  className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>

              {/* Role filter */}
              <div className="relative md:w-52">
                <select value={roleFilter} onChange={handleRoleChange} className={selectClass}>
                  <option value="All">Tất cả vai trò</option>
                  <option value="Doctor">Bác sĩ</option>
                  <option value="Dentist">Nha sĩ</option>
                  <option value="Staff">Lễ tân / Trợ lý</option>
                  <option value="Admin">Quản trị viên</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Status filter */}
              <div className="relative md:w-48">
                <select value={statusFilter} onChange={handleStatusChange} className={selectClass}>
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

              {/* Add Staff Button */}
              <button
                onClick={() => setIsAddModalOpen(true)}
                className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold px-5 py-2.5 rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all hover:translate-y-[-1px] cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm nhân viên
              </button>
            </div>

            {/* Row 2: Page size + total count + export */}
            <div className="flex items-center justify-between gap-3 flex-wrap border-t border-slate-100 pt-3">
              <div className="flex items-center gap-2.5">
                <span className="text-[12.5px] text-slate-400 font-semibold">Hiển thị</span>
                <div className="relative">
                  <select
                    value={pageSize}
                    onChange={(e) => {
                      setPageSize(Number(e.target.value));
                      setCurrentPage(1);
                    }}
                    className="pl-3 pr-7 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-650 appearance-none cursor-pointer"
                  >
                    {PAGE_SIZE_OPTIONS.map((n) => (
                      <option key={n} value={n}>
                        {n}
                      </option>
                    ))}
                  </select>
                  <span className="absolute inset-y-0 right-2 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </span>
                </div>
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">
                  nhân viên / trang
                </span>
                <span className="text-slate-200">·</span>
                <span className="text-[12.5px] text-slate-400 font-semibold">
                  Tìm thấy <span className="font-bold text-slate-600">{totalCount}</span> kết quả
                </span>
              </div>

              <button
                onClick={handleExportExcel}
                className="flex items-center gap-2 px-4 py-2 bg-white hover:bg-slate-50 text-slate-650 text-[13px] font-bold border border-slate-200 rounded-xl transition-all shadow-sm cursor-pointer"
              >
                <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
                </svg>
                Xuất Excel (.xlsx)
              </button>
            </div>
          </div>

          {/* TABLE CONTAINER */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13.5px] min-w-[960px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/85 select-none text-[11px]">
                    <th className="px-5 py-4 w-[230px]">Nhân viên</th>
                    <th className="px-5 py-4 w-[110px]">Mã nhân sự</th>
                    <th className="px-5 py-4">Địa chỉ Email</th>
                    <th className="px-5 py-4 w-[140px]">Số điện thoại</th>
                    <th className="px-5 py-4 w-[150px]">Vai trò</th>
                    <th className="px-5 py-4 w-[130px] text-center">Trạng thái</th>
                    <th className="px-5 py-4 w-[130px] text-center">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700 font-semibold">
                  {isLoading ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-16 text-center text-slate-400 font-bold">
                        Đang tải danh sách nhân viên...
                      </td>
                    </tr>
                  ) : staffList.length > 0 ? (
                    staffList.map((item) => {
                      const initials = item.fullName
                        ? item.fullName
                            .trim()
                            .split(/\s+/)
                            .slice(-2)
                            .map((w) => w[0])
                            .join("")
                            .toUpperCase()
                        : item.username.slice(0, 2).toUpperCase();

                      return (
                        <tr key={item.id} className="hover:bg-slate-50/30 transition-colors">
                          {/* Name + Dept */}
                          <td className="px-5 py-4">
                            <div className="flex items-center gap-3">
                              {item.profilePictureUrl ? (
                                <img
                                  src={item.profilePictureUrl}
                                  alt={item.fullName || item.username}
                                  className="w-10 h-10 rounded-full object-cover border border-slate-250 shadow-sm shrink-0"
                                />
                              ) : (
                                <div className="w-10 h-10 rounded-full bg-slate-100 text-slate-550 border border-slate-200/80 flex items-center justify-center font-bold text-[12px] shrink-0 select-none shadow-inner">
                                  {initials}
                                </div>
                              )}
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 truncate">
                                  {item.fullName || item.username}
                                </div>
                                <div className="text-[11.5px] text-slate-400 font-semibold mt-0.5 truncate">
                                  {item.department || "Chưa xếp bộ phận"}
                                </div>
                              </div>
                            </div>
                          </td>

                          {/* Employee ID */}
                          <td className="px-5 py-4">
                            <span className="font-black text-primary font-mono text-[13px]">
                              {item.employeeId || "—"}
                            </span>
                          </td>

                          {/* Email */}
                          <td className="px-5 py-4 font-bold text-slate-800 break-all">
                            {item.email}
                          </td>

                          {/* Phone */}
                          <td className="px-5 py-4 text-slate-600">
                            {item.phoneNumber || "—"}
                          </td>

                          {/* Role */}
                          <td className="px-5 py-4">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${ROLE_BADGES[item.role] || "bg-slate-50 border-slate-200 text-slate-600"}`}>
                              {ROLE_LABELS[item.role] || item.role}
                            </span>
                          </td>

                          {/* Status */}
                          <td className="px-5 py-4 text-center">
                            <div className="flex flex-col items-center gap-1.5 justify-center">
                              <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-black border ${STATUS_BADGES[item.employmentStatus || "Active"]}`}>
                                {STATUS_LABELS[item.employmentStatus || "Active"]}
                              </span>
                              {!item.isActive && (
                                <span className="text-[10px] text-red-500 font-bold bg-red-50 border border-red-100 px-1.5 py-0.5 rounded-full leading-none scale-[0.9]">
                                  Tài khoản khóa
                                </span>
                              )}
                            </div>
                          </td>

                          {/* Actions */}
                          <td className="px-5 py-4 text-center">
                            <div className="flex items-center justify-center gap-1">
                              {/* Edit */}
                              <button
                                onClick={() => {
                                  setSelectedStaff(item);
                                  setIsEditModalOpen(true);
                                }}
                                title="Chỉnh sửa thông tin"
                                className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
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
                      <td colSpan={7} className="px-5 py-12 text-center text-slate-450">
                        <div className="flex flex-col items-center gap-2">
                          <span className="text-2xl">🔍</span>
                          <div className="font-extrabold text-[14px]">Không tìm thấy nhân viên nào phù hợp.</div>
                          <div className="text-[12px] text-slate-400 font-semibold">Thử thay đổi từ khóa hoặc bộ lọc của bạn.</div>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination footer */}
            {!isLoading && totalCount > 0 && (
              <div className="border-t border-slate-100 px-5 py-3.5 flex flex-col sm:flex-row items-center justify-between gap-3 bg-slate-50/25">
                <span className="text-[12.5px] text-slate-400 font-semibold">
                  Hiển thị <span className="font-black text-slate-600">{(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, totalCount)}</span> trong tổng số{" "}
                  <span className="font-black text-slate-600">{totalCount}</span> nhân sự
                </span>

                {/* Page Selectors */}
                {Math.ceil(totalCount / pageSize) > 1 && (
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      disabled={currentPage === 1}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                      </svg>
                    </button>

                    {Array.from({ length: Math.ceil(totalCount / pageSize) }, (_, i) => i + 1).map((page) => (
                      <button
                        key={page}
                        onClick={() => setCurrentPage(page)}
                        className={`w-9 h-9 text-[13px] font-bold rounded-xl transition-all cursor-pointer ${
                          page === currentPage
                            ? "bg-primary text-white shadow-md shadow-primary/20"
                            : "text-slate-500 hover:bg-slate-100"
                        }`}
                      >
                        {page}
                      </button>
                    ))}

                    <button
                      onClick={() => setCurrentPage((p) => Math.min(Math.ceil(totalCount / pageSize), p + 1))}
                      disabled={currentPage === Math.ceil(totalCount / pageSize)}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                      </svg>
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </main>

      {/* ── MODALS ──────────────────────────────────────────────────────── */}
      
      {/* ADD STAFF MODAL */}
      <AddStaffModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        onSuccess={(msg) => {
          showToast(msg);
          fetchStaff();
        }}
      />

      {/* EDIT STAFF MODAL */}
      <EditStaffModal
        isOpen={isEditModalOpen}
        onClose={() => {
          setIsEditModalOpen(false);
          setSelectedStaff(null);
        }}
        staff={selectedStaff}
        onSuccess={(msg) => {
          showToast(msg);
          fetchStaff();
        }}
      />



    </div>
  );
}
