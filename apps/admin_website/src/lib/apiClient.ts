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
  await createStaffApi(data);
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
  await new Promise((resolve) => setTimeout(resolve, 200));
  const list = getMockStaffList();
  return list.map((u) => ({
    id: u.id,
    username: u.username,
    fullName: u.fullName,
    email: u.email,
    phoneNumber: u.phoneNumber,
    role: u.role,
    isActive: u.isActive,
    createdAt: u.createdAt,
  }));
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
}

const LOCAL_STORAGE_KEY = "mock_staff_list";

const INITIAL_STAFF: StaffDto[] = [
  {
    id: "admin-uuid-1111-2222-333333333333",
    username: "admin",
    email: "admin@dentalclinic.com",
    role: "Admin",
    fullName: "Quản Trị Viên",
    phoneNumber: "0999999999",
    isActive: true,
    employeeId: "NV-ADMIN",
    department: "Quản trị",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Tài khoản quản trị hệ thống.",
    createdAt: new Date("2026-01-01").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333334",
    username: "nguyenvanan",
    email: "an.nguyen@dentalclinic.com",
    role: "Doctor",
    fullName: "Nguyễn Văn An",
    phoneNumber: "0912345678",
    isActive: true,
    employeeId: "NV-001",
    department: "Khoa Nội nha",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Bác sĩ chuyên khoa I, 10 năm kinh nghiệm.",
    createdAt: new Date("2026-02-01").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333335",
    username: "tranthingoc",
    email: "ngoc.tran@dentalclinic.com",
    role: "Dentist",
    fullName: "Trần Thị Ngọc",
    phoneNumber: "0987654321",
    isActive: true,
    employeeId: "NV-002",
    department: "Khoa Phục hình",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Chuyên gia cấy ghép Implant.",
    createdAt: new Date("2026-02-05").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333336",
    username: "lehoangnam",
    email: "nam.le@dentalclinic.com",
    role: "Dentist",
    fullName: "Lê Hoàng Nam",
    phoneNumber: "0901234567",
    isActive: true,
    employeeId: "NV-003",
    department: "Khoa Chỉnh nha",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Chuyên sâu niềng răng mắc cài và Invisalign.",
    createdAt: new Date("2026-02-10").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333337",
    username: "phamminhduc",
    email: "duc.pham@dentalclinic.com",
    role: "Doctor",
    fullName: "Phạm Minh Đức",
    phoneNumber: "0934567890",
    isActive: true,
    employeeId: "NV-004",
    department: "Phòng khám chung",
    employmentStatus: "On Leave",
    profilePictureUrl: null,
    professionalNotes: "Đang nghỉ phép quân sự.",
    createdAt: new Date("2026-02-15").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333338",
    username: "hoangthanhhai",
    email: "hai.hoang@dentalclinic.com",
    role: "Staff",
    fullName: "Hoàng Thanh Hải",
    phoneNumber: "0976543210",
    isActive: true,
    employeeId: "NV-005",
    department: "Bộ phận Lễ tân",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Lễ tân trưởng ca sáng.",
    createdAt: new Date("2026-03-01").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333339",
    username: "ngoquockhanh",
    email: "khanh.ngo@dentalclinic.com",
    role: "Staff",
    fullName: "Ngô Quốc Khánh",
    phoneNumber: "0945678901",
    isActive: true,
    employeeId: "NV-006",
    department: "Bộ phận Trợ lý",
    employmentStatus: "Inactive",
    profilePictureUrl: null,
    professionalNotes: "Đã nghỉ việc từ tháng 5/2026.",
    createdAt: new Date("2026-03-15").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333340",
    username: "vuthilan",
    email: "lan.vu@dentalclinic.com",
    role: "Doctor",
    fullName: "Vũ Thị Lan",
    phoneNumber: "0967890123",
    isActive: true,
    employeeId: "NV-007",
    department: "Khoa Phẫu thuật",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Chuyên gia tiểu phẫu răng khôn.",
    createdAt: new Date("2026-03-20").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333341",
    username: "domylinh",
    email: "linh.do@dentalclinic.com",
    role: "Dentist",
    fullName: "Đỗ Mỹ Linh",
    phoneNumber: "0954321098",
    isActive: true,
    employeeId: "NV-008",
    department: "Khoa Răng trẻ em",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Thân thiện với trẻ em.",
    createdAt: new Date("2026-04-01").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333342",
    username: "buivannam",
    email: "nam.bui@dentalclinic.com",
    role: "Dentist",
    fullName: "Bùi Văn Nam",
    phoneNumber: "0923456789",
    isActive: true,
    employeeId: "NV-009",
    department: "Khoa Nha chu",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Điều trị viêm lợi và tủy răng.",
    createdAt: new Date("2026-04-10").toISOString()
  },
  {
    id: "staff-uuid-1111-2222-333333333343",
    username: "phanthioanh",
    email: "oanh.phan@dentalclinic.com",
    role: "Staff",
    fullName: "Phan Thị Oanh",
    phoneNumber: "0911223344",
    isActive: true,
    employeeId: "NV-010",
    department: "Bộ phận CSKH",
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: "Hỗ trợ khách hàng qua hotline.",
    createdAt: new Date("2026-04-15").toISOString()
  }
];

