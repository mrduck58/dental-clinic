"use client";

import { useRouter, usePathname } from "next/navigation";
import { useRef, useState, useTransition } from "react";

export interface SortOption {
  value: string;
  label: string;
}

export interface FilterGroup {
  key: string;
  label: string;
  options: { value: string; label: string }[];
}

interface Props {
  searchPlaceholder?: string;
  initialSearch?: string;
  initialSort?: string;
  initialFilters?: Record<string, string>;
  sortOptions?: SortOption[];
  filterGroups?: FilterGroup[];
  totalCount: number;
  filteredCount: number;
}

export default function FilterBar({
  searchPlaceholder = "Tìm kiếm...",
  initialSearch = "",
  initialSort = "default",
  initialFilters = {},
  sortOptions,
  filterGroups,
  totalCount,
  filteredCount,
}: Props) {
  const router = useRouter();
  const pathname = usePathname();
  const [isPending, startTransition] = useTransition();
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [search, setSearch] = useState(initialSearch);
  const [sort, setSort] = useState(initialSort);
  const [filters, setFilters] = useState<Record<string, string>>(initialFilters);

  const buildUrl = (s: string, so: string, fi: Record<string, string>) => {
    const params = new URLSearchParams();
    if (s.trim()) params.set("q", s.trim());
    if (so && so !== "default") params.set("sort", so);
    for (const [k, v] of Object.entries(fi)) {
      if (v && v !== "all") params.set(k, v);
    }
    const qs = params.toString();
    return qs ? `${pathname}?${qs}` : pathname;
  };

  const navigate = (s: string, so: string, fi: Record<string, string>) => {
    startTransition(() => router.replace(buildUrl(s, so, fi)));
  };

  const handleSearchChange = (value: string) => {
    setSearch(value);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => navigate(value, sort, filters), 400);
  };

  const handleSortChange = (value: string) => {
    setSort(value);
    navigate(search, value, filters);
  };

  const handleFilterChange = (key: string, value: string) => {
    const next = { ...filters, [key]: value };
    setFilters(next);
    navigate(search, sort, next);
  };

  const handleClear = () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    const reset: Record<string, string> = Object.fromEntries(
      Object.keys(filters).map((k) => [k, "all"])
    );
    setSearch("");
    setSort("default");
    setFilters(reset);
    navigate("", "default", reset);
  };

  const isFiltered =
    search.trim() !== "" ||
    sort !== "default" ||
    Object.values(filters).some((v) => v && v !== "all");

  return (
    <div
      className={`mb-8 transition-opacity duration-200 ${
        isPending ? "opacity-60 pointer-events-none" : ""
      }`}
    >
      <div className="flex flex-col sm:flex-row gap-3">
        {/* Search */}
        <div className="relative flex-1">
          <svg
            className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 pointer-events-none"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z"
            />
          </svg>
          <input
            type="text"
            value={search}
            onChange={(e) => handleSearchChange(e.target.value)}
            placeholder={searchPlaceholder}
            className="w-full pl-10 pr-9 py-2.5 text-[14px] border border-slate-200 rounded-xl bg-white placeholder:text-slate-400 focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/10 transition-all"
          />
          {search && (
            <button
              type="button"
              onClick={() => handleSearchChange("")}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              aria-label="Xóa tìm kiếm"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>

        {/* Sort dropdown */}
        {sortOptions && (
          <select
            value={sort}
            onChange={(e) => handleSortChange(e.target.value)}
            className="py-2.5 px-4 text-[14px] border border-slate-200 rounded-xl bg-white focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/10 transition-all cursor-pointer text-slate-700 font-medium shrink-0"
          >
            {sortOptions.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        )}

        {/* Reset */}
        {isFiltered && (
          <button
            type="button"
            onClick={handleClear}
            className="py-2.5 px-4 text-[13px] font-bold text-slate-500 border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors shrink-0 flex items-center gap-1.5"
          >
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
            Xóa bộ lọc
          </button>
        )}
      </div>

      {/* Filter chip groups */}
      {filterGroups?.map((group) => (
        <div key={group.key} className="flex flex-wrap items-center gap-2 mt-3">
          <span className="text-[11px] font-black text-slate-400 uppercase tracking-wider mr-0.5">
            {group.label}:
          </span>
          {group.options.map((opt) => {
            const active = (filters[group.key] ?? "all") === opt.value;
            return (
              <button
                key={opt.value}
                type="button"
                onClick={() => handleFilterChange(group.key, opt.value)}
                className={`px-3 py-1 rounded-full text-[12px] font-bold transition-all ${
                  active
                    ? "bg-primary text-white shadow-sm shadow-primary/20"
                    : "bg-white border border-slate-200 text-slate-500 hover:border-primary hover:text-primary"
                }`}
              >
                {opt.label}
              </button>
            );
          })}
        </div>
      ))}

      {/* Result count */}
      <p className="mt-2.5 text-[12px] text-slate-400 font-medium">
        {isFiltered && filteredCount === 0
          ? "Không tìm thấy kết quả phù hợp"
          : isFiltered
          ? `${filteredCount} / ${totalCount} kết quả`
          : `${totalCount} kết quả`}
      </p>
    </div>
  );
}
