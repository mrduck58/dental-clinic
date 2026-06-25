const CONTACT_INFO = [
  {
    label: "Địa chỉ",
    value: "123 Đường Ba Tháng Hai, Quận 10, TP.HCM",
    icon: (
      <>
        <path strokeLinecap="round" strokeLinejoin="round" d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z" />
        <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1115 0z" />
      </>
    ),
  },
  {
    label: "Hotline",
    value: "1900 6789 — 028 7300 1234",
    icon: <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 6.75c0 8.284 6.716 15 15 15h2.25a2.25 2.25 0 002.25-2.25v-1.372c0-.516-.351-.966-.852-1.091l-4.423-1.106c-.44-.11-.902.055-1.173.417l-.97 1.293c-.282.376-.769.542-1.21.38a12.035 12.035 0 01-7.143-7.143c-.162-.441.004-.928.38-1.21l1.293-.97c.363-.271.527-.734.417-1.173L6.963 3.102a1.125 1.125 0 00-1.091-.852H4.5A2.25 2.25 0 002.25 4.5v2.25z" />,
  },
  {
    label: "Email",
    value: "contact@songiangdental.vn",
    icon: <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />,
  },
  {
    label: "Giờ làm việc",
    value: "T2–T6: 8:00 – 20:00 • T7–CN: 8:00 – 17:00",
    icon: <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />,
  },
];

export default function ContactSection() {
  return (
    <section className="py-16 bg-slate-50">
      <div className="max-w-7xl mx-auto px-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-start">
          {/* Left — info + map */}
          <div>
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Liên Hệ</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-5">
              Đến Thăm Phòng Khám
            </h2>
            <p className="text-slate-500 text-[15px] leading-relaxed mb-8 max-w-lg">
              Đặt lịch hẹn hoặc ghé thăm trực tiếp — đội ngũ Sơn Giang Dental luôn sẵn sàng đồng hành cùng nụ cười của bạn.
            </p>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-5 mb-8">
              {CONTACT_INFO.map(({ label, value, icon }) => (
                <div key={label} className="flex gap-4 bg-white rounded-2xl p-5 border border-slate-200/60 shadow-sm">
                  <div className="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      {icon}
                    </svg>
                  </div>
                  <div>
                    <div className="text-[12px] font-black text-slate-400 uppercase tracking-wider mb-1">{label}</div>
                    <div className="text-[14px] font-semibold text-slate-700 leading-snug">{value}</div>
                  </div>
                </div>
              ))}
            </div>

            {/* Map placeholder */}
            <div className="rounded-2xl overflow-hidden border border-slate-200/60 shadow-sm h-64 bg-slate-200 relative">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src="https://picsum.photos/seed/sgiangmap/800/400"
                alt="Bản đồ phòng khám Sơn Giang Dental"
                className="w-full h-full object-cover"
              />
              <div className="absolute inset-0 flex items-center justify-center">
                <span className="bg-white/95 text-slate-700 text-[13px] font-bold px-4 py-2 rounded-xl shadow-lg flex items-center gap-2">
                  <svg className="w-4 h-4 text-primary" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1115 0z" />
                  </svg>
                  Sơn Giang Dental — Quận 10, TP.HCM
                </span>
              </div>
            </div>
          </div>

          {/* Right — contact form */}
          <div className="bg-white rounded-3xl p-8 border border-slate-200/60 shadow-sm">
            <h3 className="text-[20px] font-black text-slate-900 mb-1">Gửi yêu cầu tư vấn</h3>
            <p className="text-slate-500 text-[13px] mb-6">Điền thông tin, chúng tôi sẽ liên hệ lại trong vòng 24 giờ.</p>

            <form className="flex flex-col gap-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[13px] font-bold text-slate-700">Họ và tên</label>
                  <input
                    type="text"
                    placeholder="Nguyễn Văn A"
                    className="px-4 py-3 rounded-xl border border-slate-200 text-[14px] text-slate-700 focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/10 transition-all"
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <label className="text-[13px] font-bold text-slate-700">Số điện thoại</label>
                  <input
                    type="tel"
                    placeholder="0901 234 567"
                    className="px-4 py-3 rounded-xl border border-slate-200 text-[14px] text-slate-700 focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/10 transition-all"
                  />
                </div>
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-[13px] font-bold text-slate-700">Dịch vụ quan tâm</label>
                <select className="px-4 py-3 rounded-xl border border-slate-200 text-[14px] text-slate-700 bg-white focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/10 transition-all">
                  <option>Niềng răng thẩm mỹ</option>
                  <option>Cấy ghép Implant</option>
                  <option>Bọc răng sứ thẩm mỹ</option>
                  <option>Tẩy trắng răng</option>
                  <option>Điều trị tủy răng</option>
                  <option>Nhổ răng khôn</option>
                </select>
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-[13px] font-bold text-slate-700">Nội dung</label>
                <textarea
                  rows={4}
                  placeholder="Mô tả ngắn gọn nhu cầu của bạn..."
                  className="px-4 py-3 rounded-xl border border-slate-200 text-[14px] text-slate-700 resize-none focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/10 transition-all"
                />
              </div>
              <button
                type="submit"
                className="mt-2 flex items-center justify-center gap-2 bg-primary hover:bg-primary-hover text-white px-6 py-3.5 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
              >
                Gửi yêu cầu
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
                </svg>
              </button>
            </form>
          </div>
        </div>
      </div>
    </section>
  );
}
