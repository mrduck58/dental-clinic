"use client";

import React, { useState, useEffect, useRef } from "react";
import Link from "next/link";
import AdminSidebar from "../../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../../hooks/useRequireAdmin";

interface EditMedicinePageProps {
  params: Promise<{ id: string }>;
}

interface Medicine {
  id: string;
  name: string;
  genericName: string;
  unit: string;
  manufacturer: string;
  usage: string;
  imageUrl?: string;
}

const mockMedicine: Medicine = {
  id: "1",
  name: "Amoxicillin 500mg",
  genericName: "Amoxicillin",
  unit: "Viên",
  manufacturer: "Domesco",
  usage: "Kháng sinh điều trị nhiễm khuẩn",
  imageUrl: "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=100&h=100&fit=crop",
};

export default function EditMedicinePage({ params }: EditMedicinePageProps) {
  useRequireAdmin();
  const { id } = React.use(params);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [formData, setFormData] = useState({
    name: "",
    genericName: "",
    unit: "",
    manufacturer: "",
    usage: "",
  });

  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [existingImageUrl, setExistingImageUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  useEffect(() => {
    const fetchMedicine = async () => {
      setIsLoading(true);
      await new Promise((resolve) => setTimeout(resolve, 500));
      const medicine = mockMedicine;
      setFormData({
        name: medicine.name,
        genericName: medicine.genericName,
        unit: medicine.unit,
        manufacturer: medicine.manufacturer,
        usage: medicine.usage,
      });
      setExistingImageUrl(medicine.imageUrl || null);
      setImagePreview(medicine.imageUrl || null);
      setIsLoading(false);
    };
    fetchMedicine();
  }, [id]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setImageFile(file);
      setExistingImageUrl(null);
      const reader = new FileReader();
      reader.onloadend = () => {
        setImagePreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleRemoveImage = () => {
    setImageFile(null);
    setImagePreview(null);
    setExistingImageUrl(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaveError(null);

    if (!formData.name || !formData.genericName || !formData.unit || !formData.manufacturer) {
      setSaveError("Vui lòng điền đầy đủ thông tin bắt buộc.");
      return;
    }

    setIsSaving(true);
    try {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      const formDataToSend = new FormData();
      formDataToSend.append("name", formData.name);
      formDataToSend.append("genericName", formData.genericName);
      formDataToSend.append("unit", formData.unit);
      formDataToSend.append("manufacturer", formData.manufacturer);
      formDataToSend.append("usage", formData.usage);
      if (imageFile) {
        formDataToSend.append("image", imageFile);
      }
      console.log("Updated medicine data:", { id, ...formData, imageFile });
      setSaveSuccess(true);
      setTimeout(() => {
        window.location.href = "/dashboard/medicines";
      }, 1500);
    } catch {
      setSaveError("Cập nhật thuốc thất bại. Vui lòng thử lại.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="medicines" />

      <main className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <Link
              href="/dashboard/medicines"
              className="flex items-center justify-center w-10 h-10 rounded-xl bg-slate-100 text-slate-600 hover:bg-primary hover:text-white transition-all cursor-pointer"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
            <div>
              <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Chỉnh sửa thuốc</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
                Cập nhật thông tin thuốc trong hệ thống.
              </p>
            </div>
          </div>
          <NotificationBell />
        </header>

        <div className="p-8 flex-1 overflow-y-auto">
          {isLoading ? (
            <div className="max-w-2xl mx-auto flex items-center justify-center py-20">
              <div className="flex flex-col items-center gap-4">
                <svg className="animate-spin w-10 h-10 text-primary" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <p className="text-slate-400 font-semibold">Đang tải thông tin thuốc...</p>
              </div>
            </div>
          ) : saveSuccess ? (
            <div className="max-w-2xl mx-auto flex flex-col items-center justify-center py-20">
              <div className="w-20 h-20 rounded-full bg-green-100 text-green-600 flex items-center justify-center mb-6 animate-bounce">
                <svg className="w-10 h-10" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                </svg>
              </div>
              <h2 className="text-2xl font-extrabold text-slate-900 mb-2">Cập nhật thuốc thành công!</h2>
              <p className="text-slate-500 font-semibold">Đang chuyển hướng về trang danh sách...</p>
            </div>
          ) : (
            <form onSubmit={handleSave} className="max-w-2xl mx-auto">
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-6 py-5 border-b border-slate-100 bg-slate-50/30 flex items-center justify-between">
                  <h3 className="text-[15px] font-extrabold text-slate-700">Thông tin thuốc</h3>
                  <span className="text-[12px] font-bold text-slate-400 bg-slate-100 px-3 py-1.5 rounded-lg">
                    ID: {id}
                  </span>
                </div>

                <div className="p-6 flex flex-col gap-5">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Tên thuốc <span className="text-primary">*</span>
                      </label>
                      <input
                        type="text"
                        name="name"
                        required
                        placeholder="Ví dụ: Amoxicillin 500mg"
                        value={formData.name}
                        onChange={handleChange}
                        className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                    </div>

                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Hoạt chất <span className="text-primary">*</span>
                      </label>
                      <input
                        type="text"
                        name="genericName"
                        required
                        placeholder="Ví dụ: Amoxicillin"
                        value={formData.genericName}
                        onChange={handleChange}
                        className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Đơn vị <span className="text-primary">*</span>
                      </label>
                      <div className="relative">
                        <select
                          name="unit"
                          required
                          value={formData.unit}
                          onChange={handleChange}
                          className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer"
                        >
                          <option value="">Chọn đơn vị</option>
                          <option value="Viên">Viên</option>
                          <option value="Ống">Ống</option>
                          <option value="Lọ">Lọ</option>
                          <option value="Hộp">Hộp</option>
                          <option value="Tuýp">Tuýp</option>
                          <option value="Gói">Gói</option>
                          <option value="Vĩ">Vĩ</option>
                          <option value="Ml">Ml</option>
                        </select>
                        <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                          </svg>
                        </span>
                      </div>
                    </div>

                    <div className="flex flex-col gap-2">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Nhà sản xuất <span className="text-primary">*</span>
                      </label>
                      <input
                        type="text"
                        name="manufacturer"
                        required
                        placeholder="Ví dụ: Domesco, Stada..."
                        value={formData.manufacturer}
                        onChange={handleChange}
                        className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300"
                      />
                    </div>
                  </div>

                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Công dụng
                    </label>
                    <textarea
                      rows={3}
                      name="usage"
                      placeholder="Mô tả công dụng của thuốc..."
                      value={formData.usage}
                      onChange={handleChange}
                      className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none transition-all font-semibold text-slate-800 placeholder:text-slate-300 resize-none"
                    />
                  </div>

                  <div className="flex flex-col gap-2">
                    <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                      Hình ảnh thuốc
                    </label>
                    <div className="border-2 border-dashed border-slate-200 rounded-xl p-4 text-center hover:border-primary/50 transition-colors">
                      {imagePreview ? (
                        <div className="relative inline-block">
                          <img
                            src={imagePreview}
                            alt="Preview"
                            className="w-32 h-32 object-cover rounded-lg mx-auto"
                          />
                          <button
                            type="button"
                            onClick={handleRemoveImage}
                            className="absolute -top-2 -right-2 w-7 h-7 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600 transition-colors shadow-md"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                          </button>
                        </div>
                      ) : (
                        <>
                          <input
                            ref={fileInputRef}
                            type="file"
                            accept="image/*"
                            onChange={handleImageChange}
                            className="hidden"
                            id="medicine-image"
                          />
                          <label
                            htmlFor="medicine-image"
                            className="flex flex-col items-center gap-2 cursor-pointer"
                          >
                            <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center">
                              <svg className="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
                              </svg>
                            </div>
                            <span className="text-[13px] font-semibold text-slate-500">
                              Nhấn để chọn hình ảnh
                            </span>
                            <span className="text-[11px] text-slate-400">
                              PNG, JPG, GIF (tối đa 5MB)
                            </span>
                          </label>
                        </>
                      )}
                    </div>
                  </div>
                </div>

                <div className="px-6 py-5 border-t border-slate-100 bg-slate-50/30 flex items-center justify-end gap-3">
                  {saveError && (
                    <p className="text-[13px] text-red-500 font-semibold flex-1">{saveError}</p>
                  )}
                  <Link
                    href="/dashboard/medicines"
                    className="px-6 py-3 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-100 border border-slate-200 rounded-xl transition-all cursor-pointer"
                  >
                    Hủy bỏ
                  </Link>
                  <button
                    type="submit"
                    disabled={isSaving}
                    className="px-8 py-3 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/25 hover:shadow-lg transition-all cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed flex items-center gap-2"
                  >
                    {isSaving ? (
                      <>
                        <svg className="animate-spin w-5 h-5" fill="none" viewBox="0 0 24 24">
                          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        Đang lưu...
                      </>
                    ) : (
                      <>
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                        </svg>
                        Lưu thay đổi
                      </>
                    )}
                  </button>
                </div>
              </div>
            </form>
          )}
        </div>
      </main>
    </div>
  );
}
