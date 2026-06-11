"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { getPosts, deletePost, Post } from "../../../components/postsDb";

const CATEGORIES = [
  "Tất cả danh mục",
  "Chăm sóc răng miệng",
  "Niềng răng",
  "Phục hình",
  "Khuyến mãi",
  "Lời khuyên nha khoa"
];

export default function PostsListPage() {
  const [posts, setPosts] = useState<Post[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("Tất cả danh mục");
  const [currentPage, setCurrentPage] = useState(1);
  const postsPerPage = 5;

  // Delete modal state
  const [deleteTarget, setDeleteTarget] = useState<Post | null>(null);

  // Load posts on mount
  useEffect(() => {
    setPosts(getPosts());
  }, []);

  const handleDeleteClick = (post: Post) => {
    setDeleteTarget(post);
  };

  const confirmDelete = () => {
    if (deleteTarget) {
      deletePost(deleteTarget.id);
      setPosts(getPosts());
      setDeleteTarget(null);
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

  // Reset page when search or filter changes
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, selectedCategory]);

  return (
    <div className="flex flex-col gap-6">
      
      {/* Search & Actions Bar */}
      <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4">
        
        {/* Search Bar */}
        <div className="relative flex-1 max-w-md">
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
            className="w-full pl-10 pr-4 py-2.5 text-[15px] bg-slate-100/80 rounded-full border border-transparent focus:bg-white focus:border-slate-200 focus:outline-none transition-all font-semibold text-slate-800"
          />
        </div>

        {/* Filters and Create Button */}
        <div className="flex items-center gap-3 self-end md:self-auto">
          {/* Category Filter Select */}
          <div className="relative">
            <select
              value={selectedCategory}
              onChange={(e) => setSelectedCategory(e.target.value)}
              className="appearance-none bg-white text-slate-700 font-bold text-[14px] pl-4 pr-10 py-2.5 rounded-xl border border-slate-200 hover:border-slate-300 focus:outline-none transition-all cursor-pointer"
            >
              {CATEGORIES.map((cat, idx) => (
                <option key={idx} value={cat}>
                  {cat === "Tất cả danh mục" ? "Lọc theo Danh mục" : cat}
                </option>
              ))}
            </select>
            {/* Custom Arrow */}
            <div className="pointer-events-none absolute inset-y-0 right-3.5 flex items-center text-slate-400">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
              </svg>
            </div>
          </div>

          {/* Create New Post Button */}
          <Link
            href="/manager/posts/create"
            className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white font-bold text-[14px] px-5 py-2.5 rounded-xl transition-all shadow-md shadow-primary/25 cursor-pointer"
          >
            Tạo bài viết mới
          </Link>
        </div>
      </div>

      {/* Table Box */}
      <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-[14px]">
            <thead>
              <tr className="bg-slate-50/50 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-150">
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
                        {/* Edit Button */}
                        <Link
                          href={`/manager/posts/${post.id}/edit`}
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
                  <td colSpan={6} className="px-6 py-12 text-center text-slate-400 font-semibold">
                    Không tìm thấy bài viết nào phù hợp.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination bar */}
        {filteredPosts.length > 0 && (
          <div className="p-4 border-t border-slate-100 flex items-center justify-end gap-2.5">
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
  );
}
