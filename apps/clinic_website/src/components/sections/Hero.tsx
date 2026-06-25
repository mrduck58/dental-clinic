import Link from "next/link";

export default function Hero() {
  return (
    <section className="relative overflow-hidden bg-white pt-14 pb-16">
      {/* Background blobs */}
      <div className="absolute top-0 right-0 w-[600px] h-[600px] bg-primary/5 rounded-full blur-3xl -translate-y-1/2 translate-x-1/3 pointer-events-none" />
      <div className="absolute bottom-0 left-0 w-[400px] h-[400px] bg-secondary/5 rounded-full blur-3xl translate-y-1/2 -translate-x-1/3 pointer-events-none" />

      <div className="relative max-w-7xl mx-auto px-6 grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
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
            <Link
              href="/tai-ung-dung"
              className="flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white px-8 py-4 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:shadow-xl hover:shadow-primary/25"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 1.5H8.25A2.25 2.25 0 006 3.75v16.5a2.25 2.25 0 002.25 2.25h7.5A2.25 2.25 0 0018 20.25V3.75a2.25 2.25 0 00-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 8.25h3m-3 4.5h3m-6-4.5H6m0 4.5H4.5" />
              </svg>
              Tải app đặt lịch ngay
            </Link>
            <Link
              href="/dich-vu"
              className="flex items-center justify-center gap-2 bg-white hover:bg-slate-50 text-slate-700 border border-slate-200 px-8 py-4 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:border-primary/30"
            >
              Xem dịch vụ
              <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
              </svg>
            </Link>
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
  );
}
