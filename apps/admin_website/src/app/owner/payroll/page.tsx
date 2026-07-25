"use client";

import React, { useState, useEffect } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import { getStaffApi, getLeaveRequestsAdminApi, type StaffDto, type LeaveRequestDto } from "../../../lib/apiClient";
import * as XLSX from "xlsx";

interface PayrollItem {
  staff: StaffDto;
  basicSalary: number;
  allowance: number;
  deduction: number;
  netSalary: number;
  status: "Paid" | "Pending";
  paymentDate: string | null;
  takenDaysInMonth: number;
  allowedLeave: number;
  exceededDays: number;
}

export default function OwnerPayrollPage() {
  useRequireOwner();

  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [deptFilter, setDeptFilter] = useState("All");
  const [payrollList, setPayrollList] = useState<PayrollItem[]>([]);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const [selectedMonth, setSelectedMonth] = useState(new Date().getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());

  useEffect(() => {
    setIsLoading(true);
    Promise.all([
      getStaffApi({ page: 1, pageSize: 100 }),
      getLeaveRequestsAdminApi("Approved"),
    ])
      .then(([staffRes, leavesRes]) => {
        const items = staffRes.items.map((staff) => {
          const isDentist = staff.role === "Doctor" || staff.role === "Dentist";
          const exp = staff.yearsOfExperience ?? 5;
          
          const basicSalary = staff.baseSalary ?? (isDentist ? (25000000 + exp * 1500000) : (10000000 + (staff.fullName?.length || 5) * 200000));
          const allowance = staff.allowance ?? (isDentist ? 2500000 : 1200000);
          
          // Tính tổng ngày nghỉ thực tế được duyệt trong tháng đã chọn
          const staffLeaves = leavesRes.filter(l => l.userId === staff.id);
          let takenDaysInMonth = 0;
          
          staffLeaves.forEach(leave => {
            if (!leave.startDate || !leave.endDate) return;
            const start = new Date(leave.startDate);
            const end = new Date(leave.endDate);
            
            let current = new Date(start);
            while (current <= end) {
              if (current.getMonth() + 1 === selectedMonth && current.getFullYear() === selectedYear) {
                takenDaysInMonth += 1;
              }
              current.setDate(current.getDate() + 1);
            }
          });
          
          const allowedLeave = staff.leaveAccrued ?? (isDentist ? 1.5 : 1);
          const exceededDays = takenDaysInMonth > allowedLeave ? takenDaysInMonth - allowedLeave : 0;
          const deduction = exceededDays > 0 ? Math.round(exceededDays * (basicSalary / 26)) : 0;
          
          const netSalary = basicSalary + allowance - deduction;
          
          return {
            staff,
            basicSalary,
            allowance,
            deduction,
            netSalary,
            status: "Pending" as const,
            paymentDate: null,
            takenDaysInMonth,
            allowedLeave,
            exceededDays,
          };
        });
        setPayrollList(items);
      })
      .catch((err) => {
        console.error("Failed to load staff for payroll", err);
      })
      .finally(() => {
        setIsLoading(false);
      });
  }, [selectedMonth, selectedYear]);

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat("vi-VN").format(val) + " đ";
  };

  const showMessage = (msg: string) => {
    setSuccessMsg(msg);
    setTimeout(() => setSuccessMsg(null), 3000);
  };

  const handlePay = (id: string, name: string) => {
    setPayrollList((prev) =>
      prev.map((item) =>
        item.staff.id === id
          ? {
              ...item,
              status: item.status === "Paid" ? "Pending" : "Paid",
              paymentDate: item.status === "Paid" ? null : new Date().toLocaleDateString("vi-VN"),
            }
          : item
      )
    );
    showMessage(`Đã cập nhật trạng thái thanh toán cho nhân viên ${name}!`);
  };

  const handlePayAll = () => {
    setPayrollList((prev) =>
      prev.map((item) => ({
        ...item,
        status: "Paid",
        paymentDate: new Date().toLocaleDateString("vi-VN"),
      }))
    );
    showMessage("Đã duyệt chi và thanh toán lương cho toàn bộ nhân viên!");
  };

  const handleExport = () => {
    const data = filteredList.map((item) => ({
      "Mã nhân viên": item.staff.employeeId ?? "Chưa cấp",
      "Họ và tên": item.staff.fullName ?? item.staff.email,
      "Số điện thoại": item.staff.phoneNumber ?? "—",
      "Bộ phận": item.staff.department ?? "Nha sĩ",
      "Chức vụ": item.staff.position ?? "Nha sĩ",
      "Lương cơ bản": item.basicSalary,
      "Phụ cấp": item.allowance,
      "Nghỉ thực tế (ngày)": item.takenDaysInMonth,
      "Định mức phép (ngày)": item.allowedLeave,
      "Vượt phép (ngày)": item.exceededDays,
      "Khấu trừ": item.deduction,
      "Thực nhận (Net)": item.netSalary,
      "Trạng thái": item.status === "Paid" ? "Đã thanh toán" : "Chưa thanh toán",
      "Ngày chi trả": item.paymentDate ?? "—",
    }));

    const worksheet = XLSX.utils.json_to_sheet(data);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "BangLuong");
    XLSX.writeFile(workbook, `Bang_Luong_Nhan_Vien_Thang_${selectedMonth}_Nam_${selectedYear}.xlsx`);
  };

  const filteredList = payrollList.filter((item) => {
    const matchesSearch =
      (item.staff.fullName ?? "").toLowerCase().includes(searchQuery.toLowerCase()) ||
      (item.staff.employeeId ?? "").toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.staff.email.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesDept =
      deptFilter === "All" ||
      (deptFilter === "Dentist" && (item.staff.role === "Dentist" || item.staff.role === "Doctor")) ||
      (item.staff.department ?? "").toLowerCase().includes(deptFilter.toLowerCase());

    return matchesSearch && matchesDept;
  });

  const totalNet = filteredList.reduce((acc, item) => acc + item.netSalary, 0);
  const paidCount = filteredList.filter((x) => x.status === "Paid").length;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="payroll" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-xl font-extrabold text-slate-900 tracking-tight">Bảng Lương Nhân Viên</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Quản lý đãi ngộ, phê duyệt bảng thanh toán lương hàng tháng.</p>
          </div>
          <NotificationBell />
        </header>

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          {successMsg && (
            <div className="bg-emerald-50 border border-emerald-100 text-emerald-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-center gap-2">
              <span>✓</span> {successMsg}
            </div>
          )}

          {/* KPI Summary Block */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng chi quỹ lương</span>
              <span className="text-2xl font-black text-slate-900 mt-1 block">{formatCurrency(totalNet)}</span>
              <span className="text-[11px] text-slate-400 font-semibold block mt-1">Tính cho {filteredList.length} nhân viên hiển thị</span>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Trạng thái giải ngân</span>
              <span className="text-2xl font-black text-slate-900 mt-1 block">
                {paidCount} / {filteredList.length} <span className="text-sm font-semibold text-slate-400">đã trả</span>
              </span>
              <span className="text-[11px] text-slate-400 font-semibold block mt-1">Còn lại {filteredList.length - paidCount} nhân sự chờ duyệt</span>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Thao tác nhanh</span>
                <span className="text-slate-400 text-[12px] font-medium mt-1 block">Duyệt chi toàn bộ quỹ</span>
              </div>
              <button
                onClick={handlePayAll}
                disabled={filteredList.length === 0 || paidCount === filteredList.length}
                className="px-4 py-2.5 bg-primary hover:bg-primary-hover disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-black rounded-xl cursor-pointer shadow-md shadow-primary/20 transition-all"
              >
                Thanh Toán Tất Cả
              </button>
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
                  onChange={(e) => setDeptFilter(e.target.value)}
                  className="px-4 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none transition-all font-semibold text-slate-600 text-[13px] pr-8 appearance-none cursor-pointer"
                >
                  <option value="All">Tất cả bộ phận</option>
                  <option value="Đón tiếp & CSKH">Đón tiếp & CSKH</option>
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
                  onChange={(e) => setSelectedMonth(Number(e.target.value))}
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
                  onChange={(e) => setSelectedYear(Number(e.target.value))}
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

            <button
              onClick={handleExport}
              disabled={filteredList.length === 0}
              className="flex items-center gap-2 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-xs font-bold transition-all cursor-pointer shadow-sm disabled:opacity-50"
            >
              📊 Xuất File Excel
            </button>
          </div>

          {/* Table Container */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px]">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/80">
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Nhân viên</th>
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Bộ phận / Chức vụ</th>
                    <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Lương cơ bản</th>
                    <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Phụ cấp</th>
                    <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Khấu trừ</th>
                    <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Thực nhận (Net)</th>
                    <th className="px-5 py-3 text-center font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Trạng thái</th>
                    <th className="px-5 py-3 text-center font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Ngày trả</th>
                    <th className="px-5 py-3 text-center font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {isLoading ? (
                    <tr>
                      <td colSpan={9} className="px-5 py-12 text-center text-slate-400 font-semibold animate-pulse">
                        Đang tải danh sách lương nhân sự...
                      </td>
                    </tr>
                  ) : filteredList.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="px-5 py-12 text-center text-slate-400 font-semibold">
                        Không tìm thấy thông tin lương phù hợp.
                      </td>
                    </tr>
                  ) : (
                    filteredList.map((item) => (
                      <tr key={item.staff.id} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-5 py-4">
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-black text-xs shrink-0">
                              {item.staff.fullName
                                ? item.staff.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
                                : item.staff.email.slice(0, 2).toUpperCase()}
                            </div>
                            <div>
                              <span className="font-bold text-slate-900 block">{item.staff.fullName ?? item.staff.email}</span>
                              <span className="text-[11px] font-mono text-slate-400 font-extrabold">{item.staff.employeeId ?? "Chưa cấp ID"}</span>
                            </div>
                          </div>
                        </td>
                        <td className="px-5 py-4">
                          <div className="font-semibold text-slate-700">{item.staff.department ?? "Nha sĩ"}</div>
                          <div className="text-[11px] text-slate-400 font-semibold mt-0.5">{item.staff.position ?? "Nha sĩ điều trị"}</div>
                        </td>
                        <td className="px-5 py-4 text-right font-bold text-slate-700">{formatCurrency(item.basicSalary)}</td>
                        <td className="px-5 py-4 text-right text-emerald-600 font-bold">+{formatCurrency(item.allowance)}</td>
                        <td className="px-5 py-4 text-right">
                          <div className="text-rose-500 font-bold">-{formatCurrency(item.deduction)}</div>
                          <div className="text-[10px] text-slate-400 font-bold mt-0.5 block whitespace-nowrap">
                            {item.takenDaysInMonth > 0 ? (
                              <span>Nghỉ {item.takenDaysInMonth}n/{item.allowedLeave}n phép {item.exceededDays > 0 && <span className="text-rose-400 font-extrabold">(vượt {item.exceededDays})</span>}</span>
                            ) : (
                              <span>Chưa nghỉ phép</span>
                            )}
                          </div>
                        </td>
                        <td className="px-5 py-4 text-right font-black text-slate-900">{formatCurrency(item.netSalary)}</td>
                        <td className="px-5 py-4 text-center">
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
                        <td className="px-5 py-4 text-center text-slate-500 font-semibold font-mono text-xs">
                          {item.paymentDate ?? "—"}
                        </td>
                        <td className="px-5 py-4 text-center">
                          <button
                            onClick={() => handlePay(item.staff.id, item.staff.fullName ?? item.staff.email)}
                            className={`px-3 py-1.5 rounded-lg text-xs font-black transition-all cursor-pointer ${
                              item.status === "Paid"
                                ? "bg-red-50 hover:bg-red-100 text-primary border border-red-200"
                                : "bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border border-emerald-200"
                            }`}
                          >
                            {item.status === "Paid" ? "Hoàn tác" : "Chi Trả"}
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
