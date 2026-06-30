"use client";

import React, { useState, useEffect, useMemo } from "react";
import OwnerSidebar from "../../../components/shared/OwnerSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireOwner } from "../../../hooks/useRequireOwner";
import {
  getFeedbacksApi,
  featureFeedbackApi,
  replyFeedbackApi,
  type FeedbackDto,
} from "../../../lib/apiClient";

export default function OwnerFeedbackPage() {
  useRequireOwner();

  const [feedbacks, setFeedbacks] = useState<FeedbackDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [ratingFilter, setRatingFilter] = useState("All");
  const [statusFilter, setStatusFilter] = useState("All");
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(5);

  const [replyTarget, setReplyTarget] = useState<FeedbackDto | null>(null);
  const [replyContent, setReplyContent] = useState("");

  const [toast, setToast] = useState<{ show: boolean; message: string; isError?: boolean } | null>(null);

  const showToast = (message: string, isError = false) => {
    setToast({ show: true, message, isError });
    setTimeout(() => setToast(null), 3000);
  };

  useEffect(() => {
    async function load() {
      setIsLoading(true);
      try {
        const data = await getFeedbacksApi();
        setFeedbacks(data);
      } catch {
        showToast("Không thể tải danh sách phản hồi.", true);
      } finally {
        setIsLoading(false);
      }
    }
    load();
  }, []);

  const stats = useMemo(() => ({
    total: feedbacks.length,
    featured: feedbacks.filter((f) => f.status === "Featured").length,
    replied: feedbacks.filter((f) => !!f.replyText).length,
    avgRating: feedbacks.length > 0
      ? (feedbacks.reduce((sum, f) => sum + f.rating, 0) / feedbacks.length).toFixed(1)
      : "0.0",
  }), [feedbacks]);

  const handleFeature = async (id: string) => {
    try {
      const updated = await featureFeedbackApi(id);
      setFeedbacks((prev) => prev.map((fb) => (fb.id === id ? updated : fb)));
      showToast("Đã cập nhật trạng thái nổi bật!");
    } catch {
      showToast("Có lỗi xảy ra. Vui lòng thử lại.", true);
    }
  };

  const handleReplyClick = (fb: FeedbackDto) => {
    setReplyTarget(fb);
    setReplyContent(fb.replyText ?? "");
  };

  const handleReplySubmit = async () => {
    if (!replyTarget) return;
    try {
      const updated = await replyFeedbackApi(replyTarget.id, { replyText: replyContent });
      setFeedbacks((prev) => prev.map((fb) => (fb.id === replyTarget.id ? updated : fb)));
      setReplyTarget(null);
      setReplyContent("");
      showToast("Đã gửi câu trả lời phản hồi thành công!");
    } catch {
      showToast("Có lỗi xảy ra. Vui lòng thử lại.", true);
    }
  };

  const filteredFeedbacks = useMemo(() => {
    return feedbacks.filter((fb) => {
      const matchesSearch =
        fb.customerName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        fb.comment.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesRating =
        ratingFilter === "All" || fb.rating === parseInt(ratingFilter);
      const matchesStatus =
        statusFilter === "All" || fb.status === statusFilter;
      return matchesSearch && matchesRating && matchesStatus;
    });
  }, [feedbacks, searchQuery, ratingFilter, statusFilter]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, ratingFilter, statusFilter, itemsPerPage]);

  const totalPages = Math.max(1, Math.ceil(filteredFeedbacks.length / itemsPerPage));
  const startIndex = (currentPage - 1) * itemsPerPage;
  const paginatedFeedbacks = filteredFeedbacks.slice(startIndex, startIndex + itemsPerPage);

  const getInitials = (name: string) =>
    name.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase();

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">

      <OwnerSidebar activeMenu="feedback" />

      <main className="flex-1 flex flex-col min-w-0">

        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Phản hồi & Đánh giá</h1>
          </div>
          <div className="flex items-center gap-6">
            <NotificationBell href="/owner/notifications" />
          </div>
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* TOAST */}
          {toast?.show && (
            <div className="fixed top-6 right-6 z-[100] animate-fade-in">
              <div className={`bg-white border rounded-2xl shadow-xl shadow-slate-200/60 p-4 flex items-center gap-3 max-w-sm ${toast.isError ? "border-red-200" : "border-green-200"}`}>
                <div className={`w-9 h-9 rounded-full flex items-center justify-center shrink-0 ${toast.isError ? "bg-red-100" : "bg-green-100"}`}>
                  {toast.isError ? (
                    <svg className="w-5 h-5 text-red-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                    </svg>
                  ) : (
                    <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  )}
                </div>
                <span className="text-[13px] font-black text-slate-900">{toast.message}</span>
              </div>
            </div>
          )}

          {/* STATS GRID */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Tổng số phản hồi</span>
                <span className="text-3xl font-black text-slate-900 block mt-1">{stats.total}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0012 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-yellow-400/60 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Nổi bật</span>
                <span className="text-3xl font-black text-yellow-500 block mt-1">{stats.featured}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-yellow-50 text-yellow-500 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-green-400/60 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Đã trả lời</span>
                <span className="text-3xl font-black text-green-600 block mt-1">{stats.replied}</span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-green-50 text-green-500 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 15L3 9m0 0l6-6M3 9h12a6 6 0 010 12h-3" />
                </svg>
              </div>
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-orange-400/60 transition-all duration-200">
              <div>
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">Điểm trung bình</span>
                <span className="text-3xl font-black text-orange-500 block mt-1">
                  {stats.avgRating}<span className="text-lg font-bold text-slate-400 ml-1">/ 5</span>
                </span>
              </div>
              <div className="w-12 h-12 rounded-xl bg-orange-50 text-orange-400 flex items-center justify-center shrink-0">
                <svg className="w-6 h-6 fill-orange-400" viewBox="0 0 24 24">
                  <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
                </svg>
              </div>
            </div>
          </div>

          {/* FILTER BAR */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3 shrink-0">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
              <div className="relative flex-1 max-w-md">
                <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </span>
                <input
                  type="text"
                  placeholder="Tìm kiếm phản hồi..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 text-[15px] bg-slate-100/80 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all font-semibold text-slate-800"
                />
              </div>

              <div className="flex items-center gap-3">
                <div className="relative">
                  <select
                    value={ratingFilter}
                    onChange={(e) => setRatingFilter(e.target.value)}
                    className="appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-10 py-2.5 rounded-xl border border-slate-200 focus:outline-none transition-all cursor-pointer"
                  >
                    <option value="All">Lọc theo Đánh giá</option>
                    <option value="5">5 Sao</option>
                    <option value="4">4 Sao</option>
                    <option value="3">3 Sao</option>
                    <option value="2">2 Sao</option>
                    <option value="1">1 Sao</option>
                  </select>
                  <div className="pointer-events-none absolute inset-y-0 right-3.5 flex items-center text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </div>
                </div>

                <div className="relative">
                  <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-10 py-2.5 rounded-xl border border-slate-200 focus:outline-none transition-all cursor-pointer"
                  >
                    <option value="All">Lọc theo Trạng thái</option>
                    <option value="Pending">Chờ phê duyệt</option>
                    <option value="Featured">Nổi bật</option>
                    <option value="Hidden">Đã ẩn</option>
                  </select>
                  <div className="pointer-events-none absolute inset-y-0 right-3.5 flex items-center text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </div>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2 text-[13px] text-slate-500 font-semibold border-t border-slate-100 pt-3">
              <span>Hiển thị</span>
              <div className="relative">
                <select
                  value={itemsPerPage}
                  onChange={(e) => setItemsPerPage(Number(e.target.value))}
                  className="appearance-none bg-white border border-slate-200 rounded-lg pl-3 pr-7 py-1 text-[13px] font-bold text-slate-700 focus:outline-none cursor-pointer"
                >
                  <option value={5}>5</option>
                  <option value={10}>10</option>
                  <option value={25}>25</option>
                  <option value={50}>50</option>
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                  <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </div>
              </div>
              <span>/ trang</span>
              <span className="text-slate-300 mx-1">·</span>
              <span>Tìm thấy <span className="font-black text-slate-700">{filteredFeedbacks.length}</span> kết quả</span>
            </div>
          </div>

          {/* TABLE */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-[14px]">
                <thead>
                  <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
                    <th className="px-6 py-4">Tên khách hàng</th>
                    <th className="px-6 py-4">Đánh giá</th>
                    <th className="px-6 py-4">Bình luận</th>
                    <th className="px-6 py-4">Ngày</th>
                    <th className="px-6 py-4 text-center">Hành động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                  {isLoading ? (
                    <tr>
                      <td colSpan={5} className="px-6 py-12 text-center text-slate-400 font-semibold">
                        Đang tải...
                      </td>
                    </tr>
                  ) : paginatedFeedbacks.length > 0 ? (
                    paginatedFeedbacks.map((fb) => (
                      <tr key={fb.id} className="hover:bg-slate-50/40 transition-colors">

                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-red-50 border border-primary/20 text-primary flex items-center justify-center font-bold text-[13px] shrink-0 shadow-sm">
                              {getInitials(fb.customerName)}
                            </div>
                            <div className="flex flex-col gap-0.5">
                              <span className="font-bold text-slate-800">{fb.customerName}</span>
                              <div>
                                {fb.status === "Featured" && (
                                  <span className="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-bold rounded-full bg-yellow-50 text-yellow-600 border border-yellow-100">
                                    <svg className="w-2.5 h-2.5 fill-yellow-500" viewBox="0 0 24 24"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" /></svg>
                                    Nổi bật
                                  </span>
                                )}
                              </div>
                            </div>
                          </div>
                        </td>

                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center gap-0.5">
                            {Array.from({ length: 5 }).map((_, idx) => (
                              <svg
                                key={idx}
                                className={`w-4 h-4 ${idx < fb.rating ? "text-amber-400 fill-amber-400" : "text-slate-200 fill-slate-200"}`}
                                viewBox="0 0 24 24"
                              >
                                <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
                              </svg>
                            ))}
                          </div>
                        </td>

                        <td className="px-6 py-4 max-w-lg">
                          <div className="flex flex-col gap-1.5">
                            <p className="text-slate-700 font-semibold text-[14px] leading-relaxed">{fb.comment}</p>
                            {fb.replyText && (
                              <div className="bg-slate-50 border border-slate-150/60 rounded-xl p-3.5 mt-1.5 shadow-sm">
                                <div className="flex items-center gap-1.5 text-slate-400 text-[10px] font-extrabold uppercase tracking-wider mb-1">
                                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 15L3 9m0 0l6-6M3 9h12a6 6 0 010 12h-3" />
                                  </svg>
                                  Phản hồi từ phòng khám:
                                </div>
                                <p className="text-slate-600 font-bold text-[13px] leading-relaxed">{fb.replyText}</p>
                              </div>
                            )}
                          </div>
                        </td>

                        <td className="px-6 py-4 whitespace-nowrap text-slate-400 font-semibold">
                          {new Date(fb.createdAt).toLocaleDateString("vi-VN", {
                            day: "2-digit", month: "2-digit", year: "numeric",
                          })}
                        </td>

                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center justify-center gap-2">
                            {fb.status === "Featured" ? (
                              <button
                                onClick={() => handleFeature(fb.id)}
                                className="inline-flex items-center gap-1.5 bg-yellow-400 hover:bg-yellow-500 text-white font-bold text-[13px] px-3.5 py-1.5 rounded-lg transition-all shadow-md shadow-yellow-200/60 cursor-pointer"
                              >
                                <svg className="w-3.5 h-3.5 fill-white" viewBox="0 0 24 24"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" /></svg>
                                Nổi bật
                              </button>
                            ) : (
                              <button
                                onClick={() => handleFeature(fb.id)}
                                className="inline-flex items-center gap-1.5 border border-yellow-300 hover:bg-yellow-50 hover:border-yellow-400 text-yellow-600 font-bold text-[13px] px-3.5 py-1.5 rounded-lg transition-all cursor-pointer"
                              >
                                <svg className="w-3.5 h-3.5 fill-yellow-400" viewBox="0 0 24 24"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" /></svg>
                                Nổi bật
                              </button>
                            )}

                            <button
                              onClick={() => handleReplyClick(fb)}
                              className="border border-slate-200 hover:bg-slate-50 hover:border-slate-300 text-slate-600 font-bold text-[13px] px-3.5 py-1.5 rounded-lg transition-all cursor-pointer"
                            >
                              Trả lời
                            </button>
                          </div>
                        </td>

                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={5} className="px-6 py-12 text-center text-slate-400 font-semibold">
                        Không tìm thấy phản hồi nào phù hợp.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            {/* PAGINATION */}
            {filteredFeedbacks.length > 0 && (
              <div className="p-4 border-t border-slate-100 flex items-center justify-between">
                <span className="text-[12px] text-slate-400 font-semibold">
                  Hiển thị{" "}
                  <span className="font-black text-slate-600">{startIndex + 1}–{Math.min(startIndex + itemsPerPage, filteredFeedbacks.length)}</span>{" "}
                  trong{" "}
                  <span className="font-black text-slate-600">{filteredFeedbacks.length}</span>{" "}
                  phản hồi
                </span>
                <div className="flex items-center gap-1.5">
                  <button onClick={() => setCurrentPage(1)} disabled={currentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${currentPage === 1 ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed" : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"}`}>
                    |&lt;
                  </button>
                  <button onClick={() => setCurrentPage((p) => Math.max(1, p - 1))} disabled={currentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${currentPage === 1 ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed" : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"}`}>
                    &lt;
                  </button>
                  {Array.from({ length: totalPages }).map((_, idx) => {
                    const p = idx + 1;
                    return (
                      <button key={p} onClick={() => setCurrentPage(p)}
                        className={`w-9 h-9 rounded-xl border flex items-center justify-center font-extrabold text-[14px] transition-all cursor-pointer ${currentPage === p ? "bg-white border-primary text-primary shadow-sm" : "border-slate-200 text-slate-600 hover:bg-slate-50"}`}>
                        {p}
                      </button>
                    );
                  })}
                  <button onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${currentPage === totalPages ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed" : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"}`}>
                    &gt;
                  </button>
                  <button onClick={() => setCurrentPage(totalPages)} disabled={currentPage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${currentPage === totalPages ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed" : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"}`}>
                    &gt;|
                  </button>
                </div>
              </div>
            )}
          </div>

        </div>
      </main>

      {/* REPLY MODAL */}
      {replyTarget && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-xl max-w-lg w-full animate-fade-in flex flex-col gap-4">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h4 className="text-[17px] font-black text-slate-900">
                Trả lời đánh giá của {replyTarget.customerName}
              </h4>
              <button onClick={() => setReplyTarget(null)}
                className="text-slate-400 hover:text-slate-600 cursor-pointer p-1 rounded-lg hover:bg-slate-50 transition-all">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div className="bg-slate-50 p-4 rounded-xl border border-slate-150">
              <div className="flex items-center justify-between gap-2 mb-2">
                <span className="font-bold text-[13px] text-slate-700">{replyTarget.customerName}</span>
                <div className="flex items-center gap-0.5">
                  {Array.from({ length: 5 }).map((_, idx) => (
                    <svg key={idx}
                      className={`w-3.5 h-3.5 ${idx < replyTarget.rating ? "text-amber-400 fill-amber-400" : "text-slate-200 fill-slate-200"}`}
                      viewBox="0 0 24 24">
                      <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
                    </svg>
                  ))}
                </div>
              </div>
              <p className="text-[13px] text-slate-500 font-semibold leading-relaxed italic">
                &quot;{replyTarget.comment}&quot;
              </p>
            </div>

            <div className="flex flex-col gap-2">
              <label className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider block">
                Nội dung phản hồi
              </label>
              <textarea
                value={replyContent}
                onChange={(e) => setReplyContent(e.target.value)}
                placeholder="Nhập câu trả lời của phòng khám..."
                className="w-full p-3.5 text-[14px] border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder-slate-400"
                rows={5}
              />
            </div>

            <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-3">
              <button onClick={() => setReplyTarget(null)}
                className="px-4 py-2 border border-slate-200 rounded-xl text-[13px] font-bold text-slate-500 hover:bg-slate-50 hover:text-slate-700 transition-all cursor-pointer">
                Hủy bỏ
              </button>
              <button onClick={handleReplySubmit} disabled={!replyContent.trim()}
                className="px-5 py-2 bg-primary hover:bg-primary-hover text-white rounded-xl text-[13px] font-bold shadow-md shadow-primary/10 transition-all disabled:opacity-55 disabled:cursor-not-allowed cursor-pointer">
                Gửi phản hồi
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}
