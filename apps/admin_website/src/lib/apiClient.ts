const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5239";

export class ApiValidationError extends Error {
  errors: Record<string, string[]>;
  constructor(message: string, errors: Record<string, string[]>) {
    super(message);
    this.name = "ApiValidationError";
    this.errors = errors;
  }
}

// ── Types ──────────────────────────────────────────────────────────────────
export interface AuthUser {
  id: string;
  email: string;
  username: string;
  fullName: string | null;
  role: string;
  isActive: boolean;
  profilePictureUrl: string | null;
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

export async function forgotPasswordApi(email: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/auth/forgot-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Gửi yêu cầu thất bại");
  }
}

export async function resetPasswordApi(email: string, token: string, newPassword: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/auth/reset-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, token, newPassword }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đặt lại mật khẩu thất bại");
  }
}

export async function loginApi(email: string, password: string): Promise<LoginResponse> {
  const res = await fetch(`${API_URL}/api/auth/staff/login`, {
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
const REMEMBER_EMAIL_KEY = "dental_clinic_remember_email";

export function saveSession(data: LoginResponse, rememberMe: boolean): void {
  const storage = rememberMe ? localStorage : sessionStorage;
  storage.setItem(TOKEN_KEY, data.accessToken);
  storage.setItem(USER_KEY, JSON.stringify(data.user));
  // Clean up the other storage to avoid stale tokens
  const other = rememberMe ? sessionStorage : localStorage;
  other.removeItem(TOKEN_KEY);
  other.removeItem(USER_KEY);
}

const REMEMBER_PASSWORD_KEY = "dental_clinic_remember_password";

export function saveRememberCredentials(email: string, password: string): void {
  localStorage.setItem(REMEMBER_EMAIL_KEY, email);
  localStorage.setItem(REMEMBER_PASSWORD_KEY, password);
}

export function clearRememberCredentials(): void {
  localStorage.removeItem(REMEMBER_EMAIL_KEY);
  localStorage.removeItem(REMEMBER_PASSWORD_KEY);
}

export function getRememberedCredentials(): { email: string; password: string } | null {
  if (typeof globalThis.window === "undefined") return null;
  const email = localStorage.getItem(REMEMBER_EMAIL_KEY);
  const password = localStorage.getItem(REMEMBER_PASSWORD_KEY);
  if (!email || !password) return null;
  return { email, password };
}

/** @deprecated use getRememberedCredentials */
export function getRememberedEmail(): string | null {
  if (typeof globalThis.window === "undefined") return null;
  return localStorage.getItem(REMEMBER_EMAIL_KEY);
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(USER_KEY);
}

export function getToken(): string | null {
  if (typeof globalThis.window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY) ?? sessionStorage.getItem(TOKEN_KEY);
}

export function getUser(): AuthUser | null {
  if (typeof globalThis.window === "undefined") return null;
  const raw = localStorage.getItem(USER_KEY) ?? sessionStorage.getItem(USER_KEY);
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

export async function toggleAccountStatusApi(id: string): Promise<AccountDto> {
  const res = await fetch(`${API_URL}/api/auth/accounts/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật trạng thái tài khoản");
  }
  return res.json() as Promise<AccountDto>;
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
  employmentType: string | null;
  baseSalary: number | null;
  salaryUnit: string | null;
  leaveAccrued: number | null;
  allowance: number | null;
  /** Id hồ sơ nha sĩ — null nếu nhân viên không phải bác sĩ. Đánh giá của bệnh nhân khóa theo id này. */
  dentistProfileId: string | null;
  /** Bài viết chi tiết (HTML) giới thiệu về nha sĩ — null nếu không phải bác sĩ. */
  content: string | null;
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
  employmentType?: string | null;
  baseSalary?: number | null;
  salaryUnit?: string | null;
  leaveAccrued?: number | null;
  allowance?: number | null;
  content?: string | null;
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
  employmentType?: string | null;
  baseSalary?: number | null;
  salaryUnit?: string | null;
  leaveAccrued?: number | null;
  allowance?: number | null;
  content?: string | null;
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
  /** Chuyên khoa (nha sĩ) hoặc chức vụ / bộ phận (nhân viên) */
  specialty?: string;
  page?: number;
  pageSize?: number;
  /** "name" | "department" | "status" — mặc định "name" */
  sortBy?: string;
  sortDir?: "asc" | "desc";
}): Promise<StaffListResponse> {
  const qs = new URLSearchParams();
  if (params?.search)    qs.set("search",    params.search);
  if (params?.role)      qs.set("role",      params.role);
  if (params?.status)    qs.set("status",    params.status);
  if (params?.specialty) qs.set("specialty", params.specialty);
  if (params?.page)     qs.set("page",     String(params.page));
  if (params?.pageSize) qs.set("pageSize", String(params.pageSize));
  if (params?.sortBy)   qs.set("sortBy",   params.sortBy);
  if (params?.sortDir)  qs.set("sortDir",  params.sortDir);
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

export async function getStaffByIdApi(id: string): Promise<StaffDto> {
  const res = await fetch(`${API_URL}/api/staff/${id}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải chi tiết nhân viên");
  }
  return res.json() as Promise<StaffDto>;
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
    if (res.status === 422 && err.errors) {
      throw new ApiValidationError(err.title ?? "Dữ liệu không hợp lệ", err.errors);
    }
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
    if (res.status === 422 && err.errors) {
      throw new ApiValidationError(err.title ?? "Dữ liệu không hợp lệ", err.errors);
    }
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

export interface ServiceOptionDto {
  id: string;
  name: string;
  price: number;
  unit: string;
  sortOrder: number;
}

export interface ServiceDto {
  id: string;
  name: string;
  price: number;
  durationMinutes: number;
  isActive: boolean;
  description: string;
  content: string;
  viewCount: number;
  imageUrl: string | null;
  iconUrl: string | null;
  createdAt: string;
  updatedAt: string | null;
  options: ServiceOptionDto[];
}

export interface ServiceOptionRequest {
  name: string;
  price: number;
  unit?: string;
  sortOrder: number;
}

export interface CreateServiceRequest {
  name: string;
  price: number;
  durationMinutes: number;
  description: string;
  content?: string;
  imageUrl?: string | null;
  iconUrl?: string | null;
  options?: ServiceOptionRequest[];
}

export interface UpdateServiceRequest {
  name: string;
  price: number;
  durationMinutes: number;
  description: string;
  content?: string;
  imageUrl?: string | null;
  iconUrl?: string | null;
  options?: ServiceOptionRequest[];
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
  shift: string; // mã ca, ví dụ "08:00-10:00" (xem lib/shifts.ts). Dữ liệu cũ: "morning"/"afternoon"
  type: "dentist" | "staff";
  role: "dentist" | "assistant" | "staff";
  name: string;
  room: string;
  roomColor: string;
  isHoliday: boolean;
}

export interface SaveScheduleEntryRequest {
  date: string;
  shift: string; // mã ca, ví dụ "08:00-10:00"
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

// Lịch làm việc của chính nha sĩ đang đăng nhập (chỉ xem)
export async function getMyScheduleApi(weekStart: string): Promise<ScheduleEntryDto[]> {
  const res = await fetch(`${API_URL}/api/schedules/my?weekStart=${weekStart}`, {
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
  const data = await res.json() as { url: string };
  return { url: data.url };
}

/**
 * File upload trả về path tương đối (ví dụ "/uploads/xxx.svg") — cố ý không ghép sẵn API_URL của
 * máy này vào lúc lưu, vì URL đó sẽ bị lưu thẳng vào DB và không load được từ máy khác (app di động,
 * máy dev khác...). Dùng hàm này ở nơi HIỂN THỊ (src={resolveAssetUrl(url)}) để ghép đúng base URL của
 * chính máy đang xem tại thời điểm render.
 */
export function resolveAssetUrl(url?: string | null): string | undefined {
  if (!url) return undefined;
  return url.startsWith("/") ? `${API_URL}${url}` : url;
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

export interface FeedbackReplyDraftDto {
  replyText: string;
}

/** Soạn NHÁP câu trả lời bằng AI cho một đánh giá — chỉ điền sẵn vào ô trả lời, không tự gửi. */
export async function generateFeedbackReplyApi(id: string): Promise<FeedbackReplyDraftDto> {
  const res = await fetch(`${API_URL}/api/feedbacks/${id}/ai-draft-reply`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể soạn nháp bằng AI");
  }
  return res.json() as Promise<FeedbackReplyDraftDto>;
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

// ── Medicine types ──────────────────────────────────────────────────────────

export interface MedicineDto {
  id: string;
  name: string;
  genericName: string;
  manufacturer: string;
  unit: string;
  description: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateMedicineRequest {
  name: string;
  genericName: string;
  manufacturer: string;
  unit: string;
  description: string;
}

export interface UpdateMedicineRequest {
  name: string;
  genericName: string;
  manufacturer: string;
  unit: string;
  description: string;
}

// ── Medicine endpoints ──────────────────────────────────────────────────────

export async function getMedicinesApi(params?: {
  status?: string;
  search?: string;
}): Promise<MedicineDto[]> {
  const qs = new URLSearchParams();
  if (params?.status) qs.set("status", params.status);
  if (params?.search) qs.set("search", params.search);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/medicines${query}`);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách thuốc");
  }
  return res.json() as Promise<MedicineDto[]>;
}

export async function getMedicineByIdApi(id: string): Promise<MedicineDto> {
  const res = await fetch(`${API_URL}/api/medicines/${id}`);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không tìm thấy thuốc");
  }
  return res.json() as Promise<MedicineDto>;
}

export async function createMedicineApi(data: CreateMedicineRequest): Promise<MedicineDto> {
  const res = await fetch(`${API_URL}/api/medicines`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Thêm thuốc thất bại");
  }
  return res.json() as Promise<MedicineDto>;
}

export async function updateMedicineApi(id: string, data: UpdateMedicineRequest): Promise<MedicineDto> {
  const res = await fetch(`${API_URL}/api/medicines/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật thuốc thất bại");
  }
  return res.json() as Promise<MedicineDto>;
}

export async function deleteMedicineApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/medicines/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xóa thuốc thất bại");
  }
}

export async function toggleMedicineStatusApi(id: string): Promise<MedicineDto> {
  const res = await fetch(`${API_URL}/api/medicines/${id}/status`, {
    method: "PATCH",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật trạng thái thất bại");
  }
  return res.json() as Promise<MedicineDto>;
}

// ── Inventory types ────────────────────────────────────────────────────────

export interface SupplyItemDto {
  id: string;
  code: string;
  name: string;
  category: string;
  unit: string;
  quantity: number;
  minQuantity: number;
  isLow: boolean;
  orderType: "standard" | "custom";
  price: number | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface SupplyTransactionDto {
  id: string;
  supplyItemId: string;
  itemName: string;
  type: "import" | "export";
  quantity: number;
  unitPrice: number | null;
  note: string | null;
  createdBy: string;
  createdAt: string;
  /** Phòng nhận hàng nếu đây là 1 lần xuất theo phòng — null với các giao dịch khác (nhập kho, tiêu hao
   * điều trị, yêu cầu vật tư...). */
  roomName: string | null;
}

export interface CreateSupplyItemRequest {
  code: string;
  name: string;
  category: string;
  unit: string;
  quantity: number;
  minQuantity: number;
  price?: number;
}

export interface UpdateSupplyItemRequest {
  name: string;
  category: string;
  unit: string;
  minQuantity: number;
  price?: number | null;
}

export interface CreateSupplyTransactionRequest {
  supplyItemId: string;
  type: "import" | "export";
  quantity: number;
  note?: string;
  /** Phòng nhận hàng — chỉ áp dụng khi type = "export" (xuất theo phòng). */
  roomId?: string;
}

// ── Inventory endpoints ────────────────────────────────────────────────────

export async function createSupplyItemApi(data: CreateSupplyItemRequest): Promise<SupplyItemDto> {
  const res = await fetch(`${API_URL}/api/inventory/items`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Thêm vật tư thất bại");
  }
  return res.json() as Promise<SupplyItemDto>;
}

export async function updateSupplyItemApi(id: string, data: UpdateSupplyItemRequest): Promise<SupplyItemDto> {
  const res = await fetch(`${API_URL}/api/inventory/items/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật vật tư thất bại");
  }
  return res.json() as Promise<SupplyItemDto>;
}

export async function deleteSupplyItemApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/inventory/items/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xóa vật tư thất bại");
  }
}

export async function getSupplyItemsApi(params?: {
  search?: string;
  category?: string;
  orderType?: string;
}): Promise<SupplyItemDto[]> {
  const qs = new URLSearchParams();
  if (params?.search) qs.set("search", params.search);
  if (params?.category) qs.set("category", params.category);
  if (params?.orderType) qs.set("orderType", params.orderType);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/inventory/items${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách vật tư");
  }
  return res.json() as Promise<SupplyItemDto[]>;
}

export async function getSupplyTransactionsApi(roomId?: string): Promise<SupplyTransactionDto[]> {
  const qs = roomId ? `?roomId=${encodeURIComponent(roomId)}` : "";
  const res = await fetch(`${API_URL}/api/inventory/transactions${qs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch sử giao dịch");
  }
  return res.json() as Promise<SupplyTransactionDto[]>;
}

export async function createSupplyTransactionApi(
  data: CreateSupplyTransactionRequest
): Promise<SupplyTransactionDto> {
  const res = await fetch(`${API_URL}/api/inventory/transactions`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo giao dịch thất bại");
  }
  return res.json() as Promise<SupplyTransactionDto>;
}

export interface StockImportRequest {
  name: string;
  unit: string;
  category: string;
  quantity: number;
  note?: string;
  /** Bắt buộc — nhập kho không có giá sẽ bị bỏ sót khỏi báo cáo chi phí vật tư. */
  unitPrice: number;
}

export async function stockImportApi(data: StockImportRequest): Promise<SupplyTransactionDto> {
  const res = await fetch(`${API_URL}/api/inventory/stock-import`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Nhập kho thất bại");
  }
  return res.json() as Promise<SupplyTransactionDto>;
}

// ── Material requests (yêu cầu vật tư từ bác sĩ) ─────────────────────────────

export interface MaterialRequestItemDto {
  id: string;
  itemName: string;
  /** Chi tiết riêng của lần đặt này (răng số mấy, hàm nào, kích thước...) — chỉ để đọc, không dùng để
   * khớp vật tư trong kho (itemName phải giữ chung/generic để cộng dồn đúng 1 mã hàng trong kho). */
  detail: string | null;
  quantity: number;
  unit: string;
  /** Số lượng thực nhận lúc nhập kho — null nếu chưa xử lý xong (Pending/Ordered). */
  actualQuantity: number | null;
}

export interface MaterialRequestDto {
  id: string;
  /** Dịch vụ cụ thể trong liệu trình mà yêu cầu này phục vụ — null nếu tạo tự do (không gắn dịch vụ nào). */
  treatmentPlanId: string | null;
  /** Buổi hẹn đã sinh ra yêu cầu này — dùng để tải ảnh đính kèm (dấu răng, răng lợi...) qua
   * getAppointmentPhotosApi(appointmentId, "material-request"). Null nếu staff tự khởi tạo. */
  appointmentId: string | null;
  courseName: string;
  patientName: string;
  dentistName: string;
  items: MaterialRequestItemDto[];
  status: string;        // "Pending" | "Ordered" | "Done"
  createdAt: string;
  orderedAt: string | null;
  orderedBy: string | null;
  supplierNote: string | null;
  handledAt: string | null;
  handledBy: string | null;
}

export async function getMaterialRequestsApi(status?: string): Promise<MaterialRequestDto[]> {
  const qs = status ? `?status=${encodeURIComponent(status)}` : "";
  const res = await fetch(`${API_URL}/api/inventory/material-requests${qs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải yêu cầu vật tư");
  }
  return res.json() as Promise<MaterialRequestDto[]>;
}

export async function markMaterialRequestDoneApi(
  id: string,
  itemPrices: { materialRequestItemId: string; unitPrice: number; actualQuantity?: number }[]
): Promise<void> {
  const res = await fetch(`${API_URL}/api/inventory/material-requests/${id}/done`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ itemPrices }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật yêu cầu vật tư thất bại");
  }
}

/** Staff đánh dấu đã đặt hàng nhà cung cấp/lab — chưa nhập kho, chỉ chuyển trạng thái sang "Đã đặt hàng". */
export async function markMaterialRequestOrderedApi(id: string, supplierNote?: string): Promise<MaterialRequestDto> {
  const res = await fetch(`${API_URL}/api/inventory/material-requests/${id}/ordered`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ supplierNote: supplierNote || null }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể đánh dấu đã đặt hàng");
  }
  return res.json() as Promise<MaterialRequestDto>;
}

/** Staff tự khởi tạo yêu cầu đặt vật tư riêng cho bệnh nhân (không cần đi qua buổi khám của bác sĩ). */
export async function createMaterialRequestByStaffApi(request: {
  patientId: string;
  patientName: string;
  description: string;
  items: { itemName: string; quantity: number; unit: string; detail?: string | null }[];
}): Promise<MaterialRequestDto> {
  const res = await fetch(`${API_URL}/api/inventory/material-requests/staff`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo yêu cầu vật tư thất bại");
  }
  return res.json() as Promise<MaterialRequestDto>;
}

// ── Yêu cầu vật tư (bác sĩ tạo từ buổi khám) ─────────────────────────────────
export interface CreateMaterialRequestRequest {
  appointmentId: string;
  items: { itemName: string; quantity: number; unit: string; detail?: string | null }[];
  /** Dịch vụ cụ thể trong liệu trình mà yêu cầu này phục vụ — bỏ trống nếu tạo tự do. */
  treatmentPlanId?: string | null;
}

export async function createMaterialRequestApi(request: CreateMaterialRequestRequest): Promise<MaterialRequestDto> {
  const res = await fetch(`${API_URL}/api/inventory/material-requests`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Gửi yêu cầu vật tư thất bại");
  }
  return res.json() as Promise<MaterialRequestDto>;
}

export async function getMaterialRequestsByPatientApi(patientId: string, patientName?: string): Promise<MaterialRequestDto[]> {
  const nameQs = patientName ? `&name=${encodeURIComponent(patientName)}` : "";
  const res = await fetch(`${API_URL}/api/inventory/material-requests/by-patient?patientId=${encodeURIComponent(patientId)}${nameQs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải yêu cầu vật tư");
  }
  return res.json() as Promise<MaterialRequestDto[]>;
}

// ── Room types ─────────────────────────────────────────────────────────────

export interface RoomDto {
  id: string;
  code: string;
  name: string;
  floor: string;
  type: string;
  status: string;
  activeStatus: string;
  description: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateRoomRequest {
  code: string;
  name: string;
  floor: string;
  type: string;
  description: string;
}

export interface UpdateRoomRequest {
  code: string;
  name: string;
  floor: string;
  type: string;
  description: string;
}

// ── Room endpoints ─────────────────────────────────────────────────────────

export async function getRoomsApi(params?: {
  floor?: string;
  status?: string;
  search?: string;
}): Promise<RoomDto[]> {
  const qs = new URLSearchParams();
  if (params?.floor) qs.set("floor", params.floor);
  if (params?.status) qs.set("status", params.status);
  if (params?.search) qs.set("search", params.search);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/rooms${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách phòng");
  }
  return res.json() as Promise<RoomDto[]>;
}

export async function getRoomByIdApi(id: string): Promise<RoomDto> {
  const res = await fetch(`${API_URL}/api/rooms/${id}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không tìm thấy phòng");
  }
  return res.json() as Promise<RoomDto>;
}

export async function createRoomApi(data: CreateRoomRequest): Promise<RoomDto> {
  const res = await fetch(`${API_URL}/api/rooms`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Tạo phòng thất bại");
  }
  return res.json() as Promise<RoomDto>;
}

export async function updateRoomApi(id: string, data: UpdateRoomRequest): Promise<RoomDto> {
  const res = await fetch(`${API_URL}/api/rooms/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật phòng thất bại");
  }
  return res.json() as Promise<RoomDto>;
}

export async function deleteRoomApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/rooms/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xóa phòng thất bại");
  }
}

export async function changeRoomStatusApi(id: string, status: string): Promise<RoomDto> {
  const res = await fetch(`${API_URL}/api/rooms/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ status }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật trạng thái phòng thất bại");
  }
  return res.json() as Promise<RoomDto>;
}
// ── Leave Request types & endpoints ───────────────────────────────────────────

export interface LeaveRequestShiftDto {
  date: string;    // "YYYY-MM-DD"
  shiftId: string; // mã ca, ví dụ "08:00-10:00" (xem lib/shifts.ts)
}

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
  shifts: LeaveRequestShiftDto[];
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
  shifts: LeaveRequestShiftDto[];
  reason: string;
}

// Ảnh hưởng của đơn nghỉ: ca làm việc đã xếp bị trùng khoảng nghỉ (duyệt đơn = gỡ hết các ca này)
// và lịch hẹn đã đặt trong những ngày đó (KHÔNG tự hủy — chỉ cảnh báo để Owner tự xử lý).
export interface LeaveImpactShiftDto {
  scheduleId: string;
  shift: string; // mã ca, ví dụ "08:00-10:00" (xem lib/shifts.ts)
  room: string;
  role: "dentist" | "assistant" | "staff";
  type: "dentist" | "staff";
}

export interface LeaveImpactDayDto {
  date: string; // "YYYY-MM-DD"
  shifts: LeaveImpactShiftDto[];
  appointmentCount: number;
  appointmentTimes: string[]; // "HH:mm" giờ Việt Nam
}

export interface LeaveImpactDto {
  leaveRequestId: string;
  staffName: string;
  status: string;
  startDate: string;
  endDate: string;
  affectedDayCount: number;
  affectedShiftCount: number;
  affectedAppointmentCount: number;
  days: LeaveImpactDayDto[];
}

export interface ApproveLeaveRequestResult {
  request: LeaveRequestDto;
  removedShiftCount: number;
  affectedDayCount: number;
  affectedAppointmentCount: number;
  affectedDates: string[]; // "YYYY-MM-DD"
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

// Owner xem trước ảnh hưởng của đơn nghỉ trước khi bấm duyệt.
export async function getLeaveRequestImpactApi(id: string): Promise<LeaveImpactDto> {
  const res = await fetch(`${API_URL}/api/leave-requests/${id}/impact`, {
    headers: authHeaders(),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải ảnh hưởng của đơn nghỉ");
  }
  return res.json() as Promise<LeaveImpactDto>;
}

// Duyệt đơn: server gỡ luôn các ca trùng khỏi lịch làm việc và trả về số ca đã gỡ.
export async function approveLeaveRequestApi(id: string): Promise<ApproveLeaveRequestResult> {
  const res = await fetch(`${API_URL}/api/leave-requests/${id}/approve`, {
    method: "PUT",
    headers: authHeaders(),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể duyệt đơn nghỉ");
  }
  return res.json() as Promise<ApproveLeaveRequestResult>;
}

// ── Appointment types ──────────────────────────────────────────────────────────

export interface StaffAppointmentDto {
  appointmentId: string;
  appointmentCode: string;
  patientId: string;
  patientName: string;
  patientPhone: string | null;
  dentistName: string;
  serviceName: string | null;
  appointmentDate: string; // ISO8601
  createdAt: string;       // ISO8601
  status: string;          // "Pending" | "Confirmed" | "CheckedIn" | "InProgress" | "PendingPayment" | "Completed" | "Cancelled" | "NoShow"
  symptoms: string | null;
  checkedInAt: string | null; // ISO8601 — thời điểm check-in (null nếu chưa check-in)
  /** "Online" = bệnh nhân tự đặt trên app/web · "WalkIn" = lễ tân lập tại quầy. */
  origin: AppointmentOrigin;
  /** Quan hệ với chủ tài khoản (VD: "Con", "Vợ"...) — null nếu bệnh nhân tự đặt cho chính mình. */
  patientRelationship: string | null;
  /** Tên chủ tài khoản đã đặt lịch — chính bệnh nhân nếu tự đặt, hoặc người thân nếu đặt hộ. */
  accountHolderName: string;
  /** Email đăng nhập của chủ tài khoản đã đặt lịch. */
  accountHolderEmail: string | null;
}

/**
 * Nguồn của lịch hẹn. Quyết định chuyện gì xảy ra khi hoàn tác check-in: lịch đặt từ xa quay về
 * chờ xác nhận, còn lịch lập tại quầy bị hủy vì nó sinh ra ngay tại lúc check-in, không có
 * trạng thái nào trước đó để quay về.
 */
export type AppointmentOrigin = "Online" | "WalkIn";

// ── Appointment endpoints ──────────────────────────────────────────────────────

export async function getStaffAppointmentsApi(params?: {
  date?: string;   // "YYYY-MM-DD"
  status?: string;
}): Promise<StaffAppointmentDto[]> {
  const qs = new URLSearchParams();
  if (params?.date)   qs.set("date",   params.date);
  if (params?.status) qs.set("status", params.status);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/appointments${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách lịch hẹn");
  }
  return res.json() as Promise<StaffAppointmentDto[]>;
}

export interface AppointmentsPagedDto {
  items: StaffAppointmentDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export async function getAppointmentsPagedApi(params?: {
  startDate?: string; // "YYYY-MM-DD"
  endDate?: string;   // "YYYY-MM-DD"
  status?: string;    // 1 hoặc nhiều trạng thái, phân tách bởi dấu phẩy
  search?: string;
  page?: number;
  pageSize?: number;
  sortDir?: "asc" | "desc";
}): Promise<AppointmentsPagedDto> {
  const qs = new URLSearchParams();
  if (params?.startDate) qs.set("startDate", params.startDate);
  if (params?.endDate)   qs.set("endDate",   params.endDate);
  if (params?.status)    qs.set("status",    params.status);
  if (params?.search)    qs.set("search",    params.search);
  if (params?.page)      qs.set("page",      String(params.page));
  if (params?.pageSize)  qs.set("pageSize",  String(params.pageSize));
  if (params?.sortDir)   qs.set("sortDir",   params.sortDir);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/appointments/paged${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách ca khám");
  }
  return res.json() as Promise<AppointmentsPagedDto>;
}

export async function confirmAppointmentApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/confirm`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xác nhận lịch hẹn thất bại");
  }
}

// ── Appointment Change Request types & endpoints ────────────────────────────

export type AppointmentChangeType = "Cancel" | "Reschedule";
export type AppointmentChangeRequestStatus = "Pending" | "Approved" | "Rejected";

export interface AppointmentChangeRequestDto {
  id: string;
  appointmentId: string;
  appointmentCode: string;
  patientId: string;
  patientName: string;
  patientPhone: string | null;
  serviceName: string | null;
  currentAppointmentDate: string;
  currentDentistName: string;
  type: AppointmentChangeType;
  status: AppointmentChangeRequestStatus;
  reason: string;
  desiredDate: string | null;
  desiredTimeSlot: string | null;
  desiredDentistId: string | null;
  desiredDentistName: string | null;
  staffNote: string | null;
  createdAt: string;
  processedAt: string | null;
  processedByName: string | null;
}

export async function getStaffAppointmentChangeRequestsApi(params?: {
  status?: string;
  date?: string;
}): Promise<AppointmentChangeRequestDto[]> {
  const qs = new URLSearchParams();
  if (params?.status) qs.set("status", params.status);
  if (params?.date) qs.set("date", params.date);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/staff/appointment-change-requests${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách yêu cầu thay đổi lịch");
  }
  return res.json() as Promise<AppointmentChangeRequestDto[]>;
}

export async function approveAppointmentChangeRequestApi(id: string, staffNote?: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/staff/appointment-change-requests/${id}/approve`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ staffNote }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string; message?: string }).message ?? (err as { title?: string }).title ?? "Duyệt yêu cầu thất bại");
  }
}

export async function rejectAppointmentChangeRequestApi(id: string, staffNote: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/staff/appointment-change-requests/${id}/reject`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ staffNote }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string; message?: string }).message ?? (err as { title?: string }).title ?? "Từ chối yêu cầu thất bại");
  }
}

/** Một lựa chọn lý do hủy do backend cung cấp — client KHÔNG tự chép cứng danh sách này. */
export interface CancellationReasonOption {
  code: string;
  labelVi: string;
  labelEn: string;
  /** Bắt buộc nhập ghi chú trước khi cho gửi. */
  requiresNote: boolean;
  staffOnly: boolean;
}

/**
 * Danh sách lý do hủy. Lấy từ server thay vì hardcode để thêm/sửa lý do không phải build lại app,
 * và để admin với mobile không lệch nhau. Server đã lọc theo vai trò của người đang đăng nhập.
 */
export async function getCancellationReasonsApi(): Promise<CancellationReasonOption[]> {
  const res = await fetch(`${API_URL}/api/appointments/cancellation-reasons`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) throw new Error("Không tải được danh sách lý do hủy");
  return res.json();
}

export async function cancelAppointmentApi(
  id: string,
  reason: string,
  note?: string,
): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/cancel`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ reason, note: note?.trim() || null }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Hủy lịch hẹn thất bại");
  }
}

export interface RescheduleAppointmentResult {
  appointmentId: string;
  appointmentDate: string;
  dentistId: string;
  status: string;
  rescheduledCount: number;
}

/**
 * Dời lịch — sửa tại chỗ, giữ nguyên appointmentId và mã lịch hẹn.
 * Bỏ trống dentistId/serviceId nghĩa là giữ nguyên giá trị hiện tại.
 */
export async function rescheduleAppointmentApi(
  id: string,
  appointmentDate: string,
  options?: { dentistId?: string; serviceId?: string; reason?: string },
): Promise<RescheduleAppointmentResult> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/reschedule`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({
      appointmentDate,
      dentistId: options?.dentistId ?? null,
      serviceId: options?.serviceId ?? null,
      reason: options?.reason?.trim() || null,
    }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Dời lịch hẹn thất bại");
  }
  return res.json();
}

export async function checkInAppointmentApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/checkin`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Check-in thất bại");
  }
}

export interface UndoCheckInResult {
  appointmentId: string;
  /** Nguồn lịch hẹn TRƯỚC khi hoàn tác — dùng để nói đúng chuyện gì vừa xảy ra. */
  origin: AppointmentOrigin;
  /** "Confirmed" (quay về danh sách chờ check-in). */
  status: string;
}

/**
 * Gỡ một lần check-in bấm nhầm. Chỉ dùng được khi lịch còn ở trạng thái CheckedIn — bác sĩ đã bắt
 * đầu khám thì server từ chối, vì lúc đó buổi khám đã có thật.
 */
export async function undoCheckInAppointmentApi(id: string): Promise<UndoCheckInResult> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/undo-checkin`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Hoàn tác check-in thất bại");
  }
  return res.json() as Promise<UndoCheckInResult>;
}

export async function markNoShowAppointmentApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/no-show`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Ghi nhận vắng thất bại");
  }
}

/**
 * Gỡ một lần ghi nhận vắng mặt bấm nhầm. Chỉ dùng được khi lịch còn ở trạng thái NoShow — luôn
 * quay về Confirmed, chờ bệnh nhân đến check-in.
 */
export async function undoNoShowAppointmentApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/undo-noshow`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Hoàn tác vắng mặt thất bại");
  }
}

export async function startTreatmentApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/start`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Bắt đầu khám thất bại");
  }
}

export async function completeTreatmentApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/complete`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Hoàn thành khám thất bại");
  }
}

