"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { getUser, clearSession, type AuthUser } from "../../lib/apiClient";

const ROLE_LABEL: Record<string, string> = {
  Admin: "Quản trị viên (Chủ)",
  Owner: "Chủ phòng khám",
};

interface SidebarProps {
  activeMenu: string;
}

export default function OwnerSidebar({ activeMenu }: SidebarProps) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [dropdownOpen, setDropdownOpen] = useState(false);

  useEffect(() => {
    setUser(getUser());
  }, []);

  const initials = user?.fullName
    ? user.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
    : (user?.username?.slice(0, 2).toUpperCase() ?? "OW");

  const handleLogout = () => {
    clearSession();
    router.push("/auth/login");
  };

  return (
    <aside className="w-72 bg-white border-r border-slate-200 p-6 flex flex-col gap-6 shrink-0 sticky top-0 h-screen justify-between z-30 font-sans">
      <div className="flex flex-col gap-6 flex-1 min-h-0">
        {/* Logo */}
        <Link href="/owner" className="flex items-center gap-3 px-2 py-2 cursor-pointer select-none">
          <span className="text-3xl text-primary shrink-0 animate-pulse">🦷</span>
          <div className="flex flex-col">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase leading-none mb-1">SơnGiang</span>
            <span className="font-extrabold text-xl tracking-tight text-slate-900 leading-none">
              Dental<span className="text-primary font-bold">Clinic</span>
            </span>
          </div>
        </Link>

        {/* Role badge */}
        <div className="flex items-center gap-2 px-3 py-2 bg-purple-50 rounded-xl border border-purple-100">
          <div className="w-2 h-2 rounded-full bg-purple-500 shrink-0" />
          <span className="text-[12.5px] font-bold text-purple-700">Cổng thông tin Chủ phòng khám</span>
        </div>

        {/* Nav list */}
        <nav className="flex flex-col gap-1 overflow-y-auto pr-1 flex-1">
          {/* Tổng quan */}
          <Link
            href="/owner"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-bold transition-all ${
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

          {/* Quản lý nhân viên */}
          <Link
            href="/owner/employee"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${
              activeMenu === "staff"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
            </svg>
            Quản lý nhân viên
          </Link>

          {/* Thông tin phòng khám */}
          <Link
            href="/owner/clinic-info"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${
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

          {/* Lịch làm việc */}
          <Link
            href="/owner/schedule"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${
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

          {/* Ca khám & điều trị */}
          <Link
            href="/owner/appointments"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${
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

          {/* Bảng lương nhân viên */}
          <Link
            href="/owner/payroll"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${
              activeMenu === "payroll"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-1.971-.659-1.171-.879-1.171-2.303 0-3.182 1.172-.879 3.07-.879 4.242 0L15 9M3 5.25h18A2.25 2.25 0 0 1 21 7.5v9a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 16.5v-9a2.25 2.25 0 0 1 2.25-2.25Z" />
            </svg>
            Bảng lương nhân viên
          </Link>

          {/* Quản lý vật tư */}
          <Link
            href="/owner/inventory"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${
              activeMenu === "inventory"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z" />
            </svg>
            Quản lý vật tư
          </Link>

          {/* Phản hồi */}
          <Link
            href="/owner/feedback"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${
              activeMenu === "feedback"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50/50 hover:text-primary"
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0012 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018z" />
            </svg>
            Phản hồi khách hàng
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
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[13.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                👤 Thông tin cá nhân
              </Link>
              <Link
                href="/owner/profile?tab=password"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[13.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                🔒 Đổi mật khẩu
              </Link>
              <Link
                href="/owner/profile?tab=activities"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[13.5px] font-bold text-slate-700 hover:text-primary hover:bg-red-50/40 rounded-lg transition-all"
              >
                📜 Lịch sử hoạt động
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
                {ROLE_LABEL[user?.role ?? ""] ?? user?.role ?? "Owner"}
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
