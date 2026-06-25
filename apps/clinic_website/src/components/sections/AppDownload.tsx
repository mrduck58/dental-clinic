export default function AppDownload() {
  return (
    <section className="py-16 bg-primary relative overflow-hidden">
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute top-0 left-1/4 w-96 h-96 bg-white/5 rounded-full blur-3xl" />
        <div className="absolute bottom-0 right-1/4 w-80 h-80 bg-white/5 rounded-full blur-3xl" />
        <div className="absolute top-1/2 right-0 w-64 h-64 bg-white/5 rounded-full blur-2xl" />
      </div>

      <div className="relative max-w-7xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">

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
  );
}
