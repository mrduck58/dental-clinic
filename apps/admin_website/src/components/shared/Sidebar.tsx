"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { getUser, clearSession, type AuthUser } from "../../lib/apiClient";

const ROLE_LABEL: Record<string, string> = {
  Admin: "Quản trị viên",
  Doctor: "Bác sĩ",
  Receptionist: "Lễ tân",
  Accountant: "Kế toán",
};

interface SidebarProps {
  activeMenu: string;
}

export default function Sidebar({ activeMenu }: SidebarProps) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);

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
        <Link href="/" className="flex items-center gap-3 px-2 py-2 cursor-pointer select-none">
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
            href="/"
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

          {/* Quản lí nhân viên */}
          <Link
            href="#"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "staff"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
            </svg>
            Quản lí nhân viên
          </Link>

          {/* Quản lí dịch vụ */}
          <Link
            href="/dashboard/services"
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

          {/* Quản lí bài viết */}
          <Link
            href="/dashboard/posts"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "articles"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 7.5h1.5m-1.5 3h1.5m-7.5 3h7.5m-7.5 3h7.5m3-9h3.375c.621 0 1.125.504 1.125 1.125V18a2.25 2.25 0 01-2.25 2.25M16.5 7.5V18a2.25 2.25 0 002.25 2.25M16.5 7.5V4.875c0-.621-.504-1.125-1.125-1.125H4.125C3.504 3.75 3 4.254 3 4.875V18a2.25 2.25 0 002.25 2.25h13.5M6 7.5h3v3H6v-3z" />
            </svg>
            Quản lí bài viết
          </Link>

          {/* Quản lí phòng */}
          <Link
            href="/dashboard/rooms"
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

          {/* Lịch làm việc nhân viên */}
          <Link
            href="/dashboard/schedule"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "schedule"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z" />
            </svg>
            Lịch làm việc
          </Link>

          {/* Tài khoản và phân quyền */}
          <Link
            href="/dashboard/permissions"
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

          {/* Phản hồi */}
          <Link
            href="/dashboard/feedback"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "feedback"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0012 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018z" />
            </svg>
            Phản hồi
          </Link>

          {/* Đơn xin nghỉ */}
          <Link
            href="#"
            className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all ${activeMenu === "leaves"
                ? "bg-primary text-white shadow-md shadow-primary/25"
                : "text-slate-500 hover:bg-red-50 hover:text-primary"
              }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
            </svg>
            Đơn xin nghỉ
          </Link>

          {/* Lịch sử hoạt động */}
          <Link
            href="/dashboard/activity-logs"
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
        </nav>
      </div>

      {/* User Profile & Logout */}
      <div className="border-t border-slate-100 pt-4 flex flex-col gap-3">
        <div className="flex items-center gap-3 px-2 select-none">
          <div className="w-10 h-10 rounded-full border-2 border-primary/20 bg-red-50/50 flex items-center justify-center font-bold text-primary shrink-0">
            {initials}
          </div>
          <div className="min-w-0">
            <div className="text-[14px] font-bold text-slate-900 leading-tight truncate">
              {user?.fullName ?? user?.username ?? "..."}
            </div>
            <div className="text-[12px] font-semibold text-slate-400 mt-0.5">
              {ROLE_LABEL[user?.role ?? ""] ?? user?.role ?? ""}
            </div>
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
