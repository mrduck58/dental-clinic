"use client";

import React, { useState, useMemo, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import { getWeekScheduleApi } from "../../../lib/apiClient";

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

const getThisWeekMonday = (): Date => {
  const today = new Date();
  const day = today.getDay();
  const diff = today.getDate() - day + (day === 0 ? -6 : 1);
  const monday = new Date(today);
  monday.setDate(diff);
  monday.setHours(0, 0, 0, 0);
  return monday;
};

const getISOWeek = (date: Date): number => {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  const dayNum = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
};

export default function SchedulePage() {
  useRequireAdmin();
  const [currentMonday, setCurrentMonday] = useState<Date>(getThisWeekMonday);
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

  const [scheduleData, setScheduleData] = useState<ScheduleEntry[]>([]);

  // Load schedule data from API whenever the displayed week changes
  useEffect(() => {
    getWeekScheduleApi(formatDateKey(currentMonday))
      .then(dtos => setScheduleData(dtos.map(dto => ({
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
      .catch(() => setScheduleData([]));
  }, [currentMonday]);

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

  // Stats computed from full week schedule data
  const weekStats = useMemo(() => {
    const activeDates = weekDates.map(d => formatDateKey(d));
    const weekEntries = scheduleData.filter(item => activeDates.includes(item.date) && !item.isHoliday);
    const dentists = new Set(weekEntries.filter(i => i.role === "dentist").map(i => i.name));
    const assistants = new Set(weekEntries.filter(i => i.role === "assistant").map(i => i.name));
    const staff = new Set(weekEntries.filter(i => i.role === "staff").map(i => i.name));
    const holidays = new Set(scheduleData.filter(i => activeDates.includes(i.date) && i.isHoliday).map(i => i.date));
    return {
      totalShifts: weekEntries.length,
      dentists: dentists.size,
      assistants: assistants.size,
      staff: staff.size,
      holidays: holidays.size,
    };
  }, [scheduleData, weekDates]);

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

  const isHighlightedDay = (date: Date) => {
    const today = new Date();
    return date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate();
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
      <AdminSidebar activeMenu="schedule" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-16 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          {/* Page title */}
          <div className="flex flex-col">
            <h1 className="text-[18px] font-black text-slate-900 leading-tight">Lịch làm việc</h1>
            <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">Quản lý ca trực và lịch làm việc nhân sự</p>
          </div>

          <div className="flex items-center gap-4">
            <NotificationBell />
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
          
          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-4 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng ca trong tuần</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{weekStats.totalShifts}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Bác sĩ trực tuần</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{weekStats.dentists}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Phụ tá / Nhân viên</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{weekStats.assistants + weekStats.staff}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-teal-50 text-teal-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Ngày nghỉ / Đóng cửa</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{weekStats.holidays}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
              </div>
            </div>
          </div>

          {/* CONTROLS CONTAINER */}
          <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex items-center justify-between gap-4 shrink-0 select-none flex-wrap">

            {/* Left: Week picker */}
            <div className="flex items-center gap-3">
              <button
                onClick={handlePrevWeek}
                className="w-8 h-8 flex items-center justify-center rounded-full border border-slate-200 text-slate-500 hover:text-slate-800 hover:bg-slate-100 hover:border-slate-300 transition-all cursor-pointer"
                title="Tuần trước"
              >
                ‹
              </button>

              <div className="text-center min-w-[168px]">
                <div className="text-[11.5px] font-bold text-slate-400 uppercase tracking-widest">
                  Tuần {getISOWeek(currentMonday)} · {currentMonday.getFullYear()}
                </div>
                <div className="text-[14.5px] font-black text-primary mt-0.5 whitespace-nowrap">
                  {formattedWeekRangeDates.monday} – {formattedWeekRangeDates.sunday}
                </div>
              </div>

              <button
                onClick={handleNextWeek}
                className="w-8 h-8 flex items-center justify-center rounded-full border border-slate-200 text-slate-500 hover:text-slate-800 hover:bg-slate-100 hover:border-slate-300 transition-all cursor-pointer"
                title="Tuần sau"
              >
                ›
              </button>
            </div>

            {/* Right: search + toggle + action buttons */}
            <div className="flex items-center gap-3 flex-wrap">

              {/* Search */}
              <div className="relative w-56">
                <input
                  type="text"
                  placeholder="Tìm kiếm bác sĩ, phòng..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-4 pr-4 py-2 text-[13.5px] bg-slate-100/80 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none focus:ring-1 focus:ring-slate-200 transition-all font-semibold"
                />
              </div>

              {/* Dentist/Staff toggle */}
              <div className="flex bg-slate-100 p-1 rounded-xl">
                <button
                  onClick={() => setStaffType("dentist")}
                  className={`px-4 py-2 rounded-lg text-[13px] font-bold transition-all cursor-pointer whitespace-nowrap ${staffType === "dentist" ? "bg-white text-primary shadow-sm" : "text-slate-500 hover:text-slate-800"}`}
                >
                  Nha sĩ
                </button>
                <button
                  onClick={() => setStaffType("staff")}
                  className={`px-4 py-2 rounded-lg text-[13px] font-bold transition-all cursor-pointer whitespace-nowrap ${staffType === "staff" ? "bg-white text-primary shadow-sm" : "text-slate-500 hover:text-slate-800"}`}
                >
                  Nhân viên
                </button>
              </div>

              {/* Export */}
              <button
                onClick={handleExport}
                className="flex items-center gap-2 px-4 py-2.5 bg-white hover:bg-slate-50 text-slate-600 hover:text-slate-900 text-[13px] font-bold border border-slate-200 rounded-xl transition-all shadow-sm cursor-pointer whitespace-nowrap"
              >
                Xuất file CSV
              </button>

              {/* Edit */}
              <Link
                href={`/dashboard/schedule/edit?week=${formatDateKey(currentMonday)}`}
                className="flex items-center gap-2 px-4 py-2.5 bg-white hover:bg-red-50/50 text-primary text-[13px] font-bold border border-primary/60 rounded-xl transition-all shadow-sm cursor-pointer whitespace-nowrap"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                </svg>
                Chỉnh sửa
              </Link>

              {/* Create new week */}
              <Link
                href={`/dashboard/schedule/edit?week=${formatDateKey(currentMonday)}`}
                className="flex items-center gap-2 px-4 py-2.5 bg-primary hover:bg-primary-hover text-white text-[13px] font-bold rounded-xl shadow-md shadow-primary/15 transition-all cursor-pointer whitespace-nowrap"
              >
                <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
                Tạo lịch tuần mới
              </Link>
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
                      const daysVN = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật"];
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
