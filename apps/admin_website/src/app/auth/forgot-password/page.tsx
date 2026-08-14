"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { forgotPasswordApi } from "../../../lib/apiClient";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const router = useRouter();

  const handleSubmit = async (e: { preventDefault(): void }) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);
    try {
      await forgotPasswordApi(email);
      setSent(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Gửi yêu cầu thất bại");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans text-slate-800">
      {/* LEFT COLUMN */}
      <div className="flex-1 flex flex-col justify-center px-6 py-12 lg:px-16 xl:px-24 bg-white relative z-10 shadow-2xl lg:max-w-[45%] shrink-0">

        {/* Header/Logo */}
        <div className="mx-auto w-full max-w-md">
          <div className="flex items-center gap-3">
            <span className="text-4xl text-primary animate-pulse">🦷</span>
            <div className="flex flex-col">
              <span className="text-[13px] font-black tracking-widest text-primary uppercase leading-none mb-1">SơnGiang</span>
              <span className="font-extrabold text-3xl tracking-tight text-slate-900 leading-none">
                Dental<span className="text-primary font-bold">Clinic</span>
              </span>
            </div>
          </div>

          <h2 className="mt-9 text-3xl font-black text-slate-900 tracking-tight">Quên mật khẩu?</h2>
          <p className="mt-2.5 text-[16px] text-slate-400 font-semibold">
            Nhập email tài khoản của bạn. Chúng tôi sẽ gửi liên kết để đặt lại mật khẩu.
          </p>
        </div>

        <div className="mt-10 mx-auto w-full max-w-md">
          {sent ? (
            /* Success state */
            <div className="flex flex-col gap-6">
              <div className="flex items-start gap-3 bg-green-50 border border-green-200 text-green-800 text-[14px] font-semibold px-4 py-4 rounded-xl">
                <svg className="w-5 h-5 shrink-0 mt-0.5 text-green-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <div className="flex flex-col gap-1">
                  <span className="font-extrabold text-green-900">Email đã được gửi!</span>
                  <span className="font-medium text-green-700">
                    Nếu địa chỉ <strong>{email}</strong> tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu. Vui lòng kiểm tra hộp thư đến (và thư rác).
                  </span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => router.push("/auth/login")}
                className="w-full bg-primary hover:bg-primary-hover text-white text-[15px] font-extrabold py-4 rounded-xl transition-all shadow-md shadow-primary/20 hover:translate-y-[-1px] cursor-pointer"
              >
                Quay lại đăng nhập
              </button>
            </div>
          ) : (
            /* Form state */
            <form onSubmit={handleSubmit} className="flex flex-col gap-6">
              <div className="flex flex-col gap-2">
                <label htmlFor="email" className="text-[14px] font-extrabold text-slate-500 uppercase tracking-wider">
                  Email tài khoản
                </label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                    </svg>
                  </span>
                  <input
                    id="email"
                    type="email"
                    required
                    placeholder="your@email.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full pl-11 pr-4 py-3.5 bg-slate-50 rounded-xl border border-slate-200/60 text-[16px] font-semibold focus:bg-white focus:border-primary/50 focus:ring-2 focus:ring-primary/10 focus:outline-none transition-all"
                  />
                </div>
              </div>

              {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 text-[14px] font-semibold px-4 py-3 rounded-xl">
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={isLoading}
                className="w-full bg-primary hover:bg-primary-hover disabled:opacity-60 disabled:cursor-not-allowed text-white text-[15px] font-extrabold py-4 rounded-xl transition-all shadow-md shadow-primary/20 hover:translate-y-[-1px] cursor-pointer"
              >
                {isLoading ? "Đang gửi..." : "Gửi liên kết đặt lại"}
              </button>

              <button
                type="button"
                onClick={() => router.push("/auth/login")}
                className="flex items-center justify-center gap-2 text-[14px] font-bold text-slate-500 hover:text-slate-700 transition-colors"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
                </svg>
                Quay lại đăng nhập
              </button>
            </form>
          )}
        </div>

        <div className="mt-16 mx-auto w-full max-w-md text-center">
          <p className="text-[13px] text-slate-400 font-semibold">
            © 2026 Sơn Giang Dental Clinic. Bảo lưu mọi quyền.
          </p>
        </div>
      </div>

      {/* RIGHT COLUMN - Static Asymmetric Collage Card Layout */}
      <div className="hidden lg:flex flex-1 flex-col items-center justify-center bg-[#070A13] border-l border-slate-900 relative overflow-hidden select-none">
        <style dangerouslySetInnerHTML={{
          __html: `
          .collage-container { position:relative;width:100%;max-width:680px;height:520px;margin-top:100px; }
          .collage-card {
            position:absolute;background:rgba(15,23,42,0.75);backdrop-filter:blur(12px);-webkit-backdrop-filter:blur(12px);
            border:1px solid rgba(255,255,255,0.05);box-shadow:0 10px 30px rgba(0,0,0,0.35);border-radius:16px;
            padding:16px;display:flex;flex-direction:column;transition:all 0.4s cubic-bezier(0.16,1,0.3,1);cursor:pointer;
          }
          .collage-card:hover { transform:scale(1.06) rotate(0deg) !important;opacity:1 !important;filter:blur(0px) !important;z-index:50 !important;border-color:rgba(220,38,38,0.3);box-shadow:0 20px 45px rgba(220,38,38,0.2),0 0 15px rgba(220,38,38,0.1); }
          .glow-blob { filter:blur(120px);will-change:transform; }
        `}} />

        <div className="absolute top-[-10%] right-[-10%] w-[500px] h-[500px] bg-primary/10 rounded-full glow-blob pointer-events-none"></div>
        <div className="absolute bottom-[-15%] left-[-10%] w-[500px] h-[500px] bg-secondary/10 rounded-full glow-blob pointer-events-none"></div>

        <div className="absolute top-16 left-16 right-16 z-20 flex flex-col gap-2">
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-primary animate-pulse"></span>
            <span className="text-[11px] font-black tracking-[0.25em] text-primary uppercase">SơnGiang Dental Clinic</span>
          </div>
          <h3 className="text-3xl font-black text-white tracking-tight">Bảo mật tài khoản</h3>
          <p className="text-[14px] text-slate-400 font-semibold max-w-md">
            Liên kết đặt lại mật khẩu chỉ có hiệu lực trong 1 giờ. Không chia sẻ liên kết cho bất kỳ ai.
          </p>
        </div>

        <div className="collage-container">
          {/* Card A */}
          <div className="collage-card w-[330px] h-[195px]" style={{ top:"4%", left:"2%", transform:"rotate(-1.5deg)", opacity:0.95, zIndex:20 }}>
            <div className="flex items-center justify-between mb-3">
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Bảo mật tài khoản</span>
                <span className="text-[16px] font-black text-slate-100 mt-0.5">Mật khẩu mạnh</span>
              </div>
              <span className="text-[10px] text-green-400 font-bold bg-green-500/10 px-2 py-0.5 rounded-full shrink-0">Khuyến nghị</span>
            </div>
            <div className="flex flex-col gap-2 mt-1">
              {[["Ít nhất 8 ký tự", true], ["Chữ hoa và chữ thường", true], ["Số và ký tự đặc biệt", true], ["Không dùng thông tin cá nhân", false]].map(([tip, done]) => (
                <div key={tip as string} className="flex items-center gap-2">
                  <span className={`w-4 h-4 rounded-full flex items-center justify-center shrink-0 text-[9px] font-black ${done ? "bg-green-500/20 text-green-400" : "bg-amber-500/20 text-amber-400"}`}>{done ? "✓" : "!"}</span>
                  <span className="text-[11px] text-slate-400 font-semibold">{tip as string}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Card B */}
          <div className="collage-card w-[290px] h-[215px]" style={{ top:"22%", right:"2%", transform:"rotate(2deg)", opacity:0.9, zIndex:22 }}>
            <div className="flex items-center justify-between border-b border-slate-800 pb-2 mb-3">
              <span className="text-[11px] text-slate-400 font-extrabold uppercase tracking-wider">Quy trình bảo mật</span>
              <span className="text-[9px] text-slate-500 font-semibold bg-slate-800 px-2 py-0.5 rounded-full">3 bước</span>
            </div>
            <div className="flex flex-col gap-3">
              {[["01", "Nhập email tài khoản", "sky"], ["02", "Kiểm tra hộp thư đến", "amber"], ["03", "Tạo mật khẩu mới", "green"]].map(([num, text, color]) => (
                <div key={num as string} className="flex items-center gap-2.5">
                  <span className={`w-6 h-6 rounded-lg bg-${color}-500/10 text-${color}-400 flex items-center justify-center shrink-0 font-extrabold text-[10px]`}>{num}</span>
                  <span className="text-[12px] font-semibold text-slate-300">{text as string}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Card C */}
          <div className="collage-card w-[260px] h-[130px]" style={{ bottom:"16%", left:"3%", transform:"rotate(-1deg)", opacity:0.8, zIndex:15 }}>
            <span className="text-[11px] text-slate-400 font-extrabold uppercase tracking-wider mb-2.5 pb-1 border-b border-slate-800">Lưu ý quan trọng</span>
            <div className="flex flex-col gap-2">
              <div className="flex items-start gap-1.5"><svg className="w-3.5 h-3.5 text-amber-400 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg><span className="text-[11px] text-slate-300 font-medium">Liên kết chỉ dùng được 1 lần</span></div>
              <div className="flex items-start gap-1.5"><svg className="w-3.5 h-3.5 text-red-400 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg><span className="text-[11px] text-slate-300 font-medium">Không chia sẻ liên kết với ai</span></div>
              <div className="flex items-start gap-1.5"><svg className="w-3.5 h-3.5 text-sky-400 mt-0.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg><span className="text-[11px] text-slate-300 font-medium">Hiệu lực trong vòng 1 giờ</span></div>
            </div>
          </div>

          {/* Card D */}
          <div className="collage-card w-[220px] h-[95px]" style={{ top:"4%", right:"12%", transform:"rotate(-2deg)", opacity:0.6, zIndex:10 }}>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Mã hóa</span>
                <span className="text-[14px] font-black text-slate-100 mt-0.5">BCrypt</span>
                <span className="text-[9px] text-slate-400 font-semibold">Work Factor 12</span>
              </div>
            </div>
          </div>

          {/* Card E */}
          <div className="collage-card w-[215px] h-[90px]" style={{ bottom:"2%", left:"40%", transform:"rotate(-3deg)", opacity:0.4, filter:"blur(0.8px)", zIndex:8 }}>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-sky-500/10 text-secondary flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Gmail SMTP</span>
                <span className="text-[13px] font-black text-slate-100 mt-0.5">Gửi tức thì</span>
                <span className="text-[9px] text-slate-400 font-semibold">TLS mã hóa</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
