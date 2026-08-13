"use client";

import React, { useEffect } from "react";
import { useRouter } from "next/navigation";

interface ServiceDetailPageProps {
  params: Promise<{ id: string }>;
}

export default function ServiceDetailPage({ params }: ServiceDetailPageProps) {
  const router = useRouter();
  const { id } = React.use(params);

  useEffect(() => {
    router.replace(`/admin/services/edit/${id}`);
  }, [id, router]);

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center font-sans text-slate-500 font-semibold gap-2">
      <svg className="w-5 h-5 animate-spin text-primary" fill="none" viewBox="0 0 24 24">
        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
      </svg>
      <span>Đang chuyển đến trang quản lý dịch vụ...</span>
    </div>
  );
}
