"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import Sidebar from "../../../../components/shared/Sidebar";
import Header from "../../../../components/Header";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";

// ── Types ──────────────────────────────────────────────────────────────────

export interface ShiftStaff {
  doctor: string;
  supportStaff: string;
}

export interface Appointment {
  time: string;
  patientName: string;
  status: string;
}

export interface Room {
  id: string;
  code: string;
  name: string;
  floor: string;
  type: string;
  status: "Trống" | "Đang khám" | "Đang vệ sinh" | "Bảo trì" | "Ngừng hoạt động";
  activeStatus: "Hoạt động" | "Ngừng hoạt động";
  currentDoctor: string;
  currentSupportStaff: string;
  todayAppointmentsCount: number;
  morningShift: ShiftStaff;
  afternoonShift: ShiftStaff;
  appointments: Appointment[];
}

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
    ]
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
    ]
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
    appointments: []
  }
];

export default function CreateRoomPage() {
  useRequireAdmin();
  const router = useRouter();

  // ── Form States ───────────────────────────────────────────────────────────
  const [roomName, setRoomName] = useState("");
  const [roomCode, setRoomCode] = useState("");
  const [floor, setFloor] = useState("");
  const [selectedType, setSelectedType] = useState<string>("Khám tổng quát");
  const [description, setDescription] = useState("");
  
  // File uploads state (prefilled with screenshot mock file)
  const [uploadedFiles, setUploadedFiles] = useState<{ id: string; name: string }[]>([
    { id: "file-1", name: "room_view_01.jpg" }
  ]);
  const [dragActive, setDragActive] = useState(false);

  // ── Drag & Drop Handlers ──────────────────────────────────────────────────
  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      const filesArray = Array.from(e.dataTransfer.files);
      const newFiles = filesArray.map((file, idx) => ({
        id: `file-${Date.now()}-${idx}`,
        name: file.name
      }));
      setUploadedFiles(prev => [...prev, ...newFiles]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const filesArray = Array.from(e.target.files);
      const newFiles = filesArray.map((file, idx) => ({
        id: `file-${Date.now()}-${idx}`,
        name: file.name
      }));
      setUploadedFiles(prev => [...prev, ...newFiles]);
    }
  };

  const removeFile = (id: string) => {
    setUploadedFiles(prev => prev.filter(f => f.id !== id));
  };

  // ── Submit Handler ────────────────────────────────────────────────────────
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!roomName.trim()) {
      alert("Vui lòng nhập Tên phòng");
      return;
    }
    if (!roomCode.trim()) {
      alert("Vui lòng nhập Mã phòng");
      return;
    }
    if (!floor) {
      alert("Vui lòng chọn Tầng");
      return;
    }

    // Load existing rooms
    const stored = localStorage.getItem("dental_clinic_rooms");
    let currentRooms: Room[] = [];
    if (stored) {
      try {
        currentRooms = JSON.parse(stored);
      } catch {
        currentRooms = INITIAL_ROOMS;
      }
    } else {
      currentRooms = INITIAL_ROOMS;
    }

    // Check if room name or code already exists
    if (currentRooms.some(r => r.name.toLowerCase() === roomName.trim().toLowerCase())) {
      alert("Tên phòng này đã tồn tại trên hệ thống!");
      return;
    }
    if (currentRooms.some(r => r.code.toUpperCase() === roomCode.trim().toUpperCase())) {
      alert("Mã phòng này đã tồn tại trên hệ thống!");
      return;
    }

    // Create new room structure matching updated schema
    const newRoom: Room = {
      id: `room-${Date.now()}`,
      code: roomCode.trim().toUpperCase(),
      name: roomName.trim(),
      floor: floor,
      type: selectedType,
      status: "Trống",
      activeStatus: "Hoạt động",
      currentDoctor: "Chưa phân công",
      currentSupportStaff: "Chưa phân công",
      todayAppointmentsCount: 0,
      morningShift: {
        doctor: "",
        supportStaff: ""
      },
      afternoonShift: {
        doctor: "",
        supportStaff: ""
      },
      appointments: []
    };

    const updatedRooms = [...currentRooms, newRoom];
    localStorage.setItem("dental_clinic_rooms", JSON.stringify(updatedRooms));
    
    // Redirect back to rooms listing
    router.push("/dashboard/rooms");
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      
      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <Sidebar activeMenu="rooms" />

      {/* ── MAIN CONTENT AREA ────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* Custom Header matching screenshot top layout */}
        <Header 
          title="" 
          showSearch={true}
          rightActions={
            <div className="flex items-center gap-5 mr-1 font-bold text-[14px] text-slate-500">
              <button className="p-2 rounded-full bg-slate-100 text-slate-500 hover:bg-red-50 hover:text-primary transition-all cursor-pointer">
                <svg className="w-5.5 h-5.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 011.37.49l1.296 2.247a1.125 1.125 0 01-.26 1.43l-1.003.828c-.293.241-.438.613-.43.992a7.723 7.723 0 010 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.954.26 1.43l-1.298 2.247a1.125 1.125 0 01-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.57 6.57 0 01-.22.128c-.331.183-.581.495-.644.869l-.213 1.28c-.09.543-.56.94-1.11.94h-2.594c-.55 0-1.02-.397-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 01-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 01-1.369-.49l-1.297-2.247a1.125 1.125 0 01.26-1.43l1.004-.827c.292-.24.437-.613.43-.992a6.932 6.932 0 010-.255c.007-.378-.138-.75-.43-.991l-1.004-.827a1.125 1.125 0 01-.26-1.43l1.297-2.247a1.125 1.125 0 011.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.087.22-.128.332-.183.582-.495.645-.869l.214-1.28z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </button>
              <a href="#" className="hover:text-slate-950 transition-colors">Hỗ trợ</a>
              <span className="w-px h-4 bg-slate-200"></span>
              <a href="#" className="hover:text-slate-950 transition-colors">Báo cáo</a>
            </div>
          }
        />

        {/* PAGE CONTENT WRAPPER */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          
          {/* Breadcrumb Navigation */}
          <nav className="text-[13px] font-bold text-slate-400 flex items-center gap-1.5 select-none">
            <Link href="/dashboard/rooms" className="hover:text-slate-600 transition-colors">Quản lý phòng</Link>
            <span>/</span>
            <span className="text-slate-500 font-extrabold">Thêm phòng mới</span>
          </nav>

          {/* Page Headers */}
          <div>
            <h1 className="text-3xl font-black text-slate-900 tracking-tight">Thêm phòng mới</h1>
            <p className="text-[14px] text-slate-400 font-semibold mt-1">
              Vui lòng điền thông tin chi tiết để thiết lập phòng điều trị mới vào hệ thống.
            </p>
          </div>

          {/* Form tag wrapping the layout */}
          <form onSubmit={handleSubmit} className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
            
            {/* ── LEFT COLUMN (65%) ────────────────────────────────────────── */}
            <div className="lg:col-span-8 flex flex-col gap-6">
              
              {/* Card A: Thông tin cơ bản */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                
                {/* Title */}
                <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                  <span className="w-5 h-5 rounded-full bg-red-15 text-primary flex items-center justify-center font-bold text-[13px]">
                    ℹ️
                  </span>
                  <h3 className="text-[16px] font-extrabold text-slate-800">Thông tin cơ bản</h3>
                </div>

                {/* Name, Code & Floor Inputs Grid */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 text-[13px]">
                  
                  {/* Name Input */}
                  <div className="flex flex-col gap-2">
                    <label className="font-bold text-slate-500">
                      Tên phòng <span className="text-primary">*</span>
                    </label>
                    <input
                      type="text"
                      required
                      placeholder="VD: Phòng 4"
                      value={roomName}
                      onChange={(e) => setRoomName(e.target.value)}
                      className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold"
                    />
                  </div>

                  {/* Code Input */}
                  <div className="flex flex-col gap-2">
                    <label className="font-bold text-slate-500">
                      Mã phòng <span className="text-primary">*</span>
                    </label>
                    <input
                      type="text"
                      required
                      placeholder="VD: P004"
                      value={roomCode}
                      onChange={(e) => setRoomCode(e.target.value)}
                      className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold uppercase"
                    />
                  </div>

                  {/* Floor Select */}
                  <div className="flex flex-col gap-2">
                    <label className="font-bold text-slate-500">
                      Tầng <span className="text-primary">*</span>
                    </label>
                    <div className="relative">
                      <select
                        required
                        value={floor}
                        onChange={(e) => setFloor(e.target.value)}
                        className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold appearance-none cursor-pointer"
                      >
                        <option value="">Chọn tầng</option>
                        <option value="1">Tầng 1</option>
                        <option value="2">Tầng 2</option>
                        <option value="3">Tầng 3</option>
                      </select>
                      <span className="absolute right-4 top-1/2 -translate-y-1/2 pointer-events-none text-slate-400 text-xs font-bold">
                        ▼
                      </span>
                    </div>
                  </div>

                </div>

                {/* Room Type Selector (Big Cards) */}
                <div className="flex flex-col gap-2 text-[13px] mt-1">
                  <label className="font-bold text-slate-500">
                    Loại phòng <span className="text-primary">*</span>
                  </label>
                  
                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-1">
                    
                    {/* Option 1: Tổng quát */}
                    <button
                      type="button"
                      onClick={() => setSelectedType("Khám tổng quát")}
                      className={`flex flex-col items-center justify-center p-5 rounded-xl border transition-all hover:scale-[1.02] cursor-pointer ${
                        selectedType === "Khám tổng quát"
                          ? "border-[#b91c1c] text-[#b91c1c] bg-red-50/15"
                          : "border-slate-200 text-slate-500 bg-white hover:border-slate-300"
                      }`}
                    >
                      {/* Tooth icon */}
                      <svg className={`w-8 h-8 mb-2.5 transition-colors ${selectedType === "Khám tổng quát" ? "text-[#b91c1c]" : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 2C8.5 2 6 4.5 6 8c0 3 2 5.5 2 8 0 1.5-1 3.5-1 4.5 0 1.5 2.5 1.5 3.5.5s1.5-2 1.5-3c0 1 .5 2 1.5 3s3.5 1 3.5-.5c0-1-1-3-1-4.5 0-2.5 2-5 2-8 0-3.5-2.5-6-6-6z" />
                      </svg>
                      <span className="text-[13px] font-extrabold leading-none">Tổng quát</span>
                    </button>

                    {/* Option 2: Cấp cứu */}
                    <button
                      type="button"
                      onClick={() => setSelectedType("Cấp cứu")}
                      className={`flex flex-col items-center justify-center p-5 rounded-xl border transition-all hover:scale-[1.02] cursor-pointer ${
                        selectedType === "Cấp cứu"
                          ? "border-[#b91c1c] text-[#b91c1c] bg-red-50/15"
                          : "border-slate-200 text-slate-500 bg-white hover:border-slate-300"
                      }`}
                    >
                      {/* Star Cross icon */}
                      <svg className={`w-8 h-8 mb-2.5 transition-colors ${selectedType === "Cấp cứu" ? "text-[#b91c1c]" : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15M17.3 6.7L6.7 17.3M6.7 6.7l10.6 10.6" />
                      </svg>
                      <span className="text-[13px] font-extrabold leading-none">Cấp cứu</span>
                    </button>

                    {/* Option 3: Phẫu thuật */}
                    <button
                      type="button"
                      onClick={() => setSelectedType("Phẫu thuật")}
                      className={`flex flex-col items-center justify-center p-5 rounded-xl border transition-all hover:scale-[1.02] cursor-pointer ${
                        selectedType === "Phẫu thuật"
                          ? "border-[#b91c1c] text-[#b91c1c] bg-red-50/15"
                          : "border-slate-200 text-slate-500 bg-white hover:border-slate-300"
                      }`}
                    >
                      {/* Mask icon */}
                      <svg className={`w-8 h-8 mb-2.5 transition-colors ${selectedType === "Phẫu thuật" ? "text-[#b91c1c]" : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                        <rect x="3" y="6" width="18" height="12" rx="2" />
                        <path strokeLinecap="round" strokeLinejoin="round" d="M3 8L1 6M21 8l2-2M3 16l-2 2M21 16l2 2M8 10h8M8 14h8" />
                      </svg>
                      <span className="text-[13px] font-extrabold leading-none">Phẫu thuật</span>
                    </button>

                    {/* Option 4: X-Quang */}
                    <button
                      type="button"
                      onClick={() => setSelectedType("X-Quang")}
                      className={`flex flex-col items-center justify-center p-5 rounded-xl border transition-all hover:scale-[1.02] cursor-pointer ${
                        selectedType === "X-Quang"
                          ? "border-[#b91c1c] text-[#b91c1c] bg-red-50/15"
                          : "border-slate-200 text-slate-500 bg-white hover:border-slate-300"
                      }`}
                    >
                      {/* Roller Scanner icon */}
                      <svg className={`w-8 h-8 mb-2.5 transition-colors ${selectedType === "X-Quang" ? "text-[#b91c1c]" : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                        <rect x="6" y="3" width="12" height="14" rx="1" />
                        <path strokeLinecap="round" strokeLinejoin="round" d="M6 7h12M6 11h12M12 3v14M9 21h6M12 17v4" />
                      </svg>
                      <span className="text-[13px] font-extrabold leading-none">X-Quang</span>
                    </button>

                  </div>
                </div>

              </div>

              {/* Card B: Ghi chú & Mô tả */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                
                {/* Title */}
                <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                  <span className="text-[16px] text-[#b91c1c]">📋</span>
                  <h3 className="text-[16px] font-extrabold text-slate-800">Ghi chú & Mô tả</h3>
                </div>

                {/* Textarea Field */}
                <div className="flex flex-col gap-2 text-[13px]">
                  <label className="font-bold text-slate-500">Mô tả chi tiết trang thiết bị</label>
                  <textarea
                    rows={4}
                    placeholder="Nhập mô tả về trang thiết bị có sẵn trong phòng, tình trạng hiện tại..."
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold resize-none"
                  />
                </div>

              </div>

            </div>

            {/* ── RIGHT COLUMN (35%) ───────────────────────────────────────── */}
            <div className="lg:col-span-4 flex flex-col gap-6">
              
              {/* Card C: Hình ảnh phòng */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                
                {/* Title */}
                <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                  <span className="text-[16px] text-[#b91c1c]">🗂️</span>
                  <h3 className="text-[16px] font-extrabold text-slate-800">Hình ảnh phòng</h3>
                </div>

                {/* Drag and Drop Zone */}
                <div 
                  onDragEnter={handleDrag}
                  onDragOver={handleDrag}
                  onDragLeave={handleDrag}
                  onDrop={handleDrop}
                  className={`border-2 border-dashed rounded-2xl p-6 text-center flex flex-col items-center justify-center gap-2.5 transition-all select-none relative ${
                    dragActive 
                      ? "border-[#b91c1c] bg-red-50/10 scale-[1.01]" 
                      : "border-slate-200 hover:border-slate-300 bg-slate-50/50"
                  }`}
                >
                  <input
                    type="file"
                    multiple
                    onChange={handleFileChange}
                    className="absolute inset-0 opacity-0 cursor-pointer"
                  />
                  
                  {/* Cloud Icon */}
                  <span className="w-12 h-12 rounded-full bg-red-50 text-primary flex items-center justify-center">
                    <svg className="w-6.5 h-6.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 16.5V9.75m0 0l3 3m-3-3l-3 3M6.75 19.5a4.5 4.5 0 01-1.41-8.775 5.25 5.25 0 0110.233-2.33 3 3 0 013.758 3.848A3.752 3.752 0 0118 19.5H6.75z" />
                    </svg>
                  </span>
                  
                  <div className="text-[13px] font-extrabold text-slate-700">
                    Tải ảnh lên hoặc kéo thả
                  </div>
                  <div className="text-[11px] font-bold text-slate-400 uppercase">
                    PNG, JPG (Tối đa 5MB)
                  </div>
                </div>

                {/* Uploaded Files List */}
                {uploadedFiles.length > 0 && (
                  <div className="flex flex-col gap-2 mt-1">
                    {uploadedFiles.map(file => (
                      <div 
                        key={file.id} 
                        className="flex items-center justify-between p-3 rounded-xl bg-slate-50 border border-slate-200/50 text-[12px] font-bold text-slate-600 hover:border-slate-300 transition-all"
                      >
                        <div className="flex items-center gap-2 truncate">
                          <span className="text-[14px]">🖼️</span>
                          <span className="truncate text-slate-700 font-extrabold">{file.name}</span>
                        </div>
                        <button
                          type="button"
                          onClick={() => removeFile(file.id)}
                          className="w-5 h-5 rounded-full hover:bg-slate-200/80 text-slate-400 hover:text-slate-700 flex items-center justify-center shrink-0 cursor-pointer text-[10px] font-extrabold"
                        >
                          ✕
                        </button>
                      </div>
                    ))}
                  </div>
                )}

              </div>

              {/* Card D: Xác nhận thiết lập */}
              <div className="bg-[#b91c1c] p-6 rounded-3xl text-white shadow-lg shadow-red-700/15 flex flex-col gap-4">
                
                {/* Badge title */}
                <div className="flex items-center gap-2">
                  <span className="w-5 h-5 rounded-full bg-white/15 text-white flex items-center justify-center">
                    ✓
                  </span>
                  <h4 className="text-[14px] font-extrabold uppercase tracking-wider">Xác nhận thiết lập</h4>
                </div>

                {/* Text description */}
                <p className="text-[12px] font-medium leading-relaxed opacity-90">
                  Khi bạn lưu phòng này, nó sẽ ngay lập tức xuất hiện trong danh sách lập lịch khám bệnh. Hãy chắc chắn rằng trang thiết bị đã sẵn sàng.
                </p>

                {/* Action Buttons */}
                <div className="flex flex-col gap-2.5 mt-2 text-[14px]">
                  
                  {/* Save Button */}
                  <button
                    type="submit"
                    className="w-full bg-white hover:bg-slate-50 text-[#b91c1c] font-black py-3 rounded-xl flex items-center justify-center gap-2 cursor-pointer transition-all hover:scale-[1.01] shadow-md shadow-black/5"
                  >
                    {/* Floppy Disk Icon */}
                    <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 13.5V12m0 0V10.5m0 3h3m-3 0H6m12-5.528v10.528c0 .621-.504 1.125-1.125 1.125H3.125A1.125 1.125 0 012 16.875V3.125C2 2.504 2.504 2 3.125 2h9.728c.298 0 .585.118.796.328l4.019 4.018c.21.21.328.498.328.796z" />
                    </svg>
                    Lưu phòng
                  </button>

                  {/* Cancel Link */}
                  <Link
                    href="/dashboard/rooms"
                    className="w-full border border-white/25 hover:border-white/50 hover:bg-white/5 text-white font-black py-3 rounded-xl flex items-center justify-center cursor-pointer transition-all text-center"
                  >
                    Hủy
                  </Link>

                </div>

              </div>

              {/* Card E: Mẹo quản trị */}
              <div className="bg-white p-4 rounded-xl border border-slate-200/60 shadow-sm border-l-4 border-l-red-500/80 flex items-start gap-3">
                <span className="text-[17px] text-[#b91c1c] shrink-0 mt-0.5 animate-pulse">💡</span>
                <div>
                  <span className="text-[11px] font-bold text-slate-400 block uppercase tracking-wider">Mẹo quản trị</span>
                  <p className="text-[12px] text-slate-500 font-semibold leading-relaxed mt-0.5">
                    Bạn có thể quản lý lịch làm việc của bác sĩ theo từng phòng sau khi tạo thành công.
                  </p>
                </div>
              </div>

            </div>

          </form>

        </div>

      </main>

    </div>
  );
}
