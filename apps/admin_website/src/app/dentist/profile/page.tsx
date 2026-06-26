"use client";

import React, { Suspense } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import ProfilePageContent from "../../../components/shared/ProfilePageContent";
import { useRequireDentist } from "../../../hooks/useRequireDentist";

export default function DentistProfilePage() {
  useRequireDentist();

  return (
    <Suspense fallback={
      <div className="flex min-h-screen bg-slate-50 items-center justify-center">
        <div className="w-12 h-12 rounded-full border-4 border-slate-200 border-t-secondary animate-spin" />
      </div>
    }>
      <ProfilePageContent
        sidebar={<DentistSidebar activeMenu="profile" />}
      />
    </Suspense>
  );
}
