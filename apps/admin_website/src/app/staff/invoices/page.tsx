"use client";

import { useState, useEffect, useCallback } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getBillablePlansApi,
  getPendingInvoicesApi,
  getInvoiceHistoryApi,
  getOutstandingInvoicesApi,
  getOutstandingCoursesApi,
  issueInvoiceApi,
  confirmInvoicePaymentApi,
  collectRemainingInvoiceApi,
  type BillablePlanDto,
  type InvoiceDto,
  type OutstandingCourseDto,
} from "../../../lib/apiClient";

/* ─── types ─────────────────────────────────────────────── */

interface Procedure { name: string; qty: number; price: number; }

interface TreatmentPlan {
  id: string; patientName: string; patientPhone: string; gender: "Nam" | "Nữ";
  dentist: string; date: string; diagnosis: string;
  procedures: Procedure[];
  // Khi mục này là "thu phần còn lại" của một hóa đơn đặt cọc
  outstandingInvoiceId?: string | null;
  sourceInvoiceNumber?: string | null;
  // Khi mục này là một đợt thu của liệu trình dài hạn
  courseId?: string | null;
  courseName?: string | null;
  courseTotal?: number;
  courseAmountPaid?: number;
  courseRemaining?: number;
}

type PayMethod = "cash" | "transfer" | "app";
type PayType = "full" | "deposit";
type InvStatus = "pending" | "app_waiting" | "paid";

interface Invoice {
  id: string; planId?: string;
  patientName: string; patientPhone: string; gender: "Nam" | "Nữ";
  dentist: string; date: string;
  items: Procedure[];
  subtotal: number; discount: number; finalTotal: number;
  paymentType: PayType; depositAmount: number; remaining: number;
  paymentMethod: PayMethod | null;
  status: InvStatus; note: string;
  // Công nợ
  parentInvoiceId?: string | null;
  isSettled?: boolean;
  collectingRemaining?: boolean;
  courseId?: string | null;
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
    procedures: b.items.map(i => ({ name: i.name, qty: i.quantity, price: i.unitPrice })),
    outstandingInvoiceId: b.outstandingInvoiceId,
    sourceInvoiceNumber: b.sourceInvoiceNumber,
    courseId: b.courseId,
    courseName: b.courseName,
    courseTotal: b.courseTotal,
    courseAmountPaid: b.courseAmountPaid,
    courseRemaining: b.courseRemaining,
  };
}

