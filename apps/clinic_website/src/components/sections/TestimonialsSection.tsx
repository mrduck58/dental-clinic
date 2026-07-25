import type { FeedbackDto } from "@/types/api";

function Stars({ rating }: { rating: number }) {
  const count = Math.max(0, Math.min(5, rating));
  return (
    <div className="flex items-center gap-1 mb-4">
      {[1, 2, 3, 4, 5].map((i) => (
        <svg
          key={i}
          className={`w-4 h-4 fill-current ${i <= count ? "text-amber-400" : "text-slate-200"}`}
          viewBox="0 0 20 20"
        >
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
        </svg>
      ))}
    </div>
  );
}

export default function TestimonialsSection({
  feedbacks,
  preview = false,
}: {
  feedbacks: FeedbackDto[];
  preview?: boolean;
}) {
  const items = preview ? feedbacks.slice(0, 3) : feedbacks;

  if (items.length === 0) return null;

  return (
    <section className="py-16 bg-slate-50">
      <div className="max-w-7xl mx-auto px-6">
        <div className="text-center max-w-2xl mx-auto mb-12">
          <span className="text-[12px] font-black tracking-widest text-primary uppercase">Đánh Giá</span>
          <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
            Khách Hàng Nói Gì
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {items.map((f) => (
            <div key={f.id} className="glass-card bg-white rounded-2xl p-7 border border-slate-200/60 flex flex-col shadow-sm">
              <Stars rating={f.rating} />
              <p className="text-slate-600 text-[14px] leading-relaxed flex-1 mb-5">&ldquo;{f.comment}&rdquo;</p>
              {f.replyText && (
                <div className="text-[12px] text-slate-500 bg-slate-50 border border-slate-100 rounded-xl p-3 mb-4">
                  <span className="font-bold text-primary">Phản hồi từ phòng khám: </span>
                  {f.replyText}
                </div>
              )}
              <div className="flex items-center gap-3 border-t border-slate-100 pt-4">
                <div className="w-10 h-10 rounded-full bg-primary/10 text-primary flex items-center justify-center font-black text-[15px] shrink-0">
                  {f.customerName.charAt(0).toUpperCase()}
                </div>
                <div>
                  <div className="text-[14px] font-bold text-slate-900">{f.customerName}</div>
                  <div className="text-[12px] text-slate-400 font-medium">Khách hàng</div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