export function getMockStaffList(): StaffDto[] {
  if (typeof window === "undefined") return [];
  const stored = localStorage.getItem(LOCAL_STORAGE_KEY);
  if (!stored) {
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(INITIAL_STAFF));
    return INITIAL_STAFF;
  }
  try {
    return JSON.parse(stored) as StaffDto[];
  } catch {
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(INITIAL_STAFF));
    return INITIAL_STAFF;
  }
}

export function saveMockStaffList(list: StaffDto[]) {
  if (typeof window !== "undefined") {
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(list));
  }
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
  await new Promise((resolve) => setTimeout(resolve, 300));
  let list = getMockStaffList();

  if (params?.search) {
    const q = params.search.toLowerCase().trim();
    list = list.filter(
      (u) =>
        (u.fullName && u.fullName.toLowerCase().includes(q)) ||
        u.username.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q) ||
        (u.phoneNumber && u.phoneNumber.toLowerCase().includes(q)) ||
        (u.employeeId && u.employeeId.toLowerCase().includes(q))
    );
  }

  if (params?.role && params.role !== "All") {
    list = list.filter((u) => u.role.toLowerCase() === params.role!.toLowerCase());
  }

  if (params?.status && params.status !== "All") {
    list = list.filter((u) => u.employmentStatus?.toLowerCase() === params.status!.toLowerCase());
  }

  const totalCount = list.length;

  const allStaff = getMockStaffList();
  const totalDentists = allStaff.filter((u) => u.role === "Dentist").length;
  const totalDoctors = allStaff.filter((u) => u.role === "Doctor").length;
  const totalEmployees = allStaff.length;

  const page = params?.page ?? 1;
  const pageSize = params?.pageSize ?? 10;
  const startIndex = (page - 1) * pageSize;
  const items = list.slice(startIndex, startIndex + pageSize);

  return {
    items,
    totalCount,
    page,
    pageSize,
    statistics: {
      totalDentists,
      totalEmployees,
      totalDoctors,
    },
  };
}

export async function createStaffApi(data: CreateStaffCommand): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 300));
  const list = getMockStaffList();

  if (list.some((u) => u.email.toLowerCase() === data.email.toLowerCase())) {
    throw new Error(`Email '${data.email}' đã được sử dụng bởi tài khoản khác.`);
  }

  if (data.employeeId && list.some((u) => u.employeeId?.toLowerCase() === data.employeeId!.toLowerCase())) {
    throw new Error(`Mã nhân viên '${data.employeeId}' đã tồn tại trong hệ thống.`);
  }

  const newStaff: StaffDto = {
    id: `mock-uuid-${Math.random().toString(36).substr(2, 9)}`,
    username: data.email.split("@")[0].toLowerCase().replace(/[.-]/g, "_"),
    email: data.email,
    role: data.role,
    fullName: data.fullName,
    phoneNumber: data.phoneNumber,
    isActive: true,
    employeeId: data.employeeId ?? null,
    department: data.department ?? null,
    employmentStatus: data.employmentStatus ?? "Active",
    profilePictureUrl: data.profilePictureUrl ?? null,
    professionalNotes: data.professionalNotes ?? null,
    createdAt: new Date().toISOString(),
  };

  list.push(newStaff);
  saveMockStaffList(list);
}

export async function updateStaffApi(id: string, data: UpdateStaffCommand): Promise<StaffDto> {
  await new Promise((resolve) => setTimeout(resolve, 300));
  const list = getMockStaffList();
  const index = list.findIndex((u) => u.id === id);
  if (index === -1) {
    throw new Error("Không tìm thấy tài khoản nhân viên.");
  }

  if (list.some((u) => u.id !== id && u.email.toLowerCase() === data.email.toLowerCase())) {
    throw new Error(`Email '${data.email}' đã được sử dụng bởi một tài khoản khác.`);
  }

  const existing = list[index];
  const updated: StaffDto = {
    ...existing,
    fullName: data.fullName,
    email: data.email,
    phoneNumber: data.phoneNumber,
    role: data.role,
    department: data.department,
    employmentStatus: data.employmentStatus,
    profilePictureUrl: data.profilePictureUrl,
    professionalNotes: data.professionalNotes,
    isActive: data.isActive,
  };

  list[index] = updated;
  saveMockStaffList(list);
  return updated;
}

export async function resetStaffPasswordApi(id: string): Promise<ResetPasswordResponse> {
  await new Promise((resolve) => setTimeout(resolve, 200));
  const list = getMockStaffList();
  const staff = list.find((u) => u.id === id);
  if (!staff) {
    throw new Error("Không tìm thấy tài khoản nhân viên.");
  }

  return {
    id,
    email: staff.email,
    temporaryPassword: `MockPassword123!`,
  };
}


// ── Service types ──────────────────────────────────────────────────────────

export interface ServiceDto {
  id: string;
  name: string;
  category: string;
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
  category: string;
  price: number;
  durationMinutes: number;
  description: string;
  imageUrl?: string | null;
}

export interface UpdateServiceRequest {
  name: string;
  category: string;
  price: number;
  durationMinutes: number;
  description: string;
  imageUrl?: string | null;
}

// ── Service endpoints ──────────────────────────────────────────────────────

export async function getServicesApi(params?: {
  category?: string;
  status?: string;
  search?: string;
}): Promise<ServiceDto[]> {
  const qs = new URLSearchParams();
  if (params?.category) qs.set("category", params.category);
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
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => {
      resolve({ url: reader.result as string });
    };
    reader.onerror = () => {
      reject(new Error("Lỗi đọc file hình ảnh."));
    };
    reader.readAsDataURL(file);
  });
}
