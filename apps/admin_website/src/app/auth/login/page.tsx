"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { loginApi, saveSession } from "../../../lib/apiClient";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sessionExpired, setSessionExpired] = useState(false);
  const router = useRouter();

  useEffect(() => {
    if (sessionStorage.getItem("sessionExpired") === "1") {
      setSessionExpired(true);
      sessionStorage.removeItem("sessionExpired");
    }
  }, []);

  const handleSubmit = async (e: { preventDefault(): void }) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);
    try {
      const data = await loginApi(email, password);
      saveSession(data);

      const role = data.user.role;
      if (role === "Dentist") {
        router.push("/dentist");
      } else if (role === "Staff") {
        router.push("/staff");
      } else {
        router.push("/");
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Đăng nhập thất bại");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans text-slate-800">
      {/* LEFT COLUMN - Form Container */}
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

          <h2 className="mt-9 text-3xl font-black text-slate-900 tracking-tight">Hệ thống nội bộ</h2>
          <p className="mt-2.5 text-[16px] text-slate-400 font-semibold">Chào mừng bạn quay trở lại. Vui lòng đăng nhập tài khoản quản trị.</p>
        </div>

        {/* Session expired banner */}
        {sessionExpired && (
          <div className="mt-6 mx-auto w-full max-w-md flex items-start gap-3 bg-amber-50 border border-amber-200 text-amber-800 text-[14px] font-semibold px-4 py-3.5 rounded-xl">
            <svg className="w-5 h-5 shrink-0 mt-0.5 text-amber-500" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
            </svg>
            <span>Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại để tiếp tục.</span>
          </div>
        )}

        {/* Login Form */}
        <div className="mt-10 mx-auto w-full max-w-md">
          <form onSubmit={handleSubmit} className="flex flex-col gap-6">

            {/* Email Field */}
            <div className="flex flex-col gap-2">
              <label htmlFor="email" className="text-[14px] font-extrabold text-slate-500 uppercase tracking-wider">
                Tài khoản / Email
              </label>
              <div className="relative">
                <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                  </svg>
                </span>
                <input
                  id="email"
                  type="email"
                  required
                  placeholder="admin@songiangclinic.vn"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-11 pr-4 py-3.5 bg-slate-50 rounded-xl border border-slate-200/60 text-[16px] font-semibold focus:bg-white focus:border-primary/50 focus:ring-2 focus:ring-primary/10 focus:outline-none transition-all"
                />
              </div>
            </div>

            {/* Password Field */}
            <div className="flex flex-col gap-2">
              <label htmlFor="password" className="text-[14px] font-extrabold text-slate-500 uppercase tracking-wider">
                Mật khẩu
              </label>
              <div className="relative">
                <span className="absolute inset-y-0 left-3.5 flex items-center text-slate-400">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                  </svg>
                </span>
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  required
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full pl-11 pr-11 py-3.5 bg-slate-50 rounded-xl border border-slate-200/60 text-[16px] font-semibold focus:bg-white focus:border-primary/50 focus:ring-2 focus:ring-primary/10 focus:outline-none transition-all"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute inset-y-0 right-3.5 flex items-center text-slate-400 hover:text-slate-600 transition-colors"
                  aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                >
                  {showPassword ? (
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

            {/* Remember Me & Forgot Password Row */}
            <div className="flex items-center justify-between text-[14px] mt-1">
              <div className="flex items-center gap-2">
                <input
                  id="remember"
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                  className="w-4.5 h-4.5 rounded border-slate-300 text-red-600 accent-red-600 focus:ring-red-500 cursor-pointer"
                />
                <label htmlFor="remember" className="font-bold text-slate-500 cursor-pointer select-none">
                  Ghi nhớ đăng nhập
                </label>
              </div>
              <button type="button" className="font-bold text-primary hover:underline">
                Quên mật khẩu?
              </button>
            </div>

            {/* Error message */}
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 text-[14px] font-semibold px-4 py-3 rounded-xl">
                {error}
              </div>
            )}

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-primary hover:bg-primary-hover disabled:opacity-60 disabled:cursor-not-allowed text-white text-[15px] font-extrabold py-4 rounded-xl transition-all shadow-md shadow-primary/20 hover:translate-y-[-1px] cursor-pointer"
            >
              {isLoading ? "Đang đăng nhập..." : "Đăng nhập hệ thống"}
            </button>
          </form>
        </div>

        {/* Footer info */}
        <div className="mt-16 mx-auto w-full max-w-md text-center">
          <p className="text-[13px] text-slate-400 font-semibold">
            © 2026 Sơn Giang Dental Clinic. Bảo lưu mọi quyền.
          </p>
        </div>

      </div>

      {/* RIGHT COLUMN - Static Asymmetric Collage Card Layout */}
      <div className="hidden lg:flex flex-1 flex-col items-center justify-center bg-[#070A13] border-l border-slate-900 relative overflow-hidden select-none">

        {/* Style block for static asymmetric collage layout */}
        <style dangerouslySetInnerHTML={{
          __html: `
          .collage-container {
            position: relative;
            width: 100%;
            height: 100%;
            max-width: 680px;
            height: 520px;
            margin-top: 100px;
          }
          .collage-card {
            position: absolute;
            background: rgba(15, 23, 42, 0.75);
            backdrop-filter: blur(12px);
            -webkit-backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.05);
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.35);
            border-radius: 16px;
            padding: 16px;
            display: flex;
            flex-direction: column;
            transition: all 0.4s cubic-bezier(0.16, 1, 0.3, 1);
            cursor: pointer;
            transform-origin: center;
          }
          .collage-card:hover {
            transform: scale(1.06) rotate(0deg) !important;
            opacity: 1 !important;
            filter: blur(0px) !important;
            z-index: 50 !important;
            border-color: rgba(220, 38, 38, 0.3);
            box-shadow: 0 20px 45px rgba(220, 38, 38, 0.2), 0 0 15px rgba(220, 38, 38, 0.1);
          }
          .glow-blob {
            filter: blur(120px);
            will-change: transform;
          }
        `}} />

        {/* Ambient Glows */}
        <div className="absolute top-[-10%] right-[-10%] w-[500px] h-[500px] bg-primary/10 rounded-full glow-blob pointer-events-none"></div>
        <div className="absolute bottom-[-15%] left-[-10%] w-[500px] h-[500px] bg-secondary/10 rounded-full glow-blob pointer-events-none"></div>

        {/* Header/Slogan info overlay (Top) */}
        <div className="absolute top-16 left-16 right-16 z-20 flex flex-col gap-2">
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-primary animate-pulse"></span>
            <span className="text-[11px] font-black tracking-[0.25em] text-primary uppercase">
              SơnGiang Dental Clinic
            </span>
          </div>
          <h3 className="text-3xl font-black text-white tracking-tight">
            Nền tảng vận hành nha khoa số
          </h3>
          <p className="text-[14px] text-slate-400 font-semibold max-w-md">
            Giao diện đồng bộ hóa các quy trình điều trị, tiếp đón bệnh nhân và theo dõi ghế điều trị theo thời gian thực.
          </p>
        </div>
        {/* Collage Container */}
        <div className="collage-container">

          {/* Card A: Doanh thu tuần này (Line Chart) */}
          <div
            className="collage-card w-[330px] h-[195px] border-red-500/10"
            style={{
              top: "4%",
              left: "2%",
              transform: "rotate(-1.5deg)",
              opacity: 0.95,
              zIndex: 20
            }}
          >
            <div className="flex items-center justify-between mb-3">
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Doanh thu tuần này</span>
                <span className="text-[18px] font-black text-slate-100 mt-0.5">45.2M VNĐ</span>
              </div>
              <span className="text-[10px] text-green-400 font-bold bg-green-500/10 px-2 py-0.5 rounded-full shrink-0">
                ↑ 18%
              </span>
            </div>

            {/* SVG Line Chart */}
            <div className="w-full flex-1 flex items-end">
              <svg className="w-full h-[100px]" viewBox="0 0 300 100" fill="none">
                <defs>
                  <linearGradient id="chart-grad-login" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#dc2626" stopOpacity="0.25" />
                    <stop offset="100%" stopColor="#dc2626" stopOpacity="0.0" />
                  </linearGradient>
                </defs>
                {/* Horizontal Grid lines */}
                <line x1="0" y1="20" x2="300" y2="20" stroke="rgba(255,255,255,0.03)" strokeWidth="1" />
                <line x1="0" y1="50" x2="300" y2="50" stroke="rgba(255,255,255,0.03)" strokeWidth="1" />
                <line x1="0" y1="80" x2="300" y2="80" stroke="rgba(255,255,255,0.03)" strokeWidth="1" />

                {/* Area under the line */}
                <path
                  d="M0 100 L 0 75 L 50 60 L 100 85 L 150 40 L 200 55 L 250 20 L 300 35 L 300 100 Z"
                  fill="url(#chart-grad-login)"
                />
                {/* Path line */}
                <path
                  d="M0 75 L 50 60 L 100 85 L 150 40 L 200 55 L 250 20 L 300 35"
                  stroke="#dc2626"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                {/* Points */}
                <circle cx="50" cy="60" r="3" fill="#ffffff" stroke="#dc2626" strokeWidth="1.5" />
                <circle cx="150" cy="40" r="3" fill="#ffffff" stroke="#dc2626" strokeWidth="1.5" />
                <circle cx="250" cy="20" r="3" fill="#ffffff" stroke="#dc2626" strokeWidth="1.5" />
                <circle cx="300" cy="35" r="3" fill="#ffffff" stroke="#dc2626" strokeWidth="1.5" />
              </svg>
            </div>
          </div>

          {/* Card B: Bảng lịch hẹn live (Appointments List) */}
          <div
            className="collage-card w-[290px] h-[215px] border-sky-500/10"
            style={{
              top: "22%",
              right: "2%",
              transform: "rotate(2deg)",
              opacity: 0.9,
              zIndex: 22
            }}
          >
            <div className="flex items-center justify-between border-b border-slate-800 pb-2 mb-3">
              <span className="text-[11px] text-slate-400 font-extrabold uppercase tracking-wider">Lịch hẹn hôm nay</span>
              <span className="text-[9px] text-slate-500 font-semibold bg-slate-800 px-2 py-0.5 rounded-full">LIVE</span>
            </div>

            <div className="flex flex-col gap-2.5">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2.5 min-w-0">
                  <div className="w-7 h-7 rounded-lg bg-sky-500/10 text-secondary flex items-center justify-center shrink-0 font-extrabold text-[10px]">SG</div>
                  <div className="flex flex-col min-w-0 leading-tight">
                    <span className="text-[12px] font-black text-slate-200 truncate">Nguyễn Văn Sơn</span>
                    <span className="text-[9px] text-slate-400 font-medium truncate">Cấy Implant</span>
                  </div>
                </div>
                <span className="text-[9px] font-bold text-sky-400 bg-sky-500/10 px-1.5 py-0.5 rounded">10:15</span>
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2.5 min-w-0">
                  <div className="w-7 h-7 rounded-lg bg-red-500/10 text-primary flex items-center justify-center shrink-0 font-extrabold text-[10px]">HN</div>
                  <div className="flex flex-col min-w-0 leading-tight">
                    <span className="text-[12px] font-black text-slate-200 truncate">Lê Hoàng Nam</span>
                    <span className="text-[9px] text-slate-400 font-medium truncate">Bọc răng sứ</span>
                  </div>
                </div>
                <span className="text-[9px] font-bold text-red-400 bg-red-500/10 px-1.5 py-0.5 rounded">10:30</span>
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2.5 min-w-0">
                  <div className="w-7 h-7 rounded-lg bg-green-500/10 text-green-500 flex items-center justify-center shrink-0 font-extrabold text-[10px]">MK</div>
                  <div className="flex flex-col min-w-0 leading-tight">
                    <span className="text-[12px] font-black text-slate-200 truncate">Đỗ Minh Khang</span>
                    <span className="text-[9px] text-slate-400 font-medium truncate">Điều trị tủy</span>
                  </div>
                </div>
                <span className="text-[9px] font-bold text-green-400 bg-green-500/10 px-1.5 py-0.5 rounded">11:00</span>
              </div>
            </div>
          </div>

          {/* Card C: Giám sát ghế máy (Chairs monitoring) */}
          <div
            className="collage-card w-[260px] h-[160px] border-green-500/10"
            style={{
              bottom: "16%",
              left: "3%",
              transform: "rotate(-1deg)",
              opacity: 0.8,
              zIndex: 15
            }}
          >
            <span className="text-[11px] text-slate-400 font-extrabold uppercase tracking-wider mb-2.5 pb-1 border-b border-slate-800">
              Giám sát ghế máy
            </span>
            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between text-[11px] font-semibold text-slate-300">
                <div className="flex items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-green-500"></span>
                  <span>Ghế điều trị 01</span>
                </div>
                <span className="text-[10px] text-green-400 font-bold bg-green-500/10 px-1.5 py-0.2 rounded">Đang chạy</span>
              </div>
              <div className="flex items-center justify-between text-[11px] font-semibold text-slate-300">
                <div className="flex items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-sky-500"></span>
                  <span>Ghế điều trị 02</span>
                </div>
                <span className="text-[10px] text-sky-400 font-bold bg-sky-500/10 px-1.5 py-0.2 rounded">Sẵn sàng</span>
              </div>
              <div className="flex items-center justify-between text-[11px] font-semibold text-slate-300">
                <div className="flex items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-amber-500"></span>
                  <span>Ghế tiểu phẫu 03</span>
                </div>
                <span className="text-[10px] text-amber-400 font-bold bg-amber-500/10 px-1.5 py-0.2 rounded">Khử trùng</span>
              </div>
            </div>
          </div>

          {/* Card D: Bác sĩ trực (Staff stats) */}
          <div
            className="collage-card w-[220px] h-[95px] border-amber-500/10"
            style={{
              top: "4%",
              right: "12%",
              transform: "rotate(-2deg)",
              opacity: 0.75,
              zIndex: 10
            }}
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-amber-500/10 text-accent flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Bác sĩ trực</span>
                <span className="text-[15px] font-black text-slate-100 mt-0.5">8 Bác sĩ trực</span>
                <span className="text-[9px] text-slate-400 font-semibold">Ca khám hôm nay</span>
              </div>
            </div>
          </div>

          {/* Card E: Doanh thu tháng (Monthly stats) */}
          <div
            className="collage-card w-[230px] h-[105px] border-green-500/10"
            style={{
              bottom: "2%",
              left: "40%",
              transform: "rotate(-3deg)",
              opacity: 0.45,
              filter: "blur(0.8px)",
              zIndex: 8
            }}
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-green-500/10 text-green-500 flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 3v17M3 12h18" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Doanh thu tháng</span>
                <span className="text-[15px] font-black text-slate-100 mt-0.5">1.2B VNĐ</span>
                <span className="text-[9px] text-slate-400 font-semibold">Đạt chỉ tiêu 92%</span>
              </div>
            </div>
          </div>

          {/* Card F: Bệnh nhân mới (New patients) */}
          <div
            className="collage-card w-[215px] h-[100px] border-red-500/10"
            style={{
              bottom: "12%",
              right: "4%",
              transform: "rotate(1.5deg)",
              opacity: 0.5,
              filter: "blur(0.5px)",
              zIndex: 7
            }}
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-red-500/10 text-primary flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198.007.031c.003.01.005.02.008.029A9.091 9.091 0 0021 18.75m-2.785-5.365A3 3 0 1016.5 9.75M16.5 13.5A3 3 0 0016.5 9.75M9 13.5a3.75 3.75 0 110-7.5 3.75 3.75 0 010 7.5zM2.25 18.75a6.75 6.75 0 0113.5 0M9 13.5c-.394 0-.776-.03-1.147-.09a6.75 6.75 0 00-5.603 5.34" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Bệnh nhân mới</span>
                <span className="text-[15px] font-black text-slate-100 mt-0.5">48 Đăng ký</span>
                <span className="text-[9px] text-green-400 font-semibold">↑ 15% tuần này</span>
              </div>
            </div>
          </div>

          {/* Card G: Dịch vụ hot Chỉnh nha (Services stats) */}
          <div
            className="collage-card w-[200px] h-[100px] border-sky-500/10"
            style={{
              top: "41%",
              left: "14%",
              transform: "rotate(3deg)",
              opacity: 0.55,
              filter: "blur(0.5px)",
              zIndex: 6
            }}
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-sky-500/10 text-secondary flex items-center justify-center shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m0-16c-3 0-5 2-5 5s2 5 5 5m0-10c3 0 5 2 5 5s-2 5-5 5m-5 0h10" />
                </svg>
              </div>
              <div className="flex flex-col leading-tight">
                <span className="text-[10px] text-slate-500 font-extrabold uppercase tracking-wider">Chỉnh nha</span>
                <span className="text-[14px] font-black text-slate-100 mt-0.5">Niềng răng</span>
                <span className="text-[9px] text-slate-400 font-semibold">35 Ca điều trị</span>
              </div>
            </div>
          </div>

        </div>
      </div>
    </div>
  );
}

