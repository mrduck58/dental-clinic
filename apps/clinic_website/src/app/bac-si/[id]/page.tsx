import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getDentistById, getDentistReviews, getDentists } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { shuffle } from "@/lib/listing";
import MediaPlaceholder from "@/components/shared/MediaPlaceholder";
import DentistsSection from "@/components/sections/DentistsSection";
import type { DentistReviewsResultDto } from "@/types/api";

const OTHER_DENTISTS_COUNT = 3;

export const dynamic = "force-dynamic";

const SHIFT_LABEL: Record<string, string> = {
  morning: "Ca sáng",
  afternoon: "Ca chiều",
};

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

/** Số năm công tác tại phòng khám, tính từ ngày bắt đầu làm việc. */
function yearsAtClinic(startDate?: string | null): number | null {
  if (!startDate) return null;
  const start = new Date(startDate);
  if (Number.isNaN(start.getTime())) return null;
  const years = Math.floor((Date.now() - start.getTime()) / (365.25 * 24 * 3600 * 1000));
  return years >= 0 ? years : null;
}

function StatCard({ value, label }: { value: string; label: string }) {
  return (
    <div className="rounded-2xl bg-slate-50 border border-slate-200/60 px-4 py-3 text-center">
      <div className="text-xl font-black text-primary">{value}</div>
      <div className="text-[12px] font-semibold text-slate-500 mt-0.5">{label}</div>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-baseline gap-0.5 sm:gap-3 py-2.5 border-b border-slate-100 last:border-0">
      <dt className="text-[13px] font-semibold text-slate-400 sm:w-48 shrink-0">{label}</dt>
      <dd className="text-[14px] text-slate-700 font-medium">{value}</dd>
    </div>
  );
}

function Stars({ rating }: { rating: number }) {
  return (
    <span className="inline-flex items-center gap-0.5" aria-label={`${rating} trên 5 sao`}>
      {[1, 2, 3, 4, 5].map((i) => (
        <svg
          key={i}
          className={`w-4 h-4 ${i <= Math.round(rating) ? "text-amber-400" : "text-slate-200"}`}
          fill="currentColor"
          viewBox="0 0 20 20"
        >
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.286 3.958a1 1 0 00.95.69h4.162c.969 0 1.371 1.24.588 1.81l-3.367 2.446a1 1 0 00-.364 1.118l1.287 3.957c.3.922-.755 1.688-1.539 1.118l-3.366-2.446a1 1 0 00-1.176 0l-3.367 2.446c-.783.57-1.838-.196-1.538-1.118l1.286-3.957a1 1 0 00-.363-1.118L2.063 9.385c-.783-.57-.38-1.81.588-1.81h4.161a1 1 0 00.951-.69l1.286-3.958z" />
        </svg>
      ))}
    </span>
  );
}

