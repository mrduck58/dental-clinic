"use client";

import { useState, Suspense, useEffect } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import DentistSidebar from "../../../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../../../components/shared/DentistPageHeader";
import ToothArchDiagram, { UPPER_TEETH, LOWER_TEETH } from "../../../../../../components/shared/ToothArchDiagram";
import { useRequireDentist } from "../../../../../../hooks/useRequireDentist";
import { createTreatmentPlanApi, createTreatmentCourseApi, getServicesApi, type ServiceDto } from "../../../../../../lib/apiClient";

const ALL_STR = [...UPPER_TEETH, ...LOWER_TEETH];

interface StepItem { 
  teeth: string[]; 
  serviceId: string; 
  serviceName: string; 
  servicePrice: number; 
  note: string 
}

function NewTreatmentPageContent() {
  useRequireDentist();
  const { id }  = useParams<{ id: string }>();
  const router  = useRouter();
  const params  = useSearchParams();

  // Chế độ tạo liệu trình dài hạn (?mode=course): chọn dịch vụ để tính tổng, rồi tạo liệu trình.
  const isCourse = params.get("mode") === "course";

  const initTeeth = params.get("teeth")?.split(",").map(t => t.trim()).filter(Boolean) ?? [];
  const [sel, setSel] = useState<Set<string>>(new Set(initTeeth));
  const [items, setItems] = useState<StepItem[]>([]);
  const [materials, setMaterials] = useState("");
  const [form, setForm] = useState({ serviceId: "", serviceName: "", servicePrice: 0, note: "" });

  // Tên liệu trình dài hạn lấy tự động từ các dịch vụ đã chọn.
  const courseDisplayName = [...new Set(items.map(i => i.serviceName))].join(", ");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchService, setSearchService] = useState("");
  
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [loadingServices, setLoadingServices] = useState(true);

  useEffect(() => {
    const loadServices = async () => {
      try {
        const data = await getServicesApi({ status: "Active" });
        setServices(data);
      } catch {
        // Silently fail
      } finally {
        setLoadingServices(false);
      }
    };
    void loadServices();
  }, []);

  const canAdd = sel.size > 0 && form.serviceId !== "";
  const totalCost = items.reduce((s, i) => s + i.servicePrice, 0);

  const toggle = (t: string) => setSel(prev => {
    const n = new Set(prev); n.has(t) ? n.delete(t) : n.add(t); return n;
  });

  const handleServiceChange = (serviceId: string) => {
    const service = services.find(s => s.id === serviceId);
    if (service) {
      setForm({ serviceId: service.id, serviceName: service.name, servicePrice: service.price, note: "" });
    }
  };

  const filteredServices = services.filter(s => 
    s.name.toLowerCase().includes(searchService.toLowerCase())
  );

  const addItem = () => {
    if (!canAdd || !form.serviceId) return;
    const teeth = [...sel].sort((a, b) => Number(a) - Number(b));
    setItems(p => [...p, { teeth, serviceId: form.serviceId, serviceName: form.serviceName, servicePrice: form.servicePrice, note: form.note }]);
    setSel(new Set()); 
    setForm({ serviceId: "", serviceName: "", servicePrice: 0, note: "" });
    setSearchService("");
  };

  const handleSave = async () => {
    if (items.length === 0) return;
    setSaving(true);
    setError(null);

    try {
      // Ghi nhận các dịch vụ đã chọn (hồ sơ chuyên môn của buổi).
      for (const item of items) {
        const note = item.note ? ` (${item.note})` : "";
        await createTreatmentPlanApi(id, {
          description: `${item.serviceName} - Răng ${item.teeth.join(", ")}${note}`,
          estimatedCost: item.servicePrice,
        });
      }

      // Chế độ dài hạn: tên liệu trình lấy từ dịch vụ, tổng = tổng dịch vụ đã chọn,
      // kèm vật liệu cần thiết gửi sang kho.
      if (isCourse) {
        await createTreatmentCourseApi(id, courseDisplayName, totalCost, materials.trim() || undefined);
      }

      router.push(`/dentist/patients/${id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lưu liệu trình thất bại");
      setSaving(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />
      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title={isCourse ? "Lập liệu trình dài hạn" : "Lập liệu trình điều trị"}
          subtitle={isCourse ? "Chọn dịch vụ để chốt tổng chi phí — bệnh nhân trả thành nhiều đợt" : `Bệnh nhân #${id}`}
          left={
            <Link href={`/dentist/patients/${id}`} className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-700 transition-all shrink-0">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
            </Link>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {error && (
            <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-[13px] text-red-700 font-semibold">
              {error}
            </div>
          )}

          <div className="grid gap-6" style={{ gridTemplateColumns: "1fr 340px" }}>

            {/* LEFT */}
            <div className="flex flex-col gap-4">

              {/* Tooth Selection Card */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-3">
                  <div className="w-8 h-8 rounded-xl bg-sky-50 text-sky-600 flex items-center justify-center">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                    </svg>
                  </div>
                  <span className="text-[14px] font-black text-slate-900">Chọn vị trí răng</span>
                  {sel.size > 0 && (
                    <span className="ml-auto px-2.5 py-1 bg-primary/10 text-primary text-[11px] font-bold rounded-lg">
                      {sel.size} răng đã chọn
                    </span>
                  )}
                </div>
                <div className="p-5">
                  <div className="flex gap-2 mb-4">
                    <button type="button" onClick={() => setSel(new Set(UPPER_TEETH))} className="px-3 py-1.5 text-[12px] font-bold rounded-lg border border-sky-200 text-sky-600 hover:bg-sky-50 cursor-pointer">Hàm trên</button>
                    <button type="button" onClick={() => setSel(new Set(LOWER_TEETH))} className="px-3 py-1.5 text-[12px] font-bold rounded-lg border border-sky-200 text-sky-600 hover:bg-sky-50 cursor-pointer">Hàm dưới</button>
                    <button type="button" onClick={() => setSel(new Set(ALL_STR))} className="px-3 py-1.5 text-[12px] font-bold rounded-lg border border-violet-200 text-violet-600 hover:bg-violet-50 cursor-pointer">Tất cả</button>
                    <button type="button" onClick={() => setSel(new Set())} className="px-3 py-1.5 text-[12px] font-bold rounded-lg border border-slate-200 text-slate-400 hover:bg-slate-50 cursor-pointer">Xóa</button>
                  </div>
                  <div className="bg-slate-50 rounded-xl p-4 border border-slate-200">
                    <ToothArchDiagram selected={sel} onToothClick={toggle} showLegend={false} />
                  </div>
                  {sel.size > 0 && (
                    <div className="flex flex-wrap gap-1.5 mt-3">
                      {[...sel].sort((a, b) => Number(a) - Number(b)).map(t => (
                        <button key={t} type="button" onClick={() => toggle(t)}
                          className="flex items-center gap-1 px-2.5 py-1 bg-primary text-white text-[12px] font-bold rounded-lg cursor-pointer hover:bg-red-600 transition-all">
                          Răng {t}
                          <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              {/* Service Selection Card */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-3">
                  <div className="w-8 h-8 rounded-xl bg-primary/10 text-primary flex items-center justify-center">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5" />
                    </svg>
                  </div>
                  <span className="text-[14px] font-black text-slate-900">Chọn dịch vụ</span>
                </div>
                <div className="p-5 flex flex-col gap-4">
                  
                  {/* Service Search */}
                  <div className="relative">
                    <input
                      type="text"
                      value={searchService}
                      onChange={e => setSearchService(e.target.value)}
                      placeholder="Tìm kiếm dịch vụ..."
                      className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400"
                    />
                    <svg className="w-4 h-4 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                  </div>

                  {/* Service List */}
                  {loadingServices ? (
                    <div className="flex items-center justify-center py-8">
                      <svg className="w-6 h-6 animate-spin text-primary" fill="none" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                      </svg>
                    </div>
                  ) : (
                    <div className="max-h-64 overflow-y-auto border border-slate-200 rounded-xl divide-y divide-slate-100">
                      {filteredServices.length === 0 ? (
                        <div className="py-6 text-center text-[13px] text-slate-400">Không tìm thấy dịch vụ</div>
                      ) : (
                        filteredServices.map(s => (
                          <button
                            key={s.id}
                            type="button"
                            onClick={() => handleServiceChange(s.id)}
                            className={`w-full px-4 py-3 flex items-center justify-between text-left hover:bg-slate-50 transition-all cursor-pointer ${form.serviceId === s.id ? "bg-primary/5" : ""}`}
                          >
                            <div>
                              <div className="text-[13px] font-bold text-slate-900">{s.name}</div>
                              <div className="text-[11px] text-slate-500 mt-0.5">{s.durationMinutes} phút</div>
                            </div>
                            <div className={`text-[14px] font-black ${form.serviceId === s.id ? "text-primary" : "text-slate-700"}`}>
                              {s.price.toLocaleString("vi-VN")}đ
                            </div>
                          </button>
                        ))
                      )}
                    </div>
                  )}

                  {/* Selected Service Info */}
                  {form.serviceId && (
                    <div className="bg-green-50 border border-green-200 rounded-xl p-4">
                      <div className="flex items-center justify-between">
                        <div>
                          <div className="text-[11px] font-bold text-green-600 uppercase">Đã chọn</div>
                          <div className="text-[14px] font-black text-slate-900 mt-0.5">{form.serviceName}</div>
                        </div>
                        <div className="text-[18px] font-black text-green-700">{form.servicePrice.toLocaleString("vi-VN")}đ</div>
                      </div>
                      <textarea
                        value={form.note}
                        onChange={e => setForm(f => ({ ...f, note: e.target.value }))}
                        placeholder="Ghi chú (vật liệu, lưu ý...)"
                        rows={2}
                        className="w-full mt-3 px-3 py-2 text-[12px] bg-white border border-green-200 rounded-lg focus:border-green-400 focus:ring-1 focus:ring-green-400 focus:outline-none resize-none font-medium"
                      />
                    </div>
                  )}

                  <button
                    onClick={addItem}
                    disabled={!canAdd}
                    className="flex items-center justify-center gap-2 py-3 bg-primary text-white font-black text-[14px] rounded-xl hover:bg-red-600 transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                    </svg>
                    Thêm dịch vụ{canAdd ? ` (${sel.size} răng)` : ""}
                  </button>
                </div>
              </div>
            </div>

            {/* RIGHT - Services List */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col h-fit sticky top-8">
              <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between">
                <span className="text-[14px] font-black text-slate-900">Danh sách dịch vụ</span>
                <span className={`text-[12px] font-bold px-2.5 py-1 rounded-lg ${items.length > 0 ? "bg-primary/10 text-primary" : "bg-slate-100 text-slate-400"}`}>
                  {items.length} dịch vụ
                </span>
              </div>

              {items.length === 0 ? (
                <div className="flex-1 flex flex-col items-center justify-center gap-3 py-12 text-center px-4">
                  <div className="w-14 h-14 rounded-2xl bg-slate-100 flex items-center justify-center">
                    <svg className="w-7 h-7 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" />
                    </svg>
                  </div>
                  <p className="text-[13px] font-semibold text-slate-400">Chưa có dịch vụ nào</p>
                  <p className="text-[11px] text-slate-400">Chọn răng và dịch vụ để thêm</p>
                </div>
              ) : (
                <div className="divide-y divide-slate-100 max-h-[400px] overflow-y-auto">
                  {items.map((item, i) => (
                    <div key={i} className="px-5 py-4">
                      <div className="flex items-start gap-3">
                        <div className="w-7 h-7 rounded-lg bg-primary/10 text-primary flex items-center justify-center text-[12px] font-black shrink-0">{i + 1}</div>
                        <div className="flex-1 min-w-0">
                          <div className="text-[13px] font-bold text-slate-900">{item.serviceName}</div>
                          <div className="text-[11px] font-medium text-slate-500 mt-0.5">Răng {item.teeth.join(", ")}</div>
                          {item.note && <div className="text-[11px] text-slate-400 mt-1 italic">{item.note}</div>}
                          <div className="text-[13px] font-black text-primary mt-1">{item.servicePrice.toLocaleString("vi-VN")}đ</div>
                        </div>
                        <button onClick={() => setItems(p => p.filter((_, idx) => idx !== i))}
                          className="p-1.5 rounded-lg hover:bg-red-50 text-slate-300 hover:text-red-500 transition-all cursor-pointer">
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {items.length > 0 && (
                <div className="px-5 py-4 border-t border-slate-100 bg-slate-50">
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-[12px] font-bold text-slate-500">Tổng chi phí dự kiến</span>
                    <span className="text-[18px] font-black text-primary">{totalCost.toLocaleString("vi-VN")}đ</span>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Preview & Actions */}
          {items.length > 0 && (
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-4 bg-gradient-to-r from-primary/5 to-red-50/50 border-b border-slate-100 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <svg className="w-5 h-5 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  <span className="text-[14px] font-black text-slate-900">Xem trước liệu trình</span>
                </div>
                <div className="flex items-center gap-4">
                  <span className="text-[13px] text-slate-500">{items.length} dịch vụ</span>
                  <span className="text-[15px] font-black text-primary">{totalCost.toLocaleString("vi-VN")}đ</span>
                </div>
              </div>
              <div className="p-4 flex flex-wrap gap-2">
                {items.map((item, i) => (
                  <div key={i} className="px-3 py-2 bg-slate-50 border border-slate-200 rounded-lg text-[12px]">
                    <span className="font-bold text-slate-700">{i + 1}. {item.serviceName}</span>
                    <span className="text-slate-400 ml-2">Răng {item.teeth.join(", ")}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Vật liệu cần thiết (chỉ ở chế độ liệu trình dài hạn) */}
          {isCourse && (
            <div className="bg-white rounded-2xl border border-indigo-200 shadow-sm p-5 flex flex-col gap-2">
              <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Vật liệu cần thiết</label>
              <textarea
                value={materials}
                onChange={e => setMaterials(e.target.value)}
                rows={3}
                placeholder="VD: Mắc cài kim loại 20 chiếc, dây cung 0.014 x4, thun chuỗi..."
                className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none font-semibold text-slate-700 resize-none"
              />
              <p className="text-[12px] font-semibold text-slate-400">
                Danh sách vật liệu sẽ được gửi sang trang <strong>Nhập–xuất vật tư</strong> của staff để nhập kho. Tên liệu trình: <span className="text-indigo-700 font-black">{courseDisplayName || "—"}</span> · Tổng: <span className="text-indigo-700 font-black">{totalCost.toLocaleString("vi-VN")}đ</span>.
              </p>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3 justify-end">
            <Link href={`/dentist/patients/${id}`}
              className="px-6 py-3 text-[14px] font-bold text-slate-500 border border-slate-200 rounded-xl hover:bg-slate-50 transition-all">
              Hủy
            </Link>
            <button
              onClick={handleSave}
              disabled={items.length === 0 || saving}
              className="flex items-center gap-2 px-8 py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 transition-all shadow-sm disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
            >
              {saving ? (
                <>
                  <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                  </svg>
                  Đang lưu...
                </>
              ) : (
                <>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                  </svg>
                  {isCourse ? "Tạo liệu trình dài hạn" : "Lưu liệu trình"}
                </>
              )}
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