// ── Waiting Queue types ─────────────────────────────────────────────────────────

export interface QueuePatientDto {
  appointmentId: string;
  appointmentCode: string;
  patientName: string;
  patientPhone: string | null;
  serviceName: string | null;
  dentistName: string;
  appointmentDate: string;
  checkedInAt: string | null;
  queueNumber: number;
  status: string;
  symptoms: string | null;
  waitMinutes: number;
}

export interface QueueDentistDto {
  dentistId: string;
  dentistName: string;
  dentistColor: string;
  shifts: string[];
  isOnShiftNow: boolean;
  isOnShiftSoon: boolean;
}

export interface RoomQueueDto {
  roomName: string | null;
  dentists: QueueDentistDto[];
  patients: QueuePatientDto[];
}

export interface WaitingQueueResponse {
  date: string;
  totalWaiting: number;
  totalInProgress: number;
  totalCompleted: number;
  rooms: RoomQueueDto[];
}

export async function getWaitingQueueApi(date?: string): Promise<WaitingQueueResponse> {
  const params = new URLSearchParams();
  if (date) params.set("date", date);
  const query = params.toString() ? `?${params}` : "";
  const res = await fetch(`${API_URL}/api/appointments/queue${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải hàng đợi");
  }
  return res.json() as Promise<WaitingQueueResponse>;
}

/**
 * Chuyển bệnh nhân đang chờ sang hàng đợi của phòng khác. Phòng được suy ra từ bác sĩ
 * phụ trách, nên API sẽ giao lịch hẹn cho bác sĩ đang trong ca trực tại phòng đích.
 * Truyền `dentistId` khi lễ tân chọn rõ người khám (gần giờ giao ca giữa bác sĩ đang trực
 * và bác sĩ sắp vào ca); bỏ trống thì API tự giao cho bác sĩ đang trực.
 */
export async function transferQueuePatientApi(
  appointmentId: string, roomName: string, dentistId?: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/queue-room`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ roomName, dentistId }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể chuyển bệnh nhân sang phòng khác");
  }
}

