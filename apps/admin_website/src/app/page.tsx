import React from "react";

export default function Dashboard() {
  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50">
      {/* SIDEBAR */}
      <aside className="w-64 bg-white border-r border-slate-200 p-6 flex flex-col gap-8">
        <div className="flex items-center gap-2">
          <span className="text-2xl">🦷</span>
          <span className="font-extrabold text-xl tracking-tight text-slate-900">
            Dental<span className="text-primary font-normal">Admin</span>
          </span>
        </div>
        
        <nav className="flex flex-col gap-1">
          <a href="#" className="flex items-center gap-3 px-4 py-3 rounded-lg font-semibold bg-primary-light text-primary transition-all">
            <span>📊</span> Tổng quan
          </a>
          <a href="#" className="flex items-center gap-3 px-4 py-3 rounded-lg font-semibold text-slate-500 hover:bg-slate-100 hover:text-slate-900 transition-all">
            <span>📅</span> Lịch khám
          </a>
          <a href="#" className="flex items-center gap-3 px-4 py-3 rounded-lg font-semibold text-slate-500 hover:bg-slate-100 hover:text-slate-900 transition-all">
            <span>👥</span> Bệnh nhân
          </a>
          <a href="#" className="flex items-center gap-3 px-4 py-3 rounded-lg font-semibold text-slate-500 hover:bg-slate-100 hover:text-slate-900 transition-all">
            <span>👨‍⚕️</span> Bác sĩ
          </a>
          <a href="#" className="flex items-center gap-3 px-4 py-3 rounded-lg font-semibold text-slate-500 hover:bg-slate-100 hover:text-slate-900 transition-all">
            <span>💳</span> Hóa đơn
          </a>
          <a href="#" className="flex items-center gap-3 px-4 py-3 rounded-lg font-semibold text-slate-500 hover:bg-slate-100 hover:text-slate-900 transition-all">
            <span>⚙️</span> Cấu hình
          </a>
        </nav>
        
        <div className="mt-auto">
          <div className="p-4 rounded-xl bg-slate-50 text-xs">
            <div className="font-bold text-slate-800">Version 1.0.0</div>
            <div className="text-slate-400 mt-1">Dental Management System</div>
          </div>
        </div>
      </aside>

      {/* MAIN CONTENT AREA */}
      <main className="flex-1 flex flex-col">
        {/* HEADER */}
        <header className="h-20 bg-white border-b border-slate-200 px-8 flex items-center justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-900">Hệ Thống Vận Hành Nội Bộ</h2>
          </div>
          <div className="flex items-center gap-4">
            <span className="text-sm font-semibold text-slate-700">
              Xin chào, <span className="text-primary font-bold">Admin</span>
            </span>
            <div className="w-10 h-10 rounded-full bg-primary-light flex items-center justify-center font-bold text-primary text-sm">
              AD
            </div>
          </div>
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 flex flex-col">
          {/* Stats Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
            <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm hover-lift">
              <div className="flex justify-between items-center mb-4">
                <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">LỊCH KHÁM HÔM NAY</span>
                <span className="text-xl">📅</span>
              </div>
              <div className="text-3xl font-extrabold text-primary mb-2">24</div>
              <div className="text-xs text-green-500 font-semibold flex items-center gap-1">
                <span>↑ 12%</span> <span className="text-slate-400 font-normal">so với hôm qua</span>
              </div>
            </div>

            <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm hover-lift">
              <div className="flex justify-between items-center mb-4">
                <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">BỆNH NHÂN MỚI</span>
                <span className="text-xl">👥</span>
              </div>
              <div className="text-3xl font-extrabold text-secondary mb-2">8</div>
              <div className="text-xs text-green-500 font-semibold flex items-center gap-1">
                <span>↑ 4%</span> <span className="text-slate-400 font-normal">tuần này</span>
              </div>
            </div>

            <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm hover-lift">
              <div className="flex justify-between items-center mb-4">
                <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">DOANH THU HÔM NAY</span>
                <span className="text-xl">💳</span>
              </div>
              <div className="text-3xl font-extrabold text-primary mb-2">18.4M</div>
              <div className="text-xs text-green-500 font-semibold flex items-center gap-1">
                <span>↑ 20%</span> <span className="text-slate-400 font-normal">mục tiêu ngày</span>
              </div>
            </div>

            <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm hover-lift">
              <div className="flex justify-between items-center mb-4">
                <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">BÁC SĨ ĐANG TRỰC</span>
                <span className="text-xl">👨‍⚕️</span>
              </div>
              <div className="text-3xl font-extrabold text-accent mb-2">6</div>
              <div className="text-xs text-slate-400 font-normal">
                Đầy đủ ca trực hôm nay
              </div>
            </div>
          </div>

          {/* Lịch Hẹn Gần Đây Table Card */}
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
            <div className="p-6 border-b border-slate-200 flex justify-between items-center">
              <h3 className="text-base font-bold text-slate-900">Danh Sách Lịch Hẹn Chờ Khám</h3>
              <button className="bg-primary hover:bg-primary-hover text-white text-xs font-bold px-4 py-2 rounded-lg transition-colors">
                + Thêm lịch hẹn mới
              </button>
            </div>
            
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50/50 text-xs font-bold text-slate-400 uppercase tracking-wider border-b border-slate-200">
                    <th className="px-6 py-4">Mã Lịch Hẹn</th>
                    <th className="px-6 py-4">Bệnh Nhân</th>
                    <th className="px-6 py-4">Bác Sĩ Điều Trị</th>
                    <th className="px-6 py-4">Giờ Hẹn</th>
                    <th className="px-6 py-4">Dịch Vụ</th>
                    <th className="px-6 py-4">Trạng Thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm text-slate-700">
                  <tr className="hover:bg-slate-50/40">
                    <td className="px-6 py-4 font-semibold text-slate-950">#LH-2045</td>
                    <td className="px-6 py-4">Nguyễn Văn A</td>
                    <td className="px-6 py-4">BS. Nguyễn Minh Đức</td>
                    <td className="px-6 py-4">09:00</td>
                    <td className="px-6 py-4">Cấy ghép Implant</td>
                    <td className="px-6 py-4">
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-semibold bg-green-50 text-green-600">Đã Xác Nhận</span>
                    </td>
                  </tr>
                  <tr className="hover:bg-slate-50/40">
                    <td className="px-6 py-4 font-semibold text-slate-950">#LH-2046</td>
                    <td className="px-6 py-4">Trần Thị B</td>
                    <td className="px-6 py-4">BS. Lê Thị Phương Thảo</td>
                    <td className="px-6 py-4">10:00</td>
                    <td className="px-6 py-4">Niềng răng Invisalign</td>
                    <td className="px-6 py-4">
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-semibold bg-green-50 text-green-600">Đã Xác Nhận</span>
                    </td>
                  </tr>
                  <tr className="hover:bg-slate-50/40">
                    <td className="px-6 py-4 font-semibold text-slate-950">#LH-2047</td>
                    <td className="px-6 py-4">Phạm Văn C</td>
                    <td className="px-6 py-4">BS. Nguyễn Minh Đức</td>
                    <td className="px-6 py-4">11:30</td>
                    <td className="px-6 py-4">Bọc răng sứ thẩm mỹ</td>
                    <td className="px-6 py-4">
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-600">Chờ Khám</span>
                    </td>
                  </tr>
                  <tr className="hover:bg-slate-50/40">
                    <td className="px-6 py-4 font-semibold text-slate-950">#LH-2048</td>
                    <td className="px-6 py-4">Lê Hoàng D</td>
                    <td className="px-6 py-4">BS. Trần Quốc Bảo</td>
                    <td className="px-6 py-4">14:00</td>
                    <td className="px-6 py-4">Điều trị nội nha (tủy)</td>
                    <td className="px-6 py-4">
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-600">Chờ Khám</span>
                    </td>
                  </tr>
                  <tr className="hover:bg-slate-50/40">
                    <td className="px-6 py-4 font-semibold text-slate-950">#LH-2049</td>
                    <td className="px-6 py-4">Đỗ Thị E</td>
                    <td className="px-6 py-4">BS. Lê Thị Phương Thảo</td>
                    <td className="px-6 py-4">15:30</td>
                    <td className="px-6 py-4">Khám răng tổng quát</td>
                    <td className="px-6 py-4">
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-semibold bg-primary-light text-primary">Chờ Xác Nhận</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
