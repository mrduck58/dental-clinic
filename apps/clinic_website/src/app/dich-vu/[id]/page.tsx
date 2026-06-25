import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import PageHeader from "@/components/shared/PageHeader";
import { getServiceById, getPostsByService, ApiError } from "@/lib/api";
import { formatPrice, formatDuration, formatDate, excerpt, categoryColor } from "@/lib/format";

export const dynamic = "force-dynamic";

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  try {
    const service = await getServiceById(id);
    return {
      title: `${service.name} | Sơn Giang Dental Clinic`,
      description: service.description?.slice(0, 160),
    };
  } catch {
    return { title: "Dịch vụ | Sơn Giang Dental Clinic" };
  }
}

export default async function ServiceDetailPage({ params }: Props) {
  const { id } = await params;
  const service = await getServiceById(id).catch((e) => {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  });
  if (!service) notFound();

  // Bài viết mô tả gắn với dịch vụ này (không chặn render nếu lỗi)
  const relatedPosts = await getPostsByService(id).catch(() => []);

  return (
    <div className="animate-fade-in">
      <PageHeader eyebrow="Dịch Vụ" title={service.name} />

      <section className="py-16 bg-white">
        <div className="max-w-5xl mx-auto px-6 grid grid-cols-1 lg:grid-cols-2 gap-12 items-start">
          {/* Ảnh */}
          <div className="rounded-3xl overflow-hidden shadow-xl border border-slate-200/60">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={service.imageUrl || `https://picsum.photos/seed/svc${service.id}/800/600`}
              alt={service.name}
              className="w-full h-full object-cover"
            />
          </div>

          {/* Thông tin */}
          <div>
            <h1 className="text-3xl font-black text-slate-900 mb-4">{service.name}</h1>

            <div className="flex flex-wrap items-center gap-3 mb-6">
              <span className="inline-flex items-center gap-1.5 bg-primary/10 text-primary font-black text-[18px] px-4 py-2 rounded-xl">
                {formatPrice(service.price)}
              </span>
              <span className="inline-flex items-center gap-1.5 bg-slate-100 text-slate-600 font-bold text-[13px] px-3 py-2 rounded-xl">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                {formatDuration(service.durationMinutes)}
              </span>
            </div>

            <p className="text-slate-600 text-[15px] leading-relaxed whitespace-pre-line mb-8">
              {service.description}
            </p>

            <Link
              href="/tai-ung-dung"
              className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-7 py-3.5 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
            >
              Đặt lịch dịch vụ này
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </Link>

            <div className="mt-8">
              <Link href="/dich-vu" className="text-[14px] font-bold text-slate-500 hover:text-primary transition-colors inline-flex items-center gap-1.5">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
                Tất cả dịch vụ
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Bài viết liên quan đến dịch vụ */}
      {relatedPosts.length > 0 && (
        <section className="py-16 bg-slate-50 border-t border-slate-200/60">
          <div className="max-w-5xl mx-auto px-6">
            <div className="mb-10">
              <span className="text-[12px] font-black tracking-widest text-primary uppercase">Tìm hiểu thêm</span>
              <h2 className="text-2xl md:text-3xl font-black text-slate-900 mt-2">
                Bài viết về {service.name}
              </h2>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {relatedPosts.map((post) => (
                <Link
                  key={post.id}
                  href={`/tin-tuc/${post.id}`}
                  className="glass-card hover-lift bg-white rounded-2xl border border-slate-200/60 overflow-hidden shadow-sm flex flex-col group"
                >
                  <div className="h-48 overflow-hidden">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={post.thumbnailUrl || `https://picsum.photos/seed/post${post.id}/600/360`}
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
          </div>
        </section>
      )}
    </div>
  );
}
