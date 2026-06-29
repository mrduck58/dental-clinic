"use client";

import React, { useState, useEffect } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import {
  getClinicInfoApi,
  updateClinicInfoApi,
  uploadFileApi,
  type ClinicInfoDto,
  type MilestoneDto,
  type FeatureDto,
  type TreatmentStepDto,
  type StatisticDto,
} from "../../../lib/apiClient";

type TabId = "basic" | "image" | "milestones" | "features" | "stats";

const TAB_ITEMS: Array<{ id: TabId; label: string; icon: React.ReactNode }> = [
  {
    id: "basic",
    label: "Thông tin cơ bản",
    icon: (
      <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 1 1 1.063.852l-.708 2.836a.75.75 0 001.063.852l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
      </svg>
    )
  },
  {
    id: "image",
    label: "Ảnh giới thiệu",
    icon: (
      <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.9 2.9m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 01.75 0z" />
      </svg>
    )
  },
  {
    id: "milestones",
    label: "Mốc lịch sử & Chứng chỉ",
    icon: (
      <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 18.75h-9m9 0a3 3 0 013 3h-15a3 3 0 013-3m9 0v-3.375c0-.621-.504-1.125-1.125-1.125h-6.75a1.125 1.125 0 00-1.125 1.125v3.375m9 0V9M7.5 18.75V9m.75-6h7.5m-7.5 0a1.5 1.5 0 00-1.5 1.5v3a1.5 1.5 0 00 1.5 1.5h7.5A1.5 1.5 0 0018 7.5v-3A1.5 1.5 0 0016.5 3m-9 0h9" />
      </svg>
    )
  },
  {
    id: "features",
    label: "Đặc trưng & Quy trình",
    icon: (
      <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0 1 21 7.5v9a2.25 2.25 0 0 1-2.244 2.222H5.25A2.25 2.25 0 0 1 3 16.5v-9A2.25 2.25 0 0 1 5.25 5.25z" />
      </svg>
    )
  },
  {
    id: "stats",
    label: "Số liệu thống kê",
    icon: (
      <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v5.25c0 .621-.504 1.125-1.125 1.125h-2.25A1.125 1.125 0 0 1 3 18.375v-5.25zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125v-9.75zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v14.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125z" />
      </svg>
    )
  }
];

