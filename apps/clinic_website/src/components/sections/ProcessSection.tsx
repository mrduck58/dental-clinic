import { getClinicInfoSafe } from "@/lib/api";

// Icon trình bày cho từng bước — giữ ở frontend, gán theo thứ tự (API chỉ cấp nội dung).
const STEP_ICONS = [
  <svg key="0" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" /></svg>,
  <svg key="1" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 15.75l-2.489-2.489m0 0a3.375 3.375 0 10-4.773-4.773 3.375 3.375 0 004.774 4.774zM21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>,
  <svg key="2" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1 1 .03 2.798-1.414 2.798H4.213c-1.444 0-2.414-1.798-1.414-2.798L4.2 15.3" /></svg>,
  <svg key="3" className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" /></svg>,
];

export default async function ProcessSection() {
  const info = await getClinicInfoSafe();
  if (!info) return null;

  const steps = info.treatmentSteps;

  return (
    <section className="py-16 bg-slate-900">
      <div className="max-w-7xl mx-auto px-6">
        <div className="text-center max-w-2xl mx-auto mb-12">
          <span className="text-[12px] font-black tracking-widest text-primary uppercase">Quy Trình</span>
          <h2 className="text-3xl md:text-4xl font-black text-white mt-2 mb-4">
            Quy Trình Khám Đơn Giản
          </h2>
          <p className="text-slate-400 text-base leading-relaxed">
            Chỉ {steps.length} bước đơn giản để sở hữu nụ cười hoàn hảo mà bạn mong ước.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          {steps.map(({ title, description }, i) => (
            <div key={`${title}-${i}`} className="relative flex flex-col items-start p-6 rounded-2xl border border-white/10 bg-white/5 hover:bg-white/10 transition-all">
              <div className="flex items-center justify-between w-full mb-5">
                <div className="w-11 h-11 rounded-xl bg-primary/20 text-primary flex items-center justify-center">
                  {STEP_ICONS[i % STEP_ICONS.length]}
                </div>
                <span className="text-4xl font-black text-white/10 select-none">{String(i + 1).padStart(2, "0")}</span>
              </div>
              <h3 className="text-[16px] font-bold text-white mb-2">{title}</h3>
              <p className="text-slate-400 text-[13px] leading-relaxed">{description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
