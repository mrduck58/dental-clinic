import type { Metadata } from "next";
import PageHeader from "@/components/shared/PageHeader";

export const metadata: Metadata = {
  title: "Hướng dẫn sử dụng app | Sơn Giang Dental Clinic",
  description: "Hướng dẫn từng bước đặt lịch khám và khám phá các chức năng trong ứng dụng Sơn Giang Dental.",
};

type Step = {
  title: string;
  description: string;
  image: string;
};

const BOOKING_STEPS: Step[] = [
  {
    title: "Từ trang chủ, bấm \"Đặt khám\"",
    description: "Mở app, ở mục \"Truy cập nhanh\" trên trang chủ bấm vào ô \"Đặt khám\" (hoặc dấu + ở thanh điều hướng dưới cùng) để bắt đầu đặt lịch.",
    image: "/guide/step-0-trang-chu.png",
  },
  {
    title: "Chọn hồ sơ bệnh nhân",
    description: "Chọn \"Tôi\" hoặc một thành viên trong hồ sơ gia đình đã thêm trước đó. Có thể bấm \"Thêm mới hồ sơ\" ngay tại bước này nếu đặt lịch cho người thân.",
    image: "/guide/step-1-chon-ho-so.png",
  },
  {
    title: "Chọn dịch vụ",
    description: "Gõ tìm nhanh chuyên khoa hoặc lướt danh sách dịch vụ kèm giá niêm yết. Bấm \"Xem thêm\" trên mỗi dịch vụ để xem mô tả chi tiết trước khi chọn.",
    image: "/guide/step-2-chon-dich-vu.png",
  },
  {
    title: "Chọn ngày khám",
    description: "Lịch hiển thị dạng calendar theo tháng — ngày còn giờ trống được tô màu rõ ràng, ngày đã qua bị khoá. Chọn ngày phù hợp để sang bước kế tiếp.",
    image: "/guide/step-3-chon-ngay.png",
  },
  {
    title: "Chọn bác sĩ & khung giờ",
    description: "Xem danh sách bác sĩ còn khung giờ trống theo buổi sáng/chiều/tối. App có gắn nhãn \"Gợi ý AI\" cho bác sĩ phù hợp nhất với nhu cầu của bạn.",
    image: "/guide/step-4-chon-bac-si.png",
  },
  {
    title: "Xác nhận thông tin đặt khám",
    description: "Kiểm tra lại dịch vụ, ngày, giờ, bác sĩ đã chọn — bấm \"Sửa\" nếu cần đổi mục nào. Nhập thêm triệu chứng (nếu có) rồi bấm \"Xác nhận đặt khám\".",
    image: "/guide/step-5-xac-nhan.png",
  },
  {
    title: "Đặt lịch thành công",
    description: "Nhận ngay mã lịch hẹn với trạng thái \"Đã xác nhận\" — app sẽ gửi thông báo nhắc trước giờ khám. Có thể xem lại lịch hẹn bất kỳ lúc nào ở mục \"Xem lịch hẹn\".",
    image: "/guide/step-6-thanh-cong.png",
  },
];

type Feature = {
  title: string;
  description: string;
  image: string;
};

const APP_FEATURES: Feature[] = [
  {
    title: "Trợ lý AI tư vấn",
    description: "Chatbot AI trả lời nhanh câu hỏi về tình trạng răng miệng, gợi ý dịch vụ và bác sĩ phù hợp trước khi đặt lịch.",
    image: "/guide/feature-ai-chatbot.png",
  },
  {
    title: "Hồ sơ gia đình",
    description: "Thêm và quản lý hồ sơ cho nhiều thành viên trong nhà — đặt lịch, theo dõi điều trị cho cả gia đình trên cùng một tài khoản.",
    image: "/guide/feature-ho-so-gia-dinh.png",
  },
  {
    title: "Lịch sử điều trị",
    description: "Xem lại chẩn đoán, đơn thuốc và phác đồ điều trị theo từng giai đoạn của mỗi lần khám — lưu trữ đầy đủ, tra cứu bất cứ lúc nào.",
    image: "/guide/feature-lich-su-dieu-tri.png",
  },
  {
    title: "Nhắc lịch & thông báo",
    description: "Tự động nhắc lịch tái khám và gửi thông báo trước giờ hẹn — không còn lo quên lịch hay trễ giờ khám.",
    image: "/guide/feature-nhac-lich.png",
  },
  {
    title: "Đánh giá bác sĩ",
    description: "Xem đánh giá từ những bệnh nhân khác và để lại nhận xét sau mỗi lần khám để giúp cộng đồng lựa chọn bác sĩ phù hợp.",
    image: "/guide/feature-danh-gia-bac-si.png",
  },
  {
    title: "Thanh toán online",
    description: "Thanh toán chi phí điều trị ngay trong app qua cổng thanh toán tích hợp, xem lại lịch sử thanh toán mọi lúc.",
    image: "/guide/feature-thanh-toan.png",
  },
];

