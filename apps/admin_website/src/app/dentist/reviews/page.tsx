"use client";

import React, { useEffect, useState, useMemo } from "react";
import DentistSidebar from "../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../hooks/useRequireDentist";
import {
  getUser,
  getPublicDentistsApi,
  getDentistReviewsApi,
  type DentistReviewItemDto,
  type DentistReviewsResultDto,
  type PublicDentistDto,
} from "../../../lib/apiClient";

function StarRating({ rating, size = "w-4 h-4" }: { rating: number; size?: string }) {
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: 5 }).map((_, idx) => (
        <svg
          key={idx}
          className={`${size} ${idx < rating ? "text-amber-400 fill-amber-400" : "text-slate-200 fill-slate-200"}`}
          viewBox="0 0 24 24"
        >
          <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
        </svg>
      ))}
    </div>
  );
}

export default function DentistReviewsPage() {
  useRequireDentist();

  const [reviewsData, setReviewsData] = useState<DentistReviewsResultDto>({
    averageRating: 0,
    reviewCount: 0,
    reviews: [],
  });
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [ratingFilter, setRatingFilter] = useState("All");

  useEffect(() => {
    async function loadReviews() {
      setLoading(true);
      try {
        const currentUser = getUser();
        const dentists = await getPublicDentistsApi();
        
        let dentistId = "";
        if (currentUser && dentists.length > 0) {
          const match = dentists.find(
            (d) => d.fullName.toLowerCase() === currentUser.fullName?.toLowerCase()
          );
          dentistId = match ? match.id : dentists[0].id;
        } else if (dentists.length > 0) {
          dentistId = dentists[0].id;
        }

        if (dentistId) {
          const data = await getDentistReviewsApi(dentistId);
          setReviewsData(data);
        }
      } catch (err) {
        console.error("Lỗi khi tải đánh giá bác sĩ:", err);
      } finally {
        setLoading(false);
      }
    }
    loadReviews();
  }, []);

  const filteredReviews = useMemo(() => {
    return reviewsData.reviews.filter((r) => {
      const matchesSearch =
        r.patientName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        r.comment.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (r.serviceName ?? "").toLowerCase().includes(searchQuery.toLowerCase());
      const matchesRating =
        ratingFilter === "All" || r.rating === parseInt(ratingFilter);
      return matchesSearch && matchesRating;
    });
  }, [reviewsData.reviews, searchQuery, ratingFilter]);

  const ratingCounts = useMemo(() => {
    const counts = { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 };
    reviewsData.reviews.forEach((r) => {
      if (r.rating >= 1 && r.rating <= 5) {
        counts[r.rating as keyof typeof counts]++;
      }
    });
    return counts;
  }, [reviewsData.reviews]);

  const popularTags = useMemo(() => {
    const map = new Map<string, number>();
    reviewsData.reviews.forEach((r) => {
      r.tags.forEach((t) => {
        map.set(t, (map.get(t) ?? 0) + 1);
      });
    });
    return Array.from(map.entries()).sort((a, b) => b[1] - a[1]);
  }, [reviewsData.reviews]);

  const getInitials = (name: string) =>
    name.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase();

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="reviews" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Đánh Giá Từ Bệnh Nhân"
          subtitle="Xem các nhận xét và phản hồi chất lượng điều trị từ bệnh nhân"
        />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {/* STATS OVERVIEW HEADER */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5 shrink-0">
            {/* Average Rating Box */}
            <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex items-center gap-6">
              <div className="flex flex-col items-center justify-center p-4 bg-amber-50 rounded-2xl border border-amber-100 min-w-[110px]">
                <span className="text-4xl font-black text-amber-500">
                  {reviewsData.averageRating > 0 ? reviewsData.averageRating.toFixed(1) : "5.0"}
                </span>
                <div className="mt-1">
                  <StarRating rating={Math.round(reviewsData.averageRating || 5)} size="w-3.5 h-3.5" />
                </div>
                <span className="text-[11px] font-bold text-slate-400 mt-1">trên 5.0 sao</span>
              </div>
              <div className="flex-1 flex flex-col gap-1">
                <span className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider">
                  Tổng lượt đánh giá
                </span>
                <span className="text-3xl font-black text-slate-900">
                  {reviewsData.reviewCount} <span className="text-sm font-bold text-slate-500">đánh giá</span>
                </span>
                <p className="text-[12.5px] text-slate-500 font-semibold mt-1">
                  Đánh giá được ghi nhận từ bệnh nhân sau khi hoàn thành buổi khám.
                </p>
              </div>
            </div>

            {/* Rating Breakdown */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col justify-center gap-2">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block mb-1">
                Phân bố xếp hạng
              </span>
              {[5, 4, 3, 2, 1].map((star) => {
                const count = ratingCounts[star as keyof typeof ratingCounts];
                const pct = reviewsData.reviewCount > 0 ? Math.round((count / reviewsData.reviewCount) * 100) : 0;
                return (
                  <div key={star} className="flex items-center gap-2 text-[12px] font-bold text-slate-600">
                    <span className="w-10 flex items-center gap-1 font-extrabold">
                      {star} <svg className="w-3 h-3 text-amber-400 fill-amber-400 inline" viewBox="0 0 24 24"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/></svg>
                    </span>
                    <div className="flex-1 h-2 bg-slate-100 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-amber-400 rounded-full transition-all duration-300"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <span className="w-8 text-right text-slate-400 font-mono text-[11px]">{count}</span>
                  </div>
                );
              })}
            </div>

            {/* Popular Tags */}
            <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3">
              <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
                Nhãn đánh giá phổ biến
              </span>
              {popularTags.length > 0 ? (
                <div className="flex倚 flex-wrap gap-2">
                  {popularTags.map(([tag, cnt]) => (
                    <span
                      key={tag}
                      className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-sky-50 text-secondary border border-sky-100 text-[12px] font-bold"
                    >
                      🏷️ {tag} <span className="px-1.5 py-0.5 rounded-md bg-white text-[10px] text-sky-700 font-black">{cnt}</span>
                    </span>
                  ))}
                </div>
              ) : (
                <div className="flex-1 flex items-center justify-center text-[13px] text-slate-400 font-semibold italic">
                  Chưa có nhãn tag nổi bật
                </div>
              )}
            </div>
          </div>

          {/* FILTER BAR */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4 shrink-0">
            <div className="relative flex-1 max-w-md">
              <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </span>
              <input
                type="text"
                placeholder="Tìm kiếm theo bệnh nhân, nhận xét, dịch vụ..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2.5 text-[14.5px] bg-slate-100/80 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all font-semibold text-slate-800"
              />
            </div>

            <div className="flex items-center gap-3">
              <div className="relative">
                <select
                  value={ratingFilter}
                  onChange={(e) => setRatingFilter(e.target.value)}
                  className="appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-10 py-2.5 rounded-xl border border-slate-200 focus:outline-none transition-all cursor-pointer"
                >
                  <option value="All">Tất cả số sao</option>
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
            </div>
          </div>

          {/* REVIEWS LIST */}
          <div className="flex flex-col gap-4">
            {loading ? (
              <div className="bg-white p-12 rounded-2xl border border-slate-200/60 shadow-sm text-center text-slate-400 font-semibold">
                Đang tải danh sách đánh giá...
              </div>
            ) : filteredReviews.length > 0 ? (
              filteredReviews.map((review: DentistReviewItemDto) => (
                <div
                  key={review.id}
                  className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm hover:border-sky-200 transition-all flex flex-col gap-4"
                >
                  {/* Review Header */}
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-center gap-3.5">
                      <div className="w-10 h-10 rounded-full bg-sky-50 border border-sky-200 text-secondary font-black text-sm flex items-center justify-center shrink-0">
                        {getInitials(review.patientName)}
                      </div>
                      <div className="flex flex-col gap-0.5">
                        <span className="font-extrabold text-[15px] text-slate-900">
                          {review.patientName}
                        </span>
                        {review.serviceName && (
                          <span className="text-[12px] font-semibold text-slate-400 flex items-center gap-1">
                            <span>Dịch vụ:</span>
                            <span className="text-sky-700 font-bold bg-sky-50 px-2 py-0.5 rounded-md border border-sky-100">
                              {review.serviceName}
                            </span>
                          </span>
                        )}
                      </div>
                    </div>

                    <div className="flex flex-col items-end gap-1">
                      <StarRating rating={review.rating} size="w-4 h-4" />
                      <span className="text-[12px] text-slate-400 font-semibold">
                        {new Date(review.createdAt).toLocaleDateString("vi-VN", {
                          day: "2-digit",
                          month: "2-digit",
                          year: "numeric",
                        })}
                      </span>
                    </div>
                  </div>

                  {/* Comment */}
                  <p className="text-[14.5px] text-slate-700 font-semibold leading-relaxed bg-slate-50/70 p-4 rounded-xl border border-slate-100">
                    &quot;{review.comment}&quot;
                  </p>

                  {/* Tags */}
                  {review.tags && review.tags.length > 0 && (
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider">Đặc điểm:</span>
                      {review.tags.map((tag) => (
                        <span
                          key={tag}
                          className="px-2.5 py-1 bg-slate-100 text-slate-600 text-[11.5px] font-bold rounded-lg"
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              ))
            ) : (
              <div className="bg-white p-12 rounded-2xl border border-slate-200/60 shadow-sm text-center text-slate-400 font-semibold">
                Không tìm thấy đánh giá nào phù hợp.
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
