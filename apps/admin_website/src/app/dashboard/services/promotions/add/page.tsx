"use client";

import React, { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import AdminSidebar from "../../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../../hooks/useRequireAdmin";
import {
  getServicesApi,
  createPromotionApi,
  type ServiceDto,
} from "../../../../../lib/apiClient";

const promotionTypes = [
  { value: "Percentage", label: "Phần trăm (%)" },
  { value: "Fixed", label: "Số tiền (VNĐ)" },
];

export default function AddPromotionPage() {
  useRequireAdmin();
  const router = useRouter();

  const [services, setServices] = useState<ServiceDto[]>([]);
  const [loadingServices, setLoadingServices] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const [code, setCode] = useState("");
  const [promotionName, setPromotionName] = useState("");
  const [promotionType, setPromotionType] = useState<"Percentage" | "Fixed">("Percentage");
  const [selectedServiceId, setSelectedServiceId] = useState<string>("");
  const [discountValue, setDiscountValue] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [message, setMessage] = useState("");
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    getServicesApi()
      .then(setServices)
      .catch(() => setServices([]))
      .finally(() => setLoadingServices(false));
  }, []);

  const filteredServices = useMemo(() => {
    if (!searchQuery.trim()) return services;
    return services.filter((s) =>
      s.name.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [searchQuery, services]);

  const selectedServiceData = services.find((s) => s.id === selectedServiceId);

  const originalPrice = selectedServiceData?.price || 0;
  const discountAmount = promotionType === "Percentage"
    ? Math.round(originalPrice * (Number(discountValue) / 100))
    : Number(discountValue);
  const finalPrice = Math.max(0, originalPrice - discountAmount);

  const daysRemaining = useMemo(() => {
    if (!endDate) return 0;
    const end = new Date(endDate);
    const today = new Date();
    const diff = Math.ceil((end.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return diff > 0 ? diff : 0;
  }, [endDate]);

  const validate = () => {
    if (!code.trim()) return "Vui lòng nhập mã khuyến mãi.";
    if (!promotionName.trim()) return "Vui lòng nhập tên chương trình.";
    if (!discountValue || Number(discountValue) <= 0) return "Vui lòng nhập giá trị giảm hợp lệ.";
    if (!startDate) return "Vui lòng chọn ngày bắt đầu.";
    if (!endDate) return "Vui lòng chọn ngày kết thúc.";
    if (endDate < startDate) return "Ngày kết thúc phải sau ngày bắt đầu.";
    return null;
  };

  const handleSave = async (isActive: boolean) => {
    const err = validate();
    if (err) { setSaveError(err); return; }
    setSaveError(null);
    setIsSaving(true);
    try {
      await createPromotionApi({
        code: code.trim().toUpperCase(),
        name: promotionName.trim(),
        description: message.trim() || undefined,
        discountType: promotionType,
        discountValue: Number(discountValue),
        serviceIds: selectedServiceId ? [selectedServiceId] : [],
        startDate,
        endDate,
        isActive,
      });
      sessionStorage.setItem("serviceSuccessMsg", isActive ? "Đã tạo và áp dụng khuyến mãi!" : "Đã thêm khuyến mãi (chưa áp dụng).");
      router.push("/dashboard/services");
    } catch (e) {
      setSaveError(e instanceof Error ? e.message : "Có lỗi xảy ra");
      setIsSaving(false);
    }
  };

  const formatCurrency = (value: number) =>
    new Intl.NumberFormat("vi-VN").format(value) + " VNĐ";

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
              <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Thiết lập Khuyến mãi</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Tạo chương trình khuyến mãi mới.</p>
            </div>
          </div>
          <NotificationBell />
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto">
          {saveError && (
            <div className="max-w-[1400px] mx-auto mb-4 px-4 py-3 bg-red-50 border border-red-200 text-red-700 text-[14px] font-semibold rounded-xl">
              {saveError}
            </div>
          )}

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-6 max-w-[1400px] mx-auto">
            {/* FORM */}
            <div className="xl:col-span-2">
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="p-6 flex flex-col gap-6">

                  {/* Mã khuyến mãi */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Mã khuyến mãi <span className="text-primary">*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="VD: SUMMER2024"
                      value={code}
                      onChange={(e) => setCode(e.target.value.toUpperCase())}
                      className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300 uppercase tracking-widest"
                    />
                  </div>

                  {/* Chọn dịch vụ */}
                  <div className="flex flex-col gap-3">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Chọn dịch vụ áp dụng <span className="text-slate-400 font-medium normal-case">(để trống = tất cả dịch vụ)</span>
                    </label>

                    <div className="relative">
                      <svg className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
                      </svg>
                      <input
                        type="text"
                        placeholder="Tìm kiếm dịch vụ..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="w-full pl-11 pr-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                    </div>

                    <div className="border border-slate-200 rounded-xl overflow-hidden bg-white">
                      <div className="max-h-[200px] overflow-y-auto">
                        {loadingServices ? (
                          <div className="px-4 py-6 text-center text-[13px] text-slate-400 font-medium">Đang tải...</div>
                        ) : filteredServices.length === 0 ? (
                          <div className="px-4 py-6 text-center text-[13px] text-slate-400 font-medium">Không tìm thấy dịch vụ</div>
                        ) : filteredServices.map((service) => (
                          <button
                            key={service.id}
                            type="button"
                            onClick={() => {
                              setSelectedServiceId(service.id === selectedServiceId ? "" : service.id);
                              setSearchQuery("");
                            }}
                            className={`w-full flex items-center justify-between px-4 py-3 text-[14px] font-semibold transition-all cursor-pointer border-b border-slate-100 last:border-b-0 ${
                              selectedServiceId === service.id
                                ? "bg-primary/5 text-primary"
                                : "bg-white text-slate-700 hover:bg-slate-50"
                            }`}
                          >
                            <div className="flex items-center gap-3">
                              <span className={`w-5 h-5 rounded-full border-2 flex items-center justify-center transition-all ${
                                selectedServiceId === service.id ? "border-primary bg-primary" : "border-slate-300"
                              }`}>
                                {selectedServiceId === service.id && (
                                  <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                                  </svg>
                                )}
                              </span>
                              <span>{service.name}</span>
                            </div>
                            <span className="text-[12px] text-slate-400 font-medium">{formatCurrency(service.price)}</span>
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>

                  {/* Badge dịch vụ đang chọn */}
                  {selectedServiceData && (
                    <div className="flex items-center gap-3 p-4 bg-primary/5 border border-primary/20 rounded-xl">
                      <svg className="w-5 h-5 text-primary shrink-0" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-1 11h-4v2h4v-2zm-6 0H9v2h3v-2zm0-4H9v2h3V10zm6 4h-4v2h4v-2zm0-4h-4v2h3v-2zM9 6h6v2H9V6z" />
                      </svg>
                      <div className="flex-1">
                        <span className="text-[14px] font-bold text-primary">{selectedServiceData.name}</span>
                        <span className="ml-2 text-[12px] text-slate-500 font-semibold">{selectedServiceData.category}</span>
                      </div>
                      <button
                        type="button"
                        onClick={() => setSelectedServiceId("")}
                        className="p-1 text-slate-400 hover:text-red-500 transition-colors cursor-pointer"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </div>
                  )}

                  {/* Tên chương trình */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Tên chương trình <span className="text-primary">*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="VD: Summer Sale - Giảm 20% dịch vụ niềng răng..."
                      value={promotionName}
                      onChange={(e) => setPromotionName(e.target.value)}
                      className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                    />
                  </div>

                  {/* Loại chiết khấu & Giá trị */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Loại chiết khấu <span className="text-primary">*</span>
                      </label>
                      <div className="relative">
                        <select
                          value={promotionType}
                          onChange={(e) => setPromotionType(e.target.value as "Percentage" | "Fixed")}
                          className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-700 appearance-none pr-10 cursor-pointer"
                        >
                          {promotionTypes.map((type) => (
                            <option key={type.value} value={type.value}>{type.label}</option>
                          ))}
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                          </svg>
                        </span>
                      </div>
                    </div>

                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Giá trị giảm <span className="text-primary">*</span>
                      </label>
                      <div className="relative">
                        <input
                          type="number"
                          min="0"
                          max={promotionType === "Percentage" ? 100 : undefined}
                          placeholder="0"
                          value={discountValue}
                          onChange={(e) => setDiscountValue(e.target.value)}
                          className="w-full px-4 py-3 pr-14 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                        />
                        <span className="absolute right-4 top-1/2 -translate-y-1/2 text-[13px] text-slate-400 font-bold">
                          {promotionType === "Percentage" ? "%" : "VNĐ"}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Ngày bắt đầu & kết thúc */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Ngày bắt đầu <span className="text-primary">*</span>
                      </label>
                      <input
                        type="date"
                        value={startDate}
                        onChange={(e) => setStartDate(e.target.value)}
                        className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-700 cursor-pointer"
                      />
                    </div>
                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Ngày kết thúc <span className="text-primary">*</span>
                      </label>
                      <input
                        type="date"
                        value={endDate}
                        min={startDate}
                        onChange={(e) => setEndDate(e.target.value)}
                        className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-700 cursor-pointer"
                      />
                    </div>
                  </div>

                  {/* Thông điệp */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Thông điệp khuyến mãi
                    </label>
                    <textarea
                      placeholder="Nhập thông điệp khuyến mãi..."
                      value={message}
                      onChange={(e) => setMessage(e.target.value)}
                      rows={3}
                      className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300 resize-none"
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* SIDEBAR */}
            <div className="xl:col-span-1 space-y-6">
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/30">
                  <h3 className="text-[15px] font-extrabold text-slate-700">Tóm tắt Khuyến mãi</h3>
                </div>

                <div className="p-6">
                  <div className="mb-5">
                    <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Mã khuyến mãi</span>
                    <p className="text-[14px] font-bold text-slate-800 mt-1 tracking-widest">
                      {code || <span className="text-slate-400 font-normal italic tracking-normal">Chưa nhập</span>}
                    </p>
                  </div>

                  <div className="mb-5">
                    <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Tên chương trình</span>
                    <p className="text-[14px] font-bold text-slate-800 mt-1">
                      {promotionName || <span className="text-slate-400 font-normal italic">Chưa nhập</span>}
                    </p>
                  </div>

                  <div className="mb-5">
                    <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Dịch vụ áp dụng</span>
                    <p className="text-[14px] font-bold text-slate-800 mt-1">
                      {selectedServiceData
                        ? <span className="text-primary">{selectedServiceData.name}</span>
                        : <span className="text-slate-500 font-semibold">Tất cả dịch vụ</span>}
                    </p>
                  </div>

                  <div className="mb-5 grid grid-cols-2 gap-4">
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Loại chiết khấu</span>
                      <p className="text-[14px] font-bold text-slate-800 mt-1">
                        {promotionType === "Percentage" ? "Phần trăm (%)" : "Số tiền (VNĐ)"}
                      </p>
                    </div>
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Giá trị giảm</span>
                      <p className="text-[14px] font-bold text-red-500 mt-1">
                        {discountValue
                          ? promotionType === "Percentage" ? `${discountValue}%` : formatCurrency(Number(discountValue))
                          : <span className="text-slate-400 font-normal italic">0</span>}
                      </p>
                    </div>
                  </div>

                  <div className="mb-5 grid grid-cols-2 gap-4">
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Ngày bắt đầu</span>
                      <p className="text-[14px] font-semibold text-slate-700 mt-1">
                        {startDate || <span className="text-slate-400 font-normal italic">--</span>}
                      </p>
                    </div>
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Ngày kết thúc</span>
                      <p className="text-[14px] font-semibold text-slate-700 mt-1">
                        {endDate || <span className="text-slate-400 font-normal italic">--</span>}
                      </p>
                    </div>
                  </div>

                  <div className="border-t border-slate-100 my-5" />

                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <span className="text-[13px] font-medium text-slate-500">Giá gốc:</span>
                      <span className="text-[14px] font-bold text-slate-700">
                        {selectedServiceData ? formatCurrency(selectedServiceData.price) : "—"}
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-[13px] font-medium text-slate-500">
                        Giảm giá{discountValue && promotionType === "Percentage" ? ` (-${discountValue}%)` : ""}:
                      </span>
                      <span className="text-[14px] font-bold text-red-500">
                        {discountAmount > 0 ? `-${formatCurrency(discountAmount)}` : "—"}
                      </span>
                    </div>
                    <div className="pt-3 border-t border-slate-100">
                      <span className="text-[12px] font-bold text-slate-400 uppercase tracking-wide">Giá sau khuyến mãi</span>
                      <div className="mt-1 text-[24px] font-extrabold text-red-500">
                        {selectedServiceData && discountAmount > 0 ? formatCurrency(finalPrice) : "—"}
                      </div>
                    </div>
                  </div>
                </div>

                {daysRemaining > 0 && (
                  <div className="px-6 py-4 bg-red-50 border-t border-red-100">
                    <div className="flex items-center gap-2 mb-1">
                      <svg className="w-4 h-4 text-red-500" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z" />
                      </svg>
                      <span className="text-[13px] font-bold text-red-600">Thời hạn còn lại</span>
                    </div>
                    <p className="text-[12px] text-red-500 font-medium">
                      Khuyến mãi sẽ kết thúc sau {daysRemaining} ngày {endDate && `(${endDate})`}.
                    </p>
                  </div>
                )}

                {/* Actions — 2 nút */}
                <div className="p-6 pt-4 space-y-3">
                  <button
                    type="button"
                    disabled={isSaving}
                    onClick={() => handleSave(true)}
                    className="w-full py-3.5 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/25 hover:shadow-lg transition-all flex items-center justify-center gap-2 cursor-pointer disabled:opacity-60"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    {isSaving ? "Đang lưu..." : "Lưu và áp dụng ngay"}
                  </button>

                  <button
                    type="button"
                    disabled={isSaving}
                    onClick={() => handleSave(false)}
                    className="w-full py-3.5 text-[14px] font-bold text-slate-600 hover:text-slate-900 hover:bg-slate-100 border border-slate-200 rounded-xl transition-all flex items-center justify-center gap-2 cursor-pointer disabled:opacity-60"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                    </svg>
                    Thêm (chưa áp dụng)
                  </button>

                  <Link
                    href="/dashboard/services"
                    className="block w-full py-3 text-center text-[13px] font-bold text-slate-400 hover:text-slate-600 transition-all cursor-pointer"
                  >
                    Hủy bỏ
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
