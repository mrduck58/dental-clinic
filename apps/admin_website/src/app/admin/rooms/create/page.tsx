"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import AdminSidebar from "../../../../components/shared/AdminSidebar";
import AdminPageHeader from "../../../../components/shared/AdminPageHeader";
import { useRequireAdmin } from "../../../../hooks/useRequireAdmin";
import { createRoomApi } from "../../../../lib/apiClient";

export default function CreateRoomPage() {
  useRequireAdmin();
  const router = useRouter();

  const [roomName, setRoomName] = useState("");
  const [roomCode, setRoomCode] = useState("");
  const [floor, setFloor] = useState("");
  const [selectedType, setSelectedType] = useState<string>("Khám tổng quát");
  const [description, setDescription] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleSubmit = async (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!roomName.trim()) { setErrorMsg("Vui lòng nhập Tên phòng"); return; }
    if (!roomCode.trim()) { setErrorMsg("Vui lòng nhập Mã phòng"); return; }
    if (!floor) { setErrorMsg("Vui lòng chọn Tầng"); return; }

    setIsSaving(true);
    setErrorMsg(null);
    try {
      await createRoomApi({
        code: roomCode.trim(),
        name: roomName.trim(),
        floor,
        type: selectedType,
        description: description.trim(),
      });
      router.push("/admin/rooms");
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : "Tạo phòng thất bại");
      setIsSaving(false);
    }
  };

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">

      <AdminSidebar activeMenu="rooms" />

      <main className="flex-1 flex flex-col min-w-0">

        {/* Header */}
        <AdminPageHeader title="Tạo Phòng Khám Mới" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-6">

          {/* Breadcrumb */}
          <nav className="text-[13px] font-bold text-slate-400 flex items-center gap-1.5 select-none">
            <Link href="/admin/rooms" className="hover:text-slate-600 transition-colors">Quản lý phòng</Link>
            <span>/</span>
            <span className="text-slate-500 font-extrabold">Thêm phòng mới</span>
          </nav>

          <div>
            <h1 className="text-3xl font-black text-slate-900 tracking-tight">Thêm phòng mới</h1>
            <p className="text-[14px] text-slate-400 font-semibold mt-1">
              Vui lòng điền thông tin chi tiết để thiết lập phòng điều trị mới vào hệ thống.
            </p>
          </div>

          {/* Error banner */}
          {errorMsg && (
            <div className="bg-red-50 border border-red-200 text-red-700 font-bold text-[13px] px-4 py-3 rounded-xl flex items-center gap-2">
              <span>⚠</span> {errorMsg}
            </div>
          )}

          <form onSubmit={handleSubmit} className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">

            {/* ── LEFT (8/12) ───────────────────────────────────────────────── */}
            <div className="lg:col-span-8 flex flex-col gap-6">

              {/* Card A: Thông tin cơ bản */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                  <span className="text-[16px] text-primary">ℹ️</span>
                  <h3 className="text-[16px] font-extrabold text-slate-800">Thông tin cơ bản</h3>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 text-[13px]">
                  <div className="flex flex-col gap-2">
                    <label className="font-bold text-slate-500">Tên phòng <span className="text-primary">*</span></label>
                    <input type="text" required placeholder="VD: Phòng 4" value={roomName}
                      onChange={(e) => setRoomName(e.target.value)}
                      className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold" />
                  </div>
                  <div className="flex flex-col gap-2">
                    <label className="font-bold text-slate-500">Mã phòng <span className="text-primary">*</span></label>
                    <input type="text" required placeholder="VD: P004" value={roomCode}
                      onChange={(e) => setRoomCode(e.target.value)}
                      className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold uppercase" />
                  </div>
                  <div className="flex flex-col gap-2">
                    <label className="font-bold text-slate-500">Tầng <span className="text-primary">*</span></label>
                    <div className="relative">
                      <select required value={floor} onChange={(e) => setFloor(e.target.value)}
                        className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold appearance-none cursor-pointer">
                        <option value="">Chọn tầng</option>
                        <option value="1">Tầng 1</option>
                        <option value="2">Tầng 2</option>
                        <option value="3">Tầng 3</option>
                      </select>
                      <span className="absolute right-4 top-1/2 -translate-y-1/2 pointer-events-none text-slate-400 text-xs font-bold">▼</span>
                    </div>
                  </div>
                </div>

                {/* Room type selector */}
                <div className="flex flex-col gap-2 text-[13px] mt-1">
                  <label className="font-bold text-slate-500">Loại phòng <span className="text-primary">*</span></label>
                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-1">
                    {[
                      { type: "Khám tổng quát", icon: (
                        <svg className="w-8 h-8 mb-2.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 2C8.5 2 6 4.5 6 8c0 3 2 5.5 2 8 0 1.5-1 3.5-1 4.5 0 1.5 2.5 1.5 3.5.5s1.5-2 1.5-3c0 1 .5 2 1.5 3s3.5 1 3.5-.5c0-1-1-3-1-4.5 0-2.5 2-5 2-8 0-3.5-2.5-6-6-6z" />
                        </svg>
                      ), label: "Tổng quát" },
                      { type: "Cấp cứu", icon: (
                        <svg className="w-8 h-8 mb-2.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                        </svg>
                      ), label: "Cấp cứu" },
                      { type: "Phẫu thuật", icon: (
                        <svg className="w-8 h-8 mb-2.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                          <rect x="3" y="6" width="18" height="12" rx="2" />
                          <path strokeLinecap="round" strokeLinejoin="round" d="M8 10h8M8 14h8" />
                        </svg>
                      ), label: "Phẫu thuật" },
                      { type: "X-Quang", icon: (
                        <svg className="w-8 h-8 mb-2.5" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                          <rect x="6" y="3" width="12" height="14" rx="1" />
                          <path strokeLinecap="round" strokeLinejoin="round" d="M6 7h12M6 11h12M12 3v14M12 17v4" />
                        </svg>
                      ), label: "X-Quang" },
                    ].map(({ type, icon, label }) => (
                      <button key={type} type="button" onClick={() => setSelectedType(type)}
                        className={`flex flex-col items-center justify-center p-5 rounded-xl border transition-all hover:scale-[1.02] cursor-pointer ${
                          selectedType === type ? "border-primary text-primary bg-red-50/15" : "border-slate-200 text-slate-500 bg-white hover:border-slate-300"
                        }`}>
                        <span className={selectedType === type ? "text-primary" : "text-slate-400"}>{icon}</span>
                        <span className="text-[13px] font-extrabold leading-none">{label}</span>
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              {/* Card B: Mô tả */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                  <span className="text-[16px] text-primary">📋</span>
                  <h3 className="text-[16px] font-extrabold text-slate-800">Ghi chú & Mô tả</h3>
                </div>
                <div className="flex flex-col gap-2 text-[13px]">
                  <label className="font-bold text-slate-500">Mô tả chi tiết trang thiết bị</label>
                  <textarea rows={4} placeholder="Nhập mô tả về trang thiết bị có sẵn trong phòng..."
                    value={description} onChange={(e) => setDescription(e.target.value)}
                    className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary/50 focus:outline-none transition-all text-[14px] font-semibold resize-none" />
                </div>
              </div>
            </div>

            {/* ── RIGHT (4/12) ──────────────────────────────────────────────── */}
            <div className="lg:col-span-4 flex flex-col gap-6">

              {/* Card C: Xác nhận */}
              <div className="bg-primary p-6 rounded-3xl text-white shadow-lg shadow-red-700/15 flex flex-col gap-4">
                <div className="flex items-center gap-2">
                  <span className="w-5 h-5 rounded-full bg-white/15 text-white flex items-center justify-center">✓</span>
                  <h4 className="text-[14px] font-extrabold uppercase tracking-wider">Xác nhận thiết lập</h4>
                </div>
                <p className="text-[12px] font-medium leading-relaxed opacity-90">
                  Khi lưu, phòng sẽ được thêm vào hệ thống với trạng thái <strong>Trống</strong> và sẵn sàng phân bổ lịch.
                </p>
                <div className="flex flex-col gap-2.5 mt-2 text-[14px]">
                  <button type="submit" disabled={isSaving}
                    className="w-full bg-white hover:bg-slate-50 text-primary font-black py-3 rounded-xl flex items-center justify-center gap-2 cursor-pointer transition-all hover:scale-[1.01] shadow-md shadow-black/5 disabled:opacity-60 disabled:cursor-not-allowed">
                    <svg className="w-4.5 h-4.5" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 13.5V12m0 0V10.5m0 3h3m-3 0H6m12-5.528v10.528c0 .621-.504 1.125-1.125 1.125H3.125A1.125 1.125 0 012 16.875V3.125C2 2.504 2.504 2 3.125 2h9.728c.298 0 .585.118.796.328l4.019 4.018c.21.21.328.498.328.796z" />
                    </svg>
                    {isSaving ? "Đang lưu..." : "Lưu phòng"}
                  </button>
                  <Link href="/admin/rooms"
                    className="w-full border border-white/25 hover:border-white/50 hover:bg-white/5 text-white font-black py-3 rounded-xl flex items-center justify-center cursor-pointer transition-all text-center">
                    Hủy
                  </Link>
                </div>
              </div>

              {/* Card D: Mẹo */}
              <div className="bg-white p-4 rounded-xl border border-slate-200/60 shadow-sm border-l-4 border-l-red-500/80 flex items-start gap-3">
                <span className="text-[17px] text-primary shrink-0 mt-0.5 animate-pulse">💡</span>
                <div>
                  <span className="text-[11px] font-bold text-slate-400 block uppercase tracking-wider">Mẹo quản trị</span>
                  <p className="text-[12px] text-slate-500 font-semibold leading-relaxed mt-0.5">
                    Sau khi tạo phòng, bạn có thể phân bổ bác sĩ và phụ tá tại trang <strong>Lịch làm việc</strong>.
                  </p>
                </div>
              </div>
            </div>

          </form>
        </div>
      </main>
    </div>
  );
}
