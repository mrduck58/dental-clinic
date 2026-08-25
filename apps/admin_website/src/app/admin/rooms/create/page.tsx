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
              <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
              </svg>
              {errorMsg}
            </div>
          )}

          <form onSubmit={handleSubmit} className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">

            {/* ── LEFT (8/12) ───────────────────────────────────────────────── */}
            <div className="lg:col-span-8 flex flex-col gap-6">

              {/* Card A: Thông tin cơ bản */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                  <svg className="w-4.5 h-4.5 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
                  </svg>
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
              </div>

              {/* Card B: Mô tả */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200/60 shadow-sm flex flex-col gap-5">
                <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
                  <svg className="w-4.5 h-4.5 text-primary" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" />
                  </svg>
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
                  <span className="w-5 h-5 rounded-full bg-white/15 text-white flex items-center justify-center shrink-0">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                    </svg>
                  </span>
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
                <svg className="w-4.5 h-4.5 text-primary shrink-0 mt-0.5 animate-pulse" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 18v-5.25m0 0a6.01 6.01 0 001.5-.189m-1.5.189a6.01 6.01 0 01-1.5-.189m3.75 7.478a12.06 12.06 0 01-4.5 0m3.75 2.383a14.406 14.406 0 01-3 0M14.25 18v-.192c0-.983.658-1.823 1.508-2.316a7.5 7.5 0 10-7.517 0c.85.493 1.509 1.333 1.509 2.316V18" />
                </svg>
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
