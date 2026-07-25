"use client";

import React, { Suspense } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import ProfilePageContent from "../../../components/shared/ProfilePageContent";
import { useRequireStaff } from "../../../hooks/useRequireStaff";

export default function StaffProfilePage() {
  useRequireStaff();

  return (
    <Suspense fallback={
      <div className="flex min-h-screen bg-slate-50 items-center justify-center">
        <div className="w-12 h-12 rounded-full border-4 border-slate-200 border-t-emerald-500 animate-spin" />
      </div>
    }>
      <ProfilePageContent
        sidebar={<StaffSidebar activeMenu="profile" />}
        notificationHref="/staff/notifications"
      />
    </Suspense>
  );
}
