// Shared role constants for the admin/owner (staff-facing) screens.
//
// The backend's `User.Role` is a strict enum with exactly 5 values:
// "Patient" | "Staff" | "Dentist" | "Owner" | "Admin". `UiRole` deliberately
// excludes "Patient" — every screen that uses this module only ever lists
// employees (staff/dentist/owner/admin accounts), never patients.
//
// The legacy "Doctor" string (an old synonym for "Dentist") no longer exists
// anywhere in backend data, so it is not handled here.

export type UiRole = "Admin" | "Owner" | "Dentist" | "Staff";

/** Collapses a raw role string from the API into a known `UiRole`, defensively
 * falling back to "Staff" for any unrecognized value. */
export function normalizeRole(raw: string): UiRole {
  if (raw === "Admin" || raw === "Owner" || raw === "Dentist" || raw === "Staff") return raw;
  return "Staff";
}

/** Canonical Vietnamese label per role — the single source of truth reconciling
 * the label variants previously duplicated across admin/owner pages. */
export const ROLE_LABELS: Record<UiRole, string> = {
  Admin: "Quản trị viên",
  Owner: "Chủ phòng khám",
  Dentist: "Bác sĩ",
  Staff: "Lễ tân / Trợ lý",
};

/** Canonical Tailwind badge classes per role, used for role pill/badge UI. */
export const ROLE_BADGE_CLASSES: Record<UiRole, string> = {
  Admin: "bg-purple-50 text-purple-700 border border-purple-200",
  Owner: "bg-amber-50 text-amber-700 border border-amber-200",
  Dentist: "bg-sky-50 text-sky-700 border border-sky-200",
  Staff: "bg-green-50 text-green-700 border border-green-200",
};
