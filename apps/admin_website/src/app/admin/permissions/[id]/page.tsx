"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import AdminPageHeader from "../../../../components/shared/AdminPageHeader";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import { getAccountsApi, toggleAccountStatusApi, type AccountDto } from "../../../../lib/apiClient";
import { normalizeRole, ROLE_LABELS, ROLE_BADGE_CLASSES, type UiRole } from "../../../../lib/roles";

// Danh sách "Người Dùng" giờ liệt kê cả bệnh nhân — UiRole (lib/roles.ts) cố tình không có
// "Patient" vì các màn hình khác chỉ liệt kê nhân sự, nên xử lý riêng ở đây.
type PageRole = UiRole | "Patient";
const ROLE_LABEL: Record<PageRole, string> = { ...ROLE_LABELS, Patient: "Bệnh nhân" };
const ROLE_BADGE: Record<PageRole, string> = { ...ROLE_BADGE_CLASSES, Patient: "bg-slate-100 text-slate-600 border border-slate-200" };
function normalizePageRole(raw: string): PageRole {
  return raw === "Patient" ? "Patient" : normalizeRole(raw);
}

interface AccountDetailPageProps {
  params: Promise<{ id: string }>;
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return (parts[parts.length - 1]?.[0] ?? "?").toUpperCase();
}

function FieldRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="flex gap-3 py-3 border-b border-slate-50 last:border-0">
      <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider w-40 shrink-0 pt-0.5">{label}</span>
      <span className="text-[13.5px] font-semibold text-slate-800 flex-1">{value || "—"}</span>
    </div>
  );
}

export default function AccountDetailPage({ params }: AccountDetailPageProps) {
  useRequireAdmin();
  const { id } = React.use(params);

  const [account, setAccount] = useState<AccountDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [togglingStatus, setTogglingStatus] = useState(false);

  // Không có endpoint lấy 1 tài khoản theo id — tận dụng luôn danh sách đã có, vì quy mô tài
  // khoản nhân sự (không tính bệnh nhân) nhỏ, không đáng thêm 1 endpoint riêng chỉ cho trang này.
  useEffect(() => {
    getAccountsApi()
      .then((accounts) => {
        const found = accounts.find((a) => a.id === id) ?? null;
        setAccount(found);
        if (!found) setError("Không tìm thấy tài khoản.");
      })
      .catch((err) => setError(err instanceof Error ? err.message : "Không thể tải thông tin tài khoản"))
      .finally(() => setIsLoading(false));
  }, [id]);

  const handleToggleStatus = async () => {
    if (!account) return;
    setTogglingStatus(true);
    try {
      const updated = await toggleAccountStatusApi(account.id);
      setAccount(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể cập nhật trạng thái tài khoản");
    } finally {
      setTogglingStatus(false);
    }
  };

  const backButton = (
    <Link
      href="/admin/permissions"
      className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
    >
      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
      </svg>
    </Link>
  );

  if (isLoading) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="permissions-users" />
        <main className="flex-1 flex flex-col min-w-0">
          <AdminPageHeader title="Chi Tiết Tài Khoản" subtitle="Đang tải..." left={backButton} />
          <div className="p-8 flex-1 flex items-center justify-center">
            <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
          </div>
        </main>
      </div>
    );
  }

  if (error || !account) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="permissions-users" />
        <main className="flex-1 flex flex-col min-w-0">
          <AdminPageHeader title="Chi Tiết Tài Khoản" subtitle="Không tìm thấy tài khoản" left={backButton} />
          <div className="p-8 flex-1 flex items-center justify-center">
            <p className="text-slate-500 font-semibold">{error || "Tài khoản không tồn tại."}</p>
          </div>
        </main>
      </div>
    );
  }

  const role = normalizePageRole(account.role);
  const realName = account.fullName || account.username;
  const name = realName || "Chưa cập nhật tên";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="permissions-users" />

      <main className="flex-1 flex flex-col min-w-0">
        <AdminPageHeader
          title="Chi Tiết Tài Khoản"
          subtitle="Thông tin đăng nhập và vai trò của tài khoản."
          left={backButton}
        />

        <div className="p-8 flex-1 overflow-y-auto flex justify-center">
          <div className="w-full max-w-3xl flex flex-col gap-6">

            {/* Profile header */}
            <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
              <div className="flex items-start gap-5">
                <div className="w-20 h-20 rounded-2xl bg-red-50 text-primary font-black flex items-center justify-center shrink-0 border border-red-100 text-2xl">
                  {initials(realName ?? "")}
                </div>
                <div className="flex-1 min-w-0">
                  <h2 className={`text-2xl font-black tracking-tight truncate ${realName ? "text-slate-900" : "text-slate-400 italic"}`}>
                    {name}
                  </h2>
                  <div className="flex flex-wrap items-center gap-2 mt-2">
                    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border ${ROLE_BADGE[role]}`}>
                      {ROLE_LABEL[role]}
                    </span>
                    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border ${
                      account.isActive
                        ? "bg-green-50 text-green-700 border-green-200"
                        : "bg-red-50 text-red-600 border-red-200"
                    }`}>
                      {account.isActive ? "Đang kích hoạt" : "Đang bị khóa"}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Thông tin tài khoản */}
            <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
              <div className="flex items-center gap-2 mb-2">
                <div className="w-1 h-4 bg-primary rounded-full" />
                <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin tài khoản</span>
              </div>
              <FieldRow label="Họ và tên" value={account.fullName} />
              <FieldRow label="Username" value={account.username} />
              <FieldRow label="Email" value={account.email} />
              <FieldRow label="Số điện thoại" value={account.phoneNumber} />
              <FieldRow
                label="Ngày tạo tài khoản"
                value={new Date(account.createdAt).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" })}
              />
            </div>

            {/* Hành động */}
            <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 flex items-center justify-between gap-4">
              <div>
                <div className="text-[13.5px] font-black text-slate-900">Trạng thái đăng nhập</div>
                <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">
                  {account.isActive ? "Tài khoản có thể đăng nhập bình thường." : "Tài khoản đang bị khóa, không thể đăng nhập."}
                </p>
              </div>
              <button
                onClick={() => void handleToggleStatus()}
                disabled={togglingStatus}
                className={`shrink-0 px-5 py-2.5 rounded-xl text-[13px] font-black transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed ${
                  account.isActive
                    ? "bg-red-50 text-red-600 border border-red-200 hover:bg-red-100"
                    : "bg-green-50 text-green-700 border border-green-200 hover:bg-green-100"
                }`}
              >
                {togglingStatus ? "Đang xử lý..." : account.isActive ? "Khóa tài khoản" : "Mở khóa tài khoản"}
              </button>
            </div>

          </div>
        </div>
      </main>
    </div>
  );
}
