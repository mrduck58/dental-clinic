"use client";

import React, { useState, useMemo } from "react";
import Sidebar from "../../../components/shared/Sidebar";

// Define TypeScript interfaces for our state management
interface AccountPermissions {
  appointmentsRead: boolean;
  appointmentsWrite: boolean;
  appointmentsCancel: boolean;
  recordsRead: boolean;
  recordsWrite: boolean;
  recordsDelete: boolean;
  revenueView: boolean;
  systemManage: boolean;
}

interface Account {
  id: string;
  name: string;
  email: string;
  phone: string;
  role: "Admin" | "Bác sĩ" | "Lễ tân" | "Kế toán";
  status: "Active" | "Inactive";
  avatar: string;
  permissions: AccountPermissions;
}

// Default permissions helper by role
const getDefaultPermissions = (role: Account["role"]): AccountPermissions => {
  switch (role) {
    case "Admin":
      return {
        appointmentsRead: true,
        appointmentsWrite: true,
        appointmentsCancel: true,
        recordsRead: true,
        recordsWrite: true,
        recordsDelete: true,
        revenueView: true,
        systemManage: true,
      };
    case "Bác sĩ":
      return {
        appointmentsRead: true,
        appointmentsWrite: true,
        appointmentsCancel: false,
        recordsRead: true,
        recordsWrite: true,
        recordsDelete: false,
        revenueView: false,
        systemManage: false,
      };
    case "Lễ tân":
      return {
        appointmentsRead: true,
        appointmentsWrite: true,
        appointmentsCancel: true,
        recordsRead: true,
        recordsWrite: false,
        recordsDelete: false,
        revenueView: false,
        systemManage: false,
      };
    case "Kế toán":
      return {
        appointmentsRead: true,
        appointmentsWrite: false,
        appointmentsCancel: false,
        recordsRead: false,
        recordsWrite: false,
        recordsDelete: false,
        revenueView: true,
        systemManage: false,
      };
  }
};

const initialAccounts: Account[] = [
  {
    id: "ACC-001",
    name: "ThS. BS. Nguyễn Minh Đức",
    email: "minhduc.nguyen@dentalclinic.com",
    phone: "0901234567",
    role: "Admin",
    status: "Active",
    avatar: "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=256&q=80",
    permissions: getDefaultPermissions("Admin"),
  },
  {
    id: "ACC-002",
    name: "BS. Lê Thị Phương Thảo",
    email: "phuongthao.le@dentalclinic.com",
    phone: "0912345678",
    role: "Bác sĩ",
    status: "Active",
    avatar: "https://images.unsplash.com/photo-1591604021695-0c69b7c05981?auto=format&fit=crop&w=256&q=80",
    permissions: getDefaultPermissions("Bác sĩ"),
  },
  {
    id: "ACC-003",
    name: "Nguyễn Thị Lan",
    email: "lan.nguyen@dentalclinic.com",
    phone: "0923456789",
    role: "Lễ tân",
    status: "Active",
    avatar: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=150&q=80",
    permissions: getDefaultPermissions("Lễ tân"),
  },
  {
    id: "ACC-004",
    name: "Trần Văn Hải",
    email: "hai.tran@dentalclinic.com",
    phone: "0934567890",
    role: "Kế toán",
    status: "Active",
    avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=150&q=80",
    permissions: getDefaultPermissions("Kế toán"),
  },
  {
    id: "ACC-005",
    name: "BS. Trần Quốc Bảo",
    email: "quocbao.tran@dentalclinic.com",
    phone: "0945678901",
    role: "Bác sĩ",
    status: "Active",
    avatar: "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=256&q=80",
    permissions: getDefaultPermissions("Bác sĩ"),
  },
  {
    id: "ACC-006",
    name: "Lê Hoàng Long",
    email: "hoanglong.le@dentalclinic.com",
    phone: "0956789012",
    role: "Lễ tân",
    status: "Inactive",
    avatar: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=150&q=80",
    permissions: getDefaultPermissions("Lễ tân"),
  },
];

