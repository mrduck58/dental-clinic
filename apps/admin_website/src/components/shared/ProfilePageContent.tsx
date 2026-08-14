"use client";

import React, { useState, useEffect, useRef } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import NotificationBell from "./NotificationBell";
import {
  getMyProfileApi,
  updateMyProfileApi,
  changePasswordApi,
  uploadFileApi,
  resolveAssetUrl,
  type UserProfileDto,
  getActivityLogsApi,
  type ActivityLogItemDto
} from "../../lib/apiClient";

interface ProfilePageContentProps {
  sidebar: React.ReactNode;
  notificationHref?: string;
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

const formatTimestamp = (tsStr: string | null | undefined) => {
  if (!tsStr) return "N/A";
  try {
    const date = new Date(tsStr);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');
    return `${day}/${month}/${year} ${hours}:${minutes}:${seconds}`;
  } catch {
    return tsStr;
  }
};

export default function ProfilePageContent({ sidebar, notificationHref = "/admin/notifications" }: ProfilePageContentProps) {
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

  // Real activity logs states
  const [activityLogs, setActivityLogs] = useState<ActivityLogItemDto[]>([]);
  const [logsLoading, setLogsLoading] = useState<boolean>(false);
  const [logsTotal, setLogsTotal] = useState<number>(0);
  const [logsPage, setLogsPage] = useState<number>(1);
  const logsPageSize = 10;

  const fileInputRef = useRef<HTMLInputElement>(null);

  const fetchActivityLogs = async (page: number) => {
    if (!profile) return;
    try {
      setLogsLoading(true);
      const data = await getActivityLogsApi({
        userId: profile.id,
        page,
        pageSize: logsPageSize,
      });
      setActivityLogs(data.items || []);
      setLogsTotal(data.totalCount || 0);
    } catch (err: any) {
      console.error("Failed to load activity logs:", err);
    } finally {
      setLogsLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab === "activities" && profile) {
      fetchActivityLogs(logsPage);
    }
  }, [activeTab, profile, logsPage]);

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

