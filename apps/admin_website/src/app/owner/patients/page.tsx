"use client";

import { useState, useEffect, useMemo, useCallback } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import Pagination from "../../../components/shared/Pagination";
import PatientDetailModal from "../../../components/shared/PatientDetailModal";
import { SortableTh, Th, toggleSortState, type SortDir } from "../../../components/shared/TableHeader";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import { getPatientBalancesApi, type PatientBalanceDto } from "../../../lib/apiClient";

const fmt = (n: number) => new Intl.NumberFormat("vi-VN").format(Math.round(n)) + " đ";
const fmtDate = (iso: string | null) => {
  if (!iso) return "—";
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
};

const ITEMS_PER_PAGE_DEFAULT = 10;

type SortKey = "name" | "totalCost" | "amountPaid" | "remainingAmount";
const SORT_DESC_BY_DEFAULT = (column: SortKey) => column !== "name";

export default function OwnerPatientBalancesPage() {
  useRequireOwner();

  const [patients, setPatients] = useState<PatientBalanceDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [onlyWithDebt, setOnlyWithDebt] = useState(false);
  const [selectedPatient, setSelectedPatient] = useState<PatientBalanceDto | null>(null);

  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(ITEMS_PER_PAGE_DEFAULT);
  const [sortKey, setSortKey] = useState<SortKey>("remainingAmount");
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  const load = useCallback(() => {
    setIsLoading(true);
    setError(null);
    getPatientBalancesApi()
      .then(setPatients)
      .catch((e) => setError(e instanceof Error ? e.message : "Không thể tải công nợ bệnh nhân"))
      .finally(() => setIsLoading(false));
  }, []);

  useEffect(() => { load(); }, [load]);

  const stats = useMemo(() => {
    const withDebt = patients.filter((p) => p.remainingAmount > 0);
    return {
      total: patients.length,
      withDebtCount: withDebt.length,
      totalPaid: patients.reduce((s, p) => s + p.amountPaid, 0),
      totalOwed: patients.reduce((s, p) => s + p.remainingAmount, 0),
    };
  }, [patients]);

  const filtered = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return patients.filter((p) => {
      const matchesSearch = !q || p.fullName.toLowerCase().includes(q) || (p.phoneNumber ?? "").includes(q);
      const matchesDebt = !onlyWithDebt || p.remainingAmount > 0;
      return matchesSearch && matchesDebt;
    });
  }, [patients, searchQuery, onlyWithDebt]);

  const sorted = useMemo(() => {
    const dir = sortDir === "asc" ? 1 : -1;
    const value = (p: PatientBalanceDto): string | number => {
      switch (sortKey) {
        case "name": return p.fullName.toLowerCase();
        case "totalCost": return p.totalCost;
        case "amountPaid": return p.amountPaid;
        case "remainingAmount": return p.remainingAmount;
      }
    };
    return [...filtered].sort((a, b) => {
      const va = value(a), vb = value(b);
      if (typeof va === "string" && typeof vb === "string") return va.localeCompare(vb, "vi") * dir;
      return ((va as number) - (vb as number)) * dir;
    });
  }, [filtered, sortKey, sortDir]);

  const paginated = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    return sorted.slice(start, start + itemsPerPage);
  }, [sorted, currentPage, itemsPerPage]);

  const handleSort = (column: SortKey) => {
    const next = toggleSortState({ key: sortKey, dir: sortDir }, column, SORT_DESC_BY_DEFAULT);
    setSortKey(next.key);
    setSortDir(next.dir);
    setCurrentPage(1);
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="patient-balances" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader
          title="Công nợ bệnh nhân"
          subtitle="Tất cả bệnh nhân — đã thanh toán bao nhiêu, còn nợ bao nhiêu, theo từng dịch vụ"
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {/* STATS */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng bệnh nhân</span>
              <span className="text-3xl font-black text-slate-900 block mt-1">{stats.total}</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Còn nợ</span>
              <span className="text-3xl font-black text-amber-600 block mt-1">{stats.withDebtCount}</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng đã thu</span>
              <span className="text-2xl font-black text-emerald-600 block mt-1">{fmt(stats.totalPaid)}</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng còn nợ</span>
              <span className="text-2xl font-black text-primary block mt-1">{fmt(stats.totalOwed)}</span>
            </div>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
            {error && (
              <div className="px-6 py-3 bg-red-50 border-b border-red-100 text-[13px] text-red-600 font-semibold">
                {error}
              </div>
            )}

            <div className="p-4 flex flex-wrap items-center gap-3 border-b border-slate-100">
              <div className="relative flex-1 min-w-[220px]">
                <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm theo tên hoặc số điện thoại..."
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); setCurrentPage(1); }}
                  className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                />
              </div>
              <label className="flex items-center gap-2 text-[13px] font-bold text-slate-600 cursor-pointer select-none shrink-0">
                <input
                  type="checkbox"
                  checked={onlyWithDebt}
                  onChange={(e) => { setOnlyWithDebt(e.target.checked); setCurrentPage(1); }}
                  className="w-4 h-4 rounded accent-primary cursor-pointer"
                />
                Chỉ hiện bệnh nhân còn nợ
              </label>
              <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold whitespace-nowrap">
                <span>Hiển thị</span>
                <select
                  value={itemsPerPage}
                  onChange={(e) => { setItemsPerPage(Number(e.target.value)); setCurrentPage(1); }}
                  className="px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 text-[13px] cursor-pointer"
                >
                  {[10, 20, 50].map((n) => (<option key={n} value={n}>{n}</option>))}
                </select>
                <span>/ trang</span>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[14px]">
                <thead>
                  <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
                    <SortableTh column="name" label="Bệnh nhân" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} className="px-6" />
                    <Th className="px-6" align="center">Số liệu trình</Th>
                    <Th className="px-6">Lần điều trị gần nhất</Th>
                    <SortableTh column="totalCost" label="Tổng chi phí" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} align="right" className="px-6" />
                    <SortableTh column="amountPaid" label="Đã thanh toán" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} align="right" className="px-6" />
                    <SortableTh column="remainingAmount" label="Còn nợ" sortKey={sortKey} sortDir={sortDir} onSort={handleSort} align="right" className="px-6" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                  {isLoading ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-10 text-center text-slate-400 font-bold">Đang tải dữ liệu...</td>
                    </tr>
                  ) : paginated.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-10 text-center text-slate-400 font-bold">Không tìm thấy bệnh nhân phù hợp.</td>
                    </tr>
                  ) : (
                    paginated.map((p) => (
                      <tr
                        key={p.patientId}
                        onClick={() => setSelectedPatient(p)}
                        className="hover:bg-slate-50/40 transition-colors cursor-pointer"
                      >
                        <td className="px-6 py-4">
                          <div className="font-extrabold text-slate-900">{p.fullName}</div>
                          <div className="text-[12px] font-mono text-slate-400">{p.phoneNumber ?? "—"}</div>
                        </td>
                        <td className="px-6 py-4 text-center tabular-nums">{p.treatmentPlanCount}</td>
                        <td className="px-6 py-4 text-slate-500">{fmtDate(p.lastTreatmentDate)}</td>
                        <td className="px-6 py-4 text-right tabular-nums">{fmt(p.totalCost)}</td>
                        <td className="px-6 py-4 text-right tabular-nums text-emerald-600">{fmt(p.amountPaid)}</td>
                        <td className="px-6 py-4 text-right tabular-nums">
                          {p.remainingAmount > 0 ? (
                            <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-red-50 text-primary font-black text-[13px]">
                              {fmt(p.remainingAmount)}
                            </span>
                          ) : (
                            <span className="text-slate-400 font-bold">Đã thanh toán đủ</span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            {!isLoading && sorted.length > 0 && (
              <div className="border-t border-slate-100 px-5 py-3.5 bg-slate-50/25">
                <Pagination
                  currentPage={currentPage}
                  totalCount={sorted.length}
                  pageSize={itemsPerPage}
                  onPageChange={setCurrentPage}
                  itemLabel="bệnh nhân"
                />
              </div>
            )}
          </div>
        </div>
      </main>

      {selectedPatient && (
        <PatientDetailModal
          patientId={selectedPatient.patientId}
          patientName={selectedPatient.fullName}
          patientPhone={selectedPatient.phoneNumber}
          defaultTab="invoices"
          onClose={() => setSelectedPatient(null)}
        />
      )}
    </div>
  );
}
