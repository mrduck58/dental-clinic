"use client";

import { useState, useMemo, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense } from "react";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { getAccountsApi, getStaffApi, toggleAccountStatusApi, type AccountDto, type StaffDto } from "../../../lib/apiClient";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";

type UiRole = "Admin" | "Owner" | "Dentist" | "Staff";

const ROLE_LABEL: Record<UiRole, string> = {
  Admin: "Admin",
  Owner: "Chủ cửa hàng",
  Dentist: "Bác sĩ",
  Staff: "Nhân viên",
};

const ROLE_BADGE: Record<UiRole, string> = {
  Admin: "bg-purple-50 text-purple-700 border border-purple-200",
  Owner: "bg-amber-50 text-amber-700 border border-amber-200",
  Dentist: "bg-sky-50 text-sky-700 border border-sky-200",
  Staff: "bg-green-50 text-green-700 border border-green-200",
};

function normalizeRole(raw: string): UiRole {
  if (raw === "Admin" || raw === "Owner" || raw === "Staff") return raw;
  if (raw === "Dentist" || raw === "Doctor") return "Dentist";
  return "Staff";
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return (parts[parts.length - 1]?.[0] ?? "?").toUpperCase();
}

const PAGE_SIZE_OPTIONS = [5, 10, 20];

interface Row {
  id: string;
  name: string;
  email: string;
  phone: string;
  role: UiRole;
  isActive: boolean;
  createdAt: string;
}

