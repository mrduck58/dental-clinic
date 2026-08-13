"use client";

import React, { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import OwnerSidebar from "../../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../../hooks/useRequireOwner";
import { updateStaffApi, uploadFileApi, resolveAssetUrl, ApiValidationError, type StaffDto, type UpdateStaffCommand } from "../../../../../lib/apiClient";

interface DentistEditForm {
  fullName: string;
  gender: string;
  dateOfBirth: string;
  phoneNumber: string;
  email: string;
  address: string;
  profilePictureUrl: string;
  specialty: string;
  licenseNumber: string;
  servicesHandled: string;
  startDate: string;
  certificateIssuedDate: string;
  certificateIssuedBy: string;
  yearsOfExperience: string;
  education: string;
  bio: string;
  role: string;
  employmentStatus: string;
  isActive: boolean;
}

export default function EditDentistPage() {
  useRequireOwner();
  const router = useRouter();
  const params = useParams();
  const id = params.id as string;

  const [staff, setStaff] = useState<StaffDto | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [formData, setFormData] = useState<DentistEditForm>({
    fullName: "", gender: "", dateOfBirth: "", phoneNumber: "", email: "",
    address: "", profilePictureUrl: "", specialty: "", licenseNumber: "",
    servicesHandled: "", startDate: "", certificateIssuedDate: "",
    certificateIssuedBy: "", yearsOfExperience: "", education: "", bio: "",
    role: "Dentist", employmentStatus: "Active", isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [formEmploymentType, setFormEmploymentType] = useState("Full-time");
  const [formBaseSalary, setFormBaseSalary] = useState(25000000);
  const [formSalaryUnit, setFormSalaryUnit] = useState("Theo tháng");
  const [formLeaveAccrued, setFormLeaveAccrued] = useState(1.5);
  const [formAllowance, setFormAllowance] = useState(2500000);

  useEffect(() => {
    if (formEmploymentType === "Full-time") {
      setFormSalaryUnit("Theo tháng");
    }
  }, [formEmploymentType]);

  useEffect(() => {
    const raw = sessionStorage.getItem("staffEditData");
    if (raw) {
      const data: StaffDto = JSON.parse(raw);
      setStaff(data);

      const exp = data.yearsOfExperience ?? 5;
      const isDentist = data.role === "Dentist";
      const isPartTime = exp % 2 === 0 && isDentist;
      const isShift = !isPartTime && (data.role === "Staff" && (data.position?.toLowerCase().includes("lễ tân") || data.position?.toLowerCase().includes("tiếp đón")));
      const calculatedType = isPartTime ? "Part-time" : isShift ? "Shift-based" : "Full-time";
      const calculatedSalary = isDentist ? (25000000 + exp * 1500000) : (10000000 + (data.fullName?.length || 5) * 200000);
      const calculatedUnit = isPartTime ? "Theo ngày" : isShift ? "Theo ca" : "Theo tháng";
      const calculatedLeave = isDentist ? 1.5 : 1;

      setFormEmploymentType(data.employmentType || calculatedType);
      setFormBaseSalary(data.baseSalary ?? calculatedSalary);
      setFormSalaryUnit(data.salaryUnit || calculatedUnit);
      setFormLeaveAccrued(data.leaveAccrued ?? calculatedLeave);
      setFormAllowance(data.allowance ?? (isDentist ? 2500000 : 1200000));

      setFormData({
        fullName: data.fullName || "",
        gender: data.gender || "",
        dateOfBirth: data.dateOfBirth || "",
        phoneNumber: data.phoneNumber || "",
        email: data.email,
        address: data.address || "",
        profilePictureUrl: data.profilePictureUrl || "",
        specialty: data.specialty || "",
        licenseNumber: data.licenseNumber || "",
        servicesHandled: data.servicesHandled || "",
        startDate: data.startDate || "",
        certificateIssuedDate: data.certificateIssuedDate || "",
        certificateIssuedBy: data.certificateIssuedBy || "",
        yearsOfExperience: data.yearsOfExperience != null ? String(data.yearsOfExperience) : "",
        education: data.education || "",
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
    if (!formData.specialty.trim()) e.specialty = "Chuyên khoa không được để trống";
    if (!formData.licenseNumber.trim()) e.licenseNumber = "Số CCHN không được để trống";
    if (!formData.servicesHandled.trim()) e.servicesHandled = "Dịch vụ phụ trách không được để trống";
    if (!formData.startDate) e.startDate = "Ngày bắt đầu làm việc không được để trống";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
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
        department: staff?.department ?? null,
        employmentStatus: formData.employmentStatus || "Active",
        profilePictureUrl: formData.profilePictureUrl.trim() || null,
        professionalNotes: staff?.professionalNotes ?? null,
        isActive: formData.isActive,
        specialty: formData.specialty.trim() || null,
        licenseNumber: formData.licenseNumber.trim() || null,
        yearsOfExperience: formData.yearsOfExperience ? Number(formData.yearsOfExperience) : null,
        gender: formData.gender || null,
        dateOfBirth: formData.dateOfBirth || null,
        address: formData.address.trim() || null,
        startDate: formData.startDate || null,
        servicesHandled: formData.servicesHandled.trim() || null,
        certificateIssuedDate: formData.certificateIssuedDate || null,
        certificateIssuedBy: formData.certificateIssuedBy.trim() || null,
        education: formData.education.trim() || null,
        bio: formData.bio.trim() || null,
        position: null,
        employmentType: formEmploymentType,
        baseSalary: formBaseSalary,
        salaryUnit: formSalaryUnit,
        leaveAccrued: formLeaveAccrued,
        allowance: formAllowance,
      };
      await updateStaffApi(id, payload);
      sessionStorage.setItem("staffSuccessMsg", `Cập nhật thông tin nha sĩ ${formData.fullName.trim()} thành công!`);
      router.push("/owner/employee");
    } catch (err: unknown) {
      if (err instanceof ApiValidationError) {
        const valErr = err;
        const mappedErrors: Record<string, string> = {};
        Object.entries(valErr.errors).forEach(([field, msgs]) => {
          mappedErrors[field] = msgs[0];
        });
        setErrors(mappedErrors);
      } else {
        const errMsg = err instanceof Error ? err.message : "Đã xảy ra lỗi không xác định";
        if (errMsg.toLowerCase().includes("email")) {
          setErrors({ email: errMsg });
        } else {
          setErrors({ api: errMsg });
        }
      }
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
        <OwnerSidebar activeMenu="staff" />
        <main className="flex-1 flex flex-col items-center justify-center gap-4">
          <svg className="w-14 h-14 text-slate-300 animate-pulse" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
          </svg>
          <p className="text-slate-500 font-bold text-[15px]">Không tìm thấy dữ liệu nha sĩ.</p>
          <button onClick={() => router.push("/owner/employee")} className="px-6 py-2.5 bg-primary hover:bg-primary-hover text-white font-extrabold rounded-xl transition-all cursor-pointer shadow-md">
            Quay lại danh sách
          </button>
        </main>
      </div>
    );
  }

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <OwnerSidebar activeMenu="staff" />

      <main className="flex-1 flex flex-col min-w-0">
        <OwnerPageHeader
          left={
            <button onClick={() => router.push("/owner/employee")} className="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-all cursor-pointer">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
              </svg>
            </button>
          }
          title="Chỉnh Sửa Hồ Sơ Nha Sĩ"
          subtitle={
            <>
              {staff.employeeId && <span className="font-mono text-primary font-bold mr-2">{staff.employeeId}</span>}
              {staff.fullName || staff.email}
            </>
          }
        />

        <div className="flex-1 p-8 flex justify-center overflow-y-auto">
          <div className="w-full max-w-5xl">

            {errors.api && (
              <div className="mb-6 bg-red-50 border border-red-200 text-red-700 p-4 rounded-2xl text-[13.5px] font-bold flex items-start gap-2.5">
                <svg className="w-5 h-5 text-red-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
                {errors.api}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-6 w-full">
              {/* Form Header Action Bar */}
              <div className="flex items-center justify-between border-b border-slate-200 pb-4 mb-2 shrink-0">
                <div>
                  <h3 className="text-lg font-extrabold text-slate-900 tracking-tight">Chỉnh sửa hồ sơ Nha sĩ</h3>
                  <p className="text-[13px] text-slate-400 font-semibold mt-1">
                    Cập nhật các thông tin của nha sĩ và bấm lưu để cập nhật hồ sơ.
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => router.push("/owner/employee")}
                    disabled={isSubmitting}
                    className="px-4 py-2 bg-white border border-slate-250 text-slate-600 rounded-xl text-[13px] font-bold transition-all hover:bg-slate-50 cursor-pointer shadow-sm disabled:opacity-50"
                  >
                    Hủy bỏ
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting}
                    className="px-5 py-2 bg-primary hover:bg-primary-hover text-white rounded-xl text-[13px] font-bold shadow-md shadow-primary/15 hover:shadow-lg transition-all cursor-pointer flex items-center gap-2 disabled:opacity-50"
                  >
                    {isSubmitting ? "Đang lưu..." : "Lưu thay đổi"}
                  </button>
                </div>
              </div>

              {/* CARD 1: THÔNG TIN CƠ BẢN */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6">
                <div className="flex items-center justify-between mb-6 pb-2 border-b border-slate-100">
                  <h4 className="text-sm font-extrabold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                    <span className="w-1.5 h-3.5 bg-primary rounded-full inline-block" />
                    Thông tin cơ bản
                  </h4>
                  {staff.employeeId && (
                    <div className="text-[12px] font-bold text-slate-500">
                      Mã nha sĩ: <span className="bg-red-50 border border-red-100 text-primary px-2.5 py-1 rounded font-mono font-extrabold">{staff.employeeId}</span>
                    </div>
                  )}
                </div>

                <div className="flex flex-col md:flex-row gap-6">
                  {/* Left side: Avatar Box */}
                  <div className="flex flex-col items-center gap-3 shrink-0">
                    <div className="w-32 h-32 rounded-2xl border-2 border-dashed border-slate-250 hover:border-primary bg-slate-50/70 hover:bg-red-50/10 flex flex-col items-center justify-center text-center cursor-pointer transition-all p-3 relative overflow-hidden group">
                      {formData.profilePictureUrl ? (
                        <>
                          <img src={resolveAssetUrl(formData.profilePictureUrl)} alt="avatar" className="absolute inset-0 w-full h-full object-cover" />
                          <div className="absolute inset-0 bg-slate-900/60 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                            <span className="text-white text-[11px] font-bold">Thay đổi ảnh</span>
                          </div>
                        </>
                      ) : (
                        <>
                          <svg className="w-8 h-8 text-slate-400 group-hover:text-primary transition-colors mb-1.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6.827 6.175A2.31 2.31 0 015.186 7.23c-.38.054-.757.112-1.134.175C2.999 7.58 2.25 8.507 2.25 9.574V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9.574c0-1.067-.75-1.994-1.802-2.169a47.865 47.865 0 00-1.134-.175 2.31 2.31 0 01-1.64-1.055l-.822-1.316a2.192 2.192 0 00-1.736-1.039 48.774 48.774 0 00-5.232 0 2.192 2.192 0 00-1.736 1.039l-.821 1.316z" />
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 12.75a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0zM18.75 10.5h.008v.008h-.008V10.5z" />
                          </svg>
                          <span className="text-[11px] font-bold text-slate-400 group-hover:text-primary transition-colors">Tải ảnh đại diện</span>
                        </>
                      )}
                      <input 
                        type="file" 
                        accept="image/*" 
                        className="absolute inset-0 opacity-0 cursor-pointer" 
                        onChange={handleImageUpload}
                        disabled={isUploadingImage}
                      />
                    </div>
                    <span className="text-[10px] text-slate-400 font-bold">JPG, PNG, WEBP (Max 5MB)</span>
                    {formData.profilePictureUrl && (
                      <button 
                        type="button" 
                        onClick={() => setFormData(prev => ({ ...prev, profilePictureUrl: "" }))} 
                        className="text-[11px] text-primary hover:underline font-bold"
                      >
                        Xóa ảnh
                      </button>
                    )}
                  </div>

                  {/* Right side: Input Grid */}
                  <div className="flex-1 grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div className="sm:col-span-2">
                      <label className={lbl}>Họ và tên *</label>
                      <input
                        type="text"
                        name="fullName"
                        required
                        placeholder="Nguyễn Văn A"
                        value={formData.fullName}
                        onChange={handleChange}
                        className={inp("fullName")}
                      />
                      {errMsg("fullName")}
                    </div>

                    <div>
                      <label className={lbl}>Giới tính *</label>
                      <select
                        name="gender"
                        value={formData.gender}
                        onChange={handleChange}
                        className={inp("gender")}
                      >
                        <option value="">-- Chọn giới tính --</option>
                        <option value="Nam">Nam</option>
                        <option value="Nữ">Nữ</option>
                        <option value="Khác">Khác</option>
                      </select>
                      {errMsg("gender")}
                    </div>

                    <div>
                      <label className={lbl}>Ngày sinh *</label>
                      <input
                        type="date"
                        name="dateOfBirth"
                        required
                        value={formData.dateOfBirth}
                        onChange={handleChange}
                        className={inp("dateOfBirth")}
                      />
                      {errMsg("dateOfBirth")}
                    </div>

                    <div>
                      <label className={lbl}>Số điện thoại *</label>
                      <input
                        type="tel"
                        name="phoneNumber"
                        required
                        placeholder="0987654321"
                        value={formData.phoneNumber}
                        onChange={handleChange}
                        className={inp("phoneNumber")}
                      />
                      {errMsg("phoneNumber")}
                    </div>

                    <div>
                      <label className={lbl}>Email *</label>
                      <input
                        type="email"
                        name="email"
                        required
                        placeholder="bacsi@dentalclinic.com"
                        value={formData.email}
                        onChange={handleChange}
                        className={inp("email")}
                      />
                      {errMsg("email")}
                    </div>

                    <div className="sm:col-span-2">
                      <label className={lbl}>Địa chỉ</label>
                      <input
                        type="text"
                        name="address"
                        placeholder="123 Nguyễn Trãi, Quận 1, TP.HCM"
                        value={formData.address}
                        onChange={handleChange}
                        className={inp("address")}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* CARD 2: HỒ SƠ CHUYÊN MÔN & KINH NGHIỆM */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6">
                <h4 className="text-sm font-extrabold text-slate-900 uppercase tracking-wider mb-6 pb-2 border-b border-slate-100 flex items-center gap-2">
                  <span className="w-1.5 h-3.5 bg-primary rounded-full inline-block" />
                  Hồ sơ chuyên môn & kinh nghiệm
                </h4>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className={lbl}>Chuyên khoa *</label>
                    <select
                      name="specialty"
                      value={formData.specialty}
                      onChange={handleChange}
                      className={inp("specialty")}
                    >
                      <option value="">-- Chọn chuyên khoa --</option>
                      <option value="Răng Hàm Mặt">Răng Hàm Mặt</option>
                      <option value="Nha chu">Nha chu</option>
                      <option value="Chỉnh nha / Niềng răng">Chỉnh nha / Niềng răng</option>
                      <option value="Phục hình răng / Implant">Phục hình răng / Implant</option>
                      <option value="Nha khoa thẩm mỹ">Nha khoa thẩm mỹ</option>
                    </select>
                    {errMsg("specialty")}
                  </div>

                  <div>
                    <label className={lbl}>Số chứng chỉ hành nghề (CCHN) *</label>
                    <input
                      type="text"
                      name="licenseNumber"
                      required
                      placeholder="VD: 123456/BYT-GCN"
                      value={formData.licenseNumber}
                      onChange={handleChange}
                      className={inp("licenseNumber")}
                    />
                    {errMsg("licenseNumber")}
                  </div>

                  <div>
                    <label className={lbl}>Ngày cấp chứng chỉ (CCHN)</label>
                    <input
                      type="date"
                      name="certificateIssuedDate"
                      value={formData.certificateIssuedDate}
                      onChange={handleChange}
                      className={inp("certificateIssuedDate")}
                    />
                    {errMsg("certificateIssuedDate")}
                  </div>

                  <div>
                    <label className={lbl}>Nơi cấp chứng chỉ (CCHN)</label>
                    <input
                      type="text"
                      name="certificateIssuedBy"
                      placeholder="Bộ Y tế, Sở Y tế TP.HCM..."
                      value={formData.certificateIssuedBy}
                      onChange={handleChange}
                      className={inp("certificateIssuedBy")}
                    />
                    {errMsg("certificateIssuedBy")}
                  </div>

                  <div className="sm:col-span-2">
                    <label className={lbl}>Dịch vụ phụ trách *</label>
                    <div className="flex flex-wrap gap-2.5">
                      {["Nhổ răng", "Lấy cao răng", "Cấy Implant", "Niềng răng mắc cài", "Niềng răng trong suốt Invisalign"].map((srv) => {
                        const currentServices = (formData.servicesHandled || "").split(",").map(s => s.trim()).filter(Boolean);
                        const isSelected = currentServices.includes(srv);
                        return (
                          <button
                            key={srv}
                            type="button"
                            onClick={() => {
                              let newVal = "";
                              if (isSelected) {
                                newVal = currentServices.filter(x => x !== srv).join(", ");
                              } else {
                                newVal = [...currentServices, srv].join(", ");
                              }
                              setFormData(prev => ({ ...prev, servicesHandled: newVal }));
                              if (errors.servicesHandled) setErrors(prev => ({ ...prev, servicesHandled: "" }));
                            }}
                            className={`flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-bold border transition-all cursor-pointer ${
                              isSelected 
                                ? "bg-red-50 border-primary text-primary shadow-sm"
                                : "bg-white border-slate-200 text-slate-500 hover:bg-slate-50"
                            }`}
                          >
                            <span className={`w-4.5 h-4.5 rounded border flex items-center justify-center shrink-0 transition-colors ${
                              isSelected ? "bg-primary border-primary text-white" : "border-slate-300 bg-white"
                            }`}>
                              {isSelected && (
                                <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                                </svg>
                              )}
                            </span>
                            {srv}
                          </button>
                        );
                      })}
                    </div>
                    {errMsg("servicesHandled")}
                  </div>

                  <div>
                    <label className={lbl}>Ngày bắt đầu làm việc *</label>
                    <input
                      type="date"
                      name="startDate"
                      required
                      value={formData.startDate}
                      onChange={handleChange}
                      className={inp("startDate")}
                    />
                    {errMsg("startDate")}
                  </div>

                  <div>
                    <label className={lbl}>Số năm kinh nghiệm</label>
                    <input
                      type="number"
                      name="yearsOfExperience"
                      placeholder="VD: 5"
                      min="0"
                      max="60"
                      value={formData.yearsOfExperience}
                      onChange={handleChange}
                      className={inp("yearsOfExperience")}
                    />
                    {errMsg("yearsOfExperience")}
                  </div>

                  <div>
                    <label className={lbl}>Trình độ học vấn</label>
                    <input
                      type="text"
                      name="education"
                      placeholder="Thạc sĩ Y khoa..."
                      value={formData.education}
                      onChange={handleChange}
                      className={inp("education")}
                    />
                    {errMsg("education")}
                  </div>

                  <div className="sm:col-span-2">
                    <label className={lbl}>Giới thiệu bản thân</label>
                    <textarea
                      name="bio"
                      rows={4}
                      placeholder="Mô tả tóm tắt kinh nghiệm, chuyên môn và quá trình làm việc của nha sĩ..."
                      value={formData.bio}
                      onChange={handleChange}
                      className={`w-full px-4 py-2.5 border rounded-xl focus:outline-none focus:ring-1 focus:ring-primary transition-all font-semibold text-[14px] resize-none ${
                        errors.bio ? "border-red-400 bg-red-50/30" : "bg-slate-50 border-slate-200 focus:bg-white"
                      }`}
                    />
                    {errMsg("bio")}
                  </div>
                </div>
              </div>

              {/* CARD 3: CẤU HÌNH LƯƠNG & NGHỈ PHÉP */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6">
                <h4 className="text-sm font-extrabold text-slate-900 uppercase tracking-wider mb-6 pb-2 border-b border-slate-100 flex items-center gap-2">
                  <span className="w-1.5 h-3.5 bg-primary rounded-full inline-block" />
                  Cấu hình lương & nghỉ phép
                </h4>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  {/* Row 1 */}
                  <div>
                    <label className={lbl}>Hình thức làm việc *</label>
                    <select
                      value={formEmploymentType}
                      onChange={(e) => setFormEmploymentType(e.target.value)}
                      className={inp("employmentType")}
                    >
                      <option value="Full-time">Full time (Toàn thời gian)</option>
                      <option value="Part-time">Part time (Bán thời gian)</option>
                      <option value="Shift-based">Theo ca (Shift-based)</option>
                    </select>
                    {errMsg("employmentType")}
                  </div>

                  <div>
                    <label className={lbl}>Mức lương cơ bản * (VNĐ)</label>
                    <input
                      type="number"
                      required
                      min="0"
                      placeholder="25.000.000"
                      value={formBaseSalary}
                      onChange={(e) => setFormBaseSalary(Number(e.target.value))}
                      className={inp("baseSalary")}
                    />
                    {errMsg("baseSalary")}
                  </div>

                  <div>
                    <label className={lbl}>Phụ cấp * (VNĐ)</label>
                    <input
                      type="number"
                      required
                      min="0"
                      placeholder="2.500.000"
                      value={formAllowance}
                      onChange={(e) => setFormAllowance(Number(e.target.value))}
                      className={inp("allowance")}
                    />
                    {errMsg("allowance")}
                  </div>

                  {/* Row 2 */}
                  <div>
                    <label className={lbl}>Đơn vị tính lương *</label>
                    <select
                      value={formSalaryUnit}
                      onChange={(e) => setFormSalaryUnit(e.target.value)}
                      disabled={formEmploymentType === "Full-time"}
                      className={`${inp("salaryUnit")} ${
                        formEmploymentType === "Full-time" ? "opacity-60 cursor-not-allowed bg-slate-200 text-slate-500 font-bold text-[14px]" : ""
                      }`}
                    >
                      <option value="Theo tháng">Theo tháng</option>
                      <option value="Theo ngày">Theo ngày</option>
                      <option value="Theo ca">Theo ca</option>
                      <option value="Theo giờ">Theo giờ</option>
                    </select>
                    {errMsg("salaryUnit")}
                  </div>

                  <div>
                    <label className={lbl}>Số ngày phép / tháng</label>
                    <input
                      type="number"
                      required
                      min="0"
                      step="0.5"
                      value={formLeaveAccrued}
                      onChange={(e) => setFormLeaveAccrued(Number(e.target.value))}
                      disabled={formEmploymentType !== "Full-time"}
                      className={`${inp("leaveAccrued")} ${
                        formEmploymentType !== "Full-time" ? "opacity-60 cursor-not-allowed bg-slate-100 font-semibold text-[14px]" : ""
                      }`}
                    />
                    {errMsg("leaveAccrued")}
                  </div>
                </div>
              </div>

              {/* CARD 4: TRẠNG THÁI TÀI KHOẢN & CÔNG TÁC */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6">
                <h4 className="text-sm font-extrabold text-slate-900 uppercase tracking-wider mb-6 pb-2 border-b border-slate-100 flex items-center gap-2">
                  <span className="w-1.5 h-3.5 bg-primary rounded-full inline-block" />
                  Vai trò & Trạng thái hoạt động
                </h4>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-6 items-end">
                  <div>
                    <label className={lbl}>Vai trò</label>
                    <div className="flex items-center gap-2.5 px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl">
                      <span className="text-[14px] font-bold text-slate-600">Nha sĩ</span>
                      <span className="text-[10px] bg-slate-200/70 text-slate-500 px-2 py-0.5 rounded font-bold uppercase tracking-wide">Cố định</span>
                    </div>
                  </div>

                  <div>
                    <label className={lbl}>Trạng thái làm việc</label>
                    <select
                      name="employmentStatus"
                      value={formData.employmentStatus}
                      onChange={handleChange}
                      className={inp("employmentStatus")}
                    >
                      <option value="Active">Đang làm việc (Active)</option>
                      <option value="On Leave">Nghỉ phép (On Leave)</option>
                      <option value="Inactive">Đã nghỉ việc (Inactive)</option>
                    </select>
                  </div>

                  <div>
                    <label className="flex items-center gap-3 cursor-pointer select-none p-3.5 bg-slate-50 border border-slate-200 rounded-xl hover:bg-slate-100 transition-colors">
                      <input
                        type="checkbox"
                        checked={formData.isActive}
                        onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                        className="w-5 h-5 rounded text-primary border-slate-355 focus:ring-primary"
                      />
                      <span className="text-[13px] font-bold text-slate-700">Tài khoản hoạt động</span>
                    </label>
                  </div>
                </div>
              </div>

              {/* Bottom Actions */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 shrink-0">
                <button
                  type="button"
                  onClick={() => router.push("/owner/employee")}
                  disabled={isSubmitting}
                  className="px-6 py-2.5 bg-white border border-slate-250 text-slate-600 rounded-xl text-[14px] font-bold transition-all hover:bg-slate-50 cursor-pointer shadow-sm disabled:opacity-50"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-8 py-2.5 bg-primary hover:bg-primary-hover text-white rounded-xl text-[14px] font-bold shadow-md shadow-primary/15 hover:shadow-lg transition-all cursor-pointer flex items-center gap-2 disabled:opacity-50"
                >
                  {isSubmitting ? "Đang lưu..." : "Lưu hồ sơ nha sĩ"}
                </button>
              </div>
            </form>
          </div>
        </div>
      </main>
    </div>
  );
}
