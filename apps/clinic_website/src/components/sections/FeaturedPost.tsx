import Link from "next/link";
import type { PostDto } from "@/types/api";
import { formatDate, excerpt, categoryColor } from "@/lib/format";

const FALLBACK_IMG = "https://picsum.photos/seed/dentalfeatured/1200/800";

/** Bài viết tiêu biểu lớn hiển thị ở đầu trang Tin tức. */
export default function FeaturedPost({ post }: { post: PostDto }) {
  return (
    <section className="pt-4 pb-4 bg-white">
      <div className="max-w-7xl mx-auto px-6">
        <div className="flex items-center gap-2.5 mb-6">
          <span className="w-6 h-px bg-primary" />
          <span className="text-[12px] font-black tracking-widest text-primary uppercase">Bài viết mới nhất</span>
        </div>

        <Link
          href={`/tin-tuc/${post.id}`}
          className="group grid grid-cols-1 lg:grid-cols-2 rounded-3xl overflow-hidden border border-slate-200/60 shadow-sm hover:shadow-xl transition-all duration-300 bg-white"
        >
          {/* Cover */}
          <div className="relative h-64 lg:h-auto lg:min-h-[400px] overflow-hidden">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={post.thumbnailUrl || FALLBACK_IMG}
              alt={post.title}
              className="absolute inset-0 w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
            />
          </div>

          {/* Content */}
          <div className="p-8 lg:p-12 flex flex-col justify-center">
            <div className="flex flex-wrap items-center gap-3 mb-4">
              <span className={`text-[11px] font-black px-2.5 py-1 rounded-full ${categoryColor(post.category)}`}>{post.category}</span>
              <span className="text-[12px] text-slate-400 font-medium">{formatDate(post.publishedAt ?? post.createdAt)}</span>
              <span className="text-[12px] text-slate-400 font-medium">• {post.author}</span>
            </div>

            <h2 className="text-2xl md:text-3xl lg:text-[32px] font-black text-slate-900 leading-tight mb-4 group-hover:text-primary transition-colors line-clamp-3">
              {post.title}
            </h2>

            <p className="text-[15px] text-slate-500 leading-relaxed mb-7 line-clamp-4">
              {excerpt(post.content, 240)}
            </p>

            <span className="inline-flex items-center gap-2 text-[14px] font-bold text-primary group-hover:gap-3 transition-all">
              Đọc bài viết
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </span>
          </div>
        </Link>
      </div>
    </section>
  );
}
