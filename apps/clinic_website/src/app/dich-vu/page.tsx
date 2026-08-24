import type { Metadata } from "next";
import ServicesSection from "@/components/sections/ServicesSection";
import ProcessSection from "@/components/sections/ProcessSection";
import FilterBar from "@/components/shared/FilterBar";
import Pagination from "@/components/shared/Pagination";
import { getServices } from "@/lib/api";
import { matchesSearch, paginate, parsePage, buildPreserve } from "@/lib/listing";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Dịch vụ | Sơn Giang Dental Clinic",
  description: "Đa dạng dịch vụ điều trị và thẩm mỹ răng miệng cao cấp: niềng răng, Implant, bọc sứ, tẩy trắng, điều trị tủy, nhổ răng khôn.",
};

const PAGE_SIZE = 9;

const SORT_OPTIONS = [
  { value: "default", label: "Mặc định" },
  { value: "price-asc", label: "Giá: thấp → cao" },
  { value: "price-desc", label: "Giá: cao → thấp" },
  { value: "name", label: "Tên A → Z" },
  { value: "popular", label: "Xem nhiều nhất" },
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

export default async function DichVuPage({ searchParams }: Props) {
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
  else if (sort === "popular") sorted.sort((a, b) => b.viewCount - a.viewCount);

  const paged = paginate(sorted, parsePage(sp.page), PAGE_SIZE);
  const preserve = buildPreserve({ q, sort, price });

  return (
    <div className="animate-fade-in">
      <ServicesSection
        services={paged.items}
        showIntro={false}
        toolbar={
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
        }
        footer={
          <Pagination
            currentPage={paged.currentPage}
            totalPages={paged.totalPages}
            basePath="/dich-vu"
            preserveParams={preserve}
          />
        }
      />
      <ProcessSection />
    </div>
  );
}
