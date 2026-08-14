"use client";

import React, { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import OwnerSidebar from "../../../../../components/shared/OwnerSidebar";
import OwnerPageHeader from "../../../../../components/shared/OwnerPageHeader";
import { useRequireOwner } from "../../../../../hooks/useRequireOwner";
import { updateStaffApi, getStaffByIdApi, uploadFileApi, resolveAssetUrl, ApiValidationError, type StaffDto, type UpdateStaffCommand } from "../../../../../lib/apiClient";

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

const fmtDateInput = (d?: string | null) => (d ? d.split("T")[0] : "");

export default function EditStaffPage() {
  useRequireOwner();
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

  const [formEmploymentType, setFormEmploymentType] = useState("Full-time");
  const [formBaseSalary, setFormBaseSalary] = useState(12000000);
  const [formSalaryUnit, setFormSalaryUnit] = useState("Theo tháng");
  const [formLeaveAccrued, setFormLeaveAccrued] = useState(1);
  const [formAllowance, setFormAllowance] = useState(1200000);

  useEffect(() => {
    if (formEmploymentType === "Full-time") {
      setFormSalaryUnit("Theo tháng");
    }
  }, [formEmploymentType]);

  const populateData = (data: StaffDto) => {
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
      dateOfBirth: fmtDateInput(data.dateOfBirth),
      phoneNumber: data.phoneNumber || "",
      email: data.email || "",
      address: data.address || "",
      profilePictureUrl: data.profilePictureUrl || "",
      position: data.position || "",
      department: data.department || "",
      startDate: fmtDateInput(data.startDate),
      bio: data.bio || "",
      role: data.role,
      employmentStatus: data.employmentStatus || "Active",
      isActive: data.isActive,
    });
  };

  useEffect(() => {
    const raw = sessionStorage.getItem("staffEditData");
    if (raw) {
      try {
        const data: StaffDto = JSON.parse(raw);
        populateData(data);
        return;
      } catch {
        // Fallback to fetching via API
      }
    }
    if (id) {
      getStaffByIdApi(id)
        .then((data) => populateData(data))
        .catch((err) => {
          setErrors({ api: err instanceof Error ? err.message : "Không thể tải thông tin nhân viên" });
        });
    }
  }, [id]);

  const scrollToError = (errObj: Record<string, string>) => {
    const keys = Object.keys(errObj).filter((k) => k !== "api");
    if (keys.length === 0) {
      window.scrollTo({ top: 0, behavior: "smooth" });
      return;
    }
    const firstKey = keys[0];
    const target = document.querySelector(`[name="${firstKey}"]`) || document.getElementById(`field-${firstKey}`);
    if (target) {
      target.scrollIntoView({ behavior: "smooth", block: "center" });
      (target as HTMLElement).focus?.();
    } else {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

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
    if (Object.keys(e).length > 0) {
      scrollToError(e);
    }
    return Object.keys(e).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) setErrors((prev) => ({ ...prev, [name]: "" }));
  };

  const handleDepartmentChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const dept = e.target.value;
    let pos = "";
    if (dept === "Phụ tá lâm sàng") {
      pos = "Trợ lý nha khoa";
    }
    setFormData((prev) => ({
      ...prev,
      department: dept,
      position: pos,
    }));
    if (errors.department) setErrors((prev) => ({ ...prev, department: "" }));
    if (errors.position) setErrors((prev) => ({ ...prev, position: "" }));
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
        employmentType: formEmploymentType,
        baseSalary: formBaseSalary,
        salaryUnit: formSalaryUnit,
        leaveAccrued: formLeaveAccrued,
        allowance: formAllowance,
      };
      await updateStaffApi(id, payload);
      sessionStorage.setItem("staffSuccessMsg", `Cập nhật thông tin nhân viên ${formData.fullName.trim()} thành công!`);
      router.push("/owner/employee");
    } catch (err: unknown) {
      const mappedErrors: Record<string, string> = {};
      if (err instanceof ApiValidationError) {
        Object.entries(err.errors).forEach(([field, msgs]) => {
          mappedErrors[field] = msgs[0];
        });
      } else {
        const errMsg = err instanceof Error ? err.message : "Đã xảy ra lỗi không xác định";
        if (errMsg.toLowerCase().includes("email")) {
          mappedErrors.email = errMsg;
        } else {
          mappedErrors.api = errMsg;
        }
      }
      setErrors(mappedErrors);
      scrollToError(mappedErrors);
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
          <p className="text-slate-500 font-bold text-[15px]">Không tìm thấy dữ liệu nhân viên.</p>
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
          title="Chỉnh Sửa Nhân Viên"
          subtitle={
            <>
              {staff.employeeId && <span className="font-mono text-primary font-bold mr-2">{staff.employeeId}</span>}
              {staff.fullName || staff.email}
            </>
          }
        />

        <div className="flex-1 p-8 flex justify-center overflow-y-auto">
          <div className="w-full max-w-5xl">

            {Object.keys(errors).length > 0 && (
              <div className="mb-6 bg-red-50 border border-red-200 text-red-700 p-4 rounded-2xl text-[13.5px] font-bold flex items-start gap-2.5 shadow-sm animate-fade-in">
                <svg className="w-5 h-5 text-red-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
                <div className="flex-1">
                  <p className="font-extrabold text-red-800">Thông tin chưa hợp lệ hoặc còn thiếu ({Object.keys(errors).filter(k => k !== 'api').length} trường):</p>
                  <ul className="list-disc list-inside mt-1 font-semibold text-[13px] text-red-700 space-y-0.5">
                    {Object.entries(errors).map(([key, msg]) => (
                      key !== "api" && <li key={key}>{msg}</li>
                    ))}
                    {errors.api && <li>{errors.api}</li>}
                  </ul>
                </div>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-6 w-full">
              {/* Form Header Action Bar */}
              <div className="flex items-center justify-between border-b border-slate-200 pb-4 mb-2 shrink-0">
                <div>
                  <h3 className="text-lg font-extrabold text-slate-900 tracking-tight">Chỉnh sửa hồ sơ Nhân viên</h3>
                  <p className="text-[13px] text-slate-400 font-semibold mt-1">
                    Cập nhật các thông tin của nhân viên và bấm lưu để cập nhật hồ sơ.
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
                      Mã nhân viên: <span className="bg-red-50 border border-red-100 text-primary px-2.5 py-1 rounded font-mono font-extrabold">{staff.employeeId}</span>
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
                        placeholder="nhanvien@dentalclinic.com"
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
                      {errMsg("address")}
                    </div>
                  </div>
                </div>
              </div>

              {/* CARD 2: THÔNG TIN CÔNG VIỆC */}
              <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6">
                <h4 className="text-sm font-extrabold text-slate-900 uppercase tracking-wider mb-6 pb-2 border-b border-slate-100 flex items-center gap-2">
                  <span className="w-1.5 h-3.5 bg-primary rounded-full inline-block" />
                  Thông tin công việc
                </h4>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className={lbl}>Bộ phận *</label>
                    <select
                      name="department"
                      value={formData.department}
                      onChange={handleDepartmentChange}
                      className={inp("department")}
                    >
                      <option value="">-- Chọn bộ phận --</option>
                      <option value="Đón tiếp & CSKH">Đón tiếp & CSKH</option>
                      <option value="Phụ tá lâm sàng">Phụ tá lâm sàng</option>
                      {formData.department && !["Đón tiếp & CSKH", "Phụ tá lâm sàng"].includes(formData.department) && (
                        <option value={formData.department}>{formData.department}</option>
                      )}
                    </select>
                    {errMsg("department")}
                  </div>

                  <div>
                    <label className={lbl}>Chức vụ *</label>
                    <select
                      name="position"
                      value={formData.position}
                      onChange={handleChange}
                      disabled={!formData.department}
                      className={inp("position")}
                    >
                      <option value="">
                        {formData.department ? "-- Chọn chức vụ --" : "-- Vui lòng chọn bộ phận trước --"}
                      </option>
                      {formData.department === "Phụ tá lâm sàng" ? (
                        <option value="Trợ lý nha khoa">Trợ lý nha khoa</option>
                      ) : (
                        formData.department === "Đón tiếp & CSKH" ? (
                          <>
                            <option value="Lễ tân">Lễ tân</option>
                            <option value="Nhân viên CSKH">Nhân viên CSKH</option>
                          </>
                        ) : (
                          formData.position && (
                            <option value={formData.position}>{formData.position}</option>
                          )
                        )
                      )}
                    </select>
                    {errMsg("position")}
                  </div>

                  <div className="sm:col-span-2">
                    <label className={lbl}>Ngày vào làm *</label>
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

                  <div className="sm:col-span-2">
                    <label className={lbl}>Mô tả công việc</label>
                    <textarea
                      name="bio"
                      rows={4}
                      placeholder="Mô tả nhiệm vụ, trách nhiệm và công việc cụ thể của nhân viên..."
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
                      placeholder="12.000.000"
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
                      placeholder="1.200.000"
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
                    <select
                      name="role"
                      value={formData.role}
                      onChange={handleChange}
                      className={inp("role")}
                    >
                      <option value="Staff">Nhân viên / Trợ lý</option>
                      <option value="Admin">Quản trị viên</option>
                    </select>
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
                  {isSubmitting ? "Đang lưu..." : "Lưu hồ sơ nhân viên"}
                </button>
              </div>
            </form>
          </div>
        </div>
      </main>
    </div>
  );
}