      // Fetch initial logs to display last login on profile and pre-fill activities tab
      try {
        const logsData = await getActivityLogsApi({
          userId: data.id,
          page: 1,
          pageSize: logsPageSize,
        });
        setActivityLogs(logsData.items || []);
        setLogsTotal(logsData.totalCount || 0);
      } catch (logErr) {
        console.error("Failed to load initial activity logs:", logErr);
      }
    } catch (err: any) {
      setError(err.message || "Không thể tải thông tin cá nhân.");
    } finally {
      setLoading(false);
    }
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
        <header className="sticky top-0 z-20 bg-white/95 backdrop-blur-md border-b border-slate-200 px-4 sm:px-8 h-16 sm:h-20 flex items-center shrink-0 shadow-sm shadow-slate-100/50 justify-between">
          <div className="flex items-center gap-3 sm:gap-4 min-w-0">
            <button
              type="button"
              onClick={() => window.dispatchEvent(new CustomEvent("toggle-sidebar"))}
              className="lg:hidden p-2 -ml-2 rounded-xl text-slate-600 hover:text-slate-900 hover:bg-slate-100 transition-all shrink-0"
              aria-label="Mở menu điều hướng"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
              </svg>
            </button>
            <h1 className="text-lg sm:text-2xl font-extrabold text-slate-900 tracking-tight truncate">
              {activeTab === "personal" && "Hồ Sơ Cá Nhân"}
              {activeTab === "password" && "Đổi Mật Khẩu"}
              {activeTab === "activities" && "Nhật Ký Hoạt Động"}
            </h1>
          </div>
          <NotificationBell href={notificationHref} />
        </header>

        {/* Container */}
        <div className="p-4 sm:p-8 flex-1 overflow-y-auto max-w-5xl w-full mx-auto">
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 text-red-700 rounded-xl text-[14px] font-semibold flex items-center gap-3 shadow-sm">
              <svg className="w-5 h-5 text-red-500 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" /></svg>
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
                        src={resolveAssetUrl(profilePictureUrl)}
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
                    {profile?.role === "Admin" ? "Quản trị viên" : profile?.role === "Dentist" ? "Bác sĩ Nha khoa" : profile?.role === "Owner" ? "Chủ phòng khám" : "Nhân viên"}
                  </span>
                </div>

                {/* Compensation Premium Glass Card (Read-only) */}
                {profile?.role !== "Owner" && (
                  <div className="bg-gradient-to-br from-indigo-950 via-slate-900 to-slate-950 text-white rounded-2xl p-6 shadow-md border border-slate-800 flex flex-col gap-4 relative overflow-hidden">
                    <div className="absolute right-0 top-0 translate-x-1/3 -translate-y-1/3 w-32 h-32 bg-primary/20 rounded-full blur-xl pointer-events-none" />
                    <div className="flex items-center justify-between border-b border-white/10 pb-3">
                      <h3 className="text-[15px] font-extrabold text-amber-400 uppercase tracking-wider flex items-center gap-2">
                        <svg className="w-5 h-5 text-amber-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 18.75a60.07 60.07 0 0115.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 013 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 00-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 01-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 003 15h-.75M15 10.5a3 3 0 11-6 0 3 3 0 016 0zm6 3.75a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0z" />
                        </svg>
                        Lương & Chi trả
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
                )}
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
                      <h4 className="text-[15px] font-extrabold text-slate-900 flex items-center gap-2">
                        <svg className="w-4 h-4 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" /></svg>
                        Thông tin Y khoa & Hành chính <span className="text-[11.5px] font-normal text-slate-400">(Chỉ đọc)</span>
                      </h4>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Số Chứng chỉ hành nghề (CCHN)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.licenseNumber || "N/A"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Ngày cấp CCHN
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{formatDate(profile?.certificateIssuedDate)}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Nơi cấp CCHN
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.certificateIssuedBy || "N/A"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Vai trò hệ thống
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>Bác sĩ Nha khoa</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Mã nhân viên (Employee ID)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.employeeId || "Chưa cấp"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Phòng ban
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.department || "N/A"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Chức danh / Vị trí
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.position || "N/A"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Ngày vào làm
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{formatDate(profile?.startDate)}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Trạng thái làm việc
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span className="text-emerald-600 font-extrabold">{profile?.employmentStatus || "Hoạt động"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col md:col-span-2 gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Các dịch vụ / liệu trình phụ trách
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed min-h-[44px]">
                            <span>{profile?.servicesHandled || "Chưa phân công"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
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
                      <h4 className="text-[15px] font-extrabold text-slate-900 flex items-center gap-2">
                        <svg className="w-4 h-4 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M20.25 14.15v4.25c0 1.094-.787 2.036-1.872 2.18-2.087.277-4.216.42-6.378.42s-4.291-.143-6.378-.42c-1.085-.144-1.872-1.086-1.872-2.18v-4.25m16.5 0a2.18 2.18 0 00.75-1.661V8.706c0-1.081-.768-2.015-1.837-2.175a48.114 48.114 0 00-3.413-.387m4.5 8.006c-.194.165-.42.295-.673.38A23.978 23.978 0 0112 15.75c-2.648 0-5.195-.429-7.577-1.22a2.016 2.016 0 01-.673-.38m0 0A2.18 2.18 0 013 12.489V8.706c0-1.081.768-2.015 1.837-2.175a48.111 48.111 0 013.413-.387m7.5 0V5.25A2.25 2.25 0 0013.5 3h-3a2.25 2.25 0 00-2.25 2.25v.894m7.5 0a48.667 48.667 0 00-7.5 0M12 12.75h.008v.008H12v-.008z" /></svg>
                        Thông tin Nhân sự Hành chính <span className="text-[11.5px] font-normal text-slate-400">(Chỉ đọc)</span>
                      </h4>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Mã số nhân viên (Employee ID)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.employeeId || "Chưa cấp"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Phòng ban
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.department || "N/A"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Chức vụ / Vị trí
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.position || "N/A"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Ngày vào làm
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{formatDate(profile?.startDate)}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Trạng thái làm việc
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span className="text-emerald-600 font-extrabold">{profile?.employmentStatus || "Hoạt động"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Vai trò phân quyền
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>Nhân viên quầy / Lễ tân</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Read-Only section for Owner inside form */}
                  {profile?.role === "Owner" && (
                    <div className="border-t border-slate-100 pt-5 mt-2 flex flex-col gap-5">
                      <h4 className="text-[15px] font-extrabold text-slate-900 flex items-center gap-2">
                        <svg className="w-4 h-4 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M17.982 18.725A7.488 7.488 0 0012 15.75a7.488 7.488 0 00-5.982 2.975m11.963 0a9 9 0 10-11.963 0m11.963 0A8.966 8.966 0 0112 21a8.966 8.966 0 01-5.982-2.275M15 9.75a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
                        Thông tin Tài khoản <span className="text-[11.5px] font-normal text-slate-400">(Chỉ đọc)</span>
                      </h4>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Tên đăng nhập (Username)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-all flex justify-between items-center cursor-not-allowed">
                            <span>{profile?.username || "N/A"}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Vai trò hệ thống
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>Owner (Chủ phòng khám)</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Trạng thái tài khoản
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-emerald-600 font-extrabold select-none flex justify-between items-center cursor-not-allowed">
                            <span>Đang hoạt động</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Ngày tạo tài khoản
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>{formatTimestamp(profile?.createdAt)}</span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>

                        <div className="flex flex-col md:col-span-2 gap-1.5">
                          <label className="text-[13px] font-extrabold text-slate-400 uppercase tracking-wide">
                            Đăng nhập gần nhất (Last Login)
                          </label>
                          <div className="w-full px-4 py-2.5 rounded-xl border border-slate-100 bg-slate-50 text-slate-500 font-bold select-none flex justify-between items-center cursor-not-allowed">
                            <span>
                              {(() => {
                                const loginLog = activityLogs.find(log => log.action === "login");
                                return loginLog 
                                  ? `${formatTimestamp(loginLog.createdAt)} (IP: ${loginLog.ipAddress || "N/A"})`
                                  : "Chưa ghi nhận đăng nhập gần đây";
                              })()}
                            </span>
                            <svg className="w-3.5 h-3.5 text-slate-400 shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {profile?.role !== "Admin" && profile?.role !== "Owner" && (
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
            <div className="bg-white rounded-2xl border border-slate-200/60 p-6 sm:p-8 shadow-sm max-w-2xl mx-auto">
              <h3 className="text-[18px] font-extrabold text-slate-900 border-b border-slate-100 pb-4 mb-6 flex items-center gap-2.5">
                <svg className="w-5 h-5 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" /></svg>
                Đổi Mật Khẩu
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
                <h3 className="text-[18px] font-extrabold text-slate-900 flex items-center gap-2.5">
                  <svg className="w-5 h-5 text-primary shrink-0" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  Nhật Ký Hoạt Động Cá Nhân
                </h3>
                <span className="text-[11.5px] bg-slate-50 border border-slate-200/80 text-slate-400 font-extrabold px-2.5 py-1 rounded-full uppercase">
                  Chỉ đọc
                </span>
              </div>

              {/* Responsive Log Table */}
              <div className="overflow-x-auto">
                {logsLoading ? (
                  <div className="flex flex-col items-center justify-center py-12 gap-3">
                    <div className="w-8 h-8 rounded-full border-3 border-slate-200 border-t-primary animate-spin" />
                    <span className="text-[13px] font-bold text-slate-400">Đang tải lịch sử...</span>
                  </div>
                ) : activityLogs.length === 0 ? (
                  <div className="flex flex-col items-center justify-center py-12 text-slate-400 font-bold text-[14px]">
                    <span>Không tìm thấy hoạt động nào.</span>
                  </div>
                ) : (
                  <>
                    <table className="w-full text-left border-collapse text-[13px] min-w-[700px] font-sans">
                      <thead>
                        <tr className="bg-slate-50/60 font-extrabold text-slate-400 uppercase tracking-wider border-b border-slate-200/80 text-[11px] select-none">
                          <th className="px-6 py-4 w-[180px]">Thời gian</th>
                          <th className="px-6 py-4 w-[120px]">Phân hệ</th>
                          <th className="px-6 py-4">Mô tả hoạt động</th>
                          <th className="px-6 py-4 w-[130px]">Địa chỉ IP</th>
                          <th className="px-6 py-4 w-[130px] text-center">Trạng thái</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 text-slate-700 font-semibold">
                        {activityLogs.map((log) => {
                          const statusStyle = (() => {
                            const s = (log.status || "").toLowerCase();
                            if (s === "success") return {
                              bg: "bg-green-50 text-green-700 border border-green-100",
                              dot: "bg-green-500",
                              label: "Thành công"
                            };
                            if (s === "failed") return {
                              bg: "bg-red-50 text-red-700 border border-red-100",
                              dot: "bg-red-500",
                              label: "Thất bại"
                            };
                            return {
                              bg: "bg-amber-50 text-amber-700 border border-amber-100",
                              dot: "bg-amber-500",
                              label: "Cảnh báo"
                            };
                          })();

                          return (
                            <tr key={log.id} className="hover:bg-slate-50/40 transition-colors">
                              {/* Time */}
                              <td className="px-6 py-3.5 font-bold text-slate-900">{formatTimestamp(log.createdAt)}</td>
                              {/* Module */}
                              <td className="px-6 py-3.5">
                                <span className="inline-flex items-center px-2 py-0.5 rounded bg-slate-100 border border-slate-200 text-slate-600 text-[12px] font-bold uppercase">
                                  {log.module}
                                </span>
                              </td>
                              {/* Description */}
                              <td className="px-6 py-3.5 text-slate-600 font-medium">{log.description}</td>
                              {/* IP */}
                              <td className="px-6 py-3.5 font-mono text-slate-500 text-[12.5px]">{log.ipAddress || "N/A"}</td>
                              {/* Status */}
                              <td className="px-6 py-3.5 text-center">
                                <span className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11.5px] font-black ${statusStyle.bg}`}>
                                  <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${statusStyle.dot}`} />
                                  {statusStyle.label}
                                </span>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>

                    {/* Pagination controls */}
                    {logsTotal > logsPageSize && (
                      <div className="px-6 py-3.5 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3">
                        <span className="text-[12.5px] text-slate-400 font-bold text-center sm:text-left">
                          Hiển thị {activityLogs.length} trên {logsTotal} hoạt động
                        </span>
                        <div className="flex items-center gap-2">
                          <button
                            disabled={logsPage === 1}
                            onClick={() => setLogsPage((prev) => Math.max(prev - 1, 1))}
                            className="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg font-bold text-[12px] text-slate-600 transition-colors"
                          >
                            Trước
                          </button>
                          <span className="text-[12.5px] font-bold text-slate-600">
                            Trang {logsPage} / {Math.ceil(logsTotal / logsPageSize)}
                          </span>
                          <button
                            disabled={logsPage >= Math.ceil(logsTotal / logsPageSize)}
                            onClick={() => setLogsPage((prev) => prev + 1)}
                            className="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg font-bold text-[12px] text-slate-600 transition-colors"
                          >
                            Sau
                          </button>
                        </div>
                      </div>
                    )}
                  </>
                )}
              </div>

              {/* Real-time Indicator Footer */}
              <div className="px-6 py-3 border-t border-slate-100 bg-slate-50/50 flex items-center justify-between text-[12.5px] text-slate-400 font-bold">
                <span>Tổng số: {logsTotal} hoạt động</span>
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