function UsersPageContent() {
  useRequireAdmin();
  const router = useRouter();
  const searchParams = useSearchParams();

  const [accounts, setAccounts] = useState<Row[]>([]);
  const [isFetching, setIsFetching] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [roleFilter, setRoleFilter] = useState<string>(searchParams.get("role") ?? "All");
  const [pageSize, setPageSize] = useState(5);
  const [currentPage, setCurrentPage] = useState(1);

  const [employeesWithoutAccount, setEmployeesWithoutAccount] = useState<StaffDto[]>([]);
  const [noAccountBannerOpen, setNoAccountBannerOpen] = useState(true);

  const [toast, setToast] = useState<{ show: boolean; message: string } | null>(null);
  const showToast = (message: string) => {
    setToast({ show: true, message });
    setTimeout(() => setToast(null), 6000);
  };

  const loadEmployeesWithoutAccount = () => {
    getStaffApi({ role: "Doctor,Dentist,Staff,Admin", pageSize: 200 })
      .then((res) => setEmployeesWithoutAccount(res.items.filter((s) => !s.hasAccount)))
      .catch(() => {});
  };

  const mapToRows = (data: AccountDto[]): Row[] =>
    data
      .filter((u) => u.role !== "Patient")
      .map((u) => ({
        id: u.id,
        name: u.fullName ?? u.username,
        email: u.email,
        phone: u.phoneNumber ?? "",
        role: normalizeRole(u.role),
        isActive: u.isActive,
        createdAt: u.createdAt,
      }));

  useEffect(() => {
    getAccountsApi()
      .then((data) => setAccounts(mapToRows(data)))
      .catch((err: unknown) =>
        setFetchError(err instanceof Error ? err.message : "Không thể tải danh sách tài khoản")
      )
      .finally(() => setIsFetching(false));

    loadEmployeesWithoutAccount();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const msg = sessionStorage.getItem("permSuccessMsg");
    if (msg) { showToast(msg); sessionStorage.removeItem("permSuccessMsg"); }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleGoToCreateAccount = (staff: StaffDto) => {
    sessionStorage.setItem("createAccountPrefill", JSON.stringify({
      staffId: staff.id,
      fullName: staff.fullName || staff.email,
      email: staff.email,
      role: staff.role,
      phoneNumber: staff.phoneNumber || "",
      profilePictureUrl: staff.profilePictureUrl,
    }));
    router.push("/admin/permissions/create-account");
  };

  const stats = useMemo(() => {
    const total = accounts.length;
    const active = accounts.filter((a) => a.isActive).length;
    const doctors = accounts.filter((a) => a.role === "Dentist").length;
    const staff = accounts.filter((a) => a.role === "Staff").length;
    return { total, active, doctors, staff };
  }, [accounts]);

  const filteredAccounts = useMemo(() => {
    return accounts.filter((account) => {
      const matchesSearch =
        account.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        account.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
        account.phone.includes(searchQuery);
      const matchesRole = roleFilter === "All" || account.role === roleFilter;
      return matchesSearch && matchesRole;
    });
  }, [accounts, searchQuery, roleFilter]);

  const filterKey = `${searchQuery}|${roleFilter}|${pageSize}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (filterKey !== prevFilterKey) {
    setPrevFilterKey(filterKey);
    setCurrentPage(1);
  }

  const totalCount = filteredAccounts.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const safePage = Math.min(currentPage, totalPages);
  const startIndex = (safePage - 1) * pageSize;
  const pagedAccounts = filteredAccounts.slice(startIndex, startIndex + pageSize);

  const handleToggleStatus = async (id: string) => {
    try {
      const updated = await toggleAccountStatusApi(id);
      setAccounts((prev) =>
        prev.map((acc) => (acc.id === id ? { ...acc, isActive: updated.isActive } : acc))
      );
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể cập nhật trạng thái tài khoản");
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="permissions-users" />

      <main className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Người Dùng</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Danh sách tài khoản nhân sự trong hệ thống.</p>
          </div>
          <NotificationBell />
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">
          {toast?.show && (
            <div className="fixed top-6 right-6 z-[100] animate-fade-in">
              <div className="bg-white border border-green-200 rounded-2xl shadow-xl shadow-slate-200/60 p-4 flex items-start gap-3.5 max-w-sm">
                <div className="w-9 h-9 rounded-full bg-green-100 flex items-center justify-center shrink-0 mt-0.5">
                  <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-black text-slate-900">Thành công!</div>
                  <p className="text-[12px] text-slate-500 font-semibold mt-0.5 leading-relaxed break-all">
                    {toast.message}
                  </p>
                </div>
                <button onClick={() => setToast(null)} className="text-slate-300 hover:text-slate-500 shrink-0 cursor-pointer">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>
          )}

          {/* STATS GRID */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng nhân sự</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.total}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Tài khoản nhân sự</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Bác sĩ</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.doctors}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Khám điều trị chính</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-sky-50 text-secondary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Nhân viên</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.staff}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Tiếp đón và hỗ trợ</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-amber-50 text-accent flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Đang hoạt động</span>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-3xl font-black text-slate-900 leading-none">{stats.active}</span>
                  <span className="relative flex h-3.5 w-3.5">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-3.5 w-3.5 bg-green-500"></span>
                  </span>
                </div>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Có quyền đăng nhập</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-green-50 text-green-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.751A11.956 11.956 0 0112 2.714z" />
                </svg>
              </div>
            </div>
          </div>

          {/* EMPLOYEES WITHOUT ACCOUNT BANNER */}
          {employeesWithoutAccount.length > 0 && (
            <div className="bg-amber-50 border border-amber-200 rounded-2xl shadow-sm overflow-hidden shrink-0">
              <button
                onClick={() => setNoAccountBannerOpen((v) => !v)}
                className="w-full flex items-center justify-between px-5 py-4 hover:bg-amber-100/50 transition-colors cursor-pointer"
              >
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-amber-100 text-amber-600 flex items-center justify-center shrink-0">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                    </svg>
                  </div>
                  <div className="text-left">
                    <div className="text-[14px] font-black text-amber-900">
                      {employeesWithoutAccount.length} nhân viên chưa có tài khoản đăng nhập
                    </div>
                    <div className="text-[12px] text-amber-700 font-semibold mt-0.5">
                      Bấm <span className="font-black">Tạo ngay</span> để tạo tài khoản và gửi thông tin đăng nhập qua email.
                    </div>
                  </div>
                </div>
                <svg className={`w-4 h-4 text-amber-500 shrink-0 transition-transform ${noAccountBannerOpen ? "rotate-180" : ""}`} fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                </svg>
              </button>

              {noAccountBannerOpen && (
                <div className="border-t border-amber-200 overflow-x-auto">
                  <table className="w-full text-left border-collapse text-[13px]">
                    <thead>
                      <tr className="bg-amber-100/60 font-extrabold text-amber-700 uppercase tracking-wider text-[11px]">
                        <th className="px-5 py-2.5">Nhân viên</th>
                        <th className="px-5 py-2.5">Vai trò</th>
                        <th className="px-5 py-2.5">Email</th>
                        <th className="px-5 py-2.5 text-right">Hành động</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-amber-100 font-semibold text-slate-700">
                      {employeesWithoutAccount.map((emp) => {
                        const empInitials = emp.fullName
                          ? emp.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
                          : emp.email.slice(0, 2).toUpperCase();
                        const role = normalizeRole(emp.role);
                        return (
                          <tr key={emp.id} className="hover:bg-amber-50/60 transition-colors">
                            <td className="px-5 py-3">
                              <div className="flex items-center gap-2.5">
                                {emp.profilePictureUrl ? (
                                  <img src={emp.profilePictureUrl} alt={emp.fullName || emp.email}
                                    className="w-8 h-8 rounded-full object-cover border border-amber-200 shrink-0" />
                                ) : (
                                  <div className="w-8 h-8 rounded-full bg-amber-100 text-amber-700 font-black text-[11px] flex items-center justify-center shrink-0">
                                    {empInitials}
                                  </div>
                                )}
                                <span className="font-bold text-slate-800 truncate max-w-[140px]">{emp.fullName || emp.email}</span>
                              </div>
                            </td>
                            <td className="px-5 py-3">
                              <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-black ${ROLE_BADGE[role]}`}>
                                {ROLE_LABEL[role]}
                              </span>
                            </td>
                            <td className="px-5 py-3 text-slate-500 font-semibold text-[12px]">{emp.email}</td>
                            <td className="px-5 py-3 text-right">
                              <button
                                onClick={() => handleGoToCreateAccount(emp)}
                                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-amber-500 hover:bg-amber-600 text-white text-[12px] font-extrabold rounded-lg shadow-sm transition-all cursor-pointer"
                              >
                                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                                </svg>
                                Tạo ngay
                              </button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {/* TOOLBAR */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3">
            {/* Row 1: search + filter + create */}
            <div className="flex flex-col md:flex-row md:items-center gap-3">
              <div className="relative flex-1">
                <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm theo tên, email, số điện thoại..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-9.5 pr-4 py-2.5 text-[14px] bg-slate-100/80 border border-transparent rounded-xl focus:bg-white focus:border-slate-200 focus:outline-none transition-all font-semibold"
                />
              </div>
              <div className="relative shrink-0">
                <select
                  value={roleFilter}
                  onChange={(e) => setRoleFilter(e.target.value)}
                  className="appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-9 py-2.5 rounded-xl border border-slate-200 focus:outline-none transition-all cursor-pointer"
                >
                  <option value="All">Tất cả vai trò</option>
                  <option value="Admin">Admin</option>
                  <option value="Owner">Chủ cửa hàng</option>
                  <option value="Dentist">Bác sĩ</option>
                  <option value="Staff">Nhân viên</option>
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </div>
              </div>
              <button
                onClick={() => router.push("/admin/permissions/create-account")}
                className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] px-5 py-2.5 rounded-xl transition-all shadow-md shadow-primary/25 shrink-0"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm tài khoản
              </button>
            </div>

            {/* Row 2: per-page + result count */}
            <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
              <span>Hiển thị</span>
              <div className="relative">
                <select
                  value={pageSize}
                  onChange={(e) => setPageSize(Number(e.target.value))}
                  className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer"
                >
                  {PAGE_SIZE_OPTIONS.map((n) => (
                    <option key={n} value={n}>{n}</option>
                  ))}
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                  <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </div>
              </div>
              <span>/ trang</span>
              <span className="text-slate-300 mx-1">·</span>
              <span>
                Tìm thấy{" "}
                <span className="text-slate-600 font-bold">{totalCount}</span>
                {" "}kết quả
              </span>
            </div>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[14px]">
                <thead>
                  <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
                    <th className="px-6 py-4">Nhân viên</th>
                    <th className="px-6 py-4">Liên hệ</th>
                    <th className="px-6 py-4">Ngày tạo tài khoản</th>
                    <th className="px-6 py-4">Vai trò</th>
                    <th className="px-6 py-4 text-center">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                  {isFetching ? (
                    <tr>
                      <td colSpan={5} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Đang tải danh sách tài khoản...
                      </td>
                    </tr>
                  ) : fetchError ? (
                    <tr>
                      <td colSpan={5} className="px-6 py-10 text-center text-red-500 font-bold">
                        {fetchError}
                      </td>
                    </tr>
                  ) : pagedAccounts.length > 0 ? (
                    pagedAccounts.map((account) => (
                      <tr key={account.id} className="hover:bg-slate-50/40 transition-colors">
                        <td className="px-6 py-4.5">
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-full bg-red-50 text-primary font-black flex items-center justify-center shrink-0 border border-red-100">
                              {initials(account.name)}
                            </div>
                            <div className="min-w-0">
                              <div className="font-extrabold text-slate-900 truncate">{account.name}</div>
                            </div>
                          </div>
                        </td>

                        <td className="px-6 py-4.5">
                          <div className="font-bold text-slate-800 text-[13px]">{account.phone || "—"}</div>
                          <div className="text-[11px] text-slate-400 font-medium mt-0.5 truncate max-w-[160px]">{account.email}</div>
                        </td>

                        <td className="px-6 py-4.5 font-bold text-slate-600 text-[13px]">
                          {new Date(account.createdAt).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" })}
                        </td>

                        <td className="px-6 py-4.5">
                          <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[12px] font-black ${ROLE_BADGE[account.role]}`}>
                            {ROLE_LABEL[account.role]}
                          </span>
                        </td>

                        <td className="px-6 py-4.5 text-center">
                          <div className="inline-flex items-center justify-center">
                            <button
                              onClick={() => void handleToggleStatus(account.id)}
                              className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                                account.isActive ? "bg-green-500" : "bg-slate-400"
                              }`}
                            >
                              <span
                                className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-md ring-0 transition duration-200 ease-in-out ${
                                  account.isActive ? "translate-x-5" : "translate-x-0"
                                }`}
                              />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={5} className="px-6 py-10 text-center text-slate-400 font-bold">
                        {accounts.length === 0
                          ? "Chưa có tài khoản nào. Hãy thêm tài khoản đầu tiên."
                          : "Không tìm thấy tài khoản nhân viên nào khớp với bộ lọc."}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination bar */}
            {totalCount > 0 && (
              <div className="p-4 border-t border-slate-100 flex items-center justify-between gap-2.5">
                <span className="text-[13px] text-slate-400 font-semibold">
                  Hiển thị{" "}
                  <span className="text-slate-600 font-bold">{startIndex + 1}–{Math.min(startIndex + pageSize, totalCount)}</span>
                  {" "}trong{" "}
                  <span className="text-slate-600 font-bold">{totalCount}</span>
                  {" "}tài khoản
                </span>
                <div className="flex items-center gap-2.5">
                  <button
                    onClick={() => setCurrentPage(1)}
                    disabled={safePage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      safePage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;|
                  </button>
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={safePage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      safePage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;
                  </button>

                  {Array.from({ length: totalPages }).map((_, idx) => {
                    const p = idx + 1;
                    const isActive = safePage === p;
                    return (
                      <button
                        key={p}
                        onClick={() => setCurrentPage(p)}
                        className={`w-9 h-9 rounded-xl border flex items-center justify-center font-extrabold text-[14px] transition-all cursor-pointer ${
                          isActive
                            ? "bg-white border-primary text-primary shadow-sm font-black"
                            : "border-slate-200 text-slate-600 hover:bg-slate-50"
                        }`}
                      >
                        {p}
                      </button>
                    );
                  })}

                  <button
                    onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                    disabled={safePage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      safePage === totalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &gt;
                  </button>
                  <button
                    onClick={() => setCurrentPage(totalPages)}
                    disabled={safePage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      safePage === totalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    |&gt;
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

export default function PermissionsPage() {
  return (
    <Suspense fallback={<div className="p-8 text-center font-bold text-slate-500">Đang tải...</div>}>
      <UsersPageContent />
    </Suspense>
  );
}
