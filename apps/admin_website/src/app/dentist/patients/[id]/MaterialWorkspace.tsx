"use client";

import { useState, useEffect, useCallback } from "react";
import {
  getExaminationApi,
  createMaterialRequestApi,
  getMaterialRequestsByPatientApi,
  getSupplyItemsApi,
  type ExaminationDto,
  type MaterialRequestDto,
  type SupplyItemDto,
} from "../../../../lib/apiClient";
import { Toast, useToast } from "../../../../components/shared/Toast";
import PhotoGallery from "../../../../components/shared/PhotoGallery";
import { SUPPLY_UNITS } from "../../../../lib/inventoryConstants";
import type { DraftMaterialItem } from "./TreatmentWorkspace";

interface MaterialWorkspaceProps {
  appointmentId: string;
  /** Cho phép gửi yêu cầu dù buổi hẹn đã kết thúc (chế độ chỉnh sửa đơn hoàn thành). */
  editMode?: boolean;
  /** Set lại (object mới) mỗi khi tab "Liệu trình" vừa thêm 1 dịch vụ có "Vật tư chính" trong định mức —
   * điền các dòng này vào form "Gửi yêu cầu vật tư" dưới dạng NHÁP, gắn treatmentPlanId để lúc gửi nhóm
   * đúng theo dịch vụ. Không tự gửi gì cả — bác sĩ phải tự bấm "Gửi sang kho". */
  draftToAdd?: { treatmentPlanId: string; items: DraftMaterialItem[] } | null;
}

const fmtDateTime = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()} ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
};

function CardHeader({ title, icon, color, action }: {
  title: string; icon: string; color: string; action?: React.ReactNode;
}) {
  return (
    <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
      <svg className={`w-5 h-5 ${color}`} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
      </svg>
      <span className="text-[14px] font-black text-slate-900">{title}</span>
      {action && <div className="ml-auto">{action}</div>}
    </div>
  );
}

const BOX_ICON = "M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z";

