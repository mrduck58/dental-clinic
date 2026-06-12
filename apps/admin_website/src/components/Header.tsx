"use client";

import React from "react";
import { usePathname } from "next/navigation";
import NotificationBell from "./shared/NotificationBell";

interface HeaderProps {
  title?: string;
  showSearch?: boolean;
}

export default function Header({ title, showSearch }: HeaderProps) {
  const pathname = usePathname();

  // Determine title dynamically if not provided as a prop
  const getHeaderTitle = () => {
    if (title) return title;
    if (pathname === "/") return "Tổng Quan Vận Hành";
    if (pathname === "/dashboard/posts") return "Quản lý Bài viết";
    if (pathname === "/dashboard/posts/create") return "Tạo Bài viết Mới";
    if (pathname === "/dashboard/feedback") return "Quản lý Phản hồi";
    if (pathname.includes("/edit")) return "Chỉnh sửa Bài viết";
    return "Hệ thống vận hành";
  };

  // Determine if search input should be visible
  // Search is hidden on Post list page ("/dashboard/posts") but shown on edit/create/dashboard.
  const displaySearch = showSearch !== undefined ? showSearch : (pathname !== "/dashboard/posts");

  return (
    <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
      <div>
        <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">
          {getHeaderTitle()}
        </h1>
      </div>

      {/* Search, Notifications */}
      <div className="flex items-center gap-6">
        {/* Search Input */}
        {displaySearch && (
          <div className="relative w-64 hidden sm:block">
            <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input
              type="text"
              placeholder="Tìm kiếm nhanh..."
              className="w-full pl-9 pr-4 py-2 text-[15px] bg-slate-100 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all"
            />
          </div>
        )}

        <NotificationBell />
      </div>
    </header>
  );
}
