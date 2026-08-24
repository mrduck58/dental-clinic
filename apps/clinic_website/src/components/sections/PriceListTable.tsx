"use client";

import { useState } from "react";
import Link from "next/link";
import type { ServiceDto } from "@/types/api";
import { formatPrice, formatDuration } from "@/lib/format";

export default function PriceListTable({ services }: { services: ServiceDto[] }) {
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  function toggle(id: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  return (
    <div className="bg-white rounded-3xl border border-slate-200/60 shadow-sm overflow-hidden">
      {/* Header bảng */}
      <div className="hidden sm:grid grid-cols-12 gap-4 px-6 py-4 bg-slate-900 text-white text-[12px] font-black uppercase tracking-wider">
        <div className="col-span-6">Dịch vụ</div>
        <div className="col-span-3 text-center">Thời lượng</div>
        <div className="col-span-3 text-right">Giá tham khảo</div>
      </div>

      {/* Hàng */}
      <div className="divide-y divide-slate-100">
        {services.map((s) => {
          const hasOptions = Boolean(s.options && s.options.length > 0);
          const isOpen = expanded.has(s.id);
          return (
            <div key={s.id}>
              <div className="flex items-center">
                <Link
                  href={`/dich-vu/${s.id}`}
                  className="flex-1 min-w-0 grid grid-cols-1 sm:grid-cols-12 gap-1 sm:gap-4 px-6 py-5 items-center hover:bg-slate-50 transition-colors group"
                >
                  <div className="sm:col-span-6">
                    <div className="text-[15px] font-bold text-slate-900 group-hover:text-primary transition-colors">{s.name}</div>
                    {s.description && (
                      <div className="text-[12px] text-slate-400 line-clamp-1 mt-0.5">{s.description}</div>
                    )}
                  </div>
                  <div className="sm:col-span-3 sm:text-center text-[13px] text-slate-500 font-medium">
                    {formatDuration(s.durationMinutes)}
                  </div>
                  <div className="sm:col-span-3 sm:text-right text-[16px] font-black text-primary">
                    {hasOptions ? `Từ ${formatPrice(s.price)}` : formatPrice(s.price)}
                  </div>
                </Link>

                {hasOptions && (
                  <button
                    type="button"
                    onClick={() => toggle(s.id)}
                    aria-expanded={isOpen}
                    aria-label={isOpen ? "Ẩn tùy chọn" : "Xem tùy chọn"}
                    className="shrink-0 w-12 self-stretch flex items-center justify-center text-slate-400 hover:text-primary hover:bg-slate-50 transition-colors"
                  >
                    <svg
                      className={`w-4 h-4 transition-transform ${isOpen ? "rotate-180" : ""}`}
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2.5"
                      viewBox="0 0 24 24"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </button>
                )}
              </div>

              {hasOptions && isOpen && (
                <div className="px-6 pb-5 -mt-1 bg-slate-50/60">
                  <div className="border border-slate-200/80 rounded-xl overflow-hidden bg-white">
                    {s.options!
                      .slice()
                      .sort((a, b) => a.sortOrder - b.sortOrder)
                      .map((opt) => (
                        <div
                          key={opt.id}
                          className="flex items-center justify-between px-4 py-2.5 text-[13px] border-b border-slate-100 last:border-0"
                        >
                          <span className="font-semibold text-slate-700">{opt.name}</span>
                          <span className="font-bold text-primary">
                            {formatPrice(opt.price)} {opt.unit ? `/ ${opt.unit}` : ""}
                          </span>
                        </div>
                      ))}
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
