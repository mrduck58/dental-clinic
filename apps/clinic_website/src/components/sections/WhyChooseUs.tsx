import { getClinicInfoSafe } from "@/lib/api";

// Icon trình bày cho thẻ thống kê — giữ ở frontend, gán theo thứ tự (API chỉ cấp nội dung).
const STAT_ICONS = [
  <svg key="0" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" /></svg>,
  <svg key="1" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" /></svg>,
  <svg key="2" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 18.75h-9m9 0a3 3 0 013 3h-15a3 3 0 013-3m9 0v-3.375c0-.621-.503-1.125-1.125-1.125h-.871M7.5 18.75v-3.375c0-.621.504-1.125 1.125-1.125h.872m5.007 0H9.497m5.007 0a7.454 7.454 0 01-.982-3.172M9.497 14.25a7.454 7.454 0 00.981-3.172M5.25 4.236c-.982.143-1.954.317-2.916.52A6.003 6.003 0 007.73 9.728M5.25 4.236V4.5c0 2.108.966 3.99 2.48 5.228M5.25 4.236V2.721C7.456 2.41 9.71 2.25 12 2.25c2.291 0 4.545.16 6.75.47v1.516M7.73 9.728a6.726 6.726 0 002.748 1.35m8.272-6.842V4.5c0 2.108-.966 3.99-2.48 5.228m2.48-5.492a46.32 46.32 0 012.916.52 6.003 6.003 0 01-5.395 4.972m0 0a6.726 6.726 0 01-2.749 1.35m0 0a6.772 6.772 0 01-3.044 0" /></svg>,
  <svg key="3" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.562.562 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" /></svg>,
];

export default async function WhyChooseUs() {
  const info = await getClinicInfoSafe();
  if (!info) return null;

  const { features, statistics: stats } = info;

  return (
    <section className="py-16 bg-white">
      <div className="max-w-7xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
          {/* Left text */}
          <div>
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Tại Sao Chọn Chúng Tôi</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-5">
              Tiêu Chuẩn Chăm Sóc<br />Vượt Trội
            </h2>
            <p className="text-slate-500 text-[15px] leading-relaxed mb-10">
              Chúng tôi không chỉ điều trị răng — chúng tôi mang đến trải nghiệm chăm sóc sức khoẻ toàn diện với sự tận tâm và chuyên nghiệp cao nhất.
            </p>

            <div className="flex flex-col gap-5">
              {features.map(({ title, description }, i) => (
                <div key={`${title}-${i}`} className="flex gap-4">
                  <div className="w-8 h-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0 font-black text-[13px] mt-0.5">
                    {String(i + 1).padStart(2, "0")}
                  </div>
                  <div>
                    <div className="text-[15px] font-bold text-slate-900 mb-1">{title}</div>
                    <div className="text-[13px] text-slate-500 leading-relaxed">{description}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Right — stat cards */}
          <div className="grid grid-cols-2 gap-5">
            {stats.map(({ value, label }, i) => {
              const isPrimary = i % 2 === 0;
              return (
                <div key={`${label}-${i}`} className={`rounded-2xl p-7 flex flex-col items-start border ${isPrimary ? "bg-primary/5 border-primary/10" : "bg-secondary/5 border-secondary/10"}`}>
                  <div className={`w-10 h-10 rounded-xl flex items-center justify-center mb-4 ${isPrimary ? "bg-primary/15 text-primary" : "bg-secondary/15 text-secondary"}`}>
                    {STAT_ICONS[i % STAT_ICONS.length]}
                  </div>
                  <span className={`text-4xl font-black leading-none mb-2 ${isPrimary ? "text-primary" : "text-secondary"}`}>{value}</span>
                  <span className="text-[13px] font-semibold text-slate-500">{label}</span>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}
