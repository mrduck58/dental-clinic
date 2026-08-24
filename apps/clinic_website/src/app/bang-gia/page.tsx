import type { Metadata } from "next";
import FilterBar from "@/components/shared/FilterBar";
import Pagination from "@/components/shared/Pagination";
import PriceListTable from "@/components/sections/PriceListTable";
import { getServices } from "@/lib/api";
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
      <section className="py-16 bg-slate-50">
        <div className="max-w-7xl mx-auto px-6">
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
            <PriceListTable services={paged.items} />
          )}

          <Pagination
            currentPage={paged.currentPage}
            totalPages={paged.totalPages}
            basePath="/bang-gia"
            preserveParams={preserve}
          />

          {/* Ghi chú */}
          <div className="mt-8 bg-primary/5 border border-primary/10 rounded-2xl px-6 py-5">
            <p className="text-[13px] text-slate-600 leading-relaxed">
              * Giá trên mang tính tham khảo. Chi phí cụ thể sẽ được bác sĩ tư vấn sau khi thăm khám trực tiếp.
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
