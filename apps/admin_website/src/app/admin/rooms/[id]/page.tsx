"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import AdminPageHeader from "../../../../components/shared/AdminPageHeader";
import Pagination from "../../../../components/shared/Pagination";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import {
  getRoomByIdApi,
  getSupplyTransactionsApi,
  type RoomDto,
  type SupplyTransactionDto,
} from "../../../../lib/apiClient";

const PAGE_SIZE_DEFAULT = 10;

interface RoomDetailPageProps {
  params: Promise<{ id: string }>;
}

const formatDateTime = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()} ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
};

export default function RoomDetailPage({ params }: RoomDetailPageProps) {
  useRequireAdmin();
  const { id } = React.use(params);

  const [room, setRoom] = useState<RoomDto | null>(null);
  const [transactions, setTransactions] = useState<SupplyTransactionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_DEFAULT);

  useEffect(() => {
    Promise.all([getRoomByIdApi(id), getSupplyTransactionsApi(id)])
      .then(([r, txs]) => {
        setRoom(r);
        // roomId chỉ gắn trên giao dịch loại "export" (xem SupplyTransaction.RoomId) — lọc lại cho chắc.
        setTransactions(txs.filter(t => t.type === "export"));
      })
      .catch(() => setError("Không thể tải thông tin phòng."))
      .finally(() => setIsLoading(false));
  }, [id]);

  const backButton = (
    <Link
      href="/admin/rooms"
      className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
    >
      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
      </svg>
    </Link>
  );

  if (isLoading) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="rooms" />
        <main className="flex-1 flex flex-col min-w-0">
          <AdminPageHeader title="Lịch sử nhập kho cho phòng" subtitle="Đang tải thông tin..." left={backButton} />
          <div className="p-8 flex-1 flex items-center justify-center">
            <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
          </div>
        </main>
      </div>
    );
  }

  if (error || !room) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="rooms" />
        <main className="flex-1 flex flex-col min-w-0">
          <AdminPageHeader title="Lịch sử nhập kho cho phòng" subtitle="Không tìm thấy phòng" left={backButton} />
          <div className="p-8 flex-1 flex items-center justify-center">
            <p className="text-slate-500 font-semibold">{error || "Phòng không tồn tại."}</p>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="rooms" />

      <main className="flex-1 flex flex-col min-w-0">
        <AdminPageHeader
          title={`Lịch sử nhập kho cho phòng — ${room.name}`}
          subtitle="Các lần vật tư được cấp/nhập vào phòng này, không phải tồn kho hiện có của phòng"
          left={backButton}
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {/* Thông tin phòng */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex flex-wrap items-center gap-x-8 gap-y-3">
            <div>
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Phòng</span>
              <span className="text-[15px] font-black text-slate-900">{room.name} <span className="text-slate-400 font-bold text-[12px]">({room.code})</span></span>
            </div>
            <div>
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tầng</span>
              <span className="text-[14px] font-bold text-slate-700">Tầng {room.floor}</span>
            </div>
            <div>
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Trạng thái</span>
              <span className="text-[14px] font-bold text-slate-700">{room.status}</span>
            </div>
          </div>

          {/* Lịch sử nhập kho cho phòng */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="px-6 py-4 flex flex-wrap items-center justify-between gap-3 border-b border-slate-100">
              <h3 className="text-[15px] font-black text-slate-900">Lịch sử nhập kho cho phòng này</h3>
              {transactions.length > 0 && (
                <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold whitespace-nowrap">
                  <span>Hiển thị</span>
                  <select value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
                    className="px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer">
                    {[10, 20, 50].map((n) => (<option key={n} value={n}>{n}</option>))}
                  </select>
                  <span>/ trang</span>
                </div>
              )}
            </div>
            {transactions.length === 0 ? (
              <p className="text-[13px] font-semibold text-slate-400 text-center py-8">Chưa có lần nhập kho nào cho phòng này.</p>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full text-[13px]">
                    <thead>
                      <tr className="border-b border-slate-100 bg-slate-50/70">
                        <th className="px-6 py-2.5 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Vật tư</th>
                        <th className="px-6 py-2.5 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Số lượng</th>
                        <th className="px-6 py-2.5 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Ghi chú</th>
                        <th className="px-6 py-2.5 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Ngày · Nhân viên</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {transactions.slice((page - 1) * pageSize, page * pageSize).map(t => (
                        <tr key={t.id} className="hover:bg-slate-50/50 transition-colors">
                          <td className="px-6 py-3 font-bold text-slate-900">{t.itemName}</td>
                          <td className="px-6 py-3 text-right font-black text-emerald-600">+{t.quantity}</td>
                          <td className="px-6 py-3 text-slate-500 font-semibold">{t.note || "—"}</td>
                          <td className="px-6 py-3">
                            <div className="text-slate-600 font-semibold">{formatDateTime(t.createdAt)}</div>
                            <div className="text-[11px] text-slate-400 font-medium">{t.createdBy}</div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <div className="px-6 py-4 border-t border-slate-100">
                  <Pagination currentPage={page} totalCount={transactions.length} pageSize={pageSize} onPageChange={setPage} itemLabel="lần nhập" />
                </div>
              </>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
