"use client";

import { useState, useEffect, useCallback } from "react";
import AdminSidebar from "../../../components/shared/AdminSidebar";
import NotificationBell from "../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../hooks/useRequireAdmin";
import { getAiAnalyticsApi, type AiAnalyticsDto } from "../../../lib/apiClient";

const RANGE_OPTIONS: { label: string; days: number | undefined }[] = [
  { label: "7 ngày", days: 7 },
  { label: "14 ngày", days: 14 },
  { label: "30 ngày", days: 30 },
  { label: "Tất cả", days: undefined },
];

const FEATURE_LABEL: Record<string, string> = {
  ChatBot: "Chatbot tư vấn",
  PatientSummary: "Tóm tắt bệnh án",
  MarketingContent: "Soạn nội dung marketing",
};

function fmtPercent(part: number, total: number): string {
  if (total === 0) return "0%";
  return `${Math.round((part / total) * 100)}%`;
}

function vnDate(iso: string): string {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}`;
}

export default function AiAnalyticsPage() {
  useRequireAdmin();

  const [rangeDays, setRangeDays] = useState<number | undefined>(14);
  const [data, setData] = useState<AiAnalyticsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getAiAnalyticsApi(rangeDays);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải thống kê AI");
    } finally {
      setLoading(false);
    }
  }, [rangeDays]);

  useEffect(() => {
    void load();
  }, [load]);

  const totalAssistantMessages = data ? data.totalMessages - data.totalUserMessages : 0;
  const maxDailyCalls = data ? Math.max(1, ...data.dailyUsage.map((d) => d.calls)) : 1;

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="ai-analytics" />

      <main className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-16 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex flex-col">
            <h1 className="text-[18px] font-black text-slate-900 leading-tight">Thống kê AI</h1>
            <p className="text-[12.5px] text-slate-400 font-semibold mt-0.5">
              Hiệu quả và chi phí vận hành của chatbot, tóm tắt bệnh án, soạn nội dung marketing
            </p>
          </div>
          <NotificationBell />
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          {/* Range selector */}
          <div className="flex items-center gap-2 shrink-0">
            {RANGE_OPTIONS.map((opt) => (
              <button
                key={opt.label}
                onClick={() => setRangeDays(opt.days)}
                className={`px-3.5 py-1.5 rounded-lg text-[12.5px] font-bold transition-all cursor-pointer ${
                  rangeDays === opt.days
                    ? "bg-primary text-white shadow-md shadow-primary/25"
                    : "bg-white text-slate-500 border border-slate-200 hover:bg-slate-50"
                }`}
              >
                {opt.label}
              </button>
            ))}
          </div>

          {error && (
            <div className="px-4 py-3 rounded-xl bg-red-50 border border-red-100 text-[12.5px] font-semibold text-red-600 flex items-center justify-between">
              {error}
              <button onClick={() => void load()} className="font-bold underline cursor-pointer">
                Thử lại
              </button>
            </div>
          )}

          {loading && !data && (
            <div className="flex items-center justify-center py-20">
              <div className="w-8 h-8 border-2 border-slate-200 border-t-primary rounded-full animate-spin" />
            </div>
          )}

          {data && (
            <>
              {/* STATS */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 shrink-0">
                <StatTile
                  label="Cuộc hội thoại"
                  value={data.totalConversations}
                  hint={data.rangeDays != null ? `${data.rangeDays} ngày gần đây` : "Từ trước tới nay"}
                  colorClass="bg-sky-50 text-secondary"
                  iconPath="M8.625 12a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H8.25m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H12m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 01-2.555-.337A5.972 5.972 0 015.41 20.97a5.969 5.969 0 01-.474-.065 4.48 4.48 0 00.978-2.025c.09-.457-.133-.901-.467-1.226C3.93 16.178 3 14.189 3 12c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25z"
                />
                <StatTile
                  label="Tổng tin nhắn"
                  value={data.totalMessages}
                  hint={`${data.totalUserMessages} từ bệnh nhân`}
                  colorClass="bg-violet-50 text-violet-600"
                  iconPath="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 01.865-.501 48.172 48.172 0 003.423-.379c1.584-.233 2.707-1.626 2.707-3.226V6.741c0-1.6-1.123-2.994-2.707-3.227a48.394 48.394 0 00-13.086 0C2.876 3.747 1.753 5.141 1.753 6.741v6.018z"
                />
                <StatTile
                  label="Gợi ý đặt lịch"
                  value={fmtPercent(data.suggestBookingCount, totalAssistantMessages)}
                  hint={`${data.suggestBookingCount} / ${totalAssistantMessages} câu trả lời`}
                  colorClass="bg-amber-50 text-amber-600"
                  iconPath="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z"
                />
                <StatTile
                  label="Đặt/hủy lịch qua chat"
                  value={data.bookingActionCount}
                  hint="Bot tự thao tác thành công"
                  colorClass="bg-emerald-50 text-emerald-600"
                  iconPath="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </div>

              {/* Usage by feature */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-5 py-4 border-b border-slate-100">
                  <span className="text-[14px] font-black text-slate-900">Sử dụng theo tính năng</span>
                </div>
                {data.usageByFeature.length === 0 ? (
                  <div className="px-5 py-8 text-center text-[12.5px] text-slate-400">
                    Chưa có lệnh gọi AI nào trong khoảng thời gian này.
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left">
                      <thead>
                        <tr className="border-b border-slate-100">
                          <th className="px-5 py-3 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Tính năng</th>
                          <th className="px-5 py-3 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Số lượt gọi</th>
                          <th className="px-5 py-3 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Tỷ lệ thành công</th>
                          <th className="px-5 py-3 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Lỗi</th>
                          <th className="px-5 py-3 text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">Thời gian phản hồi TB</th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.usageByFeature.map((f) => (
                          <tr key={f.feature} className="border-b border-slate-50 last:border-0">
                            <td className="px-5 py-3.5 text-[13px] font-bold text-slate-800">
                              {FEATURE_LABEL[f.feature] ?? f.feature}
                            </td>
                            <td className="px-5 py-3.5 text-[13px] font-semibold text-slate-600">{f.totalCalls}</td>
                            <td className="px-5 py-3.5 text-[13px] font-semibold text-slate-600">
                              {fmtPercent(f.successCount, f.totalCalls)}
                            </td>
                            <td className="px-5 py-3.5 text-[13px] font-semibold">
                              {f.failureCount > 0 ? (
                                <span className="text-red-600">{f.failureCount}</span>
                              ) : (
                                <span className="text-slate-400">0</span>
                              )}
                            </td>
                            <td className="px-5 py-3.5 text-[13px] font-semibold text-slate-600">
                              {f.avgDurationMs.toLocaleString("vi-VN")} ms
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {/* Daily usage bar chart (thuần Tailwind/SVG, không dùng thư viện biểu đồ) */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
                <div className="px-5 py-4 border-b border-slate-100">
                  <span className="text-[14px] font-black text-slate-900">Số lệnh gọi AI theo ngày</span>
                </div>
                <div className="px-5 py-6">
                  {data.dailyUsage.length === 0 ? (
                    <div className="text-center text-[12.5px] text-slate-400 py-8">Chưa có dữ liệu.</div>
                  ) : (
                    <div className="flex items-end gap-2 h-40 overflow-x-auto">
                      {data.dailyUsage.map((point) => {
                        const heightPercent = Math.max(4, (point.calls / maxDailyCalls) * 100);
                        return (
                          <div key={point.date} className="flex flex-col items-center gap-1.5 shrink-0 w-10">
                            <div className="relative w-full h-32 flex items-end">
                              <div
                                className="w-full rounded-t-md bg-primary/80 hover:bg-primary transition-all"
                                style={{ height: `${heightPercent}%` }}
                                title={`${point.calls} lượt gọi, ${point.failures} lỗi`}
                              />
                              {point.failures > 0 && (
                                <div
                                  className="absolute bottom-0 w-full rounded-t-md bg-red-500/70"
                                  style={{ height: `${Math.max(2, (point.failures / maxDailyCalls) * 100)}%` }}
                                />
                              )}
                            </div>
                            <span className="text-[10.5px] font-semibold text-slate-400">{vnDate(`${point.date}`)}</span>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>
            </>
          )}
        </div>
      </main>
    </div>
  );
}

function StatTile({
  label,
  value,
  hint,
  colorClass,
  iconPath,
}: {
  label: string;
  value: string | number;
  hint: string;
  colorClass: string;
  iconPath: string;
}) {
  return (
    <div className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift flex items-center justify-between hover:border-primary/40 transition-all duration-200">
      <div>
        <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">{label}</span>
        <span className="text-3xl font-black text-slate-900 block mt-1">{value}</span>
        <span className="text-[12px] text-slate-400 font-semibold block mt-0.5">{hint}</span>
      </div>
      <div className={`w-12 h-12 rounded-xl flex items-center justify-center shrink-0 ${colorClass}`}>
        <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d={iconPath} />
        </svg>
      </div>
    </div>
  );
}
