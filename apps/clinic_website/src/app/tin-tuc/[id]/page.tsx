import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import RelatedPosts from "@/components/sections/RelatedPosts";
import { getPostById, getPosts, ApiError } from "@/lib/api";
import { formatDate, categoryColor, excerpt } from "@/lib/format";
import { shuffle } from "@/lib/listing";
import type { PostDto } from "@/types/api";

export const dynamic = "force-dynamic";

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  try {
    const post = await getPostById(id);
    return {
      title: `${post.title} | Sơn Giang Dental Clinic`,
      description: excerpt(post.content, 160),
    };
  } catch {
    return { title: "Tin tức | Sơn Giang Dental Clinic" };
  }
}

export default async function PostDetailPage({ params }: Props) {
  const { id } = await params;

  const [post, allPosts] = await Promise.all([
    getPostById(id).catch((e) => {
      if (e instanceof ApiError && e.status === 404) return null;
      throw e;
    }),
    getPosts().catch(() => [] as PostDto[]),
  ]);
  if (!post) notFound();

  // Các bài viết khác (loại bài hiện tại), random tối đa 5 bài cho sidebar.
  const otherPosts = shuffle(allPosts.filter((p) => p.id !== post.id)).slice(0, 5);

  return (
    <div className="animate-fade-in">
      <article className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-6">
          <div className="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_320px] gap-12 lg:gap-16">

            {/* Nội dung chính */}
            <div className="min-w-0 max-w-3xl">
              {/* Meta */}
              <div className="flex flex-wrap items-center gap-3 mb-4">
                <span className={`text-[11px] font-black px-2.5 py-1 rounded-full ${categoryColor(post.category)}`}>{post.category}</span>
                <span className="text-[13px] text-slate-400 font-medium">{formatDate(post.publishedAt ?? post.createdAt)}</span>
                <span className="text-[13px] text-slate-400 font-medium">• {post.author}</span>
              </div>

              {/* Tiêu đề */}
              <h1 className="text-3xl md:text-4xl font-black text-slate-900 leading-tight mb-6">{post.title}</h1>

              {/* Ảnh bìa */}
              <div className="rounded-3xl overflow-hidden shadow-xl border border-slate-200/60 mb-10">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={post.thumbnailUrl || `https://picsum.photos/seed/post${post.id}/1200/600`}
                  alt={post.title}
                  className="w-full h-auto object-cover"
                />
              </div>

              {/* Nội dung */}
              <div className="prose-content text-slate-700 text-[16px] leading-relaxed whitespace-pre-line">
                {post.content}
              </div>

              {/* Dịch vụ liên quan */}
              {post.serviceId && (
                <div className="mt-10">
                  <Link
                    href={`/dich-vu/${post.serviceId}`}
                    className="inline-flex items-center gap-2 rounded-full bg-primary/10 hover:bg-primary/20 text-primary font-bold text-[14px] px-5 py-3 transition-colors"
                  >
                    Xem dịch vụ liên quan: {post.serviceName}
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                    </svg>
                  </Link>
                </div>
              )}

              <div className="mt-12 pt-8 border-t border-slate-100">
                <Link href="/tin-tuc" className="text-[14px] font-bold text-slate-500 hover:text-primary transition-colors inline-flex items-center gap-1.5">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                  </svg>
                  Tất cả bài viết
                </Link>
              </div>
            </div>

            {/* Sidebar — bài viết khác */}
            <aside className="min-w-0">
              <RelatedPosts posts={otherPosts} />
            </aside>

          </div>
        </div>
      </article>
    </div>
  );
}
