"use client";

import Link from "next/link";

interface NotificationBellProps {
  hasUnread?: boolean;
}

export default function NotificationBell({ hasUnread = true }: NotificationBellProps) {
  return (
    <Link
      href="/dashboard/notifications"
      className="relative p-2.5 rounded-full bg-slate-100 text-slate-600 hover:bg-red-50 hover:text-primary transition-all cursor-pointer shrink-0"
      aria-label="Xem thông báo"
    >
      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
      </svg>
      {hasUnread && (
        <span className="absolute top-1.5 right-1.5 w-3 h-3 bg-primary rounded-full border-2 border-white" />
      )}
    </Link>
  );
}
