"use client";

import React, { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import AdminSidebar from "@/src/components/shared/AdminSidebar";
import AdminPageHeader from "@/src/components/shared/AdminPageHeader";
import RichTextEditor from "@/src/components/shared/RichTextEditor";
import { ConfirmDialog, useConfirm } from "@/src/components/shared/ConfirmDialog";
import { useRequireAdmin } from "@/src/hooks/useRequireAdmin";
import {
  createServiceApi,
  updateServiceProceduresApi,
  updateServiceSupplyItemsApi,
  getSupplyItemsApi,
  SupplyItemDto,
  uploadFileApi,
  resolveAssetUrl,
} from "@/src/lib/apiClient";

// Mỗi option (Titan, Zirconia...) có DANH SÁCH VẬT TƯ RIÊNG của nó — vì option khác nhau có thể
// cần vật tư khác nhau (option A cần vật tư X, option B lại cần vật tư Y chứ không phải X).
interface OptionSupplyRow {
  id: string;
  supplyItemId: string;
  quantity: string;
}

interface ServiceOptionRow {
  id: string;
  name: string;
  price: string;
  unit: string;
  /** Danh sách vật tư riêng cho option này — không dùng chung với các option khác. */
  supplies: OptionSupplyRow[];
}

interface ProcedureStepRow {
  id: string;
  name: string;
}

export default function AddServicePage() {
  useRequireAdmin();
  const router = useRouter();
  const { confirm, confirmState, closeConfirm } = useConfirm();

  // Basic Info state
  const [formName, setFormName] = useState("");
  const [formPrice, setFormPrice] = useState("");
  const [formDuration, setFormDuration] = useState("");
  const [formDescription, setFormDescription] = useState("");
  const [formContent, setFormContent] = useState("");

  // Options state
  const [options, setOptions] = useState<ServiceOptionRow[]>([]);
  const [selectedOptionId, setSelectedOptionId] = useState<string | null>(null);

  // Treatment procedure steps state
  const [steps, setSteps] = useState<ProcedureStepRow[]>([]);

  const [catalogItems, setCatalogItems] = useState<SupplyItemDto[]>([]);

  useEffect(() => {
    getSupplyItemsApi().then(setCatalogItems).catch(() => {});
  }, []);

  // Images state
  const [uploadedImage, setUploadedImage] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Icon state
  const [iconUrl, setIconUrl] = useState<string | null>(null);
  const [isUploadingIcon, setIsUploadingIcon] = useState(false);
  const iconInputRef = useRef<HTMLInputElement>(null);

  // Submit state
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  // Handlers for Options
  const handleAddOption = () => {
    const newId = Date.now().toString();
    setOptions((prev) => [
      ...prev,
      { id: newId, name: "", price: "", unit: "Răng", supplies: [] },
    ]);
    setSelectedOptionId(newId);
  };

  const handleRemoveOption = async (id: string) => {
    const opt = options.find((o) => o.id === id);
    // Option chưa nhập gì (tên rỗng, chưa gắn vật tư) thì bỏ luôn, không cần hỏi.
    if (opt && (opt.name.trim() !== "" || opt.supplies.length > 0)) {
      const ok = await confirm({
        title: "Xóa option này?",
        message: opt.supplies.length > 0
          ? `Option "${opt.name.trim() || "chưa đặt tên"}" và ${opt.supplies.length} vật tư riêng của nó sẽ bị xóa.`
          : `Option "${opt.name.trim() || "chưa đặt tên"}" sẽ bị xóa.`,
        confirmLabel: "Xóa option",
      });
      if (!ok) return;
    }
    setOptions((prev) => {
      const next = prev.filter((o) => o.id !== id);
      if (selectedOptionId === id) {
        setSelectedOptionId(next.length > 0 ? next[0].id : null);
      }
      return next;
    });
  };

  const handleOptionChange = (id: string, field: "name" | "price" | "unit", val: string) => {
    setOptions((prev) =>
      prev.map((opt) => {
        if (opt.id !== id) return opt;
        if (field === "price") {
          const raw = val.replace(/[^0-9]/g, "");
          return {
            ...opt,
            price: raw ? parseInt(raw).toLocaleString("vi-VN") : "",
          };
        }
        return { ...opt, [field]: val };
      })
    );
  };

  // Vật tư riêng theo từng option — handlers
  const handleAddOptionSupply = (id: string) => {
    setOptions((prev) =>
      prev.map((opt) =>
        opt.id === id
          ? { ...opt, supplies: [...opt.supplies, { id: Date.now().toString(), supplyItemId: "", quantity: "1" }] }
          : opt
      )
    );
  };

  const handleRemoveOptionSupply = async (id: string, rowId: string) => {
    const row = options.find((o) => o.id === id)?.supplies.find((r) => r.id === rowId);
    if (row?.supplyItemId) {
      const ok = await confirm({ title: "Bỏ vật tư này khỏi option?", confirmLabel: "Bỏ vật tư" });
      if (!ok) return;
    }
    setOptions((prev) =>
      prev.map((opt) =>
        opt.id === id ? { ...opt, supplies: opt.supplies.filter((r) => r.id !== rowId) } : opt
      )
    );
  };

  const handleOptionSupplyChange = (id: string, rowId: string, field: "supplyItemId" | "quantity", val: string) => {
    setOptions((prev) =>
      prev.map((opt) =>
        opt.id === id
          ? { ...opt, supplies: opt.supplies.map((r) => (r.id === rowId ? { ...r, [field]: val } : r)) }
          : opt
      )
    );
  };

  // Handlers for Procedure Steps
  const handleAddStep = () => {
    setSteps((prev) => [
      ...prev,
      { id: Date.now().toString(), name: "" },
    ]);
  };

  const handleRemoveStep = (id: string) => {
    setSteps((prev) => prev.filter((step) => step.id !== id));
  };

  const handleStepChange = (id: string, val: string) => {
    setSteps((prev) =>
      prev.map((step) => (step.id === id ? { ...step, name: val } : step))
    );
  };

  // Handlers for Icon & Image
  const handleIconChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setIsUploadingIcon(true);
    try {
      const result = await uploadFileApi(file);
      setIconUrl(result.url);
    } catch (err) {
      alert(err instanceof Error ? err.message : "Tải icon lên thất bại");
    } finally {
      setIsUploadingIcon(false);
      if (iconInputRef.current) iconInputRef.current.value = "";
    }
  };

  const handleImageFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setIsUploadingImage(true);
    try {
      const result = await uploadFileApi(file);
      setUploadedImage(result.url);
    } catch (err) {
      alert(err instanceof Error ? err.message : "Tải hình ảnh thất bại");
    } finally {
      setIsUploadingImage(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file && file.type.startsWith("image/")) {
      setIsUploadingImage(true);
      try {
        const result = await uploadFileApi(file);
        setUploadedImage(result.url);
      } catch (err) {
        alert(err instanceof Error ? err.message : "Tải hình ảnh thất bại");
      } finally {
        setIsUploadingImage(false);
      }
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaveError(null);

    const rawPrice = formPrice.replace(/[^0-9]/g, "");
    if (!formName || !rawPrice || !formDuration) {
      setSaveError("Vui lòng điền đầy đủ thông tin bắt buộc.");
      return;
    }

    const validOptions = options
      .filter((opt) => opt.name.trim() !== "")
      .map((opt, index) => ({
        name: opt.name.trim(),
        price: parseInt(opt.price.replace(/[^0-9]/g, "")) || 0,
        unit: opt.unit.trim() || "Răng",
        sortOrder: index,
      }));

    const validSteps = steps
      .filter((s) => s.name.trim() !== "")
      .map((s, index) => ({
        stepNumber: index + 1,
        name: s.name.trim(),
      }));

    // Vật tư riêng của từng option (1 option có thể có nhiều vật tư khác nhau).
    const validSupplyItems = options
      .filter((opt) => opt.name.trim() !== "")
      .flatMap((opt) =>
        opt.supplies
          .filter((s) => s.supplyItemId)
          .map((s) => ({
            supplyItemId: s.supplyItemId,
            defaultQuantity: parseInt(s.quantity) || 1,
            serviceOptionName: opt.name.trim(),
          }))
      );

    if (new Set(validSupplyItems.map((r) => `${r.supplyItemId}::${r.serviceOptionName}`)).size !== validSupplyItems.length) {
      setSaveError("Không được chọn trùng một vật tư nhiều lần trong cùng một option.");
      return;
    }

    setIsSaving(true);
    try {
      const createdService = await createServiceApi({
        name: formName,
        price: parseInt(rawPrice),
        durationMinutes: parseInt(formDuration),
        description: formDescription,
        content: formContent,
        imageUrl: uploadedImage,
        iconUrl,
        options: validOptions,
      });

      if (validSteps.length > 0) {
        await updateServiceProceduresApi(createdService.id, validSteps);
      }

      if (validSupplyItems.length > 0) {
        await updateServiceSupplyItemsApi(createdService.id, validSupplyItems);
      }

      router.push("/admin/services");
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : "Thêm dịch vụ thất bại.");
    } finally {
      setIsSaving(false);
    }
  };

  const selectedOption = options.find((opt) => opt.id === selectedOptionId) ?? null;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="services" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <AdminPageHeader
          title="Thêm dịch vụ"
          subtitle="Thêm dịch vụ mới cùng tùy chọn giá, các bước thực hiện & bài viết chi tiết."
          left={
            <Link
              href="/admin/services"
              className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
          }
        />

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto">
          <form onSubmit={handleSave} className="max-w-7xl mx-auto flex flex-col gap-6">

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          <div className="lg:col-span-7 flex flex-col gap-6">

            {/* CARD: THÔNG TIN CƠ BẢN */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                <h3 className="text-[15px] font-extrabold text-slate-700 flex items-center gap-2">
                  <span className="w-2.5 h-2.5 rounded-full bg-primary" />
                  Thông tin dịch vụ
                </h3>
              </div>

              <div className="p-6 flex flex-col gap-5">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1.5 sm:col-span-2">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Tên dịch vụ <span className="text-primary">*</span>
                    </label>
                    <input
                      type="text"
                      required
                      placeholder="Nhập tên dịch vụ (vd: Bọc răng sứ)..."
                      value={formName}
                      onChange={(e) => setFormName(e.target.value)}
                      className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                    />
                  </div>

                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Giá khởi điểm (Từ) <span className="text-primary">*</span>
                    </label>
                    <div className="relative">
                      <input
                        type="text"
                        required
                        placeholder="0"
                        value={formPrice}
                        onChange={(e) => {
                          const raw = e.target.value.replace(/[^0-9]/g, "");
                          setFormPrice(raw ? parseInt(raw).toLocaleString("vi-VN") : "");
                        }}
                        className="w-full px-4 py-3 pr-12 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                      <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-[12px] text-slate-400 font-bold">
                        VNĐ
                      </span>
                    </div>
                  </div>

                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Thời gian thực hiện <span className="text-primary">*</span>
                    </label>
                    <div className="relative">
                      <input
                        type="number"
                        required
                        min="5"
                        max="300"
                        placeholder="30"
                        value={formDuration}
                        onChange={(e) => setFormDuration(e.target.value)}
                        className="w-full px-4 py-3 pr-12 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                      <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-[12px] text-slate-400 font-bold">
                        phút
                      </span>
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Ảnh bìa dịch vụ
                    </label>
                    {uploadedImage ? (
                      <div className="relative rounded-xl overflow-hidden border border-slate-200 group h-[60px] bg-slate-100">
                        <img
                          src={resolveAssetUrl(uploadedImage)}
                          alt="Preview"
                          className="w-full h-full object-cover"
                        />
                        <div className="absolute inset-0 bg-slate-900/60 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                          <button
                            type="button"
                            onClick={() => fileInputRef.current?.click()}
                            className="p-1.5 bg-white rounded-lg text-slate-700 hover:bg-slate-100 transition-all cursor-pointer"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99" />
                            </svg>
                          </button>
                          <button
                            type="button"
                            onClick={() => setUploadedImage(null)}
                            className="p-1.5 bg-red-500 rounded-lg text-white hover:bg-red-600 transition-all cursor-pointer"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                          </button>
                        </div>
                      </div>
                    ) : (
                      <div
                        onDrop={handleDrop}
                        onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
                        onDragLeave={() => setIsDragging(false)}
                        onClick={() => fileInputRef.current?.click()}
                        className={`h-[60px] rounded-xl border-2 border-dashed transition-all cursor-pointer flex items-center gap-2 px-3 ${
                          isDragging ? "border-primary bg-primary/5" : "border-slate-200 hover:border-primary/50 hover:bg-slate-50"
                        }`}
                      >
                        <input
                          ref={fileInputRef}
                          type="file"
                          accept="image/*"
                          onChange={handleImageFileChange}
                          className="hidden"
                        />
                        <div className="w-7 h-7 rounded-lg bg-slate-100 text-slate-400 flex items-center justify-center shrink-0">
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                          </svg>
                        </div>
                        <span className="text-[12px] font-semibold text-slate-500">
                          {isUploadingImage ? "Đang tải..." : "Tải ảnh lên"}
                        </span>
                      </div>
                    )}
                  </div>

                  <div className="flex flex-col gap-1.5">
                    <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Icon (SVG/App)
                    </label>
                    <div className="flex items-center gap-2 h-[60px]">
                      <div className="w-[60px] h-[60px] rounded-xl border border-slate-200 bg-slate-50 flex items-center justify-center overflow-hidden shrink-0">
                        {iconUrl ? (
                          <img src={resolveAssetUrl(iconUrl)} alt="Icon" className="w-7 h-7 object-contain" />
                        ) : (
                          <svg className="w-5 h-5 text-slate-300" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                          </svg>
                        )}
                      </div>
                      <input
                        ref={iconInputRef}
                        type="file"
                        accept=".svg,image/svg+xml,image/*"
                        onChange={handleIconChange}
                        className="hidden"
                      />
                      <button
                        type="button"
                        onClick={() => iconInputRef.current?.click()}
                        disabled={isUploadingIcon}
                        className="px-3 py-2 text-[12px] font-bold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-xl transition-all cursor-pointer disabled:opacity-60"
                      >
                        {isUploadingIcon ? "..." : iconUrl ? "Đổi" : "+ Upload"}
                      </button>
                    </div>
                  </div>
                </div>

                <div className="flex flex-col gap-1.5">
                  <label className="text-[12px] font-extrabold text-slate-500 uppercase tracking-wide">
                    Mô tả ngắn (Hiển thị ở danh sách)
                  </label>
                  <textarea
                    rows={2}
                    placeholder="Nhập tóm tắt ngắn về dịch vụ..."
                    value={formDescription}
                    onChange={(e) => setFormDescription(e.target.value)}
                    className="w-full px-4 py-2.5 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300 resize-none"
                  />
                </div>
              </div>
            </div>

          </div>
          <div className="lg:col-span-5 flex flex-col gap-6">

            {/* CARD: QUY TRÌNH ĐIỀU TRỊ */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between">
                <div>
                  <h3 className="text-[15px] font-extrabold text-slate-700 flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-blue-500" />
                    Quy trình điều trị (Các bước thực hiện)
                  </h3>
                  <p className="text-[12px] text-slate-400 mt-0.5">Các bước khám & điều trị chuẩn cho dịch vụ này</p>
                </div>
                <button
                  type="button"
                  onClick={handleAddStep}
                  className="px-3 py-1.5 text-[11px] font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 rounded-lg transition-all cursor-pointer flex items-center gap-1 shrink-0 border border-blue-200/60"
                >
                  + Thêm bước
                </button>
              </div>

              <div className="p-6">
                {steps.length === 0 ? (
                  <div className="py-4 text-center border border-dashed border-slate-200 rounded-xl bg-slate-50/50">
                    <p className="text-[12px] font-semibold text-slate-400">Chưa có quy trình điều trị nào.</p>
                    <button
                      type="button"
                      onClick={handleAddStep}
                      className="mt-1 text-[12px] font-bold text-blue-600 hover:underline cursor-pointer"
                    >
                      + Thêm bước thực hiện đầu tiên
                    </button>
                  </div>
                ) : (
                  <div className="flex flex-col gap-2">
                    {steps.map((step, idx) => (
                      <div
                        key={step.id}
                        className="flex items-center gap-2 p-2 bg-slate-50/90 rounded-xl border border-slate-200/80"
                      >
                        <span className="w-6 h-6 rounded-md bg-blue-100 text-blue-700 text-[11px] font-extrabold flex items-center justify-center shrink-0">
                          {idx + 1}
                        </span>
                        <input
                          type="text"
                          placeholder={`Bước ${idx + 1}: Tên bước (vd: Thăm khám & chụp X-quang)...`}
                          value={step.name}
                          onChange={(e) => handleStepChange(step.id, e.target.value)}
                          className="flex-1 px-2.5 py-1 text-[12px] bg-white border border-slate-200 rounded-lg focus:border-blue-500 focus:outline-none font-semibold text-slate-800 placeholder:text-slate-300"
                        />
                        <button
                          type="button"
                          onClick={() => handleRemoveStep(step.id)}
                          className="p-1 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                          title="Xóa bước này"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                          </svg>
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

          </div>
          </div>

            {/* CARD: TÙY CHỌN PHÂN LOẠI + VẬT TƯ RIÊNG THEO TỪNG OPTION (master-detail) */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between">
                <div>
                  <h3 className="text-[15px] font-extrabold text-slate-700 flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-emerald-500" />
                    Tùy chọn phân loại & Vật tư riêng
                  </h3>
                  <p className="text-[12px] text-slate-400 mt-0.5">Chọn 1 option bên trái để xem/chỉnh sửa vật tư riêng của option đó bên phải.</p>
                </div>
                <button
                  type="button"
                  onClick={handleAddOption}
                  className="px-3 py-1.5 text-[11px] font-bold text-primary bg-primary/10 hover:bg-primary/20 rounded-lg transition-all cursor-pointer flex items-center gap-1 shrink-0"
                >
                  + Thêm Option
                </button>
              </div>

              {options.length === 0 ? (
                <div className="p-6">
                  <div className="py-4 text-center border border-dashed border-slate-200 rounded-xl bg-slate-50/50">
                    <p className="text-[12px] font-semibold text-slate-400">Chưa thêm phân loại nào.</p>
                    <button
                      type="button"
                      onClick={handleAddOption}
                      className="mt-1 text-[12px] font-bold text-primary hover:underline cursor-pointer"
                    >
                      + Thêm tùy chọn giá đầu tiên
                    </button>
                  </div>
                </div>
              ) : (
                <div className="flex flex-col md:flex-row">
                  {/* DANH SÁCH OPTION (bên trái) */}
                  <div className="md:w-64 shrink-0 border-b md:border-b-0 md:border-r border-slate-100 p-4 flex flex-col gap-1.5 max-h-[420px] overflow-y-auto">
                    {options.map((opt, idx) => {
                      const active = opt.id === selectedOptionId;
                      return (
                        <button
                          type="button"
                          key={opt.id}
                          onClick={() => setSelectedOptionId(opt.id)}
                          className={`text-left px-3 py-2.5 rounded-xl border transition-all group ${
                            active ? "border-primary bg-primary/5 shadow-sm" : "border-slate-200/80 bg-slate-50/60 hover:border-slate-300"
                          }`}
                        >
                          <div className="flex items-center justify-between gap-2">
                            <span className="flex items-center gap-1.5 min-w-0">
                              <span className="w-5 h-5 rounded bg-slate-200 text-slate-600 text-[10px] font-black flex items-center justify-center shrink-0">
                                {idx + 1}
                              </span>
                              <span className="text-[12.5px] font-bold text-slate-800 truncate">
                                {opt.name.trim() || `Option ${idx + 1}`}
                              </span>
                            </span>
                            <span
                              onClick={(e) => {
                                e.stopPropagation();
                                void handleRemoveOption(opt.id);
                              }}
                              className="p-0.5 text-slate-300 group-hover:text-red-500 rounded transition-colors cursor-pointer shrink-0"
                              title="Xóa option này"
                            >
                              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                              </svg>
                            </span>
                          </div>
                          <div className="text-[11px] text-slate-400 mt-0.5 pl-6">
                            {opt.price || "0"}đ · {opt.supplies.length} vật tư
                          </div>
                        </button>
                      );
                    })}
                  </div>

                  {/* CHI TIẾT OPTION ĐANG CHỌN (bên phải) */}
                  <div className="flex-1 p-6 min-w-0">
                    {!selectedOption ? (
                      <div className="h-full flex items-center justify-center py-10">
                        <p className="text-[12px] font-semibold text-slate-400">Chọn 1 option bên trái để chỉnh sửa.</p>
                      </div>
                    ) : (
                      <div className="flex flex-col gap-5">
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                          <div className="flex flex-col gap-1.5 sm:col-span-1">
                            <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wide">Tên option</label>
                            <input
                              type="text"
                              placeholder="Tên option (vd: Titan)"
                              value={selectedOption.name}
                              onChange={(e) => handleOptionChange(selectedOption.id, "name", e.target.value)}
                              className="px-3 py-2 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:outline-none font-semibold text-slate-800 placeholder:text-slate-300"
                            />
                          </div>
                          <div className="flex flex-col gap-1.5">
                            <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wide">Giá</label>
                            <div className="relative">
                              <input
                                type="text"
                                placeholder="Giá"
                                value={selectedOption.price}
                                onChange={(e) => handleOptionChange(selectedOption.id, "price", e.target.value)}
                                className="w-full px-3 py-2 pr-7 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:outline-none font-semibold text-slate-800 placeholder:text-slate-300"
                              />
                              <span className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 font-bold">đ</span>
                            </div>
                          </div>
                          <div className="flex flex-col gap-1.5">
                            <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wide">ĐVT</label>
                            <select
                              value={selectedOption.unit || "Răng"}
                              onChange={(e) => handleOptionChange(selectedOption.id, "unit", e.target.value)}
                              className="px-3 py-2 text-[13px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:outline-none font-semibold text-slate-800 cursor-pointer"
                            >
                              <option value="Răng">Răng</option>
                              <option value="Liệu trình">Liệu trình</option>
                              <option value="Lần">Lần</option>
                              <option value="Hàm">Hàm</option>
                              <option value="Gói">Gói</option>
                              <option value="Chiếc">Chiếc</option>
                              <option value="Bộ">Bộ</option>
                              {selectedOption.unit && !["Răng", "Liệu trình", "Lần", "Hàm", "Gói", "Chiếc", "Bộ"].includes(selectedOption.unit) && (
                                <option value={selectedOption.unit}>{selectedOption.unit}</option>
                              )}
                            </select>
                          </div>
                        </div>

                        <div className="flex flex-col gap-2">
                          <div className="flex items-center justify-between">
                            <span className="text-[11px] font-bold text-amber-600 uppercase tracking-wide">Vật tư riêng cho option này</span>
                            <button
                              type="button"
                              onClick={() => handleAddOptionSupply(selectedOption.id)}
                              className="px-2.5 py-1 text-[11px] font-bold text-amber-600 bg-amber-50 hover:bg-amber-100 rounded-lg transition-all cursor-pointer border border-amber-200/60"
                            >
                              + Thêm vật tư
                            </button>
                          </div>
                          {selectedOption.supplies.length === 0 ? (
                            <p className="text-[11px] text-slate-400">Chưa gắn vật tư nào cho option này.</p>
                          ) : (
                            <div className="flex flex-col gap-1.5">
                              {selectedOption.supplies.map((row, sIdx) => (
                                <div key={row.id} className="flex items-center gap-1.5">
                                  <span className="w-5 h-5 rounded bg-amber-100 text-amber-700 text-[10px] font-black flex items-center justify-center shrink-0">
                                    {sIdx + 1}
                                  </span>
                                  <select
                                    value={row.supplyItemId}
                                    onChange={(e) => handleOptionSupplyChange(selectedOption.id, row.id, "supplyItemId", e.target.value)}
                                    className="flex-1 px-2.5 py-1.5 text-[12px] bg-white border border-slate-200 rounded-lg focus:border-amber-500 focus:outline-none font-semibold text-slate-700 cursor-pointer"
                                  >
                                    <option value="">— Chọn vật tư —</option>
                                    {catalogItems.map((item) => (
                                      <option key={item.id} value={item.id}>{item.name} ({item.unit})</option>
                                    ))}
                                  </select>
                                  <input
                                    type="number"
                                    min="1"
                                    placeholder="SL"
                                    value={row.quantity}
                                    onChange={(e) => handleOptionSupplyChange(selectedOption.id, row.id, "quantity", e.target.value)}
                                    className="w-16 px-2 py-1.5 text-[12px] bg-white border border-slate-200 rounded-lg focus:border-amber-500 focus:outline-none font-semibold text-slate-700 shrink-0"
                                  />
                                  <button
                                    type="button"
                                    onClick={() => void handleRemoveOptionSupply(selectedOption.id, row.id)}
                                    className="p-1 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer shrink-0"
                                    title="Xóa vật tư này"
                                  >
                                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                                    </svg>
                                  </button>
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* CARD: BÀI VIẾT CHI TIẾT — xuống dưới cùng, full width */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                <h3 className="text-[15px] font-extrabold text-slate-700 flex items-center gap-2">
                  <span className="w-2.5 h-2.5 rounded-full bg-blue-500" />
                  Bài viết chi tiết về dịch vụ
                </h3>
                <p className="text-[12px] text-slate-400 mt-0.5">
                  Soạn bài viết giới thiệu quy trình, ưu điểm, hình ảnh minh họa & bảng biểu cho bệnh nhân.
                </p>
              </div>

              <div className="p-6">
                <RichTextEditor
                  value={formContent}
                  onChange={setFormContent}
                  placeholder="Nhập thông tin giới thiệu dịch vụ (Ưu điểm, Quy trình thực hiện, Hình ảnh minh họa, Bảng so sánh...)..."
                  minHeight="450px"
                  maxHeight="700px"
                />
              </div>
            </div>

            {/* FOOTER: ACTION BUTTONS */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm px-6 py-4 flex items-center justify-end gap-3">
              {saveError && (
                <p className="text-[12px] text-red-500 font-semibold flex-1">{saveError}</p>
              )}
              <Link
                href="/admin/services"
                className="px-5 py-2.5 text-[13px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-100 border border-slate-200 rounded-xl transition-all cursor-pointer"
              >
                Hủy bỏ
              </Link>
              <button
                type="submit"
                disabled={isSaving}
                className="px-6 py-2.5 bg-primary hover:bg-primary-hover text-white text-[13px] font-extrabold rounded-xl shadow-md shadow-primary/25 hover:shadow-lg transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed flex items-center gap-2"
              >
                {isSaving ? (
                  <>
                    <svg className="w-4 h-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                    </svg>
                    <span>Đang lưu...</span>
                  </>
                ) : (
                  <span>Lưu dịch vụ</span>
                )}
              </button>
            </div>
          </form>
        </div>
      </main>

      <ConfirmDialog state={confirmState} onClose={closeConfirm} />
    </div>
  );
}