/**
 * Đổi chỗ thứ tự của một bệnh nhân đang chờ với người liền kề trong cùng hàng đợi phòng
 * (nút đẩy lên / đẩy xuống một bậc). Chỉ hoán vị trí, không đổi thời gian chờ.
 */
export async function reorderQueuePatientApi(
  appointmentId: string, swapWithAppointmentId: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/queue-order`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ swapWithAppointmentId }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể đổi thứ tự hàng đợi");
  }
}

export interface DentistPatientDto {
  appointmentId: string;
  appointmentCode: string;
  patientName: string;
  age: number;
  gender: string;
  phone: string | null;
  appointmentDate: string;
  status: string;
  serviceName: string | null;
  symptoms: string | null;
  isNew: boolean;
  isFollowUpVisit: boolean; // Buổi hẹn do staff check-in từ tab Tái khám
  queueNumber?: number;
  checkedInAt?: string | null;
  waitMinutes?: number;
}

export interface DentistPatientsResponse {
  date: string;
  totalWaiting: number;
  totalInProgress: number;
  totalCompleted: number;
  patients: DentistPatientDto[];
}

// ── Dentist Dashboard ──────────────────────────────────────────────────────

export interface DentistShiftInfo {
  label: string;        // "08:00 – 10:00"
  period: string;       // "Buổi sáng" | "Buổi chiều" | "Buổi tối"
  room: string | null;
}

export interface DentistWeekShifts {
  total: number;
  morning: number;
  afternoon: number;
  evening: number;
}

export interface DentistDashboardPatientDto {
  appointmentId: string;
  patientName: string;
  serviceName: string | null;
  time: string;
  status: string;
}

export interface DentistDashboardResponse {
  date: string;
  totalPatientsToday: number;
  totalWaiting: number;
  totalInProgress: number;
  totalCompleted: number;
  weekShifts: DentistWeekShifts;
  todayShifts: DentistShiftInfo[];
  upcomingPatients: DentistDashboardPatientDto[];
}

// ── Staff Walk-in Schedule ─────────────────────────────────────────────────

export interface StaffScheduleSlot {
  time: string;
  isBooked: boolean;
  patientName: string | null;
  isPast: boolean;
}

export interface StaffScheduleDentistDto {
  dentistId: string;
  name: string;
  room: string;
  slots: StaffScheduleSlot[]; // các khung giờ bác sĩ có ca hôm nay (đã lọc theo ca được phân)
}

export interface StaffScheduleResponse {
  date: string;
  dentists: StaffScheduleDentistDto[];
}

export async function getStaffScheduleApi(date?: string): Promise<StaffScheduleResponse> {
  const params = new URLSearchParams();
  if (date) params.set("date", date);
  const query = params.toString() ? `?${params}` : "";
  const res = await fetch(`${API_URL}/api/appointments/staff/schedule${query}`, {
    headers: { ...authHeaders() },
    cache: "no-store",
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch trống");
  }
  return res.json() as Promise<StaffScheduleResponse>;
}

export interface CreateWalkInRequest {
  dentistId: string;
  appointmentDate: string;
  patientName: string;
  patientPhone: string;
  dateOfBirth: string;   // "YYYY-MM-DD"
  gender: string;        // "Nam" | "Nữ" | "Khác"
  serviceId?: string;
  symptoms?: string;
  patientId?: string;    // hồ sơ đã chọn từ ô tra cứu — tránh tạo bệnh nhân trùng
  /**
   * Email của bệnh nhân MỚI. Có email thì hệ thống lập luôn tài khoản đăng nhập và gửi mật khẩu
   * tạm về đó, để lần sau bệnh nhân tự đặt lịch trên app — sau khi bỏ tự đăng ký, đây là đường
   * duy nhất sinh tài khoản. Bỏ trống vẫn khám được, chỉ là chưa dùng được app.
   */
  patientEmail?: string;
  /**
   * Mã bệnh nhân đọc từ hộp thư (lấy qua sendPatientEmailVerificationApi). Thiếu mã thì backend
   * CHỈ tạo hồ sơ bệnh nhân, không cấp tài khoản — tránh gửi mật khẩu tới email gõ nhầm.
   */
  emailVerificationCode?: string;
}

/**
 * Gửi mã xác thực tới email bệnh nhân vừa cung cấp. Bệnh nhân mở hộp thư và đọc mã cho lễ tân.
 * Không có bước này thì gõ nhầm một ký tự là mật khẩu bay tới hộp thư người lạ, kèm quyền đăng
 * nhập vào hồ sơ bệnh án của bệnh nhân thật.
 */
export async function sendPatientEmailVerificationApi(email: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/patients/accounts/verification`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ email }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Gửi mã xác thực thất bại");
  }
}

export interface CreatePatientAccountRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth?: string; // "YYYY-MM-DD"
  gender?: string;      // "Nam" | "Nữ" | "Khác"
  verificationCode: string;
}

export interface CreatePatientAccountResult {
  userId: string;
  patientId: string;
  email: string;
  fullName: string;
  linkedExistingPatient: boolean;
}

export async function createPatientAccountApi(request: CreatePatientAccountRequest): Promise<CreatePatientAccountResult> {
  const res = await fetch(`${API_URL}/api/patients/accounts`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(
      (err as { message?: string; title?: string; detail?: string }).message ??
      (err as { detail?: string }).detail ??
      (err as { title?: string }).title ??
      "Tạo tài khoản bệnh nhân thất bại"
    );
  }
  return res.json() as Promise<CreatePatientAccountResult>;
}