export default async function DentistDetailPage({ params }: Props) {
  const { id } = await params;
  const dentist = await getDentistById(id);
  if (!dentist) notFound();

  const reviewsResult: DentistReviewsResultDto | null = await getDentistReviews(dentist.id).catch(
    () => null
  );
  const reviews = reviewsResult?.reviews ?? [];
  const averageRating = reviewsResult?.averageRating ?? dentist.averageRating;
  const reviewCount = reviewsResult?.reviewCount ?? dentist.reviewCount;

  // Bác sĩ khác — chọn ngẫu nhiên để gợi ý xem thêm, loại bác sĩ đang xem.
  const allDentists = await getDentists().catch(() => []);
  const otherDentists = shuffle(allDentists.filter((d) => d.id !== dentist.id)).slice(
    0,
    OTHER_DENTISTS_COUNT
  );

  const tenure = yearsAtClinic(dentist.startDate);

  // Chỉ hiển thị dòng có dữ liệu — tránh khoảng trống "Chưa cập nhật".
  const professionalRows: { label: string; value: string }[] = [
    { label: "Chuyên khoa", value: dentist.specialty ?? "" },
    { label: "Chức vụ", value: dentist.position ?? "" },
    { label: "Khoa / Bộ phận", value: dentist.department ?? "" },
    { label: "Trình độ học vấn", value: dentist.education ?? "" },
    { label: "Số chứng chỉ hành nghề", value: dentist.licenseNumber ?? "" },
    { label: "Nơi cấp chứng chỉ", value: dentist.certificateIssuedBy ?? "" },
    { label: "Ngày cấp chứng chỉ", value: formatDate(dentist.certificateIssuedDate) },
    {
      label: "Công tác tại phòng khám",
      value: dentist.startDate
        ? `Từ ${formatDate(dentist.startDate)}${tenure && tenure > 0 ? ` (${tenure} năm)` : ""}`
        : "",
    },
    { label: "Hình thức làm việc", value: dentist.employmentType ?? "" },
    { label: "Ca làm việc", value: dentist.shift ? SHIFT_LABEL[dentist.shift] ?? dentist.shift : "" },
    { label: "Giới tính", value: dentist.gender ?? "" },
  ].filter((row) => row.value.trim() !== "");

  return (
    <div className="animate-fade-in">
      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-6 grid grid-cols-1 lg:grid-cols-5 gap-12 items-start">
          {/* Ảnh */}
          <div className="lg:col-span-2 lg:sticky lg:top-24">
            <div className="rounded-3xl overflow-hidden shadow-xl border border-slate-200/60 aspect-[6/7]">
              {dentist.profilePictureUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={dentist.profilePictureUrl}
                  alt={dentist.fullName ?? "Bác sĩ"}
                  className="w-full h-full object-cover object-top"
                />
              ) : (
                <MediaPlaceholder variant="person" />
              )}
            </div>

            {/* Số liệu hoạt động */}
            <div className="grid grid-cols-2 gap-3 mt-6">
              {typeof dentist.yearsOfExperience === "number" && dentist.yearsOfExperience > 0 && (
                <StatCard value={`${dentist.yearsOfExperience}+`} label="Năm kinh nghiệm" />
              )}
              {dentist.appointmentCount > 0 && (
                <StatCard value={`${dentist.appointmentCount}`} label="Ca điều trị" />
              )}
            </div>

            <Link
              href="/huong-dan-su-dung"
              className="mt-6 w-full inline-flex items-center justify-center gap-2 rounded-full bg-primary hover:bg-primary/90 text-white font-bold text-[14px] px-6 py-3.5 transition-colors"
            >
              Hướng dẫn đặt lịch
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </Link>
          </div>

          {/* Thông tin */}
          <div className="lg:col-span-3">
            {dentist.specialty && (
              <span className="inline-flex items-center self-start px-3 py-1 rounded-full text-[12px] font-bold mb-4 bg-primary/10 text-primary">
                {dentist.specialty}
              </span>
            )}
            <h1 className="text-3xl font-black text-slate-900 mb-3">{dentist.fullName ?? "Bác sĩ"}</h1>

            {(dentist.position || dentist.department) && (
              <p className="text-[14px] text-slate-500 font-semibold mb-3">
                {[dentist.position, dentist.department].filter(Boolean).join(" • ")}
              </p>
            )}

            {reviewCount > 0 && (
              <div className="flex items-center gap-2 mb-6">
                <Stars rating={averageRating} />
                <span className="text-[14px] font-bold text-slate-700">{averageRating.toFixed(1)}</span>
                <span className="text-[13px] text-slate-400">({reviewCount} đánh giá)</span>
              </div>
            )}

            {/* Hồ sơ chuyên môn */}
            {professionalRows.length > 0 && (
              <>
                <h2 className="text-[13px] font-black uppercase tracking-wider text-slate-400 mb-1">
                  Hồ sơ chuyên môn
                </h2>
                <dl className="mb-10">
                  {professionalRows.map((row) => (
                    <InfoRow key={row.label} label={row.label} value={row.value} />
                  ))}
                </dl>
              </>
            )}

            {/* Dịch vụ thực hiện */}
            {dentist.services.length > 0 && (
              <>
                <h2 className="text-[13px] font-black uppercase tracking-wider text-slate-400 mb-3">
                  Dịch vụ thực hiện
                </h2>
                <div className="flex flex-wrap gap-2 mb-10">
                  {dentist.services.map((name) => (
                    <span
                      key={name}
                      className="inline-flex items-center px-3 py-1.5 rounded-full text-[13px] font-semibold bg-slate-100 text-slate-600"
                    >
                      {name}
                    </span>
                  ))}
                </div>
              </>
            )}

            {/* Đánh giá của bệnh nhân */}
            {reviews.length > 0 && (
              <>
                <h2 className="text-[13px] font-black uppercase tracking-wider text-slate-400 mb-3">
                  Đánh giá của bệnh nhân
                </h2>
                <ul className="space-y-4 mb-10">
                  {reviews.slice(0, 5).map((review) => (
                    <li key={review.id} className="rounded-2xl border border-slate-200/60 p-5">
                      <div className="flex flex-wrap items-center gap-2 mb-2">
                        <span className="text-[14px] font-bold text-slate-800">{review.patientName}</span>
                        <Stars rating={review.rating} />
                        <span className="text-[12px] text-slate-400">{formatDate(review.createdAt)}</span>
                      </div>
                      <p className="text-[14px] text-slate-600 leading-relaxed">{review.comment}</p>
                      {review.tags.length > 0 && (
                        <div className="flex flex-wrap gap-1.5 mt-3">
                          {review.tags.map((tag) => (
                            <span
                              key={tag}
                              className="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-primary/10 text-primary"
                            >
                              {tag}
                            </span>
                          ))}
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              </>
            )}

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

      {/* Bài viết chi tiết về nha sĩ */}
      {dentist.content && dentist.content.trim().length > 0 && (
        <section className="py-16 bg-white border-t border-slate-200/60">
          <div className="max-w-7xl mx-auto px-6">
            <div className="max-w-4xl mx-auto">
              <div className="text-center mb-10">
                <span className="text-[12px] font-black tracking-widest text-primary uppercase">Bài Viết Chi Tiết</span>
                <h2 className="text-2xl md:text-3xl font-black text-slate-900 mt-2">
                  Tìm hiểu thêm về {dentist.fullName ?? "bác sĩ"}
                </h2>
                <div className="w-12 h-1 bg-primary rounded-full mx-auto mt-4" />
              </div>

              {/* Render HTML content safely */}
              <div
                className="prose prose-slate max-w-none text-slate-700 leading-relaxed text-[15px] prose-headings:font-black prose-headings:text-slate-900 prose-h2:text-2xl prose-h3:text-xl prose-img:rounded-2xl prose-img:shadow-md prose-img:mx-auto prose-a:text-primary prose-a:font-bold"
                dangerouslySetInnerHTML={{ __html: dentist.content }}
              />
            </div>
          </div>
        </section>
      )}

      {/* Bác sĩ khác — gợi ý ngẫu nhiên */}
      {otherDentists.length > 0 && (
        <DentistsSection
          dentists={otherDentists}
          eyebrow="Đội Ngũ"
          title="Bác Sĩ Khác"
          description="Có thể bạn cũng muốn tìm hiểu thêm về các bác sĩ khác tại phòng khám."
          bgClassName="bg-slate-50 border-t border-slate-200/60"
        />
      )}
    </div>
  );
}
