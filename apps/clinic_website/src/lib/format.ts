// Các tiện ích định dạng dùng chung (thuần, không phụ thuộc React)

/** Định dạng ngày ISO → dd/MM/yyyy (vi-VN). */
export function formatDate(iso?: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

/** Định dạng số tiền VND → "550.000đ". */
export function formatPrice(value: number): string {
  return `${value.toLocaleString("vi-VN")}đ`;
}

/** Thời lượng phút → "45 phút" / "1 giờ 30 phút". */
export function formatDuration(minutes: number): string {
  if (minutes < 60) return `${minutes} phút`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m === 0 ? `${h} giờ` : `${h} giờ ${m} phút`;
}

/** Bài viết không có field excerpt → strip thẻ HTML/markdown rồi cắt ngắn. */
export function excerpt(content: string, len = 140): string {
  const text = content
    .replace(/<[^>]*>/g, " ") // bỏ thẻ HTML
    .replace(/[#*_>`~\-]+/g, " ") // bỏ ký tự markdown phổ biến
    .replace(/\s+/g, " ")
    .trim();
  if (text.length <= len) return text;
  return text.slice(0, len).trimEnd() + "…";
}

/** Định dạng mức giảm giá: "Percentage" → "20%", còn lại → số tiền. */
export function formatDiscount(discountType: string, value: number): string {
  return discountType === "Percentage" ? `${value}%` : formatPrice(value);
}

/** Khuyến mãi còn hiệu lực: isActive và hôm nay nằm trong [startDate, endDate]. */
export function isPromotionActive(
  isActive: boolean,
  startDate: string,
  endDate: string
): boolean {
  if (!isActive) return false;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const start = new Date(startDate);
  const end = new Date(endDate);
  end.setHours(23, 59, 59, 999);
  return start <= today && today <= end;
}

export type PromotionStatus = "active" | "upcoming" | "expired";

/** Trả về trạng thái khuyến mãi theo ngày hiện tại. */
export function getPromotionStatus(p: {
  isActive: boolean;
  startDate: string;
  endDate: string;
}): PromotionStatus {
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  const start = new Date(p.startDate);
  const end = new Date(p.endDate);
  end.setHours(23, 59, 59, 999);
  if (!p.isActive || end < now) return "expired";
  if (start > now) return "upcoming";
  return "active";
}

/** Màu badge category cho card tin tức. */
export function categoryColor(category: string): string {
  const map: Record<string, string> = {
    "Sự kiện": "bg-secondary/10 text-secondary",
    "Kiến thức": "bg-primary/10 text-primary",
    "Công nghệ": "bg-amber-500/10 text-amber-600",
  };
  return map[category] ?? "bg-slate-100 text-slate-600";
}
