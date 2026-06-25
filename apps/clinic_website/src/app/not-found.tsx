import Link from "next/link";

export default function NotFound() {
  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center gap-5 px-6 text-center">
      <span className="text-6xl">🦷</span>
      <div>
        <h1 className="text-3xl font-black text-slate-900 mb-2">404 — Không tìm thấy</h1>
        <p className="text-slate-500 text-[15px] max-w-md">
          Nội dung bạn tìm không tồn tại hoặc đã bị gỡ. Hãy quay lại trang chủ để tiếp tục.
        </p>
      </div>
      <Link
        href="/"
        className="inline-flex items-center gap-2 bg-primary hover:bg-primary-hover text-white px-7 py-3.5 rounded-xl font-bold text-[15px] transition-all hover:translate-y-[-2px] hover:shadow-lg hover:shadow-primary/25"
      >
        Về trang chủ
      </Link>
    </div>
  );
}