export interface PatientSearchResultDto {
  id: string;
  fullName: string;
  phoneNumber: string | null;
  dateOfBirth: string;   // "YYYY-MM-DD"
  gender: string;
  hasAccount: boolean;
}

/** Tra cứu bệnh nhân đã có hồ sơ (staff/admin). Dưới 2 ký tự backend trả về mảng rỗng. */
export async function searchPatientsApi(query: string, limit = 8): Promise<PatientSearchResultDto[]> {
  const params = new URLSearchParams({ q: query, limit: String(limit) });
  const res = await fetch(`${API_URL}/api/patients/search?${params}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tìm bệnh nhân");
  }
  return res.json() as Promise<PatientSearchResultDto[]>;
}

export interface PatientAppointmentHistoryItemDto {
  appointmentId: string;
  appointmentCode: string;
  appointmentDate: string; // ISO8601
  dentistName: string;
  serviceName: string | null;
  status: string; // "Pending" | "Confirmed" | "CheckedIn" | "InProgress" | "PendingPayment" | "Completed" | "Cancelled" | "NoShow"
  paymentStatus: string | null; // "Unpaid" | "Paid" | "Refunded" — null nếu chưa xuất hóa đơn
  isSettled: boolean | null;
  totalAmount: number | null;
}

export interface PatientDetailDto {
  id: string;
  fullName: string;
  phone: string | null;
  email: string | null;
  dateOfBirth: string | null; // "YYYY-MM-DD"
  gender: string | null;
  address: string | null;
  appointments: PatientAppointmentHistoryItemDto[];
}

/** Thông tin bệnh nhân kèm toàn bộ lịch sử khám (mọi trạng thái) + trạng thái thanh toán từng buổi. */
export async function getPatientDetailApi(patientId: string): Promise<PatientDetailDto> {
  const res = await fetch(`${API_URL}/api/patients/${patientId}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải thông tin bệnh nhân");
  }
  return res.json() as Promise<PatientDetailDto>;
}

export interface CreateWalkInResult {
  appointmentId: string;
  appointmentCode: string;
  patientName: string;
  status: string;
}

export async function createWalkInAppointmentApi(request: CreateWalkInRequest): Promise<CreateWalkInResult> {
  const res = await fetch(`${API_URL}/api/appointments/walkin`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đặt lịch tại quầy thất bại");
  }
  return res.json() as Promise<CreateWalkInResult>;
}

// ── Dentist Dashboard ──────────────────────────────────────────────────────

export async function getDentistDashboardApi(): Promise<DentistDashboardResponse> {
  const res = await fetch(`${API_URL}/api/appointments/dentist/dashboard`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải tổng quan");
  }
  return res.json() as Promise<DentistDashboardResponse>;
}

export async function getDentistPatientsApi(date?: string): Promise<DentistPatientsResponse> {
  const params = new URLSearchParams();
  if (date) params.set("date", date);
  const query = params.toString() ? `?${params}` : "";
  const res = await fetch(`${API_URL}/api/appointments/dentist/patients${query}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách bệnh nhân");
  }
  return res.json() as Promise<DentistPatientsResponse>;
}

export async function getDentistPastPatientsApi(): Promise<DentistPatientDto[]> {
  const res = await fetch(`${API_URL}/api/appointments/dentist/patients/past`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách bệnh nhân đã từng khám");
  }
  return res.json() as Promise<DentistPatientDto[]>;
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

// ── Examination & Treatment APIs ─────────────────────────────────────────────────

export interface PatientBriefDto {
  id: string;
  fullName: string;
  phoneNumber: string | null;
  email: string | null;
  dateOfBirth: string | null;
  gender: string | null;
  address: string | null;
}

export interface DentistBriefDto {
  id: string;
  fullName: string;
}

export interface DiagnosisDto {
  id: string;
  description: string;              // Chẩn đoán
  // Tình trạng lợi – niêm mạc
  gumCondition: string | null;
  oralMucosaCondition: string | null;
  gumBleeding: string | null;
  painOnChewing: string | null;
  // Tình trạng răng
  teethCount: string | null;
  decayedTeeth: string | null;
  wornOrBrokenTeeth: string | null;
  looseTeeth: string | null;
  // Vệ sinh răng miệng
  tartar: string | null;
  plaque: string | null;
  badBreath: string | null;
  // Khớp thái dương hàm / khớp cắn
  tmjSymptoms: string | null;
  occlusion: string | null;
  occlusionDeviation: string | null;
  // Tiền sử
  medicalHistory: string | null;
  allergyHistory: string | null;
  conclusion: string | null;        // Kết quả & kế hoạch điều trị
  createdAt: string;
  updatedAt: string | null;
}

export interface StepProgressEntryDto {
  /** Id ổn định của mục này (không đổi khi sắp xếp lại) — dùng để gắn vật tư tiêu hao với đúng bước. */
  id: string;
  stepNumber: number;
  stepName: string;
  percent: number;
  date: string;
  dentistName: string;
  note: string | null;
}

export interface TreatmentPlanDto {
  id: string;
  patientId: string;
  dentistId: string;
  dentistName: string;
  appointmentId: string | null;
  serviceId: string;
  serviceName: string;
  /** Tên option đã chọn lúc thêm dịch vụ (vd: "Titan", "Zirconia") — null nếu dùng giá gốc dịch vụ. */
  serviceOptionName: string | null;
  unitPrice: number;
  quantity: number;
  teeth: string | null;
  status: string;
  warrantyUntil: string | null;
  notes: string | null;
  totalCost: number;
  amountPaid: number;
  /** Đã xuất hóa đơn → không cho bác sĩ xóa/hủy dịch vụ này khỏi liệu trình. */
  isInvoiced: boolean;
  stepProgress: StepProgressEntryDto[];
  /** Tổng số bước quy trình chuẩn của dịch vụ (0 = dịch vụ chưa khai báo quy trình). */
  totalSteps: number;
  /** Số bước đã ghi nhận đạt 100%. */
  completedSteps: number;
  /** % hoàn thành của dịch vụ = số bước xong / tổng số bước. */
  progressPercent: number;
  createdAt: string;
  completedAt: string | null;
}

export interface PrescriptionItemDto {
  id: string;
  medicineName: string;
  dosage: string;
  quantity: number;
  unit: string;
  usage: string;
  notes: string | null;
  timesPerDay: number | null;
  durationDays: number | null;
  startDate: string | null;
}

export interface PrescriptionDto {
  id: string;
  notes: string | null;
  createdAt: string;
  items: PrescriptionItemDto[];
}

export interface ExaminationDto {
  appointmentId: string;
  appointmentCode: string;
  patient: PatientBriefDto;
  dentist: DentistBriefDto;
  serviceName: string | null;
  appointmentDate: string;
  status: string;
  symptoms: string | null;
  notes: string | null;
  startTime: string | null;
  followUpDate: string | null;
  followUpNote: string | null;
  isFollowUpVisit: boolean;
  relatedAppointmentIds: string[]; // Chuỗi buổi gốc của lượt tái khám
  diagnoses: DiagnosisDto[];
  treatmentPlans: TreatmentPlanDto[];
  prescription: PrescriptionDto | null;
}

export async function getExaminationApi(appointmentId: string): Promise<ExaminationDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/examination`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải thông tin khám");
  }
  return res.json() as Promise<ExaminationDto>;
}

// Patient Medical History APIs
export interface PatientMedicalHistoryDto {
  appointmentId: string;
  appointmentCode: string;
  appointmentDate: string;
  dentistName: string;
  serviceName: string;
  symptoms: string | null;
  diagnoses: MedicalHistoryDiagnosisDto[];
  treatmentPlans: MedicalHistoryTreatmentPlanDto[];
  prescriptionItems: MedicalHistoryPrescriptionItemDto[];
}

export interface MedicalHistoryDiagnosisDto {
  description: string;
  conclusion: string | null;
  createdAt: string;
}

export interface MedicalHistoryTreatmentPlanDto {
  description: string;
  status: string;
  estimatedCost: number | null;
}

export interface MedicalHistoryPrescriptionItemDto {
  medicineName: string;
  dosage: string;
  quantity: number;
  unit: string;
}

export async function getPatientMedicalHistoryApi(patientId: string): Promise<PatientMedicalHistoryDto[]> {
  const res = await fetch(`${API_URL}/api/appointments/patients/${patientId}/medical-history`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch sử khám");
  }
  return res.json() as Promise<PatientMedicalHistoryDto[]>;
}

export interface PatientHistorySummaryDto {
  summary: string;
  disclaimer: string;
  fromCache: boolean;
}

export async function getPatientAiSummaryApi(
  appointmentId: string,
  force = false,
): Promise<PatientHistorySummaryDto> {
  const qs = force ? "?force=true" : "";
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/ai-summary${qs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo tóm tắt AI");
  }
  return res.json() as Promise<PatientHistorySummaryDto>;
}

export interface TreatmentSuggestionDto {
  suggestion: string;
  disclaimer: string;
}

export async function getTreatmentSuggestionApi(appointmentId: string): Promise<TreatmentSuggestionDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/treatment-suggestion`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo gợi ý điều trị");
  }
  return res.json() as Promise<TreatmentSuggestionDto>;
}

// Diagnosis APIs
/** Các trường của phiếu khám răng miệng — dùng chung cho tạo mới và cập nhật. */
export interface DiagnosisFields {
  description: string;              // Chẩn đoán
  gumCondition?: string;
  oralMucosaCondition?: string;
  gumBleeding?: string;
  painOnChewing?: string;
  teethCount?: string;
  decayedTeeth?: string;
  wornOrBrokenTeeth?: string;
  looseTeeth?: string;
  tartar?: string;
  plaque?: string;
  badBreath?: string;
  tmjSymptoms?: string;
  occlusion?: string;
  occlusionDeviation?: string;
  medicalHistory?: string;
  allergyHistory?: string;
  conclusion?: string;              // Kết quả & kế hoạch điều trị
}

export type CreateDiagnosisRequest = DiagnosisFields;

export interface UpdateDiagnosisRequest extends DiagnosisFields {
  diagnosisId: string;
}

export async function createDiagnosisApi(appointmentId: string, request: CreateDiagnosisRequest): Promise<DiagnosisDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/diagnosis`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể thêm chuẩn đoán");
  }
  return res.json() as Promise<DiagnosisDto>;
}

export async function updateDiagnosisApi(request: UpdateDiagnosisRequest): Promise<DiagnosisDto> {
  const res = await fetch(`${API_URL}/api/appointments/diagnosis/${request.diagnosisId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật chuẩn đoán");
  }
  return res.json() as Promise<DiagnosisDto>;
}

export async function deleteDiagnosisApi(diagnosisId: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/diagnosis/${diagnosisId}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể xóa chuẩn đoán");
  }
}

// Appointment Photo APIs — ảnh chụp tay (X-quang/dấu răng/răng lợi...) gắn với buổi hẹn, không qua
// máy tích hợp. section: "exam" (tab Khám) | "material-request" (tab Vật tư).
export interface AppointmentPhotoDto {
  id: string;
  appointmentId: string;
  section: string;
  url: string;
  note: string | null;
  uploadedBy: string;
  createdAt: string;
}

export async function getAppointmentPhotosApi(appointmentId: string, section?: string): Promise<AppointmentPhotoDto[]> {
  const qs = section ? `?section=${encodeURIComponent(section)}` : "";
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/photos${qs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải ảnh");
  }
  return res.json() as Promise<AppointmentPhotoDto[]>;
}

export async function addAppointmentPhotoApi(appointmentId: string, request: { section: string; url: string; note?: string }): Promise<AppointmentPhotoDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/photos`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể thêm ảnh");
  }
  return res.json() as Promise<AppointmentPhotoDto>;
}

export async function updateAppointmentPhotoNoteApi(photoId: string, note?: string): Promise<AppointmentPhotoDto> {
  const res = await fetch(`${API_URL}/api/appointments/photos/${photoId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ note }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật ghi chú");
  }
  return res.json() as Promise<AppointmentPhotoDto>;
}

export async function deleteAppointmentPhotoApi(photoId: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/photos/${photoId}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể xóa ảnh");
  }
}

// Treatment Plan APIs
export interface CreateTreatmentPlanRequest {
  serviceId: string;
  unitPrice?: number;
  quantity: number;
  teeth?: string;
  notes?: string;
  warrantyUntil?: string;
  serviceOptionName?: string;
}

export interface UpdateTreatmentPlanRequest {
  treatmentPlanId: string;
  unitPrice: number;
  quantity: number;
  teeth?: string;
  notes?: string;
  warrantyUntil?: string;
  status?: string;
}

export async function createTreatmentPlanApi(appointmentId: string, request: CreateTreatmentPlanRequest): Promise<TreatmentPlanDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/treatment-plan`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo liệu trình");
  }
  return res.json() as Promise<TreatmentPlanDto>;
}

