"use client";

import React, { useState, useEffect, useMemo, useRef } from "react";
import Link from "next/link";
import Sidebar from "../../../components/shared/Sidebar";
import Header from "../../../components/Header";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";

// ── TypeScript Types ────────────────────────────────────────────────────────

export interface ShiftStaff {
  doctor: string;
  supportStaff: string;
}

export interface Appointment {
  time: string;
  patientName: string;
  status: string; // "Đã đến" | "Chờ khám" | "Hoàn thành"
}

export interface Room {
  id: string;
  code: string; // P001, P002, etc.
  name: string; // Phòng 1, Phòng 2, etc.
  floor: string; // "1" | "2" | "3"
  type: string; // "Khám tổng quát" | "Cấp cứu" | "Phẫu thuật" | "X-Quang"
  status: "Trống" | "Đang khám" | "Đang vệ sinh" | "Bảo trì" | "Ngừng hoạt động";
  activeStatus: "Hoạt động" | "Ngừng hoạt động";
  currentDoctor: string;
  currentSupportStaff: string;
  todayAppointmentsCount: number;
  morningShift: ShiftStaff;
  afternoonShift: ShiftStaff;
  appointments: Appointment[];
  description?: string;
}

// ── Mock Initial Database of Rooms ──────────────────────────────────────────

const INITIAL_ROOMS: Room[] = [
  {
    id: "room-1",
    code: "P001",
    name: "Phòng 1",
    floor: "1",
    type: "Khám tổng quát",
    status: "Đang khám",
    activeStatus: "Hoạt động",
    currentDoctor: "BS Nguyễn Văn A",
    currentSupportStaff: "PT1",
    todayAppointmentsCount: 8,
    morningShift: {
      doctor: "BS Nguyễn Văn A",
      supportStaff: "PT1"
    },
    afternoonShift: {
      doctor: "BS Trần Văn B",
      supportStaff: "PT1"
    },
    appointments: [
      { time: "09:00", patientName: "Nguyễn Văn A", status: "Đã đến" },
      { time: "09:30", patientName: "Trần Thị B", status: "Chờ khám" },
      { time: "10:00", patientName: "Lê Văn C", status: "Hoàn thành" }
    ],
    description: "Phòng khám đầy đủ trang thiết bị nha khoa tổng quát, ghế khám cơ học thế hệ mới."
  },
  {
    id: "room-2",
    code: "P002",
    name: "Phòng 2",
    floor: "1",
    type: "Khám tổng quát",
    status: "Trống",
    activeStatus: "Hoạt động",
    currentDoctor: "BS Trần Văn B",
    currentSupportStaff: "PT2",
    todayAppointmentsCount: 6,
    morningShift: {
      doctor: "BS Trần Văn B",
      supportStaff: "PT2"
    },
    afternoonShift: {
      doctor: "BS Lê Văn C",
      supportStaff: "PT1"
    },
    appointments: [
      { time: "14:00", patientName: "Phạm Văn D", status: "Chờ khám" },
      { time: "14:30", patientName: "Vũ Thị E", status: "Chờ khám" }
    ],
    description: "Phòng khám nha khoa tổng quát cơ bản với dụng cụ chẩn đoán hình ảnh di động."
  },
  {
    id: "room-3",
    code: "P003",
    name: "Phòng 3",
    floor: "2",
    type: "Khám tổng quát",
    status: "Bảo trì",
    activeStatus: "Hoạt động",
    currentDoctor: "BS Lê Văn C",
    currentSupportStaff: "PT3",
    todayAppointmentsCount: 0,
    morningShift: {
      doctor: "BS Lê Văn C",
      supportStaff: "PT3"
    },
    afternoonShift: {
      doctor: "BS Nguyễn Văn A",
      supportStaff: "PT3"
    },
    appointments: [],
    description: "Phòng được trang bị ghế nha khoa chuyên dụng và hệ thống vô trùng cao cấp."
  }
];

const AVAILABLE_DENTISTS = [
  "BS Nguyễn Văn A",
  "BS Trần Văn B",
  "BS Lê Văn C",
  "BS Đặng Thu Thảo",
  "BS Vương Đình Khang",
  "BS Nguyễn Minh Đức",
  "BS Lê Thị Phương Thảo",
];

const AVAILABLE_SUPPORT_STAFF = [
  "PT1",
  "PT2",
  "PT3",
  "PT4",
  "PT5",
];

const ROOM_TYPES = [
  "Khám tổng quát",
  "Cấp cứu",
  "Phẫu thuật",
  "X-Quang",
];

