"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import DentistSidebar from "../../../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../../../../hooks/useRequireDentist";

const COMMON_MEDS = [
  "Amoxicillin 500mg","Amoxicillin + Clavulanate 875mg","Metronidazole 250mg",
  "Ibuprofen 400mg","Paracetamol 500mg","Diclofenac 50mg",
  "Chlorhexidine 0.12%","Povidone-Iodine 10%","Nước muối sinh lý 0.9%",
  "Prednisolone 5mg","Dexamethasone 0.5mg","Tramadol 50mg","Khác",
];
const DOSAGES     = ["1/2 viên","1 viên","2 viên","3 viên","5ml","10ml","15ml","1 gói","1 ống","2 lần súc miệng"];
const FREQUENCIES = ["1 lần/ngày","2 lần/ngày","3 lần/ngày","4 lần/ngày","Khi đau","Trước ăn","Sau ăn","Sáng & tối"];
const DURATIONS   = ["3 ngày","5 ngày","7 ngày","10 ngày","14 ngày","1 tháng"];
const QUICK_NOTES = ["Uống sau ăn","Uống trước ăn 30 phút","Uống với nhiều nước","Súc miệng 30 giây rồi nhổ","Bôi vào vùng điều trị","Không lái xe sau khi uống","Tránh tiếp xúc ánh nắng"];

interface MedItem { id: string; name: string; dosage: string; frequency: string; duration: string; note: string }

const BLANK = { name: COMMON_MEDS[0], dosage: DOSAGES[1], frequency: FREQUENCIES[2], duration: DURATIONS[2], note: "" };

