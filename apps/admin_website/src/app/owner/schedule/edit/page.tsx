"use client";

import React, { useState, useEffect, useMemo, useRef, useCallback, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import OwnerSidebar from "../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../hooks/useRequireOwner";
import { getWeekScheduleApi, saveWeekScheduleApi, getStaffApi, getRoomsApi, type RoomDto } from "../../../../lib/apiClient";
import { SHIFTS, SHIFT_PERIODS, shiftsByPeriod, shiftLabel, type ShiftDef } from "../../../../lib/shifts";
import * as XLSX from "xlsx";

// Define TypeScript interfaces for our scheduling data
interface ScheduleEntry {
  id: string;
  date: string; // format YYYY-MM-DD
  shift: string; // mã ca, ví dụ "08:00-10:00"
  type: "dentist" | "staff";
  role: "dentist" | "assistant" | "staff"; // dentist = Bác sĩ, assistant = Phụ tá, staff = Nhân viên hành chính
  name: string;
  room: string; // tên phòng đúng như trong bảng Rooms — ghi thẳng vào WorkSchedules.Room khi lưu
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
const DISABLED_ROOM_STATUSES = ["Bảo trì", "Ngừng hoạt động"];


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

// Nhận diện mã ca từ nhãn cột (dùng cho import Excel). Trả về id ca hoặc null.
const detectShiftFromLabel = (label: string): string | null => {
  const up = label.toUpperCase();
  // Ưu tiên khớp giờ bắt đầu của từng ca ("08:00", "10:00", "13:30"…)
  for (const s of SHIFTS) {
    const start = s.id.split("-")[0];
    if (up.includes(start)) return s.id;
  }
  // Fallback theo buổi cho file cũ (mất thông tin ca con → lấy ca đầu buổi)
  if (up.includes("SÁNG") || up.includes("MORNING")) return shiftsByPeriod("Buổi sáng")[0].id;
  if (up.includes("CHIỀU") || up.includes("AFTERNOON")) return shiftsByPeriod("Buổi chiều")[0].id;
  if (up.includes("TỐI") || up.includes("EVENING")) return shiftsByPeriod("Buổi tối")[0].id;
  return null;
};

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
  // Phân biệt rõ ba tình huống: đang tải / gọi API lỗi / tải xong nhưng bảng Rooms trống.
  // Trước đây cả ba đều cho ra cùng một lưới phòng nên không ai biết dữ liệu thật đang rỗng.
  const [roomsStatus, setRoomsStatus] = useState<"loading" | "ready" | "error">("loading");

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
    shift: string;
    room: string;
    roomColor: string;
    role: "dentist" | "assistant" | "staff";
  } | null>(null);

  const [modalSearchQuery, setModalSearchQuery] = useState("");

  // Confirm Day Action date (replacing confirmClosureDate)
  const [dayActionDate, setDayActionDate] = useState<string | null>(null);

  // Confirm Clear All modal state
  const [confirmClearAllOpen, setConfirmClearAllOpen] = useState(false);

  // Right-side staff palette: filter + search
  const [panelRole, setPanelRole] = useState<"all" | "dentist" | "assistant" | "staff">("dentist");
  const [panelSearch, setPanelSearch] = useState("");

  // Drag & drop state
  const [draggedStaff, setDraggedStaff] = useState<StaffMember | null>(null);
  const [dragOverKey, setDragOverKey] = useState<string | null>(null);

  // Bulk-assign popup (chọn phòng + nhiều ngày + nhiều ca)
  const [bulkStaff, setBulkStaff] = useState<StaffMember | null>(null);
  const [bulkRoom, setBulkRoom] = useState<string>("");
  const [bulkDays, setBulkDays] = useState<string[]>([]);
  const [bulkShifts, setBulkShifts] = useState<string[]>([]);

  // Hộp thoại báo lỗi chi tiết (trùng lịch / lỗi lưu từ server)
  const [errorModal, setErrorModal] = useState<{ title: string; messages: string[] } | null>(null);

  // Sao chép lịch tuần trước
  const [isCopying, setIsCopying] = useState(false);
  const [confirmCopyOpen, setConfirmCopyOpen] = useState(false);

  // Nhân bản lịch 1 ngày sang các thứ khác
  const [dayModalMode, setDayModalMode] = useState<"menu" | "clone">("menu");
  const [cloneTargets, setCloneTargets] = useState<string[]>([]);

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
  const loadRooms = useCallback(() => {
    getRoomsApi()
      .then(rooms => { setAllRooms(rooms); setRoomsStatus("ready"); })
      .catch(() => { setAllRooms([]); setRoomsStatus("error"); });
  }, []);

  useEffect(() => { loadRooms(); }, [loadRooms]);

  // Nút thử lại: đưa về trạng thái đang tải rồi gọi lại (loadRooms không tự đặt "loading" để
  // effect nạp lần đầu không setState đồng bộ trong thân effect).
  const retryLoadRooms = () => { setRoomsStatus("loading"); loadRooms(); };

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

  // Phòng LUÔN lấy từ bảng Rooms, không có phòng thì trả về rỗng để lưới hiện trạng thái
  // "chưa có phòng". Tuyệt đối không dựng phòng mặc định: tên phòng ở đây được ghi thẳng vào
  // WorkSchedules.Room khi lưu, và chính chuỗi đó là thứ hàng đợi lễ tân dùng để gom bệnh nhân
  // theo phòng — xếp lịch vào một phòng không tồn tại sẽ tạo ra hàng đợi ma.
  // Phòng khám dùng cho tab Nha sĩ (Bác sĩ & Phụ tá)
  const computeRoomRows = useCallback((): RoomRow[] => {
    return allRooms.map((room, idx) => ({
      label: room.name.toUpperCase(),
      key: room.name,
      color: ROOM_COLORS_DENTIST[idx % ROOM_COLORS_DENTIST.length],
      isDisabled: DISABLED_ROOM_STATUSES.includes(room.status),
      disabledReason: DISABLED_ROOM_STATUSES.includes(room.status) ? room.status : null,
    }));
  }, [allRooms]);

  const roomRows = useMemo(() => computeRoomRows(), [computeRoomRows]);

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

  // Group staff entries by Date+Shift — supports multiple people per shift without rooms
  const staffShiftLookup = useMemo(() => {
    const map: Record<string, ScheduleEntry[]> = {};
    activeWeekSchedules.forEach(item => {
      if (item.role === "staff" && !item.isHoliday) {
        const key = `${item.date}_${item.shift}`;
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
    shift: string,
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
        shift: SHIFTS[0].id, // arbitrary shift (holiday bỏ qua ca)
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

  // Reset chế độ modal ngày mỗi khi mở/đóng
  useEffect(() => {
    setDayModalMode("menu");
    setCloneTargets([]);
  }, [dayActionDate]);

  const toggleCloneTarget = (dateStr: string) =>
    setCloneTargets(prev => prev.includes(dateStr) ? prev.filter(d => d !== dateStr) : [...prev, dateStr]);

  // Nhân bản toàn bộ ca của ngày nguồn sang các ngày đích đã chọn (thay thế lịch cũ ở ngày đích)
  const handleCloneDayTo = () => {
    if (!dayActionDate || cloneTargets.length === 0) return;
    const source = activeWeekSchedules.filter(e => e.date === dayActionDate && !e.isHoliday);
    // Bỏ toàn bộ ca cũ ở các ngày đích rồi thêm bản sao từ ngày nguồn
    const next = activeWeekSchedules.filter(e => !cloneTargets.includes(e.date));
    cloneTargets.forEach(target => {
      source.forEach((e, i) => {
        next.push({
          ...e,
          id: `CLONE-${Date.now()}-${target}-${i}-${Math.random().toString(36).substr(2, 4)}`,
          date: target,
          isDraft: true,
        });
      });
    });
    setActiveWeekSchedules(next);
    const cnt = cloneTargets.length;
    const src = dayActionDate;
    setDayActionDate(null);
    showToast(`Đã nhân bản lịch ngày ${src} sang ${cnt} ngày (nháp). Nhấn "Lưu thay đổi" để xác nhận.`, "success");
  };

  const makeDraftId = () => `DRAFT-${Date.now()}-${Math.random().toString(36).substr(2, 5)}`;

  // Core assignment logic — shared by modal, drag&drop và bulk assign.
  // Trả về true nếu có thêm/ghi đè, false nếu bị bỏ qua (trùng chỗ).
  const assignStaffToSlot = (
    staff: StaffMember,
    date: string,
    shift: string,
    room: string,
    roomColor: string,
    role: "dentist" | "assistant" | "staff"
  ): boolean => {
    if (role === "staff") {
      // Staff slots support multiple people — tránh trùng cùng người trong cùng ca
      const dup = activeWeekSchedules.some(i =>
        i.date === date && i.shift === shift && i.role === "staff" && i.name === staff.name
      );
      if (dup) return false;
      const newEntry: ScheduleEntry = {
        id: makeDraftId(),
        date, shift,
        type: "staff",
        role: "staff",
        name: staff.name,
        room: "",
        roomColor: "border-green-600",
        isDraft: true,
      };
      setActiveWeekSchedules(prev => [...prev, newEntry]);
      return true;
    }

    // Dentist/assistant slots: replace existing entry for that slot
    const lookupKey = `${date}_${shift}_${room}_${role}`;
    const existing = editLookup[lookupKey];
    const updatedEntry: ScheduleEntry = {
      id: existing ? existing.id : makeDraftId(),
      date, shift,
      type: "dentist",
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
    return true;
  };

  // Save selection back to draft schedules (từ modal chọn 1 ô)
  const handleSelectStaff = (staff: StaffMember) => {
    if (!modalCell) return;
    const { date, shift, room, roomColor, role } = modalCell;
    assignStaffToSlot(staff, date, shift, room, roomColor, role);
    setModalCell(null);
    setModalSearchQuery("");
  };

  // ── Drag & drop ─────────────────────────────────────────────────────────
  const handleDropOnSlot = (
    e: React.DragEvent,
    date: string,
    shift: string,
    room: string,
    roomColor: string,
    role: "dentist" | "assistant" | "staff"
  ) => {
    e.preventDefault();
    setDragOverKey(null);
    const staff = draggedStaff;
    setDraggedStaff(null);
    if (!staff) return;

    const roleLabel = role === "dentist" ? "Bác sĩ" : role === "assistant" ? "Phụ tá" : "Nhân viên";

    // Kiểm tra đúng vai trò
    const expectedType = role === "staff" ? "staff" : role; // dentist | assistant | staff
    if (staff.type !== expectedType) {
      showToast(`Không thể thả "${staff.name}" vào vị trí ${roleLabel}.`, "error");
      return;
    }
    if (isDayClosed(date)) {
      showToast("Ngày này đang đóng cửa, không thể phân công.", "error");
      return;
    }
    // Phụ tá cần có bác sĩ trong ô trước
    if (role === "assistant" && !editLookup[`${date}_${shift}_${room}_dentist`]) {
      showToast("Vui lòng thêm bác sĩ trước khi phân bổ phụ tá.", "error");
      return;
    }
    // Người này đã bận trong cùng ca
    const busy = activeWeekSchedules.some(i => {
      if (i.date !== date || i.shift !== shift || i.name !== staff.name) return false;
      if (role === "staff") return i.role === "staff";
      return i.room !== room || i.role !== role;
    });
    if (busy) {
      showToast(`"${staff.name}" đã có ca trực trong khung giờ này.`, "error");
      return;
    }

    const ok = assignStaffToSlot(staff, date, shift, room, roomColor, role);
    if (ok) {
      if (role === "staff") {
        showToast(`Đã thêm "${staff.name}" vào ca ${shiftLabel(shift)}.`, "success");
      } else {
        showToast(`Đã thêm "${staff.name}" vào ${room}.`, "success");
      }
    } else {
      showToast(`"${staff.name}" đã có trong ca trực này.`, "info");
    }
  };

  const handleDragOverSlot = (e: React.DragEvent, key: string) => {
    if (!draggedStaff) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = "copy";
    if (dragOverKey !== key) setDragOverKey(key);
  };

  // ── Bulk assign (chọn nhiều ngày + nhiều ca) ─────────────────────
  const openBulkAssign = (staff: StaffMember) => {
    const rows = computeRoomRows();
    const firstEnabled = rows.find(r => !r.isDisabled);
    setBulkRoom(staff.type === "staff" ? "" : (firstEnabled?.key ?? ""));
    setBulkDays([]);
    setBulkShifts([]);
    setBulkStaff(staff);
  };

  const toggleBulkDay = (dateStr: string) =>
    setBulkDays(prev => prev.includes(dateStr) ? prev.filter(d => d !== dateStr) : [...prev, dateStr]);
  const toggleBulkShift = (shiftId: string) =>
    setBulkShifts(prev => prev.includes(shiftId) ? prev.filter(s => s !== shiftId) : [...prev, shiftId]);

  const handleBulkConfirm = () => {
    if (!bulkStaff) return;
    const isStaffRole = bulkStaff.type === "staff";

    if ((!isStaffRole && !bulkRoom) || bulkDays.length === 0 || bulkShifts.length === 0) {
      showToast(isStaffRole ? "Vui lòng chọn ít nhất 1 ngày và 1 ca." : "Vui lòng chọn phòng, ít nhất 1 ngày và 1 ca.", "error");
      return;
    }

    const role: "dentist" | "assistant" | "staff" = bulkStaff.type;
    const rows = computeRoomRows();
    const roomRow = rows.find(r => r.key === bulkRoom);
    const roomColor = roomRow?.color ?? "border-green-600";

    const next = [...activeWeekSchedules];
    let added = 0;
    let skipped = 0;

    const isClosed = (d: string) => next.some(i => i.date === d && i.isHoliday);

    for (const date of bulkDays) {
      if (isClosed(date)) { skipped += bulkShifts.length; continue; }
      for (const shift of bulkShifts) {
        if (role === "staff") {
          const dup = next.some(i =>
            i.date === date && i.shift === shift && i.role === "staff" && i.name === bulkStaff.name
          );
          if (dup) { skipped++; continue; }
          next.push({
            id: makeDraftId(),
            date, shift, type: "staff", role: "staff",
            name: bulkStaff.name, room: "", roomColor: "border-green-600", isDraft: true,
          });
          added++;
        } else {
          // Người này đã bận ở phòng/vai trò khác trong cùng ca
          const busy = next.some(i =>
            i.date === date && i.shift === shift && i.name === bulkStaff.name && (i.room !== bulkRoom || i.role !== role)
          );
          if (busy) { skipped++; continue; }

          if (role === "assistant") {
            // Phụ tá cần có bác sĩ trong ô
            const hasDoctor = next.some(i =>
              i.date === date && i.shift === shift && i.room === bulkRoom && i.role === "dentist"
            );
            if (!hasDoctor) { skipped++; continue; }
            const idx = next.findIndex(i =>
              i.role === "assistant" && i.date === date && i.shift === shift && i.room === bulkRoom
            );
            const entry: ScheduleEntry = {
              id: idx >= 0 ? next[idx].id : makeDraftId(),
              date, shift, type: "dentist", role: "assistant",
              name: bulkStaff.name, room: bulkRoom, roomColor, isDraft: true,
            };
            if (idx >= 0) next[idx] = entry; else next.push(entry);
            added++;
          } else {
            // dentist — ghi đè ô nếu đã có
            const idx = next.findIndex(i =>
              i.role === "dentist" && i.date === date && i.shift === shift && i.room === bulkRoom
            );
            const entry: ScheduleEntry = {
              id: idx >= 0 ? next[idx].id : makeDraftId(),
              date, shift, type: "dentist", role: "dentist",
              name: bulkStaff.name, room: bulkRoom, roomColor, isDraft: true,
            };
            if (idx >= 0) next[idx] = entry; else next.push(entry);
            added++;
          }
        }
      }
    }

    setActiveWeekSchedules(next);
    setBulkStaff(null);
    if (added === 0) {
      showToast(`Không phân bổ được ca nào cho "${bulkStaff.name}" (bỏ qua ${skipped}).`, "info");
    } else {
      showToast(`Đã phân bổ "${bulkStaff.name}" vào ${added} ca${skipped ? `, bỏ qua ${skipped}` : ""}. Nhấn "Lưu thay đổi" để xác nhận.`, "success");
    }
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
      router.push(`/owner/schedule?week=${weekParam}`);
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

    // Không có phòng thì không có gì để gắn ca vào — dừng sớm thay vì nhập ra lịch trỏ vào hư không.
    if (allRooms.length === 0) {
      showToast("Chưa có phòng nào trong hệ thống nên không thể nhập lịch. Nhờ Admin thêm phòng trước.", "error");
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }

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
        // Nhãn cột trong file không khớp phòng nào trong hệ thống — báo lại để biết vì sao thiếu ca.
        const unmatchedLabels = new Set<string>();

        let currentShift: string | null = null;

        for (let r = 0; r < rows.length; r++) {
          const rowData = rows[r];
          if (!rowData) continue;

          // Check Column A (index 0) to dynamically update current shift.
          // Ưu tiên nhận diện khung giờ cụ thể của 1 trong 6 ca; nếu file cũ chỉ ghi
          // "SÁNG"/"CHIỀU"/"TỐI" thì đưa về ca ĐẦU TIÊN của buổi đó (import thô).
          const colAVal = String(rowData[0] || "").trim().toUpperCase();
          const detected = detectShiftFromLabel(colAVal);
          if (detected) currentShift = detected;

          if (!currentShift) continue;

          // Check Column B (index 1) for room/role
          const positionLabel = String(rowData[1] || "").trim();
          if (!positionLabel) continue;

          const label = positionLabel.toLowerCase();
          const isStaffRow = ["lễ tân", "cskh", "chăm sóc", "kế toán", "hành chính", "bảo vệ", "nhân viên", "staff", "reception", "thu ngân", "tư vấn"].some(k => label.includes(k));

          let type: "dentist" | "staff" = "dentist";
          let role: "dentist" | "assistant" | "staff" = "dentist";
          let room = "";
          let roomColor = "border-green-600";

          if (isStaffRow) {
            type = "staff";
            role = "staff";
            room = "";
            roomColor = "border-green-600";
          } else {
            // Phòng lấy từ bảng Rooms cho khối Nha sĩ
            const matchedRoom = [...allRooms]
              .sort((a, b) => b.name.length - a.name.length)
              .find(r => label.includes(r.name.toLowerCase()));

            if (!matchedRoom) {
              unmatchedLabels.add(positionLabel);
              continue;
            }

            type = "dentist";
            role = label.includes("phụ tá") ? "assistant" : "dentist";
            room = matchedRoom.name;
            roomColor = computeRoomRows().find(r => r.key === room)?.color ?? "border-primary";
          }

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
            shift: SHIFTS[0].id,
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
          showToast(
            unmatchedLabels.size > 0
              ? `Không nhập được ca nào: các cột "${[...unmatchedLabels].join('", "')}" trong file không khớp phòng nào trong hệ thống.`
              : "Không tìm thấy ca làm hợp lệ để nhập.",
            "info");
          return;
        }

        setActiveWeekSchedules(importedEntries);
        showToast(
          unmatchedLabels.size > 0
            ? `Đã nạp ${importedEntries.length} ca làm. Bỏ qua các cột không khớp phòng nào: "${[...unmatchedLabels].join('", "')}". Hãy nhấn "Lưu thay đổi" để xác nhận.`
            : `Nhập thành công! Đã nạp ${importedEntries.length} ca làm từ Excel. Hãy nhấn "Lưu thay đổi" để xác nhận.`,
          unmatchedLabels.size > 0 ? "info" : "success");

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

  // Nhấn nút "Sao chép tuần trước" — luôn hỏi xác nhận trước khi thực hiện
  const handleCopyPreviousWeekClick = () => {
    setConfirmCopyOpen(true);
  };

  // Tải lịch tuần liền trước rồi dịch ngày +7 để nạp vào tuần hiện tại (dạng nháp)
  const doCopyPreviousWeek = async () => {
    setConfirmCopyOpen(false);
    setIsCopying(true);
    try {
      // Ngày tuần trước tương ứng theo từng thứ (map 1-1 với weekDates hiện tại)
      const prevKeys = weekDates.map(d => {
        const p = new Date(d);
        p.setDate(d.getDate() - 7);
        return formatDateKey(p);
      });
      const dateMap: Record<string, string> = {};
      prevKeys.forEach((pk, i) => { dateMap[pk] = formatDateKey(weekDates[i]); });

      const dtos = await getWeekScheduleApi(prevKeys[0]);
      if (dtos.length === 0) {
        showToast("Tuần trước không có lịch để sao chép.", "info");
        return;
      }

      const copied: ScheduleEntry[] = dtos.map((dto, idx) => ({
        id: `COPY-${Date.now()}-${idx}-${Math.random().toString(36).substr(2, 4)}`,
        date: dateMap[dto.date] ?? dto.date,
        shift: dto.shift,
        type: dto.type,
        role: dto.role,
        name: dto.name,
        room: dto.room,
        roomColor: dto.roomColor,
        isHoliday: dto.isHoliday,
        isDraft: true,
      }));

      setActiveWeekSchedules(copied);
      showToast(`Đã sao chép ${copied.length} ca từ tuần trước (nháp). Nhấn "Lưu thay đổi" để xác nhận.`, "success");
    } catch (err) {
      setErrorModal({
        title: "Sao chép tuần trước thất bại",
        messages: [err instanceof Error ? err.message : "Không thể tải lịch tuần trước."],
      });
    } finally {
      setIsCopying(false);
    }
  };

  const roleLabelOf = (role: string) =>
    role === "dentist" ? "Bác sĩ" : role === "assistant" ? "Phụ tá" : "Nhân viên";

  // Gom TẤT CẢ lỗi trước khi lưu, trả về danh sách mô tả rõ ràng.
  const collectValidationErrors = (list: ScheduleEntry[]): string[] => {
    const errors: string[] = [];

    // 1) Trùng lịch: cùng một người, cùng ngày + ca
    const reported = new Set<string>();
    for (let i = 0; i < list.length; i++) {
      const cur = list[i];
      if (cur.isHoliday) continue;
      for (let j = i + 1; j < list.length; j++) {
        const other = list[j];
        if (other.isHoliday) continue;
        if (
          cur.date === other.date &&
          cur.shift === other.shift &&
          cur.name === other.name
        ) {
          const key = `${cur.name}|${cur.date}|${cur.shift}`;
          if (!reported.has(key)) {
            reported.add(key);
            if (cur.role === "staff" && other.role === "staff") {
              errors.push(
                `Trùng lịch: Nhân viên "${cur.name}" bị phân công trùng 2 lần trong ca ${shiftLabel(cur.shift)} ngày ${cur.date}.`
              );
            } else {
              errors.push(
                `Trùng lịch: "${cur.name}" bị xếp 2 nơi trong ca ${shiftLabel(cur.shift)} ngày ${cur.date} — ` +
                `${cur.room || "Ca trực"} (${roleLabelOf(cur.role)}) và ${other.room || "Ca trực"} (${roleLabelOf(other.role)}).`
              );
            }
          }
        }
      }
    }

    // 2) Phụ tá không có bác sĩ trong cùng ô
    list
      .filter(e => e.role === "assistant" && !e.isHoliday)
      .forEach(a => {
        const hasDoctor = list.some(d =>
          d.role === "dentist" && d.date === a.date && d.shift === a.shift && d.room === a.room
        );
        if (!hasDoctor) {
          errors.push(
            `Thiếu bác sĩ: Phụ tá "${a.name}" ở ${a.room} ca ${shiftLabel(a.shift)} ngày ${a.date} nhưng chưa có bác sĩ trong ô này.`
          );
        }
      });

    // 3) Ô chưa chọn tên (nhân sự để trống)
    list
      .filter(e => !e.isHoliday && (!e.name || e.name.trim() === ""))
      .forEach(e => {
        errors.push(
          `Chưa chọn nhân sự: ${e.room ? e.room + " " : ""}ca ${shiftLabel(e.shift)} ngày ${e.date} (${roleLabelOf(e.role)}) đang để trống tên.`
        );
      });

    return errors;
  };

  // Save changes and check validation rules
  const handleSaveChanges = async () => {
    const errors = collectValidationErrors(activeWeekSchedules);
    if (errors.length > 0) {
      setErrorModal({
        title: `Không thể lưu — phát hiện ${errors.length} lỗi`,
        messages: errors,
      });
      return;
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
        router.push(`/owner/schedule?week=${weekParam}`);
      }, 1200);
    } catch (err) {
      // Báo rõ lỗi trả về từ server thay vì toast biến mất nhanh
      const raw = err instanceof Error ? err.message : "Lưu lịch thất bại (lỗi không xác định).";
      const messages = raw.split(/\r?\n/).map(s => s.trim()).filter(Boolean);
      setErrorModal({
        title: "Lưu lịch thất bại",
        messages: messages.length > 0 ? messages : [raw],
      });
    } finally {
      setIsSaving(false);
    }
  };

  // Filter staff list in modal matching search input and filters
  const eligibleStaffList = useMemo(() => {
    if (!modalCell) return [];

    const { date, shift, room, role } = modalCell;

    let dbType: "dentist" | "assistant" | "staff" = "staff";

    if (staffType === "dentist") {
      dbType = role === "dentist" ? "dentist" : "assistant";
    } else {
      dbType = "staff";
    }

    // Filter BR-SCH-EDIT-008: Only show active staff matching type
    let activeStaff = staffDatabase.filter(s => s.status === "ACTIVE" && s.type === dbType);

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
      // For staff: check if already assigned to THIS date+shift
      if (role === "staff") {
        const alreadyInShift = activeWeekSchedules.some(item =>
          item.date === date &&
          item.shift === shift &&
          item.role === "staff" &&
          item.name === staff.name
        );
        return {
          ...staff,
          isBusy: alreadyInShift,
          busyRoom: alreadyInShift ? "Ca này" : null,
          busyRole: alreadyInShift ? "Đã trong ca này" : null,
        };
      }

      // For dentist/assistant:
      const busyElsewhere = activeWeekSchedules.find(item =>
        item.date === date &&
        item.shift === shift &&
        item.name === staff.name &&
        (item.room !== room || item.role !== role)
      );

      const isBusy = !!busyElsewhere;
      return {
        ...staff,
        isBusy,
        busyRoom: busyElsewhere ? busyElsewhere.room : null,
        busyRole: busyElsewhere ? (busyElsewhere.role === "dentist" ? "Bác sĩ" : busyElsewhere.role === "assistant" ? "Phụ tá" : "Nhân viên") : null,
      };
    });
  }, [modalCell, activeWeekSchedules, staffType, modalSearchQuery, staffDatabase]);

  // Danh sách nhân sự hiển thị ở cột palette bên phải
  const panelStaffList = useMemo(() => {
    let list = staffDatabase.filter(s => s.status === "ACTIVE");
    if (panelRole !== "all") list = list.filter(s => s.type === panelRole);
    if (panelSearch.trim() !== "") {
      const q = panelSearch.toLowerCase();
      list = list.filter(s => s.name.toLowerCase().includes(q) || s.specialization.toLowerCase().includes(q));
    }
    return list;
  }, [staffDatabase, panelRole, panelSearch]);

  // ── Lưới rỗng: không có phòng khám nào để xếp ──────────────────────────────
  const renderNoRoomsRow = () => {
    return (
      <tr>
        <td colSpan={weekDates.length + 1} className="px-6 py-16">
          <div className="flex flex-col items-center justify-center gap-2.5 text-center">
            {roomsStatus === "loading" ? (
              <>
                <div className="w-7 h-7 border-[3px] border-primary/20 border-t-primary rounded-full animate-spin" />
                <p className="text-[13.5px] font-bold text-slate-500">Đang tải danh sách phòng...</p>
              </>
            ) : roomsStatus === "error" ? (
              <>
                <div className="w-14 h-14 rounded-full bg-red-50 flex items-center justify-center">
                  <svg className="w-7 h-7 text-red-500" fill="none" stroke="currentColor" strokeWidth="1.8" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                  </svg>
                </div>
                <p className="text-[14px] font-black text-slate-700">Không tải được danh sách phòng</p>
                <p className="text-[12.5px] font-semibold text-slate-400 max-w-md">
                  Máy chủ không phản hồi hoặc phiên đăng nhập đã hết hạn. Không thể xếp lịch khi chưa biết phòng nào đang có.
                </p>
                <button
                  onClick={retryLoadRooms}
                  className="mt-1 px-4 py-2 text-[12.5px] font-black text-white bg-primary rounded-xl hover:opacity-90 transition-all cursor-pointer"
                >
                  Thử lại
                </button>
              </>
            ) : (
              <>
                <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
                  <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21" />
                  </svg>
                </div>
                <p className="text-[14px] font-black text-slate-700">
                  Chưa có phòng khám nào
                </p>
                <p className="text-[12.5px] font-semibold text-slate-400 max-w-md leading-relaxed">
                  Hệ thống chưa có phòng khám nào để xếp bác sĩ và phụ tá. Nhờ Admin thêm phòng ở mục Quản lý phòng, sau đó tải lại danh sách.
                </p>
                <button
                  onClick={retryLoadRooms}
                  className="mt-1 px-4 py-2 text-[12.5px] font-black text-primary bg-primary/5 border border-primary/30 rounded-xl hover:bg-primary/10 transition-all cursor-pointer"
                >
                  Tải lại danh sách phòng
                </button>
              </>
            )}
          </div>
        </td>
      </tr>
    );
  };

  // ── Render một ô ngày cho khối Nha sĩ (Bác sĩ / Phụ tá theo phòng) ──────
  const renderDayCell = (shift: string, room: RoomRow, date: Date, dayIdx: number) => {
    const dateStr = formatDateKey(date);
    const isToday = isWednesday(date);
    const isClosed = isDayClosed(dateStr);

    // Render closed day block
    if (isClosed) {
      return (
        <td
          key={dayIdx}
          onClick={() => setDayActionDate(dateStr)}
          className="px-1.5 py-2 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-slate-100/50 cursor-pointer transition-all hover:bg-slate-200/35"
          title="Bấm để mở lại cửa phòng khám"
        >
          <div className="flex flex-col items-center justify-center gap-1.5 text-[10px] font-black text-slate-400 tracking-wider">
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
          className="px-1.5 py-2 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-amber-50/30"
          title={`Phòng đang ${room.disabledReason?.toLowerCase()} — không thể phân công`}
        >
          <div className="flex flex-col items-center justify-center gap-1 text-[10px] font-black text-amber-600/70 tracking-wider">
            <span className="text-sm leading-none">⚙</span>
            <span>{room.disabledReason?.toUpperCase()}</span>
          </div>
        </td>
      );
    }

    // Look up assignments
    const docKey = `${dateStr}_${shift}_${room.key}_dentist`;
    const astKey = `${dateStr}_${shift}_${room.key}_assistant`;

    const doctorEntry = editLookup[docKey];
    const assistantEntry = editLookup[astKey];

    const hasDoctor = !!doctorEntry;

    // Search queries filter
    const matchesDocSearch = !searchQuery || (doctorEntry && doctorEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));
    const matchesAstSearch = !searchQuery || (assistantEntry && assistantEntry.name.toLowerCase().includes(searchQuery.toLowerCase()));

    return (
      <td
        key={dayIdx}
        className={`px-1.5 py-2 align-top border-r border-slate-200/80 last:border-r-0 transition-colors ${isToday ? "bg-red-50/10" : ""}`}
      >
        <div className="flex flex-col gap-1.5 min-h-[56px] justify-start h-full">

          {/* DENTIST SLOT SECTION */}
          <div
            onDragOver={(e) => handleDragOverSlot(e, docKey)}
            onDragLeave={() => setDragOverKey(prev => prev === docKey ? null : prev)}
            onDrop={(e) => handleDropOnSlot(e, dateStr, shift, room.key, room.color, "dentist")}
            className={dragOverKey === docKey ? "rounded-xl ring-2 ring-primary/70 ring-offset-1" : ""}
          >
          {doctorEntry && matchesDocSearch ? (
            <div
              className={`relative group bg-white border-l-4 border-primary p-1.5 rounded-lg transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between ${doctorEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""}`}
            >
              <div className="absolute top-1 right-1 flex gap-0.5 z-10 opacity-0 group-hover:opacity-100 transition-opacity">
                <button
                  onClick={(e) => handleOpenAssignModal(e, dateStr, shift, room.key, room.color, "dentist")}
                  className="px-1 py-0.5 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[9px] leading-none"
                  title="Sửa bác sĩ"
                >
                  Sửa
                </button>
                <button
                  onClick={(e) => handleRemoveAssignment(e, doctorEntry.id)}
                  className="px-1 py-0.5 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[9px] leading-none"
                  title="Xóa bác sĩ"
                >
                  ✕
                </button>
              </div>
              <div className="pr-2">
                <div className={`text-[11px] font-black leading-tight break-words ${doctorEntry.isDraft ? "text-primary" : "text-slate-800"}`}>
                  {doctorEntry.name}
                </div>
                <div className="text-[9px] font-bold text-slate-400 mt-0.5 break-words">
                  {staffDatabase.find(s => s.name === doctorEntry.name)?.specialization || "Bác sĩ"}
                </div>
              </div>
            </div>
          ) : (
            <button
              onClick={(e) => handleOpenAssignModal(e, dateStr, shift, room.key, room.color, "dentist")}
              className="py-1.5 px-1.5 border border-dashed border-slate-250 rounded-lg hover:border-primary text-slate-400 hover:text-primary transition-all hover:bg-slate-50/80 bg-white text-[10px] font-extrabold flex items-center justify-center gap-1 cursor-pointer"
            >
              <span>+ Bác sĩ</span>
            </button>
          )}
          </div>

          {/* ASSISTANT SLOT SECTION */}
          <div
            onDragOver={(e) => handleDragOverSlot(e, astKey)}
            onDragLeave={() => setDragOverKey(prev => prev === astKey ? null : prev)}
            onDrop={(e) => handleDropOnSlot(e, dateStr, shift, room.key, room.color, "assistant")}
            className={dragOverKey === astKey ? "rounded-xl ring-2 ring-teal-500/70 ring-offset-1" : ""}
          >
          {assistantEntry && matchesAstSearch ? (
            <div
              className={`relative group bg-slate-50 border-l-4 border-teal-500 p-1.5 rounded-lg transition-all shadow-sm border border-slate-200/70 hover:border-slate-350 hover:shadow flex flex-col justify-between ${assistantEntry.isDraft ? "border-2 border-red-500 shadow-red-50/50" : ""}`}
            >
              <div className="absolute top-1 right-1 flex gap-0.5 z-10 opacity-0 group-hover:opacity-100 transition-opacity">
                <button
                  onClick={(e) => handleOpenAssignModal(e, dateStr, shift, room.key, room.color, "assistant")}
                  className="px-1 py-0.5 bg-slate-50 hover:bg-slate-100 rounded border border-slate-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[9px] leading-none"
                  title="Sửa phụ tá"
                >
                  Sửa
                </button>
                <button
                  onClick={(e) => handleRemoveAssignment(e, assistantEntry.id)}
                  className="px-1 py-0.5 bg-slate-50 hover:bg-red-50 rounded border border-slate-200 hover:border-red-200 text-slate-500 hover:text-primary transition-all cursor-pointer text-[9px] leading-none"
                  title="Xóa phụ tá"
                >
                  ✕
                </button>
              </div>
              <div className="pr-2">
                <div className={`text-[11px] font-black leading-tight break-words ${assistantEntry.isDraft ? "text-primary font-black" : "text-slate-755"}`}>
                  {assistantEntry.name}
                </div>
                <div className="text-[9px] font-bold text-slate-450 mt-0.5 uppercase tracking-wider">
                  Phụ Tá
                </div>
              </div>
            </div>
          ) : (
            <button
              disabled={!hasDoctor}
              onClick={(e) => handleOpenAssignModal(e, dateStr, shift, room.key, room.color, "assistant")}
              className={`py-1.5 px-1.5 border border-dashed rounded-lg transition-all bg-white text-[10px] font-extrabold flex items-center justify-center gap-1 ${hasDoctor
                  ? "border-slate-250 hover:border-teal-500 text-slate-400 hover:text-teal-650 cursor-pointer hover:bg-slate-50/80"
                  : "border-slate-200 text-slate-300 opacity-60 cursor-not-allowed bg-slate-50/30"}`}
              title={!hasDoctor ? "Vui lòng thêm bác sĩ trước khi phân bổ phụ tá" : "Thêm phụ tá"}
            >
              <span>+ Phụ tá {!hasDoctor && "(Khóa)"}</span>
            </button>
          )}
          </div>

        </div>
      </td>
    );
  };

  // ── Render một ô ca trực cho khối Nhân viên (không cần phòng) ────────────
  const renderStaffDayCell = (shift: string, date: Date, dayIdx: number) => {
    const dateStr = formatDateKey(date);
    const isToday = isWednesday(date);
    const isClosed = isDayClosed(dateStr);
    const cellKey = `${dateStr}_${shift}_staff`;

    if (isClosed) {
      return (
        <td
          key={dayIdx}
          onClick={() => setDayActionDate(dateStr)}
          className="px-1.5 py-2 text-center align-middle border-r border-slate-200/80 last:border-r-0 bg-slate-100/50 cursor-pointer transition-all hover:bg-slate-200/35"
          title="Bấm để mở lại cửa phòng khám"
        >
          <div className="flex flex-col items-center justify-center gap-1.5 text-[10px] font-black text-slate-400 tracking-wider">
            <span>ĐÓNG CỬA</span>
          </div>
        </td>
      );
    }

    const assignedStaff = (staffShiftLookup[`${dateStr}_${shift}`] ?? [])
      .filter(e => !searchQuery || e.name.toLowerCase().includes(searchQuery.toLowerCase()));

    return (
      <td
        key={dayIdx}
        className={`px-2 py-2.5 align-top border-r border-slate-200/80 last:border-r-0 transition-colors ${isToday ? "bg-red-50/10" : ""}`}
      >
        <div
          onDragOver={(e) => handleDragOverSlot(e, cellKey)}
          onDragLeave={() => setDragOverKey(prev => prev === cellKey ? null : prev)}
          onDrop={(e) => handleDropOnSlot(e, dateStr, shift, "", "border-green-600", "staff")}
          className={`flex flex-col gap-1.5 min-h-[64px] justify-start h-full p-1 rounded-xl transition-all ${dragOverKey === cellKey ? "ring-2 ring-emerald-500 bg-emerald-50/20" : ""}`}
        >
          {assignedStaff.map(entry => (
            <div
              key={entry.id}
              className={`group relative bg-white border-l-4 border-emerald-600 px-2.5 py-2 rounded-xl shadow-sm border border-slate-200/70 flex items-center justify-between gap-1.5 hover:border-slate-350 transition-all ${entry.isDraft ? "border-emerald-400/60 ring-1 ring-emerald-500/20 shadow-emerald-50" : ""}`}
            >
              <div className="min-w-0">
                <div className={`text-[12px] font-black leading-tight break-words ${entry.isDraft ? "text-primary font-black" : "text-slate-800"}`}>
                  {entry.name}
                </div>
                <div className="text-[10px] font-bold text-slate-400 break-words mt-0.5">
                  {staffDatabase.find(s => s.name === entry.name)?.specialization || "Nhân viên"}
                </div>
              </div>
              <button
                onClick={(e) => handleRemoveAssignment(e, entry.id)}
                className="shrink-0 w-5 h-5 flex items-center justify-center bg-slate-50 hover:bg-red-50 rounded-lg border border-slate-200 hover:border-red-200 text-slate-400 hover:text-red-500 transition-all cursor-pointer text-[10px] font-black opacity-0 group-hover:opacity-100"
                title="Xóa nhân viên khỏi ca"
              >✕</button>
            </div>
          ))}

          <button
            onClick={(e) => handleOpenAssignModal(e, dateStr, shift, "", "border-green-600", "staff")}
            className="py-1.5 px-2 border border-dashed border-slate-250 rounded-xl hover:border-emerald-600 text-slate-400 hover:text-emerald-700 transition-all hover:bg-emerald-50/30 bg-white text-[10.5px] font-extrabold flex items-center justify-center gap-1 cursor-pointer mt-auto"
          >
            <span>+ Nhân viên</span>
          </button>
        </div>
      </td>
    );
  };

  // ── Render một hàng ca trực cho khối Nhân viên ───────────────────────────
  const renderStaffShiftRow = (shift: ShiftDef) => (
    <tr key={`staff_${shift.id}`}>
      <td className="px-3 py-3 text-center border-r border-slate-200/80 font-black bg-slate-50/40 text-[12px] text-slate-700">
        <div className="flex flex-col items-center gap-0.5">
          <span className="text-[12px] font-black text-slate-800">{shift.label}</span>
          <span className="text-[9.5px] font-bold text-slate-400 uppercase tracking-wider">{shift.id}</span>
        </div>
      </td>
      {weekDates.map((date, dayIdx) => renderStaffDayCell(shift.id, date, dayIdx))}
    </tr>
  );

  // ── Render một hàng phòng cho một ca (khối Nha sĩ) ────────────────────────
  const renderRoomRow = (shift: ShiftDef, room: RoomRow) => (
    <tr key={`${shift.id}_${room.key}`}>
      {/* Room code column */}
      <td className={`px-2 py-2.5 text-center border-r border-slate-200/80 font-black bg-slate-50/30 text-[11px] ${room.isDisabled ? "text-amber-600/70" : "text-slate-655"}`}>
        <div className="flex flex-col items-center gap-1">
          <span className="leading-tight">{room.label}</span>
          {room.isDisabled && (
            <span className="text-[8px] font-black text-amber-600 bg-amber-50 px-1 py-0.5 rounded uppercase tracking-wide leading-none">
              {room.disabledReason}
            </span>
          )}
        </div>
      </td>

      {weekDates.map((date, dayIdx) => renderDayCell(shift.id, room, date, dayIdx))}
    </tr>
  );

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50 font-sans text-slate-800">

      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <OwnerSidebar activeMenu="schedule" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">

        {/* HEADER */}
        <OwnerPageHeader
          title="Chỉnh sửa lịch làm việc"
          subtitle="Quản lý ca trực và lịch làm việc nhân sự"
        />

        {/* TOAST SYSTEM */}
        {toast && (
          <div className={`fixed top-3 right-8 z-[70] px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border animate-fade-in font-bold text-[14.5px] transition-all max-w-md ${toast.type === "success"
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
        <div className="p-8 flex-1 min-h-0 overflow-y-auto flex flex-col gap-6">

          {/* TITLE & BREADCRUMBS SECTION */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-5 shrink-0 select-none">
            <div className="flex flex-col text-left">
              {/* Breadcrumb */}
              <div className="flex items-center gap-1.5 text-[12.5px] font-bold text-slate-400">
                <Link href="/owner/schedule" className="hover:text-primary transition-colors">Lịch làm việc</Link>
                <span>&gt;</span>
                <span className="text-primary">Chỉnh sửa lịch làm việc</span>
              </div>

              {/* Title & Subtitle */}
              <h1 className="text-2xl font-black text-slate-900 mt-2 tracking-tight">Chỉnh sửa lịch làm việc</h1>
              <p className="text-[13.5px] font-bold text-slate-400 mt-1 leading-none">{formattedWeekRange.title}</p>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center gap-3">
              {/* Sao chép tuần trước */}
              <button
                onClick={handleCopyPreviousWeekClick}
                disabled={isCopying}
                className="px-4.5 py-3 bg-white border border-slate-350 hover:bg-slate-50 text-slate-655 text-[14px] font-extrabold rounded-xl transition-all shadow-sm cursor-pointer flex items-center gap-1.5 disabled:opacity-60 disabled:cursor-not-allowed"
                title="Nạp lại toàn bộ lịch của tuần liền trước vào tuần này"
              >
                {isCopying ? "Đang sao chép..." : "Sao chép tuần trước"}
              </button>

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

          {/* GRID + STAFF PALETTE ROW */}
          <div className="flex gap-6 items-start">

          {/* CALENDAR MATRIX GRID */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col flex-1 min-w-0">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[680px] table-fixed">
                <thead>
                  <tr className="bg-slate-50/60 font-bold border-b border-slate-200 select-none">
                    {/* First column: Room / Shift */}
                    <th className="px-2 py-3 text-slate-500 font-extrabold text-[12px] w-[86px] text-center border-r border-slate-200/80">
                      <div className="flex flex-col items-center justify-center gap-1">
                        <span className="text-[11px] text-slate-500 font-black leading-tight">
                          {staffType === "dentist" ? "Phòng / Thứ" : "Ca trực / Thứ"}
                        </span>
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
                          className={`px-2 py-3 text-center border-r border-slate-200/80 last:border-r-0 cursor-pointer transition-all hover:bg-red-50/15 ${isToday ? "bg-red-50/30" : ""
                            } ${isClosed ? "bg-slate-100/40" : ""}`}
                          title={isClosed ? "Bấm để quản lý mở/đóng cửa ngày này" : "Bấm để quản lý đóng cửa hoặc xóa ca ngày này"}
                        >
                          <div className={`text-[12px] font-black tracking-tight leading-tight ${isClosed ? "text-slate-400 line-through" : isToday ? "text-primary font-black" : "text-slate-800"
                            }`}>
                            {formattedDay}
                          </div>
                          <div className="flex flex-col items-center justify-center gap-0.5 mt-0.5">
                            <span className={`text-[12px] font-black ${isClosed ? "text-slate-400" : isToday ? "text-primary" : "text-slate-855"
                              }`}>
                              {formattedDate}
                            </span>
                            {isClosed && <span className="text-[8.5px] text-red-500 font-extrabold bg-red-50 px-1 py-0.5 rounded leading-none">CLOSED</span>}
                          </div>
                        </th>
                      );
                    })}
                  </tr>
                </thead>

                <tbody className="divide-y divide-slate-250/70">

                  {staffType === "dentist" ? (
                    roomRows.length === 0 ? renderNoRoomsRow() : SHIFT_PERIODS.map((period) => (
                      <React.Fragment key={period}>
                        {/* Nhóm buổi: Sáng / Chiều / Tối */}
                        <tr>
                          <td
                            colSpan={weekDates.length + 1}
                            className="px-3 py-1.5 font-black text-[12px] tracking-wider text-slate-700 uppercase border-y border-slate-200/70 bg-slate-100/60"
                          >
                            {period}
                          </td>
                        </tr>

                        {shiftsByPeriod(period).map((shift) => (
                          <React.Fragment key={shift.id}>
                            {/* Một ca 2 tiếng độc lập (ngang hàng) */}
                            <tr className="bg-slate-50/20">
                              <td
                                colSpan={weekDates.length + 1}
                                className="px-3 py-1 font-black text-[11px] tracking-wide text-primary uppercase border-b border-slate-200/50 bg-red-50/10"
                              >
                                Ca {shift.label}
                              </td>
                            </tr>
                            {roomRows.map((room) => renderRoomRow(shift, room))}
                          </React.Fragment>
                        ))}
                      </React.Fragment>
                    ))
                  ) : (
                    SHIFT_PERIODS.map((period) => (
                      <React.Fragment key={period}>
                        {/* Nhóm buổi: Sáng / Chiều / Tối */}
                        <tr>
                          <td
                            colSpan={weekDates.length + 1}
                            className="px-3 py-1.5 font-black text-[12px] tracking-wider text-slate-700 uppercase border-y border-slate-200/70 bg-slate-100/60"
                          >
                            {period}
                          </td>
                        </tr>

                        {shiftsByPeriod(period).map((shift) => renderStaffShiftRow(shift))}
                      </React.Fragment>
                    ))
                  )}

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
                <span className="text-slate-500">Kéo thẻ nhân sự ở cột phải vào ô, hoặc bấm thẻ để phân bổ nhiều ca cùng lúc.</span>
              </div>
            </div>
          </div>

          {/* ── STAFF PALETTE PANEL (cột phải, ghim cố định) ─────────────── */}
          <aside className="w-64 shrink-0 self-start sticky top-2 z-10 bg-white rounded-2xl border border-slate-200/60 shadow-md flex flex-col max-h-[calc(100vh-90px)]">
            <div className="px-4 pt-4 pb-3 border-b border-slate-100">
              <h3 className="font-black text-[14.5px] text-slate-900 tracking-tight">Danh sách nhân sự</h3>
              <p className="text-[11px] text-slate-400 font-semibold mt-0.5 leading-snug">
                Kéo thả vào ô, hoặc bấm để phân bổ nhiều ngày / nhiều ca.
              </p>
            </div>

            {/* Filter chips */}
            <div className="px-3 py-2.5 flex flex-wrap gap-1.5 border-b border-slate-100">
              {([
                { k: "dentist", l: "Bác sĩ" },
                { k: "assistant", l: "Phụ tá" },
                { k: "staff", l: "Nhân viên" },
                { k: "all", l: "Tất cả" },
              ] as const).map(({ k, l }) => (
                <button
                  key={k}
                  onClick={() => {
                    setPanelRole(k);
                    if (k === "staff") setStaffType("staff");
                    else if (k === "dentist" || k === "assistant") setStaffType("dentist");
                  }}
                  className={`px-3 py-1.5 rounded-lg text-[12px] font-black transition-all cursor-pointer border ${panelRole === k
                    ? "bg-primary text-white border-primary shadow-sm"
                    : "bg-slate-50 text-slate-500 border-slate-200 hover:bg-slate-100"}`}
                >
                  {l}
                </button>
              ))}
            </div>

            {/* Search */}
            <div className="px-3 py-2.5 border-b border-slate-100">
              <input
                type="text"
                placeholder="Tìm theo tên / chuyên môn..."
                value={panelSearch}
                onChange={(e) => setPanelSearch(e.target.value)}
                className="w-full px-3.5 py-2 text-[13px] bg-slate-100/80 rounded-lg border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none focus:ring-1 focus:ring-slate-200 transition-all font-semibold"
              />
            </div>

            {/* Staff cards list */}
            <div className="flex-1 overflow-y-auto p-2 flex flex-col gap-1.5">
              {isLoadingStaff ? (
                <div className="p-6 text-center text-slate-400 font-bold text-[12.5px]">Đang tải...</div>
              ) : panelStaffList.length === 0 ? (
                <div className="p-6 text-center text-slate-400 font-bold text-[12.5px]">Không có nhân sự phù hợp</div>
              ) : (
                panelStaffList.map((staff) => {
                  const roleLabel = staff.type === "dentist" ? "Bác sĩ" : staff.type === "assistant" ? "Phụ tá" : "Nhân viên";
                  const accent = staff.type === "dentist" ? "border-l-primary" : staff.type === "assistant" ? "border-l-teal-500" : "border-l-green-600";
                  return (
                    <div
                      key={staff.id}
                      draggable
                      onDragStart={(e) => {
                        setDraggedStaff(staff);
                        e.dataTransfer.effectAllowed = "copy";
                        e.dataTransfer.setData("text/plain", staff.id);
                      }}
                      onDragEnd={() => { setDraggedStaff(null); setDragOverKey(null); }}
                      onClick={() => openBulkAssign(staff)}
                      title="Kéo vào ô lịch, hoặc bấm để phân bổ nhiều ca"
                      className={`group flex items-center justify-between gap-2 bg-white border border-slate-200/70 border-l-4 ${accent} rounded-xl px-3 py-2.5 cursor-grab active:cursor-grabbing hover:border-slate-300 hover:shadow-sm transition-all`}
                    >
                      <div className="min-w-0">
                        <div className="text-[12.5px] font-black text-slate-800 truncate">{staff.name}</div>
                        <div className="text-[10.5px] font-bold text-slate-400 truncate">
                          {staff.specialization} · {roleLabel}
                        </div>
                      </div>
                      <button
                        onClick={(e) => { e.stopPropagation(); openBulkAssign(staff); }}
                        className="shrink-0 px-2.5 py-1 bg-slate-50 group-hover:bg-primary group-hover:text-white text-slate-500 border border-slate-200 group-hover:border-primary rounded-lg text-[11px] font-black transition-all cursor-pointer"
                        title="Phân bổ nhiều ngày / nhiều ca"
                      >
                        + Thêm
                      </button>
                    </div>
                  );
                })
              )}
            </div>
          </aside>

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
                  Phân bổ {modalCell.role === "dentist" ? "Bác sĩ" : modalCell.role === "assistant" ? "Phụ tá" : "Nhân viên"}
                </h3>
                <p className="text-[12px] text-slate-455 font-bold mt-0.5">
                  {modalCell.role === "staff" ? "" : `${modalCell.room} | `}Ca {shiftLabel(modalCell.shift)} (Ngày {modalCell.date})
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

      {/* ── BULK ASSIGN POPUP (phòng + nhiều ngày + nhiều ca) ─────────────── */}
      {bulkStaff && (() => {
        const bulkRoomRows = computeRoomRows();
        const roleLabel = bulkStaff.type === "dentist" ? "Bác sĩ" : bulkStaff.type === "assistant" ? "Phụ tá" : "Nhân viên";
        const daysVN = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật"];
        const pad = (n: number) => String(n).padStart(2, "0");
        const totalSlots = bulkDays.length * bulkShifts.length;
        return (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
            <div className="bg-white w-full max-w-lg rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col animate-scale-up max-h-[90vh]">

              {/* Header */}
              <div className="bg-slate-50 border-b border-slate-100 px-6 py-4.5 flex justify-between items-center shrink-0">
                <div>
                  <h3 className="font-black text-[16px] text-slate-900 tracking-tight">Phân bổ nhiều ca</h3>
                  <p className="text-[12px] text-slate-455 font-bold mt-0.5">
                    {bulkStaff.name} · {roleLabel}
                  </p>
                </div>
                <button
                  onClick={() => setBulkStaff(null)}
                  className="text-slate-400 hover:text-slate-700 font-extrabold text-lg p-1 hover:bg-slate-100 rounded cursor-pointer"
                >
                  ✕
                </button>
              </div>

              <div className="p-6 overflow-y-auto flex flex-col gap-5">
                {/* Chọn phòng (chỉ hiện cho khối Nha sĩ) */}
                {bulkStaff.type === "staff" ? (
                  <div className="px-4 py-3 rounded-xl bg-emerald-50 border border-emerald-200/80 text-[12.5px] font-bold text-emerald-800 flex items-center gap-2">
                    <span className="text-emerald-600 text-base">ℹ</span>
                    <span>Phân bổ khối Nhân viên trực tiếp theo ca trực (không cần chọn phòng).</span>
                  </div>
                ) : (
                  <div>
                    <label className="block text-[12.5px] font-black text-slate-700 mb-2 uppercase tracking-wide">Chọn phòng</label>
                    {bulkRoomRows.length === 0 && (
                      <div className="px-3.5 py-3 rounded-lg bg-slate-50 border border-dashed border-slate-250 text-[12.5px] font-bold text-slate-400">
                        Chưa có phòng nào để phân bổ — nhờ Admin thêm phòng ở mục Quản lý phòng.
                      </div>
                    )}
                    <div className="flex flex-wrap gap-2">
                      {bulkRoomRows.map((room) => (
                        <button
                          key={room.key}
                          disabled={room.isDisabled}
                          onClick={() => setBulkRoom(room.key)}
                          className={`px-3.5 py-2 rounded-lg text-[12.5px] font-black border transition-all ${room.isDisabled
                            ? "bg-slate-50 text-slate-300 border-slate-200 cursor-not-allowed line-through"
                            : bulkRoom === room.key
                              ? "bg-primary text-white border-primary shadow-sm cursor-pointer"
                              : "bg-white text-slate-600 border-slate-250 hover:border-primary cursor-pointer"}`}
                        >
                          {room.label}{room.isDisabled ? ` (${room.disabledReason})` : ""}
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                {/* Chọn nhiều ngày */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <label className="text-[12.5px] font-black text-slate-700 uppercase tracking-wide">Chọn ngày</label>
                    <button
                      onClick={() => setBulkDays(prev => prev.length === weekDates.length ? [] : weekDates.map(d => formatDateKey(d)))}
                      className="text-[11px] font-black text-primary hover:underline cursor-pointer"
                    >
                      {bulkDays.length === weekDates.length ? "Bỏ chọn tất cả" : "Chọn tất cả"}
                    </button>
                  </div>
                  <div className="grid grid-cols-4 gap-2">
                    {weekDates.map((date, idx) => {
                      const dateStr = formatDateKey(date);
                      const closed = isDayClosed(dateStr);
                      const selected = bulkDays.includes(dateStr);
                      return (
                        <button
                          key={dateStr}
                          disabled={closed}
                          onClick={() => toggleBulkDay(dateStr)}
                          className={`flex flex-col items-center py-2 rounded-lg border text-[12px] font-black transition-all ${closed
                            ? "bg-slate-50 text-slate-300 border-slate-200 cursor-not-allowed"
                            : selected
                              ? "bg-primary/10 text-primary border-primary cursor-pointer"
                              : "bg-white text-slate-600 border-slate-250 hover:border-primary cursor-pointer"}`}
                        >
                          <span>{daysVN[idx]}</span>
                          <span className="text-[11px] font-bold text-slate-400 mt-0.5">
                            {closed ? "Đóng cửa" : `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>

                {/* Chọn nhiều ca */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <label className="text-[12.5px] font-black text-slate-700 uppercase tracking-wide">Chọn ca</label>
                    <button
                      onClick={() => setBulkShifts(prev => prev.length === SHIFTS.length ? [] : SHIFTS.map(s => s.id))}
                      className="text-[11px] font-black text-primary hover:underline cursor-pointer"
                    >
                      {bulkShifts.length === SHIFTS.length ? "Bỏ chọn tất cả" : "Chọn tất cả"}
                    </button>
                  </div>
                  {SHIFT_PERIODS.map((period) => (
                    <div key={period} className="mb-2.5">
                      <div className="text-[10.5px] font-black text-slate-400 uppercase tracking-wider mb-1.5">{period}</div>
                      <div className="grid grid-cols-2 gap-2">
                        {shiftsByPeriod(period).map((shift) => {
                          const selected = bulkShifts.includes(shift.id);
                          return (
                            <button
                              key={shift.id}
                              onClick={() => toggleBulkShift(shift.id)}
                              className={`py-2 rounded-lg border text-[12px] font-black transition-all cursor-pointer ${selected
                                ? "bg-primary/10 text-primary border-primary"
                                : "bg-white text-slate-600 border-slate-250 hover:border-primary"}`}
                            >
                              {shift.label}
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Footer */}
              <div className="bg-slate-50/50 border-t border-slate-100 px-6 py-4 flex items-center justify-between gap-3 shrink-0">
                <span className="text-[12px] font-bold text-slate-500">
                  {totalSlots > 0 ? `${totalSlots} ô sẽ được phân bổ` : "Chọn ngày và ca"}
                </span>
                <div className="flex gap-2.5">
                  <button
                    onClick={() => setBulkStaff(null)}
                    className="px-4.5 py-2.5 border border-slate-200 hover:bg-slate-100 text-slate-655 text-[13px] font-black rounded-lg transition-all cursor-pointer"
                  >
                    Hủy bỏ
                  </button>
                  <button
                    onClick={handleBulkConfirm}
                    disabled={(bulkStaff.type !== "staff" && !bulkRoom) || bulkDays.length === 0 || bulkShifts.length === 0}
                    className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[13px] font-black rounded-lg transition-all cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
                  >
                    Phân bổ
                  </button>
                </div>
              </div>

            </div>
          </div>
        );
      })()}

      {/* ── DAY ACTIONS MODAL ──────────────────────────────── */}
      {dayActionDate && (() => {
        const daysVN = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật"];
        const pad = (n: number) => String(n).padStart(2, "0");
        const sourceCount = activeWeekSchedules.filter(e => e.date === dayActionDate && !e.isHoliday).length;
        return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col p-6 animate-scale-up">
            {dayModalMode === "clone" ? (
              /* ── Chế độ NHÂN BẢN sang ngày khác ── */
              <>
                <h3 className="font-black text-lg text-slate-900 flex items-center gap-2">
                  Nhân bản lịch ngày {dayActionDate}
                </h3>
                <p className="text-[13.5px] text-slate-500 mt-3 font-semibold leading-relaxed">
                  Chọn các thứ muốn sao chép <strong>{sourceCount}</strong> ca của ngày này sang. Lịch hiện có ở những ngày được chọn sẽ <strong>bị thay thế</strong>.
                </p>

                <div className="grid grid-cols-4 gap-2 mt-5">
                  {weekDates.map((date, idx) => {
                    const dk = formatDateKey(date);
                    if (dk === dayActionDate) return null; // bỏ ngày nguồn
                    const closed = isDayClosed(dk);
                    const selected = cloneTargets.includes(dk);
                    return (
                      <button
                        key={dk}
                        disabled={closed}
                        onClick={() => toggleCloneTarget(dk)}
                        className={`flex flex-col items-center py-2 rounded-lg border text-[12px] font-black transition-all ${closed
                          ? "bg-slate-50 text-slate-300 border-slate-200 cursor-not-allowed"
                          : selected
                            ? "bg-primary/10 text-primary border-primary cursor-pointer"
                            : "bg-white text-slate-600 border-slate-250 hover:border-primary cursor-pointer"}`}
                      >
                        <span>{daysVN[idx]}</span>
                        <span className="text-[11px] font-bold text-slate-400 mt-0.5">
                          {closed ? "Đóng cửa" : `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`}
                        </span>
                      </button>
                    );
                  })}
                </div>

                <div className="flex items-center justify-between gap-2 mt-6">
                  <button
                    onClick={() => setDayModalMode("menu")}
                    className="px-4 py-2.5 border border-slate-200 hover:bg-slate-100 text-slate-655 text-[13px] font-black rounded-lg transition-all cursor-pointer"
                  >
                    ← Quay lại
                  </button>
                  <button
                    onClick={handleCloneDayTo}
                    disabled={cloneTargets.length === 0}
                    className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[13px] font-black rounded-lg transition-all cursor-pointer shadow-sm disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Nhân bản{cloneTargets.length > 0 ? ` (${cloneTargets.length} ngày)` : ""}
                  </button>
                </div>
              </>
            ) : (
              /* ── Menu tùy chọn mặc định ── */
              <>
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
                        onClick={() => setDayModalMode("clone")}
                        disabled={sourceCount === 0}
                        className="w-full py-3 bg-primary/10 hover:bg-primary/15 text-primary border border-primary/30 font-extrabold rounded-xl transition-all cursor-pointer flex items-center justify-center gap-1.5 disabled:opacity-50 disabled:cursor-not-allowed"
                        title={sourceCount === 0 ? "Ngày này chưa có ca để nhân bản" : "Sao chép lịch ngày này sang các thứ khác"}
                      >
                        Nhân bản sang thứ khác{sourceCount > 0 ? ` (${sourceCount} ca)` : ""}
                      </button>

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
              </>
            )}
          </div>
        </div>
        );
      })()}

      {/* ── ERROR REPORT MODAL (báo rõ lỗi) ───────────────────────────── */}
      {errorModal && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-lg rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col animate-scale-up max-h-[85vh]">
            {/* Header */}
            <div className="bg-red-50 border-b border-red-100 px-6 py-4.5 flex justify-between items-start gap-3 shrink-0">
              <div className="flex items-start gap-2.5">
                <span className="text-red-500 text-xl leading-none mt-0.5">⚠</span>
                <div>
                  <h3 className="font-black text-[16px] text-red-700 tracking-tight">{errorModal.title}</h3>
                  <p className="text-[12px] text-red-500/80 font-bold mt-0.5">
                    Vui lòng sửa các mục dưới đây rồi lưu lại.
                  </p>
                </div>
              </div>
              <button
                onClick={() => setErrorModal(null)}
                className="text-slate-400 hover:text-slate-700 font-extrabold text-lg p-1 hover:bg-white/60 rounded cursor-pointer shrink-0"
              >
                ✕
              </button>
            </div>

            {/* Danh sách lỗi */}
            <div className="p-4 overflow-y-auto flex flex-col gap-2">
              {errorModal.messages.map((msg, idx) => (
                <div
                  key={idx}
                  className="flex items-start gap-2.5 bg-red-50/60 border border-red-100 rounded-xl px-3.5 py-2.5"
                >
                  <span className="shrink-0 w-5 h-5 rounded-full bg-red-500 text-white text-[11px] font-black flex items-center justify-center mt-0.5">
                    {idx + 1}
                  </span>
                  <span className="text-[13px] font-semibold text-slate-700 leading-snug">{msg}</span>
                </div>
              ))}
            </div>

            {/* Footer */}
            <div className="bg-slate-50/50 border-t border-slate-100 px-6 py-4 flex justify-end shrink-0">
              <button
                onClick={() => setErrorModal(null)}
                className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[13px] font-black rounded-lg transition-all cursor-pointer shadow-sm"
              >
                Đã hiểu, quay lại sửa
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── COPY PREVIOUS WEEK CONFIRMATION MODAL ─────────────────────── */}
      {confirmCopyOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm animate-fade-in p-4">
          <div className="bg-white w-full max-w-md rounded-2xl shadow-2xl border border-slate-200/80 overflow-hidden flex flex-col p-6 animate-scale-up">
            <h3 className="font-black text-lg text-slate-900 flex items-center gap-2">
              Sao chép lịch tuần trước
            </h3>
            <p className="text-[13.5px] text-slate-500 mt-3 font-semibold leading-relaxed">
              {activeWeekSchedules.length > 0 ? (
                <>Tuần này đang có <strong>{activeWeekSchedules.length}</strong> ca. Sao chép tuần trước sẽ <strong>thay thế toàn bộ</strong> phân bổ hiện tại bằng lịch của tuần liền trước (dạng nháp). Bạn có chắc chắn không?</>
              ) : (
                <>Nạp lại toàn bộ lịch của tuần liền trước vào tuần này (dạng nháp). Bạn có chắc chắn muốn tiếp tục không?</>
              )}
            </p>
            <div className="flex justify-end gap-3 mt-6">
              <button
                onClick={() => setConfirmCopyOpen(false)}
                className="px-4 py-2 border border-slate-250 hover:bg-slate-100 text-slate-655 text-[13px] font-black rounded-lg transition-all cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={() => void doCopyPreviousWeek()}
                className="px-4 py-2 bg-primary hover:bg-primary-hover text-white text-[13px] font-black rounded-lg transition-all cursor-pointer"
              >
                Sao chép & thay thế
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
  useRequireOwner();
  return (
    <Suspense fallback={<div className="p-8 text-center font-bold text-slate-550">Đang tải cấu hình lịch làm việc...</div>}>
      <EditScheduleContent />
    </Suspense>
  );
}
