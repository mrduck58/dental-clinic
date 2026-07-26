"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { getUser, clearSession, type AuthUser } from "../../lib/apiClient";

const ROLE_LABEL: Record<string, string> = {
  Admin: "Quản trị viên",
  Doctor: "Bác sĩ",
  Receptionist: "Lễ tân",
};

interface SidebarProps {
  activeMenu: string;
}

export default function AdminSidebar({ activeMenu }: SidebarProps) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [dropdownOpen, setDropdownOpen] = useState(false);

  useEffect(() => { setUser(getUser()); }, []);

  const initials = user?.fullName
    ? user.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
    : (user?.username?.slice(0, 2).toUpperCase() ?? "??");

  const handleLogout = () => {
    clearSession();
    router.push("/auth/login");
  };

  return (
    <aside className="w-72 bg-white border-r border-slate-200 p-6 flex flex-col gap-6 shrink-0 sticky top-0 h-screen justify-between z-30">
      <div className="flex flex-col gap-6 flex-1 min-h-0">
        {/* Logo */}
        <Link href="/admin" className="flex items-center gap-3 px-2 py-2 cursor-pointer select-none">
          <span className="text-3xl text-primary shrink-0 animate-pulse">🦷</span>
          <div className="flex flex-col">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase leading-none mb-1">SơnGiang</span>
            <span className="font-extrabold text-xl tracking-tight text-slate-900 leading-none">
              Dental<span className="text-primary font-bold">Clinic</span>
            </span>
          </div>
        </Link>

        {/* Nav list */}
        <nav className="flex flex-col gap-1 overflow-y-auto pr-1 flex-1">
          {/* Tổng quan */}
          <Link
            href="/admin"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-bold transition-all ${activeMenu === "overview"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 6a7.5 7.5 0 107.5 7.5h-7.5V6z" />
              <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 10.5H21A7.5 7.5 0 0013.5 3v7.5z" />
            </svg>
            Tổng quan
          </Link>

          {/* Quản lí dịch vụ */}
          <Link
            href="/admin/services"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "services"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.83-5.83m0 0a2.95 2.95 0 11-4.174-4.172 2.95 2.95 0 014.174 4.172zm-7.42 7.42l9.39-9.39" />
            </svg>
            Quản lí dịch vụ
          </Link>

          {/* Quản lí thuốc */}
          <Link
            href="/admin/medicines"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "medicines"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm-2.25.005h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm2.25-2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75zm5.25 0v.75h.75v-.75h-.75zm-.75 0h.75v.75h-.75v-.75zm-.75 0h.005v.005h-.005v-.005zm-.75 0h.005v.005h-.005v-.005zm.75-2.25h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm0 2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75z" />
              <circle cx="12" cy="12" r="8" strokeWidth="1.5" />
            </svg>
            Quản lí thuốc
          </Link>

          {/* Quản lí phòng */}
          <Link
            href="/admin/rooms"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "rooms"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3a1.5 1.5 0 011.5-1.5h3a1.5 1.5 0 011.5 1.5v3m-9-10.5h.75m-.75 3h.75m-.75 3h.75m9-6h.75m-.75 3h.75m-.75 3h.75" />
            </svg>
            Quản lí phòng
          </Link>

          {/* Tài khoản và phân quyền */}
          <Link
            href="/admin/permissions"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "permissions"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="24" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.751A11.956 11.956 0 0112 2.714z" />
            </svg>
            Tài khoản
          </Link>

          {/* Thống kê AI */}
          <Link
            href="/admin/ai-analytics"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "ai-analytics"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z" />
            </svg>
            Thống kê AI
          </Link>

          {/* Lịch sử hoạt động */}
          <Link
            href="/admin/activity-logs"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "history"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Lịch sử hoạt động
          </Link>

          {/* Thông báo */}
          <Link
            href="/admin/notifications"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "notifications"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
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
                href="/admin/profile?tab=personal"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[13.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
                Thông tin cá nhân
              </Link>
              <Link
                href="/admin/profile?tab=password"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[13.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
                Đổi mật khẩu
              </Link>
              <Link
                href="/admin/profile?tab=activities"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[13.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
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
                src={user.profilePictureUrl}
                alt="Avatar"
                className="w-10 h-10 rounded-full border-2 border-primary/20 object-cover shrink-0"
              />
            ) : (
              <div className="w-10 h-10 rounded-full border-2 border-primary/20 bg-red-50/50 flex items-center justify-center font-bold text-primary shrink-0">
                {initials}
              </div>
            )}
            <div className="min-w-0 flex-1">
              <div className="text-[14px] font-bold text-slate-900 leading-tight truncate">
                {user?.fullName ?? user?.username ?? "..."}
              </div>
              <div className="text-[12px] font-semibold text-slate-400 mt-0.5">
                {ROLE_LABEL[user?.role ?? ""] ?? user?.role ?? ""}
              </div>
            </div>
            {/* Arrow up/down */}
            <svg
              className={`w-4 h-4 text-slate-450 transition-transform duration-200 shrink-0 ${dropdownOpen ? "rotate-180" : ""}`}
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
          className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl text-[13px] font-bold text-slate-500 hover:text-primary hover:bg-red-50 border border-slate-100 hover:border-primary/20 transition-all cursor-pointer"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
          </svg>
          Đăng xuất
        </button>
      </div>
    </aside>
  );
}
