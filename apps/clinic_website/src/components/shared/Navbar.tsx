"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";

const NAV_LINKS = [
  { label: "Trang chủ", href: "/" },
  { label: "Giới thiệu", href: "/gioi-thieu" },
  { label: "Dịch vụ", href: "/dich-vu" },
  { label: "Bảng giá", href: "/bang-gia" },
  { label: "Bác sĩ", href: "/bac-si" },
  { label: "Khuyến mãi", href: "/khuyen-mai" },
  { label: "Tin tức", href: "/tin-tuc" },
  { label: "Liên hệ", href: "/lien-he" },
];

export default function Navbar() {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);

  const isActive = (href: string) =>
    href === "/" ? pathname === "/" : pathname.startsWith(href);

  return (
    <header className="sticky top-0 z-50 bg-white/90 backdrop-blur-md border-b border-slate-200/60 shadow-sm">
      <div className="max-w-7xl mx-auto px-6 h-20 flex items-center justify-between">
        {/* Logo */}
        <Link href="/" className="flex items-center gap-2.5 select-none">
          <span className="text-2xl text-primary">🦷</span>
          <div className="flex flex-col leading-none">
            <span className="text-[10px] font-black tracking-widest text-primary uppercase">SơnGiang</span>
            <span className="font-extrabold text-xl tracking-tight text-slate-900">
              Dental<span className="text-primary">Clinic</span>
            </span>
          </div>
        </Link>

        {/* Nav */}
        <nav className="hidden lg:flex items-center gap-0.5">
          {NAV_LINKS.map(({ label, href }) => (
            <Link
              key={href}
              href={href}
              className={`px-3 py-2 rounded-xl text-[14px] font-semibold whitespace-nowrap transition-all ${
                isActive(href)
                  ? "text-primary bg-primary/5"
                  : "text-slate-500 hover:text-primary hover:bg-slate-50"
              }`}
            >
              {label}
            </Link>
          ))}
        </nav>

        {/* CTA */}
        <Link
          href="/tai-ung-dung"
          className="hidden lg:flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-5 py-2.5 rounded-xl text-[14px] font-bold transition-all hover:translate-y-[-1px] hover:shadow-lg hover:shadow-primary/25"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 1.5H8.25A2.25 2.25 0 006 3.75v16.5a2.25 2.25 0 002.25 2.25h7.5A2.25 2.25 0 0018 20.25V3.75a2.25 2.25 0 00-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 8.25h3m-3 4.5h3m-6-4.5H6m0 4.5H4.5" />
          </svg>
          Tải ứng dụng
        </Link>

        {/* Mobile hamburger */}
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          aria-label="Mở menu"
          className="lg:hidden p-2 rounded-xl text-slate-500 hover:bg-slate-100 transition-all"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            {open ? (
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            ) : (
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
            )}
          </svg>
        </button>
      </div>

      {/* Mobile menu */}
      {open && (
        <nav className="lg:hidden border-t border-slate-200/60 bg-white px-6 py-4 flex flex-col gap-1">
          {NAV_LINKS.map(({ label, href }) => (
            <Link
              key={href}
              href={href}
              onClick={() => setOpen(false)}
              className={`px-4 py-2.5 rounded-xl text-[15px] font-semibold transition-all ${
                isActive(href)
                  ? "text-primary bg-primary/5"
                  : "text-slate-600 hover:text-primary hover:bg-slate-50"
              }`}
            >
              {label}
            </Link>
          ))}
          <Link
            href="/tai-ung-dung"
            onClick={() => setOpen(false)}
            className="mt-2 flex items-center justify-center gap-2 bg-primary text-white px-5 py-3 rounded-xl text-[15px] font-bold"
          >
            Tải ứng dụng
          </Link>
        </nav>
      )}
    </header>
  );
}
