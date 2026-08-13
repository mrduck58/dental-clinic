"use client";

import React, { useState, useEffect } from "react";
import OwnerSidebar from "../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../hooks/useRequireOwner";
import {
  getOwnerDashboardApi,
  type OwnerDashboardDto,
} from "../../lib/apiClient";

export default function OwnerDashboardPage() {
  useRequireOwner();

  const [dashboardData, setDashboardData] = useState<OwnerDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getOwnerDashboardApi()
      .then(setDashboardData)
      .catch((err) => console.error("Lỗi khi tải Owner Dashboard:", err))
      .finally(() => setLoading(false));
  }, []);

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat("vi-VN").format(val) + " đ";
  };

  const weeklyData = dashboardData?.weeklyTrend ?? [];
  const ratingStats = dashboardData?.ratingStats ?? {
    averageRating: 5.0,
    totalReviews: 0,
    fiveStar: 0,
    fourStar: 0,
    threeStar: 0,
    twoStar: 0,
    oneStar: 0,
  };
  const outstandingEmployees = dashboardData?.outstandingEmployees ?? [];

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
          {loading ? (
            <div className="bg-white p-12 rounded-2xl border border-slate-200/60 shadow-sm text-center text-slate-400 font-semibold">
              Đang tải dữ liệu tổng quan phòng khám...
            </div>
          ) : (
            <>
              {/* Row 1: KPI Cards */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {/* Thu card */}
                <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-5 hover:shadow-md transition-all group">
                  <div className="w-14 h-14 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform duration-300">
                    <svg className="w-7 h-7" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-1.971-.659-1.171-.879-1.171-2.303 0-3.182 1.172-.879 3.07-.879 4.242 0L15 9M3 5.25h18A2.25 2.25 0 0 1 21 7.5v9a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 16.5v-9a2.25 2.25 0 0 1 2.25-2.25Z" />
                    </svg>
                  </div>
                  <div>
                    <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng Doanh Thu (Thu)</span>
                    <span className="text-2xl font-black text-slate-900 mt-1 block">
                      {formatCurrency(dashboardData?.totalRevenue ?? 0)}
                    </span>
                    <span className="text-[11.5px] font-bold text-emerald-600 mt-1 flex items-center gap-1">
                      <span>{dashboardData?.revenueGrowthPercent && dashboardData.revenueGrowthPercent >= 0 ? "↑" : "↓"} {Math.abs(dashboardData?.revenueGrowthPercent ?? 0)}%</span>{" "}
                      <span className="text-slate-400 font-medium">so với tháng trước</span>
                    </span>
                  </div>
                </div>

                {/* Chi card */}
                <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-5 hover:shadow-md transition-all group">
                  <div className="w-14 h-14 rounded-2xl bg-rose-50 text-rose-500 flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform duration-300">
                    <svg className="w-7 h-7" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 0 0 2.25-2.25V6.75A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25v10.5A2.25 2.25 0 0 0 4.5 19.5Z" />
                    </svg>
                  </div>
                  <div>
                    <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng Chi Phí (Chi)</span>
                    <span className="text-2xl font-black text-slate-900 mt-1 block">
                      {formatCurrency(dashboardData?.totalExpense ?? 0)}
                    </span>
                    <span className="text-[11.5px] font-bold text-rose-500 mt-1 flex items-center gap-1">
                      <span>{dashboardData?.expenseGrowthPercent && dashboardData.expenseGrowthPercent >= 0 ? "↑" : "↓"} {Math.abs(dashboardData?.expenseGrowthPercent ?? 0)}%</span>{" "}
                      <span className="text-slate-400 font-medium">so với tháng trước</span>
                    </span>
                  </div>
                </div>

                {/* Bệnh nhân mới card */}
                <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-5 hover:shadow-md transition-all group">
                  <div className="w-14 h-14 rounded-2xl bg-sky-50 text-sky-600 flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform duration-300">
                    <svg className="w-7 h-7" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z" />
                    </svg>
                  </div>
                  <div>
                    <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block">Bệnh Nhân Mới Trong Tháng</span>
                    <span className="text-2xl font-black text-slate-900 mt-1 block">
                      {dashboardData?.newPatientsCount ?? 0}
                    </span>
                    <span className="text-[11.5px] font-bold text-sky-600 mt-1 flex items-center gap-1">
                      <span>+{dashboardData?.newPatientsThisWeekCount ?? 0} bệnh nhân</span>{" "}
                      <span className="text-slate-400 font-medium">mới tuần này</span>
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
                      const maxVal = Math.max(10000000, ...weeklyData.map((w) => Math.max(w.revenue, w.expense)));
                      const revHeight = Math.min(100, Math.max(10, (day.revenue / maxVal) * 100));
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
                    <p className="text-[12px] text-slate-400 font-semibold mb-5">Đánh giá của bệnh nhân cho phòng khám</p>

                    {/* Score display */}
                    <div className="flex items-center gap-4 mb-5">
                      <div className="text-[44px] font-black text-slate-900 tracking-tight leading-none">
                        {ratingStats.averageRating.toFixed(1)}
                      </div>
                      <div>
                        <div className="flex text-amber-400 text-lg leading-none">★★★★★</div>
                        <span className="text-[12px] text-slate-400 font-bold mt-1 block">Tất cả {ratingStats.totalReviews} đánh giá</span>
                      </div>
                    </div>

                    {/* Stars breakdown */}
                    <div className="space-y-2.5">
                      {[
                        { stars: 5, count: ratingStats.fiveStar, color: "bg-emerald-400" },
                        { stars: 4, count: ratingStats.fourStar, color: "bg-emerald-300" },
                        { stars: 3, count: ratingStats.threeStar, color: "bg-amber-400" },
                        { stars: 2, count: ratingStats.twoStar, color: "bg-orange-400" },
                        { stars: 1, count: ratingStats.oneStar, color: "bg-red-400" },
                      ].map((row) => {
                        const pct = ratingStats.totalReviews > 0 ? Math.round((row.count / ratingStats.totalReviews) * 100) : 0;
                        return (
                          <div key={row.stars} className="flex items-center gap-3 text-[12px] font-semibold text-slate-600">
                            <span className="w-3 text-right">{row.stars}</span>
                            <span className="text-amber-400">★</span>
                            <div className="flex-1 h-2 bg-slate-100 rounded-full overflow-hidden">
                              <div style={{ width: `${pct}%` }} className={`h-full rounded-full ${row.color}`} />
                            </div>
                            <span className="w-8 text-right text-slate-400">{pct}%</span>
                          </div>
                        );
                      })}
                    </div>
                  </div>

                  <div className="bg-sky-50/50 border border-sky-100 p-3 rounded-xl text-[12px] text-sky-700 font-bold mt-5 flex items-center gap-2">
                    <svg className="w-5 h-5 text-sky-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M10.34 15.84c-.688-.06-1.38-.09-2.072-.09H6.75a2.25 2.25 0 01-2.25-2.25V10.5a2.25 2.25 0 012.25-2.25h1.518c.69 0 1.384-.03 2.072-.09l4.572-1.524A1.125 1.125 0 0116.5 7.689v8.622a1.125 1.125 0 01-1.637 1.004l-4.523-1.475zM19.5 12a3 3 0 00-1.5-2.6M21.75 12a6 6 0 00-3-5.2" />
                    </svg>
                    <span>Bệnh nhân đánh giá rất tốt về khâu đón tiếp và chất lượng điều trị.</span>
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
                      {outstandingEmployees.length > 0 ? (
                        outstandingEmployees.map((emp, idx) => (
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
                                ★ {emp.rating.toFixed(1)}
                              </span>
                            </td>
                            <td className="px-5 py-4 text-center">
                              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-green-50 text-green-700 border border-green-100 rounded-full text-[11px] font-black">
                                <span className="w-1.5 h-1.5 rounded-full bg-green-500" /> Hoạt động
                              </span>
                            </td>
                          </tr>
                        ))
                      ) : (
                        <tr>
                          <td colSpan={5} className="px-5 py-8 text-center text-slate-400 font-semibold">
                            Chưa có dữ liệu xếp hạng nhân viên.
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </>
          )}
        </div>
      </main>
    </div>
  );
}
