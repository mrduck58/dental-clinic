"use client";

import React, { useState, useMemo } from "react";
import Link from "next/link";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";

interface Medicine {
  id: string;
  name: string;
  genericName: string;
  unit: string;
  manufacturer: string;
  usage: string;
  imageUrl?: string;
}

const mockMedicines: Medicine[] = [
  { id: "1", name: "Amoxicillin 500mg", genericName: "Amoxicillin", unit: "Viên", manufacturer: "Domesco", usage: "Kháng sinh điều trị nhiễm khuẩn", imageUrl: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=100&h=100&fit=crop" },
  { id: "2", name: "Paracetamol 500mg", genericName: "Acetaminophen", unit: "Viên", manufacturer: "Stada", usage: "Hạ sốt, giảm đau", imageUrl: "https://images.unsplash.com/photo-1550572017-edd951b55104?w=100&h=100&fit=crop" },
  { id: "3", name: "Ibuprofen 400mg", genericName: "Ibuprofen", unit: "Viên", manufacturer: "Berkem", usage: "Giảm đau, kháng viêm", imageUrl: "https://images.unsplash.com/photo-1550572017-4ed1bd8b0c4d?w=100&h=100&fit=crop" },
  { id: "4", name: "Metronidazole 500mg", genericName: "Metronidazole", unit: "Viên", manufacturer: "Pharmamed", usage: "Kháng khuẩn, điều trị nhiễm trùng", imageUrl: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=100&h=100&fit=crop" },
  { id: "5", name: "Omeprazole 20mg", genericName: "Omeprazole", unit: "Viên", manufacturer: "Dopharma", usage: "Giảm tiết axit dạ dày", imageUrl: "https://images.unsplash.com/photo-1550572017-edd951b55104?w=100&h=100&fit=crop" },
  { id: "6", name: "Vitamin B-Complex", genericName: "Vitamin B Complex", unit: "Viên", manufacturer: "Pharbaco", usage: "Bổ sung vitamin nhóm B", imageUrl: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=100&h=100&fit=crop" },
  { id: "7", name: "Dexamethasone 0.5mg", genericName: "Dexamethasone", unit: "Viên", manufacturer: "DKSH", usage: "Kháng viêm, chống dị ứng", imageUrl: "https://images.unsplash.com/photo-1550572017-4ed1bd8b0c4d?w=100&h=100&fit=crop" },
  { id: "8", name: "Ciprofloxacin 500mg", genericName: "Ciprofloxacin", unit: "Viên", manufacturer: "Domesco", usage: "Kháng sinh fluoroquinolone", imageUrl: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=100&h=100&fit=crop" },
  { id: "9", name: "Aspirin 81mg", genericName: "Acetylsalicylic acid", unit: "Viên", manufacturer: "Bayer", usage: "Chống kết tập tiểu cầu", imageUrl: "https://images.unsplash.com/photo-1550572017-edd951b55104?w=100&h=100&fit=crop" },
  { id: "10", name: "Lidocaine 2%", genericName: "Lidocaine", unit: "Ống", manufacturer: "Mekophar", usage: "Gây tê cục bộ trong nha khoa", imageUrl: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=100&h=100&fit=crop" },
  { id: "11", name: "Augmentin 625mg", genericName: "Amoxicillin/Clavulanic acid", unit: "Viên", manufacturer: "GSK", usage: "Kháng sinh phổ rộng", imageUrl: "https://images.unsplash.com/photo-1550572017-4ed1bd8b0c4d?w=100&h=100&fit=crop" },
  { id: "12", name: "Clindamycin 300mg", genericName: "Clindamycin", unit: "Viên", manufacturer: "Pfizer", usage: "Kháng sinh điều trị viêm nhiễm", imageUrl: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=100&h=100&fit=crop" },
];

const ITEMS_PER_PAGE = 5;

export default function MedicinesPage() {
  useRequireAdmin();

  const [medicines, setMedicines] = useState<Medicine[]>(mockMedicines);
  const [isLoading, setIsLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [filterUnit, setFilterUnit] = useState("");
  const [filterManufacturer, setFilterManufacturer] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedMedicine, setSelectedMedicine] = useState<Medicine | null>(null);

  const units = Array.from(new Set(mockMedicines.map((m) => m.unit))).sort();
  const manufacturers = Array.from(new Set(mockMedicines.map((m) => m.manufacturer))).sort();

  const filteredMedicines = useMemo(() => {
    return medicines.filter((medicine) => {
      const matchesSearch =
        medicine.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        medicine.genericName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        medicine.manufacturer.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesUnit = !filterUnit || medicine.unit === filterUnit;
      const matchesManufacturer = !filterManufacturer || medicine.manufacturer === filterManufacturer;
      return matchesSearch && matchesUnit && matchesManufacturer;
    });
  }, [medicines, searchQuery, filterUnit, filterManufacturer]);

  const totalPages = Math.ceil(filteredMedicines.length / ITEMS_PER_PAGE);
  const paginatedMedicines = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE;
    return filteredMedicines.slice(start, start + ITEMS_PER_PAGE);
  }, [filteredMedicines, currentPage]);

  const openDeleteModal = (medicine: Medicine) => {
    setSelectedMedicine(medicine);
    setIsDeleteModalOpen(true);
  };

  const handleDelete = () => {
    if (!selectedMedicine) return;
    setMedicines((prev) => prev.filter((m) => m.id !== selectedMedicine.id));
    setIsDeleteModalOpen(false);
    setSelectedMedicine(null);
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="medicines" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Quản Lí Thuốc</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Danh sách thuốc để bác sĩ kê đơn.
            </p>
          </div>

          <NotificationBell />
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">
          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                  Tổng thuốc
                </span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{medicines.length}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm-2.25.005h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm2.25-2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75zm5.25 0v.75h.75v-.75h-.75zm-.75 0h.75v.75h-.75v-.75zm-.75 0h.005v.005h-.005v-.005zm-.75 0h.005v.005h-.005v-.005zm.75-2.25h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm0 2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75z" />
                  <circle cx="12" cy="12" r="8" strokeWidth="1.5" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                  Hoạt chất
                </span>
                <span className="text-3xl font-black text-slate-900 block mt-1">
                  {new Set(medicines.map((m) => m.genericName)).size}
                </span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-green-50 text-green-600 flex items-center justify-center">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75l4.5 4.5m0-4.5l-4.5 4.5M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                  Nhà sản xuất
                </span>
                <span className="text-3xl font-black text-slate-900 block mt-1">
                  {new Set(medicines.map((m) => m.manufacturer)).size}
                </span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                </svg>
              </div>
            </div>
          </div>

          {/* TOOLBAR */}
          <div className="bg-white p-4.5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4 shrink-0">
            <div className="flex items-center gap-3.5 flex-1 flex-wrap">
              <div className="relative flex-1 min-w-[200px] max-w-[300px]">
                <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm theo tên thuốc, hoạt chất..."
                  value={searchQuery}
                  onChange={(e) => {
                    setSearchQuery(e.target.value);
                    setCurrentPage(1);
                  }}
                  className="w-full pl-9.5 pr-4 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>

              <div className="relative">
                <select
                  value={filterUnit}
                  onChange={(e) => {
                    setFilterUnit(e.target.value);
                    setCurrentPage(1);
                  }}
                  className="pl-4 pr-9 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer"
                >
                  <option value="">Tất cả đơn vị</option>
                  {units.map((unit) => (
                    <option key={unit} value={unit}>{unit}</option>
                  ))}
                </select>
                <span className="absolute inset-y-0 right-2.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>

              <div className="relative">
                <select
                  value={filterManufacturer}
                  onChange={(e) => {
                    setFilterManufacturer(e.target.value);
                    setCurrentPage(1);
                  }}
                  className="pl-4 pr-9 py-2.5 text-[14px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer"
                >
                  <option value="">Tất cả nhà sản xuất</option>
                  {manufacturers.map((mfr) => (
                    <option key={mfr} value={mfr}>{mfr}</option>
                  ))}
                </select>
                <span className="absolute inset-y-0 right-2.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
            </div>

            <Link
              href="/dashboard/medicines/add"
              className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold px-5 py-2.5 rounded-xl shadow-md shadow-primary/20 hover:shadow-lg hover:shadow-primary/30 transition-all hover:translate-y-[-1px] cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              Thêm thuốc
            </Link>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[13px] sm:text-[14px]">
                <thead>
                  <tr className="bg-slate-50/70 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 select-none">
                    <th className="px-6 py-4 w-20">Ảnh</th>
                    <th className="px-6 py-4">Tên thuốc</th>
                    <th className="px-6 py-4">Hoạt chất</th>
                    <th className="px-6 py-4">Đơn vị</th>
                    <th className="px-6 py-4">Nhà sản xuất</th>
                    <th className="px-6 py-4">Công dụng</th>
                    <th className="px-6 py-4 text-right">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                  {isLoading ? (
                    <tr>
                      <td colSpan={7} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Đang tải dữ liệu...
                      </td>
                    </tr>
                  ) : paginatedMedicines.length > 0 ? (
                    paginatedMedicines.map((medicine) => (
                      <tr key={medicine.id} className="hover:bg-slate-50/20 transition-colors">
                        <td className="px-6 py-4.5">
                          <div className="w-12 h-12 rounded-xl overflow-hidden bg-slate-100 flex items-center justify-center">
                            {medicine.imageUrl ? (
                              <img
                                src={medicine.imageUrl}
                                alt={medicine.name}
                                className="w-full h-full object-cover"
                              />
                            ) : (
                              <svg className="w-6 h-6 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm-2.25.005h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm2.25-2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75zm5.25 0v.75h.75v-.75h-.75zm-.75 0h.75v.75h-.75v-.75zm-.75 0h.005v.005h-.005v-.005zm-.75 0h.005v.005h-.005v-.005zm.75-2.25h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm0 2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75z" />
                                <circle cx="12" cy="12" r="8" />
                              </svg>
                            )}
                          </div>
                        </td>
                        <td className="px-6 py-4.5">
                          <div className="font-extrabold text-slate-900">{medicine.name}</div>
                        </td>
                        <td className="px-6 py-4.5">
                          <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-blue-50 text-blue-600 font-bold text-[12px]">
                            {medicine.genericName}
                          </span>
                        </td>
                        <td className="px-6 py-4.5">
                          <span className="text-slate-600">{medicine.unit}</span>
                        </td>
                        <td className="px-6 py-4.5">
                          <span className="text-slate-500">{medicine.manufacturer}</span>
                        </td>
                        <td className="px-6 py-4.5">
                          <span className="text-slate-500 text-[13px] line-clamp-1" title={medicine.usage}>
                            {medicine.usage}
                          </span>
                        </td>
                        <td className="px-6 py-4.5 text-right">
                          <div className="flex items-center justify-end gap-2.5">
                            <Link
                              href={`/dashboard/medicines/edit/${medicine.id}`}
                              title="Sửa thông tin"
                              className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                              </svg>
                            </Link>
                            <button
                              onClick={() => openDeleteModal(medicine)}
                              title="Xóa thuốc"
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
                      <td colSpan={7} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Không tìm thấy thuốc nào.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className="px-6 py-4 border-t border-slate-200/80 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-50/30">
                <span className="text-[13px] text-slate-500 font-semibold">
                  Hiển thị {(currentPage - 1) * ITEMS_PER_PAGE + 1} - {Math.min(currentPage * ITEMS_PER_PAGE, filteredMedicines.length)} của {filteredMedicines.length} thuốc
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
        </div>
      </main>

      {isDeleteModalOpen && selectedMedicine && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto animate-fade-in">
          <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-md shadow-2xl p-6 relative flex flex-col gap-5">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-full bg-red-100 text-red-600 flex items-center justify-center">
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-[18px] font-black text-slate-900 leading-tight">Xóa Thuốc</h3>
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
                Bạn có chắc chắn muốn xóa thuốc{" "}
                <span className="font-extrabold text-slate-900">"{selectedMedicine.name}"</span> không?
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
                Xóa thuốc
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
