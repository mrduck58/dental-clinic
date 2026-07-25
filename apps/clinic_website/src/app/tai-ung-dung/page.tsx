import type { Metadata } from "next";
import PageHeader from "@/components/shared/PageHeader";
import AppDownload from "@/components/sections/AppDownload";
import ProcessSection from "@/components/sections/ProcessSection";

export const metadata: Metadata = {
  title: "Tải ứng dụng | Sơn Giang Dental Clinic",
  description: "Tải app Sơn Giang Dental để đặt lịch, theo dõi lịch sử điều trị, nhắc tái khám và chat trực tiếp với bác sĩ.",
};

export default function TaiUngDungPage() {
  return (
    <div className="animate-fade-in">
      <PageHeader
        eyebrow="Ứng Dụng Di Động"
        title="Tải ứng dụng"
        description="Đặt lịch nha khoa chưa bao giờ dễ hơn — tất cả trong lòng bàn tay bạn."
      />
      <AppDownload />
      <ProcessSection />
    </div>
  );
}
