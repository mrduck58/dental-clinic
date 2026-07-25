import type { Metadata } from "next";
import Link from "next/link";
import PageHeader from "@/components/shared/PageHeader";
import FilterBar from "@/components/shared/FilterBar";
import Pagination from "@/components/shared/Pagination";
import { getPromotions } from "@/lib/api";
import { formatDate, formatDiscount, getPromotionStatus } from "@/lib/format";
import { matchesSearch, paginate, parsePage, buildPreserve } from "@/lib/listing";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Khuyến mãi | Sơn Giang Dental Clinic",
  description: "Các chương trình ưu đãi, khuyến mãi dịch vụ nha khoa đang diễn ra tại Sơn Giang Dental.",
};

const PAGE_SIZE = 9;

const SORT_OPTIONS = [
  { value: "default", label: "Mặc định" },
  { value: "ending-soon", label: "Sắp hết hạn" },
  { value: "newest", label: "Mới nhất" },
  { value: "name", label: "Tên A → Z" },
];

const STATUS_GROUP = {
  key: "status",
  label: "Trạng thái",
  options: [
    { value: "all", label: "Tất cả" },
    { value: "active", label: "Đang diễn ra" },
    { value: "upcoming", label: "Sắp diễn ra" },
    { value: "expired", label: "Đã kết thúc" },
  ],
};

const STATUS_BADGE: Record<string, { label: string; cls: string } | null> = {
  active: null,
  upcoming: { label: "Sắp diễn ra", cls: "bg-amber-500/90 text-white" },
  expired: { label: "Đã kết thúc", cls: "bg-slate-500/90 text-white" },
};

type Props = {
  searchParams: Promise<{ q?: string; sort?: string; status?: string; page?: string }>;
};

export default async function KhuyenMaiPage({ searchParams }: Props) {
  const sp = await searchParams;
  const q = sp.q ?? "";
  const sort = sp.sort ?? "default";
  const status = sp.status ?? "active";

  const all = await getPromotions();

  let filtered = all.filter((p) =>
    matchesSearch(q, p.name, p.code, p.description, p.serviceNames.join(" "))
  );
  if (status !== "all") filtered = filtered.filter((p) => getPromotionStatus(p) === status);

  const sorted = [...filtered];
  if (sort === "ending-soon") {
    sorted.sort((a, b) => new Date(a.endDate).getTime() - new Date(b.endDate).getTime());
  } else if (sort === "newest") {
    sorted.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  } else if (sort === "name") {
    sorted.sort((a, b) => a.name.localeCompare(b.name, "vi"));
  }

  const paged = paginate(sorted, parsePage(sp.page), PAGE_SIZE);
  const preserve = buildPreserve({ q, sort, status });

  return (
    <div className="animate-fade-in">
      <PageHeader
        eyebrow="Ưu Đãi"
        title="Khuyến mãi"
        description="Đừng bỏ lỡ những chương trình ưu đãi hấp dẫn đang diễn ra tại Sơn Giang Dental."
      />

      <section className="py-16 bg-slate-50">
        <div className="max-w-7xl mx-auto px-6">
          <FilterBar
            searchPlaceholder="Tìm theo tên hoặc mã ưu đãi..."
            initialSearch={q}
            initialSort={sort}
            initialFilters={{ status }}
            sortOptions={SORT_OPTIONS}
            filterGroups={[STATUS_GROUP]}
            totalCount={all.length}
            filteredCount={filtered.length}
          />

          {paged.items.length === 0 ? (
            <div className="text-center py-16">
              <span className="text-5xl">🎁</span>
              <p className="text-slate-400 text-[15px] mt-4">
                {all.length > 0
                  ? "Không tìm thấy chương trình khuyến mãi phù hợp."
                  : "Hiện chưa có chương trình khuyến mãi nào."}
              </p>
              <Link href="/tai-ung-dung" className="inline-flex items-center gap-2 mt-6 text-primary font-bold text-[14px] hover:gap-3 transition-all">
                Tải app để nhận thông báo ưu đãi sớm nhất
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                </svg>
              </Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {paged.items.map((p) => {
                const badge = STATUS_BADGE[getPromotionStatus(p)];
                const isExpired = getPromotionStatus(p) === "expired";
                return (
                  <div
                    key={p.id}
                    className={`relative bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col ${isExpired ? "opacity-75" : ""}`}
                  >
                    {/* Dải giảm giá */}
                    <div className="relative bg-gradient-to-r from-primary to-secondary px-6 py-5 text-white">
                      <div className="text-[11px] font-bold uppercase tracking-wider opacity-80 mb-1">Ưu đãi đến</div>
                      <div className="text-3xl font-black leading-none">
                        {formatDiscount(p.discountType, p.discountValue)}
                      </div>
                      {badge && (
                        <span className={`absolute top-4 right-4 text-[10px] font-black px-2.5 py-1 rounded-full ${badge.cls}`}>
                          {badge.label}
                        </span>
                      )}
                    </div>

                    <div className="p-6 flex flex-col flex-1">
                      <h3 className="text-[17px] font-bold text-slate-900 mb-2">{p.name}</h3>
                      {p.description && (
                        <p className="text-slate-500 text-[13px] leading-relaxed mb-4 flex-1">{p.description}</p>
                      )}

                      {/* Dịch vụ áp dụng */}
                      {p.serviceNames.length > 0 && (
                        <div className="flex flex-wrap gap-1.5 mb-4">
                          {p.serviceNames.map((name) => (
                            <span key={name} className="text-[11px] font-bold text-slate-500 bg-slate-100 px-2.5 py-1 rounded-lg">
                              {name}
                            </span>
                          ))}
                        </div>
                      )}

                      {/* Mã + hạn dùng */}
                      <div className="flex items-center justify-between border-t border-dashed border-slate-200 pt-4 mt-auto">
                        <span className="inline-flex items-center gap-1.5 text-[12px] font-black text-primary bg-primary/10 px-3 py-1.5 rounded-lg">
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M9.568 3H5.25A2.25 2.25 0 003 5.25v4.318c0 .597.237 1.17.659 1.591l9.581 9.581c.699.699 1.78.872 2.607.33a18.095 18.095 0 005.223-5.223c.542-.827.369-1.908-.33-2.607L11.16 3.66A2.25 2.25 0 009.568 3z" />
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 6h.008v.008H6V6z" />
                          </svg>
                          {p.code}
                        </span>
                        <span className="text-[12px] text-slate-400 font-medium">
                          HSD: {formatDate(p.endDate)}
                        </span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          <Pagination
            currentPage={paged.currentPage}
            totalPages={paged.totalPages}
            basePath="/khuyen-mai"
            preserveParams={preserve}
          />
        </div>
      </section>
    </div>
  );
}
