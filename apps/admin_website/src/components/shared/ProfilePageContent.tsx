"use client";

import React, { useState, useEffect, useRef } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import {
  getMyProfileApi,
  updateMyProfileApi,
  changePasswordApi,
  uploadFileApi,
  type UserProfileDto
} from "../../lib/apiClient";

interface ProfilePageContentProps {
  sidebar: React.ReactNode;
}

type TabType = "personal" | "password" | "activities";

interface SimulatedLog {
  id: string;
  time: string;
  action: string;
  module: string;
  ip: string;
  status: "success" | "warning";
}

const formatDate = (dateStr: string | null | undefined) => {
  if (!dateStr) return "N/A";
  try {
    const [year, month, day] = dateStr.split("-");
    return `${day}/${month}/${year}`;
  } catch {
    return dateStr;
  }
};

export default function ProfilePageContent({ sidebar }: ProfilePageContentProps) {
  const searchParams = useSearchParams();
  const router = useRouter();

  // Tab state controlled strictly by URL tab parameter
  const tabParam = searchParams.get("tab") as TabType | null;
  const [activeTab, setActiveTab] = useState<TabType>("personal");

  useEffect(() => {
    if (tabParam && ["personal", "password", "activities"].includes(tabParam)) {
      setActiveTab(tabParam);
    }
  }, [tabParam]);

  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [saving, setSaving] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Validation and Toast states
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" | "info" } | null>(null);

  const showToast = (message: string, type: "success" | "error" | "info" = "success") => {
    setToast({ message, type });
  };

  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 4000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  // Clear field errors on tab switch
  useEffect(() => {
    setFieldErrors({});
  }, [activeTab]);

  // Form states (Personal Profile)
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [gender, setGender] = useState("Nam");
  const [address, setAddress] = useState("");
  const [bio, setBio] = useState("");
  const [education, setEducation] = useState("");
  const [profilePictureUrl, setProfilePictureUrl] = useState<string | null>(null);

  // Dentist-specific editable states
  const [specialty, setSpecialty] = useState("");
  const [yearsOfExperience, setYearsOfExperience] = useState<number>(0);

  // Password states
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  // Simulated activity logs
  const [simulatedLogs, setSimulatedLogs] = useState<SimulatedLog[]>([]);

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    fetchProfile();
  }, []);

  const fetchProfile = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyProfileApi();
      setProfile(data);

      // Populate form
      setFullName(data.fullName || "");
      setPhoneNumber(data.phoneNumber || "");
      setDateOfBirth(data.dateOfBirth || "");
      setGender(data.gender || "Nam");
      setAddress(data.address || "");
      setBio(data.bio || "");
      setEducation(data.education || "");
      setProfilePictureUrl(data.profilePictureUrl);

      if (data.role === "Dentist") {
        setSpecialty(data.specialty || "");
        setYearsOfExperience(data.yearsOfExperience || 0);
      }

      generateSimulatedLogs(data.fullName || "Người dùng", data.role);
    } catch (err: any) {
      setError(err.message || "Không thể tải thông tin cá nhân.");
    } finally {
      setLoading(false);
    }
  };

  const generateSimulatedLogs = (name: string, role: string) => {
    let logs: SimulatedLog[] = [];

    if (role === "Admin") {
      logs = [
        {
          id: "ACT-001",
          time: "Hôm nay, 16:15:22",
          action: "Đăng nhập hệ thống quản trị thành công (Chrome/Windows)",
          module: "Hệ thống",
          ip: "192.168.1.15",
          status: "success",
        },
        {
          id: "ACT-002",
          time: "Hôm nay, 14:30:10",
          action: "Tạo tài khoản mới cho bác sĩ Nguyễn Quốc Anh (Bác sĩ Nha khoa)",
          module: "Tài khoản",
          ip: "192.168.1.15",
          status: "success",
        },
        {
          id: "ACT-003",
          time: "Hôm qua, 11:20:45",
          action: "Cập nhật giá dịch vụ 'Cấy ghép răng Implant Nobel' thành 16.500.000đ",
          module: "Dịch vụ",
          ip: "192.168.1.15",
          status: "success",
        },
        {
          id: "ACT-004",
          time: "Hôm qua, 09:15:00",
          action: "Phê duyệt đơn xin nghỉ phép của nhân viên CSKH Lê Thị Hồng",
          module: "Nhân sự",
          ip: "192.168.1.15",
          status: "success",
        },
        {
          id: "ACT-005",
          time: "2 ngày trước, 16:45:00",
          action: "Đăng nhập hệ thống từ địa chỉ lạ (Cảnh báo bảo mật)",
          module: "Hệ thống",
          ip: "103.45.67.89",
          status: "warning",
        },
        {
          id: "ACT-006",
          time: "3 ngày trước, 10:10:30",
          action: "Thay đổi phân quyền truy cập phòng chức năng",
          module: "Bảo mật",
          ip: "192.168.1.10",
          status: "success",
        },
        {
          id: "ACT-007",
          time: "5 ngày trước, 08:30:00",
          action: "Xuất file báo cáo doanh thu phòng khám quý 2/2026",
          module: "Hệ thống",
          ip: "192.168.1.10",
          status: "success",
        }
      ];
    } else if (role === "Dentist") {
      logs = [
        {
          id: "ACT-001",
          time: "Hôm nay, 15:20:10",
          action: "Đăng nhập tài khoản Bác sĩ thành công",
          module: "Hệ thống",
          ip: "192.168.1.25",
          status: "success",
        },
        {
          id: "ACT-002",
          time: "Hôm nay, 14:05:32",
          action: "Cập nhật bệnh án điều trị cho bệnh nhân Nguyễn Văn Nam",
          module: "Bệnh án",
          ip: "192.168.1.25",
          status: "success",
        },
        {
          id: "ACT-003",
          time: "Hôm nay, 10:45:15",
          action: "Kê đơn thuốc điều trị sau nhổ răng cho bệnh nhân Lê Hoài An",
          module: "Đơn thuốc",
          ip: "192.168.1.25",
          status: "success",
        },
        {
          id: "ACT-004",
          time: "Hôm qua, 15:30:22",
          action: "Chỉ định chụp X-Quang răng toàn cảnh cho bệnh nhân Vũ Thị Mai",
          module: "Chỉ định",
          ip: "192.168.1.25",
          status: "success",
        },
        {
          id: "ACT-005",
          time: "Hôm qua, 08:45:00",
          action: "Xác nhận lịch hẹn tái khám niềng răng Invisalign",
          module: "Lịch hẹn",
          ip: "192.168.1.25",
          status: "success",
        }
      ];
    } else {
      logs = [
        {
          id: "ACT-001",
          time: "Hôm nay, 16:02:11",
          action: "Đăng nhập tài khoản Nhân sự thành công",
          module: "Hệ thống",
          ip: "192.168.1.30",
          status: "success",
        },
        {
          id: "ACT-002",
          time: "Hôm nay, 15:45:30",
          action: "Tạo hóa đơn thanh toán khám răng cho bệnh nhân Nguyễn Thị Lan",
          module: "Hóa đơn",
          ip: "192.168.1.30",
          status: "success",
        },
        {
          id: "ACT-003",
          time: "Hôm nay, 11:15:00",
          action: "Xác nhận đặt lịch hẹn mới cho khách hàng Trần Văn Đức",
          module: "Lịch hẹn",
          ip: "192.168.1.30",
          status: "success",
        },
        {
          id: "ACT-004",
          time: "Hôm qua, 07:55:00",
          action: "Check-in thành công ca làm việc sáng Thứ Ba",
          module: "Chuyên cần",
          ip: "192.168.1.30",
          status: "success",
        },
        {
          id: "ACT-005",
          time: "2 ngày trước, 14:22:18",
          action: "Ghi nhận phản hồi và đánh giá dịch vụ từ khách hàng",
          module: "CSKH",
          ip: "192.168.1.30",
          status: "success",
        }
      ];
    }

    setSimulatedLogs(logs);
  };

  const handleAvatarClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      setSaving(true);
      setError(null);
      const uploadRes = await uploadFileApi(file);
      setProfilePictureUrl(uploadRes.url);

      const updatedCommand = {
        fullName,
        phoneNumber,
        dateOfBirth: dateOfBirth || null,
        gender,
        address: address || null,
        profilePictureUrl: uploadRes.url,
        bio: bio || null,
        education: education || null,
        ...(profile?.role === "Dentist" && {
          specialty: specialty || null,
          yearsOfExperience: Number(yearsOfExperience) || 0
        })
      };
      await updateMyProfileApi(updatedCommand);

      showToast("Tải và cập nhật ảnh đại diện thành công!", "success");
    } catch (err: any) {
      showToast(err.message || "Không thể tải ảnh đại diện lên.", "error");
    } finally {
      setSaving(false);
    }
  };

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const errors: Record<string, string> = {};
    if (!fullName.trim()) {
      errors.fullName = "Họ và tên không được để trống.";
    }
    if (!phoneNumber.trim()) {
      errors.phoneNumber = "Số điện thoại không được để trống.";
    } else if (!/^[0-9]{10,11}$/.test(phoneNumber.trim())) {
      errors.phoneNumber = "Số điện thoại phải gồm 10-11 chữ số.";
    }
    
    if (dateOfBirth) {
      const dobDate = new Date(dateOfBirth);
      const today = new Date();
      if (dobDate > today) {
        errors.dateOfBirth = "Ngày sinh không thể ở tương lai.";
      }
    }

    if (profile?.role === "Dentist" && Number(yearsOfExperience) < 0) {
      errors.yearsOfExperience = "Số năm kinh nghiệm không được nhỏ hơn 0.";
    }

    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      showToast("Vui lòng kiểm tra lại thông tin nhập vào.", "error");
      return;
    }
    setFieldErrors({});

    try {
      setSaving(true);
      setError(null);

      const command: any = {
        fullName,
        phoneNumber,
        dateOfBirth: dateOfBirth || null,
        gender,
        address: address || null,
        profilePictureUrl: profilePictureUrl || null,
        bio: bio || null,
        education: education || null,
      };

      if (profile?.role === "Dentist") {
        command.specialty = specialty || null;
        command.yearsOfExperience = Number(yearsOfExperience) || 0;
      }

      await updateMyProfileApi(command);
      showToast("Cập nhật thông tin cá nhân thành công!", "success");

      // Refresh the page data
      const updatedData = await getMyProfileApi();
      setProfile(updatedData);
    } catch (err: any) {
      const errMsg = err.message || "Cập nhật thất bại.";
      setError(errMsg);
      showToast(errMsg, "error");
    } finally {
      setSaving(false);
    }
  };

  const handlePasswordChange = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const errors: Record<string, string> = {};
    if (!currentPassword) {
      errors.currentPassword = "Mật khẩu hiện tại không được để trống.";
    }
    if (!newPassword) {
      errors.newPassword = "Mật khẩu mới không được để trống.";
    } else if (newPassword.length < 8) {
      errors.newPassword = "Mật khẩu mới phải có ít nhất 8 ký tự.";
    }
    if (!confirmPassword) {
      errors.confirmPassword = "Xác nhận mật khẩu mới không được để trống.";
    } else if (newPassword !== confirmPassword) {
      errors.confirmPassword = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
    }
    if (newPassword && currentPassword && newPassword === currentPassword) {
      errors.newPassword = "Mật khẩu mới không được giống mật khẩu cũ.";
    }

    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      showToast("Vui lòng kiểm tra lại mật khẩu.", "error");
      return;
    }
    setFieldErrors({});

    try {
      setSaving(true);
      setError(null);

      await changePasswordApi(currentPassword, newPassword);

      showToast("Thay đổi mật khẩu thành công!", "success");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");

      // Add a simulated log entry
      const newLog: SimulatedLog = {
        id: `ACT-${Date.now()}`,
        time: "Vừa xong",
        action: "Đổi mật khẩu tài khoản thành công",
        module: "Bảo mật",
        ip: "192.168.1.15",
        status: "success",
      };
      setSimulatedLogs((prev) => [newLog, ...prev]);

    } catch (err: any) {
      const errMsg = err.message || "Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu hiện tại.";
      setError(errMsg);
      showToast(errMsg, "error");
      
      if (errMsg.toLowerCase().includes("mật khẩu hiện tại không chính xác") || 
          errMsg.toLowerCase().includes("mật khẩu hiện tại không đúng") || 
          errMsg.toLowerCase().includes("incorrect current password") || 
          errMsg.toLowerCase().includes("wrong password")) {
        setFieldErrors({ currentPassword: "Mật khẩu hiện tại không chính xác." });
      }
    } finally {
      setSaving(false);
    }
  };

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
    }).format(val);
  };

  const initials = fullName
    ? fullName.trim().split(/\s+/).slice(-2).map((w) => w[0]).join("").toUpperCase()
    : "??";

  if (loading) {
    return (
      <div className="flex min-h-screen bg-slate-50 text-slate-800">
        {sidebar}
        <main className="flex-1 flex items-center justify-center">
          <div className="flex flex-col items-center gap-3">
            <div className="w-12 h-12 rounded-full border-4 border-slate-200 border-t-primary animate-spin" />
            <span className="text-[14px] font-bold text-slate-400">Đang tải thông tin...</span>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen bg-slate-50 text-slate-800 font-sans">
      {/* Sidebar */}
      {sidebar}

      {/* Main content */}
      <main className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-8 h-20 flex items-center shrink-0 shadow-sm shadow-slate-100/50 justify-between">
          <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">
            {activeTab === "personal" && "Hồ Sơ Cá Nhân"}
            {activeTab === "password" && "Đổi Mật Khẩu"}
            {activeTab === "activities" && "Nhật Ký Hoạt Động"}
          </h1>
        </header>

        {/* Container */}
        <div className="p-8 flex-1 overflow-y-auto max-w-5xl w-full mx-auto">
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 text-red-700 rounded-xl text-[14px] font-semibold flex items-center gap-3 shadow-sm">
              <span className="text-xl">⚠️</span>
              {error}
            </div>
          )}
          {activeTab === "personal" ? (
            /* Tab 1: Personal Profile - Split Layout (Avatar/Salary on left, Form on right) */
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
              {/* Left Box - Avatar and Quick Read Only metadata */}
              <div className="lg:col-span-1 flex flex-col gap-6">
                <div className="bg-white rounded-2xl border border-slate-200/60 p-6 shadow-sm flex flex-col items-center text-center">
                  {/* Avatar Uploader */}
                  <div
                    className="relative group w-32 h-32 rounded-full border-4 border-white shadow-md cursor-pointer overflow-hidden mb-4"
                    onClick={handleAvatarClick}
                  >
                    {profilePictureUrl ? (
                      <img
                        src={profilePictureUrl}
                        alt={fullName}
                        className="w-full h-full object-cover transition-transform group-hover:scale-110"
                      />
                    ) : (
                      <div className="w-full h-full bg-slate-100 text-slate-400 font-black text-3xl flex items-center justify-center uppercase">
                        {initials}
                      </div>
                    )}
                    {/* Overlay camera */}
                    <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center justify-center text-white">
                      <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M6.827 6.175A2.31 2.31 0 015.186 7.23c-.38.054-.757.112-1.134.175C2.999 7.58 2.25 8.507 2.25 9.574V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9.574c0-1.067-.75-1.994-1.802-2.169a47.865 47.865 0 00-1.134-.175 2.31 2.31 0 01-1.64-1.055l-.822-1.316a2.192 2.192 0 00-1.736-1.039 48.774 48.774 0 00-5.232 0 2.192 2.192 0 00-1.736 1.039l-.821 1.316z" />
                        <circle cx="12" cy="13" r="3" strokeWidth="2" />
                      </svg>
                      <span className="text-[11px] font-bold mt-1">Thay đổi ảnh</span>
                    </div>
                  </div>

                  <input type="file" ref={fileInputRef} onChange={handleFileChange} accept="image/*" className="hidden" />

                  <h2 className="text-xl font-bold text-slate-900 leading-tight">{fullName || profile?.email}</h2>
                  <span className="px-3 py-1 bg-primary/10 text-primary font-bold text-[12px] rounded-full mt-2 uppercase tracking-wide">
                    {profile?.role === "Admin" ? "Quản trị viên" : profile?.role === "Dentist" ? "Bác sĩ Nha khoa" : "Nhân viên"}
                  </span>
                </div>

                {/* Compensation Premium Glass Card (Read-only) */}
                <div className="bg-gradient-to-br from-indigo-950 via-slate-900 to-slate-950 text-white rounded-2xl p-6 shadow-md border border-slate-800 flex flex-col gap-4 relative overflow-hidden">
                  <div className="absolute right-0 top-0 translate-x-1/3 -translate-y-1/3 w-32 h-32 bg-primary/20 rounded-full blur-xl pointer-events-none" />
                  <div className="flex items-center justify-between border-b border-white/10 pb-3">
                    <h3 className="text-[16px] font-extrabold text-amber-400 uppercase tracking-wider flex items-center gap-1.5">
                      💵 Lương & Chi trả
                    </h3>
                    <span className="text-[10px] bg-white/15 px-2 py-0.5 rounded-full font-bold text-white/80">Chỉ đọc</span>
                  </div>

                  <div className="flex flex-col gap-3">
                    <div className="flex justify-between items-baseline">
                      <span className="text-[13px] font-medium text-white/60">
                        {profile?.role === "Dentist" && "Lương cơ bản cao:"}
                        {profile?.role === "Staff" && "Lương cơ bản hành chính:"}
                        {profile?.role === "Admin" && "Lương cơ bản quản lý:"}
                      </span>
                      <span className="text-[16px] font-bold">{formatCurrency(profile?.baseSalary || 0)}</span>
                    </div>
                    <div className="flex justify-between items-baseline">
                      <span className="text-[13px] font-medium text-white/60">
                        {profile?.role === "Dentist" && "Phụ cấp chuyên môn (ca điều trị):"}
                        {profile?.role === "Staff" && "Phụ cấp tăng ca/trách nhiệm:"}
                        {profile?.role === "Admin" && "Phụ cấp thâm niên:"}
                      </span>
                      <span className="text-[16px] font-bold text-emerald-400">+{formatCurrency(profile?.allowance || 0)}</span>
                    </div>
                    <div className="border-t border-white/5 my-1" />
                    <div className="flex justify-between items-baseline">
                      <span className="text-[14px] font-bold text-white/80">Tổng thu nhập tạm tính:</span>
                      <span className="text-[20px] font-black text-amber-300">
                        {formatCurrency((profile?.baseSalary || 0) + (profile?.allowance || 0))}
                      </span>
                    </div>
                  </div>

                  {profile?.salaryNote && (
                    <p className="text-[11.5px] italic text-white/50 leading-relaxed border-t border-white/10 pt-3">
                      * {profile.salaryNote}
                    </p>
                  )}
                </div>
              </div>

              {/* Right Column - Detailed Form */}
              <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200/60 p-6 shadow-sm">
                <h3 className="text-[18px] font-extrabold text-slate-900 border-b border-slate-100 pb-4 mb-6">
                  Thông Tin Cá Nhân
                </h3>

                <form onSubmit={handleSaveProfile} className="flex flex-col gap-6 font-sans">
                  {/* General Profile Grid */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Họ và Tên <span className="text-red-500">*</span>
                      </label>
                      <input
                        type="text"
                        value={fullName}
                        onChange={(e) => {
                          setFullName(e.target.value);
                          if (fieldErrors.fullName) {
                            setFieldErrors(prev => {
                              const updated = { ...prev };
                              delete updated.fullName;
                              return updated;
                            });
                          }
                        }}
                        placeholder="Họ và tên..."
                        className={`w-full px-4 py-2.5 rounded-xl border ${
                          fieldErrors.fullName ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                        } focus:ring-1 focus:outline-none transition-all font-semibold`}
                      />
                      {fieldErrors.fullName && (
                        <span className="text-red-500 text-[12px] font-bold mt-1 block">
                          {fieldErrors.fullName}
                        </span>
                      )}
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">Email</label>
                      <input
                        type="email"
                        value={profile?.email || ""}
                        disabled
                        className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-400 font-semibold cursor-not-allowed focus:outline-none"
                      />
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Số điện thoại <span className="text-red-500">*</span>
                      </label>
                      <input
                        type="text"
                        value={phoneNumber}
                        onChange={(e) => {
                          setPhoneNumber(e.target.value);
                          if (fieldErrors.phoneNumber) {
                            setFieldErrors(prev => {
                              const updated = { ...prev };
                              delete updated.phoneNumber;
                              return updated;
                            });
                          }
                        }}
                        placeholder="Số điện thoại..."
                        className={`w-full px-4 py-2.5 rounded-xl border ${
                          fieldErrors.phoneNumber ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                        } focus:ring-1 focus:outline-none transition-all font-semibold`}
                      />
                      {fieldErrors.phoneNumber && (
                        <span className="text-red-500 text-[12px] font-bold mt-1 block">
                          {fieldErrors.phoneNumber}
                        </span>
                      )}
                    </div>

                    {profile?.role !== "Admin" && (
                      <div className="grid grid-cols-2 gap-4">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                            Ngày sinh
                          </label>
                          <input
                            type="date"
                            value={dateOfBirth}
                            onChange={(e) => {
                              setDateOfBirth(e.target.value);
                              if (fieldErrors.dateOfBirth) {
                                setFieldErrors(prev => {
                                  const updated = { ...prev };
                                  delete updated.dateOfBirth;
                                  return updated;
                                });
                              }
                            }}
                            className={`w-full px-4 py-2.5 rounded-xl border ${
                              fieldErrors.dateOfBirth ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                            } focus:ring-1 focus:outline-none transition-all font-semibold`}
                          />
                          {fieldErrors.dateOfBirth && (
                            <span className="text-red-500 text-[12px] font-bold mt-1 block">
                              {fieldErrors.dateOfBirth}
                            </span>
                          )}
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                            Giới tính
                          </label>
                          <select
                            value={gender}
                            onChange={(e) => setGender(e.target.value)}
                            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-primary focus:outline-none transition-all font-semibold"
                          >
                            <option value="Nam">Nam</option>
                            <option value="Nữ">Nữ</option>
                            <option value="Khác">Khác</option>
                          </select>
                        </div>
                      </div>
                    )}
                  </div>

                  {profile?.role !== "Admin" && (
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">Địa chỉ</label>
                      <input
                        type="text"
                        value={address}
                        onChange={(e) => setAddress(e.target.value)}
                        placeholder="Địa chỉ thường trú..."
                        className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                      />
                    </div>
                  )}

                  {profile?.role === "Dentist" && (
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        Học vấn / Học hàm
                      </label>
                      <input
                        type="text"
                        value={education}
                        onChange={(e) => setEducation(e.target.value)}
                        placeholder="Ví dụ: Thạc sĩ, Bác sĩ chuyên khoa I..."
                        className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold"
                      />
                    </div>
                  )}

                  {/* ──────────────────────────────────────────────────────────
                      BỔ SUNG THÔNG TIN CHỈ ĐỌC (READ-ONLY) THEO TỪNG VAI TRÒ
                  ────────────────────────────────────────────────────────── */}
                  
                  {/* Read-Only section for Dentist inside form */}
                  {profile?.role === "Dentist" && (
                    <div className="border-t border-slate-100 pt-5 mt-2 flex flex-col gap-5">
                      <h4 className="text-[15px] font-extrabold text-slate-900 flex items-center gap-1.5">
                        🛠️ Thông tin Y khoa & Hành chính <span className="text-[11.5px] font-normal text-slate-400">(Chỉ đọc)</span>
                      </h4>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Số Chứng chỉ hành nghề (CCHN)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.licenseNumber || "N/A"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Ngày cấp CCHN
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{formatDate(profile?.certificateIssuedDate)}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Nơi cấp CCHN
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.certificateIssuedBy || "N/A"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Vai trò hệ thống
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>Bác sĩ Nha khoa</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Mã nhân viên (Employee ID)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.employeeId || "Chưa cấp"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Phòng ban
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.department || "N/A"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Chức danh / Vị trí
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.position || "N/A"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Ngày vào làm
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{formatDate(profile?.startDate)}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Trạng thái làm việc
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span className="text-emerald-600 font-extrabold">{profile?.employmentStatus || "Hoạt động"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col md:col-span-2 gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Các dịch vụ / liệu trình phụ trách
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed min-h-[44px]">
                            <span>{profile?.servicesHandled || "Chưa phân công"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        {/* Specialty for Dentist (Editable) */}
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                            Chuyên khoa chính
                          </label>
                          <input
                            type="text"
                            value={specialty}
                            onChange={(e) => {
                              setSpecialty(e.target.value);
                              if (fieldErrors.specialty) {
                                setFieldErrors(prev => {
                                  const updated = { ...prev };
                                  delete updated.specialty;
                                  return updated;
                                });
                              }
                            }}
                            placeholder="Ví dụ: Chỉnh nha, Bọc sứ..."
                            className={`w-full px-4 py-2.5 rounded-xl border ${
                              fieldErrors.specialty ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                            } focus:ring-1 focus:outline-none transition-all font-semibold`}
                          />
                          {fieldErrors.specialty && (
                            <span className="text-red-500 text-[12px] font-bold mt-1 block">
                              {fieldErrors.specialty}
                            </span>
                          )}
                        </div>

                        {/* Years of Experience for Dentist (Editable) */}
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                            Số năm kinh nghiệm
                          </label>
                          <input
                            type="number"
                            value={yearsOfExperience}
                            onChange={(e) => {
                              setYearsOfExperience(Number(e.target.value));
                              if (fieldErrors.yearsOfExperience) {
                                setFieldErrors(prev => {
                                  const updated = { ...prev };
                                  delete updated.yearsOfExperience;
                                  return updated;
                                });
                              }
                            }}
                            placeholder="Số năm kinh nghiệm..."
                            className={`w-full px-4 py-2.5 rounded-xl border ${
                              fieldErrors.yearsOfExperience ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                            } focus:ring-1 focus:outline-none transition-all font-semibold`}
                          />
                          {fieldErrors.yearsOfExperience && (
                            <span className="text-red-500 text-[12px] font-bold mt-1 block">
                              {fieldErrors.yearsOfExperience}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Read-Only section for Staff inside form */}
                  {profile?.role === "Staff" && (
                    <div className="border-t border-slate-100 pt-5 mt-2 flex flex-col gap-5">
                      <h4 className="text-[15px] font-extrabold text-slate-900 flex items-center gap-1.5">
                        🛠️ Thông tin Nhân sự Hành chính <span className="text-[11.5px] font-normal text-slate-400">(Chỉ đọc)</span>
                      </h4>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Mã số nhân viên (Employee ID)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.employeeId || "Chưa cấp"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Phòng ban
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.department || "N/A"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Chức vụ / Vị trí
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.position || "N/A"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Ngày vào làm
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{formatDate(profile?.startDate)}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Trạng thái làm việc
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span className="text-emerald-600 font-extrabold">{profile?.employmentStatus || "Hoạt động"}</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Vai trò phân quyền
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>Nhân viên quầy / Lễ tân</span>
                            <span className="text-xs text-slate-400">🔒</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {profile?.role !== "Admin" && (
                    <div className="flex flex-col gap-1.5">
                      <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                        {profile?.role === "Dentist"
                          ? "Tiểu sử giới thiệu chuyên môn (Hiển thị trực tiếp trên website)"
                          : "Tiểu sử ngắn"}
                      </label>
                      <textarea
                        value={bio}
                        onChange={(e) => setBio(e.target.value)}
                        placeholder={profile?.role === "Dentist" ? "Giới thiệu chuyên môn, bằng cấp, thế mạnh của bác sĩ..." : "Giới thiệu bản thân ngắn gọn..."}
                        rows={4}
                        className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:border-primary focus:ring-1 focus:ring-primary focus:outline-none transition-all font-semibold resize-none"
                      />
                    </div>
                  )}

                  <div className="border-t border-slate-100 pt-6 flex justify-end">
                    <button
                      type="submit"
                      disabled={saving}
                      className="px-6 py-3 bg-primary text-white rounded-xl font-bold text-[14px] hover:bg-primary/95 transition-all shadow-md shadow-primary/20 flex items-center justify-center gap-2 cursor-pointer disabled:bg-slate-350 disabled:cursor-not-allowed hover:translate-y-[-1px]"
                    >
                      {saving ? (
                        <>
                          <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                          <span>Đang lưu...</span>
                        </>
                      ) : (
                        <span>Lưu thông tin</span>
                      )}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          ) : activeTab === "password" ? (
            /* Tab 2: Change Password - Centered Full-Width Card (No avatar column) */
            <div className="bg-white rounded-2xl border border-slate-200/60 p-8 shadow-sm max-w-2xl mx-auto">
              <h3 className="text-[18px] font-extrabold text-slate-900 border-b border-slate-100 pb-4 mb-6">
                🔒 Đổi Mật Khẩu
              </h3>

              <form onSubmit={handlePasswordChange} className="flex flex-col gap-6 font-sans">
                <div className="flex flex-col gap-1.5">
                  <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                    Mật khẩu hiện tại <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="password"
                    value={currentPassword}
                    onChange={(e) => {
                      setCurrentPassword(e.target.value);
                      if (fieldErrors.currentPassword) {
                        setFieldErrors(prev => {
                          const updated = { ...prev };
                          delete updated.currentPassword;
                          return updated;
                        });
                      }
                    }}
                    placeholder="Nhập mật khẩu hiện tại..."
                    className={`w-full px-4 py-2.5 rounded-xl border ${
                      fieldErrors.currentPassword ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                    } focus:ring-1 focus:outline-none transition-all font-semibold`}
                  />
                  {fieldErrors.currentPassword && (
                    <span className="text-red-500 text-[12px] font-bold mt-1 block">
                      {fieldErrors.currentPassword}
                    </span>
                  )}
                </div>

                <div className="flex flex-col gap-1.5">
                  <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                    Mật khẩu mới <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="password"
                    value={newPassword}
                    onChange={(e) => {
                      setNewPassword(e.target.value);
                      if (fieldErrors.newPassword) {
                        setFieldErrors(prev => {
                          const updated = { ...prev };
                          delete updated.newPassword;
                          return updated;
                        });
                      }
                    }}
                    placeholder="Mật khẩu mới (ít nhất 8 ký tự)..."
                    className={`w-full px-4 py-2.5 rounded-xl border ${
                      fieldErrors.newPassword ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                    } focus:ring-1 focus:outline-none transition-all font-semibold`}
                  />
                  {fieldErrors.newPassword && (
                    <span className="text-red-500 text-[12px] font-bold mt-1 block">
                      {fieldErrors.newPassword}
                    </span>
                  )}
                </div>

                <div className="flex flex-col gap-1.5">
                  <label className="text-[13px] font-extrabold text-slate-500 uppercase tracking-wide">
                    Xác nhận mật khẩu mới <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => {
                      setConfirmPassword(e.target.value);
                      if (fieldErrors.confirmPassword) {
                        setFieldErrors(prev => {
                          const updated = { ...prev };
                          delete updated.confirmPassword;
                          return updated;
                        });
                      }
                    }}
                    placeholder="Nhập lại mật khẩu mới..."
                    className={`w-full px-4 py-2.5 rounded-xl border ${
                      fieldErrors.confirmPassword ? "border-red-400 focus:border-red-500 focus:ring-red-500" : "border-slate-200 focus:border-primary focus:ring-primary"
                    } focus:ring-1 focus:outline-none transition-all font-semibold`}
                  />
                  {fieldErrors.confirmPassword && (
                    <span className="text-red-500 text-[12px] font-bold mt-1 block">
                      {fieldErrors.confirmPassword}
                    </span>
                  )}
                </div>

                <div className="border-t border-slate-100 pt-6 flex justify-end">
                  <button
                    type="submit"
                    disabled={saving}
                    className="px-6 py-3 bg-primary text-white rounded-xl font-bold text-[14px] hover:bg-primary/95 transition-all shadow-md shadow-primary/20 flex items-center justify-center gap-2 cursor-pointer disabled:bg-slate-350 disabled:cursor-not-allowed hover:translate-y-[-1px]"
                  >
                    {saving ? (
                      <>
                        <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                        <span>Đang cập nhật...</span>
                      </>
                    ) : (
                      <span>Cập nhật mật khẩu</span>
                    )}
                  </button>
                </div>
              </form>
            </div>
          ) : (
            /* Tab 3: Activities Log - Full Width Table Layout (No avatar column) */
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden flex flex-col w-full">
              <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
                <h3 className="text-[18px] font-extrabold text-slate-900">
                  📜 Nhật Ký Hoạt Động Cá Nhân
                </h3>
                <span className="text-[11.5px] bg-slate-50 border border-slate-200/80 text-slate-400 font-extrabold px-2.5 py-1 rounded-full uppercase">
                  Chỉ đọc
                </span>
              </div>

              {/* Responsive Log Table */}
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse text-[13px] min-w-[700px] font-sans">
                  <thead>
                    <tr className="bg-slate-50/60 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 text-[11px] select-none">
                      <th className="px-6 py-4 w-[160px]">Thời gian</th>
                      <th className="px-6 py-4 w-[120px]">Phân hệ</th>
                      <th className="px-6 py-4">Mô tả hoạt động</th>
                      <th className="px-6 py-4 w-[130px]">Địa chỉ IP</th>
                      <th className="px-6 py-4 w-[130px] text-center">Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-700 font-semibold">
                    {simulatedLogs.map((log) => (
                      <tr key={log.id} className="hover:bg-slate-50/40 transition-colors">
                        {/* Time */}
                        <td className="px-6 py-3.5 font-bold text-slate-900">{log.time}</td>
                        {/* Module */}
                        <td className="px-6 py-3.5">
                          <span className="inline-flex items-center px-2 py-0.5 rounded bg-slate-100 border border-slate-200 text-slate-600 text-[12px] font-bold">
                            {log.module}
                          </span>
                        </td>
                        {/* Description */}
                        <td className="px-6 py-3.5 text-slate-600 font-medium">{log.action}</td>
                        {/* IP */}
                        <td className="px-6 py-3.5 font-mono text-slate-500 text-[12.5px]">{log.ip}</td>
                        {/* Status */}
                        <td className="px-6 py-3.5 text-center">
                          <span
                            className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11.5px] font-black ${
                              log.status === "success"
                                ? "bg-green-50 text-green-700 border border-green-100"
                                : "bg-amber-50 text-amber-700 border border-amber-100"
                            }`}
                          >
                            <span
                              className={`w-1.5 h-1.5 rounded-full shrink-0 ${
                                log.status === "success" ? "bg-green-500" : "bg-amber-500"
                              }`}
                            />
                            {log.status === "success" ? "Thành công" : "Cảnh báo"}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Real-time Indicator Footer */}
              <div className="px-6 py-3 border-t border-slate-100 bg-slate-50/50 flex items-center justify-between text-[12.5px] text-slate-400 font-bold">
                <span>Tổng số: {simulatedLogs.length} hoạt động</span>
                <div className="flex items-center gap-1.5 text-emerald-600">
                  <span className="relative flex h-2 w-2">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
                  </span>
                  Thời gian thực
                </div>
              </div>
            </div>
          )}
        </div>
      </main>
      {/* ── TOAST NOTIFICATION ───────────────── */}
      {toast && (
        <div className={`fixed top-6 right-6 z-[9999] px-5 py-3.5 rounded-xl shadow-xl flex items-center gap-3 border font-bold text-[14.5px] max-w-md animate-fade-in ${
          toast.type === "success" ? "bg-emerald-900 text-white border-emerald-800"
          : toast.type === "error" ? "bg-red-900 text-white border-red-800"
          : "bg-slate-900 text-white border-slate-800"
        }`}>
          <span className="text-lg">{toast.type === "success" ? "✓" : toast.type === "error" ? "⚠" : "ℹ"}</span>
          <span>{toast.message}</span>
        </div>
      )}
    </div>
  );
}
