"use client";

import { useState } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireOwner } from "../../../hooks/useRequireOwner";

interface StockItem { id: string; name: string; category: string; unit: string; qty: number; minQty: number }

const INITIAL_STOCK: StockItem[] = [
  { id: "VT001", name: "Găng tay latex (M)",        category: "Bảo hộ",   unit: "Hộp (100c)",   qty: 15, minQty: 5  },
  { id: "VT002", name: "Khẩu trang y tế",            category: "Bảo hộ",   unit: "Hộp (50c)",    qty: 8,  minQty: 5  },
  { id: "VT003", name: "Mũi khoan composite",        category: "Dụng cụ",  unit: "Cái",          qty: 45, minQty: 20 },
  { id: "VT004", name: "Composite A2",               category: "Vật liệu", unit: "Tuýp",         qty: 6,  minQty: 10 },
  { id: "VT005", name: "Composite A3",               category: "Vật liệu", unit: "Tuýp",         qty: 3,  minQty: 10 },
  { id: "VT006", name: "Nước súc miệng Listerine",   category: "Tiêu hao", unit: "Chai 500ml",   qty: 12, minQty: 6  },
  { id: "VT007", name: "Kim tiêm nha khoa",          category: "Dụng cụ",  unit: "Hộp (100c)",   qty: 4,  minQty: 5  },
  { id: "VT008", name: "Thuốc tê Lidocaine",         category: "Thuốc",    unit: "Hộp (50 ống)", qty: 7,  minQty: 4  },
  { id: "VT009", name: "Giấy cắn khớp",              category: "Tiêu hao", unit: "Cuộn",         qty: 20, minQty: 8  },
  { id: "VT010", name: "Bơm rửa nha khoa",           category: "Dụng cụ",  unit: "Cái",          qty: 30, minQty: 10 },
];

const HISTORY_LOG = [
  { id: "TX001", item: "Găng tay latex (M)",      type: "import", qty: 10, note: "Nhập từ Công ty Y tế Minh Phú",  date: "13/06/2026", by: "NV. Lan" },
  { id: "TX002", item: "Composite A2",             type: "export", qty: 2,  note: "Xuất cho phòng khám số 1",       date: "13/06/2026", by: "NV. Hoa" },
  { id: "TX003", item: "Kim tiêm nha khoa",        type: "import", qty: 5,  note: "Nhập khẩn cấp do sắp hết",      date: "12/06/2026", by: "NV. Lan" },
  { id: "TX004", item: "Nước súc miệng Listerine", type: "export", qty: 3,  note: "Xuất cho quầy lễ tân",           date: "12/06/2026", by: "NV. Hoa" },
  { id: "TX005", item: "Mũi khoan composite",      type: "import", qty: 20, note: "Nhập theo kế hoạch tháng",       date: "11/06/2026", by: "NV. Lan" },
];

const CATEGORIES = ["Tất cả","Bảo hộ","Dụng cụ","Vật liệu","Tiêu hao","Thuốc"];
const EMPTY_FORM = { item: INITIAL_STOCK[0].id, type: "import" as "import"|"export", qty: 1, note: "" };

const filterSelectCls = "px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 appearance-none cursor-pointer pr-8";
const selectCls = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8";
const inputCls  = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";

