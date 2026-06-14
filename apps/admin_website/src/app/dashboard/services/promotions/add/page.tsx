"use client";

import React, { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../../hooks/useRequireAdmin";

const promotionTypes = [
  { value: "percentage", label: "Phần trăm (%)" },
  { value: "fixed", label: "Số tiền (VNĐ)" },
];

const services = [
  { value: "nieng-rang", label: "Niềng răng", id: "SV-204", price: 15000000 },
  { value: "tay-trang", label: "Tẩy trắng răng", id: "SV-205", price: 2500000 },
  { value: "trong-rang", label: "Trồng răng implant", id: "SV-206", price: 25000000 },
  { value: "tay-cao", label: "Tẩy cao răng", id: "SV-207", price: 500000 },
  { value: "dieu-tri", label: "Điều trị tủy", id: "SV-208", price: 1500000 },
  { value: "chup-x-quang", label: "Chụp X-quang", id: "SV-209", price: 300000 },
  { value: "nieng-rang-mo", label: "Niềng răng mắc cài", id: "SV-210", price: 20000000 },
  { value: "tay-trang-chi-u", label: "Tẩy trắng chỉ thị", id: "SV-211", price: 3500000 },
];

export default function AddPromotionPage() {
  useRequireAdmin();
  const [promotionName, setPromotionName] = useState("");
  const [promotionType, setPromotionType] = useState("percentage");
  const [selectedService, setSelectedService] = useState<string>("");
  const [discountValue, setDiscountValue] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [message, setMessage] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");

  const filteredServices = useMemo(() => {
    if (!searchQuery.trim()) return services;
    return services.filter((s) =>
      s.label.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [searchQuery]);

  const selectedServiceData = services.find((s) => s.value === selectedService);

  // Tính toán giá trị khuyến mãi
  const originalPrice = selectedServiceData?.price || 0;
  const discountAmount = promotionType === "percentage"
    ? Math.round(originalPrice * (Number(discountValue) / 100))
    : Number(discountValue);
  const finalPrice = originalPrice - discountAmount;

  // Tính số ngày còn lại
  const daysRemaining = useMemo(() => {
    if (!endDate) return 0;
    const end = new Date(endDate);
    const today = new Date();
    const diff = Math.ceil((end.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return diff > 0 ? diff : 0;
  }, [endDate]);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    if (!promotionName || !discountValue || !startDate || !endDate || !selectedService) {
      alert("Vui lòng điền đầy đủ thông tin bắt buộc.");
      return;
    }
    alert("Khuyến mãi đã được tạo thành công!");
    window.location.href = "/dashboard/services";
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat("vi-VN").format(value) + " VNĐ";
  };

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
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
                Tạo chương trình khuyến mãi mới.
              </p>
            </div>
          </div>

          <NotificationBell />
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto">
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-6 max-w-[1400px] mx-auto">
            {/* FORM */}
            <div className="xl:col-span-2">
              <form onSubmit={handleSave}>
                <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                  {/* Form Content */}
                  <div className="p-6 flex flex-col gap-6">
                    {/* Chọn dịch vụ - Compact Design */}
                    <div className="flex flex-col gap-3">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Chọn dịch vụ áp dụng <span className="text-primary">*</span>
                      </label>
                      
                      {/* Search input */}
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

                      {/* Service list - Card style */}
                      <div className="border border-slate-200 rounded-xl overflow-hidden bg-white">
                        <div className="max-h-[200px] overflow-y-auto">
                          {filteredServices.map((service) => (
                            <button
                              key={service.value}
                              type="button"
                              onClick={() => {
                                setSelectedService(service.value);
                                setSearchQuery("");
                              }}
                              className={`w-full flex items-center justify-between px-4 py-3 text-[14px] font-semibold transition-all cursor-pointer border-b border-slate-100 last:border-b-0 ${
                                selectedService === service.value
                                  ? "bg-primary/5 text-primary"
                                  : "bg-white text-slate-700 hover:bg-slate-50"
                              }`}
                            >
                              <div className="flex items-center gap-3">
                                <span className={`w-5 h-5 rounded-full border-2 flex items-center justify-center transition-all ${
                                  selectedService === service.value
                                    ? "border-primary bg-primary"
                                    : "border-slate-300"
                                }`}>
                                  {selectedService === service.value && (
                                    <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                                    </svg>
                                  )}
                                </span>
                                <span>{service.label}</span>
                              </div>
                              <span className="text-[12px] text-slate-400 font-medium">{service.id}</span>
                            </button>
                          ))}
                          
                          {filteredServices.length === 0 && (
                            <div className="px-4 py-6 text-center text-[13px] text-slate-400 font-medium">
                              Không tìm thấy dịch vụ
                            </div>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Dịch vụ đang chọn - Badge */}
                    {selectedServiceData && (
                      <div className="flex items-center gap-3 p-4 bg-primary/5 border border-primary/20 rounded-xl">
                        <svg className="w-5 h-5 text-primary" fill="currentColor" viewBox="0 0 24 24">
                          <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-1 11h-4v2h4v-2zm-6 0H9v2h3v-2zm0-4H9v2h3V10zm6 4h-4v2h4v-2zm0-4h-4v2h3v-2zM9 6h6v2H9V6z" />
                        </svg>
                        <div className="flex-1">
                          <span className="text-[14px] font-bold text-primary">{selectedServiceData.label}</span>
                          <span className="ml-2 px-2 py-0.5 bg-white text-slate-500 text-[11px] font-bold rounded-md">
                            {selectedServiceData.id}
                          </span>
                        </div>
                        <button
                          type="button"
                          onClick={() => setSelectedService("")}
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
                        required
                        placeholder="VD: Summer Sale - Giảm 20% dịch vụ niềng răng..."
                        value={promotionName}
                        onChange={(e) => setPromotionName(e.target.value)}
                        className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                    </div>

                    {/* Loại chiết khấu & Giá trị giảm */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                      {/* Loại chiết khấu */}
                      <div className="flex flex-col gap-2">
                        <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                          Loại chiết khấu <span className="text-primary">*</span>
                        </label>
                        <div className="relative">
                          <select
                            value={promotionType}
                            onChange={(e) => setPromotionType(e.target.value)}
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

                      {/* Giá trị giảm */}
                      <div className="flex flex-col gap-2">
                        <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                          Giá trị giảm <span className="text-primary">*</span>
                        </label>
                        <div className="relative">
                          <input
                            type="number"
                            required
                            min="0"
                            max={promotionType === "percentage" ? 100 : undefined}
                            placeholder="0"
                            value={discountValue}
                            onChange={(e) => setDiscountValue(e.target.value)}
                            className="w-full px-4 py-3 pr-14 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                          />
                          <span className="absolute right-4 top-1/2 -translate-y-1/2 text-[13px] text-slate-400 font-bold">
                            {promotionType === "percentage" ? "%" : "VNĐ"}
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* Ngày bắt đầu & Ngày kết thúc */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                      {/* Ngày bắt đầu */}
                      <div className="flex flex-col gap-2">
                        <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                          Ngày bắt đầu <span className="text-primary">*</span>
                        </label>
                        <input
                          type="date"
                          required
                          value={startDate}
                          onChange={(e) => setStartDate(e.target.value)}
                          className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-700 cursor-pointer"
                        />
                      </div>

                      {/* Ngày kết thúc */}
                      <div className="flex flex-col gap-2">
                        <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                          Ngày kết thúc <span className="text-primary">*</span>
                        </label>
                        <input
                          type="date"
                          required
                          value={endDate}
                          onChange={(e) => setEndDate(e.target.value)}
                          className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-700 cursor-pointer"
                        />
                      </div>
                    </div>

                    {/* Thông điệp khuyến mãi */}
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
              </form>
            </div>

            {/* SIDEBAR - Tóm tắt thông tin khuyến mãi */}
            <div className="xl:col-span-1 space-y-6">
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                {/* Header */}
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/30">
                  <div className="flex items-center justify-between">
                    <h3 className="text-[15px] font-extrabold text-slate-700">Tóm tắt Khuyến mãi</h3>
                    <button
                      type="button"
                      onClick={() => setIsActive(!isActive)}
                      className={`flex items-center gap-2 px-3 py-1.5 text-[12px] font-bold rounded-lg border transition-all cursor-pointer ${
                        isActive
                          ? "bg-green-50 text-green-600 border-green-200"
                          : "bg-slate-100 text-slate-400 border-slate-200"
                      }`}
                    >
                      <span className={`w-2 h-2 rounded-full ${isActive ? "bg-green-500" : "bg-slate-400"}`}></span>
                      {isActive ? "Đang kích hoạt" : "Tắt"}
                    </button>
                  </div>
                </div>

                {/* Content */}
                <div className="p-6">
                  {/* Tên chương trình */}
                  <div className="mb-5">
                    <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Tên chương trình</span>
                    <p className="text-[14px] font-bold text-slate-800 mt-1">
                      {promotionName || <span className="text-slate-400 font-normal italic">Chưa nhập</span>}
                    </p>
                  </div>

                  {/* Dịch vụ */}
                  <div className="mb-5">
                    <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Dịch vụ áp dụng</span>
                    <p className="text-[14px] font-bold text-slate-800 mt-1">
                      {selectedServiceData ? (
                        <span className="text-primary">{selectedServiceData.label}</span>
                      ) : (
                        <span className="text-slate-400 font-normal italic">Chưa chọn dịch vụ</span>
                      )}
                    </p>
                  </div>

                  {/* Loại chiết khấu & Giá trị */}
                  <div className="mb-5 grid grid-cols-2 gap-4">
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Loại chiết khấu</span>
                      <p className="text-[14px] font-bold text-slate-800 mt-1">
                        {promotionType === "percentage" ? "Phần trăm (%)" : "Số tiền (VNĐ)"}
                      </p>
                    </div>
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide">Giá trị giảm</span>
                      <p className="text-[14px] font-bold text-red-500 mt-1">
                        {discountValue ? (
                          promotionType === "percentage" 
                            ? `${discountValue}%` 
                            : formatCurrency(Number(discountValue))
                        ) : (
                          <span className="text-slate-400 font-normal italic">0</span>
                        )}
                      </p>
                    </div>
                  </div>

                  {/* Ngày */}
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

                  {/* Divider */}
                  <div className="border-t border-slate-100 my-5"></div>

                  {/* Giá */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <span className="text-[13px] font-medium text-slate-500">Giá gốc:</span>
                      <span className="text-[14px] font-bold text-slate-700">
                        {selectedServiceData ? formatCurrency(selectedServiceData.price) : "0 VNĐ"}
                      </span>
                    </div>
                    
                    <div className="flex items-center justify-between">
                      <span className="text-[13px] font-medium text-slate-500">
                        Giảm giá {discountValue && promotionType === "percentage" ? `(-${discountValue}%)` : ""}:
                      </span>
                      <span className="text-[14px] font-bold text-red-500">
                        {discountAmount > 0 ? `-${formatCurrency(discountAmount)}` : "0 VNĐ"}
                      </span>
                    </div>

                    <div className="pt-3 border-t border-slate-100">
                      <span className="text-[12px] font-bold text-slate-400 uppercase tracking-wide">Giá sau khuyến mãi</span>
                      <div className="mt-1 text-[24px] font-extrabold text-red-500">
                        {finalPrice > 0 ? formatCurrency(finalPrice) : "0 VNĐ"}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Thời hạn */}
                {daysRemaining > 0 && (
                  <div className="px-6 py-4 bg-red-50 border-t border-red-100">
                    <div className="flex items-center gap-2 mb-2">
                      <svg className="w-4 h-4 text-red-500" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z" />
                      </svg>
                      <span className="text-[13px] font-bold text-red-600">Thời hạn còn lại</span>
                    </div>
                    <p className="text-[12px] text-red-500 font-medium">
                      Khuyến mãi sẽ tự động đóng bỏ sau {daysRemaining} ngày {endDate && `(${endDate})`}.
                    </p>
                  </div>
                )}

                {/* Actions */}
                <div className="p-6 pt-0 space-y-3 mt-4">
                  <button
                    type="button"
                    onClick={handleSave}
                    className="w-full py-3.5 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/25 hover:shadow-lg transition-all flex items-center justify-center gap-2 cursor-pointer"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    Áp dụng khuyến mãi
                  </button>
                  
                  <Link
                    href="/dashboard/services"
                    className="block w-full py-3.5 text-center text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-100 border border-slate-200 rounded-xl transition-all cursor-pointer"
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
