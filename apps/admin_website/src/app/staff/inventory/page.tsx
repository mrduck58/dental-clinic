"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { createPortal } from "react-dom";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import Pagination from "../../../components/shared/Pagination";
import { SortableTh, Th, toggleSortState, type SortDir } from "../../../components/shared/TableHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getSupplyItemsApi,
  getSupplyTransactionsApi,
  createSupplyTransactionApi,
  createSupplyItemApi,
  stockImportApi,
  getMaterialRequestsApi,
  markMaterialRequestDoneApi,
  type SupplyItemDto,
  type SupplyTransactionDto,
  type MaterialRequestDto,
} from "../../../lib/apiClient";
import { SUPPLY_UNITS } from "../../../lib/inventoryConstants";

const ITEM_CATEGORIES = ["Bảo hộ", "Dụng cụ", "Vật liệu", "Tiêu hao", "Thuốc"];

const CATEGORIES = ["Tất cả", "Bảo hộ", "Dụng cụ", "Vật liệu", "Tiêu hao", "Thuốc"];
const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

// "standard" = vật dụng thường ngày (găng tay, khẩu trang...); "custom" = hàng đặt riêng cho bệnh nhân
// (răng sứ, hàm tháo lắp...) — vẫn cùng cơ chế tồn kho/nhập-xuất, chỉ khác tab hiển thị.
const ORDER_TYPES: { value: "standard" | "custom"; label: string }[] = [
  { value: "standard", label: "Vật dụng thường ngày" },
  { value: "custom",   label: "Đặt riêng cho bệnh nhân" },
];

const fmt = (n: number) => n.toLocaleString("vi-VN") + "₫";

type StockSortKey = "code" | "name" | "category" | "unit" | "quantity";
const STOCK_SORT_DESC_BY_DEFAULT = (column: StockSortKey) => column === "quantity";

type LogSortKey = "type" | "itemName" | "quantity" | "total" | "date";
const LOG_SORT_DESC_BY_DEFAULT = (column: LogSortKey) => column === "quantity" || column === "total" || column === "date";

// Modal phải render qua Portal thẳng vào document.body — trang bọc ngoài dùng class "animate-fade-in"
// (transform), mà theo spec CSS, "position: fixed" bên trong 1 ancestor có transform sẽ neo theo ancestor
// đó thay vì theo viewport. Nếu không portal, modal bị đẩy xuống theo chiều cao trang, phải cuộn mới thấy.
function Portal({ children }: { children: React.ReactNode }) {
  if (typeof document === "undefined") return null;
  return createPortal(children, document.body);
}

// ── Styles ─────────────────────────────────────────────────────────────────

const filterSelectCls = "px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 appearance-none cursor-pointer pr-8";
const selectCls = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8";
const inputCls  = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";

// ── Main page ──────────────────────────────────────────────────────────────

