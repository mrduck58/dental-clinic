"use client";

import { useState, useEffect, useCallback } from "react";
import { QRCodeSVG } from "qrcode.react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getBillablePlansApi,
  getPendingInvoicesApi,
  getInvoiceHistoryApi,
  getOutstandingInvoicesApi,
  getOutstandingPlansApi,
  issueInvoiceApi,
  confirmInvoicePaymentApi,
  collectRemainingInvoiceApi,
  createPaymentRequestApi,
  getPaymentStatusApi,
  getPromotionsApi,
  type BillablePlanDto,
  type InvoiceDto,
  type OutstandingPlanDto,
  type PaymentTransactionDto,
  type PromotionDto,
} from "../../../lib/apiClient";

/* ─── types ─────────────────────────────────────────────── */

interface Procedure {
  name: string; qty: number; price: number; treatmentPlanId?: string | null;
  // Thanh toán theo từng dòng: "full" = thu toàn bộ dòng; "deposit" = đặt cọc theo % (`depositPct`).
  payType?: "full" | "deposit";
  depositPct?: number;
}

// % cọc hợp lệ (0, 100].
const clampPct = (p: number) => Math.min(100, Math.max(0, p));

// Số tiền thu ngay của một dòng dịch vụ (đặt cọc = thành tiền × %/100, làm tròn).
const lineCollected = (it: Procedure) =>
  it.payType === "deposit"
    ? Math.round((it.qty * it.price) * clampPct(it.depositPct ?? 0) / 100)
    : it.qty * it.price;

interface TreatmentPlan {
  id: string; patientName: string; patientPhone: string; gender: "Nam" | "Nữ";
  dentist: string; date: string; diagnosis: string;
  procedures: Procedure[];
  // Khi mục này là "thu phần còn lại" của một hóa đơn đặt cọc
  outstandingInvoiceId?: string | null;
  sourceInvoiceNumber?: string | null;
  // Khi mục này là một đợt thu của liệu trình điều trị
  treatmentPlanId?: string | null;
  planName?: string | null;
  planTotal?: number;
  planAmountPaid?: number;
  planRemaining?: number;
}

type PayMethod = "cash" | "transfer" | "app";
type PayType = "full" | "deposit";
type InvStatus = "pending" | "awaiting_payment" | "paid";

interface Invoice {
  id: string; planId?: string;
  patientName: string; patientPhone: string; gender: "Nam" | "Nữ";
  dentist: string; date: string;
  // Ngày thực thu — tab "Lịch sử" lọc và hiển thị theo ngày này, không phải ngày hẹn.
  paidDate: string | null;
  items: Procedure[];
  subtotal: number; discount: number; finalTotal: number;
  promotionId?: string | null; promotionCode?: string | null; promotionName?: string | null;
  paymentType: PayType; depositAmount: number; remaining: number;
  paymentMethod: PayMethod | null;
  status: InvStatus; note: string;
  // Công nợ
  parentInvoiceId?: string | null;
  isSettled?: boolean;
  collectingRemaining?: boolean;
  treatmentPlanId?: string | null;
}

/* ─── API ↔ UI mappers ──────────────────────────────────── */

const apiToPayMethod = (m: string): PayMethod =>
  m === "Cash" ? "cash" : m === "BankTransfer" ? "transfer" : "app";

const toGender = (g: string | null): "Nam" | "Nữ" => (g === "Nữ" ? "Nữ" : "Nam");

const fmtDate = (iso: string) =>
  new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });

function mapPlan(b: BillablePlanDto): TreatmentPlan {
  return {
    id: b.appointmentId,
    patientName: b.patientName,
    patientPhone: b.patientPhone ?? "—",
    gender: toGender(b.gender),
    dentist: b.dentistName,
    date: fmtDate(b.appointmentDate),
    diagnosis: b.diagnosis || "Chưa có chẩn đoán",
    procedures: b.items.map(i => ({ name: i.name, qty: i.quantity, price: i.unitPrice, treatmentPlanId: i.treatmentPlanId })),
    outstandingInvoiceId: b.outstandingInvoiceId,
    sourceInvoiceNumber: b.sourceInvoiceNumber,
    treatmentPlanId: b.treatmentPlanId,
    planName: b.planName,
    planTotal: b.planTotal,
    planAmountPaid: b.planAmountPaid,
    planRemaining: b.planRemaining,
  };
}

function mapInvoice(inv: InvoiceDto): Invoice {
  const method = apiToPayMethod(inv.paymentMethod);
  const status: InvStatus =
    inv.status === "Paid" ? "paid" : (method === "app" || method === "transfer") ? "awaiting_payment" : "pending";
  return {
    id: inv.id,
    planId: inv.invoiceNumber,
    patientName: inv.patientName,
    patientPhone: inv.patientPhone ?? "—",
    gender: toGender(inv.gender),
    dentist: inv.dentistName,
    date: fmtDate(inv.appointmentDate),
    paidDate: inv.paymentDate ? fmtDate(inv.paymentDate) : null,
    items: inv.items.map(i => ({ name: i.name, qty: i.quantity, price: i.unitPrice, treatmentPlanId: i.treatmentPlanId })),
    subtotal: inv.subtotal,
    discount: inv.discount,
    finalTotal: inv.totalAmount,
    promotionId: inv.promotionId,
    promotionCode: inv.promotionCode,
    promotionName: inv.promotionName,
    paymentType: inv.paymentType === "Deposit" ? "deposit" : "full",
    depositAmount: inv.depositAmount,
    remaining: inv.remainingAmount,
    paymentMethod: method,
    status,
    note: inv.notes ?? "",
    parentInvoiceId: inv.parentInvoiceId,
    isSettled: inv.isSettled,
    collectingRemaining: inv.collectingRemaining,
  };
}

/* ─── helpers ────────────────────────────────────────────── */

const fmt = (n: number) => n.toLocaleString("vi-VN") + "₫";

// Ô nhập tiền: hiển thị có dấu phân cách (5.000.000), parse về số.
const fmtMoneyInput = (n: number) => (n ? n.toLocaleString("vi-VN") : "");
const parseMoneyInput = (s: string) => Number(s.replace(/[^\d]/g, "")) || 0;

// Ngày local dạng "YYYY-MM-DD" (cho input type=date)
const todayIso = () => {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};

// "YYYY-MM-DD" → "DD/MM/YYYY" để so khớp với field date đã format theo vi-VN
const isoToVi = (iso: string) => {
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
};
const sum = (items: Procedure[]) => items.reduce((s, i) => s + i.qty * i.price, 0);

const PAY_CFG: Record<PayMethod, { label: string; icon: string; color: string; bg: string; border: string }> = {
  cash:     { label: "Tiền mặt",    icon: "M2.25 18.75a60.07 60.07 0 0115.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 013 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 00-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 01-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 003 15h-.75M15 10.5a3 3 0 11-6 0 3 3 0 016 0zm3 0h.008v.008H18V10.5zm-12 0h.008v.008H6V10.5z", color: "text-emerald-700", bg: "bg-emerald-50", border: "border-emerald-200" },
  transfer: { label: "Chuyển khoản", icon: "M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5",                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   color: "text-sky-700",     bg: "bg-sky-50",     border: "border-sky-200"     },
  app:      { label: "Thanh toán App",icon: "M10.5 1.5H8.25A2.25 2.25 0 006 3.75v16.5a2.25 2.25 0 002.25 2.25h7.5A2.25 2.25 0 0018 20.25V3.75a2.25 2.25 0 00-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 8.25h3m-3 3h3m-3 3h3M6.75 21h10.5",                                                                                                                                                                                                                                                                                                                                                                    color: "text-indigo-700",  bg: "bg-indigo-50",  border: "border-indigo-200" },
};

const DENTIST_COLOR: Record<string, string> = {
  "BS. Thảo": "bg-sky-50 text-sky-700 border-sky-200",
  "BS. Minh":  "bg-violet-50 text-violet-700 border-violet-200",
  "BS. Linh":  "bg-rose-50 text-rose-700 border-rose-200",
  "BS. Hùng":  "bg-amber-50 text-amber-700 border-amber-200",
};

const inputCls = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";
const labelCls = "text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider";

/* ─── Tab 1: Treatment plan → Invoice ───────────────────── */

