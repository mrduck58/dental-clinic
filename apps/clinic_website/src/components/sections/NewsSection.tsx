import Link from "next/link";
import type { ReactNode } from "react";
import type { PostDto } from "@/types/api";
import { formatDate, excerpt, categoryColor } from "@/lib/format";

const FALLBACK_IMG = "https://picsum.photos/seed/dentalnews/600/360";

export default function NewsSection({
  posts,
  preview = false,
  heading = "Cập Nhật Mới Nhất",
  toolbar,
  footer,
}: {
  posts: PostDto[];
  preview?: boolean;
  heading?: string;
  toolbar?: ReactNode;
  footer?: ReactNode;
}) {
  const items = preview ? posts.slice(0, 3) : posts;

  return (
    <section className="py-16 bg-white">
      <div className="max-w-7xl mx-auto px-6">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-8">
          <div>
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Tin Tức & Sự Kiện</span>
            {heading && (
              <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2">
                {heading}
              </h2>
            )}
          </div>
          {preview && posts.length > 3 && (
            <Link href="/tin-tuc" className="inline-flex items-center gap-1.5 text-[13px] font-bold text-primary hover:gap-3 transition-all shrink-0">
              Xem tất cả
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </Link>
          )}
        </div>

        {toolbar}

        {items.length === 0 ? (
          <p className="text-center text-slate-400 text-[15px] py-10">
            {toolbar ? "Không tìm thấy bài viết phù hợp." : "Chưa có bài viết nào."}
          </p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {items.map((post) => (
              <Link
                key={post.id}
                href={`/tin-tuc/${post.id}`}
                className="glass-card hover-lift bg-white rounded-2xl border border-slate-200/60 overflow-hidden shadow-sm flex flex-col group"
              >
                {/* Cover image */}
                <div className="h-52 overflow-hidden">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={post.thumbnailUrl || FALLBACK_IMG}
                    alt={post.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                </div>

                <div className="p-6 flex flex-col flex-1">
                  <div className="flex items-center gap-3 mb-3">
                    <span className={`text-[11px] font-black px-2.5 py-1 rounded-full ${categoryColor(post.category)}`}>{post.category}</span>
                    <span className="text-[11px] text-slate-400 font-medium">{formatDate(post.publishedAt ?? post.createdAt)}</span>
                  </div>

                  <h3 className="text-[16px] font-bold text-slate-900 leading-snug mb-3 group-hover:text-primary transition-colors line-clamp-2">{post.title}</h3>
                  <p className="text-[13px] text-slate-500 leading-relaxed flex-1 mb-5 line-clamp-3">{excerpt(post.content)}</p>

                  <span className="flex items-center gap-1.5 text-[13px] font-bold text-primary group-hover:gap-3 transition-all">
                    Đọc tiếp
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                    </svg>
                  </span>
                </div>
              </Link>
            ))}
          </div>
        )}

        {footer}
      </div>
    </section>
  );
}
