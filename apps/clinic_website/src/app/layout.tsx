import type { Metadata } from "next";
import { Nunito } from "next/font/google";
import "./globals.css";

const nunito = Nunito({
  variable: "--font-nunito",
  subsets: ["vietnamese", "latin"],
  weight: ["300", "400", "500", "600", "700", "800", "900"],
});

export const metadata: Metadata = {
  title: "Nha Khoa Dental Clinic | Chăm sóc nụ cười của bạn",
  description: "Phòng khám nha khoa uy tín với đội ngũ bác sĩ giàu kinh nghiệm. Đặt lịch khám trực tuyến, nhanh chóng và tiện lợi.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="vi"
      className={`${nunito.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col font-sans">{children}</body>
    </html>
  );
}

