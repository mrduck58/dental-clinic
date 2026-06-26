"use client";

import React, { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import { getStaffApi, getWeekScheduleApi, type StaffDto, type StaffStatsDto, type ScheduleEntryDto } from "../../../lib/apiClient";
import * as XLSX from "xlsx";

// ── Constants ──────────────────────────────────────────────────────────────

type TabKey = "staff" | "doctors";

const TABS: Array<{ key: TabKey; label: string; scopeRoles: string; defaultAddRole: string }> = [
  { key: "doctors", label: "Bác sĩ",     scopeRoles: "Doctor,Dentist", defaultAddRole: "Dentist" },
  { key: "staff",   label: "Nhân viên",  scopeRoles: "Staff",    defaultAddRole: "Staff"   },
];

const ROLE_OPTIONS: Record<TabKey, Array<{ value: string; label: string }>> = {
  staff: [
    { value: "",       label: "Tất cả nhân viên"  },
    { value: "Staff",  label: "Lễ tân / Trợ lý"  },
  ],
  doctors: [
    { value: "",        label: "Tất cả bác sĩ"       },
    { value: "Dentist", label: "Nha sĩ"               },
    { value: "Doctor",  label: "Bác sĩ chuyên khoa"  },
  ],
};

const ROLE_LABELS: Record<string, string> = {
  Admin: "Quản trị viên", Doctor: "Bác sĩ",
  Dentist: "Bác sĩ",      Staff: "Lễ tân / Trợ lý",
};

const ROLE_BADGES: Record<string, string> = {
  Admin:   "bg-purple-50 text-purple-700 border-purple-100",
  Doctor:  "bg-emerald-50 text-emerald-700 border-emerald-100",
  Dentist: "bg-emerald-50 text-emerald-700 border-emerald-100",
  Staff:   "bg-green-50 text-green-700 border-green-100",
};

const STATUS_LABELS: Record<string, string> = {
  Active: "Đang làm việc", "On Leave": "Nghỉ phép", Inactive: "Đã nghỉ việc",
};

const STATUS_BADGES: Record<string, string> = {
  Active:     "bg-green-50 text-green-700 border-green-200",
  "On Leave": "bg-amber-50 text-amber-700 border-amber-200",
  Inactive:   "bg-red-50 text-red-700 border-red-200",
};

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

function getMonday(d: Date): string {
  const day = d.getDay();
  const mon = new Date(d);
  mon.setDate(d.getDate() - (day === 0 ? 6 : day - 1));
  return mon.toISOString().split("T")[0];
}

// ── Page ───────────────────────────────────────────────────────────────────

export default function StaffManagementPage() {
  useRequireAdmin();
  const router = useRouter();

  const [activeTab, setActiveTab]       = useState<TabKey>("doctors");
  const [staffList, setStaffList]       = useState<StaffDto[]>([]);
  const [stats, setStats]               = useState<StaffStatsDto>({ totalDentists: 0, totalEmployees: 0, totalDoctors: 0 });
  const [totalCount, setTotalCount]     = useState(0);
  const [isLoading, setIsLoading]       = useState(true);

  const [searchQuery, setSearchQuery]   = useState("");
  const [roleFilter, setRoleFilter]     = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [currentPage, setCurrentPage]   = useState(1);
  const [pageSize, setPageSize]         = useState(10);

  const [toast, setToast] = useState<{ show: boolean; message: string } | null>(null);
  const [todaySchedule, setTodaySchedule] = useState<ScheduleEntryDto[]>([]);
  const [onLeaveCount, setOnLeaveCount]   = useState({ doctors: 0, staff: 0 });
  const [baseTotal, setBaseTotal]         = useState({ doctors: 0, staff: 0 });

  const tab = TABS.find((t) => t.key === activeTab)!;

  const showToast = (message: string) => {
    setToast({ show: true, message });
    setTimeout(() => setToast(null), 4000);
  };

  useEffect(() => {
    const msg = sessionStorage.getItem("staffSuccessMsg");
    if (msg) { showToast(msg); sessionStorage.removeItem("staffSuccessMsg"); }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchStaff = useCallback(() => {
    setIsLoading(true);
    const effectiveRole = roleFilter || tab.scopeRoles;
    getStaffApi({
      search:   searchQuery || undefined,
      role:     effectiveRole,
      status:   statusFilter !== "All" ? statusFilter : undefined,
      page:     currentPage,
      pageSize,
    })
      .then((res) => {
        setStaffList(res.items);
        setTotalCount(res.totalCount);
        setStats(res.statistics);
      })
      .catch((err) => showToast("Lỗi tải dữ liệu: " + (err instanceof Error ? err.message : "")))
      .finally(() => setIsLoading(false));
  }, [searchQuery, roleFilter, statusFilter, currentPage, pageSize, tab.scopeRoles]);

  useEffect(() => { fetchStaff(); }, [fetchStaff]);

  // Fetch today's schedule + on-leave counts + base totals once on mount
  useEffect(() => {
    const mon = getMonday(new Date());
    const today = new Date().toISOString().split("T")[0];
    getWeekScheduleApi(mon)
      .then(entries => setTodaySchedule(entries.filter(e => e.date === today && !e.isHoliday)))
      .catch(() => {});
    Promise.all([
      getStaffApi({ role: "Doctor,Dentist", status: "On Leave", page: 1, pageSize: 1 }),
      getStaffApi({ role: "Staff",           status: "On Leave", page: 1, pageSize: 1 }),
      getStaffApi({ role: "Doctor,Dentist",                      page: 1, pageSize: 1 }),
      getStaffApi({ role: "Staff",                               page: 1, pageSize: 1 }),
    ]).then(([drLeave, stLeave, drTotal, stTotal]) => {
      setOnLeaveCount({ doctors: drLeave.totalCount, staff: stLeave.totalCount });
      setBaseTotal({ doctors: drTotal.totalCount,    staff: stTotal.totalCount  });
    }).catch(() => {});
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const switchTab = (key: TabKey) => {
    setActiveTab(key);
    setRoleFilter("");
    setStatusFilter("All");
    setSearchQuery("");
    setCurrentPage(1);
  };

  const selectClass =
    "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer";

  // ── Stat cards ─────────────────────────────────────────────────────────

  const workingTodayDoctors = new Set(todaySchedule.filter(e => e.type === "dentist").map(e => e.name)).size;
  const workingTodayStaff   = new Set(todaySchedule.filter(e => e.type === "staff").map(e => e.name)).size;
  const offTodayDoctors     = Math.max(0, baseTotal.doctors - workingTodayDoctors);
  const offTodayStaff       = Math.max(0, baseTotal.staff   - workingTodayStaff);

  const ICON_USERS   = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.109A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" /></svg>;
  const ICON_CHECK   = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>;
  const ICON_OFF     = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z" /></svg>;
  const ICON_LEAVE   = <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>;

  const statCards = activeTab === "staff"
    ? [
        { label: "Tổng nhân viên",      sub: "Lễ tân và trợ lý",           value: baseTotal.staff,      numClass: "text-slate-900",   bg: "bg-slate-50",   iconCls: "text-slate-500",   icon: ICON_USERS  },
        { label: "Làm việc hôm nay",    sub: "Có lịch trong ngày",         value: workingTodayStaff,    numClass: "text-green-700",   bg: "bg-green-50",   iconCls: "text-green-600",   icon: ICON_CHECK  },
        { label: "Nghỉ hôm nay",        sub: "Không có lịch trong ngày",   value: offTodayStaff,        numClass: "text-amber-700",   bg: "bg-amber-50",   iconCls: "text-amber-600",   icon: ICON_OFF    },
        { label: "Đang nghỉ phép",      sub: "Đã đăng ký nghỉ phép",       value: onLeaveCount.staff,   numClass: "text-indigo-700",  bg: "bg-indigo-50",  iconCls: "text-indigo-500",  icon: ICON_LEAVE  },
      ]
    : [
        { label: "Tổng bác sĩ",         sub: "Nha sĩ và bác sĩ chuyên khoa", value: baseTotal.doctors,  numClass: "text-slate-900",   bg: "bg-slate-50",   iconCls: "text-slate-500",   icon: ICON_USERS  },
        { label: "Làm việc hôm nay",    sub: "Có lịch trong ngày",            value: workingTodayDoctors, numClass: "text-green-700", bg: "bg-green-50",   iconCls: "text-green-600",   icon: ICON_CHECK  },
        { label: "Nghỉ hôm nay",        sub: "Không có lịch trong ngày",      value: offTodayDoctors,    numClass: "text-amber-700",  bg: "bg-amber-50",   iconCls: "text-amber-600",   icon: ICON_OFF    },
        { label: "Đang nghỉ phép",      sub: "Đã đăng ký nghỉ phép",          value: onLeaveCount.doctors, numClass: "text-indigo-700", bg: "bg-indigo-50", iconCls: "text-indigo-500", icon: ICON_LEAVE  },
      ];

  const handleExportExcel = () => {
    const rows = staffList.map((u) => [
      u.employeeId || "—", u.fullName || u.username, u.email,
      u.phoneNumber || "—", ROLE_LABELS[u.role] || u.role,
      u.department || "—", STATUS_LABELS[u.employmentStatus || "Active"],
    ]);
    const ws = XLSX.utils.aoa_to_sheet([
      [`DANH SÁCH - ${activeTab === "staff" ? "NHÂN VIÊN" : "BÁC SĨ"}`],
      ["Mã NV", "Họ tên", "Email", "SĐT", "Vai trò", "Bộ phận", "Trạng thái"],
      ...rows,
    ]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, activeTab === "staff" ? "NhanVien" : "BacSi");
    XLSX.writeFile(wb, `${activeTab === "staff" ? "NhanVien" : "BacSi"}.xlsx`);
    showToast("Đã xuất file Excel thành công.");
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="staff" />

      <main className="flex-1 flex flex-col min-w-0">

        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Quản Lý Nhân Sự</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Tra cứu, thêm mới, phân quyền và cập nhật hồ sơ nhân sự phòng khám.
            </p>
          </div>
          <NotificationBell />
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* TOAST */}
          {toast?.show && (
            <div className="fixed top-6 right-6 z-[100] animate-fade-in">
              <div className="bg-white border border-green-200 rounded-2xl shadow-2xl p-4 flex items-center gap-3 max-w-sm">
                <div className="w-9 h-9 rounded-full bg-green-100 flex items-center justify-center shrink-0">
                  <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <span className="text-[13px] font-black text-slate-900 leading-tight">{toast.message}</span>
              </div>
            </div>
          )}

          {/* TABS */}
          <div className="flex items-center gap-1 bg-white border border-slate-200/60 rounded-2xl p-1.5 shadow-sm w-fit shrink-0">
            {TABS.map((t) => (
              <button
                key={t.key}
                onClick={() => switchTab(t.key)}
                className={`px-5 py-2.5 rounded-xl text-[13.5px] font-extrabold transition-all cursor-pointer ${
                  activeTab === t.key
                    ? "bg-primary text-white shadow-md shadow-primary/25"
                    : "text-slate-500 hover:text-slate-800 hover:bg-slate-50"
                }`}
              >
                {t.label}
              </button>
            ))}
          </div>

          {/* STAT CARDS */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 shrink-0">
            {statCards.map((card, i) => (
              <div key={i} className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between transition-all duration-200 hover-lift">
                <div>
                  <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">{card.label}</span>
                  <span className={`text-3xl font-black block mt-1 ${card.numClass}`}>{card.value}</span>
                  <span className="text-[11.5px] text-slate-400 font-semibold block mt-0.5">{card.sub}</span>
                </div>
                <div className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 ${card.bg} ${card.iconCls}`}>
                  {card.icon}
                </div>
              </div>
            ))}
          </div>

          {/* FILTER TOOLBAR */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4 shrink-0">
            <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3.5 flex-wrap">
              {/* Search */}
              <div className="relative flex-1 min-w-[240px]">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm theo tên, mã NV, email, số điện thoại..."
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); setCurrentPage(1); }}
                  className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>

              {/* Role filter — options change per tab */}
              <div className="relative md:w-52">
                <select
                  value={roleFilter}
                  onChange={(e) => { setRoleFilter(e.target.value); setCurrentPage(1); }}
                  className={selectClass}
                >
                  {ROLE_OPTIONS[activeTab].map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Status filter */}
              <div className="relative md:w-48">
                <select
                  value={statusFilter}
                  onChange={(e) => { setStatusFilter(e.target.value); setCurrentPage(1); }}
                  className={selectClass}
                >
                  <option value="All">Tất cả trạng thái</option>
                  <option value="Active">Đang làm việc</option>
                  <option value="On Leave">Nghỉ phép</option>
                  <option value="Inactive">Đã nghỉ việc</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Add */}
              <button
                onClick={() => router.push(activeTab === "doctors" ? "/dashboard/employee/add-doctor" : "/dashboard/employee/add-staff")}
                className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold px-5 py-2.5 rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all hover:translate-y-[-1px] cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm mới
              </button>
            </div>

            {/* Row 2 */}
            <div className="flex items-center justify-between gap-3 flex-wrap border-t border-slate-100 pt-3">
              <div className="flex items-center gap-2.5">
                <span className="text-[12.5px] text-slate-400 font-semibold">Hiển thị</span>
                <div className="relative">
                  <select
                    value={pageSize}
                    onChange={(e) => { setPageSize(Number(e.target.value)); setCurrentPage(1); }}
                    className="pl-3 pr-7 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-bold text-slate-650 appearance-none cursor-pointer"
                  >
                    {PAGE_SIZE_OPTIONS.map((n) => <option key={n} value={n}>{n}</option>)}
                  </select>
                  <span className="absolute inset-y-0 right-2 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </span>
                </div>
                <span className="text-[12.5px] text-slate-400 font-semibold whitespace-nowrap">/ trang</span>
                <span className="text-slate-200">·</span>
                <span className="text-[12.5px] text-slate-400 font-semibold">
                  Tìm thấy <span className="font-bold text-slate-600">{totalCount}</span> kết quả
                </span>
              </div>
              <button
                onClick={handleExportExcel}
                className="flex items-center gap-2 px-4 py-2 bg-white hover:bg-slate-50 text-slate-650 text-[13px] font-bold border border-slate-200 rounded-xl transition-all shadow-sm cursor-pointer"
              >
                <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
                </svg>
                Xuất Excel
              </button>
            </div>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13.5px] min-w-[960px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/85 select-none text-[11px]">
                    <th className="px-5 py-4 w-[230px]">Nhân viên</th>
                    <th className="px-5 py-4 w-[110px]">Mã nhân sự</th>
                    <th className="px-5 py-4">Email</th>
                    <th className="px-5 py-4 w-[140px]">Số điện thoại</th>
                    <th className="px-5 py-4 w-[150px]">Vai trò</th>
                    <th className="px-5 py-4 w-[130px] text-center">Trạng thái</th>
                    <th className="px-5 py-4 w-[110px] text-center">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700 font-semibold">
                  {isLoading ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-16 text-center text-slate-400 font-bold">
                        Đang tải dữ liệu...
                      </td>
                    </tr>
                  ) : staffList.length > 0 ? (
                    staffList.map((item) => {
                      const initials = item.fullName
                        ? item.fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
                        : item.username.slice(0, 2).toUpperCase();
                      return (
                        <tr key={item.id} className="hover:bg-slate-50/30 transition-colors">
                          <td className="px-5 py-4">
                            <div className="flex items-center gap-3">
                              {item.profilePictureUrl ? (
                                <img src={item.profilePictureUrl} alt={item.fullName || item.username}
                                  className="w-10 h-10 rounded-full object-cover border border-slate-200 shadow-sm shrink-0" />
                              ) : (
                                <div className="w-10 h-10 rounded-full bg-slate-100 border border-slate-200/80 flex items-center justify-center font-bold text-[12px] text-slate-500 shrink-0 select-none shadow-inner">
                                  {initials}
                                </div>
                              )}
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 truncate">{item.fullName || item.username}</div>
                                <div className="text-[11.5px] text-slate-400 font-semibold mt-0.5 truncate">{item.department || "Chưa xếp bộ phận"}</div>
                              </div>
                            </div>
                          </td>
                          <td className="px-5 py-4">
                            <span className="font-black text-primary font-mono text-[13px]">{item.employeeId || "—"}</span>
                          </td>
                          <td className="px-5 py-4 font-bold text-slate-800 break-all">{item.email}</td>
                          <td className="px-5 py-4 text-slate-600">{item.phoneNumber || "—"}</td>
                          <td className="px-5 py-4">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${ROLE_BADGES[item.role] || "bg-slate-50 border-slate-200 text-slate-600"}`}>
                              {ROLE_LABELS[item.role] || item.role}
                            </span>
                          </td>
                          <td className="px-5 py-4 text-center">
                            <div className="flex flex-col items-center gap-1.5 justify-center">
                              <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-black border ${STATUS_BADGES[item.employmentStatus || "Active"]}`}>
                                {STATUS_LABELS[item.employmentStatus || "Active"]}
                              </span>
                              {!item.hasAccount && (
                                <span className="text-[10px] text-amber-600 font-bold bg-amber-50 border border-amber-200 px-1.5 py-0.5 rounded-full leading-none whitespace-nowrap">
                                  Chưa có TK
                                </span>
                              )}
                              {item.hasAccount && !item.isActive && (
                                <span className="text-[10px] text-red-500 font-bold bg-red-50 border border-red-100 px-1.5 py-0.5 rounded-full leading-none">
                                  Tài khoản khóa
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="px-5 py-4 text-center">
                            <div className="flex items-center justify-center gap-1">
                              <button
                                onClick={() => {
                                  sessionStorage.setItem("staffDetailData", JSON.stringify(item));
                                  router.push(`/dashboard/employee/detail/${item.id}`);
                                }}
                                title="Xem chi tiết"
                                className="p-2 text-slate-400 hover:text-secondary hover:bg-sky-50 rounded-lg transition-all cursor-pointer"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                </svg>
                              </button>
                              <button
                                onClick={() => {
                                  sessionStorage.setItem("staffEditData", JSON.stringify(item));
                                  router.push(
                                    (item.role === "Doctor" || item.role === "Dentist")
                                      ? `/dashboard/employee/edit-doctor/${item.id}`
                                      : `/dashboard/employee/edit-staff/${item.id}`
                                  );
                                }}
                                title="Chỉnh sửa"
                                className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                                </svg>
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  ) : (
                    <tr>
                      <td colSpan={7} className="px-5 py-12 text-center">
                        <div className="flex flex-col items-center gap-2">
                          <svg className="w-10 h-10 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
                          </svg>
                          <div className="font-extrabold text-[14px] text-slate-500">Không tìm thấy kết quả phù hợp.</div>
                          <div className="text-[12px] text-slate-400 font-semibold">Thử thay đổi từ khóa hoặc bộ lọc.</div>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {!isLoading && totalCount > 0 && (
              <div className="border-t border-slate-100 px-5 py-3.5 flex flex-col sm:flex-row items-center justify-between gap-3 bg-slate-50/25">
                <span className="text-[12.5px] text-slate-400 font-semibold">
                  Hiển thị <span className="font-black text-slate-600">{(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, totalCount)}</span> trong{" "}
                  <span className="font-black text-slate-600">{totalCount}</span> kết quả
                </span>
                {Math.ceil(totalCount / pageSize) > 1 && (
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      disabled={currentPage === 1}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                      </svg>
                    </button>
                    {Array.from({ length: Math.ceil(totalCount / pageSize) }, (_, i) => i + 1).map((page) => (
                      <button
                        key={page}
                        onClick={() => setCurrentPage(page)}
                        className={`w-9 h-9 text-[13px] font-bold rounded-xl transition-all cursor-pointer ${page === currentPage ? "bg-primary text-white shadow-md shadow-primary/20" : "text-slate-500 hover:bg-slate-100"}`}
                      >
                        {page}
                      </button>
                    ))}
                    <button
                      onClick={() => setCurrentPage((p) => Math.min(Math.ceil(totalCount / pageSize), p + 1))}
                      disabled={currentPage === Math.ceil(totalCount / pageSize)}
                      className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                      </svg>
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </main>

    </div>
  );
}