export async function updateTreatmentPlanApi(request: UpdateTreatmentPlanRequest): Promise<TreatmentPlanDto> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${request.treatmentPlanId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật liệu trình");
  }
  return res.json() as Promise<TreatmentPlanDto>;
}

export async function getPatientTreatmentPlansApi(patientId: string): Promise<TreatmentPlanDto[]> {
  const res = await fetch(`${API_URL}/api/appointments/patients/${patientId}/treatment-plans`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải liệu trình của bệnh nhân");
  }
  return res.json() as Promise<TreatmentPlanDto[]>;
}

export interface AddStepProgressRequest {
  stepNumber: number;
  stepName: string;
  percent: number;
  date?: string;
  note?: string;
}

export interface UpdateStepProgressRequest {
  entryIndex: number; // vị trí mục trong nhật ký (stepProgress)
  percent: number;
  note?: string;
  stepName?: string; // đổi tên bước điều trị
  date?: string;     // đổi ngày thực hiện (yyyy-MM-dd)
}

export async function updateTreatmentPlanProgressApi(
  treatmentPlanId: string,
  request: UpdateStepProgressRequest
): Promise<TreatmentPlanDto> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${treatmentPlanId}/progress`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể sửa quá trình điều trị");
  }
  return res.json() as Promise<TreatmentPlanDto>;
}

export async function reorderTreatmentPlanProgressApi(
  treatmentPlanId: string,
  order: number[] // hoán vị các index gốc theo thứ tự mới
): Promise<TreatmentPlanDto> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${treatmentPlanId}/progress/reorder`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ order }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể đổi thứ tự quá trình điều trị");
  }
  return res.json() as Promise<TreatmentPlanDto>;
}

export async function deleteTreatmentPlanProgressApi(
  treatmentPlanId: string,
  entryIndex: number
): Promise<TreatmentPlanDto> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${treatmentPlanId}/progress/${entryIndex}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể xóa quá trình điều trị");
  }
  return res.json() as Promise<TreatmentPlanDto>;
}

export async function addTreatmentPlanProgressApi(
  treatmentPlanId: string,
  request: AddStepProgressRequest
): Promise<TreatmentPlanDto> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${treatmentPlanId}/progress`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể ghi nhận quá trình điều trị");
  }
  return res.json() as Promise<TreatmentPlanDto>;
}

// Treatment Procedure APIs (quy trình điều trị chuẩn theo dịch vụ)
export interface TreatmentProcedureDto {
  id: string;
  serviceId: string;
  stepNumber: number;
  name: string;
}

export interface ProcedureStepRequest {
  stepNumber: number;
  name: string;
}

export async function getServiceProceduresApi(serviceId: string): Promise<TreatmentProcedureDto[]> {
  const res = await fetch(`${API_URL}/api/services/${serviceId}/procedures`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải quy trình điều trị");
  }
  return res.json() as Promise<TreatmentProcedureDto[]>;
}

export async function updateServiceProceduresApi(
  serviceId: string,
  steps: ProcedureStepRequest[]
): Promise<TreatmentProcedureDto[]> {
  const res = await fetch(`${API_URL}/api/services/${serviceId}/procedures`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(steps),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể lưu quy trình điều trị");
  }
  return res.json() as Promise<TreatmentProcedureDto[]>;
}

// Định mức vật tư chuẩn theo dịch vụ (BOM) — gợi ý mặc định cho bác sĩ khi ghi nhận tiêu hao thực tế.
export interface ServiceSupplyItemDto {
  id: string;
  serviceId: string;
  /** Option riêng mà dòng này áp dụng (vd: "Titan") — null = dùng chung cho mọi option. */
  serviceOptionName: string | null;
  supplyItemId: string;
  supplyItemName: string;
  unit: string;
  defaultQuantity: number;
}

export interface ServiceSupplyItemStepRequest {
  supplyItemId: string;
  defaultQuantity: number;
  serviceOptionName?: string | null;
}

/** Toàn bộ định mức của dịch vụ (mọi option) — dùng cho màn quản lý (Admin). */
export async function getServiceSupplyItemsApi(serviceId: string): Promise<ServiceSupplyItemDto[]> {
  const res = await fetch(`${API_URL}/api/services/${serviceId}/supply-items`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải định mức vật tư");
  }
  return res.json() as Promise<ServiceSupplyItemDto[]>;
}

/** Định mức HIỆU LỰC khi đã biết option cụ thể (chung + theo option đã chọn) — dùng phía bác sĩ. */
export async function getEffectiveServiceSupplyItemsApi(serviceId: string, optionName?: string | null): Promise<ServiceSupplyItemDto[]> {
  const qs = optionName ? `?option=${encodeURIComponent(optionName)}` : "";
  const res = await fetch(`${API_URL}/api/services/${serviceId}/supply-items/effective${qs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải định mức vật tư");
  }
  return res.json() as Promise<ServiceSupplyItemDto[]>;
}

export async function updateServiceSupplyItemsApi(
  serviceId: string,
  items: ServiceSupplyItemStepRequest[]
): Promise<ServiceSupplyItemDto[]> {
  const res = await fetch(`${API_URL}/api/services/${serviceId}/supply-items`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(items),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể lưu định mức vật tư");
  }
  return res.json() as Promise<ServiceSupplyItemDto[]>;
}

// Vật tư đã ghi nhận tiêu hao thực tế cho một liệu trình điều trị (xem TreatmentSupplyUsage).
export interface TreatmentSupplyUsageDto {
  id: string;
  supplyItemId: string;
  supplyItemName: string;
  unit: string;
  quantity: number;
  unitCostAtUsage: number;
  totalCost: number;
  createdAt: string;
  /** True nếu đã được hoàn kho lại do bước điều trị gắn với lần dùng này bị xóa. */
  isReversed: boolean;
}

export interface RecordSupplyUsageItemInput {
  supplyItemId: string;
  quantity: number;
}

export async function getTreatmentSupplyUsageApi(treatmentPlanId: string): Promise<TreatmentSupplyUsageDto[]> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${treatmentPlanId}/supply-usage`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải vật tư đã dùng");
  }
  return res.json() as Promise<TreatmentSupplyUsageDto[]>;
}

export async function recordTreatmentSupplyUsageApi(
  treatmentPlanId: string,
  items: RecordSupplyUsageItemInput[],
  stepEntryId?: string | null
): Promise<TreatmentSupplyUsageDto[]> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${treatmentPlanId}/supply-usage`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ items, stepEntryId: stepEntryId ?? null }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể ghi nhận vật tư đã dùng");
  }
  return res.json() as Promise<TreatmentSupplyUsageDto[]>;
}

export async function deleteTreatmentPlanApi(treatmentPlanId: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/treatment-plan/${treatmentPlanId}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể xóa liệu trình");
  }
}

// Prescription APIs
export interface PrescriptionItemRequest {
  medicineName: string;
  dosage: string;
  quantity: number;
  unit: string;
  usage: string;
  notes?: string;
  timesPerDay?: number;
  durationDays?: number;
  startDate?: string;
}

export interface CreatePrescriptionRequest {
  notes?: string;
  items?: PrescriptionItemRequest[];
}

export interface UpdatePrescriptionRequest {
  prescriptionId: string;
  notes?: string;
}

export interface AddPrescriptionItemRequest {
  prescriptionId: string;
  medicineName: string;
  dosage: string;
  quantity: number;
  unit: string;
  usage: string;
  notes?: string;
  timesPerDay?: number;
  durationDays?: number;
  startDate?: string;
}

export interface UpdatePrescriptionItemRequest {
  itemId: string;
  medicineName: string;
  dosage: string;
  quantity: number;
  unit: string;
  usage: string;
  notes?: string;
  timesPerDay?: number;
  durationDays?: number;
  startDate?: string;
}

export async function createPrescriptionApi(appointmentId: string, request: CreatePrescriptionRequest): Promise<PrescriptionDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/prescription`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo đơn thuốc");
  }
  return res.json() as Promise<PrescriptionDto>;
}

export async function updatePrescriptionApi(request: UpdatePrescriptionRequest): Promise<PrescriptionDto> {
  const res = await fetch(`${API_URL}/api/appointments/prescription/${request.prescriptionId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ notes: request.notes }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật đơn thuốc");
  }
  return res.json() as Promise<PrescriptionDto>;
}

export async function addPrescriptionItemApi(request: AddPrescriptionItemRequest): Promise<PrescriptionDto> {
  const res = await fetch(`${API_URL}/api/appointments/prescription/${request.prescriptionId}/items`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({
      medicineName: request.medicineName,
      dosage: request.dosage,
      quantity: request.quantity,
      unit: request.unit,
      usage: request.usage,
      notes: request.notes,
      timesPerDay: request.timesPerDay,
      durationDays: request.durationDays,
      startDate: request.startDate,
    }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể thêm thuốc vào đơn");
  }
  return res.json() as Promise<PrescriptionDto>;
}

export async function deletePrescriptionItemApi(itemId: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/prescription-items/${itemId}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể xóa thuốc khỏi đơn");
  }
}

// Follow-up Reminder APIs (nhắc tái khám — chỉ hẹn ngày, không đặt lịch mới)
export interface FollowUpReminderDto {
  appointmentId: string;
  followUpDate: string | null;
  followUpNote: string | null;
}

export async function setFollowUpReminderApi(
  appointmentId: string,
  request: { followUpDate: string; note?: string }
): Promise<FollowUpReminderDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/follow-up-reminder`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(request),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể lưu lịch hẹn tái khám");
  }
  return res.json() as Promise<FollowUpReminderDto>;
}

export async function clearFollowUpReminderApi(appointmentId: string): Promise<FollowUpReminderDto> {
  const res = await fetch(`${API_URL}/api/appointments/${appointmentId}/follow-up-reminder`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể hủy lịch hẹn tái khám");
  }
  return res.json() as Promise<FollowUpReminderDto>;
}

// Bệnh nhân đang trong diện chờ tái khám (còn liệu trình đang thực hiện sau khi kết thúc điều trị)
export interface FollowUpDueDto {
  originalAppointmentId: string;
  patientId: string;
  patientName: string;
  patientPhone: string | null;
  gender: string | null;
  dentistName: string;
  serviceName: string | null;
  originalAppointmentDate: string;
  followUpDate: string | null;
  followUpNote: string | null;
  activePlans: string[]; // Các liệu trình đang thực hiện
}

export async function getFollowUpDueApi(): Promise<FollowUpDueDto[]> {
  const res = await fetch(`${API_URL}/api/appointments/follow-up-due`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách chờ tái khám");
  }
  return res.json() as Promise<FollowUpDueDto[]>;
}

export async function checkInFollowUpApi(originalAppointmentId: string): Promise<{ appointmentId: string }> {
  const res = await fetch(`${API_URL}/api/appointments/${originalAppointmentId}/follow-up-check-in`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Check-in tái khám thất bại");
  }
  return res.json() as Promise<{ appointmentId: string }>;
}

// End Treatment API
export async function endTreatmentApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/appointments/${id}/end-treatment`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Kết thúc điều trị thất bại");
  }
}

// ── Profile APIs ──────────────────────────────────────────────────────────

export interface UserProfileDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  dateOfBirth: string | null;
  gender: string | null;
  profilePictureUrl: string | null;
  role: string;
  employeeId: string | null;
  department: string | null;
  employmentStatus: string | null;
  position: string | null;
  startDate: string | null;
  specialty: string | null;
  licenseNumber: string | null;
  yearsOfExperience: number | null;
  education: string | null;
  bio: string | null;
  address: string | null;
  baseSalary: number;
  allowance: number;
  salaryNote: string;
  certificateIssuedDate: string | null;
  certificateIssuedBy: string | null;
  servicesHandled: string | null;
  username: string | null;
  createdAt: string;
}

export interface UpdateMyProfileCommand {
  fullName: string;
  phoneNumber: string;
  dateOfBirth: string | null;
  gender: string | null;
  address?: string | null;
  profilePictureUrl?: string | null;
  bio?: string | null;
  education?: string | null;
  specialty?: string | null;
  yearsOfExperience?: number | null;
}

export async function getMyProfileApi(): Promise<UserProfileDto> {
  const res = await fetch(`${API_URL}/api/auth/me/profile`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải thông tin cá nhân");
  }
  return res.json() as Promise<UserProfileDto>;
}

export async function updateMyProfileApi(data: UpdateMyProfileCommand): Promise<void> {
  const res = await fetch(`${API_URL}/api/auth/me/profile`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật thông tin cá nhân thất bại");
  }

  // Update cached user session in local storage
  if (typeof window !== "undefined") {
    const rawUser = localStorage.getItem("dental_clinic_user");
    if (rawUser) {
      try {
        const cachedUser = JSON.parse(rawUser) as AuthUser;
        cachedUser.fullName = data.fullName;
        if (data.profilePictureUrl !== undefined) {
          cachedUser.profilePictureUrl = data.profilePictureUrl;
        }
        localStorage.setItem("dental_clinic_user", JSON.stringify(cachedUser));
      } catch (e) {
        console.error("Failed to update cached user", e);
      }
    }
  }
}

