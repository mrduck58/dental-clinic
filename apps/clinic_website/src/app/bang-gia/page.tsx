import type { Metadata } from "next";
import Link from "next/link";
import PageHeader from "@/components/shared/PageHeader";
import FilterBar from "@/components/shared/FilterBar";
import Pagination from "@/components/shared/Pagination";
import { getServices } from "@/lib/api";
import { formatPrice, formatDuration } from "@/lib/format";
import { matchesSearch, paginate, parsePage, buildPreserve } from "@/lib/listing";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Bảng giá dịch vụ | Sơn Giang Dental Clinic",
  description: "Bảng giá dịch vụ nha khoa minh bạch tại Sơn Giang Dental — niềng răng, Implant, bọc sứ, tẩy trắng, điều trị tủy và nhiều dịch vụ khác.",
};

const PAGE_SIZE = 12;

const SORT_OPTIONS = [
  { value: "default", label: "Mặc định" },
  { value: "price-asc", label: "Giá: thấp → cao" },
  { value: "price-desc", label: "Giá: cao → thấp" },
  { value: "name", label: "Tên A → Z" },
  { value: "duration", label: "Thời lượng" },
];

const PRICE_GROUP = {
  key: "price",
  label: "Mức giá",
  options: [
    { value: "all", label: "Tất cả" },
    { value: "lt1m", label: "Dưới 1 triệu" },
    { value: "1m-5m", label: "1 – 5 triệu" },
    { value: "gt5m", label: "Trên 5 triệu" },
  ],
};

type Props = {
  searchParams: Promise<{ q?: string; sort?: string; price?: string; page?: string }>;
};

export default async function BangGiaPage({ searchParams }: Props) {
  const sp = await searchParams;
  const q = sp.q ?? "";
  const sort = sp.sort ?? "default";
  const price = sp.price ?? "all";

  const all = await getServices();

  let filtered = all.filter((s) => matchesSearch(q, s.name, s.description));
  if (price === "lt1m") filtered = filtered.filter((s) => s.price < 1_000_000);
  else if (price === "1m-5m") filtered = filtered.filter((s) => s.price >= 1_000_000 && s.price <= 5_000_000);
  else if (price === "gt5m") filtered = filtered.filter((s) => s.price > 5_000_000);

  const sorted = [...filtered];
  if (sort === "price-asc") sorted.sort((a, b) => a.price - b.price);
  else if (sort === "price-desc") sorted.sort((a, b) => b.price - a.price);
  else if (sort === "name") sorted.sort((a, b) => a.name.localeCompare(b.name, "vi"));
  else if (sort === "duration") sorted.sort((a, b) => a.durationMinutes - b.durationMinutes);

  const paged = paginate(sorted, parsePage(sp.page), PAGE_SIZE);
  const preserve = buildPreserve({ q, sort, price });

  return (
    <div className="animate-fade-in">
      <PageHeader
        eyebrow="Bảng Giá"
        title="Bảng giá dịch vụ"
        description="Chi phí minh bạch, công khai — báo giá rõ ràng trước điều trị, không phát sinh chi phí ẩn."
      />

      <section className="py-16 bg-slate-50">
        <div className="max-w-5xl mx-auto px-6">
          <FilterBar
            searchPlaceholder="Tìm dịch vụ theo tên..."
            initialSearch={q}
            initialSort={sort}
            initialFilters={{ price }}
            sortOptions={SORT_OPTIONS}
            filterGroups={[PRICE_GROUP]}
            totalCount={all.length}
            filteredCount={filtered.length}
          />

          {paged.items.length === 0 ? (
            <p className="text-center text-slate-400 text-[15px] py-10">
              {filtered.length === 0 && all.length > 0
                ? "Không tìm thấy dịch vụ phù hợp."
                : "Chưa có dịch vụ nào."}
            </p>
          ) : (
            <div className="bg-white rounded-3xl border border-slate-200/60 shadow-sm overflow-hidden">
              {/* Header bảng */}
              <div className="hidden sm:grid grid-cols-12 gap-4 px-6 py-4 bg-slate-900 text-white text-[12px] font-black uppercase tracking-wider">
                <div className="col-span-6">Dịch vụ</div>
                <div className="col-span-3 text-center">Thời lượng</div>
                <div className="col-span-3 text-right">Giá tham khảo</div>
              </div>

              {/* Hàng */}
              <div className="divide-y divide-slate-100">
                {paged.items.map((s) => (
                  <Link
                    key={s.id}
                    href={`/dich-vu/${s.id}`}
                    className="grid grid-cols-1 sm:grid-cols-12 gap-1 sm:gap-4 px-6 py-5 items-center hover:bg-slate-50 transition-colors group"
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
                      {formatPrice(s.price)}
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          )}

          <Pagination
            currentPage={paged.currentPage}
            totalPages={paged.totalPages}
            basePath="/bang-gia"
            preserveParams={preserve}
          />

          {/* Ghi chú + CTA */}
          <div className="mt-8 flex flex-col sm:flex-row items-center justify-between gap-4 bg-primary/5 border border-primary/10 rounded-2xl px-6 py-5">
            <p className="text-[13px] text-slate-600 leading-relaxed">
              * Giá trên mang tính tham khảo. Chi phí cụ thể sẽ được bác sĩ tư vấn sau khi thăm khám trực tiếp.
            </p>
            <Link
              href="/tai-ung-dung"
              className="shrink-0 inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-6 py-3 rounded-xl font-bold text-[14px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
            >
              Đặt lịch tư vấn miễn phí
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
