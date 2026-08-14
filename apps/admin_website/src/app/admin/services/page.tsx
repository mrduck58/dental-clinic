"use client";

import React, { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import AdminPageHeader from "../../../components/shared/AdminPageHeader";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import { useRouter } from "next/navigation";
import {
  getServicesApi,
  deleteServiceApi,
  toggleServiceStatusApi,
  getPromotionsApi,
  togglePromotionStatusApi,
  deletePromotionApi,
  resolveAssetUrl,
  type ServiceDto,
  type PromotionDto,
} from "../../../lib/apiClient";

interface Service {
  id: string;
  name: string;
  price: number;
  duration: number;
  status: "Active" | "Inactive";
  description: string;
  popular: number;
  iconUrl: string | null;
}

function toService(dto: ServiceDto): Service {
  return {
    id: dto.id,
    name: dto.name,
    price: dto.price,
    duration: dto.durationMinutes,
    status: dto.isActive ? "Active" : "Inactive",
    description: dto.description,
    popular: dto.viewCount,
    iconUrl: dto.iconUrl,
  };
}

const ITEMS_PER_PAGE_DEFAULT = 5;

export default function ServicesPage() {
  useRequireAdmin();
  const router = useRouter();
  const [services, setServices] = useState<Service[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState("");

  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(ITEMS_PER_PAGE_DEFAULT);

  const [promoCurrentPage, setPromoCurrentPage] = useState(1);
  const [promoItemsPerPage, setPromoItemsPerPage] = useState(ITEMS_PER_PAGE_DEFAULT);

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedService, setSelectedService] = useState<Service | null>(null);

  const [promotions, setPromotions] = useState<PromotionDto[]>([]);
  const [isPromosLoading, setIsPromosLoading] = useState(true);
  const [isDeletePromoOpen, setIsDeletePromoOpen] = useState(false);
  const [selectedPromo, setSelectedPromo] = useState<PromotionDto | null>(null);

  useEffect(() => {
    getServicesApi()
      .then((data) => setServices(data.map(toService)))
      .finally(() => setIsLoading(false));
    getPromotionsApi()
      .then(setPromotions)
      .finally(() => setIsPromosLoading(false));
  }, []);

  const handleTogglePromo = async (id: string) => {
    try {
      const updated = await togglePromotionStatusApi(id);
      setPromotions(prev => prev.map(p => p.id === id ? updated : p));
    } catch { /* silent */ }
  };

  const handleDeletePromo = async () => {
    if (!selectedPromo) return;
    try {
      await deletePromotionApi(selectedPromo.id);
      setPromotions(prev => prev.filter(p => p.id !== selectedPromo.id));
    } finally {
      setIsDeletePromoOpen(false);
      setSelectedPromo(null);
    }
  };

  const stats = useMemo(() => {
    const total = services.length;
    const active = services.filter((s) => s.status === "Active").length;
    const inactive = services.filter((s) => s.status === "Inactive").length;
    const mostPopular = services.length > 0
      ? services.reduce((max, s) => (s.popular > max.popular ? s : max), services[0])
      : null;
    return { total, active, inactive, mostPopular };
  }, [services]);

  const promoStats = useMemo(() => {
    const now = new Date();
    const monthStart = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split("T")[0];
    const monthEnd = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().split("T")[0];
    const thisMonth = promotions.filter(
      (p) => p.startDate <= monthEnd && p.endDate >= monthStart
    );
    const active = thisMonth.filter((p) => p.isActive);
    return { total: thisMonth.length, active: active.length, codes: thisMonth.slice(0, 5).map((p) => p.code) };
  }, [promotions]);

  const filteredServices = useMemo(() => {
    return services.filter((service) => {
      const matchesSearch =
        service.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        service.description.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesStatus = statusFilter === "" || statusFilter === "Tất cả" || service.status === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [services, searchQuery, statusFilter]);

  const totalPages = Math.ceil(filteredServices.length / itemsPerPage);
  const paginatedServices = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    return filteredServices.slice(start, start + itemsPerPage);
  }, [filteredServices, currentPage, itemsPerPage]);

  const filteredPromotions = useMemo(() => {
    return promotions;
  }, [promotions]);

  const promoTotalPages = Math.ceil(filteredPromotions.length / promoItemsPerPage);
  const paginatedPromotions = useMemo(() => {
    const start = (promoCurrentPage - 1) * promoItemsPerPage;
    return filteredPromotions.slice(start, start + promoItemsPerPage);
  }, [filteredPromotions, promoCurrentPage, promoItemsPerPage]);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
      minimumFractionDigits: 0,
    }).format(price);
  };

  const handleToggleStatus = async (id: string) => {
    try {
      const updated = await toggleServiceStatusApi(id);
      setServices((prev) => prev.map((s) => (s.id === id ? toService(updated) : s)));
    } catch {
      alert("Không thể cập nhật trạng thái. Vui lòng thử lại.");
    }
  };

  const handleItemsPerPageChange = (value: number) => {
    setItemsPerPage(value);
    setCurrentPage(1);
  };

  const handlePromoItemsPerPageChange = (value: number) => {
    setPromoItemsPerPage(value);
    setPromoCurrentPage(1);
  };

  const openDeleteModal = (service: Service) => {
    setSelectedService(service);
    setIsDeleteModalOpen(true);
  };

  const handleDelete = async () => {
    if (!selectedService) return;
    try {
      await deleteServiceApi(selectedService.id);
      setServices((prev) => prev.filter((s) => s.id !== selectedService.id));
      setIsDeleteModalOpen(false);
      setSelectedService(null);
    } catch {
      alert("Không thể xóa dịch vụ. Vui lòng thử lại.");
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="services" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <AdminPageHeader
          title="Quản Lí Dịch Vụ"
          subtitle="Quản lý danh sách dịch vụ và cấu hình thông tin."
        />

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">
          {/* STATS GRID */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            {/* Total services */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                  Tổng dịch vụ
                </span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.total}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.83-5.83m0 0a2.95 2.95 0 11-4.174-4.172 2.95 2.95 0 014.174 4.172zm-7.42 7.42l9.39-9.39" />
                </svg>
              </div>
            </div>

            {/* Active services */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                  Đang hoạt động
                </span>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-3xl font-black text-slate-900 leading-none">{stats.active}</span>
                  <span className="relative flex h-3.5 w-3.5">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-3.5 w-3.5 bg-green-500"></span>
                  </span>
                </div>
              </div>
              <div className="w-12 h-12 rounded-xl bg-green-50 text-green-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>

            {/* Inactive services */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                  Tạm ngưng
                </span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.inactive}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-slate-100 text-slate-500 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                </svg>
              </div>
            </div>

            {/* Khuyến mãi trong tháng */}
            <div
              onClick={() => document.getElementById("promotions-section")?.scrollIntoView({ behavior: "smooth" })}
              className="bg-gradient-to-br from-primary to-primary-hover p-5 rounded-2xl border border-primary/20 shadow-lg shadow-primary/15 hover:shadow-xl hover:shadow-primary/25 transition-all duration-200 cursor-pointer"
            >
              <div>
                <span className="text-[11px] font-extrabold text-white/70 uppercase tracking-wider block">
                  Khuyến mãi tháng này
                </span>
                <div className="flex items-center justify-between mt-1">
                  <div className="flex items-center gap-2">
                    <span className="text-3xl font-black text-white leading-none">
                      {promoStats.active}
                    </span>
                    <span className="px-2 py-0.5 rounded-full bg-white/20 text-white text-[12px] font-bold">
                      đang chạy
                    </span>
                  </div>
                </div>
              </div>
              <div className="mt-3 flex items-center gap-2 overflow-hidden">
                {promoStats.codes.length > 0 ? (
                  <div className="flex gap-1.5 animate-marquee">
                    {promoStats.codes.map((code) => (
                      <span key={code} className="px-2 py-1 rounded-lg bg-white/20 text-white text-[11px] font-bold whitespace-nowrap">
                        {code}
                      </span>
                    ))}
                  </div>
                ) : (
                  <span className="text-white/50 text-[11px]">Không có khuyến mãi</span>
                )}
              </div>
            </div>
          </div>

          {/* TOOLBAR */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3">

            {/* Row 1: search + filters + create */}
            <div className="flex flex-col md:flex-row md:items-center gap-3">
              {/* Search */}
              <div className="relative flex-1">
                <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm theo tên dịch vụ..."
                  value={searchQuery}
                  onChange={(e) => {
                    setSearchQuery(e.target.value);
                    setCurrentPage(1);
                  }}
                  className="w-full pl-9.5 pr-4 py-2.5 text-[14px] bg-slate-100/80 border border-transparent rounded-xl focus:bg-white focus:border-slate-200 focus:outline-none transition-all font-semibold"
                />
              </div>

              {/* Status filter */}
              <div className="relative shrink-0">
                <select
                  value={statusFilter}
                  onChange={(e) => {
                    setStatusFilter(e.target.value);
                    setCurrentPage(1);
                  }}
                  className="appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-9 py-2.5 rounded-xl border border-slate-200 focus:outline-none transition-all cursor-pointer"
                >
                  <option value="" disabled>Trạng thái</option>
                  <option value="Tất cả">Tất cả</option>
                  <option value="Active">Hoạt động</option>
                  <option value="Inactive">Tạm ngưng</option>
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </div>
              </div>

              {/* Add service button */}
              <Link
                href="/admin/services/add"
                className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] px-5 py-2.5 rounded-xl transition-all shadow-md shadow-primary/25 shrink-0"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm dịch vụ
              </Link>
            </div>

            {/* Row 2: per-page + result count */}
            <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
              <span>Hiển thị</span>
              <div className="relative">
                <select
                  value={itemsPerPage}
                  onChange={(e) => handleItemsPerPageChange(Number(e.target.value))}
                  className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer"
                >
                  {[5, 10, 20].map(n => (
                    <option key={n} value={n}>{n}</option>
                  ))}
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                  <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </div>
              </div>
              <span>/ trang</span>
              <span className="text-slate-300 mx-1">·</span>
              <span>
                Tìm thấy{" "}
                <span className="text-slate-600 font-bold">{filteredServices.length}</span>
                {" "}kết quả
              </span>
            </div>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[14px]">
                <thead>
                  <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
                    <th className="px-6 py-4">Icon</th>
                    <th className="px-6 py-4">Dịch vụ</th>
                    <th className="px-6 py-4">Giá dịch vụ</th>
                    <th className="px-6 py-4">Thời gian</th>
                    <th className="px-6 py-4 text-center">Trạng thái</th>
                    <th className="px-6 py-4 text-center">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                  {isLoading ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Đang tải dữ liệu...
                      </td>
                    </tr>
                  ) : paginatedServices.length > 0 ? (
                    paginatedServices.map((service) => (
                      <tr key={service.id} className="hover:bg-slate-50/40 transition-colors">
                        {/* Icon */}
                        <td className="px-6 py-4.5">
                          <div className="w-10 h-10 rounded-xl border border-slate-200 bg-slate-50/50 flex items-center justify-center overflow-hidden shrink-0">
                            {service.iconUrl ? (
                              // eslint-disable-next-line @next/next/no-img-element
                              <img src={resolveAssetUrl(service.iconUrl)} alt={service.name} className="w-6 h-6 object-contain" />
                            ) : (
                              <svg className="w-4 h-4 text-slate-300" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
                              </svg>
                            )}
                          </div>
                        </td>

                        {/* Service name & description */}
                        <td className="px-6 py-4.5">
                          <div className="min-w-0">
                            <div className="font-extrabold text-slate-900 truncate max-w-[280px]">{service.name}</div>
                            <div className="text-[12px] text-slate-400 font-medium truncate mt-0.5 max-w-[280px]">
                              {service.description.length > 60
                                ? `${service.description.substring(0, 60)}...`
                                : service.description}
                            </div>
                          </div>
                        </td>

                        {/* Price */}
                        <td className="px-6 py-4.5 font-bold text-slate-800 whitespace-nowrap">
                          {formatPrice(service.price)}
                        </td>

                        {/* Duration */}
                        <td className="px-6 py-4.5">
                          <div className="flex items-center gap-1.5 text-slate-600">
                            <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            <span className="font-bold">{service.duration} phút</span>
                          </div>
                        </td>

                        {/* Toggle switch */}
                        <td className="px-6 py-4.5 text-center">
                          <div className="inline-flex items-center justify-center">
                            <button
                              onClick={() => handleToggleStatus(service.id)}
                              className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                                service.status === "Active" ? "bg-green-500" : "bg-slate-400"
                              }`}
                            >
                              <span
                                className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-md ring-0 transition duration-200 ease-in-out ${
                                  service.status === "Active" ? "translate-x-5" : "translate-x-0"
                                }`}
                              />
                            </button>
                          </div>
                        </td>

                        {/* Action buttons */}
                        <td className="px-6 py-4.5">
                          <div className="flex items-center justify-center gap-4">
                            <Link
                              href={`/admin/services/${service.id}`}
                              title="Xem chi tiết"
                              className="p-1.5 rounded-lg text-slate-400 hover:text-blue-600 hover:bg-blue-50 transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                              </svg>
                            </Link>

                            <Link
                              href={`/admin/services/edit/${service.id}`}
                              title="Sửa thông tin"
                              className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                              </svg>
                            </Link>

                            <button
                              onClick={() => openDeleteModal(service)}
                              title="Xóa dịch vụ"
                              className="p-1.5 rounded-lg text-red-400 hover:text-primary hover:bg-red-50 transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                              </svg>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={6} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Không tìm thấy dịch vụ nào khớp với bộ lọc.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination bar */}
            {filteredServices.length > 0 && (
              <div className="p-4 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-2.5">
                {/* Page info */}
                <span className="text-[13px] text-slate-400 font-semibold text-center sm:text-left">
                  Hiển thị{" "}
                  <span className="text-slate-600 font-bold">{(currentPage - 1) * itemsPerPage + 1}–{Math.min(currentPage * itemsPerPage, filteredServices.length)}</span>
                  {" "}trong{" "}
                  <span className="text-slate-600 font-bold">{filteredServices.length}</span>
                  {" "}dịch vụ
                </span>
                <div className="flex items-center gap-1.5 sm:gap-2.5 flex-wrap justify-center">
                  {/* Quick First Page */}
                  <button
                    onClick={() => setCurrentPage(1)}
                    disabled={currentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;|
                  </button>
                  {/* Previous Page */}
                  <button
                    onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                    disabled={currentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;
                  </button>

                  {/* Pages indicator list */}
                  {Array.from({ length: totalPages }).map((_, idx) => {
                    const p = idx + 1;
                    const isActive = currentPage === p;
                    return (
                      <button
                        key={p}
                        onClick={() => setCurrentPage(p)}
                        className={`w-9 h-9 rounded-xl border flex items-center justify-center font-extrabold text-[14px] transition-all cursor-pointer ${
                          isActive
                            ? "bg-white border-primary text-primary shadow-sm font-black"
                            : "border-slate-200 text-slate-600 hover:bg-slate-50"
                        }`}
                      >
                        {p}
                      </button>
                    );
                  })}

                  {/* Next Page */}
                  <button
                    onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                    disabled={currentPage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === totalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &gt;
                  </button>
                  {/* Quick Last Page */}
                  <button
                    onClick={() => setCurrentPage(totalPages)}
                    disabled={currentPage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === totalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    |&gt;
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* THIẾT LẬP KHUYẾN MÃI */}
          <div id="promotions-section" className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="px-5 py-4 border-b border-slate-100 flex flex-col gap-3">
              {/* Row 1: Title + Create button */}
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-[18px] font-extrabold text-slate-900">Thiết lập khuyến mãi</h3>
                  <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Quản lý chiến dịch khuyến mãi cho dịch vụ.</p>
                </div>
                <Link
                  href="/admin/services/promotions/add"
                  className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] px-5 py-2.5 rounded-xl transition-all shadow-md shadow-primary/25 shrink-0"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  Thêm khuyến mãi
                </Link>
              </div>

              {/* Row 2: per-page + result count */}
              <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
                <span>Hiển thị</span>
                <div className="relative">
                  <select
                    value={promoItemsPerPage}
                    onChange={(e) => handlePromoItemsPerPageChange(Number(e.target.value))}
                    className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer"
                  >
                    {[5, 10, 20].map(n => (
                      <option key={n} value={n}>{n}</option>
                    ))}
                  </select>
                  <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </div>
                </div>
                <span>/ trang</span>
                <span className="text-slate-300 mx-1">·</span>
                <span>
                  Tìm thấy{" "}
                  <span className="text-slate-600 font-bold">{filteredPromotions.length}</span>
                  {" "}kết quả
                </span>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[14px]">
                <thead>
                  <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
                    <th className="px-6 py-4">Mã khuyến mãi</th>
                    <th className="px-6 py-4">Thiết lập khuyến mãi</th>
                    <th className="px-6 py-4">Giảm giá</th>
                    <th className="px-6 py-4">Thời gian áp dụng</th>
                    <th className="px-6 py-4 text-center">Trạng thái</th>
                    <th className="px-6 py-4 text-center">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                  {isPromosLoading ? (
                    <tr><td colSpan={6} className="px-6 py-8 text-center text-slate-400 text-[13px]">Đang tải...</td></tr>
                  ) : paginatedPromotions.length === 0 ? (
                    <tr><td colSpan={6} className="px-6 py-10 text-center text-slate-400 text-[13px]">Chưa có khuyến mãi nào</td></tr>
                  ) : paginatedPromotions.map((promo) => {
                    const fmt = (d: string) => d.split("-").reverse().join("/");
                    const discount = promo.discountType === "Percentage"
                      ? `-${promo.discountValue}%`
                      : `-${Number(promo.discountValue).toLocaleString("vi-VN")}đ`;
                    return (
                      <tr key={promo.id} className="hover:bg-slate-50/40 transition-colors">
                        <td className="px-6 py-4">
                          <span className={`inline-flex items-center px-3 py-1.5 rounded-lg font-black text-[13px] border ${promo.isActive ? "bg-primary/10 text-primary border-primary/20" : "bg-slate-100 text-slate-500 border-slate-200"}`}>
                            {promo.code}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          <div className="font-extrabold text-slate-900">{promo.name}</div>
                          <div className="text-[12px] text-slate-400 font-medium mt-0.5">
                            {promo.serviceNames.join(", ")}
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-green-50 text-green-600 font-black text-[13px] border border-green-100">
                            {discount}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-slate-500 text-[13px]">
                          {fmt(promo.startDate)} - {fmt(promo.endDate)}
                        </td>
                        <td className="px-6 py-4.5 text-center">
                          <div className="inline-flex items-center justify-center">
                            <button
                              onClick={() => handleTogglePromo(promo.id)}
                              className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                                promo.isActive ? "bg-green-500" : "bg-slate-400"
                              }`}
                            >
                              <span
                                className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-md ring-0 transition duration-200 ease-in-out ${
                                  promo.isActive ? "translate-x-5" : "translate-x-0"
                                }`}
                              />
                            </button>
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex items-center justify-center gap-4">
                            <Link
                              href={`/admin/services/promotions/${promo.id}`}
                              title="Xem chi tiết"
                              className="p-1.5 rounded-lg text-slate-400 hover:text-blue-600 hover:bg-blue-50 transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                              </svg>
                            </Link>

                            <button
                              title="Chỉnh sửa"
                              onClick={() => router.push(`/admin/services/promotions/edit/${promo.id}`)}
                              className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                              </svg>
                            </button>
                            <button
                              title="Xóa"
                              onClick={() => { setSelectedPromo(promo); setIsDeletePromoOpen(true); }}
                              className="p-1.5 rounded-lg text-red-400 hover:text-primary hover:bg-red-50 transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                              </svg>
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination bar */}
            {filteredPromotions.length > 0 && (
              <div className="p-4 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-2.5">
                {/* Page info */}
                <span className="text-[13px] text-slate-400 font-semibold text-center sm:text-left">
                  Hiển thị{" "}
                  <span className="text-slate-600 font-bold">{(promoCurrentPage - 1) * promoItemsPerPage + 1}–{Math.min(promoCurrentPage * promoItemsPerPage, filteredPromotions.length)}</span>
                  {" "}trong{" "}
                  <span className="text-slate-600 font-bold">{filteredPromotions.length}</span>
                  {" "}khuyến mãi
                </span>
                <div className="flex items-center gap-1.5 sm:gap-2.5 flex-wrap justify-center">
                  {/* Quick First Page */}
                  <button
                    onClick={() => setPromoCurrentPage(1)}
                    disabled={promoCurrentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      promoCurrentPage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;|
                  </button>
                  {/* Previous Page */}
                  <button
                    onClick={() => setPromoCurrentPage(prev => Math.max(1, prev - 1))}
                    disabled={promoCurrentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      promoCurrentPage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;
                  </button>

                  {/* Pages indicator list */}
                  {Array.from({ length: promoTotalPages }).map((_, idx) => {
                    const p = idx + 1;
                    const isActive = promoCurrentPage === p;
                    return (
                      <button
                        key={p}
                        onClick={() => setPromoCurrentPage(p)}
                        className={`w-9 h-9 rounded-xl border flex items-center justify-center font-extrabold text-[14px] transition-all cursor-pointer ${
                          isActive
                            ? "bg-white border-primary text-primary shadow-sm font-black"
                            : "border-slate-200 text-slate-600 hover:bg-slate-50"
                        }`}
                      >
                        {p}
                      </button>
                    );
                  })}

                  {/* Next Page */}
                  <button
                    onClick={() => setPromoCurrentPage(prev => Math.min(promoTotalPages, prev + 1))}
                    disabled={promoCurrentPage === promoTotalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      promoCurrentPage === promoTotalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &gt;
                  </button>
                  {/* Quick Last Page */}
                  <button
                    onClick={() => setPromoCurrentPage(promoTotalPages)}
                    disabled={promoCurrentPage === promoTotalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      promoCurrentPage === promoTotalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    |&gt;
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </main>

      {/* ── MODAL: XÁC NHẬN XÓA ──────────────────────────────────────────── */}
      {isDeleteModalOpen && selectedService && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto animate-fade-in">
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-md shadow-2xl p-6 relative flex flex-col gap-5">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-full bg-red-100 text-primary flex items-center justify-center">
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-[18px] font-black text-slate-900 leading-tight">Xóa Dịch Vụ</h3>
                  <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Hành động này không thể hoàn tác.</p>
                </div>
              </div>
              <button
                onClick={() => setIsDeleteModalOpen(false)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-all cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div className="bg-red-50/50 border border-red-100 rounded-xl p-4">
              <p className="text-[14px] text-slate-700 font-semibold leading-relaxed">
                Bạn có chắc chắn muốn xóa dịch vụ{" "}
                <span className="font-extrabold text-slate-900">"{selectedService.name}"</span> không? Toàn bộ dữ
                liệu liên quan sẽ bị mất vĩnh viễn.
              </p>
            </div>

            <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-4">
              <button
                onClick={() => setIsDeleteModalOpen(false)}
                className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleDelete}
                className="px-5 py-2.5 bg-red-500 hover:bg-red-600 text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-red-500/20 hover:shadow-lg transition-all cursor-pointer"
              >
                Xóa dịch vụ
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL: XÁC NHẬN XÓA KHUYẾN MÃI ─────────────────────────────── */}
      {isDeletePromoOpen && selectedPromo && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 animate-fade-in">
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-md shadow-2xl p-6 flex flex-col gap-5">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-full bg-red-100 text-primary flex items-center justify-center">
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-[18px] font-black text-slate-900 leading-tight">Xóa Khuyến Mãi</h3>
                  <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Hành động này không thể hoàn tác.</p>
                </div>
              </div>
              <button onClick={() => setIsDeletePromoOpen(false)} className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-all cursor-pointer">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <div className="bg-red-50/50 border border-red-100 rounded-xl p-4">
              <p className="text-[14px] text-slate-700 font-semibold leading-relaxed">
                Bạn có chắc chắn muốn xóa khuyến mãi{" "}
                <span className="font-extrabold text-slate-900">"{selectedPromo.name}"</span> không?
              </p>
            </div>
            <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-4">
              <button onClick={() => setIsDeletePromoOpen(false)} className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer">
                Hủy bỏ
              </button>
              <button onClick={handleDeletePromo} className="px-5 py-2.5 bg-red-500 hover:bg-red-600 text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-red-500/20 transition-all cursor-pointer">
                Xóa khuyến mãi
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
