"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../components/shared/NotificationBell";
import { getAccountsApi } from "../../../../lib/apiClient";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";

type UiRole = "Admin" | "Owner" | "Dentist" | "Staff";

function normalizeRole(raw: string): UiRole {
  if (raw === "Admin" || raw === "Owner" || raw === "Staff") return raw;
  if (raw === "Dentist" || raw === "Doctor") return "Dentist";
  return "Staff";
}

interface RoleInfo {
  key: UiRole;
  label: string;
  description: string;
  iconPath: string;
  colorBg: string;
  colorText: string;
  accessScope: string[];
}

const ROLES: RoleInfo[] = [
  {
    key: "Admin",
    label: "Admin",
    description: "Quản trị hệ thống: tài khoản, phân quyền, danh mục dùng chung (dịch vụ, thuốc, phòng khám) và theo dõi hoạt động hệ thống.",
    iconPath: "M9.348 14.652a3.75 3.75 0 010-5.304m5.304 0a3.75 3.75 0 010 5.304m-7.425 2.121a6.75 6.75 0 010-9.546m9.546 0a6.75 6.75 0 010 9.546M12 12h.008v.008H12V12z",
    colorBg: "bg-purple-50",
    colorText: "text-purple-700",
    accessScope: ["Tài khoản", "Dịch vụ", "Thuốc", "Phòng khám", "Phân tích AI", "Nhật ký hệ thống", "Thông báo"],
  },
  {
    key: "Owner",
    label: "Chủ cửa hàng",
    description: "Điều hành vận hành kinh doanh phòng khám: nhân sự, lịch làm việc, tài chính, đơn nghỉ và phản hồi khách hàng.",
    iconPath: "M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3a1.5 1.5 0 011.5-1.5h3a1.5 1.5 0 011.5 1.5v3",
    colorBg: "bg-amber-50",
    colorText: "text-amber-700",
    accessScope: ["Quản lý nhân viên", "Thông tin phòng khám", "Lịch làm việc", "Ca khám & điều trị", "Bảng lương nhân viên", "Đơn xin nghỉ", "Phản hồi & Đánh giá", "Thông báo"],
  },
  {
    key: "Dentist",
    label: "Bác sĩ",
    description: "Khám chữa bệnh: hồ sơ bệnh án, chẩn đoán, phác đồ điều trị và kê đơn thuốc cho bệnh nhân.",
    iconPath: "M9 12h3.75M9 15h3.375c.621 0 1.125-.504 1.125-1.125V11.25M9 9h7.5M12 3v18M3 5.25h18A2.25 2.25 0 0121 7.5v9a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 16.5v-9A2.25 2.25 0 015.25 5.25z",
    colorBg: "bg-sky-50",
    colorText: "text-sky-700",
    accessScope: ["Lịch làm việc", "Bệnh nhân hôm nay", "Bệnh nhân đã khám", "Đơn xin nghỉ", "Thông báo"],
  },
  {
    key: "Staff",
    label: "Nhân viên",
    description: "Tiếp đón bệnh nhân, đặt và điều phối lịch hẹn, hỗ trợ vận hành hằng ngày tại phòng khám.",
    iconPath: "M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z",
    colorBg: "bg-green-50",
    colorText: "text-green-700",
    accessScope: ["Đặt lịch & Nhận đơn", "Check-in bệnh nhân", "Hàng đợi", "Hóa đơn & Thanh toán", "Phản hồi & Đánh giá", "Nhập xuất vật tư", "Quản lí bài viết", "Đơn xin nghỉ", "Thông báo"],
  },
];

export default function RolesPage() {
  useRequireAdmin();
  const router = useRouter();

  const [counts, setCounts] = useState<Record<UiRole, number>>({ Admin: 0, Owner: 0, Dentist: 0, Staff: 0 });
  const [isFetching, setIsFetching] = useState(true);

  useEffect(() => {
    getAccountsApi()
      .then((data) => {
        const next: Record<UiRole, number> = { Admin: 0, Owner: 0, Dentist: 0, Staff: 0 };
        data
          .filter((u) => u.role !== "Patient")
          .forEach((u) => { next[normalizeRole(u.role)]++; });
        setCounts(next);
      })
      .catch(() => {})
      .finally(() => setIsFetching(false));
  }, []);

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="permissions-roles" />

      <main className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 font-sans shadow-sm shadow-slate-100/50">
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">Vai Trò</h1>
            <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Các vai trò cố định trong hệ thống và phạm vi trách nhiệm.</p>
          </div>
          <NotificationBell />
        </header>

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">
          <div className="bg-slate-50/70 p-3.5 rounded-xl border border-slate-200 flex items-start gap-2.5">
            <svg className="w-4.5 h-4.5 text-slate-400 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
            </svg>
            <p className="text-[13px] text-slate-500 font-semibold leading-relaxed">
              Đây là 4 vai trò cố định của hệ thống, không thể tạo thêm hoặc xoá. Mỗi tài khoản được gán đúng một vai trò khi tạo mới tại trang{" "}
              <span className="font-extrabold text-slate-700">Người dùng</span>. Mục{" "}
              <span className="font-extrabold text-slate-700">Phạm vi truy cập</span> bên dưới liệt kê đúng những trang/chức năng mà vai trò đó thực sự được vào — đây chỉ là bảng tham khảo, không thể chỉnh sửa.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            {ROLES.map((role) => (
              <div
                key={role.key}
                className="bg-white p-5 rounded-2xl border border-slate-200/60 shadow-sm hover-lift hover:border-primary/40 transition-all duration-200 flex flex-col gap-4"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3.5">
                    <span className={`w-12 h-12 rounded-xl flex items-center justify-center shrink-0 ${role.colorBg} ${role.colorText}`}>
                      <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d={role.iconPath} />
                      </svg>
                    </span>
                    <div>
                      <h3 className="text-[16px] font-extrabold text-slate-900">{role.label}</h3>
                      <p className="text-[12px] text-slate-400 font-bold mt-0.5">
                        {isFetching ? "—" : `${counts[role.key]} tài khoản`}
                      </p>
                    </div>
                  </div>
                </div>

                <p className="text-[13px] text-slate-500 font-semibold leading-relaxed">{role.description}</p>

                <div>
                  <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider mb-2">Phạm vi truy cập</div>
                  <div className="flex flex-wrap gap-1.5">
                    {role.accessScope.map((item) => (
                      <span
                        key={item}
                        className={`inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold border ${role.colorBg} ${role.colorText} border-current/20`}
                      >
                        {item}
                      </span>
                    ))}
                  </div>
                </div>

                <button
                  onClick={() => router.push(`/admin/permissions?role=${role.key}`)}
                  className="self-start inline-flex items-center gap-1.5 text-[13px] font-extrabold text-primary hover:text-primary-hover transition-colors cursor-pointer"
                >
                  Xem tài khoản
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M17.25 8.25L21 12m0 0l-3.75 3.75M21 12H3" />
                  </svg>
                </button>
              </div>
            ))}
          </div>
        </div>
      </main>
    </div>
  );
}
