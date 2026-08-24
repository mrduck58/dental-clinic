// Tiện ích lọc / tìm kiếm / phân trang phía server (thuần, không phụ thuộc React).

/** Chuẩn hoá chuỗi: bỏ dấu tiếng Việt + lowercase để tìm kiếm không phân biệt dấu. */
export function normalizeText(s: string): string {
  return s
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .trim();
}

/** Khớp tìm kiếm: query rỗng → luôn khớp; ngược lại tìm trong các trường (không phân biệt dấu). */
export function matchesSearch(
  query: string,
  ...fields: (string | null | undefined)[]
): boolean {
  const q = normalizeText(query);
  if (!q) return true;
  const hay = normalizeText(fields.filter(Boolean).join(" "));
  return hay.includes(q);
}

export interface Paged<T> {
  items: T[];
  currentPage: number;
  totalPages: number;
  total: number;
}

/** Cắt mảng theo trang. currentPage luôn được kẹp trong [1, totalPages]. */
export function paginate<T>(items: T[], page: number, pageSize: number): Paged<T> {
  const total = items.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const currentPage = Math.min(Math.max(1, page), totalPages);
  const start = (currentPage - 1) * pageSize;
  return { items: items.slice(start, start + pageSize), currentPage, totalPages, total };
}

/** Parse tham số ?page= → số nguyên dương, mặc định 1. */
export function parsePage(raw?: string): number {
  const n = parseInt(raw ?? "1", 10);
  return Number.isFinite(n) && n > 0 ? n : 1;
}

/** Xáo trộn ngẫu nhiên mảng (Fisher–Yates), không sửa mảng gốc. */
export function shuffle<T>(items: T[]): T[] {
  const result = items.slice();
  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
}

/** Tạo query-string giữ lại các filter (bỏ qua giá trị rỗng / "all" / "default") để truyền cho Pagination. */
export function buildPreserve(params: Record<string, string>): string {
  const ps = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v && v !== "all" && v !== "default") ps.set(k, v);
  }
  return ps.toString();
}
