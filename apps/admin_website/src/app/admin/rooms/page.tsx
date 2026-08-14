"use client";

import React, { useState, useEffect, useMemo, useRef } from "react";
import Link from "next/link";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import AdminPageHeader from "../../../components/shared/AdminPageHeader";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import {
  getRoomsApi,
  updateRoomApi,
  deleteRoomApi,
  changeRoomStatusApi,
  getWeekScheduleApi,
  type RoomDto,
  type ScheduleEntryDto,
} from "../../../lib/apiClient";
import { SHIFTS } from "../../../lib/shifts";

// ── Types ───────────────────────────────────────────────────────────────────

type RoomStatus = "Trống" | "Đang khám" | "Đang vệ sinh" | "Bảo trì" | "Ngừng hoạt động";

const getTodayStr = (): string => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
};

const getMondayStr = (): string => {
  const today = new Date();
  const day = today.getDay();
  const diff = today.getDate() - day + (day === 0 ? -6 : 1);
  const monday = new Date(today);
  monday.setDate(diff);
  return `${monday.getFullYear()}-${String(monday.getMonth() + 1).padStart(2, "0")}-${String(monday.getDate()).padStart(2, "0")}`;
};

export default function RoomsPage() {
  useRequireAdmin();

  // ── States ─────────────────────────────────────────────────────────────────
  const [rooms, setRooms] = useState<RoomDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [selectedFloor, setSelectedFloor] = useState<string>("all");
  const [selectedStatus, setSelectedStatus] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [currentPage, setCurrentPage] = useState(1);
  const [roomsPerPage, setRoomsPerPage] = useState(5);

  const [sortField, setSortField] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");

  const [showStatusFilterDropdown, setShowStatusFilterDropdown] = useState(false);

  const [showDetailModal, setShowDetailModal] = useState(false);
  const [showEditRoomModal, setShowEditRoomModal] = useState(false);
  const [showStatusModal, setShowStatusModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);

  const [selectedRoom, setSelectedRoom] = useState<RoomDto | null>(null);
  const [roomToDelete, setRoomToDelete] = useState<RoomDto | null>(null);

  const [editRoomName, setEditRoomName] = useState("");
  const [editRoomCode, setEditRoomCode] = useState("");
  const [editRoomFloor, setEditRoomFloor] = useState("");
  const [editRoomType, setEditRoomType] = useState("");
  const [editRoomDescription, setEditRoomDescription] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  const [statusValue, setStatusValue] = useState<RoomStatus>("Trống");

  const [toast, setToast] = useState<{ message: string; type: "success" | "error" | "info" } | null>(null);

  const [todaySchedule, setTodaySchedule] = useState<ScheduleEntryDto[]>([]);
  const [isLoadingSchedule, setIsLoadingSchedule] = useState(false);

  const statusFilterRef = useRef<HTMLDivElement>(null);

  // ── Load rooms từ API ─────────────────────────────────────────────────────
  const loadRooms = async () => {
    setIsLoading(true);
    setErrorMsg(null);
    try {
      const data = await getRoomsApi();
      setRooms(data);
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : "Không thể tải danh sách phòng");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { loadRooms(); }, []);

  const showToast = (message: string, type: "success" | "error" | "info" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  // ── Available floors (derived from actual data) ───────────────────────────
  const availableFloors = useMemo(() =>
    [...new Set(rooms.map(r => r.floor))].sort((a, b) => Number(a) - Number(b)),
  [rooms]);

  // Reset page khi filter thay đổi
  useEffect(() => { setCurrentPage(1); }, [searchQuery, selectedFloor, selectedStatus, roomsPerPage]);

  // Reset floor filter nếu tầng đang chọn không còn phòng nào
  useEffect(() => {
    if (selectedFloor !== "all" && !availableFloors.includes(selectedFloor))
      setSelectedFloor("all");
  }, [availableFloors, selectedFloor]);

  // ── Stats ─────────────────────────────────────────────────────────────────
  const stats = useMemo(() => ({
    total: rooms.length,
    vacant: rooms.filter(r => r.status === "Trống").length,
    using: rooms.filter(r => r.status === "Đang khám").length,
    maintenance: rooms.filter(r => r.status === "Bảo trì").length,
    cleaning: rooms.filter(r => r.status === "Đang vệ sinh").length,
    inactive: rooms.filter(r => r.status === "Ngừng hoạt động").length,
  }), [rooms]);

  // ── Sort ──────────────────────────────────────────────────────────────────
  const handleSort = (field: string) => {
    if (sortField === field) setSortDirection(prev => prev === "asc" ? "desc" : "asc");
    else { setSortField(field); setSortDirection("asc"); }
  };

  // ── Filter + Sort ─────────────────────────────────────────────────────────
  const sortedAndFilteredRooms = useMemo(() => {
    const filtered = rooms.filter(room => {
      const matchesFloor = selectedFloor === "all" || room.floor === selectedFloor;
      const matchesStatus = selectedStatus === "all" || room.status === selectedStatus;
      const q = searchQuery.toLowerCase();
      const matchesSearch = !q ||
        room.name.toLowerCase().includes(q) ||
        room.code.toLowerCase().includes(q) ||
        room.type.toLowerCase().includes(q);
      return matchesFloor && matchesStatus && matchesSearch;
    });

    if (!sortField) return filtered;

    return [...filtered].sort((a, b) => {
      const valA = (a[sortField as keyof RoomDto] ?? "").toString().toLowerCase();
      const valB = (b[sortField as keyof RoomDto] ?? "").toString().toLowerCase();
      if (valA < valB) return sortDirection === "asc" ? -1 : 1;
      if (valA > valB) return sortDirection === "asc" ? 1 : -1;
      return 0;
    });
  }, [rooms, selectedFloor, selectedStatus, searchQuery, sortField, sortDirection]);

  // ── Pagination ────────────────────────────────────────────────────────────
  const totalPages = Math.max(1, Math.ceil(sortedAndFilteredRooms.length / roomsPerPage));
  const startIndex = (currentPage - 1) * roomsPerPage;
  const paginatedRooms = sortedAndFilteredRooms.slice(startIndex, startIndex + roomsPerPage);

  // ── Handlers ──────────────────────────────────────────────────────────────

  const handleEditRoomSubmit = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedRoom || !editRoomName.trim() || !editRoomCode.trim()) return;
    setIsSaving(true);
    try {
      const updated = await updateRoomApi(selectedRoom.id, {
        code: editRoomCode.trim(),
        name: editRoomName.trim(),
        floor: editRoomFloor,
        type: editRoomType,
        description: editRoomDescription.trim(),
      });
      setRooms(prev => prev.map(r => r.id === updated.id ? updated : r));
      setShowEditRoomModal(false);
      setSelectedRoom(null);
      showToast("Cập nhật phòng thành công!", "success");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Cập nhật thất bại", "error");
    } finally {
      setIsSaving(false);
    }
  };

  const handleStatusSubmit = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedRoom) return;
    setIsSaving(true);
    try {
      const updated = await changeRoomStatusApi(selectedRoom.id, statusValue);
      setRooms(prev => prev.map(r => r.id === updated.id ? updated : r));
      setShowStatusModal(false);
      setSelectedRoom(null);
      showToast("Cập nhật trạng thái thành công!", "success");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Cập nhật trạng thái thất bại", "error");
    } finally {
      setIsSaving(false);
    }
  };

  const handleConfirmDelete = async () => {
    if (!roomToDelete) return;
    setIsSaving(true);
    try {
      await deleteRoomApi(roomToDelete.id);
      setRooms(prev => prev.filter(r => r.id !== roomToDelete.id));
      setShowDeleteModal(false);
      setRoomToDelete(null);
      showToast("Đã xóa phòng thành công!", "success");
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Xóa phòng thất bại", "error");
    } finally {
      setIsSaving(false);
    }
  };

  const openDetailModal = (room: RoomDto) => {
    setSelectedRoom(room);
    setShowDetailModal(true);
    setIsLoadingSchedule(true);
    getWeekScheduleApi(getMondayStr())
      .then(entries => {
        const today = getTodayStr();
        setTodaySchedule(entries.filter(e => e.date === today && e.room === room.name && !e.isHoliday));
      })
      .catch(() => setTodaySchedule([]))
      .finally(() => setIsLoadingSchedule(false));
  };
  const closeDetailModal = () => { setShowDetailModal(false); setSelectedRoom(null); setTodaySchedule([]); };

  const openEditModal = (room: RoomDto) => {
    setSelectedRoom(room);
    setEditRoomName(room.name);
    setEditRoomCode(room.code);
    setEditRoomFloor(room.floor);
    setEditRoomType(room.type);
    setEditRoomDescription(room.description ?? "");
    setShowEditRoomModal(true);
  };

  const openStatusModal = (room: RoomDto) => {
    setSelectedRoom(room);
    setStatusValue(room.status as RoomStatus);
    setShowStatusModal(true);
  };

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (statusFilterRef.current && !statusFilterRef.current.contains(event.target as Node))
        setShowStatusFilterDropdown(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Khóa scroll nền khi có modal mở
  const isAnyModalOpen = showDetailModal || showEditRoomModal || showStatusModal || showDeleteModal;
  useEffect(() => {
    document.body.style.overflow = isAnyModalOpen ? "hidden" : "";
    return () => { document.body.style.overflow = ""; };
  }, [isAnyModalOpen]);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Đang khám":
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-red-50 text-primary border border-red-200">Đang khám</span>;
      case "Trống":
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">Trống</span>;
      case "Đang vệ sinh":
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-blue-50 text-blue-700 border border-blue-200">Đang vệ sinh</span>;
      case "Bảo trì":
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-amber-50 text-amber-700 border border-amber-200">Bảo trì</span>;
      case "Ngừng hoạt động":
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-slate-100 text-slate-600 border border-slate-300">Ngừng hoạt động</span>;
      default:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-slate-100 text-slate-600 border border-slate-200">{status}</span>;
    }
  };

  const renderSortHeader = (label: string, field: string) => {
    const isSorted = sortField === field;
    return (
      <button
        onClick={() => handleSort(field)}
        className="flex items-center gap-1.5 hover:text-slate-800 transition-colors uppercase font-bold text-[12px] tracking-wider cursor-pointer focus:outline-none"
      >
        <span>{label}</span>
        <span className="inline-block text-slate-400 select-none text-[10px] ml-0.5">
          {!isSorted ? "⇅" : sortDirection === "asc" ? "▲" : "▼"}
        </span>
      </button>
    );
  };

  const formatDate = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
  };

  return (
    <>
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">

      <AdminSidebar activeMenu="rooms" />

      <main className="flex-1 flex flex-col min-w-0">

        {/* Header */}
        <AdminPageHeader title="Quản lý Phòng khám" />


        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">

          {/* ── STATS ──────────────────────────────────────────────────────── */}
          <section className="grid grid-cols-2 lg:grid-cols-4 gap-4 shrink-0">
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between">
              <div className="flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Tổng số phòng</span>
                <span className="text-3xl font-black text-slate-900">{stats.total}</span>
              </div>
              <div className="w-11 h-11 rounded-full bg-slate-100 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21" />
                </svg>
              </div>
            </div>
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between">
              <div className="flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Đang trống</span>
                <div className="flex items-center gap-2">
                  <span className="text-3xl font-black text-slate-900">{stats.vacant}</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 shrink-0"></span>
                </div>
              </div>
              <div className="w-11 h-11 rounded-full bg-emerald-50 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-emerald-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between">
              <div className="flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Đang khám</span>
                <div className="flex items-center gap-2">
                  <span className="text-3xl font-black text-slate-900">{stats.using}</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-red-500 shrink-0"></span>
                </div>
              </div>
              <div className="w-11 h-11 rounded-full bg-red-50 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
              </div>
            </div>
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between">
              <div className="flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Bảo trì / Ngừng HĐ</span>
                <div className="flex items-center gap-2">
                  <span className="text-3xl font-black text-slate-900">{stats.maintenance + stats.inactive}</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-amber-500 shrink-0"></span>
                </div>
              </div>
              <div className="w-11 h-11 rounded-full bg-amber-50 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.877-5.877M11.42 15.17l2.496-3.03c.317-.384.74-.626 1.208-.766M11.42 15.17l-4.655 5.653a2.548 2.548 0 11-3.586-3.586l6.837-5.63m5.108-.233c.55-.164 1.163-.188 1.743-.14a4.5 4.5 0 004.486-6.336l-3.276 3.277a3.004 3.004 0 01-2.25-2.25l3.276-3.276a4.5 4.5 0 00-6.336 4.486c.091 1.076-.071 2.264-.904 2.95l-.102.085m-1.745 1.437L5.909 7.5H4.5L2.25 3.75l1.5-1.5L7.5 4.5v1.409l4.26 4.26m-1.745 1.437l1.745-1.437m6.615 8.206L15.75 15.75M4.867 19.125h.008v.008h-.008v-.008z" />
                </svg>
              </div>
            </div>
          </section>

          {/* ── FILTERS ──────────────────────────────────────────────────────── */}
          <section className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm shrink-0 flex flex-col gap-3">
            <div className="flex flex-col md:flex-row md:items-center gap-3">

              {/* Floor tabs — chỉ hiện các tầng có phòng */}
              <div className="flex bg-slate-100 p-1 rounded-xl shrink-0">
                {["all", ...availableFloors].map((floorVal) => (
                  <button
                    key={floorVal}
                    onClick={() => setSelectedFloor(floorVal)}
                    className={`px-4 py-2 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${
                      selectedFloor === floorVal ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"
                    }`}
                  >
                    {floorVal === "all" ? "Tất cả tầng" : `Tầng ${floorVal}`}
                  </button>
                ))}
              </div>

              {/* Status dropdown */}
              <div className="relative shrink-0" ref={statusFilterRef}>
                <button
                  type="button"
                  onClick={() => setShowStatusFilterDropdown(prev => !prev)}
                  className="bg-slate-50 border border-slate-200 rounded-xl px-4 py-2.5 text-[13px] font-bold focus:outline-none focus:bg-white flex items-center justify-between gap-3 min-w-[190px] cursor-pointer hover:border-slate-300 transition-all shadow-sm"
                >
                  <span className="flex items-center gap-2">
                    {selectedStatus !== "all" && (
                      <span className={`w-2.5 h-2.5 rounded-full shrink-0 ${
                        selectedStatus === "Đang khám" ? "bg-red-500" :
                        selectedStatus === "Trống" ? "bg-emerald-500" :
                        selectedStatus === "Đang vệ sinh" ? "bg-blue-500" :
                        selectedStatus === "Bảo trì" ? "bg-amber-500" : "bg-slate-400"
                      }`} />
                    )}
                    <span>{selectedStatus === "all" ? "Tất cả trạng thái" : selectedStatus}</span>
                  </span>
                  <span className="text-slate-400 text-[10px] select-none">▼</span>
                </button>
                {showStatusFilterDropdown && (
                  <div className="absolute left-0 mt-1.5 w-56 bg-white border border-slate-200 rounded-xl shadow-lg py-1.5 z-30 text-[13px] font-bold text-slate-600">
                    <button type="button" onClick={() => { setSelectedStatus("all"); setShowStatusFilterDropdown(false); }}
                      className="w-full text-left px-4 py-2 hover:bg-slate-50 hover:text-slate-900 flex items-center gap-2 cursor-pointer">
                      <span className="w-2.5 h-2.5 rounded-full bg-slate-300 shrink-0" /> Tất cả trạng thái
                    </button>
                    {[
                      { name: "Trống", color: "bg-emerald-500" },
                      { name: "Đang khám", color: "bg-red-500" },
                      { name: "Đang vệ sinh", color: "bg-blue-500" },
                      { name: "Bảo trì", color: "bg-amber-500" },
                      { name: "Ngừng hoạt động", color: "bg-slate-400" }
                    ].map(st => (
                      <button key={st.name} type="button"
                        onClick={() => { setSelectedStatus(st.name); setShowStatusFilterDropdown(false); }}
                        className="w-full text-left px-4 py-2 hover:bg-slate-50 hover:text-slate-900 flex items-center gap-2 cursor-pointer">
                        <span className={`w-2.5 h-2.5 rounded-full shrink-0 ${st.color}`} /> {st.name}
                      </button>
                    ))}
                  </div>
                )}
              </div>

              {/* Search */}
              <div className="relative flex-1">
                <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400 pointer-events-none">
                  <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Tìm kiếm phòng, mã phòng, loại phòng..."
                  className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-100/80 rounded-xl border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all placeholder:text-slate-400 font-semibold text-slate-800"
                />
                {searchQuery && (
                  <button onClick={() => setSearchQuery("")}
                    className="absolute right-3.5 inset-y-0 flex items-center text-slate-400 hover:text-slate-600">✕</button>
                )}
              </div>

              {/* Add button */}
              <Link
                href="/admin/rooms/create"
                className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] px-5 py-2.5 rounded-xl transition-all shadow-md shadow-red-600/20 shrink-0"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Thêm phòng mới
              </Link>
            </div>

            {/* Per-page + count */}
            <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
              <span>Hiển thị</span>
              <div className="relative">
                <select value={roomsPerPage} onChange={(e) => setRoomsPerPage(Number(e.target.value))}
                  className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer">
                  {[5, 10, 20].map(n => <option key={n} value={n}>{n}</option>)}
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                  <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </div>
              </div>
              <span>/ trang</span>
              <span className="text-slate-300 mx-1">·</span>
              <span>Tìm thấy <span className="text-slate-600 font-bold">{sortedAndFilteredRooms.length}</span> kết quả</span>
            </div>
          </section>

          {/* ── TABLE ────────────────────────────────────────────────────────── */}
          <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex-1 flex flex-col">

            {isLoading ? (
              <div className="p-12 text-center flex-1 flex flex-col items-center justify-center gap-3">
                <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
                <p className="text-[14px] font-bold text-slate-400">Đang tải danh sách phòng...</p>
              </div>
            ) : errorMsg ? (
              <div className="p-12 text-center flex-1 flex flex-col items-center justify-center gap-3">
                <p className="text-[15px] font-bold text-red-600">{errorMsg}</p>
                <button onClick={loadRooms}
                  className="px-4 py-2 bg-primary text-white font-bold rounded-xl text-[13px] cursor-pointer hover:bg-primary-hover">
                  Thử lại
                </button>
              </div>
            ) : sortedAndFilteredRooms.length === 0 ? (
              <div className="p-12 text-center flex-1 flex flex-col items-center justify-center">
                <h4 className="text-[16px] font-bold text-slate-700">Không tìm thấy phòng nào phù hợp</h4>
                <p className="text-[13px] text-slate-400 mt-1">Vui lòng kiểm tra lại bộ lọc hoặc từ khóa tìm kiếm.</p>
              </div>
            ) : (
              <div className="overflow-x-auto flex-1">
                <table className="w-full text-left border-collapse text-[14px]">
                  <thead>
                    <tr className="bg-slate-50/75 border-b border-slate-200/80 text-slate-500 font-bold text-[12px] uppercase tracking-wider">
                      <th className="py-4 px-6">{renderSortHeader("Phòng", "name")}</th>
                      <th className="py-4 px-6">{renderSortHeader("Loại phòng", "type")}</th>
                      <th className="py-4 px-6">{renderSortHeader("Tầng", "floor")}</th>
                      <th className="py-4 px-6 text-center">{renderSortHeader("Trạng thái", "status")}</th>
                      <th className="py-4 px-6">{renderSortHeader("Ngày tạo", "createdAt")}</th>
                      <th className="py-4 px-6 text-right">Hành động</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {paginatedRooms.map((room) => (
                      <tr key={room.id} className="hover:bg-slate-50/40 transition-colors group">
                        <td className="py-4.5 px-6">
                          <div className="inline-flex flex-col">
                            <span className="flex items-center gap-1.5 font-extrabold text-slate-900">
                              {room.name}
                              <span className="text-[11px] font-bold text-slate-400 bg-slate-100 px-1.5 py-0.5 rounded">{room.code}</span>
                            </span>
                            <span className={`text-[11px] font-bold mt-0.5 ${room.activeStatus === "Hoạt động" ? "text-emerald-600" : "text-slate-400"}`}>
                              ● {room.activeStatus}
                            </span>
                          </div>
                        </td>
                        <td className="py-4.5 px-6 font-semibold text-slate-700">{room.type}</td>
                        <td className="py-4.5 px-6 font-semibold text-slate-600">Tầng {room.floor}</td>
                        <td className="py-4.5 px-6 text-center">
                          <button onClick={() => openStatusModal(room)}
                            className="hover:opacity-85 hover:scale-[1.02] active:scale-[0.98] transition-all cursor-pointer focus:outline-none block w-full text-center"
                            title="Click để đổi trạng thái">
                            {getStatusBadge(room.status)}
                          </button>
                        </td>
                        <td className="py-4.5 px-6 text-[13px] font-semibold text-slate-500">{formatDate(room.createdAt)}</td>
                        <td className="py-4.5 px-6 text-right">
                          <div className="flex items-center justify-end gap-2.5">
                            <button onClick={() => openDetailModal(room)}
                              className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-all cursor-pointer" title="Xem chi tiết">
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                              </svg>
                            </button>
                            <button onClick={() => openEditModal(room)}
                              className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-all cursor-pointer" title="Chỉnh sửa phòng">
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                              </svg>
                            </button>
                            <button onClick={() => { setRoomToDelete(room); setShowDeleteModal(true); }}
                              className="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-all cursor-pointer" title="Xóa phòng">
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                              </svg>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {/* Pagination */}
            {!isLoading && !errorMsg && sortedAndFilteredRooms.length > 0 && (
              <div className="px-6 py-4 border-t border-slate-200/80 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-50/30 shrink-0">
                <span className="text-[13px] text-slate-500 font-semibold">
                  Hiển thị <span className="text-slate-700 font-bold">{startIndex + 1}–{Math.min(startIndex + roomsPerPage, sortedAndFilteredRooms.length)}</span> trong <span className="text-slate-700 font-bold">{sortedAndFilteredRooms.length}</span> phòng
                </span>
                <div className="flex items-center gap-2">
                  <button onClick={() => setCurrentPage(p => Math.max(1, p - 1))} disabled={currentPage === 1}
                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-600 font-bold text-[13px] hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-all cursor-pointer">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
                    </svg>
                  </button>
                  {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
                    <button key={page} onClick={() => setCurrentPage(page)}
                      className={`w-8 h-8 rounded-lg font-bold text-[13px] transition-all cursor-pointer ${page === currentPage ? "bg-primary text-white shadow-md" : "bg-white border border-slate-200 text-slate-600 hover:bg-slate-50"}`}>
                      {page}
                    </button>
                  ))}
                  <button onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages}
                    className="px-3 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-600 font-bold text-[13px] hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-all cursor-pointer">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                    </svg>
                  </button>
                </div>
              </div>
            )}
          </section>

        </div>
      </main>

    </div>{/* end animate-fade-in */}

      {/* ── TOAST — ngoài animate-fade-in, fixed theo viewport ───────────────── */}
      {toast && (
        <div className={`fixed top-6 right-6 z-[9999] px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border font-bold text-[14.5px] max-w-md ${
          toast.type === "success" ? "bg-emerald-900 text-white border-emerald-800"
          : toast.type === "error" ? "bg-red-900 text-white border-red-800"
          : "bg-slate-900 text-white border-slate-800"
        }`}>
          <span className="text-lg">{toast.type === "success" ? "✓" : toast.type === "error" ? "⚠" : "ℹ"}</span>
          <span>{toast.message}</span>
        </div>
      )}

      {/* ── MODAL: VIEW DETAIL ──────────────────────────────────────────────── */}
      {showDetailModal && selectedRoom && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
          <div className="bg-white w-full max-w-lg rounded-2xl shadow-xl overflow-hidden border border-slate-200 border-t-4 border-t-primary">
            <div className="px-6 py-4.5 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
              <div>
                <h3 className="text-[17px] font-black text-slate-900">{selectedRoom.name} ({selectedRoom.code})</h3>
                <span className="text-[11px] text-slate-400 font-bold uppercase tracking-wider block mt-0.5">Chi tiết phòng khám</span>
              </div>
              <button onClick={closeDetailModal} className="text-slate-400 hover:text-slate-600 text-lg cursor-pointer">✕</button>
            </div>
            <div className="p-6 flex flex-col gap-5 text-[13px] max-h-[75vh] overflow-y-auto">
              <div>
                <h4 className="font-extrabold text-[12px] text-slate-400 uppercase tracking-wider mb-2.5">Thông tin cơ bản</h4>
                <div className="grid grid-cols-2 gap-4 bg-slate-50 p-4 rounded-xl border border-slate-150">
                  <div className="flex flex-col gap-0.5">
                    <span className="text-[11px] text-slate-400 font-semibold">Tên phòng</span>
                    <span className="font-bold text-slate-800">{selectedRoom.name}</span>
                  </div>
                  <div className="flex flex-col gap-0.5">
                    <span className="text-[11px] text-slate-400 font-semibold">Mã phòng</span>
                    <span className="font-bold text-slate-800">{selectedRoom.code}</span>
                  </div>
                  <div className="flex flex-col gap-0.5 mt-2">
                    <span className="text-[11px] text-slate-400 font-semibold">Loại phòng</span>
                    <span className="font-bold text-slate-800">{selectedRoom.type}</span>
                  </div>
                  <div className="flex flex-col gap-0.5 mt-2">
                    <span className="text-[11px] text-slate-400 font-semibold">Tầng</span>
                    <span className="font-bold text-slate-800">Tầng {selectedRoom.floor}</span>
                  </div>
                  <div className="flex flex-col gap-0.5 mt-2">
                    <span className="text-[11px] text-slate-400 font-semibold">Trạng thái hoạt động</span>
                    <span className={`font-bold ${selectedRoom.activeStatus === "Hoạt động" ? "text-emerald-600" : "text-slate-500"}`}>
                      ● {selectedRoom.activeStatus}
                    </span>
                  </div>
                  <div className="flex flex-col gap-0.5 mt-2">
                    <span className="text-[11px] text-slate-400 font-semibold">Trạng thái hiện tại</span>
                    <span className="font-bold text-slate-800">{selectedRoom.status}</span>
                  </div>
                  <div className="flex flex-col gap-0.5 mt-2">
                    <span className="text-[11px] text-slate-400 font-semibold">Ngày tạo</span>
                    <span className="font-bold text-slate-800">{formatDate(selectedRoom.createdAt)}</span>
                  </div>
                  {selectedRoom.updatedAt && (
                    <div className="flex flex-col gap-0.5 mt-2">
                      <span className="text-[11px] text-slate-400 font-semibold">Cập nhật lần cuối</span>
                      <span className="font-bold text-slate-800">{formatDate(selectedRoom.updatedAt)}</span>
                    </div>
                  )}
                </div>
              </div>

              {selectedRoom.description && (
                <div>
                  <h4 className="font-extrabold text-[12px] text-slate-400 uppercase tracking-wider mb-2">Mô tả trang thiết bị</h4>
                  <div className="bg-slate-50 p-3.5 rounded-xl border border-slate-150 font-semibold italic text-slate-700 leading-relaxed">
                    {selectedRoom.description}
                  </div>
                </div>
              )}

              {/* Nhân sự làm việc hôm nay */}
              <div>
                <h4 className="font-extrabold text-[12px] text-slate-400 uppercase tracking-wider mb-2.5">
                  Nhân sự làm việc hôm nay ({getTodayStr().split("-").reverse().join("/")})
                </h4>
                {isLoadingSchedule ? (
                  <div className="flex items-center gap-2 py-4 text-[13px] text-slate-400 font-semibold">
                    <div className="w-4 h-4 border-2 border-primary border-t-transparent rounded-full animate-spin" />
                    Đang tải lịch...
                  </div>
                ) : todaySchedule.length === 0 ? (
                  <div className="bg-slate-50 border border-slate-150 rounded-xl px-4 py-3 text-[13px] text-slate-400 font-semibold">
                    Chưa có phân công nhân sự cho phòng này hôm nay.
                  </div>
                ) : (
                  <div className="flex flex-col gap-2.5">
                    {SHIFTS.map(shift => {
                      const entries = todaySchedule.filter(e => e.shift === shift.id);
                      if (entries.length === 0) return null;
                      return (
                        <div key={shift.id} className="bg-slate-50 border border-slate-150 rounded-xl p-3.5">
                          <p className="text-[11px] font-extrabold text-primary uppercase tracking-wider mb-2">
                            Ca {shift.label} · {shift.period}
                          </p>
                          <div className="flex flex-col gap-1.5">
                            {entries.map(e => (
                              <div key={e.id} className="flex items-center justify-between">
                                <span className="font-bold text-slate-800 text-[13px]">{e.name}</span>
                                <span className={`text-[11px] font-bold px-2 py-0.5 rounded-full ${
                                  e.role === "dentist"   ? "bg-red-50 text-primary border border-red-200" :
                                  e.role === "assistant" ? "bg-teal-50 text-teal-700 border border-teal-200" :
                                                           "bg-slate-100 text-slate-600 border border-slate-200"
                                }`}>
                                  {e.role === "dentist" ? "Bác sĩ" : e.role === "assistant" ? "Phụ tá" : "Nhân viên"}
                                </span>
                              </div>
                            ))}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>

              <div className="flex justify-end gap-3 mt-1 border-t border-slate-100 pt-4">
                <button onClick={closeDetailModal}
                  className="px-5 py-2 bg-slate-150 hover:bg-slate-200 text-slate-700 rounded-lg font-bold cursor-pointer transition-colors">
                  Đóng
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL: EDIT ─────────────────────────────────────────────────────── */}
      {showEditRoomModal && selectedRoom && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-xl flex flex-col max-h-[90vh] border border-slate-200 border-t-4 border-t-slate-800">

            {/* Header — cố định */}
            <div className="px-6 py-4 border-b border-slate-100 flex justify-between items-center bg-slate-50/50 shrink-0 rounded-t-2xl">
              <div>
                <h3 className="text-[17px] font-black text-slate-900">Chỉnh sửa {selectedRoom.name}</h3>
                <span className="text-[11px] text-slate-400 font-bold block mt-0.5 uppercase tracking-wider">Cập nhật thông tin phòng khám</span>
              </div>
              <button type="button" onClick={() => { setShowEditRoomModal(false); setSelectedRoom(null); }}
                className="text-slate-400 hover:text-slate-600 text-lg cursor-pointer">✕</button>
            </div>

            {/* Form — scroll bên trong */}
            <form onSubmit={handleEditRoomSubmit} className="flex flex-col flex-1 overflow-hidden">
              <div className="p-6 flex flex-col gap-4 text-[13px] overflow-y-auto flex-1">
                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1">
                    <label className="font-bold text-slate-600">Tên phòng <span className="text-primary">*</span></label>
                    <input type="text" required value={editRoomName} onChange={(e) => setEditRoomName(e.target.value)}
                      className="w-full px-3.5 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:border-slate-800 font-semibold" />
                  </div>
                  <div className="flex flex-col gap-1">
                    <label className="font-bold text-slate-600">Mã phòng <span className="text-primary">*</span></label>
                    <input type="text" required value={editRoomCode} onChange={(e) => setEditRoomCode(e.target.value)}
                      className="w-full px-3.5 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:border-slate-800 font-semibold uppercase" />
                  </div>
                </div>
                <div className="flex flex-col gap-1">
                  <label className="font-bold text-slate-600">Tầng <span className="text-primary">*</span></label>
                  <select required value={editRoomFloor} onChange={(e) => setEditRoomFloor(e.target.value)}
                    className="w-full px-3.5 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:border-slate-800 font-semibold">
                    <option value="">Chọn tầng</option>
                    <option value="1">Tầng 1</option>
                    <option value="2">Tầng 2</option>
                    <option value="3">Tầng 3</option>
                  </select>
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="font-bold text-slate-600">Loại phòng <span className="text-primary">*</span></label>
                  <div className="grid grid-cols-2 gap-2 mt-1">
                    {["Khám tổng quát", "Cấp cứu", "Phẫu thuật", "X-Quang"].map(name => (
                      <button key={name} type="button" onClick={() => setEditRoomType(name)}
                        className={`flex items-center p-3 rounded-xl border text-left transition-all cursor-pointer ${editRoomType === name ? "border-primary text-primary bg-red-50/15 font-bold shadow-sm" : "border-slate-200 text-slate-600 bg-white hover:border-slate-300 font-medium"}`}>
                        <span className="text-[12.5px] truncate">{name}</span>
                      </button>
                    ))}
                  </div>
                </div>
                <div className="flex flex-col gap-1">
                  <label className="font-bold text-slate-600">Mô tả chi tiết trang thiết bị</label>
                  <textarea rows={3} value={editRoomDescription} onChange={(e) => setEditRoomDescription(e.target.value)}
                    placeholder="Nhập mô tả về trang thiết bị..."
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-slate-800 focus:outline-none transition-all font-semibold resize-none text-[13px]" />
                </div>
              </div>

              {/* Footer — cố định dưới cùng */}
              <div className="flex justify-end gap-3 px-6 py-4 border-t border-slate-100 shrink-0 bg-white rounded-b-2xl">
                <button type="button" onClick={() => { setShowEditRoomModal(false); setSelectedRoom(null); }}
                  className="px-4 py-2 bg-slate-100 hover:bg-slate-200 rounded-lg font-bold text-slate-600 cursor-pointer">Hủy</button>
                <button type="submit" disabled={isSaving}
                  className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-lg font-bold cursor-pointer disabled:opacity-60">
                  {isSaving ? "Đang lưu..." : "Lưu thay đổi"}
                </button>
              </div>
            </form>

          </div>
        </div>
      )}

      {/* ── MODAL: CHANGE STATUS ──────────────────────────────────────────── */}
      {showStatusModal && selectedRoom && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-xl overflow-hidden border border-slate-200 border-t-4 border-t-primary">
            <div className="px-6 py-4 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
              <div>
                <h3 className="text-[17px] font-black text-slate-900">Chuyển trạng thái</h3>
                <span className="text-[11px] text-slate-400 font-bold block mt-0.5 uppercase tracking-wider">Cập nhật hoạt động: {selectedRoom.name}</span>
              </div>
              <button type="button" onClick={() => { setShowStatusModal(false); setSelectedRoom(null); }}
                className="text-slate-400 hover:text-slate-600 text-lg cursor-pointer">✕</button>
            </div>
            <form onSubmit={handleStatusSubmit} className="p-6 flex flex-col gap-4.5 text-[13px]">
              <div className="flex flex-col gap-2">
                <label className="font-bold text-slate-500 mb-1">Chọn trạng thái mới:</label>
                <div className="flex flex-col gap-2">
                  {[
                    { name: "Trống", desc: "Phòng trống, sẵn sàng nhận bệnh nhân", color: "bg-emerald-500" },
                    { name: "Đang khám", desc: "Bác sĩ đang thực hiện khám/điều trị", color: "bg-red-500" },
                    { name: "Đang vệ sinh", desc: "Đang dọn dẹp và khử trùng trang thiết bị", color: "bg-blue-500" },
                    { name: "Bảo trì", desc: "Tạm dừng hoạt động để bảo dưỡng kỹ thuật", color: "bg-amber-500" },
                    { name: "Ngừng hoạt động", desc: "Đóng cửa phòng khám không thời hạn", color: "bg-slate-400" }
                  ].map(item => {
                    const isSelected = statusValue === item.name;
                    return (
                      <button key={item.name} type="button" onClick={() => setStatusValue(item.name as RoomStatus)}
                        className={`flex items-center justify-between p-3 rounded-xl border text-left transition-all cursor-pointer ${isSelected ? "border-primary bg-red-50/10 shadow-sm" : "border-slate-200 bg-white hover:border-slate-300"}`}>
                        <div className="flex items-center gap-3">
                          <span className={`w-3 h-3 rounded-full shrink-0 ${item.color}`} />
                          <div>
                            <span className="font-bold text-slate-800 text-[13.5px]">{item.name}</span>
                            <p className="text-[11px] text-slate-400 mt-0.5 font-medium">{item.desc}</p>
                          </div>
                        </div>
                        {isSelected && <span className="text-primary font-black text-sm pr-1.5">✓</span>}
                      </button>
                    );
                  })}
                </div>
              </div>
              <div className="flex justify-end gap-3 mt-2 border-t border-slate-100 pt-4">
                <button type="button" onClick={() => { setShowStatusModal(false); setSelectedRoom(null); }}
                  className="px-4 py-2 bg-slate-100 hover:bg-slate-200 rounded-lg font-bold text-slate-600 cursor-pointer">Hủy</button>
                <button type="submit" disabled={isSaving}
                  className="px-5 py-2 bg-primary hover:bg-primary-hover text-white rounded-lg font-bold cursor-pointer transition-colors shadow-sm disabled:opacity-60">
                  {isSaving ? "Đang lưu..." : "Cập nhật"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: DELETE ────────────────────────────────────────────────────── */}
      {showDeleteModal && roomToDelete && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
          <div className="bg-white w-full max-w-sm rounded-2xl shadow-xl overflow-hidden border border-slate-200 border-t-4 border-t-red-600">
            <div className="p-6 flex flex-col items-center text-center gap-4 bg-slate-50/20">
              <div className="w-14 h-14 rounded-full bg-red-50 text-red-600 flex items-center justify-center animate-bounce">
                <svg className="w-8 h-8" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
              </div>
              <div>
                <h3 className="text-[17px] font-black text-slate-900">Xác nhận xóa phòng?</h3>
                <p className="text-[13px] text-slate-500 font-semibold leading-relaxed mt-2">
                  Bạn có chắc chắn muốn xóa phòng <span className="text-slate-900 font-black">{roomToDelete.name}</span> ({roomToDelete.code}) không?
                </p>
                <p className="text-[11px] text-red-600/80 font-bold bg-red-50 rounded-lg p-2.5 border border-red-100 mt-3 leading-relaxed">
                  ⚠️ Hành động này không thể hoàn tác.
                </p>
              </div>
              <div className="flex gap-3 w-full mt-2 border-t border-slate-100 pt-4 text-[13px]">
                <button type="button" onClick={() => { setShowDeleteModal(false); setRoomToDelete(null); }}
                  className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 rounded-xl font-bold text-slate-600 cursor-pointer transition-colors">Hủy bỏ</button>
                <button type="button" onClick={handleConfirmDelete} disabled={isSaving}
                  className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white rounded-xl font-bold cursor-pointer transition-colors shadow-md disabled:opacity-60">
                  {isSaving ? "Đang xóa..." : "Đồng ý xóa"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

    </>
  );
}
