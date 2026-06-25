import Link from "next/link";
import type { ReactNode } from "react";
import type { DentistDto } from "@/types/api";

const BADGE_COLORS = ["bg-primary/10 text-primary", "bg-secondary/10 text-secondary"];
const FALLBACK_PHOTO = "https://picsum.photos/seed/dentist/400/400";

export default function DentistsSection({
  dentists,
  preview = false,
  showIntro = true,
  toolbar,
  footer,
}: {
  dentists: DentistDto[];
  preview?: boolean;
  showIntro?: boolean;
  toolbar?: ReactNode;
  footer?: ReactNode;
}) {
  const items = preview ? dentists.slice(0, 3) : dentists;

  return (
    <section className={`bg-white ${showIntro ? "py-16" : "pt-8 pb-16"}`}>
      <div className="max-w-7xl mx-auto px-6">
        {showIntro && (
          <div className="text-center max-w-2xl mx-auto mb-12">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Đội Ngũ</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
              Bác Sĩ Chuyên Gia
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Đội ngũ bác sĩ tâm huyết, giàu kinh nghiệm lâm sàng và liên tục tu nghiệp quốc tế.
            </p>
          </div>
        )}

        {toolbar}

        {items.length === 0 ? (
          <p className="text-center text-slate-400 text-[15px] py-10">
            {toolbar ? "Không tìm thấy bác sĩ phù hợp." : "Chưa có thông tin bác sĩ."}
          </p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {items.map((d, i) => (
              <Link
                key={d.id}
                href={`/bac-si/${d.id}`}
                className="glass-card hover-lift rounded-2xl border border-slate-200/60 overflow-hidden shadow-sm flex flex-col bg-white"
              >
                <div className="h-64 overflow-hidden">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={d.profilePictureUrl || FALLBACK_PHOTO} alt={d.fullName ?? "Bác sĩ"} className="w-full h-full object-cover object-top" />
                </div>
                <div className="p-6 flex flex-col flex-1">
                  {d.specialty && (
                    <span className={`inline-flex items-center self-start px-3 py-1 rounded-full text-[11px] font-bold mb-3 ${BADGE_COLORS[i % BADGE_COLORS.length]}`}>
                      {d.specialty}
                    </span>
                  )}
                  <h3 className="text-[17px] font-bold text-slate-900 mb-2">{d.fullName ?? "Bác sĩ"}</h3>
                  {d.bio && <p className="text-slate-500 text-[13px] leading-relaxed mb-4 flex-1 line-clamp-3">{d.bio}</p>}
                  {typeof d.yearsOfExperience === "number" && d.yearsOfExperience > 0 && (
                    <div className="text-[12px] text-primary font-bold border-t border-slate-100 pt-3 flex items-center gap-1.5 mt-auto">
                      <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 18.75h-9m9 0a3 3 0 013 3h-15a3 3 0 013-3m9 0v-3.375c0-.621-.503-1.125-1.125-1.125h-.871M7.5 18.75v-3.375c0-.621.504-1.125 1.125-1.125h.872m5.007 0H9.497m5.007 0a7.454 7.454 0 01-.982-3.172M9.497 14.25a7.454 7.454 0 00.981-3.172" />
                      </svg>
                      {d.yearsOfExperience}+ năm kinh nghiệm
                    </div>
                  )}
                </div>
              </Link>
            ))}
          </div>
        )}

        {footer}

        {preview && dentists.length > 3 && (
          <div className="text-center mt-12">
            <Link
              href="/bac-si"
              className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-7 py-3.5 rounded-xl font-bold text-[14px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
            >
              Xem toàn bộ đội ngũ bác sĩ
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
