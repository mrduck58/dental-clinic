"use client";

import { useState, useEffect, useCallback, useMemo, useRef } from "react";
import { createPortal } from "react-dom";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import MaterialRequestPhotoStrip from "../../../components/shared/MaterialRequestPhotoStrip";
import Pagination from "../../../components/shared/Pagination";
import { SortableTh, Th, toggleSortState, type SortDir } from "../../../components/shared/TableHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import {
  getSupplyItemsApi,
  getSupplyTransactionsApi,
  createSupplyItemApi,
  updateSupplyItemApi,
  deleteSupplyItemApi,
  stockImportApi,
  createSupplyTransactionApi,
  getMaterialRequestsApi,
  markMaterialRequestDoneApi,
  markMaterialRequestOrderedApi,
  createMaterialRequestByStaffApi,
  searchPatientsApi,
  getRoomsApi,
  type SupplyItemDto,
  type SupplyTransactionDto,
  type MaterialRequestDto,
  type PatientSearchResultDto,
  type RoomDto,
} from "../../../lib/apiClient";
import { SUPPLY_UNITS } from "../../../lib/inventoryConstants";

// 3 danh mục vật tư — Vật tư chính gắn trực tiếp với option dịch vụ (mão sứ, veneer...), luôn là hàng đặt
// riêng theo bệnh nhân nên KHÔNG nhập nhanh ở đây (xem QUICK_IMPORT_CATEGORIES) mà phải qua tab
// "Yêu cầu vật tư". 2 danh mục còn lại là hàng tồn kho dùng chung, nhập nhanh trực tiếp được.
const CATEGORY_MAIN = "Vật tư chính";
const CATEGORY_CONSUMABLE = "Vật tư tiêu hao";
const CATEGORY_TECHNICAL = "Vật tư kỹ thuật/labo";
const ITEM_CATEGORIES = [CATEGORY_MAIN, CATEGORY_CONSUMABLE, CATEGORY_TECHNICAL];
const QUICK_IMPORT_CATEGORIES = [CATEGORY_CONSUMABLE, CATEGORY_TECHNICAL];

const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

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

/**
 * Danh sách "Tồn kho hiện tại" dùng chung cho tab "+ Nhập kho" và "Xuất kho theo phòng" — bấm 1 dòng để
 * chọn thẳng vật tư đó vào form bên cạnh, khỏi phải gõ tay/dùng dropdown riêng. Chỉ 2 tab Vật tư tiêu
 * hao/Vật tư kỹ thuật (không có Vật tư chính — loại đó đặt riêng theo bệnh nhân qua "Yêu cầu vật tư",
 * không nhập/xuất nhanh ở đây). Chiều cao giới hạn theo viewport (không đo theo cột form bên cạnh bằng
 * ResizeObserver như cũ — cách đó không cuộn được ổn định khi danh sách dài) nên luôn cuộn nội bộ được.
 */
