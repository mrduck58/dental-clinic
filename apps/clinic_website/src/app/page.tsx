import React from "react";

export default function Home() {
  return (
    <div className="animate-fade-in flex flex-col min-h-screen">
      {/* NAVBAR */}
      <header className="navbar sticky top-0 z-50 h-20 bg-white/80 backdrop-blur-md border-b border-slate-200/50">
        <div className="container mx-auto px-4 h-full flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className="text-2xl text-primary">🦷</span>
            <span className="font-extrabold text-xl tracking-tight text-slate-900">
              Dental<span className="text-primary">Clinic</span>
            </span>
          </div>
          
          <nav className="hidden md:flex items-center gap-6">
            <a href="#" className="font-semibold text-primary bg-primary/5 px-4 py-2 rounded-md transition-all">Trang chủ</a>
            <a href="#services" className="font-semibold text-slate-500 hover:text-primary px-4 py-2 rounded-md transition-all">Dịch vụ</a>
            <a href="#dentists" className="font-semibold text-slate-500 hover:text-primary px-4 py-2 rounded-md transition-all">Bác sĩ</a>
            <a href="#contact" className="font-semibold text-slate-500 hover:text-primary px-4 py-2 rounded-md transition-all">Liên hệ</a>
          </nav>
          
          <div>
            <a href="#booking" className="btn bg-primary hover:bg-primary-hover text-white px-6 py-2.5 rounded-full font-semibold transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/20">
              Đặt lịch ngay
            </a>
          </div>
        </div>
      </header>

      {/* HERO SECTION */}
      <section className="relative py-28 bg-radial from-primary-light/40 via-transparent to-transparent">
        <div className="container mx-auto px-4 flex flex-col items-center text-center">
          <span className="inline-flex items-center px-4 py-1.5 rounded-full text-xs font-semibold bg-primary-light text-primary mb-6">
            ✨ Hệ Thống Nha Khoa Tiêu Chuẩn Quốc Tế
          </span>
          
          <h1 className="text-4xl md:text-6xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-primary to-secondary leading-tight mb-6">
            Kiến Tạo Nụ Cười Xinh<br />Chăm Sóc Sức Khỏe Toàn Diện
          </h1>
          
          <p className="text-lg text-slate-500 max-w-2xl mb-10 leading-relaxed">
            Phòng khám nha khoa DentalClinic mang lại dịch vụ chăm sóc răng miệng cao cấp 
            với công nghệ hiện đại hàng đầu thế giới và đội ngũ bác sĩ giàu kinh nghiệm lâm sàng.
          </p>
          
          <div className="flex flex-col sm:flex-row gap-4 justify-center w-full mb-16">
            <a href="#booking" className="btn bg-primary hover:bg-primary-hover text-white px-8 py-3.5 rounded-full font-bold transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25">
              Khám miễn phí
            </a>
            <a href="#services" className="btn bg-white hover:bg-primary-light/40 text-primary border border-primary/20 hover:border-primary px-8 py-3.5 rounded-full font-bold transition-all hover:translate-y-[-2px]">
              Tìm hiểu dịch vụ
            </a>
          </div>

          {/* Stats card */}
          <div className="glass-card w-full max-w-3xl rounded-2xl shadow-premium p-6 md:p-8 flex flex-col sm:flex-row justify-between items-center gap-6 border border-slate-200/60">
            <div className="flex flex-col items-center flex-1">
              <span className="text-3xl md:text-4xl font-extrabold text-primary">10k+</span>
              <span className="text-xs font-semibold text-slate-500 mt-1 uppercase tracking-wider">Khách Hàng Hài Lòng</span>
            </div>
            <div className="hidden sm:block w-px h-12 bg-slate-200"></div>
            <div className="flex flex-col items-center flex-1">
              <span className="text-3xl md:text-4xl font-extrabold text-secondary">20+</span>
              <span className="text-xs font-semibold text-slate-500 mt-1 uppercase tracking-wider">Bác Sĩ Chuyên Khoa</span>
            </div>
            <div className="hidden sm:block w-px h-12 bg-slate-200"></div>
            <div className="flex flex-col items-center flex-1">
              <span className="text-3xl md:text-4xl font-extrabold text-primary">99%</span>
              <span className="text-xs font-semibold text-slate-500 mt-1 uppercase tracking-wider">Đánh Giá Xuất Sắc</span>
            </div>
          </div>
        </div>
      </section>

      {/* SERVICES SECTION */}
      <section id="services" className="py-24 bg-white">
        <div className="container mx-auto px-4">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h2 className="text-3xl md:text-4xl font-extrabold text-slate-900 mb-4 section-title relative after:content-[''] after:block after:w-16 after:height-[4px] after:bg-primary after:mx-auto after:mt-4 after:rounded-full">
              Dịch Vụ Nổi Bật Tại DentalClinic
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Chúng tôi cung cấp đa dạng dịch vụ điều trị và thẩm mỹ răng miệng cao cấp 
              giúp bạn tự tin sở hữu nụ cười tỏa sáng.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Service 1 */}
            <div className="glass-card hover-lift p-8 rounded-2xl border border-slate-200/50 flex flex-col items-start shadow-sm hover:shadow-lg">
              <div className="w-12 h-12 rounded-xl bg-primary-light flex items-center justify-center text-2xl text-primary mb-6">
                🦷
              </div>
              <h3 className="text-xl font-bold text-slate-900 mb-3">Niềng Răng Thẩm Mỹ</h3>
              <p className="text-slate-500 text-sm leading-relaxed mb-6 flex-1">
                Giải pháp chỉnh nha hiệu quả giúp răng đều đẹp, chuẩn khớp cắn với công nghệ khay niềng trong suốt Invisalign tiên tiến.
              </p>
              <a href="#booking" className="font-bold text-primary inline-flex items-center gap-1 hover:gap-2 transition-all">
                Chi tiết <span>→</span>
              </a>
            </div>

            {/* Service 2 */}
            <div className="glass-card hover-lift p-8 rounded-2xl border border-slate-200/50 flex flex-col items-start shadow-sm hover:shadow-lg">
              <div className="w-12 h-12 rounded-xl bg-secondary-light flex items-center justify-center text-2xl text-secondary mb-6">
                🔩
              </div>
              <h3 className="text-xl font-bold text-slate-900 mb-3">Cấy Ghép Implant</h3>
              <p className="text-slate-500 text-sm leading-relaxed mb-6 flex-1">
                Phục hình răng đã mất hoàn hảo từ chân răng đến mặt nhai, đảm bảo khả năng ăn nhai vững chãi và tính thẩm mỹ tuyệt đối.
              </p>
              <a href="#booking" className="font-bold text-primary inline-flex items-center gap-1 hover:gap-2 transition-all">
                Chi tiết <span>→</span>
              </a>
            </div>

            {/* Service 3 */}
            <div className="glass-card hover-lift p-8 rounded-2xl border border-slate-200/50 flex flex-col items-start shadow-sm hover:shadow-lg">
              <div className="w-12 h-12 rounded-xl bg-primary-light flex items-center justify-center text-2xl text-primary mb-6">
                ✨
              </div>
              <h3 className="text-xl font-bold text-slate-900 mb-3">Bọc Răng Sứ Thẩm Mỹ</h3>
              <p className="text-slate-500 text-sm leading-relaxed mb-6 flex-1">
                Khắc phục tình trạng răng xỉn màu, thưa, sứt mẻ với dòng sứ cao cấp chính hãng từ Đức và Mỹ, mang lại nụ cười rạng rỡ.
              </p>
              <a href="#booking" className="font-bold text-primary inline-flex items-center gap-1 hover:gap-2 transition-all">
                Chi tiết <span>→</span>
              </a>
            </div>
          </div>
        </div>
      </section>

      {/* DENTISTS SECTION */}
      <section id="dentists" className="py-24 bg-slate-50">
        <div className="container mx-auto px-4">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h2 className="text-3xl md:text-4xl font-extrabold text-slate-900 mb-4 section-title relative after:content-[''] after:block after:w-16 after:height-[4px] after:bg-primary after:mx-auto after:mt-4 after:rounded-full">
              Đội Ngũ Bác Sĩ Chuyên Gia
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Đội ngũ bác sĩ tâm huyết, giàu kinh nghiệm thực tế lâm sàng và liên tục tu nghiệp tại nước ngoài.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Doctor 1 */}
            <div className="glass-card hover-lift rounded-2xl border border-slate-200/50 overflow-hidden shadow-sm flex flex-col bg-white">
              <div className="h-60 bg-primary-light/30 flex items-center justify-center text-7xl select-none">
                👨‍⚕️
              </div>
              <div className="p-6 flex flex-col flex-1">
                <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-primary-light text-primary mb-3 self-start">
                  Giám Đốc Chuyên Môn
                </span>
                <h3 className="text-lg font-bold text-slate-900 mb-2">ThS. BS. Nguyễn Minh Đức</h3>
                <p className="text-slate-500 text-sm leading-relaxed mb-4 flex-1">
                  Hơn 15 năm kinh nghiệm về Cấy ghép Implant và Phục hình sứ thẩm mỹ.
                </p>
                <div className="text-xs text-primary font-bold border-t border-slate-100 pt-3">
                  Tốt nghiệp Đại học Y Dược TP.HCM
                </div>
              </div>
            </div>

            {/* Doctor 2 */}
            <div className="glass-card hover-lift rounded-2xl border border-slate-200/50 overflow-hidden shadow-sm flex flex-col bg-white">
              <div className="h-60 bg-secondary-light/30 flex items-center justify-center text-7xl select-none">
                👩‍⚕️
              </div>
              <div className="p-6 flex flex-col flex-1">
                <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-primary-light text-primary mb-3 self-start">
                  Chuyên Gia Chỉnh Nha
                </span>
                <h3 className="text-lg font-bold text-slate-900 mb-2">BS. Lê Thị Phương Thảo</h3>
                <p className="text-slate-500 text-sm leading-relaxed mb-4 flex-1">
                  Chuyên gia chứng nhận Invisalign Hoa Kỳ, thực hiện thành công hơn 1.000 ca niềng răng.
                </p>
                <div className="text-xs text-primary font-bold border-t border-slate-100 pt-3">
                  Tu nghiệp Chỉnh nha chuyên sâu tại Pháp
                </div>
              </div>
            </div>

            {/* Doctor 3 */}
            <div className="glass-card hover-lift rounded-2xl border border-slate-200/50 overflow-hidden shadow-sm flex flex-col bg-white">
              <div className="h-60 bg-primary-light/30 flex items-center justify-center text-7xl select-none">
                👨‍⚕️
              </div>
              <div className="p-6 flex flex-col flex-1">
                <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-primary-light text-primary mb-3 self-start">
                  Chuyên Gia Điều Trị Nội Nha
                </span>
                <h3 className="text-lg font-bold text-slate-900 mb-2">BSCKI. Trần Quốc Bảo</h3>
                <p className="text-slate-500 text-sm leading-relaxed mb-4 flex-1">
                  Khám và xử lý triệt để các bệnh lý tủy răng, bảo tồn răng gốc tối đa.
                </p>
                <div className="text-xs text-primary font-bold border-t border-slate-100 pt-3">
                  Tốt nghiệp Chuyên khoa I ĐH Y Hà Nội
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* FOOTER */}
      <footer className="bg-slate-900 text-slate-400 py-16 border-t border-slate-800">
        <div className="container mx-auto px-4 flex flex-col gap-10">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-10">
            <div>
              <div className="flex items-center gap-2 mb-6">
                <span className="text-3xl">🦷</span>
                <span className="font-extrabold text-2xl text-white">
                  Dental<span className="text-primary">Clinic</span>
                </span>
              </div>
              <p className="text-slate-400 text-sm leading-relaxed">
                Hệ thống phòng khám nha khoa cao cấp cam kết mang lại trải nghiệm khám chữa răng không đau, chuẩn xác và bền vững.
              </p>
            </div>
            
            <div>
              <h4 className="text-white font-bold mb-4 text-base">Liên Kết</h4>
              <div className="flex flex-col gap-3 text-sm">
                <a href="#" className="hover:text-white transition-colors">Về chúng tôi</a>
                <a href="#" className="hover:text-white transition-colors">Bảng giá dịch vụ</a>
                <a href="#" className="hover:text-white transition-colors">Tin tức y khoa</a>
              </div>
            </div>
            
            <div>
              <h4 className="text-white font-bold mb-4 text-base">Thông Tin Liên Hệ</h4>
              <div className="flex flex-col gap-3 text-sm">
                <span>📍 Địa chỉ: 123 Đường Ba Tháng Hai, Quận 10, TP.HCM</span>
                <span>📞 Hotline: 1900 6789 - 028 7300 1234</span>
                <span>✉️ Email: contact@dentalclinic.vn</span>
              </div>
            </div>
          </div>
          
          <div className="border-t border-slate-800 pt-8 text-center text-xs text-slate-500">
            © {new Date().getFullYear()} DentalClinic. All rights reserved.
          </div>
        </div>
      </footer>
    </div>
  );
}
