"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import { getServiceByIdApi, type ServiceDto } from "../../../../lib/apiClient";

interface ServiceDetailPageProps {
  params: Promise<{ id: string }>;
}

export default function ServiceDetailPage({ params }: ServiceDetailPageProps) {
  useRequireAdmin();
  const { id } = React.use(params);

  const [service, setService] = useState<ServiceDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getServiceByIdApi(id)
      .then(setService)
      .catch(() => setError("Không thể tải thông tin dịch vụ."))
      .finally(() => setIsLoading(false));
  }, [id]);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
      minimumFractionDigits: 0,
    }).format(price);
  };

  if (isLoading) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="services" />
        <main className="flex-1 flex flex-col min-w-0">
          <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
            <div className="flex items-center gap-4">
              <Link
                href="/admin/services"
                className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
              </Link>
              <div>
                <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chi tiết dịch vụ</h1>
                <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Đang tải thông tin...</p>
              </div>
            </div>
            <NotificationBell />
          </header>
          <div className="p-8 flex-1 flex items-center justify-center">
            <div className="flex flex-col items-center gap-4">
              <svg className="animate-spin w-10 h-10 text-primary" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <p className="text-slate-400 font-semibold">Đang tải thông tin dịch vụ...</p>
            </div>
          </div>
        </main>
      </div>
    );
  }

  if (error || !service) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="services" />
        <main className="flex-1 flex flex-col min-w-0">
          <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
            <div className="flex items-center gap-4">
              <Link
                href="/admin/services"
                className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
              </Link>
              <div>
                <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chi tiết dịch vụ</h1>
                <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Không tìm thấy dịch vụ</p>
              </div>
            </div>
            <NotificationBell />
          </header>
          <div className="p-8 flex-1 flex items-center justify-center">
            <div className="text-center">
              <div className="w-20 h-20 rounded-full bg-red-100 text-red-500 flex items-center justify-center mx-auto mb-4">
                <svg className="w-10 h-10" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                </svg>
              </div>
              <p className="text-slate-500 font-semibold">{error || "Dịch vụ không tồn tại."}</p>
              <Link
                href="/admin/services"
                className="inline-block mt-4 px-6 py-2.5 bg-primary text-white font-bold rounded-xl hover:bg-primary-hover transition-all"
              >
                Quay lại danh sách
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="services" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <Link
              href="/admin/services"
              className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
            <div>
              <h1 className="text-xl font-extrabold text-slate-900 tracking-tight">{service.name}</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Chi tiết dịch vụ</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <Link
              href={`/dashboard/services/edit/${service.id}`}
              className="flex items-center gap-2 px-4 py-2 text-[14px] font-bold text-slate-600 hover:text-slate-900 hover:bg-slate-100 border border-slate-200 rounded-xl transition-all cursor-pointer"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
              </svg>
              Chỉnh sửa
            </Link>
            <NotificationBell />
          </div>
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto">
          <div className="max-w-4xl mx-auto">
            {/* Service Image */}
            {service.imageUrl && (
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden mb-6">
                <div className="aspect-video w-full overflow-hidden bg-slate-100">
                  <img
                    src={service.imageUrl}
                    alt={service.name}
                    className="w-full h-full object-cover"
                  />
                </div>
              </div>
            )}

            {/* Main Info Card */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-5 border-b border-slate-100 bg-slate-50/30 flex items-center justify-between">
                <h3 className="text-[15px] font-extrabold text-slate-700">Thông tin dịch vụ</h3>
                <span
                  className={`inline-flex items-center px-3 py-1.5 rounded-full text-[12px] font-black border ${service.isActive ? "bg-green-50 text-green-600 border-green-100" : "bg-slate-100 text-slate-500 border-slate-200"}`}
                >
                  {service.isActive ? "Hoạt động" : "Tạm ngưng"}
                </span>
              </div>

              <div className="p-6">
                {/* Service Name & ID */}
                <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-6">
                  <div>
                    <h2 className="text-2xl font-black text-slate-900">{service.name}</h2>
                  </div>
                  <span className="text-[12px] font-bold text-slate-400 bg-slate-100 px-3 py-1.5 rounded-lg shrink-0">
                    ID: {service.id}
                  </span>
                </div>

                {/* Price & Duration Grid */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-5 mb-6">
                  <div className="bg-gradient-to-br from-primary/5 to-primary/10 rounded-xl p-5 border border-primary/10">
                    <span className="text-[11px] font-extrabold text-primary uppercase tracking-wider block">
                      Giá dịch vụ
                    </span>
                    <span className="text-3xl font-black text-slate-900 block mt-2">
                      {formatPrice(service.price)}
                    </span>
                  </div>
                  <div className="bg-gradient-to-br from-slate-50 to-slate-100 rounded-xl p-5 border border-slate-200">
                    <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                      Thời gian thực hiện
                    </span>
                    <span className="text-3xl font-black text-slate-900 block mt-2">
                      {service.durationMinutes} <span className="text-base font-semibold text-slate-500">phút</span>
                    </span>
                  </div>
                </div>

                {/* Description */}
                <div className="border-t border-slate-100 pt-6">
                  <h4 className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-3">
                    Mô tả dịch vụ
                  </h4>
                  <p className="text-[14px] text-slate-600 font-semibold leading-relaxed">
                    {service.description || "Không có mô tả."}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
