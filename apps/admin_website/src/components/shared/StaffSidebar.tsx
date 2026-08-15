"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { getUser, clearSession, resolveAssetUrl, type AuthUser } from "../../lib/apiClient";

interface StaffSidebarProps {
  activeMenu: string;
}

const NAV_GROUPS: Array<{ title: string | null; items: Array<{ menu: string; href: string; label: string; icon: string; sw: string }> }> = [
  {
    title: null,
    items: [
      {
        menu: "overview",
        href: "/staff",
        label: "Tổng quan",
        icon: "M10.5 6a7.5 7.5 0 107.5 7.5h-7.5V6zM13.5 10.5H21A7.5 7.5 0 0013.5 3v7.5z",
        sw: "2.5",
      },
    ],
  },
  {
    title: "Tiếp đón & Lịch hẹn",
    items: [
      {
        menu: "appointments",
        href: "/staff/appointments",
        label: "Đặt lịch & Nhận đơn",
        icon: "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z",
        sw: "2",
      },
      {
        menu: "checkin",
        href: "/staff/checkin",
        label: "Check-in bệnh nhân",
        icon: "M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z",
        sw: "2",
      },
      {
        menu: "queue",
        href: "/staff/queue",
        label: "Hàng đợi",
        icon: "M3.75 5.25h16.5m-16.5 4.5h16.5m-16.5 4.5h16.5m-16.5 4.5h16.5",
        sw: "2",
      },
    ],
  },
  {
    title: "Vận hành",
    items: [
      {
        menu: "invoices",
        href: "/staff/invoices",
        label: "Hóa đơn & Thanh toán",
        icon: "M9 14.25l6-6m4.5-3.493V21.75l-3.75-1.5-3.75 1.5-3.75-1.5-3.75 1.5V4.757c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0111.186 0c1.1.128 1.907 1.077 1.907 2.185zM9.75 9h.008v.008H9.75V9zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm4.125 4.5h.008v.008h-.008V13.5zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z",
        sw: "2",
      },
      {
        menu: "inventory",
        href: "/staff/inventory",
        label: "Nhập xuất vật tư",
        icon: "M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z",
        sw: "2",
      },
      {
        menu: "articles",
        href: "/staff/posts",
        label: "Quản lí bài viết",
        icon: "M12 7.5h1.5m-1.5 3h1.5m-7.5 3h7.5m-7.5 3h7.5m3-9h3.375c.621 0 1.125.504 1.125 1.125V18a2.25 2.25 0 01-2.25 2.25M16.5 7.5V18a2.25 2.25 0 002.25 2.25M16.5 7.5V4.875c0-.621-.504-1.125-1.125-1.125H4.125C3.504 3.75 3 4.254 3 4.875V18a2.25 2.25 0 002.25 2.25h13.5M6 7.5h3v3H6v-3z",
        sw: "2",
      },
    ],
  },
  {
    title: "Cá nhân",
    items: [
      {
        menu: "payroll",
        href: "/staff/payroll",
        label: "Bảng lương",
        icon: "M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-1.971-.659-1.171-.879-1.171-2.303 0-3.182 1.172-.879 3.07-.879 4.242 0L15 9M3 5.25h18A2.25 2.25 0 0 1 21 7.5v9a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 16.5v-9a2.25 2.25 0 0 1 2.25-2.25Z",
        sw: "2",
      },
      {
        menu: "feedback",
        href: "/staff/feedback",
        label: "Phản hồi & Đánh giá",
        icon: "M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z",
        sw: "2",
      },
      {
        menu: "leave",
        href: "/staff/leave",
        label: "Đơn xin nghỉ",
        icon: "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z",
        sw: "2",
      },
      {
        menu: "notifications",
        href: "/staff/notifications",
        label: "Thông báo",
        icon: "M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0",
        sw: "2",
      },
    ],
  },
];

