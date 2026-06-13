"use client";

import { useState, Suspense } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import DentistSidebar from "../../../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../../../components/shared/DentistPageHeader";
import ToothArchDiagram, { UPPER_TEETH, LOWER_TEETH } from "../../../../../../components/shared/ToothArchDiagram";
import { useRequireDentist } from "../../../../../../hooks/useRequireDentist";

const ALL_STR = [...UPPER_TEETH, ...LOWER_TEETH];

const PROCEDURES = [
  "Trám răng composite","Điều trị tủy","Nhổ răng","Lấy cao răng siêu âm",
  "Bọc sứ","Cấy ghép Implant","Tẩy trắng răng","Niềng răng",
  "Nhổ răng khôn","Điều trị nha chu","Phục hình tháo lắp","Khác",
];

interface StepItem { id: string; teeth: string[]; procedure: string; cost: string; note: string }
const BLANK = { procedure: PROCEDURES[0], cost: "", note: "" };

function NewTreatmentPageContent() {
  useRequireDentist();
  const { id }  = useParams<{ id: string }>();
  const router  = useRouter();
  const params  = useSearchParams();

  const initTeeth = params.get("teeth")?.split(",").map(t => t.trim()).filter(Boolean) ?? [];
  const [sel,    setSel]    = useState<Set<string>>(new Set(initTeeth));
  const [items,  setItems]  = useState<StepItem[]>([]);
  const [form,   setForm]   = useState(BLANK);
  const [custom, setCustom] = useState("");
  const [saving, setSaving] = useState(false);

  const canAdd    = sel.size > 0;
  const totalCost = items.reduce((s, i) => s + (Number(i.cost) || 0), 0);

  const toggle = (t: string) => setSel(prev => {
    const n = new Set(prev); n.has(t) ? n.delete(t) : n.add(t); return n;
  });

  const addItem = () => {
    if (!canAdd) return;
    const proc  = form.procedure === "Khác" ? (custom.trim() || "Thủ thuật khác") : form.procedure;
    const teeth = [...sel].sort((a, b) => Number(a) - Number(b));
    setItems(p => [...p, { id: String(Date.now()), teeth, procedure: proc, cost: form.cost, note: form.note }]);
    setSel(new Set()); setForm(BLANK); setCustom("");
  };

  const inp   = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";
  const mini  = "px-2.5 py-1 text-[11px] font-bold rounded-lg border transition-all cursor-pointer";

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />
      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Lập liệu trình điều trị"
          subtitle={`Bệnh nhân #${id}`}
          left={
            <Link href={`/dentist/patients/${id}`} className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-700 transition-all shrink-0">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
            </Link>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          <div className="grid gap-6" style={{ gridTemplateColumns: "1fr 320px" }}>

            {/* LEFT — form */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
                <div className="w-7 h-7 rounded-lg bg-red-50 text-primary flex items-center justify-center shrink-0">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                </div>
                <span className="text-[14px] font-black text-slate-900">Thêm bước điều trị</span>
                <span className="text-[12px] text-slate-400 font-semibold">Chọn răng → chọn thủ thuật → thêm</span>
              </div>

              <div className="p-6 flex flex-col gap-5">

                {/* ── Tooth diagram ── */}
                <div className="flex flex-col gap-2">
                  <div className="flex items-center justify-between">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">
                      Vị trí răng <span className="text-primary">*</span>
                    </label>
                    <div className="flex gap-1.5">
                      <button type="button" onClick={() => setSel(new Set(UPPER_TEETH))}
                        className={mini + " border-sky-200 text-sky-600 hover:bg-sky-50"}>Hàm trên</button>
                      <button type="button" onClick={() => setSel(new Set(LOWER_TEETH))}
                        className={mini + " border-sky-200 text-sky-600 hover:bg-sky-50"}>Hàm dưới</button>
                      <button type="button" onClick={() => setSel(new Set(ALL_STR))}
                        className={mini + " border-violet-200 text-violet-600 hover:bg-violet-50"}>Tất cả</button>
                      <button type="button" onClick={() => setSel(new Set())}
                        className={mini + " border-slate-200 text-slate-400 hover:bg-slate-50"}>Xóa hết</button>
                    </div>
                  </div>

                  <div className="bg-slate-50/80 border border-slate-200 rounded-xl px-4 py-4">
                    <ToothArchDiagram
                      selected={sel}
                      onToothClick={toggle}
                      showLegend={false}
                    />
                  </div>

                  {sel.size > 0 ? (
                    <div className="flex flex-wrap gap-1.5">
                      {[...sel].sort((a, b) => Number(a) - Number(b)).map(t => (
                        <button key={t} type="button" onClick={() => toggle(t)}
                          className="flex items-center gap-1 px-2 py-0.5 bg-primary/10 text-primary text-[12px] font-bold rounded-lg hover:bg-primary hover:text-white transition-all cursor-pointer">
                          {t}
                          <svg className="w-2.5 h-2.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                        </button>
                      ))}
                    </div>
                  ) : (
                    <p className="text-[12px] text-slate-400 font-semibold">Nhấn vào răng để chọn, hoặc dùng nút phía trên</p>
                  )}
                </div>

                {/* Procedure + cost */}
                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Thủ thuật</label>
                    <div className="relative">
                      <select value={form.procedure} onChange={e => setForm(f => ({...f, procedure: e.target.value}))}
                        className={inp + " appearance-none pr-8 cursor-pointer"}>
                        {PROCEDURES.map(p => <option key={p} value={p}>{p}</option>)}
                      </select>
                      <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                      </span>
                    </div>
                    {form.procedure === "Khác" && (
                      <input value={custom} onChange={e => setCustom(e.target.value)}
                        placeholder="Nhập tên thủ thuật..." className={inp + " mt-1"} />
                    )}
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Chi phí dự kiến (VNĐ)</label>
                    <input type="number" value={form.cost} onChange={e => setForm(f => ({...f, cost: e.target.value}))}
                      placeholder="0" min="0" step="50000" className={inp} />
                    {form.cost && <span className="text-[12px] font-bold text-primary">{Number(form.cost).toLocaleString("vi-VN")}đ</span>}
                  </div>
                </div>

                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                  <textarea value={form.note} onChange={e => setForm(f => ({...f, note: e.target.value}))}
                    rows={2} placeholder="Vật liệu, lưu ý đặc biệt..." className={inp + " resize-none"} />
                </div>

                <button onClick={addItem} disabled={!canAdd}
                  className="flex items-center justify-center gap-2 py-2.5 bg-primary text-white font-black text-[14px] rounded-xl hover:bg-red-600 transition-all shadow-sm shadow-primary/25 disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                  Thêm vào danh sách{canAdd ? ` (${sel.size} răng)` : ""}
                </button>
              </div>
            </div>

            {/* RIGHT — Queue */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
              <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between shrink-0">
                <span className="text-[14px] font-black text-slate-900">Danh sách bước</span>
                <span className={`text-[12px] font-bold px-2.5 py-1 rounded-lg ${items.length > 0 ? "bg-primary/10 text-primary" : "bg-slate-100 text-slate-400"}`}>
                  {items.length} bước
                </span>
              </div>

              {items.length === 0 ? (
                <div className="flex-1 flex flex-col items-center justify-center gap-2 py-12 text-center px-4">
                  <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center">
                    <svg className="w-6 h-6 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" /></svg>
                  </div>
                  <p className="text-[12.5px] font-semibold text-slate-400">Chưa có bước nào.<br />Chọn răng và thủ thuật để thêm.</p>
                </div>
              ) : (
                <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
                  {items.map((item, i) => (
                    <div key={item.id} className="px-5 py-3.5 flex items-start gap-3">
                      <div className="w-6 h-6 rounded-full bg-primary/10 text-primary flex items-center justify-center text-[11px] font-black shrink-0 mt-0.5">{i + 1}</div>
                      <div className="flex-1 min-w-0">
                        <div className="text-[13px] font-black text-slate-900 leading-snug">{item.procedure}</div>
                        <div className="text-[12px] font-semibold text-slate-500">Răng {item.teeth.join(", ")}</div>
                        {item.cost && <div className="text-[12px] font-black text-primary mt-0.5">{Number(item.cost).toLocaleString("vi-VN")}đ</div>}
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

              {items.length > 0 && (
                <div className="px-5 py-3.5 border-t border-slate-100 bg-slate-50/70 flex items-center justify-between shrink-0">
                  <span className="text-[12.5px] font-bold text-slate-500">Tổng chi phí</span>
                  <span className="text-[15px] font-black text-primary">{totalCost.toLocaleString("vi-VN")}đ</span>
                </div>
              )}
            </div>
          </div>

          {/* PREVIEW */}
          {items.length > 0 && (
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-3.5 bg-gradient-to-r from-red-50 to-rose-50/60 border-b border-red-100 flex items-center gap-2.5">
                <svg style={{width:18,height:18}} className="shrink-0 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <span className="text-[13.5px] font-black text-primary">Xem trước liệu trình</span>
                <span className="text-[12px] text-red-400 font-semibold">{items.length} bước · Tổng {totalCost.toLocaleString("vi-VN")}đ</span>
              </div>
              <table className="w-full text-[13px]">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-100 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">
                    <th className="px-5 py-3 text-left w-8">#</th>
                    <th className="px-5 py-3 text-left">Thủ thuật</th>
                    <th className="px-5 py-3 text-left">Vị trí</th>
                    <th className="px-5 py-3 text-left">Ghi chú</th>
                    <th className="px-5 py-3 text-right">Chi phí</th>
                    <th className="px-5 py-3 text-left">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {items.map((item, i) => (
                    <tr key={item.id} className="hover:bg-slate-50/50">
                      <td className="px-5 py-3.5 text-slate-400 font-bold">{i + 1}</td>
                      <td className="px-5 py-3.5 font-black text-slate-900">{item.procedure}</td>
                      <td className="px-5 py-3.5 font-semibold text-slate-600">Răng {item.teeth.join(", ")}</td>
                      <td className="px-5 py-3.5 text-slate-500 font-medium max-w-[180px] truncate">{item.note || "—"}</td>
                      <td className="px-5 py-3.5 text-right font-black text-slate-800">{item.cost ? Number(item.cost).toLocaleString("vi-VN") + "đ" : "—"}</td>
                      <td className="px-5 py-3.5"><span className="px-2.5 py-1 bg-slate-100 text-slate-500 text-[11px] font-black rounded-full">Chờ thực hiện</span></td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="bg-red-50/50 border-t-2 border-red-100">
                    <td colSpan={4} className="px-5 py-3 text-[12.5px] font-extrabold text-primary uppercase tracking-wider">Tổng cộng</td>
                    <td className="px-5 py-3 text-right text-[15px] font-black text-primary">{totalCost.toLocaleString("vi-VN")}đ</td>
                    <td />
                  </tr>
                </tfoot>
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
              className="flex items-center gap-2 px-6 py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 transition-all shadow-sm shadow-primary/25 disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed">
              {saving
                ? <><svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>Đang lưu...</>
                : <><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>Xác nhận & Lưu liệu trình ({items.length} bước)</>
              }
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}

export default function NewTreatmentPage() {
  return (
    <Suspense>
      <NewTreatmentPageContent />
    </Suspense>
  );
}
