import type { Metadata } from "next";
import AboutSection from "@/components/sections/AboutSection";
import WhyChooseUs from "@/components/sections/WhyChooseUs";
import ProcessSection from "@/components/sections/ProcessSection";
import TestimonialsSection from "@/components/sections/TestimonialsSection";
import { getFeaturedFeedbacks } from "@/lib/api";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Giới thiệu | Sơn Giang Dental Clinic",
  description: "Hơn 15 năm kiến tạo nụ cười Việt Nam — câu chuyện, giá trị và tiêu chuẩn chăm sóc của Sơn Giang Dental.",
};

export default async function GioiThieuPage() {
  const feedbacks = await getFeaturedFeedbacks();

  return (
    <div className="animate-fade-in">
      <AboutSection />
      <WhyChooseUs />
      <ProcessSection />
      <TestimonialsSection feedbacks={feedbacks} />
    </div>
  );
}
