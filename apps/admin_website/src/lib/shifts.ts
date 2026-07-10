// ── Ca làm việc ────────────────────────────────────────────────────────────
//
// 6 ca NGANG HÀNG trong một ngày (2 sáng, 2 chiều, 2 tối). Không còn khái niệm
// "ca sáng" là một đơn vị lớn không thể tách — mỗi ca là một khung độc lập,
// được phân công riêng.
//
// `id` được lưu trực tiếp vào WorkSchedule.Shift ở backend (tối đa 20 ký tự) và
// phải khớp với DentalClinic.API.Domain.Schedules.WorkShifts.

export type ShiftPeriod = "Buổi sáng" | "Buổi chiều" | "Buổi tối";

export interface ShiftDef {
  id: string;        // ví dụ "08:00-10:00"
  label: string;     // ví dụ "08:00 – 10:00"
  period: ShiftPeriod;
}

export const SHIFTS: ShiftDef[] = [
  { id: "08:00-10:00", label: "08:00 – 10:00", period: "Buổi sáng" },
  { id: "10:00-12:00", label: "10:00 – 12:00", period: "Buổi sáng" },
  { id: "13:30-15:30", label: "13:30 – 15:30", period: "Buổi chiều" },
  { id: "15:30-17:30", label: "15:30 – 17:30", period: "Buổi chiều" },
  { id: "17:30-19:30", label: "17:30 – 19:30", period: "Buổi tối" },
  { id: "19:30-21:30", label: "19:30 – 21:30", period: "Buổi tối" },
];

export const SHIFT_PERIODS: ShiftPeriod[] = ["Buổi sáng", "Buổi chiều", "Buổi tối"];

// Các ca thuộc một buổi, giữ đúng thứ tự thời gian.
export const shiftsByPeriod = (period: ShiftPeriod): ShiftDef[] =>
  SHIFTS.filter((s) => s.period === period);

const SHIFT_BY_ID: Record<string, ShiftDef> = Object.fromEntries(
  SHIFTS.map((s) => [s.id, s]),
);

export const shiftLabel = (id: string): string => SHIFT_BY_ID[id]?.label ?? id;
export const shiftPeriod = (id: string): ShiftPeriod | null => SHIFT_BY_ID[id]?.period ?? null;

// Tương thích dữ liệu cũ ("morning"/"afternoon") khi map ca đọc từ backend về buổi.
export const periodOfShift = (id: string): ShiftPeriod => {
  const def = SHIFT_BY_ID[id];
  if (def) return def.period;
  if (id === "afternoon") return "Buổi chiều";
  return "Buổi sáng";
};

// Suy ra buổi từ một thời điểm cụ thể (ví dụ giờ hẹn của lịch khám) dựa trên ranh
// giới các ca: trước 12:00 là sáng, trước 17:30 là chiều, còn lại là tối.
export const periodOfTime = (date: Date): ShiftPeriod => {
  const minutes = date.getHours() * 60 + date.getMinutes();
  if (minutes < 12 * 60) return "Buổi sáng";
  if (minutes < 17 * 60 + 30) return "Buổi chiều";
  return "Buổi tối";
};
