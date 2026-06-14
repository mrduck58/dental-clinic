"use client";

import React, { useState, useRef } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import { createServiceApi } from "../../../../lib/apiClient";

const categories = ["Niềng răng", "Tẩy trắng răng", "Trồng răng", "Lấy cao răng", "Điều trị tủy", "Nhổ răng", "Trám răng"];

export default function AddServicePage() {
  useRequireAdmin();
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [formName, setFormName] = useState("");
  const [formCategory, setFormCategory] = useState("Niềng răng");
  const [formPrice, setFormPrice] = useState("");
  const [formDuration, setFormDuration] = useState("");
  const [formDescription, setFormDescription] = useState("");
  const [uploadedImage, setUploadedImage] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const handleImageUpload = (file: File) => {
    if (file && file.type.startsWith("image/")) {
      const reader = new FileReader();
      reader.onload = (e) => {
        setUploadedImage(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleImageUpload(file);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleImageUpload(file);
  };

  const handleRemoveImage = () => {
    setUploadedImage(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
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
    setIsSaving(true);
    try {
      await createServiceApi({
        name: formName,
        category: formCategory,
        price: parseInt(rawPrice),
        durationMinutes: parseInt(formDuration),
        description: formDescription,
        imageUrl: null,
      });
      router.push("/dashboard/services");
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : "Thêm dịch vụ thất bại.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="services" />

      <main className="flex-1 flex flex-col min-w-0">
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <Link
              href="/dashboard/services"
              className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
            <div>
              <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Thêm dịch vụ</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
                Thêm dịch vụ mới vào hệ thống.
              </p>
            </div>
          </div>

          <NotificationBell />
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto">
          <form onSubmit={handleSave} className="max-w-3xl mx-auto">
            {/* Form Card */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
              {/* Form Header */}
              <div className="px-6 py-5 border-b border-slate-100 bg-slate-50/30">
                <h3 className="text-[15px] font-extrabold text-slate-700">Thông tin dịch vụ</h3>
              </div>

              {/* Form Content */}
              <div className="p-6 flex flex-col gap-5">
                {/* Tên dịch vụ */}
                <div className="flex flex-col gap-2">
                  <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                    Tên dịch vụ <span className="text-primary">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    placeholder="Nhập tên dịch vụ..."
                    value={formName}
                    onChange={(e) => setFormName(e.target.value)}
                    className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                  />
                </div>

                {/* Danh mục và Giá */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                  {/* Danh mục */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Danh mục <span className="text-primary">*</span>
                    </label>
                    <div className="relative">
                      <select
                        value={formCategory}
                        onChange={(e) => setFormCategory(e.target.value)}
                        className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-700 appearance-none pr-10 cursor-pointer"
                      >
                        {categories.map((cat) => (
                          <option key={cat} value={cat}>{cat}</option>
                        ))}
                      </select>
                      <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                        </svg>
                      </span>
                    </div>
                  </div>

                  {/* Giá dịch vụ */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Giá dịch vụ <span className="text-primary">*</span>
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
                        className="w-full px-4 py-3 pr-14 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                      <span className="absolute right-4 top-1/2 -translate-y-1/2 text-[13px] text-slate-400 font-bold">
                        VNĐ
                      </span>
                    </div>
                  </div>
                </div>

                {/* Thời gian và Hình ảnh */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                  {/* Thời gian */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
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
                        className="w-full px-4 py-3 pr-14 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                      <span className="absolute right-4 top-1/2 -translate-y-1/2 text-[13px] text-slate-400 font-bold">
                        phút
                      </span>
                    </div>
                  </div>

                  {/* Hình ảnh */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Hình ảnh dịch vụ
                    </label>
                    
                    {uploadedImage ? (
                      /* Preview Image */
                      <div className="relative rounded-xl overflow-hidden border border-slate-200 group h-[50px]">
                        <img
                          src={uploadedImage}
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
                            onClick={handleRemoveImage}
                            className="p-1.5 bg-red-500 rounded-lg text-white hover:bg-red-600 transition-all cursor-pointer"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                          </button>
                        </div>
                      </div>
                    ) : (
                      /* Upload Area */
                      <div
                        onDrop={handleDrop}
                        onDragOver={handleDragOver}
                        onDragLeave={handleDragLeave}
                        onClick={() => fileInputRef.current?.click()}
                        className={`relative h-[50px] rounded-xl border-2 border-dashed transition-all cursor-pointer flex items-center gap-3 px-4 ${
                          isDragging
                            ? "border-primary bg-primary/5"
                            : "border-slate-200 hover:border-primary/50 hover:bg-slate-50/50"
                        }`}
                      >
                        <input
                          ref={fileInputRef}
                          type="file"
                          accept="image/*"
                          onChange={handleFileChange}
                          className="hidden"
                        />
                        <div className={`w-8 h-8 rounded-lg flex items-center justify-center shrink-0 ${
                          isDragging ? "bg-primary/10 text-primary" : "bg-slate-100 text-slate-400"
                        }`}>
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                          </svg>
                        </div>
                        <span className="text-[13px] font-semibold text-slate-500">
                          Tải ảnh lên
                        </span>
                        <span className="text-[11px] text-slate-400 ml-auto">
                          PNG, JPG (5MB)
                        </span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Mô tả */}
                <div className="flex flex-col gap-2">
                  <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                    Mô tả dịch vụ
                  </label>
                  <textarea
                    rows={4}
                    placeholder="Nhập mô tả chi tiết về dịch vụ..."
                    value={formDescription}
                    onChange={(e) => setFormDescription(e.target.value)}
                    className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300 resize-none"
                  />
                </div>
              </div>

              {/* Form Footer */}
              <div className="px-6 py-5 border-t border-slate-100 bg-slate-50/30 flex items-center justify-end gap-3">
                {saveError && (
                  <p className="text-[13px] text-red-500 font-semibold flex-1">{saveError}</p>
                )}
                <Link
                  href="/dashboard/services"
                  className="px-6 py-3 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-100 border border-slate-200 rounded-xl transition-all cursor-pointer"
                >
                  Hủy bỏ
                </Link>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="px-8 py-3 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/25 hover:shadow-lg transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed"
                >
                  {isSaving ? "Đang lưu..." : "Lưu dịch vụ"}
                </button>
              </div>
            </div>
          </form>
        </div>
      </main>
    </div>
  );
}
