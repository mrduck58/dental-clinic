"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { createPortal } from "react-dom";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import {
  getCommissionRulesApi,
  createCommissionRuleApi,
  updateCommissionRuleApi,
  toggleCommissionRuleActiveApi,
  deleteCommissionRuleApi,
  getPublicDentistsApi,
  getServicesApi,
  type CommissionRuleDto,
  type PublicDentistDto,
  type ServiceDto,
} from "../../../lib/apiClient";

const fmt = (n: number) => new Intl.NumberFormat("vi-VN").format(Math.round(n)) + " đ";
const formatDate = (iso: string) => new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
const toISODate = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

type PeriodPreset = "month" | "quarter" | "year" | "custom";
const PERIOD_OPTIONS: { value: PeriodPreset; label: string }[] = [
  { value: "month", label: "Tháng này" },
  { value: "quarter", label: "Quý này" },
  { value: "year", label: "Năm nay" },
  { value: "custom", label: "Tùy chọn" },
];

function presetRange(preset: PeriodPreset, today: Date): { from: Date; to: Date } {
  switch (preset) {
    case "month": return { from: new Date(today.getFullYear(), today.getMonth(), 1), to: today };
    case "quarter": {
      const qStart = Math.floor(today.getMonth() / 3) * 3;
      return { from: new Date(today.getFullYear(), qStart, 1), to: today };
    }
    case "year": return { from: new Date(today.getFullYear(), 0, 1), to: today };
    default: return { from: today, to: today };
  }
}

function Portal({ children }: { children: React.ReactNode }) {
  if (typeof document === "undefined") return null;
  return createPortal(children, document.body);
}

interface RuleFormState {
  dentistId: string;
  serviceName: string;
  ratePercent: number;
  effectiveFrom: string;
  effectiveTo: string;
  note: string;
}

const emptyForm = (): RuleFormState => ({
  dentistId: "",
  serviceName: "",
  ratePercent: 10,
  effectiveFrom: toISODate(new Date()),
  effectiveTo: "",
  note: "",
});

