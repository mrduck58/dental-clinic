"use client";

import React, { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import {
  getServicesApi,
  deleteServiceApi,
  toggleServiceStatusApi,
  type ServiceDto,
} from "../../../lib/apiClient";

interface Service {
  id: string;
  name: string;
  category: string;
  price: number;
  duration: number;
  status: "Active" | "Inactive";
  description: string;
  popular: number;
}

const categories = ["Tất cả", "Niềng răng", "Tẩy trắng răng", "Trồng răng", "Lấy cao răng", "Điều trị tủy", "Nhổ răng", "Trám răng"];

function toService(dto: ServiceDto): Service {
  return {
    id: dto.id,
    name: dto.name,
    category: dto.category,
    price: dto.price,
    duration: dto.durationMinutes,
    status: dto.isActive ? "Active" : "Inactive",
    description: dto.description,
    popular: dto.viewCount,
  };
}

const ITEMS_PER_PAGE = 5;

export default function ServicesPage() {
  useRequireAdmin();
  const [services, setServices] = useState<Service[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("Tất cả");
  const [statusFilter, setStatusFilter] = useState("Tất cả");

  const [currentPage, setCurrentPage] = useState(1);

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedService, setSelectedService] = useState<Service | null>(null);

  useEffect(() => {
    getServicesApi()
      .then((data) => setServices(data.map(toService)))
      .finally(() => setIsLoading(false));
  }, []);

  const stats = useMemo(() => {
    const total = services.length;
    const active = services.filter((s) => s.status === "Active").length;
    const inactive = services.filter((s) => s.status === "Inactive").length;
    const mostPopular = services.length > 0
      ? services.reduce((max, s) => (s.popular > max.popular ? s : max), services[0])
      : null;

    return { total, active, inactive, mostPopular };
  }, [services]);

  const filteredServices = useMemo(() => {
    return services.filter((service) => {
      const matchesSearch =
        service.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        service.description.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesCategory = categoryFilter === "Tất cả" || service.category === categoryFilter;
      const matchesStatus = statusFilter === "Tất cả" || service.status === statusFilter;

      return matchesSearch && matchesCategory && matchesStatus;
    });
  }, [services, searchQuery, categoryFilter, statusFilter]);

  const totalPages = Math.ceil(filteredServices.length / ITEMS_PER_PAGE);
  const paginatedServices = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE;
    return filteredServices.slice(start, start + ITEMS_PER_PAGE);
  }, [filteredServices, currentPage]);

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

  const getCategoryBadgeClass = (category: string) => {
    switch (category) {
      case "Niềng răng":
        return "bg-purple-50 text-purple-600 border-purple-100";
      case "Tẩy trắng răng":
        return "bg-cyan-50 text-cyan-600 border-cyan-100";
      case "Trồng răng":
        return "bg-emerald-50 text-emerald-600 border-emerald-100";
      case "Lấy cao răng":
        return "bg-teal-50 text-teal-600 border-teal-100";
      case "Điều trị tủy":
        return "bg-rose-50 text-rose-600 border-rose-100";
      case "Nhổ răng":
        return "bg-orange-50 text-orange-600 border-orange-100";
      case "Trám răng":
        return "bg-amber-50 text-amber-600 border-amber-100";
      default:
        return "bg-slate-50 text-slate-600 border-slate-100";
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="services" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Quản Lí Dịch Vụ</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Quản lý danh sách dịch vụ và cấu hình thông tin.
            </p>
          </div>

          <NotificationBell />
        </header>

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
                    <span className="text-3xl font-black text-white leading-none">03</span>
                    <span className="px-2 py-0.5 rounded-full bg-white/20 text-white text-[12px] font-bold">
                      đang chạy
                    </span>
                  </div>
                </div>
              </div>
              <div className="mt-3 flex items-center gap-2 overflow-hidden">
                <div className="flex gap-1.5 animate-marquee">
                  <span className="px-2 py-1 rounded-lg bg-white/20 text-white text-[11px] font-bold whitespace-nowrap">SUMMER2024</span>
                  <span className="px-2 py-1 rounded-lg bg-white/20 text-white text-[11px] font-bold whitespace-nowrap">IMPLANT50</span>
                  <span className="px-2 py-1 rounded-lg bg-white/20 text-white text-[11px] font-bold whitespace-nowrap">NEWPATIENT</span>
                </div>
              </div>
            </div>
          </div>

          {/* TOOLBAR */}
          <div className="bg-white p-4.5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4 shrink-0">
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3.5 flex-1 max-w-3xl">
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
                  className="w-full pl-9.5 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>

              {/* Category filter */}
              <div className="relative">
                <select
                  value={categoryFilter}
                  onChange={(e) => {
                    setCategoryFilter(e.target.value);
                    setCurrentPage(1);
                  }}
                  className="w-full sm:w-48 px-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer"
                >
                  {categories.map((cat) => (
                    <option key={cat} value={cat}>{cat}</option>
                  ))}
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              {/* Status filter */}
              <div className="relative">
                <select
                  value={statusFilter}
                  onChange={(e) => {
                    setStatusFilter(e.target.value);
                    setCurrentPage(1);
                  }}
                  className="w-full sm:w-36 px-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-bold text-slate-600 appearance-none pr-8 cursor-pointer"
                >
                  <option value="Tất cả">Tất cả</option>
                  <option value="Active">Hoạt động</option>
                  <option value="Inactive">Tạm ngưng</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
            </div>

            {/* Add service button */}
            <Link
              href="/dashboard/services/add"
              className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold px-5 py-2.5 rounded-xl shadow-md shadow-primary/20 hover:shadow-lg hover:shadow-primary/30 transition-all hover:translate-y-[-1px] cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              Thêm dịch vụ
            </Link>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13px] sm:text-[14px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 select-none">
                    <th className="px-6 py-4">Dịch vụ</th>
                    <th className="px-6 py-4">Danh mục</th>
                    <th className="px-6 py-4">Giá dịch vụ</th>
                    <th className="px-6 py-4">Thời gian</th>
                    <th className="px-6 py-4 text-center">Trạng thái</th>
                    <th className="px-6 py-4 text-right">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-150/70 font-semibold text-slate-600">
                  {isLoading ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Đang tải dữ liệu...
                      </td>
                    </tr>
                  ) : paginatedServices.length > 0 ? (
                    paginatedServices.map((service) => (
                      <tr key={service.id} className="hover:bg-slate-50/20 transition-colors">
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

                        {/* Category badge */}
                        <td className="px-6 py-4.5">
                          <span
                            className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border ${getCategoryBadgeClass(
                              service.category
                            )}`}
                          >
                            {service.category}
                          </span>
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
                                service.status === "Active" ? "bg-green-500" : "bg-slate-250"
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
                        <td className="px-6 py-4.5 text-right">
                          <div className="flex items-center justify-end gap-2.5">
                            <Link
                              href={`/dashboard/services/edit/${service.id}`}
                              title="Sửa thông tin"
                              className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                              </svg>
                            </Link>

                            <button
                              onClick={() => openDeleteModal(service)}
                              title="Xóa dịch vụ"
                              className="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
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

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="px-6 py-4 border-t border-slate-200/80 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-50/30">
                <span className="text-[13px] text-slate-500 font-semibold">
                  Hiển thị {(currentPage - 1) * ITEMS_PER_PAGE + 1} - {Math.min(currentPage * ITEMS_PER_PAGE, filteredServices.length)} của {filteredServices.length} dịch vụ
                </span>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={currentPage === 1}
                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-600 font-bold text-[13px] hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                    </svg>
                  </button>

                  {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                    <button
                      key={page}
                      onClick={() => setCurrentPage(page)}
                      className={`w-8 h-8 rounded-lg font-bold text-[13px] transition-all ${
                        page === currentPage
                          ? "bg-primary text-white shadow-md"
                          : "bg-white border border-slate-200 text-slate-600 hover:bg-slate-50"
                      }`}
                    >
                      {page}
                    </button>
                  ))}

                  <button
                    onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                    disabled={currentPage === totalPages}
                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-600 font-bold text-[13px] hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                    </svg>
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* THIẾT LẬP KHUYẾN MÃI */}
          <div id="promotions-section" className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="p-5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div>
                <h3 className="text-[18px] font-extrabold text-slate-900">Thiết lập khuyến mãi</h3>
                <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Quản lý chiến dịch khuyến mãi cho dịch vụ.</p>
              </div>
              <Link
                href="/dashboard/services/promotions/add"
                className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold px-5 py-2.5 rounded-xl shadow-md shadow-primary/20 hover:shadow-lg hover:shadow-primary/30 transition-all hover:-translate-y-0.5 cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm khuyến mãi
              </Link>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13px] sm:text-[14px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 select-none">
                    <th className="px-6 py-4">Mã khuyến mãi</th>
                    <th className="px-6 py-4">Thiết lập khuyến mãi</th>
                    <th className="px-6 py-4">Giảm giá</th>
                    <th className="px-6 py-4">Thời gian áp dụng</th>
                    <th className="px-6 py-4 text-center">Trạng thái</th>
                    <th className="px-6 py-4 text-right">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-150/70 font-semibold text-slate-600">
                  <tr className="hover:bg-slate-50/20 transition-colors">
                    <td className="px-6 py-4.5">
                      <span className="inline-flex items-center px-3 py-1.5 rounded-lg bg-primary/10 text-primary font-black text-[13px] border border-primary/20">
                        SUMMER2024
                      </span>
                    </td>
                    <td className="px-6 py-4.5">
                      <div className="font-extrabold text-slate-900">Khuyến mãi mùa hè</div>
                      <div className="text-[12px] text-slate-400 font-medium mt-0.5">Niềng răng, Tẩy trắng</div>
                    </td>
                    <td className="px-6 py-4.5">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-green-50 text-green-600 font-black text-[13px] border border-green-100">
                        -15%
                      </span>
                    </td>
                    <td className="px-6 py-4.5 text-slate-500 text-[13px]">
                      01/06/2024 - 31/08/2024
                    </td>
                    <td className="px-6 py-4.5 text-center">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-green-50 text-green-600 text-[12px] font-bold">
                        Đang chạy
                      </span>
                    </td>
                    <td className="px-6 py-4.5 text-right">
                      <div className="flex items-center justify-end gap-2.5">
                        <button
                          title="Chỉnh sửa"
                          className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                        >
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>

                  <tr className="hover:bg-slate-50/20 transition-colors">
                    <td className="px-6 py-4.5">
                      <span className="inline-flex items-center px-3 py-1.5 rounded-lg bg-primary/10 text-primary font-black text-[13px] border border-primary/20">
                        IMPLANT50
                      </span>
                    </td>
                    <td className="px-6 py-4.5">
                      <div className="font-extrabold text-slate-900">Giảm 5 triệu Implant</div>
                      <div className="text-[12px] text-slate-400 font-medium mt-0.5">Trồng răng Implant</div>
                    </td>
                    <td className="px-6 py-4.5">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-green-50 text-green-600 font-black text-[13px] border border-green-100">
                        -5.000.000đ
                      </span>
                    </td>
                    <td className="px-6 py-4.5 text-slate-500 text-[13px]">
                      01/07/2024 - 31/12/2024
                    </td>
                    <td className="px-6 py-4.5 text-center">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-green-50 text-green-600 text-[12px] font-bold">
                        Đang chạy
                      </span>
                    </td>
                    <td className="px-6 py-4.5 text-right">
                      <div className="flex items-center justify-end gap-2.5">
                        <button
                          title="Chỉnh sửa"
                          className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                        >
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>

                  <tr className="hover:bg-slate-50/20 transition-colors">
                    <td className="px-6 py-4.5">
                      <span className="inline-flex items-center px-3 py-1.5 rounded-lg bg-slate-100 text-slate-500 font-black text-[13px] border border-slate-200">
                        NEWPATIENT
                      </span>
                    </td>
                    <td className="px-6 py-4.5">
                      <div className="font-extrabold text-slate-900">Khách hàng mới</div>
                      <div className="text-[12px] text-slate-400 font-medium mt-0.5">Tất cả dịch vụ</div>
                    </td>
                    <td className="px-6 py-4.5">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-green-50 text-green-600 font-black text-[13px] border border-green-100">
                        -10%
                      </span>
                    </td>
                    <td className="px-6 py-4.5 text-slate-500 text-[13px]">
                      01/01/2024 - 31/12/2024
                    </td>
                    <td className="px-6 py-4.5 text-center">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-slate-100 text-slate-500 text-[12px] font-bold">
                        Tạm dừng
                      </span>
                    </td>
                    <td className="px-6 py-4.5 text-right">
                      <div className="flex items-center justify-end gap-2.5">
                        <button
                          title="Chỉnh sửa"
                          className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                        >
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
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
    </div>
  );
}
