import Link from "next/link";
import type { PostDto } from "@/types/api";
import { formatDate, categoryColor } from "@/lib/format";

const FALLBACK_IMG = "https://picsum.photos/seed/dentalrelated/200/200";

/** Danh sách "Bài viết khác" hiển thị ở sidebar trang chi tiết tin tức. */
export default function RelatedPosts({ posts }: { posts: PostDto[] }) {
  if (posts.length === 0) return null;

  return (
    <div className="lg:sticky lg:top-24">
      <h2 className="text-[13px] font-black tracking-widest text-primary uppercase mb-5">Bài viết khác</h2>

      <div className="flex flex-col gap-5">
        {posts.map((post) => (
          <Link key={post.id} href={`/tin-tuc/${post.id}`} className="group flex gap-4">
            <div className="w-20 h-20 rounded-xl overflow-hidden shrink-0 border border-slate-200/60">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={post.thumbnailUrl || FALLBACK_IMG}
                alt={post.title}
                className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
              />
            </div>
            <div className="min-w-0">
              <span className={`text-[10px] font-black px-2 py-0.5 rounded-full ${categoryColor(post.category)}`}>{post.category}</span>
              <h3 className="text-[14px] font-bold text-slate-800 leading-snug mt-1.5 line-clamp-2 group-hover:text-primary transition-colors">
                {post.title}
              </h3>
              <span className="block text-[11px] text-slate-400 font-medium mt-1">
                {formatDate(post.publishedAt ?? post.createdAt)}
              </span>
            </div>
          </Link>
        ))}
      </div>

      <Link
        href="/tin-tuc"
        className="mt-7 inline-flex items-center gap-1.5 text-[13px] font-bold text-primary hover:gap-3 transition-all"
      >
        Xem tất cả bài viết
        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
        </svg>
      </Link>
    </div>
  );
}
