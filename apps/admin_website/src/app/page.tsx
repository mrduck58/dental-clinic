"use client";

import React, { useState } from "react";

export default function Dashboard() {
  // State to track hover item in Donut Chart
  const [hoveredIndex, setHoveredIndex] = useState<number | null>(null);

  // States to track filter ranges
  const [statsRange, setStatsRange] = useState<"week" | "month" | "year">("week");
  const [chartRange, setChartRange] = useState<"week" | "month" | "year">("week");

  // Donut chart raw data by range
  const donutDataRaw = {
    week: [
      { name: "Niềng răng", value: 35, stroke: "rgb(220 38 38)" },
      { name: "Implant", value: 25, stroke: "rgb(2 132 199)" },
      { name: "Bọc răng sứ", value: 20, stroke: "rgb(245 158 11)" },
      { name: "Điều trị tủy", value: 12, stroke: "rgb(34 197 94)" },
      { name: "Dịch vụ khác", value: 8, stroke: "#94a3b8" }
    ],
    month: [
      { name: "Niềng răng", value: 30, stroke: "rgb(220 38 38)" },
      { name: "Implant", value: 30, stroke: "rgb(2 132 199)" },
      { name: "Bọc răng sứ", value: 18, stroke: "rgb(245 158 11)" },
      { name: "Điều trị tủy", value: 14, stroke: "rgb(34 197 94)" },
      { name: "Dịch vụ khác", value: 8, stroke: "#94a3b8" }
    ],
    year: [
      { name: "Niềng răng", value: 40, stroke: "rgb(220 38 38)" },
      { name: "Implant", value: 20, stroke: "rgb(2 132 199)" },
      { name: "Bọc răng sứ", value: 22, stroke: "rgb(245 158 11)" },
      { name: "Điều trị tủy", value: 10, stroke: "rgb(34 197 94)" },
      { name: "Dịch vụ khác", value: 8, stroke: "#94a3b8" }
    ]
  };

  // Compute segments with offsets dynamically
  const activeDonutRaw = donutDataRaw[statsRange];
  let tempOffset = 0;
  const segments = activeDonutRaw.map(item => {
    const seg = { ...item, offset: tempOffset };
    tempOffset -= item.value;
    return seg;
  });

  // Stats Grid Data depending on range
  const statsData = {
    week: [
      {
        name: "Bệnh nhân mới",
        val: "48",
        trend: "↑ 15%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "Đăng ký mới tuần này",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198.007.031c.003.01.005.02.008.029A9.091 9.091 0 0021 18.75m-2.785-5.365A3 3 0 1016.5 9.75M16.5 13.5A3 3 0 0016.5 9.75M9 13.5a3.75 3.75 0 110-7.5 3.75 3.75 0 010 7.5zM2.25 18.75a6.75 6.75 0 0113.5 0M9 13.5c-.394 0-.776-.03-1.147-.09a6.75 6.75 0 00-5.603 5.34" />
          </svg>
        )
      },
      {
        name: "Lịch hẹn hôm nay",
        val: "32",
        trend: "↓ 5%",
        trendColor: "text-primary",
        trendBg: "bg-red-50",
        desc: "Lịch khám đã đặt",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
          </svg>
        )
      },
      {
        name: "Doanh thu ngày",
        val: "45.2M",
        trend: "↑ 18%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "VNĐ thu trong hôm nay",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5z" />
          </svg>
        )
      }
    ],
    month: [
      {
        name: "Bệnh nhân mới",
        val: "192",
        trend: "↑ 22%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "Đăng ký mới tháng này",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198.007.031c.003.01.005.02.008.029A9.091 9.091 0 0021 18.75m-2.785-5.365A3 3 0 1016.5 9.75M16.5 13.5A3 3 0 0016.5 9.75M9 13.5a3.75 3.75 0 110-7.5 3.75 3.75 0 010 7.5zM2.25 18.75a6.75 6.75 0 0113.5 0M9 13.5c-.394 0-.776-.03-1.147-.09a6.75 6.75 0 00-5.603 5.34" />
          </svg>
        )
      },
      {
        name: "Tổng lịch hẹn tháng",
        val: "840",
        trend: "↑ 12%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "Tổng số ca khám tháng này",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
          </svg>
        )
      },
      {
        name: "Doanh thu tháng",
        val: "1.24B",
        trend: "↑ 14%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "Lũy kế tháng này",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5z" />
          </svg>
        )
      }
    ],
    year: [
      {
        name: "Bệnh nhân mới",
        val: "2,450",
        trend: "↑ 30%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "Đăng ký mới năm nay",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198.007.031c.003.01.005.02.008.029A9.091 9.091 0 0021 18.75m-2.785-5.365A3 3 0 1016.5 9.75M16.5 13.5A3 3 0 0016.5 9.75M9 13.5a3.75 3.75 0 110-7.5 3.75 3.75 0 010 7.5zM2.25 18.75a6.75 6.75 0 0113.5 0M9 13.5c-.394 0-.776-.03-1.147-.09a6.75 6.75 0 00-5.603 5.34" />
          </svg>
        )
      },
      {
        name: "Tổng lịch hẹn năm",
        val: "9,820",
        trend: "↑ 25%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "Tổng số ca khám năm nay",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
          </svg>
        )
      },
      {
        name: "Doanh thu năm",
        val: "14.8B",
        trend: "↑ 20%",
        trendColor: "text-green-600",
        trendBg: "bg-green-50",
        desc: "Lũy kế năm nay",
        icon: (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5z" />
          </svg>
        )
      }
    ]
  };

  // Bar Chart Data depending on range
  const chartData = {
    week: [
      { label: "T2", value: 18, height: "98px" },
      { label: "T3", value: 24, height: "130px" },
      { label: "T4", value: 32, height: "172px", active: true },
      { label: "T5", value: 28, height: "151px" },
      { label: "T6", value: 30, height: "162px" },
      { label: "T7", value: 15, height: "81px" },
      { label: "CN", value: 12, height: "64px" }
    ],
    month: [
      { label: "Tuần 1", value: 180, height: "110px" },
      { label: "Tuần 2", value: 210, height: "130px" },
      { label: "Tuần 3", value: 250, height: "155px" },
      { label: "Tuần 4", value: 290, height: "180px", active: true }
    ],
    year: [
      { label: "Th1", value: 320, height: "70px" },
      { label: "Th2", value: 280, height: "61px" },
      { label: "Th3", value: 390, height: "85px" },
      { label: "Th4", value: 410, height: "90px" },
      { label: "Th5", value: 450, height: "98px" },
      { label: "Th6", value: 480, height: "105px" },
      { label: "Th7", value: 350, height: "76px" },
      { label: "Th8", value: 390, height: "85px" },
      { label: "Th9", value: 420, height: "92px" },
      { label: "Th10", value: 460, height: "100px" },
      { label: "Th11", value: 490, height: "107px", active: true },
      { label: "Th12", value: 520, height: "114px" }
    ]
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">

      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <aside className="w-72 bg-white border-r border-slate-200 p-6 flex flex-col gap-6 shrink-0 sticky top-0 h-screen justify-between">
        <div className="flex flex-col gap-6 flex-1 min-h-0">
          <div className="flex items-center gap-3 px-2 py-2">
            <span className="text-3xl text-primary shrink-0 animate-pulse">🦷</span>
            <div className="flex flex-col">
              <span className="text-[12px] font-black tracking-widest text-primary uppercase leading-none mb-1">SơnGiang</span>
              <span className="font-extrabold text-xl tracking-tight text-slate-900 leading-none">
                Dental<span className="text-primary font-bold">Clinic</span>
              </span>
            </div>
          </div>

          <nav className="flex flex-col gap-1 overflow-y-auto pr-1 flex-1">
            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-bold bg-primary text-white shadow-md shadow-primary/25 transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 6a7.5 7.5 0 107.5 7.5h-7.5V6z" />
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 10.5H21A7.5 7.5 0 0013.5 3v7.5z" />
              </svg>
              Tổng quan
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
              </svg>
              Quản lí nhân viên
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 7.5h1.5m-1.5 3h1.5m-7.5 3h7.5m-7.5 3h7.5m3-9h3.375c.621 0 1.125.504 1.125 1.125V18a2.25 2.25 0 01-2.25 2.25M16.5 7.5V18a2.25 2.25 0 002.25 2.25M16.5 7.5V4.875c0-.621-.504-1.125-1.125-1.125H4.125C3.504 3.75 3 4.254 3 4.875V18a2.25 2.25 0 002.25 2.25h13.5M6 7.5h3v3H6v-3z" />
              </svg>
              Quản lí bài viết
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z" />
              </svg>
              Lịch làm việc nhân viên
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.751A11.956 11.956 0 0112 2.714z" />
              </svg>
              Tài khoản và phân quyền
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0012 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018z" />
              </svg>
              Phản hồi
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.83-5.83m0 0a2.95 2.95 0 11-4.174-4.172 2.95 2.95 0 014.174 4.172zm-7.42 7.42l9.39-9.39" />
              </svg>
              Quản lí dịch vụ
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z" />
              </svg>
              Đơn xin nghỉ
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3a1.5 1.5 0 011.5-1.5h3a1.5 1.5 0 011.5 1.5v3m-9-10.5h.75m-.75 3h.75m-.75 3h.75m9-6h.75m-.75 3h.75m-.75 3h.75" />
              </svg>
              Quản lí phòng
            </a>

            <a href="#" className="flex items-center gap-3.5 px-4 py-3 rounded-xl font-semibold text-slate-500 hover:bg-red-50 hover:text-primary transition-all">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Lịch sử hoạt động
            </a>
          </nav>
        </div>

        {/* Admin Profile & Logout at Sidebar Bottom */}
        <div className="border-t border-slate-100 pt-4 flex flex-col gap-3">
          <div className="flex items-center gap-3 px-2">
            <div className="w-10 h-10 rounded-full border-2 border-primary/20 bg-primary-light flex items-center justify-center font-bold text-primary select-none shrink-0">
              MĐ
            </div>
            <div className="min-w-0">
              <div className="text-[14px] font-bold text-slate-900 leading-tight truncate">ThS. BS. Nguyễn Minh Đức</div>
              <div className="text-[12px] font-semibold text-slate-400 mt-0.5">Quản trị viên</div>
            </div>
          </div>

          <button className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl text-[13px] font-bold text-slate-500 hover:text-primary hover:bg-red-50 border border-slate-100 hover:border-primary/20 transition-all cursor-pointer">
            <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
            </svg>
            Đăng xuất
          </button>
        </div>
      </aside>

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">

        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Tổng Quan Vận Hành</h1>
          </div>

          {/* Search, Notifications, Profile */}
          <div className="flex items-center gap-6">

            {/* Search Input */}
            <div className="relative w-64 hidden sm:block">
              <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Tìm kiếm nhanh..."
                className="w-full pl-9 pr-4 py-2 text-[15px] bg-slate-100 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all"
              />
            </div>

            {/* Notification Button */}
            <button className="relative p-2.5 rounded-full bg-slate-100 text-slate-600 hover:bg-red-50 hover:text-primary transition-all animate-ring-hover cursor-pointer">
              <svg className="w-5.5 h-5.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
              </svg>
              {/* Badge */}
              <span className="absolute top-1.5 right-1.5 w-3 h-3 bg-primary rounded-full border-2 border-white"></span>
            </button>



          </div>
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">

            {/* ── LEFT COLUMN (Stats, Bar Chart, Donut Chart & Table) ───────────── */}
            <div className="lg:col-span-8 flex flex-col gap-8 min-w-0">

              {/* ── STATS HEADER & GRID ────────────────────────────────────────── */}
              <section className="bg-white py-3.5 px-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5 shrink-0">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Báo cáo chỉ số chính</h3>
                    <p className="text-[14px] text-slate-400 mt-0.5 font-semibold">Thống kê bệnh nhân, lịch hẹn & doanh thu.</p>
                  </div>

                  {/* Range select buttons */}
                  <div className="flex bg-slate-100 p-1 rounded-xl self-start shrink-0">
                    <button
                      onClick={() => { setStatsRange("week"); setChartRange("week"); }}
                      className={`px-4 py-1.5 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${statsRange === "week" ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
                    >
                      Theo Tuần
                    </button>
                    <button
                      onClick={() => { setStatsRange("month"); setChartRange("month"); }}
                      className={`px-4 py-1.5 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${statsRange === "month" ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
                    >
                      Theo Tháng
                    </button>
                    <button
                      onClick={() => { setStatsRange("year"); setChartRange("year"); }}
                      className={`px-4 py-1.5 rounded-lg text-[13px] font-bold transition-all cursor-pointer ${statsRange === "year" ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900"}`}
                    >
                      Theo Năm
                    </button>
                  </div>
                </div>

                {/* 3-column Stats Grid */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {statsData[statsRange].map((stat, idx) => (
                    <div key={idx} className="bg-slate-50/50 py-3.5 px-4 rounded-xl border border-slate-200/60 shadow-sm hover-lift flex flex-col justify-between hover:border-primary/40 transition-all duration-200">
                      <div className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider mb-1">
                        {stat.name}
                      </div>

                      <div className="flex items-center justify-between gap-3 my-1">
                        <div className="flex items-baseline gap-1.5">
                          <span className="text-3xl font-black text-slate-900 leading-none">{stat.val}</span>
                          <span className={`inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded-full text-[11px] font-bold leading-none ${stat.trendBg} ${stat.trendColor}`}>
                            {stat.trend}
                          </span>
                        </div>

                        <span className="w-11 h-11 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                          {React.cloneElement(stat.icon, { className: "w-6.5 h-6.5" })}
                        </span>
                      </div>

                      <p className="text-[12px] text-slate-400 mt-1 font-semibold">{stat.desc}</p>
                    </div>
                  ))}
                </div>
              </section>

              {/* Row 1: Charts song song */}
              <div className="grid grid-cols-1 md:grid-cols-12 gap-6">

                {/* Lịch Hẹn - Bar Chart (Chiếm 7/12) */}
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:col-span-7 justify-between min-h-[320px] hover:border-primary/40 transition-all duration-200">
                  <div className="flex items-center justify-between mb-4">
                    <div>
                      <h3 className="text-[18px] font-extrabold text-slate-900">Thống Kê Lịch Hẹn</h3>
                      <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Số lượng cuộc hẹn khám tại hệ thống</p>
                    </div>
                  </div>

                  {/* Custom SVG/CSS Bar Chart with dynamic data */}
                  <div className="flex-1 flex flex-col justify-end pt-2">
                    <div className="flex items-end justify-between h-[160px] border-b border-slate-100 pb-1 px-2">
                      {chartData[chartRange].map((item, idx) => (
                        <div
                          key={idx}
                          className="flex flex-col items-center gap-2 group cursor-pointer"
                          style={{ width: chartRange === "year" ? "6%" : chartRange === "month" ? "18%" : "10%" }}
                        >
                          <div className="relative w-full flex justify-center">
                            <span className="absolute bottom-full mb-1 opacity-0 group-hover:opacity-100 bg-slate-900 text-white text-[11px] font-semibold py-0.5 px-1.5 rounded transition-opacity duration-200 pointer-events-none z-10 whitespace-nowrap">
                              {item.value}
                            </span>
                            <div
                              className={`w-full max-w-[28px] rounded-t transition-all ${item.active ? "bg-primary" : "bg-slate-200 group-hover:bg-primary/80"}`}
                              style={{ height: item.height }}
                            ></div>
                          </div>
                          <span className={`text-[11px] font-bold transition-all truncate max-w-full ${item.active ? "text-slate-950 font-black" : "text-slate-400 group-hover:text-slate-900"}`}>
                            {item.label}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Tỷ Lệ Sử Dụng Dịch Vụ - Donut Chart (Chiếm 5/12) */}
                <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:col-span-5 justify-between min-h-[320px]">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Tỷ Lệ Dịch Vụ</h3>
                    <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Tỷ lệ dịch vụ nha khoa</p>
                  </div>

                  <div className="flex-1 flex flex-col items-center justify-center gap-4 py-1 mt-2">
                    {/* Donut Chart SVG */}
                    <div className="relative w-48 h-48 shrink-0">
                      <svg className="w-full h-full transform -rotate-90 overflow-visible" viewBox="0 0 40 40">
                        {/* Grey Background Ring */}
                        <circle
                          cx="20"
                          cy="20"
                          r="13"
                          fill="none"
                          stroke="#f1f5f9"
                          strokeWidth="6"
                        />
                        {/* Render Normal segments */}
                        {segments.map((segment, idx) => (
                          <circle
                            key={idx}
                            cx="20"
                            cy="20"
                            r="13"
                            fill="none"
                            stroke={segment.stroke}
                            strokeWidth="6"
                            strokeDasharray={`${segment.value} ${100 - segment.value}`}
                            strokeDashoffset={segment.offset}
                            className="transition-all duration-300 cursor-pointer origin-center hover:scale-[1.05]"
                            style={{ transformOrigin: "center" }}
                            onMouseEnter={() => setHoveredIndex(idx)}
                            onMouseLeave={() => setHoveredIndex(null)}
                          />
                        ))}
                        {/* Render Hovered segment on top of everything to fix border cover bug */}
                        {hoveredIndex !== null && (
                          <circle
                            cx="20"
                            cy="20"
                            r="13"
                            fill="none"
                            stroke={segments[hoveredIndex].stroke}
                            strokeWidth="7.5"
                            strokeDasharray={`${segments[hoveredIndex].value} ${100 - segments[hoveredIndex].value}`}
                            strokeDashoffset={segments[hoveredIndex].offset}
                            className="origin-center pointer-events-none transition-all duration-200 scale-[1.05]"
                            style={{
                              transformOrigin: "center",
                            }}
                          />
                        )}
                      </svg>

                      {/* Interactive Center Content */}
                      <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-2 pointer-events-none">
                        {hoveredIndex !== null ? (
                          <>
                            <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wide truncate max-w-[70px]">
                              {segments[hoveredIndex].name}
                            </span>
                            <span className="text-lg font-black mt-0.5 leading-none" style={{ color: segments[hoveredIndex].stroke }}>
                              {segments[hoveredIndex].value}%
                            </span>
                          </>
                        ) : (
                          <>
                            <span className="text-[12px] font-bold text-slate-400 uppercase tracking-widest leading-none">Cơ cấu</span>
                            <span className="text-[12px] font-bold text-slate-500 mt-0.5">Dịch vụ</span>
                          </>
                        )}
                      </div>
                    </div>

                    {/* Custom color legends (placed below) */}
                    <div className="flex flex-wrap items-center justify-center gap-x-4 gap-y-2 w-full text-[13px] font-bold text-slate-500 mt-1">
                      {segments.map((segment, idx) => (
                        <div
                          key={idx}
                          className={`flex items-center gap-1.5 transition-all cursor-pointer ${hoveredIndex === idx ? "text-slate-900 scale-105" : "text-slate-500"}`}
                          onMouseEnter={() => setHoveredIndex(idx)}
                          onMouseLeave={() => setHoveredIndex(null)}
                        >
                          <span className="w-2 h-2 rounded-full shrink-0" style={{ backgroundColor: segment.stroke }}></span>
                          <span className="truncate">{segment.name} ({segment.value}%)</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

              </div>

              {/* Row 2: Danh Sách Lịch Hẹn Hôm Nay (Full-width) */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
                <div className="p-4.5 border-b border-slate-100 flex justify-between items-center">
                  <div>
                    <h3 className="text-[18px] font-extrabold text-slate-900">Lịch Hẹn Hôm Nay</h3>
                    <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Danh sách các cuộc hẹn đăng ký khám chữa bệnh trong ngày hôm nay</p>
                  </div>
                  <button className="flex items-center gap-1.5 bg-slate-50 hover:bg-slate-100 text-slate-700 hover:text-slate-900 border border-slate-200 text-[12px] font-bold px-3.5 py-1.5 rounded-xl transition-all hover:translate-y-[-1px] cursor-pointer">
                    <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 6h9.75M10.5 6a1.5 1.5 0 11-3 0m3 0a1.5 1.5 0 10-3 0M3.75 6H7.5m3 12h9.75m-9.75 0a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m-3.75 0H7.5m9-6h3.75m-3.75 0a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m-9.75 0h9.75" />
                    </svg>
                    Bộ lọc & Sắp xếp
                  </button>
                </div>

                <div className="overflow-x-auto flex-1">
                  <table className="w-full text-left border-collapse text-[13px]">
                    <thead>
                      <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-100">
                        <th className="px-5 py-3">Giờ khám</th>
                        <th className="px-5 py-3">Họ và tên bệnh nhân</th>
                        <th className="px-5 py-3">Dịch vụ điều trị</th>
                        <th className="px-5 py-3">Trạng thái</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                      <tr className="hover:bg-slate-50/30 transition-colors">
                        <td className="px-5 py-3 font-bold text-slate-950">09:00</td>
                        <td className="px-5 py-3 font-bold text-slate-800">Nguyễn Văn A</td>
                        <td className="px-5 py-3 text-slate-500 font-medium">Cấy ghép Implant</td>
                        <td className="px-5 py-3">
                          <span className="inline-flex px-2 py-0.5 rounded-full text-[12px] font-bold bg-green-50 text-green-600">Đã hoàn thành</span>
                        </td>
                      </tr>

                      <tr className="hover:bg-slate-50/30 transition-colors">
                        <td className="px-5 py-3 font-bold text-slate-950">10:00</td>
                        <td className="px-5 py-3 font-bold text-slate-800">Trần Thị B</td>
                        <td className="px-5 py-3 text-slate-500 font-medium">Niềng răng Invisalign</td>
                        <td className="px-5 py-3">
                          <span className="inline-flex px-2 py-0.5 rounded-full text-[12px] font-bold bg-red-50 text-primary">Đang khám</span>
                        </td>
                      </tr>

                      <tr className="hover:bg-slate-50/30 transition-colors">
                        <td className="px-5 py-3 font-bold text-slate-950">13:30</td>
                        <td className="px-5 py-3 font-bold text-slate-800">Phạm Văn C</td>
                        <td className="px-5 py-3 text-slate-500 font-medium">Bọc sứ Emax</td>
                        <td className="px-5 py-3">
                          <span className="inline-flex px-2 py-0.5 rounded-full text-[12px] font-bold bg-amber-50 text-amber-600">Đang chờ</span>
                        </td>
                      </tr>

                      <tr className="hover:bg-slate-50/30 transition-colors">
                        <td className="px-5 py-3 font-bold text-slate-950">15:30</td>
                        <td className="px-5 py-3 font-bold text-slate-800">Phạm Văn D</td>
                        <td className="px-5 py-3 text-slate-500 font-medium">Bọc sứ Emax</td>
                        <td className="px-5 py-3">
                          <span className="inline-flex px-2 py-0.5 rounded-full text-[12px] font-bold bg-amber-50 text-amber-600">Đang chờ</span>
                        </td>
                      </tr>

                      <tr className="hover:bg-slate-50/30 transition-colors">
                        <td className="px-5 py-3 font-bold text-slate-950">17:30</td>
                        <td className="px-5 py-3 font-bold text-slate-800">Phạm Văn E</td>
                        <td className="px-5 py-3 text-slate-500 font-medium">Bọc sứ Emax</td>
                        <td className="px-5 py-3">
                          <span className="inline-flex px-2 py-0.5 rounded-full text-[12px] font-bold bg-amber-50 text-amber-600">Đang chờ</span>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

            </div>

            {/* ── RIGHT COLUMN (Calendar & Shifts & Top-rated dentists) ── */}
            <div className="lg:col-span-4 flex flex-col min-w-0 self-stretch">

              {/* Container 1: Lịch vận hành & ca trực */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5 min-h-[500px] shrink-0">
                <div>
                  <h3 className="text-[18px] font-extrabold text-slate-900">Lịch Vận Hành Phòng Khám</h3>
                  <p className="text-[14px] text-slate-400 mt-0.5 font-medium">Thông tin ca làm việc của các bác sĩ</p>
                </div>

                {/* Weekly Horizontal Calendar */}
                <div className="grid grid-cols-7 gap-1.5 text-center">
                  {/* Mon */}
                  <div className="flex flex-col items-center py-2.5 rounded-md border border-slate-100 cursor-pointer hover:bg-red-50/20 hover:border-primary/40 transition-all duration-200">
                    <span className="text-[12px] font-bold text-slate-400 uppercase">T2</span>
                    <span className="text-[15px] font-extrabold text-slate-700 mt-0.5">08</span>
                  </div>
                  {/* Tue */}
                  <div className="flex flex-col items-center py-2.5 rounded-md border border-slate-100 cursor-pointer hover:bg-red-50/20 hover:border-primary/40 transition-all duration-200">
                    <span className="text-[12px] font-bold text-slate-400 uppercase">T3</span>
                    <span className="text-[15px] font-extrabold text-slate-700 mt-0.5">09</span>
                  </div>
                  {/* Wed (Active / Today) */}
                  <div className="flex flex-col items-center py-2.5 rounded-md bg-primary text-white border border-primary shadow-sm shadow-primary/20 cursor-pointer transition-all">
                    <span className="text-[12px] font-bold text-white/80 uppercase">T4</span>
                    <span className="text-[15px] font-extrabold text-white mt-0.5">10</span>
                  </div>
                  {/* Thu */}
                  <div className="flex flex-col items-center py-2.5 rounded-md border border-slate-100 cursor-pointer hover:bg-red-50/20 hover:border-primary/40 transition-all duration-200">
                    <span className="text-[12px] font-bold text-slate-400 uppercase">T5</span>
                    <span className="text-[15px] font-extrabold text-slate-700 mt-0.5">11</span>
                  </div>
                  {/* Fri */}
                  <div className="flex flex-col items-center py-2.5 rounded-md border border-slate-100 cursor-pointer hover:bg-red-50/20 hover:border-primary/40 transition-all duration-200">
                    <span className="text-[12px] font-bold text-slate-400 uppercase">T6</span>
                    <span className="text-[15px] font-extrabold text-slate-700 mt-0.5">12</span>
                  </div>
                  {/* Sat */}
                  <div className="flex flex-col items-center py-2.5 rounded-md border border-slate-100 cursor-pointer hover:bg-red-50/20 hover:border-primary/40 transition-all duration-200">
                    <span className="text-[12px] font-bold text-slate-400 uppercase">T7</span>
                    <span className="text-[15px] font-extrabold text-slate-700 mt-0.5">13</span>
                  </div>
                  {/* Sun */}
                  <div className="flex flex-col items-center py-2.5 rounded-md border border-slate-100 cursor-pointer hover:bg-red-50/20 hover:border-primary/40 transition-all duration-200">
                    <span className="text-[12px] font-bold text-slate-400 uppercase">CN</span>
                    <span className="text-[15px] font-extrabold text-slate-700 mt-0.5">14</span>
                  </div>
                </div>

                {/* Shifts Split: Morning & Afternoon (Compacted) */}
                <div className="flex flex-col gap-4 border-t border-slate-100 pt-4 flex-1 justify-start">

                  {/* Morning Shift (Ca Sáng) */}
                  <div className="flex flex-col gap-2">
                    <div className="flex items-center gap-2">
                      <span className="w-2 h-2 rounded-full bg-primary"></span>
                      <span className="text-[12px] font-extrabold text-slate-900 uppercase tracking-wider">Lịch trực ca sáng (08:00 - 12:00)</span>
                    </div>
                    <div className="flex flex-col gap-2">
                      {/* Doctor 1 */}
                      <div className="flex items-center justify-between p-2 rounded-xl bg-slate-50 border border-slate-200/60 hover:border-primary/40 hover:bg-red-50/10 cursor-pointer transition-all duration-200">
                        <div className="flex items-center gap-2">
                          <img
                            src="https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=256&q=80"
                            alt="BS. Nguyễn Minh Đức"
                            className="w-6 h-6 rounded-full object-cover border border-slate-200 shrink-0"
                          />
                          <div>
                            <div className="text-[13px] font-bold text-slate-900">BS. Nguyễn Minh Đức</div>
                            <div className="text-[11px] text-slate-400 font-semibold">Cấy ghép Implant</div>
                          </div>
                        </div>
                        <span className="inline-flex items-center gap-1 text-[11px] font-bold text-amber-500 bg-amber-50 px-1.5 py-0.5 rounded-full">
                          Đang khám
                        </span>
                      </div>

                      {/* Doctor 2 */}
                      <div className="flex items-center justify-between p-2 rounded-xl bg-slate-50 border border-slate-200/60 hover:border-primary/40 hover:bg-red-50/10 cursor-pointer transition-all duration-200">
                        <div className="flex items-center gap-2">
                          <img
                            src="https://images.unsplash.com/photo-1591604021695-0c69b7c05981?auto=format&fit=crop&w=256&q=80"
                            alt="BS. Lê Thị Phương Thảo"
                            className="w-6 h-6 rounded-full object-cover border border-slate-200 shrink-0"
                          />
                          <div>
                            <div className="text-[13px] font-bold text-slate-900">BS. Lê Thị Phương Thảo</div>
                            <div className="text-[11px] text-slate-400 font-semibold">Chỉnh nha Invisalign</div>
                          </div>
                        </div>
                        <span className="inline-flex items-center gap-1 text-[11px] font-bold text-green-600 bg-green-50 px-1.5 py-0.5 rounded-full">
                          Đang rảnh
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Afternoon Shift (Ca Chiều) */}
                  <div className="flex flex-col gap-2">
                    <div className="flex items-center gap-2">
                      <span className="w-2 h-2 rounded-full bg-secondary"></span>
                      <span className="text-[12px] font-extrabold text-slate-900 uppercase tracking-wider">Lịch trực ca chiều (13:30 - 17:30)</span>
                    </div>
                    <div className="flex flex-col gap-2">
                      {/* Doctor 3 */}
                      <div className="flex items-center justify-between p-2 rounded-xl bg-slate-50 border border-slate-200/60 hover:border-primary/40 hover:bg-red-50/10 cursor-pointer transition-all duration-200">
                        <div className="flex items-center gap-2">
                          <img
                            src="https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=256&q=80"
                            alt="BS. Trần Quốc Bảo"
                            className="w-6 h-6 rounded-full object-cover border border-slate-200 shrink-0"
                          />
                          <div>
                            <div className="text-[13px] font-bold text-slate-900">BS. Trần Quốc Bảo</div>
                            <div className="text-[11px] text-slate-400 font-semibold">Nội nha / Tủy răng</div>
                          </div>
                        </div>
                        <span className="inline-flex items-center gap-1 text-[11px] font-bold text-green-600 bg-green-50 px-1.5 py-0.5 rounded-full">
                          Đang rảnh
                        </span>
                      </div>

                      {/* Doctor 1 (Also works afternoon) */}
                      <div className="flex items-center justify-between p-2 rounded-xl bg-slate-50 border border-slate-200/60 hover:border-primary/40 hover:bg-red-50/10 cursor-pointer transition-all duration-200">
                        <div className="flex items-center gap-2">
                          <img
                            src="https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=256&q=80"
                            alt="BS. Nguyễn Minh Đức"
                            className="w-6 h-6 rounded-full object-cover border border-slate-200 shrink-0"
                          />
                          <div>
                            <div className="text-[13px] font-bold text-slate-900">BS. Nguyễn Minh Đức</div>
                            <div className="text-[11px] text-slate-400 font-semibold">Phục hình thẩm mỹ</div>
                          </div>
                        </div>
                        <span className="inline-flex items-center gap-1 text-[11px] font-bold text-amber-500 bg-amber-50 px-1.5 py-0.5 rounded-full">
                          Đang khám
                        </span>
                      </div>
                    </div>
                  </div>

                </div>
              </div>

              {/* Container 2: Đánh giá từ khách hàng (Độc lập) */}
              <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3.5 mt-6 shrink-0">
                <div className="flex items-center justify-between">
                  <span className="text-[16px] font-extrabold text-slate-900 tracking-wider">Đánh giá từ khách hàng</span>
                  <span className="text-[12px] text-amber-500 font-bold flex items-center gap-0.5">★ Đánh giá 5.0/5</span>
                </div>
                <div className="flex flex-col gap-3">
                  {/* Review 1 */}
                  <div className="flex flex-col gap-2 p-3 rounded-xl bg-slate-50 border border-slate-100 hover:border-slate-200 hover:bg-slate-100/30 transition-all duration-200">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <img
                          src="https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=150&q=80"
                          alt="Nguyễn Thị Thu Hà"
                          className="w-7 h-7 rounded-full object-cover border border-slate-200 shrink-0"
                        />
                        <div className="min-w-0">
                          <div className="text-[12px] font-bold text-slate-900 truncate">Nguyễn Thị Thu Hà</div>
                          <div className="text-[10px] text-slate-400 font-medium">Khách hàng thân thiết</div>
                        </div>
                      </div>
                      <span className="text-[10px] font-bold text-primary bg-red-50 px-1.5 py-0.5 rounded-full">
                        Invisalign
                      </span>
                    </div>
                    <div className="flex items-center gap-0.5 text-amber-400 text-[10px]">⭐⭐⭐⭐⭐</div>
                    <p className="text-[12px] text-slate-600 italic leading-relaxed">
                      "Bác sĩ Thảo tư vấn niềng Invisalign rất kỹ, nhẹ nhàng. Phòng khám vô cùng sạch sẽ và hiện đại!"
                    </p>
                  </div>

                  {/* Review 2 */}
                  <div className="flex flex-col gap-2 p-3 rounded-xl bg-slate-50 border border-slate-100 hover:border-slate-200 hover:bg-slate-100/30 transition-all duration-200">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <img
                          src="https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=150&q=80"
                          alt="Trần Hoàng Nam"
                          className="w-7 h-7 rounded-full object-cover border border-slate-200 shrink-0"
                        />
                        <div className="min-w-0">
                          <div className="text-[12px] font-bold text-slate-900 truncate">Trần Hoàng Nam</div>
                          <div className="text-[10px] text-slate-400 font-medium">Đã điều trị</div>
                        </div>
                      </div>
                      <span className="text-[10px] font-bold text-secondary bg-sky-50 px-1.5 py-0.5 rounded-full">
                        Implant
                      </span>
                    </div>
                    <div className="flex items-center gap-0.5 text-amber-400 text-[10px]">⭐⭐⭐⭐⭐</div>
                    <p className="text-[12px] text-slate-600 italic leading-relaxed">
                      "Trải nghiệm cấy Implant của bác sĩ Đức rất êm ái, không đau nhức như tôi nghĩ. Rất chuyên nghiệp."
                    </p>
                  </div>

                  {/* Review 3 */}
                  <div className="flex flex-col gap-2 p-3 rounded-xl bg-slate-50 border border-slate-100 hover:border-slate-200 hover:bg-slate-100/30 transition-all duration-200">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <img
                          src="https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=150&q=80"
                          alt="Phạm Minh Anh"
                          className="w-7 h-7 rounded-full object-cover border border-slate-200 shrink-0"
                        />
                        <div className="min-w-0">
                          <div className="text-[12px] font-bold text-slate-900 truncate">Phạm Minh Anh</div>
                          <div className="text-[10px] text-slate-400 font-medium">Đã điều trị</div>
                        </div>
                      </div>
                      <span className="text-[10px] font-bold text-emerald-600 bg-emerald-50 px-1.5 py-0.5 rounded-full">
                        Răng sứ
                      </span>
                    </div>
                    <div className="flex items-center gap-0.5 text-amber-400 text-[10px]">⭐⭐⭐⭐⭐</div>
                    <p className="text-[12px] text-slate-600 italic leading-relaxed">
                      "Tôi làm răng sứ thẩm mỹ ở đây, dáng răng tự nhiên, ăn nhai tốt. Cảm ơn đội ngũ nha sĩ tận tâm."
                    </p>
                  </div>
                </div>
              </div>

            </div>

          </div>
        </div>
      </main>
    </div>
  );
}
