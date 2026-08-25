"use client";

import { useState, useEffect, useCallback } from "react";
import { createPortal } from "react-dom";
import {
  getPatientMedicalHistoryApi,
  getInvoicesByPatientApi,
  type PatientMedicalHistoryDto,
  type InvoiceDto,
} from "../../lib/apiClient";

/* ─── helpers ────────────────────────────────────────────── */

const fmt = (n: number) => n.toLocaleString("vi-VN") + "₫";

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? "—" : `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
};

const fmtDateTime = (iso: string) => {
  const d = new Date(iso);
  if (isNaN(d.getTime())) return "—";
  return `${fmtDate(iso)} ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
};

const initialsOf = (name: string) =>
  name.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();

const INVOICE_STATUS_CFG: Record<string, { label: string; badge: string }> = {
  Unpaid:   { label: "Chưa thanh toán", badge: "bg-orange-50 text-orange-700 border-orange-200" },
  Paid:     { label: "Đã thanh toán",   badge: "bg-emerald-50 text-emerald-700 border-emerald-200" },
  Refunded: { label: "Đã hoàn tiền",    badge: "bg-slate-100 text-slate-500 border-slate-200" },
};

const invoiceStatusCfg = (status: string) =>
  INVOICE_STATUS_CFG[status] ?? { label: status, badge: "bg-slate-100 text-slate-500 border-slate-200" };

const PAYMENT_METHOD_LABEL: Record<string, string> = {
  Cash: "Tiền mặt",
  BankTransfer: "Chuyển khoản",
  OnlinePayment: "Thanh toán online",
};

/* ─── component ──────────────────────────────────────────── */

type Tab = "history" | "invoices";

/**
 * Chi tiết bệnh nhân xem từ danh sách ca khám (owner/staff/dentist): lịch sử khám và hóa đơn/thanh
 * toán, gộp trong một modal thay vì phải mở nhiều trang riêng lẻ.
 */
