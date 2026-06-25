import Link from "next/link";

export default function PageHeader({
  eyebrow,
  title,
  description,
}: {
  eyebrow: string;
  title: string;
  description?: string;
}) {
  return (
    <section className="relative overflow-hidden bg-white border-b border-slate-100 pt-10 pb-8">
      <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-primary/5 rounded-full blur-3xl -translate-y-1/2 translate-x-1/3 pointer-events-none" />
      <div className="relative max-w-7xl mx-auto px-6">
        {/* Breadcrumb */}
        <nav className="flex items-center gap-2 text-[12px] font-semibold text-slate-400 mb-5">
          <Link href="/" className="hover:text-primary transition-colors">Trang chủ</Link>
          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
          </svg>
          <span className="text-primary">{title}</span>
        </nav>

        <span className="text-[12px] font-black tracking-widest text-primary uppercase">{eyebrow}</span>
        <h1 className="text-3xl md:text-4xl lg:text-5xl font-black text-slate-900 mt-2 tracking-tight">
          {title}
        </h1>
        {description && (
          <p className="text-slate-500 text-[15px] leading-relaxed mt-4 max-w-2xl">{description}</p>
        )}
      </div>
    </section>
  );
}
