const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5239";

// ── Types ──────────────────────────────────────────────────────────────────

export interface AuthUser {
  id: string;
  email: string;
  username: string;
  fullName: string | null;
  role: string;
  isActive: boolean;
}

export interface AccountDto {
  id: string;
  username: string;
  fullName: string | null;
  email: string;
  phoneNumber: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  user: AuthUser;
}

// ── Auth endpoints ─────────────────────────────────────────────────────────

export async function loginApi(email: string, password: string): Promise<LoginResponse> {
  const res = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đăng nhập thất bại");
  }

  return res.json() as Promise<LoginResponse>;
}

export interface CreateAccountCommand {
  fullName: string;
  email: string;
  phoneNumber: string;
  role: string;
}

export async function createAccountApi(data: CreateAccountCommand): Promise<void> {
  const res = await fetch(`${API_URL}/api/auth/accounts`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...authHeaders(),
    },
    body: JSON.stringify(data),
  });

  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo tài khoản thất bại");
  }
}

// ── Session helpers ────────────────────────────────────────────────────────

const TOKEN_KEY = "dental_clinic_token";
const USER_KEY = "dental_clinic_user";

export function saveSession(data: LoginResponse): void {
  localStorage.setItem(TOKEN_KEY, data.accessToken);
  localStorage.setItem(USER_KEY, JSON.stringify(data.user));
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function getToken(): string | null {
  if (typeof globalThis.window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function getUser(): AuthUser | null {
  if (typeof globalThis.window === "undefined") return null;
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function authHeaders(): HeadersInit {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

function handleUnauthorized() {
  clearSession();
  if (typeof globalThis.window !== "undefined") {
    sessionStorage.setItem("sessionExpired", "1");
    globalThis.window.location.href = "/auth/login";
  }
}

async function checkAuth(res: Response): Promise<void> {
  if (res.status === 401) {
    handleUnauthorized();
    throw new Error("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
  }
}

export async function getAccountsApi(): Promise<AccountDto[]> {
  const res = await fetch(`${API_URL}/api/auth/accounts`, {
    headers: { "Content-Type": "application/json", ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách tài khoản");
  }
  return res.json() as Promise<AccountDto[]>;
}

// ── Staff types & endpoints ──────────────────────────────────────────────────

export interface StaffDto {
  id: string;
  username: string;
  email: string;
  role: string;
  fullName: string | null;
  phoneNumber: string | null;
  isActive: boolean;
  employeeId: string | null;
  department: string | null;
  employmentStatus: string | null;
  profilePictureUrl: string | null;
  professionalNotes: string | null;
  createdAt: string;
  specialty: string | null;
  licenseNumber: string | null;
  yearsOfExperience: number | null;
  hasAccount: boolean;
  gender: string | null;
  dateOfBirth: string | null;
  address: string | null;
  startDate: string | null;
  servicesHandled: string | null;
  certificateIssuedDate: string | null;
  certificateIssuedBy: string | null;
  education: string | null;
  bio: string | null;
  position: string | null;
}


export interface StaffStatsDto {
  totalDentists: number;
  totalEmployees: number;
  totalDoctors: number;
}

export interface StaffListResponse {
  items: StaffDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  statistics: StaffStatsDto;
}

export interface CreateStaffCommand {
  fullName: string;
  email: string;
  phoneNumber: string;
  role: string;
  employeeId?: string | null;
  department?: string | null;
  employmentStatus?: string | null;
  profilePictureUrl?: string | null;
  professionalNotes?: string | null;
  specialty?: string | null;
  licenseNumber?: string | null;
  yearsOfExperience?: number | null;
  gender?: string | null;
  dateOfBirth?: string | null;
  address?: string | null;
  startDate?: string | null;
  servicesHandled?: string | null;
  certificateIssuedDate?: string | null;
  certificateIssuedBy?: string | null;
  education?: string | null;
  bio?: string | null;
  position?: string | null;
}

export interface UpdateStaffCommand {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: string;
  department: string | null;
  employmentStatus: string | null;
  profilePictureUrl: string | null;
  professionalNotes: string | null;
  isActive: boolean;
  specialty: string | null;
  licenseNumber: string | null;
  yearsOfExperience: number | null;
  gender?: string | null;
  dateOfBirth?: string | null;
  address?: string | null;
  startDate?: string | null;
  servicesHandled?: string | null;
  certificateIssuedDate?: string | null;
  certificateIssuedBy?: string | null;
  education?: string | null;
  bio?: string | null;
  position?: string | null;
}

export interface ResetPasswordResponse {
  id: string;
  email: string;
  temporaryPassword: string;
}

export async function getStaffApi(params?: {
  search?: string;
  role?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}): Promise<StaffListResponse> {
  const qs = new URLSearchParams();
  if (params?.search)   qs.set("search",   params.search);
  if (params?.role)     qs.set("role",     params.role);
  if (params?.status)   qs.set("status",   params.status);
  if (params?.page)     qs.set("page",     String(params.page));
  if (params?.pageSize) qs.set("pageSize", String(params.pageSize));
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/staff${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách nhân viên");
  }
  return res.json() as Promise<StaffListResponse>;
}

export async function createStaffApi(data: CreateStaffCommand): Promise<void> {
  const res = await fetch(`${API_URL}/api/staff`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo nhân viên thất bại");
  }
}

export async function updateStaffApi(id: string, data: UpdateStaffCommand): Promise<StaffDto> {
  const res = await fetch(`${API_URL}/api/staff/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật nhân viên thất bại");
  }
  return res.json() as Promise<StaffDto>;
}

export async function resetStaffPasswordApi(id: string): Promise<ResetPasswordResponse> {
  const res = await fetch(`${API_URL}/api/staff/${id}/reset-password`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đặt lại mật khẩu thất bại");
  }
  return res.json() as Promise<ResetPasswordResponse>;
}

export async function createStaffAccountApi(id: string): Promise<StaffDto> {
  const res = await fetch(`${API_URL}/api/staff/${id}/create-account`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo tài khoản thất bại");
  }
  return res.json() as Promise<StaffDto>;
}


// ── Service types ──────────────────────────────────────────────────────────

export interface ServiceDto {
  id: string;
  name: string;
  price: number;
  durationMinutes: number;
  isActive: boolean;
  description: string;
  viewCount: number;
  imageUrl: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateServiceRequest {
  name: string;
  price: number;
  durationMinutes: number;
  description: string;
  imageUrl?: string | null;
}

export interface UpdateServiceRequest {
  name: string;
  price: number;
  durationMinutes: number;
  description: string;
  imageUrl?: string | null;
}

// ── Service endpoints ──────────────────────────────────────────────────────

export async function getServicesApi(params?: {
  status?: string;
  search?: string;
}): Promise<ServiceDto[]> {
  const qs = new URLSearchParams();
  if (params?.status) qs.set("status", params.status);
  if (params?.search) qs.set("search", params.search);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/services${query}`);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách dịch vụ");
  }
  return res.json() as Promise<ServiceDto[]>;
}

export async function getServiceByIdApi(id: string): Promise<ServiceDto> {
  const res = await fetch(`${API_URL}/api/services/${id}`);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không tìm thấy dịch vụ");
  }
  return res.json() as Promise<ServiceDto>;
}

export async function createServiceApi(data: CreateServiceRequest): Promise<ServiceDto> {
  const res = await fetch(`${API_URL}/api/services`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo dịch vụ thất bại");
  }
  return res.json() as Promise<ServiceDto>;
}

export async function updateServiceApi(id: string, data: UpdateServiceRequest): Promise<ServiceDto> {
  const res = await fetch(`${API_URL}/api/services/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật dịch vụ thất bại");
  }
  return res.json() as Promise<ServiceDto>;
}

export async function deleteServiceApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/services/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xóa dịch vụ thất bại");
  }
}

// ── Post types ─────────────────────────────────────────────────────────────

export interface PostDto {
  id: string;
  title: string;
  category: string;
  author: string;
  content: string;
  thumbnailUrl: string | null;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string | null;
  publishedAt: string | null;
}

export interface CreatePostRequest {
  title: string;
  category: string;
  author: string;
  content: string;
  thumbnailUrl?: string | null;
  isPublished: boolean;
}

export interface UpdatePostRequest {
  title: string;
  category: string;
  content: string;
  thumbnailUrl?: string | null;
  isPublished: boolean;
}

// ── Post endpoints ─────────────────────────────────────────────────────────

export async function getPostsApi(params?: {
  category?: string;
  status?: string;
  search?: string;
}): Promise<PostDto[]> {
  const qs = new URLSearchParams();
  if (params?.category) qs.set("category", params.category);
  if (params?.status) qs.set("status", params.status);
  if (params?.search) qs.set("search", params.search);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/posts${query}`);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách bài viết");
  }
  return res.json() as Promise<PostDto[]>;
}

export async function getPostByIdApi(id: string): Promise<PostDto> {
  const res = await fetch(`${API_URL}/api/posts/${id}`);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không tìm thấy bài viết");
  }
  return res.json() as Promise<PostDto>;
}

export async function createPostApi(data: CreatePostRequest): Promise<PostDto> {
  const res = await fetch(`${API_URL}/api/posts`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo bài viết thất bại");
  }
  return res.json() as Promise<PostDto>;
}

export async function updatePostApi(id: string, data: UpdatePostRequest): Promise<PostDto> {
  const res = await fetch(`${API_URL}/api/posts/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật bài viết thất bại");
  }
  return res.json() as Promise<PostDto>;
}

export async function deletePostApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/posts/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xóa bài viết thất bại");
  }
}

// ── Schedule types ─────────────────────────────────────────────────────────

export interface ScheduleEntryDto {
  id: string;
  date: string; // "YYYY-MM-DD"
  shift: "morning" | "afternoon";
  type: "dentist" | "staff";
  role: "dentist" | "assistant" | "staff";
  name: string;
  room: string;
  roomColor: string;
  isHoliday: boolean;
}

export interface SaveScheduleEntryRequest {
  date: string;
  shift: "morning" | "afternoon";
  type: "dentist" | "staff";
  role: "dentist" | "assistant" | "staff";
  name: string;
  room: string;
  roomColor: string;
  isHoliday: boolean;
}

// ── Schedule endpoints ─────────────────────────────────────────────────────

export async function getWeekScheduleApi(weekStart: string): Promise<ScheduleEntryDto[]> {
  const res = await fetch(`${API_URL}/api/schedules?weekStart=${weekStart}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch làm việc");
  }
  return res.json() as Promise<ScheduleEntryDto[]>;
}

export async function saveWeekScheduleApi(
  weekStart: string,
  entries: SaveScheduleEntryRequest[]
): Promise<ScheduleEntryDto[]> {
  const res = await fetch(`${API_URL}/api/schedules/week/${weekStart}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ entries }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Lưu lịch làm việc thất bại");
  }
  return res.json() as Promise<ScheduleEntryDto[]>;
}

export async function toggleServiceStatusApi(id: string): Promise<ServiceDto> {
  const res = await fetch(`${API_URL}/api/services/${id}/status`, {
    method: "PATCH",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật trạng thái thất bại");
  }
  return res.json() as Promise<ServiceDto>;
}

export async function uploadFileApi(file: File): Promise<{ url: string }> {
  const formData = new FormData();
  formData.append("file", file);
  const res = await fetch(`${API_URL}/api/files/upload`, {
    method: "POST",
    headers: { ...authHeaders() },
    body: formData,
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Upload file thất bại");
  }
  return res.json() as Promise<{ url: string }>;
}

// ── Promotion types ────────────────────────────────────────────────────────

export interface PromotionDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  discountType: "Percentage" | "Fixed";
  discountValue: number;
  serviceIds: string[];
  serviceNames: string[];
  startDate: string;
  endDate: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreatePromotionRequest {
  code: string;
  name: string;
  description?: string;
  discountType: "Percentage" | "Fixed";
  discountValue: number;
  serviceIds: string[];
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface UpdatePromotionRequest {
  code: string;
  name: string;
  description?: string;
  discountType: "Percentage" | "Fixed";
  discountValue: number;
  serviceIds: string[];
  startDate: string;
  endDate: string;
}

// ── Promotion endpoints ────────────────────────────────────────────────────

export async function getPromotionsApi(): Promise<PromotionDto[]> {
  const res = await fetch(`${API_URL}/api/promotions`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Khong the tai danh sach khuyen mai");
  }
  return res.json() as Promise<PromotionDto[]>;
}

export async function getPromotionByIdApi(id: string): Promise<PromotionDto> {
  const res = await fetch(`${API_URL}/api/promotions/${id}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Khong tim thay khuyen mai");
  }
  return res.json() as Promise<PromotionDto>;
}

export async function createPromotionApi(data: CreatePromotionRequest): Promise<{ id: string }> {
  const res = await fetch(`${API_URL}/api/promotions`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tao khuyen mai that bai");
  }
  return res.json() as Promise<{ id: string }>;
}

export async function updatePromotionApi(id: string, data: UpdatePromotionRequest): Promise<void> {
  const res = await fetch(`${API_URL}/api/promotions/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cap nhat khuyen mai that bai");
  }
}

export async function deletePromotionApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/promotions/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xoa khuyen mai that bai");
  }
}

export async function togglePromotionStatusApi(id: string): Promise<PromotionDto> {
  const res = await fetch(`${API_URL}/api/promotions/${id}/status`, {
    method: "PATCH",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cap nhat trang thai that bai");
  }
  return res.json() as Promise<PromotionDto>;
}

// ── Feedback types ─────────────────────────────────────────────────────────

export interface FeedbackDto {
  id: string;
  customerName: string;
  rating: number;
  comment: string;
  status: string;
  replyText: string | null;
  repliedAt: string | null;
  createdAt: string;
}

export interface CreateFeedbackRequest {
  customerName: string;
  rating: number;
  comment: string;
}

export interface ReplyFeedbackRequest {
  replyText: string;
}

// ── Feedback endpoints ─────────────────────────────────────────────────────

export async function getFeedbacksApi(params?: {
  status?: string;
  search?: string;
}): Promise<FeedbackDto[]> {
  const qs = new URLSearchParams();
  if (params?.status) qs.set("status", params.status);
  if (params?.search) qs.set("search", params.search);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/feedbacks${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách phản hồi");
  }
  return res.json() as Promise<FeedbackDto[]>;
}

export async function featureFeedbackApi(id: string): Promise<FeedbackDto> {
  const res = await fetch(`${API_URL}/api/feedbacks/${id}/feature`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đánh dấu nổi bật thất bại");
  }
  return res.json() as Promise<FeedbackDto>;
}

export async function hideFeedbackApi(id: string): Promise<FeedbackDto> {
  const res = await fetch(`${API_URL}/api/feedbacks/${id}/hide`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Ẩn phản hồi thất bại");
  }
  return res.json() as Promise<FeedbackDto>;
}

export async function replyFeedbackApi(id: string, data: ReplyFeedbackRequest): Promise<FeedbackDto> {
  const res = await fetch(`${API_URL}/api/feedbacks/${id}/reply`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Trả lời phản hồi thất bại");
  }
  return res.json() as Promise<FeedbackDto>;
}

export async function createFeedbackApi(data: CreateFeedbackRequest): Promise<FeedbackDto> {
  const res = await fetch(`${API_URL}/api/feedbacks`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Gửi phản hồi thất bại");
  }
  return res.json() as Promise<FeedbackDto>;
}

// ── Leave Request types & endpoints ───────────────────────────────────────────

export interface LeaveRequestDto {
  id: string;
  userId: string;
  userFullName: string;
  department: string | null;
  leaveType: string;
  startDate: string;   // "YYYY-MM-DD"
  endDate: string;     // "YYYY-MM-DD"
  daysCount: number;
  reason: string;
  status: string;
  reviewerNote: string | null;
  createdAt: string;
  reviewedAt: string | null;
}

export interface MyLeaveStatsDto {
  totalAnnualDays: number;
  usedAnnualDays: number;
  remainingAnnualDays: number;
  pendingCount: number;
  approvedThisYear: number;
}

export interface MyLeaveRequestsResponse {
  stats: MyLeaveStatsDto;
  requests: LeaveRequestDto[];
}

export interface CreateLeaveRequestRequest {
  leaveType: string;
  startDate: string; // "YYYY-MM-DD"
  endDate: string;   // "YYYY-MM-DD"
  reason: string;
}

export async function getMyLeaveRequestsApi(): Promise<MyLeaveRequestsResponse> {
  const res = await fetch(`${API_URL}/api/leave-requests/my`, {
    headers: { "Content-Type": "application/json", ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách đơn nghỉ");
  }
  return res.json() as Promise<MyLeaveRequestsResponse>;
}

export async function createLeaveRequestApi(data: CreateLeaveRequestRequest): Promise<LeaveRequestDto> {
  const res = await fetch(`${API_URL}/api/leave-requests`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Gửi đơn xin nghỉ thất bại");
  }
  return res.json() as Promise<LeaveRequestDto>;
}

// ── Admin leave request endpoints ─────────────────────────────────────────────

export async function getLeaveRequestByIdApi(id: string): Promise<LeaveRequestDto> {
  const res = await fetch(`${API_URL}/api/leave-requests/${id}`, {
    headers: authHeaders(),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không tìm thấy đơn nghỉ");
  }
  return res.json() as Promise<LeaveRequestDto>;
}

export async function getLeaveRequestsAdminApi(status?: string, search?: string): Promise<LeaveRequestDto[]> {
  const params = new URLSearchParams();
  if (status && status !== "All") params.set("status", status);
  if (search) params.set("search", search);
  const query = params.toString() ? `?${params}` : "";
  const res = await fetch(`${API_URL}/api/leave-requests${query}`, {
    headers: authHeaders(),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách đơn nghỉ");
  }
  return res.json() as Promise<LeaveRequestDto[]>;
}

export async function approveLeaveRequestApi(id: string): Promise<LeaveRequestDto> {
  const res = await fetch(`${API_URL}/api/leave-requests/${id}/approve`, {
    method: "PUT",
    headers: authHeaders(),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể duyệt đơn nghỉ");
  }
  return res.json() as Promise<LeaveRequestDto>;
}

export async function rejectLeaveRequestApi(id: string, reviewerNote?: string): Promise<LeaveRequestDto> {
  const res = await fetch(`${API_URL}/api/leave-requests/${id}/reject`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ reviewerNote: reviewerNote ?? null }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể từ chối đơn nghỉ");
  }
  return res.json() as Promise<LeaveRequestDto>;
}
