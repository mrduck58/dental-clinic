"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../../hooks/useRequireAdmin";
import { getPromotionByIdApi, getServicesApi, type PromotionDto, type ServiceDto } from "../../../../../lib/apiClient";

interface PromotionDetailPageProps {
  params: Promise<{ id: string }>;
}

export default function PromotionDetailPage({ params }: PromotionDetailPageProps) {
  useRequireAdmin();
  const { id } = React.use(params);

  const [promotion, setPromotion] = useState<PromotionDto | null>(null);
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([getPromotionByIdApi(id), getServicesApi()])
      .then(([promo, svcs]) => {
        setPromotion(promo);
        setServices(svcs);
      })
      .catch(() => setError("Không thể tải thông tin khuyến mãi."))
      .finally(() => setIsLoading(false));
  }, [id]);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
      minimumFractionDigits: 0,
    }).format(price);
  };

  const formatDate = (date: string) => {
    return date.split("-").reverse().join("/");
  };

  const getDiscountDisplay = () => {
    if (!promotion) return "";
    if (promotion.discountType === "Percentage") {
      return `${promotion.discountValue}%`;
    }
    return formatPrice(promotion.discountValue);
  };

  const getServiceNames = () => {
    if (!promotion) return [];
    if (promotion.serviceIds.length === 0) return [];
    return services
      .filter((s) => promotion.serviceIds.includes(s.id))
      .map((s) => s.name);
  };

  const getTotalOriginalPrice = () => {
    if (!promotion) return 0;
    if (promotion.serviceIds.length === 0) {
      return services.reduce((sum, s) => sum + s.price, 0);
    }
    return services
      .filter((s) => promotion.serviceIds.includes(s.id))
      .reduce((sum, s) => sum + s.price, 0);
  };

  const getDiscountAmount = () => {
    if (!promotion) return 0;
    const original = getTotalOriginalPrice();
    if (promotion.discountType === "Percentage") {
      return Math.round(original * (promotion.discountValue / 100));
    }
    return promotion.discountValue;
  };

  const getFinalPrice = () => {
    return Math.max(0, getTotalOriginalPrice() - getDiscountAmount());
  };

  const isExpired = () => {
    if (!promotion) return false;
    return new Date(promotion.endDate) < new Date();
  };

  const getDaysRemaining = () => {
    if (!promotion) return 0;
    const end = new Date(promotion.endDate);
    const today = new Date();
    const diff = Math.ceil((end.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return diff > 0 ? diff : 0;
  };

  if (isLoading) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="services" />
        <main className="flex-1 flex flex-col min-w-0">
          <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
            <div className="flex items-center gap-4">
              <Link
                href="/dashboard/services"
                className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
              </Link>
              <div>
                <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chi tiết khuyến mãi</h1>
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
              <p className="text-slate-400 font-semibold">Đang tải thông tin khuyến mãi...</p>
            </div>
          </div>
        </main>
      </div>
    );
  }

  if (error || !promotion) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="services" />
        <main className="flex-1 flex flex-col min-w-0">
          <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
            <div className="flex items-center gap-4">
              <Link
                href="/dashboard/services"
                className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
              </Link>
              <div>
                <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chi tiết khuyến mãi</h1>
                <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Không tìm thấy khuyến mãi</p>
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
              <p className="text-slate-500 font-semibold">{error || "Khuyến mãi không tồn tại."}</p>
              <Link
                href="/dashboard/services"
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

  const serviceNames = getServiceNames();
  const expired = isExpired();
  const daysRemaining = getDaysRemaining();

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="services" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <Link
              href="/dashboard/services"
              className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
            <div>
              <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chi tiết khuyến mãi</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Xem thông tin chi tiết của khuyến mãi.</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <Link
              href={`/dashboard/services/promotions/edit/${promotion.id}`}
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
            {/* Promotion Header Card */}
            <div className="bg-gradient-to-br from-primary to-primary-hover rounded-2xl border border-primary/20 shadow-lg shadow-primary/15 p-6 mb-6">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-3">
                  <div className="w-14 h-14 rounded-xl bg-white/20 flex items-center justify-center">
                    <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 14.25l6-6m4.5-3.493V21.75l-3.75-1.5-3.75 1.5-3.75-1.5-3.75 1.5H4.5m0 0l3.75-1.5m3.75 1.5l3.75-1.5m3.75-1.5l3.75 1.5M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008zm0 3h.008v.008h-.008v-.008zm0 3h.008v.008h-.008v-.008z" />
                    </svg>
                  </div>
                  <div>
                    <span className={`inline-flex items-center px-3 py-1.5 rounded-full text-[12px] font-black border ${
                      promotion.isActive && !expired
                        ? "bg-green-100 text-green-700 border-green-200"
                        : "bg-red-100 text-red-600 border-red-200"
                    }`}>
                      {promotion.isActive && !expired ? "Đang hoạt động" : "Đã kết thúc"}
                    </span>
                  </div>
                </div>
                <span className="text-white/70 text-[12px] font-bold bg-white/10 px-3 py-1.5 rounded-lg">
                  ID: {promotion.id}
                </span>
              </div>

              <h2 className="text-2xl font-black text-white mb-2">{promotion.name}</h2>

              <div className="flex items-center gap-4">
                <span className="inline-flex items-center px-4 py-2 rounded-lg bg-white/20 text-white text-[14px] font-extrabold tracking-widest">
                  {promotion.code}
                </span>
                <span className="text-white/80 text-[14px] font-semibold">
                  Giảm {getDiscountDisplay()}
                </span>
              </div>
            </div>

            {/* Main Info Card */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden mb-6">
              <div className="px-6 py-5 border-b border-slate-100 bg-slate-50/30">
                <h3 className="text-[15px] font-extrabold text-slate-700">Thông tin khuyến mãi</h3>
              </div>

              <div className="p-6">
                {/* Time Period */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
                  <div className="bg-slate-50 rounded-xl p-4">
                    <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                      Ngày bắt đầu
                    </span>
                    <span className="text-lg font-black text-slate-900 block mt-1">
                      {formatDate(promotion.startDate)}
                    </span>
                  </div>
                  <div className="bg-slate-50 rounded-xl p-4">
                    <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                      Ngày kết thúc
                    </span>
                    <span className="text-lg font-black text-slate-900 block mt-1">
                      {formatDate(promotion.endDate)}
                    </span>
                    {!expired && daysRemaining > 0 && (
                      <span className="inline-flex items-center mt-2 px-2 py-1 rounded-full bg-orange-50 text-orange-600 text-[11px] font-bold">
                        Còn {daysRemaining} ngày
                      </span>
                    )}
                    {expired && (
                      <span className="inline-flex items-center mt-2 px-2 py-1 rounded-full bg-red-50 text-red-600 text-[11px] font-bold">
                        Đã hết hạn
                      </span>
                    )}
                  </div>
                </div>

                {/* Applied Services */}
                <div className="border-t border-slate-100 pt-6 mb-6">
                  <h4 className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-3">
                    Dịch vụ áp dụng
                  </h4>
                  {serviceNames.length > 0 ? (
                    <div className="flex flex-wrap gap-2">
                      {serviceNames.map((name, idx) => (
                        <span key={idx} className="inline-flex items-center px-3 py-1.5 rounded-lg bg-primary/10 text-primary text-[13px] font-bold border border-primary/20">
                          {name}
                        </span>
                      ))}
                    </div>
                  ) : (
                    <span className="inline-flex items-center px-3 py-1.5 rounded-lg bg-slate-100 text-slate-600 text-[13px] font-semibold">
                      Tất cả dịch vụ
                    </span>
                  )}
                </div>

                {/* Description */}
                {promotion.description && (
                  <div className="border-t border-slate-100 pt-6">
                    <h4 className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide mb-3">
                      Mô tả / Thông điệp
                    </h4>
                    <p className="text-[14px] text-slate-600 font-semibold leading-relaxed">
                      {promotion.description}
                    </p>
                  </div>
                )}
              </div>
            </div>

            {/* Price Summary Card */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-5 border-b border-slate-100 bg-slate-50/30">
                <h3 className="text-[15px] font-extrabold text-slate-700">Tóm tắt giá</h3>
              </div>

              <div className="p-6">
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="text-[14px] text-slate-500 font-medium">Tổng giá gốc:</span>
                    <span className="text-[14px] font-bold text-slate-700">
                      {formatPrice(getTotalOriginalPrice())}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-[14px] text-slate-500 font-medium">Giảm giá:</span>
                    <span className="text-[14px] font-bold text-red-500">
                      -{getDiscountDisplay()}
                      {promotion.discountType === "Percentage" && (
                        <span className="text-[12px] font-medium ml-1">
                          ({formatPrice(getDiscountAmount())})
                        </span>
                      )}
                    </span>
                  </div>
                  <div className="pt-3 border-t border-slate-100">
                    <div className="flex items-center justify-between">
                      <span className="text-[14px] font-extrabold text-slate-700 uppercase tracking-wide">
                        Giá sau khuyến mãi
                      </span>
                      <span className="text-2xl font-black text-red-500">
                        {formatPrice(getFinalPrice())}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