export default function OwnerClinicInfoPage() {
  useRequireOwner();

  const [activeTab, setActiveTab] = useState<TabId>("basic");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [toast, setToast] = useState<{ type: "success" | "error"; message: string } | null>(null);
  const [valErrors, setValErrors] = useState<Record<string, string>>({});

  const showToast = (type: "success" | "error", message: string) => {
    setToast({ type, message });
    setTimeout(() => setToast(null), 4000);
  };

  const handleFieldChange = (field: string, value: any, setter: (val: any) => void) => {
    setter(value);
    if (valErrors[field]) {
      setValErrors(prev => {
        const next = { ...prev };
        delete next[field];
        return next;
      });
    }
  };

  const validate = () => {
    const errs: Record<string, string> = {};
    if (!aboutTitle.trim()) {
      errs.aboutTitle = "Tiêu đề giới thiệu không được để trống";
    }
    if (!foundedYear || foundedYear < 1900 || foundedYear > new Date().getFullYear()) {
      errs.foundedYear = `Năm thành lập phải từ 1900 đến ${new Date().getFullYear()}`;
    }
    if (!phone.trim()) {
      errs.phone = "Số điện thoại liên hệ không được để trống";
    } else if (!/^\d{9,11}$/.test(phone.replace(/\s+/g, ""))) {
      errs.phone = "Số điện thoại phải từ 9 đến 11 chữ số";
    }
    if (!email.trim()) {
      errs.email = "Email không được để trống";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      errs.email = "Email không đúng định dạng";
    }
    if (!address.trim()) {
      errs.address = "Địa chỉ trụ sở chính không được để trống";
    }
    if (!aboutDescription.trim()) {
      errs.aboutDescription = "Bài viết giới thiệu không được để trống";
    }
    setValErrors(errs);
    return Object.keys(errs).length === 0;
  };

  // Form states
  const [aboutTitle, setAboutTitle] = useState("");
  const [aboutDescription, setAboutDescription] = useState("");
  const [foundedYear, setFoundedYear] = useState<number>(2024);
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [address, setAddress] = useState("");
  const [aboutImageUrl, setAboutImageUrl] = useState<string | null>(null);

  // List states
  const [milestones, setMilestones] = useState<MilestoneDto[]>([]);
  const [certifications, setCertifications] = useState<string[]>([]);
  const [features, setFeatures] = useState<FeatureDto[]>([]);
  const [treatmentSteps, setTreatmentSteps] = useState<TreatmentStepDto[]>([]);
  const [statistics, setStatistics] = useState<StatisticDto[]>([]);

  // Local UI helper states for adding items
  const [newCert, setNewCert] = useState("");
  const [uploadingImage, setUploadingImage] = useState(false);

  useEffect(() => {
    async function loadInfo() {
      setIsLoading(true);
      try {
        const data = await getClinicInfoApi();
        if (data) {
          setAboutTitle(data.aboutTitle || "");
          setAboutDescription(data.aboutDescription || "");
          setFoundedYear(data.foundedYear || 2024);
          setPhone(data.phone || "");
          setEmail(data.email || "");
          setAddress(data.address || "");
          setAboutImageUrl(data.aboutImageUrl || null);
          setMilestones(data.milestones || []);
          setCertifications(data.certifications || []);
          setFeatures(data.features || []);
          setTreatmentSteps(data.treatmentSteps || []);
          setStatistics(data.statistics || []);
        }
      } catch (err) {
        showToast("error", err instanceof Error ? err.message : "Không thể tải thông tin phòng khám");
      } finally {
        setIsLoading(false);
      }
    }
    loadInfo();
  }, []);

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploadingImage(true);
    try {
      const result = await uploadFileApi(file);
      setAboutImageUrl(result.url);
      showToast("success", "Tải ảnh lên thành công!");
    } catch (err) {
      showToast("error", err instanceof Error ? err.message : "Tải ảnh lên thất bại");
    } finally {
      setUploadingImage(false);
    }
  };

  const handleRemoveImage = () => {
    setAboutImageUrl(""); // Empty string means clear/remove image as per backend spec
  };

  // Milestone list helpers
  const handleAddMilestone = () => {
    setMilestones([...milestones, { year: new Date().getFullYear(), description: "" }]);
  };
  const handleRemoveMilestone = (index: number) => {
    setMilestones(milestones.filter((_, i) => i !== index));
  };
  const handleMilestoneChange = (index: number, field: keyof MilestoneDto, value: any) => {
    const updated = [...milestones];
    updated[index] = { ...updated[index], [field]: value };
    setMilestones(updated);
  };

  // Certification tags helpers
  const handleAddCert = (e?: React.FormEvent | React.MouseEvent | React.KeyboardEvent) => {
    if (e) e.preventDefault();
    if (!newCert.trim()) return;
    if (!certifications.includes(newCert.trim())) {
      setCertifications([...certifications, newCert.trim()]);
    }
    setNewCert("");
  };
  const handleRemoveCert = (cert: string) => {
    setCertifications(certifications.filter((c) => c !== cert));
  };

  // Features list helpers
  const handleAddFeature = () => {
    setFeatures([...features, { title: "", description: "" }]);
  };
  const handleRemoveFeature = (index: number) => {
    setFeatures(features.filter((_, i) => i !== index));
  };
  const handleFeatureChange = (index: number, field: keyof FeatureDto, value: string) => {
    const updated = [...features];
    updated[index] = { ...updated[index], [field]: value };
    setFeatures(updated);
  };

  // Treatment steps list helpers
  const handleAddStep = () => {
    setTreatmentSteps([...treatmentSteps, { title: "", description: "" }]);
  };
  const handleRemoveStep = (index: number) => {
    setTreatmentSteps(treatmentSteps.filter((_, i) => i !== index));
  };
  const handleStepChange = (index: number, field: keyof TreatmentStepDto, value: string) => {
    const updated = [...treatmentSteps];
    updated[index] = { ...updated[index], [field]: value };
    setTreatmentSteps(updated);
  };

  // Statistics list helpers
  const handleAddStat = () => {
    setStatistics([...statistics, { value: "", label: "" }]);
  };
  const handleRemoveStat = (index: number) => {
    setStatistics(statistics.filter((_, i) => i !== index));
  };
  const handleStatChange = (index: number, field: keyof StatisticDto, value: string) => {
    const updated = [...statistics];
    updated[index] = { ...updated[index], [field]: value };
    setStatistics(updated);
  };

  // Main Submit handler
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) {
      showToast("error", "Vui lòng kiểm tra lại các trường thông tin bị lỗi!");
      return;
    }
    setIsSaving(true);

    const payload = {
      aboutTitle,
      aboutDescription,
      foundedYear,
      phone,
      email,
      address,
      aboutImageUrl,
      milestones,
      certifications,
      features,
      treatmentSteps,
      statistics,
    };

    try {
      await updateClinicInfoApi(payload);
      showToast("success", "Cập nhật thông tin phòng khám thành công!");
    } catch (err) {
      showToast("error", err instanceof Error ? err.message : "Cập nhật thông tin thất bại");
    } finally {
      setIsSaving(false);
    }
  };

  const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5239";
  const imagePreviewUrl = aboutImageUrl
    ? aboutImageUrl.startsWith("http")
      ? aboutImageUrl
      : `${API_URL}${aboutImageUrl}`
    : null;

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="clinic-info" />

      <main className="animate-fade-in flex-1 flex flex-col min-w-0">
        {/* Header */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">
              Thông Tin Phòng Khám
            </h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Cấu hình thông tin giới thiệu và thông tin liên hệ hiển thị ở trang Landing Page khách hàng.</p>
          </div>
          <NotificationBell href="/owner/notifications" />
        </header>

        <div className="flex-1 px-8 py-5 overflow-y-auto w-full">

          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-20 gap-3">
              <div className="w-10 h-10 border-4 border-primary border-t-transparent rounded-full animate-spin" />
              <p className="text-slate-400 font-bold text-[14px]">Đang tải cấu hình phòng khám...</p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="flex flex-col gap-5" noValidate>
              {/* Tabs Navigation */}
              <div className="grid grid-cols-5 bg-primary p-1.5 rounded-2xl shadow-inner shrink-0 gap-1.5">
                {TAB_ITEMS.map((tab) => (
                  <button
                    key={tab.id}
                    type="button"
                    onClick={() => setActiveTab(tab.id)}
                    className={`flex items-center justify-center gap-2.5 px-4 py-3 rounded-xl transition-all cursor-pointer select-none ${
                      activeTab === tab.id
                        ? "bg-white text-primary shadow-md font-extrabold scale-[1.01]"
                        : "text-white hover:bg-white/10 hover:text-white/90"
                    }`}
                  >
                    {tab.icon}
                    <span className="text-[13.5px] font-extrabold leading-none">{tab.label}</span>
                  </button>
                ))}
              </div>

              {/* Tab Contents */}
              <div className="bg-white p-8 rounded-3xl border border-slate-200/80 shadow-md shadow-slate-100 flex flex-col gap-6">
                
                {/* ── TAB 1: BASIC INFO ────────────────────────────────────── */}
                {activeTab === "basic" && (
                  <div className="flex flex-col gap-6">
                    <div className="border-b border-slate-100 pb-3">
                      <h2 className="text-[17px] font-black text-slate-800">Thông tin liên hệ & Giới thiệu cơ bản</h2>
                      <p className="text-[12px] text-slate-400 font-semibold mt-1">Các thông tin quan trọng hiển thị ở tiêu đề trang giới thiệu và phần liên hệ ở footer.</p>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-5 text-[13px] font-bold text-slate-500">
                      <div className="flex flex-col gap-2">
                        <label className="text-slate-500">Tiêu đề giới thiệu (About Title) <span className="text-primary">*</span></label>
                        <input
                          type="text"
                          placeholder="VD: Nha khoa quốc tế Sơn Giang"
                          value={aboutTitle}
                          onChange={(e) => handleFieldChange("aboutTitle", e.target.value, setAboutTitle)}
                          className={`w-full px-4.5 py-3.5 bg-slate-50 border rounded-xl focus:bg-white focus:outline-none transition-all text-[14px] font-semibold text-slate-800 ${
                            valErrors.aboutTitle ? "border-red-400 focus:border-red-450" : "border-slate-200 focus:border-primary/50"
                          }`}
                        />
                        {valErrors.aboutTitle && (
                          <span className="text-[11.5px] text-red-500 font-bold mt-1 block animate-fade-in">
                            ⚠ {valErrors.aboutTitle}
                          </span>
                        )}
                      </div>

                      <div className="flex flex-col gap-2">
                        <label className="text-slate-500">Năm thành lập <span className="text-primary">*</span></label>
                        <input
                          type="number"
                          value={foundedYear}
                          onChange={(e) => handleFieldChange("foundedYear", Number(e.target.value), setFoundedYear)}
                          className={`w-full px-4.5 py-3.5 bg-slate-50 border rounded-xl focus:bg-white focus:outline-none transition-all text-[14px] font-semibold text-slate-800 ${
                            valErrors.foundedYear ? "border-red-400 focus:border-red-455" : "border-slate-200 focus:border-primary/50"
                          }`}
                        />
                        {valErrors.foundedYear && (
                          <span className="text-[11.5px] text-red-500 font-bold mt-1 block animate-fade-in">
                            ⚠ {valErrors.foundedYear}
                          </span>
                        )}
                      </div>

                      <div className="flex flex-col gap-2">
                        <label className="text-slate-500">Số điện thoại liên hệ <span className="text-primary">*</span></label>
                        <input
                          type="text"
                          placeholder="VD: 0912345678"
                          value={phone}
                          onChange={(e) => handleFieldChange("phone", e.target.value, setPhone)}
                          className={`w-full px-4.5 py-3.5 bg-slate-50 border rounded-xl focus:bg-white focus:outline-none transition-all text-[14px] font-semibold text-slate-800 ${
                            valErrors.phone ? "border-red-400 focus:border-red-450" : "border-slate-200 focus:border-primary/50"
                          }`}
                        />
                        {valErrors.phone && (
                          <span className="text-[11.5px] text-red-500 font-bold mt-1 block animate-fade-in">
                            ⚠ {valErrors.phone}
                          </span>
                        )}
                      </div>

                      <div className="flex flex-col gap-2">
                        <label className="text-slate-500">Email phòng khám <span className="text-primary">*</span></label>
                        <input
                          type="email"
                          placeholder="VD: contact@songiangclinic.vn"
                          value={email}
                          onChange={(e) => handleFieldChange("email", e.target.value, setEmail)}
                          className={`w-full px-4.5 py-3.5 bg-slate-50 border rounded-xl focus:bg-white focus:outline-none transition-all text-[14px] font-semibold text-slate-800 ${
                            valErrors.email ? "border-red-400 focus:border-red-450" : "border-slate-200 focus:border-primary/50"
                          }`}
                        />
                        {valErrors.email && (
                          <span className="text-[11.5px] text-red-500 font-bold mt-1 block animate-fade-in">
                            ⚠ {valErrors.email}
                          </span>
                        )}
                      </div>

                      <div className="md:col-span-2 flex flex-col gap-2">
                        <label className="text-slate-500">Địa chỉ trụ sở chính <span className="text-primary">*</span></label>
                        <input
                          type="text"
                          placeholder="VD: 123 Đường Ba Tháng Hai, Quận 10, TP. Hồ Chí Minh"
                          value={address}
                          onChange={(e) => handleFieldChange("address", e.target.value, setAddress)}
                          className={`w-full px-4.5 py-3.5 bg-slate-50 border rounded-xl focus:bg-white focus:outline-none transition-all text-[14px] font-semibold text-slate-800 ${
                            valErrors.address ? "border-red-400 focus:border-red-450" : "border-slate-200 focus:border-primary/50"
                          }`}
                        />
                        {valErrors.address && (
                          <span className="text-[11.5px] text-red-500 font-bold mt-1 block animate-fade-in">
                            ⚠ {valErrors.address}
                          </span>
                        )}
                      </div>

                      <div className="md:col-span-2 flex flex-col gap-2">
                        <label className="text-slate-500">Bài viết giới thiệu chi tiết (About Description) <span className="text-primary">*</span></label>
                        <span className="text-[11px] text-slate-400 font-medium leading-none -mt-1 mb-1">Mẹo: Bạn có thể xuống dòng 2 lần (\n\n) để chia bài viết thành các đoạn văn riêng biệt.</span>
                        <textarea
                          rows={8}
                          placeholder="Nêu chi tiết sứ mệnh, lịch sử hình thành, trang thiết bị công nghệ hàng đầu và cam kết của nha khoa..."
                          value={aboutDescription}
                          onChange={(e) => handleFieldChange("aboutDescription", e.target.value, setAboutDescription)}
                          className={`w-full px-4.5 py-3.5 bg-slate-50 border rounded-xl focus:bg-white focus:outline-none transition-all text-[14px] font-semibold text-slate-800 resize-none leading-relaxed ${
                            valErrors.aboutDescription ? "border-red-400 focus:border-red-450" : "border-slate-200 focus:border-primary/50"
                          }`}
                        />
                        {valErrors.aboutDescription && (
                          <span className="text-[11.5px] text-red-500 font-bold mt-1 block animate-fade-in">
                            ⚠ {valErrors.aboutDescription}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                )}

                {/* ── TAB 2: COVER IMAGE ───────────────────────────────────── */}
                {activeTab === "image" && (
                  <div className="flex flex-col gap-6">
                    <div className="border-b border-slate-100 pb-3">
                      <h2 className="text-[17px] font-black text-slate-800">Ảnh bìa giới thiệu phòng khám</h2>
                      <p className="text-[12px] text-slate-400 font-semibold mt-1">Ảnh này sẽ xuất hiện bên cạnh phần giới thiệu About Us tại trang chủ.</p>
                    </div>

                    <div className="flex flex-col sm:flex-row gap-8 items-start">
                      {/* Image Preview Card */}
                      <div className="w-full sm:w-[350px] aspect-[4/3] rounded-3xl bg-slate-100 border border-slate-200 overflow-hidden relative flex flex-col items-center justify-center shadow-inner group">
                        {imagePreviewUrl ? (
                          <>
                            <img
                              src={imagePreviewUrl}
                              alt="Clinic About Cover"
                              className="w-full h-full object-cover"
                            />
                            <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-3">
                              <button
                                type="button"
                                onClick={handleRemoveImage}
                                className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white font-bold text-[12px] rounded-xl transition-all cursor-pointer shadow-md"
                              >
                                Gỡ ảnh hiện tại
                              </button>
                            </div>
                          </>
                        ) : (
                          <div className="flex flex-col items-center gap-2.5 p-6 text-center text-slate-400 font-semibold">
                            <svg className="w-10 h-10 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.8" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.9 2.9m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
                            </svg>
                            <span className="text-[13px]">Chưa cấu hình ảnh bìa</span>
                            <span className="text-[10px] text-slate-400 font-medium">Hệ thống sẽ hiển thị ảnh mặc định bên landing page</span>
                          </div>
                        )}

                        {uploadingImage && (
                          <div className="absolute inset-0 bg-white/70 backdrop-blur-sm flex flex-col items-center justify-center gap-2">
                            <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
                            <span className="text-[12px] text-slate-600 font-bold">Đang tải ảnh lên...</span>
                          </div>
                        )}
                      </div>

                      {/* Upload Controls */}
                      <div className="flex-1 flex flex-col gap-4 text-[13px] font-semibold text-slate-500">
                        <label className="font-bold text-slate-700">Tải ảnh mới lên</label>
                        <p className="text-[12px] text-slate-400 font-medium leading-relaxed -mt-2">
                          Hỗ trợ định dạng JPG, PNG, WEBP. Dung lượng tối đa 5MB. Nên chọn hình ảnh chất lượng cao chụp không gian thực tế hoặc mặt tiền phòng khám.
                        </p>
                        <div className="flex items-center gap-4">
                          <label className="relative overflow-hidden cursor-pointer bg-slate-900 hover:bg-slate-800 text-white font-bold px-6 py-3 rounded-xl transition-all text-center select-none shadow-md">
                            <span>Chọn tệp ảnh</span>
                            <input
                              type="file"
                              accept="image/*"
                              onChange={handleImageUpload}
                              className="hidden"
                            />
                          </label>
                          {aboutImageUrl && (
                            <button
                              type="button"
                              onClick={handleRemoveImage}
                              className="text-red-600 hover:text-red-700 font-bold hover:underline cursor-pointer"
                            >
                              Xóa ảnh
                            </button>
                          )}
                        </div>
                        
                        {aboutImageUrl && (
                          <div className="mt-4 p-3 bg-slate-50 border border-slate-200 rounded-xl flex flex-col gap-1">
                            <span className="text-[11px] text-slate-400 font-extrabold uppercase">Đường dẫn ảnh hiện tại</span>
                            <span className="text-[12px] text-slate-600 font-mono font-bold select-all break-all">{aboutImageUrl}</span>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                )}

                {/* ── TAB 3: MILESTONES & CERTS ────────────────────────────── */}
                {activeTab === "milestones" && (
                  <div className="flex flex-col gap-8">
                    
                    {/* Milestones section */}
                    <div className="flex flex-col gap-4">
                      <div className="border-b border-slate-100 pb-3 flex justify-between items-end">
                        <div>
                          <h2 className="text-[17px] font-black text-slate-800">Mốc lịch sử phát triển</h2>
                          <p className="text-[12px] text-slate-400 font-semibold mt-1">Các cột mốc tiêu biểu thể hiện sự phát triển của phòng khám qua các năm.</p>
                        </div>
                        <button
                          type="button"
                          onClick={handleAddMilestone}
                          className="flex items-center gap-1.5 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-[13px] font-bold transition-all shadow-sm cursor-pointer"
                        >
                          <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                          </svg>
                          Thêm cột mốc
                        </button>
                      </div>

                      {milestones.length === 0 ? (
                        <div className="bg-slate-50 border border-slate-200/80 rounded-2xl p-6 text-center text-slate-400 font-bold text-[13px]">
                          Chưa có mốc lịch sử nào. Nhấn "Thêm cột mốc" để bắt đầu tạo.
                        </div>
                      ) : (
                        <div className="flex flex-col gap-3">
                          {milestones.map((m, idx) => (
                            <div key={idx} className="flex gap-4 items-start p-4 bg-slate-50 border border-slate-200 rounded-2xl transition-all hover:bg-slate-100/30">
                              <div className="w-24 shrink-0 flex flex-col gap-1 text-[13px] font-bold">
                                <label className="text-[11px] text-slate-400 uppercase leading-none">Năm</label>
                                <input
                                  type="number"
                                  required
                                  value={m.year}
                                  onChange={(e) => handleMilestoneChange(idx, "year", Number(e.target.value))}
                                  className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 text-center font-bold text-[14px]"
                                />
                              </div>
                              <div className="flex-1 flex flex-col gap-1 text-[13px] font-bold">
                                <label className="text-[11px] text-slate-400 uppercase leading-none">Nội dung sự kiện</label>
                                <input
                                  type="text"
                                  required
                                  placeholder="VD: Thành lập chi nhánh thứ 2 tại Quận 3."
                                  value={m.description}
                                  onChange={(e) => handleMilestoneChange(idx, "description", e.target.value)}
                                  className="w-full px-3.5 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 font-semibold text-[14.5px]"
                                />
                              </div>
                              <button
                                type="button"
                                onClick={() => handleRemoveMilestone(idx)}
                                className="mt-5.5 p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-colors cursor-pointer"
                                title="Xóa mốc"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                                </svg>
                              </button>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>

                    {/* Certifications section */}
                    <div className="flex flex-col gap-4 border-t border-slate-100 pt-6">
                      <div>
                        <h2 className="text-[17px] font-black text-slate-800">Chứng nhận & Giải thưởng</h2>
                        <p className="text-[12px] text-slate-400 font-semibold mt-1">Các chứng chỉ chuyên môn, giải thưởng hoặc hiệp hội mà phòng khám tham gia.</p>
                      </div>

                      {/* Add Certification Tag form */}
                      <div className="flex gap-3 text-[13px] max-w-md items-end">
                        <div className="flex-1 flex flex-col gap-2">
                          <label className="font-bold text-slate-500">Tên chứng nhận / giải thưởng</label>
                          <input
                            type="text"
                            placeholder="VD: Đạt chuẩn quốc tế ISO 9001:2015"
                            value={newCert}
                            onChange={(e) => setNewCert(e.target.value)}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") {
                                e.preventDefault();
                                handleAddCert();
                              }
                            }}
                            className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-slate-450 focus:outline-none font-semibold text-slate-800 text-[14px]"
                          />
                        </div>
                        <button
                          type="button"
                          onClick={() => handleAddCert()}
                          className="flex items-center gap-1.5 px-4.5 py-2.5 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-[13px] font-bold transition-all shadow-sm cursor-pointer shrink-0"
                        >
                          <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                          </svg>
                          Thêm chứng nhận
                        </button>
                      </div>

                      {/* Tags List */}
                      <div className="flex flex-wrap gap-2.5 mt-2">
                        {certifications.length === 0 ? (
                          <span className="text-[13px] text-slate-400 font-bold italic">Chưa có chứng nhận nào được thêm.</span>
                        ) : (
                          certifications.map((cert) => (
                            <span
                              key={cert}
                              className="inline-flex items-center gap-1.5 bg-red-50/70 border border-red-200/80 text-primary font-bold text-[12.5px] px-3.5 py-1.5 rounded-full select-none"
                            >
                              <svg className="w-4 h-4 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 18.75h-9m9 0a3 3 0 013 3h-15a3 3 0 013-3m9 0v-3.375c0-.621-.504-1.125-1.125-1.125h-6.75a1.125 1.125 0 00-1.125 1.125v3.375m9 0V9M7.5 18.75V9m.75-6h7.5m-7.5 0a1.5 1.5 0 00-1.5 1.5v3a1.5 1.5 0 001.5 1.5h7.5A1.5 1.5 0 0018 7.5v-3A1.5 1.5 0 0016.5 3m-9 0h9" />
                              </svg>
                              <span>{cert}</span>
                              <button
                                type="button"
                                onClick={() => handleRemoveCert(cert)}
                                className="text-slate-400 hover:text-primary font-black cursor-pointer ml-1 p-0.5"
                              >
                                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                                </svg>
                              </button>
                            </span>
                          ))
                        )}
                      </div>
                    </div>

                  </div>
                )}

                {/* ── TAB 4: FEATURES & STEPS ──────────────────────────────── */}
                {activeTab === "features" && (
                  <div className="flex flex-col gap-8">
                    
                    {/* Features section */}
                    <div className="flex flex-col gap-4">
                      <div className="border-b border-slate-100 pb-3 flex justify-between items-end">
                        <div>
                          <h2 className="text-[17px] font-black text-slate-800">Lý do chọn Sơn Giang (Features)</h2>
                          <p className="text-[12px] text-slate-400 font-semibold mt-1">Các điểm nổi bật, thế mạnh dịch vụ thu hút khách hàng đến khám.</p>
                        </div>
                        <button
                          type="button"
                          onClick={handleAddFeature}
                          className="flex items-center gap-1.5 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-[13px] font-bold transition-all shadow-sm cursor-pointer"
                        >
                          <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                          </svg>
                          Thêm đặc trưng
                        </button>
                      </div>

                      {features.length === 0 ? (
                        <div className="bg-slate-50 border border-slate-200/80 rounded-2xl p-6 text-center text-slate-400 font-bold text-[13px]">
                          Chưa có đặc trưng nào. Nhấn "Thêm đặc trưng" để bắt đầu tạo.
                        </div>
                      ) : (
                        <div className="flex flex-col gap-3">
                          {features.map((f, idx) => (
                            <div key={idx} className="flex gap-4 items-start p-4 bg-slate-50 border border-slate-200 rounded-2xl transition-all hover:bg-slate-100/30">
                              <div className="flex-1 flex flex-col gap-4 text-[13px] font-bold">
                                <div className="flex flex-col gap-1">
                                  <label className="text-[11px] text-slate-400 uppercase leading-none">Tiêu đề ngắn</label>
                                  <input
                                    type="text"
                                    required
                                    placeholder="VD: Đội ngũ Bác sĩ chất lượng cao"
                                    value={f.title}
                                    onChange={(e) => handleFeatureChange(idx, "title", e.target.value)}
                                    className="w-full px-3.5 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 font-bold text-[14.5px]"
                                  />
                                </div>
                                <div className="flex flex-col gap-1">
                                  <label className="text-[11px] text-slate-400 uppercase leading-none">Mô tả cụ thể</label>
                                  <textarea
                                    required
                                    rows={2}
                                    placeholder="VD: Quy tụ các bác sĩ chuyên khoa tốt nghiệp chính quy ĐH Y Dược với nhiều năm kinh nghiệm thực tế."
                                    value={f.description}
                                    onChange={(e) => handleFeatureChange(idx, "description", e.target.value)}
                                    className="w-full px-3.5 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 font-semibold text-[13.5px] resize-none"
                                  />
                                </div>
                              </div>
                              <button
                                type="button"
                                onClick={() => handleRemoveFeature(idx)}
                                className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-colors cursor-pointer shrink-0 mt-5.5"
                                title="Xóa"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                                </svg>
                              </button>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>

                    {/* Treatment steps section */}
                    <div className="flex flex-col gap-4 border-t border-slate-100 pt-6">
                      <div className="border-b border-slate-100 pb-3 flex justify-between items-end">
                        <div>
                          <h2 className="text-[17px] font-black text-slate-800">Quy trình điều trị chuẩn y khoa</h2>
                          <p className="text-[12px] text-slate-400 font-semibold mt-1">Các bước thăm khám cơ bản của bệnh nhân tại nha khoa.</p>
                        </div>
                        <button
                          type="button"
                          onClick={handleAddStep}
                          className="flex items-center gap-1.5 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-[13px] font-bold transition-all shadow-sm cursor-pointer"
                        >
                          <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                          </svg>
                          Thêm bước điều trị
                        </button>
                      </div>

                      {treatmentSteps.length === 0 ? (
                        <div className="bg-slate-50 border border-slate-200/80 rounded-2xl p-6 text-center text-slate-400 font-bold text-[13px]">
                          Chưa có bước điều trị nào. Nhấn "Thêm bước điều trị" để bắt đầu tạo.
                        </div>
                      ) : (
                        <div className="flex flex-col gap-3">
                          {treatmentSteps.map((step, idx) => (
                            <div key={idx} className="flex gap-4 items-start p-4 bg-slate-50 border border-slate-200 rounded-2xl transition-all hover:bg-slate-100/30">
                              <div className="flex-1 flex flex-col gap-4 text-[13px] font-bold">
                                <div className="flex flex-col gap-1">
                                  <label className="text-[11px] text-slate-400 uppercase leading-none">Tên bước (VD: Bước {idx + 1})</label>
                                  <input
                                    type="text"
                                    required
                                    placeholder={`VD: Thăm khám và Chẩn đoán`}
                                    value={step.title}
                                    onChange={(e) => handleStepChange(idx, "title", e.target.value)}
                                    className="w-full px-3.5 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 font-bold text-[14.5px]"
                                  />
                                </div>
                                <div className="flex flex-col gap-1">
                                  <label className="text-[11px] text-slate-400 uppercase leading-none">Nội dung chi tiết</label>
                                  <textarea
                                    required
                                    rows={2}
                                    placeholder="VD: Bác sĩ tiến hành khám tổng quát khoang miệng, chụp X-quang nếu cần thiết để chẩn đoán chính xác tình trạng."
                                    value={step.description}
                                    onChange={(e) => handleStepChange(idx, "description", e.target.value)}
                                    className="w-full px-3.5 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 font-semibold text-[13.5px] resize-none"
                                  />
                                </div>
                              </div>
                              <button
                                type="button"
                                onClick={() => handleRemoveStep(idx)}
                                className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-colors cursor-pointer shrink-0 mt-5.5"
                                title="Xóa"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                                </svg>
                              </button>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>

                  </div>
                )}

                {/* ── TAB 5: STATISTICS ────────────────────────────────────── */}
                {activeTab === "stats" && (
                  <div className="flex flex-col gap-6">
                    <div className="border-b border-slate-100 pb-3 flex justify-between items-end">
                      <div>
                        <h2 className="text-[17px] font-black text-slate-800">Các con số ấn tượng (Statistics)</h2>
                        <p className="text-[12px] text-slate-400 font-semibold mt-1">Các thông số thống kê nổi bật chứng minh chất lượng và uy tín của nha khoa.</p>
                      </div>
                      <button
                        type="button"
                        onClick={handleAddStat}
                        className="flex items-center gap-1.5 px-4 py-2 bg-white border border-slate-250 hover:bg-slate-50 text-slate-600 rounded-xl text-[13px] font-bold transition-all shadow-sm cursor-pointer"
                      >
                        <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                        </svg>
                        Thêm chỉ số
                      </button>
                    </div>

                    {statistics.length === 0 ? (
                      <div className="bg-slate-50 border border-slate-200/80 rounded-2xl p-6 text-center text-slate-400 font-bold text-[13px]">
                        Chưa có số liệu thống kê nào. Nhấn "Thêm chỉ số" để bắt đầu tạo.
                      </div>
                    ) : (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {statistics.map((st, idx) => (
                          <div key={idx} className="flex gap-4 items-start p-4 bg-slate-50 border border-slate-200 rounded-2xl transition-all hover:bg-slate-100/30">
                            <div className="flex-1 flex flex-col gap-3 text-[13px] font-bold">
                              <div className="flex flex-col gap-1">
                                <label className="text-[11px] text-slate-400 uppercase leading-none">Giá trị số / Ký tự nổi bật</label>
                                <input
                                  type="text"
                                  required
                                  placeholder="VD: 15.000+ hoặc 99.8%"
                                  value={st.value}
                                  onChange={(e) => handleStatChange(idx, "value", e.target.value)}
                                  className="w-full px-3.5 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 font-black text-slate-900 text-[15px]"
                                />
                              </div>
                              <div className="flex flex-col gap-1">
                                <label className="text-[11px] text-slate-400 uppercase leading-none">Nhãn giải thích</label>
                                <input
                                  type="text"
                                  required
                                  placeholder="VD: Khách hàng hài lòng hoặc Tỷ lệ thành công"
                                  value={st.label}
                                  onChange={(e) => handleStatChange(idx, "label", e.target.value)}
                                  className="w-full px-3.5 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:border-slate-400 font-semibold text-slate-700 text-[13.5px]"
                                />
                              </div>
                            </div>
                            <button
                              type="button"
                              onClick={() => handleRemoveStat(idx)}
                              className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-colors cursor-pointer shrink-0 mt-5.5"
                              title="Xóa"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                              </svg>
                            </button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )}

              </div>

              {/* Action buttons footer */}
              <div className="flex items-center justify-end gap-3 pt-6 border-t border-slate-200 shrink-0">
                <button
                  type="submit"
                  disabled={isSaving || uploadingImage}
                  className="px-8 py-3 bg-primary hover:bg-primary-hover text-white rounded-xl text-[14px] font-bold shadow-md shadow-primary/15 hover:shadow-lg transition-all cursor-pointer flex items-center justify-center gap-2 disabled:opacity-50 hover:translate-y-[-1px]"
                >
                  {isSaving ? (
                    <>
                      <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                      <span>Đang lưu...</span>
                    </>
                  ) : (
                    <span>Lưu tất cả thay đổi</span>
                  )}
                </button>
              </div>
            </form>
          )}
        </div>
      </main>

      {/* ── TOAST NOTIFICATION ───────────────── */}
      {toast && (
        <div className={`fixed top-6 right-6 z-[9999] px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border font-bold text-[14px] max-w-md animate-fade-in ${
          toast.type === "success" ? "bg-emerald-900 text-white border-emerald-800"
          : "bg-red-900 text-white border-red-800"
        }`}>
          {toast.type === "success" ? (
            <svg className="w-5 h-5 text-white shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
            </svg>
          ) : (
            <svg className="w-5 h-5 text-white shrink-0" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
            </svg>
          )}
          <span>{toast.message}</span>
        </div>
      )}
    </div>
  );
}
