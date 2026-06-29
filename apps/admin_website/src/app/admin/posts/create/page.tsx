"use client";

import React, { useState, useRef } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { createPostApi } from "../../../../lib/apiClient";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import NotificationBell from "../../../../components/shared/NotificationBell";

const CATEGORIES = [
  "Chăm sóc răng miệng",
  "Niềng răng",
  "Phục hình",
  "Khuyến mãi",
  "Lời khuyên nha khoa"
];

export default function CreatePostPage() {
  useRequireAdmin();
  const router = useRouter();
  
  // Form state
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [category, setCategory] = useState("");
  const [thumbnail, setThumbnail] = useState("");
  const [dragActive, setDragActive] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Parse file to Base64 URL
  const handleFile = (file: File) => {
    if (!file.type.startsWith("image/")) {
      setErrorMessage("Chỉ hỗ trợ file hình ảnh (JPG, PNG, WEBP,...)");
      return;
    }
    setErrorMessage("");
    const reader = new FileReader();
    reader.onload = (e) => {
      if (e.target?.result) {
        setThumbnail(e.target.result as string);
      }
    };
    reader.readAsDataURL(file);
  };

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFile(e.dataTransfer.files[0]);
    }
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      handleFile(e.target.files[0]);
    }
  };

  const triggerFileInput = () => {
    fileInputRef.current?.click();
  };

  const handleSave = async (isPublished: boolean) => {
    if (!title.trim()) { setErrorMessage("Vui lòng nhập tiêu đề bài viết."); return; }
    if (!category) { setErrorMessage("Vui lòng chọn danh mục bài viết."); return; }
    if (!content.trim()) { setErrorMessage("Vui lòng nhập nội dung bài viết."); return; }
    setIsSaving(true);
    try {
      await createPostApi({
        title,
        category,
        content,
        author: "ThS. BS. Nguyễn Minh Đức",
        thumbnailUrl: thumbnail || null,
        isPublished,
      });
      router.push("/admin/posts");
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : "Tạo bài viết thất bại.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      
      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <AdminSidebar activeMenu="articles" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">
              Tạo Bài viết Mới
            </h1>
          </div>

          {/* Search, Notifications */}
          <div className="flex items-center gap-6">
            {/* Search Input */}
            <div className="relative w-64 hidden sm:block">
              <span className="absolute inset-y-0 left-3 flex items-center text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Tìm kiếm nhanh..."
                className="w-full pl-9 pr-4 py-2 text-[15px] bg-slate-100 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all"
              />
            </div>

            <NotificationBell />
          </div>
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">
          
          <div className="flex flex-col gap-6">
            
            {/* Title Bar & Back button */}
            <div className="flex items-center justify-between">
              <Link
                href="/admin/posts"
                className="inline-flex items-center gap-2 text-slate-500 hover:text-primary font-bold text-[14px] transition-all"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
                Quay lại danh sách
              </Link>
            </div>

            {errorMessage && (
              <div className="bg-red-50 border border-red-200 text-primary px-4 py-3 rounded-xl text-[14px] font-bold">
                {errorMessage}
              </div>
            )}

            {/* Main Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
              
              {/* Left Column (Inputs: Title & Content) - takes 8 cols */}
              <div className="lg:col-span-8 flex flex-col gap-6">
                <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                  
                  {/* Title Input */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[14px] font-extrabold text-slate-800">Tiêu đề bài viết</label>
                    <input
                      type="text"
                      value={title}
                      onChange={(e) => setTitle(e.target.value)}
                      placeholder="Nhập tiêu đề bài viết..."
                      className="w-full px-4 py-3 border border-slate-200 rounded-xl focus:border-primary/50 focus:outline-none transition-all font-semibold text-slate-800 text-[15px]"
                    />
                  </div>

                  {/* Rich Editor Box */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[14px] font-extrabold text-slate-800">Nội dung bài viết</label>
                    
                    <div className="border border-slate-200 rounded-xl overflow-hidden flex flex-col">
                      
                      {/* Formatting Toolbar */}
                      <div className="bg-slate-50 border-b border-slate-200 p-2 flex flex-wrap items-center gap-1">
                        <button type="button" className="p-1.5 rounded-lg text-slate-550 hover:text-slate-800 hover:bg-slate-200/50 transition-all font-extrabold text-[13px] w-8 h-8 flex items-center justify-center">B</button>
                        <button type="button" className="p-1.5 rounded-lg text-slate-550 hover:text-slate-800 hover:bg-slate-200/50 transition-all italic text-[13px] w-8 h-8 flex items-center justify-center">I</button>
                        <button type="button" className="p-1.5 rounded-lg text-slate-550 hover:text-slate-800 hover:bg-slate-200/50 transition-all underline text-[13px] w-8 h-8 flex items-center justify-center">U</button>
                        <span className="w-[1px] h-5 bg-slate-200 mx-1"></span>
                        <button type="button" className="p-1.5 rounded-lg text-slate-550 hover:text-slate-800 hover:bg-slate-200/50 transition-all w-8 h-8 flex items-center justify-center">
                          <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zM3.75 12h.007v.008H3.75V12zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm-.375 5.25h.007v.008H3.75v-.008zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
                          </svg>
                        </button>
                        <button type="button" className="p-1.5 rounded-lg text-slate-550 hover:text-slate-800 hover:bg-slate-200/50 transition-all w-8 h-8 flex items-center justify-center">
                          <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 5.25h16.5m-16.5 4.5h16.5m-16.5 4.5h16.5m-16.5 4.5h16.5" />
                          </svg>
                        </button>
                        <span className="w-[1px] h-5 bg-slate-200 mx-1"></span>
                        <button type="button" className="p-1.5 rounded-lg text-slate-550 hover:text-slate-800 hover:bg-slate-200/50 transition-all w-8 h-8 flex items-center justify-center">
                          <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M13.19 8.688a4.5 4.5 0 011.242 7.244l-4.5 4.5a4.5 4.5 0 01-6.364-6.364l1.757-1.757m13.35-.622l1.757-1.757a4.5 4.5 0 00-6.364-6.364l-4.5 4.5a4.5 4.5 0 001.242 7.244" />
                          </svg>
                        </button>
                        <button type="button" className="p-1.5 rounded-lg text-slate-550 hover:text-slate-800 hover:bg-slate-200/50 transition-all w-8 h-8 flex items-center justify-center" onClick={triggerFileInput}>
                          <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
                          </svg>
                        </button>
                      </div>

                      {/* Text Area */}
                      <textarea
                        value={content}
                        onChange={(e) => setContent(e.target.value)}
                        placeholder="Viết nội dung bài viết tại đây..."
                        rows={12}
                        className="w-full p-4 focus:outline-none transition-all font-medium text-slate-700 text-[15px] resize-y min-h-[300px]"
                      />
                    </div>

                  </div>
                </div>
              </div>

              {/* Right Column (Category & Thumbnail & Actions) - takes 4 cols */}
              <div className="lg:col-span-4 flex flex-col gap-6">
                
                {/* Metadata Box */}
                <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                  
                  {/* Category Dropdown */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[14px] font-extrabold text-slate-800">Danh mục bài viết</label>
                    <div className="relative">
                      <select
                        value={category}
                        onChange={(e) => setCategory(e.target.value)}
                        className="w-full appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-10 py-3 rounded-xl border border-slate-200 hover:border-slate-350 focus:border-primary/50 focus:outline-none transition-all cursor-pointer"
                      >
                        <option value="" disabled>Chọn danh mục</option>
                        {CATEGORIES.map((cat, idx) => (
                          <option key={idx} value={cat}>
                            {cat}
                          </option>
                        ))}
                      </select>
                      <div className="pointer-events-none absolute inset-y-0 right-3.5 flex items-center text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                        </svg>
                      </div>
                    </div>
                  </div>

                  {/* Thumbnail Drag & Drop Uploader */}
                  <div className="flex flex-col gap-2">
                    <label className="text-[14px] font-extrabold text-slate-800">Hình ảnh bài viết</label>
                    
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*"
                      onChange={handleFileInput}
                      className="hidden"
                    />

                    {thumbnail ? (
                      // Selected Thumbnail Preview
                      <div className="relative border border-slate-200 rounded-xl overflow-hidden group">
                        <img
                          src={thumbnail}
                          alt="Thumbnail Preview"
                          className="w-full h-40 object-cover"
                        />
                        <div className="absolute inset-0 bg-slate-900/40 opacity-0 group-hover:opacity-100 flex items-center justify-center gap-3 transition-opacity duration-200">
                          <button
                            type="button"
                            onClick={triggerFileInput}
                            className="px-3.5 py-1.5 bg-white text-slate-800 rounded-lg font-bold text-[12px] hover:bg-slate-100 transition-all cursor-pointer"
                          >
                            Thay đổi
                          </button>
                          <button
                            type="button"
                            onClick={() => setThumbnail("")}
                            className="px-3.5 py-1.5 bg-primary text-white rounded-lg font-bold text-[12px] hover:bg-primary-hover transition-all cursor-pointer"
                          >
                            Xóa bỏ
                          </button>
                        </div>
                      </div>
                    ) : (
                      // Drag and Drop Zone
                      <div
                        onDragEnter={handleDrag}
                        onDragOver={handleDrag}
                        onDragLeave={handleDrag}
                        onDrop={handleDrop}
                        onClick={triggerFileInput}
                        className={`border-2 border-dashed rounded-xl p-6 flex flex-col items-center justify-center text-center cursor-pointer transition-all min-h-[160px] ${
                          dragActive
                            ? "border-primary bg-red-50/10"
                            : "border-slate-200 hover:border-slate-300 hover:bg-slate-50/50"
                        }`}
                      >
                        <span className="text-3xl text-slate-400 mb-2">
                          <svg className="w-10 h-10 mx-auto text-slate-350" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 16.5V9.75m0 0l3 3m-3-3l-3 3M6.75 19.5a4.5 4.5 0 01-1.41-8.775 5.25 5.25 0 0110.233-2.33 3 3 0 013.758 3.848A3.752 3.752 0 0118 19.5H6.75z" />
                          </svg>
                        </span>
                        <span className="text-[13px] font-extrabold text-slate-700">Hình ảnh bài viết</span>
                        <span className="text-[11px] text-slate-450 mt-1">Kéo thả hình ảnh hoặc nhấn để tải lên</span>
                        <span className="text-[11px] text-slate-400">(JPG, PNG)</span>
                      </div>
                    )}

                  </div>

                  {/* Actions Form */}
                  <div className="flex flex-col gap-3 border-t border-slate-100 pt-4">
                    <button
                      type="button"
                      disabled={isSaving}
                      onClick={() => handleSave(true)}
                      className="w-full bg-primary hover:bg-primary-hover text-white font-bold text-[14px] py-3 rounded-xl transition-all shadow-md shadow-primary/25 cursor-pointer text-center disabled:opacity-60"
                    >
                      {isSaving ? "Đang lưu..." : "Xuất bản"}
                    </button>

                    <button
                      type="button"
                      disabled={isSaving}
                      onClick={() => handleSave(false)}
                      className="w-full bg-slate-200 hover:bg-slate-300 text-slate-700 font-bold text-[14px] py-3 rounded-xl transition-all cursor-pointer text-center disabled:opacity-60"
                    >
                      Lưu nháp
                    </button>
                  </div>

                </div>

              </div>

            </div>

          </div>

        </div>
      </main>

    </div>
  );
}
