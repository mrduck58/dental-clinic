"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import DentistSidebar from "../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../components/shared/DentistPageHeader";
import ToothArchDiagram, { TOOTH_COLOR, TOOTH_LEGEND, UPPER_TEETH, LOWER_TEETH, ARCH_H, type ToothStatus as TS, type ToothState as TState } from "../../../../components/shared/ToothArchDiagram";
import { useRequireDentist } from "../../../../hooks/useRequireDentist";

// ─── Types & mock data ────────────────────────────────────────────────────────

type ToothStatus = TS;
type TreatmentStatus = "pending" | "in_progress" | "done";

type ToothState = TState;
interface TreatmentStep { id: string; tooth: string; procedure: string; status: TreatmentStatus; cost: number; note: string; date: string }
interface Medication { id: string; name: string; dosage: string; frequency: string; duration: string; note: string }
interface FollowUp { date: string; notes: string; doctor: string }

const MOCK_PATIENTS: Record<string, {
  name: string; age: number; gender: string; dob: string;
  phone: string; email: string; address: string; bloodType: string;
  allergies: string[]; conditions: string[];
  visitHistory: { date: string; reason: string; doctor: string }[];
}> = {
  P001: {
    name: "Nguyễn Văn An", age: 34, gender: "Nam", dob: "15/03/1992",
    phone: "0912 345 678", email: "nguyenvanan@email.com", address: "45 Nguyễn Huệ, Q.1, TP.HCM",
    bloodType: "O+", allergies: ["Penicillin"], conditions: ["Tăng huyết áp (đang kiểm soát)"],
    visitHistory: [
      { date: "12/06/2026", reason: "Nhổ răng khôn hàm dưới", doctor: "Bs. Lê Phương Thảo" },
      { date: "20/03/2026", reason: "Lấy cao răng định kỳ",    doctor: "Bs. Lê Phương Thảo" },
      { date: "05/11/2025", reason: "Trám răng số 7",           doctor: "Bs. Nguyễn Hòa"     },
    ],
  },
  P002: {
    name: "Trần Thị Bích", age: 28, gender: "Nữ", dob: "22/07/1997",
    phone: "0908 765 432", email: "tranbich97@gmail.com", address: "12 Lê Lợi, Q.3, TP.HCM",
    bloodType: "A+", allergies: [], conditions: [],
    visitHistory: [
      { date: "12/06/2026", reason: "Trám răng số 6",    doctor: "Bs. Lê Phương Thảo" },
      { date: "10/01/2026", reason: "Kiểm tra định kỳ", doctor: "Bs. Lê Phương Thảo" },
    ],
  },
};
const FALLBACK = MOCK_PATIENTS["P002"];


const TX_STATUS: Record<TreatmentStatus, { label: string; cls: string }> = {
  pending:     { label: "Chờ thực hiện",  cls: "bg-slate-100 text-slate-600 border border-slate-200" },
  in_progress: { label: "Đang thực hiện", cls: "bg-sky-50 text-sky-700 border border-sky-100"         },
  done:        { label: "Hoàn thành",     cls: "bg-green-50 text-green-700 border border-green-100"   },
};

function SectionHeading({ color, title, icon, action }: {
  color: string; title: string; icon: string; action?: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
      <div className="flex items-center gap-3">
        <div className={`w-8 h-8 rounded-xl ${color} flex items-center justify-center shrink-0`}>
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
          </svg>
        </div>
        <span className="text-[15px] font-black text-slate-900">{title}</span>
      </div>
      {action}
    </div>
  );
}