export default function RoomsPage() {
  useRequireAdmin();

  // ── States ────────────────────────────────────────────────────────────────
  const [rooms, setRooms] = useState<Room[]>([]);
  const [isLoaded, setIsLoaded] = useState(false);
  const [selectedFloor, setSelectedFloor] = useState<string>("all");
  const [selectedStatus, setSelectedStatus] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState<string>("");

  // Sort states
  const [sortField, setSortField] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");

  // Custom status filter dropdown toggle
  const [showStatusFilterDropdown, setShowStatusFilterDropdown] = useState(false);
  
  // Modals state
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [showEditRoomModal, setShowEditRoomModal] = useState(false);
  const [showStatusModal, setShowStatusModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  
  const [selectedRoom, setSelectedRoom] = useState<Room | null>(null);
  const [roomToDelete, setRoomToDelete] = useState<Room | null>(null);

  // Form states (Edit Room)
  const [editRoomName, setEditRoomName] = useState("");
  const [editRoomCode, setEditRoomCode] = useState("");
  const [editRoomType, setEditRoomType] = useState("");
  const [editRoomDescription, setEditRoomDescription] = useState("");

  // Form states (In-place Shift Assignments inside details modal)
  const [isEditingShifts, setIsEditingShifts] = useState(false);
  const [assignMorningDoctor, setAssignMorningDoctor] = useState("");
  const [assignMorningSupport, setAssignMorningSupport] = useState("");
  const [assignAfternoonDoctor, setAssignAfternoonDoctor] = useState("");
  const [assignAfternoonSupport, setAssignAfternoonSupport] = useState("");

  // Status Change Form
  const [statusValue, setStatusValue] = useState<Room["status"]>("Trống");

  // Dropdown ref for auto closing when clicking outside
  const statusFilterRef = useRef<HTMLDivElement>(null);

  // ── Load & Save localStorage ──────────────────────────────────────────────
  useEffect(() => {
    if (typeof window !== "undefined") {
      const stored = localStorage.getItem("dental_clinic_rooms");
      if (stored) {
        try {
          const parsed = JSON.parse(stored);
          if (parsed.length > 0 && (parsed[0].name || parsed[0].code)) {
            setRooms(parsed);
          } else {
            setRooms(INITIAL_ROOMS);
            localStorage.setItem("dental_clinic_rooms", JSON.stringify(INITIAL_ROOMS));
          }
        } catch {
          setRooms(INITIAL_ROOMS);
        }
      } else {
        setRooms(INITIAL_ROOMS);
        localStorage.setItem("dental_clinic_rooms", JSON.stringify(INITIAL_ROOMS));
      }
      setIsLoaded(true);
    }
  }, []);

  const saveRooms = (newRooms: Room[]) => {
    setRooms(newRooms);
    localStorage.setItem("dental_clinic_rooms", JSON.stringify(newRooms));
  };

  // Helper to sync current staff based on shifts and system hour
  const updateCurrentStaff = (room: Room): Room => {
    const currentHour = new Date().getHours();
    const shift = currentHour < 12 ? room.morningShift : room.afternoonShift;
    return {
      ...room,
      currentDoctor: shift.doctor || "Chưa phân công",
      currentSupportStaff: shift.supportStaff || "Chưa phân công",
    };
  };

  // ── Calculated Statistics ─────────────────────────────────────────────────
  const stats = useMemo(() => {
    return {
      total: rooms.length,
      vacant: rooms.filter(r => r.status === "Trống").length,
      using: rooms.filter(r => r.status === "Đang khám").length,
      maintenance: rooms.filter(r => r.status === "Bảo trì").length,
      cleaning: rooms.filter(r => r.status === "Đang vệ sinh").length,
      inactive: rooms.filter(r => r.status === "Ngừng hoạt động").length,
    };
  }, [rooms]);

  // ── Sorting Handler ───────────────────────────────────────────────────────
  const handleSort = (field: string) => {
    if (sortField === field) {
      setSortDirection(prev => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortField(field);
      setSortDirection("asc");
    }
  };

  // ── Filters, Search & Sort Logic ──────────────────────────────────────────
  const sortedAndFilteredRooms = useMemo(() => {
    // 1. Filter & Search
    const filtered = rooms.filter(room => {
      const matchesFloor = selectedFloor === "all" || room.floor === selectedFloor;
      const matchesStatus = selectedStatus === "all" || room.status === selectedStatus;
      
      const searchLower = searchQuery.toLowerCase();
      const matchesSearch = 
        room.name.toLowerCase().includes(searchLower) ||
        room.code.toLowerCase().includes(searchLower) ||
        room.currentDoctor.toLowerCase().includes(searchLower) ||
        room.currentSupportStaff.toLowerCase().includes(searchLower) ||
        room.type.toLowerCase().includes(searchLower);

      return matchesFloor && matchesStatus && matchesSearch;
    });

    // 2. Sorting
    if (!sortField) return filtered;

    return [...filtered].sort((a, b) => {
      let valA: any = "";
      let valB: any = "";

      if (sortField === "name") {
        valA = a.name.toLowerCase();
        valB = b.name.toLowerCase();
      } else if (sortField === "currentDoctor") {
        valA = a.currentDoctor.toLowerCase();
        valB = b.currentDoctor.toLowerCase();
      } else if (sortField === "currentSupportStaff") {
        valA = a.currentSupportStaff.toLowerCase();
        valB = b.currentSupportStaff.toLowerCase();
      } else if (sortField === "status") {
        valA = a.status.toLowerCase();
        valB = b.status.toLowerCase();
      } else if (sortField === "todayAppointmentsCount") {
        valA = a.todayAppointmentsCount;
        valB = b.todayAppointmentsCount;
      }

      if (valA < valB) return sortDirection === "asc" ? -1 : 1;
      if (valA > valB) return sortDirection === "asc" ? 1 : -1;
      return 0;
    });
  }, [rooms, selectedFloor, selectedStatus, searchQuery, sortField, sortDirection]);

  // ── Handlers ──────────────────────────────────────────────────────────────

  // 1. Submit Edit Room (Admin edits Name, Code, Type, Description)
  const handleEditRoomSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRoom || !editRoomName.trim() || !editRoomCode.trim()) return;

    const updated = rooms.map(r => {
      if (r.id === selectedRoom.id) {
        return {
          ...r,
          name: editRoomName.trim(),
          code: editRoomCode.trim().toUpperCase(),
          type: editRoomType,
          description: editRoomDescription.trim()
        };
      }
      return r;
    });

    saveRooms(updated);
    setShowEditRoomModal(false);
    setSelectedRoom(null);
  };

  // 2. Submit In-place Shift Assignments
  const handleSaveShifts = () => {
    if (!selectedRoom) return;
    const updated = rooms.map(r => {
      if (r.id === selectedRoom.id) {
        const updatedRoom: Room = {
          ...r,
          morningShift: {
            doctor: assignMorningDoctor,
            supportStaff: assignMorningSupport
          },
          afternoonShift: {
            doctor: assignAfternoonDoctor,
            supportStaff: assignAfternoonSupport
          }
        };
        return updateCurrentStaff(updatedRoom);
      }
      return r;
    });

    saveRooms(updated);
    const newlyUpdated = updated.find(r => r.id === selectedRoom.id);
    if (newlyUpdated) {
      setSelectedRoom(newlyUpdated);
    }
    setIsEditingShifts(false);
  };

  // 3. Submit Quick Change Status
  const handleStatusSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRoom) return;

    const updated = rooms.map(r => {
      if (r.id === selectedRoom.id) {
        return {
          ...r,
          status: statusValue,
          activeStatus: statusValue === "Ngừng hoạt động" ? ("Ngừng hoạt động" as const) : ("Hoạt động" as const)
        };
      }
      return r;
    });

    saveRooms(updated);
    setShowStatusModal(false);
    setSelectedRoom(null);
  };

  // 4. Custom Delete Room Handlers
  const handleDeleteClick = (room: Room) => {
    setRoomToDelete(room);
    setShowDeleteModal(true);
  };

  const handleConfirmDelete = () => {
    if (!roomToDelete) return;
    const updated = rooms.filter(r => r.id !== roomToDelete.id);
    saveRooms(updated);
    setShowDeleteModal(false);
    setRoomToDelete(null);
  };

  // Modal Openers & Closers
  const openDetailModal = (room: Room) => {
    setSelectedRoom(room);
    setShowDetailModal(true);
  };

  const closeDetailModal = () => {
    setShowDetailModal(false);
    setSelectedRoom(null);
    setIsEditingShifts(false);
  };

  const openEditModal = (room: Room) => {
    setSelectedRoom(room);
    setEditRoomName(room.name);
    setEditRoomCode(room.code);
    setEditRoomType(room.type);
    setEditRoomDescription(room.description || "");
    setShowEditRoomModal(true);
  };

  const openStatusModal = (room: Room) => {
    setSelectedRoom(room);
    setStatusValue(room.status);
    setShowStatusModal(true);
  };

  // Auto-close dropdown on outside click
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (statusFilterRef.current && !statusFilterRef.current.contains(event.target as Node)) {
        setShowStatusFilterDropdown(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  // Helper styling for status badges
  const getStatusBadge = (status: Room["status"]) => {
    switch (status) {
      case "Đang khám":
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-red-50 text-[#b91c1c] border border-red-200">Đang khám</span>;
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

  // Custom sort header render helper
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

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      
      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <Sidebar activeMenu="rooms" />

      {/* ── MAIN CONTENT AREA ────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* Header with Custom Link Action Button */}
        <Header 
          title="Quản lý Phòng khám" 
          showSearch={false}
          rightActions={
            <Link
              href="/dashboard/rooms/create"
              id="add-room-btn"
              className="flex items-center gap-2 bg-[#b91c1c] hover:bg-[#991b1b] text-white px-5 py-2.5 rounded-xl font-bold transition-all hover:scale-[1.02] shadow-md shadow-red-600/10 cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              Thêm phòng mới
            </Link>
          }
        />

        {/* BODY CONTAINER */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">

          {/* ── SECTION 1: STATS GRID ────────────────────────────────────── */}
          <section className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4 shrink-0">
            {/* Total */}
            <div className="bg-white p-4 rounded-xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[12px] font-bold text-slate-400 block">Tổng số phòng</span>
                <span className="text-2xl font-black text-slate-800">{stats.total}</span>
              </div>
              <span className="text-xl">🏢</span>
            </div>
            {/* Vacant */}
            <div className="bg-white p-4 rounded-xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[12px] font-bold text-emerald-600 block">Đang trống</span>
                <span className="text-2xl font-black text-emerald-600">{stats.vacant}</span>
              </div>
              <span className="text-xl">✅</span>
            </div>
            {/* Using */}
            <div className="bg-white p-4 rounded-xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[12px] font-bold text-red-600 block">Đang khám</span>
                <span className="text-2xl font-black text-[#b91c1c]">{stats.using}</span>
              </div>
              <span className="text-xl">😷</span>
            </div>
            {/* Cleaning */}
            <div className="bg-white p-4 rounded-xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[12px] font-bold text-blue-600 block">Đang vệ sinh</span>
                <span className="text-2xl font-black text-blue-600">{stats.cleaning}</span>
              </div>
              <span className="text-xl">🧹</span>
            </div>
            {/* Maintenance & Inactive */}
            <div className="bg-white p-4 rounded-xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[12px] font-bold text-amber-600 block">Bảo trì / Ngừng HĐ</span>
                <span className="text-2xl font-black text-amber-700">{stats.maintenance + stats.inactive}</span>
              </div>
              <span className="text-xl">🔧</span>
            </div>
          </section>

          {/* ── SECTION 2: FILTERS & SEARCH ROW ────────────────────────────── */}
          <section className="flex flex-col md:flex-row items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm shrink-0">
            <div className="flex flex-wrap items-center gap-3 w-full md:w-auto">
              
              {/* Floor Tabs Selector */}
              <div className="flex bg-slate-100 p-1 rounded-xl">
                {["all", "1", "2", "3"].map((floorVal) => (
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

              {/* Custom Status Dropdown Filter */}
              <div className="relative" ref={statusFilterRef}>
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
                        selectedStatus === "Bảo trì" ? "bg-amber-500" :
                        "bg-slate-400"
                      }`} />
                    )}
                    <span>{selectedStatus === "all" ? "Tất cả trạng thái" : selectedStatus}</span>
                  </span>
                  <span className="text-slate-400 text-[10px] select-none">▼</span>
                </button>
                {showStatusFilterDropdown && (
                  <div className="absolute left-0 mt-1.5 w-56 bg-white border border-slate-200 rounded-xl shadow-lg py-1.5 z-30 text-[13px] font-bold text-slate-600 animate-in fade-in slide-in-from-top-1 duration-150">
                    <button
                      type="button"
                      onClick={() => { setSelectedStatus("all"); setShowStatusFilterDropdown(false); }}
                      className="w-full text-left px-4 py-2 hover:bg-slate-50 hover:text-slate-900 flex items-center gap-2 cursor-pointer"
                    >
                      <span className="w-2.5 h-2.5 rounded-full bg-slate-300 shrink-0" />
                      Tất cả trạng thái
                    </button>
                    {[
                      { name: "Trống", color: "bg-emerald-500" },
                      { name: "Đang khám", color: "bg-red-500" },
                      { name: "Đang vệ sinh", color: "bg-blue-500" },
                      { name: "Bảo trì", color: "bg-amber-500" },
                      { name: "Ngừng hoạt động", color: "bg-slate-400" }
                    ].map((st) => (
                      <button
                        key={st.name}
                        type="button"
                        onClick={() => { setSelectedStatus(st.name); setShowStatusFilterDropdown(false); }}
                        className="w-full text-left px-4 py-2 hover:bg-slate-50 hover:text-slate-900 flex items-center gap-2 cursor-pointer"
                      >
                        <span className={`w-2.5 h-2.5 rounded-full shrink-0 ${st.color}`} />
                        {st.name}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Search Input */}
            <div className="relative w-full md:w-80">
              <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400 pointer-events-none">
                <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Tìm kiếm phòng, bác sĩ..."
                className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-50 rounded-xl border border-slate-200 focus:bg-white focus:border-[#b91c1c]/45 focus:outline-none transition-all placeholder:text-slate-400 font-medium"
              />
              {searchQuery && (
                <button 
                  onClick={() => setSearchQuery("")}
                  className="absolute right-3.5 inset-y-0 flex items-center text-slate-400 hover:text-slate-600"
                >
                  ✕
                </button>
              )}
            </div>
          </section>

          {/* ── SECTION 3: ROOMS LIST TABLE ────────────────────────────────── */}
          <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex-1 flex flex-col">
            {sortedAndFilteredRooms.length === 0 ? (
              <div className="p-12 text-center flex-1 flex flex-col items-center justify-center">
                <div className="text-4xl mb-3">📭</div>
                <h4 className="text-[16px] font-bold text-slate-700">Không tìm thấy phòng khám nào phù hợp</h4>
                <p className="text-[13px] text-slate-400 mt-1">Vui lòng kiểm tra lại bộ lọc hoặc từ khóa tìm kiếm.</p>
              </div>
            ) : (
              <div className="overflow-x-auto flex-1">
                <table className="w-full text-left border-collapse text-[14px]">
                  <thead>
                    <tr className="bg-slate-50/75 border-b border-slate-200/80 text-slate-500 font-bold text-[12px] uppercase tracking-wider">
                      <th className="py-4 px-6">{renderSortHeader("Phòng", "name")}</th>
                      <th className="py-4 px-6">{renderSortHeader("Bác sĩ hiện tại", "currentDoctor")}</th>
                      <th className="py-4 px-6">{renderSortHeader("Nhân viên hỗ trợ", "currentSupportStaff")}</th>
                      <th className="py-4 px-6 text-center">{renderSortHeader("Trạng thái", "status")}</th>
                      <th className="py-4 px-6 text-center">{renderSortHeader("Lịch hẹn hôm nay", "todayAppointmentsCount")}</th>
                      <th className="py-4 px-6 text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-150">
                    {sortedAndFilteredRooms.map((room) => (
                      <tr 
                        key={room.id}
                        className="hover:bg-slate-50/40 transition-colors group"
                      >
                        {/* Phòng */}
                        <td className="py-4.5 px-6">
                          <button
                            onClick={() => openDetailModal(room)}
                            className="font-extrabold text-slate-900 text-left hover:text-[#b91c1c] transition-colors cursor-pointer group-hover:translate-x-0.5 inline-flex flex-col"
                          >
                            <span className="flex items-center gap-1.5">
                              {room.name}
                              <span className="text-[11px] font-bold text-slate-400 bg-slate-100 px-1.5 py-0.5 rounded">
                                {room.code}
                              </span>
                            </span>
                            <span className="text-[11px] font-semibold text-slate-400 mt-0.5">
                              {room.type} • Tầng {room.floor}
                            </span>
                          </button>
                        </td>

                        {/* Bác sĩ hiện tại */}
                        <td className="py-4.5 px-6 font-semibold text-slate-700">
                          {room.currentDoctor || "Chưa phân công"}
                        </td>

                        {/* Nhân viên hỗ trợ */}
                        <td className="py-4.5 px-6 font-semibold text-slate-600">
                          {room.currentSupportStaff || "Chưa phân công"}
                        </td>

                        {/* Trạng thái - click to change status */}
                        <td className="py-4.5 px-6 text-center">
                          <button
                            onClick={() => openStatusModal(room)}
                            className="hover:opacity-85 hover:scale-[1.02] active:scale-[0.98] transition-all cursor-pointer focus:outline-none block w-full text-center"
                            title="Click để đổi trạng thái"
                          >
                            {getStatusBadge(room.status)}
                          </button>
                        </td>

                        {/* Lịch hẹn hôm nay */}
                        <td className="py-4.5 px-6 text-center">
                          <span className={`inline-flex items-center justify-center w-7 h-7 rounded-full text-xs font-black ${
                            room.todayAppointmentsCount > 0 ? "bg-red-50 text-[#b91c1c] border border-red-100" : "bg-slate-100 text-slate-400"
                          }`}>
                            {room.todayAppointmentsCount}
                          </span>
                        </td>

                        {/* Thao tác (direct icons) */}
                        <td className="py-4.5 px-6 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <button
                              onClick={() => openEditModal(room)}
                              className="p-2 rounded-xl text-slate-500 hover:text-slate-900 hover:bg-slate-100 transition-all cursor-pointer"
                              title="Chỉnh sửa phòng"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L6.832 19.82a4.5 4.5 0 01-1.897 1.13l-2.685.8.8-2.685a4.5 4.5 0 011.13-1.897L16.863 4.487zm0 0L19.5 7.125" />
                              </svg>
                            </button>
                            <button
                              onClick={() => handleDeleteClick(room)}
                              className="p-2 rounded-xl text-[#b91c1c]/70 hover:text-[#b91c1c] hover:bg-red-50 transition-all cursor-pointer"
                              title="Xóa phòng"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
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
          </section>

        </div>

      </main>

      {/* ── MODAL 1: VIEW DETAILS ─────────────────────────────────────────── */}
      {showDetailModal && selectedRoom && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="bg-white w-full max-w-lg rounded-2xl shadow-xl overflow-hidden border border-slate-200 border-t-4 border-t-[#b91c1c]">
            <div className="px-6 py-4.5 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
              <div>
                <h3 className="text-[17px] font-black text-slate-900 flex items-center gap-2">
                  <span>🏢 {selectedRoom.name} ({selectedRoom.code})</span>
                </h3>
                <span className="text-[11px] text-slate-400 font-bold uppercase tracking-wider block mt-0.5">
                  Chi tiết phòng khám
                </span>
              </div>
              <button 
                onClick={closeDetailModal}
                className="text-slate-400 hover:text-slate-600 text-lg cursor-pointer"
              >
                ✕
              </button>
            </div>

            <div className="p-6 flex flex-col gap-5.5 text-[13px] max-h-[80vh] overflow-y-auto">
              
              {/* Basic Info Box */}
              <div>
                <h4 className="font-extrabold text-[12px] text-slate-400 uppercase tracking-wider mb-2.5">
                  Thông tin cơ bản
                </h4>
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
                    <span className="text-[11px] text-slate-400 font-semibold">Trạng thái</span>
                    <span className={`font-bold ${
                      selectedRoom.activeStatus === "Hoạt động" ? "text-emerald-600" : "text-slate-500"
                    }`}>
                      ● {selectedRoom.activeStatus}
                    </span>
                  </div>
                </div>
              </div>

              {/* Description */}
              {selectedRoom.description && (
                <div>
                  <h4 className="font-extrabold text-[12px] text-slate-400 uppercase tracking-wider mb-2">
                    Mô tả trang thiết bị
                  </h4>
                  <div className="bg-slate-50 p-3.5 rounded-xl border border-slate-150 font-semibold italic text-slate-700 leading-relaxed">
                    {selectedRoom.description}
                  </div>
                </div>
              )}

              {/* Personnel Assigned */}
              <div>
                <div className="flex justify-between items-center mb-2.5">
                  <h4 className="font-extrabold text-[12px] text-slate-400 uppercase tracking-wider">
                    Nhân sự được phân công hôm nay
                  </h4>
                  {!isEditingShifts ? (
                    <button
                      type="button"
                      onClick={() => {
                        setAssignMorningDoctor(selectedRoom.morningShift.doctor || AVAILABLE_DENTISTS[0]);
                        setAssignMorningSupport(selectedRoom.morningShift.supportStaff || AVAILABLE_SUPPORT_STAFF[0]);
                        setAssignAfternoonDoctor(selectedRoom.afternoonShift.doctor || AVAILABLE_DENTISTS[1]);
                        setAssignAfternoonSupport(selectedRoom.afternoonShift.supportStaff || AVAILABLE_SUPPORT_STAFF[0]);
                        setIsEditingShifts(true);
                      }}
                      className="text-xs bg-slate-100 hover:bg-slate-200 text-slate-600 hover:text-slate-800 px-2.5 py-1 rounded-lg font-bold flex items-center gap-1 transition-colors cursor-pointer"
                    >
                      ✏️ Phân công
                    </button>
                  ) : (
                    <div className="flex items-center gap-1.5">
                      <button
                        type="button"
                        onClick={handleSaveShifts}
                        className="text-xs bg-emerald-500 hover:bg-emerald-600 text-white px-2.5 py-1 rounded-lg font-bold transition-colors cursor-pointer"
                      >
                        Lưu
                      </button>
                      <button
                        type="button"
                        onClick={() => setIsEditingShifts(false)}
                        className="text-xs bg-slate-100 hover:bg-slate-200 text-slate-600 px-2.5 py-1 rounded-lg font-bold transition-colors cursor-pointer"
                      >
                        Hủy
                      </button>
                    </div>
                  )}
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  {/* Morning Shift */}
                  <div className="bg-red-50/25 border border-red-100 p-4 rounded-xl">
                    <span className="text-[11px] font-black text-[#b91c1c] uppercase block mb-2">Ca sáng</span>
                    {isEditingShifts ? (
                      <div className="flex flex-col gap-2">
                        <div className="flex flex-col gap-0.5">
                          <label className="text-[10px] text-slate-400 font-bold">Bác sĩ</label>
                          <select
                            value={assignMorningDoctor}
                            onChange={(e) => setAssignMorningDoctor(e.target.value)}
                            className="w-full px-2 py-1.5 border border-slate-200 rounded bg-white text-[12px] font-semibold focus:outline-none"
                          >
                            <option value="">-- Chọn bác sĩ --</option>
                            {AVAILABLE_DENTISTS.map((d, i) => (
                              <option key={i} value={d}>{d}</option>
                            ))}
                          </select>
                        </div>
                        <div className="flex flex-col gap-0.5">
                          <label className="text-[10px] text-slate-400 font-bold">Hỗ trợ</label>
                          <select
                            value={assignMorningSupport}
                            onChange={(e) => setAssignMorningSupport(e.target.value)}
                            className="w-full px-2 py-1.5 border border-slate-200 rounded bg-white text-[12px] font-semibold focus:outline-none"
                          >
                            <option value="">-- Chọn hỗ trợ --</option>
                            {AVAILABLE_SUPPORT_STAFF.map((s, i) => (
                              <option key={i} value={s}>{s}</option>
                            ))}
                          </select>
                        </div>
                      </div>
                    ) : (
                      <div className="flex flex-col gap-1.5 font-semibold text-slate-700">
                        <div>🧑‍⚕️ Bác sĩ: <span className="font-extrabold text-slate-900">{selectedRoom.morningShift.doctor || "Chưa phân công"}</span></div>
                        <div>👥 Hỗ trợ: <span className="font-extrabold text-slate-900">{selectedRoom.morningShift.supportStaff || "Chưa phân công"}</span></div>
                      </div>
                    )}
                  </div>

                  {/* Afternoon Shift */}
                  <div className="bg-blue-50/20 border border-blue-100/60 p-4 rounded-xl">
                    <span className="text-[11px] font-black text-blue-700 uppercase block mb-2">Ca chiều</span>
                    {isEditingShifts ? (
                      <div className="flex flex-col gap-2">
                        <div className="flex flex-col gap-0.5">
                          <label className="text-[10px] text-slate-400 font-bold">Bác sĩ</label>
                          <select
                            value={assignAfternoonDoctor}
                            onChange={(e) => setAssignAfternoonDoctor(e.target.value)}
                            className="w-full px-2 py-1.5 border border-slate-200 rounded bg-white text-[12px] font-semibold focus:outline-none"
                          >
                            <option value="">-- Chọn bác sĩ --</option>
                            {AVAILABLE_DENTISTS.map((d, i) => (
                              <option key={i} value={d}>{d}</option>
                            ))}
                          </select>
                        </div>
                        <div className="flex flex-col gap-0.5">
                          <label className="text-[10px] text-slate-400 font-bold">Hỗ trợ</label>
                          <select
                            value={assignAfternoonSupport}
                            onChange={(e) => setAssignAfternoonSupport(e.target.value)}
                            className="w-full px-2 py-1.5 border border-slate-200 rounded bg-white text-[12px] font-semibold focus:outline-none"
                          >
                            <option value="">-- Chọn hỗ trợ --</option>
                            {AVAILABLE_SUPPORT_STAFF.map((s, i) => (
                              <option key={i} value={s}>{s}</option>
                            ))}
                          </select>
                        </div>
                      </div>
                    ) : (
                      <div className="flex flex-col gap-1.5 font-semibold text-slate-700">
                        <div>🧑‍⚕️ Bác sĩ: <span className="font-extrabold text-slate-900">{selectedRoom.afternoonShift.doctor || "Chưa phân công"}</span></div>
                        <div>👥 Hỗ trợ: <span className="font-extrabold text-slate-900">{selectedRoom.afternoonShift.supportStaff || "Chưa phân công"}</span></div>
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* Appointments of the day */}
              <div>
                <h4 className="font-extrabold text-[12px] text-slate-400 uppercase tracking-wider mb-2.5 flex justify-between items-center">
                  <span>Lịch hẹn trong ngày</span>
                  <span className="bg-slate-100 text-slate-600 px-2 py-0.5 rounded text-[11px] font-bold">
                    Tổng: {selectedRoom.appointments.length}
                  </span>
                </h4>
                {selectedRoom.appointments.length === 0 ? (
                  <div className="bg-slate-50 border-2 border-dashed border-slate-200 p-6 text-center rounded-xl font-bold text-slate-400">
                    Không có lịch hẹn nào hôm nay
                  </div>
                ) : (
                  <div className="border border-slate-200 rounded-xl overflow-hidden">
                    <table className="w-full text-left text-[12px]">
                      <thead>
                        <tr className="bg-slate-50 border-b border-slate-200 font-bold text-slate-500 uppercase">
                          <th className="py-2.5 px-4">Giờ</th>
                          <th className="py-2.5 px-4">Bệnh nhân</th>
                          <th className="py-2.5 px-4 text-right">Trạng thái</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 font-semibold">
                        {selectedRoom.appointments.map((appt, idx) => (
                          <tr key={idx} className="hover:bg-slate-50/50">
                            <td className="py-2.5 px-4 font-bold text-slate-800">{appt.time}</td>
                            <td className="py-2.5 px-4 text-slate-700">{appt.patientName}</td>
                            <td className="py-2.5 px-4 text-right">
                              <span className={`inline-block px-2 py-0.5 rounded-full text-[10px] font-extrabold ${
                                appt.status === "Đã đến" ? "bg-amber-50 text-amber-700 border border-amber-200" :
                                appt.status === "Chờ khám" ? "bg-blue-50 text-blue-700 border border-blue-200" :
                                "bg-emerald-50 text-emerald-700 border border-emerald-200"
                              }`}>
                                {appt.status}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {/* Close Button */}
              <div className="flex justify-end gap-3 mt-2 border-t border-slate-100 pt-4">
                <button
                  type="button"
                  onClick={closeDetailModal}
                  className="px-5 py-2 bg-slate-150 hover:bg-slate-200 text-slate-700 rounded-lg font-bold cursor-pointer transition-colors"
                >
                  Đóng
                </button>
              </div>

            </div>
          </div>
        </div>
      )}

      {/* ── MODAL 2: EDIT DETAILS ────────────────────────────────────────── */}
      {showEditRoomModal && selectedRoom && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-xl overflow-hidden border border-slate-200 border-t-4 border-t-slate-800">
            <div className="px-6 py-4 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
              <div>
                <h3 className="text-[17px] font-black text-slate-900">
                  Chỉnh sửa {selectedRoom.name}
                </h3>
                <span className="text-[11px] text-slate-400 font-bold block mt-0.5 uppercase tracking-wider">
                  Cập nhật thông tin phòng khám
                </span>
              </div>
              <button 
                type="button"
                onClick={() => { setShowEditRoomModal(false); setSelectedRoom(null); }}
                className="text-slate-400 hover:text-slate-600 text-lg cursor-pointer"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleEditRoomSubmit} className="p-6 flex flex-col gap-4 text-[13px]">
              
              <div className="flex flex-col gap-1">
                <label className="font-bold text-slate-600">Tên phòng <span className="text-[#b91c1c]">*</span></label>
                <input
                  type="text"
                  required
                  value={editRoomName}
                  onChange={(e) => setEditRoomName(e.target.value)}
                  className="w-full px-3.5 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:border-slate-800 font-semibold"
                />
              </div>

              <div className="flex flex-col gap-1">
                <label className="font-bold text-slate-600">Mã phòng <span className="text-[#b91c1c]">*</span></label>
                <input
                  type="text"
                  required
                  value={editRoomCode}
                  onChange={(e) => setEditRoomCode(e.target.value)}
                  className="w-full px-3.5 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:border-slate-800 font-semibold uppercase"
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="font-bold text-slate-600">Loại phòng <span className="text-[#b91c1c]">*</span></label>
                <div className="grid grid-cols-2 gap-2 mt-1">
                  {[
                    { name: "Khám tổng quát", icon: "🦷" },
                    { name: "Cấp cứu", icon: "🚨" },
                    { name: "Phẫu thuật", icon: "😷" },
                    { name: "X-Quang", icon: "🩻" }
                  ].map((t) => {
                    const isSelected = editRoomType === t.name;
                    return (
                      <button
                        key={t.name}
                        type="button"
                        onClick={() => setEditRoomType(t.name)}
                        className={`flex items-center gap-2.5 p-3 rounded-xl border text-left transition-all hover:scale-[1.01] cursor-pointer ${
                          isSelected 
                            ? "border-[#b91c1c] text-[#b91c1c] bg-red-50/15 font-bold shadow-sm"
                            : "border-slate-200 text-slate-600 bg-white hover:border-slate-300 font-medium"
                        }`}
                      >
                        <span className="text-base select-none">{t.icon}</span>
                        <span className="text-[12.5px] truncate">{t.name}</span>
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="flex flex-col gap-1">
                <label className="font-bold text-slate-600">Mô tả chi tiết trang thiết bị</label>
                <textarea
                  rows={4}
                  placeholder="Nhập mô tả về trang thiết bị..."
                  value={editRoomDescription}
                  onChange={(e) => setEditRoomDescription(e.target.value)}
                  className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-slate-800 focus:outline-none transition-all font-semibold resize-none text-[13px]"
                />
              </div>

              <div className="flex justify-end gap-3 mt-2 border-t border-slate-100 pt-4">
                <button
                  type="button"
                  onClick={() => { setShowEditRoomModal(false); setSelectedRoom(null); }}
                  className="px-4 py-2 bg-slate-100 hover:bg-slate-200 rounded-lg font-bold text-slate-600 cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-lg font-bold cursor-pointer"
                >
                  Lưu thay đổi
                </button>
              </div>

            </form>
          </div>
        </div>
      )}

      {/* ── MODAL 3: CHANGE STATUS ────────────────────────────────────────── */}
      {showStatusModal && selectedRoom && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-xl overflow-hidden border border-slate-200 border-t-4 border-t-[#b91c1c]">
            <div className="px-6 py-4 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
              <div>
                <h3 className="text-[17px] font-black text-slate-900">
                  Chuyển trạng thái
                </h3>
                <span className="text-[11px] text-slate-400 font-bold block mt-0.5 uppercase tracking-wider">
                  Cập nhật hoạt động: {selectedRoom.name}
                </span>
              </div>
              <button 
                type="button"
                onClick={() => { setShowStatusModal(false); setSelectedRoom(null); }}
                className="text-slate-400 hover:text-slate-600 text-lg cursor-pointer"
              >
                ✕
              </button>
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
                  ].map((item) => {
                    const isSelected = statusValue === item.name;
                    return (
                      <button
                        key={item.name}
                        type="button"
                        onClick={() => setStatusValue(item.name as any)}
                        className={`flex items-center justify-between p-3 rounded-xl border text-left transition-all hover:scale-[1.005] cursor-pointer ${
                          isSelected 
                            ? "border-[#b91c1c] bg-red-50/10 shadow-sm" 
                            : "border-slate-200 bg-white hover:border-slate-300"
                        }`}
                      >
                        <div className="flex items-center gap-3">
                          <span className={`w-3 h-3 rounded-full shrink-0 ${item.color}`} />
                          <div>
                            <span className="font-bold text-slate-800 text-[13.5px]">{item.name}</span>
                            <p className="text-[11px] text-slate-400 mt-0.5 font-medium">{item.desc}</p>
                          </div>
                        </div>
                        {isSelected && (
                          <span className="text-[#b91c1c] font-black text-sm pr-1.5">✓</span>
                        )}
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="flex justify-end gap-3 mt-2 border-t border-slate-100 pt-4">
                <button
                  type="button"
                  onClick={() => { setShowStatusModal(false); setSelectedRoom(null); }}
                  className="px-4 py-2 bg-slate-100 hover:bg-slate-200 rounded-lg font-bold text-slate-600 cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 bg-[#b91c1c] hover:bg-[#991b1b] text-white rounded-lg font-bold cursor-pointer transition-colors shadow-sm"
                >
                  Cập nhật
                </button>
              </div>

            </form>
          </div>
        </div>
      )}

      {/* ── MODAL 4: DELETE CONFIRMATION ──────────────────────────────────── */}
      {showDeleteModal && roomToDelete && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="bg-white w-full max-w-sm rounded-2xl shadow-xl overflow-hidden border border-slate-200 border-t-4 border-t-red-600">
            <div className="p-6 flex flex-col items-center text-center gap-4 bg-slate-50/20">
              
              {/* Alert Warning icon */}
              <div className="w-14 h-14 rounded-full bg-red-50 text-red-600 flex items-center justify-center animate-bounce">
                <svg className="w-8 h-8" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
              </div>

              <div>
                <h3 className="text-[17px] font-black text-slate-900">
                  Xác nhận xóa phòng?
                </h3>
                <p className="text-[13px] text-slate-500 font-semibold leading-relaxed mt-2">
                  Bạn có chắc chắn muốn xóa phòng khám <span className="text-slate-900 font-black">{roomToDelete.name}</span> ({roomToDelete.code}) không? 
                </p>
                <p className="text-[11px] text-red-600/80 font-bold bg-red-50 rounded-lg p-2.5 border border-red-100 mt-3 leading-relaxed">
                  ⚠️ Hành động này không thể hoàn tác. Lịch hẹn và dữ liệu của phòng sẽ bị xóa khỏi hệ thống.
                </p>
              </div>

              <div className="flex gap-3 w-full mt-2 border-t border-slate-100 pt-4 text-[13px]">
                <button
                  type="button"
                  onClick={() => { setShowDeleteModal(false); setRoomToDelete(null); }}
                  className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 rounded-xl font-bold text-slate-600 cursor-pointer transition-colors"
                >
                  Hủy bỏ
                </button>
                <button
                  type="button"
                  onClick={handleConfirmDelete}
                  className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white rounded-xl font-bold cursor-pointer transition-colors shadow-md shadow-red-600/10"
                >
                  Đồng ý xóa
                </button>
              </div>

            </div>
          </div>
        </div>
      )}

    </div>
  );
}
