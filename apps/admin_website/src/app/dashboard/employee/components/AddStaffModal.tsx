"use client";

import React, { useState } from "react";
import { createStaffApi, type CreateStaffCommand } from "../../../../lib/apiClient";

interface AddStaffModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (message: string) => void;
  defaultRole?: string;
}

export default function AddStaffModal({ isOpen, onClose, onSuccess, defaultRole = "Staff" }: AddStaffModalProps) {
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
    employeeId: "",
    professionalNotes: "",
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!isOpen) return null;

  const validate = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.fullName.trim()) newErrors.fullName = "Họ và tên không được để trống";
    if (!formData.email.trim()) {
      newErrors.email = "Email không được để trống";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = "Email không đúng định dạng";
    }
    if (!formData.phoneNumber.trim()) {
      newErrors.phoneNumber = "Số điện thoại không được để trống";
    } else if (!/^\d{9,11}$/.test(formData.phoneNumber.replace(/\s+/g, ""))) {
      newErrors.phoneNumber = "Số điện thoại phải chứa từ 9 đến 11 chữ số";
    }
    if (!formData.employeeId.trim()) newErrors.employeeId = "Mã nhân viên không được để trống";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) setErrors((prev) => ({ ...prev, [name]: "" }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setIsSubmitting(true);
    try {
      const payload: CreateStaffCommand = {
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        phoneNumber: formData.phoneNumber.trim(),
        role: defaultRole,
        employeeId: formData.employeeId.trim() || null,
        professionalNotes: formData.professionalNotes.trim() || null,
        employmentStatus: "Active",
        profilePictureUrl: null,
        department: null,
      };
      await createStaffApi(payload);
      onSuccess("Thêm nhân viên mới thành công! Tạo tài khoản đăng nhập tại trang Tài khoản & Phân quyền.");
      onClose();
      setFormData({ fullName: "", email: "", phoneNumber: "", employeeId: "", professionalNotes: "" });
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Đã xảy ra lỗi không xác định" });
    } finally {
      setIsSubmitting(false);
    }
  };

  const inputClass = (field: string) =>
    `w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold ${
      errors[field] ? "border-red-400 bg-red-50/20" : "border-slate-200"
    }`;
  const lbl = "text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-1 block";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto animate-fade-in">
      <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-2xl shadow-2xl p-6 relative my-8 flex flex-col gap-5">

        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-100 pb-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-xl bg-red-50 text-primary flex items-center justify-center shrink-0">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.231 0-4.334-.588-6.15-1.615z" />
              </svg>
            </div>
            <div>
              <h3 className="text-[18px] font-black text-slate-900 leading-tight">Thêm Nhân Viên Mới</h3>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Lưu hồ sơ nhân viên. Tài khoản đăng nhập có thể tạo sau.</p>
            </div>
          </div>
          <button onClick={onClose} className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-all cursor-pointer">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {errors.api && (
          <div className="bg-red-50 border border-red-100 p-4 rounded-xl text-[13.5px] font-bold flex items-start gap-2 text-red-700">
            <svg className="w-5 h-5 text-red-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
            </svg>
            {errors.api}
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">

            <div>
              <label className={lbl}>Họ và tên <span className="text-red-500">*</span></label>
              <input type="text" name="fullName" value={formData.fullName} onChange={handleChange} placeholder="Ví dụ: Nguyễn Văn A" className={inputClass("fullName")} />
              {errors.fullName && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.fullName}</p>}
            </div>

            <div>
              <label className={lbl}>Mã nhân viên <span className="text-red-500">*</span></label>
              <input type="text" name="employeeId" value={formData.employeeId} onChange={handleChange} placeholder="Ví dụ: NV-001" className={inputClass("employeeId")} />
              {errors.employeeId && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.employeeId}</p>}
            </div>

            <div>
              <label className={lbl}>Địa chỉ Email <span className="text-red-500">*</span></label>
              <input type="email" name="email" value={formData.email} onChange={handleChange} placeholder="Ví dụ: anhnv@dentalclinic.com" className={inputClass("email")} />
              {errors.email && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.email}</p>}
            </div>

            <div>
              <label className={lbl}>Số điện thoại <span className="text-red-500">*</span></label>
              <input type="text" name="phoneNumber" value={formData.phoneNumber} onChange={handleChange} placeholder="Ví dụ: 0987654321" className={inputClass("phoneNumber")} />
              {errors.phoneNumber && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.phoneNumber}</p>}
            </div>

          </div>

          <div>
            <label className={lbl}>Ghi chú thêm</label>
            <textarea name="professionalNotes" value={formData.professionalNotes} onChange={handleChange} rows={3}
              placeholder="Kinh nghiệm, chuyên môn, thông tin phụ trợ..."
              className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold resize-none"
            />
          </div>

          <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-4 mt-2">
            <button type="button" onClick={onClose} disabled={isSubmitting}
              className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer disabled:opacity-50">
              Hủy bỏ
            </button>
            <button type="submit" disabled={isSubmitting}
              className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all cursor-pointer flex items-center justify-center gap-2 min-w-[120px] disabled:opacity-50">
              {isSubmitting ? (
                <><svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>Đang tạo...</>
              ) : "Lưu thông tin"}
            </button>
          </div>
        </form>

      </div>
    </div>
  );
}