function mapInvoice(inv: InvoiceDto): Invoice {
  const method = apiToPayMethod(inv.paymentMethod);
  const status: InvStatus =
    inv.status === "Paid" ? "paid" : method === "app" ? "app_waiting" : "pending";
  return {
    id: inv.id,
    planId: inv.invoiceNumber,
    patientName: inv.patientName,
    patientPhone: inv.patientPhone ?? "—",
    gender: toGender(inv.gender),
    dentist: inv.dentistName,
    date: fmtDate(inv.appointmentDate),
    items: inv.items.map(i => ({ name: i.name, qty: i.quantity, price: i.unitPrice })),
    subtotal: inv.subtotal,
    discount: inv.discount,
    finalTotal: inv.totalAmount,
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

function PlansTab({ plans, onIssued }: {
  plans: TreatmentPlan[];
  onIssued: (inv: Invoice) => Promise<boolean>;
}) {
  const [selected, setSelected] = useState<TreatmentPlan | null>(null);
  const [items,    setItems]    = useState<Procedure[]>([]);
  const [discount, setDiscount] = useState(0);
  const [note,     setNote]     = useState("");
  const [payType,  setPayType]  = useState<PayType>("full");
  const [deposit,  setDeposit]  = useState(0);
  const [courseAmount, setCourseAmount] = useState(0);
  const [method,   setMethod]   = useState<PayMethod | null>(null);
  const [saved,    setSaved]    = useState(false);

  const selectPlan = (p: TreatmentPlan) => {
    setSelected(p);
    setItems(p.procedures.map(pr => ({ ...pr })));
    setDiscount(0); setNote(""); setPayType("full"); setDeposit(0);
    setCourseAmount(p.courseRemaining ?? 0); setMethod(null); setSaved(false);
  };

  const updateQty = (i: number, qty: number) =>
    setItems(prev => prev.map((it, idx) => idx === i ? { ...it, qty: Math.max(1, qty) } : it));

  const removeItem = (i: number) =>
    setItems(prev => prev.filter((_, idx) => idx !== i));

  // Mục "thu phần còn lại" của hóa đơn đặt cọc → chỉ thanh toán toàn bộ phần còn lại.
  const isRemaining = !!selected?.outstandingInvoiceId;
  // Mục "đợt thu" của liệu trình dài hạn → nhập số tiền đợt này.
  const isCourse = !!selected?.courseId;
  const courseRemaining = selected?.courseRemaining ?? 0;

  const subtotal   = sum(items);
  const finalTotal = Math.max(0, subtotal - discount);

  // Số tiền thu trên hóa đơn này + kiểm tra hợp lệ khi đặt cọc
  const effType    = isRemaining ? "full" : payType;
  const payAmount  = effType === "deposit" ? deposit : finalTotal;
  const depositOk  = effType === "full" || (deposit > 0 && deposit <= finalTotal);
  const courseOk   = !isCourse || (courseAmount > 0 && courseAmount <= courseRemaining);

  const [issuing, setIssuing] = useState(false);

  const handleIssue = async () => {
    if (!selected || !method || !depositOk || !courseOk) return;
    const inv: Invoice = isCourse
      ? {
          id: selected.id,
          planId: selected.id,
          patientName: selected.patientName,
          patientPhone: selected.patientPhone,
          gender: selected.gender,
          dentist: selected.dentist,
          date: selected.date,
          items: [{ name: `Đợt thu - ${selected.courseName}`, qty: 1, price: courseAmount }],
          subtotal: courseAmount, discount: 0, finalTotal: courseAmount,
          paymentType: "full",
          depositAmount: courseAmount,
          remaining: Math.max(0, courseRemaining - courseAmount),
          paymentMethod: method,
          status: "pending",
          note,
          courseId: selected.courseId,
        }
      : {
          id: selected.id,        // transient; planId carries the appointmentId
          planId: selected.id,
          patientName: selected.patientName,
          patientPhone: selected.patientPhone,
          gender: selected.gender,
          dentist: selected.dentist,
          date: selected.date,
          items: [...items],
          subtotal, discount, finalTotal,
          paymentType: effType,
          depositAmount: payAmount,
          remaining: Math.max(0, finalTotal - payAmount),
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
    <div className="flex gap-6 items-start">
      {/* Plans sidebar */}
      <div className="w-80 shrink-0 flex flex-col gap-3">
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
                  {p.courseId && (
                    <span className="self-start text-[10.5px] font-black px-2 py-0.5 rounded-md bg-indigo-100 text-indigo-700 uppercase tracking-wide">
                      Đợt thu liệu trình
                    </span>
                  )}
                  <div className="flex items-center justify-between">
                    <span className="text-[11.5px] font-semibold text-slate-400">{p.courseId ? "Liệu trình dài hạn" : p.outstandingInvoiceId ? "Phần còn lại" : `${p.procedures.length} dịch vụ`}</span>
                    <span className="text-[13px] font-black text-slate-700">{fmt(p.courseId ? (p.courseRemaining ?? 0) : sum(p.procedures))}</span>
                  </div>
                </div>
              </button>
            );
          })
        )}
      </div>

      {/* Invoice form */}
      <div className="flex-1 min-w-0">
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
            <div className="px-7 py-5 border-b border-slate-100 flex items-center justify-between">
              <div>
                <h3 className="text-[16px] font-black text-slate-900">Hóa đơn điều trị</h3>
                <p className="text-[12.5px] font-semibold text-slate-400 mt-0.5">Từ liệu trình {selected.id} · {selected.date}</p>
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

              {/* Course installment — đợt thu liệu trình dài hạn */}
              {isCourse && (
                <div className="flex flex-col gap-4">
                  <div className="grid grid-cols-3 gap-3">
                    {[
                      { label: "Tổng chi phí", value: selected.courseTotal ?? 0, cls: "text-slate-900" },
                      { label: "Đã thu", value: selected.courseAmountPaid ?? 0, cls: "text-emerald-600" },
                      { label: "Còn lại", value: courseRemaining, cls: "text-orange-600" },
                    ].map(s => (
                      <div key={s.label} className="bg-slate-50 border border-slate-100 rounded-xl px-4 py-3">
                        <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{s.label}</div>
                        <div className={`text-[16px] font-black mt-0.5 font-mono ${s.cls}`}>{fmt(s.value)}</div>
                      </div>
                    ))}
                  </div>
                  <div className="flex flex-col gap-1.5 w-64">
                    <label className={labelCls}>Số tiền thu đợt này (₫)</label>
                    <input type="text" inputMode="numeric" value={fmtMoneyInput(courseAmount)}
                      onChange={e => setCourseAmount(parseMoneyInput(e.target.value))}
                      placeholder="0" className={inputCls} />
                    {courseAmount > courseRemaining && (
                      <p className="text-[12px] font-bold text-red-600">Số tiền vượt quá công nợ còn lại.</p>
                    )}
                  </div>
                </div>
              )}

              {!isCourse && (<>
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
                          <td className="px-3 py-3 text-right font-black text-slate-800 font-mono">{fmt(it.qty * it.price)}</td>
                          <td className="px-2 py-3 text-center">
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

              {/* Discount + total */}
              <div className="flex gap-6 items-end">
                <div className="flex flex-col gap-1.5 w-56">
                  <label className={labelCls}>Giảm giá (₫)</label>
                  <input type="text" inputMode="numeric" value={fmtMoneyInput(discount)}
                    onChange={e => setDiscount(parseMoneyInput(e.target.value))}
                    placeholder="0"
                    className={inputCls} />
                </div>
                <div className="flex-1" />
                <div className="flex flex-col items-end gap-2 min-w-56">
                  <div className="flex items-center justify-between w-full text-[13px] text-slate-500 font-semibold">
                    <span>Tạm tính</span><span className="font-mono">{fmt(subtotal)}</span>
                  </div>
                  {discount > 0 && (
                    <div className="flex items-center justify-between w-full text-[13px] text-emerald-600 font-semibold">
                      <span>Giảm giá</span><span className="font-mono">−{fmt(discount)}</span>
                    </div>
                  )}
                  <div className="w-full h-px bg-slate-200" />
                  <div className="flex items-center justify-between w-full">
                    <span className="text-[13.5px] font-extrabold text-slate-700 uppercase tracking-wider">Tổng cộng</span>
                    <span className="text-[22px] font-black text-primary font-mono leading-none">{fmt(finalTotal)}</span>
                  </div>
                </div>
              </div>
              </>)}

              {/* Payment type (ẩn khi thu phần còn lại / đợt thu liệu trình) */}
              {!isRemaining && !isCourse && (
              <div className="flex flex-col gap-3">
                <span className={labelCls}>Loại thanh toán</span>
                <div className="grid grid-cols-2 gap-3">
                  {([
                    { key: "full",    label: "Thanh toán toàn bộ", desc: "Thu đủ tổng tiền điều trị",
                      icon: "M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" },
                    { key: "deposit", label: "Đặt cọc",            desc: "Thu trước một phần, còn lại trả sau",
                      icon: "M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5z" },
                  ] as { key: PayType; label: string; desc: string; icon: string }[]).map(t => {
                    const active = payType === t.key;
                    return (
                      <button key={t.key} onClick={() => { setPayType(t.key); if (t.key === "full") setDeposit(0); }}
                        className={`flex items-start gap-3 px-4 py-3.5 rounded-2xl border-2 text-left transition-all cursor-pointer ${
                          active ? "bg-primary/5 border-primary shadow-sm" : "bg-white border-slate-200 hover:border-slate-300"
                        }`}>
                        <svg className={`w-6 h-6 shrink-0 mt-0.5 transition-colors ${active ? "text-primary" : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d={t.icon} />
                        </svg>
                        <div>
                          <div className={`text-[13.5px] font-black transition-colors ${active ? "text-primary" : "text-slate-700"}`}>{t.label}</div>
                          <div className="text-[11.5px] font-semibold text-slate-400 mt-0.5">{t.desc}</div>
                        </div>
                      </button>
                    );
                  })}
                </div>

                {payType === "deposit" && (
                  <div className="flex flex-wrap items-end gap-6 px-4 py-4 bg-amber-50/60 border border-amber-200 rounded-xl">
                    <div className="flex flex-col gap-1.5 w-56">
                      <label className={labelCls}>Số tiền đặt cọc (₫)</label>
                      <input type="text" inputMode="numeric" value={fmtMoneyInput(deposit)}
                        onChange={e => setDeposit(parseMoneyInput(e.target.value))}
                        placeholder="0"
                        className={inputCls} />
                    </div>
                    <div className="flex flex-col items-end gap-1.5 ml-auto">
                      <div className="flex items-center justify-between gap-6 w-full text-[13px] font-semibold text-slate-500">
                        <span>Cọc trước</span><span className="font-mono text-amber-700 font-black">{fmt(payAmount)}</span>
                      </div>
                      <div className="flex items-center justify-between gap-6 w-full text-[13px] font-semibold text-slate-500">
                        <span>Còn lại</span><span className="font-mono">{fmt(Math.max(0, finalTotal - payAmount))}</span>
                      </div>
                    </div>
                    {deposit > finalTotal && (
                      <p className="w-full text-[12px] font-bold text-red-600">Số tiền đặt cọc không được vượt quá tổng tiền.</p>
                    )}
                  </div>
                )}
              </div>
              )}

              {/* Payment method */}
              <div className="flex flex-col gap-3">
                <span className={labelCls}>Phương thức thanh toán</span>
                <div className="grid grid-cols-3 gap-3">
                  {(Object.keys(PAY_CFG) as PayMethod[]).map(m => {
                    const cfg    = PAY_CFG[m];
                    const active = method === m;
                    return (
                      <button key={m} onClick={() => setMethod(m)}
                        className={`flex flex-col items-center gap-2.5 px-4 py-5 rounded-2xl border-2 transition-all cursor-pointer ${
                          active
                            ? `${cfg.bg} ${cfg.border} shadow-sm`
                            : "bg-white border-slate-200 hover:border-slate-300"
                        }`}>
                        <svg className={`w-7 h-7 transition-colors ${active ? cfg.color : "text-slate-400"}`} fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d={cfg.icon} />
                        </svg>
                        <span className={`text-[13px] font-black transition-colors ${active ? cfg.color : "text-slate-500"}`}>{cfg.label}</span>
                        {active && <span className={`w-1.5 h-1.5 rounded-full ${cfg.bg.replace("bg-","bg-").replace("50","500")}`} style={{ backgroundColor: "currentColor" }} />}
                      </button>
                    );
                  })}
                </div>
                {method === "app" && (
                  <div className="flex items-center gap-2 px-4 py-2.5 bg-indigo-50 border border-indigo-100 rounded-xl text-[12.5px] font-semibold text-indigo-700">
                    <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" /></svg>
                    Hóa đơn sẽ được gửi đến app của bệnh nhân. Staff xác nhận sau khi nhận được thông báo thanh toán thành công.
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
                <button onClick={handleIssue} disabled={!method || (!isCourse && items.length === 0) || !depositOk || !courseOk || issuing}
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

/* ─── Tab 2: Pending payment ─────────────────────────────── */

function PendingTab({ invoices, onPaid }: {
  invoices: Invoice[];
  onPaid: (id: string, method: PayMethod | null) => void;
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
      {invoices.map(inv => {
        const m   = getMethod(inv);
        const isAppWaiting = inv.status === "app_waiting";

        return (
          <div key={inv.id} className={`bg-white rounded-2xl border shadow-sm overflow-hidden ${
            isAppWaiting ? "border-indigo-200 shadow-indigo-50" : "border-slate-200/70"
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
                      <div className="text-[12px] font-semibold text-emerald-600 mt-0.5">Đã giảm {fmt(inv.discount)}</div>
                    )}
                  </>
                )}
              </div>
            </div>

            {/* Payment section */}
            <div className="px-7 pb-5">
              {isAppWaiting ? (
                <div className="flex items-center justify-between gap-4 px-5 py-4 bg-indigo-50 border border-indigo-200 rounded-xl">
                  <div className="flex items-center gap-3">
                    <div className="relative flex h-3 w-3 shrink-0">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75" />
                      <span className="relative inline-flex rounded-full h-3 w-3 bg-indigo-500" />
                    </div>
                    <div>
                      <p className="text-[13.5px] font-black text-indigo-800">Bệnh nhân đã thanh toán qua App</p>
                      <p className="text-[12px] font-semibold text-indigo-600 mt-0.5">Xác nhận để hoàn tất hóa đơn</p>
                    </div>
                  </div>
                  <button onClick={() => onPaid(inv.id, null)}
                    className="flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm shadow-indigo-200 whitespace-nowrap">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                    Xác nhận đã nhận
                  </button>
                </div>
              ) : (
                <div className="flex items-center gap-3 flex-wrap">
                  <span className={`${labelCls} shrink-0`}>Thanh toán qua:</span>
                  <div className="flex gap-2 flex-wrap flex-1">
                    {(Object.keys(PAY_CFG) as PayMethod[]).map(pm => {
                      const pcfg   = PAY_CFG[pm];
                      const active = m === pm;
                      return (
                        <button key={pm} onClick={() => setMethodEdit(prev => ({ ...prev, [inv.id]: pm }))}
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
                  {m && (
                    <button onClick={() => onPaid(inv.id, m)}
                      className={`flex items-center gap-2 px-5 py-2.5 text-white rounded-xl text-[13px] font-black cursor-pointer transition-all shadow-sm whitespace-nowrap ${
                        m === "cash"     ? "bg-emerald-500 hover:bg-emerald-600 shadow-emerald-200" :
                        m === "transfer" ? "bg-sky-500 hover:bg-sky-600 shadow-sky-200" :
                                           "bg-indigo-500 hover:bg-indigo-600 shadow-indigo-200"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      {m === "cash" ? "Đã nhận tiền mặt" : m === "transfer" ? "Đã nhận chuyển khoản" : "Gửi yêu cầu lên App"}
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        );
      })}
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
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Doanh thu",   value: fmt(todayRevenue), icon: "M2.25 18.75a60.07 60.07 0 0115.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 013 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 00-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 01-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 003 15h-.75M15 10.5a3 3 0 11-6 0 3 3 0 016 0zm3 0h.008v.008H18V10.5zm-12 0h.008v.008H6V10.5z", color: "text-emerald-600", bg: "bg-emerald-50" },
          { label: "Tổng hóa đơn",        value: `${paid.length} hóa đơn`,                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                icon: "M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z", color: "text-sky-600",     bg: "bg-sky-50"     },
          { label: "Trung bình / hóa đơn", value: paid.length > 0 ? fmt(Math.round(paid.reduce((s, i) => s + i.depositAmount, 0) / paid.length)) : "—",                                                                                                                                                                                                                                                                                                                                                                                                                                                      icon: "M7.5 14.25v2.25m3-4.5v4.5m3-6.75v6.75m3-9v9M6 20.25h12A2.25 2.25 0 0020.25 18V6A2.25 2.25 0 0018 3.75H6A2.25 2.25 0 003.75 6v12A2.25 2.25 0 006 20.25z", color: "text-violet-600", bg: "bg-violet-50"  },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-2xl border border-slate-200/70 shadow-sm px-5 py-4 flex items-center gap-4">
            <div className={`w-11 h-11 rounded-xl ${s.bg} flex items-center justify-center shrink-0`}>
              <svg className={`w-5 h-5 ${s.color}`} fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={s.icon} /></svg>
            </div>
            <div>
              <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{s.label}</div>
              <div className="text-[17px] font-black text-slate-900 mt-0.5">{s.value}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
        <table className="w-full text-[13px]">
          <thead>
            <tr className="bg-slate-50/70 border-b border-slate-200">
              {["Mã HĐ","Ngày","Bệnh nhân","Bác sĩ","Nội dung","Thanh toán","Tổng tiền"].map(h => (
                <th key={h} className="px-5 py-3 text-left text-[11px] font-extrabold text-slate-400 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {[...paid].reverse().map(inv => {
              const m = inv.paymentMethod ? PAY_CFG[inv.paymentMethod] : null;
              return (
                <tr key={inv.id} className="hover:bg-slate-50/50 transition-colors">
                  <td className="px-5 py-3.5 font-black text-slate-500 font-mono text-[12px]">{inv.id}</td>
                  <td className="px-5 py-3.5 font-semibold text-slate-500 whitespace-nowrap">{inv.date}</td>
                  <td className="px-5 py-3.5">
                    <div className="font-black text-slate-900">{inv.patientName}</div>
                    <div className="font-mono text-slate-400 text-[11.5px]">{inv.patientPhone}</div>
                  </td>
                  <td className="px-5 py-3.5">
                    <span className={`text-[11.5px] font-black px-2 py-0.5 rounded-lg border ${DENTIST_COLOR[inv.dentist] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>{inv.dentist}</span>
                  </td>
                  <td className="px-5 py-3.5 max-w-[200px]">
                    <p className="text-slate-600 font-semibold text-[12.5px] leading-snug">
                      {inv.items.map((it, i) => (
                        <span key={i}>{it.qty > 1 ? `${it.qty}× ` : ""}{it.name}{i < inv.items.length - 1 ? "; " : ""}</span>
                      ))}
                    </p>
                  </td>
                  <td className="px-5 py-3.5">
                    {m && (
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl text-[12px] font-black border ${m.bg} ${m.border} ${m.color}`}>
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d={m.icon} /></svg>
                        {m.label}
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3.5 text-right">
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
  );
}

/* ─── Tab 4: Outstanding (công nợ) ───────────────────────── */

function OutstandingTab({ invoices, courses, onCollect }: { invoices: Invoice[]; courses: OutstandingCourseDto[]; onCollect: (id: string) => void }) {
  const totalRemaining = invoices.reduce((s, i) => s + i.remaining, 0) + courses.reduce((s, c) => s + c.remainingAmount, 0);
  const totalCollected = invoices.reduce((s, i) => s + i.depositAmount, 0) + courses.reduce((s, c) => s + c.amountPaid, 0);
  const totalCount = invoices.length + courses.length;

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
      {/* Summary cards */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Tổng còn nợ",    value: fmt(totalRemaining),          color: "text-orange-600", bg: "bg-orange-50" },
          { label: "Đã thu (cọc)",   value: fmt(totalCollected),          color: "text-emerald-600", bg: "bg-emerald-50" },
          { label: "Số khoản nợ",    value: `${totalCount} khoản`,        color: "text-sky-600",     bg: "bg-sky-50" },
        ].map(s => (
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

      {/* Deposit invoices table */}
      {invoices.length > 0 && (<>
      <span className={labelCls}>Hóa đơn đặt cọc còn nợ</span>
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
      </>)}

      {/* Long-term course debts */}
      {courses.length > 0 && (<>
      <span className={labelCls}>Liệu trình dài hạn còn nợ</span>
      <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm overflow-hidden">
        <table className="w-full text-[13px]">
          <thead>
            <tr className="bg-slate-50/70 border-b border-slate-200">
              {["Liệu trình","Bệnh nhân","Bác sĩ","Tổng chi phí","Đã thu","Còn lại","Trạng thái"].map((h, hi) => (
                <th key={hi} className="px-5 py-3 text-left text-[11px] font-extrabold text-slate-400 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {courses.map(c => (
              <tr key={c.courseId} className="hover:bg-slate-50/50 transition-colors">
                <td className="px-5 py-3.5 font-black text-slate-900">{c.courseName}</td>
                <td className="px-5 py-3.5">
                  <div className="font-black text-slate-900">{c.patientName}</div>
                  <div className="font-mono text-slate-400 text-[11.5px]">{c.patientPhone ?? "—"}</div>
                </td>
                <td className="px-5 py-3.5">
                  <span className={`text-[11.5px] font-black px-2 py-0.5 rounded-lg border ${DENTIST_COLOR[c.dentistName] ?? "bg-slate-50 text-slate-600 border-slate-200"}`}>{c.dentistName}</span>
                </td>
                <td className="px-5 py-3.5 font-mono font-semibold text-slate-600">{fmt(c.totalCost)}</td>
                <td className="px-5 py-3.5 font-mono font-semibold text-emerald-600">{fmt(c.amountPaid)}</td>
                <td className="px-5 py-3.5 font-mono font-black text-orange-600">{fmt(c.remainingAmount)}</td>
                <td className="px-5 py-3.5">
                  <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl text-[11.5px] font-black border bg-indigo-50 border-indigo-200 text-indigo-700">Đang điều trị</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="text-[12px] font-semibold text-slate-400 -mt-1">
        Mỗi đợt thu của liệu trình được thực hiện ở tab <strong>Liệu trình → Hóa đơn</strong> sau khi bác sĩ kết thúc buổi điều trị/tái khám.
      </p>
      </>)}
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
  const [outstandingCourses, setOutstandingCourses] = useState<OutstandingCourseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);

  // Bộ lọc theo ngày — mặc định là hôm nay; "" = tất cả các ngày
  const [filterDate, setFilterDate] = useState<string>(todayIso());
  const viDate = filterDate ? isoToVi(filterDate) : null;
  const byDate = <T extends { date: string }>(list: T[]) =>
    viDate ? list.filter(i => i.date === viDate) : list;

  const fPlans   = byDate(plans);
  const fPending = byDate(pending);
  const fPaid    = byDate(paid);

  const reload = useCallback(async () => {
    const [billable, pendingInv, history, outstandingInv, outstandingCrs] = await Promise.all([
      getBillablePlansApi(),
      getPendingInvoicesApi(),
      getInvoiceHistoryApi(),
      getOutstandingInvoicesApi(),
      getOutstandingCoursesApi(),
    ]);
    setPlans(billable.map(mapPlan));
    setPending(pendingInv.map(mapInvoice));
    setPaid(history.map(mapInvoice));
    setOutstanding(outstandingInv.map(mapInvoice));
    setOutstandingCourses(outstandingCrs);
  }, []);

  useEffect(() => {
    reload()
      .catch(e => setError(e instanceof Error ? e.message : "Không thể tải dữ liệu hóa đơn"))
      .finally(() => setLoading(false));
  }, [reload]);

  // Xuất hóa đơn từ liệu trình (planId chính là appointmentId)
  const handleIssued = async (inv: Invoice): Promise<boolean> => {
    try {
      await issueInvoiceApi({
        appointmentId: inv.planId!,
        items: inv.items.map(i => ({ name: i.name, quantity: i.qty, unitPrice: i.price })),
        discount: inv.discount,
        paymentMethod: inv.paymentMethod ?? "cash",
        paymentType: inv.paymentType,
        depositAmount: inv.depositAmount,
        notes: inv.note || undefined,
        parentInvoiceId: inv.parentInvoiceId ?? undefined,
        courseId: inv.courseId ?? undefined,
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
      setError(e instanceof Error ? e.message : "Xác nhận thanh toán thất bại");
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
            <div className="flex items-center gap-2 text-[12.5px] font-bold">
              {fPlans.length > 0 && (
                <span className="flex items-center gap-1.5 px-2.5 py-1.5 bg-amber-50 text-amber-700 border border-amber-200 rounded-xl">
                  <span className="relative flex h-2 w-2">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
                    <span className="relative inline-flex rounded-full h-2 w-2 bg-amber-500" />
                  </span>
                  {fPlans.length} liệu trình chờ
                </span>
              )}
              {fPending.length > 0 && (
                <span className="flex items-center gap-1.5 px-2.5 py-1.5 bg-indigo-50 text-indigo-700 border border-indigo-200 rounded-xl">
                  {fPending.some(i => i.status === "app_waiting") && (
                    <span className="relative flex h-2 w-2">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75" />
                      <span className="relative inline-flex rounded-full h-2 w-2 bg-indigo-500" />
                    </span>
                  )}
                  {fPending.length} chờ thanh toán
                </span>
              )}

              {/* Bộ lọc ngày */}
              <div className="flex items-center gap-1.5 pl-1">
                <div className="relative flex items-center">
                  <svg className="w-4 h-4 text-slate-400 absolute left-2.5 pointer-events-none" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                  </svg>
                  <input
                    type="date"
                    value={filterDate}
                    onChange={e => setFilterDate(e.target.value)}
                    className="pl-8 pr-3 py-1.5 text-[12.5px] font-bold bg-white border border-slate-200 rounded-xl text-slate-700 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none cursor-pointer"
                  />
                </div>
                {filterDate ? (
                  <button onClick={() => setFilterDate("")}
                    title="Xem tất cả các ngày"
                    className="px-2.5 py-1.5 rounded-xl border border-slate-200 bg-white text-slate-500 hover:text-primary hover:border-primary/40 transition-all cursor-pointer">
                    Tất cả
                  </button>
                ) : (
                  <button onClick={() => setFilterDate(todayIso())}
                    title="Về hôm nay"
                    className="px-2.5 py-1.5 rounded-xl border border-primary/30 bg-primary/5 text-primary hover:bg-primary/10 transition-all cursor-pointer">
                    Hôm nay
                  </button>
                )}
              </div>
            </div>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Tabs */}
          <div className="flex gap-2">
            {([
              { key: "plans",       label: "Liệu trình → Hóa đơn", count: fPlans.length,      dot: fPlans.length > 0 },
              { key: "pending",     label: "Chờ thanh toán",         count: fPending.length,    dot: fPending.some(i => i.status === "app_waiting") },
              { key: "outstanding", label: "Công nợ",                count: outstanding.length + outstandingCourses.length, dot: (outstanding.length + outstandingCourses.length) > 0 },
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
              {tab === "plans"       && <PlansTab       plans={fPlans}        onIssued={handleIssued} />}
              {tab === "pending"     && <PendingTab     invoices={fPending}    onPaid={handlePaid}    />}
              {tab === "outstanding" && <OutstandingTab invoices={outstanding} courses={outstandingCourses} onCollect={handleCollectRemaining} />}
              {tab === "history"     && <HistoryTab     paid={fPaid}                                  />}
            </>
          )}
        </div>
      </main>
    </div>
  );
}
