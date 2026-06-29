"use client";

import React, { useState } from "react";
import { createStaffApi, type CreateStaffCommand } from "../../../../lib/apiClient";

interface AddDoctorModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (message: string) => void;
  defaultRole?: string;
}

interface DoctorForm {
  fullName: string;
  email: string;
  phoneNumber: string;
  specialty: string;
  licenseNumber: string;
  yearsOfExperience: string;
  professionalNotes: string;
}

export default function AddDoctorModal({ isOpen, onClose, onSuccess, defaultRole = "Dentist" }: AddDoctorModalProps) {
  const [formData, setFormData] = useState<DoctorForm>({
    fullName: "",
    email: "",
    phoneNumber: "",
    specialty: "",
    licenseNumber: "",
    yearsOfExperience: "",
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
    if (!formData.specialty.trim()) newErrors.specialty = "Chuyên khoa không được để trống";
    if (!formData.licenseNumber.trim()) newErrors.licenseNumber = "Số giấy phép hành nghề không được để trống";
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
        specialty: formData.specialty.trim() || null,
        licenseNumber: formData.licenseNumber.trim() || null,
        yearsOfExperience: formData.yearsOfExperience ? Number(formData.yearsOfExperience) : null,
        professionalNotes: formData.professionalNotes.trim() || null,
        employmentStatus: "Active",
        profilePictureUrl: null,
      };
      await createStaffApi(payload);
      onSuccess("Thêm bác sĩ mới thành công! Hồ sơ đã được lưu.");
      onClose();
      setFormData({ fullName: "", email: "", phoneNumber: "", specialty: "", licenseNumber: "", yearsOfExperience: "", professionalNotes: "" });
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Đã xảy ra lỗi không xác định" });
    } finally {
      setIsSubmitting(false);
    }
  };

  const inputClass = (field: string) =>
    `w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border rounded-xl focus:bg-white focus:border-sky-500 focus:ring-1 focus:ring-sky-400 focus:outline-none transition-all font-semibold ${
      errors[field] ? "border-red-400 bg-red-50/20" : "border-slate-200"
    }`;
  const lbl = "text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-1 block";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto animate-fade-in">
      <div className="bg-white rounded-2xl border border-slate-200 w-full max-w-2xl shadow-2xl p-6 relative my-8 flex flex-col gap-5">

        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-100 pb-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-xl bg-sky-50 text-secondary flex items-center justify-center shrink-0">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.231 0-4.334-.588-6.15-1.615z" />
              </svg>
            </div>
            <div>
              <h3 className="text-[18px] font-black text-slate-900 leading-tight">Thêm Bác Sĩ Mới</h3>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Tạo hồ sơ nha sĩ hoặc bác sĩ chuyên khoa.</p>
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
              <input type="text" name="fullName" value={formData.fullName} onChange={handleChange} placeholder="Nguyễn Văn A" className={inputClass("fullName")} />
              {errors.fullName && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.fullName}</p>}
            </div>

            <div>
              <label className={lbl}>Email <span className="text-red-500">*</span></label>
              <input type="email" name="email" value={formData.email} onChange={handleChange} placeholder="bacsi@dentalclinic.com" className={inputClass("email")} />
              {errors.email && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.email}</p>}
            </div>

            <div>
              <label className={lbl}>Số điện thoại <span className="text-red-500">*</span></label>
              <input type="text" name="phoneNumber" value={formData.phoneNumber} onChange={handleChange} placeholder="0987654321" className={inputClass("phoneNumber")} />
              {errors.phoneNumber && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.phoneNumber}</p>}
            </div>

            <div>
              <label className={lbl}>Số năm kinh nghiệm</label>
              <input type="number" name="yearsOfExperience" value={formData.yearsOfExperience} onChange={handleChange} placeholder="VD: 5" min="0" max="50" className={inputClass("yearsOfExperience")} />
            </div>

            <div>
              <label className={lbl}>Chuyên khoa <span className="text-red-500">*</span></label>
              <input type="text" name="specialty" value={formData.specialty} onChange={handleChange} placeholder="Implant, Niềng răng, Nội nha..." className={inputClass("specialty")} />
              {errors.specialty && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.specialty}</p>}
            </div>

            <div>
              <label className={lbl}>Số giấy phép hành nghề <span className="text-red-500">*</span></label>
              <input type="text" name="licenseNumber" value={formData.licenseNumber} onChange={handleChange} placeholder="VD: 123456/BYT-GCN" className={inputClass("licenseNumber")} />
              {errors.licenseNumber && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.licenseNumber}</p>}
            </div>

          </div>

          <div>
            <label className={lbl}>Ghi chú / Thông tin thêm</label>
            <textarea name="professionalNotes" value={formData.professionalNotes} onChange={handleChange} rows={3}
              placeholder="Bằng cấp, kinh nghiệm đặc biệt, công trình nghiên cứu..."
              className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-sky-500 focus:ring-1 focus:ring-sky-400 focus:outline-none transition-all font-semibold resize-none"
            />
          </div>

          <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-4 mt-2">
            <button type="button" onClick={onClose} disabled={isSubmitting}
              className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer disabled:opacity-50">
              Hủy bỏ
            </button>
            <button type="submit" disabled={isSubmitting}
              className="px-5 py-2.5 bg-secondary hover:bg-sky-600 text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-sky-200 hover:shadow-lg transition-all cursor-pointer flex items-center gap-2 min-w-[130px] justify-center disabled:opacity-50">
              {isSubmitting ? (
                <><svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>Đang tạo...</>
              ) : "Lưu hồ sơ"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
