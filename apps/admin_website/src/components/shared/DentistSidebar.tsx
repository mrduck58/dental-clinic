"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { getUser, clearSession, type AuthUser } from "../../lib/apiClient";

interface DentistSidebarProps {
  activeMenu: string;
  onTabChange?: (key: string) => void;
}

const NAV_ITEMS = [
  {
    menu: "overview",
    href: "/dentist",
    label: "Tổng quan",
    icon: "M10.5 6a7.5 7.5 0 107.5 7.5h-7.5V6zM13.5 10.5H21A7.5 7.5 0 0013.5 3v7.5z",
    sw: "2.5",
  },
  {
    menu: "schedule",
    href: "/dentist/schedule",
    label: "Lịch làm việc",
    icon: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z",
    sw: "2",
  },
  {
    menu: "patients",
    href: "/dentist/patients",
    label: "Bệnh nhân hôm nay",
    icon: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z",
    sw: "2",
  },
  {
    menu: "leave",
    href: "/dentist/leave",
    label: "Đơn xin nghỉ",
    icon: "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z",
    sw: "2",
  },
];

export default function DentistSidebar({ activeMenu, onTabChange }: DentistSidebarProps) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  useEffect(() => { setUser(getUser()); }, []);

  const initials = user?.fullName
    ? user.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
    : (user?.username?.slice(0, 2).toUpperCase() ?? "??");

  return (
    <aside className="w-72 bg-white border-r border-slate-200 p-6 flex flex-col gap-6 shrink-0 sticky top-0 h-screen justify-between z-30">
      <div className="flex flex-col gap-6 flex-1 min-h-0">
        {/* Logo */}
        <Link href="/dentist" className="flex items-center gap-3 px-2 py-2 cursor-pointer select-none">
          <span className="text-3xl text-primary shrink-0">🦷</span>
          <div className="flex flex-col">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase leading-none mb-1">SơnGiang</span>
            <span className="font-extrabold text-xl tracking-tight text-slate-900 leading-none">
              Dental<span className="text-primary font-bold">Clinic</span>
            </span>
          </div>
        </Link>

        {/* Role badge */}
        <div className="flex items-center gap-2 px-3 py-2 bg-sky-50 rounded-xl border border-sky-100">
          <div className="w-2 h-2 rounded-full bg-sky-500 shrink-0" />
          <span className="text-[12.5px] font-bold text-sky-700">Cổng thông tin Bác sĩ</span>
        </div>

        {/* Nav */}
        <nav className="flex flex-col gap-1 overflow-y-auto pr-1 flex-1">
          {NAV_ITEMS.map(({ menu, href, label, icon, sw }) => {
            const isActive = activeMenu === menu;
            const cls = `flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all cursor-pointer w-full text-left ${
              isActive ? "bg-primary text-white shadow-md shadow-primary/25" : "text-slate-500 hover:bg-red-50 hover:text-primary"
            }`;
            const content = (
              <>
                <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth={sw} viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
                </svg>
                {label}
              </>
            );
            return onTabChange ? (
              <button key={menu} onClick={() => onTabChange(menu)} className={cls}>{content}</button>
            ) : (
              <Link key={menu} href={href} className={cls}>{content}</Link>
            );
          })}
        </nav>
      </div>

      {/* User + Logout */}
      <div className="border-t border-slate-100 pt-4 flex flex-col gap-3">
        <div className="flex items-center gap-3 px-2 select-none">
          <div className="w-10 h-10 rounded-full border-2 border-sky-200 bg-sky-50 flex items-center justify-center font-bold text-secondary shrink-0">
            <span suppressHydrationWarning>{initials}</span>
          </div>
          <div className="min-w-0 flex-1">
            <div className="text-[14px] font-bold text-slate-900 leading-tight truncate" suppressHydrationWarning>
              {user?.fullName ?? user?.username ?? "..."}
            </div>
            <div className="text-[12px] font-semibold text-slate-400 mt-0.5">Bác sĩ</div>
          </div>
        </div>
        <button
          onClick={() => { clearSession(); router.push("/auth/login"); }}
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
