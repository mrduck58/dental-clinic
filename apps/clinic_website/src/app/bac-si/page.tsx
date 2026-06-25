import type { Metadata } from "next";
import PageHeader from "@/components/shared/PageHeader";
import DentistsSection from "@/components/sections/DentistsSection";
import TestimonialsSection from "@/components/sections/TestimonialsSection";
import FilterBar from "@/components/shared/FilterBar";
import Pagination from "@/components/shared/Pagination";
import { getDentists, getFeaturedFeedbacks } from "@/lib/api";
import { matchesSearch, paginate, parsePage, buildPreserve } from "@/lib/listing";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Đội ngũ bác sĩ | Sơn Giang Dental Clinic",
  description: "Đội ngũ bác sĩ chuyên gia tâm huyết, giàu kinh nghiệm lâm sàng và liên tục tu nghiệp quốc tế tại Sơn Giang Dental.",
};

const PAGE_SIZE = 9;

const SORT_OPTIONS = [
  { value: "default", label: "Mặc định" },
  { value: "exp-desc", label: "Kinh nghiệm nhiều nhất" },
  { value: "exp-asc", label: "Kinh nghiệm ít nhất" },
  { value: "name", label: "Tên A → Z" },
];

type Props = {
  searchParams: Promise<{ q?: string; sort?: string; specialty?: string; page?: string }>;
};

export default async function BacSiPage({ searchParams }: Props) {
  const sp = await searchParams;
  const q = sp.q ?? "";
  const sort = sp.sort ?? "default";
  const specialty = sp.specialty ?? "all";

  const [all, feedbacks] = await Promise.all([getDentists(), getFeaturedFeedbacks()]);

  // Danh sách chuyên khoa duy nhất → chip lọc động.
  const specialties = Array.from(
    new Set(all.map((d) => d.specialty).filter((s): s is string => Boolean(s)))
  ).sort((a, b) => a.localeCompare(b, "vi"));

  const specialtyGroup = {
    key: "specialty",
    label: "Chuyên khoa",
    options: [
      { value: "all", label: "Tất cả" },
      ...specialties.map((s) => ({ value: s, label: s })),
    ],
  };

  let filtered = all.filter((d) => matchesSearch(q, d.fullName, d.specialty, d.bio));
  if (specialty !== "all") filtered = filtered.filter((d) => d.specialty === specialty);

  const sorted = [...filtered];
  if (sort === "exp-desc") sorted.sort((a, b) => (b.yearsOfExperience ?? 0) - (a.yearsOfExperience ?? 0));
  else if (sort === "exp-asc") sorted.sort((a, b) => (a.yearsOfExperience ?? 0) - (b.yearsOfExperience ?? 0));
  else if (sort === "name") sorted.sort((a, b) => (a.fullName ?? "").localeCompare(b.fullName ?? "", "vi"));

  const paged = paginate(sorted, parsePage(sp.page), PAGE_SIZE);
  const preserve = buildPreserve({ q, sort, specialty });

  return (
    <div className="animate-fade-in">
      <PageHeader
        eyebrow="Đội Ngũ"
        title="Bác sĩ"
        description="Đội ngũ bác sĩ tâm huyết, giàu kinh nghiệm lâm sàng và liên tục tu nghiệp quốc tế."
      />
      <DentistsSection
        dentists={paged.items}
        showIntro={false}
        toolbar={
          <FilterBar
            searchPlaceholder="Tìm bác sĩ theo tên..."
            initialSearch={q}
            initialSort={sort}
            initialFilters={{ specialty }}
            sortOptions={SORT_OPTIONS}
            filterGroups={specialties.length > 0 ? [specialtyGroup] : undefined}
            totalCount={all.length}
            filteredCount={filtered.length}
          />
        }
        footer={
          <Pagination
            currentPage={paged.currentPage}
            totalPages={paged.totalPages}
            basePath="/bac-si"
            preserveParams={preserve}
          />
        }
      />
      <TestimonialsSection feedbacks={feedbacks} />
    </div>
  );
}
