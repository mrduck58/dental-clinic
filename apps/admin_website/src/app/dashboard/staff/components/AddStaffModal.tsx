"use client";

import React, { useState } from "react";
import { createStaffApi, uploadFileApi, type CreateStaffCommand } from "../../../../lib/apiClient";

interface AddStaffModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (message: string) => void;
}

export default function AddStaffModal({ isOpen, onClose, onSuccess }: AddStaffModalProps) {
  const [formData, setFormData] = useState<CreateStaffCommand>({
    fullName: "",
    email: "",
    phoneNumber: "",
    role: "Staff",
    employeeId: "",
    department: "",
    employmentStatus: "Active",
    profilePictureUrl: "",
    professionalNotes: "",
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [isUploadingImage, setIsUploadingImage] = useState(false);

  if (!isOpen) return null;

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith("image/")) {
      alert("Vui lòng chọn một tệp hình ảnh hợp lệ.");
      return;
    }

    setIsUploadingImage(true);
    try {
      const result = await uploadFileApi(file);
      setFormData((prev) => ({ ...prev, profilePictureUrl: result.url }));
    } catch (err) {
      alert(err instanceof Error ? err.message : "Tải ảnh lên thất bại");
    } finally {
      setIsUploadingImage(false);
    }
  };

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

    if (!formData.employeeId?.trim()) {
      newErrors.employeeId = "Mã nhân viên không được để trống";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: "" }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      // Clean up empty fields to null
      const payload: CreateStaffCommand = {
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        phoneNumber: formData.phoneNumber.trim(),
        role: formData.role,
        employeeId: formData.employeeId?.trim() || null,
        department: formData.department?.trim() || null,
        employmentStatus: formData.employmentStatus || "Active",
        profilePictureUrl: formData.profilePictureUrl?.trim() || null,
        professionalNotes: formData.professionalNotes?.trim() || null,
      };

      await createStaffApi(payload);
      onSuccess("Thêm nhân viên mới thành công! Email thông tin tài khoản đã được gửi.");
      onClose();
      // Reset form
      setFormData({
        fullName: "",
        email: "",
        phoneNumber: "",
        role: "Staff",
        employeeId: "",
        department: "",
        employmentStatus: "Active",
        profilePictureUrl: "",
        professionalNotes: "",
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Đã xảy ra lỗi không xác định";
      setErrors({ api: msg });
    } finally {
      setIsSubmitting(false);
    }
  };

  const inputClass = (fieldName: string) =>
    `w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold ${
      errors[fieldName] ? "border-red-400 bg-red-50/20" : "border-slate-200"
    }`;

  const labelClass = "text-[12.5px] font-extrabold text-slate-500 uppercase tracking-wider mb-1 block";

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
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Tạo tài khoản và phân quyền cho nhân sự mới.</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-all cursor-pointer"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* API Error display */}
        {errors.api && (
          <div className="bg-red-50 border border-red-100 text-red-650 p-4 rounded-xl text-[13.5px] font-bold">
            ⚠️ {errors.api}
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            
            {/* Fullname */}
            <div>
              <label className={labelClass}>Họ và tên <span className="text-red-500">*</span></label>
              <input
                type="text"
                name="fullName"
                value={formData.fullName}
                onChange={handleChange}
                placeholder="Ví dụ: Nguyễn Văn A"
                className={inputClass("fullName")}
              />
              {errors.fullName && (
                <p className="text-red-500 text-[11px] font-bold mt-1">{errors.fullName}</p>
              )}
            </div>

            {/* Employee ID */}
            <div>
              <label className={labelClass}>Mã nhân viên <span className="text-red-500">*</span></label>
              <input
                type="text"
                name="employeeId"
                value={formData.employeeId || ""}
                onChange={handleChange}
                placeholder="Ví dụ: NV-001 hoặc BS-010"
                className={inputClass("employeeId")}
              />
              {errors.employeeId && (
                <p className="text-red-500 text-[11px] font-bold mt-1">{errors.employeeId}</p>
              )}
            </div>

            {/* Email */}
            <div>
              <label className={labelClass}>Địa chỉ Email <span className="text-red-500">*</span></label>
              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                placeholder="Ví dụ: anhnv@dentalclinic.com"
                className={inputClass("email")}
              />
              {errors.email && (
                <p className="text-red-500 text-[11px] font-bold mt-1">{errors.email}</p>
              )}
            </div>

            {/* Phone Number */}
            <div>
              <label className={labelClass}>Số điện thoại <span className="text-red-500">*</span></label>
              <input
                type="text"
                name="phoneNumber"
                value={formData.phoneNumber}
                onChange={handleChange}
                placeholder="Ví dụ: 0987654321"
                className={inputClass("phoneNumber")}
              />
              {errors.phoneNumber && (
                <p className="text-red-500 text-[11px] font-bold mt-1">{errors.phoneNumber}</p>
              )}
            </div>

            {/* Role select */}
            <div>
              <label className={labelClass}>Vai trò / Vị trí</label>
              <div className="relative">
                <select
                  name="role"
                  value={formData.role}
                  onChange={handleChange}
                  className={inputClass("role") + " appearance-none pr-8 cursor-pointer"}
                >
                  <option value="Doctor">Bác sĩ</option>
                  <option value="Dentist">Nha sĩ</option>
                  <option value="Staff">Lễ tân / Trợ lý</option>
                  <option value="Admin">Quản trị viên (Admin)</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
            </div>

            {/* Department */}
            <div>
              <label className={labelClass}>Phòng ban / Bộ phận</label>
              <input
                type="text"
                name="department"
                value={formData.department || ""}
                onChange={handleChange}
                placeholder="Ví dụ: Phòng lâm sàng, Lễ tân"
                className={inputClass("department")}
              />
            </div>

            {/* Status select */}
            <div>
              <label className={labelClass}>Trạng thái làm việc</label>
              <div className="relative">
                <select
                  name="employmentStatus"
                  value={formData.employmentStatus || "Active"}
                  onChange={handleChange}
                  className={inputClass("employmentStatus") + " appearance-none pr-8 cursor-pointer"}
                >
                  <option value="Active">Đang làm việc (Active)</option>
                  <option value="On Leave">Nghỉ phép (On Leave)</option>
                  <option value="Inactive">Đã nghỉ việc (Inactive)</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                  </svg>
                </span>
              </div>
            </div>

            {/* Profile Picture Upload */}
            <div className="md:col-span-2">
              <label className={labelClass}>Ảnh đại diện</label>
              <div className="flex items-center gap-4 bg-slate-50 border border-slate-200 rounded-xl p-3.5">
                {formData.profilePictureUrl ? (
                  <img
                    src={formData.profilePictureUrl}
                    alt="Preview"
                    className="w-14 h-14 rounded-full object-cover border border-slate-200 shadow-sm shrink-0"
                  />
                ) : (
                  <div className="w-14 h-14 rounded-full bg-slate-100 border border-slate-200 flex items-center justify-center font-black text-slate-400 text-[18px] shrink-0 select-none">
                    👤
                  </div>
                )}
                <div className="flex flex-col gap-1.5">
                  <input
                    type="file"
                    accept="image/*"
                    onChange={handleImageUpload}
                    disabled={isUploadingImage}
                    id="add-staff-avatar"
                    className="hidden"
                  />
                  <label
                    htmlFor="add-staff-avatar"
                    className="px-4 py-2 bg-white hover:bg-slate-50 text-slate-700 text-[13px] font-extrabold border border-slate-200 rounded-lg cursor-pointer transition-all shadow-sm flex items-center gap-1.5 max-w-max"
                  >
                    {isUploadingImage ? "Đang tải..." : "Tải ảnh từ thiết bị"}
                  </label>
                  <span className="text-[11px] text-slate-400 font-semibold leading-none">
                    Hỗ trợ JPG, PNG, GIF, WEBP.
                  </span>
                </div>
              </div>
            </div>

          </div>

          {/* Professional Notes */}
          <div>
            <label className={labelClass}>Ghi chú chuyên môn / Ghi chú thêm</label>
            <textarea
              name="professionalNotes"
              value={formData.professionalNotes || ""}
              onChange={handleChange}
              rows={3}
              placeholder="Chuyên môn, kinh nghiệm, các thông tin phụ trợ khác..."
              className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold resize-none"
            />
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-4 mt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer disabled:opacity-50"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all cursor-pointer flex items-center justify-center gap-2 min-w-[120px] disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Đang tạo...
                </>
              ) : (
                "Lưu thông tin"
              )}
            </button>
          </div>
        </form>

      </div>
    </div>
  );
}
