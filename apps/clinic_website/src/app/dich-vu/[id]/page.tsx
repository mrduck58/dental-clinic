import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getServiceById, getPostsByService, ApiError } from "@/lib/api";
import { formatPrice, formatDuration, formatDate, excerpt, categoryColor } from "@/lib/format";
import MediaPlaceholder from "@/components/shared/MediaPlaceholder";

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

  // Bài viết liên quan đến dịch vụ này
  const relatedPosts = await getPostsByService(id).catch(() => []);

  const hasOptions = service.options && service.options.length > 0;

  return (
    <div className="animate-fade-in bg-slate-50/50">
      {/* Banner */}
      <section className="py-16 bg-white border-b border-slate-200/60">
        <div className="max-w-7xl mx-auto px-6">
          {/* Ảnh bìa */}
          <div className="max-w-4xl mx-auto">
            <div className="rounded-3xl overflow-hidden shadow-xl border border-slate-200/60 bg-slate-100 aspect-[21/9] max-h-[380px]">
              {service.imageUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={service.imageUrl}
                  alt={service.name}
                  className="w-full h-full object-cover"
                />
              ) : (
                <MediaPlaceholder />
              )}
            </div>
          </div>

          {/* Thông tin chính */}
          <div className="flex flex-col max-w-4xl mx-auto mt-10">
            <h1 className="text-3xl font-black text-slate-900 mb-4">{service.name}</h1>

            <div className="flex flex-wrap items-center gap-3 mb-8">
              <span className="inline-flex items-center gap-1.5 bg-primary/10 text-primary font-black text-[20px] px-5 py-2.5 rounded-2xl">
                {hasOptions ? `Từ ${formatPrice(service.price)}` : formatPrice(service.price)}
              </span>
              <span className="inline-flex items-center gap-1.5 bg-slate-100 text-slate-600 font-bold text-[14px] px-4 py-2.5 rounded-2xl">
                <svg className="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                {formatDuration(service.durationMinutes)}
              </span>
            </div>

            {/* Bảng giá Options nếu có */}
            {hasOptions && (
              <div className="mb-8 border border-slate-200/80 rounded-2xl overflow-hidden shadow-sm bg-slate-50/50">
                <div className="bg-slate-100 px-5 py-3 border-b border-slate-200 font-extrabold text-[13px] text-slate-700 uppercase tracking-wide flex items-center justify-between">
                  <span>Tùy chọn / Phân loại chất liệu</span>
                  <span>Chi phí</span>
                </div>
                <div className="divide-y divide-slate-200/60 bg-white">
                  {service.options!.map((opt, idx) => (
                    <div key={opt.id || idx} className="px-5 py-3.5 flex items-center justify-between hover:bg-slate-50 transition-colors">
                      <span className="text-[14px] font-bold text-slate-800">{opt.name}</span>
                      <span className="text-[14px] font-extrabold text-primary">
                        {formatPrice(opt.price)} {opt.unit ? `/ ${opt.unit}` : ""}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <p className="text-slate-600 text-[15px] leading-relaxed whitespace-pre-line mb-8">
              {service.description}
            </p>

            <div className="flex items-center gap-4">
              <Link
                href="/huong-dan-su-dung"
                className="inline-flex items-center justify-center gap-2 rounded-full bg-primary hover:bg-primary/90 text-white font-black text-[15px] px-7 py-4 shadow-lg shadow-primary/25 hover:shadow-xl transition-all"
              >
                Đặt lịch khám ngay
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                </svg>
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Bài viết mô tả chi tiết (Nha Khoa Kim style) */}
      {service.content && service.content.trim().length > 0 && (
        <section className="py-16 bg-white border-b border-slate-200/60">
          <div className="max-w-7xl mx-auto px-6">
            <div className="max-w-4xl mx-auto">
              <div className="text-center mb-10">
                <span className="text-[12px] font-black tracking-widest text-primary uppercase">Thông Tin Chi Tiết</span>
                <h2 className="text-2xl md:text-3xl font-black text-slate-900 mt-2">
                  Tìm hiểu thêm về dịch vụ {service.name}
                </h2>
                <div className="w-12 h-1 bg-primary rounded-full mx-auto mt-4" />
              </div>

              {/* Render HTML content safely */}
              <div
                className="prose prose-slate max-w-none text-slate-700 leading-relaxed text-[15px] prose-headings:font-black prose-headings:text-slate-900 prose-h2:text-2xl prose-h3:text-xl prose-img:rounded-2xl prose-img:shadow-md prose-img:mx-auto prose-a:text-primary prose-a:font-bold"
                dangerouslySetInnerHTML={{ __html: service.content }}
              />
            </div>
          </div>
        </section>
      )}

      {/* Bài viết liên quan đến dịch vụ */}
      {relatedPosts.length > 0 && (
        <section className="py-16 bg-slate-50">
          <div className="max-w-7xl mx-auto px-6">
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