export async function changePasswordApi(currentPassword: string, newPassword: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/auth/me/change-password`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ currentPassword, newPassword }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đổi mật khẩu thất bại");
  }
}

// ── Clinic Info types & endpoints ───────────────────────────────────────────

export interface MilestoneDto {
  year: number;
  description: string;
}

export interface FeatureDto {
  title: string;
  description: string;
}

export interface TreatmentStepDto {
  title: string;
  description: string;
}

export interface StatisticDto {
  value: string;
  label: string;
}

export interface ClinicInfoDto {
  id: string;
  aboutTitle: string;
  aboutDescription: string;
  foundedYear: number;
  aboutImageUrl: string | null;
  phone: string;
  email: string;
  address: string;
  milestones: MilestoneDto[];
  certifications: string[];
  features: FeatureDto[];
  treatmentSteps: TreatmentStepDto[];
  statistics: StatisticDto[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpdateClinicInfoRequest {
  aboutTitle: string;
  aboutDescription: string;
  foundedYear: number;
  phone: string;
  email: string;
  address: string;
  aboutImageUrl: string | null;
  milestones?: MilestoneDto[] | null;
  certifications?: string[] | null;
  features?: FeatureDto[] | null;
  treatmentSteps?: TreatmentStepDto[] | null;
  statistics?: StatisticDto[] | null;
}

export async function getClinicInfoApi(): Promise<ClinicInfoDto> {
  const res = await fetch(`${API_URL}/api/clinic-info`, {
    headers: { "Content-Type": "application/json" },
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải thông tin phòng khám");
  }
  return res.json() as Promise<ClinicInfoDto>;
}

export async function updateClinicInfoApi(data: UpdateClinicInfoRequest): Promise<ClinicInfoDto> {
  const res = await fetch(`${API_URL}/api/clinic-info`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      ...authHeaders(),
    },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật thông tin phòng khám thất bại");
  }
  return res.json() as Promise<ClinicInfoDto>;
}

// ── Invoice types & endpoints ────────────────────────────────────────────────

export interface InvoiceItemDto {
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  treatmentPlanId?: string | null; // liệu trình mà dòng này thu tiền cho
}

export interface BillablePlanDto {
  appointmentId: string;
  appointmentCode: string;
  patientName: string;
  patientPhone: string | null;
  gender: string | null;
  dentistName: string;
  appointmentDate: string;
  diagnosis: string;
  items: InvoiceItemDto[];
  suggestedTotal: number;
  outstandingInvoiceId: string | null;  // hóa đơn đặt cọc gốc nếu đây là "thu phần còn lại"
  sourceInvoiceNumber: string | null;
  // Khi mục này là một đợt thu của liệu trình điều trị
  treatmentPlanId: string | null;
  planName: string | null;
  planTotal: number;
  planAmountPaid: number;
  planRemaining: number;
}

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  appointmentId: string;
  patientName: string;
  patientPhone: string | null;
  gender: string | null;
  dentistName: string;
  appointmentDate: string;
  items: InvoiceItemDto[];
  subtotal: number;
  discount: number;
  promotionId: string | null;
  promotionCode: string | null;
  promotionName: string | null;
  totalAmount: number;
  paymentType: string;    // "Full" | "Deposit"
  depositAmount: number;  // Số tiền thu trên hóa đơn này
  remainingAmount: number;
  paymentMethod: string; // "Cash" | "BankTransfer" | "OnlinePayment"
  status: string;        // "Unpaid" | "Paid" | "Refunded"
  notes: string | null;
  createdAt: string;
  paymentDate: string | null;
  parentInvoiceId: string | null;
  isSettled: boolean;
  collectingRemaining: boolean;
}

export interface IssueInvoiceItemRequest {
  name: string;
  quantity: number;
  unitPrice: number;
  treatmentPlanId?: string | null;
  amountCollected?: number | null; // số thu ngay của dòng (toàn bộ hoặc cọc theo dòng)
}

export interface IssueInvoiceRequest {
  appointmentId: string;
  items: IssueInvoiceItemRequest[];
  discount: number;
  paymentMethod: string;   // "cash" | "transfer" | "app" or enum name
  paymentType?: string;    // "full" | "deposit"
  depositAmount?: number;  // bắt buộc > 0 khi đặt cọc
  notes?: string | null;
  parentInvoiceId?: string | null;  // khi thu phần còn lại của hóa đơn đặt cọc
  treatmentPlanId?: string | null;  // khi thu một đợt của liệu trình điều trị
  promotionId?: string | null;      // khuyến mãi áp dụng — server tự tính lại discount từ khuyến mãi này
}

// ── Công nợ liệu trình điều trị ──────────────────────────────────────────────

export interface OutstandingPlanDto {
  treatmentPlanId: string;
  planName: string;
  patientName: string;
  patientPhone: string | null;
  gender: string | null;
  dentistName: string;
  totalCost: number;
  amountPaid: number;
  remainingAmount: number;
  /** Phần chi phí chưa gắn vào hóa đơn nào — số còn phải xuất hóa đơn ở các đợt thu sau. */
  unbilledAmount: number;
  status: string;
  createdAt: string;
}

export async function getOutstandingPlansApi(): Promise<OutstandingPlanDto[]> {
  const res = await fetch(`${API_URL}/api/invoices/outstanding-plans`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải công nợ liệu trình");
  }
  return res.json() as Promise<OutstandingPlanDto[]>;
}

export async function getBillablePlansApi(): Promise<BillablePlanDto[]> {
  const res = await fetch(`${API_URL}/api/invoices/billable-plans`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách liệu trình chờ xuất hóa đơn");
  }
  return res.json() as Promise<BillablePlanDto[]>;
}

export async function issueInvoiceApi(data: IssueInvoiceRequest): Promise<InvoiceDto> {
  const res = await fetch(`${API_URL}/api/invoices`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xuất hóa đơn thất bại");
  }
  return res.json() as Promise<InvoiceDto>;
}

export async function getPendingInvoicesApi(): Promise<InvoiceDto[]> {
  const res = await fetch(`${API_URL}/api/invoices/pending`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải hóa đơn chờ thanh toán");
  }
  return res.json() as Promise<InvoiceDto[]>;
}

export async function getInvoiceHistoryApi(): Promise<InvoiceDto[]> {
  const res = await fetch(`${API_URL}/api/invoices/history`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch sử hóa đơn");
  }
  return res.json() as Promise<InvoiceDto[]>;
}

export async function getOutstandingInvoicesApi(): Promise<InvoiceDto[]> {
  const res = await fetch(`${API_URL}/api/invoices/outstanding`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách công nợ");
  }
  return res.json() as Promise<InvoiceDto[]>;
}

export async function getInvoicesByPatientApi(patientId: string): Promise<InvoiceDto[]> {
  const res = await fetch(`${API_URL}/api/invoices/by-patient/${patientId}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải hóa đơn của bệnh nhân");
  }
  return res.json() as Promise<InvoiceDto[]>;
}

export async function collectRemainingInvoiceApi(invoiceId: string): Promise<InvoiceDto> {
  const res = await fetch(`${API_URL}/api/invoices/${invoiceId}/collect-remaining`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo yêu cầu thu phần còn lại");
  }
  return res.json() as Promise<InvoiceDto>;
}

export async function confirmInvoicePaymentApi(invoiceId: string, paymentMethod?: string): Promise<InvoiceDto> {
  const res = await fetch(`${API_URL}/api/invoices/${invoiceId}/pay`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ paymentMethod: paymentMethod ?? null }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xác nhận thanh toán thất bại");
  }
  return res.json() as Promise<InvoiceDto>;
}

// ── Payment gateway (PayOS) — VietQR chuyển khoản & thanh toán online ───────

export interface PaymentTransactionDto {
  id: string;
  invoiceId: string;
  gateway: string;          // "PayOS"
  status: string;           // "Pending" | "Success" | "Failed" | "Cancelled" | "Expired"
  gatewayOrderCode: string;
  amount: number;
  checkoutUrl: string | null;
  qrCode: string | null;
  createdAt: string;
  expiresAt: string | null;
}

export interface PaymentStatusDto {
  invoiceId: string;
  invoiceStatus: string;             // "Unpaid" | "Paid" | "Refunded"
  latestTransaction: PaymentTransactionDto | null;
}

export async function createPaymentRequestApi(invoiceId: string, gateway?: string): Promise<PaymentTransactionDto> {
  const res = await fetch(`${API_URL}/api/payments/invoices/${invoiceId}/request`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ gateway: gateway ?? "PayOS" }),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo yêu cầu thanh toán");
  }
  return res.json() as Promise<PaymentTransactionDto>;
}

export async function getPaymentStatusApi(invoiceId: string): Promise<PaymentStatusDto> {
  const res = await fetch(`${API_URL}/api/payments/invoices/${invoiceId}/status`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể kiểm tra trạng thái thanh toán");
  }
  return res.json() as Promise<PaymentStatusDto>;
}

// ── Activity Logs ───────────────────────────────────────────────────────────

export interface ActivityLogItemDto {
  id: number;
  userId: string | null;
  userName: string;
  userRole: string;
  action: string;
  module: string;
  description: string;
  ipAddress: string | null;
  status: string;
  targetId: string | null;
  createdAt: string;
}

