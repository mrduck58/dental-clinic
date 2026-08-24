"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { getPostByIdApi, resolveAssetUrl, type PostDto } from "../../../../../lib/apiClient";
import StaffSidebar from "../../../../../components/shared/StaffSidebar";
import { useRequireStaff } from "../../../../../hooks/useRequireStaff";
import NotificationBell from "../../../../../components/shared/NotificationBell";

interface PreviewPostPageProps {
  params: Promise<{ id: string }>;
}

export default function PreviewPostPage({ params }: PreviewPostPageProps) {
  useRequireStaff();

  const resolvedParams = React.use(params);
  const id = resolvedParams.id;

  const [post, setPost] = useState<PostDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    getPostByIdApi(id)
      .then(setPost)
      .finally(() => setIsLoading(false));
  }, [id]);

  const formatDate = (iso: string) => {
    const d = new Date(iso);
    return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">

      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <StaffSidebar activeMenu="articles" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">

        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-4 sm:px-8 h-16 sm:h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-3 sm:gap-4 min-w-0">
            <button
              type="button"
              onClick={() => window.dispatchEvent(new CustomEvent("toggle-sidebar"))}
              className="lg:hidden p-2 -ml-2 rounded-xl text-slate-600 hover:text-slate-900 hover:bg-slate-100 transition-all shrink-0"
              aria-label="Mở menu điều hướng"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
              </svg>
            </button>
            <Link
              href="/staff/posts"
              className="p-2 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-all shrink-0"
              title="Quay lại danh sách"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </Link>
            <div className="min-w-0">
              <h1 className="text-xl sm:text-2xl font-extrabold text-slate-900 tracking-tight truncate">Xem trước bài viết</h1>
              {post && (
                <p className="text-[13px] text-slate-400 font-semibold truncate max-w-md">{post.title}</p>
              )}
            </div>
          </div>

          <div className="flex items-center gap-4">
            {post && (
              <Link
                href={`/staff/posts/${id}/edit`}
                className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[13px] px-4 py-2 rounded-xl transition-all shadow-md shadow-primary/25"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
                </svg>
                Chỉnh sửa
              </Link>
            )}
            <NotificationBell href="/staff/notifications" />
          </div>
        </header>

        {/* BODY */}
        <div className="p-8 flex-1 overflow-y-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-32 text-slate-400 font-semibold text-[15px]">
              Đang tải bài viết...
            </div>
          ) : !post ? (
            <div className="flex flex-col items-center justify-center py-32 gap-4">
              <p className="text-slate-400 font-semibold text-[15px]">Không tìm thấy bài viết.</p>
              <Link href="/staff/posts" className="text-primary font-bold text-[14px] hover:underline">
                Quay lại danh sách
              </Link>
            </div>
          ) : (
            <div className="max-w-3xl mx-auto flex flex-col gap-6">

              {/* Status bar */}
              <div className="flex items-center gap-3">
                <span
                  className={`inline-flex px-3 py-1 rounded-full text-[12px] font-bold ${
                    post.isPublished
                      ? "bg-green-50 text-green-600 border border-green-100"
                      : "bg-amber-50 text-amber-600 border border-amber-100"
                  }`}
                >
                  {post.isPublished ? "Đã xuất bản" : "Bản nháp"}
                </span>
                <span className="px-3 py-1 rounded-full bg-slate-100 text-slate-500 text-[12px] font-bold">
                  {post.category}
                </span>
              </div>

              {/* Article card - mimics how it looks on the website */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">

                {/* Thumbnail */}
                {post.thumbnailUrl ? (
                  <img
                    src={resolveAssetUrl(post.thumbnailUrl)}
                    alt={post.title}
                    className="w-full h-72 object-cover"
                  />
                ) : (
                  <div className="w-full h-48 bg-slate-100 flex items-center justify-center">
                    <svg className="w-12 h-12 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909M3.75 18h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v10.5a1.5 1.5 0 001.5 1.5z" />
                    </svg>
                  </div>
                )}

                <div className="px-8 py-7 flex flex-col gap-5">
                  {/* Title */}
                  <h1 className="text-[26px] font-black text-slate-900 leading-snug">{post.title}</h1>

                  {/* Date */}
                  <div className="flex items-center gap-5 text-[13px] text-slate-400 font-semibold border-b border-slate-100 pb-5">
                    <div className="flex items-center gap-1.5">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                      </svg>
                      <span>{formatDate(post.createdAt)}</span>
                    </div>
                    {post.publishedAt && (
                      <div className="flex items-center gap-1.5">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 7.5h1.5m-1.5 3h1.5m-7.5 3h7.5m-7.5 3h7.5m3-9h3.375c.621 0 1.125.504 1.125 1.125V18a2.25 2.25 0 01-2.25 2.25M16.5 7.5V18a2.25 2.25 0 002.25 2.25M16.5 7.5V4.875c0-.621-.504-1.125-1.125-1.125H4.125C3.504 3.75 3 4.254 3 4.875V18a2.25 2.25 0 002.25 2.25h13.5M6 7.5h3v3H6v-3z" />
                        </svg>
                        <span>Xuất bản: {formatDate(post.publishedAt)}</span>
                      </div>
                    )}
                  </div>

                  {/* Content */}
                  <div
                    className="prose prose-slate max-w-none text-[15px] leading-relaxed text-slate-700"
                    dangerouslySetInnerHTML={{ __html: post.content }}
                  />
                </div>
              </div>

            </div>
          )}
        </div>
      </main>

    </div>
  );
}
