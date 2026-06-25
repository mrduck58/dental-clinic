import Link from "next/link";

function getPageRange(current: number, total: number): (number | "...")[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const delta = 2;
  const range: (number | "...")[] = [1];
  if (current - delta > 2) range.push("...");
  for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
    range.push(i);
  }
  if (current + delta < total - 1) range.push("...");
  range.push(total);
  return range;
}

export default function Pagination({
  currentPage,
  totalPages,
  basePath,
  preserveParams,
}: {
  currentPage: number;
  totalPages: number;
  basePath: string;
  preserveParams?: string;
}) {
  if (totalPages <= 1) return null;

  const pageHref = (p: number) => {
    const ps = new URLSearchParams(preserveParams ?? "");
    if (p > 1) ps.set("page", String(p));
    else ps.delete("page");
    const qs = ps.toString();
    return qs ? `${basePath}?${qs}` : basePath;
  };

  const pages = getPageRange(currentPage, totalPages);

  return (
    <div className="flex justify-center items-center gap-2 mt-12">
      {currentPage > 1 && (
        <Link
          href={pageHref(currentPage - 1)}
          className="w-10 h-10 flex items-center justify-center rounded-xl border border-slate-200 text-slate-500 hover:border-primary hover:text-primary transition-colors"
          aria-label="Trang trước"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
          </svg>
        </Link>
      )}

      {pages.map((p, i) =>
        p === "..." ? (
          <span key={`el-${i}`} className="w-10 h-10 flex items-center justify-center text-slate-400 text-[14px]">
            …
          </span>
        ) : (
          <Link
            key={p}
            href={pageHref(p)}
            className={`w-10 h-10 flex items-center justify-center rounded-xl text-[14px] font-bold transition-colors ${
              p === currentPage
                ? "bg-primary text-white shadow-sm shadow-primary/30"
                : "border border-slate-200 text-slate-500 hover:border-primary hover:text-primary"
            }`}
            aria-current={p === currentPage ? "page" : undefined}
          >
            {p}
          </Link>
        )
      )}

      {currentPage < totalPages && (
        <Link
          href={pageHref(currentPage + 1)}
          className="w-10 h-10 flex items-center justify-center rounded-xl border border-slate-200 text-slate-500 hover:border-primary hover:text-primary transition-colors"
          aria-label="Trang sau"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
          </svg>
        </Link>
      )}
    </div>
  );
}