export default function InventoryPage() {
  useRequireStaff();

  const [tab,           setTab]           = useState<"stock" | "transaction" | "log" | "requests">("stock");
  const [requests,      setRequests]      = useState<MaterialRequestDto[]>([]);
  const [loadingReqs,   setLoadingReqs]   = useState(false);
  const [processingId,  setProcessingId]  = useState<string | null>(null);
  const [priceDrafts,   setPriceDrafts]   = useState<Record<string, string>>({});
  const [priceErrors,   setPriceErrors]   = useState<Record<string, boolean>>({});
  const [confirmingRequest, setConfirmingRequest] = useState<MaterialRequestDto | null>(null);
  const [cat,           setCat]           = useState("Tất cả");
  const [orderTypeTab,  setOrderTypeTab]  = useState<"standard" | "custom">("standard");
  const [search,        setSearch]        = useState("");
  const [items,         setItems]         = useState<SupplyItemDto[]>([]);
  const [log,           setLog]           = useState<SupplyTransactionDto[]>([]);
  const [loadingItems,  setLoadingItems]  = useState(false);
  const [loadingLog,    setLoadingLog]    = useState(false);
  const [error,         setError]         = useState<string | null>(null);

  // form state
  const [selectedItemId, setSelectedItemId] = useState("");
  const [txType,         setTxType]         = useState<"import" | "export">("import");
  const [txItemSearch,   setTxItemSearch]   = useState(""); // text input for import
  const [txItemFocused,  setTxItemFocused]  = useState(false); // dropdown gợi ý tên vật tư khi nhập kho
  const [txUnit,         setTxUnit]         = useState("Cái");
  const [txCategory,     setTxCategory]     = useState(ITEM_CATEGORIES[0]);
  const [txOrderType,    setTxOrderType]    = useState<"standard" | "custom">("standard");
  const [txQtyStr,       setTxQtyStr]       = useState("");
  const [txPriceStr,     setTxPriceStr]     = useState("");
  const [txNote,         setTxNote]         = useState("");
  const [txErrors,       setTxErrors]       = useState<{ name?: string; unit?: string; qty?: string; price?: string }>({});
  const [submitting,     setSubmitting]     = useState(false);
  const [saved,          setSaved]          = useState(false);

  // modal thêm vật tư
  const [showAddModal,   setShowAddModal]   = useState(false);
  const [addForm,        setAddForm]        = useState<{
    code: string; name: string; category: string; unit: string; quantity: number; minQuantity: number; orderType: "standard" | "custom"; priceStr: string;
  }>({ code: "", name: "", category: ITEM_CATEGORIES[0], unit: "", quantity: 0, minQuantity: 0, orderType: "standard", priceStr: "" });
  const [addSubmitting,  setAddSubmitting]  = useState(false);
  const [addError,       setAddError]       = useState<string | null>(null);

  // pagination
  const [stockPage,     setStockPage]     = useState(1);
  const [logPage,       setLogPage]       = useState(1);
  const [stockPageSize, setStockPageSize] = useState(5);
  const [logPageSize,   setLogPageSize]   = useState(5);

  // sorting
  const [stockSortKey, setStockSortKey] = useState<StockSortKey>("name");
  const [stockSortDir, setStockSortDir] = useState<SortDir>("asc");
  const [logSortKey,   setLogSortKey]   = useState<LogSortKey>("date");
  const [logSortDir,   setLogSortDir]   = useState<SortDir>("desc");

  const fetchItems = useCallback(async () => {
    setLoadingItems(true);
    setError(null);
    try {
      const data = await getSupplyItemsApi();
      setItems(data);
      if (data.length > 0 && !selectedItemId) setSelectedItemId(data[0].id);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể tải vật tư");
    } finally {
      setLoadingItems(false);
    }
  }, [selectedItemId]);

  const fetchLog = useCallback(async () => {
    setLoadingLog(true);
    try {
      const data = await getSupplyTransactionsApi();
      setLog(data);
    } catch {
      // log lỗi nhẹ, không block UI
    } finally {
      setLoadingLog(false);
    }
  }, []);

  const fetchRequests = useCallback(async () => {
    setLoadingReqs(true);
    try {
      setRequests(await getMaterialRequestsApi());
    } catch {
      // lỗi nhẹ, không block UI
    } finally {
      setLoadingReqs(false);
    }
  }, []);

  // Bấm "Nhập kho & Đã xử lý" → chỉ validate đủ giá rồi MỞ MODAL xác nhận, chưa gọi API — tránh trường hợp
  // bấm nhầm là nhập kho luôn (không thể hoàn tác). Phải bấm "Xác nhận" trong modal mới thực sự nhập kho.
  const handleRequestValidateAndConfirm = (r: MaterialRequestDto) => {
    const missing: Record<string, boolean> = {};
    for (const it of r.items) {
      const price = Number(priceDrafts[it.id]);
      if (priceDrafts[it.id] === undefined || priceDrafts[it.id] === "" || Number.isNaN(price) || price < 0) {
        missing[it.id] = true;
      }
    }
    if (Object.keys(missing).length > 0) {
      setPriceErrors(prev => ({ ...prev, ...missing }));
      setError("Vui lòng nhập đủ đơn giá cho từng vật tư trước khi xác nhận.");
      return;
    }
    setError(null);
    setConfirmingRequest(r);
  };

  const handleConfirmImport = async () => {
    const r = confirmingRequest;
    if (!r) return;
    setProcessingId(r.id);
    try {
      const itemPrices = r.items.map(it => ({ materialRequestItemId: it.id, unitPrice: Number(priceDrafts[it.id]) }));
      await markMaterialRequestDoneApi(r.id, itemPrices);
      setPriceDrafts(prev => {
        const next = { ...prev };
        for (const it of r.items) delete next[it.id];
        return next;
      });
      setConfirmingRequest(null);
      await Promise.all([fetchRequests(), fetchItems(), fetchLog()]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Nhập kho theo yêu cầu vật tư thất bại");
    } finally {
      setProcessingId(null);
    }
  };

  useEffect(() => { fetchItems(); fetchLog(); fetchRequests(); }, []);

  // Filter stock
  const filteredStock = items.filter(s => {
    const matchOrderType = s.orderType === orderTypeTab;
    const matchCat    = cat === "Tất cả" || s.category === cat;
    const matchSearch = !search || s.name.toLowerCase().includes(search.toLowerCase());
    return matchOrderType && matchCat && matchSearch;
  });

  // Gợi ý tên vật tư khi nhập kho — click vào gợi ý sẽ điền tên đó vào ô input
  const txItemSuggestions = txItemSearch.trim()
    ? items.filter(s => s.name.toLowerCase().includes(txItemSearch.trim().toLowerCase())).slice(0, 6)
    : [];
  const showTxItemSuggestions = txItemFocused && !selectedItemId && txItemSuggestions.length > 0;

  useEffect(() => { setStockPage(1); }, [search, cat, orderTypeTab, stockPageSize]);

  const sortedStock = useMemo(() => {
    const dir = stockSortDir === "asc" ? 1 : -1;
    const value = (s: SupplyItemDto): string | number => {
      switch (stockSortKey) {
        case "code": return s.code.toLowerCase();
        case "name": return s.name.toLowerCase();
        case "category": return s.category.toLowerCase();
        case "unit": return s.unit.toLowerCase();
        case "quantity": return s.quantity;
      }
    };
    return [...filteredStock].sort((a, b) => {
      const va = value(a), vb = value(b);
      if (typeof va === "string" && typeof vb === "string") return va.localeCompare(vb, "vi") * dir;
      return ((va as number) - (vb as number)) * dir;
    });
  }, [filteredStock, stockSortKey, stockSortDir]);

  const handleStockSort = (column: StockSortKey) => {
    const next = toggleSortState({ key: stockSortKey, dir: stockSortDir }, column, STOCK_SORT_DESC_BY_DEFAULT);
    setStockSortKey(next.key);
    setStockSortDir(next.dir);
    setStockPage(1);
  };

  const sortedLog = useMemo(() => {
    const dir = logSortDir === "asc" ? 1 : -1;
    const value = (tx: SupplyTransactionDto): string | number => {
      switch (logSortKey) {
        case "type": return tx.type.toLowerCase();
        case "itemName": return tx.itemName.toLowerCase();
        case "quantity": return tx.quantity;
        case "total": return tx.unitPrice != null ? tx.unitPrice * tx.quantity : 0;
        case "date": return new Date(tx.createdAt).getTime();
      }
    };
    return [...log].sort((a, b) => {
      const va = value(a), vb = value(b);
      if (typeof va === "string" && typeof vb === "string") return va.localeCompare(vb, "vi") * dir;
      return ((va as number) - (vb as number)) * dir;
    });
  }, [log, logSortKey, logSortDir]);

  const handleLogSort = (column: LogSortKey) => {
    const next = toggleSortState({ key: logSortKey, dir: logSortDir }, column, LOG_SORT_DESC_BY_DEFAULT);
    setLogSortKey(next.key);
    setLogSortDir(next.dir);
    setLogPage(1);
  };

  const pagedStock = sortedStock.slice((stockPage - 1) * stockPageSize, stockPage * stockPageSize);
  const pagedLog   = sortedLog.slice((logPage - 1) * logPageSize, logPage * logPageSize);

  const handleTransaction = async (e: React.FormEvent) => {
    e.preventDefault();

    const txQty = Number(txQtyStr);
    const txPrice = txPriceStr ? Number(txPriceStr) : undefined;

    // ── Nhập kho: dùng endpoint mới (tự tạo hoặc cộng vào vật tư đã có) ──
    if (txType === "import") {
      const errors: { name?: string; unit?: string; qty?: string; price?: string } = {};
      if (!txItemSearch.trim()) errors.name = "Vui lòng nhập tên vật tư.";
      if (!txUnit) errors.unit = "Vui lòng chọn đơn vị.";
      if (!txQtyStr || txQty <= 0) errors.qty = "Số lượng phải lớn hơn 0.";
      if (txPriceStr && (Number.isNaN(txPrice) || (txPrice ?? 0) < 0)) errors.price = "Đơn giá không hợp lệ.";

      if (Object.keys(errors).length > 0) {
        setTxErrors(errors);
        return;
      }
      setTxErrors({});
      setSubmitting(true);
      setError(null);
      try {
        const tx = await stockImportApi({
          name: txItemSearch.trim(),
          unit: txUnit,
          category: txCategory,
          quantity: txQty,
          note: txNote || undefined,
          unitPrice: txPrice,
          orderType: txOrderType,
        });
        // Nếu vật tư đã có → cập nhật local state; nếu mới → refetch
        const existingItem = items.find(it => it.id === tx.supplyItemId);
        if (existingItem) {
          setItems(prev => prev.map(it => {
            if (it.id !== tx.supplyItemId) return it;
            const newQty = it.quantity + txQty;
            return { ...it, quantity: newQty, isLow: newQty <= it.minQuantity };
          }));
        } else {
          await fetchItems();
        }
        setLog(prev => [tx, ...prev]);
        setLogPage(1);
        setSaved(true);
        setTimeout(() => {
          setSaved(false);
          setTxQtyStr(""); setTxPriceStr(""); setTxNote(""); setTxItemSearch(""); setTxUnit("Cái"); setTxCategory(ITEM_CATEGORIES[0]); setTxOrderType("standard");
        }, 2000);
      } catch (e) {
        setError(e instanceof Error ? e.message : "Nhập kho thất bại");
      } finally {
        setSubmitting(false);
      }
      return;
    }

    // ── Xuất kho: giữ nguyên logic cũ ──
    if (!txQtyStr || txQty <= 0) {
      setError("Số lượng phải lớn hơn 0.");
      return;
    }
    if (!selectedItemId) {
      setError("Vui lòng chọn vật tư.");
      return;
    }
    const exportItem = items.find(s => s.id === selectedItemId);
    if (exportItem && txQty > exportItem.quantity) {
      setError(`Số lượng xuất (${txQty}) vượt quá tồn kho hiện tại (${exportItem.quantity} ${exportItem.unit}).`);
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const tx = await createSupplyTransactionApi({
        supplyItemId: selectedItemId,
        type: "export",
        quantity: txQty,
        note: txNote || undefined,
      });
      setItems(prev => prev.map(it => {
        if (it.id !== tx.supplyItemId) return it;
        const newQty = Math.max(0, it.quantity - txQty);
        return { ...it, quantity: newQty, isLow: newQty <= it.minQuantity };
      }));
      setLog(prev => [tx, ...prev]);
      setLogPage(1);
      setSaved(true);
      setTimeout(() => { setSaved(false); setTxQtyStr(""); setTxNote(""); }, 2000);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Tạo giao dịch thất bại");
    } finally {
      setSubmitting(false);
    }
  };

  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault();
    setAddSubmitting(true);
    setAddError(null);
    try {
      const created = await createSupplyItemApi({
        code: addForm.code.trim(),
        name: addForm.name.trim(),
        category: addForm.category,
        unit: addForm.unit.trim(),
        quantity: addForm.quantity,
        minQuantity: addForm.minQuantity,
        orderType: addForm.orderType,
        price: addForm.priceStr ? Number(addForm.priceStr) : undefined,
      });
      setItems(prev => [...prev, created]);
      setOrderTypeTab(created.orderType);
      setShowAddModal(false);
    } catch (e) {
      setAddError(e instanceof Error ? e.message : "Thêm vật tư thất bại");
    } finally {
      setAddSubmitting(false);
    }
  };

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="inventory" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader title="Nhập Xuất Vật Tư" subtitle="Quản lý kho vật tư và dụng cụ y tế" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Error banner */}
          {error && (
            <div className="flex items-center gap-3 px-5 py-3.5 bg-red-50 border border-red-200 rounded-2xl">
              <svg className="w-5 h-5 text-red-500 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" /></svg>
              <span className="text-[13.5px] font-bold text-red-700">{error}</span>
            </div>
          )}

          {/* Tabs */}
          <div className="flex gap-2">
            {([
              { key: "stock",       label: "Tồn kho",           count: 0 },
              { key: "transaction", label: "+ Nhập / Xuất",     count: 0 },
              { key: "requests",    label: "Yêu cầu vật tư",     count: requests.filter(r => r.status === "Pending").length },
              { key: "log",         label: "Lịch sử giao dịch", count: 0 },
            ] as const).map(t => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={`flex items-center gap-2 px-5 py-2 rounded-xl text-[13.5px] font-bold transition-all cursor-pointer border ${
                  tab === t.key ? "bg-primary text-white border-primary shadow-sm shadow-primary/20" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                }`}>
                {t.label}
                {t.count > 0 && (
                  <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${tab === t.key ? "bg-white/25 text-white" : "bg-amber-100 text-amber-700"}`}>{t.count}</span>
                )}
              </button>
            ))}
          </div>

          {/* ── Tab: Tồn kho ── */}
          {tab === "stock" && (
            <>
              {/* Sub-tab: vật dụng thường ngày vs hàng đặt riêng cho bệnh nhân */}
              <div className="flex gap-2">
                {ORDER_TYPES.map(ot => {
                  const count = items.filter(i => i.orderType === ot.value).length;
                  const active = orderTypeTab === ot.value;
                  return (
                    <button key={ot.value} onClick={() => setOrderTypeTab(ot.value)}
                      className={`flex items-center gap-2 px-4 py-1.5 rounded-lg text-[12.5px] font-bold transition-all cursor-pointer border ${
                        active ? "bg-slate-800 text-white border-slate-800" : "bg-white text-slate-500 border-slate-200 hover:border-slate-300"
                      }`}>
                      {ot.label}
                      <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${active ? "bg-white/25 text-white" : "bg-slate-100 text-slate-500"}`}>{count}</span>
                    </button>
                  );
                })}
              </div>

              <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/70 shadow-sm flex flex-col gap-3">
                <div className="flex flex-col sm:flex-row gap-3 items-center">
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
                <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
                  <span>Hiển thị</span>
                  <div className="relative">
                    <select value={stockPageSize} onChange={e => { setStockPageSize(Number(e.target.value)); setStockPage(1); }}
                      className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer">
                      {PAGE_SIZE_OPTIONS.map(n => <option key={n} value={n}>{n}</option>)}
                    </select>
                    <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                      <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                    </div>
                  </div>
                  <span>/ trang</span>
                  <span className="text-slate-300 mx-1">·</span>
                  <span>Tìm thấy <span className="text-slate-600 font-bold">{filteredStock.length}</span> kết quả</span>
                </div>
              </div>

              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <table className="w-full text-[13px]">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/70">
                      <SortableTh column="code" label="Mã" sortKey={stockSortKey} sortDir={stockSortDir} onSort={handleStockSort} className="px-5" />
                      <SortableTh column="name" label="Tên vật tư" sortKey={stockSortKey} sortDir={stockSortDir} onSort={handleStockSort} className="px-5" />
                      <SortableTh column="category" label="Danh mục" sortKey={stockSortKey} sortDir={stockSortDir} onSort={handleStockSort} className="px-5" />
                      <SortableTh column="unit" label="Đơn vị" sortKey={stockSortKey} sortDir={stockSortDir} onSort={handleStockSort} className="px-5" />
                      <SortableTh column="quantity" label="Tồn kho" sortKey={stockSortKey} sortDir={stockSortDir} onSort={handleStockSort} align="right" className="px-5" />
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {loadingItems ? (
                      <tr><td colSpan={5} className="px-5 py-10 text-center text-[13px] text-slate-400 font-semibold">Đang tải...</td></tr>
                    ) : pagedStock.length === 0 ? (
                      <tr><td colSpan={5} className="px-5 py-10 text-center text-[13px] text-slate-400 font-semibold">
                        {items.length === 0 ? "Chưa có vật tư nào trong kho." : "Không tìm thấy vật tư nào."}
                      </td></tr>
                    ) : pagedStock.map(s => (
                      <tr key={s.id} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-5 py-3.5 font-mono text-[12px] font-black text-slate-400">{s.code}</td>
                        <td className="px-5 py-3.5 font-bold text-slate-900">{s.name}</td>
                        <td className="px-5 py-3.5 text-slate-500 font-semibold">{s.category}</td>
                        <td className="px-5 py-3.5 text-slate-500 font-semibold">{s.unit}</td>
                        <td className="px-5 py-3.5 text-right font-black text-slate-900">{s.quantity}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <div className="p-4 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-2.5">
                  <Pagination
                    currentPage={stockPage}
                    totalCount={filteredStock.length}
                    pageSize={stockPageSize}
                    onPageChange={setStockPage}
                    itemLabel="mục"
                  />
                </div>
              </div>
            </>
          )}

          {/* ── Tab: Nhập / Xuất ── */}
          {tab === "transaction" && (
            <div className="flex flex-col gap-5">
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
                    <div className="flex gap-3">
                      {([
                        { key: "import", label: "Nhập kho", activeCls: "bg-emerald-500 hover:bg-emerald-600 text-white border-emerald-500 shadow-sm shadow-emerald-200" },
                        { key: "export", label: "Xuất kho", activeCls: "bg-primary hover:bg-red-600 text-white border-primary shadow-sm shadow-primary/20" },
                      ] as const).map(t => (
                        <button type="button" key={t.key}
                          onClick={() => { setTxType(t.key); setTxItemSearch(""); setTxUnit("Cái"); setTxCategory(ITEM_CATEGORIES[0]); setTxErrors({}); setError(null); setSelectedItemId(t.key === "export" && items.length > 0 ? items[0].id : ""); }}
                          className={`flex-1 py-3 rounded-xl text-[13.5px] font-black border-2 cursor-pointer transition-all ${
                            txType === t.key ? t.activeCls : "bg-white text-slate-400 border-slate-200 hover:border-slate-300"
                          }`}>
                          {t.label}
                        </button>
                      ))}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Vật tư *</label>
                      {txType === "import" ? (
                        <>
                          <div className="relative">
                            <input
                              value={txItemSearch}
                              onChange={e => {
                                setTxItemSearch(e.target.value);
                                setTxErrors(prev => ({ ...prev, name: undefined }));
                                const match = items.find(s => s.name.toLowerCase() === e.target.value.toLowerCase().trim());
                                setSelectedItemId(match ? match.id : "");
                              }}
                              onFocus={() => setTxItemFocused(true)}
                              onBlur={() => setTimeout(() => setTxItemFocused(false), 150)}
                              placeholder="Nhập tên vật tư..."
                              autoComplete="off"
                              className={`${inputCls} ${txErrors.name ? "!border-red-300 focus:!border-red-400 focus:!ring-red-200" : ""}`}
                            />
                            {showTxItemSuggestions && (
                              <div className="absolute z-20 top-full left-0 right-0 mt-1 bg-white border border-slate-200 rounded-xl shadow-lg overflow-hidden">
                                {txItemSuggestions.map(s => (
                                  <button
                                    key={s.id}
                                    type="button"
                                    onMouseDown={e => e.preventDefault()}
                                    onClick={() => {
                                      setTxItemSearch(s.name);
                                      setSelectedItemId(s.id);
                                      setTxUnit(s.unit);
                                      setTxCategory(s.category);
                                      setTxOrderType(s.orderType === "custom" ? "custom" : "standard");
                                      setTxErrors(prev => ({ ...prev, name: undefined }));
                                      setTxItemFocused(false);
                                    }}
                                    className="w-full flex items-center justify-between gap-2 px-3.5 py-2.5 text-left hover:bg-slate-50 transition-colors cursor-pointer"
                                  >
                                    <span className="text-[13px] font-semibold text-slate-700 truncate">{s.name}</span>
                                    <span className="text-[11px] font-bold text-slate-400 shrink-0">Tồn: {s.quantity} {s.unit}</span>
                                  </button>
                                ))}
                              </div>
                            )}
                          </div>
                          {txErrors.name && <p className="text-[12px] text-red-500 font-semibold">{txErrors.name}</p>}
                          {txItemSearch && !selectedItemId && !txErrors.name && (
                            <p className="text-[12px] text-emerald-600 font-semibold">Vật tư mới — sẽ được tạo tự động khi xác nhận.</p>
                          )}
                        </>
                      ) : (
                        <div className="relative">
                          <select value={selectedItemId} onChange={e => setSelectedItemId(e.target.value)} className={selectCls} required>
                            {items.length === 0
                              ? <option value="">Chưa có vật tư</option>
                              : items.map(s => (
                                <option key={s.id} value={s.id}>{s.name} — Tồn: {s.quantity} {s.unit}</option>
                              ))}
                          </select>
                          <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                        </div>
                      )}
                    </div>

                    {txType === "import" && (
                      <>
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Đơn vị *</label>
                          <div className="relative">
                            <select
                              value={txUnit}
                              onChange={e => { setTxUnit(e.target.value); setTxErrors(prev => ({ ...prev, unit: undefined })); }}
                              className={`${selectCls} ${txErrors.unit ? "!border-red-300" : ""}`}
                            >
                              {SUPPLY_UNITS.map(u => <option key={u}>{u}</option>)}
                            </select>
                            <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                          </div>
                          {txErrors.unit && <p className="text-[12px] text-red-500 font-semibold">{txErrors.unit}</p>}
                          <p className="text-[11.5px] text-slate-400 font-semibold">Nếu vật tư đã tồn tại, đơn vị trong kho sẽ được giữ nguyên.</p>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Danh mục *</label>
                          <div className="relative">
                            <select
                              value={txCategory}
                              onChange={e => setTxCategory(e.target.value)}
                              className={selectCls}
                            >
                              {ITEM_CATEGORIES.map(c => <option key={c}>{c}</option>)}
                            </select>
                            <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                          </div>
                          <p className="text-[11.5px] text-slate-400 font-semibold">Nếu vật tư đã tồn tại, danh mục trong kho sẽ được giữ nguyên.</p>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Loại vật tư *</label>
                          <div className="grid grid-cols-2 gap-2">
                            {ORDER_TYPES.map(ot => (
                              <button key={ot.value} type="button" onClick={() => setTxOrderType(ot.value)}
                                className={`px-3 py-2 rounded-xl border text-[12.5px] font-bold transition-all cursor-pointer ${
                                  txOrderType === ot.value ? "bg-slate-800 text-white border-slate-800" : "bg-white text-slate-500 border-slate-200 hover:border-slate-300"
                                }`}>
                                {ot.label}
                              </button>
                            ))}
                          </div>
                          <p className="text-[11.5px] text-slate-400 font-semibold">Nếu vật tư đã tồn tại, loại vật tư trong kho sẽ được giữ nguyên.</p>
                        </div>
                      </>
                    )}

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Số lượng *</label>
                      <input type="number" min={0}
                        value={txQtyStr}
                        onChange={e => { setTxQtyStr(e.target.value); setTxErrors(prev => ({ ...prev, qty: undefined })); }}
                        onWheel={e => e.currentTarget.blur()}
                        placeholder="Nhập số lượng..."
                        className={`${inputCls} [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none ${txErrors.qty ? "!border-red-300 focus:!border-red-400 focus:!ring-red-200" : ""}`} />
                      {txErrors.qty && <p className="text-[12px] text-red-500 font-semibold">{txErrors.qty}</p>}
                    </div>

                    {txType === "import" && (
                      <div className="flex flex-col gap-1.5">
                        <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Đơn giá (₫)</label>
                        <input type="number" min={0}
                          value={txPriceStr}
                          onChange={e => { setTxPriceStr(e.target.value); setTxErrors(prev => ({ ...prev, price: undefined })); }}
                          onWheel={e => e.currentTarget.blur()}
                          placeholder="Giá nhập / 1 đơn vị — bỏ trống nếu không rõ"
                          className={`${inputCls} [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none ${txErrors.price ? "!border-red-300 focus:!border-red-400 focus:!ring-red-200" : ""}`} />
                        {txErrors.price && <p className="text-[12px] text-red-500 font-semibold">{txErrors.price}</p>}
                        {txPriceStr && txQtyStr && !Number.isNaN(Number(txPriceStr)) && !Number.isNaN(Number(txQtyStr)) && (
                          <p className="text-[12px] text-slate-500 font-semibold">
                            Thành tiền: <span className="font-black text-slate-700">{fmt(Number(txPriceStr) * Number(txQtyStr))}</span>
                          </p>
                        )}
                      </div>
                    )}

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                      <input value={txNote} onChange={e => setTxNote(e.target.value)}
                        placeholder={txType === "import" ? "Nhà cung cấp, lý do nhập..." : "Phòng nhận, lý do xuất..."}
                        className={inputCls} />
                    </div>

                    <button type="submit" disabled={submitting || (txType === "export" && items.length === 0)}
                      className={`flex items-center justify-center gap-2 w-full py-3 rounded-xl text-[14px] font-black transition-all cursor-pointer shadow-sm mt-1 disabled:opacity-60 disabled:cursor-not-allowed ${
                        txType === "import"
                          ? "bg-emerald-500 hover:bg-emerald-600 text-white shadow-emerald-200"
                          : "bg-primary hover:bg-red-600 text-white shadow-primary/25"
                      }`}>
                      {submitting ? (
                        <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                      ) : (
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      )}
                      {txType === "import" ? "Xác nhận nhập kho" : "Xác nhận xuất kho"}
                    </button>
                  </form>
                )}
              </div>

              {/* Current stock reference */}
              <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200/60 shadow-sm flex flex-col overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100">
                  <h3 className="text-[15px] font-black text-slate-900">Tồn kho hiện tại</h3>
                </div>
                <ul className="flex-1 divide-y divide-slate-100 overflow-y-auto max-h-[500px]">
                  {loadingItems ? (
                    <li className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Đang tải...</li>
                  ) : items.length === 0 ? (
                    <li className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Chưa có vật tư nào.</li>
                  ) : items.map(s => (
                    <li key={s.id} className="px-6 py-3.5 flex items-center justify-between gap-3 hover:bg-slate-50/50 transition-colors">
                      <div>
                        <div className="text-[13.5px] font-bold text-slate-900">{s.name}</div>
                        <div className="text-[12px] text-slate-400 font-semibold">{s.category} · {s.unit}</div>
                      </div>
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black shrink-0 ${
                        s.isLow ? "bg-amber-50 text-amber-700 border border-amber-100" : "bg-green-50 text-green-700 border border-green-100"
                      }`}>
                        <span className={`w-1.5 h-1.5 rounded-full ${s.isLow ? "bg-amber-500" : "bg-green-500"}`} />
                        {s.quantity} {s.unit}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
            </div>
          )}

          {/* ── Tab: Lịch sử giao dịch ── */}
          {tab === "log" && (
            <>
              <div className="bg-white px-5 py-3.5 rounded-2xl border border-slate-200/70 shadow-sm flex items-center gap-2 text-[13px] text-slate-400 font-semibold">
                <span>Hiển thị</span>
                <div className="relative">
                  <select value={logPageSize} onChange={e => { setLogPageSize(Number(e.target.value)); setLogPage(1); }}
                    className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer">
                    {PAGE_SIZE_OPTIONS.map(n => <option key={n} value={n}>{n}</option>)}
                  </select>
                  <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                  </div>
                </div>
                <span>/ trang</span>
                <span className="text-slate-300 mx-1">·</span>
                <span>Tìm thấy <span className="text-slate-600 font-bold">{log.length}</span> kết quả</span>
              </div>
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <table className="w-full text-[13px]">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/70">
                      <SortableTh column="type" label="Loại" sortKey={logSortKey} sortDir={logSortDir} onSort={handleLogSort} className="px-5" />
                      <SortableTh column="itemName" label="Vật tư" sortKey={logSortKey} sortDir={logSortDir} onSort={handleLogSort} className="px-5" />
                      <SortableTh column="quantity" label="SL" sortKey={logSortKey} sortDir={logSortDir} onSort={handleLogSort} align="right" className="px-5" />
                      <SortableTh column="total" label="Thành tiền" sortKey={logSortKey} sortDir={logSortDir} onSort={handleLogSort} align="right" className="px-5" />
                      <Th className="px-5">Ghi chú</Th>
                      <SortableTh column="date" label="Ngày · NV" sortKey={logSortKey} sortDir={logSortDir} onSort={handleLogSort} className="px-5" />
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {loadingLog ? (
                      <tr><td colSpan={6} className="px-5 py-10 text-center text-[13px] text-slate-400 font-semibold">Đang tải...</td></tr>
                    ) : pagedLog.length === 0 ? (
                      <tr><td colSpan={6} className="px-5 py-10 text-center text-[13px] text-slate-400 font-semibold">Chưa có giao dịch nào.</td></tr>
                    ) : pagedLog.map(tx => (
                      <tr key={tx.id} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-5 py-3.5">
                          <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black ${
                            tx.type === "import" ? "bg-green-50 text-green-700 border border-green-100" : "bg-red-50 text-primary border border-red-100"
                          }`}>
                            <span className={`w-1.5 h-1.5 rounded-full ${tx.type === "import" ? "bg-green-500" : "bg-red-400"}`} />
                            {tx.type === "import" ? "Nhập" : "Xuất"}
                          </span>
                        </td>
                        <td className="px-5 py-3.5 font-bold text-slate-900">{tx.itemName}</td>
                        <td className="px-5 py-3.5 text-right font-black">
                          <span className={tx.type === "import" ? "text-green-600" : "text-primary"}>
                            {tx.type === "import" ? "+" : "-"}{tx.quantity}
                          </span>
                        </td>
                        <td className="px-5 py-3.5 text-right font-semibold text-slate-600">
                          {tx.unitPrice != null ? fmt(tx.unitPrice * tx.quantity) : "—"}
                        </td>
                        <td className="px-5 py-3.5 text-slate-500 font-semibold max-w-xs truncate">{tx.note || "—"}</td>
                        <td className="px-5 py-3.5">
                          <div className="text-slate-600 font-semibold">{formatDate(tx.createdAt)}</div>
                          <div className="text-[11.5px] text-slate-400 font-medium">{tx.createdBy}</div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <div className="p-4 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-2.5">
                  <Pagination
                    currentPage={logPage}
                    totalCount={log.length}
                    pageSize={logPageSize}
                    onPageChange={setLogPage}
                    itemLabel="mục"
                  />
                </div>
              </div>
            </>
          )}

          {/* ── Tab: Yêu cầu vật tư ── */}
          {tab === "requests" && (
            <div className="flex flex-col gap-3">
              <p className="text-[13px] text-slate-500 font-semibold">
                Vật liệu bác sĩ yêu cầu khi lập liệu trình dài hạn. Nhập <strong>đơn giá</strong> cho từng vật tư rồi bấm <strong>Nhập kho &amp; Đã xử lý</strong> — hệ thống tự cộng vào tồn kho, loại <strong>&quot;Đặt riêng cho bệnh nhân&quot;</strong>. Số lượng/đơn vị giữ nguyên theo yêu cầu của bác sĩ.
              </p>
              {loadingReqs ? (
                <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm py-16 text-center text-[13px] text-slate-400 font-semibold">Đang tải...</div>
              ) : requests.length === 0 ? (
                <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm py-16 text-center text-[13px] text-slate-400 font-semibold">Chưa có yêu cầu vật tư nào.</div>
              ) : (
                requests.map(r => (
                  <div key={r.id} className={`bg-white rounded-2xl border shadow-sm px-6 py-4 flex items-start gap-4 ${r.status === "Pending" ? "border-amber-200" : "border-slate-200/70 opacity-75"}`}>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-[14px] font-black text-slate-900">{r.courseName || "Liệu trình"}</span>
                        {r.status === "Pending" ? (
                          <span className="text-[11px] font-black px-2 py-0.5 rounded-lg bg-amber-50 text-amber-700 border border-amber-200">Chờ xử lý</span>
                        ) : (
                          <span className="text-[11px] font-black px-2 py-0.5 rounded-lg bg-green-50 text-green-700 border border-green-200">Đã xử lý</span>
                        )}
                      </div>
                      <div className="text-[12px] text-slate-400 font-semibold mt-0.5">
                        BN: {r.patientName} · BS: {r.dentistName} · {formatDate(r.createdAt)}
                        {r.handledBy ? ` · Xử lý bởi ${r.handledBy}` : ""}
                      </div>
                      <div className="mt-2 flex flex-col gap-1.5 bg-slate-50 border border-slate-100 rounded-xl px-4 py-3">
                        {r.items.map(it => (
                          <div key={it.id} className="flex items-center gap-3">
                            <div className="text-[13px] font-semibold text-slate-700 flex-1 min-w-0">
                              {it.itemName} — {it.quantity} {it.unit}
                            </div>
                            {r.status === "Pending" && (
                              <div className="shrink-0 flex flex-col items-end">
                                <div className="flex items-center gap-1">
                                  <span className="text-[12px] text-slate-400 font-semibold">Đơn giá</span>
                                  <input
                                    type="number" min={0}
                                    value={priceDrafts[it.id] ?? ""}
                                    onChange={e => {
                                      setPriceDrafts(prev => ({ ...prev, [it.id]: e.target.value }));
                                      setPriceErrors(prev => ({ ...prev, [it.id]: false }));
                                    }}
                                    onWheel={e => e.currentTarget.blur()}
                                    placeholder="₫"
                                    className={`w-28 px-2.5 py-1.5 text-[13px] bg-white border rounded-lg focus:outline-none focus:border-primary font-semibold text-slate-700 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none ${priceErrors[it.id] ? "border-red-300" : "border-slate-200"}`}
                                  />
                                </div>
                                {priceErrors[it.id] && <span className="text-[11px] text-red-500 font-semibold mt-0.5">Cần nhập giá</span>}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                    {r.status === "Pending" && (
                      <div className="shrink-0 flex items-center gap-2">
                        <button onClick={() => handleRequestValidateAndConfirm(r)}
                          disabled={processingId === r.id}
                          className="flex items-center gap-2 px-4 py-2 bg-emerald-500 hover:bg-emerald-600 text-white text-[13px] font-black rounded-xl transition-all shadow-sm shadow-emerald-200 cursor-pointer whitespace-nowrap disabled:opacity-60 disabled:cursor-not-allowed">
                          {processingId === r.id ? (
                            <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                          ) : (
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                          )}
                          Nhập kho & Đã xử lý
                        </button>
                      </div>
                    )}
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </main>

      {/* ── Modal: Thêm vật tư ── */}
      {showAddModal && (
        <Portal>
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4" onClick={e => { if (e.target === e.currentTarget) setShowAddModal(false); }}>
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md flex flex-col">
            <div className="flex items-center justify-between px-6 py-5 border-b border-slate-100">
              <h2 className="text-[15px] font-black text-slate-900">Thêm vật tư mới</h2>
              <button onClick={() => setShowAddModal(false)} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-all cursor-pointer">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <form onSubmit={handleAddItem} className="flex flex-col gap-4 px-6 py-5">
              {addError && (
                <div className="flex items-center gap-2 px-4 py-3 bg-red-50 border border-red-100 rounded-xl text-[13px] font-bold text-red-700">
                  <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" /></svg>
                  {addError}
                </div>
              )}

              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Mã vật tư *</label>
                  <input required value={addForm.code} onChange={e => setAddForm(f => ({ ...f, code: e.target.value }))}
                    placeholder="VT011" className={inputCls} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Đơn vị *</label>
                  <input required value={addForm.unit} onChange={e => setAddForm(f => ({ ...f, unit: e.target.value }))}
                    placeholder="Hộp, Cái, Gói..." className={inputCls} />
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Tên vật tư *</label>
                <input required value={addForm.name} onChange={e => setAddForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="Tên đầy đủ của vật tư" className={inputCls} />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Danh mục *</label>
                <div className="relative">
                  <select value={addForm.category} onChange={e => setAddForm(f => ({ ...f, category: e.target.value }))} className={selectCls}>
                    {ITEM_CATEGORIES.map(c => <option key={c}>{c}</option>)}
                  </select>
                  <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Loại vật tư *</label>
                <div className="grid grid-cols-2 gap-2">
                  {ORDER_TYPES.map(ot => (
                    <button key={ot.value} type="button" onClick={() => setAddForm(f => ({ ...f, orderType: ot.value }))}
                      className={`px-3 py-2 rounded-xl border text-[12.5px] font-bold transition-all cursor-pointer ${
                        addForm.orderType === ot.value ? "bg-slate-800 text-white border-slate-800" : "bg-white text-slate-500 border-slate-200 hover:border-slate-300"
                      }`}>
                      {ot.label}
                    </button>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Tồn kho ban đầu</label>
                  <input type="number" min={0} value={addForm.quantity} onChange={e => setAddForm(f => ({ ...f, quantity: Number(e.target.value) }))} onWheel={e => e.currentTarget.blur()} className={inputCls} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Tối thiểu</label>
                  <input type="number" min={0} value={addForm.minQuantity} onChange={e => setAddForm(f => ({ ...f, minQuantity: Number(e.target.value) }))} onWheel={e => e.currentTarget.blur()} className={inputCls} />
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Giá (₫)</label>
                <input type="number" min={0} value={addForm.priceStr}
                  onChange={e => setAddForm(f => ({ ...f, priceStr: e.target.value }))}
                  onWheel={e => e.currentTarget.blur()}
                  placeholder="Bỏ trống nếu chưa rõ giá" className={inputCls} />
              </div>

              <div className="flex gap-3 pt-1">
                <button type="button" onClick={() => setShowAddModal(false)}
                  className="flex-1 py-2.5 rounded-xl text-[13.5px] font-bold border border-slate-200 text-slate-600 hover:bg-slate-50 transition-all cursor-pointer">
                  Huỷ
                </button>
                <button type="submit" disabled={addSubmitting}
                  className="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-[13.5px] font-black bg-primary text-white hover:bg-red-600 transition-all cursor-pointer shadow-sm shadow-primary/20 disabled:opacity-60 disabled:cursor-not-allowed">
                  {addSubmitting ? (
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  )}
                  Thêm vật tư
                </button>
              </div>
            </form>
          </div>
        </div>
        </Portal>
      )}

      {/* ── Modal: Xác nhận nhập kho theo yêu cầu vật tư ── */}
      {confirmingRequest && (() => {
        const busy = processingId === confirmingRequest.id;
        const total = confirmingRequest.items.reduce((sum, it) => sum + Number(priceDrafts[it.id] ?? 0) * it.quantity, 0);
        return (
          <Portal>
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
            onClick={e => { if (e.target === e.currentTarget && !busy) setConfirmingRequest(null); }}>
            <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[85vh] flex flex-col overflow-hidden">
              <div className="shrink-0 flex items-center justify-between px-6 py-5 border-b border-slate-100">
                <h2 className="text-[15px] font-black text-slate-900">Xác nhận nhập kho</h2>
                <button onClick={() => setConfirmingRequest(null)} disabled={busy}
                  className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-all cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                </button>
              </div>

              <div className="flex-1 min-h-0 overflow-y-auto px-6 py-5 flex flex-col gap-4">
                <div className="text-[13px] text-slate-500 font-semibold">
                  BN: <span className="font-bold text-slate-700">{confirmingRequest.patientName}</span> · BS: <span className="font-bold text-slate-700">{confirmingRequest.dentistName}</span>
                </div>

                <div className="border border-slate-200 rounded-xl overflow-hidden">
                  <table className="w-full text-[13px]">
                    <thead>
                      <tr className="bg-slate-50 border-b border-slate-100">
                        <th className="px-3 py-2 text-left font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Vật tư</th>
                        <th className="px-3 py-2 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">SL</th>
                        <th className="px-3 py-2 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Đơn giá</th>
                        <th className="px-3 py-2 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Thành tiền</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {confirmingRequest.items.map(it => {
                        const price = Number(priceDrafts[it.id] ?? 0);
                        return (
                          <tr key={it.id}>
                            <td className="px-3 py-2.5 font-bold text-slate-800">{it.itemName} <span className="text-slate-400 font-semibold">({it.unit})</span></td>
                            <td className="px-3 py-2.5 text-right font-semibold text-slate-600">{it.quantity}</td>
                            <td className="px-3 py-2.5 text-right font-semibold text-slate-600">{fmt(price)}</td>
                            <td className="px-3 py-2.5 text-right font-black text-slate-900">{fmt(price * it.quantity)}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>

                <div className="flex items-center justify-between px-1">
                  <span className="text-[13px] font-bold text-slate-500">Tổng cộng</span>
                  <span className="text-[18px] font-black text-emerald-600">{fmt(total)}</span>
                </div>

                <p className="text-[12px] text-amber-700 font-semibold bg-amber-50 border border-amber-100 rounded-xl px-3.5 py-2.5">
                  Xác nhận sẽ cộng thẳng các vật tư trên vào tồn kho (loại &quot;Đặt riêng cho bệnh nhân&quot;) và đánh dấu yêu cầu đã xử lý — không thể hoàn tác.
                </p>
              </div>

              <div className="shrink-0 px-6 py-5 border-t border-slate-100 flex gap-3">
                <button type="button" onClick={() => setConfirmingRequest(null)} disabled={busy}
                  className="flex-1 py-2.5 rounded-xl text-[13.5px] font-bold border border-slate-200 text-slate-600 hover:bg-slate-50 transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed">
                  Huỷ
                </button>
                <button type="button" onClick={() => void handleConfirmImport()} disabled={busy}
                  className="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-[13.5px] font-black bg-emerald-500 text-white hover:bg-emerald-600 transition-all cursor-pointer shadow-sm shadow-emerald-200 disabled:opacity-60 disabled:cursor-not-allowed">
                  {busy ? (
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  )}
                  Xác nhận nhập kho
                </button>
              </div>
            </div>
          </div>
          </Portal>
        );
      })()}
    </div>
  );
}