export default function StaffSidebar({ activeMenu }: StaffSidebarProps) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [dropdownOpen, setDropdownOpen] = useState(false);
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
    : (user?.username?.slice(0, 2).toUpperCase() ?? "??");

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
        className={`fixed inset-y-0 left-0 z-50 w-72 bg-white border-r border-slate-200 p-6 flex flex-col gap-6 shrink-0 h-[100dvh] max-h-[100dvh] justify-between transition-transform duration-300 ease-in-out lg:sticky lg:top-0 lg:translate-x-0 ${
          mobileOpen ? "translate-x-0 shadow-2xl" : "-translate-x-full"
        }`}
      >
        <div className="flex flex-col gap-6 flex-1 min-h-0">
          {/* Logo + Nút đóng trên Mobile */}
          <div className="flex items-center justify-between">
            <Link
              href="/staff"
              onClick={() => setMobileOpen(false)}
              className="flex items-center gap-3 px-2 py-2 cursor-pointer select-none"
            >
              <span className="text-3xl text-primary shrink-0">🦷</span>
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

        {/* Nav */}
        <nav className="flex flex-col gap-1 overflow-y-auto pr-1 flex-1">
          {NAV_GROUPS.map((group, gi) => (
            <div key={gi} className="flex flex-col gap-1">
              {group.title && (
                <div className="px-4 pt-3 pb-1 text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider">
                  {group.title}
                </div>
              )}
              {group.items.map(({ menu, href, label, icon, sw }) => {
                const isActive = activeMenu === menu;
                return (
                  <Link key={menu} href={href}
                    className={`flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold transition-all cursor-pointer w-full text-left ${
                      isActive ? "bg-primary text-white shadow-md shadow-primary/25" : "text-slate-500 hover:bg-red-50 hover:text-primary"
                    }`}>
                    <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth={sw} viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
                    </svg>
                    <span className="text-[13px]">{label}</span>
                  </Link>
                );
              })}
            </div>
          ))}
        </nav>
      </div>

      {/* User + Logout */}
      <div className="border-t border-slate-100 pt-4 flex flex-col gap-3">
        <div className="relative">
          {/* Dropdown Menu */}
          {dropdownOpen && (
            <div className="absolute bottom-full left-0 mb-2 w-full bg-white border border-slate-200 rounded-xl shadow-lg p-2 z-50 flex flex-col gap-0.5 animate-fade-in font-sans">
              <Link
                href="/staff/profile?tab=personal"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[12.5px] font-bold text-slate-700 hover:text-emerald-600 hover:bg-emerald-50/40 rounded-lg transition-all"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
                Thông tin cá nhân
              </Link>
              <Link
                href="/staff/profile?tab=password"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[12.5px] font-bold text-slate-700 hover:text-emerald-600 hover:bg-emerald-50/40 rounded-lg transition-all"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
                Đổi mật khẩu
              </Link>
              <Link
                href="/staff/profile?tab=activities"
                onClick={() => setDropdownOpen(false)}
                className="flex items-center gap-2.5 px-3.5 py-2.5 text-[12.5px] font-bold text-slate-700 hover:text-emerald-600 hover:bg-emerald-50/40 rounded-lg transition-all"
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
                className="w-10 h-10 rounded-full border-2 border-emerald-200 object-cover shrink-0"
              />
            ) : (
              <div className="w-10 h-10 rounded-full border-2 border-emerald-200 bg-emerald-50 flex items-center justify-center font-bold text-emerald-600 shrink-0">
                {initials}
              </div>
            )}
            <div className="min-w-0 flex-1">
              <div className="text-[13px] font-bold text-slate-900 leading-tight truncate">
                {user?.fullName ?? user?.username ?? "..."}
              </div>
              <div className="text-[11px] font-semibold text-slate-400 mt-0.5">Nhân viên</div>
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
          onClick={() => { clearSession(); router.push("/auth/login"); }}
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