function PhoneFrame({ src, alt }: { src: string; alt: string }) {
  return (
    <div className="w-[220px] shrink-0 rounded-[1.75rem] border-[6px] border-slate-900 bg-slate-900 shadow-xl overflow-hidden">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={src} alt={alt} className="w-full h-auto block" />
    </div>
  );
}

export default function HuongDanSuDungPage() {
  return (
    <div className="animate-fade-in">
      <PageHeader
        eyebrow="Ứng Dụng Di Động"
        title="Hướng dẫn sử dụng app"
        description="Đặt lịch khám và quản lý sức khoẻ răng miệng cho cả gia đình, tất cả trong ứng dụng Sơn Giang Dental."
      />

      {/* Các bước đặt lịch */}
      <section className="py-16 bg-white">
        <div className="max-w-5xl mx-auto px-6">
          <div className="text-center max-w-2xl mx-auto mb-14">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Đặt Lịch Qua App</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
              {BOOKING_STEPS.length} Bước Đặt Lịch Khám
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Từ trang chủ đến nhận xác nhận lịch hẹn — toàn bộ quy trình chỉ mất chưa đến 2 phút.
            </p>
          </div>

          <div className="flex flex-col gap-14">
            {BOOKING_STEPS.map(({ title, description, image }, i) => {
              const reverse = i % 2 === 1;
              return (
                <div
                  key={title}
                  className={`flex flex-col ${reverse ? "sm:flex-row-reverse" : "sm:flex-row"} items-center gap-8 sm:gap-12`}
                >
                  <PhoneFrame src={image} alt={title} />
                  <div className="flex-1 text-center sm:text-left">
                    <div className="inline-flex items-center gap-2 mb-3">
                      <span className="w-9 h-9 rounded-xl bg-primary/10 text-primary flex items-center justify-center font-black text-[13px]">
                        {String(i).padStart(2, "0")}
                      </span>
                      <span className="text-[12px] font-black tracking-widest text-primary uppercase">
                        Bước {i}
                      </span>
                    </div>
                    <h3 className="text-xl md:text-2xl font-black text-slate-900 mb-2">{title}</h3>
                    <p className="text-slate-500 text-[14px] leading-relaxed max-w-md mx-auto sm:mx-0">{description}</p>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Các chức năng khác trong app */}
      <section className="py-16 bg-slate-50">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center max-w-2xl mx-auto mb-12">
            <span className="text-[12px] font-black tracking-widest text-primary uppercase">Khám Phá App</span>
            <h2 className="text-3xl md:text-4xl font-black text-slate-900 mt-2 mb-4">
              Chức Năng Trong Ứng Dụng
            </h2>
            <p className="text-slate-500 text-base leading-relaxed">
              Ngoài đặt lịch, app còn hỗ trợ theo dõi và chăm sóc sức khoẻ răng miệng toàn diện cho cả gia đình.
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
            {APP_FEATURES.map(({ title, description, image }) => (
              <div key={title} className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col items-center text-center">
                <PhoneFrame src={image} alt={title} />
                <h3 className="text-[15px] font-bold text-slate-900 mt-5 mb-1.5">{title}</h3>
                <p className="text-slate-500 text-[13px] leading-relaxed">{description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
