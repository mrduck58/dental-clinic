import Link from "next/link";
import { getClinicInfoSafe } from "@/lib/api";

export default async function AboutSection({ preview = false }: { preview?: boolean }) {
  const info = await getClinicInfoSafe();
  if (!info) return null;

  const { aboutTitle: title, foundedYear, milestones, certifications } = info;

  // Mỗi đoạn văn cách nhau bằng "\n\n".
  const paragraphs = info.aboutDescription.split("\n\n").filter((p) => p.trim().length > 0);

  return (
    <section className="py-16 bg-slate-50">
      <div className="max-w-7xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">

          {/* Left — image collage */}
          <div className="relative h-[320px] sm:h-[420px] lg:h-[500px] mb-6 lg:mb-0">
            {/* Main photo */}
            <div className="absolute top-0 left-0 w-[68%] h-[72%] rounded-2xl sm:rounded-3xl overflow-hidden shadow-xl">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={info.aboutImageUrl || "https://picsum.photos/seed/aboutclinic1/600/440"}
                alt="Phòng khám Sơn Giang Dental"
                className="w-full h-full object-cover"
              />
            </div>
            {/* Secondary photo */}
            <div className="absolute bottom-0 right-0 w-[55%] h-[55%] rounded-2xl sm:rounded-3xl overflow-hidden shadow-xl border-4 border-white">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src="https://picsum.photos/seed/aboutclinic2/400/320"
                alt="Đội ngũ Sơn Giang Dental"
                className="w-full h-full object-cover"
              />
            </div>
            {/* Badge: thành lập */}
            <div className="absolute top-[62%] left-[60%] -translate-x-1/2 bg-primary rounded-2xl px-4 sm:px-5 py-3 sm:py-4 shadow-xl text-center z-10">
              <div className="text-2xl sm:text-3xl font-black text-white leading-none">{foundedYear}</div>
              <div className="text-[10px] sm:text-[11px] font-bold text-white/80 mt-1 whitespace-nowrap">Năm thành lập</div>
            </div>
          </div>

          {/* Right — content */}
          <div>
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Về Chúng Tôi</span>
            <h2 className="text-2xl sm:text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4 sm:mb-5">
              {title}
            </h2>
            {paragraphs.map((p, i) => (
              <p key={i} className="text-slate-500 text-[14px] sm:text-[15px] leading-relaxed mb-4 sm:mb-5 last:mb-6 sm:last:mb-8">
                {p}
              </p>
            ))}

            {preview ? (
              <Link
                href="/gioi-thieu"
                className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-6 sm:px-7 py-3 sm:py-3.5 rounded-xl font-bold text-[13px] sm:text-[14px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
              >
                Tìm hiểu thêm về chúng tôi
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                </svg>
              </Link>
            ) : (
              <>
                {/* Milestones */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
                  {milestones.map(({ year, description }, i) => (
                    <div key={`${year}-${i}`} className="flex gap-3">
                      <div className="w-12 h-12 rounded-xl bg-primary/10 text-primary flex items-center justify-center font-black text-[12px] shrink-0">
                        {year}
                      </div>
                      <p className="text-[13px] text-slate-500 leading-snug pt-1">{description}</p>
                    </div>
                  ))}
                </div>

                {/* Certifications */}
                <div className="flex flex-wrap gap-2">
                  {certifications.map((cert, i) => (
                    <span key={`${cert}-${i}`} className="text-[12px] font-bold text-slate-600 border border-slate-200 bg-white rounded-xl px-3 py-1.5 flex items-center gap-1.5">
                      <svg className="w-3.5 h-3.5 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" />
                      </svg>
                      {cert}
                    </span>
                  ))}
                </div>
              </>
            )}
          </div>

        </div>
      </div>
    </section>
  );
}
