"use client";

import React, { useState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { getPostById, updatePost, Post } from "../../../../../components/postsDb";

const CATEGORIES = [
  "Chăm sóc răng miệng",
  "Niềng răng",
  "Phục hình",
  "Khuyến mãi",
  "Lời khuyên nha khoa"
];

interface EditPostPageProps {
  params: Promise<{ id: string }>;
}

export default function EditPostPage({ params }: EditPostPageProps) {
  const router = useRouter();
  
  // Unwrap parameters
  const resolvedParams = React.use(params);
  const id = resolvedParams.id;

  // Form states
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [category, setCategory] = useState("");
  const [thumbnail, setThumbnail] = useState("");
  const [author, setAuthor] = useState("");
  const [date, setDate] = useState("");
  const [status, setStatus] = useState<"Đã xuất bản" | "Bản nháp">("Bản nháp");
  
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  const fileInputRef = useRef<HTMLInputElement>(null);

  // Load post details
  useEffect(() => {
    const post = getPostById(id);
    if (post) {
      setTitle(post.title);
      setContent(post.content);
      setCategory(post.category);
      setThumbnail(post.thumbnail);
      setAuthor(post.author);
      setDate(post.date);
      setStatus(post.status);
    } else {
      setErrorMessage("Không tìm thấy bài viết trong hệ thống.");
    }
    setIsLoading(false);
  }, [id]);

  // Image upload
  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      if (!file.type.startsWith("image/")) {
        setErrorMessage("Chỉ hỗ trợ file hình ảnh (JPG, PNG, WEBP,...)");
        return;
      }
      setErrorMessage("");
      const reader = new FileReader();
      reader.onload = (event) => {
        if (event.target?.result) {
          setThumbnail(event.target.result as string);
        }
      };
      reader.readAsDataURL(file);
    }
  };

  const triggerFileInput = () => {
    fileInputRef.current?.click();
  };

  // Submit Handler
  const handleSaveChanges = () => {
    if (!title.trim()) {
      setErrorMessage("Tiêu đề bài viết không được để trống.");
      return;
    }
    if (!category) {
      setErrorMessage("Vui lòng chọn danh mục.");
      return;
    }
    if (!content.trim()) {
      setErrorMessage("Nội dung bài viết không được để trống.");
      return;
    }

    const updated = updatePost(id, {
      title,
      content,
      category,
      thumbnail,
      status
    });

    if (updated) {
      router.push("/manager/posts");
    } else {
      setErrorMessage("Đã xảy ra lỗi khi lưu bài viết.");
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[300px] text-slate-400 font-bold">
        Đang tải thông tin bài viết...
      </div>
    );
  }

  if (errorMessage && !title) {
    return (
      <div className="flex flex-col gap-4 items-center justify-center min-h-[300px]">
        <div className="text-primary font-bold text-lg">{errorMessage}</div>
        <Link href="/manager/posts" className="px-4 py-2 bg-primary text-white rounded-xl font-bold text-[14px]">
          Quay lại danh sách
        </Link>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      
      {/* Title Bar & Back button */}
      <div className="flex items-center justify-between">
        <Link
          href="/manager/posts"
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
        
        {/* Left Column (Edit Form) - 7 cols */}
        <div className="lg:col-span-7 flex flex-col gap-6">
          <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
            
            {/* Title */}
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

            {/* Content editor */}
            <div className="flex flex-col gap-2">
              <label className="text-[14px] font-extrabold text-slate-800">Nội dung bài viết</label>
              <div className="border border-slate-200 rounded-xl overflow-hidden flex flex-col">
                {/* Toolbar */}
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
                {/* Textarea */}
                <textarea
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder="Nhập nội dung bài viết..."
                  rows={10}
                  className="w-full p-4 focus:outline-none transition-all font-medium text-slate-700 text-[15px] resize-y min-h-[260px]"
                />
              </div>
            </div>

            {/* Category selection */}
            <div className="flex flex-col gap-2">
              <label className="text-[14px] font-extrabold text-slate-800">Danh mục</label>
              <div className="relative">
                <select
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  className="w-full appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-10 py-3 rounded-xl border border-slate-200 hover:border-slate-350 focus:border-primary/50 focus:outline-none transition-all cursor-pointer"
                >
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

            {/* Status Option */}
            <div className="flex flex-col gap-2">
              <label className="text-[14px] font-extrabold text-slate-800">Trạng thái bài viết</label>
              <div className="flex gap-4">
                <label className="inline-flex items-center gap-2 cursor-pointer text-[14px] font-bold text-slate-700">
                  <input
                    type="radio"
                    name="status"
                    value="Đã xuất bản"
                    checked={status === "Đã xuất bản"}
                    onChange={() => setStatus("Đã xuất bản")}
                    className="w-4 h-4 text-primary focus:ring-primary"
                  />
                  Đã xuất bản
                </label>
                <label className="inline-flex items-center gap-2 cursor-pointer text-[14px] font-bold text-slate-700">
                  <input
                    type="radio"
                    name="status"
                    value="Bản nháp"
                    checked={status === "Bản nháp"}
                    onChange={() => setStatus("Bản nháp")}
                    className="w-4 h-4 text-primary focus:ring-primary"
                  />
                  Bản nháp
                </label>
              </div>
            </div>

            {/* Thumbnail selector */}
            <div className="flex flex-col gap-2">
              <label className="text-[14px] font-extrabold text-slate-800">Ảnh đại diện</label>
              <div className="flex items-center gap-4">
                <div className="w-24 h-16 rounded-lg overflow-hidden border border-slate-200 bg-slate-50 shrink-0">
                  {thumbnail ? (
                    <img src={thumbnail} alt="Thumbnail" className="w-full h-full object-cover" />
                  ) : (
                    <span className="w-full h-full flex items-center justify-center text-slate-350 text-xs">No Image</span>
                  )}
                </div>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  onChange={handleFileInput}
                  className="hidden"
                />
                <button
                  type="button"
                  onClick={triggerFileInput}
                  className="px-4 py-2 bg-white hover:bg-slate-50 text-slate-800 border border-slate-200 rounded-xl font-bold text-[13px] transition-all cursor-pointer shadow-sm"
                >
                  Tải lên ảnh mới
                </button>
              </div>
            </div>

            {/* Actions Form */}
            <div className="flex items-center gap-3 border-t border-slate-100 pt-4 mt-2">
              <button
                type="button"
                onClick={handleSaveChanges}
                className="flex-1 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] py-3 rounded-xl transition-all shadow-md shadow-primary/25 cursor-pointer text-center"
              >
                Lưu thay đổi
              </button>
              <Link
                href="/manager/posts"
                className="flex-1 border border-primary text-primary hover:bg-red-50/40 font-bold text-[14px] py-3 rounded-xl transition-all cursor-pointer text-center"
              >
                Hủy
              </Link>
            </div>

          </div>
        </div>

        {/* Right Column (Live Article Preview) - 5 cols */}
        <div className="lg:col-span-5 sticky top-24">
          <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-4">
            <h3 className="text-[16px] font-black text-slate-900 border-b border-slate-100 pb-3">Xem trước bài viết</h3>
            
            {/* Image Preview */}
            <div className="w-full h-48 rounded-xl overflow-hidden bg-slate-100 border border-slate-200/40 shadow-inner flex items-center justify-center relative">
              {thumbnail ? (
                <img
                  src={thumbnail}
                  alt="Post Cover"
                  className="w-full h-full object-cover"
                />
              ) : (
                <span className="text-slate-400 font-semibold text-[14px]">Hình ảnh bài viết</span>
              )}
            </div>

            {/* Title Preview */}
            <h1 className="text-xl font-black text-slate-900 leading-snug tracking-tight">
              {title || "Chưa có tiêu đề bài viết"}
            </h1>

            {/* Metadata Preview */}
            <div className="flex items-center gap-4 text-[13px] text-slate-400 font-semibold">
              {category && (
                <span className="text-primary font-extrabold">{category}</span>
              )}
              <div className="flex items-center gap-1">
                <svg className="w-4 h-4 text-slate-350" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>ngày {date || "hôm nay"}</span>
              </div>
            </div>

            {/* Author Preview */}
            <div className="text-[13px] text-slate-450 font-bold">
              Tác giả: <span className="text-slate-700">{author || "BS. Nguyễn Minh Đức"}</span>
            </div>

            {/* Content Preview */}
            <div className="text-[14px] text-slate-500 font-medium leading-relaxed max-h-48 overflow-y-auto pr-1 whitespace-pre-line border-t border-slate-100 pt-3">
              {content || "Nội dung bài viết sẽ được hiển thị trực quan tại đây..."}
            </div>

          </div>
        </div>

      </div>

    </div>
  );
}
