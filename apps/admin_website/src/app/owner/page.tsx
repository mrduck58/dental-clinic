"use client";

import React, { useState, useEffect } from "react";
import OwnerSidebar from "../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../hooks/useRequireOwner";

export default function OwnerDashboardPage() {
  useRequireOwner();

  // Dynamic date calculation for the last 7 days
  const [weeklyData, setWeeklyData] = useState<Array<{ dateStr: string; dayName: string; revenue: number; expense: number }>>([]);

  useEffect(() => {
    const dayNames = ["Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy"];
    const temp = [];
    const baseRevenue = [32000000, 28000000, 42000000, 35000000, 48000000, 52000000, 15000000];
    const baseExpense = [12000000, 10000000, 15000000, 11000000, 18000000, 14000000, 5000000];

    for (let i = 6; i >= 0; i--) {
      const d = new Date();
      d.setDate(d.getDate() - i);
      const dateStr = `${d.getDate()}/${d.getMonth() + 1}`;
      const dayName = dayNames[d.getDay()];
      // Use index or random variations to match mock data
      const idx = d.getDay();
      temp.push({
        dateStr,
        dayName,
        revenue: baseRevenue[idx % baseRevenue.length],
        expense: baseExpense[idx % baseExpense.length],
      });
    }
    setWeeklyData(temp);
  }, []);

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat("vi-VN").format(val) + " đ";
  };

  const outstandingEmployees = [
    { name: "BS. Trần Văn Hùng", role: "Nha sĩ chuyên khoa", cases: 45, rating: "4.9", status: "Active" },
    { name: "BS. Phạm Đức Anh", role: "Nha sĩ chuyên khoa", cases: 38, rating: "4.8", status: "Active" },
    { name: "Nguyễn Thị Lan", role: "Trưởng bộ phận Lễ tân", cases: 32, rating: "5.0", status: "Active" },
    { name: "Hoàng Thị Hương", role: "Nhân viên CSKH", cases: 29, rating: "4.8", status: "Active" },
  ];

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="overview" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <OwnerPageHeader
          title={
            <>
              <span>Hệ Thống Tổng Quản</span>
              <span className="text-[10px] bg-red-100 text-primary px-2 py-0.5 rounded-full font-black uppercase tracking-wide">Owner</span>
            </>
          }
          subtitle="Báo cáo kinh doanh phòng khám và xếp hạng hiệu suất hoạt động."
        />

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          {/* Row 1: KPI Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Thu card */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-5 hover:shadow-md transition-all group">
              <div className="w-14 h-14 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center text-2xl shrink-0 group-hover:scale-105 transition-transform duration-300">
                💰
              </div>
              <div>
                <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng Doanh Thu (Thu)</span>
                <span className="text-2xl font-black text-slate-900 mt-1 block">250.000.000 đ</span>
                <span className="text-[11.5px] font-bold text-emerald-600 mt-1 flex items-center gap-1">
                  <span>↑ 12.5%</span> <span className="text-slate-400 font-medium">so với tháng trước</span>
                </span>
              </div>
            </div>

            {/* Chi card */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-5 hover:shadow-md transition-all group">
              <div className="w-14 h-14 rounded-2xl bg-rose-50 text-primary flex items-center justify-center text-2xl shrink-0 group-hover:scale-105 transition-transform duration-300">
                💸
              </div>
              <div>
                <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng Chi Phí (Chi)</span>
                <span className="text-2xl font-black text-slate-900 mt-1 block">85.000.000 đ</span>
                <span className="text-[11.5px] font-bold text-rose-500 mt-1 flex items-center gap-1">
                  <span>↓ 4.2%</span> <span className="text-slate-400 font-medium">tiết kiệm ngân sách</span>
                </span>
              </div>
            </div>

            {/* Bệnh nhân mới card */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-5 hover:shadow-md transition-all group">
              <div className="w-14 h-14 rounded-2xl bg-sky-50 text-sky-600 flex items-center justify-center text-2xl shrink-0 group-hover:scale-105 transition-transform duration-300">
                👥
              </div>
              <div>
                <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block">Bệnh Nhân Mới Trong Tháng</span>
                <span className="text-2xl font-black text-slate-900 mt-1 block">142</span>
                <span className="text-[11.5px] font-bold text-sky-600 mt-1 flex items-center gap-1">
                  <span>+28 bệnh nhân</span> <span className="text-slate-400 font-medium">mới tuần này</span>
                </span>
              </div>
            </div>
          </div>

          {/* Row 2: Biểu đồ & Ratings */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Chart Widget */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm lg:col-span-2 flex flex-col gap-4">
              <div>
                <h3 className="text-base font-extrabold text-slate-900">Biểu Đồ Doanh Thu & Chi Phí</h3>
                <p className="text-[12px] text-slate-400 font-semibold">Thống kê hoạt động hàng tuần (tính từ hôm nay về 7 ngày trước)</p>
              </div>

              {/* Graphical bar chart */}
              <div className="flex-1 min-h-[250px] flex items-end justify-between gap-2 pt-6 px-2">
                {weeklyData.map((day, idx) => {
                  const maxVal = 60000000;
                  const revHeight = Math.min(100, Math.max(15, (day.revenue / maxVal) * 100));
                  const expHeight = Math.min(100, Math.max(10, (day.expense / maxVal) * 100));

                  return (
                    <div key={idx} className="flex-1 flex flex-col items-center gap-2 group cursor-pointer">
                      {/* Bar Container */}
                      <div className="w-full h-44 flex items-end justify-center gap-1.5 relative">
                        {/* Tooltip on hover */}
                        <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 hidden group-hover:flex flex-col bg-slate-900 text-white text-[10px] font-black rounded-lg p-2 shadow-lg z-15 w-28 text-center pointer-events-none transition-all">
                          <span className="text-slate-300 font-bold block">{day.dayName}</span>
                          <span className="text-emerald-400 font-black block mt-0.5">Thu: {formatCurrency(day.revenue)}</span>
                          <span className="text-rose-400 font-black block">Chi: {formatCurrency(day.expense)}</span>
                        </div>

                        {/* Revenue Bar */}
                        <div
                          style={{ height: `${revHeight}%` }}
                          className="w-3 md:w-5 bg-gradient-to-t from-emerald-400 to-emerald-300 rounded-t-md transition-all duration-500 group-hover:brightness-105 group-hover:shadow-md group-hover:shadow-emerald-200"
                        />
                        {/* Expense Bar */}
                        <div
                          style={{ height: `${expHeight}%` }}
                          className="w-3 md:w-5 bg-gradient-to-t from-red-400 to-red-300 rounded-t-md transition-all duration-500 group-hover:brightness-105 group-hover:shadow-md group-hover:shadow-red-200"
                        />
                      </div>
                      {/* X label */}
                      <div className="text-center">
                        <span className="text-[10px] font-black text-slate-700 block">{day.dateStr}</span>
                        <span className="text-[9px] font-semibold text-slate-400 block truncate max-w-[50px] md:max-w-none">{day.dayName.split(" ").slice(-1)[0]}</span>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Chart Legend */}
              <div className="flex items-center justify-center gap-6 border-t border-slate-100 pt-4 text-[12px] font-bold">
                <span className="flex items-center gap-2 text-slate-600">
                  <span className="w-3.5 h-3.5 rounded bg-emerald-400 inline-block" /> Doanh thu (Thu)
                </span>
                <span className="flex items-center gap-2 text-slate-600">
                  <span className="w-3.5 h-3.5 rounded bg-red-400 inline-block" /> Chi phí (Chi)
                </span>
              </div>
            </div>

            {/* Ratings Card */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col justify-between">
              <div>
                <h3 className="text-base font-extrabold text-slate-900">Đánh Giá (Rating)</h3>
                <p className="text-[12px] text-slate-400 font-semibold mb-5">Đánh giá của bệnh nhân trong tháng</p>

                {/* Score display */}
                <div className="flex items-center gap-4 mb-5">
                  <div className="text-[44px] font-black text-slate-900 tracking-tight leading-none">4.8</div>
                  <div>
                    <div className="flex text-amber-400 text-lg leading-none">★★★★★</div>
                    <span className="text-[12px] text-slate-400 font-bold mt-1 block">Tất cả 110 đánh giá</span>
                  </div>
                </div>

                {/* Stars breakdown */}
                <div className="space-y-2.5">
                  {[
                    { stars: 5, pct: 85, count: 93, color: "bg-emerald-400" },
                    { stars: 4, pct: 10, count: 11, color: "bg-emerald-300" },
                    { stars: 3, pct: 3, count: 4, color: "bg-amber-400" },
                    { stars: 2, pct: 1, count: 1, color: "bg-orange-400" },
                    { stars: 1, pct: 1, count: 1, color: "bg-red-400" },
                  ].map((row) => (
                    <div key={row.stars} className="flex items-center gap-3 text-[12px] font-semibold text-slate-600">
                      <span className="w-3 text-right">{row.stars}</span>
                      <span className="text-amber-400">★</span>
                      <div className="flex-1 h-2 bg-slate-100 rounded-full overflow-hidden">
                        <div style={{ width: `${row.pct}%` }} className={`h-full rounded-full ${row.color}`} />
                      </div>
                      <span className="w-8 text-right text-slate-400">{row.pct}%</span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="bg-sky-50/50 border border-sky-100 p-3 rounded-xl text-[12px] text-sky-700 font-bold mt-5 flex items-center gap-2">
                <span className="text-lg">📢</span>
                <span>Bệnh nhân đánh giá rất tốt về khâu đón tiếp và hỗ trợ điều trị.</span>
              </div>
            </div>
          </div>

          {/* Row 3: Leaderboard (Outstanding Employees) */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm">
            <div className="mb-4">
              <h3 className="text-base font-extrabold text-slate-900">Nhân Viên Tiêu Biểu Trong Tháng</h3>
              <p className="text-[12px] text-slate-400 font-semibold">Xếp hạng hiệu suất theo số ca khám và hỗ trợ điều trị.</p>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px]">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/80">
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Họ và tên</th>
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Bộ phận / Chức vụ</th>
                    <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Số ca hoàn thành</th>
                    <th className="px-5 py-3 text-center font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Rating trung bình</th>
                    <th className="px-5 py-3 text-center font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {outstandingEmployees.map((emp, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <div className="w-9 h-9 rounded-full bg-primary/10 text-primary flex items-center justify-center font-black text-xs shrink-0 border border-primary/20">
                            {emp.name.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()}
                          </div>
                          <span className="font-bold text-slate-900">{emp.name}</span>
                        </div>
                      </td>
                      <td className="px-5 py-4 text-slate-500 font-semibold">{emp.role}</td>
                      <td className="px-5 py-4 text-right font-black text-slate-800">{emp.cases} ca</td>
                      <td className="px-5 py-4 text-center">
                        <span className="bg-amber-50 text-amber-700 border border-amber-200 px-2 py-0.5 rounded-md text-xs font-black">
                          ★ {emp.rating}
                        </span>
                      </td>
                      <td className="px-5 py-4 text-center">
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-green-50 text-green-700 border border-green-100 rounded-full text-[11px] font-black">
                          <span className="w-1.5 h-1.5 rounded-full bg-green-500" /> Đang trực
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