export interface ActivityLogPagedDto {
  items: ActivityLogItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ── Notification types & endpoints ──────────────────────────────────────────

export interface NotificationDto {
  id: string;
  type: string;
  priority: string;
  title: string;
  body: string;
  isRead: boolean;
  readAt: string | null;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  createdAt: string;
}

export interface NotificationPagedDto {
  items: NotificationDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  unreadCount: number;
}

export async function getNotificationsApi(params?: {
  type?: string;
  priority?: string;
  isRead?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
  /** "asc" | "desc" theo thời gian — mặc định "desc" (mới nhất trước) */
  sortDir?: "asc" | "desc";
}): Promise<NotificationPagedDto> {
  const qs = new URLSearchParams();
  if (params?.type)               qs.set("type",     params.type);
  if (params?.priority)           qs.set("priority", params.priority);
  if (params?.isRead !== undefined) qs.set("isRead",  String(params.isRead));
  if (params?.search)             qs.set("search",   params.search);
  if (params?.page)               qs.set("page",     String(params.page));
  if (params?.pageSize)           qs.set("pageSize", String(params.pageSize));
  if (params?.sortDir)            qs.set("sortDir",  params.sortDir);
  const query = qs.toString() ? `?${qs.toString()}` : "";
  const res = await fetch(`${API_URL}/api/notifications${query}`, {
    headers: { "Content-Type": "application/json", ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải thông báo");
  }
  return res.json() as Promise<NotificationPagedDto>;
}

export async function markNotificationReadApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/notifications/${id}/read`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đánh dấu đã đọc thất bại");
  }
}

export async function markAllNotificationsReadApi(): Promise<void> {
  const res = await fetch(`${API_URL}/api/notifications/read-all`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Đánh dấu đọc tất cả thất bại");
  }
}

export async function deleteNotificationApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/notifications/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xóa thông báo thất bại");
  }
}

export async function getActivityLogsApi(params?: {
  userId?: string;
  action?: string;
  module?: string;
  status?: string;
  search?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
  /** "asc" | "desc" theo thời gian ghi nhận — mặc định "desc" (mới nhất trước) */
  sortDir?: "asc" | "desc";
}): Promise<ActivityLogPagedDto> {
  const qs = new URLSearchParams();
  if (params?.userId)    qs.set("userId",    params.userId);
  if (params?.action)    qs.set("action",    params.action);
  if (params?.module)    qs.set("module",    params.module);
  if (params?.status)    qs.set("status",    params.status);
  if (params?.search)    qs.set("search",    params.search);
  if (params?.startDate) qs.set("startDate", params.startDate);
  if (params?.endDate)   qs.set("endDate",   params.endDate);
  if (params?.page)      qs.set("page",      String(params.page));
  if (params?.pageSize)  qs.set("pageSize",  String(params.pageSize));
  if (params?.sortDir)   qs.set("sortDir",   params.sortDir);
  const query = qs.toString() ? `?${qs.toString()}` : "";

  const res = await fetch(`${API_URL}/api/activity-logs${query}`, {
    headers: { "Content-Type": "application/json", ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch sử hoạt động");
  }
  return res.json() as Promise<ActivityLogPagedDto>;
}

// ── Dashboard Admin (Tổng quan vận hành) ─────────────────────────────────────

export type DashboardRange = "week" | "month" | "year";

export interface DashboardStatsDto {
  range: DashboardRange;
  periodStart: string;
  periodEnd: string;
  newPatientsCount: number;
  newPatientsTrendPercent: number;
  appointmentsCount: number;
  appointmentsTrendPercent: number;
  revenueAmount: number;
  revenueTrendPercent: number;
}

export async function getDashboardStatsApi(range: DashboardRange): Promise<DashboardStatsDto> {
  const res = await fetch(`${API_URL}/api/dashboard/stats?range=${range}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải chỉ số tổng quan");
  }
  return res.json() as Promise<DashboardStatsDto>;
}

export interface AppointmentTrendPointDto {
  periodStart: string;
  periodEnd: string;
  count: number;
}

export interface AppointmentTrendDto {
  range: DashboardRange;
  points: AppointmentTrendPointDto[];
}

export async function getAppointmentTrendApi(range: DashboardRange): Promise<AppointmentTrendDto> {
  const res = await fetch(`${API_URL}/api/dashboard/appointment-trend?range=${range}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải biểu đồ lịch hẹn");
  }
  return res.json() as Promise<AppointmentTrendDto>;
}

export interface ServiceDistributionItemDto {
  serviceId: string | null;
  serviceName: string | null;
  count: number;
  percentage: number;
}

export interface ServiceDistributionDto {
  range: DashboardRange;
  totalAppointments: number;
  items: ServiceDistributionItemDto[];
}

export async function getServiceDistributionApi(
  range: DashboardRange,
  topN = 5
): Promise<ServiceDistributionDto> {
  const res = await fetch(`${API_URL}/api/dashboard/service-distribution?range=${range}&topN=${topN}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải tỷ lệ dịch vụ");
  }
  return res.json() as Promise<ServiceDistributionDto>;
}

export interface DashboardTodayAppointmentDto {
  id: string;
  appointmentDate: string;
  patientName: string;
  serviceName: string | null;
  status: string;
}

export interface DashboardTodayAppointmentsDto {
  items: DashboardTodayAppointmentDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export async function getDashboardTodayAppointmentsApi(
  page = 1,
  pageSize = 10
): Promise<DashboardTodayAppointmentsDto> {
  const res = await fetch(`${API_URL}/api/dashboard/today-appointments?page=${page}&pageSize=${pageSize}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch hẹn hôm nay");
  }
  return res.json() as Promise<DashboardTodayAppointmentsDto>;
}

export interface DashboardCalendarDayDto {
  date: string;
  isToday: boolean;
}

export interface DashboardShiftEntryDto {
  staffName: string;
  specialization: string | null;
  profilePictureUrl: string | null;
  room: string;
  roomColor: string;
  isBusy: boolean;
}

export interface DashboardWeeklyScheduleDto {
  selectedDate: string;
  week: DashboardCalendarDayDto[];
  morningShift: DashboardShiftEntryDto[];
  afternoonShift: DashboardShiftEntryDto[];
}

export async function getDashboardWeeklyScheduleApi(date?: string): Promise<DashboardWeeklyScheduleDto> {
  const qs = date ? `?date=${date}` : "";
  const res = await fetch(`${API_URL}/api/dashboard/weekly-schedule${qs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch vận hành phòng khám");
  }
  return res.json() as Promise<DashboardWeeklyScheduleDto>;
}

export interface DashboardFeedbackSummaryDto {
  id: string;
  customerName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface DashboardRecentFeedbackDto {
  items: DashboardFeedbackSummaryDto[];
  averageRating: number;
  totalFeaturedCount: number;
}

export async function getDashboardRecentFeedbackApi(limit = 3): Promise<DashboardRecentFeedbackDto> {
  const res = await fetch(`${API_URL}/api/dashboard/recent-feedback?limit=${limit}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải đánh giá khách hàng");
  }
  return res.json() as Promise<DashboardRecentFeedbackDto>;
}

// ── Dashboard Staff (Tổng quan lễ tân) ───────────────────────────────────────

export interface StaffDashboardStatsDto {
  appointmentsTodayCount: number;
  waitingCheckInCount: number;
  inProgressCount: number;
  pendingInvoicesCount: number;
}

export async function getStaffDashboardStatsApi(): Promise<StaffDashboardStatsDto> {
  const res = await fetch(`${API_URL}/api/staff-dashboard/stats`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải chỉ số tổng quan");
  }
  return res.json() as Promise<StaffDashboardStatsDto>;
}

export interface StaffDashboardTodayAppointmentDto {
  id: string;
  patientName: string;
  serviceName: string | null;
  dentistName: string;
  appointmentDate: string;
  status: string; // "Confirmed" | "CheckedIn" | "InProgress"
}

export async function getStaffDashboardTodayAppointmentsApi(
  limit = 5
): Promise<StaffDashboardTodayAppointmentDto[]> {
  const res = await fetch(`${API_URL}/api/staff-dashboard/today-appointments?limit=${limit}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải lịch hẹn hôm nay");
  }
  return res.json() as Promise<StaffDashboardTodayAppointmentDto[]>;
}

export interface StaffDashboardPendingInvoiceDto {
  id: string;
  invoiceNumber: string;
  patientName: string;
  serviceName: string | null;
  amount: number;
}

export async function getStaffDashboardPendingInvoicesApi(
  limit = 3
): Promise<StaffDashboardPendingInvoiceDto[]> {
  const res = await fetch(`${API_URL}/api/staff-dashboard/pending-invoices?limit=${limit}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải hóa đơn chờ thanh toán");
  }
  return res.json() as Promise<StaffDashboardPendingInvoiceDto[]>;
}

// ── AI Analytics (thống kê vận hành các tính năng AI) ────────────────────────

export interface AiFeatureUsageDto {
  feature: string;
  totalCalls: number;
  successCount: number;
  failureCount: number;
  avgDurationMs: number;
}

export interface AiDailyUsageDto {
  date: string;
  calls: number;
  failures: number;
}

export interface AiAnalyticsDto {
  rangeDays: number | null;
  totalConversations: number;
  totalMessages: number;
  totalUserMessages: number;
  suggestBookingCount: number;
  bookingActionCount: number;
  usageByFeature: AiFeatureUsageDto[];
  dailyUsage: AiDailyUsageDto[];
}

/** rangeDays = undefined/null → lấy TẤT CẢ dữ liệu từ trước tới nay (tùy chọn "Tất cả" trên UI). */
export async function getAiAnalyticsApi(rangeDays?: number): Promise<AiAnalyticsDto> {
  const qs = rangeDays != null ? `?rangeDays=${rangeDays}` : "";
  const res = await fetch(`${API_URL}/api/ai-analytics${qs}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải thống kê AI");
  }
  return res.json() as Promise<AiAnalyticsDto>;
}

// ── AI Marketing Content Assistant (soạn nội dung bài viết/ưu đãi bằng AI) ───

export interface MarketingContentDraftDto {
  title: string;
  content: string;
  suggestedCategory: string;
}

export interface GenerateMarketingContentRequest {
  serviceId?: string;
  promotionId?: string;
  topic?: string;
  tone?: string;
}

export async function generateMarketingContentApi(
  body: GenerateMarketingContentRequest
): Promise<MarketingContentDraftDto> {
  const res = await fetch(`${API_URL}/api/posts/generate-ai-draft`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(body),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể soạn nội dung bằng AI");
  }
  return res.json() as Promise<MarketingContentDraftDto>;
}

// ── Payroll types & endpoints ────────────────────────────────────────────────

export interface PayrollItemDto {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  employeeId: string | null;
  department: string | null;
  position: string | null;
  phoneNumber: string | null;
  baseSalary: number;
  allowance: number;
  leaveShifts: number;
  allowedLeaveShifts: number;
  exceededShifts: number;
  deduction: number;
  bonus: number;
  netSalary: number;
  status: "NotCreated" | "Draft" | "Calculated" | "Approved" | "Paid";
  paidAt: string | null;
  note: string | null;
  hasSalaryConfigured: boolean;
  previousNetSalary: number;
  isCreated: boolean;
}

export interface PayrollSummaryDto {
  totalStaff: number;
  paidCount: number;
  pendingCount: number;
  totalNet: number;
  totalPaid: number;
  totalDeduction: number;
  missingSalaryCount: number;
  previousTotalNet: number;
  notCreatedCount: number;
  draftCount: number;
  calculatedCount: number;
  approvedCount: number;
}

export interface PayrollPeriodDto {
  year: number;
  month: number;
  workingShiftsPerMonth: number;
  summary: PayrollSummaryDto;
  items: PayrollItemDto[];
}

export interface PayrollFailureDto {
  userId: string;
  fullName: string;
  reason: string;
}

export interface PayAllPayrollResult {
  paidCount: number;
  skippedCount: number;
  totalPaid: number;
  alreadyPaidCount: number;
  failures: PayrollFailureDto[];
}

export interface PayrollMonthStatDto {
  month: number;
  staffCount: number;
  paidCount: number;
  totalNet: number;
  totalPaid: number;
  totalDeduction: number;
}

export interface PayrollYearlyDto {
  year: number;
  totalNet: number;
  totalPaid: number;
  totalDeduction: number;
  averageMonthlyNet: number;
  peakMonth: number;
  months: PayrollMonthStatDto[];
}

export async function getPayrollYearlyApi(year: number): Promise<PayrollYearlyDto> {
  const res = await fetch(`${API_URL}/api/payrolls/yearly?year=${year}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải báo cáo lương theo năm");
  }
  return res.json() as Promise<PayrollYearlyDto>;
}

export async function getPayrollPeriodApi(params: {
  year: number;
  month: number;
  search?: string;
  department?: string;
  role?: string;
}): Promise<PayrollPeriodDto> {
  const qs = new URLSearchParams({
    year: String(params.year),
    month: String(params.month),
  });
  if (params.search)     qs.set("search",     params.search);
  if (params.department) qs.set("department", params.department);
  if (params.role)       qs.set("role",       params.role);
  const res = await fetch(`${API_URL}/api/payrolls?${qs.toString()}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải bảng lương");
  }
  return res.json() as Promise<PayrollPeriodDto>;
}

export async function payPayrollApi(data: {
  year: number;
  month: number;
  userId: string;
  note?: string | null;
}): Promise<PayrollItemDto> {
  const res = await fetch(`${API_URL}/api/payrolls/pay`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể chi trả lương");
  }
  return res.json() as Promise<PayrollItemDto>;
}

export async function unpayPayrollApi(data: {
  year: number;
  month: number;
  userId: string;
}): Promise<PayrollItemDto> {
  const res = await fetch(`${API_URL}/api/payrolls/unpay`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể hoàn tác chi trả lương");
  }
  return res.json() as Promise<PayrollItemDto>;
}

export async function payAllPayrollApi(data: {
  year: number;
  month: number;
  note?: string | null;
}): Promise<PayAllPayrollResult> {
  const res = await fetch(`${API_URL}/api/payrolls/pay-all`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể chi trả lương toàn bộ nhân sự");
  }
  return res.json() as Promise<PayAllPayrollResult>;
}

export interface PayrollPeriodActionResult {
  affectedCount: number;
  skippedCount: number;
  failures: PayrollFailureDto[];
}

export async function createPayrollPeriodApi(data: {
  year: number;
  month: number;
}): Promise<PayrollPeriodActionResult> {
  const res = await fetch(`${API_URL}/api/payrolls/periods`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo kỳ lương");
  }
  return res.json() as Promise<PayrollPeriodActionResult>;
}

export async function calculatePayrollPeriodApi(data: {
  year: number;
  month: number;
}): Promise<PayrollPeriodActionResult> {
  const res = await fetch(`${API_URL}/api/payrolls/periods/calculate`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tính lương kỳ này");
  }
  return res.json() as Promise<PayrollPeriodActionResult>;
}

export async function approvePayrollPeriodApi(data: {
  year: number;
  month: number;
}): Promise<PayrollPeriodActionResult> {
  const res = await fetch(`${API_URL}/api/payrolls/periods/approve`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể duyệt kỳ lương");
  }
  return res.json() as Promise<PayrollPeriodActionResult>;
}

export async function setPayrollBonusApi(data: {
  year: number;
  month: number;
  userId: string;
  bonus: number;
}): Promise<PayrollItemDto> {
  const res = await fetch(`${API_URL}/api/payrolls/bonus`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật thưởng");
  }
  return res.json() as Promise<PayrollItemDto>;
}

// ── Bảng lương của tôi (Dentist/Staff tự xem) ────────────────────────────────

export interface MyPayrollPeriodDto {
  year: number;
  month: number;
  workingShiftsPerMonth: number;
  item: PayrollItemDto | null;
}

export interface MyPayrollMonthDto {
  month: number;
  netSalary: number;
  status: "NotCreated" | "Draft" | "Calculated" | "Approved" | "Paid";
  paidAt: string | null;
}

export interface MyPayrollYearlyDto {
  year: number;
  totalNet: number;
  paidCount: number;
  months: MyPayrollMonthDto[];
}

export async function getMyPayrollPeriodApi(params: { year: number; month: number }): Promise<MyPayrollPeriodDto> {
  const qs = new URLSearchParams({ year: String(params.year), month: String(params.month) });
  const res = await fetch(`${API_URL}/api/payrolls/me?${qs.toString()}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải bảng lương");
  }
  return res.json() as Promise<MyPayrollPeriodDto>;
}

export async function getMyPayrollYearlyApi(year: number): Promise<MyPayrollYearlyDto> {
  const res = await fetch(`${API_URL}/api/payrolls/me/yearly?year=${year}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải diễn biến lương theo năm");
  }
  return res.json() as Promise<MyPayrollYearlyDto>;
}

// ── Dentist Reviews APIs ───────────────────────────────────────────────────

export interface DentistReviewItemDto {
  id: string;
  patientName: string;
  rating: number;
  comment: string;
  tags: string[];
  createdAt: string;
  serviceName?: string | null;
}

export interface DentistReviewsResultDto {
  averageRating: number;
  reviewCount: number;
  reviews: DentistReviewItemDto[];
}

export interface PublicDentistDto {
  id: string;
  fullName: string;
  specialization?: string;
  experienceYears?: number;
  biography?: string;
  averageRating: number;
  reviewCount: number;
}

export async function getPublicDentistsApi(): Promise<PublicDentistDto[]> {
  const res = await fetch(`${API_URL}/api/dentists`);
  if (!res.ok) return [];
  return res.json() as Promise<PublicDentistDto[]>;
}

/**
 * Đánh giá của bệnh nhân cho một nha sĩ. NÉM LỖI khi gọi thất bại thay vì trả về danh sách rỗng:
 * "chưa có đánh giá nào" và "không tải được đánh giá" là hai chuyện khác nhau, gộp lại thì màn hình
 * sẽ báo bác sĩ không có đánh giá trong khi thực ra API đang hỏng.
 */
export async function getDentistReviewsApi(dentistId: string): Promise<DentistReviewsResultDto> {
  const res = await fetch(`${API_URL}/api/dentists/${dentistId}/reviews`);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải đánh giá của nha sĩ");
  }
  return res.json() as Promise<DentistReviewsResultDto>;
}

// ── Owner Dashboard APIs ───────────────────────────────────────────────────

export interface OwnerDashboardWeeklyTrendDto {
  dateStr: string;
  dayName: string;
  revenue: number;
  expense: number;
}

export interface OwnerOutstandingEmployeeDto {
  name: string;
  role: string;
  cases: number;
  rating: number;
  status: string;
}

export interface OwnerRatingBreakdownDto {
  averageRating: number;
  totalReviews: number;
  fiveStar: number;
  fourStar: number;
  threeStar: number;
  twoStar: number;
  oneStar: number;
}

export interface OwnerDashboardDto {
  totalRevenue: number;
  revenueGrowthPercent: number;
  totalExpense: number;
  expenseGrowthPercent: number;
  stockExpense: number;
  payrollExpense: number;
  newPatientsCount: number;
  newPatientsThisWeekCount: number;
  weeklyTrend: OwnerDashboardWeeklyTrendDto[];
  ratingStats: OwnerRatingBreakdownDto;
  outstandingEmployees: OwnerOutstandingEmployeeDto[];
}

export async function getOwnerDashboardApi(): Promise<OwnerDashboardDto> {
  const res = await fetch(`${API_URL}/api/owner/dashboard`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải báo cáo Owner Dashboard");
  }
  return res.json() as Promise<OwnerDashboardDto>;
}

// ── Owner: Doanh thu chi tiết (trang riêng, có filter theo khoảng thời gian) ─

export interface OwnerRevenueIncomeItemDto {
  invoiceId: string;
  invoiceNumber: string;
  patientName: string;
  dentistName: string;
  paymentMethod: string;
  amount: number;
  date: string;
}

export interface OwnerRevenueExpenseItemDto {
  id: string;
  category: "supply" | "payroll";
  description: string;
  subDescription: string;
  status: string;
  amount: number;
  date: string;
}

export interface OwnerRevenueReportDto {
  periodStart: string;
  periodEnd: string;
  totalIncome: number;
  totalStockExpense: number;
  totalPayrollExpense: number;
  incomeItems: OwnerRevenueIncomeItemDto[];
  expenseItems: OwnerRevenueExpenseItemDto[];
}

export async function getOwnerRevenueReportApi(from?: string, to?: string): Promise<OwnerRevenueReportDto> {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const qs = params.toString();
  const res = await fetch(`${API_URL}/api/owner/dashboard/revenue${qs ? `?${qs}` : ""}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải báo cáo doanh thu");
  }
  return res.json() as Promise<OwnerRevenueReportDto>;
}

// ── Tài chính: Doanh thu (module mới — KPI/giao dịch/biểu đồ) ────────────────

export interface RevenueSummaryDto {
  totalBilled: number;
  totalCollected: number;
  totalUncollected: number;
  totalRefunded: number;
}

export interface RevenueTransactionDto {
  invoiceId: string;
  invoiceNumber: string;
  patientId: string;
  patientName: string;
  serviceSummary: string;
  dentistId: string;
  dentistName: string;
  date: string;
  paymentMethod: string;
  amount: number;
  status: "Unpaid" | "Paid" | "Refunded";
  // > 0 chỉ khi đây là hóa đơn đặt cọc đã thu "amount" nhưng còn nợ phần này, và chưa có hóa đơn
  // "thu phần còn lại" nào được tạo cho nó.
  remainingAmount: number;
}

export interface RevenueTransactionsPagedDto {
  items: RevenueTransactionDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface RevenueByServiceDto {
  serviceName: string;
  amount: number;
  supplyCost: number;
}

export interface RevenueByDentistDto {
  dentistId: string;
  dentistName: string;
  amount: number;
}

export interface RevenueChartsDto {
  byService: RevenueByServiceDto[];
  byDentist: RevenueByDentistDto[];
}

export async function getRevenueSummaryApi(from: string, to: string): Promise<RevenueSummaryDto> {
  const res = await fetch(`${API_URL}/api/revenue/summary?from=${from}&to=${to}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải tổng quan doanh thu");
  }
  return res.json() as Promise<RevenueSummaryDto>;
}

export async function getRevenueTransactionsApi(params: {
  from: string;
  to: string;
  dentistId?: string;
  serviceName?: string;
  status?: string;
  paymentMethod?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
}): Promise<RevenueTransactionsPagedDto> {
  const qs = new URLSearchParams({ from: params.from, to: params.to });
  if (params.dentistId)   qs.set("dentistId",   params.dentistId);
  if (params.serviceName) qs.set("serviceName", params.serviceName);
  if (params.status)      qs.set("status",      params.status);
  if (params.paymentMethod) qs.set("paymentMethod", params.paymentMethod);
  if (params.search)      qs.set("search",      params.search);
  qs.set("page",     String(params.page ?? 1));
  qs.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy)  qs.set("sortBy",  params.sortBy);
  if (params.sortDir) qs.set("sortDir", params.sortDir);

  const res = await fetch(`${API_URL}/api/revenue/transactions?${qs.toString()}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách giao dịch");
  }
  return res.json() as Promise<RevenueTransactionsPagedDto>;
}

export async function getRevenueChartsApi(from: string, to: string): Promise<RevenueChartsDto> {
  const res = await fetch(`${API_URL}/api/revenue/charts?from=${from}&to=${to}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải biểu đồ doanh thu");
  }
  return res.json() as Promise<RevenueChartsDto>;
}

// ── Tài chính: Chi phí (module mới — CRUD + KPI/biểu đồ + định kỳ) ──────────

export type ExpenseCategory =
  | "Medicine" | "Equipment" | "Rent" | "Utilities"
  | "Marketing" | "Maintenance" | "Software" | "Other";

export type RecurrenceFrequency = "Monthly" | "Quarterly" | "Yearly";

export interface ExpenseDto {
  id: string;
  category: ExpenseCategory;
  description: string;
  amount: number;
  date: string;
  note: string | null;
  isRecurring: boolean;
  frequency: RecurrenceFrequency | null;
  recurringSourceId: string | null;
  createdAt: string;
}

export interface ExpensesPagedDto {
  items: ExpenseDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ExpenseSummaryDto {
  totalExpense: number;
  totalOther: number;
  totalSupply: number;
  totalPayroll: number;
}

export interface ExpenseByCategoryDto {
  categoryLabel: string;
  amount: number;
}

export interface ExpenseChartsDto {
  byCategory: ExpenseByCategoryDto[];
}

export interface ExpenseRequest {
  category: ExpenseCategory;
  description: string;
  amount: number;
  date: string;
  note?: string | null;
  isRecurring: boolean;
  frequency?: RecurrenceFrequency | null;
}

export interface GenerateRecurringExpensesResult {
  generatedCount: number;
}

export async function getExpensesApi(params: {
  from: string;
  to: string;
  category?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
}): Promise<ExpensesPagedDto> {
  const qs = new URLSearchParams({ from: params.from, to: params.to });
  if (params.category) qs.set("category", params.category);
  if (params.search)   qs.set("search",   params.search);
  qs.set("page",     String(params.page ?? 1));
  qs.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy)  qs.set("sortBy",  params.sortBy);
  if (params.sortDir) qs.set("sortDir", params.sortDir);