export default function MaterialWorkspace({ appointmentId, editMode = false, draftToAdd }: MaterialWorkspaceProps) {
  const [examination, setExamination] = useState<ExaminationDto | null>(null);
  const [requests, setRequests] = useState<MaterialRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const emptyRow = () => ({ itemName: "", detail: "", quantity: "", unit: SUPPLY_UNITS[0] });
  // Lưu tạm form đang gõ vào localStorage theo từng buổi hẹn — để nếu bác sĩ thoát hẳn khỏi trang (không
  // chỉ chuyển tab) trước khi bấm gửi, quay lại vẫn còn nguyên, không phải gõ/chọn lại từ đầu.
  const draftStorageKey = `materialRequestDraft:${appointmentId}`;
  const [rows, setRows] = useState<{ itemName: string; detail: string; quantity: string; unit: string; treatmentPlanId?: string }[]>(() => {
    if (typeof window === "undefined") return [emptyRow()];
    try {
      const saved = window.localStorage.getItem(draftStorageKey);
      const parsed = saved ? JSON.parse(saved) : null;
      if (Array.isArray(parsed) && parsed.length > 0) return parsed;
    } catch {
      // dữ liệu lưu hỏng/không đọc được — bỏ qua, dùng form trống mặc định
    }
    return [emptyRow()];
  });
  const [saving, setSaving] = useState(false);

  // Đồng bộ form vào localStorage mỗi khi đổi — xoá key khi form trống hẳn để không để rác lại.
  useEffect(() => {
    if (typeof window === "undefined") return;
    const hasContent = rows.some(r => r.itemName.trim() || r.detail.trim() || r.quantity.trim());
    if (hasContent) {
      window.localStorage.setItem(draftStorageKey, JSON.stringify(rows));
    } else {
      window.localStorage.removeItem(draftStorageKey);
    }
  }, [rows, draftStorageKey]);

  // Vật tư "đặt riêng cho bệnh nhân" đã có sẵn trong kho — dùng để gợi ý tên + khoá đơn vị theo kho.
  const [customItems, setCustomItems] = useState<SupplyItemDto[]>([]);
  const [focusedRow, setFocusedRow] = useState<number | null>(null);

  useEffect(() => {
    getSupplyItemsApi({ orderType: "custom" }).then(setCustomItems).catch(() => {});
  }, []);

  const findExactMatch = (itemName: string) =>
    customItems.find(ci => ci.name.toLowerCase() === itemName.trim().toLowerCase());
  const findSuggestions = (itemName: string) =>
    itemName.trim()
      ? customItems.filter(ci => ci.name.toLowerCase().includes(itemName.trim().toLowerCase())).slice(0, 6)
      : [];

  const updateRow = (index: number, patch: Partial<{ itemName: string; detail: string; quantity: string; unit: string }>) => {
    setRows(prev => prev.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  };
  const addRow = () => setRows(prev => [...prev, emptyRow()]);
  const removeRow = (index: number) => setRows(prev => prev.length === 1 ? prev : prev.filter((_, i) => i !== index));

  const effectiveUnit = (row: { itemName: string; unit: string }) => findExactMatch(row.itemName)?.unit ?? row.unit;
  const validRows = rows.filter(r => r.itemName.trim() && Number(r.quantity) > 0);

  const { toast, showToast } = useToast();

  const canEdit = examination?.status === "InProgress" || editMode;

  const loadRequests = useCallback(async (patientId: string, patientName: string) => {
    setRequests(await getMaterialRequestsByPatientApi(patientId, patientName).catch(() => []));
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const exam = await getExaminationApi(appointmentId);
        if (cancelled) return;
        setExamination(exam);
        setError(null);
        await loadRequests(exam.patient.id, exam.patient.fullName);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Không thể tải thông tin bệnh nhân");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [appointmentId, loadRequests]);

  // draftToAdd đổi (object mới mỗi lần) → tab "Liệu trình" vừa thêm dịch vụ có Vật tư chính trong định mức —
  // điền các dòng đó vào form nháp bên dưới thay vì tự gửi. Nếu form đang chỉ có đúng 1 dòng trống mặc định
  // thì thay hẳn dòng đó; ngược lại (bác sĩ đang gõ dở) thì nối thêm vào cuối, không mất nội dung đang nhập.
  useEffect(() => {
    if (!draftToAdd || draftToAdd.items.length === 0) return;
    const newRows = draftToAdd.items.map(i => ({
      itemName: i.itemName,
      detail: i.detail ?? "",
      quantity: String(i.quantity),
      unit: i.unit,
      treatmentPlanId: draftToAdd.treatmentPlanId,
    }));
    setRows(prev => {
      const isSingleEmptyRow = prev.length === 1 && !prev[0].itemName.trim() && !prev[0].detail.trim() && !prev[0].quantity.trim();
      return isSingleEmptyRow ? newRows : [...prev, ...newRows];
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [draftToAdd]);

  const handleSubmit = async () => {
    if (validRows.length === 0 || !examination) return;
    try {
      setSaving(true);
      // Gộp theo dịch vụ (treatmentPlanId) — nếu có dòng gắn dịch vụ và dòng không gắn dịch vụ nào lẫn
      // trong cùng lượt gửi thì tách thành các MaterialRequest riêng để liên kết đúng dịch vụ.
      const groups = new Map<string, typeof validRows>();
      for (const r of validRows) {
        const key = r.treatmentPlanId ?? "";
        groups.set(key, [...(groups.get(key) ?? []), r]);
      }
      for (const [treatmentPlanId, groupRows] of groups) {
        await createMaterialRequestApi({
          appointmentId,
          items: groupRows.map(r => ({
            itemName: r.itemName.trim(),
            detail: r.detail.trim() || undefined,
            quantity: Number(r.quantity),
            unit: effectiveUnit(r),
          })),
          treatmentPlanId: treatmentPlanId || undefined,
        });
      }
      showToast("Đã gửi yêu cầu vật tư sang kho");
      setRows([emptyRow()]);
      await loadRequests(examination.patient.id, examination.patient.fullName);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể gửi yêu cầu", "error");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center py-24">
        <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !examination) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-4 py-24">
        <p className="text-[14px] font-semibold text-red-500">{error ?? "Đã xảy ra lỗi hệ thống."}</p>
      </div>
    );
  }

  const pt = examination.patient;
  const initials = pt.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();

  return (
    <div>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
        {/* ══════════ LEFT 2/3: PATIENT + REQUEST HISTORY ══════════ */}
        <div className="lg:col-span-2 flex flex-col gap-6">
          {/* Thông tin bệnh nhân */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-sky-600"
              title="Thông tin bệnh nhân"
              icon="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"
            />
            <div className="p-5 flex flex-col sm:flex-row items-start gap-4">
              <div className={`w-14 h-14 sm:w-16 sm:h-16 rounded-2xl flex items-center justify-center font-black text-lg sm:text-xl border-2 shrink-0 ${pt.gender === "Nữ" ? "bg-rose-50 text-rose-500 border-rose-100" : "bg-sky-50 text-sky-600 border-sky-100"}`}>
                {initials}
              </div>
              <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-3 w-full">
                {[
                  ["Họ tên", pt.fullName],
                  ["Giới tính", pt.gender ?? "—"],
                  ["Điện thoại", pt.phoneNumber ?? "—"],
                  ["Dịch vụ", examination.serviceName ?? "Khám tổng quát"],
                ].map(([label, value]) => (
                  <div key={label}>
                    <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{label}</div>
                    <div className="text-[13px] font-bold text-slate-800 mt-0.5 break-words">{value}</div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Lịch sử yêu cầu vật tư */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-amber-600"
              title="Yêu cầu vật tư đã gửi"
              icon={BOX_ICON}
              action={
                <span className="text-[11px] font-bold px-2.5 py-1 bg-amber-50 text-amber-700 rounded-lg border border-amber-200">{requests.length} yêu cầu</span>
              }
            />
            <div className="p-5 max-h-[360px] overflow-y-auto">
              {requests.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-6">Chưa có yêu cầu vật tư nào cho bệnh nhân này.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  {requests.map(r => (
                    <div key={r.id} className="py-3.5">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-[13px] font-black text-slate-800">{r.courseName || "Yêu cầu vật tư"}</span>
                        <span className={`text-[10.5px] font-black px-2 py-0.5 rounded-md uppercase tracking-wide ${r.status === "Done" ? "bg-emerald-50 text-emerald-700 border border-emerald-200" : "bg-amber-50 text-amber-700 border border-amber-200"}`}>
                          {r.status === "Done" ? "Đã xử lý" : r.status === "Ordered" ? "Đã đặt hàng" : "Chờ kho xử lý"}
                        </span>
                        <span className="ml-auto text-[11.5px] font-semibold text-slate-400 font-mono">{fmtDateTime(r.createdAt)}</span>
                      </div>
                      <div className="flex flex-col gap-1 bg-slate-50 border border-slate-100 rounded-xl px-3.5 py-2.5">
                        {r.items.map(it => (
                          <div key={it.id} className="text-[13px] font-semibold text-slate-600">
                            {it.itemName}{it.detail ? ` — ${it.detail}` : ""} × {it.quantity} {it.unit}
                          </div>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Ảnh đính kèm — dấu răng, răng lợi... phục vụ yêu cầu vật tư */}
          <PhotoGallery appointmentId={appointmentId} section="material-request" title="Ảnh đính kèm (dấu răng, răng lợi...)" canEdit={canEdit} />
        </div>

        {/* ══════════ RIGHT 1/3: NEW REQUEST FORM ══════════ */}
        <div className="lg:col-span-1 bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
          <CardHeader color="text-emerald-600" title="Gửi yêu cầu vật tư" icon={BOX_ICON} />
          <div className="p-5 flex flex-col gap-4">
            <div className="flex flex-col gap-2.5">
              <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Danh sách vật tư cần dùng</label>
              {rows.map((row, i) => {
                const exactMatch = findExactMatch(row.itemName);
                const suggestions = findSuggestions(row.itemName);
                const showSuggestions = canEdit && focusedRow === i && !exactMatch && suggestions.length > 0;
                return (
                <div key={i} className="flex flex-col gap-1">
                  <div className="flex items-start gap-1.5">
                    <div className="relative flex-[2] min-w-0">
                      <input
                        value={row.itemName}
                        onChange={e => updateRow(i, { itemName: e.target.value })}
                        onFocus={() => setFocusedRow(i)}
                        onBlur={() => setTimeout(() => setFocusedRow(f => (f === i ? null : f)), 150)}
                        placeholder="Tên vật tư"
                        disabled={!canEdit}
                        className="w-full px-3 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 disabled:opacity-60 disabled:cursor-not-allowed"
                      />
                      {showSuggestions && (
                        <div className="absolute z-20 top-full left-0 right-0 mt-1 bg-white border border-slate-200 rounded-xl shadow-lg overflow-hidden">
                          {suggestions.map(s => (
                            <button
                              key={s.id}
                              type="button"
                              onMouseDown={e => e.preventDefault()}
                              onClick={() => { updateRow(i, { itemName: s.name, unit: s.unit }); setFocusedRow(null); }}
                              className="w-full flex items-center justify-between gap-2 px-3 py-2 text-left hover:bg-slate-50 transition-colors cursor-pointer"
                            >
                              <span className="text-[12.5px] font-semibold text-slate-700 truncate">{s.name}</span>
                              <span className="text-[11px] font-bold text-slate-400 shrink-0">{s.unit}</span>
                            </button>
                          ))}
                        </div>
                      )}
                    </div>
                    <input
                      type="number" min={0}
                      value={row.quantity}
                      onChange={e => updateRow(i, { quantity: e.target.value })}
                      placeholder="SL"
                      disabled={!canEdit}
                      className="w-16 shrink-0 px-2.5 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 disabled:opacity-60 disabled:cursor-not-allowed [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                    />
                    <select
                      value={exactMatch ? exactMatch.unit : row.unit}
                      onChange={e => updateRow(i, { unit: e.target.value })}
                      disabled={!canEdit || !!exactMatch}
                      title={exactMatch ? "Vật tư đã có trong kho — dùng đơn vị đã lưu" : undefined}
                      className="w-[76px] shrink-0 px-2 py-2 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 disabled:opacity-60 disabled:cursor-not-allowed"
                    >
                      {SUPPLY_UNITS.map(u => <option key={u} value={u}>{u}</option>)}
                    </select>
                    <button
                      type="button"
                      onClick={() => removeRow(i)}
                      disabled={!canEdit || rows.length === 1}
                      title="Xoá dòng"
                      className="w-9 h-9 shrink-0 flex items-center justify-center rounded-xl border border-slate-200 text-slate-400 hover:text-red-500 hover:border-red-200 transition-all disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                    </button>
                  </div>
                  <input
                    value={row.detail}
                    onChange={e => updateRow(i, { detail: e.target.value })}
                    placeholder="Chi tiết: răng số mấy, hàm nào, kích thước... (tuỳ chọn)"
                    disabled={!canEdit}
                    className="w-full px-3 py-1.5 text-[12px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600 disabled:opacity-60 disabled:cursor-not-allowed"
                  />
                  {exactMatch ? (
                    <p className="text-[11px] font-semibold text-emerald-600 pl-0.5">✓ Vật tư đã có trong kho — đơn vị: {exactMatch.unit}</p>
                  ) : row.itemName.trim() && (
                    <p className="text-[11px] font-semibold text-slate-400 pl-0.5">Vật tư mới</p>
                  )}
                </div>
                );
              })}
              <button
                type="button"
                onClick={addRow}
                disabled={!canEdit}
                className="self-start text-[12.5px] font-bold text-emerald-600 hover:text-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
              >
                + Thêm vật tư
              </button>
            </div>

            <button
              onClick={() => void handleSubmit()}
              disabled={!canEdit || validRows.length === 0 || saving}
              title={canEdit ? undefined : "Chỉ gửi yêu cầu khi buổi hẹn đang khám"}
              className="w-full py-3 bg-emerald-600 text-white text-[14px] font-black rounded-xl hover:bg-emerald-700 transition-all shadow-sm shadow-emerald-500/25 disabled:bg-slate-200 disabled:text-slate-400 disabled:shadow-none disabled:cursor-not-allowed cursor-pointer"
            >
              {saving ? "Đang gửi..." : "Gửi sang kho"}
            </button>

            <p className="text-[11.5px] font-semibold text-slate-400 leading-relaxed">
              Vật tư chính của dịch vụ vừa thêm ở tab &quot;Liệu trình&quot; được điền sẵn ở đây dưới dạng
              nháp — kiểm tra/sửa số lượng rồi bấm &quot;Gửi sang kho&quot; để thật sự gửi yêu cầu.
            </p>
            {!canEdit && (
              <p className="text-[11.5px] font-semibold text-amber-600 text-center">
                Buổi hẹn chưa ở trạng thái đang khám — chỉ xem.
              </p>
            )}
          </div>
        </div>
      </div>

      <Toast toast={toast} />
    </div>
  );
}
