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

export async function getAccountsApi(): Promise<AccountDto[]> {
  const res = await fetch(`${API_URL}/api/auth/accounts`, {
    headers: { "Content-Type": "application/json", ...authHeaders() },
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Không thể tải danh sách tài khoản");
  }
  return res.json() as Promise<AccountDto[]>;
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
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Xóa dịch vụ thất bại");
  }
}

export async function toggleServiceStatusApi(id: string): Promise<ServiceDto> {
  const res = await fetch(`${API_URL}/api/services/${id}/status`, {
    method: "PATCH",
    headers: { ...authHeaders() },
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as { title?: string }).title ?? "Cập nhật trạng thái thất bại");
  }
  return res.json() as Promise<ServiceDto>;
}
