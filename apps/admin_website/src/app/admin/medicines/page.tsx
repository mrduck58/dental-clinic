"use client";

import React, { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import AdminPageHeader from "../../../components/shared/AdminPageHeader";
import Pagination from "../../../components/shared/Pagination";
import { SortableTh, Th, toggleSortState, type SortDir } from "../../../components/shared/TableHeader";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import { getMedicinesApi, deleteMedicineApi, type MedicineDto } from "../../../lib/apiClient";

const ITEMS_PER_PAGE_DEFAULT = 5;

type SortKey = "name" | "genericName" | "unit" | "manufacturer";
const SORT_DESC_BY_DEFAULT = (_column: SortKey) => false;

export default function MedicinesPage() {
  useRequireAdmin();

  const [medicines, setMedicines] = useState<MedicineDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(ITEMS_PER_PAGE_DEFAULT);
  const [sortKey, setSortKey] = useState<SortKey>("name");
  const [sortDir, setSortDir] = useState<SortDir>("asc");
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedMedicine, setSelectedMedicine] = useState<MedicineDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const fetchMedicines = async () => {
    setIsLoading(true);
    setActionError(null);
    try {
      const data = await getMedicinesApi({
        search: searchQuery || undefined,
      });
      setMedicines(data);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Không thể tải danh sách thuốc");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchMedicines();
  }, []);

  const filteredMedicines = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return medicines.filter((medicine) => {
      const matchesSearch =
        !q ||
        medicine.name.toLowerCase().includes(q) ||
        medicine.genericName.toLowerCase().includes(q) ||
        medicine.manufacturer.toLowerCase().includes(q) ||
        (medicine.description ?? "").toLowerCase().includes(q);

      return matchesSearch;
    });
  }, [medicines, searchQuery]);

  const sortedMedicines = useMemo(() => {
    const dir = sortDir === "asc" ? 1 : -1;
    const value = (m: MedicineDto) => {
      switch (sortKey) {
        case "name": return m.name.toLowerCase();
        case "genericName": return m.genericName.toLowerCase();
        case "unit": return m.unit.toLowerCase();
        case "manufacturer": return m.manufacturer.toLowerCase();
      }
    };
    return [...filteredMedicines].sort((a, b) => value(a).localeCompare(value(b), "vi") * dir);
  }, [filteredMedicines, sortKey, sortDir]);

  const totalPages = Math.max(1, Math.ceil(sortedMedicines.length / itemsPerPage));
  const safeCurrentPage = Math.min(currentPage, totalPages);
  const paginatedMedicines = useMemo(() => {
    const start = (safeCurrentPage - 1) * itemsPerPage;
    return sortedMedicines.slice(start, start + itemsPerPage);
  }, [sortedMedicines, safeCurrentPage, itemsPerPage]);

  const handleItemsPerPageChange = (value: number) => {
    setItemsPerPage(value);
    setCurrentPage(1);
  };

  const handleSort = (column: SortKey) => {
    const next = toggleSortState({ key: sortKey, dir: sortDir }, column, SORT_DESC_BY_DEFAULT);
    setSortKey(next.key);
    setSortDir(next.dir);
    setCurrentPage(1);
  };

  const openDeleteModal = (medicine: MedicineDto) => {
    setSelectedMedicine(medicine);
    setIsDeleteModalOpen(true);
    setActionError(null);
  };

  const handleDelete = async () => {
    if (!selectedMedicine) return;
    try {
      await deleteMedicineApi(selectedMedicine.id);
      setMedicines((prev) => prev.filter((m) => m.id !== selectedMedicine.id));
      setIsDeleteModalOpen(false);
      setSelectedMedicine(null);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Xóa thuốc thất bại");
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="medicines" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <AdminPageHeader
          title="Quản Lí Thuốc"
          subtitle="Danh sách thuốc để bác sĩ kê đơn."
        />

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">
          {/* INFO BANNER */}
          <div className="bg-gradient-to-r from-primary/5 via-primary/10 to-primary/5 border border-primary/20 rounded-2xl px-6 py-4 flex items-center justify-between">
            <div>
              <h2 className="text-[15px] font-extrabold text-slate-800">Danh sách thuốc dành cho nha sĩ kê đơn</h2>
              <p className="text-[13px] text-slate-500 font-medium mt-0.5">Quản lý và tra cứu thông tin thuốc trong phòng khám</p>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <span className="text-3xl font-black text-primary">{medicines.length}</span>
              <span className="text-[14px] text-slate-500 font-semibold">loại thuốc</span>
            </div>
          </div>

          {/* TOOLBAR */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3">
            <div className="flex flex-col md:flex-row md:items-center gap-3">
              <div className="relative flex-1">
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
                  className="w-full pl-9.5 pr-4 py-2.5 text-[14px] bg-slate-100/80 border border-transparent rounded-xl focus:bg-white focus:border-slate-200 focus:outline-none transition-all font-semibold"
                />
              </div>

              <Link
                href="/admin/medicines/add"
                className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] px-5 py-2.5 rounded-xl transition-all shadow-md shadow-primary/25 shrink-0"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm thuốc
              </Link>
            </div>

            <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
              <span>Hiển thị</span>
              <div className="relative">
                <select
                  value={itemsPerPage}
                  onChange={(e) => handleItemsPerPageChange(Number(e.target.value))}
                  className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer"
                >
                  {[5, 10, 20].map((n) => (
                    <option key={n} value={n}>
                      {n}
                    </option>
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
                <span className="text-slate-600 font-bold">{filteredMedicines.length}</span>
                {" "}kết quả
              </span>
            </div>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
            {actionError && (
              <div className="px-6 py-3 bg-red-50 border-b border-red-100 text-[13px] text-red-600 font-semibold">
                {actionError}
              </div>
            )}
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[14px]">
                <thead>
                  <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
                    <Th className="px-6 w-20">Ảnh</Th>
                    <SortableTh column="name" label="Tên thuốc" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <SortableTh column="genericName" label="Hoạt chất" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <SortableTh column="unit" label="Đơn vị" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <SortableTh column="manufacturer" label="Nhà sản xuất" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <Th className="px-6">Mô tả</Th>
                    <Th className="px-6" align="center">Hành động</Th>
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
                      <tr key={medicine.id} className="hover:bg-slate-50/40 transition-colors">
                        <td className="px-6 py-4.5">
                          <div className="w-12 h-12 rounded-xl overflow-hidden bg-slate-100 flex items-center justify-center">
                            <svg className="w-6 h-6 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 9.75h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm-2.25.005h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm2.25-2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75zm5.25 0v.75h.75v-.75h-.75zm-.75 0h.75v.75h-.75v-.75zm-.75 0h.005v.005h-.005v-.005zm-.75 0h.005v.005h-.005v-.005zm.75-2.25h.005v.005h-.005v-.005zm0 2.25h.005v.005h-.005v-.005zm0 2.25h.75v.75h-.75v-.75zm-.75 0v.75h.75v-.75h-.75z" />
                              <circle cx="12" cy="12" r="8" />
                            </svg>
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
                          <span className="text-slate-500 text-[13px] line-clamp-1" title={medicine.description ?? ""}>
                            {medicine.description}
                          </span>
                        </td>
                        <td className="px-6 py-4.5">
                          <div className="flex items-center justify-center gap-1">
                            <Link
                              href={`/admin/medicines/edit/${medicine.id}`}
                              title="Sửa thông tin"
                              className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-all cursor-pointer"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                              </svg>
                            </Link>
                            <button
                              onClick={() => openDeleteModal(medicine)}
                              title="Xóa thuốc"
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
                      <td colSpan={8} className="px-6 py-10 text-center text-slate-400 font-bold">
                        Không tìm thấy thuốc nào.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination bar */}
            {filteredMedicines.length > 0 && (
              <div className="p-4 border-t border-slate-100">
                <Pagination
                  currentPage={currentPage}
                  totalCount={filteredMedicines.length}
                  pageSize={itemsPerPage}
                  onPageChange={setCurrentPage}
                  itemLabel="thuốc"
                />
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
                <div className="w-12 h-12 rounded-full bg-red-100 text-primary flex items-center justify-center">
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
