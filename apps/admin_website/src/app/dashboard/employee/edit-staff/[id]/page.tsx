"use client";

import React, { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import AdminSidebar from "../../../../../components/shared/AdminSidebar";
import NotificationBell from "../../../../../components/shared/NotificationBell";
import { useRequireAdmin } from "../../../../../hooks/useRequireAdmin";
import { updateStaffApi, uploadFileApi, type StaffDto, type UpdateStaffCommand } from "../../../../../lib/apiClient";

interface StaffEditForm {
  fullName: string;
  gender: string;
  dateOfBirth: string;
  phoneNumber: string;
  email: string;
  address: string;
  profilePictureUrl: string;
  position: string;
  department: string;
  startDate: string;
  bio: string;
  role: string;
  employmentStatus: string;
  isActive: boolean;
}

export default function EditStaffPage() {
  useRequireAdmin();
  const router = useRouter();
  const params = useParams();
  const id = params.id as string;

  const [staff, setStaff] = useState<StaffDto | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [formData, setFormData] = useState<StaffEditForm>({
    fullName: "", gender: "", dateOfBirth: "", phoneNumber: "", email: "",
    address: "", profilePictureUrl: "", position: "", department: "",
    startDate: "", bio: "", role: "Staff", employmentStatus: "Active", isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const raw = sessionStorage.getItem("staffEditData");
    if (raw) {
      const data: StaffDto = JSON.parse(raw);
      sessionStorage.removeItem("staffEditData");
      setStaff(data);
      setFormData({
        fullName: data.fullName || "",
        gender: data.gender || "",
        dateOfBirth: data.dateOfBirth || "",
        phoneNumber: data.phoneNumber || "",
        email: data.email,
        address: data.address || "",
        profilePictureUrl: data.profilePictureUrl || "",
        position: data.position || "",
        department: data.department || "",
        startDate: data.startDate || "",
        bio: data.bio || "",
        role: data.role,
        employmentStatus: data.employmentStatus || "Active",
        isActive: data.isActive,
      });
    }
  }, []);

  const validate = () => {
    const e: Record<string, string> = {};
    if (!formData.fullName.trim()) e.fullName = "Họ và tên không được để trống";
    if (!formData.gender) e.gender = "Vui lòng chọn giới tính";
    if (!formData.dateOfBirth) e.dateOfBirth = "Ngày sinh không được để trống";
    if (!formData.phoneNumber.trim()) {
      e.phoneNumber = "Số điện thoại không được để trống";
    } else if (!/^\d{9,11}$/.test(formData.phoneNumber.replace(/\s+/g, ""))) {
      e.phoneNumber = "Số điện thoại phải chứa 9–11 chữ số";
    }
    if (!formData.email.trim()) {
      e.email = "Email không được để trống";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      e.email = "Email không đúng định dạng";
    }
    if (!formData.position.trim()) e.position = "Chức vụ không được để trống";
    if (!formData.department.trim()) e.department = "Bộ phận không được để trống";
    if (!formData.startDate) e.startDate = "Ngày vào làm không được để trống";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    if (name === "isActive") {
      setFormData((prev) => ({ ...prev, isActive: value === "true" }));
    } else {
      setFormData((prev) => ({ ...prev, [name]: value }));
    }
    if (errors[name]) setErrors((prev) => ({ ...prev, [name]: "" }));
  };

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setIsSubmitting(true);
    try {
      const payload: UpdateStaffCommand = {
        id,
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        phoneNumber: formData.phoneNumber.trim(),
        role: formData.role,
        department: formData.department.trim() || null,
        employmentStatus: formData.employmentStatus || "Active",
        profilePictureUrl: formData.profilePictureUrl.trim() || null,
        professionalNotes: staff?.professionalNotes ?? null,
        isActive: formData.isActive,
        specialty: null,
        licenseNumber: null,
        yearsOfExperience: null,
        gender: formData.gender || null,
        dateOfBirth: formData.dateOfBirth || null,
        address: formData.address.trim() || null,
        position: formData.position.trim() || null,
        startDate: formData.startDate || null,
        servicesHandled: null,
        certificateIssuedDate: null,
        certificateIssuedBy: null,
        education: null,
        bio: formData.bio.trim() || null,
      };
      await updateStaffApi(id, payload);
      sessionStorage.setItem("staffSuccessMsg", `Cập nhật thông tin nhân viên ${formData.fullName.trim()} thành công!`);
      router.push("/dashboard/employee");
    } catch (err: unknown) {
      setErrors({ api: err instanceof Error ? err.message : "Đã xảy ra lỗi không xác định" });
      setIsSubmitting(false);
    }
  };

  const inp = (field: string) =>
    `w-full px-4 py-3 text-[14px] bg-white border rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold ${
      errors[field] ? "border-red-400 bg-red-50/30" : "border-slate-200"
    }`;
  const lbl = "text-[12px] font-extrabold text-slate-500 uppercase tracking-wider mb-1.5 block";
  const errMsg = (field: string) =>
    errors[field] ? <p className="text-red-500 text-[11px] font-bold mt-1">{errors[field]}</p> : null;

  if (!staff) {
    return (
      <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
        <AdminSidebar activeMenu="staff" />
        <main className="flex-1 flex flex-col items-center justify-center gap-4">
          <svg className="w-14 h-14 text-slate-300" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
          </svg>
          <p className="text-slate-500 font-bold text-[15px]">Không tìm thấy dữ liệu nhân viên.</p>
          <button onClick={() => router.push("/dashboard/employee")} className="px-6 py-3 bg-primary text-white font-extrabold rounded-xl cursor-pointer">
            Quay lại danh sách
          </button>
        </main>
      </div>
    );
  }

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <AdminSidebar activeMenu="staff" />

      <main className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center justify-between shrink-0 shadow-sm shadow-slate-100/50">
          <div className="flex items-center gap-4">
            <button onClick={() => router.push("/dashboard/employee")} className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-all cursor-pointer">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </button>
            <div>
              <h1 className="text-xl font-extrabold text-slate-900 tracking-tight">Chỉnh Sửa Nhân Viên</h1>
              <p className="text-[13px] text-slate-400 font-semibold mt-0.5">
                {staff.employeeId && <span className="font-mono text-primary mr-2">{staff.employeeId}</span>}
                {staff.fullName || staff.email}
              </p>
            </div>
          </div>
          <NotificationBell />
        </header>

        <div className="flex-1 p-8 flex justify-center">
          <div className="w-full max-w-3xl">

            {errors.api && (
              <div className="mb-6 bg-red-50 border border-red-200 text-red-700 p-4 rounded-2xl text-[13.5px] font-bold flex items-start gap-2.5">
                <svg className="w-5 h-5 text-red-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
                {errors.api}
              </div>
            )}

            <form onSubmit={handleSubmit} className="flex flex-col gap-6">

              {/* ── Section 1: Thông tin cơ bản ── */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 flex flex-col gap-5">
                <div className="flex items-center gap-2 pb-1 border-b border-slate-100">
                  <div className="w-1 h-4 bg-primary rounded-full" />
                  <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin cơ bản</span>
                </div>

                {staff.employeeId && (
                  <div>
                    <label className={lbl}>Mã nhân viên</label>
                    <div className="flex items-center gap-3 px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl">
                      <span className="font-black text-primary text-[16px] font-mono tracking-wider">{staff.employeeId}</span>
                      <span className="text-[12px] text-slate-400 font-semibold">· Không thể thay đổi</span>
                    </div>
                  </div>
                )}

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="md:col-span-2">
                    <label className={lbl}>Họ và tên <span className="text-red-500">*</span></label>
                    <input type="text" name="fullName" value={formData.fullName} onChange={handleChange} className={inp("fullName")} />
                    {errMsg("fullName")}
                  </div>

                  <div>
                    <label className={lbl}>Giới tính <span className="text-red-500">*</span></label>
                    <div className="relative">
                      <select name="gender" value={formData.gender} onChange={handleChange} className={inp("gender") + " appearance-none pr-8 cursor-pointer"}>
                        <option value="">-- Chọn giới tính --</option>
                        <option value="Nam">Nam</option>
                        <option value="Nữ">Nữ</option>
                        <option value="Khác">Khác</option>
                      </select>
                      <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                      </span>
                    </div>
                    {errMsg("gender")}
                  </div>

                  <div>
                    <label className={lbl}>Ngày sinh <span className="text-red-500">*</span></label>
                    <input type="date" name="dateOfBirth" value={formData.dateOfBirth} onChange={handleChange} className={inp("dateOfBirth")} />
                    {errMsg("dateOfBirth")}
                  </div>

                  <div>
                    <label className={lbl}>Số điện thoại <span className="text-red-500">*</span></label>
                    <input type="text" name="phoneNumber" value={formData.phoneNumber} onChange={handleChange} className={inp("phoneNumber")} />
                    {errMsg("phoneNumber")}
                  </div>

                  <div>
                    <label className={lbl}>Email <span className="text-red-500">*</span></label>
                    <input type="email" name="email" value={formData.email} onChange={handleChange} className={inp("email")} />
                    {errMsg("email")}
                  </div>

                  <div className="md:col-span-2">
                    <label className={lbl}>Địa chỉ</label>
                    <input type="text" name="address" value={formData.address} onChange={handleChange} placeholder="123 Nguyễn Trãi, Quận 1, TP.HCM" className={inp("address")} />
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
                        <input type="file" accept="image/*" onChange={handleImageUpload} disabled={isUploadingImage} id="edit-staff-avatar" className="hidden" />
                        <label htmlFor="edit-staff-avatar" className="px-4 py-2 bg-white hover:bg-slate-50 text-slate-700 text-[13px] font-extrabold border border-slate-200 rounded-lg cursor-pointer transition-all shadow-sm max-w-max">
                          {isUploadingImage ? "Đang tải..." : "Tải ảnh từ thiết bị"}
                        </label>
                        <span className="text-[11px] text-slate-400 font-semibold">Hỗ trợ JPG, PNG, WEBP · Tối đa 5MB</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* ── Section 2: Thông tin công việc ── */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 flex flex-col gap-5">
                <div className="flex items-center gap-2 pb-1 border-b border-slate-100">
                  <div className="w-1 h-4 bg-primary rounded-full" />
                  <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Thông tin công việc</span>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className={lbl}>Chức vụ <span className="text-red-500">*</span></label>
                    <input type="text" name="position" value={formData.position} onChange={handleChange} placeholder="Lễ tân, Trợ lý nha khoa..." className={inp("position")} />
                    {errMsg("position")}
                  </div>

                  <div>
                    <label className={lbl}>Bộ phận <span className="text-red-500">*</span></label>
                    <input type="text" name="department" value={formData.department} onChange={handleChange} placeholder="Phòng khám, Kế toán..." className={inp("department")} />
                    {errMsg("department")}
                  </div>

                  <div>
                    <label className={lbl}>Ngày vào làm <span className="text-red-500">*</span></label>
                    <input type="date" name="startDate" value={formData.startDate} onChange={handleChange} className={inp("startDate")} />
                    {errMsg("startDate")}
                  </div>
                </div>

                <div>
                  <label className={lbl}>Mô tả công việc</label>
                  <textarea name="bio" value={formData.bio} onChange={handleChange} rows={4}
                    placeholder="Mô tả nhiệm vụ và trách nhiệm của nhân viên..."
                    className="w-full px-4 py-3 text-[14px] bg-white border border-slate-200 rounded-xl focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold resize-none"
                  />
                </div>
              </div>

              {/* ── Section 3: Trạng thái ── */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 flex flex-col gap-5">
                <div className="flex items-center gap-2 pb-1 border-b border-slate-100">
                  <div className="w-1 h-4 bg-amber-400 rounded-full" />
                  <span className="text-[11px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</span>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div>
                    <label className={lbl}>Vai trò</label>
                    <div className="relative">
                      <select name="role" value={formData.role} onChange={handleChange} className={inp("role") + " appearance-none pr-8 cursor-pointer"}>
                        <option value="Staff">Lễ tân / Trợ lý</option>
                        <option value="Admin">Quản trị viên</option>
                      </select>
                      <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                      </span>
                    </div>
                  </div>

                  <div>
                    <label className={lbl}>Trạng thái làm việc</label>
                    <div className="relative">
                      <select name="employmentStatus" value={formData.employmentStatus} onChange={handleChange} className={inp("employmentStatus") + " appearance-none pr-8 cursor-pointer"}>
                        <option value="Active">Đang làm việc</option>
                        <option value="On Leave">Nghỉ phép</option>
                        <option value="Inactive">Đã nghỉ việc</option>
                      </select>
                      <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                      </span>
                    </div>
                  </div>

                  <div>
                    <label className={lbl}>Trạng thái tài khoản</label>
                    <div className="relative">
                      <select name="isActive" value={formData.isActive ? "true" : "false"} onChange={handleChange} className={inp("isActive") + " appearance-none pr-8 cursor-pointer"}>
                        <option value="true">Đang kích hoạt</option>
                        <option value="false">Tạm khóa</option>
                      </select>
                      <span className="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end gap-3">
                <button type="button" onClick={() => router.push("/dashboard/employee")} disabled={isSubmitting}
                  className="px-6 py-3 text-[14px] font-bold text-slate-500 hover:text-slate-800 hover:bg-white border border-slate-200 rounded-xl transition-all cursor-pointer disabled:opacity-50 shadow-sm">
                  Hủy bỏ
                </button>
                <button type="submit" disabled={isSubmitting}
                  className="px-6 py-3 bg-primary hover:bg-primary-hover text-white text-[14px] font-extrabold rounded-xl shadow-md shadow-primary/20 hover:shadow-lg transition-all cursor-pointer flex items-center gap-2 min-w-[140px] justify-center disabled:opacity-50">
                  {isSubmitting ? (
                    <><svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>Đang lưu...</>
                  ) : "Cập nhật thông tin"}
                </button>
              </div>

            </form>
          </div>
        </div>
      </main>
    </div>
  );
}
