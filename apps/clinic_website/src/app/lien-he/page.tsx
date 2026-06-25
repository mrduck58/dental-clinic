import type { Metadata } from "next";
import PageHeader from "@/components/shared/PageHeader";
import ContactSection from "@/components/sections/ContactSection";

export const metadata: Metadata = {
  title: "Liên hệ | Sơn Giang Dental Clinic",
  description: "Liên hệ Sơn Giang Dental — địa chỉ, hotline, email, giờ làm việc và gửi yêu cầu tư vấn trực tuyến.",
};

export default function LienHePage() {
  return (
    <div className="animate-fade-in">
      <PageHeader
        eyebrow="Liên Hệ"
        title="Liên hệ"
        description="Đội ngũ Sơn Giang Dental luôn sẵn sàng hỗ trợ và đồng hành cùng nụ cười của bạn."
      />
      <ContactSection />
    </div>
  );
}
