"use client";

import React from "react";
import StaffManagementPage, { type StaffPageConfig } from "../staff/components/StaffManagementPage";

const doctorsConfig: StaffPageConfig = {
  pageTitle: "Quản Lý Bác Sĩ",
  pageSubtitle: "Nha sĩ và bác sĩ chuyên khoa tại phòng khám.",
  activeMenu: "doctors",
  scopeRoles: "Doctor,Dentist",
  defaultAddRole: "Dentist",
  excelSheetName: "BacSi",
  roleOptions: [
    { value: "",        label: "Tất cả bác sĩ" },
    { value: "Dentist", label: "Nha sĩ" },
    { value: "Doctor",  label: "Bác sĩ chuyên khoa" },
  ],
  statCards: [
    {
      label: "Tổng bác sĩ",
      sublabel: "Nha sĩ và bác sĩ chuyên khoa",
      getValue: (s) => s.totalDentists + s.totalDoctors,
      colorClass: "text-slate-900",
      bgClass: "bg-red-50/50",
      iconClass: "text-primary",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.109A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z" />
        </svg>
      ),
    },
    {
      label: "Nha sĩ",
      sublabel: "Nha sĩ điều trị lâm sàng",
      getValue: (s) => s.totalDentists,
      colorClass: "text-secondary",
      bgClass: "bg-sky-50",
      iconClass: "text-secondary",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="1.75" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M9 4.5C7.2 4.5 6 5.9 6 7.5c0 1.1.42 2.1 1.12 2.85L8.2 20.5h3l.8-4.5.8 4.5h3l1.08-10.15c.7-.75 1.12-1.75 1.12-2.85C18 5.9 16.8 4.5 15 4.5H9z" />
        </svg>
      ),
    },
    {
      label: "Bác sĩ chuyên khoa",
      sublabel: "Bác sĩ khám và tư vấn",
      getValue: (s) => s.totalDoctors,
      colorClass: "text-emerald-600",
      bgClass: "bg-emerald-50",
      iconClass: "text-emerald-600",
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="1.75" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-3-3v6M12 3a9 9 0 110 18A9 9 0 0112 3z" />
        </svg>
      ),
    },
  ],
};

export default function DoctorsPage() {
  return <StaffManagementPage config={doctorsConfig} />;
}
