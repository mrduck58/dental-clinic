export default function LoadingState({ label = "Đang tải dữ liệu…" }: { label?: string }) {
  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center gap-4 text-slate-400">
      <span className="w-10 h-10 rounded-full border-4 border-slate-200 border-t-primary animate-spin" />
      <span className="text-[14px] font-semibold">{label}</span>
    </div>
  );
}
