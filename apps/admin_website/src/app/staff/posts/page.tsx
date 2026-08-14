"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { getPostsApi, deletePostApi, resolveAssetUrl, type PostDto } from "../../../lib/apiClient";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import { useRequireStaff } from "../../../hooks/useRequireStaff";
import NotificationBell from "../../../components/shared/NotificationBell";

interface Post {
  id: string;
  title: string;
  category: string;
  author: string;
  date: string;
  status: "Đã xuất bản" | "Bản nháp";
  thumbnailUrl: string | null;
}

function toPost(dto: PostDto): Post {
  const d = new Date(dto.createdAt);
  const date = `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
  return {
    id: dto.id,
    title: dto.title,
    category: dto.category,
    author: dto.author,
    date,
    status: dto.isPublished ? "Đã xuất bản" : "Bản nháp",
    thumbnailUrl: dto.thumbnailUrl,
  };
}

const CATEGORIES = [
  "Tất cả danh mục",
  "Chăm sóc răng miệng",
  "Niềng răng",
  "Phục hình",
  "Khuyến mãi",
  "Lời khuyên nha khoa"
];

export default function PostsListPage() {
  useRequireStaff();
  const [posts, setPosts] = useState<Post[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("Tất cả danh mục");
  const [currentPage, setCurrentPage] = useState(1);
  const [postsPerPage, setPostsPerPage] = useState(5);

  // Delete modal state
  const [deleteTarget, setDeleteTarget] = useState<Post | null>(null);

  useEffect(() => {
    getPostsApi().then((data) => setPosts(data.map(toPost)));
  }, []);

  const handleDeleteClick = (post: Post) => {
    setDeleteTarget(post);
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deletePostApi(deleteTarget.id);
      setPosts((prev) => prev.filter((p) => p.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch {
      alert("Không thể xóa bài viết. Vui lòng thử lại.");
    }
  };

  const cancelDelete = () => {
    setDeleteTarget(null);
  };

  // Filter and search logic
  const filteredPosts = posts.filter(post => {
    const matchesSearch = post.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      post.author.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesCategory = selectedCategory === "Tất cả danh mục" || post.category === selectedCategory;
    return matchesSearch && matchesCategory;
  });

  // Pagination logic
  const totalPages = Math.max(1, Math.ceil(filteredPosts.length / postsPerPage));
  const startIndex = (currentPage - 1) * postsPerPage;
  const paginatedPosts = filteredPosts.slice(startIndex, startIndex + postsPerPage);

  // Reset page when search, filter, or page size changes
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, selectedCategory, postsPerPage]);

  // Stats
  const totalPosts = posts.length;
  const publishedCount = posts.filter(p => p.status === "Đã xuất bản").length;
  const draftCount = posts.filter(p => p.status === "Bản nháp").length;
  const latestPost = posts.length > 0
    ? posts.reduce((a, b) => (a.date >= b.date ? a : b))
    : null;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      
      {/* ── SIDEBAR ──────────────────────────────────────────────────────── */}
      <StaffSidebar activeMenu="articles" />

      {/* ── MAIN AREA ────────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0">
        
        {/* HEADER */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-4 sm:px-8 h-16 sm:h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-3 min-w-0">
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
            <h1 className="text-xl sm:text-2xl font-extrabold text-slate-900 tracking-tight truncate">
              Quản lý Bài viết
            </h1>
          </div>

          {/* Notifications */}
          <div className="flex items-center gap-4">
            <NotificationBell href="/staff/notifications" />
          </div>
        </header>

        {/* BODY */}
        <div className="p-4 sm:p-8 flex-1 overflow-y-auto flex flex-col gap-6 sm:gap-8">
          
          {/* Stats Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Total */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between">
              <div className="flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Tổng bài viết</span>
                <span className="text-3xl font-black text-slate-900">{totalPosts}</span>
              </div>
              <div className="w-11 h-11 rounded-full bg-red-50 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                </svg>
              </div>
            </div>

            {/* Published */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between">
              <div className="flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Đã xuất bản</span>
                <div className="flex items-center gap-2">
                  <span className="text-3xl font-black text-slate-900">{publishedCount}</span>
                  <span className="w-2.5 h-2.5 rounded-full bg-green-500 shrink-0"></span>
                </div>
              </div>
              <div className="w-11 h-11 rounded-full bg-green-50 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-green-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>

            {/* Draft */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between">
              <div className="flex flex-col gap-1">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Bản nháp</span>
                <span className="text-3xl font-black text-slate-900">{draftCount}</span>
              </div>
              <div className="w-11 h-11 rounded-full bg-slate-100 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                </svg>
              </div>
            </div>

            {/* Latest post */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5 flex items-start justify-between gap-3">
              <div className="flex flex-col gap-1 min-w-0">
                <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Bài mới nhất</span>
                {latestPost ? (
                  <>
                    <p className="text-[14px] font-black text-slate-800 leading-snug truncate">{latestPost.title}</p>
                    <div className="flex items-center gap-1.5 mt-0.5">
                      <span className={`inline-flex px-2 py-0.5 rounded-full text-[11px] font-bold ${
                        latestPost.status === "Đã xuất bản"
                          ? "bg-green-50 text-green-600"
                          : "bg-amber-50 text-amber-600"
                      }`}>{latestPost.status}</span>
                      <span className="text-[11px] text-slate-400 font-semibold">{latestPost.date}</span>
                    </div>
                  </>
                ) : (
                  <span className="text-[13px] text-slate-400 font-semibold">Chưa có bài viết</span>
                )}
              </div>
              <div className="w-11 h-11 rounded-full bg-blue-50 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-blue-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-6">

            {/* Search & Actions Bar */}
            <div className="bg-white px-5 py-4 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-3">

              {/* Row 1: search + filters + create */}
              <div className="flex flex-col md:flex-row md:items-center gap-3">
                {/* Search Bar */}
                <div className="relative flex-1">
                  <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                  </span>
                  <input
                    type="text"
                    placeholder="Tìm kiếm bài viết..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full pl-10 pr-4 py-2.5 text-[14px] bg-slate-100/80 rounded-xl border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all font-semibold text-slate-800"
                  />
                </div>

                {/* Category Filter */}
                <div className="relative shrink-0">
                  <select
                    value={selectedCategory}
                    onChange={(e) => setSelectedCategory(e.target.value)}
                    className="appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-9 py-2.5 rounded-xl border border-slate-200 focus:outline-none transition-all cursor-pointer"
                  >
                    {CATEGORIES.map((cat, idx) => (
                      <option key={idx} value={cat}>
                        {cat === "Tất cả danh mục" ? "Tất cả danh mục" : cat}
                      </option>
                    ))}
                  </select>
                  <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-slate-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </div>
                </div>

                {/* Create Button */}
                <Link
                  href="/staff/posts/create"
                  className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] px-5 py-2.5 rounded-xl transition-all shadow-md shadow-primary/25 shrink-0"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  Tạo bài viết mới
                </Link>
              </div>

              {/* Row 2: per-page + result count */}
              <div className="flex items-center gap-2 text-[13px] text-slate-400 font-semibold border-t border-slate-100 pt-3">
                <span>Hiển thị</span>
                <div className="relative">
                  <select
                    value={postsPerPage}
                    onChange={(e) => setPostsPerPage(Number(e.target.value))}
                    className="appearance-none bg-white text-slate-700 font-bold text-[13px] pl-3 pr-7 py-1 rounded-lg border border-slate-200 focus:outline-none cursor-pointer"
                  >
                    {[5, 10, 20].map(n => (
                      <option key={n} value={n}>{n}</option>
                    ))}
                  </select>
                  <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-slate-400">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                    </svg>
                  </div>
                </div>
                <span>/ trang</span>
                <span className="text-slate-300 mx-1">·</span>
                <span>
                  Tìm thấy{" "}
                  <span className="text-slate-600 font-bold">{filteredPosts.length}</span>
                  {" "}kết quả
                </span>
              </div>

            </div>

            {/* Table Box */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse text-[14px]">
                  <thead>
                    <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
                      <th className="px-4 py-4 w-16">Ảnh</th>
                      <th className="px-6 py-4">Tiêu đề</th>
                      <th className="px-6 py-4">Danh mục</th>
                      <th className="px-6 py-4">Tác giả</th>
                      <th className="px-6 py-4">Ngày đăng</th>
                      <th className="px-6 py-4">Trạng thái</th>
                      <th className="px-6 py-4 text-center">Chức năng</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 font-semibold text-slate-600">
                    {paginatedPosts.length > 0 ? (
                      paginatedPosts.map((post) => (
                        <tr key={post.id} className="hover:bg-slate-50/40 transition-colors">
                          <td className="px-4 py-3">
                            {post.thumbnailUrl ? (
                              <img
                                src={resolveAssetUrl(post.thumbnailUrl)}
                                alt={post.title}
                                className="w-12 h-12 rounded-lg object-cover border border-slate-100 shrink-0"
                              />
                            ) : (
                              <div className="w-12 h-12 rounded-lg bg-slate-100 border border-slate-200 flex items-center justify-center shrink-0">
                                <svg className="w-5 h-5 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909M3.75 18h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v10.5a1.5 1.5 0 001.5 1.5z" />
                                </svg>
                              </div>
                            )}
                          </td>
                          <td className="px-6 py-4 font-bold text-slate-800 max-w-xs md:max-w-sm lg:max-w-md truncate">
                            {post.title}
                          </td>
                          <td className="px-6 py-4 text-slate-500 font-medium">{post.category}</td>
                          <td className="px-6 py-4 text-slate-600 font-bold">{post.author}</td>
                          <td className="px-6 py-4 text-slate-400 font-semibold">{post.date}</td>
                          <td className="px-6 py-4">
                            <span
                              className={`inline-flex px-3 py-1 rounded-full text-[12px] font-bold ${
                                post.status === "Đã xuất bản"
                                  ? "bg-green-50 text-green-600 border border-green-100"
                                  : "bg-amber-50 text-amber-600 border border-amber-100"
                              }`}
                            >
                              {post.status}
                            </span>
                          </td>
                          <td className="px-6 py-4">
                            <div className="flex items-center justify-center gap-4">
                              {/* Preview Button */}
                              <Link
                                href={`/staff/posts/${post.id}/preview`}
                                className="p-1.5 rounded-lg text-slate-400 hover:text-blue-600 hover:bg-blue-50 transition-all"
                                title="Xem trước bài viết"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                </svg>
                              </Link>
                              {/* Edit Button */}
                              <Link
                                href={`/staff/posts/${post.id}/edit`}
                                className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-all"
                                title="Chỉnh sửa bài viết"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                                </svg>
                              </Link>
                              {/* Delete Button */}
                              <button
                                onClick={() => handleDeleteClick(post)}
                                className="p-1.5 rounded-lg text-red-400 hover:text-primary hover:bg-red-50 transition-all cursor-pointer"
                                title="Xóa bài viết"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                                </svg>
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={7} className="px-6 py-12 text-center text-slate-400 font-semibold">
                          Không tìm thấy bài viết nào phù hợp.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              {/* Pagination bar */}
              {filteredPosts.length > 0 && (
                <div className="p-4 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-2.5">
                  {/* Page info */}
                  <span className="text-[13px] text-slate-400 font-semibold text-center sm:text-left">
                    Hiển thị{" "}
                    <span className="text-slate-600 font-bold">{startIndex + 1}–{Math.min(startIndex + postsPerPage, filteredPosts.length)}</span>
                    {" "}trong{" "}
                    <span className="text-slate-600 font-bold">{filteredPosts.length}</span>
                    {" "}bài viết
                  </span>
                  <div className="flex items-center gap-1.5 sm:gap-2.5 flex-wrap justify-center">
                  {/* Quick First Page */}
                  <button
                    onClick={() => setCurrentPage(1)}
                    disabled={currentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;|
                  </button>
                  {/* Previous Page */}
                  <button
                    onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                    disabled={currentPage === 1}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === 1
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &lt;
                  </button>
                  
                  {/* Pages indicator list */}
                  {Array.from({ length: totalPages }).map((_, idx) => {
                    const p = idx + 1;
                    const isActive = currentPage === p;
                    return (
                      <button
                        key={p}
                        onClick={() => setCurrentPage(p)}
                        className={`w-9 h-9 rounded-xl border flex items-center justify-center font-extrabold text-[14px] transition-all cursor-pointer ${
                          isActive
                            ? "bg-white border-primary text-primary shadow-sm font-black"
                            : "border-slate-200 text-slate-600 hover:bg-slate-50"
                        }`}
                      >
                        {p}
                      </button>
                    );
                  })}

                  {/* Next Page */}
                  <button
                    onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                    disabled={currentPage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === totalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    &gt;
                  </button>
                  {/* Quick Last Page */}
                  <button
                    onClick={() => setCurrentPage(totalPages)}
                    disabled={currentPage === totalPages}
                    className={`w-9 h-9 rounded-xl border flex items-center justify-center font-bold transition-all text-[13px] ${
                      currentPage === totalPages
                        ? "border-slate-100 text-slate-300 bg-slate-50 cursor-not-allowed"
                        : "border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 cursor-pointer"
                    }`}
                  >
                    |&gt;
                  </button>
                  </div>
                </div>
              )}
            </div>

            {/* Delete Confirmation Modal */}
            {deleteTarget && (
              <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
                <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-xl max-w-md w-full animate-fade-in flex flex-col gap-4">
                  <div className="flex items-center gap-3 text-primary">
                    <span className="w-10 h-10 rounded-full bg-red-50 flex items-center justify-center font-bold text-lg">⚠️</span>
                    <h4 className="text-[17px] font-black text-slate-900">Xác nhận xóa bài viết</h4>
                  </div>
                  <p className="text-[14px] text-slate-500 font-semibold leading-relaxed">
                    Bạn có chắc chắn muốn xóa bài viết <strong className="text-slate-800">&quot;{deleteTarget.title}&quot;</strong>? Hành động này sẽ không thể khôi phục lại.
                  </p>
                  <div className="flex items-center justify-end gap-3 mt-2">
                    <button
                      onClick={cancelDelete}
                      className="px-4 py-2 border border-slate-200 rounded-xl text-[13px] font-bold text-slate-500 hover:bg-slate-50 hover:text-slate-700 transition-all cursor-pointer"
                    >
                      Hủy bỏ
                    </button>
                    <button
                      onClick={confirmDelete}
                      className="px-4 py-2 bg-primary hover:bg-primary-hover text-white rounded-xl text-[13px] font-bold shadow-md shadow-primary/10 transition-all cursor-pointer"
                    >
                      Xóa bài viết
                    </button>
                  </div>
                </div>
              </div>
            )}

          </div>

        </div>
      </main>

    </div>
  );
}