export default function PatientDetailModal({
  patientId,
  patientName,
  patientPhone,
  defaultTab = "history",
  onClose,
}: {
  patientId: string;
  patientName: string;
  patientPhone?: string | null;
  defaultTab?: Tab;
  onClose: () => void;
}) {
  const [tab, setTab] = useState<Tab>(defaultTab);

  const [history, setHistory] = useState<PatientMedicalHistoryDto[]>([]);
  const [historyLoading, setHistoryLoading] = useState(true);
  const [historyError, setHistoryError] = useState<string | null>(null);

  const [invoices, setInvoices] = useState<InvoiceDto[]>([]);
  const [invoicesLoading, setInvoicesLoading] = useState(true);
  const [invoicesError, setInvoicesError] = useState<string | null>(null);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);
    setHistoryError(null);
    try {
      setHistory(await getPatientMedicalHistoryApi(patientId));
    } catch (e) {
      setHistoryError(e instanceof Error ? e.message : "Không thể tải lịch sử khám");
    } finally {
      setHistoryLoading(false);
    }
  }, [patientId]);

  const loadInvoices = useCallback(async () => {
    setInvoicesLoading(true);
    setInvoicesError(null);
    try {
      setInvoices(await getInvoicesByPatientApi(patientId));
    } catch (e) {
      setInvoicesError(e instanceof Error ? e.message : "Không thể tải hóa đơn");
    } finally {
      setInvoicesLoading(false);
    }
  }, [patientId]);

  useEffect(() => { void loadHistory(); void loadInvoices(); }, [loadHistory, loadInvoices]);

  if (typeof document === "undefined") return null;

  const paidTotal = invoices.filter(i => i.status === "Paid").reduce((s, i) => s + i.totalAmount, 0);
  const outstandingTotal = invoices.filter(i => i.status === "Unpaid").reduce((s, i) => s + i.remainingAmount, 0);

  return createPortal(
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-[9998] bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 sm:p-6"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-2xl shadow-xl w-full max-w-3xl max-h-[92vh] flex flex-col overflow-hidden"
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div className="px-5 sm:px-6 py-4 border-b border-slate-100 flex items-start justify-between gap-4 shrink-0">
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-11 h-11 rounded-xl bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-[13px] text-sky-700 shrink-0">
              {initialsOf(patientName)}
            </div>
            <div className="min-w-0">
              <h3 className="text-[16px] font-black text-slate-900 truncate">{patientName}</h3>
              <p className="text-[12.5px] font-semibold text-slate-500 mt-0.5">{patientPhone ?? "Chưa có SĐT"}</p>
            </div>
          </div>
          <button onClick={onClose} className="w-8 h-8 rounded-xl text-slate-400 hover:bg-slate-100 hover:text-slate-600 flex items-center justify-center shrink-0 cursor-pointer transition-colors">
            <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
          </button>
        </div>

        {/* Tabs */}
        <div className="px-5 sm:px-6 pt-3 flex items-center gap-1.5 border-b border-slate-100 shrink-0">
          {([
            { key: "history", label: "Lịch sử khám" },
            { key: "invoices", label: "Hóa đơn & thanh toán" },
          ] as const).map(t => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`px-3.5 py-2 text-[12.5px] font-black rounded-t-xl border-b-2 -mb-px transition-colors cursor-pointer ${
                tab === t.key ? "border-primary text-primary" : "border-transparent text-slate-400 hover:text-slate-600"
              }`}
            >
              {t.label}
            </button>
          ))}
        </div>

        {/* Body */}
        <div className="flex-1 min-h-0 overflow-y-auto px-5 sm:px-6 py-5">
          {tab === "history" ? (
            historyLoading ? (
              <div className="py-14 flex items-center justify-center">
                <div className="w-6 h-6 border-2 border-primary/20 border-t-primary rounded-full animate-spin" />
              </div>
            ) : historyError ? (
              <div className="py-10 flex flex-col items-center gap-3">
                <p className="text-[13px] font-semibold text-red-500">{historyError}</p>
                <button onClick={loadHistory} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">Thử lại</button>
              </div>
            ) : history.length === 0 ? (
              <p className="py-14 text-center text-[13px] font-semibold text-slate-400">Chưa có lịch sử khám nào.</p>
            ) : (
              <div className="flex flex-col gap-3">
                {history.map(h => (
                  <div key={h.appointmentId} className="border border-slate-200 rounded-xl p-4">
                    <div className="flex items-start justify-between gap-3 flex-wrap">
                      <div>
                        <div className="text-[13.5px] font-black text-slate-900">{h.serviceName || "Chưa gán dịch vụ"}</div>
                        <div className="text-[12px] font-semibold text-slate-400 mt-0.5">
                          {fmtDate(h.appointmentDate)} · BS {h.dentistName} · {h.appointmentCode}
                        </div>
                      </div>
                    </div>

                    {h.symptoms && (
                      <p className="mt-2 text-[12.5px] font-semibold text-amber-600">Triệu chứng: {h.symptoms}</p>
                    )}

                    {h.diagnoses.length > 0 && (
                      <div className="mt-2.5">
                        <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Chẩn đoán</div>
                        <ul className="mt-1 flex flex-col gap-1">
                          {h.diagnoses.map((d, i) => (
                            <li key={i} className="text-[12.5px] font-semibold text-slate-700">
                              {d.description}{d.conclusion ? ` — ${d.conclusion}` : ""}
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {h.treatmentPlans.length > 0 && (
                      <div className="mt-2.5">
                        <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Liệu trình điều trị</div>
                        <ul className="mt-1 flex flex-col gap-1">
                          {h.treatmentPlans.map((p, i) => (
                            <li key={i} className="text-[12.5px] font-semibold text-slate-700 flex items-center justify-between gap-2">
                              <span>{p.description} <span className="text-slate-400 font-bold">({p.status})</span></span>
                              {p.estimatedCost != null && <span className="font-mono font-bold text-slate-500 shrink-0">{fmt(p.estimatedCost)}</span>}
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {h.prescriptionItems.length > 0 && (
                      <div className="mt-2.5">
                        <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Đơn thuốc</div>
                        <ul className="mt-1 flex flex-col gap-1">
                          {h.prescriptionItems.map((p, i) => (
                            <li key={i} className="text-[12.5px] font-semibold text-slate-700">
                              {p.medicineName} — {p.quantity} {p.unit} ({p.dosage})
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )
          ) : invoicesLoading ? (
            <div className="py-14 flex items-center justify-center">
              <div className="w-6 h-6 border-2 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : invoicesError ? (
            <div className="py-10 flex flex-col items-center gap-3">
              <p className="text-[13px] font-semibold text-red-500">{invoicesError}</p>
              <button onClick={loadInvoices} className="px-4 py-2 text-[13px] font-bold bg-primary text-white rounded-xl cursor-pointer">Thử lại</button>
            </div>
          ) : invoices.length === 0 ? (
            <p className="py-14 text-center text-[13px] font-semibold text-slate-400">Chưa có hóa đơn nào.</p>
          ) : (
            <div className="flex flex-col gap-3">
              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-xl bg-emerald-50 border border-emerald-100 p-3">
                  <div className="text-[11px] font-extrabold text-emerald-600 uppercase tracking-wider">Đã thu</div>
                  <div className="text-[16px] font-black text-emerald-700 font-mono mt-0.5">{fmt(paidTotal)}</div>
                </div>
                <div className="rounded-xl bg-orange-50 border border-orange-100 p-3">
                  <div className="text-[11px] font-extrabold text-orange-600 uppercase tracking-wider">Còn nợ</div>
                  <div className="text-[16px] font-black text-orange-700 font-mono mt-0.5">{fmt(outstandingTotal)}</div>
                </div>
              </div>

              {invoices.map(inv => {
                const cfg = invoiceStatusCfg(inv.status);
                return (
                  <div key={inv.id} className="border border-slate-200 rounded-xl p-4">
                    <div className="flex items-start justify-between gap-3 flex-wrap">
                      <div>
                        <div className="text-[13px] font-mono font-bold text-slate-500">{inv.invoiceNumber}</div>
                        <div className="text-[12px] font-semibold text-slate-400 mt-0.5">
                          {fmtDate(inv.appointmentDate)} · BS {inv.dentistName}
                        </div>
                      </div>
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[11.5px] font-black border whitespace-nowrap ${cfg.badge}`}>
                        {cfg.label}
                      </span>
                    </div>

                    <ul className="mt-2.5 flex flex-col gap-1">
                      {inv.items.map((it, i) => (
                        <li key={i} className="text-[12.5px] font-semibold text-slate-700 flex items-center justify-between gap-2">
                          <span>{it.name} × {it.quantity}</span>
                          <span className="font-mono font-bold text-slate-500 shrink-0">{fmt(it.lineTotal)}</span>
                        </li>
                      ))}
                    </ul>

                    <div className="mt-3 pt-3 border-t border-slate-100 flex items-center justify-between gap-3 flex-wrap">
                      <div className="text-[12px] font-semibold text-slate-400">
                        {PAYMENT_METHOD_LABEL[inv.paymentMethod] ?? inv.paymentMethod}
                        {inv.paymentDate && <> · thu lúc {fmtDateTime(inv.paymentDate)}</>}
                        {inv.status === "Unpaid" && inv.remainingAmount > 0 && (
                          <span className="text-orange-600"> · còn nợ {fmt(inv.remainingAmount)}</span>
                        )}
                      </div>
                      <div className="text-[15px] font-black text-slate-900 font-mono">{fmt(inv.totalAmount)}</div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>,
    document.body
  );
}