export default function NewPrescriptionPage() {
  useRequireDentist();
  const { id }  = useParams<{ id: string }>();
  const router  = useRouter();

  const [items,      setItems]  = useState<MedItem[]>([]);
  const [form,       setForm]   = useState(BLANK);
  const [customName, setCustom] = useState("");
  const [saving,     setSaving] = useState(false);

  const canAdd = form.name !== "Khác" || customName.trim() !== "";

  const addItem = () => {
    if (!canAdd) return;
    const name = form.name === "Khác" ? customName.trim() : form.name;
    setItems(p => [...p, { id: String(Date.now()), name, dosage: form.dosage, frequency: form.frequency, duration: form.duration, note: form.note }]);
    setForm(BLANK); setCustom("");
  };

  const sel = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none pr-8 cursor-pointer";
  const inp = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";

  const chevron = (
    <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
    </span>
  );

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />
      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Lập đơn thuốc"
          subtitle={`Bệnh nhân #${id}`}
          left={
            <Link href={`/dentist/patients/${id}`} className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-700 transition-all shrink-0">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
            </Link>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* Top: form + queue */}
          <div className="grid gap-6" style={{ gridTemplateColumns: "1fr 320px" }}>

            {/* LEFT — form */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
                <div className="w-7 h-7 rounded-lg bg-violet-50 text-violet-600 flex items-center justify-center shrink-0">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                </div>
                <span className="text-[14px] font-black text-slate-900">Thêm thuốc vào đơn</span>
                <span className="text-[12px] text-slate-400 font-semibold">Chọn thuốc → liều lượng → thêm vào đơn</span>
              </div>

              <div className="p-6 flex flex-col gap-4">
                {/* Tên thuốc */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Tên thuốc</label>
                  <div className="relative"><select value={form.name} onChange={e => setForm(f => ({...f, name: e.target.value}))} className={sel}>{COMMON_MEDS.map(m => <option key={m} value={m}>{m}</option>)}</select>{chevron}</div>
                  {form.name === "Khác" && (
                    <input value={customName} onChange={e => setCustom(e.target.value)}
                      placeholder="Nhập tên thuốc..." className={inp + " mt-1"} />
                  )}
                </div>

                {/* Liều + Tần suất + Thời gian */}
                <div className="grid grid-cols-3 gap-3">
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Liều dùng</label>
                    <div className="relative"><select value={form.dosage} onChange={e => setForm(f => ({...f, dosage: e.target.value}))} className={sel}>{DOSAGES.map(d => <option key={d} value={d}>{d}</option>)}</select>{chevron}</div>
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Tần suất</label>
                    <div className="relative"><select value={form.frequency} onChange={e => setForm(f => ({...f, frequency: e.target.value}))} className={sel}>{FREQUENCIES.map(f => <option key={f} value={f}>{f}</option>)}</select>{chevron}</div>
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Thời gian</label>
                    <div className="relative"><select value={form.duration} onChange={e => setForm(f => ({...f, duration: e.target.value}))} className={sel}>{DURATIONS.map(d => <option key={d} value={d}>{d}</option>)}</select>{chevron}</div>
                  </div>
                </div>

                {/* Hướng dẫn — field nhập tự do duy nhất */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Hướng dẫn sử dụng</label>
                  <div className="flex flex-wrap gap-1.5 mb-1">
                    {QUICK_NOTES.map(hint => (
                      <button key={hint} type="button"
                        onClick={() => setForm(f => ({ ...f, note: f.note ? f.note + ". " + hint : hint }))}
                        className="px-2.5 py-1 text-[11px] font-semibold bg-slate-100 text-slate-500 rounded-lg hover:bg-violet-100 hover:text-violet-700 border border-transparent hover:border-violet-200 transition-all cursor-pointer">
                        + {hint}
                      </button>
                    ))}
                  </div>
                  <textarea value={form.note} onChange={e => setForm(f => ({...f, note: e.target.value}))}
                    rows={2} placeholder="Hoặc nhập hướng dẫn tùy chỉnh..."
                    className={inp + " resize-none"} />
                </div>

                {/* Inline preview */}
                {canAdd && (
                  <div className="bg-violet-50 border border-violet-100 rounded-xl px-4 py-2.5 text-[12.5px] text-violet-800 font-semibold">
                    <span className="font-black">{form.name === "Khác" ? (customName || "Thuốc") : form.name}</span>
                    {" — "}{form.dosage} · {form.frequency} · {form.duration}
                    {form.note && <span className="text-violet-500"> · {form.note}</span>}
                  </div>
                )}

                <button onClick={addItem} disabled={!canAdd}
                  className="flex items-center justify-center gap-2 py-2.5 bg-violet-600 text-white font-black text-[14px] rounded-xl hover:bg-violet-700 transition-all shadow-sm shadow-violet-500/25 disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                  Thêm vào đơn
                </button>
              </div>
            </div>

            {/* RIGHT — Queue */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
              <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between shrink-0">
                <span className="text-[14px] font-black text-slate-900">Đơn thuốc</span>
                <span className={`text-[12px] font-bold px-2.5 py-1 rounded-lg ${items.length > 0 ? "bg-violet-100 text-violet-700" : "bg-slate-100 text-slate-400"}`}>
                  {items.length} thuốc
                </span>
              </div>

              {items.length === 0 ? (
                <div className="flex-1 flex flex-col items-center justify-center gap-2 py-12 text-center px-4">
                  <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center">
                    <svg className="w-6 h-6 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5" /></svg>
                  </div>
                  <p className="text-[12.5px] font-semibold text-slate-400">Đơn thuốc trống.<br />Chọn thuốc bên trái để thêm.</p>
                </div>
              ) : (
                <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
                  {items.map((item, i) => (
                    <div key={item.id} className="px-5 py-3.5 flex items-start gap-3">
                      <div className="w-6 h-6 rounded-full bg-violet-100 text-violet-700 flex items-center justify-center text-[11px] font-black shrink-0 mt-0.5">{i + 1}</div>
                      <div className="flex-1 min-w-0">
                        <div className="text-[13px] font-black text-slate-900 leading-snug">{item.name}</div>
                        <div className="text-[12px] font-semibold text-slate-500">{item.dosage} · {item.frequency} · {item.duration}</div>
                        {item.note && <div className="text-[11.5px] text-slate-400 mt-0.5 line-clamp-1">{item.note}</div>}
                      </div>
                      <button onClick={() => setItems(p => p.filter(x => x.id !== item.id))}
                        className="p-1 rounded-lg hover:bg-red-50 text-slate-300 hover:text-primary transition-all cursor-pointer shrink-0 mt-0.5">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* PREVIEW */}
          {items.length > 0 && (
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-3.5 bg-gradient-to-r from-violet-50 to-purple-50/60 border-b border-violet-100 flex items-center gap-2.5">
                <svg style={{width:18,height:18}} className="shrink-0 text-violet-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <span className="text-[13.5px] font-black text-violet-700">Xem trước đơn thuốc</span>
                <span className="text-[12px] text-violet-400 font-semibold">{items.length} loại thuốc</span>
              </div>
              <table className="w-full text-[13px]">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-100 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">
                    <th className="px-5 py-3 text-left w-8">#</th>
                    <th className="px-5 py-3 text-left">Tên thuốc</th>
                    <th className="px-5 py-3 text-left">Liều dùng</th>
                    <th className="px-5 py-3 text-left">Tần suất</th>
                    <th className="px-5 py-3 text-left">Thời gian</th>
                    <th className="px-5 py-3 text-left">Hướng dẫn</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {items.map((item, i) => (
                    <tr key={item.id} className="hover:bg-slate-50/50">
                      <td className="px-5 py-3.5 text-slate-400 font-bold">{i + 1}</td>
                      <td className="px-5 py-3.5 font-black text-slate-900">{item.name}</td>
                      <td className="px-5 py-3.5 font-semibold text-slate-700">{item.dosage}</td>
                      <td className="px-5 py-3.5 font-semibold text-slate-700">{item.frequency}</td>
                      <td className="px-5 py-3.5 font-semibold text-slate-700">{item.duration}</td>
                      <td className="px-5 py-3.5 text-slate-500 font-medium max-w-[200px] truncate">{item.note || "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3 justify-end">
            <Link href={`/dentist/patients/${id}`}
              className="px-6 py-3 text-[14px] font-bold text-slate-500 border border-slate-200 rounded-xl hover:bg-slate-50 transition-all">
              Hủy
            </Link>
            <button
              onClick={() => { if (items.length === 0) return; setSaving(true); setTimeout(() => router.push(`/dentist/patients/${id}`), 800); }}
              disabled={items.length === 0 || saving}
              className="flex items-center gap-2 px-6 py-3 bg-violet-600 text-white text-[14px] font-black rounded-xl hover:bg-violet-700 transition-all shadow-sm shadow-violet-500/25 disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed">
              {saving
                ? <><svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>Đang lưu...</>
                : <><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>Xác nhận & Lưu đơn thuốc ({items.length} thuốc)</>
              }
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}
