import Hero from "@/components/sections/Hero";
import AboutSection from "@/components/sections/AboutSection";
import ServicesSection from "@/components/sections/ServicesSection";
import WhyChooseUs from "@/components/sections/WhyChooseUs";
import DentistsSection from "@/components/sections/DentistsSection";
import TestimonialsSection from "@/components/sections/TestimonialsSection";
import NewsSection from "@/components/sections/NewsSection";
import AppDownload from "@/components/sections/AppDownload";
import { getServices, getPosts, getDentists, getFeaturedFeedbacks } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function Home() {
  const [services, posts, dentists, feedbacks] = await Promise.all([
    getServices(),
    getPosts(),
    getDentists(),
    getFeaturedFeedbacks(),
  ]);

  return (
    <div className="animate-fade-in">
      <Hero />
      <AboutSection preview />
      <ServicesSection services={services} preview />
      <WhyChooseUs />
      <DentistsSection dentists={dentists} preview />
      <TestimonialsSection feedbacks={feedbacks} preview />
      <NewsSection posts={posts} preview />
      <AppDownload />
    </div>
  );
}
