"use client";

import React, { useState, useEffect, useMemo, useRef, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import NotificationBell from "../../../../components/shared/NotificationBell";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import { getWeekScheduleApi, saveWeekScheduleApi, getStaffApi, getRoomsApi, type RoomDto } from "../../../../lib/apiClient";
import * as XLSX from "xlsx";

interface ScheduleEntry {
  id: string;
  date: string;
  shift: "morning" | "afternoon";
  type: "dentist" | "staff";
  role: "dentist" | "assistant" | "staff";
  name: string;
  room: string;
  roomColor: string;
  isHoliday?: boolean;
  isDraft?: boolean;
}

interface StaffMember {
  id: string;
  name: string;
  specialization: string;
  type: "dentist" | "assistant" | "staff";
  status: "ACTIVE" | "INACTIVE";
}

interface RoomRow {
  label: string;
  key: string;
  color: string;
  isDisabled: boolean;
  disabledReason: string | null;
}

const ROOM_COLORS_DENTIST = ["border-primary", "border-secondary", "border-purple-600", "border-orange-500", "border-pink-600"];
const ROOM_COLORS_STAFF   = ["border-green-600", "border-teal-600", "border-indigo-600", "border-yellow-600"];
const DISABLED_ROOM_STATUSES = ["Bảo trì", "Ngừng hoạt động"];
const STAFF_ROOM_KEYWORDS    = ["lễ tân", "cskh", "chăm sóc", "kế toán", "hành chính", "reception"];

const isStaffRoomType = (room: RoomDto) =>
  STAFF_ROOM_KEYWORDS.some(k => (room.type ?? "").toLowerCase().includes(k));

const getWeekDates = (mondayDate: Date): Date[] => {
  const dates: Date[] = [];
  for (let i = 0; i < 7; i++) {
    const tempDate = new Date(mondayDate);
    tempDate.setDate(mondayDate.getDate() + i);
    dates.push(tempDate);
  }
  return dates;
};

const formatDateKey = (date: Date): string => {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, "0");
  const dd = String(date.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
};

const DEFAULT_MONDAY_STR = "2023-10-16";

function EditScheduleContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const weekParam = searchParams.get("week") || DEFAULT_MONDAY_STR;
  const currentMonday = useMemo(() => {
    const d = new Date(weekParam);
    return isNaN(d.getTime()) ? new Date(DEFAULT_MONDAY_STR) : d;
  }, [weekParam]);

  const [staffDatabase, setStaffDatabase] = useState<StaffMember[]>([]);
  const [isLoadingStaff, setIsLoadingStaff] = useState(true);
  const [allRooms, setAllRooms] = useState<RoomDto[]>([]);

  const [staffType, setStaffType] = useState<"dentist" | "staff">("dentist");
  const [searchQuery, setSearchQuery] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [activeWeekSchedules, setActiveWeekSchedules] = useState<ScheduleEntry[]>([]);
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" | "info" } | null>(null);
  const [modalCell, setModalCell] = useState<{
    date: string;
    shift: "morning" | "afternoon";
    room: string;
    roomColor: string;
    role: "dentist" | "assistant" | "staff";
  } | null>(null);
  const [modalSearchQuery, setModalSearchQuery] = useState("");
  const [dayActionDate, setDayActionDate] = useState<string | null>(null);
  const [confirmClearAllOpen, setConfirmClearAllOpen] = useState(false);

  const showToast = (message: string, type: "success" | "error" | "info" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  const weekDates = useMemo(() => getWeekDates(currentMonday), [currentMonday]);

  useEffect(() => {
    setIsLoadingStaff(true);
    getStaffApi({ pageSize: 100, status: "Active" })
      .then(res => {
        const members: StaffMember[] = res.items.map(dto => {
          const roleLower = (dto.role ?? "").toLowerCase();
          let type: "dentist" | "assistant" | "staff";
          if (roleLower === "dentist" || roleLower === "doctor") {
            type = "dentist";
          } else if (roleLower === "assistant" || (dto.position ?? "").toLowerCase().includes("phụ tá")) {
            type = "assistant";
          } else {
            type = "staff";
          }
          const specialization =
            type === "dentist"
              ? (dto.specialty ?? dto.position ?? "Bác sĩ")
              : (dto.position ?? dto.department ?? "Nhân viên");
          return { id: dto.id, name: dto.fullName ?? dto.username, specialization, type, status: "ACTIVE" as const };
        });
        setStaffDatabase(members);
      })
      .catch(() => setStaffDatabase([]))
      .finally(() => setIsLoadingStaff(false));
  }, []);

  useEffect(() => {
    getRoomsApi()
      .then(rooms => setAllRooms(rooms))
      .catch(() => setAllRooms([]));
  }, []);

  useEffect(() => {
    getWeekScheduleApi(weekParam)
      .then(dtos => setActiveWeekSchedules(dtos.map(dto => ({
        id: dto.id, date: dto.date, shift: dto.shift, type: dto.type, role: dto.role,
        name: dto.name, room: dto.room, roomColor: dto.roomColor, isHoliday: dto.isHoliday,
      }))))
      .catch(() => setActiveWeekSchedules([]));
  }, [weekParam]);

  const formattedWeekRange = useMemo(() => {
    const monday = weekDates[0];
    const sunday = weekDates[6];
    const pad = (n: number) => String(n).padStart(2, "0");
    const mondayStr = `${pad(monday.getDate())}/${pad(monday.getMonth() + 1)}`;
    const sundayStr = `${pad(sunday.getDate())}/${pad(sunday.getMonth() + 1)}/${sunday.getFullYear()}`;
    const weekNum = 24;
    return { title: `Quản lý phân bổ nhân sự cho tuần ${weekNum} (${mondayStr} - ${sundayStr})` };
  }, [weekDates]);

  const roomRows = useMemo((): RoomRow[] => {
    const colors = staffType === "dentist" ? ROOM_COLORS_DENTIST : ROOM_COLORS_STAFF;
    if (allRooms.length > 0) {
      const filtered = allRooms.filter(r => staffType === "dentist" ? !isStaffRoomType(r) : isStaffRoomType(r));
      if (filtered.length > 0) {
        return filtered.map((room, idx) => ({
          label: room.name.toUpperCase(), key: room.name, color: colors[idx % colors.length],
          isDisabled: DISABLED_ROOM_STATUSES.includes(room.status),
          disabledReason: DISABLED_ROOM_STATUSES.includes(room.status) ? room.status : null,
        }));
      }
    }
    if (staffType === "dentist") {
      return [
        { label: "PHÒNG 1", key: "PHÒNG 1", color: "border-primary",   isDisabled: false, disabledReason: null },
        { label: "PHÒNG 2", key: "PHÒNG 2", color: "border-secondary", isDisabled: false, disabledReason: null },
      ];
    }
    return [
      { label: "LỄ TÂN", key: "LỄ TÂN", color: "border-green-600", isDisabled: false, disabledReason: null },
      { label: "CSKH",   key: "CSKH",   color: "border-teal-600",   isDisabled: false, disabledReason: null },
    ];
  }, [allRooms, staffType]);

  const editLookup = useMemo(() => {
    const map: Record<string, ScheduleEntry> = {};
    activeWeekSchedules.forEach(item => {
      if (item.role !== "staff") {
        const key = `${item.date}_${item.shift}_${item.room}_${item.role}`;
        map[key] = item;
      }
    });
    return map;
  }, [activeWeekSchedules]);

  const staffMultiLookup = useMemo(() => {
    const map: Record<string, ScheduleEntry[]> = {};
    activeWeekSchedules.forEach(item => {
      if (item.role === "staff" && !item.isHoliday) {
        const key = `${item.date}_${item.shift}_${item.room}`;
        if (!map[key]) map[key] = [];
        map[key].push(item);
      }
    });
    return map;
  }, [activeWeekSchedules]);

  const isToday = (date: Date) => {
    const today = new Date();
    return date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate();
  };

  const isDayClosed = (dateStr: string) =>
    activeWeekSchedules.some(item => item.date === dateStr && item.isHoliday);

  const handleOpenAssignModal = (
    e: React.MouseEvent, dateStr: string, shift: "morning" | "afternoon",
    room: string, roomColor: string, role: "dentist" | "assistant" | "staff"
  ) => {
    e.stopPropagation();
    setModalSearchQuery("");
    setModalCell({ date: dateStr, shift, room, roomColor, role });
  };

  const handleToggleClosure = (dateStr: string) => {
    if (isDayClosed(dateStr)) {
      setActiveWeekSchedules(prev => prev.filter(item => !(item.date === dateStr && item.isHoliday)));
      showToast(`Đã mở cửa hoạt động lại vào ngày ${dateStr}`, "success");
    } else {
      const clearedList = activeWeekSchedules.filter(item => item.date !== dateStr);
      const newHoliday: ScheduleEntry = {
        id: `HOLID-${dateStr}-${Date.now()}`, date: dateStr, shift: "morning",
        type: "dentist", role: "dentist", name: "Nha khoa đóng cửa", room: "", roomColor: "",
        isHoliday: true, isDraft: true,
      };
      setActiveWeekSchedules([...clearedList, newHoliday]);
      showToast(`Đã đặt ngày ${dateStr} là ngày nghỉ phòng khám`, "info");
    }
    setDayActionDate(null);
  };

  const handleClearDayShifts = (dateStr: string) => {
    setActiveWeekSchedules(prev => prev.filter(item => item.date !== dateStr));
    showToast(`Đã xóa toàn bộ ca trực trong ngày ${dateStr} (Dạng nháp)!`, "info");
    setDayActionDate(null);
  };

  const handleSelectStaff = (staff: StaffMember) => {
    if (!modalCell) return;
    const { date, shift, room, roomColor, role } = modalCell;
    if (role === "staff") {
      const newEntry: ScheduleEntry = {
        id: `DRAFT-${Date.now()}-${Math.random().toString(36).substr(2, 5)}`,
        date, shift, type: "staff", role: "staff", name: staff.name, room, roomColor, isDraft: true,
      };
      setActiveWeekSchedules(prev => [...prev, newEntry]);
    } else {
      const lookupKey = `${date}_${shift}_${room}_${role}`;
      const existing = editLookup[lookupKey];
      const updatedEntry: ScheduleEntry = {
        id: existing ? existing.id : `DRAFT-${Date.now()}-${Math.random().toString(36).substr(2, 5)}`,
        date, shift, type: staffType, role, name: staff.name, room, roomColor, isDraft: true,
      };
      if (existing) {
        setActiveWeekSchedules(prev => prev.map(item => item.id === existing.id ? updatedEntry : item));
      } else {
        setActiveWeekSchedules(prev => [...prev, updatedEntry]);
      }
    }
    setModalCell(null);
    setModalSearchQuery("");
  };

  const handleRemoveAssignment = (e: React.MouseEvent, entryId: string) => {
    e.stopPropagation();
    setActiveWeekSchedules(prev => prev.filter(item => item.id !== entryId));
  };

  const handleCancelEditing = () => {
    showToast("Đã hủy bỏ toàn bộ chỉnh sửa!", "info");
    setTimeout(() => router.push(`/owner/schedule?week=${weekParam}`), 1000);
  };

  const findStaffByName = (name: string, targetType: "dentist" | "assistant" | "staff"): string => {
    const cleanInput = name.trim().toLowerCase();
    if (!cleanInput || cleanInput === "-" || cleanInput === "off") return "";
    const stripPrefix = (n: string) => n.replace(/^(bs\.|pt\.)\s*/i, "").trim().toLowerCase();
    const cleanInputNoPrefix = stripPrefix(cleanInput);
    const candidates = staffDatabase.filter(s => s.type === targetType);
    let match = candidates.find(s => stripPrefix(s.name) === cleanInputNoPrefix);
    if (match) return match.name;
    match = candidates.find(s => s.name.toLowerCase() === cleanInput);
    if (match) return match.name;
    match = candidates.find(s => {
      const c = stripPrefix(s.name);
      return c.includes(cleanInputNoPrefix) || cleanInputNoPrefix.includes(c);
    });
    return match ? match.name : name.trim();
  };

  const handleImportExcel = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (evt) => {
      try {
        const buffer = evt.target?.result;
        if (!buffer) return;
        const workbook = XLSX.read(buffer, { type: "array" });
        const worksheet = workbook.Sheets[workbook.SheetNames[0]];
        const rows = XLSX.utils.sheet_to_json<any[]>(worksheet, { header: 1 });
        if (rows.length < 5) { showToast("File Excel không đúng cấu hình bản phân bổ.", "error"); return; }

        const activeDates = weekDates.map(d => formatDateKey(d));
        const parsedEntries: ScheduleEntry[] = [];
        const holidayDates = new Set<string>();
        let currentShift: "morning" | "afternoon" | null = null;

        for (let r = 0; r < rows.length; r++) {
          const rowData = rows[r];
          if (!rowData) continue;
          const colA = String(rowData[0] || "").trim().toUpperCase();
          if (colA.includes("SÁNG") || colA.includes("08:00")) currentShift = "morning";
          else if (colA.includes("CHIỀU") || colA.includes("13:30")) currentShift = "afternoon";
          if (!currentShift) continue;

          const positionLabel = String(rowData[1] || "").trim();
          if (!positionLabel) continue;

          let type: "dentist" | "staff" = "dentist";
          let role: "dentist" | "assistant" | "staff" = "dentist";
          let room = "";
          let roomColor = "border-primary";
          let matched = false;
          const label = positionLabel.toLowerCase();

          if (label.includes("phòng 1") && label.includes("bác sĩ")) { type = "dentist"; role = "dentist"; room = "PHÒNG 1"; roomColor = "border-primary"; matched = true; }
          else if (label.includes("phòng 1") && label.includes("phụ tá")) { type = "dentist"; role = "assistant"; room = "PHÒNG 1"; roomColor = "border-primary"; matched = true; }
          else if (label.includes("phòng 2") && label.includes("bác sĩ")) { type = "dentist"; role = "dentist"; room = "PHÒNG 2"; roomColor = "border-secondary"; matched = true; }
          else if (label.includes("phòng 2") && label.includes("phụ tá")) { type = "dentist"; role = "assistant"; room = "PHÒNG 2"; roomColor = "border-secondary"; matched = true; }
          else if (label.includes("lễ tân")) { type = "staff"; role = "staff"; room = "LỄ TÂN"; roomColor = "border-green-600"; matched = true; }
          else if (label.includes("cskh") || label.includes("chăm sóc")) { type = "staff"; role = "staff"; room = "CSKH"; roomColor = "border-teal-600"; matched = true; }
          else if (label.includes("kế toán")) { type = "staff"; role = "staff"; room = "KẾ TOÁN"; roomColor = "border-indigo-600"; matched = true; }
          if (!matched) continue;

          for (let dayIdx = 0; dayIdx < 7; dayIdx++) {
            const cellVal = String(rowData[2 + dayIdx] || "").trim();
            if (!cellVal || cellVal === "-") continue;
            const dateStr = activeDates[dayIdx];
            if (["ĐÓNG CỬA", "OFF", "NGHỈ LỄ", "CLOSED"].includes(cellVal.toUpperCase()) || cellVal.toLowerCase().includes("đóng cửa")) {
              holidayDates.add(dateStr); continue;
            }
            parsedEntries.push({
              id: `IMPORT-${Date.now()}-${r}-${dayIdx}-${Math.random().toString(36).substr(2, 5)}`,
              date: dateStr, shift: currentShift, type, role,
              name: findStaffByName(cellVal, type), room, roomColor, isDraft: true,
            });
          }
        }

        const importedEntries: ScheduleEntry[] = [];
        holidayDates.forEach(dateStr => importedEntries.push({
          id: `IMPORT-HOLID-${dateStr}-${Date.now()}`, date: dateStr, shift: "morning",
          type: "dentist", role: "dentist", name: "Nha khoa đóng cửa", room: "", roomColor: "",
          isHoliday: true, isDraft: true,
        }));
        parsedEntries.forEach(entry => { if (!holidayDates.has(entry.date)) importedEntries.push(entry); });

        if (importedEntries.length === 0) { showToast("Không tìm thấy ca làm hợp lệ để nhập.", "info"); return; }
        setActiveWeekSchedules(importedEntries);
        showToast(`Nhập thành công! Đã nạp ${importedEntries.length} ca làm từ Excel.`, "success");
      } catch { showToast("Đọc file Excel thất bại. Vui lòng kiểm tra lại cấu trúc file.", "error"); }
    };
    reader.readAsArrayBuffer(file);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handleSaveChanges = async () => {
    for (let i = 0; i < activeWeekSchedules.length; i++) {
      const current = activeWeekSchedules[i];
      if (current.isHoliday) continue;
      const duplicate = activeWeekSchedules.find((item, idx) =>
        idx !== i && item.date === current.date && item.shift === current.shift &&
        item.name === current.name && (item.room !== current.room || item.role !== current.role)
      );
      if (duplicate) {
        showToast(`${current.name} đã được phân công tại ${duplicate.room} trong ca này. Vui lòng kiểm tra lại!`, "error");
        return;
      }
    }
    setIsSaving(true);
    try {
      const entries = activeWeekSchedules.map(item => ({
        date: item.date, shift: item.shift, type: item.type, role: item.role,
        name: item.name, room: item.room, roomColor: item.roomColor, isHoliday: item.isHoliday ?? false,
      }));
      await saveWeekScheduleApi(weekParam, entries);
      showToast("Cập nhật lịch làm việc thành công!", "success");
      setTimeout(() => router.push(`/owner/schedule?week=${weekParam}`), 1200);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Lưu lịch thất bại", "error");
    } finally {
      setIsSaving(false);
    }
  };

  const eligibleStaffList = useMemo(() => {
    if (!modalCell) return [];
    const { date, shift, room, role } = modalCell;
    let dbType: "dentist" | "assistant" | "staff" = "staff";
    let filterSpecialization = "";
    if (staffType === "dentist") {
      dbType = role === "dentist" ? "dentist" : "assistant";
    } else {
      dbType = "staff";
      if (room === "LỄ TÂN") filterSpecialization = "Lễ tân";
      if (room === "CSKH") filterSpecialization = "Chăm sóc khách hàng";
      if (room === "KẾ TOÁN") filterSpecialization = "Kế toán";
    }
    let activeStaff = staffDatabase.filter(s => {
      const isCorrectType = s.status === "ACTIVE" && s.type === dbType;
      const hasNoSpecificPosition = s.specialization === "Nhân viên";
      const matchesSpecialization = !filterSpecialization || hasNoSpecificPosition ||
        s.specialization.toLowerCase().includes(filterSpecialization.toLowerCase());
      return isCorrectType && matchesSpecialization;
    });
    if (modalSearchQuery.trim()) {
      const q = modalSearchQuery.toLowerCase();
      activeStaff = activeStaff.filter(s => s.name.toLowerCase().includes(q) || s.specialization.toLowerCase().includes(q));
    }
    return activeStaff.map(staff => {
      const busyElsewhere = activeWeekSchedules.find(item =>
        item.date === date && item.shift === shift && item.name === staff.name &&
        (item.room !== room || item.role !== role)
      );
      const alreadyInSlot = role === "staff" && activeWeekSchedules.some(item =>
        item.date === date && item.shift === shift && item.room === room && item.role === "staff" && item.name === staff.name
      );
      const isBusy = !!busyElsewhere || alreadyInSlot;
      return {
        ...staff, isBusy,
        busyRoom: alreadyInSlot ? room : (busyElsewhere ? busyElsewhere.room : null),
        busyRole: alreadyInSlot ? "Đã trong ca này" : (busyElsewhere ? (busyElsewhere.role === "dentist" ? "Bác sĩ" : busyElsewhere.role === "assistant" ? "Phụ tá" : "Nhân viên") : null),
      };
    });
  }, [modalCell, activeWeekSchedules, staffType, modalSearchQuery, staffDatabase]);

  const renderDayCell = (dateStr: string, shift: "morning" | "afternoon", date: Date) => {
    const isTodayDate = isToday(date);
    const isClosed = isDayClosed(dateStr);

    if (isClosed) {
      return (
        <td onClick={() => setDayActionDate(dateStr)}
          className="px-3.5 py-4 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-slate-100/50 cursor-pointer transition-all hover:bg-slate-200/35"
          title="Bấm để mở lại cửa phòng khám">
          <div className="flex flex-col items-center justify-center gap-1.5 text-[11.5px] font-black text-slate-400 tracking-wider">
            <span>ĐÓNG CỬA</span>
          </div>
        </td>
      );
    }

    return null;
  };

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="schedule" />

      <main className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-16 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex flex-col">
            <h1 className="text-[18px] font-black text-slate-900 leading-tight">Chỉnh sửa lịch làm việc</h1>
            <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">Quản lý ca trực và lịch làm việc nhân sự</p>
          </div>
          <div className="flex items-center gap-4">
            <NotificationBell href="/owner/notifications" />
          </div>
        </header>

        {toast && (
          <div className={`fixed top-24 right-8 z-50 px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border animate-fade-in font-bold text-[14.5px] transition-all max-w-md ${
            toast.type === "success" ? "bg-emerald-900 text-white border-emerald-800"
            : toast.type === "error" ? "bg-red-900 text-white border-red-800"
            : "bg-slate-900 text-white border-slate-800"
          }`}>
            <span className="text-lg">{toast.type === "success" ? "✓" : toast.type === "error" ? "⚠" : "ℹ"}</span>
            <span>{toast.message}</span>
          </div>
        )}

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* Title & Actions */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-5 shrink-0 select-none">
            <div className="flex flex-col text-left">
              <div className="flex items-center gap-1.5 text-[12.5px] font-bold text-slate-400">
                <Link href="/owner/schedule" className="hover:text-primary transition-colors">Lịch làm việc</Link>
                <span>&gt;</span>
                <span className="text-primary">Chỉnh sửa lịch làm việc</span>
              </div>
              <h1 className="text-2xl font-black text-slate-900 mt-2 tracking-tight">Chỉnh sửa lịch làm việc</h1>
              <p className="text-[13.5px] font-bold text-slate-400 mt-1 leading-none">{formattedWeekRange.title}</p>
            </div>
            <div className="flex items-center gap-3">
              <input type="file" ref={fileInputRef} onChange={handleImportExcel} accept=".xlsx, .xls" className="hidden" />
              <button onClick={() => fileInputRef.current?.click()}
                className="px-4.5 py-3 bg-white border border-slate-350 hover:bg-slate-50 text-slate-655 text-[14px] font-extrabold rounded-xl transition-all shadow-sm cursor-pointer flex items-center gap-1.5 animate-pulse">
                Nhập từ Excel
              </button>
              <button onClick={() => setConfirmClearAllOpen(true)}
                className="px-4.5 py-3 bg-white border border-red-200 hover:bg-red-50 text-red-650 text-[14px] font-extrabold rounded-xl transition-all shadow-sm cursor-pointer flex items-center gap-1.5">
                Xóa tất cả các ca
              </button>
              <button onClick={handleCancelEditing}
                className="px-6 py-3 border border-slate-300 hover:bg-slate-50 text-slate-655 text-[14px] font-extrabold rounded-xl transition-all shadow-sm cursor-pointer">
                Hủy
              </button>
              <button onClick={handleSaveChanges} disabled={isSaving}
                className="px-6 py-3 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl transition-all shadow-md shadow-primary/20 cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed">
                {isSaving ? "Đang lưu..." : "Lưu thay đổi"}
              </button>
            </div>
          </div>

          {/* Dentist/Staff selector */}
          <div className="flex items-center justify-between gap-4">
            <div className="flex bg-[#eef2f6] p-1 rounded-xl border border-slate-200/20 shadow-sm select-none">
              <button onClick={() => setStaffType("dentist")}
                className={`w-28 py-2.5 rounded-lg text-[13.5px] font-black transition-all cursor-pointer flex flex-col items-center justify-center leading-none ${staffType === "dentist" ? "bg-white text-primary shadow-sm border border-slate-200/20" : "text-slate-500 hover:text-slate-800"}`}>
                <span>Nha sĩ</span>
                <span className="text-[10px] font-semibold text-slate-400 mt-0.5">(Dentists)</span>
              </button>
              <button onClick={() => setStaffType("staff")}
                className={`w-28 py-2.5 rounded-lg text-[13.5px] font-black transition-all cursor-pointer flex flex-col items-center justify-center leading-none ${staffType === "staff" ? "bg-white text-primary shadow-sm border border-slate-200/20" : "text-slate-500 hover:text-slate-800"}`}>
                <span>Nhân viên</span>
                <span className="text-[10px] font-semibold text-slate-400 mt-0.5">(Staff)</span>
              </button>
            </div>
            <div className="relative w-64">
              <input type="text" placeholder="Tìm kiếm bác sĩ, phòng..." value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-4 pr-4 py-2 text-[13.5px] bg-slate-100/80 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none focus:ring-1 focus:ring-slate-200 transition-all font-semibold" />
            </div>
          </div>

          {/* Calendar Grid */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[1000px] table-fixed">
                <thead>
                  <tr className="bg-slate-50/60 font-bold border-b border-slate-200 select-none">
                    <th className="px-5 py-5 text-slate-500 font-extrabold text-[14px] w-[150px] text-center border-r border-slate-200/80">
                      <span className="text-[12px] text-slate-400 font-black">Phòng / Thứ</span>
                    </th>
                    {weekDates.map((date, idx) => {
                      const daysVN = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật"];
                      const pad = (n: number) => String(n).padStart(2, "0");
                      const dateStr = formatDateKey(date);
                      const formattedDate = `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
                      const isTodayDate = isToday(date);
                      const isClosed = isDayClosed(dateStr);
                      return (
                        <th key={idx} onClick={() => setDayActionDate(dateStr)}
                          className={`px-4 py-5 text-center border-r border-slate-200/80 last:border-r-0 cursor-pointer transition-all hover:bg-red-50/15 ${isTodayDate ? "bg-red-50/30" : ""} ${isClosed ? "bg-slate-100/40" : ""}`}>
                          <div className={`text-[13.5px] font-black tracking-tight ${isClosed ? "text-slate-400 line-through" : isTodayDate ? "text-primary font-black" : "text-slate-800"}`}>
                            {daysVN[idx]}
                          </div>
                          <div className="flex items-center justify-center gap-1.5 mt-1">
                            <span className={`text-[14px] font-black ${isClosed ? "text-slate-400" : isTodayDate ? "text-primary" : "text-slate-855"}`}>
                              {formattedDate}
                            </span>
                            {isClosed && <span className="text-[10px] text-red-500 font-extrabold bg-red-50 px-1.5 py-0.5 rounded leading-none">CLOSED</span>}
                          </div>
                        </th>
                      );
                    })}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-250/70">

                  {/* CA SÁNG */}
                  <tr className="bg-slate-50/20">
                    <td colSpan={8} className="px-5 py-3 font-black text-[13px] tracking-wide text-primary uppercase border-b border-slate-200/60 bg-red-50/10">
                      Ca Sáng <span className="text-[11.5px] font-bold text-slate-400 lowercase italic ml-1">(08:00 - 12:00)</span>
                    </td>
                  </tr>

                  {roomRows.map((room) => (
                    <tr key={`morning_${room.key}`} className="min-h-[160px]">
                      <td className={`px-4 py-6 text-center border-r border-slate-200/80 font-black bg-slate-50/30 text-[12.5px] ${room.isDisabled ? "text-amber-600/70" : "text-slate-655"}`}>
                        <div className="flex flex-col items-center gap-1">
                          <span>{room.label}</span>
                          {room.isDisabled && <span className="text-[9px] font-black text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded uppercase tracking-wide leading-none">{room.disabledReason}</span>}
                        </div>
                      </td>
                      {weekDates.map((date, dayIdx) => {
                        const dateStr = formatDateKey(date);
                        const isTodayDate = isToday(date);
                        const isClosed = isDayClosed(dateStr);
                        const closedCell = renderDayCell(dateStr, "morning", date);
                        if (isClosed) return <React.Fragment key={dayIdx}>{closedCell}</React.Fragment>;
                        if (room.isDisabled) return (
                          <td key={dayIdx} className="px-3.5 py-4 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-amber-50/30">
                            <div className="flex flex-col items-center justify-center gap-1.5 text-[11.5px] font-black text-amber-600/70 tracking-wider">
                              <span className="text-base leading-none">⚙</span>
                              <span>{room.disabledReason?.toUpperCase()}</span>
                            </div>
                          </td>
                        );
                        const docKey = `${dateStr}_morning_${room.key}_dentist`;
                        const astKey = `${dateStr}_morning_${room.key}_assistant`;
                        const doctorEntry = editLookup[docKey];
                        const assistantEntry = editLookup[astKey];
                        const hasDoctor = !!doctorEntry;
                        const matchesDocSearch = !searchQuery || (doctorEntry && doctorEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));
                        const matchesAstSearch = !searchQuery || (assistantEntry && assistantEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));
                        return (
                          <td key={dayIdx} className={`px-3 py-4.5 align-top border-r border-slate-200/80 last:border-r-0 transition-colors ${isTodayDate ? "bg-red-50/10" : ""}`}>
                            <div className="flex flex-col gap-3 min-h-[120px] justify-start h-full">
                              {staffType === "dentist" ? (
                                <>
                                  {doctorEntry && matchesDocSearch ? (
                                    <div className={`relative group bg-white border-l-4 border-primary p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${doctorEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""}`}>
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "dentist")} className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">Sửa</button>
                                        <button onClick={(e) => handleRemoveAssignment(e, doctorEntry.id)} className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">✕</button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${doctorEntry.isDraft ? "text-primary" : "text-slate-800"}`}>{doctorEntry.name}</div>
                                        <div className="text-[10.5px] font-bold text-slate-400 mt-0.5 truncate">{staffDatabase.find(s => s.name === doctorEntry.name)?.specialization || "Bác sĩ"}</div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "dentist")} className="py-2.5 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer">
                                      <span>+ Bác sĩ</span>
                                    </button>
                                  )}
                                  {assistantEntry && matchesAstSearch ? (
                                    <div className={`relative group bg-slate-50 border-l-4 border-teal-500 p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${assistantEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""}`}>
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "assistant")} className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">Sửa</button>
                                        <button onClick={(e) => handleRemoveAssignment(e, assistantEntry.id)} className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">✕</button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${assistantEntry.isDraft ? "text-primary" : "text-slate-755"}`}>{assistantEntry.name}</div>
                                        <div className="text-[10.5px] font-bold text-slate-450 mt-0.5 truncate uppercase tracking-wider">Phụ Tá</div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button disabled={!hasDoctor} onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "assistant")}
                                      className={`py-2.5 px-3 border border-dashed rounded-xl transition-all bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 ${hasDoctor ? "border-slate-250 hover:border-teal-500 text-slate-400 hover:text-teal-650 cursor-pointer hover:bg-slate-50/80" : "border-slate-200 text-slate-300 opacity-60 cursor-not-allowed bg-slate-50/30"}`}>
                                      <span>+ Phụ tá {!hasDoctor && "(Khóa)"}</span>
                                    </button>
                                  )}
                                </>
                              ) : (
                                <div className="flex flex-col gap-2">
                                  {(staffMultiLookup[`${dateStr}_morning_${room.key}`] ?? [])
                                    .filter(e => !searchQuery || e.name.toLowerCase().includes(searchQuery.toLowerCase()))
                                    .map(entry => (
                                      <div key={entry.id} className={`relative bg-white border-l-4 ${room.color} px-2.5 py-2 rounded-xl shadow-sm border border-slate-200/70 flex items-center justify-between gap-2 ${entry.isDraft ? "border-red-400/60" : ""}`}>
                                        <div className="min-w-0">
                                          <div className={`text-[12.5px] font-black truncate ${entry.isDraft ? "text-primary" : "text-slate-800"}`}>{entry.name}</div>
                                          <div className="text-[10.5px] font-bold text-slate-400 truncate">{staffDatabase.find(s => s.name === entry.name)?.specialization || "Nhân viên"}</div>
                                        </div>
                                        <button onClick={(e) => handleRemoveAssignment(e, entry.id)} className="shrink-0 p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-400 hover:text-red-500 transition-all cursor-pointer text-[10px] leading-none">✕</button>
                                      </div>
                                    ))}
                                  <button onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "staff")} className="py-2 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer">
                                    <span>+ Thêm nhân viên</span>
                                  </button>
                                </div>
                              )}
                            </div>
                          </td>
                        );
                      })}
                    </tr>
                  ))}

                  {/* CA CHIỀU */}
                  <tr className="bg-slate-50/20">
                    <td colSpan={8} className="px-5 py-3 font-black text-[13px] tracking-wide text-primary uppercase border-b border-slate-200/60 bg-red-50/10">
                      Ca Chiều <span className="text-[11.5px] font-bold text-slate-400 lowercase italic ml-1">(13:30 - 17:30)</span>
                    </td>
                  </tr>

                  {roomRows.map((room) => (
                    <tr key={`afternoon_${room.key}`} className="min-h-[160px]">
                      <td className={`px-4 py-6 text-center border-r border-slate-200/80 font-black bg-slate-50/30 text-[12.5px] ${room.isDisabled ? "text-amber-600/70" : "text-slate-655"}`}>
                        <div className="flex flex-col items-center gap-1">
                          <span>{room.label}</span>
                          {room.isDisabled && <span className="text-[9px] font-black text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded uppercase tracking-wide leading-none">{room.disabledReason}</span>}
                        </div>
                      </td>
                      {weekDates.map((date, dayIdx) => {
                        const dateStr = formatDateKey(date);
                        const isTodayDate = isToday(date);
                        const isClosed = isDayClosed(dateStr);
                        const closedCell = renderDayCell(dateStr, "afternoon", date);
                        if (isClosed) return <React.Fragment key={dayIdx}>{closedCell}</React.Fragment>;
                        if (room.isDisabled) return (
                          <td key={dayIdx} className="px-3.5 py-4 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-amber-50/30">
                            <div className="flex flex-col items-center justify-center gap-1.5 text-[11.5px] font-black text-amber-600/70 tracking-wider">
                              <span className="text-base leading-none">⚙</span>
                              <span>{room.disabledReason?.toUpperCase()}</span>
                            </div>
                          </td>
                        );
                        const docKey = `${dateStr}_afternoon_${room.key}_dentist`;
                        const astKey = `${dateStr}_afternoon_${room.key}_assistant`;
                        const doctorEntry = editLookup[docKey];
                        const assistantEntry = editLookup[astKey];
                        const hasDoctor = !!doctorEntry;
                        const matchesDocSearch = !searchQuery || (doctorEntry && doctorEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));
                        const matchesAstSearch = !searchQuery || (assistantEntry && assistantEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));
                        return (
                          <td key={dayIdx} className={`px-3 py-4.5 align-top border-r border-slate-200/80 last:border-r-0 transition-colors ${isTodayDate ? "bg-red-50/10" : ""}`}>
                            <div className="flex flex-col gap-3 min-h-[120px] justify-start h-full">
                              {staffType === "dentist" ? (
                                <>
                                  {doctorEntry && matchesDocSearch ? (
                                    <div className={`relative group bg-white border-l-4 border-primary p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${doctorEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""}`}>
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "dentist")} className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">Sửa</button>
                                        <button onClick={(e) => handleRemoveAssignment(e, doctorEntry.id)} className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">✕</button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${doctorEntry.isDraft ? "text-primary" : "text-slate-800"}`}>{doctorEntry.name}</div>
                                        <div className="text-[10.5px] font-bold text-slate-400 mt-0.5 truncate">{staffDatabase.find(s => s.name === doctorEntry.name)?.specialization || "Bác sĩ"}</div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "dentist")} className="py-2.5 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer">
                                      <span>+ Bác sĩ</span>
                                    </button>
                                  )}
                                  {assistantEntry && matchesAstSearch ? (
                                    <div className={`relative group bg-slate-50 border-l-4 border-teal-500 p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${assistantEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""}`}>
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "assistant")} className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">Sửa</button>
                                        <button onClick={(e) => handleRemoveAssignment(e, assistantEntry.id)} className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none">✕</button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${assistantEntry.isDraft ? "text-primary" : "text-slate-755"}`}>{assistantEntry.name}</div>
                                        <div className="text-[10.5px] font-bold text-slate-450 mt-0.5 truncate uppercase tracking-wider">Phụ Tá</div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button disabled={!hasDoctor} onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "assistant")}
                                      className={`py-2.5 px-3 border border-dashed rounded-xl transition-all bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 ${hasDoctor ? "border-slate-250 hover:border-teal-500 text-slate-400 hover:text-teal-650 cursor-pointer hover:bg-slate-50/80" : "border-slate-200 text-slate-300 opacity-60 cursor-not-allowed bg-slate-50/30"}`}>
                                      <span>+ Phụ tá {!hasDoctor && "(Khóa)"}</span>
                                    </button>
                                  )}
                                </>
                              ) : (
                                <div className="flex flex-col gap-2">
                                  {(staffMultiLookup[`${dateStr}_afternoon_${room.key}`] ?? [])
                                    .filter(e => !searchQuery || e.name.toLowerCase().includes(searchQuery.toLowerCase()))
                                    .map(entry => (
                                      <div key={entry.id} className={`relative bg-white border-l-4 ${room.color} px-2.5 py-2 rounded-xl shadow-sm border border-slate-200/70 flex items-center justify-between gap-2 ${entry.isDraft ? "border-red-400/60" : ""}`}>
                                        <div className="min-w-0">
                                          <div className={`text-[12.5px] font-black truncate ${entry.isDraft ? "text-primary" : "text-slate-800"}`}>{entry.name}</div>
                                          <div className="text-[10.5px] font-bold text-slate-400 truncate">{staffDatabase.find(s => s.name === entry.name)?.specialization || "Nhân viên"}</div>
                                        </div>
                                        <button onClick={(e) => handleRemoveAssignment(e, entry.id)} className="shrink-0 p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-400 hover:text-red-500 transition-all cursor-pointer text-[10px] leading-none">✕</button>
                                      </div>
                                    ))}
                                  <button onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "staff")} className="py-2 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer">
                                    <span>+ Thêm nhân viên</span>
                                  </button>
                                </div>
                              )}
                            </div>
                          </td>
                        );
                      })}
                    </tr>
                  ))}

                </tbody>
              </table>
            </div>

            {/* Legend */}
            <div className="bg-slate-50/60 border-t border-slate-200 px-6 py-4 flex flex-wrap gap-x-8 gap-y-2.5 text-[12.5px] font-bold text-slate-500 select-none">
              <div className="flex items-center gap-2"><span className="w-2.5 h-2.5 rounded-full bg-primary animate-pulse"></span><span>Lịch đang chỉnh sửa (Draft)</span></div>
              <div className="flex items-center gap-2"><span className="w-2.5 h-2.5 rounded-full bg-slate-350"></span><span>Lịch đã xác nhận (Confirmed)</span></div>
              <div className="flex items-center gap-2"><span className="w-6 h-4 border-2 border-dashed border-slate-300 rounded bg-white"></span><span>Vị trí còn trống (Empty Slot)</span></div>
              <div className="flex items-center gap-2"><span className="text-[11.5px] font-black text-slate-450 uppercase leading-none">Mẹo:</span><span className="text-slate-500">Bấm vào tên Thứ ở đầu cột để đóng/mở cửa phòng khám cả ngày đó.</span></div>
            </div>
          </div>

        </div>
      </main>

      {/* Staff selection modal */}
      {modalCell && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col animate-scale-up">
            <div className="bg-slate-50 border-b border-slate-100 px-6 py-4.5 flex justify-between items-center">
              <div>
                <h3 className="font-black text-[16px] text-slate-900 tracking-tight">
                  Phân bổ {modalCell.role === "dentist" ? "Bác sĩ" : modalCell.role === "assistant" ? "Phụ tá" : "Nhân sự"}
                </h3>
                <p className="text-[12px] text-slate-455 font-bold mt-0.5">
                  {modalCell.room} | Ca {modalCell.shift === "morning" ? "Sáng" : "Chiều"} (Ngày {modalCell.date})
                </p>
              </div>
              <button onClick={() => setModalCell(null)} className="text-slate-400 hover:text-slate-700 font-extrabold text-lg p-1 hover:bg-slate-100 rounded cursor-pointer animate-pulse">✕</button>
            </div>
            <div className="p-4 border-b border-slate-100 bg-slate-50/50">
              <input type="text"
                placeholder={`Tìm tên hoặc chuyên môn ${modalCell.role === "dentist" ? "bác sĩ" : modalCell.role === "assistant" ? "phụ tá" : "nhân viên"}...`}
                value={modalSearchQuery} onChange={(e) => setModalSearchQuery(e.target.value)}
                className="w-full pl-4 pr-4 py-2 text-[13.5px] bg-white border border-slate-200 focus:border-primary focus:outline-none rounded-lg font-semibold shadow-sm"
                autoFocus />
            </div>
            <div className="max-h-[350px] overflow-y-auto divide-y divide-slate-100 p-2">
              {isLoadingStaff ? (
                <div className="p-8 text-center text-slate-400 font-bold text-[13.5px]">Đang tải danh sách nhân viên...</div>
              ) : eligibleStaffList.length > 0 ? (
                eligibleStaffList.map((staff) => (
                  <button key={staff.id} disabled={staff.isBusy} onClick={() => handleSelectStaff(staff)}
                    className={`w-full flex items-center justify-between p-3.5 text-left rounded-xl transition-all cursor-pointer ${staff.isBusy ? "bg-slate-50/50 opacity-55 cursor-not-allowed" : "hover:bg-red-50/45 hover:text-primary active:bg-red-50 text-slate-755 font-semibold"}`}>
                    <div>
                      <div className="font-black text-[13.5px]">{staff.name}</div>
                      <div className="text-[11.5px] font-bold text-slate-400 mt-0.5">{staff.specialization}</div>
                    </div>
                    {staff.isBusy ? (
                      <span className="text-[10px] font-black text-red-500 bg-red-50 px-2 py-0.5 rounded-full uppercase tracking-wider">Đã bận làm {staff.busyRole} tại {staff.busyRoom}</span>
                    ) : (
                      <span className="text-[10.5px] font-black text-emerald-600 bg-emerald-50 px-2.5 py-0.5 rounded-full uppercase tracking-wider">Sẵn sàng</span>
                    )}
                  </button>
                ))
              ) : (
                <div className="p-8 text-center text-slate-400 font-bold text-[13.5px]">Không tìm thấy nhân viên phù hợp</div>
              )}
            </div>
            <div className="bg-slate-50/50 border-t border-slate-100 px-6 py-4 flex justify-end">
              <button onClick={() => setModalCell(null)} className="px-4.5 py-2.5 border border-slate-200 hover:bg-slate-100 text-slate-655 text-[13px] font-black rounded-lg transition-all cursor-pointer">Hủy bỏ</button>
            </div>
          </div>
        </div>
      )}

      {/* Day actions modal */}
      {dayActionDate && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col p-6 animate-scale-up">
            <h3 className="font-black text-lg text-slate-900">Tùy chọn lịch ngày {dayActionDate}</h3>
            <p className="text-[13.5px] text-slate-500 mt-3 font-semibold leading-relaxed">
              Vui lòng chọn hành động bạn muốn thực hiện đối với lịch làm việc của ngày <strong>{dayActionDate}</strong>:
            </p>
            <div className="flex flex-col gap-3 mt-6">
              {isDayClosed(dayActionDate) ? (
                <button onClick={() => handleToggleClosure(dayActionDate)} className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-extrabold rounded-xl transition-all cursor-pointer shadow-md shadow-emerald-600/10 flex items-center justify-center gap-1.5">
                  Mở cửa phòng khám
                </button>
              ) : (
                <>
                  <button onClick={() => handleToggleClosure(dayActionDate)} className="w-full py-3 bg-slate-800 hover:bg-slate-900 text-white font-extrabold rounded-xl transition-all cursor-pointer shadow-md flex items-center justify-center gap-1.5">
                    Đóng cửa phòng khám (Nghỉ cả ngày)
                  </button>
                  <button onClick={() => handleClearDayShifts(dayActionDate)} className="w-full py-3 bg-red-50 hover:bg-red-100 text-red-650 border border-red-200 font-extrabold rounded-xl transition-all cursor-pointer flex items-center justify-center gap-1.5">
                    Xóa sạch ca làm việc trong ngày
                  </button>
                </>
              )}
              <button onClick={() => setDayActionDate(null)} className="w-full py-3 bg-white hover:bg-slate-50 border border-slate-200 text-slate-500 font-extrabold rounded-xl transition-all cursor-pointer flex items-center justify-center">
                Hủy bỏ
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Clear all confirmation */}
      {confirmClearAllOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col p-6 animate-scale-up">
            <h3 className="font-black text-lg text-slate-900">Xóa tất cả các ca làm việc</h3>
            <p className="text-[13.5px] text-slate-500 mt-3 font-semibold leading-relaxed">
              Bạn có chắc chắn muốn xóa toàn bộ phân bổ lịch trực đã xếp cho tuần này không? Hãy nhấn <strong>"Lưu thay đổi"</strong> để chính thức áp dụng.
            </p>
            <div className="flex justify-end gap-3 mt-6">
              <button onClick={() => setConfirmClearAllOpen(false)} className="px-4 py-2 border border-slate-250 hover:bg-slate-100 text-slate-655 text-[13px] font-black rounded-lg transition-all cursor-pointer">Hủy bỏ</button>
              <button onClick={() => { setActiveWeekSchedules([]); setConfirmClearAllOpen(false); showToast("Đã xóa sạch các ca trong tuần (Dạng nháp)!", "info"); }} className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-[13px] font-black rounded-lg transition-all cursor-pointer">
                Xác nhận xóa
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}

export default function OwnerEditSchedulePage() {
  useRequireOwner();
  return (
    <Suspense fallback={<div className="p-8 text-center font-bold text-slate-550">Đang tải cấu hình lịch làm việc...</div>}>
      <EditScheduleContent />
    </Suspense>
  );
}
