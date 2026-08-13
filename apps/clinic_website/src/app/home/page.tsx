import AboutSection from "@/components/sections/AboutSection";
import ServicesSection from "@/components/sections/ServicesSection";
import WhyChooseUs from "@/components/sections/WhyChooseUs";
import DentistsSection from "@/components/sections/DentistsSection";
import TestimonialsSection from "@/components/sections/TestimonialsSection";
import NewsSection from "@/components/sections/NewsSection";
import { getServices, getPosts, getDentists, getFeaturedFeedbacks } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const [services, posts, dentists, feedbacks] = await Promise.all([
    getServices(),
    getPosts(),
    getDentists(),
    getFeaturedFeedbacks(),
  ]);

  return (
    <div className="animate-fade-in">
      <AboutSection preview />
      <ServicesSection services={services} preview />
      <WhyChooseUs />
      <DentistsSection dentists={dentists} preview />
      <TestimonialsSection feedbacks={feedbacks} preview />
      <NewsSection posts={posts} preview />
    </div>
  );
}
