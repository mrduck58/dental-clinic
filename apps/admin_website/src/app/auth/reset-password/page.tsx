"use client";

import { useState, useEffect, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { resetPasswordApi } from "../../../lib/apiClient";

function ResetPasswordForm() {
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const router = useRouter();
  const searchParams = useSearchParams();

  const token = searchParams.get("token") ?? "";
  const email = searchParams.get("email") ?? "";

  useEffect(() => {
    if (!token || !email) {
      setError("Liên kết không hợp lệ. Vui lòng thử lại quy trình quên mật khẩu.");
    }
  }, [token, email]);

  const handleSubmit = async (e: { preventDefault(): void }) => {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      setError("Mật khẩu xác nhận không khớp.");
      return;
    }
    if (newPassword.length < 6) {
      setError("Mật khẩu phải có ít nhất 6 ký tự.");
      return;
    }
    setError(null);
    setIsLoading(true);
    try {
      await resetPasswordApi(email, token, newPassword);
      setDone(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Đặt lại mật khẩu thất bại");
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

          <h2 className="mt-9 text-3xl font-black text-slate-900 tracking-tight">Đặt lại mật khẩu</h2>
          <p className="mt-2.5 text-[16px] text-slate-400 font-semibold">
            Tạo mật khẩu mới cho tài khoản <strong className="text-slate-600">{email}</strong>
          </p>
        </div>

        <div className="mt-10 mx-auto w-full max-w-md">
          {done ? (
            /* Success state */
            <div className="flex flex-col gap-6">
              <div className="flex items-start gap-3 bg-green-50 border border-green-200 text-green-800 text-[14px] font-semibold px-4 py-4 rounded-xl">
                <svg className="w-5 h-5 shrink-0 mt-0.5 text-green-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <div className="flex flex-col gap-1">
                  <span className="font-extrabold text-green-900">Mật khẩu đã được cập nhật!</span>
                  <span className="font-medium text-green-700">Bạn có thể đăng nhập ngay bây giờ bằng mật khẩu mới.</span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => router.push("/auth/login")}
                className="w-full bg-primary hover:bg-primary-hover text-white text-[15px] font-extrabold py-4 rounded-xl transition-all shadow-md shadow-primary/20 hover:translate-y-[-1px] cursor-pointer"
              >
                Đăng nhập ngay
              </button>
            </div>
          ) : (
            /* Form state */
            <form onSubmit={handleSubmit} className="flex flex-col gap-6">

              {/* New password */}
              <div className="flex flex-col gap-2">
                <label htmlFor="newPassword" className="text-[14px] font-extrabold text-slate-500 uppercase tracking-wider">
                  Mật khẩu mới
                </label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                    </svg>
                  </span>
                  <input
                    id="newPassword"
                    type={showNewPassword ? "text" : "password"}
                    required
                    minLength={6}
                    placeholder="••••••••"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="w-full pl-11 pr-11 py-3.5 bg-slate-50 rounded-xl border border-slate-200/60 text-[16px] font-semibold focus:bg-white focus:border-primary/50 focus:ring-2 focus:ring-primary/10 focus:outline-none transition-all"
                  />
                  <button type="button" onClick={() => setShowNewPassword(v => !v)}
                    className="absolute inset-y-0 right-3.5 flex items-center text-slate-400 hover:text-slate-600 transition-colors"
                    aria-label={showNewPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}>
                    {showNewPassword ? (
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
                      </svg>
                    ) : (
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      </svg>
                    )}
                  </button>
                </div>
              </div>

              {/* Confirm password */}
              <div className="flex flex-col gap-2">
                <label htmlFor="confirmPassword" className="text-[14px] font-extrabold text-slate-500 uppercase tracking-wider">
                  Xác nhận mật khẩu
                </label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </span>
                  <input
                    id="confirmPassword"
                    type={showConfirmPassword ? "text" : "password"}
                    required
                    placeholder="••••••••"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className={`w-full pl-11 pr-11 py-3.5 bg-slate-50 rounded-xl border text-[16px] font-semibold focus:bg-white focus:outline-none transition-all ${
                      confirmPassword && confirmPassword !== newPassword
                        ? "border-red-300 focus:border-red-400 focus:ring-2 focus:ring-red-100"
                        : "border-slate-200/60 focus:border-primary/50 focus:ring-2 focus:ring-primary/10"
                    }`}
                  />
                  <button type="button" onClick={() => setShowConfirmPassword(v => !v)}
                    className="absolute inset-y-0 right-3.5 flex items-center text-slate-400 hover:text-slate-600 transition-colors"
                    aria-label={showConfirmPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}>
                    {showConfirmPassword ? (
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
                      </svg>
                    ) : (
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      </svg>
                    )}
                  </button>
                </div>
                {confirmPassword && confirmPassword !== newPassword && (
                  <p className="text-[13px] text-red-500 font-semibold">Mật khẩu xác nhận chưa khớp</p>
                )}
              </div>

              {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 text-[14px] font-semibold px-4 py-3 rounded-xl">
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={isLoading || !token || !email}
                className="w-full bg-primary hover:bg-primary-hover disabled:opacity-60 disabled:cursor-not-allowed text-white text-[15px] font-extrabold py-4 rounded-xl transition-all shadow-md shadow-primary/20 hover:translate-y-[-1px] cursor-pointer"
              >
                {isLoading ? "Đang cập nhật..." : "Đặt lại mật khẩu"}
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

      {/* RIGHT COLUMN */}
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
          <h3 className="text-3xl font-black text-white tracking-tight">Mật khẩu mới của bạn</h3>
          <p className="text-[14px] text-slate-400 font-semibold max-w-md">
            Chọn mật khẩu đủ mạnh và không sử dụng lại mật khẩu cũ để bảo vệ tài khoản.
          </p>
        </div>

        <div className="collage-container">
          {/* Card A - strength indicator */}
          <div className="collage-card w-[330px] h-[195px]" style={{ top:"4%", left:"2%", transform:"rotate(-1.5deg)", opacity:0.95, zIndex:20 }}>
            <div className="flex items-center justify-between mb-3">
              <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Độ mạnh mật khẩu</span>
              <span className="text-[10px] text-green-400 font-bold bg-green-500/10 px-2 py-0.5 rounded-full">Rất mạnh</span>
            </div>
            <div className="flex gap-1.5 mb-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className={`flex-1 h-2 rounded-full ${i < 4 ? "bg-green-500" : "bg-green-500/30"}`} />
              ))}
            </div>
            <div className="flex flex-col gap-1.5">
              {[["8+ ký tự", true], ["Chữ hoa & thường", true], ["Số & ký tự đặc biệt", true]].map(([tip, ok]) => (
                <div key={tip as string} className="flex items-center gap-2">
                  <span className="w-3.5 h-3.5 rounded-full bg-green-500/20 text-green-400 flex items-center justify-center text-[8px] font-black">✓</span>
                  <span className="text-[11px] text-slate-400">{tip as string}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Card B */}
          <div className="collage-card w-[290px] h-[160px]" style={{ top:"26%", right:"2%", transform:"rotate(2deg)", opacity:0.9, zIndex:22 }}>
            <div className="flex items-center justify-between border-b border-slate-800 pb-2 mb-3">
              <span className="text-[11px] text-slate-400 font-extrabold uppercase tracking-wider">Mẹo bảo mật</span>
            </div>
            <div className="flex flex-col gap-2">
              <div className="flex items-start gap-2"><span className="text-green-400 text-[10px] mt-0.5">✓</span><span className="text-[11px] text-slate-300">Dùng câu mật khẩu dài khó đoán</span></div>
              <div className="flex items-start gap-2"><span className="text-green-400 text-[10px] mt-0.5">✓</span><span className="text-[11px] text-slate-300">Kết hợp chữ số và ký hiệu đặc biệt</span></div>
              <div className="flex items-start gap-2"><span className="text-red-400 text-[10px] mt-0.5">✗</span><span className="text-[11px] text-slate-300">Tránh dùng ngày sinh, tên riêng</span></div>
              <div className="flex items-start gap-2"><span className="text-red-400 text-[10px] mt-0.5">✗</span><span className="text-[11px] text-slate-300">Không dùng lại mật khẩu cũ</span></div>
            </div>
          </div>

          {/* Card C */}
          <div className="collage-card w-[250px] h-[110px]" style={{ bottom:"16%", left:"5%", transform:"rotate(-1deg)", opacity:0.7, zIndex:15 }}>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Bảo vệ</span>
                <span className="text-[14px] font-black text-slate-100 mt-0.5">Token 1 lần</span>
                <span className="text-[9px] text-slate-400 font-semibold">Hết hiệu lực sau dùng</span>
              </div>
            </div>
          </div>

          {/* Card D */}
          <div className="collage-card w-[220px] h-[90px]" style={{ top:"4%", right:"12%", transform:"rotate(-2deg)", opacity:0.5, zIndex:10 }}>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-amber-500/10 text-accent flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Hiệu lực</span>
                <span className="text-[15px] font-black text-slate-100 mt-0.5">1 giờ</span>
                <span className="text-[9px] text-slate-400 font-semibold">Từ khi gửi yêu cầu</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={
      <div className="flex min-h-screen items-center justify-center bg-white">
        <span className="text-slate-400 font-semibold">Đang tải...</span>
      </div>
    }>
      <ResetPasswordForm />
    </Suspense>
  );
}
