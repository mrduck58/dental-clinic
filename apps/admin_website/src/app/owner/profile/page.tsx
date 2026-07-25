"use client";

import React, { Suspense } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import ProfilePageContent from "../../../components/shared/ProfilePageContent";
import { useRequireOwner } from "../../../hooks/useRequireOwner";

export default function OwnerProfilePage() {
  useRequireOwner();

  return (
    <Suspense fallback={
      <div className="flex min-h-screen bg-slate-50 items-center justify-center">
        <div className="w-12 h-12 rounded-full border-4 border-slate-200 border-t-emerald-500 animate-spin" />
      </div>
    }>
      <ProfilePageContent
        sidebar={<OwnerSidebar activeMenu="profile" />}
        notificationHref="/owner/notifications"
      />
    </Suspense>
  );
}
