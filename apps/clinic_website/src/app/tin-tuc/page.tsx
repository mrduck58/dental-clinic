import type { Metadata } from "next";
import PageHeader from "@/components/shared/PageHeader";
import FeaturedPost from "@/components/sections/FeaturedPost";
import NewsSection from "@/components/sections/NewsSection";
import FilterBar from "@/components/shared/FilterBar";
import Pagination from "@/components/shared/Pagination";
import { getPosts } from "@/lib/api";
import { matchesSearch, paginate, parsePage, buildPreserve } from "@/lib/listing";
import type { PostDto } from "@/types/api";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Tin tức & Sự kiện | Sơn Giang Dental Clinic",
  description: "Cập nhật tin tức, kiến thức nha khoa, công nghệ điều trị mới nhất và các sự kiện sắp tới tại Sơn Giang Dental.",
};

const PAGE_SIZE = 9;

const SORT_OPTIONS = [
  { value: "default", label: "Mặc định" },
  { value: "newest", label: "Mới nhất trước" },
  { value: "oldest", label: "Cũ nhất trước" },
  { value: "az", label: "Tiêu đề A → Z" },
];

function postTime(p: PostDto): number {
  return new Date(p.publishedAt ?? p.createdAt).getTime();
}

type Props = {
  searchParams: Promise<{ q?: string; sort?: string; category?: string; page?: string }>;
};

export default async function TinTucPage({ searchParams }: Props) {
  const sp = await searchParams;
  const q = sp.q ?? "";
  const sort = sp.sort ?? "default";
  const category = sp.category ?? "all";

  const all = await getPosts();

  // Danh sách chuyên mục duy nhất → chip lọc động.
  const categories = Array.from(
    new Set(all.map((p) => p.category).filter((c): c is string => Boolean(c)))
  ).sort((a, b) => a.localeCompare(b, "vi"));

  const categoryGroup = {
    key: "category",
    label: "Chuyên mục",
    options: [
      { value: "all", label: "Tất cả" },
      ...categories.map((c) => ({ value: c, label: c })),
    ],
  };

  let filtered = all.filter((p) => matchesSearch(q, p.title, p.content, p.author));
  if (category !== "all") filtered = filtered.filter((p) => p.category === category);

  const sorted = [...filtered];
  if (sort === "newest") sorted.sort((a, b) => postTime(b) - postTime(a));
  else if (sort === "oldest") sorted.sort((a, b) => postTime(a) - postTime(b));
  else if (sort === "az") sorted.sort((a, b) => a.title.localeCompare(b.title, "vi"));

  const isFiltering = q.trim() !== "" || category !== "all" || sort !== "default";

  // Khi không lọc: bài đầu làm bài tiêu biểu, phần còn lại lên lưới.
  // Khi đang lọc: bỏ bài tiêu biểu, toàn bộ kết quả lên lưới.
  const featured = isFiltering ? undefined : sorted[0];
  const gridSource = isFiltering ? sorted : sorted.slice(1);

  const paged = paginate(gridSource, parsePage(sp.page), PAGE_SIZE);
  const preserve = buildPreserve({ q, sort, category });

  return (
    <div className="animate-fade-in">
      <PageHeader
        eyebrow="Tin Tức & Sự Kiện"
        title="Tin tức"
        description="Cập nhật những tin tức, kiến thức và sự kiện mới nhất từ Sơn Giang Dental."
      />

      {featured && paged.currentPage === 1 && <FeaturedPost post={featured} />}

      <NewsSection
        posts={paged.items}
        heading={isFiltering ? "Kết quả tìm kiếm" : "Cập Nhật Mới Nhất"}
        showBanner={!isFiltering}
        toolbar={
          <FilterBar
            searchPlaceholder="Tìm bài viết..."
            initialSearch={q}
            initialSort={sort}
            initialFilters={{ category }}
            sortOptions={SORT_OPTIONS}
            filterGroups={categories.length > 0 ? [categoryGroup] : undefined}
            totalCount={all.length}
            filteredCount={filtered.length}
          />
        }
        footer={
          <Pagination
            currentPage={paged.currentPage}
            totalPages={paged.totalPages}
            basePath="/tin-tuc"
            preserveParams={preserve}
          />
        }
      />
    </div>
  );
}