export default function OwnerInventoryPage() {
  useRequireOwner();

  const [tab,    setTab]    = useState<"stock"|"transaction"|"log">("stock");
  const [cat,    setCat]    = useState("Tất cả");
  const [search, setSearch] = useState("");
  const [stock,  setStock]  = useState<StockItem[]>(INITIAL_STOCK);
  const [log,    setLog]    = useState(HISTORY_LOG);
  const [form,   setForm]   = useState(EMPTY_FORM);
  const [saved,  setSaved]  = useState(false);

  const filteredStock = stock.filter(s => {
    const matchCat    = cat === "Tất cả" || s.category === cat;
    const matchSearch = !search || s.name.toLowerCase().includes(search.toLowerCase());
    return matchCat && matchSearch;
  });

  const lowStock = stock.filter(s => s.qty <= s.minQty);

  const handleTransaction = (e: React.FormEvent) => {
    e.preventDefault();
    const target = stock.find(s => s.id === form.item);
    if (!target) return;
    setStock(prev => prev.map(s => s.id === form.item
      ? { ...s, qty: form.type === "import" ? s.qty + form.qty : Math.max(0, s.qty - form.qty) }
      : s
    ));
    setLog(prev => [{
      id: `TX${String(prev.length + 1).padStart(3, "0")}`,
      item: target.name, type: form.type, qty: form.qty, note: form.note,
      date: new Date().toLocaleDateString("vi-VN"), by: "NV. Lan",
    }, ...prev]);
    setSaved(true);
    setTimeout(() => { setSaved(false); setForm(EMPTY_FORM); }, 2000);
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="inventory" />
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Quản lý vật tư</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
              Quản lý kho vật tư và dụng cụ y tế phục vụ vận hành.
            </p>
          </div>
          <NotificationBell />
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Low-stock alert */}
          {lowStock.length > 0 && (
            <div className="flex items-center gap-3 px-5 py-3.5 bg-amber-50 border border-amber-200 rounded-2xl">
              <svg className="w-5 h-5 text-amber-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
              <span className="text-[13.5px] font-bold text-amber-800">
                {lowStock.length} mặt hàng sắp hết: {lowStock.map(s => s.name).join(", ")}
              </span>
            </div>
          )}

          {/* Tabs */}
          <div className="flex gap-2">
            {([
              { key: "stock",       label: "Tồn kho"           },
              { key: "transaction", label: "+ Nhập / Xuất"     },
              { key: "log",         label: "Lịch sử giao dịch" },
            ] as const).map(t => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`px-5 py-2 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer border ${
                  tab === t.key ? "bg-primary text-white border-primary shadow-sm shadow-primary/20" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                }`}>
                {t.label}
              </button>
            ))}
          </div>

          {tab === "stock" && (
            <>
              {/* Filter toolbar */}
              <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/70 shadow-sm flex flex-col sm:flex-row gap-3">
                <div className="relative flex-1">
                  <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                  </span>
                  <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Tìm tên vật tư..."
                    className="w-full pl-10 pr-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400" />
                </div>
                <div className="relative">
                  <select value={cat} onChange={e => setCat(e.target.value)} className={filterSelectCls}>
                    {CATEGORIES.map(c => <option key={c}>{c}</option>)}
                  </select>
                  <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                </div>
              </div>

              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <table className="w-full text-[13px]">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/70 border-collapse">
                      <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Mã</th>
                      <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Tên vật tư</th>
                      <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Danh mục</th>
                      <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Đơn vị</th>
                      <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Tồn kho</th>
                      <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Tối thiểu</th>
                      <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {filteredStock.map(s => {
                      const isLow = s.qty <= s.minQty;
                      return (
                        <tr key={s.id} className={`transition-colors ${isLow ? "bg-amber-50/30 hover:bg-amber-50/50" : "hover:bg-slate-50/50"}`}>
                          <td className="px-5 py-3.5 font-mono text-[12px] font-black text-slate-400">{s.id}</td>
                          <td className="px-5 py-3.5 font-bold text-slate-900">{s.name}</td>
                          <td className="px-5 py-3.5 text-slate-500 font-semibold">{s.category}</td>
                          <td className="px-5 py-3.5 text-slate-500 font-semibold">{s.unit}</td>
                          <td className={`px-5 py-3.5 text-right font-black ${isLow ? "text-amber-600" : "text-slate-900"}`}>{s.qty}</td>
                          <td className="px-5 py-3.5 text-right text-slate-400 font-semibold">{s.minQty}</td>
                          <td className="px-5 py-3.5">
                            {isLow ? (
                              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-amber-50 text-amber-700 border border-amber-100 rounded-full text-[11.5px] font-black">
                                <span className="w-1.5 h-1.5 rounded-full bg-amber-500" />Sắp hết
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-green-50 text-green-700 border border-green-100 rounded-full text-[11.5px] font-black">
                                <span className="w-1.5 h-1.5 rounded-full bg-green-500" />Đủ
                              </span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {tab === "transaction" && (
            <div className="grid grid-cols-1 lg:grid-cols-5 gap-5">
              <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-7 flex flex-col gap-5">
                <h2 className="text-[15px] font-black text-slate-900">Tạo phiếu nhập / xuất</h2>
                {saved ? (
                  <div className="flex items-center gap-3 bg-green-50 border border-green-100 text-green-700 px-4 py-3 rounded-xl text-[13px] font-bold">
                    <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    Đã cập nhật tồn kho thành công!
                  </div>
                ) : (
                  <form onSubmit={handleTransaction} className="flex flex-col gap-4">
                    {/* Import / Export toggle */}
                    <div className="flex gap-3">
                      {([
                        { key: "import", label: "Nhập kho", activeCls: "bg-emerald-500 hover:bg-emerald-600 text-white border-emerald-500 shadow-sm shadow-emerald-200" },
                        { key: "export", label: "Xuất kho", activeCls: "bg-primary hover:bg-red-600 text-white border-primary shadow-sm shadow-primary/20" },
                      ] as const).map(t => (
                        <button type="button" key={t.key} onClick={() => setForm(p => ({ ...p, type: t.key }))}
                          className={`flex-1 py-3 rounded-xl text-[13.5px] font-black border-2 cursor-pointer transition-all ${
                            form.type === t.key ? t.activeCls : "bg-white text-slate-400 border-slate-200 hover:border-slate-300"
                          }`}>
                          {t.label}
                        </button>
                      ))}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Vật tư *</label>
                      <div className="relative">
                        <select value={form.item} onChange={e => setForm(p => ({ ...p, item: e.target.value }))} className={selectCls}>
                          {stock.map(s => (
                            <option key={s.id} value={s.id}>{s.name} — Tồn: {s.qty} {s.unit}</option>
                          ))}
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                      </div>
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Số lượng *</label>
                      <input type="number" min={1} required value={form.qty} onChange={e => setForm(p => ({ ...p, qty: Number(e.target.value) }))}
                        className={inputCls} />
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                      <input value={form.note} onChange={e => setForm(p => ({ ...p, note: e.target.value }))}
                        placeholder={form.type === "import" ? "Nhà cung cấp, lý do nhập..." : "Phòng nhận, lý do xuất..."}
                        className={inputCls} />
                    </div>

                    <button type="submit"
                      className={`flex items-center justify-center gap-2 w-full py-3 rounded-xl text-[14px] font-black transition-all cursor-pointer shadow-sm mt-1 ${
                        form.type === "import"
                          ? "bg-emerald-500 hover:bg-emerald-600 text-white shadow-emerald-200"
                          : "bg-primary hover:bg-red-600 text-white shadow-primary/25"
                      }`}>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      {form.type === "import" ? "Xác nhận nhập kho" : "Xác nhận xuất kho"}
                    </button>
                  </form>
                )}
              </div>

              {/* Current stock reference */}
              <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200/60 shadow-sm flex flex-col overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100">
                  <h3 className="text-[15px] font-black text-slate-900">Tồn kho hiện tại</h3>
                </div>
                <ul className="flex-1 divide-y divide-slate-100 overflow-y-auto">
                  {stock.map(s => {
                    const isLow = s.qty <= s.minQty;
                    return (
                      <li key={s.id} className="px-6 py-3.5 flex items-center justify-between gap-3 hover:bg-slate-50/50 transition-colors">
                        <div>
                          <div className="text-[13.5px] font-bold text-slate-900">{s.name}</div>
                          <div className="text-[12px] text-slate-400 font-semibold">{s.category} · {s.unit}</div>
                        </div>
                        <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black shrink-0 ${
                          isLow ? "bg-amber-50 text-amber-700 border border-amber-100" : "bg-green-50 text-green-700 border border-green-100"
                        }`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${isLow ? "bg-amber-500" : "bg-green-500"}`} />
                          {s.qty} {s.unit}
                        </span>
                      </li>
                    );
                  })}
                </ul>
              </div>
            </div>
          )}

          {tab === "log" && (
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <table className="w-full text-[13px]">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70 border-collapse">
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Mã GD</th>
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Loại</th>
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Vật tư</th>
                    <th className="px-5 py-3 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">SL</th>
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Ghi chú</th>
                    <th className="px-5 py-3 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Ngày · NV</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {log.map(tx => (
                    <tr key={tx.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-5 py-3.5 font-mono text-[12px] font-black text-slate-400">{tx.id}</td>
                      <td className="px-5 py-3.5">
                        <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black ${
                          tx.type === "import" ? "bg-green-50 text-green-700 border border-green-100" : "bg-red-50 text-primary border border-red-100"
                        }`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${tx.type === "import" ? "bg-green-500" : "bg-red-400"}`} />
                          {tx.type === "import" ? "Nhập" : "Xuất"}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 font-bold text-slate-900">{tx.item}</td>
                      <td className="px-5 py-3.5 text-right font-black">
                        <span className={tx.type === "import" ? "text-green-600" : "text-primary"}>
                          {tx.type === "import" ? "+" : "-"}{tx.qty}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-slate-500 font-semibold max-w-xs truncate">{tx.note || "—"}</td>
                      <td className="px-5 py-3.5">
                        <div className="text-slate-600 font-semibold">{tx.date}</div>
                        <div className="text-[11.5px] text-slate-400 font-medium">{tx.by}</div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
