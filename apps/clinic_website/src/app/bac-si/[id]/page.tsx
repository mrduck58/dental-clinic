import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import PageHeader from "@/components/shared/PageHeader";
import { getDentistById } from "@/lib/api";

export const dynamic = "force-dynamic";

const FALLBACK_PHOTO = "https://picsum.photos/seed/dentistdetail/600/700";

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  try {
    const dentist = await getDentistById(id);
    if (dentist?.fullName) {
      return {
        title: `${dentist.fullName} | Sơn Giang Dental Clinic`,
        description: dentist.bio?.slice(0, 160) ?? dentist.specialty ?? undefined,
      };
    }
  } catch {
    /* bỏ qua — dùng title mặc định */
  }
  return { title: "Bác sĩ | Sơn Giang Dental Clinic" };
}

export default async function DentistDetailPage({ params }: Props) {
  const { id } = await params;
  const dentist = await getDentistById(id);
  if (!dentist) notFound();

  return (
    <div className="animate-fade-in">
      <PageHeader eyebrow="Đội Ngũ" title={dentist.fullName ?? "Bác sĩ"} />

      <section className="py-16 bg-white">
        <div className="max-w-5xl mx-auto px-6 grid grid-cols-1 lg:grid-cols-5 gap-12 items-start">
          {/* Ảnh */}
          <div className="lg:col-span-2 rounded-3xl overflow-hidden shadow-xl border border-slate-200/60">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={dentist.profilePictureUrl || FALLBACK_PHOTO}
              alt={dentist.fullName ?? "Bác sĩ"}
              className="w-full h-full object-cover object-top"
            />
          </div>

          {/* Thông tin */}
          <div className="lg:col-span-3">
            {dentist.specialty && (
              <span className="inline-flex items-center self-start px-3 py-1 rounded-full text-[12px] font-bold mb-4 bg-primary/10 text-primary">
                {dentist.specialty}
              </span>
            )}
            <h1 className="text-3xl font-black text-slate-900 mb-3">{dentist.fullName ?? "Bác sĩ"}</h1>

            {typeof dentist.yearsOfExperience === "number" && dentist.yearsOfExperience > 0 && (
              <div className="text-[14px] text-primary font-bold mb-6 flex items-center gap-1.5">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 18.75h-9m9 0a3 3 0 013 3h-15a3 3 0 013-3m9 0v-3.375c0-.621-.503-1.125-1.125-1.125h-.871M7.5 18.75v-3.375c0-.621.504-1.125 1.125-1.125h.872" />
                </svg>
                {dentist.yearsOfExperience}+ năm kinh nghiệm
              </div>
            )}

            {dentist.bio ? (
              <p className="text-slate-600 text-[15px] leading-relaxed whitespace-pre-line mb-8">{dentist.bio}</p>
            ) : (
              <p className="text-slate-400 text-[15px] italic mb-8">Chưa có thông tin giới thiệu.</p>
            )}

            <Link
              href="/tai-ung-dung"
              className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-7 py-3.5 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
            >
              Đặt lịch với bác sĩ
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </Link>

            <div className="mt-8">
              <Link href="/bac-si" className="text-[14px] font-bold text-slate-500 hover:text-primary transition-colors inline-flex items-center gap-1.5">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
                Tất cả bác sĩ
              </Link>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