export default function PermissionsPage() {
  const [accounts, setAccounts] = useState<Account[]>(initialAccounts);
  const [searchQuery, setSearchQuery] = useState("");
  const [roleFilter, setRoleFilter] = useState<string>("All");

  // Modals state
  const [isAddEditModalOpen, setIsAddEditModalOpen] = useState(false);
  const [isPermModalOpen, setIsPermModalOpen] = useState(false);
  const [selectedAccount, setSelectedAccount] = useState<Account | null>(null);

  // Form states
  const [formMode, setFormMode] = useState<"add" | "edit">("add");
  const [formName, setFormName] = useState("");
  const [formEmail, setFormEmail] = useState("");
  const [formPhone, setFormPhone] = useState("");
  const [formRole, setFormRole] = useState<Account["role"]>("Bác sĩ");
  const [formPassword, setFormPassword] = useState("");

  // Custom permissions local state for edit
  const [tempPermissions, setTempPermissions] = useState<AccountPermissions | null>(null);

  // Calculate statistics
  const stats = useMemo(() => {
    const total = accounts.length;
    const active = accounts.filter((a) => a.status === "Active").length;
    const doctors = accounts.filter((a) => a.role === "Bác sĩ").length;
    const staff = accounts.filter((a) => a.role === "Lễ tân" || a.role === "Kế toán").length;

    return { total, active, doctors, staff };
  }, [accounts]);

  // Filtered accounts
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

  // Toggle account status
  const handleToggleStatus = (id: string) => {
    setAccounts((prev) =>
      prev.map((acc) =>
        acc.id === id ? { ...acc, status: acc.status === "Active" ? "Inactive" : "Active" } : acc
      )
    );
  };

  // Open modal to add account
  const openAddModal = () => {
    setFormMode("add");
    setFormName("");
    setFormEmail("");
    setFormPhone("");
    setFormRole("Bác sĩ");
    setFormPassword("");
    setIsAddEditModalOpen(true);
  };

  // Open modal to edit account
  const openEditModal = (account: Account) => {
    setFormMode("edit");
    setSelectedAccount(account);
    setFormName(account.name);
    setFormEmail(account.email);
    setFormPhone(account.phone);
    setFormRole(account.role);
    setFormPassword("••••••••"); // Mask password
    setIsAddEditModalOpen(true);
  };

  // Save Add/Edit form
  const handleSaveAccount = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formName || !formEmail || !formPhone) {
      alert("Vui lòng điền đầy đủ thông tin.");
      return;
    }

    if (formMode === "add") {
      const newAcc: Account = {
        id: `ACC-0${accounts.length + 1}`,
        name: formName,
        email: formEmail,
        phone: formPhone,
        role: formRole,
        status: "Active",
        avatar: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=150&q=80", // default avatar
        permissions: getDefaultPermissions(formRole),
      };
      setAccounts((prev) => [...prev, newAcc]);
    } else if (formMode === "edit" && selectedAccount) {
      setAccounts((prev) =>
        prev.map((acc) =>
          acc.id === selectedAccount.id
            ? {
                ...acc,
                name: formName,
                email: formEmail,
                phone: formPhone,
                role: formRole,
                // If role changes, reset to default permissions of new role
                permissions: acc.role === formRole ? acc.permissions : getDefaultPermissions(formRole),
              }
            : acc
        )
      );
    }
    setIsAddEditModalOpen(false);
  };

  // Open custom permissions modal
  const openPermissionsModal = (account: Account) => {
    setSelectedAccount(account);
    setTempPermissions({ ...account.permissions });
    setIsPermModalOpen(true);
  };

  // Handle individual permission checkbox change
  const handlePermissionChange = (key: keyof AccountPermissions) => {
    if (tempPermissions) {
      setTempPermissions({
        ...tempPermissions,
        [key]: !tempPermissions[key],
      });
    }
  };

  // Save custom permissions
  const handleSavePermissions = () => {
    if (selectedAccount && tempPermissions) {
      setAccounts((prev) =>
        prev.map((acc) =>
          acc.id === selectedAccount.id ? { ...acc, permissions: tempPermissions } : acc
        )
      );
      setIsPermModalOpen(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <Sidebar activeMenu="permissions" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Tài Khoản & Phân Quyền</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Quản lý thành viên hệ thống và cấu hình chi tiết quyền hạn.</p>
          </div>

          {/* Quick User Avatar */}
          <div className="flex items-center gap-3 select-none">
            <div className="w-10 h-10 rounded-full border-2 border-primary/20 bg-red-50/50 flex items-center justify-center font-bold text-primary shrink-0">
              MĐ
            </div>
            <div className="hidden md:block text-left">
              <div className="text-[13px] font-bold text-slate-900 leading-tight">ThS. BS. Nguyễn Minh Đức</div>
              <div className="text-[11px] font-semibold text-slate-400 mt-0.5">Quản trị hệ thống</div>
            </div>
          </div>
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">
          {/* STATS GRID */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            {/* Total accounts */}
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

            {/* Doctors */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Bác sĩ trực</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.doctors}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Khám điều trị chính</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-sky-50 text-secondary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
                </svg>
              </div>
            </div>

            {/* Receptionists / Accountants */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Lễ tân & Kế toán</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.staff}</span>
                <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">Tiếp đón và Thu ngân</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-amber-50 text-accent flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
                </svg>
              </div>
            </div>

            {/* Active Accounts */}
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

          {/* TOOLBAR */}
          <div className="bg-white p-4.5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4 shrink-0">
            {/* Search and Role Filter */}
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3.5 flex-1 max-w-2xl">
              {/* Search */}
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
                  className="w-full pl-9.5 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>

              {/* Role filter */}
              <div className="relative">
                <select
                  value={roleFilter}
                  onChange={(e) => setRoleFilter(e.target.value)}
                  className="w-full sm:w-44 px-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer"
                >
                  <option value="All">Tất cả vai trò</option>
                  <option value="Admin">Admin</option>
                  <option value="Bác sĩ">Bác sĩ</option>
                  <option value="Lễ tân">Lễ tân</option>
                  <option value="Kế toán">Kế toán</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
            </div>

            {/* Add account button */}
            <button
              onClick={openAddModal}
              className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold px-5 py-2.5 rounded-xl shadow-md shadow-primary/20 hover:shadow-lg hover:shadow-primary/30 transition-all hover:translate-y-[-1px] cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              Thêm tài khoản
            </button>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13px] sm:text-[14px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 select-none">
                    <th className="px-6 py-4">Nhân viên</th>
                    <th className="px-6 py-4">Số điện thoại</th>
                    <th className="px-6 py-4">Vai trò</th>
                    <th className="px-6 py-4 text-center">Trạng thái</th>
                    <th className="px-6 py-4 text-right">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-150/70 font-semibold text-slate-600">
                  {filteredAccounts.length > 0 ? (
                    filteredAccounts.map((account) => {
                      // Color badges helper
                      let roleBadgeClass = "";
                      switch (account.role) {
                        case "Admin":
                          roleBadgeClass = "bg-red-50 text-primary border border-red-100";
                          break;
                        case "Bác sĩ":
                          roleBadgeClass = "bg-sky-50 text-secondary border border-sky-100";
                          break;
                        case "Lễ tân":
                          roleBadgeClass = "bg-green-50 text-green-600 border border-green-100";
                          break;
                        case "Kế toán":
                          roleBadgeClass = "bg-amber-50 text-amber-600 border border-amber-100";
                          break;
                      }

                      return (
                        <tr key={account.id} className="hover:bg-slate-50/20 transition-colors">
                          {/* Name & Avatar */}
                          <td className="px-6 py-4.5">
                            <div className="flex items-center gap-3">
                              <img
                                src={account.avatar}
                                alt={account.name}
                                className="w-10 h-10 rounded-full object-cover border border-slate-200 shrink-0 shadow-sm"
                              />
                              <div className="min-w-0">
                                <div className="font-extrabold text-slate-900 truncate">{account.name}</div>
                                <div className="text-[12px] text-slate-400 font-medium truncate mt-0.5">{account.email}</div>
                              </div>
                            </div>
                          </td>

                          {/* Phone */}
                          <td className="px-6 py-4.5 font-bold text-slate-800">
                            {account.phone}
                          </td>

                          {/* Role Tag */}
                          <td className="px-6 py-4.5">
                            <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[12px] font-black ${roleBadgeClass}`}>
                              {account.role}
                            </span>
                          </td>

                          {/* Toggle Switch */}
                          <td className="px-6 py-4.5 text-center">
                            <div className="inline-flex items-center justify-center">
                              <button
                                onClick={() => handleToggleStatus(account.id)}
                                className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                                  account.status === "Active" ? "bg-green-500" : "bg-slate-250"
                                }`}
                              >
                                <span
                                  className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-md ring-0 transition duration-200 ease-in-out ${
                                    account.status === "Active" ? "translate-x-5" : "translate-x-0"
                                  }`}
                                />
                              </button>
                            </div>
                          </td>

                          {/* Action Buttons */}
                          <td className="px-6 py-4.5 text-right">
                            <div className="flex items-center justify-end gap-2.5">
                              {/* Edit details */}
                              <button
                                onClick={() => openEditModal(account)}
                                title="Sửa thông tin"
                                className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                                </svg>
                              </button>

                              {/* Config permissions */}
                              <button
                                onClick={() => openPermissionsModal(account)}
                                title="Cấu hình quyền chi tiết"
                                className="p-2 text-slate-400 hover:text-primary hover:bg-red-50 rounded-lg transition-all cursor-pointer"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.751A11.956 11.956 0 0112 2.714z" />
                                </svg>
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  ) : (
                    <tr>
                      <td colSpan={5} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Không tìm thấy tài khoản nhân viên nào khớp với bộ lọc.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>

      {/* ── MODAL: THÊM / SỬA TÀI KHOẢN ────────────────────────────────────── */}
      {isAddEditModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto animate-fade-in">
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-lg shadow-2xl p-6 relative flex flex-col gap-5">
            {/* Header */}
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h3 className="text-[18px] font-black text-slate-900">
                {formMode === "add" ? "Thêm Tài Khoản Nhân Viên" : "Sửa Tài Khoản Nhân Viên"}
              </h3>
              <button
                onClick={() => setIsAddEditModalOpen(false)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-all cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Form */}
            <form onSubmit={handleSaveAccount} className="flex flex-col gap-4">
              {/* Full Name */}
              <div>
                <label className="block text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-1.5">
                  Họ và tên nhân viên <span className="text-primary">*</span>
                </label>
                <input
                  type="text"
                  required
                  placeholder="Ví dụ: Lê Văn An"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  className="w-full px-4.5 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-800"
                />
              </div>

              {/* Email */}
              <div>
                <label className="block text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-1.5">
                  Địa chỉ Email <span className="text-primary">*</span>
                </label>
                <input
                  type="email"
                  required
                  placeholder="nhanvien@songiangdental.com"
                  value={formEmail}
                  onChange={(e) => setFormEmail(e.target.value)}
                  className="w-full px-4.5 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-800"
                />
              </div>

              {/* Phone and Role (Grid) */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Phone */}
                <div>
                  <label className="block text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-1.5">
                    Số điện thoại <span className="text-primary">*</span>
                  </label>
                  <input
                    type="tel"
                    required
                    placeholder="09xxxxxxxx"
                    value={formPhone}
                    onChange={(e) => setFormPhone(e.target.value)}
                    className="w-full px-4.5 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-800"
                  />
                </div>

                {/* Role */}
                <div>
                  <label className="block text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-1.5">
                    Vai trò hệ thống <span className="text-primary">*</span>
                  </label>
                  <div className="relative">
                    <select
                      value={formRole}
                      onChange={(e) => setFormRole(e.target.value as Account["role"])}
                      className="w-full px-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-700 appearance-none pr-8 cursor-pointer"
                    >
                      <option value="Admin">Admin</option>
                      <option value="Bác sĩ">Bác sĩ</option>
                      <option value="Lễ tân">Lễ tân</option>
                      <option value="Kế toán">Kế toán</option>
                    </select>
                    <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                      </svg>
                    </span>
                  </div>
                </div>
              </div>

              {/* Password */}
              <div>
                <label className="block text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-1.5">
                  Mật khẩu đăng nhập <span className="text-primary">*</span>
                </label>
                <input
                  type="password"
                  required
                  placeholder={formMode === "add" ? "Tối thiểu 6 ký tự" : ""}
                  disabled={formMode === "edit"}
                  value={formPassword}
                  onChange={(e) => setFormPassword(e.target.value)}
                  className="w-full px-4.5 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-800 disabled:opacity-50"
                />
                {formMode === "edit" && (
                  <p className="text-[11px] text-slate-400 font-semibold mt-1">Lưu ý: Chỉ cho phép đặt lại mật khẩu từ bảng quản trị chính.</p>
                )}
              </div>

              {/* Buttons */}
              <div className="flex items-center justify-end gap-3 mt-4 border-t border-slate-100 pt-4">
                <button
                  type="button"
                  onClick={() => setIsAddEditModalOpen(false)}
                  className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all cursor-pointer"
                >
                  {formMode === "add" ? "Tạo tài khoản" : "Cập nhật"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: PHÂN QUYỀN CHI TIẾT ─────────────────────────────────────── */}
      {isPermModalOpen && selectedAccount && tempPermissions && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto animate-fade-in">
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-2xl shadow-2xl p-6 relative flex flex-col gap-5">
            {/* Header */}
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div className="flex items-center gap-3">
                <img
                  src={selectedAccount.avatar}
                  alt={selectedAccount.name}
                  className="w-11 h-11 rounded-full object-cover border border-slate-200 shadow-sm"
                />
                <div>
                  <h3 className="text-[18px] font-black text-slate-900 leading-tight">Phân Quyền Chi Tiết</h3>
                  <p className="text-[12px] text-slate-400 font-bold mt-0.5">
                    Nhân viên: <span className="text-slate-600 font-extrabold">{selectedAccount.name}</span> • Vai trò: <span className="text-primary font-extrabold">{selectedAccount.role}</span>
                  </p>
                </div>
              </div>
              <button
                onClick={() => setIsPermModalOpen(false)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-all cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Matrix of Permissions */}
            <div className="flex flex-col gap-4 max-h-[380px] overflow-y-auto pr-1">
              <div className="bg-slate-50/70 p-3.5 rounded-xl border border-slate-100">
                <p className="text-[12px] text-slate-500 font-bold leading-relaxed">
                  💡 Hệ thống tự động thiết lập các quyền mặc định dựa trên vai trò. Bạn có thể bật/tắt thủ công từng quyền dưới đây để tuỳ chỉnh riêng cho nhân viên này.
                </p>
              </div>

              {/* Grouped Table */}
              <div className="border border-slate-200 rounded-xl overflow-hidden shadow-sm">
                <table className="w-full text-left border-collapse text-[13.5px]">
                  <thead>
                    <tr className="bg-slate-50 font-extrabold text-slate-500 border-b border-slate-250 select-none">
                      <th className="px-5 py-3">Danh mục tính năng</th>
                      <th className="px-4 py-3 text-center w-24">Xem / Đọc</th>
                      <th className="px-4 py-3 text-center w-24">Thêm / Sửa</th>
                      <th className="px-4 py-3 text-center w-24">Hủy / Quản lý</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-150 font-bold text-slate-700">
                    {/* Category: Lịch hẹn */}
                    <tr className="hover:bg-slate-50/30 transition-colors">
                      <td className="px-5 py-3.5">
                        <div className="font-extrabold text-slate-900">Quản lý lịch hẹn</div>
                        <div className="text-[11px] text-slate-400 font-semibold mt-0.5">Đặt lịch khám, điều phối ca trực bác sĩ.</div>
                      </td>
                      {/* Read */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.appointmentsRead}
                          onChange={() => handlePermissionChange("appointmentsRead")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                      {/* Write */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.appointmentsWrite}
                          onChange={() => handlePermissionChange("appointmentsWrite")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                      {/* Cancel */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.appointmentsCancel}
                          onChange={() => handlePermissionChange("appointmentsCancel")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                    </tr>

                    {/* Category: Bệnh án */}
                    <tr className="hover:bg-slate-50/30 transition-colors">
                      <td className="px-5 py-3.5">
                        <div className="font-extrabold text-slate-900">Hồ sơ bệnh án</div>
                        <div className="text-[11px] text-slate-400 font-semibold mt-0.5">Bệnh lịch, chẩn đoán, lịch sử răng và phác đồ.</div>
                      </td>
                      {/* Read */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.recordsRead}
                          onChange={() => handlePermissionChange("recordsRead")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                      {/* Write */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.recordsWrite}
                          onChange={() => handlePermissionChange("recordsWrite")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                      {/* Delete */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.recordsDelete}
                          onChange={() => handlePermissionChange("recordsDelete")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                    </tr>

                    {/* Category: Doanh thu */}
                    <tr className="hover:bg-slate-50/30 transition-colors">
                      <td className="px-5 py-3.5">
                        <div className="font-extrabold text-slate-900">Báo cáo & Doanh thu</div>
                        <div className="text-[11px] text-slate-400 font-semibold mt-0.5">Biểu đồ doanh số ngày/tháng/năm và hóa đơn.</div>
                      </td>
                      {/* View */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.revenueView}
                          onChange={() => handlePermissionChange("revenueView")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                      {/* Write - N/A */}
                      <td className="px-4 py-3.5 text-center text-slate-300 text-[12px] select-none">-</td>
                      {/* Cancel - N/A */}
                      <td className="px-4 py-3.5 text-center text-slate-300 text-[12px] select-none">-</td>
                    </tr>

                    {/* Category: Hệ thống */}
                    <tr className="hover:bg-slate-50/30 transition-colors">
                      <td className="px-5 py-3.5">
                        <div className="font-extrabold text-slate-900">Quản trị hệ thống</div>
                        <div className="text-[11px] text-slate-400 font-semibold mt-0.5">Cài đặt thiết bị, phòng khám và phân quyền.</div>
                      </td>
                      {/* Manage */}
                      <td className="px-4 py-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={tempPermissions.systemManage}
                          onChange={() => handlePermissionChange("systemManage")}
                          className="w-4.5 h-4.5 rounded border-slate-300 text-primary focus:ring-primary cursor-pointer"
                        />
                      </td>
                      {/* Write - N/A */}
                      <td className="px-4 py-3.5 text-center text-slate-300 text-[12px] select-none">-</td>
                      {/* Cancel - N/A */}
                      <td className="px-4 py-3.5 text-center text-slate-300 text-[12px] select-none">-</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            {/* Buttons */}
            <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-4 mt-2">
              <button
                onClick={() => setIsPermModalOpen(false)}
                className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleSavePermissions}
                className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all cursor-pointer"
              >
                Lưu cấu hình
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
