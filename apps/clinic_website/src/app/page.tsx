export default function Home() {
  return (
    <div className="flex flex-col min-h-screen font-sans text-slate-800 animate-fade-in">

      {/* ── NAVBAR ──────────────────────────────────────────────────────────── */}
      <header className="sticky top-0 z-50 h-20 bg-white/90 backdrop-blur-md border-b border-slate-200/60 shadow-sm">
        <div className="max-w-7xl mx-auto px-6 h-full flex items-center justify-between">
          {/* Logo */}
          <a href="/" className="flex items-center gap-2.5 select-none">
            <span className="text-2xl text-primary">🦷</span>
            <div className="flex flex-col leading-none">
              <span className="text-[10px] font-black tracking-widest text-primary uppercase">SơnGiang</span>
              <span className="font-extrabold text-xl tracking-tight text-slate-900">
                Dental<span className="text-primary">Clinic</span>
              </span>
            </div>
          </a>

          {/* Nav */}
          <nav className="hidden md:flex items-center gap-1">
            {[
              { label: "Trang chủ", href: "/", active: true },
              { label: "Giới thiệu", href: "#about" },
              { label: "Dịch vụ", href: "#services" },
              { label: "Bác sĩ", href: "#dentists" },
              { label: "Tin tức", href: "#news" },
              { label: "Liên hệ", href: "#contact" },
            ].map(({ label, href, active }) => (
              <a
                key={label}
                href={href}
                className={`px-4 py-2 rounded-xl text-[14px] font-semibold transition-all ${
                  active
                    ? "text-primary bg-primary/5"
                    : "text-slate-500 hover:text-primary hover:bg-slate-50"
                }`}
              >
                {label}
              </a>
            ))}
          </nav>

          {/* CTA */}
          <a
            href="#download"
            className="hidden md:flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-5 py-2.5 rounded-xl text-[14px] font-bold transition-all hover:translate-y-[-1px] hover:shadow-lg hover:shadow-primary/25"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 1.5H8.25A2.25 2.25 0 006 3.75v16.5a2.25 2.25 0 002.25 2.25h7.5A2.25 2.25 0 0018 20.25V3.75a2.25 2.25 0 00-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 8.25h3m-3 4.5h3m-6-4.5H6m0 4.5H4.5" />
            </svg>
            Tải ứng dụng
          </a>

          {/* Mobile hamburger */}
          <button className="md:hidden p-2 rounded-xl text-slate-500 hover:bg-slate-100 transition-all">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
            </svg>
          </button>
        </div>
      </header>

      {/* ── HERO ────────────────────────────────────────────────────────────── */}
      <section className="relative overflow-hidden bg-white pt-20 pb-24">
        {/* Background blobs */}
        <div className="absolute top-0 right-0 w-[600px] h-[600px] bg-primary/5 rounded-full blur-3xl -translate-y-1/2 translate-x-1/3 pointer-events-none" />
        <div className="absolute bottom-0 left-0 w-[400px] h-[400px] bg-secondary/5 rounded-full blur-3xl translate-y-1/2 -translate-x-1/3 pointer-events-none" />

        <div className="relative max-w-7xl mx-auto px-6 grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
          {/* Left */}
          <div className="flex flex-col items-start">
            <span className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-[12px] font-bold bg-primary/5 text-primary border border-primary/10 mb-6">
              <span className="w-2 h-2 rounded-full bg-primary animate-pulse" />{"Hệ Thống Nha Khoa Tiêu Chuẩn Quốc Tế"}
            </span>

            <h1 className="text-4xl md:text-5xl lg:text-6xl font-black text-slate-900 leading-[1.1] tracking-tight mb-6">
              Nụ Cười Toả Sáng<br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-primary to-secondary">
                Sức Khoẻ Trọn Đời
              </span>
            </h1>

            <p className="text-[16px] text-slate-500 leading-relaxed mb-10 max-w-lg">
              Sơn Giang Dental mang đến dịch vụ chăm sóc răng miệng cao cấp với công nghệ hiện đại hàng đầu và đội ngũ bác sĩ giàu kinh nghiệm lâm sàng.
            </p>

            <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
              <a
                href="#download"
                className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white px-8 py-4 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:shadow-xl hover:shadow-primary/25"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 1.5H8.25A2.25 2.25 0 006 3.75v16.5a2.25 2.25 0 002.25 2.25h7.5A2.25 2.25 0 0018 20.25V3.75a2.25 2.25 0 00-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 8.25h3m-3 4.5h3m-6-4.5H6m0 4.5H4.5" />
                </svg>
                Tải app đặt lịch ngay
              </a>
              <a
                href="#services"
                className="flex items-center justify-center gap-2 bg-white hover:bg-slate-50 text-slate-700 border border-slate-200 px-8 py-4 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:border-primary/30"
              >
                Xem dịch vụ
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                </svg>
              </a>
            </div>

            {/* Trust badges */}
            <div className="flex items-center gap-6 mt-10 pt-8 border-t border-slate-100 w-full">
              {[
                { value: "10.000+", label: "Khách hàng" },
                { value: "20+", label: "Bác sĩ chuyên khoa" },
                { value: "99%", label: "Hài lòng" },
                { value: "15+", label: "Năm kinh nghiệm" },
              ].map(({ value, label }) => (
                <div key={label} className="flex flex-col items-start">
                  <span className="text-[20px] font-black text-primary leading-none">{value}</span>
                  <span className="text-[11px] font-semibold text-slate-400 mt-0.5 whitespace-nowrap">{label}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Right — clinic photo with floating cards */}
          <div className="hidden lg:flex items-center justify-center relative h-[520px]">
            {/* Main clinic photo */}
            <div className="absolute inset-0 rounded-3xl overflow-hidden shadow-2xl">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src="https://picsum.photos/seed/sgiangdental/900/520"
                alt="Phòng khám nha khoa Sơn Giang"
                className="w-full h-full object-cover"
              />
              <div className="absolute inset-0 bg-gradient-to-tr from-slate-900/40 via-transparent to-transparent" />
            </div>

            {/* Card: Lịch hẹn */}
            <div className="absolute top-8 left-8 bg-white/95 backdrop-blur-sm rounded-2xl p-5 w-64 shadow-xl border border-white/60 z-20">
              <div className="flex items-center justify-between mb-3">
                <span className="text-[11px] font-black text-slate-500 uppercase tracking-wider">Lịch hẹn hôm nay</span>
                <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
              </div>
              <div className="flex flex-col gap-2.5">
                {[
                  { name: "Nguyễn Văn A", time: "09:00", service: "Cấy Implant", img: "https://randomuser.me/api/portraits/men/12.jpg" },
                  { name: "Trần Thị B",   time: "10:30", service: "Niềng răng",  img: "https://randomuser.me/api/portraits/women/21.jpg" },
                  { name: "Lê Minh C",    time: "13:00", service: "Bọc sứ",      img: "https://randomuser.me/api/portraits/men/45.jpg" },
                ].map(({ name, time, service, img }) => (
                  <div key={name} className="flex items-center gap-2.5">
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img src={img} alt={name} className="w-7 h-7 rounded-lg object-cover shrink-0" />
                    <div className="flex-1 min-w-0">
                      <div className="text-[12px] font-bold text-slate-800 truncate">{name}</div>
                      <div className="text-[10px] text-slate-400 font-medium">{service}</div>
                    </div>
                    <span className="text-[10px] font-bold text-primary bg-primary/10 px-1.5 py-0.5 rounded">{time}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Card: Đánh giá */}
            <div className="absolute bottom-10 right-4 bg-white/95 backdrop-blur-sm rounded-2xl p-5 w-56 shadow-xl border border-white/60 z-20">
              <div className="flex items-center gap-1 mb-2">
                {[1,2,3,4,5].map(i => (
                  <svg key={i} className="w-4 h-4 text-amber-400 fill-current" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                ))}
              </div>
              <p className="text-[12px] text-slate-600 font-semibold leading-relaxed">
                &ldquo;Dịch vụ tuyệt vời, bác sĩ tận tâm. Tôi rất hài lòng!&rdquo;
              </p>
              <div className="mt-3 flex items-center gap-2">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src="https://randomuser.me/api/portraits/women/33.jpg" alt="Nguyễn Thuỳ Linh" className="w-6 h-6 rounded-full object-cover" />
                <span className="text-[11px] font-bold text-slate-500">Nguyễn Thuỳ Linh</span>
              </div>
            </div>

            {/* Card: Bác sĩ */}
            <div className="absolute top-1/2 -translate-y-1/2 right-6 bg-white/95 backdrop-blur-sm rounded-2xl p-4 w-48 shadow-xl border border-white/60 z-10">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src="https://randomuser.me/api/portraits/men/36.jpg"
                alt="BS. Nguyễn Minh Đức"
                className="w-12 h-12 rounded-xl object-cover mx-auto mb-3"
              />
              <div className="text-center">
                <div className="text-[12px] font-black text-slate-800">ThS. BS. Nguyễn<br />Minh Đức</div>
                <div className="text-[10px] text-primary font-bold mt-1 bg-primary/5 rounded-lg px-2 py-0.5">Implant & Phục hình</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ── ABOUT ───────────────────────────────────────────────────────────── */}
      <section id="about" className="py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">

            {/* Left — image collage */}
            <div className="relative h-[500px]">
              {/* Main photo */}
              <div className="absolute top-0 left-0 w-[68%] h-[72%] rounded-3xl overflow-hidden shadow-xl">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src="https://picsum.photos/seed/aboutclinic1/600/440"
                  alt="Phòng khám Sơn Giang Dental"
                  className="w-full h-full object-cover"
                />
              </div>
              {/* Secondary photo */}
              <div className="absolute bottom-0 right-0 w-[55%] h-[55%] rounded-3xl overflow-hidden shadow-xl border-4 border-white">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src="https://picsum.photos/seed/aboutclinic2/400/320"
                  alt="Đội ngũ Sơn Giang Dental"
                  className="w-full h-full object-cover"
                />
              </div>
              {/* Badge: thành lập */}
              <div className="absolute top-[62%] left-[60%] -translate-x-1/2 bg-primary rounded-2xl px-5 py-4 shadow-xl text-center z-10">
                <div className="text-3xl font-black text-white leading-none">2009</div>
                <div className="text-[11px] font-bold text-white/80 mt-1 whitespace-nowrap">Năm thành lập</div>
              </div>
            </div>

            {/* Right — content */}
            <div>
              <span className="text-[12px] font-black tracking-widest text-primary uppercase">Về Chúng Tôi</span>
              <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-5">
                Hơn 15 Năm Kiến Tạo<br />Nụ Cười Việt Nam
              </h2>
              <p className="text-slate-500 text-[15px] leading-relaxed mb-5">
                Sơn Giang Dental được thành lập năm 2009 với sứ mệnh mang lại dịch vụ chăm sóc răng miệng chất lượng cao, tiệm cận chuẩn quốc tế, với mức chi phí phù hợp nhất cho người Việt.
              </p>
              <p className="text-slate-500 text-[15px] leading-relaxed mb-8">
                Trải qua hơn 15 năm phát triển, chúng tôi đã xây dựng được đội ngũ hơn 20 bác sĩ chuyên khoa, trang bị công nghệ điều trị hàng đầu và phục vụ hơn 10.000 khách hàng hài lòng trên khắp cả nước.
              </p>

              {/* Milestones */}
              <div className="grid grid-cols-2 gap-4 mb-8">
                {[
                  { year: "2009", desc: "Thành lập phòng khám đầu tiên tại TP.HCM" },
                  { year: "2015", desc: "Mở rộng lên 3 cơ sở, đạt chứng nhận ISO 9001" },
                  { year: "2019", desc: "Đối tác chính thức của Invisalign tại Việt Nam" },
                  { year: "2023", desc: "Ra mắt ứng dụng đặt lịch trên di động" },
                ].map(({ year, desc }) => (
                  <div key={year} className="flex gap-3">
                    <div className="w-12 h-12 rounded-xl bg-primary/10 text-primary flex items-center justify-center font-black text-[12px] shrink-0">
                      {year}
                    </div>
                    <p className="text-[13px] text-slate-500 leading-snug pt-1">{desc}</p>
                  </div>
                ))}
              </div>

              {/* Certifications */}
              <div className="flex flex-wrap gap-2">
                {["ISO 9001:2015", "Invisalign Provider", "Bộ Y tế cấp phép", "ADA Member"].map((cert) => (
                  <span key={cert} className="text-[12px] font-bold text-slate-600 border border-slate-200 bg-white rounded-xl px-3 py-1.5 flex items-center gap-1.5">
                    <svg className="w-3.5 h-3.5 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" />
                    </svg>
                    {cert}
                  </span>
                ))}
              </div>
            </div>

          </div>
        </div>
      </section>

      {/* ── SERVICES ────────────────────────────────────────────────────────── */}
      <section id="services" className="py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center max-w-2xl mx-auto mb-16">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Dịch Vụ</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
              Dịch Vụ Nổi Bật
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Đa dạng dịch vụ điều trị và thẩm mỹ răng miệng cao cấp giúp bạn tự tin với nụ cười toả sáng.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[
              {
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.562.562 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
                  </svg>
                ),
                color: "primary",
                title: "Niềng Răng Thẩm Mỹ",
                desc: "Chỉnh nha hiệu quả với công nghệ khay trong suốt Invisalign, giúp răng đều đẹp và chuẩn khớp cắn.",
              },
              {
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1 1 .03 2.798-1.414 2.798H4.213c-1.444 0-2.414-1.798-1.414-2.798L4.2 15.3" />
                  </svg>
                ),
                color: "secondary",
                title: "Cấy Ghép Implant",
                desc: "Phục hình răng mất hoàn hảo từ chân đến mặt nhai, đảm bảo khả năng ăn nhai vững chắc và tính thẩm mỹ tuyệt đối.",
              },
              {
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15.182 15.182a4.5 4.5 0 01-6.364 0M21 12a9 9 0 11-18 0 9 9 0 0118 0zM9.75 9.75c0 .414-.168.75-.375.75S9 10.164 9 9.75 9.168 9 9.375 9s.375.336.375.75zm-.375 0h.008v.015h-.008V9.75zm5.625 0c0 .414-.168.75-.375.75s-.375-.336-.375-.75.168-.75.375-.75.375.336.375.75zm-.375 0h.008v.015h-.008V9.75z" />
                  </svg>
                ),
                color: "primary",
                title: "Bọc Răng Sứ Thẩm Mỹ",
                desc: "Khắc phục răng xỉn màu, sứt mẻ với dòng sứ cao cấp chính hãng Đức và Mỹ, mang lại nụ cười rạng rỡ.",
              },
              {
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" />
                  </svg>
                ),
                color: "secondary",
                title: "Tẩy Trắng Răng",
                desc: "Công nghệ tẩy trắng Zoom Whitening tiên tiến giúp răng sáng hơn 8 tông màu chỉ trong 1 giờ, an toàn và không ê buốt.",
              },
              {
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.877-5.877M11.42 15.17l2.496-3.03c.317-.384.74-.626 1.208-.766M11.42 15.17l-4.655 5.653a2.548 2.548 0 11-3.586-3.586l6.837-5.63m5.108-.233c.55-.164 1.163-.188 1.743-.14a4.5 4.5 0 004.486-6.336l-3.276 3.277a3.004 3.004 0 01-2.25-2.25l3.276-3.276a4.5 4.5 0 00-6.336 4.486c.091 1.076-.071 2.264-.904 2.95l-.102.085m-1.745 1.437L5.909 7.5H4.5L2.25 3.75l1.5-1.5L7.5 4.5v1.409l4.26 4.26m-1.745 1.437l1.745-1.437m6.615 8.206L15.75 15.75M4.867 19.125h.008v.008h-.008v-.008z" />
                  </svg>
                ),
                color: "primary",
                title: "Điều Trị Tủy Răng",
                desc: "Xử lý triệt để bệnh lý tủy, bảo tồn răng gốc tối đa với hệ thống máy Rotary hiện đại, không đau và nhanh chóng.",
              },
              {
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                  </svg>
                ),
                color: "secondary",
                title: "Nhổ Răng Khôn",
                desc: "Phẫu thuật răng khôn mọc lệch bằng kỹ thuật ít xâm lấn, gây tê tại chỗ, hạn chế đau và phục hồi nhanh sau 1–2 ngày.",
              },
            ].map(({ icon, color, title, desc }) => (
              <div key={title} className="glass-card hover-lift bg-white rounded-2xl p-7 border border-slate-200/60 flex flex-col shadow-sm hover:shadow-lg">
                <div className={`w-12 h-12 rounded-xl flex items-center justify-center mb-5 ${color === "primary" ? "bg-primary/10 text-primary" : "bg-secondary/10 text-secondary"}`}>
                  {icon}
                </div>
                <h3 className="text-[17px] font-bold text-slate-900 mb-2">{title}</h3>
                <p className="text-slate-500 text-[13px] leading-relaxed flex-1">{desc}</p>
                <a href="#services" className={`mt-5 inline-flex items-center gap-1.5 text-[13px] font-bold transition-all hover:gap-2.5 ${color === "primary" ? "text-primary" : "text-secondary"}`}>
                  Tìm hiểu thêm
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                  </svg>
                </a>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── WHY CHOOSE US ───────────────────────────────────────────────────── */}
      <section className="py-24 bg-white">
        <div className="max-w-7xl mx-auto px-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
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
                {[
                  {
                    title: "Công nghệ hiện đại hàng đầu",
                    desc: "Trang bị máy CT Cone Beam 3D, laser nha khoa, kính lúp phẫu thuật và hệ thống CAD/CAM làm sứ ngay tại phòng khám.",
                  },
                  {
                    title: "Đội ngũ bác sĩ chuyên sâu",
                    desc: "100% bác sĩ có chứng chỉ chuyên khoa, tu nghiệp tại Pháp, Mỹ, Nhật — liên tục cập nhật kỹ thuật mới nhất.",
                  },
                  {
                    title: "Cam kết minh bạch giá cả",
                    desc: "Báo giá rõ ràng trước điều trị, không phát sinh, bảo hành lên đến 10 năm cho các ca phục hình cao cấp.",
                  },
                  {
                    title: "Môi trường vô trùng tuyệt đối",
                    desc: "Quy trình tiệt khuẩn đạt chuẩn CDC/ADA, mỗi bệnh nhân dùng bộ dụng cụ riêng đóng gói sealed.",
                  },
                ].map(({ title, desc }, i) => (
                  <div key={title} className="flex gap-4">
                    <div className="w-8 h-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0 font-black text-[13px] mt-0.5">
                      {String(i + 1).padStart(2, "0")}
                    </div>
                    <div>
                      <div className="text-[15px] font-bold text-slate-900 mb-1">{title}</div>
                      <div className="text-[13px] text-slate-500 leading-relaxed">{desc}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Right — stat cards */}
            <div className="grid grid-cols-2 gap-5">
              {[
                {
                  value: "10.000+", label: "Khách hàng hài lòng", color: "primary",
                  icon: <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" /></svg>,
                },
                {
                  value: "20+", label: "Bác sĩ chuyên khoa", color: "secondary",
                  icon: <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" /></svg>,
                },
                {
                  value: "15+", label: "Năm kinh nghiệm", color: "primary",
                  icon: <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 18.75h-9m9 0a3 3 0 013 3h-15a3 3 0 013-3m9 0v-3.375c0-.621-.503-1.125-1.125-1.125h-.871M7.5 18.75v-3.375c0-.621.504-1.125 1.125-1.125h.872m5.007 0H9.497m5.007 0a7.454 7.454 0 01-.982-3.172M9.497 14.25a7.454 7.454 0 00.981-3.172M5.25 4.236c-.982.143-1.954.317-2.916.52A6.003 6.003 0 007.73 9.728M5.25 4.236V4.5c0 2.108.966 3.99 2.48 5.228M5.25 4.236V2.721C7.456 2.41 9.71 2.25 12 2.25c2.291 0 4.545.16 6.75.47v1.516M7.73 9.728a6.726 6.726 0 002.748 1.35m8.272-6.842V4.5c0 2.108-.966 3.99-2.48 5.228m2.48-5.492a46.32 46.32 0 012.916.52 6.003 6.003 0 01-5.395 4.972m0 0a6.726 6.726 0 01-2.749 1.35m0 0a6.772 6.772 0 01-3.044 0" /></svg>,
                },
                {
                  value: "99%", label: "Đánh giá 5 sao", color: "secondary",
                  icon: <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.562.562 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" /></svg>,
                },
              ].map(({ value, label, color, icon }) => (
                <div key={label} className={`rounded-2xl p-7 flex flex-col items-start border ${color === "primary" ? "bg-primary/5 border-primary/10" : "bg-secondary/5 border-secondary/10"}`}>
                  <div className={`w-10 h-10 rounded-xl flex items-center justify-center mb-4 ${color === "primary" ? "bg-primary/15 text-primary" : "bg-secondary/15 text-secondary"}`}>
                    {icon}
                  </div>
                  <span className={`text-4xl font-black leading-none mb-2 ${color === "primary" ? "text-primary" : "text-secondary"}`}>{value}</span>
                  <span className="text-[13px] font-semibold text-slate-500">{label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* ── PROCESS ─────────────────────────────────────────────────────────── */}
      <section id="process" className="py-24 bg-slate-900">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center max-w-2xl mx-auto mb-16">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Quy Trình</span>
            <h2 className="text-3xl md:text-4xl font-black text-white mt-2 mb-4">
              Quy Trình Khám Đơn Giản
            </h2>
            <p className="text-slate-400 text-base leading-relaxed">
              Chỉ 4 bước đơn giản để sở hữu nụ cười hoàn hảo mà bạn mong ước.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            {[
              {
                step: "01",
                title: "Tải App & Đặt Lịch",
                desc: "Tải app Sơn Giang Dental, chọn dịch vụ và đặt lịch trong 30 giây. Xác nhận ngay lập tức.",
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                  </svg>
                ),
              },
              {
                step: "02",
                title: "Khám & Tư Vấn",
                desc: "Bác sĩ chuyên khoa thăm khám toàn diện, chụp X-quang và tư vấn phác đồ phù hợp.",
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 15.75l-2.489-2.489m0 0a3.375 3.375 0 10-4.773-4.773 3.375 3.375 0 004.774 4.774zM21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                ),
              },
              {
                step: "03",
                title: "Điều Trị",
                desc: "Thực hiện điều trị theo phác đồ đã tư vấn với công nghệ hiện đại, không đau, an toàn.",
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1 1 .03 2.798-1.414 2.798H4.213c-1.444 0-2.414-1.798-1.414-2.798L4.2 15.3" />
                  </svg>
                ),
              },
              {
                step: "04",
                title: "Theo Dõi Sau Điều Trị",
                desc: "Tái khám định kỳ miễn phí, bảo hành dài hạn và hỗ trợ 24/7 khi có vấn đề phát sinh.",
                icon: (
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" />
                  </svg>
                ),
              },
            ].map(({ step, title, desc, icon }) => (
              <div key={step} className="relative flex flex-col items-start p-6 rounded-2xl border border-white/10 bg-white/5 hover:bg-white/10 transition-all">
                <div className="flex items-center justify-between w-full mb-5">
                  <div className="w-11 h-11 rounded-xl bg-primary/20 text-primary flex items-center justify-center">
                    {icon}
                  </div>
                  <span className="text-4xl font-black text-white/10 select-none">{step}</span>
                </div>
                <h3 className="text-[16px] font-bold text-white mb-2">{title}</h3>
                <p className="text-slate-400 text-[13px] leading-relaxed">{desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── DENTISTS ─────────────────────────────────────────────────────────── */}
      <section id="dentists" className="py-24 bg-white">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center max-w-2xl mx-auto mb-16">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Đội Ngũ</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
              Bác Sĩ Chuyên Gia
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Đội ngũ bác sĩ tâm huyết, giàu kinh nghiệm lâm sàng và liên tục tu nghiệp quốc tế.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {[
              {
                photo: "https://randomuser.me/api/portraits/men/32.jpg",
                badge: "Giám Đốc Chuyên Môn",
                badgeColor: "bg-primary/10 text-primary",
                name: "ThS. BS. Nguyễn Minh Đức",
                desc: "Hơn 15 năm kinh nghiệm về Cấy ghép Implant và Phục hình sứ thẩm mỹ.",
                edu: "Tốt nghiệp Đại học Y Dược TP.HCM",
                specs: ["Implant", "Phục hình sứ"],
              },
              {
                photo: "https://randomuser.me/api/portraits/women/44.jpg",
                badge: "Chuyên Gia Chỉnh Nha",
                badgeColor: "bg-secondary/10 text-secondary",
                name: "BS. Lê Thị Phương Thảo",
                desc: "Chứng nhận Invisalign Hoa Kỳ, thực hiện thành công hơn 1.000 ca niềng răng.",
                edu: "Tu nghiệp Chỉnh nha chuyên sâu tại Pháp",
                specs: ["Niềng răng", "Invisalign"],
              },
              {
                photo: "https://randomuser.me/api/portraits/men/55.jpg",
                badge: "Chuyên Gia Nội Nha",
                badgeColor: "bg-primary/10 text-primary",
                name: "BSCKI. Trần Quốc Bảo",
                desc: "Khám và xử lý triệt để bệnh lý tủy răng, bảo tồn răng gốc tối đa.",
                edu: "Chuyên khoa I ĐH Y Hà Nội",
                specs: ["Điều trị tủy", "Phẫu thuật"],
              },
            ].map(({ photo, badge, badgeColor, name, desc, edu, specs }) => (
              <div key={name} className="glass-card hover-lift rounded-2xl border border-slate-200/60 overflow-hidden shadow-sm flex flex-col bg-white">
                <div className="h-64 overflow-hidden">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={photo} alt={name} className="w-full h-full object-cover object-top" />
                </div>
                <div className="p-6 flex flex-col flex-1">
                  <span className={`inline-flex items-center self-start px-3 py-1 rounded-full text-[11px] font-bold mb-3 ${badgeColor}`}>
                    {badge}
                  </span>
                  <h3 className="text-[17px] font-bold text-slate-900 mb-2">{name}</h3>
                  <p className="text-slate-500 text-[13px] leading-relaxed mb-4 flex-1">{desc}</p>
                  <div className="flex flex-wrap gap-2 mb-4">
                    {specs.map(s => (
                      <span key={s} className="text-[11px] font-bold text-slate-500 bg-slate-100 px-2.5 py-1 rounded-lg">{s}</span>
                    ))}
                  </div>
                  <div className="text-[12px] text-primary font-bold border-t border-slate-100 pt-3 flex items-center gap-1.5">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M4.26 10.147a60.436 60.436 0 00-.491 6.347A48.627 48.627 0 0112 20.904a48.627 48.627 0 018.232-4.41 60.46 60.46 0 00-.491-6.347m-15.482 0a50.57 50.57 0 00-2.658-.813A59.905 59.905 0 0112 3.493a59.902 59.902 0 0110.399 5.84c-.896.248-1.783.52-2.658.814m-15.482 0A50.697 50.697 0 0112 13.489a50.702 50.702 0 017.74-3.342M6.75 15a.75.75 0 100-1.5.75.75 0 000 1.5zm0 0v-3.675A55.378 55.378 0 0112 8.443m-7.007 11.55A5.981 5.981 0 006.75 15.75v-1.5" />
                    </svg>
                    {edu}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── TESTIMONIALS ────────────────────────────────────────────────────── */}
      <section className="py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center max-w-2xl mx-auto mb-16">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Đánh Giá</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
              Khách Hàng Nói Gì
            </h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {[
              {
                name: "Nguyễn Thuỳ Linh",
                role: "Kế toán, 28 tuổi",
                text: "Mình niềng răng Invisalign ở đây được 18 tháng, kết quả vượt ngoài mong đợi. Bác sĩ Thảo rất tận tâm và chuyên nghiệp, giải thích từng bước rõ ràng.",
                service: "Niềng răng Invisalign",
                photo: "https://randomuser.me/api/portraits/women/68.jpg",
                color: "primary",
              },
              {
                name: "Trần Minh Khôi",
                role: "Kỹ sư, 35 tuổi",
                text: "Cấy implant ở đây cực kỳ ổn. Quy trình không đau như mình lo, bác sĩ Đức giải thích kỹ lưỡng và theo dõi sát sau ca phẫu thuật. Sẽ giới thiệu cho gia đình.",
                service: "Cấy ghép Implant",
                photo: "https://randomuser.me/api/portraits/men/75.jpg",
                color: "secondary",
              },
              {
                name: "Lê Phương Mai",
                role: "Giáo viên, 42 tuổi",
                text: "Tẩy trắng Zoom trong 1 buổi mà hiệu quả thấy rõ, trắng sáng tự nhiên, không ê buốt như lo sợ. Phòng khám sạch sẽ, nhân viên thân thiện và chu đáo.",
                service: "Tẩy trắng Zoom",
                photo: "https://randomuser.me/api/portraits/women/52.jpg",
                color: "primary",
              },
            ].map(({ name, role, text, service, photo, color }) => (
              <div key={name} className="glass-card bg-white rounded-2xl p-7 border border-slate-200/60 flex flex-col shadow-sm">
                <div className="flex items-center gap-1 mb-4">
                  {[1,2,3,4,5].map(i => (
                    <svg key={i} className="w-4 h-4 text-amber-400 fill-current" viewBox="0 0 20 20">
                      <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                    </svg>
                  ))}
                </div>
                <p className="text-slate-600 text-[14px] leading-relaxed flex-1 mb-5">&ldquo;{text}&rdquo;</p>
                <div className={`text-[11px] font-bold px-2.5 py-1 rounded-lg self-start mb-4 ${color === "primary" ? "bg-primary/10 text-primary" : "bg-secondary/10 text-secondary"}`}>
                  {service}
                </div>
                <div className="flex items-center gap-3 border-t border-slate-100 pt-4">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={photo} alt={name} className="w-10 h-10 rounded-full object-cover shrink-0" />
                  <div>
                    <div className="text-[14px] font-bold text-slate-900">{name}</div>
                    <div className="text-[12px] text-slate-400 font-medium">{role}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── NEWS & EVENTS ───────────────────────────────────────────────────── */}
      <section id="news" className="py-24 bg-white">
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-14">
            <div>
              <span className="text-[12px] font-black tracking-widest text-primary uppercase">Tin Tức & Sự Kiện</span>
              <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2">
                Cập Nhật Mới Nhất
              </h2>
            </div>
            <a href="#" className="inline-flex items-center gap-1.5 text-[13px] font-bold text-primary hover:gap-3 transition-all shrink-0">
              Xem tất cả
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </a>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {[
              {
                img: "https://picsum.photos/seed/news1dental/600/360",
                cat: "Sự kiện",
                catColor: "bg-secondary/10 text-secondary",
                date: "10/06/2026",
                title: "Sơn Giang Dental khai trương cơ sở mới tại Quận 7",
                excerpt: "Chúng tôi vui mừng thông báo cơ sở thứ 4 chính thức đi vào hoạt động, trang bị đầy đủ thiết bị nha khoa hiện đại nhất khu vực phía Nam.",
              },
              {
                img: "https://picsum.photos/seed/news2dental/600/360",
                cat: "Kiến thức",
                catColor: "bg-primary/10 text-primary",
                date: "02/06/2026",
                title: "5 dấu hiệu cần đến gặp nha sĩ ngay lập tức",
                excerpt: "Nhiều người có thói quen trì hoãn việc đi khám răng. Bài viết này giúp bạn nhận biết những triệu chứng không nên bỏ qua để bảo vệ sức khoẻ răng miệng.",
              },
              {
                img: "https://picsum.photos/seed/news3dental/600/360",
                cat: "Công nghệ",
                catColor: "bg-amber-500/10 text-amber-600",
                date: "25/05/2026",
                title: "Implant tức thì: Giải pháp phục hình răng trong 1 ngày",
                excerpt: "Công nghệ cấy ghép Implant tức thì (Same-Day Implant) giúp bệnh nhân có thể phục hình răng hoàn chỉnh chỉ trong một buổi điều trị duy nhất.",
              },
            ].map(({ img, cat, catColor, date, title, excerpt }) => (
              <article key={title} className="glass-card hover-lift bg-white rounded-2xl border border-slate-200/60 overflow-hidden shadow-sm flex flex-col group">
                {/* Cover image */}
                <div className="h-52 overflow-hidden">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={img}
                    alt={title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                </div>

                <div className="p-6 flex flex-col flex-1">
                  <div className="flex items-center gap-3 mb-3">
                    <span className={`text-[11px] font-black px-2.5 py-1 rounded-full ${catColor}`}>{cat}</span>
                    <span className="text-[11px] text-slate-400 font-medium">{date}</span>
                  </div>

                  <h3 className="text-[16px] font-bold text-slate-900 leading-snug mb-3 group-hover:text-primary transition-colors">{title}</h3>
                  <p className="text-[13px] text-slate-500 leading-relaxed flex-1 mb-5">{excerpt}</p>

                  <div className="flex items-center gap-1.5 text-[13px] font-bold text-primary group-hover:gap-3 transition-all">
                    Đọc tiếp
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                    </svg>
                  </div>
                </div>
              </article>
            ))}
          </div>

          {/* Events banner */}
          <div className="mt-12 rounded-3xl bg-slate-900 overflow-hidden relative">
            <div className="absolute inset-0 pointer-events-none">
              <div className="absolute top-0 right-1/4 w-64 h-64 bg-primary/20 rounded-full blur-3xl" />
              <div className="absolute bottom-0 left-1/4 w-48 h-48 bg-secondary/20 rounded-full blur-3xl" />
            </div>
            <div className="relative flex flex-col md:flex-row items-center gap-8 p-8 md:p-10">
              <div className="flex-1">
                <span className="inline-flex items-center gap-2 text-[11px] font-black tracking-widest text-primary uppercase bg-primary/15 rounded-full px-3 py-1.5 mb-4">
                  <span className="w-1.5 h-1.5 bg-primary rounded-full animate-pulse" />{"Sự kiện sắp tới"}
                </span>
                <h3 className="text-2xl md:text-3xl font-black text-white mb-2">
                  Ngày Hội Chăm Sóc Răng Miệng 2026
                </h3>
                <p className="text-slate-400 text-[14px] leading-relaxed max-w-lg">
                  Khám răng miễn phí, tư vấn chỉnh nha, triển lãm công nghệ nha khoa tiên tiến và nhiều ưu đãi đặc biệt dành cho 500 khách đầu tiên đăng ký.
                </p>
              </div>
              <div className="flex flex-col items-center sm:items-end gap-3 shrink-0">
                <div className="flex items-center gap-4">
                  <div className="text-center bg-white/10 rounded-2xl px-5 py-3">
                    <div className="text-3xl font-black text-white leading-none">20</div>
                    <div className="text-[10px] font-bold text-slate-400 mt-1">THÁNG 7</div>
                  </div>
                  <div className="text-center bg-white/10 rounded-2xl px-5 py-3">
                    <div className="text-3xl font-black text-white leading-none">2026</div>
                    <div className="text-[10px] font-bold text-slate-400 mt-1">NĂM</div>
                  </div>
                </div>
                <a
                  href="#download"
                  className="flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-6 py-3 rounded-xl font-bold text-[14px] transition-all hover:translate-y-[-1px] hover:shadow-lg hover:shadow-primary/25"
                >
                  Đăng ký qua App
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                  </svg>
                </a>
              </div>
            </div>
          </div>

        </div>
      </section>

      {/* ── APP DOWNLOAD ────────────────────────────────────────────────────── */}
      <section id="download" className="py-24 bg-primary relative overflow-hidden">
        <div className="absolute inset-0 pointer-events-none">
          <div className="absolute top-0 left-1/4 w-96 h-96 bg-white/5 rounded-full blur-3xl" />
          <div className="absolute bottom-0 right-1/4 w-80 h-80 bg-white/5 rounded-full blur-3xl" />
          <div className="absolute top-1/2 right-0 w-64 h-64 bg-white/5 rounded-full blur-2xl" />
        </div>

        <div className="relative max-w-7xl mx-auto px-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">

            {/* Left — text */}
            <div>
              <span className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-[11px] font-black bg-white/15 text-white border border-white/20 mb-6 uppercase tracking-wider">
                <span className="w-1.5 h-1.5 rounded-full bg-white animate-pulse" />{"Ứng Dụng Sơn Giang Dental"}
              </span>

              <h2 className="text-3xl md:text-4xl lg:text-5xl font-black text-white leading-[1.1] mb-5">
                Đặt Lịch Nha Khoa<br />
                <span className="text-white/80">Chưa Bao Giờ Dễ Hơn</span>
              </h2>

              <p className="text-white/75 text-[15px] leading-relaxed mb-8 max-w-lg">
                Tải app Sơn Giang Dental để đặt lịch, theo dõi lịch sử điều trị, nhận nhắc nhở tái khám và chat trực tiếp với bác sĩ — mọi lúc, mọi nơi.
              </p>

              {/* Feature list */}
              <div className="flex flex-col gap-3 mb-10">
                {[
                  "Đặt lịch trong 30 giây, xác nhận tức thì",
                  "Theo dõi tiến trình điều trị theo thời gian thực",
                  "Nhắc nhở tái khám tự động",
                  "Chat với bác sĩ, xem hồ sơ sức khoẻ răng miệng",
                ].map((feat) => (
                  <div key={feat} className="flex items-center gap-3">
                    <div className="w-5 h-5 rounded-full bg-white/20 flex items-center justify-center shrink-0">
                      <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                      </svg>
                    </div>
                    <span className="text-white/85 text-[14px] font-medium">{feat}</span>
                  </div>
                ))}
              </div>

              {/* Download buttons */}
              <div className="flex flex-col sm:flex-row gap-3">
                {/* App Store */}
                <a
                  href="#"
                  className="flex items-center gap-3 bg-white hover:bg-slate-50 text-slate-900 px-6 py-3.5 rounded-xl font-bold transition-all hover:translate-y-[-2px] hover:shadow-xl hover:shadow-black/20 group"
                >
                  <svg className="w-7 h-7 shrink-0" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.8-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z"/>
                  </svg>
                  <div className="flex flex-col leading-none text-left">
                    <span className="text-[10px] font-semibold text-slate-500 group-hover:text-slate-600">Tải trên</span>
                    <span className="text-[15px] font-black">App Store</span>
                  </div>
                </a>

                {/* Google Play */}
                <a
                  href="#"
                  className="flex items-center gap-3 bg-white hover:bg-slate-50 text-slate-900 px-6 py-3.5 rounded-xl font-bold transition-all hover:translate-y-[-2px] hover:shadow-xl hover:shadow-black/20 group"
                >
                  <svg className="w-7 h-7 shrink-0" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M3.18 23.76a2 2 0 01-.96-1.76V2a2 2 0 01.96-1.76l.1-.06 12.02 12.02-.1.1L3.18 23.76zm14.57-8.4l-2.3-2.3 2.3-2.3 2.59 1.47a1.4 1.4 0 010 2.42l-2.59 1.71zM4.03 23.17L15.2 12 4.03.83 3.18.25l-.1.06A2 2 0 002 2v20a2 2 0 001.08 1.76l.95.58.1-.06-.1-.07zM14.36 11.17L4.03.83l10.33 10.34-1.06 1.06 1.06-1.06z"/>
                  </svg>
                  <div className="flex flex-col leading-none text-left">
                    <span className="text-[10px] font-semibold text-slate-500 group-hover:text-slate-600">Tải trên</span>
                    <span className="text-[15px] font-black">Google Play</span>
                  </div>
                </a>
              </div>
            </div>

            {/* Right — phone mockup */}
            <div className="flex justify-center lg:justify-end">
              <div className="relative">
                {/* Phone frame */}
                <div className="w-64 h-[520px] bg-slate-900 rounded-[48px] border-4 border-white/20 shadow-2xl shadow-black/40 overflow-hidden relative">
                  {/* Status bar */}
                  <div className="h-10 bg-slate-900 flex items-center justify-between px-6 pt-2">
                    <span className="text-white text-[11px] font-bold">9:41</span>
                    <div className="flex items-center gap-1">
                      <svg className="w-3.5 h-3.5 text-white" fill="currentColor" viewBox="0 0 20 20">
                        <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zm6-4a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zm6-3a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z" />
                      </svg>
                      <svg className="w-3.5 h-3.5 text-white" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M17.778 8.222c-4.296-4.296-11.26-4.296-15.556 0A1 1 0 00.808 9.636c3.71-3.71 9.727-3.71 13.436 0l1.07 1.07a1 1 0 001.414-1.414l-1.07-1.07zM7 13a1 1 0 011-1 1 1 0 010 2 1 1 0 01-1-1zm3-2a3 3 0 016 0 1 1 0 01-2 0 1 1 0 10-2 0 1 1 0 01-2 0zm-3-2a5 5 0 0110 0 1 1 0 01-2 0 3 3 0 00-6 0 1 1 0 01-2 0z" clipRule="evenodd" />
                      </svg>
                    </div>
                  </div>

                  {/* App screen */}
                  <div className="flex-1 bg-white h-full">
                    {/* App header */}
                    <div className="bg-primary px-5 py-4">
                      <div className="text-[11px] font-black text-white/70 uppercase tracking-wider mb-1">Xin chào, Minh!</div>
                      <div className="text-[16px] font-black text-white">Lịch khám sắp tới</div>
                    </div>

                    {/* Appointment card */}
                    <div className="mx-4 -mt-3 bg-white rounded-2xl shadow-lg p-4 border border-slate-100 mb-4">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
                          <svg className="w-5 h-5 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                          </svg>
                        </div>
                        <div>
                          <div className="text-[12px] font-black text-slate-900">Cấy Implant</div>
                          <div className="text-[10px] text-slate-400 font-medium">Thứ Sáu, 14/06 • 09:30</div>
                        </div>
                        <span className="ml-auto text-[10px] font-black text-green-600 bg-green-50 px-2 py-0.5 rounded-full">Đã xác nhận</span>
                      </div>
                    </div>

                    {/* Service grid */}
                    <div className="px-4">
                      <div className="text-[12px] font-black text-slate-700 mb-3">Dịch vụ phổ biến</div>
                      <div className="grid grid-cols-3 gap-2">
                        {[
                          { icon: "🦷", label: "Implant" },
                          { icon: "😁", label: "Niềng" },
                          { icon: "✨", label: "Tẩy trắng" },
                          { icon: "🔬", label: "Tủy" },
                          { icon: "🛡️", label: "Bọc sứ" },
                          { icon: "📋", label: "Kiểm tra" },
                        ].map(({ icon, label }) => (
                          <div key={label} className="bg-slate-50 rounded-xl p-2.5 flex flex-col items-center gap-1">
                            <span className="text-lg">{icon}</span>
                            <span className="text-[9px] font-bold text-slate-600">{label}</span>
                          </div>
                        ))}
                      </div>
                    </div>

                    {/* Bottom nav */}
                    <div className="absolute bottom-0 left-0 right-0 bg-white border-t border-slate-100 flex justify-around py-3 px-2">
                      {[
                        { icon: "🏠", label: "Trang chủ", active: true },
                        { icon: "📅", label: "Lịch hẹn" },
                        { icon: "💬", label: "Tư vấn" },
                        { icon: "👤", label: "Hồ sơ" },
                      ].map(({ icon, label, active }) => (
                        <div key={label} className={`flex flex-col items-center gap-0.5 ${active ? "text-primary" : "text-slate-400"}`}>
                          <span className="text-base">{icon}</span>
                          <span className={`text-[8px] font-bold ${active ? "text-primary" : "text-slate-400"}`}>{label}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Floating badge: rating */}
                <div className="absolute -top-4 -right-4 bg-white rounded-2xl px-4 py-2.5 shadow-xl border border-slate-100 flex items-center gap-2">
                  <div className="flex gap-0.5">
                    {[1,2,3,4,5].map(i => (
                      <svg key={i} className="w-3.5 h-3.5 text-amber-400 fill-current" viewBox="0 0 20 20">
                        <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                      </svg>
                    ))}
                  </div>
                  <div>
                    <div className="text-[13px] font-black text-slate-900">4.9 / 5</div>
                    <div className="text-[9px] text-slate-400 font-medium">10k+ đánh giá</div>
                  </div>
                </div>

                {/* Floating badge: downloads */}
                <div className="absolute -bottom-2 -left-6 bg-white rounded-2xl px-4 py-2.5 shadow-xl border border-slate-100">
                  <div className="text-[13px] font-black text-primary">50.000+</div>
                  <div className="text-[10px] text-slate-400 font-medium">Lượt tải xuống</div>
                </div>
              </div>
            </div>

          </div>
        </div>
      </section>

      {/* ── FOOTER ──────────────────────────────────────────────────────────── */}
      <footer id="contact" className="bg-slate-900 text-slate-400 pt-16 pb-8">
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
                  <a key={s} href="#services" className="hover:text-white transition-colors">{s}</a>
                ))}
              </div>
            </div>

            {/* Liên kết */}
            <div>
              <h4 className="text-white font-bold mb-5 text-[14px]">Thông Tin</h4>
              <div className="flex flex-col gap-3 text-[13px]">
                {[
                  { label: "Về chúng tôi", href: "#about" },
                  { label: "Đội ngũ bác sĩ", href: "#dentists" },
                  { label: "Bảng giá dịch vụ", href: "#services" },
                  { label: "Tin tức & sự kiện", href: "#news" },
                  { label: "Câu hỏi thường gặp", href: "#" },
                ].map(({ label, href }) =>
                  href === "#" ? (
                    <span key={label} className="cursor-default text-slate-500">{label}</span>
                  ) : (
                    <a key={label} href={href} className="hover:text-white transition-colors">{label}</a>
                  )
                )}
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

    </div>
  );
}
