import Link from "next/link";

export default function Footer() {
  return (
    <footer className="bg-slate-900 text-slate-400 pt-16 pb-8">
      <div className="max-w-7xl mx-auto px-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-10 mb-12">
          {/* Brand */}
          <div className="lg:col-span-1">
            <div className="flex items-center gap-2.5 mb-5">
              <span className="text-2xl">🦷</span>
              <div className="flex flex-col leading-none">
                <span className="text-[10px] font-black tracking-widest text-primary uppercase">SơnGiang</span>
                <span className="font-extrabold text-xl text-white">
                  Dental<span className="text-primary">Clinic</span>
                </span>
              </div>
            </div>
            <p className="text-slate-400 text-[13px] leading-relaxed mb-5">
              Hệ thống phòng khám nha khoa cao cấp cam kết trải nghiệm chăm sóc không đau, chuẩn xác và bền vững.
            </p>
            <div className="flex items-center gap-2">
              {["fb", "ig", "yt"].map((s) => (
                <button key={s} type="button" className="w-9 h-9 rounded-xl bg-white/5 hover:bg-primary/20 hover:text-primary flex items-center justify-center text-slate-400 transition-all text-[11px] font-bold uppercase">
                  {s}
                </button>
              ))}
            </div>
          </div>

          {/* Dịch vụ */}
          <div>
            <h4 className="text-white font-bold mb-5 text-[14px]">Dịch Vụ</h4>
            <div className="flex flex-col gap-3 text-[13px]">
              {["Niềng răng Invisalign", "Cấy ghép Implant", "Bọc răng sứ thẩm mỹ", "Tẩy trắng răng", "Điều trị tủy"].map(s => (
                <Link key={s} href="/dich-vu" className="hover:text-white transition-colors">{s}</Link>
              ))}
            </div>
          </div>

          {/* Thông tin */}
          <div>
            <h4 className="text-white font-bold mb-5 text-[14px]">Thông Tin</h4>
            <div className="flex flex-col gap-3 text-[13px]">
              {[
                { label: "Về chúng tôi", href: "/gioi-thieu" },
                { label: "Đội ngũ bác sĩ", href: "/bac-si" },
                { label: "Bảng giá dịch vụ", href: "/bang-gia" },
                { label: "Tin tức & sự kiện", href: "/tin-tuc" },
              ].map(({ label, href }) => (
                <Link key={label} href={href} className="hover:text-white transition-colors">{label}</Link>
              ))}
            </div>
          </div>

          {/* Liên hệ */}
          <div>
            <h4 className="text-white font-bold mb-5 text-[14px]">Liên Hệ</h4>
            <div className="flex flex-col gap-4 text-[13px]">
              <div className="flex gap-3">
                <svg className="w-4 h-4 text-primary shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1115 0z" />
                </svg>
                <span>123 Đường Ba Tháng Hai, Quận 10, TP.HCM</span>
              </div>
              <div className="flex gap-3">
                <svg className="w-4 h-4 text-primary shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 6.75c0 8.284 6.716 15 15 15h2.25a2.25 2.25 0 002.25-2.25v-1.372c0-.516-.351-.966-.852-1.091l-4.423-1.106c-.44-.11-.902.055-1.173.417l-.97 1.293c-.282.376-.769.542-1.21.38a12.035 12.035 0 01-7.143-7.143c-.162-.441.004-.928.38-1.21l1.293-.97c.363-.271.527-.734.417-1.173L6.963 3.102a1.125 1.125 0 00-1.091-.852H4.5A2.25 2.25 0 002.25 4.5v2.25z" />
                </svg>
                <div className="flex flex-col gap-0.5">
                  <span>1900 6789</span>
                  <span>028 7300 1234</span>
                </div>
              </div>
              <div className="flex gap-3">
                <svg className="w-4 h-4 text-primary shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                </svg>
                <span>contact@songiangdental.vn</span>
              </div>
              <div className="flex gap-3">
                <svg className="w-4 h-4 text-primary shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <div className="flex flex-col gap-0.5">
                  <span>T2–T6: 8:00 – 20:00</span>
                  <span>T7–CN: 8:00 – 17:00</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="border-t border-slate-800 pt-8 flex flex-col sm:flex-row items-center justify-between gap-4 text-[12px] text-slate-500">
          <span>© 2026 Sơn Giang Dental Clinic. Bảo lưu mọi quyền.</span>
          <div className="flex items-center gap-4">
            <span className="cursor-default text-slate-500">Chính sách bảo mật</span>
            <span className="cursor-default text-slate-500">Điều khoản sử dụng</span>
          </div>
        </div>
      </div>
    </footer>
  );
}
