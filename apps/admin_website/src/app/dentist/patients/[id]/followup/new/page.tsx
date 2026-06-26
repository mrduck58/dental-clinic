"use client";

import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import DentistSidebar from "../../../../../../components/shared/DentistSidebar";
import DentistPageHeader from "../../../../../../components/shared/DentistPageHeader";
import { useRequireDentist } from "../../../../../../hooks/useRequireDentist";
import { createFollowUpApi, getFollowUpSlotsApi, type FollowUpSlotsResultDto } from "../../../../../../lib/apiClient";

const REASONS = [
  "Kiểm tra sau điều trị",
  "Theo dõi tiến trình điều trị",
  "Kiểm tra định kỳ 3 tháng",
  "Kiểm tra định kỳ 6 tháng",
  "Tái khám sau nhổ răng",
  "Tái khám sau trám răng",
  "Tái khám sau điều trị tủy",
  "Tái khám sau bọc sứ",
  "Khác",
];

export default function NewFollowUpPage() {
  useRequireDentist();
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const [date, setDate] = useState("");
  const [timeSlot, setTimeSlot] = useState("");
  const [reason, setReason] = useState(REASONS[0]);
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Slots state
  const [slotsData, setSlotsData] = useState<FollowUpSlotsResultDto | null>(null);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [dentistId, setDentistId] = useState<string | null>(null);

  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const minDate = tomorrow.toISOString().split("T")[0];

  // Load dentist ID from examination data
  useEffect(() => {
    const fetchDentistId = async () => {
      try {
        const res = await fetch(`/api/appointments/${id}/examination`, {
          headers: {
            Authorization: `Bearer ${localStorage.getItem("token")}`,
          },
        });
        if (res.ok) {
          const data = await res.json();
          setDentistId(data.dentist?.id);
        }
      } catch {
        // ignore
      }
    };
    void fetchDentistId();
  }, [id]);

  // Load slots when date or dentist changes
  useEffect(() => {
    if (!date || !dentistId) return;

    const loadSlots = async () => {
      setLoadingSlots(true);
      setTimeSlot("");
      try {
        const data = await getFollowUpSlotsApi(dentistId, date);
        setSlotsData(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Không thể tải lịch khám");
        setSlotsData(null);
      } finally {
        setLoadingSlots(false);
      }
    };

    void loadSlots();
  }, [date, dentistId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!timeSlot) return;

    setSaving(true);
    setError(null);

    try {
      const appointmentDate = new Date(`${date}T${timeSlot}:00`);

      await createFollowUpApi(id, {
        appointmentDate: appointmentDate.toISOString(),
        symptoms: reason,
        notes: notes || undefined,
      });

      router.push(`/dentist/patients/${id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Tạo lịch tái khám thất bại");
      setSaving(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <DentistSidebar activeMenu="patients" />

      <main className="flex-1 flex flex-col min-w-0">
        <DentistPageHeader
          title="Đặt lịch tái khám"
          subtitle={`Bệnh nhân #${id?.slice(0, 8)}`}
          left={
            <Link href={`/dentist/patients/${id}`} className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-700 transition-all shrink-0">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" /></svg>
            </Link>
          }
        />

        <div className="p-8 flex-1 overflow-y-auto flex justify-center">
          <form onSubmit={handleSubmit} className="w-full max-w-xl bg-white rounded-2xl border border-slate-200/60 shadow-sm p-8 flex flex-col gap-6">

            {error && (
              <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-[13px] text-red-700 font-semibold">
                {error}
              </div>
            )}

            {/* Date Selection */}
            <div className="flex flex-col gap-1.5">
              <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Ngày tái khám</label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                required
                min={minDate}
                className="px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700"
              />
            </div>

            {/* Time Slots */}
            <div className="flex flex-col gap-2">
              <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Giờ hẹn</label>

              {!date && (
                <div className="bg-slate-50 border border-slate-200 rounded-xl px-4 py-3 text-[13px] text-slate-400 font-semibold text-center">
                  Vui lòng chọn ngày trước
                </div>
              )}

              {date && loadingSlots && (
                <div className="flex items-center justify-center py-6">
                  <div className="w-6 h-6 border-2 border-primary/30 border-t-primary rounded-full animate-spin" />
                </div>
              )}

              {date && !loadingSlots && slotsData && !slotsData.hasSchedule && (
                <div className="bg-slate-50 border border-slate-200 rounded-xl px-4 py-3 text-[13px] text-slate-500 font-semibold text-center">
                  {slotsData.message ?? "Không có lịch làm việc"}
                </div>
              )}

              {date && !loadingSlots && slotsData && slotsData.hasSchedule && (
                <div className="grid grid-cols-4 gap-2">
                  {slotsData.slots.map((slot) => {
                    const isDisabled = !slot.isAvailable;
                    const isSelected = timeSlot === slot.time;

                    return (
                      <button
                        key={slot.time}
                        type="button"
                        onClick={() => !isDisabled && setTimeSlot(slot.time)}
                        disabled={isDisabled}
                        className={`py-2.5 text-[13px] font-bold rounded-xl border transition-all ${
                          isDisabled
                            ? "bg-slate-100 border-slate-200 text-slate-400 cursor-not-allowed"
                            : isSelected
                            ? "bg-primary text-white border-primary shadow-sm shadow-primary/25"
                            : "border-slate-200 text-slate-600 hover:border-primary/40 hover:bg-red-50 hover:text-primary cursor-pointer"
                        }`}
                      >
                        {slot.time}
                      </button>
                    );
                  })}
                </div>
              )}

              {!timeSlot && date && !loadingSlots && slotsData?.hasSchedule && (
                <p className="text-[12px] text-slate-400 font-semibold">Chưa chọn giờ</p>
              )}
            </div>

            {/* Reason */}
            <div className="flex flex-col gap-1.5">
              <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Lý do tái khám</label>
              <div className="relative">
                <select
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  className="w-full px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 appearance-none cursor-pointer pr-8"
                >
                  {REASONS.map((r) => <option key={r} value={r}>{r}</option>)}
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
            </div>

            {/* Notes */}
            <div className="flex flex-col gap-1.5">
              <label className="text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider">Dặn dò bệnh nhân</label>
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
                placeholder="VD: Không ăn uống 2 tiếng trước khi đến, mang theo phim X-quang..."
                className="px-4 py-3 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold text-slate-700 placeholder:text-slate-400 resize-none"
              />
            </div>

            {/* Summary */}
            {date && timeSlot && (
              <div className="bg-green-50 border border-green-100 rounded-xl px-4 py-3 flex items-center gap-3 text-[13px] text-green-800">
                <svg className="w-5 h-5 text-green-600 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                </svg>
                <span>
                  <span className="font-black">{date.split("-").reverse().join("/")}</span>
                  {" lúc "}
                  <span className="font-black">{timeSlot}</span>
                  {" — "}{reason}
                </span>
              </div>
            )}

            {/* Actions */}
            <div className="flex gap-3 pt-2">
              <Link href={`/dentist/patients/${id}`}
                className="flex-1 py-3 text-center text-[14px] font-bold text-slate-500 border border-slate-200 rounded-xl hover:bg-slate-50 transition-all">
                Hủy
              </Link>
              <button
                type="submit"
                disabled={saving || !timeSlot}
                className="flex-1 flex items-center justify-center gap-2 py-3 bg-primary text-white text-[14px] font-black rounded-xl hover:bg-red-600 transition-all shadow-sm shadow-primary/25 disabled:opacity-60 cursor-pointer"
              >
                {saving ? (
                  <>
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                    Đang lưu...
                  </>
                ) : (
                  <>
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                    </svg>
                    Xác nhận lịch hẹn
                  </>
                )}
              </button>
            </div>
          </form>
        </div>
      </main>
    </div>
  );
}