export default function PatientDetailPage() {
  useRequireDentist();
  const { id } = useParams<{ id: string }>();
  const patient = MOCK_PATIENTS[id as string] ?? FALLBACK;

  const [teeth, setTeeth]         = useState<ToothState>({ "36": "decay", "46": "decay", "11": "crown", "21": "crown", "17": "filled" });
  const [selectedTooth, setSel]   = useState<string | null>(null);
  const [complaint, setComplaint] = useState("Bệnh nhân đau nhức răng hàm dưới trái, cảm giác ê buốt khi ăn đồ lạnh.");
  const [findings, setFindings]   = useState("Răng 36 có lỗ sâu độ 2, chạm ngà. Nướu quanh răng viêm nhẹ.");
  const [diagnosis, setDiagnosis] = useState("Sâu răng số 36 độ 2 (D2). Viêm nướu cục bộ.");
  const [diagSaved, setDiagSaved] = useState(false);

  const [steps, setSteps] = useState<TreatmentStep[]>([
    { id: "T1", tooth: "Răng 36",  procedure: "Trám răng composite", status: "in_progress", cost: 350000,  note: "Trám composite P60, màu A2",     date: "12/06/2026" },
    { id: "T2", tooth: "Răng 46",  procedure: "Điều trị tủy",        status: "pending",     cost: 1200000, note: "Cần chụp X-quang trước khi làm", date: "" },
    { id: "T3", tooth: "Toàn hàm", procedure: "Lấy cao răng siêu âm",status: "pending",     cost: 200000,  note: "",                               date: "" },
  ]);

  const [meds, setMeds] = useState<Medication[]>([
    { id: "M1", name: "Amoxicillin 500mg",   dosage: "500mg", frequency: "3 lần/ngày", duration: "7 ngày",  note: "Uống sau ăn" },
    { id: "M2", name: "Ibuprofen 400mg",     dosage: "400mg", frequency: "2 lần/ngày", duration: "5 ngày",  note: "Uống khi đau, không uống khi đói" },
    { id: "M3", name: "Chlorhexidine 0.12%", dosage: "15ml",  frequency: "2 lần/ngày", duration: "10 ngày", note: "Súc miệng 30 giây, không nuốt" },
  ]);

  const [followUps] = useState<FollowUp[]>([
    { date: "20/03/2026", notes: "Kiểm tra sau trám răng số 7, tái khám nếu còn đau",         doctor: "Bs. Lê Phương Thảo" },
    { date: "05/11/2025", notes: "Theo dõi sau lấy cao răng, hẹn 6 tháng kiểm tra định kỳ",  doctor: "Bs. Nguyễn Hòa" },
  ]);

  const initials = patient.name.trim().split(/\s+/).slice(-2).map((w: string) => w[0]).join("").toUpperCase();
  const totalCost = steps.reduce((sum, s) => sum + s.cost, 0);

  const setToothStatus = (tooth: string, status: ToothStatus) => { setTeeth((p) => ({ ...p, [tooth]: status })); setSel(null); };
  const cycleStatus = (sid: string) => {
    const order: TreatmentStatus[] = ["pending", "in_progress", "done"];
    setSteps((prev) => prev.map((s) => s.id === sid ? { ...s, status: order[(order.indexOf(s.status) + 1) % order.length] } : s));
  };

return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title={patient.name}
          subtitle={`${patient.age} tuổi · ${patient.gender} · ${patient.phone}`}
          left={
            <div className="flex items-center gap-2.5">
              <Link href="/dentist/patients" className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-700 transition-all shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
              </Link>
              <div className={`w-9 h-9 rounded-full flex items-center justify-center font-black text-[12px] border shrink-0 ${patient.gender === "Nữ" ? "bg-rose-50 text-rose-600 border-rose-100" : "bg-sky-50 text-sky-700 border-sky-100"}`}>
                {initials}
              </div>
            </div>
          }
          right={
            <span className="px-3 py-1.5 bg-sky-50 text-sky-700 border border-sky-100 text-[12px] font-black rounded-xl">Đang khám</span>
          }
        />

        {/* CONTENT — 2 columns */}
        <div className="p-8 flex-1 overflow-y-auto">
          <div className="grid gap-6" style={{ gridTemplateColumns: "1fr 22rem" }}>

            {/* ── LEFT: clinical sections ── */}
            <div className="flex flex-col gap-6 min-w-0">

          {/* 2 — CHUẨN ĐOÁN */}
          <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <SectionHeading color="bg-amber-50 text-amber-700" title="Chuẩn đoán"
              icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            <div className="p-6 flex flex-col gap-5">
              <div className="bg-slate-50 rounded-xl p-5 flex flex-col gap-4">
                <span className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Sơ đồ răng</span>
                <ToothArchDiagram
                  teeth={teeth}
                  selected={selectedTooth ? new Set([selectedTooth]) : new Set()}
                  onToothClick={(num) => setSel(selectedTooth === num ? null : num)}
                  showLegend
                />
                {selectedTooth && (
                  <div className="bg-white border border-slate-200 rounded-xl p-4 flex flex-col gap-2">
                    <span className="text-[13px] font-black text-slate-800">Răng {selectedTooth} — Chọn trạng thái:</span>
                    <div className="flex gap-2 flex-wrap">
                      {(["normal","decay","filled","missing","crown","implant"] as ToothStatus[]).map((s) => (
                        <button key={s} onClick={() => setToothStatus(selectedTooth, s)}
                          className={`px-3 py-1.5 rounded-lg text-[12px] font-bold border-2 transition-all cursor-pointer ${TOOTH_COLOR[s]}`}>
                          {TOOTH_LEGEND.find((l) => l.status === s)?.label ?? s}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                {([
                  { label: "Lý do đến khám",              value: complaint,  set: setComplaint },
                  { label: "Kết quả thăm khám lâm sàng",  value: findings,   set: setFindings  },
                  { label: "Chẩn đoán",                   value: diagnosis,  set: setDiagnosis },
                ] as { label: string; value: string; set: (v: string) => void }[]).map(({ label, value, set }) => (
                  <div key={label} className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">{label}</label>
                    <textarea value={value} onChange={(e) => set(e.target.value)} rows={4}
                      className="px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 resize-none" />
                  </div>
                ))}
              </div>
              <div className="flex items-center gap-3 justify-end">
                {diagSaved && <span className="text-[13px] text-green-700 font-bold animate-fade-in">✓ Đã lưu</span>}
                <button onClick={() => { setDiagSaved(true); setTimeout(() => setDiagSaved(false), 3000); }}
                  className="flex items-center gap-2 px-5 py-2.5 bg-primary text-white text-[13px] font-black rounded-xl hover:bg-red-600 transition-all shadow-sm cursor-pointer">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  Lưu chuẩn đoán
                </button>
              </div>
            </div>
          </section>

          {/* 3 — LIỆU TRÌNH */}
          <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <SectionHeading color="bg-red-50 text-primary" title="Liệu trình điều trị"
              icon="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z"
              action={
                steps.length > 0 ? (
                  <Link href={`/dentist/patients/${id}/treatment/new${selectedTooth ? `?teeth=${selectedTooth}` : ""}`}
                    className="flex items-center gap-2 px-4 py-2 bg-slate-100 text-slate-600 hover:bg-slate-200 text-[13px] font-bold rounded-xl transition-all">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125" /></svg>
                    Chỉnh sửa
                  </Link>
                ) : (
                  <Link href={`/dentist/patients/${id}/treatment/new${selectedTooth ? `?teeth=${selectedTooth}` : ""}`}
                    className="flex items-center gap-2 px-4 py-2 bg-primary text-white text-[13px] font-bold rounded-xl hover:bg-red-600 transition-all shadow-sm">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                    Tạo liệu trình
                  </Link>
                )
              } />
            <div className="p-6 flex flex-col gap-4">
              <div className="text-[13px] font-bold text-slate-600">
                Tổng chi phí dự kiến: <span className="text-primary font-black text-[15px]">{totalCost.toLocaleString("vi-VN")}đ</span>
              </div>
              <div className="flex flex-col gap-3">
                {steps.map((step, idx) => {
                  const s = TX_STATUS[step.status];
                  return (
                    <div key={step.id} className="bg-slate-50 border border-slate-200 rounded-xl p-4 flex items-start gap-4">
                      <div className="w-8 h-8 rounded-full bg-white border-2 border-slate-200 flex items-center justify-center text-[13px] font-black text-slate-500 shrink-0">{idx + 1}</div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between gap-3 flex-wrap">
                          <div>
                            <span className="text-[14px] font-black text-slate-900">{step.procedure}</span>
                            <span className="text-[12.5px] text-slate-500 font-semibold ml-2">· {step.tooth}</span>
                          </div>
                          <div className="flex items-center gap-2 shrink-0">
                            <span className="text-[13px] font-black text-slate-700">{step.cost.toLocaleString("vi-VN")}đ</span>
                            <button onClick={() => cycleStatus(step.id)}
                              className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11.5px] font-black border cursor-pointer hover:opacity-80 transition-opacity ${s.cls}`}>
                              {s.label}
                            </button>
                          </div>
                        </div>
                        {step.note && <p className="text-[12.5px] text-slate-500 font-medium mt-1">{step.note}</p>}
                        {step.date && <p className="text-[11.5px] text-slate-400 font-semibold mt-1">Ngày: {step.date}</p>}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </section>

          {/* 4 — ĐƠN THUỐC */}
          <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <SectionHeading color="bg-violet-50 text-violet-700" title="Đơn thuốc"
              icon="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5"
              action={
                <div className="flex items-center gap-2">
                  {meds.length > 0 && (
                    <button className="flex items-center gap-2 px-4 py-2 bg-slate-100 text-slate-600 hover:bg-slate-200 text-[13px] font-bold rounded-xl transition-all cursor-pointer">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.72 13.829c-.24.03-.48.062-.72.096m.72-.096a42.415 42.415 0 0110.56 0m-10.56 0L6.34 18m10.94-4.171c.24.03.48.062.72.096m-.72-.096L17.66 18m0 0l.229 2.523a1.125 1.125 0 01-1.12 1.227H7.231c-.662 0-1.18-.568-1.12-1.227L6.34 18m11.318 0h1.091A2.25 2.25 0 0021 15.75V9.456c0-1.081-.768-2.015-1.837-2.175a48.055 48.055 0 00-1.913-.247M6.34 18H5.25A2.25 2.25 0 013 15.75V9.456c0-1.081.768-2.015 1.837-2.175a48.041 48.041 0 011.913-.247m10.5 0a48.536 48.536 0 00-10.5 0m10.5 0V3.375c0-.621-.504-1.125-1.125-1.125h-8.25c-.621 0-1.125.504-1.125 1.125v3.659M18 10.5h.008v.008H18V10.5zm-3 0h.008v.008H15V10.5z" /></svg>
                      In đơn
                    </button>
                  )}
                  {meds.length > 0 ? (
                    <Link href={`/dentist/patients/${id}/prescription/new`}
                      className="flex items-center gap-2 px-4 py-2 bg-slate-100 text-slate-600 hover:bg-slate-200 text-[13px] font-bold rounded-xl transition-all">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125" /></svg>
                      Chỉnh sửa
                    </Link>
                  ) : (
                    <Link href={`/dentist/patients/${id}/prescription/new`}
                      className="flex items-center gap-2 px-4 py-2 bg-primary text-white text-[13px] font-bold rounded-xl hover:bg-red-600 transition-all shadow-sm">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                      Tạo đơn thuốc
                    </Link>
                  )}
                </div>
              } />
            <div className="overflow-x-auto">
              <table className="w-full text-[13px]">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-100 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">
                    <th className="px-5 py-3.5 text-left w-8">#</th>
                    <th className="px-5 py-3.5 text-left">Tên thuốc</th>
                    <th className="px-5 py-3.5 text-left">Liều dùng</th>
                    <th className="px-5 py-3.5 text-left">Tần suất</th>
                    <th className="px-5 py-3.5 text-left">Thời gian</th>
                    <th className="px-5 py-3.5 text-left">Ghi chú</th>
                    <th className="px-5 py-3.5 w-10" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {meds.map((m, idx) => (
                    <tr key={m.id} className="hover:bg-slate-50/50">
                      <td className="px-5 py-3.5 text-slate-400 font-bold">{idx + 1}</td>
                      <td className="px-5 py-3.5 font-black text-slate-900">{m.name}</td>
                      <td className="px-5 py-3.5 font-semibold text-slate-700">{m.dosage}</td>
                      <td className="px-5 py-3.5 font-semibold text-slate-700">{m.frequency}</td>
                      <td className="px-5 py-3.5 font-semibold text-slate-700">{m.duration}</td>
                      <td className="px-5 py-3.5 text-slate-500 font-medium">{m.note}</td>
                      <td className="px-5 py-3.5">
                        <button onClick={() => setMeds(meds.filter((x) => x.id !== m.id))}
                          className="p-1 rounded-lg hover:bg-red-50 text-slate-400 hover:text-primary transition-all cursor-pointer">
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          {/* 5 — TÁI KHÁM */}
          <section className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <SectionHeading color="bg-green-50 text-green-700" title="Lịch tái khám"
              icon="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5m-9-6h.008v.008H12v-.008zM12 15h.008v.008H12V15zm0 2.25h.008v.008H12v-.008zM9.75 15h.008v.008H9.75V15zm0 2.25h.008v.008H9.75v-.008zM7.5 15h.008v.008H7.5V15zm0 2.25h.008v.008H7.5v-.008zm6.75-4.5h.008v.008h-.008v-.008zm0 2.25h.008v.008h-.008V15zm0 2.25h.008v.008h-.008v-.008zm2.25-4.5h.008v.008H16.5v-.008zm0 2.25h.008v.008H16.5V15z"
              action={
                <Link href={`/dentist/patients/${id}/followup/new`}
                  className="flex items-center gap-2 px-4 py-2 bg-primary text-white text-[13px] font-bold rounded-xl hover:bg-red-600 transition-all shadow-sm">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                  Đặt lịch mới
                </Link>
              } />
            <div className="p-6 flex flex-col gap-3">
              {followUps.length === 0 && <p className="text-[13px] text-slate-400 font-semibold">Chưa có lịch tái khám.</p>}
              {followUps.map((h, i) => (
                <div key={i} className="bg-slate-50 border border-slate-100 rounded-xl px-5 py-4 flex items-start gap-4">
                  <div className="w-10 h-10 rounded-xl bg-green-50 text-green-700 flex items-center justify-center shrink-0">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center justify-between gap-3">
                      <span className="text-[14px] font-black text-slate-900">{h.date}</span>
                      <span className="text-[12px] font-semibold text-slate-400">{h.doctor}</span>
                    </div>
                    <p className="text-[13px] text-slate-600 font-medium mt-1">{h.notes}</p>
                  </div>
                </div>
              ))}
            </div>
          </section>

            </div>{/* end left col */}

            {/* ── RIGHT: patient profile ── */}
            <aside className="flex flex-col gap-4">
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">

                {/* Avatar + name + code */}
                <div className="flex flex-col items-center gap-3 px-6 pt-6 pb-5 border-b border-slate-100 bg-gradient-to-b from-slate-50 to-white">
                  <div className={`w-20 h-20 rounded-2xl flex items-center justify-center font-black text-2xl border-2 shadow-sm ${patient.gender === "Nữ" ? "bg-rose-50 text-rose-500 border-rose-100" : "bg-sky-50 text-sky-600 border-sky-100"}`}>
                    {initials}
                  </div>
                  <div className="flex flex-col items-center gap-1">
                    <span className="text-[16px] font-black text-slate-900 text-center leading-tight">{patient.name}</span>
                    <span className="text-[12px] font-semibold text-slate-400">{patient.age} tuổi · {patient.gender}</span>
                    <span className="mt-1 px-3 py-1 bg-sky-50 border border-sky-100 text-sky-700 text-[11.5px] font-black rounded-full tracking-wide">
                      # {id}
                    </span>
                  </div>
                </div>

                {/* Basic info */}
                <div className="px-5 py-4 flex flex-col gap-2.5 overflow-x-hidden">
                  {([
                    ["Ngày sinh",  patient.dob],
                    ["Giới tính",  patient.gender],
                    ["Nhóm máu",   patient.bloodType],
                    ["Điện thoại", patient.phone],
                    ["Email",      patient.email],
                    ["Địa chỉ",    patient.address],
                  ] as [string, string][]).map(([k, v]) => (
                    <div key={k} className="flex items-start gap-2">
                      <span className="text-[11.5px] font-bold text-slate-400 w-20 shrink-0 mt-0.5">{k}</span>
                      <span className="text-[12.5px] font-semibold text-slate-800 leading-snug min-w-0 break-words">{v}</span>
                    </div>
                  ))}
                </div>

                {/* Allergies */}
                <div className="border-t border-slate-100 px-5 py-3 flex flex-col gap-2">
                  <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider">Dị ứng</span>
                  {patient.allergies.length > 0
                    ? patient.allergies.map((a) => (
                        <span key={a} className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-red-50 border border-red-100 text-red-700 text-[12px] font-bold rounded-lg w-fit">
                          <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
                          {a}
                        </span>
                      ))
                    : <span className="text-[12px] text-slate-400 font-semibold">Không có</span>}
                </div>

                {/* Conditions */}
                <div className="border-t border-slate-100 px-5 py-3 flex flex-col gap-2">
                  <span className="text-[10.5px] font-extrabold text-slate-400 uppercase tracking-wider">Bệnh nền</span>
                  {patient.conditions.length > 0
                    ? patient.conditions.map((c) => <span key={c} className="text-[12.5px] font-semibold text-slate-700">{c}</span>)
                    : <span className="text-[12px] text-slate-400 font-semibold">Không có</span>}
                </div>
              </div>

              {/* Visit history */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
                  <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <span className="text-[13px] font-black text-slate-900">Lịch sử khám</span>
                </div>
                <div className="divide-y divide-slate-100">
                  {patient.visitHistory.map((v, i) => (
                    <div key={i} className="px-5 py-3.5 flex items-start gap-3">
                      <div className="w-7 h-7 rounded-lg bg-red-50 text-primary flex items-center justify-center shrink-0 mt-0.5">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                        </svg>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between gap-1 flex-wrap">
                          <span className="text-[11.5px] font-mono font-bold text-primary">{v.date}</span>
                          <span className="text-[11px] font-semibold text-slate-400 truncate">{v.doctor.replace("Bs. ", "")}</span>
                        </div>
                        <span className="text-[12.5px] font-semibold text-slate-700 leading-snug">{v.reason}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </aside>

          </div>{/* end 2-col wrapper */}
        </div>
      </main>
    </div>
  );
}
