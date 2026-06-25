import { cache } from "react";
import type { ServiceDto, PostDto, DentistDto, FeedbackDto, PromotionDto, ClinicInfoDto } from "@/types/api";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "") ?? "http://localhost/api";

/** Lỗi từ API, mang theo HTTP status để phân biệt 404 (notFound) với lỗi khác (error.tsx). */
export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = "ApiError";
  }
}

/**
 * Fetch JSON từ backend. Luôn `no-store` để trang render động (dữ liệu mới nhất mỗi request).
 * Ném `ApiError` khi response không OK — để route phân biệt 404 và hiển thị error.tsx.
 */
async function apiGet<T>(path: string): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, { cache: "no-store" });
  if (!res.ok) {
    throw new ApiError(res.status, `API ${path} trả về ${res.status} ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

// ── Dịch vụ ──────────────────────────────────────────────────────────────
export function getServices(): Promise<ServiceDto[]> {
  return apiGet<ServiceDto[]>("/services?status=active");
}

export const getServiceById = cache((id: string): Promise<ServiceDto> =>
  apiGet<ServiceDto>(`/services/${id}`)
);

// ── Bài viết / Tin tức ───────────────────────────────────────────────────
export function getPosts(): Promise<PostDto[]> {
  return apiGet<PostDto[]>("/posts?status=published");
}

export const getPostById = cache((id: string): Promise<PostDto> =>
  apiGet<PostDto>(`/posts/${id}`)
);

/** Bài viết đã xuất bản gắn với một dịch vụ cụ thể (hiển thị ở trang chi tiết dịch vụ). */
export const getPostsByService = cache((serviceId: string): Promise<PostDto[]> =>
  apiGet<PostDto[]>(`/posts?status=published&serviceId=${serviceId}`)
);

// ── Bác sĩ ───────────────────────────────────────────────────────────────
export function getDentists(): Promise<DentistDto[]> {
  return apiGet<DentistDto[]>("/dentists");
}

// Backend không có endpoint get-by-id cho bác sĩ → lấy danh sách rồi tìm theo id.
export const getDentistById = cache(async (id: string): Promise<DentistDto | undefined> => {
  const dentists = await getDentists();
  return dentists.find((d) => d.id === id);
});

// ── Đánh giá nổi bật ──────────────────────────────────────────────────────
export function getFeaturedFeedbacks(): Promise<FeedbackDto[]> {
  return apiGet<FeedbackDto[]>("/feedbacks/featured");
}

// ── Khuyến mãi ─────────────────────────────────────────────────────────────
export function getPromotions(): Promise<PromotionDto[]> {
  return apiGet<PromotionDto[]>("/promotions");
}

// ── Thông tin giới thiệu phòng khám ─────────────────────────────────────────
// Tài nguyên singleton dùng chung cho nhiều section/trang. Bọc `cache()` để các
// section trên cùng một trang chỉ gọi API một lần mỗi request.
export const getClinicInfo = cache((): Promise<ClinicInfoDto> =>
  apiGet<ClinicInfoDto>("/clinic-info")
);

/**
 * Phiên bản an toàn: trả về `null` thay vì ném lỗi khi API chưa sẵn sàng
 * (chưa seed / backend tắt). Dùng cho các section tự fetch, để fallback về
 * nội dung mặc định mà không làm sập cả trang.
 */
export const getClinicInfoSafe = cache(async (): Promise<ClinicInfoDto | null> => {
  try {
    return await getClinicInfo();
  } catch {
    return null;
  }
});
