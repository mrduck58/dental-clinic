"use client";

import React, { Suspense } from "react";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import ProfilePageContent from "../../../components/shared/ProfilePageContent";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";

export default function AdminProfilePage() {
  useRequireAdmin();

  return (
    <Suspense fallback={
      <div className="flex min-h-screen bg-slate-50 items-center justify-center">
        <div className="w-12 h-12 rounded-full border-4 border-slate-200 border-t-primary animate-spin" />
      </div>
    }>
      <ProfilePageContent
        sidebar={<AdminSidebar activeMenu="profile" />}
      />
    </Suspense>
  );
}
