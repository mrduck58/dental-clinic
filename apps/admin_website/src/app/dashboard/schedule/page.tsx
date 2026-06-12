"use client";

import React, { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import Sidebar from "../../../components/shared/Sidebar";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";

// Define TypeScript interfaces for our scheduling data
interface ScheduleEntry {
  id: string;
  date: string; // format YYYY-MM-DD
  shift: "morning" | "afternoon";
  type: "dentist" | "staff";
  role: "dentist" | "assistant" | "staff";
  name: string;
  room: string;
  roomColor: string; // custom room indicator color
  isHoliday?: boolean;
}

// Helper to format Date objects as YYYY-MM-DD
const formatDateKey = (date: Date): string => {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, "0");
  const dd = String(date.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
};

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

// Start week as Oct 16, 2023 to match screenshot out-of-the-box
const DEFAULT_MONDAY = new Date("2023-10-16");

export default function SchedulePage() {
  useRequireAdmin();
  const [currentMonday, setCurrentMonday] = useState<Date>(DEFAULT_MONDAY);
  const [staffType, setStaffType] = useState<"dentist" | "staff">("dentist");
  const [searchQuery, setSearchQuery] = useState("");
  
  // Custom toast notifications for action feedback
  const [notification, setNotification] = useState<{ message: string; type: "success" | "info" } | null>(null);

  const showNotification = (message: string, type: "success" | "info" = "success") => {
    setNotification({ message, type });
    setTimeout(() => {
      setNotification(null);
    }, 3500);
  };

  // Default mock database of schedules
  const DEFAULT_SCHEDULES = useMemo<ScheduleEntry[]>(() => [
    // WEEK 1 (16/10/2023 - 22/10/2023) - Dentist
    { id: "S-001", date: "2023-10-16", shift: "morning", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-002", date: "2023-10-16", shift: "morning", type: "dentist", role: "dentist", name: "BS. Tuấn Kiệt", room: "PHÒNG PHẪU THUẬT", roomColor: "border-accent" },
    { id: "S-003", date: "2023-10-16", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Hoàng My", room: "PHÒNG 2", roomColor: "border-secondary" },
    
    { id: "S-004", date: "2023-10-17", shift: "morning", type: "dentist", role: "dentist", name: "BS. Hoàng My", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-005", date: "2023-10-17", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-006", date: "2023-10-17", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Đăng Trần", room: "PHÒNG 4", roomColor: "border-emerald-600" },
    
    { id: "S-007", date: "2023-10-18", shift: "morning", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-008", date: "2023-10-18", shift: "morning", type: "dentist", role: "dentist", name: "BS. Quốc Bảo", room: "PHÒNG 3", roomColor: "border-amber-600" },
    { id: "S-009", date: "2023-10-18", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Hoàng My", room: "PHÒNG 2", roomColor: "border-secondary" },
    
    { id: "S-010", date: "2023-10-19", shift: "morning", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-011", date: "2023-10-19", shift: "afternoon", type: "dentist", role: "dentist", name: "Nghỉ lễ", room: "", roomColor: "", isHoliday: true },
    
    { id: "S-012", date: "2023-10-20", shift: "morning", type: "dentist", role: "dentist", name: "BS. Hoàng My", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-013", date: "2023-10-20", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },

    // WEEK 1 (16/10/2023 - 22/10/2023) - Staff
    { id: "S-101", date: "2023-10-16", shift: "morning", type: "staff", role: "staff", name: "Nguyễn Thị Lan", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-102", date: "2023-10-16", shift: "morning", type: "staff", role: "staff", name: "Trần Văn Hải", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-103", date: "2023-10-16", shift: "afternoon", type: "staff", role: "staff", name: "Lê Hoàng Long", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-104", date: "2023-10-17", shift: "morning", type: "staff", role: "staff", name: "Nguyễn Thị Lan", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-105", date: "2023-10-17", shift: "afternoon", type: "staff", role: "staff", name: "Trần Văn Hải", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-106", date: "2023-10-18", shift: "morning", type: "staff", role: "staff", name: "Lê Hoàng Long", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-107", date: "2023-10-18", shift: "morning", type: "staff", role: "staff", name: "Phan Mỹ Linh", room: "CSKH", roomColor: "border-teal-600" },
    { id: "S-108", date: "2023-10-18", shift: "afternoon", type: "staff", role: "staff", name: "Nguyễn Thị Lan", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-109", date: "2023-10-19", shift: "morning", type: "staff", role: "staff", name: "Trần Văn Hải", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-110", date: "2023-10-19", shift: "afternoon", type: "staff", role: "staff", name: "Nghỉ lễ", room: "", roomColor: "", isHoliday: true },
    { id: "S-111", date: "2023-10-20", shift: "morning", type: "staff", role: "staff", name: "Lê Hoàng Long", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-112", date: "2023-10-20", shift: "afternoon", type: "staff", role: "staff", name: "Đỗ Thu Hà", room: "CSKH", roomColor: "border-teal-600" },

    // WEEK 2 (23/10/2023 - 29/10/2023) - Dentist (Demonstrating week change updates)
    { id: "S-020", date: "2023-10-23", shift: "morning", type: "dentist", role: "dentist", name: "BS. Hoàng My", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-021", date: "2023-10-23", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-022", date: "2023-10-24", shift: "morning", type: "dentist", role: "dentist", name: "BS. Tuấn Kiệt", room: "PHÒNG PHẪU THUẬT", roomColor: "border-accent" },
    { id: "S-023", date: "2023-10-25", shift: "morning", type: "dentist", role: "dentist", name: "BS. Đăng Trần", room: "PHÒNG 4", roomColor: "border-emerald-600" },
    { id: "S-024", date: "2023-10-25", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Quốc Bảo", room: "PHÒNG 3", roomColor: "border-amber-600" },
    { id: "S-025", date: "2023-10-26", shift: "morning", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-026", date: "2023-10-27", shift: "morning", type: "dentist", role: "dentist", name: "BS. Hoàng My", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-027", date: "2023-10-27", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Tuấn Kiệt", room: "PHÒNG PHẪU THUẬT", roomColor: "border-accent" },

    // WEEK 24 (12/06/2024 - 18/06/2024) - Dentist & Assistant cells
    // Morning shift Room 1
    { id: "S-2401", date: "2024-06-12", shift: "morning", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2402", date: "2024-06-12", shift: "morning", type: "dentist", role: "assistant", name: "PT. Vân Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2403", date: "2024-06-14", shift: "morning", type: "dentist", role: "dentist", name: "BS. Hoàng Nam", room: "PHÒNG 1", roomColor: "border-accent" },
    { id: "S-2404", date: "2024-06-14", shift: "morning", type: "dentist", role: "assistant", name: "PT. Bảo Nam", room: "PHÒNG 1", roomColor: "border-accent" },
    { id: "S-2405", date: "2024-06-15", shift: "morning", type: "dentist", role: "dentist", name: "BS. Lan Chi", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2406", date: "2024-06-15", shift: "morning", type: "dentist", role: "assistant", name: "PT. Khánh Vy", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2407", date: "2024-06-17", shift: "morning", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2408", date: "2024-06-17", shift: "morning", type: "dentist", role: "assistant", name: "PT. Vân Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2409", date: "2024-06-18", shift: "morning", type: "dentist", role: "dentist", name: "BS. Hoàng My", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2410", date: "2024-06-18", shift: "morning", type: "dentist", role: "assistant", name: "PT. Vân Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    
    // Morning shift Room 2
    { id: "S-2411", date: "2024-06-13", shift: "morning", type: "dentist", role: "dentist", name: "BS. Quốc Huy", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-2412", date: "2024-06-13", shift: "morning", type: "dentist", role: "assistant", name: "PT. Hồng Đăng", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-2413", date: "2024-06-15", shift: "morning", type: "dentist", role: "dentist", name: "BS. Lan Chi", room: "PHÒNG 2", roomColor: "border-primary" },
    { id: "S-2414", date: "2024-06-15", shift: "morning", type: "dentist", role: "assistant", name: "PT. Thu Trà", room: "PHÒNG 2", roomColor: "border-primary" },
    { id: "S-2415", date: "2024-06-16", shift: "morning", type: "dentist", role: "dentist", name: "BS. Quốc Huy", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-2416", date: "2024-06-16", shift: "morning", type: "dentist", role: "assistant", name: "PT. Hồng Đăng", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-2417", date: "2024-06-18", shift: "morning", type: "dentist", role: "dentist", name: "BS. Quốc Bảo", room: "PHÒNG 2", roomColor: "border-secondary" },
    { id: "S-2418", date: "2024-06-18", shift: "morning", type: "dentist", role: "assistant", name: "PT. Bảo Nam", room: "PHÒNG 2", roomColor: "border-secondary" },

    // Afternoon shift Room 1
    { id: "S-2419", date: "2024-06-12", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Lan Chi", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2420", date: "2024-06-12", shift: "afternoon", type: "dentist", role: "assistant", name: "PT. Khánh Vy", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2421", date: "2024-06-14", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2422", date: "2024-06-14", shift: "afternoon", type: "dentist", role: "assistant", name: "PT. Vân Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2423", date: "2024-06-15", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Hoàng Nam", room: "PHÒNG 1", roomColor: "border-accent" },
    { id: "S-2424", date: "2024-06-15", shift: "afternoon", type: "dentist", role: "assistant", name: "PT. Bảo Nam", room: "PHÒNG 1", roomColor: "border-accent" },
    { id: "S-2425", date: "2024-06-16", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Minh Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2426", date: "2024-06-16", shift: "afternoon", type: "dentist", role: "assistant", name: "PT. Vân Anh", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2427", date: "2024-06-18", shift: "afternoon", type: "dentist", role: "dentist", name: "BS. Tuấn Kiệt", room: "PHÒNG 1", roomColor: "border-primary" },
    { id: "S-2428", date: "2024-06-18", shift: "afternoon", type: "dentist", role: "assistant", name: "PT. Khánh Vy", room: "PHÒNG 1", roomColor: "border-primary" },

    // WEEK 24 Staff (simplified Lễ tân, CSKH, Kế toán)
    // Lễ tân
    { id: "S-2450", date: "2024-06-12", shift: "morning", type: "staff", role: "staff", name: "Nguyễn Thị Lan", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-2451", date: "2024-06-13", shift: "morning", type: "staff", role: "staff", name: "Lê Hoàng Long", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-2452", date: "2024-06-14", shift: "morning", type: "staff", role: "staff", name: "Nguyễn Thị Lan", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-2453", date: "2024-06-15", shift: "morning", type: "staff", role: "staff", name: "Lê Hoàng Long", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-2454", date: "2024-06-16", shift: "morning", type: "staff", role: "staff", name: "Nguyễn Thị Lan", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-2455", date: "2024-06-17", shift: "morning", type: "staff", role: "staff", name: "Lê Hoàng Long", room: "LỄ TÂN", roomColor: "border-green-600" },
    { id: "S-2456", date: "2024-06-18", shift: "morning", type: "staff", role: "staff", name: "Nguyễn Thị Lan", room: "LỄ TÂN", roomColor: "border-green-600" },
    // CSKH
    { id: "S-2460", date: "2024-06-12", shift: "morning", type: "staff", role: "staff", name: "Phan Mỹ Linh", room: "CSKH", roomColor: "border-teal-600" },
    { id: "S-2461", date: "2024-06-13", shift: "morning", type: "staff", role: "staff", name: "Đỗ Thu Hà", room: "CSKH", roomColor: "border-teal-600" },
    { id: "S-2462", date: "2024-06-14", shift: "morning", type: "staff", role: "staff", name: "Phan Mỹ Linh", room: "CSKH", roomColor: "border-teal-600" },
    { id: "S-2463", date: "2024-06-15", shift: "morning", type: "staff", role: "staff", name: "Đỗ Thu Hà", room: "CSKH", roomColor: "border-teal-600" },
    { id: "S-2464", date: "2024-06-16", shift: "morning", type: "staff", role: "staff", name: "Phan Mỹ Linh", room: "CSKH", roomColor: "border-teal-600" },
    { id: "S-2465", date: "2024-06-17", shift: "morning", type: "staff", role: "staff", name: "Đỗ Thu Hà", room: "CSKH", roomColor: "border-teal-600" },
    { id: "S-2466", date: "2024-06-18", shift: "morning", type: "staff", role: "staff", name: "Phan Mỹ Linh", room: "CSKH", roomColor: "border-teal-600" },
    // Kế toán
    { id: "S-2470", date: "2024-06-12", shift: "morning", type: "staff", role: "staff", name: "Trần Văn Hải", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-2471", date: "2024-06-13", shift: "morning", type: "staff", role: "staff", name: "Vũ Minh Thư", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-2472", date: "2024-06-14", shift: "morning", type: "staff", role: "staff", name: "Trần Văn Hải", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-2473", date: "2024-06-15", shift: "morning", type: "staff", role: "staff", name: "Vũ Minh Thư", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-2474", date: "2024-06-16", shift: "morning", type: "staff", role: "staff", name: "Trần Văn Hải", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-2475", date: "2024-06-17", shift: "morning", type: "staff", role: "staff", name: "Vũ Minh Thư", room: "KẾ TOÁN", roomColor: "border-indigo-600" },
    { id: "S-2476", date: "2024-06-18", shift: "morning", type: "staff", role: "staff", name: "Trần Văn Hải", room: "KẾ TOÁN", roomColor: "border-indigo-600" }
  ], []);

  const [scheduleData, setScheduleData] = useState<ScheduleEntry[]>([]);

  // Load schedule data on mount
  useEffect(() => {
    const saved = localStorage.getItem("dental_clinic_schedules");
    if (saved) {
      try {
        setScheduleData(JSON.parse(saved));
      } catch (e) {
        setScheduleData(DEFAULT_SCHEDULES);
      }
    } else {
      localStorage.setItem("dental_clinic_schedules", JSON.stringify(DEFAULT_SCHEDULES));
      setScheduleData(DEFAULT_SCHEDULES);
    }
  }, [DEFAULT_SCHEDULES]);

  // Compute active week dates
  const weekDates = useMemo(() => getWeekDates(currentMonday), [currentMonday]);

  // Format string for week range header: "DD/MM/YYYY - DD/MM/YYYY"
  const formattedWeekRange = useMemo(() => {
    const monday = weekDates[0];
    const sunday = weekDates[6];
    
    const pad = (n: number) => String(n).padStart(2, "0");
    const mondayStr = `${pad(monday.getDate())}/${pad(monday.getMonth() + 1)}/${monday.getFullYear()}`;
    const sundayStr = `${pad(sunday.getDate())}/${pad(sunday.getMonth() + 1)}/${sunday.getFullYear()}`;
    
    return `${mondayStr} - ${sundayStr}`;
  }, [weekDates]);

  // Specific formatted values for the two-line calendar header
  const formattedWeekRangeDates = useMemo(() => {
    const monday = weekDates[0];
    const sunday = weekDates[6];
    
    const pad = (n: number) => String(n).padStart(2, "0");
    const mondayStr = `${pad(monday.getDate())}/${pad(monday.getMonth() + 1)}/${monday.getFullYear()}`;
    const sundayStr = `${pad(sunday.getDate())}/${pad(sunday.getMonth() + 1)}/${sunday.getFullYear()}`;
    
    return { monday: mondayStr, sunday: sundayStr };
  }, [weekDates]);

  // Navigation handlers
  const handlePrevWeek = () => {
    const newMonday = new Date(currentMonday);
    newMonday.setDate(currentMonday.getDate() - 7);
    setCurrentMonday(newMonday);
  };

  const handleNextWeek = () => {
    const newMonday = new Date(currentMonday);
    newMonday.setDate(currentMonday.getDate() + 7);
    setCurrentMonday(newMonday);
  };

  const handleToday = () => {
    // Navigate back to the default week defined in mock database
    setCurrentMonday(DEFAULT_MONDAY);
  };

  // Filter schedules based on: active date range, staff type, and search query
  const filteredSchedule = useMemo(() => {
    const activeDates = weekDates.map(d => formatDateKey(d));
    return scheduleData.filter(item => {
      const isCorrectType = item.type === staffType;
      const isInWeek = activeDates.includes(item.date);
      const matchesSearch = searchQuery === "" || 
        item.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.room.toLowerCase().includes(searchQuery.toLowerCase());
      
      return isCorrectType && isInWeek && matchesSearch;
    });
  }, [scheduleData, weekDates, staffType, searchQuery]);

  // Map filtered items into a lookup map by date and shift
  const scheduleLookup = useMemo(() => {
    const map: Record<string, ScheduleEntry[]> = {};
    filteredSchedule.forEach(item => {
      const key = `${item.date}_${item.shift}`;
      if (!map[key]) {
        map[key] = [];
      }
      map[key].push(item);
    });
    return map;
  }, [filteredSchedule]);

  // Checks if a date matches the calendar highlight from mockup
  // The mockup highlights T4 (Wednesday, Oct 18, 2023)
  const isHighlightedDay = (date: Date) => {
    return date.getDate() === 18 && date.getMonth() === 9 && date.getFullYear() === 2023;
  };

  // CSV Export action
  const handleExport = () => {
    // Generate simple CSV content
    const headers = "Ngay,Ca,Loai,Nhan Vien,Phong Ban\n";
    const rows = filteredSchedule.map(item => {
      return `"${item.date}","${item.shift === "morning" ? "Ca Sang" : "Ca Chieu"}","${item.type === "dentist" ? "Nha si" : "Nhan vien"}","${item.name}","${item.room}"`;
    }).join("\n");
    
    const csvContent = "data:text/csv;charset=utf-8,\uFEFF" + encodeURIComponent(headers + rows);
    
    // Create hidden download trigger
    const link = document.createElement("a");
    link.setAttribute("href", csvContent);
    link.setAttribute("download", `Lich_Lam_Viec_${formattedWeekRange.replace(/[\/\s]/g, "")}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    showNotification("Đã xuất file lịch làm việc thành công dưới dạng CSV!");
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      
      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <Sidebar activeMenu="schedule" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          {/* Global Search box in header matching general style */}
          <div className="relative w-96 hidden sm:block">
            <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input
              type="text"
              placeholder="Tìm kiếm bác sĩ, ca làm..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-11 pr-5 py-2.5 text-[14.5px] bg-slate-100/80 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none focus:ring-1 focus:ring-slate-200 transition-all font-semibold"
            />
          </div>

          {/* Profile Panel */}
          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2 text-slate-400 relative p-1.5 rounded-full hover:bg-slate-100 transition-all cursor-pointer">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
              </svg>
              <span className="absolute top-1 right-1 w-2.5 h-2.5 bg-primary rounded-full border border-white"></span>
            </div>
            
            <div className="flex items-center gap-2.5 text-slate-450 hover:text-slate-700 cursor-pointer transition-all">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z" />
              </svg>
            </div>

            <div className="h-6 w-px bg-slate-200"></div>

            <div className="flex items-center gap-3 select-none">
              <div className="text-right">
                <div className="text-[14px] font-black text-slate-900 leading-tight">Admin Clinic</div>
                <div className="text-[12px] font-bold text-slate-450 mt-0.5">Hồ Chí Minh</div>
              </div>
              <div className="w-10 h-10 rounded-full border-2 border-primary/20 bg-red-50 flex items-center justify-center font-bold text-primary shrink-0 text-sm">
                AD
              </div>
            </div>
          </div>
        </header>

        {/* NOTIFICATION TOAST */}
        {notification && (
          <div className="fixed top-24 right-8 z-55 bg-slate-900 text-white px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border border-slate-800 animate-fade-in font-bold text-[14px]">
            <span className="text-emerald-500 text-lg">✓</span>
            {notification.message}
          </div>
        )}

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          
          {/* TITLE & CONTROLS CONTAINER */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-5 shrink-0 select-none">
            
            {/* Title and Week Picker */}
            <div className="flex flex-col text-left">
              <span className="text-[14px] text-slate-500 font-medium tracking-tight">Lịch làm việc</span>
              
              {/* Week selector bar */}
              <div className="flex items-center gap-3.5 mt-1.5 select-none">
                <button
                  onClick={handlePrevWeek}
                  className="text-slate-400 hover:text-slate-800 transition-all cursor-pointer font-black text-lg p-1.5 hover:bg-slate-50 rounded"
                  title="Tuần trước"
                >
                  &lt;
                </button>
                
                <div className="text-[14.5px] font-black text-primary leading-tight w-24 text-center">
                  <div>{formattedWeekRangeDates.monday} -</div>
                  <div>{formattedWeekRangeDates.sunday}</div>
                </div>

                <button
                  onClick={handleNextWeek}
                  className="text-slate-400 hover:text-slate-800 transition-all cursor-pointer font-black text-lg p-1.5 hover:bg-slate-50 rounded"
                  title="Tuần sau"
                >
                  &gt;
                </button>
              </div>
            </div>

            {/* Filtering tabs (Nha sĩ / Nhân viên) & Main Actions */}
            <div className="flex flex-wrap items-center gap-4.5">
              
              {/* Dentist/Staff Toggle (styled to match tabs mockup) */}
              <div className="flex bg-[#eef2f6] p-1 rounded-xl border border-slate-200/20">
                <button
                  onClick={() => { setStaffType("dentist"); }}
                  className={`w-20 py-2.5 rounded-lg text-[13.5px] font-black transition-all cursor-pointer flex flex-col items-center justify-center leading-none ${staffType === "dentist" ? "bg-white text-primary shadow-sm border border-slate-200/20" : "text-slate-500 hover:text-slate-800"}`}
                >
                  <span>Nha</span>
                  <span className="mt-0.5">sĩ</span>
                </button>
                <button
                  onClick={() => { setStaffType("staff"); }}
                  className={`w-20 py-2.5 rounded-lg text-[13.5px] font-black transition-all cursor-pointer flex flex-col items-center justify-center leading-none ${staffType === "staff" ? "bg-white text-primary shadow-sm border border-slate-200/20" : "text-slate-500 hover:text-slate-800"}`}
                >
                  <span>Nhân</span>
                  <span className="mt-0.5">viên</span>
                </button>
              </div>

              {/* Action buttons */}
              <div className="flex flex-wrap items-center gap-3.5">
                
                {/* Export */}
                <button
                  onClick={handleExport}
                  className="flex items-center justify-center gap-2 px-4.5 w-32 h-14 bg-white hover:bg-slate-50 text-slate-500 hover:text-slate-800 text-[13px] font-black border border-slate-250 rounded-xl transition-all shadow-sm cursor-pointer leading-tight"
                >
                  <svg className="w-4.5 h-4.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
                  </svg>
                  <div className="flex flex-col items-start leading-none text-left shrink-0">
                    <span>Xuất file</span>
                    <span className="text-[11px] font-bold text-slate-400 mt-1">(Export)</span>
                  </div>
                </button>

                {/* Edit */}
                <Link
                  href={`/dashboard/schedule/edit?week=${formatDateKey(currentMonday)}`}
                  className="flex items-center justify-center gap-2 px-4 w-28 h-14 bg-white border border-primary hover:bg-red-50/45 text-primary text-[13px] font-black rounded-xl transition-all shadow-sm cursor-pointer leading-tight"
                >
                  <svg className="w-4.5 h-4.5 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                  </svg>
                  <div className="flex flex-col items-start leading-none text-left shrink-0">
                    <span>Chỉnh</span>
                    <span className="mt-1">sửa</span>
                  </div>
                </Link>

                {/* Create New Week Schedule */}
                <button
                  onClick={() => showNotification("Chức năng tạo lịch tuần mới sẽ được kích hoạt ở phiên bản tiếp theo.", "info")}
                  className="flex items-center justify-center gap-2 px-4 w-36 h-14 bg-primary hover:bg-primary-hover text-white text-[13px] font-black rounded-xl shadow-md shadow-primary/15 transition-all cursor-pointer leading-tight"
                >
                  <svg className="w-4.5 h-4.5 text-white shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008z" />
                  </svg>
                  <div className="flex flex-col items-start leading-none text-left shrink-0">
                    <span>Tạo lịch tuần</span>
                    <span className="mt-1">mới</span>
                  </div>
                </button>
              </div>
            </div>

          </div>

          {/* MAIN CALENDAR GRID */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[950px] table-fixed">
                <thead>
                  <tr className="bg-slate-50/60 font-bold border-b border-slate-200 select-none">
                    
                    {/* Shift name column header */}
                    <th className="px-5 py-5 text-slate-500 font-extrabold text-[14px] w-[140px] text-center border-r border-slate-200/80">
                      Ca trực
                    </th>

                    {/* Weekdays columns headers */}
                    {weekDates.map((date, idx) => {
                      const daysVN = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
                      const formattedDay = daysVN[idx];
                      const formattedDate = `${date.getDate()}/${date.getMonth() + 1}`;
                      const isHighlighted = isHighlightedDay(date);

                      return (
                        <th 
                          key={idx} 
                          className={`px-4 py-5 text-center border-r border-slate-200/80 last:border-r-0 ${isHighlighted ? "bg-red-50/50" : ""}`}
                        >
                          <div className={`text-[15px] font-black tracking-tight ${isHighlighted ? "text-primary" : "text-slate-800"}`}>
                            {formattedDay}
                          </div>
                          <div className={`text-[12.5px] font-semibold mt-1 ${isHighlighted ? "text-primary" : "text-slate-400"}`}>
                            {formattedDate}
                          </div>
                        </th>
                      );
                    })}

                  </tr>
                </thead>

                <tbody className="divide-y divide-slate-200/80">
                  
                  {/* MORNING SHIFT ROW */}
                  <tr className="min-h-[160px]">
                    {/* Shift Label */}
                    <td className="px-4 py-6 text-center border-r border-slate-200/80 font-bold bg-slate-50/30">
                      <div className="text-[12px] font-bold text-slate-400 tracking-wider">(08:00 - 12:00)</div>
                      <div className="text-[14px] font-black text-slate-700 mt-1 uppercase">Ca Sáng</div>
                    </td>

                    {/* Days Slots */}
                    {weekDates.map((date, dayIdx) => {
                      const dateKey = formatDateKey(date);
                      const key = `${dateKey}_morning`;
                      const cellItems = scheduleLookup[key] || [];
                      const isHighlighted = isHighlightedDay(date);

                      return (
                        <td 
                          key={dayIdx} 
                          className={`px-3.5 py-4.5 align-top border-r border-slate-200/80 last:border-r-0 ${isHighlighted ? "bg-red-50/20" : ""}`}
                        >
                          <div className="flex flex-col gap-3.5 min-h-[110px]">
                            {cellItems.map((item) => {
                              if (item.isHoliday) {
                                return (
                                  <div
                                    key={item.id}
                                    className="bg-slate-100 border border-slate-200 p-3 rounded-lg flex items-center justify-center shadow-inner text-center flex-1"
                                  >
                                    <span className="text-[13.5px] font-black text-slate-400 uppercase tracking-widest">
                                      {item.name}
                                    </span>
                                  </div>
                                );
                              }

                              return (
                                <div
                                  key={item.id}
                                  className={`bg-slate-50 border-l-4 ${item.roomColor} p-2.5 rounded-lg shadow-sm hover-lift flex flex-col gap-1 transition-all border border-slate-200/70`}
                                >
                                  <div className="text-[10px] font-black text-slate-450 uppercase tracking-wide flex items-center justify-between">
                                    <span>{item.room}</span>
                                    {item.role === "assistant" && (
                                      <span className="text-teal-600 font-extrabold text-[8px] bg-teal-50 px-1 rounded">PHỤ TÁ</span>
                                    )}
                                  </div>
                                  <div className="text-[13.5px] font-extrabold text-slate-800">
                                    {item.name}
                                  </div>
                                </div>
                              );
                            })}

                            {/* Holiday or Off day state */}
                            {cellItems.length === 0 && (
                              <div className="flex-1 flex items-center justify-center">
                                <span className="text-[12.5px] font-semibold text-slate-350 italic">Không có ca</span>
                              </div>
                            )}

                            {/* + Thêm button matching mockup design */}
                            {!cellItems.some(i => i.isHoliday) && (
                              <button
                                onClick={() => showNotification("Tính năng bổ sung nhân sự vào ca đang phát triển.", "info")}
                                className="mt-auto py-1.5 border border-dashed border-slate-300 rounded-lg hover:border-primary hover:bg-red-50/25 transition-all text-slate-400 hover:text-primary text-[12px] font-bold flex items-center justify-center gap-1.5 cursor-pointer shadow-sm shadow-slate-100/50 bg-white"
                              >
                                <span>+ Thêm</span>
                              </button>
                            )}
                          </div>
                        </td>
                      );
                    })}
                  </tr>

                  {/* AFTERNOON SHIFT ROW */}
                  <tr className="min-h-[160px]">
                    {/* Shift Label */}
                    <td className="px-4 py-6 text-center border-r border-slate-200/80 font-bold bg-slate-50/30">
                      <div className="text-[12px] font-bold text-slate-400 tracking-wider">(13:30 - 17:30)</div>
                      <div className="text-[14px] font-black text-slate-700 mt-1 uppercase">Ca Chiều</div>
                    </td>

                    {/* Days Slots */}
                    {weekDates.map((date, dayIdx) => {
                      const dateKey = formatDateKey(date);
                      const key = `${dateKey}_afternoon`;
                      const cellItems = scheduleLookup[key] || [];
                      const isHighlighted = isHighlightedDay(date);

                      return (
                        <td 
                          key={dayIdx} 
                          className={`px-3.5 py-4.5 align-top border-r border-slate-200/80 last:border-r-0 ${isHighlighted ? "bg-red-50/20" : ""}`}
                        >
                          <div className="flex flex-col gap-3.5 min-h-[110px]">
                            {cellItems.map((item) => {
                              if (item.isHoliday) {
                                return (
                                  <div
                                    key={item.id}
                                    className="bg-slate-100 border border-slate-200 p-3 rounded-lg flex items-center justify-center shadow-inner text-center flex-1"
                                  >
                                    <span className="text-[13.5px] font-black text-slate-400 uppercase tracking-widest">
                                      {item.name}
                                    </span>
                                  </div>
                                );
                              }

                              return (
                                <div
                                  key={item.id}
                                  className={`bg-slate-50 border-l-4 ${item.roomColor} p-2.5 rounded-lg shadow-sm hover-lift flex flex-col gap-1 transition-all border border-slate-200/70`}
                                >
                                  <div className="text-[10px] font-black text-slate-450 uppercase tracking-wide flex items-center justify-between">
                                    <span>{item.room}</span>
                                    {item.role === "assistant" && (
                                      <span className="text-teal-600 font-extrabold text-[8px] bg-teal-50 px-1 rounded">PHỤ TÁ</span>
                                    )}
                                  </div>
                                  <div className="text-[13.5px] font-extrabold text-slate-800">
                                    {item.name}
                                  </div>
                                </div>
                              );
                            })}

                            {/* Holiday or Off day state */}
                            {cellItems.length === 0 && (
                              <div className="flex-1 flex items-center justify-center">
                                <span className="text-[12.5px] font-semibold text-slate-350 italic">Không có ca</span>
                              </div>
                            )}

                            {/* + Thêm button matching mockup design */}
                            {/* Do not show add button if it's already a Holiday block */}
                            {!cellItems.some(i => i.isHoliday) && (
                              <button
                                onClick={() => showNotification("Tính năng bổ sung nhân sự vào ca đang phát triển.", "info")}
                                className="mt-auto py-1.5 border border-dashed border-slate-300 rounded-lg hover:border-primary hover:bg-red-50/25 transition-all text-slate-400 hover:text-primary text-[12px] font-bold flex items-center justify-center gap-1.5 cursor-pointer shadow-sm shadow-slate-100/50 bg-white"
                              >
                                <span>+ Thêm</span>
                              </button>
                            )}
                          </div>
                        </td>
                      );
                    })}
                  </tr>

                </tbody>
              </table>
            </div>
          </div>
          {/* Removed business rules footer block */}

        </div>
      </main>

    </div>
  );
}
