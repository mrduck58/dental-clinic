"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { getUser, clearSession, resolveAssetUrl, type AuthUser } from "../../lib/apiClient";
import { ROLE_LABELS, type UiRole } from "../../lib/roles";

interface SidebarProps {
  activeMenu: string;
}

const STAFF_SUBMENU_KEYS = ["staff-dentists", "staff-list"];
const PAYROLL_SUBMENU_KEYS = ["payroll-dentists", "payroll-staff"];

export default function OwnerSidebar({ activeMenu }: SidebarProps) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const isStaffGroupActive = STAFF_SUBMENU_KEYS.includes(activeMenu);
  const [staffMenuOpen, setStaffMenuOpen] = useState(isStaffGroupActive);
  const isPayrollGroupActive = PAYROLL_SUBMENU_KEYS.includes(activeMenu);
  const [payrollMenuOpen, setPayrollMenuOpen] = useState(isPayrollGroupActive);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    setUser(getUser());
    const handleToggle = () => setMobileOpen((v) => !v);
    const handleClose = () => setMobileOpen(false);
    window.addEventListener("toggle-sidebar", handleToggle);
    window.addEventListener("close-sidebar", handleClose);
    return () => {
      window.removeEventListener("toggle-sidebar", handleToggle);
      window.removeEventListener("close-sidebar", handleClose);
    };
  }, []);

  useEffect(() => {
    if (mobileOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
    return () => {
      document.body.style.overflow = "";
    };
  }, [mobileOpen]);

  const initials = user?.fullName
    ? user.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
    : (user?.username?.slice(0, 2).toUpperCase() ?? "OW");

  const handleLogout = () => {
    clearSession();
    router.push("/auth/login");
  };

  return (
    <>
      {/* Mobile Backdrop */}
      {mobileOpen && (
        <div
          className="fixed inset-0 bg-slate-900/50 backdrop-blur-xs z-40 lg:hidden transition-opacity"
          onClick={() => setMobileOpen(false)}
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-50 w-72 bg-white border-r border-slate-200 p-6 flex flex-col gap-6 shrink-0 h-[100dvh] max-h-[100dvh] justify-between transition-transform duration-300 ease-in-out lg:sticky lg:top-0 lg:translate-x-0 font-sans ${
          mobileOpen ? "translate-x-0 shadow-2xl" : "-translate-x-full"
        }`}
      >
        <div className="flex flex-col gap-6 flex-1 min-h-0">
          {/* Logo + Nút đóng trên Mobile */}
          <div className="flex items-center justify-between">
            <Link
              href="/owner"
              onClick={() => setMobileOpen(false)}
              className="flex items-center gap-3 px-2 py-2 cursor-pointer select-none"
            >
              <span className="text-3xl text-primary shrink-0 animate-pulse">🦷</span>
              <div className="flex flex-col">
                <span className="text-[12px] font-black tracking-widest text-primary uppercase leading-none mb-1">
                  SơnGiang
                </span>
                <span className="font-extrabold text-lg tracking-tight text-slate-900 leading-none">
                  Dental<span className="text-primary font-bold">Clinic</span>
                </span>
              </div>
            </Link>

            <button
              type="button"
              onClick={() => setMobileOpen(false)}
              className="lg:hidden p-2 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-all"
              aria-label="Đóng menu"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

        {/* Nav list */}
        <nav className="flex flex-col gap-1 overflow-y-auto pr-1 flex-1">
          {/* Tổng quan */}
          <Link
            href="/owner"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-bold text-[13px] transition-all ${
              activeMenu === "overview"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 6a7.5 7.5 0 107.5 7.5h-7.5V6z" />
              <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 10.5H21A7.5 7.5 0 0013.5 3v7.5z" />
            </svg>
            Tổng quan
          </Link>

          {/* Nhóm: Tài chính */}
          <div className="px-4 pt-3 pb-1 text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider">
            Tài chính
          </div>

          {/* Tổng quan tài chính */}
          <Link
            href="/owner/finance/overview"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "finance-overview"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" />
            </svg>
            Tổng quan tài chính
          </Link>

          {/* Doanh thu */}
          <Link
            href="/owner/revenue"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "revenue"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 18L9 11.25l4.306 4.306a11.95 11.95 0 015.814-5.518l2.74-1.22m0 0l-5.94-2.281m5.94 2.28l-2.28 5.941" />
            </svg>
            Doanh thu
          </Link>

          {/* Chi phí */}
          <Link
            href="/owner/expenses"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "expenses"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-9-11.25h18a1.5 1.5 0 011.5 1.5v10.5a1.5 1.5 0 01-1.5 1.5H3a1.5 1.5 0 01-1.5-1.5V6a1.5 1.5 0 011.5-1.5z" />
            </svg>
            Chi phí
          </Link>

          {/* Lương (mở rộng: Nha sĩ / Nhân viên) */}
          <button
            onClick={() => setPayrollMenuOpen((v) => !v)}
            className={`flex items-center justify-between gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all cursor-pointer ${
              isPayrollGroupActive
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <span className="flex items-center gap-3.5">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-1.971-.659-1.171-.879-1.171-2.303 0-3.182 1.172-.879 3.07-.879 4.242 0L15 9M3 5.25h18A2.25 2.25 0 0 1 21 7.5v9a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 16.5v-9a2.25 2.25 0 0 1 2.25-2.25Z" />
              </svg>
              Lương
            </span>
            <svg
              className={`w-4 h-4 shrink-0 transition-transform duration-200 ${payrollMenuOpen ? "rotate-180" : ""}`}
              fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </button>

          {payrollMenuOpen && (
            <div className="flex flex-col gap-1 pl-4">
              <Link
                href="/owner/payroll/dentists"
                className={`flex items-center gap-2.5 px-4 py-2.5 rounded-lg text-[12.5px] font-semibold transition-all ${
                  activeMenu === "payroll-dentists"
                    ? "bg-red-50 text-primary"
                    : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
                }`}
              >
                <span className="w-1.5 h-1.5 rounded-full bg-current shrink-0" />
                Nha sĩ
              </Link>
              <Link
                href="/owner/payroll/staff"
                className={`flex items-center gap-2.5 px-4 py-2.5 rounded-lg text-[12.5px] font-semibold transition-all ${
                  activeMenu === "payroll-staff"
                    ? "bg-red-50 text-primary"
                    : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
                }`}
              >
                <span className="w-1.5 h-1.5 rounded-full bg-current shrink-0" />
                Nhân viên
              </Link>
            </div>
          )}

          {/* Hoa hồng */}
          <Link
            href="/owner/commissions"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "commissions"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 6.75V15m6-6v8.25m.503 3.498l4.875-2.437c.381-.19.622-.58.622-1.006V4.82c0-.836-.88-1.38-1.628-1.006l-3.869 1.934c-.317.159-.69.159-1.006 0L9.503 3.252a1.125 1.125 0 00-1.006 0L3.622 5.689C3.24 5.88 3 6.27 3 6.695V19.18c0 .836.88 1.38 1.628 1.006l3.869-1.934c.317-.159.69-.159 1.006 0l4.994 2.497c.317.158.69.158 1.006 0z" />
            </svg>
            Hoa hồng
          </Link>

          {/* Báo cáo */}
          <Link
            href="/owner/finance/reports"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "finance-reports"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
            </svg>
            Báo cáo
          </Link>

          {/* Nhóm: Nhân sự */}
          <div className="px-4 pt-3 pb-1 text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider">
            Nhân sự
          </div>

          {/* Nhân sự (mở rộng: Nha sĩ / Nhân viên) */}
          <button
            onClick={() => setStaffMenuOpen((v) => !v)}
            className={`flex items-center justify-between gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all cursor-pointer ${
              isStaffGroupActive
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <span className="flex items-center gap-3.5">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
              </svg>
              Nhân sự
            </span>
            <svg
              className={`w-4 h-4 shrink-0 transition-transform duration-200 ${staffMenuOpen ? "rotate-180" : ""}`}
              fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </button>

          {staffMenuOpen && (
            <div className="flex flex-col gap-1 pl-4">
              <Link
                href="/owner/employee/dentists"
                className={`flex items-center gap-2.5 px-4 py-2.5 rounded-lg text-[12.5px] font-semibold transition-all ${
                  activeMenu === "staff-dentists"
                    ? "bg-red-50 text-primary"
                    : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
                }`}
              >
                <span className="w-1.5 h-1.5 rounded-full bg-current shrink-0" />
                Nha sĩ
              </Link>
              <Link
                href="/owner/employee/staff"
                className={`flex items-center gap-2.5 px-4 py-2.5 rounded-lg text-[12.5px] font-semibold transition-all ${
                  activeMenu === "staff-list"
                    ? "bg-red-50 text-primary"
                    : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
                }`}
              >
                <span className="w-1.5 h-1.5 rounded-full bg-current shrink-0" />
                Nhân viên
              </Link>
            </div>
          )}

          {/* Lịch làm việc */}
          <Link
            href="/owner/schedule"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "schedule"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z" />
            </svg>
            Lịch làm việc
          </Link>

          {/* Đơn xin nghỉ */}
          <Link
            href="/owner/leaves"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "leaves"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
            </svg>
            Đơn xin nghỉ
          </Link>

          {/* Nhóm: Vận hành */}
          <div className="px-4 pt-3 pb-1 text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider">
            Vận hành
          </div>

          {/* Ca khám & điều trị */}
          <Link
            href="/owner/appointments"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "appointments"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
            </svg>
            Ca khám & điều trị
          </Link>

          {/* Thông tin phòng khám */}
          <Link
            href="/owner/clinic-info"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "clinic-info"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 111.063.852l-.708 2.836a.75.75 0 001.063.852l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
            </svg>
            Thông tin phòng khám
          </Link>

          {/* Phản hồi & Đánh giá */}
          <Link
            href="/owner/feedback"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "feedback"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
            </svg>
            Phản hồi & Đánh giá
          </Link>

          {/* Thông báo */}
          <Link
            href="/owner/notifications"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-[13px] transition-all ${
              activeMenu === "notifications"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
            </svg>
            Thông báo
          </Link>
        </nav>
      </div>

      {/* User Profile & Logout */}
      <div className="border-t border-slate-100 pt-4 flex flex-col gap-3">
        <div className="relative">
          {/* Dropdown Menu */}
          {dropdownOpen && (
            <div className="absolute bottom-full left-0 mb-2 w-full bg-white border border-slate-200 rounded-xl shadow-lg p-2 z-50 flex flex-col gap-0.5 animate-fade-in font-sans">
              <Link
                href="/owner/profile?tab=personal"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[12.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
                Thông tin cá nhân
              </Link>
              <Link
                href="/owner/profile?tab=password"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[12.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
                Đổi mật khẩu
              </Link>
              <Link
                href="/owner/profile?tab=activities"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[12.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Lịch sử hoạt động
              </Link>
            </div>
          )}

          {/* Trigger Card */}
          <div
            onClick={() => setDropdownOpen(!dropdownOpen)}
            className="flex items-center gap-3 px-2 py-1.5 rounded-xl hover:bg-slate-50 border border-transparent hover:border-slate-100 transition-all cursor-pointer select-none"
          >
            {user?.profilePictureUrl ? (
              <img
                src={resolveAssetUrl(user.profilePictureUrl)}
                alt="Avatar"
                className="w-10 h-10 rounded-full border-2 border-primary/20 object-cover shrink-0"
              />
            ) : (
              <div className="w-10 h-10 rounded-full border-2 border-primary/20 bg-red-50/50 flex items-center justify-center font-bold text-primary shrink-0">
                {initials}
              </div>
            )}
            <div className="min-w-0 flex-1">
              <div className="text-[13px] font-bold text-slate-900 leading-tight truncate">
                {user?.fullName ?? user?.username ?? "..."}
              </div>
              <div className="text-[11px] font-semibold text-slate-400 mt-0.5">
                {ROLE_LABELS[(user?.role ?? "") as UiRole] ?? user?.role ?? "Owner"}
              </div>
            </div>
            {/* Arrow up/down */}
            <svg
              className={`w-4 h-4 text-slate-400 transition-transform duration-200 shrink-0 ${dropdownOpen ? "rotate-180" : ""}`}
              fill="none"
              stroke="currentColor"
              strokeWidth="2.5"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
            </svg>
          </div>
        </div>

        <button
          onClick={handleLogout}
          className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl text-[12px] font-bold text-slate-500 hover:text-primary hover:bg-red-50 border border-slate-100 hover:border-primary/20 transition-all cursor-pointer"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
          </svg>
          Đăng xuất
        </button>
      </div>
    </aside>
    </>
  );
}
