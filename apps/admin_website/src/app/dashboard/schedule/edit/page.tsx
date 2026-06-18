"use client";

import React, { useState, useEffect, useMemo, useRef, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import { getWeekScheduleApi, saveWeekScheduleApi, getStaffApi, getRoomsApi, type RoomDto } from "../../../../lib/apiClient";
import * as XLSX from "xlsx";

// Define TypeScript interfaces for our scheduling data
interface ScheduleEntry {
  id: string;
  date: string; // format YYYY-MM-DD
  shift: "morning" | "afternoon";
  type: "dentist" | "staff";
  role: "dentist" | "assistant" | "staff"; // dentist = Bác sĩ, assistant = Phụ tá, staff = Nhân viên hành chính
  name: string;
  room: string; // "PHÒNG 1" | "PHÒNG 2" for dentist, or "LỄ TÂN" | "CSKH" | "KẾ TOÁN" for staff
  roomColor: string;
  isHoliday?: boolean;
  isDraft?: boolean; // temporary flag for modifying slots
}

interface StaffMember {
  id: string;
  name: string;
  specialization: string; // Specialty or Role
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


// Generates dates for a week starting on a given Monday date
const getWeekDates = (mondayDate: Date): Date[] => {
  const dates: Date[] = [];
  for (let i = 0; i < 7; i++) {
    const tempDate = new Date(mondayDate);
    tempDate.setDate(mondayDate.getDate() + i);
    dates.push(tempDate);
  }
  return dates;
};

// Helper to format Date objects as YYYY-MM-DD
const formatDateKey = (date: Date): string => {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, "0");
  const dd = String(date.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
};

// Default Monday fallback
const DEFAULT_MONDAY_STR = "2023-10-16";

function EditScheduleContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Parse target week from URL query
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

  // Copy of schedules for active week that we are currently editing
  const [activeWeekSchedules, setActiveWeekSchedules] = useState<ScheduleEntry[]>([]);

  // Toast notifications state
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" | "info" } | null>(null);

  // Modal selector states
  const [modalCell, setModalCell] = useState<{
    date: string;
    shift: "morning" | "afternoon";
    room: string;
    roomColor: string;
    role: "dentist" | "assistant" | "staff";
  } | null>(null);

  const [modalSearchQuery, setModalSearchQuery] = useState("");

  // Confirm Day Action date (replacing confirmClosureDate)
  const [dayActionDate, setDayActionDate] = useState<string | null>(null);

  // Confirm Clear All modal state
  const [confirmClearAllOpen, setConfirmClearAllOpen] = useState(false);

  const showToast = (message: string, type: "success" | "error" | "info" = "success") => {
    setToast({ message, type });
    setTimeout(() => {
      setToast(null);
    }, 4000);
  };

  const weekDates = useMemo(() => getWeekDates(currentMonday), [currentMonday]);

  // Load active staff list from backend
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
          return {
            id: dto.id,
            name: dto.fullName ?? dto.username,
            specialization,
            type,
            status: "ACTIVE" as const,
          };
        });
        setStaffDatabase(members);
      })
      .catch(() => setStaffDatabase([]))
      .finally(() => setIsLoadingStaff(false));
  }, []);

  // Load rooms from backend
  useEffect(() => {
    getRoomsApi()
      .then(rooms => setAllRooms(rooms))
      .catch(() => setAllRooms([]));
  }, []);

  // Load schedules for the active week from API
  useEffect(() => {
    getWeekScheduleApi(weekParam)
      .then(dtos => setActiveWeekSchedules(dtos.map(dto => ({
        id: dto.id,
        date: dto.date,
        shift: dto.shift,
        type: dto.type,
        role: dto.role,
        name: dto.name,
        room: dto.room,
        roomColor: dto.roomColor,
        isHoliday: dto.isHoliday,
      }))))
      .catch(() => setActiveWeekSchedules([]));
  }, [weekParam]);

  // Determine week range formatting
  const formattedWeekRange = useMemo(() => {
    const monday = weekDates[0];
    const sunday = weekDates[6];

    const pad = (n: number) => String(n).padStart(2, "0");
    const mondayStr = `${pad(monday.getDate())}/${pad(monday.getMonth() + 1)}`;
    const sundayStr = `${pad(sunday.getDate())}/${pad(sunday.getMonth() + 1)}/${sunday.getFullYear()}`;

    let weekNum = 24;
    if (monday.getFullYear() === 2023) {
      weekNum = 42;
    }

    return {
      title: `Quản lý phân bổ nhân sự cho tuần ${weekNum} (${mondayStr} - ${sundayStr})`,
      dates: {
        monday: `${pad(monday.getDate())}/${pad(monday.getMonth() + 1)}`,
        sunday: `${pad(sunday.getDate())}/${pad(sunday.getMonth() + 1)}`
      }
    };
  }, [weekDates]);

  // Dynamic rooms fetched from backend, split by type and annotated with disabled status
  const roomRows = useMemo((): RoomRow[] => {
    const colors = staffType === "dentist" ? ROOM_COLORS_DENTIST : ROOM_COLORS_STAFF;

    if (allRooms.length > 0) {
      const filtered = allRooms.filter(r =>
        staffType === "dentist" ? !isStaffRoomType(r) : isStaffRoomType(r)
      );
      if (filtered.length > 0) {
        return filtered.map((room, idx) => ({
          label: room.name.toUpperCase(),
          key: room.name,
          color: colors[idx % colors.length],
          isDisabled: DISABLED_ROOM_STATUSES.includes(room.status),
          disabledReason: DISABLED_ROOM_STATUSES.includes(room.status) ? room.status : null,
        }));
      }
    }

    // Fallback to hardcoded while rooms are loading or none found
    if (staffType === "dentist") {
      return [
        { label: "PHÒNG 1", key: "PHÒNG 1", color: "border-primary",    isDisabled: false, disabledReason: null },
        { label: "PHÒNG 2", key: "PHÒNG 2", color: "border-secondary",  isDisabled: false, disabledReason: null },
      ];
    }
    return [
      { label: "LỄ TÂN", key: "LỄ TÂN", color: "border-green-600", isDisabled: false, disabledReason: null },
      { label: "CSKH",   key: "CSKH",   color: "border-teal-600",   isDisabled: false, disabledReason: null },
    ];
  }, [allRooms, staffType]);

  // Map dentist/assistant entries by Room+Date+Shift+Role for single-slot lookup
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

  // Group staff entries by Date+Shift+Room — supports multiple people per slot
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

  // Checks if date is today
  const isWednesday = (date: Date) => {
    const today = new Date();
    return date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate();
  };

  // Check if a day has been set as holiday closed
  const isDayClosed = (dateStr: string) => {
    return activeWeekSchedules.some(item => item.date === dateStr && item.isHoliday);
  };

  // AF-1 & AF-3: Open Modal to select Staff member
  const handleOpenAssignModal = (
    e: React.MouseEvent,
    dateStr: string,
    shift: "morning" | "afternoon",
    room: string,
    roomColor: string,
    role: "dentist" | "assistant" | "staff"
  ) => {
    e.stopPropagation();
    setModalSearchQuery(""); // Clear search filter when opening modal
    setModalCell({ date: dateStr, shift, room, roomColor, role });
  };

  // Toggle clinic closure for a given day
  const handleToggleClosure = (dateStr: string) => {
    const alreadyClosed = isDayClosed(dateStr);

    if (alreadyClosed) {
      // Re-open day by removing the holiday marker
      setActiveWeekSchedules(prev => prev.filter(item => !(item.date === dateStr && item.isHoliday)));
      showToast(`Đã mở cửa hoạt động lại vào ngày ${dateStr}`, "success");
    } else {
      // Close day by clearing schedules for this day and inserting a holiday entry
      const clearedList = activeWeekSchedules.filter(item => item.date !== dateStr);
      const newHoliday: ScheduleEntry = {
        id: `HOLID-${dateStr}-${Date.now()}`,
        date: dateStr,
        shift: "morning", // arbitrary shift
        type: "dentist", // arbitrary type
        role: "dentist", // arbitrary role
        name: "Nha khoa đóng cửa",
        room: "",
        roomColor: "",
        isHoliday: true,
        isDraft: true
      };
      setActiveWeekSchedules([...clearedList, newHoliday]);
      showToast(`Đã đặt ngày ${dateStr} là ngày nghỉ phòng khám`, "info");
    }

    setDayActionDate(null);
  };

  // Clear all shifts for a specific day
  const handleClearDayShifts = (dateStr: string) => {
    setActiveWeekSchedules(prev => prev.filter(item => item.date !== dateStr));
    showToast(`Đã xóa toàn bộ ca trực trong ngày ${dateStr} (Dạng nháp)!`, "info");
    setDayActionDate(null);
  };

  // Save selection back to draft schedules
  const handleSelectStaff = (staff: StaffMember) => {
    if (!modalCell) return;

    const { date, shift, room, roomColor, role } = modalCell;

    if (role === "staff") {
      // Staff slots support multiple people — always add a new entry
      const newEntry: ScheduleEntry = {
        id: `DRAFT-${Date.now()}-${Math.random().toString(36).substr(2, 5)}`,
        date, shift,
        type: "staff",
        role: "staff",
        name: staff.name,
        room, roomColor,
        isDraft: true,
      };
      setActiveWeekSchedules(prev => [...prev, newEntry]);
    } else {
      // Dentist/assistant slots: replace existing entry for that slot
      const lookupKey = `${date}_${shift}_${room}_${role}`;
      const existing = editLookup[lookupKey];
      const updatedEntry: ScheduleEntry = {
        id: existing ? existing.id : `DRAFT-${Date.now()}-${Math.random().toString(36).substr(2, 5)}`,
        date, shift,
        type: staffType,
        role,
        name: staff.name,
        room, roomColor,
        isDraft: true,
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

  // AF-2: Remove staff assignment
  const handleRemoveAssignment = (e: React.MouseEvent, entryId: string) => {
    e.stopPropagation();
    setActiveWeekSchedules(prev => prev.filter(item => item.id !== entryId));
  };

  // AF-4: Cancel schedule editing
  const handleCancelEditing = () => {
    showToast("Đã hủy bỏ toàn bộ chỉnh sửa!", "info");
    setTimeout(() => {
      router.push(`/dashboard/schedule?week=${weekParam}`);
    }, 1000);
  };

  // Loose matching for staff names against database
  const findStaffByName = (name: string, targetType: "dentist" | "assistant" | "staff"): string => {
    const cleanInput = name.trim().toLowerCase();
    if (!cleanInput || cleanInput === "-" || cleanInput === "off") return "";

    // Strip common prefixes like bs., bs, pt., pt
    const stripPrefix = (n: string) => n.replace(/^(bs\.|pt\.)\s*/i, "").trim().toLowerCase();
    const cleanInputNoPrefix = stripPrefix(cleanInput);

    const candidates = staffDatabase.filter(s => s.type === targetType);

    // 1. Exact match with prefix stripped
    let match = candidates.find(s => stripPrefix(s.name) === cleanInputNoPrefix);
    if (match) return match.name;

    // 2. Exact match on raw name
    match = candidates.find(s => s.name.toLowerCase() === cleanInput);
    if (match) return match.name;

    // 3. Substring contains match
    match = candidates.find(s => {
      const candidateClean = stripPrefix(s.name);
      return candidateClean.includes(cleanInputNoPrefix) || cleanInputNoPrefix.includes(candidateClean);
    });
    if (match) return match.name;

    // Fallback to original value if no match is found in DB
    return name.trim();
  };

  // Excel parsing handler
  const handleImportExcel = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (evt) => {
      try {
        const buffer = evt.target?.result;
        if (!buffer) return;

        const workbook = XLSX.read(buffer, { type: "array" });
        const sheetName = workbook.SheetNames[0];
        const worksheet = workbook.Sheets[sheetName];

        // Convert worksheet rows to array of arrays
        const rows = XLSX.utils.sheet_to_json<any[]>(worksheet, { header: 1 });
        if (rows.length < 5) {
          showToast("File Excel không đúng cấu hình bản phân bổ.", "error");
          return;
        }

        const activeDates = weekDates.map(d => formatDateKey(d));
        const parsedStaffEntries: ScheduleEntry[] = [];
        const holidayDates = new Set<string>();

        let currentShift: "morning" | "afternoon" | null = null;

        for (let r = 0; r < rows.length; r++) {
          const rowData = rows[r];
          if (!rowData) continue;

          // Check Column A (index 0) to dynamically update current shift
          const colAVal = String(rowData[0] || "").trim().toUpperCase();
          if (colAVal.includes("SÁNG") || colAVal.includes("08:00") || colAVal.includes("MORNING")) {
            currentShift = "morning";
          } else if (colAVal.includes("CHIỀU") || colAVal.includes("13:30") || colAVal.includes("AFTERNOON")) {
            currentShift = "afternoon";
          }

          if (!currentShift) continue;

          // Check Column B (index 1) for room/role
          const positionLabel = String(rowData[1] || "").trim();
          if (!positionLabel) continue;

          let type: "dentist" | "staff" = "dentist";
          let role: "dentist" | "assistant" | "staff" = "dentist";
          let room = "";
          let roomColor = "border-primary";
          let labelMatched = false;

          const label = positionLabel.toLowerCase();

          if (label.includes("phòng 1") && (label.includes("bác sĩ") || label.includes("bác sỹ"))) {
            type = "dentist";
            role = "dentist";
            room = "PHÒNG 1";
            roomColor = "border-primary";
            labelMatched = true;
          } else if (label.includes("phòng 1") && label.includes("phụ tá")) {
            type = "dentist";
            role = "assistant";
            room = "PHÒNG 1";
            roomColor = "border-primary";
            labelMatched = true;
          } else if (label.includes("phòng 2") && (label.includes("bác sĩ") || label.includes("bác sỹ"))) {
            type = "dentist";
            role = "dentist";
            room = "PHÒNG 2";
            roomColor = "border-secondary";
            labelMatched = true;
          } else if (label.includes("phòng 2") && label.includes("phụ tá")) {
            type = "dentist";
            role = "assistant";
            room = "PHÒNG 2";
            roomColor = "border-secondary";
            labelMatched = true;
          } else if (label.includes("lễ tân")) {
            type = "staff";
            role = "staff";
            room = "LỄ TÂN";
            roomColor = "border-green-600";
            labelMatched = true;
          } else if (label.includes("chăm sóc khách hàng") || label.includes("cskh")) {
            type = "staff";
            role = "staff";
            room = "CSKH";
            roomColor = "border-teal-600";
            labelMatched = true;
          } else if (label.includes("kế toán")) {
            type = "staff";
            role = "staff";
            room = "KẾ TOÁN";
            roomColor = "border-indigo-600";
            labelMatched = true;
          }

          if (!labelMatched) continue;

          // Parse columns Monday (C/2) to Sunday (I/8)
          for (let dayIdx = 0; dayIdx < 7; dayIdx++) {
            const cellVal = String(rowData[2 + dayIdx] || "").trim();
            if (!cellVal || cellVal === "-") continue;

            const dateStr = activeDates[dayIdx];

            // Check day closure keywords
            if (["ĐÓNG CỬA", "OFF", "NGHỈ LỄ", "CLOSED", "NHA KHOA ĐÓNG CỬA"].includes(cellVal.toUpperCase()) ||
                cellVal.toLowerCase().includes("nghỉ") ||
                cellVal.toLowerCase().includes("đóng cửa")) {
              holidayDates.add(dateStr);
              continue;
            }

            const matchedName = findStaffByName(cellVal, type);
            parsedStaffEntries.push({
              id: `IMPORT-${Date.now()}-${r}-${dayIdx}-${Math.random().toString(36).substr(2, 5)}`,
              date: dateStr,
              shift: currentShift,
              type,
              role,
              name: matchedName,
              room,
              roomColor,
              isDraft: true
            });
          }
        }

        const importedEntries: ScheduleEntry[] = [];

        // Build holiday closed entries
        holidayDates.forEach(dateStr => {
          importedEntries.push({
            id: `IMPORT-HOLID-${dateStr}-${Date.now()}`,
            date: dateStr,
            shift: "morning",
            type: "dentist",
            role: "dentist",
            name: "Nha khoa đóng cửa",
            room: "",
            roomColor: "",
            isHoliday: true,
            isDraft: true
          });
        });

        // Add regular assignments for non-holiday days
        parsedStaffEntries.forEach(entry => {
          if (!holidayDates.has(entry.date)) {
            importedEntries.push(entry);
          }
        });

        if (importedEntries.length === 0) {
          showToast("Không tìm thấy ca làm hợp lệ để nhập.", "info");
          return;
        }

        setActiveWeekSchedules(importedEntries);
        showToast(`Nhập thành công! Đã nạp ${importedEntries.length} ca làm từ Excel. Hãy nhấn "Lưu thay đổi" để xác nhận.`, "success");

      } catch (err) {
        showToast("Đọc file Excel thất bại. Vui lòng kiểm tra lại cấu trúc file.", "error");
        console.error(err);
      }
    };
    reader.readAsArrayBuffer(file);

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  // Clear all schedules in active week (Reset draft to empty)
  const handleClearAllSchedules = () => {
    setConfirmClearAllOpen(true);
  };

  // Save changes and check validation rules
  const handleSaveChanges = async () => {
    const schedulesToCheck = activeWeekSchedules;

    // EX-1 Check: Trùng lịch trực phòng khác
    for (let i = 0; i < schedulesToCheck.length; i++) {
      const current = schedulesToCheck[i];
      if (current.isHoliday) continue;

      const duplicate = schedulesToCheck.find((item, idx) =>
        idx !== i &&
        item.date === current.date &&
        item.shift === current.shift &&
        item.name === current.name &&
        (item.room !== current.room || item.role !== current.role)
      );

      if (duplicate) {
        showToast(`${current.name} đã được phân công tại ${duplicate.room} (${duplicate.role === "dentist" ? "Bác sĩ" : duplicate.role === "assistant" ? "Phụ tá" : "Nhân viên"}) trong ca này. Vui lòng kiểm tra lại!`, "error");
        return;
      }
    }

    setIsSaving(true);
    try {
      const entries = activeWeekSchedules.map(item => ({
        date: item.date,
        shift: item.shift,
        type: item.type,
        role: item.role,
        name: item.name,
        room: item.room,
        roomColor: item.roomColor,
        isHoliday: item.isHoliday ?? false,
      }));

      await saveWeekScheduleApi(weekParam, entries);
      showToast("Cập nhật lịch làm việc thành công!", "success");
      setTimeout(() => {
        router.push(`/dashboard/schedule?week=${weekParam}`);
      }, 1200);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Lưu lịch thất bại", "error");
    } finally {
      setIsSaving(false);
    }
  };

  // Filter staff list in modal matching search input and filters
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

    // Filter BR-SCH-EDIT-008: Only show active staff matching type and specialization
    let activeStaff = staffDatabase.filter(s => {
      const isCorrectType = s.status === "ACTIVE" && s.type === dbType;
      // Staff without specific position (default "Nhân viên") can be assigned to any slot
      const hasNoSpecificPosition = s.specialization === "Nhân viên";
      const matchesSpecialization = !filterSpecialization ||
        hasNoSpecificPosition ||
        s.specialization.toLowerCase().includes(filterSpecialization.toLowerCase());
      return isCorrectType && matchesSpecialization;
    });

    // Apply modal search query filter
    if (modalSearchQuery.trim() !== "") {
      const query = modalSearchQuery.toLowerCase();
      activeStaff = activeStaff.filter(s =>
        s.name.toLowerCase().includes(query) ||
        s.specialization.toLowerCase().includes(query)
      );
    }

    // Add info on whether staff is busy
    return activeStaff.map(staff => {
      // Already assigned to a DIFFERENT room/role in same shift
      const busyElsewhere = activeWeekSchedules.find(item =>
        item.date === date &&
        item.shift === shift &&
        item.name === staff.name &&
        (item.room !== room || item.role !== role)
      );
      // For staff multi-slots: already added to THIS slot
      const alreadyInSlot = role === "staff" && activeWeekSchedules.some(item =>
        item.date === date &&
        item.shift === shift &&
        item.room === room &&
        item.role === "staff" &&
        item.name === staff.name
      );

      const isBusy = !!busyElsewhere || alreadyInSlot;
      const busySource = busyElsewhere ?? null;
      return {
        ...staff,
        isBusy,
        busyRoom: alreadyInSlot ? room : (busySource ? busySource.room : null),
        busyRole: alreadyInSlot ? "Đã trong ca này" : (busySource ? (busySource.role === "dentist" ? "Bác sĩ" : busySource.role === "assistant" ? "Phụ tá" : "Nhân viên") : null),
      };
    });
  }, [modalCell, activeWeekSchedules, staffType, modalSearchQuery]);

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans text-slate-800">

      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <AdminSidebar activeMenu="schedule" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">

        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-16 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex flex-col">
            <h1 className="text-[18px] font-black text-slate-900 leading-tight">Chỉnh sửa lịch làm việc</h1>
            <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">Quản lý ca trực và lịch làm việc nhân sự</p>
          </div>

          <div className="flex items-center gap-4">
            <NotificationBell />
          </div>
        </header>

        {/* TOAST SYSTEM */}
        {toast && (
          <div className={`fixed top-24 right-8 z-50 px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border animate-fade-in font-bold text-[14.5px] transition-all max-w-md ${toast.type === "success"
              ? "bg-emerald-900 text-white border-emerald-800"
              : toast.type === "error"
                ? "bg-red-900 text-white border-red-800"
                : "bg-slate-900 text-white border-slate-800"
            }`}>
            <span className="text-lg">
              {toast.type === "success" ? "✓" : toast.type === "error" ? "⚠" : "ℹ"}
            </span>
            <span>{toast.message}</span>
          </div>
        )}

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* TITLE & BREADCRUMBS SECTION */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-5 shrink-0 select-none">
            <div className="flex flex-col text-left">
              {/* Breadcrumb */}
              <div className="flex items-center gap-1.5 text-[12.5px] font-bold text-slate-400">
                <Link href="/dashboard/schedule" className="hover:text-primary transition-colors">Lịch làm việc</Link>
                <span>&gt;</span>
                <span className="text-primary">Chỉnh sửa lịch làm việc</span>
              </div>

              {/* Title & Subtitle */}
              <h1 className="text-2xl font-black text-slate-900 mt-2 tracking-tight">Chỉnh sửa lịch làm việc</h1>
              <p className="text-[13.5px] font-bold text-slate-400 mt-1 leading-none">{formattedWeekRange.title}</p>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center gap-3">
              {/* Import Excel */}
              <input
                type="file"
                ref={fileInputRef}
                onChange={handleImportExcel}
                accept=".xlsx, .xls"
                className="hidden"
              />
              <button
                onClick={() => fileInputRef.current?.click()}
                className="px-4.5 py-3 bg-white border border-slate-350 hover:bg-slate-50 text-slate-655 text-[14px] font-extrabold rounded-xl transition-all shadow-sm cursor-pointer flex items-center gap-1.5 animate-pulse"
                title="Chọn file Excel để nhập nhanh lịch làm việc"
              >
                Nhập từ Excel
              </button>

              <button
                onClick={handleClearAllSchedules}
                className="px-4.5 py-3 bg-white border border-red-200 hover:bg-red-50 text-red-650 text-[14px] font-extrabold rounded-xl transition-all shadow-sm cursor-pointer flex items-center gap-1.5"
                title="Xóa tất cả các ca đang hiển thị ở tuần này"
              >
                Xóa tất cả các ca
              </button>

              <button
                onClick={handleCancelEditing}
                className="px-6 py-3 border border-slate-300 hover:bg-slate-50 text-slate-655 text-[14px] font-extrabold rounded-xl transition-all shadow-sm cursor-pointer"
              >
                Hủy
              </button>

              <button
                onClick={handleSaveChanges}
                disabled={isSaving}
                className="px-6 py-3 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl transition-all shadow-md shadow-primary/20 cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed"
              >
                {isSaving ? "Đang lưu..." : "Lưu thay đổi"}
              </button>
            </div>
          </div>

          {/* DENTIST/STAFF SELECTOR */}
          <div className="flex items-center justify-between gap-4">
            <div className="flex bg-[#eef2f6] p-1 rounded-xl border border-slate-200/20 shadow-sm select-none">
              <button
                onClick={() => { setStaffType("dentist"); }}
                className={`w-28 py-2.5 rounded-lg text-[13.5px] font-black transition-all cursor-pointer flex flex-col items-center justify-center leading-none ${staffType === "dentist" ? "bg-white text-primary shadow-sm border border-slate-200/20" : "text-slate-500 hover:text-slate-800"
                  }`}
              >
                <span>Nha sĩ</span>
                <span className="text-[10px] font-semibold text-slate-400 mt-0.5">(Dentists)</span>
              </button>
              <button
                onClick={() => { setStaffType("staff"); }}
                className={`w-28 py-2.5 rounded-lg text-[13.5px] font-black transition-all cursor-pointer flex flex-col items-center justify-center leading-none ${staffType === "staff" ? "bg-white text-primary shadow-sm border border-slate-200/20" : "text-slate-500 hover:text-slate-800"
                  }`}
              >
                <span>Nhân viên</span>
                <span className="text-[10px] font-semibold text-slate-400 mt-0.5">(Staff)</span>
              </button>
            </div>

            {/* Search */}
            <div className="relative w-64">
              <input
                type="text"
                placeholder="Tìm kiếm bác sĩ, phòng..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-4 pr-4 py-2 text-[13.5px] bg-slate-100/80 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none focus:ring-1 focus:ring-slate-200 transition-all font-semibold"
              />
            </div>
          </div>

          {/* CALENDAR MATRIX GRID */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[1000px] table-fixed">
                <thead>
                  <tr className="bg-slate-50/60 font-bold border-b border-slate-200 select-none">
                    {/* First column: Room / Shift */}
                    <th className="px-5 py-5 text-slate-500 font-extrabold text-[14px] w-[150px] text-center border-r border-slate-200/80">
                      <div className="flex flex-col items-center justify-center gap-1">
                        <span className="text-[12px] text-slate-400 font-black">Phòng / Thứ</span>
                      </div>
                    </th>

                    {/* Columns representing Mon to Sun */}
                    {weekDates.map((date, idx) => {
                      const daysVN = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật"];
                      const formattedDay = daysVN[idx];
                      const pad = (n: number) => String(n).padStart(2, "0");
                      const dateStr = formatDateKey(date);
                      const formattedDate = `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
                      const isToday = isWednesday(date);
                      const isClosed = isDayClosed(dateStr);

                      return (
                        <th
                          key={idx}
                          onClick={() => setDayActionDate(dateStr)}
                          className={`px-4 py-5 text-center border-r border-slate-200/80 last:border-r-0 cursor-pointer transition-all hover:bg-red-50/15 ${isToday ? "bg-red-50/30" : ""
                            } ${isClosed ? "bg-slate-100/40" : ""}`}
                          title={isClosed ? "Bấm để quản lý mở/đóng cửa ngày này" : "Bấm để quản lý đóng cửa hoặc xóa ca ngày này"}
                        >
                          <div className={`text-[13.5px] font-black tracking-tight ${isClosed ? "text-slate-400 line-through" : isToday ? "text-primary font-black" : "text-slate-800"
                            }`}>
                            {formattedDay}
                          </div>
                          <div className="flex items-center justify-center gap-1.5 mt-1">
                            <span className={`text-[14px] font-black ${isClosed ? "text-slate-400" : isToday ? "text-primary" : "text-slate-855"
                              }`}>
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

                  {/* CA SÁNG GROUP */}
                  <tr className="bg-slate-50/20">
                    <td
                      colSpan={8}
                      className="px-5 py-3 font-black text-[13px] tracking-wide text-primary uppercase border-b border-slate-200/60 bg-red-50/10"
                    >
                      Ca Sáng <span className="text-[11.5px] font-bold text-slate-400 lowercase italic ml-1">(08:00 - 12:00)</span>
                    </td>
                  </tr>

                  {roomRows.map((room) => (
                    <tr key={`morning_${room.key}`} className="min-h-[160px]">
                      {/* Room code column */}
                      <td className={`px-4 py-6 text-center border-r border-slate-200/80 font-black bg-slate-50/30 text-[12.5px] ${room.isDisabled ? "text-amber-600/70" : "text-slate-655"}`}>
                        <div className="flex flex-col items-center gap-1">
                          <span>{room.label}</span>
                          {room.isDisabled && (
                            <span className="text-[9px] font-black text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded uppercase tracking-wide leading-none">
                              {room.disabledReason}
                            </span>
                          )}
                        </div>
                      </td>

                      {/* Weekday Cells */}
                      {weekDates.map((date, dayIdx) => {
                        const dateStr = formatDateKey(date);
                        const isToday = isWednesday(date);
                        const isClosed = isDayClosed(dateStr);

                        // Render closed day block
                        if (isClosed) {
                          return (
                            <td
                              key={dayIdx}
                              onClick={() => setDayActionDate(dateStr)}
                              className="px-3.5 py-4 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-slate-100/50 cursor-pointer transition-all hover:bg-slate-200/35"
                              title="Bấm để mở lại cửa phòng khám"
                            >
                              <div className="flex flex-col items-center justify-center gap-1.5 text-[11.5px] font-black text-slate-400 tracking-wider">
                                <span>ĐÓNG CỬA</span>
                              </div>
                            </td>
                          );
                        }

                        // Render maintenance/inactive room block
                        if (room.isDisabled) {
                          return (
                            <td
                              key={dayIdx}
                              className="px-3.5 py-4 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-amber-50/30"
                              title={`Phòng đang ${room.disabledReason?.toLowerCase()} — không thể phân công`}
                            >
                              <div className="flex flex-col items-center justify-center gap-1.5 text-[11.5px] font-black text-amber-600/70 tracking-wider">
                                <span className="text-base leading-none">⚙</span>
                                <span>{room.disabledReason?.toUpperCase()}</span>
                              </div>
                            </td>
                          );
                        }

                        // Look up assignments
                        const docKey = `${dateStr}_morning_${room.key}_dentist`;
                        const astKey = `${dateStr}_morning_${room.key}_assistant`;

                        const doctorEntry = editLookup[docKey];
                        const assistantEntry = editLookup[astKey];

                        const hasDoctor = !!doctorEntry;

                        // Search queries filter
                        const matchesDocSearch = !searchQuery || (doctorEntry && doctorEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));
                        const matchesAstSearch = !searchQuery || (assistantEntry && assistantEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));

                        return (
                          <td
                            key={dayIdx}
                            className={`px-3 py-4.5 align-top border-r border-slate-200/80 last:border-r-0 transition-colors ${isToday ? "bg-red-50/10" : ""
                              }`}
                          >
                            <div className="flex flex-col gap-3 min-h-[120px] justify-start h-full">

                              {staffType === "dentist" ? (
                                <>
                                  {/* DENTIST SLOT SECTION */}
                                  {doctorEntry && matchesDocSearch ? (
                                    <div
                                      className={`relative group bg-white border-l-4 border-primary p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${doctorEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""
                                        }`}
                                    >
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button
                                          onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "dentist")}
                                          className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Sửa bác sĩ"
                                        >
                                          Sửa
                                        </button>
                                        <button
                                          onClick={(e) => handleRemoveAssignment(e, doctorEntry.id)}
                                          className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Xóa bác sĩ"
                                        >
                                          ✕
                                        </button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${doctorEntry.isDraft ? "text-primary" : "text-slate-800"}`}>
                                          {doctorEntry.name}
                                        </div>
                                        <div className="text-[10.5px] font-bold text-slate-400 mt-0.5 truncate">
                                          {staffDatabase.find(s => s.name === doctorEntry.name)?.specialization || "Bác sĩ"}
                                        </div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button
                                      onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "dentist")}
                                      className="py-2.5 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer"
                                    >
                                      <span>+ Bác sĩ</span>
                                    </button>
                                  )}

                                  {/* ASSISTANT SLOT SECTION */}
                                  {assistantEntry && matchesAstSearch ? (
                                    <div
                                      className={`relative group bg-slate-50 border-l-4 border-teal-500 p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${assistantEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""
                                        }`}
                                    >
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button
                                          onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "assistant")}
                                          className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Sửa phụ tá"
                                        >
                                          Sửa
                                        </button>
                                        <button
                                          onClick={(e) => handleRemoveAssignment(e, assistantEntry.id)}
                                          className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Xóa phụ tá"
                                        >
                                          ✕
                                        </button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${assistantEntry.isDraft ? "text-primary font-black" : "text-slate-755"}`}>
                                          {assistantEntry.name}
                                        </div>
                                        <div className="text-[10.5px] font-bold text-slate-450 mt-0.5 truncate uppercase tracking-wider">
                                          Phụ Tá
                                        </div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button
                                      disabled={!hasDoctor}
                                      onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "assistant")}
                                      className={`py-2.5 px-3 border border-dashed rounded-xl transition-all bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 ${hasDoctor
                                          ? "border-slate-250 hover:border-teal-500 text-slate-400 hover:text-teal-650 cursor-pointer hover:bg-slate-50/80"
                                          : "border-slate-200 text-slate-300 opacity-60 cursor-not-allowed bg-slate-50/30"
                                        }`}
                                      title={!hasDoctor ? "Vui lòng thêm bác sĩ trước khi phân bổ phụ tá" : "Thêm phụ tá"}
                                    >
                                      <span>+ Phụ tá {!hasDoctor && "(Khóa)"}</span>
                                    </button>
                                  )}
                                </>
                              ) : (
                                /* STAFF SLOT — multiple people allowed */
                                <div className="flex flex-col gap-2">
                                  {(staffMultiLookup[`${dateStr}_morning_${room.key}`] ?? [])
                                    .filter(e => !searchQuery || e.name.toLowerCase().includes(searchQuery.toLowerCase()))
                                    .map(entry => (
                                      <div
                                        key={entry.id}
                                        className={`relative bg-white border-l-4 ${room.color} px-2.5 py-2 rounded-xl shadow-sm border border-slate-200/70 flex items-center justify-between gap-2 ${entry.isDraft ? "border-red-400/60" : ""}`}
                                      >
                                        <div className="min-w-0">
                                          <div className={`text-[12.5px] font-black truncate ${entry.isDraft ? "text-primary" : "text-slate-800"}`}>
                                            {entry.name}
                                          </div>
                                          <div className="text-[10.5px] font-bold text-slate-400 truncate">
                                            {staffDatabase.find(s => s.name === entry.name)?.specialization || "Nhân viên"}
                                          </div>
                                        </div>
                                        <button
                                          onClick={(e) => handleRemoveAssignment(e, entry.id)}
                                          className="shrink-0 p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-400 hover:text-red-500 transition-all cursor-pointer text-[10px] leading-none"
                                          title="Xóa"
                                        >✕</button>
                                      </div>
                                    ))}
                                  <button
                                    onClick={(e) => handleOpenAssignModal(e, dateStr, "morning", room.key, room.color, "staff")}
                                    className="py-2 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer"
                                  >
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

                  {/* CA CHIỀU GROUP */}
                  <tr className="bg-slate-50/20">
                    <td
                      colSpan={8}
                      className="px-5 py-3 font-black text-[13px] tracking-wide text-primary uppercase border-b border-slate-200/60 bg-red-50/10"
                    >
                      Ca Chiều <span className="text-[11.5px] font-bold text-slate-400 lowercase italic ml-1">(13:30 - 17:30)</span>
                    </td>
                  </tr>

                  {/* Render rooms for afternoon */}
                  {roomRows.map((room) => (
                    <tr key={`afternoon_${room.key}`} className="min-h-[160px]">
                      {/* Room code column */}
                      <td className={`px-4 py-6 text-center border-r border-slate-200/80 font-black bg-slate-50/30 text-[12.5px] ${room.isDisabled ? "text-amber-600/70" : "text-slate-655"}`}>
                        <div className="flex flex-col items-center gap-1">
                          <span>{room.label}</span>
                          {room.isDisabled && (
                            <span className="text-[9px] font-black text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded uppercase tracking-wide leading-none">
                              {room.disabledReason}
                            </span>
                          )}
                        </div>
                      </td>

                      {/* Weekday Cells */}
                      {weekDates.map((date, dayIdx) => {
                        const dateStr = formatDateKey(date);
                        const isToday = isWednesday(date);
                        const isClosed = isDayClosed(dateStr);

                        // Closed day block
                        if (isClosed) {
                          return (
                            <td
                              key={dayIdx}
                              onClick={() => setDayActionDate(dateStr)}
                              className="px-3.5 py-4 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-slate-100/50 cursor-pointer transition-all hover:bg-slate-200/35"
                            >
                              <div className="flex flex-col items-center justify-center gap-1.5 text-[11.5px] font-black text-slate-400 tracking-wider">
                                <span>ĐÓNG CỬA</span>
                              </div>
                            </td>
                          );
                        }

                        // Render maintenance/inactive room block
                        if (room.isDisabled) {
                          return (
                            <td
                              key={dayIdx}
                              className="px-3.5 py-4 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-amber-50/30"
                              title={`Phòng đang ${room.disabledReason?.toLowerCase()} — không thể phân công`}
                            >
                              <div className="flex flex-col items-center justify-center gap-1.5 text-[11.5px] font-black text-amber-600/70 tracking-wider">
                                <span className="text-base leading-none">⚙</span>
                                <span>{room.disabledReason?.toUpperCase()}</span>
                              </div>
                            </td>
                          );
                        }

                        // Look up assignments for Doctor and Assistant inside room cell
                        const docKey = `${dateStr}_afternoon_${room.key}_dentist`;
                        const astKey = `${dateStr}_afternoon_${room.key}_assistant`;

                        const doctorEntry = editLookup[docKey];
                        const assistantEntry = editLookup[astKey];

                        const hasDoctor = !!doctorEntry;

                        // Search queries filter
                        const matchesDocSearch = !searchQuery || (doctorEntry && doctorEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));
                        const matchesAstSearch = !searchQuery || (assistantEntry && assistantEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));

                        return (
                          <td
                            key={dayIdx}
                            className={`px-3 py-4.5 align-top border-r border-slate-200/80 last:border-r-0 transition-colors ${isToday ? "bg-red-50/10" : ""
                              }`}
                          >
                            <div className="flex flex-col gap-3 min-h-[120px] justify-start h-full">

                              {staffType === "dentist" ? (
                                <>
                                  {/* DENTIST SLOT SECTION */}
                                  {doctorEntry && matchesDocSearch ? (
                                    <div
                                      className={`relative group bg-white border-l-4 border-primary p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${doctorEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""
                                        }`}
                                    >
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button
                                          onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "dentist")}
                                          className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Sửa bác sĩ"
                                        >
                                          Sửa
                                        </button>
                                        <button
                                          onClick={(e) => handleRemoveAssignment(e, doctorEntry.id)}
                                          className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Xóa bác sĩ"
                                        >
                                          ✕
                                        </button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${doctorEntry.isDraft ? "text-primary" : "text-slate-800"}`}>
                                          {doctorEntry.name}
                                        </div>
                                        <div className="text-[10.5px] font-bold text-slate-400 mt-0.5 truncate">
                                          {staffDatabase.find(s => s.name === doctorEntry.name)?.specialization || "Bác sĩ"}
                                        </div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button
                                      onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "dentist")}
                                      className="py-2.5 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer"
                                    >
                                      <span>+ Bác sĩ</span>
                                    </button>
                                  )}

                                  {/* ASSISTANT SLOT SECTION */}
                                  {assistantEntry && matchesAstSearch ? (
                                    <div
                                      className={`relative group bg-slate-50 border-l-4 border-teal-500 p-2.5 rounded-xl transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between min-h-[70px] ${assistantEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""
                                        }`}
                                    >
                                      <div className="absolute top-1.5 right-1.5 flex gap-1 z-10">
                                        <button
                                          onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "assistant")}
                                          className="p-1 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Sửa phụ tá"
                                        >
                                          Sửa
                                        </button>
                                        <button
                                          onClick={(e) => handleRemoveAssignment(e, assistantEntry.id)}
                                          className="p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[10px] leading-none"
                                          title="Xóa phụ tá"
                                        >
                                          ✕
                                        </button>
                                      </div>
                                      <div className="pr-10">
                                        <div className={`text-[12.5px] font-black ${assistantEntry.isDraft ? "text-primary font-black" : "text-slate-755"}`}>
                                          {assistantEntry.name}
                                        </div>
                                        <div className="text-[10.5px] font-bold text-slate-450 mt-0.5 truncate uppercase tracking-wider">
                                          Phụ Tá
                                        </div>
                                      </div>
                                    </div>
                                  ) : (
                                    <button
                                      disabled={!hasDoctor}
                                      onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "assistant")}
                                      className={`py-2.5 px-3 border border-dashed rounded-xl transition-all bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 ${hasDoctor
                                          ? "border-slate-250 hover:border-teal-500 text-slate-400 hover:text-teal-650 cursor-pointer hover:bg-slate-50/80"
                                          : "border-slate-200 text-slate-300 opacity-60 cursor-not-allowed bg-slate-50/30"
                                        }`}
                                      title={!hasDoctor ? "Vui lòng thêm bác sĩ trước khi phân bổ phụ tá" : "Thêm phụ tá"}
                                    >
                                      <span>+ Phụ tá {!hasDoctor && "(Khóa)"}</span>
                                    </button>
                                  )}
                                </>
                              ) : (
                                /* STAFF SLOT — multiple people allowed */
                                <div className="flex flex-col gap-2">
                                  {(staffMultiLookup[`${dateStr}_afternoon_${room.key}`] ?? [])
                                    .filter(e => !searchQuery || e.name.toLowerCase().includes(searchQuery.toLowerCase()))
                                    .map(entry => (
                                      <div
                                        key={entry.id}
                                        className={`relative bg-white border-l-4 ${room.color} px-2.5 py-2 rounded-xl shadow-sm border border-slate-200/70 flex items-center justify-between gap-2 ${entry.isDraft ? "border-red-400/60" : ""}`}
                                      >
                                        <div className="min-w-0">
                                          <div className={`text-[12.5px] font-black truncate ${entry.isDraft ? "text-primary" : "text-slate-800"}`}>
                                            {entry.name}
                                          </div>
                                          <div className="text-[10.5px] font-bold text-slate-400 truncate">
                                            {staffDatabase.find(s => s.name === entry.name)?.specialization || "Nhân viên"}
                                          </div>
                                        </div>
                                        <button
                                          onClick={(e) => handleRemoveAssignment(e, entry.id)}
                                          className="shrink-0 p-1 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-400 hover:text-red-500 transition-all cursor-pointer text-[10px] leading-none"
                                          title="Xóa"
                                        >✕</button>
                                      </div>
                                    ))}
                                  <button
                                    onClick={(e) => handleOpenAssignModal(e, dateStr, "afternoon", room.key, room.color, "staff")}
                                    className="py-2 px-3 border border-dashed border-slate-250 rounded-xl hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[11.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer"
                                  >
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

            {/* LEGEND SECTION */}
            <div className="bg-slate-50/60 border-t border-slate-200 px-6 py-4 flex flex-wrap gap-x-8 gap-y-2.5 text-[12.5px] font-bold text-slate-500 select-none">
              <div className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-primary animate-pulse"></span>
                <span>Lịch đang chỉnh sửa (Draft)</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-slate-350"></span>
                <span>Lịch đã xác nhận (Confirmed)</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="w-6 h-4 border-2 border-dashed border-slate-300 rounded bg-white"></span>
                <span>Vị trí còn trống (Empty Slot)</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-[11.5px] font-black text-slate-450 uppercase leading-none">Mẹo:</span>
                <span className="text-slate-500">Bấm vào tên Thứ ở đầu cột để đóng/mở cửa phòng khám cả ngày đó.</span>
              </div>
            </div>
          </div>

        </div>
      </main>

      {/* ── STAFF SELECTION POPUP MODAL ───────────────────────────────────── */}
      {modalCell && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col animate-scale-up">

            {/* Modal Header */}
            <div className="bg-slate-50 border-b border-slate-100 px-6 py-4.5 flex justify-between items-center">
              <div>
                <h3 className="font-black text-[16px] text-slate-900 tracking-tight">
                  Phân bổ {modalCell.role === "dentist" ? "Bác sĩ" : modalCell.role === "assistant" ? "Phụ tá" : "Nhân sự"}
                </h3>
                <p className="text-[12px] text-slate-455 font-bold mt-0.5">
                  {modalCell.room} | Ca {modalCell.shift === "morning" ? "Sáng" : "Chiều"} (Ngày {modalCell.date})
                </p>
              </div>
              <button
                onClick={() => setModalCell(null)}
                className="text-slate-400 hover:text-slate-700 font-extrabold text-lg p-1 hover:bg-slate-100 rounded cursor-pointer animate-pulse"
              >
                ✕
              </button>
            </div>

            {/* Modal Search Input (Functional search filter) */}
            <div className="p-4 border-b border-slate-100 bg-slate-50/50">
              <div className="relative">
                <span className="absolute inset-y-0 left-3 flex items-center text-slate-400 pointer-events-none"></span>
                <input
                  type="text"
                  placeholder={`Tìm tên hoặc chuyên môn ${modalCell.role === "dentist" ? "bác sĩ" : modalCell.role === "assistant" ? "phụ tá" : "nhân viên"}...`}
                  value={modalSearchQuery}
                  onChange={(e) => setModalSearchQuery(e.target.value)}
                  className="w-full pl-9 pr-4 py-2 text-[13.5px] bg-white border border-slate-200 focus:border-primary focus:outline-none rounded-lg font-semibold shadow-sm"
                  autoFocus
                />
              </div>
            </div>

            {/* Modal List of Active Staff */}
            <div className="max-h-[350px] overflow-y-auto divide-y divide-slate-100 p-2">
              {isLoadingStaff ? (
                <div className="p-8 text-center text-slate-400 font-bold text-[13.5px]">
                  Đang tải danh sách nhân viên...
                </div>
              ) : eligibleStaffList.length > 0 ? (
                eligibleStaffList.map((staff) => (
                  <button
                    key={staff.id}
                    disabled={staff.isBusy}
                    onClick={() => handleSelectStaff(staff)}
                    className={`w-full flex items-center justify-between p-3.5 text-left rounded-xl transition-all cursor-pointer ${staff.isBusy
                        ? "bg-slate-50/50 opacity-55 cursor-not-allowed"
                        : "hover:bg-red-50/45 hover:text-primary active:bg-red-50 text-slate-755 font-semibold"
                      }`}
                  >
                    <div>
                      <div className="font-black text-[13.5px]">{staff.name}</div>
                      <div className="text-[11.5px] font-bold text-slate-400 mt-0.5">{staff.specialization}</div>
                    </div>

                    {/* Status badge */}
                    {staff.isBusy ? (
                      <span className="text-[10px] font-black text-red-500 bg-red-50 px-2 py-0.5 rounded-full uppercase tracking-wider">
                        Đã bận làm {staff.busyRole} tại {staff.busyRoom}
                      </span>
                    ) : (
                      <span className="text-[10.5px] font-black text-emerald-600 bg-emerald-50 px-2.5 py-0.5 rounded-full uppercase tracking-wider">
                        Sẵn sàng
                      </span>
                    )}
                  </button>
                ))
              ) : (
                <div className="p-8 text-center text-slate-400 font-bold text-[13.5px]">
                  Không tìm thấy nhân viên phù hợp
                </div>
              )}
            </div>

            {/* Modal Footer */}
            <div className="bg-slate-50/50 border-t border-slate-100 px-6 py-4 flex justify-end gap-2.5">
              <button
                onClick={() => setModalCell(null)}
                className="px-4.5 py-2.5 border border-slate-200 hover:bg-slate-100 text-slate-655 text-[13px] font-black rounded-lg transition-all cursor-pointer"
              >
                Hủy bỏ
              </button>
            </div>

          </div>
        </div>
      )}

      {/* ── DAY ACTIONS MODAL ──────────────────────────────── */}
      {dayActionDate && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col p-6 animate-scale-up">
            <h3 className="font-black text-lg text-slate-900 flex items-center gap-2">
              Tùy chọn lịch ngày {dayActionDate}
            </h3>
            
            <p className="text-[13.5px] text-slate-500 mt-3 font-semibold leading-relaxed">
              Vui lòng chọn hành động bạn muốn thực hiện đối với lịch làm việc của ngày <strong>{dayActionDate}</strong>:
            </p>

            <div className="flex flex-col gap-3 mt-6">
              {isDayClosed(dayActionDate) ? (
                <button
                  onClick={() => handleToggleClosure(dayActionDate)}
                  className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-extrabold rounded-xl transition-all cursor-pointer shadow-md shadow-emerald-600/10 flex items-center justify-center gap-1.5"
                >
                  Mở cửa phòng khám
                </button>
              ) : (
                <>
                  <button
                    onClick={() => handleToggleClosure(dayActionDate)}
                    className="w-full py-3 bg-slate-800 hover:bg-slate-900 text-white font-extrabold rounded-xl transition-all cursor-pointer shadow-md flex items-center justify-center gap-1.5"
                  >
                    Đóng cửa phòng khám (Nghỉ cả ngày)
                  </button>
                  
                  <button
                    onClick={() => handleClearDayShifts(dayActionDate)}
                    className="w-full py-3 bg-red-50 hover:bg-red-100 text-red-650 border border-red-200 font-extrabold rounded-xl transition-all cursor-pointer flex items-center justify-center gap-1.5"
                  >
                    Xóa sạch ca làm việc trong ngày
                  </button>
                </>
              )}

              <button
                onClick={() => setDayActionDate(null)}
                className="w-full py-3 bg-white hover:bg-slate-50 border border-slate-200 text-slate-500 font-extrabold rounded-xl transition-all cursor-pointer flex items-center justify-center"
              >
                Hủy bỏ
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── CLEAR ALL CONFIRMATION MODAL ──────────────────────────────── */}
      {confirmClearAllOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col p-6 animate-scale-up">
            <h3 className="font-black text-lg text-slate-900 flex items-center gap-2">
              Xóa tất cả các ca làm việc
            </h3>
            <p className="text-[13.5px] text-slate-500 mt-3 font-semibold leading-relaxed">
              Bạn có chắc chắn muốn xóa toàn bộ phân bổ lịch trực (Bác sĩ, Phụ tá và Nhân viên) đã xếp cho tuần này không? Hành động này sẽ đưa tuần này về trạng thái trống (nháp) và bạn cần nhấn <strong>"Lưu thay đổi"</strong> để chính thức áp dụng.
            </p>
            <div className="flex justify-end gap-3 mt-6">
              <button
                onClick={() => setConfirmClearAllOpen(false)}
                className="px-4 py-2 border border-slate-250 hover:bg-slate-100 text-slate-655 text-[13px] font-black rounded-lg transition-all cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={() => {
                  setActiveWeekSchedules([]);
                  setConfirmClearAllOpen(false);
                  showToast("Đã xóa sạch các ca trong tuần (Dạng nháp)!", "info");
                }}
                className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-[13px] font-black rounded-lg transition-all cursor-pointer"
              >
                Xác nhận xóa
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}

export default function EditSchedulePage() {
  useRequireAdmin();
  return (
    <Suspense fallback={<div className="p-8 text-center font-bold text-slate-550">Đang tải cấu hình lịch làm việc...</div>}>
      <EditScheduleContent />
    </Suspense>
  );
}