export default function OwnerCommissionsPage() {
  useRequireOwner();

  const today = useMemo(() => new Date(), []);
  const [preset, setPreset] = useState<PeriodPreset>("month");
  const [customFrom, setCustomFrom] = useState(toISODate(new Date(today.getFullYear(), today.getMonth(), 1)));
  const [customTo, setCustomTo] = useState(toISODate(today));

  const { fromISO, toISO } = useMemo(() => {
    if (preset === "custom") return { fromISO: customFrom, toISO: customTo };
    const { from, to } = presetRange(preset, today);
    return { fromISO: toISODate(from), toISO: toISODate(to) };
  }, [preset, customFrom, customTo, today]);

  const [rules, setRules] = useState<CommissionRuleDto[]>([]);
  const [totalCommission, setTotalCommission] = useState(0);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const [dentists, setDentists] = useState<PublicDentistDto[]>([]);
  const [services, setServices] = useState<ServiceDto[]>([]);

  const [formOpen, setFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<RuleFormState>(emptyForm());
  const [saving, setSaving] = useState(false);

  const [deleteTarget, setDeleteTarget] = useState<CommissionRuleDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [togglingId, setTogglingId] = useState<string | null>(null);

  useEffect(() => {
    getPublicDentistsApi().then(setDentists).catch(() => setDentists([]));
    getServicesApi().then(setServices).catch(() => setServices([]));
  }, []);

  const showMessage = (msg: string) => { setErrorMsg(null); setSuccessMsg(msg); setTimeout(() => setSuccessMsg(null), 4000); };
  const showError = (err: unknown, fallback: string) => { setSuccessMsg(null); setErrorMsg(err instanceof Error ? err.message : fallback); };

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getCommissionRulesApi(fromISO, toISO);
      setRules(data.items);
      setTotalCommission(data.totalCommission);
    } catch (err) {
      setRules([]); setTotalCommission(0);
      showError(err, "Không thể tải danh sách quy tắc hoa hồng");
    } finally { setLoading(false); }
  }, [fromISO, toISO]);

  useEffect(() => { reload(); }, [reload]);

  const openCreateForm = () => { setEditingId(null); setForm(emptyForm()); setFormOpen(true); };
  const openEditForm = (rule: CommissionRuleDto) => {
    setEditingId(rule.id);
    setForm({
      dentistId: rule.dentistId ?? "",
      serviceName: rule.serviceName ?? "",
      ratePercent: rule.ratePercent,
      effectiveFrom: rule.effectiveFrom,
      effectiveTo: rule.effectiveTo ?? "",
      note: rule.note ?? "",
    });
    setFormOpen(true);
  };

  const handleSubmitForm = async () => {
    if (form.ratePercent <= 0 || form.ratePercent > 100) {
      showError(null, "Tỷ lệ hoa hồng phải lớn hơn 0 và không vượt quá 100%.");
      return;
    }
    setSaving(true);
    try {
      const payload = {
        dentistId: form.dentistId || null,
        serviceName: form.serviceName || null,
        ratePercent: form.ratePercent,
        effectiveFrom: form.effectiveFrom,
        effectiveTo: form.effectiveTo || null,
        note: form.note.trim() || null,
      };
      if (editingId) {
        await updateCommissionRuleApi(editingId, payload);
        showMessage("Đã cập nhật quy tắc hoa hồng.");
      } else {
        await createCommissionRuleApi(payload);
        showMessage("Đã tạo quy tắc hoa hồng mới.");
      }
      setFormOpen(false);
      reload();
    } catch (err) { showError(err, "Không thể lưu quy tắc hoa hồng"); }
    finally { setSaving(false); }
  };

  const handleToggleActive = async (rule: CommissionRuleDto) => {
    setTogglingId(rule.id);
    try {
      await toggleCommissionRuleActiveApi(rule.id);
      showMessage(rule.isActive ? "Đã tắt quy tắc hoa hồng." : "Đã bật quy tắc hoa hồng.");
      reload();
    } catch (err) { showError(err, "Không thể đổi trạng thái quy tắc hoa hồng"); }
    finally { setTogglingId(null); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await deleteCommissionRuleApi(deleteTarget.id);
      showMessage("Đã xoá quy tắc hoa hồng.");
      setDeleteTarget(null);
      reload();
    } catch (err) { showError(err, "Không thể xoá quy tắc hoa hồng"); }
    finally { setDeleting(false); }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="commissions" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader title="Hoa Hồng" subtitle="Quy tắc hoa hồng theo % doanh thu đã thu, áp dụng cho nha sĩ và/hoặc dịch vụ cụ thể." />

        <Portal>
          <div className="fixed top-24 right-8 z-50 w-[min(28rem,calc(100vw-4rem))] space-y-3 pointer-events-none">
            {successMsg && (
              <div className="animate-fade-in pointer-events-auto bg-emerald-50 border border-emerald-200 text-emerald-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-emerald-900/5">
                <span className="flex-1">{successMsg}</span>
              </div>
            )}
            {errorMsg && (
              <div className="animate-fade-in pointer-events-auto bg-rose-50 border border-rose-200 text-rose-700 px-5 py-3 rounded-2xl text-[13.5px] font-bold flex items-start gap-2 shadow-lg shadow-rose-900/5">
                <span className="flex-1">{errorMsg}</span>
                <button onClick={() => setErrorMsg(null)} className="text-rose-400 hover:text-rose-600 cursor-pointer">✕</button>
              </div>
            )}
          </div>
        </Portal>

        <div className="flex-1 p-8 overflow-y-auto space-y-6">
          <div className="bg-blue-50 border border-blue-100 text-blue-700 px-5 py-3 rounded-2xl text-[13px] font-semibold flex items-center gap-2">
            <svg className="w-4 h-4 text-blue-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
            <span>Doanh thu căn cứ và tiền hoa hồng chỉ tự tính chính xác được cho nha sĩ (gắn trực tiếp với lịch hẹn/hóa đơn). Quy tắc để trống "Nha sĩ" áp dụng chung, không dùng để tính hoa hồng cho từng nhân viên cụ thể.</span>
          </div>

          {/* Bộ lọc thời gian + KPI */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-wrap items-center gap-3">
            <div className="inline-flex rounded-xl border border-slate-200 overflow-hidden shrink-0">
              {PERIOD_OPTIONS.map((opt) => (
                <button key={opt.value} onClick={() => setPreset(opt.value)}
                  className={`px-3.5 py-2 text-[12.5px] font-black cursor-pointer transition-colors whitespace-nowrap ${
                    preset === opt.value ? "bg-primary text-white" : "bg-white text-slate-500 hover:bg-slate-50"
                  }`}>
                  {opt.label}
                </button>
              ))}
            </div>
            {preset === "custom" && (
              <div className="flex items-center gap-2.5 flex-wrap">
                <input type="date" value={customFrom} max={customTo} onChange={(e) => setCustomFrom(e.target.value)}
                  className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />
                <span className="text-[12.5px] text-slate-400 font-semibold">—</span>
                <input type="date" value={customTo} min={customFrom} onChange={(e) => setCustomTo(e.target.value)}
                  className="px-2.5 py-1.5 text-[13px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-650 cursor-pointer" />
              </div>
            )}
            <div className="ml-auto flex items-center gap-4">
              <div className="text-right">
                <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng hoa hồng kỳ này</span>
                <span className="text-lg font-black text-primary block">{loading ? "…" : fmt(totalCommission)}</span>
              </div>
              <button onClick={openCreateForm}
                className="px-4 py-2 bg-primary hover:bg-primary-hover text-white text-xs font-black rounded-xl cursor-pointer shadow-md shadow-primary/20 transition-all whitespace-nowrap">
                + Thêm quy tắc
              </button>
            </div>
          </div>

          {/* Danh sách quy tắc */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-[13.5px] text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-150 bg-slate-50/70 select-none">
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Áp dụng cho</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Dịch vụ</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-right">Tỷ lệ</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-right">Doanh thu căn cứ</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-right">Tiền hoa hồng</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Thời gian áp dụng</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-center">Trạng thái</th>
                    <th className="px-6 py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider text-center">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {loading ? (
                    <tr><td colSpan={8} className="px-6 py-12 text-center text-slate-400 font-semibold animate-pulse">Đang tải danh sách quy tắc...</td></tr>
                  ) : rules.length === 0 ? (
                    <tr><td colSpan={8} className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Chưa có quy tắc hoa hồng nào.</td></tr>
                  ) : rules.map((rule) => (
                    <tr key={rule.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-6 py-4 font-extrabold text-slate-900">{rule.dentistName ?? "Mọi nha sĩ"}</td>
                      <td className="px-6 py-4 font-semibold text-slate-600">{rule.serviceName ?? "Mọi dịch vụ"}</td>
                      <td className="px-6 py-4 text-right font-black text-slate-700">{rule.ratePercent}%</td>
                      <td className="px-6 py-4 text-right font-bold text-slate-600">{fmt(rule.revenueBasis)}</td>
                      <td className="px-6 py-4 text-right font-black text-primary">{fmt(rule.commissionAmount)}</td>
                      <td className="px-6 py-4 font-semibold text-slate-500 whitespace-nowrap">
                        {formatDate(rule.effectiveFrom)} {rule.effectiveTo ? `— ${formatDate(rule.effectiveTo)}` : "trở đi"}
                      </td>
                      <td className="px-6 py-4 text-center">
                        <button onClick={() => handleToggleActive(rule)} disabled={togglingId === rule.id}
                          className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-black cursor-pointer transition-all disabled:opacity-50 ${
                            rule.isActive ? "bg-emerald-50 text-emerald-700 border border-emerald-200" : "bg-slate-100 text-slate-500 border border-slate-250"
                          }`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${rule.isActive ? "bg-emerald-500" : "bg-slate-400"}`} />
                          {rule.isActive ? "Đang bật" : "Đã tắt"}
                        </button>
                      </td>
                      <td className="px-6 py-4 text-center">
                        <div className="flex items-center justify-center gap-2">
                          <button onClick={() => openEditForm(rule)}
                            className="px-2.5 py-1.5 rounded-lg text-xs font-black bg-slate-50 hover:bg-slate-100 text-slate-600 border border-slate-200 cursor-pointer transition-all">
                            Sửa
                          </button>
                          <button onClick={() => setDeleteTarget(rule)}
                            className="px-2.5 py-1.5 rounded-lg text-xs font-black bg-red-50 hover:bg-red-100 text-primary border border-red-200 cursor-pointer transition-all">
                            Xoá
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </main>

      {/* Modal thêm/sửa quy tắc */}
      {formOpen && (
        <Portal>
          <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4"
            onClick={() => !saving && setFormOpen(false)} role="dialog" aria-modal="true">
            <div className="animate-fade-in bg-white rounded-2xl shadow-2xl w-full max-w-lg p-6 max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
              <h2 className="text-lg font-extrabold text-slate-900">{editingId ? "Sửa quy tắc hoa hồng" : "Thêm quy tắc hoa hồng"}</h2>

              <div className="mt-4 space-y-4">
                <div>
                  <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Nha sĩ áp dụng</label>
                  <select value={form.dentistId} onChange={(e) => setForm((f) => ({ ...f, dentistId: e.target.value }))}
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 text-[13.5px] cursor-pointer">
                    <option value="">Mọi nha sĩ</option>
                    {dentists.map((d) => <option key={d.id} value={d.id}>{d.fullName}</option>)}
                  </select>
                </div>

                <div>
                  <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Dịch vụ áp dụng</label>
                  <select value={form.serviceName} onChange={(e) => setForm((f) => ({ ...f, serviceName: e.target.value }))}
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 text-[13.5px] cursor-pointer">
                    <option value="">Mọi dịch vụ</option>
                    {services.map((s) => <option key={s.id} value={s.name}>{s.name}</option>)}
                  </select>
                </div>

                <div>
                  <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Tỷ lệ hoa hồng (%)</label>
                  <input type="number" min={0.1} max={100} step={0.1} value={form.ratePercent}
                    onChange={(e) => setForm((f) => ({ ...f, ratePercent: Number(e.target.value) || 0 }))}
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 text-[13.5px]" />
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Hiệu lực từ ngày</label>
                    <input type="date" value={form.effectiveFrom} onChange={(e) => setForm((f) => ({ ...f, effectiveFrom: e.target.value }))}
                      className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 text-[13.5px] cursor-pointer" />
                  </div>
                  <div>
                    <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Đến ngày (tùy chọn)</label>
                    <input type="date" value={form.effectiveTo} min={form.effectiveFrom} onChange={(e) => setForm((f) => ({ ...f, effectiveTo: e.target.value }))}
                      className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-bold text-slate-700 text-[13.5px] cursor-pointer" />
                  </div>
                </div>

                <div>
                  <label className="text-[12px] font-bold text-slate-500 block mb-1.5">Ghi chú (tùy chọn)</label>
                  <input type="text" value={form.note} onChange={(e) => setForm((f) => ({ ...f, note: e.target.value }))}
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 text-[13.5px]" />
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 mt-6">
                <button onClick={() => setFormOpen(false)} disabled={saving}
                  className="px-4 py-2.5 rounded-xl border border-slate-200 bg-white text-slate-600 text-xs font-black hover:bg-slate-50 cursor-pointer transition-all disabled:opacity-50">
                  Hủy
                </button>
                <button onClick={handleSubmitForm} disabled={saving}
                  className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-hover disabled:opacity-50 text-white text-xs font-black cursor-pointer shadow-md shadow-primary/20 transition-all">
                  {saving ? "Đang lưu..." : editingId ? "Lưu thay đổi" : "Tạo quy tắc"}
                </button>
              </div>
            </div>
          </div>
        </Portal>
      )}

      {/* Xác nhận xoá */}
      {deleteTarget && (
        <Portal>
          <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4"
            onClick={() => !deleting && setDeleteTarget(null)} role="dialog" aria-modal="true">
            <div className="animate-fade-in bg-white rounded-2xl shadow-2xl w-full max-w-md p-6" onClick={(e) => e.stopPropagation()}>
              <h2 className="text-lg font-extrabold text-slate-900">Xác nhận xoá quy tắc</h2>
              <p className="text-[13.5px] text-slate-500 font-semibold mt-1.5 leading-relaxed">
                Xoá quy tắc hoa hồng <span className="font-black text-slate-700">{deleteTarget.ratePercent}%</span> cho{" "}
                <span className="font-black text-slate-700">{deleteTarget.dentistName ?? "mọi nha sĩ"}</span>? Thao tác này không thể hoàn tác.
              </p>
              <div className="flex items-center justify-end gap-3 mt-6">
                <button onClick={() => setDeleteTarget(null)} disabled={deleting}
                  className="px-4 py-2.5 rounded-xl border border-slate-200 bg-white text-slate-600 text-xs font-black hover:bg-slate-50 cursor-pointer transition-all disabled:opacity-50">
                  Hủy
                </button>
                <button onClick={handleDelete} disabled={deleting}
                  className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-hover disabled:opacity-50 text-white text-xs font-black cursor-pointer shadow-md shadow-primary/20 transition-all">
                  {deleting ? "Đang xoá..." : "Xoá"}
                </button>
              </div>
            </div>
          </div>
        </Portal>
      )}
    </div>
  );
}