function StockPickerPanel({ items, loading, selectedId, onSelect, hint }: {
  items: SupplyItemDto[];
  loading: boolean;
  selectedId: string;
  onSelect: (item: SupplyItemDto) => void;
  hint: string;
}) {
  const [pickerCategory, setPickerCategory] = useState(QUICK_IMPORT_CATEGORIES[0]);
  const filtered = items.filter(it => it.category === pickerCategory);

  return (
    <div className="lg:col-span-3 bg-white rounded-2xl border border-slate-200/60 shadow-sm flex flex-col overflow-hidden max-h-[calc(100vh-260px)]">
      <div className="px-6 py-4 border-b border-slate-100 shrink-0">
        <h3 className="text-[15px] font-black text-slate-900">Tồn kho hiện tại</h3>
        <p className="text-[11.5px] text-slate-400 font-semibold mt-0.5">{hint}</p>
      </div>
      <div className="px-6 pt-3 flex gap-2 shrink-0">
        {QUICK_IMPORT_CATEGORIES.map(cat => (
          <button
            key={cat}
            type="button"
            onClick={() => setPickerCategory(cat)}
            className={`px-3 py-1.5 rounded-lg text-[12px] font-bold transition-colors cursor-pointer border ${
              pickerCategory === cat ? "bg-primary text-white border-primary" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40"
            }`}
          >
            {cat}
          </button>
        ))}
      </div>
      <ul className="flex-1 min-h-0 mt-2 divide-y divide-slate-100 overflow-y-auto">
        {loading ? (
          <li className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Đang tải...</li>
        ) : filtered.length === 0 ? (
          <li className="px-6 py-10 text-center text-[13px] text-slate-400 font-semibold">Chưa có vật tư nào.</li>
        ) : filtered.map(s => (
          <li key={s.id}>
            <button
              type="button"
              onClick={() => onSelect(s)}
              title={`Chọn "${s.name}"`}
              className={`w-full px-6 py-3.5 flex items-center justify-between gap-3 text-left transition-colors cursor-pointer ${
                selectedId === s.id ? "bg-primary/5" : "hover:bg-slate-50/50"
              }`}
            >
              <div className="text-[13.5px] font-bold text-slate-900">{s.name}</div>
              <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11.5px] font-black shrink-0 ${
                s.isLow ? "bg-amber-50 text-amber-700 border border-amber-100" : "bg-green-50 text-green-700 border border-green-100"
              }`}>
                <span className={`w-1.5 h-1.5 rounded-full ${s.isLow ? "bg-amber-500" : "bg-green-500"}`} />
                {s.quantity} {s.unit}
              </span>
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

// ── Styles ─────────────────────────────────────────────────────────────────

const filterSelectCls = "px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-600 appearance-none cursor-pointer pr-8";
const selectCls = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8";
const inputCls  = "w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400";

// ── Main page ──────────────────────────────────────────────────────────────

export default function InventoryPage() {
  useRequireStaff();

  const [tab,           setTab]           = useState<"stock" | "transaction" | "room-export" | "log" | "requests">("stock");
  const [requests,      setRequests]      = useState<MaterialRequestDto[]>([]);
  const [loadingReqs,   setLoadingReqs]   = useState(false);
  const [processingId,  setProcessingId]  = useState<string | null>(null);
  const [priceDrafts,   setPriceDrafts]   = useState<Record<string, string>>({});
  const [priceErrors,   setPriceErrors]   = useState<Record<string, boolean>>({});
  const [actualQtyDrafts, setActualQtyDrafts] = useState<Record<string, string>>({});
  const [confirmingRequest, setConfirmingRequest] = useState<MaterialRequestDto | null>(null);
  const [requestStatusFilter, setRequestStatusFilter] = useState<"all" | "Pending" | "Ordered" | "Done">("all");

  // "Đặt hàng" — chuyển Pending → Ordered, chưa nhập kho
  const [orderingRequest, setOrderingRequest] = useState<MaterialRequestDto | null>(null);
  const [orderNoteDraft,  setOrderNoteDraft]  = useState("");
  const [orderingBusy,    setOrderingBusy]    = useState(false);

  // Modal "+ Tạo yêu cầu mới" — staff tự khởi tạo yêu cầu đặt vật tư riêng cho bệnh nhân
  const [showNewRequestModal, setShowNewRequestModal] = useState(false);
  const [newReqLookup,    setNewReqLookup]    = useState("");
  const [newReqResults,   setNewReqResults]   = useState<PatientSearchResultDto[]>([]);
  const [newReqSearching, setNewReqSearching] = useState(false);
  const [newReqPatient,   setNewReqPatient]   = useState<PatientSearchResultDto | null>(null);
  const [newReqDescription, setNewReqDescription] = useState("");
  const [newReqItems,     setNewReqItems]     = useState<{ id: string; itemName: string; detail: string; quantity: string; unit: string }[]>([
    { id: "0", itemName: "", detail: "", quantity: "1", unit: SUPPLY_UNITS[0] },
  ]);
  const [newReqSubmitting, setNewReqSubmitting] = useState(false);
  const [newReqError,     setNewReqError]     = useState<string | null>(null);
  const [categoryTab,   setCategoryTab]   = useState(ITEM_CATEGORIES[0]);
  const [search,        setSearch]        = useState("");
  const [items,         setItems]         = useState<SupplyItemDto[]>([]);
  const [log,           setLog]           = useState<SupplyTransactionDto[]>([]);
  const [loadingItems,  setLoadingItems]  = useState(false);
  const [loadingLog,    setLoadingLog]    = useState(false);
  const [error,         setError]         = useState<string | null>(null);

  // form state — nhập kho
  const [txItemSearch,   setTxItemSearch]   = useState("");
  const [txItemFocused,  setTxItemFocused]  = useState(false); // dropdown gợi ý tên vật tư khi nhập kho
  const [txSelectedItemId, setTxSelectedItemId] = useState("");
  const [txUnit,         setTxUnit]         = useState("Cái");
  const [txCategory,     setTxCategory]     = useState(QUICK_IMPORT_CATEGORIES[0]);
  const [txQtyStr,       setTxQtyStr]       = useState("");
  const [txPriceStr,     setTxPriceStr]     = useState("");
  const [txNote,         setTxNote]         = useState("");
  const [txErrors,       setTxErrors]       = useState<{ name?: string; unit?: string; qty?: string; price?: string }>({});
  const [submitting,     setSubmitting]     = useState(false);
  const [saved,          setSaved]          = useState(false);

  // form state — xuất kho theo phòng (bác sĩ báo miệng hết đồ giữa ca khám, staff cấp bù trực tiếp cho
  // phòng đó — không cần đi qua "Yêu cầu vật tư" vì đây là hàng dùng chung có sẵn trong kho, không phải
  // đặt riêng theo bệnh nhân).
  const [rooms,          setRooms]          = useState<RoomDto[]>([]);
  const [exportItemId,   setExportItemId]   = useState("");
  const [exportRoomId,   setExportRoomId]   = useState("");
  const [exportQtyStr,   setExportQtyStr]   = useState("");
  const [exportNote,     setExportNote]     = useState("");
  const [exportErrors,   setExportErrors]   = useState<{ item?: string; room?: string; qty?: string }>({});
  const [exportSubmitting, setExportSubmitting] = useState(false);
  const [exportSaved,    setExportSaved]    = useState(false);

  // modal thêm/sửa vật tư — dùng chung 1 form cho cả 2 chế độ
  const [itemModal,      setItemModal]      = useState<{ mode: "add" | "edit"; id?: string } | null>(null);
  const [itemForm,       setItemForm]       = useState<{
    code: string; name: string; category: string; unit: string; quantity: number; minQuantity: number; priceStr: string;
  }>({ code: "", name: "", category: ITEM_CATEGORIES[0], unit: "", quantity: 0, minQuantity: 0, priceStr: "" });
  const [itemSubmitting, setItemSubmitting] = useState(false);
  const [itemError,      setItemError]      = useState<string | null>(null);

  // xóa vật tư
  const [deletingItem,   setDeletingItem]   = useState<SupplyItemDto | null>(null);
  const [deleteBusy,     setDeleteBusy]     = useState(false);

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

  // Bấm 1 dòng trong "Tồn kho hiện tại" (StockPickerPanel) để điền sẵn form nhập/xuất kho bên cạnh — đỡ
  // phải gõ tay/dùng dropdown riêng khi chỉ đơn giản là chọn 1 vật tư đã có sẵn.
  const txQtyInputRef = useRef<HTMLInputElement>(null);
  const exportQtyInputRef = useRef<HTMLInputElement>(null);

  const fetchItems = useCallback(async () => {
    setLoadingItems(true);
    setError(null);
    try {
      const data = await getSupplyItemsApi();
      setItems(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể tải vật tư");
    } finally {
      setLoadingItems(false);
    }
  }, []);

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
  // Dùng được cho cả yêu cầu Pending (bỏ qua bước đặt hàng) lẫn Ordered (đã đặt hàng, hàng vừa về).
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
    // SL thực nhận mặc định = số lượng đã xin, staff có thể sửa trong modal xác nhận nếu nhà cung cấp giao khác.
    setActualQtyDrafts(prev => {
      const next = { ...prev };
      for (const it of r.items) if (next[it.id] === undefined) next[it.id] = String(it.quantity);
      return next;
    });
    setConfirmingRequest(r);
  };

  const handleConfirmImport = async () => {
    const r = confirmingRequest;
    if (!r) return;
    setProcessingId(r.id);
    try {
      const itemPrices = r.items.map(it => ({
        materialRequestItemId: it.id,
        unitPrice: Number(priceDrafts[it.id]),
        actualQuantity: Number(actualQtyDrafts[it.id] ?? it.quantity) || it.quantity,
      }));
      await markMaterialRequestDoneApi(r.id, itemPrices);
      setPriceDrafts(prev => {
        const next = { ...prev };
        for (const it of r.items) delete next[it.id];
        return next;
      });
      setActualQtyDrafts(prev => {
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

  const handleConfirmOrder = async () => {
    const r = orderingRequest;
    if (!r) return;
    setOrderingBusy(true);
    try {
      await markMaterialRequestOrderedApi(r.id, orderNoteDraft.trim() || undefined);
      setOrderingRequest(null);
      setOrderNoteDraft("");
      await fetchRequests();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể đánh dấu đã đặt hàng");
    } finally {
      setOrderingBusy(false);
    }
  };

  // Tra cứu bệnh nhân cho modal "+ Tạo yêu cầu mới" (giống pattern check-in walk-in).
  useEffect(() => {
    const term = newReqLookup.trim();
    if (term.length < 2) { setNewReqResults([]); setNewReqSearching(false); return; }
    let cancelled = false;
    setNewReqSearching(true);
    const timer = setTimeout(() => {
      void searchPatientsApi(term)
        .then(rows => { if (!cancelled) setNewReqResults(rows); })
        .catch(() => { if (!cancelled) setNewReqResults([]); })
        .finally(() => { if (!cancelled) setNewReqSearching(false); });
    }, 300);
    return () => { cancelled = true; clearTimeout(timer); };
  }, [newReqLookup]);

  const resetNewRequestForm = () => {
    setNewReqLookup(""); setNewReqResults([]); setNewReqPatient(null);
    setNewReqDescription("");
    setNewReqItems([{ id: "0", itemName: "", detail: "", quantity: "1", unit: SUPPLY_UNITS[0] }]);
    setNewReqError(null);
  };

  const handleAddNewReqRow = () => {
    setNewReqItems(prev => [...prev, { id: Date.now().toString(), itemName: "", detail: "", quantity: "1", unit: SUPPLY_UNITS[0] }]);
  };

  const handleRemoveNewReqRow = (rowId: string) => {
    setNewReqItems(prev => (prev.length > 1 ? prev.filter(r => r.id !== rowId) : prev));
  };

  const handleNewReqRowChange = (rowId: string, field: "itemName" | "detail" | "quantity" | "unit", val: string) => {
    setNewReqItems(prev => prev.map(r => (r.id === rowId ? { ...r, [field]: val } : r)));
  };

  const handleSubmitNewRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    setNewReqError(null);
    if (!newReqPatient) { setNewReqError("Vui lòng chọn bệnh nhân."); return; }
    if (!newReqDescription.trim()) { setNewReqError("Vui lòng nhập mô tả yêu cầu."); return; }
    const validItems = newReqItems.filter(r => r.itemName.trim());
    if (validItems.length === 0) { setNewReqError("Phải có ít nhất 1 vật tư."); return; }
    for (const it of validItems) {
      const qty = Number(it.quantity);
      if (!qty || qty <= 0) { setNewReqError(`Số lượng của "${it.itemName}" phải lớn hơn 0.`); return; }
    }

    setNewReqSubmitting(true);
    try {
      await createMaterialRequestByStaffApi({
        patientId: newReqPatient.id,
        patientName: newReqPatient.fullName,
        description: newReqDescription.trim(),
        items: validItems.map(it => ({
          itemName: it.itemName.trim(),
          detail: it.detail.trim() || undefined,
          quantity: Number(it.quantity),
          unit: it.unit,
        })),
      });
      setShowNewRequestModal(false);
      resetNewRequestForm();
      await fetchRequests();
    } catch (e) {
      setNewReqError(e instanceof Error ? e.message : "Tạo yêu cầu vật tư thất bại");
    } finally {
      setNewReqSubmitting(false);
    }
  };

  useEffect(() => { fetchItems(); fetchLog(); fetchRequests(); getRoomsApi().then(setRooms).catch(() => {}); }, []);

  // Filter stock
  const filteredStock = items.filter(s => {
    const matchCategory = s.category === categoryTab;
    const matchSearch = !search || s.name.toLowerCase().includes(search.toLowerCase());
    return matchCategory && matchSearch;
  });

  // Gợi ý tên vật tư khi nhập kho — click vào gợi ý sẽ điền tên đó vào ô input
  const txItemSuggestions = txItemSearch.trim()
    ? items.filter(s => s.name.toLowerCase().includes(txItemSearch.trim().toLowerCase())).slice(0, 6)
    : [];
  const showTxItemSuggestions = txItemFocused && !txSelectedItemId && txItemSuggestions.length > 0;

  useEffect(() => { setStockPage(1); }, [search, categoryTab, stockPageSize]);

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

  // Bấm 1 dòng trong "Tồn kho hiện tại" → điền sẵn tên/đơn vị/danh mục vào form nhập kho rồi focus thẳng
  // vào ô số lượng, khỏi phải gõ tay tên vật tư (vẫn có thể tự gõ nếu là vật tư mới, xem showTxItemSuggestions).
  const selectItemForImport = (s: SupplyItemDto) => {
    setTxItemSearch(s.name);
    setTxSelectedItemId(s.id);
    setTxUnit(s.unit);
    setTxCategory(s.category);
    setTxErrors(prev => ({ ...prev, name: undefined }));
    setTxItemFocused(false);
    txQtyInputRef.current?.focus();
  };

  // Bấm 1 dòng trong "Tồn kho hiện tại" của tab Xuất kho theo phòng → chọn thẳng vật tư đó rồi focus vào
  // ô số lượng, khỏi phải dùng dropdown riêng.
  const selectItemForExport = (s: SupplyItemDto) => {
    setExportItemId(s.id);
    setExportErrors(prev => ({ ...prev, item: undefined, qty: undefined }));
    exportQtyInputRef.current?.focus();
  };

  // Form này chỉ còn dùng để Nhập kho — không có xuất kho tự do ở đây. Xuất kho thật luôn có truy vết rõ
  // ràng: qua "Ghi nhận vật tư đã dùng" lúc điều trị (tự trừ kho theo liệu trình), qua "Yêu cầu vật tư"
  // (Vật tư chính, tự động theo dịch vụ), hoặc qua tab "Xuất kho theo phòng" (cấp bù cho phòng khi bác sĩ
  // báo miệng hết đồ giữa ca khám — xem handleExportToRoom).
  const handleTransaction = async (e: React.FormEvent) => {
    e.preventDefault();

    const txQty = Number(txQtyStr);
    const txPrice = txPriceStr ? Number(txPriceStr) : undefined;

    const errors: { name?: string; unit?: string; qty?: string; price?: string } = {};
    if (!txItemSearch.trim()) errors.name = "Vui lòng nhập tên vật tư.";
    if (!txUnit) errors.unit = "Vui lòng chọn đơn vị.";
    if (!txQtyStr || txQty <= 0) errors.qty = "Số lượng phải lớn hơn 0.";
    if (!txPriceStr) errors.price = "Vui lòng nhập đơn giá.";
    else if (Number.isNaN(txPrice) || (txPrice ?? 0) < 0) errors.price = "Đơn giá không hợp lệ.";

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
        unitPrice: Number(txPriceStr),
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
        setTxQtyStr(""); setTxPriceStr(""); setTxNote(""); setTxItemSearch(""); setTxUnit("Cái"); setTxCategory(QUICK_IMPORT_CATEGORIES[0]); setTxSelectedItemId("");
      }, 2000);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Nhập kho thất bại");
    } finally {
      setSubmitting(false);
    }
  };

  // Cấp bù vật tư dùng chung (găng tay, khẩu trang...) trực tiếp cho 1 phòng — dùng khi bác sĩ báo miệng
  // hết đồ giữa ca khám, không cần lập yêu cầu vật tư (đó là dành riêng cho Vật tư chính theo bệnh nhân).
  const handleExportToRoom = async (e: React.FormEvent) => {
    e.preventDefault();

    const exportQty = Number(exportQtyStr);
    const selectedItem = items.find(it => it.id === exportItemId);

    const errors: { item?: string; room?: string; qty?: string } = {};
    if (!exportItemId) errors.item = "Vui lòng chọn vật tư.";
    if (!exportRoomId) errors.room = "Vui lòng chọn phòng.";
    if (!exportQtyStr || exportQty <= 0) errors.qty = "Số lượng phải lớn hơn 0.";
    else if (selectedItem && exportQty > selectedItem.quantity) errors.qty = `Vượt quá tồn kho hiện tại (${selectedItem.quantity}).`;

    if (Object.keys(errors).length > 0) {
      setExportErrors(errors);
      return;
    }
    setExportErrors({});
    setExportSubmitting(true);
    setError(null);
    try {
      const tx = await createSupplyTransactionApi({
        supplyItemId: exportItemId,
        type: "export",
        quantity: exportQty,
        note: exportNote || undefined,
        roomId: exportRoomId,
      });
      setItems(prev => prev.map(it => {
        if (it.id !== exportItemId) return it;
        const newQty = it.quantity - exportQty;
        return { ...it, quantity: newQty, isLow: newQty <= it.minQuantity };
      }));
      setLog(prev => [tx, ...prev]);
      setLogPage(1);
      setExportSaved(true);
      setTimeout(() => {
        setExportSaved(false);
        setExportItemId(""); setExportRoomId(""); setExportQtyStr(""); setExportNote("");
      }, 2000);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Xuất kho thất bại");
    } finally {
      setExportSubmitting(false);
    }
  };

  const openAddItemModal = () => {
    setItemForm({ code: "", name: "", category: ITEM_CATEGORIES[0], unit: "", quantity: 0, minQuantity: 0, priceStr: "" });
    setItemError(null);
    setItemModal({ mode: "add" });
  };

  const openEditItemModal = (item: SupplyItemDto) => {
    setItemForm({
      code: item.code, name: item.name, category: item.category, unit: item.unit,
      quantity: item.quantity, minQuantity: item.minQuantity, priceStr: item.price != null ? String(item.price) : "",
    });
    setItemError(null);
    setItemModal({ mode: "edit", id: item.id });
  };

  const handleSubmitItem = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!itemModal) return;
    setItemSubmitting(true);
    setItemError(null);
    try {
      if (itemModal.mode === "add") {
        const created = await createSupplyItemApi({
          code: itemForm.code.trim(),
          name: itemForm.name.trim(),
          category: itemForm.category,
          unit: itemForm.unit.trim(),
          quantity: itemForm.quantity,
          minQuantity: itemForm.minQuantity,
          price: itemForm.priceStr ? Number(itemForm.priceStr) : undefined,
        });
        setItems(prev => [...prev, created]);
        setCategoryTab(created.category);
      } else {
        const updated = await updateSupplyItemApi(itemModal.id!, {
          name: itemForm.name.trim(),
          category: itemForm.category,
          unit: itemForm.unit.trim(),
          minQuantity: itemForm.minQuantity,
          price: itemForm.priceStr ? Number(itemForm.priceStr) : null,
        });
        setItems(prev => prev.map(it => (it.id === updated.id ? updated : it)));
      }
      setItemModal(null);
    } catch (e) {
      setItemError(e instanceof Error ? e.message : (itemModal.mode === "add" ? "Thêm vật tư thất bại" : "Cập nhật vật tư thất bại"));
    } finally {
      setItemSubmitting(false);
    }
  };

  const handleConfirmDeleteItem = async () => {
    if (!deletingItem) return;
    setDeleteBusy(true);
    try {
      await deleteSupplyItemApi(deletingItem.id);
      setItems(prev => prev.filter(it => it.id !== deletingItem.id));
      setDeletingItem(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Xóa vật tư thất bại");
      setDeletingItem(null);
    } finally {
      setDeleteBusy(false);
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
              { key: "transaction", label: "+ Nhập kho",        count: 0 },
              { key: "room-export", label: "Xuất kho theo phòng", count: 0 },
              { key: "requests",    label: "Yêu cầu vật tư",     count: requests.filter(r => r.status !== "Done").length },
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
              {/* Sub-tab: 3 danh mục vật tư */}
              <div className="flex items-center justify-between gap-2">
                <div className="flex gap-2">
                  {ITEM_CATEGORIES.map(category => {
                    const count = items.filter(i => i.category === category).length;
                    const active = categoryTab === category;
                    return (
                      <button key={category} onClick={() => setCategoryTab(category)}
                        className={`flex items-center gap-2 px-4 py-1.5 rounded-lg text-[12.5px] font-bold transition-all cursor-pointer border ${
                          active ? "bg-slate-800 text-white border-slate-800" : "bg-white text-slate-500 border-slate-200 hover:border-slate-300"
                        }`}>
                        {category}
                        <span className={`px-1.5 py-0.5 rounded-full text-[10.5px] font-black leading-none ${active ? "bg-white/25 text-white" : "bg-slate-100 text-slate-500"}`}>{count}</span>
                      </button>
                    );
                  })}
                </div>
                <button onClick={() => openAddItemModal()}
                  className="flex items-center gap-1.5 px-4 py-1.5 bg-primary hover:bg-red-600 text-white text-[12.5px] font-bold rounded-lg transition-all cursor-pointer shrink-0">
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                  Thêm vật tư mới
                </button>
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
                      <Th className="px-5 w-10">{""}</Th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {loadingItems ? (
                      <tr><td colSpan={6} className="px-5 py-10 text-center text-[13px] text-slate-400 font-semibold">Đang tải...</td></tr>
                    ) : pagedStock.length === 0 ? (
                      <tr><td colSpan={6} className="px-5 py-10 text-center text-[13px] text-slate-400 font-semibold">
                        {items.length === 0 ? "Chưa có vật tư nào trong kho." : "Không tìm thấy vật tư nào."}
                      </td></tr>
                    ) : pagedStock.map(s => (
                      <tr key={s.id} onClick={() => openEditItemModal(s)} className="hover:bg-slate-50/50 transition-colors cursor-pointer">
                        <td className="px-5 py-3.5 font-mono text-[12px] font-black text-slate-400">{s.code}</td>
                        <td className="px-5 py-3.5 font-bold text-slate-900">{s.name}</td>
                        <td className="px-5 py-3.5 text-slate-500 font-semibold">{s.category}</td>
                        <td className="px-5 py-3.5 text-slate-500 font-semibold">{s.unit}</td>
                        <td className="px-5 py-3.5 text-right font-black text-slate-900">{s.quantity}</td>
                        <td className="px-5 py-3.5 text-right">
                          <button
                            onClick={e => { e.stopPropagation(); setDeletingItem(s); }}
                            className="p-1.5 text-slate-300 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                            title="Xóa vật tư này"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" /></svg>
                          </button>
                        </td>
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

          {/* ── Tab: Nhập kho ── */}
          {tab === "transaction" && (
            <div className="flex flex-col gap-5">
            <div className="grid grid-cols-1 lg:grid-cols-5 gap-5 items-start">
              <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-7 flex flex-col gap-5">
                <div>
                  <h2 className="text-[15px] font-black text-slate-900">Tạo phiếu nhập kho</h2>
                  <p className="text-[12px] text-slate-400 font-semibold mt-1">
                    Chỉ dùng cho <strong>Vật tư tiêu hao</strong>/<strong>kỹ thuật-labo</strong> (hàng tồn kho dùng chung).
                    Vật tư chính (đặt riêng theo bệnh nhân) phải qua tab &quot;Yêu cầu vật tư&quot;.
                  </p>
                </div>
                {saved ? (
                  <div className="flex items-center gap-3 bg-green-50 border border-green-100 text-green-700 px-4 py-3 rounded-xl text-[13px] font-bold">
                    <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    Đã cập nhật tồn kho thành công!
                  </div>
                ) : (
                  <form onSubmit={handleTransaction} className="flex flex-col gap-4">
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Vật tư *</label>
                      <div className="relative">
                        <input
                          value={txItemSearch}
                          onChange={e => {
                            setTxItemSearch(e.target.value);
                            setTxErrors(prev => ({ ...prev, name: undefined }));
                            const match = items.find(s => s.name.toLowerCase() === e.target.value.toLowerCase().trim());
                            setTxSelectedItemId(match ? match.id : "");
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
                                  setTxSelectedItemId(s.id);
                                  setTxUnit(s.unit);
                                  setTxCategory(s.category);
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
                      {txItemSearch && !txSelectedItemId && !txErrors.name && (
                        <p className="text-[12px] text-emerald-600 font-semibold">Vật tư mới — sẽ được tạo tự động khi xác nhận.</p>
                      )}
                    </div>

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
                          {QUICK_IMPORT_CATEGORIES.map(c => <option key={c}>{c}</option>)}
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                      </div>
                      <p className="text-[11.5px] text-slate-400 font-semibold">Nếu vật tư đã tồn tại, danh mục trong kho sẽ được giữ nguyên.</p>
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Số lượng *</label>
                      <input type="number" min={0}
                        ref={txQtyInputRef}
                        value={txQtyStr}
                        onChange={e => { setTxQtyStr(e.target.value); setTxErrors(prev => ({ ...prev, qty: undefined })); }}
                        onWheel={e => e.currentTarget.blur()}
                        placeholder="Nhập số lượng..."
                        className={`${inputCls} [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none ${txErrors.qty ? "!border-red-300 focus:!border-red-400 focus:!ring-red-200" : ""}`} />
                      {txErrors.qty && <p className="text-[12px] text-red-500 font-semibold">{txErrors.qty}</p>}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Đơn giá (₫) *</label>
                      <input type="number" min={0}
                        value={txPriceStr}
                        onChange={e => { setTxPriceStr(e.target.value); setTxErrors(prev => ({ ...prev, price: undefined })); }}
                        onWheel={e => e.currentTarget.blur()}
                        placeholder="Giá nhập / 1 đơn vị"
                        className={`${inputCls} [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none ${txErrors.price ? "!border-red-300 focus:!border-red-400 focus:!ring-red-200" : ""}`} />
                      {txErrors.price && <p className="text-[12px] text-red-500 font-semibold">{txErrors.price}</p>}
                      {txPriceStr && txQtyStr && !Number.isNaN(Number(txPriceStr)) && !Number.isNaN(Number(txQtyStr)) && (
                        <p className="text-[12px] text-slate-500 font-semibold">
                          Thành tiền: <span className="font-black text-slate-700">{fmt(Number(txPriceStr) * Number(txQtyStr))}</span>
                        </p>
                      )}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                      <input value={txNote} onChange={e => setTxNote(e.target.value)}
                        placeholder="Nhà cung cấp, lý do nhập..."
                        className={inputCls} />
                    </div>

                    <button type="submit" disabled={submitting}
                      className="flex items-center justify-center gap-2 w-full py-3 rounded-xl text-[14px] font-black transition-all cursor-pointer shadow-sm mt-1 disabled:opacity-60 disabled:cursor-not-allowed bg-emerald-500 hover:bg-emerald-600 text-white shadow-emerald-200">
                      {submitting ? (
                        <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                      ) : (
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      )}
                      Xác nhận nhập kho
                    </button>
                  </form>
                )}
              </div>

              <StockPickerPanel
                items={items}
                loading={loadingItems}
                selectedId={txSelectedItemId}
                onSelect={selectItemForImport}
                hint="Bấm 1 dòng để nhập thêm cho vật tư đó — khỏi phải gõ tay."
              />
            </div>
            </div>
          )}

          {/* ── Tab: Xuất kho theo phòng ── */}
          {tab === "room-export" && (
            <div className="grid grid-cols-1 lg:grid-cols-5 gap-5 items-start">
              <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/60 shadow-sm p-7 flex flex-col gap-5">
                <div>
                  <h2 className="text-[15px] font-black text-slate-900">Xuất kho cho phòng</h2>
                  <p className="text-[12px] text-slate-400 font-semibold mt-1">
                    Dùng khi bác sĩ báo miệng hết vật tư dùng chung (găng tay, khẩu trang...) giữa ca khám —
                    cấp bù trực tiếp cho đúng phòng đó, không cần lập yêu cầu vật tư.
                  </p>
                </div>
                {exportSaved ? (
                  <div className="flex items-center gap-3 bg-green-50 border border-green-100 text-green-700 px-4 py-3 rounded-xl text-[13px] font-bold">
                    <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    Đã xuất kho cho phòng thành công!
                  </div>
                ) : (
                  <form onSubmit={handleExportToRoom} className="flex flex-col gap-4">
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Vật tư *</label>
                      <div className="relative">
                        <select
                          value={exportItemId}
                          onChange={e => { setExportItemId(e.target.value); setExportErrors(prev => ({ ...prev, item: undefined, qty: undefined })); }}
                          className={`${selectCls} ${exportErrors.item ? "!border-red-300" : ""}`}
                        >
                          <option value="">— Chọn vật tư —</option>
                          {items.filter(it => it.category !== CATEGORY_MAIN).sort((a, b) => a.name.localeCompare(b.name)).map(it => (
                            <option key={it.id} value={it.id}>{it.name} (tồn: {it.quantity} {it.unit})</option>
                          ))}
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                      </div>
                      {exportErrors.item && <p className="text-[12px] text-red-500 font-semibold">{exportErrors.item}</p>}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Phòng nhận *</label>
                      <div className="relative">
                        <select
                          value={exportRoomId}
                          onChange={e => { setExportRoomId(e.target.value); setExportErrors(prev => ({ ...prev, room: undefined })); }}
                          className={`${selectCls} ${exportErrors.room ? "!border-red-300" : ""}`}
                        >
                          <option value="">— Chọn phòng —</option>
                          {[...rooms].sort((a, b) => a.name.localeCompare(b.name)).map(r => (
                            <option key={r.id} value={r.id}>{r.name} (tầng {r.floor})</option>
                          ))}
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                      </div>
                      {exportErrors.room && <p className="text-[12px] text-red-500 font-semibold">{exportErrors.room}</p>}
                      {rooms.length === 0 && <p className="text-[11.5px] text-slate-400 font-semibold">Chưa có phòng nào — thêm phòng ở trang Quản lý phòng.</p>}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Số lượng *</label>
                      <input type="number" min={0}
                        ref={exportQtyInputRef}
                        value={exportQtyStr}
                        onChange={e => { setExportQtyStr(e.target.value); setExportErrors(prev => ({ ...prev, qty: undefined })); }}
                        onWheel={e => e.currentTarget.blur()}
                        placeholder="Nhập số lượng..."
                        className={`${inputCls} [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none ${exportErrors.qty ? "!border-red-300 focus:!border-red-400 focus:!ring-red-200" : ""}`} />
                      {exportErrors.qty && <p className="text-[12px] text-red-500 font-semibold">{exportErrors.qty}</p>}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú</label>
                      <input value={exportNote} onChange={e => setExportNote(e.target.value)}
                        placeholder="Vd: BS Thảo báo hết găng tay size M..."
                        className={inputCls} />
                    </div>

                    <button type="submit" disabled={exportSubmitting}
                      className="flex items-center justify-center gap-2 w-full py-3 rounded-xl text-[14px] font-black transition-all cursor-pointer shadow-sm mt-1 disabled:opacity-60 disabled:cursor-not-allowed bg-primary hover:bg-red-600 text-white shadow-red-200">
                      {exportSubmitting ? (
                        <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                      ) : (
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                      )}
                      Xuất kho cho phòng
                    </button>
                  </form>
                )}
              </div>

              <StockPickerPanel
                items={items}
                loading={loadingItems}
                selectedId={exportItemId}
                onSelect={selectItemForExport}
                hint="Bấm 1 dòng để chọn vật tư đó cho lần xuất này."
              />
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
                        <td className="px-5 py-3.5 text-slate-500 font-semibold max-w-xs truncate">
                          {tx.roomName ? `Phòng ${tx.roomName}${tx.note ? ` · ${tx.note}` : ""}` : (tx.note || "—")}
                        </td>
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
              <div className="flex items-center justify-between gap-3 flex-wrap">
                <p className="text-[13px] text-slate-500 font-semibold flex-1 min-w-[280px]">
                  Vật tư đặt riêng cho bệnh nhân: <strong>Chờ xử lý</strong> → (tuỳ chọn) <strong>Đặt hàng</strong> nhà cung cấp/lab →
                  khi hàng về, nhập <strong>đơn giá</strong> + <strong>SL thực nhận</strong> rồi bấm <strong>Nhập kho &amp; Đã xử lý</strong>.
                </p>
                <div className="flex items-center gap-2 shrink-0">
                  <div className="relative">
                    <select value={requestStatusFilter} onChange={e => setRequestStatusFilter(e.target.value as typeof requestStatusFilter)} className={filterSelectCls}>
                      <option value="all">Tất cả trạng thái</option>
                      <option value="Pending">Chờ xử lý</option>
                      <option value="Ordered">Đã đặt hàng</option>
                      <option value="Done">Đã nhập kho</option>
                    </select>
                    <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                  </div>
                  <button onClick={() => { resetNewRequestForm(); setShowNewRequestModal(true); }}
                    className="flex items-center gap-1.5 px-4 py-2 bg-primary hover:bg-red-600 text-white text-[12.5px] font-bold rounded-xl transition-all cursor-pointer whitespace-nowrap">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                    Tạo yêu cầu mới
                  </button>
                </div>
              </div>
              {loadingReqs ? (
                <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm py-16 text-center text-[13px] text-slate-400 font-semibold">Đang tải...</div>
              ) : requests.filter(r => requestStatusFilter === "all" || r.status === requestStatusFilter).length === 0 ? (
                <div className="bg-white rounded-2xl border border-slate-200/70 shadow-sm py-16 text-center text-[13px] text-slate-400 font-semibold">Chưa có yêu cầu vật tư nào.</div>
              ) : (
                requests.filter(r => requestStatusFilter === "all" || r.status === requestStatusFilter).map(r => {
                  const needsAction = r.status === "Pending" || r.status === "Ordered";
                  const statusBadge = r.status === "Pending"
                    ? <span className="text-[11px] font-black px-2 py-0.5 rounded-lg bg-amber-50 text-amber-700 border border-amber-200">Chờ xử lý</span>
                    : r.status === "Ordered"
                    ? <span className="text-[11px] font-black px-2 py-0.5 rounded-lg bg-sky-50 text-sky-700 border border-sky-200">Đã đặt hàng — chờ về</span>
                    : <span className="text-[11px] font-black px-2 py-0.5 rounded-lg bg-green-50 text-green-700 border border-green-200">Đã nhập kho</span>;
                  return (
                  <div key={r.id} className={`bg-white rounded-2xl border shadow-sm px-6 py-4 flex items-start gap-4 ${needsAction ? "border-amber-200" : "border-slate-200/70 opacity-75"}`}>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-[14px] font-black text-slate-900">{r.courseName || "Liệu trình"}</span>
                        {statusBadge}
                      </div>
                      <div className="text-[12px] text-slate-400 font-semibold mt-0.5">
                        BN: {r.patientName}{r.dentistName ? ` · BS: ${r.dentistName}` : " · Staff tự đặt"} · {formatDate(r.createdAt)}
                        {r.orderedBy ? ` · Đặt hàng bởi ${r.orderedBy}` : ""}
                        {r.handledBy ? ` · Nhập kho bởi ${r.handledBy}` : ""}
                      </div>
                      {r.supplierNote && (
                        <div className="text-[12px] text-sky-600 font-semibold mt-0.5 italic">Ghi chú đặt hàng: {r.supplierNote}</div>
                      )}
                      <div className="mt-2 flex flex-col gap-1.5 bg-slate-50 border border-slate-100 rounded-xl px-4 py-3">
                        {r.items.map(it => (
                          <div key={it.id} className="flex items-center gap-3">
                            <div className="text-[13px] font-semibold text-slate-700 flex-1 min-w-0">
                              {it.itemName}
                              {it.detail && <span className="text-slate-400"> — {it.detail}</span>}
                              {" "}× {it.quantity} {it.unit}
                              {it.actualQuantity != null && it.actualQuantity !== it.quantity && (
                                <span className="text-emerald-600"> (thực nhận {it.actualQuantity})</span>
                              )}
                            </div>
                            {needsAction && (
                              <div className="shrink-0 flex items-center gap-3">
                                <div className="flex flex-col items-end">
                                  <div className="flex items-center gap-1">
                                    <span className="text-[12px] text-slate-400 font-semibold">SL nhận</span>
                                    <input
                                      type="number" min={1}
                                      value={actualQtyDrafts[it.id] ?? String(it.quantity)}
                                      onChange={e => setActualQtyDrafts(prev => ({ ...prev, [it.id]: e.target.value }))}
                                      onWheel={e => e.currentTarget.blur()}
                                      className="w-16 px-2 py-1.5 text-[13px] bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-primary font-semibold text-slate-700 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                                    />
                                  </div>
                                </div>
                                <div className="flex flex-col items-end">
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
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                      <MaterialRequestPhotoStrip appointmentId={r.appointmentId} />
                    </div>
                    {needsAction && (
                      <div className="shrink-0 flex flex-col items-stretch gap-2">
                        {r.status === "Pending" && (
                          <button onClick={() => { setOrderingRequest(r); setOrderNoteDraft(""); }}
                            className="flex items-center justify-center gap-2 px-4 py-2 bg-sky-500 hover:bg-sky-600 text-white text-[13px] font-black rounded-xl transition-all shadow-sm shadow-sky-200 cursor-pointer whitespace-nowrap">
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M8.25 18.75a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 01-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 00-3.213-9.193 2.056 2.056 0 00-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.354-1.06-.807-1.325a48.494 48.494 0 00-3.686-1.876c-.86-.286-1.746.343-1.746 1.25v.5" /></svg>
                            Đặt hàng
                          </button>
                        )}
                        <button onClick={() => handleRequestValidateAndConfirm(r)}
                          disabled={processingId === r.id}
                          className="flex items-center justify-center gap-2 px-4 py-2 bg-emerald-500 hover:bg-emerald-600 text-white text-[13px] font-black rounded-xl transition-all shadow-sm shadow-emerald-200 cursor-pointer whitespace-nowrap disabled:opacity-60 disabled:cursor-not-allowed">
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
                  );
                })
              )}
            </div>
          )}
        </div>
      </main>

      {/* ── Modal: Thêm/Sửa vật tư (dùng chung 1 form) ── */}
      {itemModal && (
        <Portal>
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4" onClick={e => { if (e.target === e.currentTarget) setItemModal(null); }}>
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md flex flex-col">
            <div className="flex items-center justify-between px-6 py-5 border-b border-slate-100">
              <h2 className="text-[15px] font-black text-slate-900">{itemModal.mode === "add" ? "Thêm vật tư mới" : "Sửa thông tin vật tư"}</h2>
              <button onClick={() => setItemModal(null)} className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-all cursor-pointer">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <form onSubmit={handleSubmitItem} className="flex flex-col gap-4 px-6 py-5">
              {itemError && (
                <div className="flex items-center gap-2 px-4 py-3 bg-red-50 border border-red-100 rounded-xl text-[13px] font-bold text-red-700">
                  <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" /></svg>
                  {itemError}
                </div>
              )}

              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Mã vật tư *</label>
                  <input required disabled={itemModal.mode === "edit"} value={itemForm.code} onChange={e => setItemForm(f => ({ ...f, code: e.target.value }))}
                    placeholder="VT011" className={`${inputCls} disabled:opacity-60 disabled:cursor-not-allowed`} />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Đơn vị *</label>
                  <input required value={itemForm.unit} onChange={e => setItemForm(f => ({ ...f, unit: e.target.value }))}
                    placeholder="Hộp, Cái, Gói..." className={inputCls} />
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Tên vật tư *</label>
                <input required value={itemForm.name} onChange={e => setItemForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="Tên đầy đủ của vật tư" className={inputCls} />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Danh mục *</label>
                <div className="relative">
                  <select value={itemForm.category} onChange={e => setItemForm(f => ({ ...f, category: e.target.value }))} className={selectCls}>
                    {ITEM_CATEGORIES.map(c => <option key={c}>{c}</option>)}
                  </select>
                  <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400"><svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg></span>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">
                    {itemModal.mode === "add" ? "Tồn kho ban đầu" : "Tồn kho hiện tại"}
                  </label>
                  <input type="number" min={0} disabled={itemModal.mode === "edit"} value={itemForm.quantity}
                    onChange={e => setItemForm(f => ({ ...f, quantity: Number(e.target.value) }))} onWheel={e => e.currentTarget.blur()}
                    className={`${inputCls} disabled:opacity-60 disabled:cursor-not-allowed`} />
                  {itemModal.mode === "edit" && <p className="text-[11px] text-slate-400 font-semibold">Sửa số lượng qua Nhập kho/Yêu cầu vật tư.</p>}
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Tối thiểu</label>
                  <input type="number" min={0} value={itemForm.minQuantity} onChange={e => setItemForm(f => ({ ...f, minQuantity: Number(e.target.value) }))} onWheel={e => e.currentTarget.blur()} className={inputCls} />
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Giá (₫)</label>
                <input type="number" min={0} value={itemForm.priceStr}
                  onChange={e => setItemForm(f => ({ ...f, priceStr: e.target.value }))}
                  onWheel={e => e.currentTarget.blur()}
                  placeholder="Bỏ trống nếu chưa rõ giá" className={inputCls} />
              </div>

              <div className="flex gap-3 pt-1">
                <button type="button" onClick={() => setItemModal(null)}
                  className="flex-1 py-2.5 rounded-xl text-[13.5px] font-bold border border-slate-200 text-slate-600 hover:bg-slate-50 transition-all cursor-pointer">
                  Huỷ
                </button>
                <button type="submit" disabled={itemSubmitting}
                  className="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-[13.5px] font-black bg-primary text-white hover:bg-red-600 transition-all cursor-pointer shadow-sm shadow-primary/20 disabled:opacity-60 disabled:cursor-not-allowed">
                  {itemSubmitting ? (
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  )}
                  {itemModal.mode === "add" ? "Thêm vật tư" : "Lưu thay đổi"}
                </button>
              </div>
            </form>
          </div>
        </div>
        </Portal>
      )}

      {/* ── Modal: Xác nhận xóa vật tư ── */}
      {deletingItem && (
        <Portal>
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4" onClick={e => { if (e.target === e.currentTarget && !deleteBusy) setDeletingItem(null); }}>
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm flex flex-col">
            <div className="px-6 py-5 flex flex-col gap-3">
              <div className="w-11 h-11 rounded-xl bg-red-50 text-red-500 flex items-center justify-center">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" /></svg>
              </div>
              <h2 className="text-[15px] font-black text-slate-900">Xóa vật tư?</h2>
              <p className="text-[13px] text-slate-500 font-semibold">
                Xóa hẳn <strong className="text-slate-800">{deletingItem.name}</strong> khỏi danh mục vật tư. Nếu vật tư đã có giao dịch nhập/xuất hoặc dùng trong định mức của dịch vụ, thao tác sẽ bị từ chối.
              </p>
            </div>
            <div className="px-6 pb-5 flex gap-3">
              <button type="button" onClick={() => setDeletingItem(null)} disabled={deleteBusy}
                className="flex-1 py-2.5 rounded-xl text-[13.5px] font-bold border border-slate-200 text-slate-600 hover:bg-slate-50 transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed">
                Huỷ
              </button>
              <button type="button" onClick={() => void handleConfirmDeleteItem()} disabled={deleteBusy}
                className="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-[13.5px] font-black bg-red-500 text-white hover:bg-red-600 transition-all cursor-pointer shadow-sm shadow-red-200 disabled:opacity-60 disabled:cursor-not-allowed">
                {deleteBusy ? (
                  <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                ) : "Xóa vật tư"}
              </button>
            </div>
          </div>
        </div>
        </Portal>
      )}

      {/* ── Modal: Xác nhận nhập kho theo yêu cầu vật tư ── */}
      {confirmingRequest && (() => {
        const busy = processingId === confirmingRequest.id;
        const qtyOf = (id: string, requested: number) => Number(actualQtyDrafts[id] ?? requested) || requested;
        const total = confirmingRequest.items.reduce((sum, it) => sum + Number(priceDrafts[it.id] ?? 0) * qtyOf(it.id, it.quantity), 0);
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
                        <th className="px-3 py-2 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">SL xin</th>
                        <th className="px-3 py-2 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">SL thực nhận</th>
                        <th className="px-3 py-2 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Đơn giá</th>
                        <th className="px-3 py-2 text-right font-extrabold text-slate-400 text-[11px] uppercase tracking-wider">Thành tiền</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {confirmingRequest.items.map(it => {
                        const price = Number(priceDrafts[it.id] ?? 0);
                        const qty = qtyOf(it.id, it.quantity);
                        return (
                          <tr key={it.id}>
                            <td className="px-3 py-2.5 font-bold text-slate-800">{it.itemName} <span className="text-slate-400 font-semibold">({it.unit})</span></td>
                            <td className="px-3 py-2.5 text-right font-semibold text-slate-600">{it.quantity}</td>
                            <td className={`px-3 py-2.5 text-right font-semibold ${qty !== it.quantity ? "text-emerald-600" : "text-slate-600"}`}>{qty}</td>
                            <td className="px-3 py-2.5 text-right font-semibold text-slate-600">{fmt(price)}</td>
                            <td className="px-3 py-2.5 text-right font-black text-slate-900">{fmt(price * qty)}</td>
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

      {/* ── Modal: Xác nhận đặt hàng (Pending → Ordered, chưa nhập kho) ── */}
      {orderingRequest && (
        <Portal>
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
          onClick={e => { if (e.target === e.currentTarget && !orderingBusy) setOrderingRequest(null); }}>
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md flex flex-col">
            <div className="flex items-center justify-between px-6 py-5 border-b border-slate-100">
              <h2 className="text-[15px] font-black text-slate-900">Đánh dấu đã đặt hàng</h2>
              <button onClick={() => setOrderingRequest(null)} disabled={orderingBusy}
                className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-all cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <div className="flex flex-col gap-4 px-6 py-5">
              <p className="text-[13px] text-slate-500 font-semibold">
                Đánh dấu <strong>{orderingRequest.courseName}</strong> (BN: {orderingRequest.patientName}) đã được đặt hàng với nhà cung cấp/lab.
                Bước này <strong>chưa nhập kho</strong> — chỉ để theo dõi đang chờ hàng về.
              </p>
              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Ghi chú (nhà cung cấp, ngày dự kiến về...)</label>
                <textarea value={orderNoteDraft} onChange={e => setOrderNoteDraft(e.target.value)} rows={2}
                  placeholder="Tuỳ chọn..." className={`${inputCls} resize-none`} />
              </div>
              <div className="flex gap-3 pt-1">
                <button type="button" onClick={() => setOrderingRequest(null)} disabled={orderingBusy}
                  className="flex-1 py-2.5 rounded-xl text-[13.5px] font-bold border border-slate-200 text-slate-600 hover:bg-slate-50 transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed">
                  Huỷ
                </button>
                <button type="button" onClick={() => void handleConfirmOrder()} disabled={orderingBusy}
                  className="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-[13.5px] font-black bg-sky-500 text-white hover:bg-sky-600 transition-all cursor-pointer shadow-sm shadow-sky-200 disabled:opacity-60 disabled:cursor-not-allowed">
                  {orderingBusy ? (
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  )}
                  Xác nhận đã đặt hàng
                </button>
              </div>
            </div>
          </div>
        </div>
        </Portal>
      )}

      {/* ── Modal: Tạo yêu cầu vật tư mới (staff tự khởi tạo) ── */}
      {showNewRequestModal && (
        <Portal>
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
          onClick={e => { if (e.target === e.currentTarget && !newReqSubmitting) setShowNewRequestModal(false); }}>
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[85vh] flex flex-col overflow-hidden">
            <div className="shrink-0 flex items-center justify-between px-6 py-5 border-b border-slate-100">
              <h2 className="text-[15px] font-black text-slate-900">Tạo yêu cầu đặt vật tư riêng cho bệnh nhân</h2>
              <button onClick={() => setShowNewRequestModal(false)} disabled={newReqSubmitting}
                className="w-8 h-8 flex items-center justify-center rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-all cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <form onSubmit={handleSubmitNewRequest} className="flex-1 min-h-0 overflow-y-auto px-6 py-5 flex flex-col gap-4">
              {newReqError && (
                <div className="flex items-center gap-2 px-4 py-3 bg-red-50 border border-red-100 rounded-xl text-[13px] font-bold text-red-700">
                  <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" /></svg>
                  {newReqError}
                </div>
              )}

              {/* Tra cứu bệnh nhân */}
              {newReqPatient ? (
                <div className="flex items-center gap-2.5 p-3 bg-emerald-50 border border-emerald-200 rounded-xl">
                  <div className="w-8 h-8 rounded-xl bg-emerald-100 flex items-center justify-center shrink-0 text-emerald-700 font-black text-[11px]">
                    {newReqPatient.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="text-[12.5px] font-black text-emerald-900 truncate">{newReqPatient.fullName}</div>
                    <div className="text-[11px] text-emerald-700 font-semibold">{newReqPatient.phoneNumber ?? "—"}</div>
                  </div>
                  <button type="button" onClick={() => setNewReqPatient(null)} className="ml-auto text-slate-400 hover:text-red-500 cursor-pointer shrink-0" title="Bỏ chọn, tìm bệnh nhân khác">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                  </button>
                </div>
              ) : (
                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Bệnh nhân *</label>
                  <div className="relative">
                    <span className="absolute inset-y-0 left-3.5 flex items-center pointer-events-none text-slate-400">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                    </span>
                    <input value={newReqLookup} onChange={e => setNewReqLookup(e.target.value)}
                      placeholder="Tên hoặc số điện thoại..." className={`${inputCls} pl-10`} />
                    {newReqSearching && (
                      <span className="absolute inset-y-0 right-3 flex items-center">
                        <span className="w-3.5 h-3.5 border-2 border-slate-200 border-t-slate-400 rounded-full animate-spin" />
                      </span>
                    )}
                  </div>
                  {newReqLookup.trim().length >= 2 && !newReqSearching && newReqResults.length === 0 && (
                    <p className="text-[11.5px] font-semibold text-slate-400">Không tìm thấy bệnh nhân khớp.</p>
                  )}
                  {newReqResults.length > 0 && (
                    <div className="flex flex-col gap-1 max-h-40 overflow-y-auto rounded-xl border border-slate-200 bg-white p-1">
                      {newReqResults.map(p => (
                        <button key={p.id} type="button"
                          onClick={() => { setNewReqPatient(p); setNewReqLookup(""); setNewReqResults([]); }}
                          className="flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-left hover:bg-sky-50 transition-colors cursor-pointer">
                          <div className="w-8 h-8 rounded-lg bg-slate-100 text-slate-600 border border-slate-200 flex items-center justify-center font-black text-[11px] shrink-0">
                            {p.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase()}
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-1.5 flex-wrap">
                              <span className="text-[12.5px] font-bold text-slate-800 truncate">{p.fullName}</span>
                              {p.relationship && (
                                <span className="text-[10px] font-bold px-1.5 py-0.2 rounded bg-indigo-50 text-indigo-700 border border-indigo-100">
                                  {p.relationship}
                                </span>
                              )}
                            </div>
                            <div className="text-[11px] text-slate-400 font-mono">{p.phoneNumber ?? "—"}</div>
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}

              <div className="flex flex-col gap-1.5">
                <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Mô tả yêu cầu *</label>
                <input value={newReqDescription} onChange={e => setNewReqDescription(e.target.value)}
                  placeholder="VD: Đặt răng sứ Zirconia cho ca hẹn tuần sau" className={inputCls} />
              </div>

              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wider">Vật tư cần đặt *</label>
                  <button type="button" onClick={handleAddNewReqRow}
                    className="text-[11.5px] font-bold text-primary hover:underline cursor-pointer">+ Thêm dòng</button>
                </div>
                {newReqItems.map(row => (
                  <div key={row.id} className="flex flex-col gap-1.5 p-2.5 bg-slate-50/80 rounded-xl border border-slate-100">
                    <div className="flex items-center gap-2">
                      <input value={row.itemName} onChange={e => handleNewReqRowChange(row.id, "itemName", e.target.value)}
                        placeholder="Tên vật tư..." className={`${inputCls} flex-1`} />
                      <input type="number" min={1} value={row.quantity} onWheel={e => e.currentTarget.blur()}
                        onChange={e => handleNewReqRowChange(row.id, "quantity", e.target.value)}
                        className={`${inputCls} w-20 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none`} />
                      <div className="relative w-28 shrink-0">
                        <select value={row.unit} onChange={e => handleNewReqRowChange(row.id, "unit", e.target.value)} className={selectCls}>
                          {SUPPLY_UNITS.map(u => <option key={u}>{u}</option>)}
                        </select>
                      </div>
                      <button type="button" onClick={() => handleRemoveNewReqRow(row.id)}
                        className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer shrink-0">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                      </button>
                    </div>
                    <input value={row.detail} onChange={e => handleNewReqRowChange(row.id, "detail", e.target.value)}
                      placeholder="Chi tiết: răng số mấy, hàm nào, kích thước... (tuỳ chọn)"
                      className={`${inputCls} text-[12px]`} />
                  </div>
                ))}
              </div>

              <div className="flex gap-3 pt-1">
                <button type="button" onClick={() => setShowNewRequestModal(false)} disabled={newReqSubmitting}
                  className="flex-1 py-2.5 rounded-xl text-[13.5px] font-bold border border-slate-200 text-slate-600 hover:bg-slate-50 transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed">
                  Huỷ
                </button>
                <button type="submit" disabled={newReqSubmitting}
                  className="flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-[13.5px] font-black bg-primary text-white hover:bg-red-600 transition-all cursor-pointer shadow-sm shadow-primary/20 disabled:opacity-60 disabled:cursor-not-allowed">
                  {newReqSubmitting ? (
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" /></svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>
                  )}
                  Tạo yêu cầu
                </button>
              </div>
            </form>
          </div>
        </div>
        </Portal>
      )}
    </div>
  );
}
