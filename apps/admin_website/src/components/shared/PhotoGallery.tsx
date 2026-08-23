"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  addAppointmentPhotoApi,
  deleteAppointmentPhotoApi,
  getAppointmentPhotosApi,
  resolveAssetUrl,
  updateAppointmentPhotoNoteApi,
  uploadFileApi,
  type AppointmentPhotoDto,
} from "../../lib/apiClient";
import { Toast, useToast } from "./Toast";
import { ConfirmDialog, useConfirm } from "./ConfirmDialog";

interface PhotoGalleryProps {
  appointmentId: string;
  section: string;
  title: string;
}

/**
 * Ảnh chụp tay (X-quang, dấu răng, răng lợi...) gắn với 1 buổi hẹn — không có máy tích hợp nên chỉ
 * upload ảnh thường (giống các nơi khác trong app) kèm ghi chú tuỳ chọn mỗi ảnh. Dùng chung cho tab
 * "Khám" (section="exam") và tab "Vật tư" (section="material-request").
 */
export default function PhotoGallery({ appointmentId, section, title }: PhotoGalleryProps) {
  const [photos, setPhotos] = useState<AppointmentPhotoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [notes, setNotes] = useState<Record<string, string>>({});
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { toast, showToast } = useToast();
  const { confirm, confirmState, closeConfirm } = useConfirm();

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const result = await getAppointmentPhotosApi(appointmentId, section);
      setPhotos(result);
      setNotes(Object.fromEntries(result.map(p => [p.id, p.note ?? ""])));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải ảnh");
    } finally {
      setLoading(false);
    }
  }, [appointmentId, section]);

  useEffect(() => { void load(); }, [load]);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const { url } = await uploadFileApi(file);
      const photo = await addAppointmentPhotoApi(appointmentId, { section, url });
      setPhotos(prev => [photo, ...prev]);
      setNotes(prev => ({ ...prev, [photo.id]: "" }));
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Tải ảnh lên thất bại", "error");
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleSaveNote = async (photoId: string) => {
    const current = photos.find(p => p.id === photoId);
    const note = notes[photoId] ?? "";
    if (current && (current.note ?? "") === note) return;
    try {
      const updated = await updateAppointmentPhotoNoteApi(photoId, note || undefined);
      setPhotos(prev => prev.map(p => (p.id === photoId ? updated : p)));
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể lưu ghi chú", "error");
    }
  };

  const handleDelete = async (photoId: string) => {
    const ok = await confirm({ title: "Xóa ảnh này?", message: "Không thể hoàn tác.", confirmLabel: "Xóa" });
    if (!ok) return;
    try {
      await deleteAppointmentPhotoApi(photoId);
      setPhotos(prev => prev.filter(p => p.id !== photoId));
      setNotes(prev => { const next = { ...prev }; delete next[photoId]; return next; });
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể xóa ảnh", "error");
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
        <svg className="w-5 h-5 text-sky-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M6.827 6.175A2.31 2.31 0 015.186 7.23c-.38.054-.757.112-1.134.174C3.023 7.58 2.25 8.507 2.25 9.574V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9.574c0-1.067-.773-1.994-1.802-2.169a47.865 47.865 0 00-1.134-.175 2.31 2.31 0 01-1.64-1.055l-.822-1.316a2.192 2.192 0 00-1.736-1.039 48.774 48.774 0 00-4.352 0 2.192 2.192 0 00-1.736 1.039l-.821 1.316z" />
          <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 12.75a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0z" />
        </svg>
        <span className="text-[14px] font-black text-slate-900">{title}</span>
        <span className="text-[11px] font-bold px-2 py-0.5 bg-sky-50 text-sky-700 rounded-lg border border-sky-200 ml-1">{photos.length}</span>
        <div className="ml-auto">
          <input ref={fileInputRef} type="file" accept="image/*" className="hidden" onChange={e => void handleFileChange(e)} />
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            disabled={uploading}
            className="px-3 py-1.5 rounded-lg bg-primary text-white text-[11.5px] font-bold hover:opacity-90 disabled:opacity-50 transition-all cursor-pointer"
          >
            {uploading ? "Đang tải..." : "+ Thêm ảnh"}
          </button>
        </div>
      </div>

      <div className="p-5">
        {loading && (
          <div className="flex items-center justify-center py-6">
            <div className="w-5 h-5 border-2 border-slate-200 border-t-primary rounded-full animate-spin" />
          </div>
        )}

        {!loading && error && (
          <div className="flex flex-col gap-2">
            <span className="text-[12.5px] font-semibold text-red-600">{error}</span>
            <button onClick={() => void load()} className="self-start text-[12px] font-bold text-primary hover:underline cursor-pointer">
              Thử lại
            </button>
          </div>
        )}

        {!loading && !error && photos.length === 0 && (
          <span className="text-[12.5px] text-slate-400">Chưa có ảnh nào. Bấm &quot;Thêm ảnh&quot; để tải lên.</span>
        )}

        {!loading && !error && photos.length > 0 && (
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
            {photos.map(p => (
              <div key={p.id} className="flex flex-col gap-1.5">
                <div className="relative group rounded-xl overflow-hidden border border-slate-200 aspect-square bg-slate-50">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={resolveAssetUrl(p.url)} alt="Ảnh chụp" className="w-full h-full object-cover" />
                  <button
                    type="button"
                    onClick={() => void handleDelete(p.id)}
                    title="Xóa ảnh"
                    className="absolute top-1.5 right-1.5 w-7 h-7 flex items-center justify-center rounded-lg bg-white/90 text-slate-500 hover:text-red-600 shadow-sm opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                    </svg>
                  </button>
                </div>
                <input
                  value={notes[p.id] ?? ""}
                  onChange={e => setNotes(prev => ({ ...prev, [p.id]: e.target.value }))}
                  onBlur={() => void handleSaveNote(p.id)}
                  placeholder="Ghi chú..."
                  className="w-full px-2.5 py-1.5 text-[11.5px] bg-slate-50 border border-slate-200 rounded-lg focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-600"
                />
              </div>
            ))}
          </div>
        )}
      </div>

      <Toast toast={toast} />
      <ConfirmDialog state={confirmState} onClose={closeConfirm} />
    </div>
  );
}