function PlansTab({ plans, promotions, onIssued }: {
  plans: TreatmentPlan[];
  promotions: PromotionDto[];
  onIssued: (inv: Invoice) => Promise<boolean>;
}) {
  const [selected, setSelected] = useState<TreatmentPlan | null>(null);
  const [items,    setItems]    = useState<Procedure[]>([]);
  const [note,     setNote]     = useState("");
  const [installAmount, setInstallAmount] = useState(0);
  const [method,   setMethod]   = useState<PayMethod | null>(null);
  const [saved,    setSaved]    = useState(false);

  // Khuyến mãi — nhận từ trang cha (đã tải song song cùng các danh sách khác), chỉ lọc còn hiệu lực
  // (đang bật + trong khoảng ngày) ở đây.
  const [selectedPromotionId, setSelectedPromotionId] = useState<string | null>(null);

  const todayIsoForPromo = todayIso();
  const activePromotions = promotions.filter(p =>
    p.isActive && p.startDate <= todayIsoForPromo && p.endDate >= todayIsoForPromo);

  const selectPlan = (p: TreatmentPlan) => {
    setSelected(p);
    setItems(p.procedures.map(pr => ({ ...pr, payType: "full" as const, depositPct: 50 })));
    setNote("");
    setInstallAmount(p.planRemaining ?? 0); setMethod(null); setSaved(false);
    setSelectedPromotionId(null);
  };

  const updateQty = (i: number, qty: number) =>
    setItems(prev => prev.map((it, idx) => idx === i ? { ...it, qty: Math.max(1, qty) } : it));

  const removeItem = (i: number) =>
    setItems(prev => prev.filter((_, idx) => idx !== i));

  const setItemPay = (i: number, payType: "full" | "deposit") =>
    setItems(prev => prev.map((it, idx) => idx === i ? { ...it, payType, depositPct: payType === "deposit" && !it.depositPct ? 50 : it.depositPct } : it));

  const setItemDepositPct = (i: number, pct: number) =>
    setItems(prev => prev.map((it, idx) => idx === i ? { ...it, depositPct: pct } : it));

  // Mục "thu phần còn lại" của hóa đơn đặt cọc → chỉ thanh toán toàn bộ phần còn lại.
  const isRemaining = !!selected?.outstandingInvoiceId;
  // Mục "đợt thu" của liệu trình điều trị (dữ liệu cũ) → nhập số tiền đợt này.
  const isInstallment = !!selected?.treatmentPlanId;
  const planRemaining = selected?.planRemaining ?? 0;

  const subtotal   = sum(items);

  // Khuyến mãi chỉ áp dụng cho hóa đơn xuất mới (không áp dụng cho "thu phần còn lại" / "đợt thu liệu trình").
  const selectedPromotion = !isRemaining && !isInstallment
    ? activePromotions.find(p => p.id === selectedPromotionId) ?? null
    : null;
  const discountAmount = selectedPromotion
    ? Math.min(subtotal, selectedPromotion.discountType === "Percentage"
        ? Math.round(subtotal * selectedPromotion.discountValue / 100)
        : selectedPromotion.discountValue)
    : 0;
  const finalTotal = Math.max(0, subtotal - discountAmount);

  // Thu ngay = tổng số thu của từng dòng (toàn bộ / đặt cọc theo dòng), quy đổi theo tỉ lệ giảm giá
  // (nếu có áp dụng khuyến mãi) để tổng thu không bao giờ vượt quá tổng hóa đơn đã giảm giá.
  const collectRatio = subtotal > 0 && discountAmount > 0 ? finalTotal / subtotal : 1;
  const collectedTotal = items.reduce((s, it) => s + Math.floor(lineCollected(it) * collectRatio), 0);
  const perLineOk = items.every(it => it.payType !== "deposit" || ((it.depositPct ?? 0) > 0 && (it.depositPct ?? 0) <= 100));

  const installOk  = !isInstallment || (installAmount > 0 && installAmount <= planRemaining);
  const canIssue   = !!method && (
    isRemaining ? true :
    isInstallment ? installOk :
    (items.length > 0 && perLineOk && collectedTotal > 0)
  );

  const [issuing, setIssuing] = useState(false);

  const handleIssue = async () => {
    if (!selected || !canIssue) return;
    const inv: Invoice = isInstallment
      ? {
          id: selected.id,
          planId: selected.id,
          patientName: selected.patientName,
          patientPhone: selected.patientPhone,
          gender: selected.gender,
          dentist: selected.dentist,
          date: selected.date,
          paidDate: null,   // hóa đơn vừa lập, chưa thu
          items: [{ name: `Đợt thu - ${selected.planName}`, qty: 1, price: installAmount }],
          subtotal: installAmount, discount: 0, finalTotal: installAmount,
          paymentType: "full",
          depositAmount: installAmount,
          remaining: Math.max(0, planRemaining - installAmount),
          paymentMethod: method,
          status: "pending",
          note,
          treatmentPlanId: selected.treatmentPlanId,
        }
      : {
          id: selected.id,        // transient; planId carries the appointmentId
          planId: selected.id,
          patientName: selected.patientName,
          patientPhone: selected.patientPhone,
          gender: selected.gender,
          dentist: selected.dentist,
          date: selected.date,
          paidDate: null,   // hóa đơn vừa lập, chưa thu
          items: [...items],
          subtotal, discount: discountAmount, finalTotal,
          promotionId: selectedPromotion?.id ?? null,
          promotionCode: selectedPromotion?.code ?? null,
          promotionName: selectedPromotion?.name ?? null,
          paymentType: collectedTotal < finalTotal ? "deposit" : "full",
          depositAmount: collectedTotal,
          remaining: Math.max(0, finalTotal - collectedTotal),
          paymentMethod: method,
          status: "pending",
          note,
          parentInvoiceId: selected.outstandingInvoiceId,
        };
    setIssuing(true);
    const ok = await onIssued(inv);
    setIssuing(false);
    if (ok) {
      setSaved(true);
      setTimeout(() => { setSaved(false); setSelected(null); }, 2200);
    }
  };

  return (
    <div className="flex flex-col lg:flex-row gap-6 items-start">
      {/* Plans sidebar */}
      <div className="w-full lg:w-80 shrink-0 flex flex-col gap-3">
        <div className="flex items-center gap-2">
          <span className={labelCls}>Liệu trình chờ xuất hóa đơn</span>
          <span className="text-[12px] font-black text-slate-400">{plans.length}</span>
        </div>
        {plans.length === 0 ? (
          <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-2 py-14">
            <svg className="w-9 h-9 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>
            <span className="text-[13px] font-bold text-slate-400">Không còn liệu trình chờ</span>
          </div>
        ) : (
          plans.map(p => {
            const isSelected = selected?.id === p.id;
            return (
              <button key={p.id} onClick={() => selectPlan(p)}
                className={`flex rounded-2xl border overflow-hidden text-left w-full transition-all cursor-pointer group ${
                  isSelected
                    ? "border-primary shadow-md shadow-primary/10"
                    : "border-slate-200/70 bg-white shadow-sm hover:shadow-md hover:-translate-y-px"
                }`}>
                <div className={`w-1.5 shrink-0 ${isSelected ? "bg-primary" : "bg-slate-200 group-hover:bg-slate-400"}`} />
                <div className="flex flex-col gap-2.5 px-4 py-4 flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <div className="text-[14px] font-black text-slate-900 leading-tight">{p.patientName}</div>
                      <div className="text-[12px] text-slate-400 font-mono mt-0.5">{p.patientPhone}</div>
                    </div>
                    <span className={`text-[11.5px] font-black px-2 py-0.5 rounded-lg border shrink-0 ${DENTIST_COLOR[p.dentist] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>
                      {p.dentist}
                    </span>
                  </div>
                  <p className="text-[12px] text-slate-500 font-semibold leading-snug">{p.diagnosis}</p>
                  {p.outstandingInvoiceId && (
                    <span className="self-start text-[10.5px] font-black px-2 py-0.5 rounded-md bg-orange-100 text-orange-700 uppercase tracking-wide">
                      Thu phần còn lại · {p.sourceInvoiceNumber}
                    </span>
                  )}
                  {p.treatmentPlanId && (
                    <span className="self-start text-[10.5px] font-black px-2 py-0.5 rounded-md bg-indigo-100 text-indigo-700 uppercase tracking-wide">
                      Đợt thu liệu trình
                    </span>
                  )}
                  <div className="flex items-center justify-between">
                    <span className="text-[11.5px] font-semibold text-slate-400">{p.treatmentPlanId ? "Liệu trình điều trị" : p.outstandingInvoiceId ? "Phần còn lại" : `${p.procedures.length} dịch vụ`}</span>
                    <span className="text-[13px] font-black text-slate-700">{fmt(p.treatmentPlanId ? (p.planRemaining ?? 0) : sum(p.procedures))}</span>
                  </div>
                </div>
              </button>
            );
          })
        )}
      </div>

      {/* Invoice form */}
      <div className={`flex-1 min-w-0 w-full ${!selected && !saved ? "hidden lg:block" : ""}`}>
        {saved ? (
          <div className="bg-white rounded-2xl border border-emerald-200 shadow-sm flex flex-col items-center gap-3 py-20">
            <div className="w-14 h-14 rounded-full bg-emerald-50 border border-emerald-100 flex items-center justify-center">
              <svg className="w-7 h-7 text-emerald-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
            </div>
            <p className="text-[16px] font-black text-slate-900">Đã xuất hóa đơn thành công</p>
            <p className="text-[13px] font-semibold text-slate-500">Hóa đơn đã chuyển sang tab <strong>Chờ thanh toán</strong></p>
          </div>
        ) : !selected ? (
          <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-24">
            <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center">
              <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" /></svg>
            </div>
            <p className="text-[14px] font-bold text-slate-500">Chọn một liệu trình để tạo hóa đơn</p>
          </div>
        ) : (
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            {/* Header */}
            <div className="px-5 sm:px-7 py-4 sm:py-5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setSelected(null)}
                  className="lg:hidden p-2 -ml-2 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-all shrink-0"
                  title="Quay lại danh sách"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" /></svg>
                </button>
                <div>
                  <h3 className="text-[16px] font-black text-slate-900">Hóa đơn điều trị</h3>
                  <p className="text-[12.5px] font-semibold text-slate-400 mt-0.5">Từ liệu trình {selected.id} · {selected.date}</p>
                </div>
              </div>
              <div className="flex items-center gap-2.5">
                <div className={`w-10 h-10 rounded-xl flex items-center justify-center font-black text-[12px] border ${
                  selected.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"
                }`}>
                  {selected.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                </div>
                <div>
                  <div className="text-[14px] font-black text-slate-900">{selected.patientName}</div>
                  <div className="text-[12px] font-mono text-slate-400">{selected.patientPhone}</div>
                </div>
                <span className={`ml-1 text-[11.5px] font-black px-2.5 py-1 rounded-lg border ${DENTIST_COLOR[selected.dentist] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>
                  {selected.dentist}
                </span>
              </div>
            </div>

            <div className="px-7 py-6 flex flex-col gap-6">
              {/* Diagnosis */}
              <div className="flex items-start gap-2.5 px-4 py-3 bg-sky-50 border border-sky-100 rounded-xl">
                <svg className="w-4 h-4 text-sky-500 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                <div>
                  <span className="text-[11px] font-extrabold text-sky-700 uppercase tracking-wider">Chẩn đoán</span>
                  <p className="text-[13px] font-semibold text-slate-700 mt-0.5">{selected.diagnosis}</p>
                </div>
              </div>

              {/* Plan installment — đợt thu liệu trình điều trị */}
              {isInstallment && (
                <div className="flex flex-col gap-4">
                  <div className="grid grid-cols-3 gap-3">
                    {[
                      { label: "Tổng chi phí", value: selected.planTotal ?? 0, cls: "text-slate-900" },
                      { label: "Đã thu", value: selected.planAmountPaid ?? 0, cls: "text-emerald-600" },
                      { label: "Còn lại", value: planRemaining, cls: "text-orange-600" },
                    ].map(s => (
                      <div key={s.label} className="bg-slate-50 border border-slate-100 rounded-xl px-4 py-3">
                        <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{s.label}</div>
                        <div className={`text-[16px] font-black mt-0.5 font-mono ${s.cls}`}>{fmt(s.value)}</div>
                      </div>
                    ))}
                  </div>
                  <div className="flex flex-col gap-1.5 w-64">
                    <label className={labelCls}>Số tiền thu đợt này (₫)</label>
                    <input type="text" inputMode="numeric" value={fmtMoneyInput(installAmount)}
                      onChange={e => setInstallAmount(parseMoneyInput(e.target.value))}
                      placeholder="0" className={inputCls} />
                    {installAmount > planRemaining && (
                      <p className="text-[12px] font-bold text-red-600">Số tiền vượt quá công nợ còn lại.</p>
                    )}
                  </div>
                </div>
              )}

              {!isInstallment && (<>
              {/* Items table */}
              <div className="flex flex-col gap-2">
                <span className={labelCls}>Dịch vụ & thủ thuật</span>
                <div className="rounded-xl border border-slate-200 overflow-hidden">
                  <table className="w-full text-[13px]">
                    <thead>
                      <tr className="bg-slate-50 border-b border-slate-200">
                        <th className="px-4 py-2.5 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Dịch vụ</th>
                        <th className="px-3 py-2.5 text-center font-extrabold text-slate-400 text-[11px] uppercase tracking-wider w-24">Số lượng</th>
                        <th className="px-3 py-2.5 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider w-32">Đơn giá</th>
                        <th className="px-3 py-2.5 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider w-32">Thành tiền</th>
                        <th className="px-3 py-2.5 text-center font-extrabold text-slate-400 text-[11px] uppercase tracking-wider w-48">Thanh toán</th>
                        <th className="w-10" />
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {items.map((it, i) => (
                        <tr key={i} className="hover:bg-slate-50/50">
                          <td className="px-4 py-3 font-semibold text-slate-700">{it.name}</td>
                          <td className="px-3 py-2.5 text-center">
                            <div className="flex items-center justify-center gap-1.5">
                              <button onClick={() => updateQty(i, it.qty - 1)} className="w-6 h-6 rounded-lg bg-slate-100 text-slate-500 hover:bg-slate-200 cursor-pointer text-[15px] font-bold flex items-center justify-center leading-none transition-colors">−</button>
                              <span className="w-5 text-center font-black text-slate-800 tabular-nums">{it.qty}</span>
                              <button onClick={() => updateQty(i, it.qty + 1)} className="w-6 h-6 rounded-lg bg-slate-100 text-slate-500 hover:bg-slate-200 cursor-pointer text-[15px] font-bold flex items-center justify-center leading-none transition-colors">+</button>
                            </div>
                          </td>
                          <td className="px-3 py-3 text-right font-semibold text-slate-500 font-mono text-[12.5px]">{fmt(it.price)}</td>
                          {/* Thành tiền = số thực thu của dòng; đặt cọc thì hiện tiền cọc, kèm tổng dòng cho dễ đối chiếu */}
                          <td className="px-3 py-3 text-right font-mono">
                            {it.payType === "deposit" ? (
                              <>
                                <div className="font-black text-amber-700">{fmt(lineCollected(it))}</div>
                                <div className="text-[11px] font-semibold text-slate-400 mt-0.5">/ {fmt(it.qty * it.price)}</div>
                              </>
                            ) : (
                              <div className="font-black text-slate-800">{fmt(it.qty * it.price)}</div>
                            )}
                          </td>
                          <td className="px-3 py-2.5">
                            {/* Chọn "Đặt cọc" → gõ % ngay tại chính ô đó, không mọc thêm ô bên dưới */}
                            <div className="flex gap-1 w-full">
                              <button onClick={() => setItemPay(i, "full")}
                                className={`flex-1 px-2 py-1.5 rounded-lg text-[11.5px] font-bold border transition-all cursor-pointer ${it.payType !== "deposit" ? "bg-primary/10 border-primary text-primary" : "bg-white border-slate-200 text-slate-500 hover:border-slate-300"}`}>
                                Toàn bộ
                              </button>
                              {it.payType === "deposit" ? (
                                <div className="relative flex-1">
                                  <input type="text" inputMode="numeric" autoFocus title="% đặt cọc"
                                    value={it.depositPct ?? ""}
                                    onChange={e => setItemDepositPct(i, clampPct(Number(e.target.value.replace(/[^\d]/g, ""))))}
                                    className="w-full pl-2.5 pr-6 py-1.5 text-[11.5px] text-right font-mono font-bold bg-amber-50 border border-amber-400 text-amber-700 rounded-lg focus:outline-none focus:ring-1 focus:ring-amber-400" />
                                  <span className="absolute right-2 top-1/2 -translate-y-1/2 text-[11px] font-bold text-amber-500">%</span>
                                </div>
                              ) : (
                                <button onClick={() => setItemPay(i, "deposit")}
                                  className="flex-1 px-2 py-1.5 rounded-lg text-[11.5px] font-bold border transition-all cursor-pointer bg-white border-slate-200 text-slate-500 hover:border-slate-300">
                                  Đặt cọc
                                </button>
                              )}
                            </div>
                          </td>
                          <td className="px-2 py-3 text-center align-top">
                            <button onClick={() => removeItem(i)} className="w-6 h-6 rounded-lg hover:bg-red-50 text-slate-300 hover:text-primary cursor-pointer flex items-center justify-center transition-colors">
                              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Khuyến mãi — bấm chọn 1 chương trình khuyến mãi đang áp dụng để tự trừ vào hóa đơn.
                  Không áp dụng cho "thu phần còn lại" (isRemaining) — hóa đơn đó chỉ thu đúng số nợ gốc. */}
              {!isRemaining && (
              <div className="flex flex-col gap-2">
                <span className={labelCls}>Khuyến mãi</span>
                {activePromotions.length === 0 ? (
                  <p className="text-[12.5px] text-slate-400 font-semibold">Hiện không có khuyến mãi nào đang áp dụng.</p>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    <button type="button" onClick={() => setSelectedPromotionId(null)}
                      className={`px-3.5 py-2 rounded-xl border text-[12.5px] font-bold transition-all cursor-pointer ${
                        !selectedPromotionId ? "bg-primary/10 border-primary text-primary" : "bg-white border-slate-200 text-slate-500 hover:border-slate-300"
                      }`}>
                      Không áp dụng
                    </button>
                    {activePromotions.map(p => (
                      <button type="button" key={p.id} onClick={() => setSelectedPromotionId(p.id)}
                        className={`px-3.5 py-2 rounded-xl border text-left transition-all cursor-pointer ${
                          selectedPromotionId === p.id ? "bg-emerald-50 border-emerald-400 text-emerald-700" : "bg-white border-slate-200 text-slate-600 hover:border-slate-300"
                        }`}>
                        <div className="text-[12.5px] font-bold">{p.name}</div>
                        <div className="text-[11px] font-semibold opacity-70 mt-0.5">
                          {p.discountType === "Percentage" ? `Giảm ${p.discountValue}%` : `Giảm ${fmt(p.discountValue)}`} · {p.code}
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
              )}

              {/* Tổng kết: tổng hóa đơn → thu hôm nay (số lễ tân cần nhìn nhất) → còn nợ */}
              <div className="flex justify-end">
                <div className="flex flex-col items-end gap-2.5 w-full max-w-sm">
                  {discountAmount > 0 && (
                    <>
                      <div className="flex items-center justify-between w-full">
                        <span className="text-[12.5px] font-semibold text-slate-400">Tạm tính</span>
                        <span className="text-[13px] font-bold text-slate-400 font-mono line-through">{fmt(subtotal)}</span>
                      </div>
                      <div className="flex items-center justify-between w-full">
                        <span className="text-[12.5px] font-bold text-emerald-600">Giảm giá ({selectedPromotion?.code})</span>
                        <span className="text-[13px] font-black text-emerald-600 font-mono">−{fmt(discountAmount)}</span>
                      </div>
                    </>
                  )}
                  <div className="flex items-center justify-between w-full">
                    <span className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wider">Tổng cộng hóa đơn</span>
                    <span className="text-[17px] font-black text-slate-800 font-mono leading-none">{fmt(finalTotal)}</span>
                  </div>

                  <div className="flex items-center justify-between w-full bg-emerald-50 border border-emerald-200 rounded-xl px-4 py-3">
                    <span className="text-[13px] font-extrabold text-emerald-700 uppercase tracking-wider">Thu hôm nay</span>
                    <span className="text-[24px] font-black text-emerald-700 font-mono leading-none">{fmt(collectedTotal)}</span>
                  </div>

                  <div className="flex items-center justify-between w-full">
                    <span className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wider">Còn nợ</span>
                    <span className={`text-[15px] font-black font-mono leading-none ${collectedTotal < finalTotal ? "text-amber-600" : "text-slate-400"}`}>
                      {fmt(Math.max(0, finalTotal - collectedTotal))}
                    </span>
                  </div>
                </div>
              </div>
              </>)}

              {/* Payment method — chỉ 2 lựa chọn thật: tiền mặt (thu tay) hoặc trực tuyến (QR/App PayOS thật).
                  "Chuyển khoản" và "Thanh toán App" trước đây dẫn tới cùng 1 luồng backend hệt nhau nên gộp lại
                  làm 1 để khỏi gây hiểu lầm là 2 kênh khác nhau — nội bộ vẫn lưu paymentMethod="transfer". */}
              <div className="flex flex-col gap-3">
                <span className={labelCls}>Phương thức thanh toán</span>
                <div className="grid grid-cols-2 gap-3">
                  <button onClick={() => setMethod("cash")}
                    className={`flex flex-col items-center gap-2.5 px-4 py-5 rounded-2xl border-2 transition-all cursor-pointer ${
                      method === "cash" ? `${PAY_CFG.cash.bg} ${PAY_CFG.cash.border} shadow-sm` : "bg-white border-slate-200 hover:border-slate-300"
                    }`}>
                    <svg className={`w-7 h-7 transition-colors ${method === "cash" ? PAY_CFG.cash.color : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d={PAY_CFG.cash.icon} />
                    </svg>
                    <span className={`text-[13px] font-black transition-colors ${method === "cash" ? PAY_CFG.cash.color : "text-slate-500"}`}>Tiền mặt</span>
                  </button>
                  <button onClick={() => setMethod("transfer")}
                    className={`flex flex-col items-center gap-2.5 px-4 py-5 rounded-2xl border-2 transition-all cursor-pointer ${
                      method === "transfer" ? "bg-indigo-50 border-indigo-200 shadow-sm" : "bg-white border-slate-200 hover:border-slate-300"
                    }`}>
                    <svg className={`w-7 h-7 transition-colors ${method === "transfer" ? "text-indigo-600" : "text-slate-400"}`} viewBox="0 0 24 24" fill="none">
                      <rect x="3" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                      <rect x="5.5" y="5.5" width="2" height="2" fill="currentColor" />
                      <rect x="14" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                      <rect x="16.5" y="5.5" width="2" height="2" fill="currentColor" />
                      <rect x="3" y="14" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                      <rect x="5.5" y="16.5" width="2" height="2" fill="currentColor" />
                      <rect x="14" y="14" width="3" height="3" fill="currentColor" />
                      <rect x="18" y="14" width="3" height="3" rx="0.5" stroke="currentColor" strokeWidth="1.5" />
                      <rect x="14" y="18" width="3" height="3" rx="0.5" stroke="currentColor" strokeWidth="1.5" />
                      <rect x="18" y="18" width="3" height="3" fill="currentColor" />
                    </svg>
                    <span className={`text-[13px] font-black transition-colors ${method === "transfer" ? "text-indigo-600" : "text-slate-500"}`}>Thanh toán online</span>
                  </button>
                </div>
                {method === "transfer" && (
                  <div className="flex items-center gap-2 px-4 py-2.5 bg-indigo-50 border border-indigo-100 rounded-xl text-[12.5px] font-semibold text-indigo-700">
                    <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                    Hệ thống sẽ tự tạo mã QR/link thanh toán thật. Bệnh nhân có thể quét mã tại quầy hoặc tự thanh toán từ app của họ — hóa đơn tự chuyển &quot;Đã thanh toán&quot; khi nhận được tiền.
                  </div>
                )}
              </div>

              {/* Note */}
              <div className="flex flex-col gap-1.5">
                <label className={labelCls}>Ghi chú hóa đơn</label>
                <textarea rows={2} value={note} onChange={e => setNote(e.target.value)}
                  placeholder="Ghi chú nội bộ hoặc thông tin bổ sung..."
                  className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none" />
              </div>

              {/* Submit */}
              <div className="flex items-center gap-4 pt-1 border-t border-slate-100">
                <button onClick={handleIssue} disabled={!canIssue || issuing}
                  className="flex items-center gap-2 px-7 py-3 bg-primary hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-xl text-[14px] font-black cursor-pointer transition-all shadow-sm shadow-primary/20">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m.75 12l3 3m0 0l3-3m-3 3v-6m-1.5-9H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" /></svg>
                  {issuing ? "Đang xuất..." : "Xuất hóa đơn"}
                </button>
                {!method && <p className="text-[12.5px] text-slate-400 font-semibold">Chọn phương thức thanh toán để tiếp tục</p>}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

/* ─── Payment status panel (chuyển khoản QR / chờ thanh toán App) ───────── */

// Cache các request tạo-yêu-cầu-thanh-toán đang chạy dở, theo invoiceId — đặt ở module scope (ngoài component).
// React StrictMode (dev) mount → unmount → mount lại component gần như ngay lập tức trước khi request đầu tiên kịp
// trả lời; nếu không cache, lần mount thứ 2 sẽ gọi API lần nữa trong khi lần 1 vẫn đang chạy → tạo trùng giao dịch.
// Dùng chung 1 Promise (thay vì chặn hẳn) để lần mount sau vẫn nhận được đúng kết quả của lần gọi đầu.
const paymentRequestCache = new Map<string, ReturnType<typeof createPaymentRequestApi>>();

interface PaymentRequestState {
  txn: PaymentTransactionDto | null;
  creating: boolean;
  err: string | null;
  // Giao dịch hiện tại đã bị hủy/hết hạn bên cổng thanh toán (phát hiện qua poll đối soát) — PayOS thường
  // KHÔNG gửi webhook cho trường hợp này nên phải tự dò; cần tạo yêu cầu thanh toán MỚI để thử lại.
  txnCancelled: boolean;
  retry: () => void;
}

// Quản lý vòng đời "yêu cầu thanh toán online" của 1 hóa đơn (tạo link/QR, poll trạng thái, phát hiện hủy) —
// tách thành hook dùng chung để nút xem QR (đặt ở đầu card) và thanh trạng thái/nút xác nhận (đặt ở cuối card)
// cùng đọc chung 1 nguồn state, dù nằm ở 2 vị trí khác nhau trong DOM.
// `active` do component cha quyết định — hóa đơn được xuất sẵn với PaymentMethod=Transfer/App, HOẶC staff bấm
// "Tạo yêu cầu thanh toán online" cho một hóa đơn bất kỳ — backend không phân biệt 2 trường hợp này, dùng chung
// 1 API tạo yêu cầu duy nhất (không phụ thuộc PaymentMethod hiện tại của hóa đơn).
function usePaymentRequest(invoice: Invoice, active: boolean, onAutoConfirmed: () => void): PaymentRequestState {
  const [txn, setTxn] = useState<PaymentTransactionDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [txnCancelled, setTxnCancelled] = useState(false);

  const requestPayment = useCallback(() => {
    setCreating(true);
    setErr(null);

    let promise = paymentRequestCache.get(invoice.id);
    if (!promise) {
      promise = createPaymentRequestApi(invoice.id);
      paymentRequestCache.set(invoice.id, promise);
      promise.finally(() => paymentRequestCache.delete(invoice.id));
    }

    let cancelled = false;
    promise
      .then(t => { if (!cancelled) setTxn(t); })
      .catch(e => { if (!cancelled) setErr(e instanceof Error ? e.message : "Không thể tạo yêu cầu thanh toán"); })
      .finally(() => { if (!cancelled) setCreating(false); });
    return () => { cancelled = true; };
  }, [invoice.id]);

  // Tạo yêu cầu thanh toán (link/QR) qua PayOS khi hóa đơn này chuyển sang trạng thái chờ thanh toán online.
  useEffect(() => {
    if (!active) return;
    return requestPayment();
  }, [active, requestPayment]);

  // Poll trạng thái — tự động chuyển sang "Đã thanh toán" khi cổng thanh toán xác nhận qua webhook (hoặc qua đối
  // soát dự phòng phía backend nếu webhook chưa tới kịp); đồng thời phát hiện khi giao dịch bị hủy/hết hạn.
  useEffect(() => {
    if (!active) return;
    const interval = setInterval(async () => {
      try {
        const status = await getPaymentStatusApi(invoice.id);
        if (status.invoiceStatus === "Paid") { onAutoConfirmed(); return; }
        if (status.latestTransaction?.status === "Failed") setTxnCancelled(true);
      } catch {
        // Bỏ qua lỗi polling — không làm gián đoạn UI, sẽ thử lại ở lượt kế tiếp.
      }
    }, 4000);
    return () => clearInterval(interval);
  }, [invoice.id, active, onAutoConfirmed]);

  const retry = () => {
    setTxnCancelled(false);
    setTxn(null);
    requestPayment();
  };

  return { txn, creating, err, txnCancelled, retry };
}

function PaymentStatusPanel({ payment, onManualConfirm }: {
  payment: PaymentRequestState;
  onManualConfirm: () => void;
}) {
  const { creating, txnCancelled, retry } = payment;

  return (
    <div className={`flex items-center justify-between gap-4 px-5 py-4 rounded-xl border ${
      txnCancelled ? "bg-rose-50 border-rose-200" : "bg-indigo-50 border-indigo-200"
    }`}>
      <div className="flex items-center gap-3 min-w-0">
        {txnCancelled ? (
          <svg className="w-5 h-5 shrink-0 text-rose-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
          </svg>
        ) : (
          <div className="relative flex h-3 w-3 shrink-0">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-3 w-3 bg-indigo-500" />
          </div>
        )}
        <div className="min-w-0">
          <p className={`text-[13.5px] font-black ${txnCancelled ? "text-rose-800" : "text-indigo-800"}`}>
            {txnCancelled ? "Giao dịch đã bị hủy hoặc hết hạn" : "Đang chờ thanh toán trực tuyến"}
          </p>
          <p className={`text-[12px] font-semibold mt-0.5 ${txnCancelled ? "text-rose-600" : "text-indigo-600"}`}>
            {txnCancelled
              ? "Bấm tạo lại để lấy mã QR / link thanh toán mới"
              : "Bệnh nhân có thể quét mã QR tại quầy hoặc tự thanh toán từ app — trạng thái tự động cập nhật khi nhận được tiền"}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-2 shrink-0">
        {txnCancelled ? (
          <button onClick={retry} disabled={creating}
            className="flex items-center gap-2 px-4 py-2 bg-white border border-rose-300 hover:border-rose-400 text-rose-700 rounded-xl text-[12.5px] font-black cursor-pointer transition-all whitespace-nowrap disabled:opacity-40 disabled:cursor-not-allowed">
            {creating ? "Đang tạo..." : "Tạo lại yêu cầu thanh toán"}
          </button>
        ) : (
          <button onClick={onManualConfirm}
            className="flex items-center gap-2 px-4 py-2 bg-white border border-indigo-300 hover:border-indigo-400 text-indigo-700 rounded-xl text-[12.5px] font-black cursor-pointer transition-all whitespace-nowrap">
            Xác nhận thủ công
          </button>
        )}
      </div>
    </div>
  );
}

/* ─── Popup xác nhận trước khi đánh dấu thủ công đã thanh toán ───────────── */

function ConfirmManualPaymentModal({ invoice, onConfirm, onClose }: {
  invoice: Invoice;
  onConfirm: () => void;
  onClose: () => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4" onClick={onClose}>
      <div
        className="bg-white rounded-2xl shadow-xl border border-slate-200/70 w-full max-w-sm overflow-hidden"
        onClick={e => e.stopPropagation()}
      >
        <div className="px-6 py-5 border-b border-slate-100">
          <div className="text-[15px] font-black text-slate-900">Xác nhận thanh toán thủ công?</div>
          <div className="mt-1.5 text-[12.5px] text-slate-500 font-semibold leading-relaxed">
            Hóa đơn của <span className="font-black text-slate-700">{invoice.patientName}</span> ({fmt(invoice.depositAmount)}) sẽ được đánh dấu <span className="font-black text-emerald-700">đã thanh toán</span> ngay, không thông qua xác nhận từ cổng thanh toán. Chỉ bấm khi bạn đã chắc chắn nhận được tiền.
          </div>
        </div>
        <div className="flex gap-2 p-4">
          <button onClick={onClose}
            className="flex-1 px-4 py-2.5 rounded-xl border border-slate-200 text-slate-600 font-bold text-[13px] hover:bg-slate-50 cursor-pointer transition-all">
            Hủy
          </button>
          <button onClick={onConfirm}
            className="flex-1 px-4 py-2.5 rounded-xl bg-primary hover:bg-red-600 text-white font-bold text-[13px] cursor-pointer transition-all">
            Xác nhận đã nhận tiền
          </button>
        </div>
      </div>
    </div>
  );
}

/* ─── Popup mã QR chuyển khoản của 1 hóa đơn ─────────────────────────────── */

function PaymentQrModal({ invoice, txn, err, onClose }: {
  invoice: Invoice;
  txn: PaymentTransactionDto | null;
  err: string | null;
  onClose: () => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4" onClick={onClose}>
      <div
        className="bg-white rounded-2xl shadow-xl border border-slate-200/70 w-full max-w-sm overflow-hidden"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-4 px-6 py-5 border-b border-slate-100">
          <div className="min-w-0">
            <div className="text-[15px] font-black text-slate-900">Mã QR chuyển khoản</div>
            <div className="mt-1 text-[12.5px] text-slate-500 font-semibold truncate">
              {invoice.patientName}{invoice.planId ? ` · Từ ${invoice.planId}` : ""}
            </div>
          </div>
          <button onClick={onClose}
            className="shrink-0 w-8 h-8 flex items-center justify-center rounded-lg text-slate-400 hover:bg-slate-100 hover:text-slate-600 cursor-pointer transition-all">
            <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
          </button>
        </div>

        <div className="flex flex-col items-center gap-3 px-6 py-6">
          {txn?.qrCode ? (
            <>
              <QRCodeSVG value={txn.qrCode} size={220} marginSize={2} />
              <p className="text-[15px] font-black text-slate-800">{fmt(invoice.depositAmount)}</p>
              <p className="text-[11.5px] font-semibold text-slate-400 text-center">
                Quét mã bằng app ngân hàng bất kỳ để chuyển khoản
              </p>
              {txn.checkoutUrl && (
                <a href={txn.checkoutUrl} target="_blank" rel="noreferrer"
                  className="text-[11.5px] font-bold text-indigo-600 hover:text-indigo-700">
                  Không quét được? Mở trang thanh toán →
                </a>
              )}
            </>
          ) : (
            <p className="px-4 py-6 text-[12.5px] font-semibold text-slate-400 text-center">
              {err ?? "Không thể tạo mã QR chuyển khoản."}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

/* ─── Tab 2: Pending payment ─────────────────────────────── */

function PendingTab({ invoices, onPaid, onAutoConfirmed }: {
  invoices: Invoice[];
  onPaid: (id: string, method: PayMethod | null) => void;
  onAutoConfirmed: (invoice: Invoice) => void;
}) {
  const [methodEdit, setMethodEdit] = useState<Record<string, PayMethod>>({});

  const getMethod = (inv: Invoice): PayMethod | null =>
    methodEdit[inv.id] ?? inv.paymentMethod;

  if (invoices.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
        <div className="w-14 h-14 rounded-full bg-emerald-50 border border-emerald-100 flex items-center justify-center">
          <svg className="w-7 h-7 text-emerald-500" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
        </div>
        <p className="text-[14px] font-bold text-slate-500">Không có hóa đơn nào chờ thanh toán</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {invoices.map(inv => (
        <PendingInvoiceRow
          key={inv.id}
          inv={inv}
          method={getMethod(inv)}
          onMethodChange={pm => setMethodEdit(prev => ({ ...prev, [inv.id]: pm }))}
          onPaid={onPaid}
          onAutoConfirmed={onAutoConfirmed}
        />
      ))}
    </div>
  );
}

function PendingInvoiceRow({ inv, method, onMethodChange, onPaid, onAutoConfirmed }: {
  inv: Invoice;
  method: PayMethod | null;
  onMethodChange: (pm: PayMethod) => void;
  onPaid: (id: string, method: PayMethod | null) => void;
  onAutoConfirmed: (invoice: Invoice) => void;
}) {
  // Hóa đơn được xuất sẵn với PaymentMethod=Transfer/App (awaiting_payment), HOẶC staff bấm "Tạo yêu cầu thanh
  // toán online" ngay tại đây cho một hóa đơn bất kỳ — cả 2 đều dùng chung 1 luồng tạo QR/link + poll thật.
  const [onlineRequested, setOnlineRequested] = useState(false);
  const isPaymentActive = inv.status === "awaiting_payment" || onlineRequested;
  const handleAutoConfirmed = useCallback(() => onAutoConfirmed(inv), [onAutoConfirmed, inv]);
  const payment = usePaymentRequest(inv, isPaymentActive, handleAutoConfirmed);
  const [showQr, setShowQr] = useState(false);
  // method=null → "Xác nhận thủ công" chung; method cụ thể → xác nhận từ selector cash/transfer.
  const [confirmDialog, setConfirmDialog] = useState<{ method: PayMethod | null } | null>(null);

  return (
    <div className={`bg-white rounded-2xl border shadow-sm overflow-hidden ${
      isPaymentActive ? "border-indigo-200 shadow-indigo-50" : "border-slate-200/70"
    }`}>
      <div className="flex items-center gap-5 px-7 py-5">
        {/* Avatar */}
        <div className={`w-12 h-12 rounded-2xl flex items-center justify-center font-black text-[13px] border shrink-0 ${
          inv.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"
        }`}>
          {inv.patientName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2.5 flex-wrap">
            <span className="text-[15px] font-black text-slate-900">{inv.patientName}</span>
            <span className="text-[12px] font-mono text-slate-400">{inv.patientPhone}</span>
            <span className={`text-[11.5px] font-black px-2 py-0.5 rounded-lg border ${DENTIST_COLOR[inv.dentist] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>{inv.dentist}</span>
            {inv.planId && (
              <span className="text-[11.5px] font-bold text-slate-400 px-2 py-0.5 bg-slate-50 border border-slate-100 rounded-lg">Từ {inv.planId}</span>
            )}
          </div>
          <div className="mt-1.5 flex flex-wrap gap-1.5">
            {inv.items.map((it, i) => (
              <span key={i} className="text-[12px] font-semibold text-slate-500 px-2 py-0.5 bg-slate-50 border border-slate-100 rounded-lg">
                {it.qty > 1 ? `${it.qty}× ` : ""}{it.name}
              </span>
            ))}
          </div>
          {inv.note && <p className="mt-1.5 text-[12px] font-semibold text-amber-700">{inv.note}</p>}
        </div>

        {/* Total */}
        <div className="text-right shrink-0 ml-4">
          {inv.paymentType === "deposit" ? (
            <>
              <div className="flex items-center justify-end gap-1.5">
                <span className="text-[10px] font-black px-1.5 py-0.5 rounded-md bg-amber-100 text-amber-700 uppercase tracking-wide">Đặt cọc</span>
                <span className="text-[11px] font-semibold text-slate-400">/ {fmt(inv.finalTotal)}</span>
              </div>
              <div className="text-[24px] font-black text-slate-900 font-mono leading-none mt-0.5">{fmt(inv.depositAmount)}</div>
              <div className="text-[12px] font-semibold text-orange-600 mt-0.5">Còn lại {fmt(inv.remaining)}</div>
            </>
          ) : (
            <>
              {inv.discount > 0 && (
                <div className="text-[12px] font-semibold text-slate-400 line-through font-mono">{fmt(inv.subtotal)}</div>
              )}
              <div className="text-[24px] font-black text-slate-900 font-mono leading-none">{fmt(inv.finalTotal)}</div>
              {inv.discount > 0 && (
                <div className="text-[12px] font-semibold text-emerald-600 mt-0.5">
                  Đã giảm {fmt(inv.discount)}{inv.promotionCode ? ` (${inv.promotionCode})` : ""}
                </div>
              )}
            </>
          )}
        </div>

        {/* Nút xem mã QR — đặt ở đầu card (header), tách xa nút "Xác nhận thủ công" ở cuối card bên dưới
            để tránh bấm nhầm giữa 2 thao tác có hậu quả khác nhau hẳn. */}
        {isPaymentActive && !payment.txnCancelled && (
          <button onClick={() => setShowQr(true)} disabled={payment.creating || !payment.txn?.qrCode}
            title="Xem mã QR chuyển khoản"
            className="flex items-center justify-center w-10 h-10 bg-indigo-50 border border-indigo-200 hover:border-indigo-400 text-indigo-600 rounded-xl cursor-pointer transition-all disabled:opacity-40 disabled:cursor-not-allowed shrink-0">
            {payment.creating ? (
              <div className="w-4 h-4 border-2 border-indigo-200 border-t-indigo-500 rounded-full animate-spin" />
            ) : (
              <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none">
                <rect x="3" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                <rect x="5.5" y="5.5" width="2" height="2" fill="currentColor" />
                <rect x="14" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                <rect x="16.5" y="5.5" width="2" height="2" fill="currentColor" />
                <rect x="3" y="14" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                <rect x="5.5" y="16.5" width="2" height="2" fill="currentColor" />
                <rect x="14" y="14" width="3" height="3" fill="currentColor" />
                <rect x="18" y="14" width="3" height="3" rx="0.5" stroke="currentColor" strokeWidth="1.5" />
                <rect x="14" y="18" width="3" height="3" rx="0.5" stroke="currentColor" strokeWidth="1.5" />
                <rect x="18" y="18" width="3" height="3" fill="currentColor" />
              </svg>
            )}
          </button>
        )}
      </div>

      {/* Payment section */}
      <div className="px-7 pb-5">
        {isPaymentActive ? (
          <PaymentStatusPanel payment={payment} onManualConfirm={() => setConfirmDialog({ method: null })} />
        ) : (
          <div className="flex flex-col gap-3">
            <div className="flex items-center gap-3 flex-wrap">
              <span className={`${labelCls} shrink-0`}>Đã nhận tiền qua:</span>
              <div className="flex gap-2 flex-wrap flex-1">
                {(["cash", "transfer"] as PayMethod[]).map(pm => {
                  const pcfg   = PAY_CFG[pm];
                  const active = method === pm;
                  return (
                    <button key={pm} onClick={() => onMethodChange(pm)}
                      className={`flex items-center gap-2 px-4 py-2 rounded-xl border text-[13px] font-bold cursor-pointer transition-all ${
                        active ? `${pcfg.bg} ${pcfg.border} ${pcfg.color} shadow-sm` : "bg-white border-slate-200 text-slate-500 hover:border-slate-300"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d={pcfg.icon} />
                      </svg>
                      {pcfg.label}
                    </button>
                  );
                })}
              </div>
              {method && (
                <button onClick={() => setConfirmDialog({ method })}
                  className={`flex items-center gap-2 px-5 py-2.5 text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm whitespace-nowrap ${
                    method === "cash" ? "bg-emerald-500 hover:bg-emerald-600 shadow-emerald-200" : "bg-sky-500 hover:bg-sky-600 shadow-sky-200"
                  }`}>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  {method === "cash" ? "Đã nhận tiền mặt" : "Đã nhận chuyển khoản"}
                </button>
              )}
            </div>

            <div className="flex items-center gap-3">
              <div className="h-px flex-1 bg-slate-100" />
              <span className="text-[10.5px] font-black text-slate-300 uppercase tracking-wider">hoặc</span>
              <div className="h-px flex-1 bg-slate-100" />
            </div>

            {/* Bệnh nhân đã được thông báo & có thể tự thanh toán từ app của họ ngay từ lúc hóa đơn được xuất —
                nút này chỉ để staff chủ động tạo QR/link thật (vd. đưa bệnh nhân quét tại quầy), không bắt buộc. */}
            <button onClick={() => setOnlineRequested(true)}
              className="flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl border border-indigo-200 bg-indigo-50 text-indigo-700 text-[13px] font-black cursor-pointer hover:border-indigo-400 transition-all">
              <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none">
                <rect x="3" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                <rect x="5.5" y="5.5" width="2" height="2" fill="currentColor" />
                <rect x="14" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                <rect x="16.5" y="5.5" width="2" height="2" fill="currentColor" />
                <rect x="3" y="14" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5" />
                <rect x="5.5" y="16.5" width="2" height="2" fill="currentColor" />
                <rect x="14" y="14" width="3" height="3" fill="currentColor" />
                <rect x="18" y="14" width="3" height="3" rx="0.5" stroke="currentColor" strokeWidth="1.5" />
                <rect x="14" y="18" width="3" height="3" rx="0.5" stroke="currentColor" strokeWidth="1.5" />
                <rect x="18" y="18" width="3" height="3" fill="currentColor" />
              </svg>
              Tạo yêu cầu thanh toán online (QR / App)
            </button>
          </div>
        )}
      </div>

      {showQr && (
        <PaymentQrModal
          invoice={inv}
          txn={payment.txn}
          err={payment.err}
          onClose={() => setShowQr(false)}
        />
      )}

      {confirmDialog && (
        <ConfirmManualPaymentModal
          invoice={inv}
          onConfirm={() => { const m = confirmDialog.method; setConfirmDialog(null); onPaid(inv.id, m); }}
          onClose={() => setConfirmDialog(null)}
        />
      )}
    </div>
  );
}

/* ─── Tab 3: History ─────────────────────────────────────── */

function HistoryTab({ paid }: { paid: Invoice[] }) {
  // Doanh thu = số tiền thực thu của các hóa đơn đang hiển thị (theo bộ lọc ngày);
  // hóa đơn đặt cọc tính theo số đã cọc.
  const todayRevenue = paid.reduce((s, i) => s + i.depositAmount, 0);

  return (
    <div className="flex flex-col gap-5">
      {/* Summary cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 sm:gap-4">
        {[
          { label: "Doanh thu",   value: fmt(todayRevenue), icon: "M2.25 18.75a60.07 60.07 0 0115.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 013 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 00-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 01-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 003 15h-.75M15 10.5a3 3 0 11-6 0 3 3 0 016 0zm3 0h.008v.008H18V10.5zm-12 0h.008v.008H6V10.5z", color: "text-emerald-600", bg: "bg-emerald-50" },
          { label: "Tổng hóa đơn",        value: `${paid.length} hóa đơn`,                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                icon: "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z", color: "text-sky-600",     bg: "bg-sky-50"     },
          { label: "Trung bình / hóa đơn", value: paid.length > 0 ? fmt(Math.round(paid.reduce((s, i) => s + i.depositAmount, 0) / paid.length)) : "—",                                                                                                                                                                                                                                                                                                                                                                                                                                                      icon: "M7.5 14.25v2.25m3-4.5v4.5m3-6.75v6.75m3-9v9M6 20.25h12A2.25 2.25 0 0020.25 18V6A2.25 2.25 0 0018 3.75H6A2.25 2.25 0 003.75 6v12A2.25 2.25 0 006 20.25z", color: "text-violet-600", bg: "bg-violet-50"  },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-2xl border border-slate-200/70 shadow-sm p-4 flex items-center gap-3.5">
            <div className={`w-10 h-10 sm:w-11 sm:h-11 rounded-xl ${s.bg} flex items-center justify-center shrink-0`}>
              <svg className={`w-5 h-5 ${s.color}`} fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={s.icon} /></svg>
            </div>
            <div className="min-w-0">
              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider truncate">{s.label}</div>
              <div className="text-[15px] sm:text-[17px] font-black text-slate-900 mt-0.5 truncate">{s.value}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
        <table className="w-full text-[13px] min-w-[900px]">
          <thead>
            <tr className="bg-slate-50/70 border-b border-slate-200">
              {/* Cột tiền canh phải để tiêu đề thẳng hàng với con số bên dưới */}
              {([
                { label: "Mã HĐ",      cls: "text-left w-28" },
                { label: "Ngày thu",   cls: "text-left w-24" },
                { label: "Bệnh nhân",  cls: "text-left" },
                { label: "Bác sĩ",     cls: "text-left" },
                { label: "Nội dung",   cls: "text-left" },
                { label: "Thanh toán", cls: "text-left" },
                { label: "Tổng tiền",  cls: "text-right" },
              ] as const).map(h => (
                <th key={h.label} className={`px-4 py-3 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider whitespace-nowrap ${h.cls}`}>{h.label}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {[...paid].reverse().map(inv => {
              const m = inv.paymentMethod ? PAY_CFG[inv.paymentMethod] : null;
              return (
                <tr key={inv.id} className="hover:bg-slate-50/50 transition-colors">
                  {/* Mã hóa đơn dạng người đọc được (INV001) — in cả GUID thì ô tràn 3 dòng */}
                  <td className="px-4 py-3.5 font-black text-slate-500 font-mono text-[12px] whitespace-nowrap" title={inv.id}>{inv.planId}</td>
                  <td className="px-4 py-3.5 font-semibold text-slate-500 whitespace-nowrap">{inv.paidDate ?? inv.date}</td>
                  <td className="px-4 py-3.5 whitespace-nowrap">
                    <div className="font-black text-slate-900">{inv.patientName}</div>
                    <div className="font-mono text-slate-400 text-[11.5px]">{inv.patientPhone}</div>
                  </td>
                  <td className="px-4 py-3.5 whitespace-nowrap">
                    <span className={`inline-block text-[11.5px] font-black px-2 py-0.5 rounded-lg border whitespace-nowrap ${DENTIST_COLOR[inv.dentist] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>{inv.dentist}</span>
                  </td>
                  {/* Chỉ cột nội dung được xuống dòng — các cột còn lại giữ trên một hàng */}
                  <td className="px-4 py-3.5 min-w-[220px]">
                    <p className="text-slate-600 font-semibold text-[12.5px] leading-snug">
                      {inv.items.map((it, i) => (
                        <span key={i}>{it.qty > 1 ? `${it.qty}× ` : ""}{it.name}{i < inv.items.length - 1 ? "; " : ""}</span>
                      ))}
                    </p>
                  </td>
                  <td className="px-4 py-3.5 whitespace-nowrap">
                    {m && (
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl text-[12px] font-black border whitespace-nowrap ${m.bg} ${m.border} ${m.color}`}>
                        <svg className="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={m.icon} /></svg>
                        {m.label}
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3.5 text-right whitespace-nowrap">
                    <div className="font-black text-slate-900 font-mono text-[13.5px]">{fmt(inv.depositAmount)}</div>
                    {inv.paymentType === "deposit"
                      ? <div className="text-[11px] text-amber-600 font-semibold">Đặt cọc · còn {fmt(inv.remaining)}</div>
                      : inv.discount > 0 && <div className="text-[11px] text-emerald-600 font-semibold">−{fmt(inv.discount)}</div>}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        </div>
      </div>
    </div>
  );
}

/* ─── Tab 4: Outstanding (công nợ) ───────────────────────── */

function OutstandingTab({ invoices, plans, onCollect }: { invoices: Invoice[]; plans: OutstandingPlanDto[]; onCollect: (id: string) => void }) {
  // Hai tab con = hai CÁCH NHÌN cùng dòng tiền, mỗi bên có tổng riêng và KHÔNG cộng vào nhau:
  // - theo hóa đơn: hóa đơn đã xuất mà chưa thu đủ (thu bằng nút "Thu phần còn lại");
  // - theo liệu trình: liệu trình đang điều trị đã thu một phần mà còn thiếu, trong đó
  //   `unbilledAmount` là phần chưa nằm trên bất kỳ hóa đơn nào (sẽ xuất ở đợt thu sau).
  const invoiceRemaining = invoices.reduce((s, i) => s + i.remaining, 0);
  const invoiceCollected = invoices.reduce((s, i) => s + i.depositAmount, 0);

  const planRemaining = plans.reduce((s, p) => s + p.remainingAmount, 0);
  const planCollected = plans.reduce((s, p) => s + p.amountPaid, 0);
  const planUnbilled = plans.reduce((s, p) => s + p.unbilledAmount, 0);

  const totalCount = invoices.length + plans.length;

  const [sub, setSub] = useState<"invoices" | "plans">("invoices");

  const cards = sub === "invoices"
    ? [
        { label: "Còn nợ trên hóa đơn", value: fmt(invoiceRemaining), color: "text-orange-600",  bg: "bg-orange-50" },
        { label: "Đã thu (cọc)",        value: fmt(invoiceCollected), color: "text-emerald-600", bg: "bg-emerald-50" },
        { label: "Số hóa đơn",          value: `${invoices.length} hóa đơn`, color: "text-sky-600", bg: "bg-sky-50" },
      ]
    : [
        { label: "Còn nợ liệu trình", value: fmt(planRemaining),  color: "text-orange-600",  bg: "bg-orange-50" },
        { label: "Đã thu",            value: fmt(planCollected),  color: "text-emerald-600", bg: "bg-emerald-50" },
        { label: "Chưa xuất hóa đơn", value: fmt(planUnbilled),   color: "text-violet-600",  bg: "bg-violet-50" },
      ];

  if (totalCount === 0) {
    return (
      <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm flex flex-col items-center gap-3 py-20">
        <div className="w-14 h-14 rounded-full bg-emerald-50 border border-emerald-100 flex items-center justify-center">
          <svg className="w-7 h-7 text-emerald-500" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
        </div>
        <p className="text-[14px] font-bold text-slate-500">Không có công nợ nào</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      {/* Hai cách nhìn công nợ — tách tab để tổng của mỗi bên đứng riêng, không cộng chồng nhau:
          theo HÓA ĐƠN đã xuất, và theo LIỆU TRÌNH đang điều trị. */}
      <div className="flex gap-2 flex-wrap">
        {([
          { key: "invoices", label: "Hóa đơn đặt cọc còn nợ", count: invoices.length },
          { key: "plans",    label: "Liệu trình còn nợ",      count: plans.length },
        ] as const).map(t => (
          <button key={t.key} onClick={() => setSub(t.key)}
            className={`flex items-center gap-2 px-4 py-1.5 rounded-xl text-[12.5px] font-bold border transition-all cursor-pointer ${
              sub === t.key
                ? "bg-slate-900 text-white border-slate-900"
                : "bg-white text-slate-500 border-slate-200 hover:border-slate-300 hover:text-slate-700"
            }`}>
            {t.label}
            {t.count > 0 && (
              <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${
                sub === t.key ? "bg-white/25 text-white" : "bg-slate-100 text-slate-500"
              }`}>{t.count}</span>
            )}
          </button>
        ))}
      </div>

      {/* Summary cards — chỉ của tab đang xem */}
      <div className="grid grid-cols-3 gap-4">
        {cards.map(s => (
          <div key={s.label} className="bg-white rounded-2xl border border-slate-200/70 shadow-sm px-5 py-4 flex items-center gap-4">
            <div className={`w-11 h-11 rounded-xl ${s.bg} flex items-center justify-center shrink-0`}>
              <svg className={`w-5 h-5 ${s.color}`} fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M2.25 18.75a60.07 60.07 0 0115.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 013 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 00-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 01-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 003 15h-.75M15 10.5a3 3 0 11-6 0 3 3 0 016 0zm3 0h.008v.008H18V10.5zm-12 0h.008v.008H6V10.5z" /></svg>
            </div>
            <div>
              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{s.label}</div>
              <div className="text-[17px] font-black text-slate-900 mt-0.5">{s.value}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Tab con 1: hóa đơn đã xuất mà chưa thu đủ */}
      {sub === "invoices" && (invoices.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm py-16 text-center text-[13.5px] font-bold text-slate-400">
          Không có hóa đơn nào còn nợ.
        </div>
      ) : (<>
      <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
        <table className="w-full text-[13px]">
          <thead>
            <tr className="bg-slate-50/70 border-b border-slate-200">
              {["Mã HĐ","Ngày","Bệnh nhân","Bác sĩ","Tổng tiền","Đã thu","Còn lại","Trạng thái",""].map((h, hi) => (
                <th key={hi} className="px-5 py-3 text-left text-[11px] font-extrabold text-slate-400 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {invoices.map(inv => (
              <tr key={inv.id} className="hover:bg-slate-50/50 transition-colors">
                <td className="px-5 py-3.5 font-black text-slate-500 font-mono text-[12px]">{inv.planId}</td>
                <td className="px-5 py-3.5 font-semibold text-slate-500 whitespace-nowrap">{inv.date}</td>
                <td className="px-5 py-3.5">
                  <div className="font-black text-slate-900">{inv.patientName}</div>
                  <div className="font-mono text-slate-400 text-[11.5px]">{inv.patientPhone}</div>
                </td>
                <td className="px-5 py-3.5">
                  <span className={`text-[11.5px] font-black px-2 py-0.5 rounded-lg border ${DENTIST_COLOR[inv.dentist] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>{inv.dentist}</span>
                </td>
                <td className="px-5 py-3.5 font-mono font-semibold text-slate-600">{fmt(inv.finalTotal)}</td>
                <td className="px-5 py-3.5 font-mono font-semibold text-emerald-600">{fmt(inv.depositAmount)}</td>
                <td className="px-5 py-3.5 font-mono font-black text-orange-600">{fmt(inv.remaining)}</td>
                <td className="px-5 py-3.5">
                  {inv.status === "paid" ? (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl text-[11.5px] font-black border bg-amber-50 border-amber-200 text-amber-700">Đã thu cọc</span>
                  ) : (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl text-[11.5px] font-black border bg-slate-50 border-slate-200 text-slate-500">Chưa thu</span>
                  )}
                </td>
                <td className="px-5 py-3.5 text-right whitespace-nowrap">
                  {inv.collectingRemaining ? (
                    <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-[11.5px] font-black bg-orange-50 border border-orange-200 text-orange-600">
                      <span className="relative flex h-2 w-2">
                        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-orange-400 opacity-75" />
                        <span className="relative inline-flex rounded-full h-2 w-2 bg-orange-500" />
                      </span>
                      Đang chờ xuất HĐ
                    </span>
                  ) : (
                    <button onClick={() => onCollect(inv.id)}
                      className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-primary hover:bg-red-600 text-white rounded-xl text-[12.5px] font-black cursor-pointer transition-all shadow-sm shadow-primary/20 whitespace-nowrap">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                      Thu phần còn lại
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      </>))}

      {/* Tab con 2: công nợ nhìn theo liệu trình điều trị */}
      {sub === "plans" && (plans.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm py-16 text-center text-[13.5px] font-bold text-slate-400">
          Không có liệu trình nào còn nợ.
        </div>
      ) : (<>
      <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
        <table className="w-full text-[13px]">
          <thead>
            <tr className="bg-slate-50/70 border-b border-slate-200">
              {["Liệu trình","Bệnh nhân","Bác sĩ","Tổng chi phí","Đã thu","Còn lại","Chưa xuất HĐ","Trạng thái"].map((h, hi) => (
                <th key={hi} className="px-5 py-3 text-left text-[11px] font-extrabold text-slate-400 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {plans.map(p => (
              <tr key={p.treatmentPlanId} className="hover:bg-slate-50/50 transition-colors">
                <td className="px-5 py-3.5 font-black text-slate-900">{p.planName}</td>
                <td className="px-5 py-3.5">
                  <div className="font-black text-slate-900">{p.patientName}</div>
                  <div className="font-mono text-slate-400 text-[11.5px]">{p.patientPhone ?? "—"}</div>
                </td>
                <td className="px-5 py-3.5">
                  <span className={`text-[11.5px] font-black px-2 py-0.5 rounded-lg border ${DENTIST_COLOR[p.dentistName] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>{p.dentistName}</span>
                </td>
                <td className="px-5 py-3.5 font-mono font-semibold text-slate-600">{fmt(p.totalCost)}</td>
                <td className="px-5 py-3.5 font-mono font-semibold text-emerald-600">{fmt(p.amountPaid)}</td>
                <td className="px-5 py-3.5 font-mono font-black text-orange-600">{fmt(p.remainingAmount)}</td>
                {/* Phần chưa nằm trên hóa đơn nào — số sẽ xuất hóa đơn ở đợt thu sau. Phần còn lại của
                    khoản nợ (nếu có) đang nằm ở tab "Hóa đơn đặt cọc còn nợ". */}
                <td className="px-5 py-3.5 font-mono font-semibold text-violet-600">{fmt(p.unbilledAmount)}</td>
                <td className="px-5 py-3.5">
                  <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl text-[11.5px] font-black border bg-indigo-50 border-indigo-200 text-indigo-700">Đang điều trị</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="text-[12px] font-semibold text-slate-400 -mt-1">
        Đây là công nợ nhìn theo liệu trình — phần <strong>đã xuất hóa đơn mà chưa thu</strong> nằm ở tab
        {" "}<strong>Hóa đơn đặt cọc còn nợ</strong>, nên đừng cộng tổng của hai tab với nhau.
        Mỗi đợt thu của liệu trình được thực hiện ở tab <strong>Liệu trình → Hóa đơn</strong> sau khi bác sĩ kết thúc buổi điều trị/tái khám.
      </p>
      </>))}
    </div>
  );
}

/* ─── Main page ──────────────────────────────────────────── */

export default function InvoicesPage() {
  useRequireStaff();

  const [tab,     setTab]     = useState<"plans" | "pending" | "history" | "outstanding">("plans");
  const [plans,   setPlans]   = useState<TreatmentPlan[]>([]);
  const [pending, setPending] = useState<Invoice[]>([]);
  const [paid,    setPaid]    = useState<Invoice[]>([]);
  const [outstanding, setOutstanding] = useState<Invoice[]>([]);
  const [outstandingPlans, setOutstandingPlans] = useState<OutstandingPlanDto[]>([]);
  const [promotions, setPromotions] = useState<PromotionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);
  const [toast,   setToast]   = useState<{ message: string; type: "success" | "error" | "info" } | null>(null);

  const showToast = (message: string, type: "success" | "error" | "info" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  // Bộ lọc theo ngày — mặc định là hôm nay; "" = tất cả các ngày
  const [filterDate, setFilterDate] = useState<string>(todayIso());
  const viDate = filterDate ? isoToVi(filterDate) : null;
  const byDate = <T extends { date: string }>(list: T[]) =>
    viDate ? list.filter(i => i.date === viDate) : list;

  // Lịch sử phải lọc theo NGÀY THU TIỀN: hóa đơn hôm nay thường thuộc lịch hẹn của ngày khác,
  // lọc theo ngày hẹn sẽ làm tab này gần như luôn trống.
  const byPaidDate = (list: Invoice[]) =>
    viDate ? list.filter(i => (i.paidDate ?? i.date) === viDate) : list;

  const fPlans   = byDate(plans);
  const fPending = byDate(pending);
  const fPaid    = byPaidDate(paid);

  const reload = useCallback(async () => {
    // Khuyến mãi tải song song với các danh sách khác thay vì để PlansTab tự fetch riêng sau khi
    // trang đã hiện xong — tránh phần "Khuyến mãi" bị hiện trễ một nhịp so với phần còn lại.
    const [billable, pendingInv, history, outstandingInv, outstandingPls, promos] = await Promise.all([
      getBillablePlansApi(),
      getPendingInvoicesApi(),
      getInvoiceHistoryApi(),
      getOutstandingInvoicesApi(),
      getOutstandingPlansApi(),
      getPromotionsApi().catch(() => []),
    ]);
    setPlans(billable.map(mapPlan));
    setPending(pendingInv.map(mapInvoice));
    setPaid(history.map(mapInvoice));
    setOutstanding(outstandingInv.map(mapInvoice));
    setOutstandingPlans(outstandingPls);
    setPromotions(promos);
  }, []);

  useEffect(() => {
    reload()
      .catch(e => setError(e instanceof Error ? e.message : "Không thể tải dữ liệu hóa đơn"))
      .finally(() => setLoading(false));
  }, [reload]);

  // Cổng thanh toán tự xác nhận thành công (qua webhook/đối soát) trong lúc màn hình staff đang mở — báo cho
  // nhân viên biết bằng toast, vì hóa đơn sẽ lặng lẽ biến mất khỏi tab "Chờ thanh toán" ngay sau reload().
  const handleAutoConfirmed = (inv: Invoice) => {
    showToast(`Đã nhận thanh toán từ ${inv.patientName} — ${fmt(inv.depositAmount)}`, "success");
    reload();
  };

  // Xuất hóa đơn từ liệu trình (planId chính là appointmentId)
  const handleIssued = async (inv: Invoice): Promise<boolean> => {
    try {
      // Khi có áp dụng khuyến mãi, quy đổi số thu từng dòng theo cùng tỉ lệ giảm giá đã hiển thị ở
      // PlansTab — tránh tổng thu (chưa giảm giá) vượt quá tổng hóa đơn (đã giảm giá) và bị backend
      // từ chối. Dùng Math.floor để tổng luôn ≤ finalTotal, không bị lệch làm tròn.
      const collectRatio = inv.subtotal > 0 && inv.discount > 0 ? inv.finalTotal / inv.subtotal : 1;
      await issueInvoiceApi({
        appointmentId: inv.planId!,
        items: inv.items.map(i => ({ name: i.name, quantity: i.qty, unitPrice: i.price, treatmentPlanId: i.treatmentPlanId ?? undefined, amountCollected: Math.floor(lineCollected(i) * collectRatio) })),
        discount: inv.discount,
        paymentMethod: inv.paymentMethod ?? "cash",
        paymentType: inv.paymentType,
        depositAmount: inv.depositAmount,
        notes: inv.note || undefined,
        parentInvoiceId: inv.parentInvoiceId ?? undefined,
        treatmentPlanId: inv.treatmentPlanId ?? undefined,
        promotionId: inv.promotionId ?? undefined,
      });
      await reload();
      setTimeout(() => setTab("pending"), 2200);
      return true;
    } catch (e) {
      setError(e instanceof Error ? e.message : "Xuất hóa đơn thất bại");
      return false;
    }
  };

  const handlePaid = async (id: string, method: PayMethod | null) => {
    try {
      await confirmInvoicePaymentApi(id, method ?? undefined);
      await reload();
    } catch (e) {
      // Lỗi thường gặp nhất ở đây là bấm xác nhận liên tiếp (hóa đơn đã được request trước đó xử lý xong) —
      // dùng toast tự ẩn thay vì banner đỏ cố định, đỡ gây cảm giác như một lỗi nghiêm trọng cần xử lý.
      showToast(e instanceof Error ? e.message : "Xác nhận thanh toán thất bại", "info");
      await reload(); // đồng bộ lại UI với trạng thái thật (hóa đơn đã Paid từ lần bấm trước) thay vì đứng yên
    }
  };

  // Thu phần còn lại → đưa vào tab "Liệu trình → Hóa đơn" để xuất hóa đơn phần còn lại
  const handleCollectRemaining = async (id: string) => {
    try {
      await collectRemainingInvoiceApi(id);
      await reload();
      setTab("plans");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể tạo yêu cầu thu phần còn lại");
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="invoices" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader
          title="Hóa Đơn & Thanh Toán"
          subtitle="Xuất hóa đơn từ liệu trình điều trị và xác nhận thanh toán"
          right={
            <div className="flex items-center gap-1.5 sm:gap-2 text-[12px] sm:text-[12.5px] font-bold">
              {fPlans.length > 0 && (
                <span className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl whitespace-nowrap">
                  <span className="relative flex h-2 w-2">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
                    <span className="relative inline-flex rounded-full h-2 w-2 bg-amber-500" />
                  </span>
                  {fPlans.length} liệu trình chờ
                </span>
              )}
              {fPending.length > 0 && (
                <span className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-1.5 bg-indigo-50 text-indigo-700 border border-indigo-200 rounded-xl whitespace-nowrap">
                  {fPending.some(i => i.status === "awaiting_payment") && (
                    <span className="relative flex h-2 w-2">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75" />
                      <span className="relative inline-flex rounded-full h-2 w-2 bg-indigo-500" />
                    </span>
                  )}
                  {fPending.length} chờ thanh toán
                </span>
              )}

              {/* Bộ lọc ngày */}
              <div className="flex items-center gap-1 sm:gap-1.5 pl-0.5 sm:pl-1">
                <div className="relative flex items-center">
                  <svg className="w-4 h-4 text-slate-400 absolute left-2.5 pointer-events-none" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                  </svg>
                  <input
                    type="date"
                    value={filterDate}
                    onChange={e => setFilterDate(e.target.value)}
                    className="pl-8 pr-2.5 py-1.5 text-[12px] sm:text-[12.5px] font-bold bg-white border border-slate-200 rounded-xl text-slate-700 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none cursor-pointer max-w-[130px] sm:max-w-none"
                  />
                </div>
                {filterDate ? (
                  <button onClick={() => setFilterDate("")}
                    title="Xem tất cả các ngày"
                    className="px-2 sm:px-2.5 py-1.5 rounded-xl border border-slate-200 bg-white text-slate-500 hover:text-primary hover:border-primary/40 transition-all cursor-pointer text-[12px] font-bold">
                    Tất cả
                  </button>
                ) : (
                  <button onClick={() => setFilterDate(todayIso())}
                    title="Về hôm nay"
                    className="px-2 sm:px-2.5 py-1.5 rounded-xl border border-primary/30 bg-primary/5 text-primary hover:bg-primary/10 transition-all cursor-pointer text-[12px] font-bold">
                    Hôm nay
                  </button>
                )}
              </div>
            </div>
          }
        />

        <div className="p-4 sm:p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Tabs */}
          <div className="flex gap-2 overflow-x-auto pb-1 max-w-full flex-nowrap shrink-0">
            {([
              { key: "plans",       label: "Liệu trình → Hóa đơn", count: fPlans.length,      dot: fPlans.length > 0 },
              { key: "pending",     label: "Chờ thanh toán",         count: fPending.length,    dot: fPending.some(i => i.status === "awaiting_payment") },
              { key: "outstanding", label: "Công nợ",                count: outstanding.length + outstandingPlans.length, dot: (outstanding.length + outstandingPlans.length) > 0 },
              { key: "history",     label: "Lịch sử hóa đơn",        count: fPaid.length,       dot: false },
            ] as const).map(t => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`flex items-center gap-2 px-5 py-2 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer border ${
                  tab === t.key
                    ? "bg-primary text-white border-primary shadow-sm shadow-primary/20"
                    : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                }`}>
                {t.label}
                {t.count > 0 && (
                  <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${
                    tab === t.key ? "bg-white/25 text-white" : t.dot ? "bg-amber-100 text-amber-700" : "bg-slate-100 text-slate-500"
                  }`}>{t.count}</span>
                )}
              </button>
            ))}
          </div>

          {error && (
            <div className="flex items-center justify-between gap-4 px-5 py-3.5 bg-red-50 border border-red-200 rounded-xl">
              <span className="text-[13px] font-bold text-red-700">{error}</span>
              <button onClick={() => setError(null)} className="text-red-400 hover:text-red-600 cursor-pointer">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          )}

          {loading ? (
            <div className="flex items-center justify-center py-24">
              <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
            </div>
          ) : (
            <>
              {tab === "plans"       && <PlansTab       plans={fPlans}        promotions={promotions} onIssued={handleIssued} />}
              {tab === "pending"     && <PendingTab     invoices={fPending}    onPaid={handlePaid}    onAutoConfirmed={handleAutoConfirmed} />}
              {tab === "outstanding" && <OutstandingTab invoices={outstanding} plans={outstandingPlans} onCollect={handleCollectRemaining} />}
              {tab === "history"     && <HistoryTab     paid={fPaid}                                  />}
            </>
          )}
        </div>
      </main>

      {toast && (
        <div className={`fixed top-6 right-6 z-[9999] px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border font-bold text-[14.5px] max-w-md ${
          toast.type === "success" ? "bg-emerald-900 text-white border-emerald-800"
          : toast.type === "error" ? "bg-red-900 text-white border-red-800"
          : "bg-slate-900 text-white border-slate-800"
        }`}>
          <span className="text-lg">{toast.type === "success" ? "✓" : toast.type === "error" ? "⚠" : "ℹ"}</span>
          <span>{toast.message}</span>
        </div>
      )}
    </div>
  );
}