  const res = await fetch(`${API_URL}/api/expenses?${qs.toString()}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách chi phí");
  }
  return res.json() as Promise<ExpensesPagedDto>;
}

export async function getExpenseSummaryApi(from: string, to: string): Promise<ExpenseSummaryDto> {
  const res = await fetch(`${API_URL}/api/expenses/summary?from=${from}&to=${to}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải tổng quan chi phí");
  }
  return res.json() as Promise<ExpenseSummaryDto>;
}

export async function getExpenseChartsApi(from: string, to: string): Promise<ExpenseChartsDto> {
  const res = await fetch(`${API_URL}/api/expenses/charts?from=${from}&to=${to}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải biểu đồ chi phí");
  }
  return res.json() as Promise<ExpenseChartsDto>;
}

export async function createExpenseApi(data: ExpenseRequest): Promise<ExpenseDto> {
  const res = await fetch(`${API_URL}/api/expenses`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể thêm chi phí");
  }
  return res.json() as Promise<ExpenseDto>;
}

export async function updateExpenseApi(id: string, data: ExpenseRequest): Promise<ExpenseDto> {
  const res = await fetch(`${API_URL}/api/expenses/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật chi phí");
  }
  return res.json() as Promise<ExpenseDto>;
}

export async function deleteExpenseApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/expenses/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể xoá chi phí");
  }
}

export async function generateRecurringExpensesApi(): Promise<GenerateRecurringExpensesResult> {
  const res = await fetch(`${API_URL}/api/expenses/generate-recurring`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể sinh chi phí định kỳ");
  }
  return res.json() as Promise<GenerateRecurringExpensesResult>;
}

// ── Tài chính: Hoa hồng (module mới — quy tắc hoa hồng theo % doanh thu) ─────

export interface CommissionRuleDto {
  id: string;
  dentistId: string | null;
  dentistName: string | null;
  serviceName: string | null;
  ratePercent: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  note: string | null;
  revenueBasis: number;
  commissionAmount: number;
}

export interface CommissionRulesResultDto {
  items: CommissionRuleDto[];
  totalCommission: number;
}

export interface CommissionRuleRequest {
  dentistId?: string | null;
  serviceName?: string | null;
  ratePercent: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  note?: string | null;
}

export async function getCommissionRulesApi(from: string, to: string): Promise<CommissionRulesResultDto> {
  const res = await fetch(`${API_URL}/api/commissions?from=${from}&to=${to}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách quy tắc hoa hồng");
  }
  return res.json() as Promise<CommissionRulesResultDto>;
}

export async function createCommissionRuleApi(data: CommissionRuleRequest): Promise<{ id: string }> {
  const res = await fetch(`${API_URL}/api/commissions`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tạo quy tắc hoa hồng");
  }
  return res.json() as Promise<{ id: string }>;
}

export async function updateCommissionRuleApi(id: string, data: CommissionRuleRequest): Promise<void> {
  const res = await fetch(`${API_URL}/api/commissions/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(data),
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể cập nhật quy tắc hoa hồng");
  }
}

export async function toggleCommissionRuleActiveApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/commissions/${id}/toggle-active`, {
    method: "PUT",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể đổi trạng thái quy tắc hoa hồng");
  }
}

export async function deleteCommissionRuleApi(id: string): Promise<void> {
  const res = await fetch(`${API_URL}/api/commissions/${id}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể xoá quy tắc hoa hồng");
  }
}

// ── Tài chính: Tổng quan (module mới — tổng hợp Doanh thu + Chi phí + Lương) ─

export interface FinanceOverviewDto {
  totalRevenue: number;
  totalExpense: number;
  totalPayroll: number;
  profit: number;
  revenueGrowthPercent: number;
  expenseGrowthPercent: number;
  profitGrowthPercent: number;
  topServices: RevenueByServiceDto[];
  topDentists: RevenueByDentistDto[];
  recentTransactions: RevenueTransactionDto[];
}

export async function getFinanceOverviewApi(from: string, to: string): Promise<FinanceOverviewDto> {
  const res = await fetch(`${API_URL}/api/finance/overview?from=${from}&to=${to}`, {
    headers: { ...authHeaders() },
  });
  await checkAuth(res);
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải tổng quan tài chính");
  }
  return res.json() as Promise<FinanceOverviewDto>;
}
