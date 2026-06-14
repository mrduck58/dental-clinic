"use client";

import React, { useState, useEffect } from "react";
import { updateStaffApi, uploadFileApi, type StaffDto, type UpdateStaffCommand } from "../../../../lib/apiClient";

interface EditDoctorModalProps {
  isOpen: boolean;
  onClose: () => void;
  staff: StaffDto | null;
  onSuccess: (message: string) => void;
}

export default function EditDoctorModal({ isOpen, onClose, staff, onSuccess }: EditDoctorModalProps) {
  const [formData, setFormData] = useState<UpdateStaffCommand>({
    id: "",
    fullName: "",
    email: "",
    phoneNumber: "",
    role: "Dentist",
    department: null,
    employmentStatus: "Active",
    profilePictureUrl: null,
    professionalNotes: null,
    isActive: true,
    specialty: null,
    licenseNumber: null,
    yearsOfExperience: null,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isUploadingImage, setIsUploadingImage] = useState(false);

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!file.type.startsWith("image/")) { alert("Vui lòng chọn tệp hình ảnh hợp lệ."); return; }
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

  useEffect(() => {
    if (staff) {
      setFormData({
        id: staff.id,
        fullName: staff.fullName || "",
        email: staff.email,
        phoneNumber: staff.phoneNumber || "",
        role: staff.role,
        department: staff.department,
        employmentStatus: staff.employmentStatus || "Active",
        profilePictureUrl: staff.profilePictureUrl,
        professionalNotes: staff.professionalNotes,
        isActive: staff.isActive,
        specialty: staff.specialty,
        licenseNumber: staff.licenseNumber,
        yearsOfExperience: staff.yearsOfExperience,
      });
      setErrors({});
    }
  }, [staff]);

  if (!isOpen || !staff) return null;

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
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    if (name === "isActive") {
      setFormData((prev) => ({ ...prev, isActive: value === "true" }));
    } else if (name === "yearsOfExperience") {
      setFormData((prev) => ({ ...prev, yearsOfExperience: value ? Number(value) : null }));
    } else {
      setFormData((prev) => ({ ...prev, [name]: value }));
    }
    if (errors[name]) setErrors((prev) => ({ ...prev, [name]: "" }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setIsSubmitting(true);
    try {
      const payload: UpdateStaffCommand = {
        ...formData,
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        phoneNumber: formData.phoneNumber.trim(),
        specialty: formData.specialty?.trim() || null,
        licenseNumber: formData.licenseNumber?.trim() || null,
        professionalNotes: formData.professionalNotes?.trim() || null,
      };
      await updateStaffApi(formData.id, payload);
      onSuccess("Cập nhật thông tin bác sĩ thành công!");
      onClose();
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
            <div className="w-11 h-11 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center shrink-0">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
              </svg>
            </div>
            <div>
              <h3 className="text-[18px] font-black text-slate-900 leading-tight">Chỉnh Sửa Hồ Sơ Bác Sĩ</h3>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">Cập nhật thông tin chuyên môn và cá nhân.</p>
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
              <input type="text" name="fullName" value={formData.fullName} onChange={handleChange} className={inputClass("fullName")} />
              {errors.fullName && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.fullName}</p>}
            </div>

            <div>
              <label className={lbl}>Vai trò</label>
              <div className="relative">
                <select name="role" value={formData.role} onChange={handleChange} className={inputClass("role") + " appearance-none pr-8 cursor-pointer"}>
                  <option value="Dentist">Nha sĩ</option>
                  <option value="Doctor">Bác sĩ chuyên khoa</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>
            </div>

            <div>
              <label className={lbl}>Email <span className="text-red-500">*</span></label>
              <input type="email" name="email" value={formData.email} onChange={handleChange} className={inputClass("email")} />
              {errors.email && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.email}</p>}
            </div>

            <div>
              <label className={lbl}>Số điện thoại <span className="text-red-500">*</span></label>
              <input type="text" name="phoneNumber" value={formData.phoneNumber} onChange={handleChange} className={inputClass("phoneNumber")} />
              {errors.phoneNumber && <p className="text-red-500 text-[11px] font-bold mt-1">{errors.phoneNumber}</p>}
            </div>

            <div>
              <label className={lbl}>Chuyên khoa</label>
              <input type="text" name="specialty" value={formData.specialty || ""} onChange={handleChange} placeholder="Implant, Niềng răng, Nội nha..." className={inputClass("specialty")} />
            </div>

            <div>
              <label className={lbl}>Số giấy phép hành nghề</label>
              <input type="text" name="licenseNumber" value={formData.licenseNumber || ""} onChange={handleChange} placeholder="123456/BYT-GCN" className={inputClass("licenseNumber")} />
            </div>

            <div>
              <label className={lbl}>Số năm kinh nghiệm</label>
              <input type="number" name="yearsOfExperience" value={formData.yearsOfExperience ?? ""} onChange={handleChange} min="0" max="50" className={inputClass("yearsOfExperience")} />
            </div>

            <div>
              <label className={lbl}>Trạng thái làm việc</label>
              <div className="relative">
                <select name="employmentStatus" value={formData.employmentStatus || "Active"} onChange={handleChange} className={inputClass("employmentStatus") + " appearance-none pr-8 cursor-pointer"}>
                  <option value="Active">Đang làm việc</option>
                  <option value="On Leave">Nghỉ phép</option>
                  <option value="Inactive">Đã nghỉ việc</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>
            </div>

            <div>
              <label className={lbl}>Trạng thái tài khoản</label>
              <div className="relative">
                <select name="isActive" value={formData.isActive ? "true" : "false"} onChange={handleChange} className={inputClass("isActive") + " appearance-none pr-8 cursor-pointer"}>
                  <option value="true">Đang kích hoạt</option>
                  <option value="false">Tạm khóa</option>
                </select>
                <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                </span>
              </div>
            </div>

            <div className="md:col-span-2">
              <label className={lbl}>Ảnh đại diện</label>
              <div className="flex items-center gap-4 bg-slate-50 border border-slate-200 rounded-xl p-3.5">
                {formData.profilePictureUrl ? (
                  <img src={formData.profilePictureUrl} alt="Preview" className="w-14 h-14 rounded-full object-cover border border-slate-200 shadow-sm shrink-0" />
                ) : (
                  <div className="w-14 h-14 rounded-full bg-slate-100 border border-slate-200 flex items-center justify-center shrink-0">
                    <svg className="w-7 h-7 text-slate-400" fill="none" stroke="currentColor" strokeWidth="1.75" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                    </svg>
                  </div>
                )}
                <div className="flex flex-col gap-1.5">
                  <input type="file" accept="image/*" onChange={handleImageUpload} disabled={isUploadingImage} id="edit-doctor-avatar" className="hidden" />
                  <label htmlFor="edit-doctor-avatar" className="px-4 py-2 bg-white hover:bg-slate-50 text-slate-700 text-[13px] font-extrabold border border-slate-200 rounded-lg cursor-pointer transition-all shadow-sm max-w-max">
                    {isUploadingImage ? "Đang tải..." : "Tải ảnh từ thiết bị"}
                  </label>
                  <span className="text-[11px] text-slate-400 font-semibold">Hỗ trợ JPG, PNG, WEBP.</span>
                </div>
              </div>
            </div>

          </div>

          <div>
            <label className={lbl}>Ghi chú / Thông tin thêm</label>
            <textarea name="professionalNotes" value={formData.professionalNotes || ""} onChange={handleChange} rows={3}
              placeholder="Bằng cấp, công trình nghiên cứu, kinh nghiệm đặc biệt..."
              className="w-full px-4 py-2.5 text-[13.5px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-sky-500 focus:ring-1 focus:ring-sky-400 focus:outline-none transition-all font-semibold resize-none"
            />
          </div>

          <div className="flex items-center justify-end gap-3 border-t border-slate-100 pt-4 mt-2">
            <button type="button" onClick={onClose} disabled={isSubmitting}
              className="px-5 py-2.5 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer disabled:opacity-50">
              Hủy bỏ
            </button>
            <button type="submit" disabled={isSubmitting}
              className="px-5 py-2.5 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all cursor-pointer flex items-center gap-2 min-w-[120px] justify-center disabled:opacity-50">
              {isSubmitting ? (
                <><svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>Đang lưu...</>
              ) : "Cập nhật"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
