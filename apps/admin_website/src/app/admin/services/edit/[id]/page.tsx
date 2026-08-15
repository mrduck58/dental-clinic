"use client";

import React, { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import AdminSidebar from "@/src/components/shared/AdminSidebar";
import AdminPageHeader from "@/src/components/shared/AdminPageHeader";
import RichTextEditor from "@/src/components/shared/RichTextEditor";
import { useRequireAdmin } from "@/src/hooks/useRequireAdmin";
import {
  getServiceByIdApi,
  updateServiceApi,
  getServiceProceduresApi,
  updateServiceProceduresApi,
  uploadFileApi,
  resolveAssetUrl,
} from "@/src/lib/apiClient";

interface ServiceOptionRow {
  id: string;
  name: string;
  price: string;
  unit: string;
}

interface ProcedureStepRow {
  id: string;
  name: string;
}

interface EditServicePageProps {
  params: Promise<{ id: string }>;
}

export default function EditServicePage({ params }: EditServicePageProps) {
  useRequireAdmin();
  const router = useRouter();
  const { id } = React.use(params);

  // Form states
  const [formName, setFormName] = useState("");
  const [formPrice, setFormPrice] = useState("");
  const [formDuration, setFormDuration] = useState("");
  const [formDescription, setFormDescription] = useState("");
  const [formContent, setFormContent] = useState("");
  const [options, setOptions] = useState<ServiceOptionRow[]>([]);
  const [steps, setSteps] = useState<ProcedureStepRow[]>([]);

  // Images state
  const [uploadedImage, setUploadedImage] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Icon state
  const [iconUrl, setIconUrl] = useState<string | null>(null);
  const [isUploadingIcon, setIsUploadingIcon] = useState(false);
  const iconInputRef = useRef<HTMLInputElement>(null);

  // Submit & Loading state
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      getServiceByIdApi(id),
      getServiceProceduresApi(id).catch(() => []),
    ])
      .then(([dto, procedureList]) => {
        setFormName(dto.name);
        setFormPrice(dto.price ? dto.price.toLocaleString("vi-VN") : "0");
        setFormDuration(String(dto.durationMinutes || 30));
        setFormDescription(dto.description || "");
        setFormContent(dto.content || "");
        setUploadedImage(dto.imageUrl ?? null);
        setIconUrl(dto.iconUrl ?? null);

        if (dto.options && dto.options.length > 0) {
          setOptions(
            dto.options.map((opt) => ({
              id: opt.id,
              name: opt.name,
              price: opt.price ? opt.price.toLocaleString("vi-VN") : "0",
              unit: opt.unit || "Răng",
            }))
          );
        }

        if (procedureList && procedureList.length > 0) {
          const sorted = [...procedureList].sort((a, b) => a.stepNumber - b.stepNumber);
          setSteps(sorted.map((p, idx) => ({ id: idx.toString(), name: p.name })));
        }
      })
      .catch(() => {
        setSaveError("Không thể tải thông tin dịch vụ.");
      })
      .finally(() => setIsLoading(false));
  }, [id]);

  // Options handlers
  const handleAddOption = () => {
    setOptions((prev) => [
      ...prev,
      { id: Date.now().toString(), name: "", price: "", unit: "Răng" },
    ]);
  };

  const handleRemoveOption = (optId: string) => {
    setOptions((prev) => prev.filter((opt) => opt.id !== optId));
  };

  const handleOptionChange = (optId: string, field: "name" | "price" | "unit", val: string) => {
    setOptions((prev) =>
      prev.map((opt) => {
        if (opt.id !== optId) return opt;
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

  // Procedure steps handlers
  const handleAddStep = () => {
    setSteps((prev) => [
      ...prev,
      { id: Date.now().toString(), name: "" },
    ]);
  };

  const handleRemoveStep = (stepId: string) => {
    setSteps((prev) => prev.filter((s) => s.id !== stepId));
  };

  const handleStepChange = (stepId: string, val: string) => {
    setSteps((prev) =>
      prev.map((s) => (s.id === stepId ? { ...s, name: val } : s))
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

    setIsSaving(true);
    try {
      await updateServiceApi(id, {
        name: formName,
        price: parseInt(rawPrice),
        durationMinutes: parseInt(formDuration),
        description: formDescription,
        content: formContent,
        imageUrl: uploadedImage,
        iconUrl,
        options: validOptions,
      });

      await updateServiceProceduresApi(id, validSteps);

      router.push("/admin/services");
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : "Cập nhật dịch vụ thất bại.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="services" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <AdminPageHeader
          title="Chỉnh sửa dịch vụ"
          subtitle="Cập nhật thông tin dịch vụ, tùy chọn phân loại giá, quy trình điều trị & bài viết chi tiết."
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
          {isLoading ? (
            <div className="max-w-7xl mx-auto flex flex-col items-center justify-center py-24 text-slate-400 font-semibold gap-3">
              <svg className="w-8 h-8 animate-spin text-primary" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
              <span>Đang tải thông tin dịch vụ...</span>
            </div>
          ) : (
            <form onSubmit={handleSave} className="max-w-7xl mx-auto">
              <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
                
                {/* LEFT COLUMN (5/12): 1 UNIFIED CARD WITH ALL BASIC INFO, OPTIONS, STEPS & BUTTONS */}
                <div className="lg:col-span-5">
                  <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
                    {/* Card Header */}
                    <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between">
                      <h3 className="text-[15px] font-extrabold text-slate-700 flex items-center gap-2">
                        <span className="w-2.5 h-2.5 rounded-full bg-primary" />
                        Thông tin & Tùy chọn dịch vụ
                      </h3>
                      <span className="text-[11px] font-bold text-slate-400 bg-slate-100 px-2.5 py-1 rounded-lg">
                        ID: {id.slice(0, 8)}...
                      </span>
                    </div>

                    {/* Card Body */}
                    <div className="p-6 flex flex-col gap-6">
                      {/* Tên dịch vụ */}
                      <div className="flex flex-col gap-1.5">
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

                      {/* Giá khởi điểm & Thời gian */}
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        {/* Giá khởi điểm */}
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

                        {/* Thời gian */}
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

                      {/* Hình ảnh & Icon */}
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        {/* Hình ảnh */}
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

                        {/* Icon (SVG) */}
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

                      {/* Mô tả ngắn */}
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

                      {/* Divider */}
                      <div className="border-t border-slate-100 my-1" />

                      {/* SUB-SECTION: OPTIONS & PRICING TIERS */}
                      <div className="flex flex-col gap-3">
                        <div className="flex items-center justify-between">
                          <div>
                            <label className="text-[13px] font-extrabold text-slate-700 flex items-center gap-1.5 uppercase tracking-wide">
                              <span className="w-2 h-2 rounded-full bg-emerald-500" />
                              Tùy chọn phân loại & Bảng giá
                            </label>
                            <p className="text-[11px] text-slate-400">Chất liệu (Titan, Zirconia...), giá & đơn vị tính</p>
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
                          <div className="py-3 text-center border border-dashed border-slate-200 rounded-xl bg-slate-50/50">
                            <p className="text-[12px] font-semibold text-slate-400">Chưa thêm phân loại nào.</p>
                            <button
                              type="button"
                              onClick={handleAddOption}
                              className="mt-1 text-[12px] font-bold text-primary hover:underline cursor-pointer"
                            >
                              + Thêm tùy chọn giá đầu tiên
                            </button>
                          </div>
                        ) : (
                          <div className="flex flex-col gap-2.5 max-h-[220px] overflow-y-auto pr-1">
                            {options.map((opt, idx) => (
                              <div
                                key={opt.id}
                                className="flex flex-col gap-2 p-2.5 bg-slate-50/80 rounded-xl border border-slate-200/80 hover:border-slate-300 transition-all"
                              >
                                <div className="flex items-center gap-2">
                                  <span className="w-5 h-5 rounded bg-slate-200 text-slate-600 text-[10px] font-black flex items-center justify-center shrink-0">
                                    {idx + 1}
                                  </span>
                                  <input
                                    type="text"
                                    placeholder="Tên option (vd: Răng sứ Zirconia)"
                                    value={opt.name}
                                    onChange={(e) => handleOptionChange(opt.id, "name", e.target.value)}
                                    className="flex-1 px-2.5 py-1.5 text-[12px] bg-white border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-semibold text-slate-800 placeholder:text-slate-300"
                                  />
                                  <button
                                    type="button"
                                    onClick={() => handleRemoveOption(opt.id)}
                                    className="p-1 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                                    title="Xóa option này"
                                  >
                                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                                    </svg>
                                  </button>
                                </div>

                                <div className="grid grid-cols-2 gap-2 pl-7">
                                  <div className="relative">
                                    <input
                                      type="text"
                                      placeholder="Giá"
                                      value={opt.price}
                                      onChange={(e) => handleOptionChange(opt.id, "price", e.target.value)}
                                      className="w-full px-2.5 py-1.5 pr-7 text-[12px] bg-white border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-semibold text-slate-800 placeholder:text-slate-300"
                                    />
                                    <span className="absolute right-2 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 font-bold">
                                      đ
                                    </span>
                                  </div>
                                  <div className="relative">
                                    <select
                                      value={opt.unit || "Răng"}
                                      onChange={(e) => handleOptionChange(opt.id, "unit", e.target.value)}
                                      className="w-full px-2.5 py-1.5 text-[12px] bg-white border border-slate-200 rounded-lg focus:border-primary focus:outline-none font-semibold text-slate-800 cursor-pointer"
                                    >
                                      <option value="Răng">ĐVT: Răng</option>
                                      <option value="Liệu trình">ĐVT: Liệu trình</option>
                                      <option value="Lần">ĐVT: Lần</option>
                                      <option value="Hàm">ĐVT: Hàm</option>
                                      <option value="Gói">ĐVT: Gói</option>
                                      <option value="Chiếc">ĐVT: Chiếc</option>
                                      <option value="Bộ">ĐVT: Bộ</option>
                                      {opt.unit && !["Răng", "Liệu trình", "Lần", "Hàm", "Gói", "Chiếc", "Bộ"].includes(opt.unit) && (
                                        <option value={opt.unit}>ĐVT: {opt.unit}</option>
                                      )}
                                    </select>
                                  </div>
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>

                      {/* Divider */}
                      <div className="border-t border-slate-100 my-1" />

                      {/* SUB-SECTION: QUY TRÌNH ĐIỀU TRỊ (CÁC BƯỚC THỰC HIỆN) */}
                      <div className="flex flex-col gap-3">
                        <div className="flex items-center justify-between">
                          <div>
                            <label className="text-[13px] font-extrabold text-slate-700 flex items-center gap-1.5 uppercase tracking-wide">
                              <span className="w-2 h-2 rounded-full bg-blue-500" />
                              Quy trình điều trị (Các bước thực hiện)
                            </label>
                            <p className="text-[11px] text-slate-400">Các bước khám & điều trị chuẩn cho dịch vụ này</p>
                          </div>
                          <button
                            type="button"
                            onClick={handleAddStep}
                            className="px-3 py-1.5 text-[11px] font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 rounded-lg transition-all cursor-pointer flex items-center gap-1 shrink-0 border border-blue-200/60"
                          >
                            + Thêm bước
                          </button>
                        </div>

                        {steps.length === 0 ? (
                          <div className="py-3 text-center border border-dashed border-slate-200 rounded-xl bg-slate-50/50">
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
                          <div className="flex flex-col gap-2 max-h-[180px] overflow-y-auto pr-1">
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

                    {/* Card Footer: Action Buttons inside the same card */}
                    <div className="px-6 py-4 border-t border-slate-100 bg-slate-50/50 flex items-center justify-end gap-3">
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
                          <span>Cập nhật dịch vụ</span>
                        )}
                      </button>
                    </div>

                  </div>
                </div>

                {/* RIGHT COLUMN (7/12): RICH TEXT EDITOR CARD WITH STRICT MAX HEIGHT & SCROLL */}
                <div className="lg:col-span-7 flex flex-col">
                  <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col max-h-[760px]">
                    <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex items-center justify-between shrink-0">
                      <div>
                        <h3 className="text-[15px] font-extrabold text-slate-700 flex items-center gap-2">
                          <span className="w-2.5 h-2.5 rounded-full bg-blue-500" />
                          Bài viết chi tiết về dịch vụ
                        </h3>
                        <p className="text-[12px] text-slate-400 mt-0.5">
                          Soạn bài viết giới thiệu quy trình, ưu điểm, hình ảnh minh họa & bảng biểu cho bệnh nhân.
                        </p>
                      </div>
                    </div>

                    <div className="p-6 flex-1 overflow-hidden flex flex-col">
                      <RichTextEditor
                        value={formContent}
                        onChange={setFormContent}
                        placeholder="Nhập thông tin giới thiệu dịch vụ (Ưu điểm, Quy trình thực hiện, Hình ảnh minh họa, Bảng so sánh...)..."
                        minHeight="450px"
                        maxHeight="580px"
                      />
                    </div>
                  </div>
                </div>

              </div>
            </form>
          )}
        </div>
      </main>
    </div>
  );
}
