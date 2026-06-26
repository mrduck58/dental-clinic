import Link from "next/link";
import type { ReactNode } from "react";
import type { ServiceDto } from "@/types/api";
import { serviceIcon, serviceColor } from "@/lib/serviceIcons";
import { formatPrice } from "@/lib/format";

export default function ServicesSection({
  services,
  preview = false,
  showIntro = true,
  toolbar,
  footer,
}: {
  services: ServiceDto[];
  preview?: boolean;
  showIntro?: boolean;
  toolbar?: ReactNode;
  footer?: ReactNode;
}) {
  const items = preview ? services.slice(0, 3) : services;

  return (
    <section className={`bg-slate-50 ${showIntro ? "py-16" : "pt-8 pb-16"}`}>
      <div className="max-w-7xl mx-auto px-6">
        {showIntro && (
          <div className="text-center max-w-2xl mx-auto mb-12">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Dịch Vụ</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
              Dịch Vụ Nổi Bật
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Đa dạng dịch vụ điều trị và thẩm mỹ răng miệng cao cấp giúp bạn tự tin với nụ cười toả sáng.
            </p>
          </div>
        )}

        {toolbar}

        {items.length === 0 ? (
          <p className="text-center text-slate-400 text-[15px] py-10">
            {toolbar ? "Không tìm thấy dịch vụ phù hợp." : "Chưa có dịch vụ nào."}
          </p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {items.map((service, i) => {
              const color = serviceColor(i);
              return (
                <Link
                  key={service.id}
                  href={`/dich-vu/${service.id}`}
                  className="glass-card hover-lift bg-white rounded-2xl p-7 border border-slate-200/60 flex flex-col shadow-sm hover:shadow-lg"
                >
                  <div className={`w-12 h-12 rounded-xl flex items-center justify-center mb-5 ${color === "primary" ? "bg-primary/10 text-primary" : "bg-secondary/10 text-secondary"}`}>
                    {serviceIcon(i)}
                  </div>
                  <h3 className="text-[17px] font-bold text-slate-900 mb-2">{service.name}</h3>
                  <p className="text-slate-500 text-[13px] leading-relaxed flex-1 line-clamp-3">{service.description}</p>
                  <div className="mt-5 flex items-center justify-between">
                    <span className={`text-[15px] font-black ${color === "primary" ? "text-primary" : "text-secondary"}`}>
                      {formatPrice(service.price)}
                    </span>
                    <span className={`inline-flex items-center gap-1.5 text-[13px] font-bold transition-all ${color === "primary" ? "text-primary" : "text-secondary"}`}>
                      Tìm hiểu thêm
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                      </svg>
                    </span>
                  </div>
                </Link>
              );
            })}
          </div>
        )}

        {footer}

        {preview && services.length > 3 && (
          <div className="text-center mt-12">
            <Link
              href="/dich-vu"
              className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-7 py-3.5 rounded-xl font-bold text-[14px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
            >
              Xem tất cả dịch vụ
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </Link>
          </div>
        )}
      </div>
    </section>
  );
}
