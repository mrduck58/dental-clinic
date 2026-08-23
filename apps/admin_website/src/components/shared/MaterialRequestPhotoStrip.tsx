"use client";

import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { getAppointmentPhotosApi, resolveAssetUrl, type AppointmentPhotoDto } from "../../lib/apiClient";

/**
 * Dải ảnh dấu răng/răng lợi... bác sĩ đính kèm khi gửi yêu cầu vật tư — chỉ xem, giúp staff đặt đúng
 * hàng theo ảnh. Chỉ đọc (không sửa/xoá) vì đây là ảnh của bác sĩ, không phải của staff.
 */
export default function MaterialRequestPhotoStrip({ appointmentId }: { appointmentId: string | null }) {
  const [photos, setPhotos] = useState<AppointmentPhotoDto[]>([]);
  const [preview, setPreview] = useState<AppointmentPhotoDto | null>(null);

  useEffect(() => {
    if (!appointmentId) return;
    let cancelled = false;
    getAppointmentPhotosApi(appointmentId, "material-request")
      .then(rows => { if (!cancelled) setPhotos(rows); })
      .catch(() => {});
    return () => { cancelled = true; };
  }, [appointmentId]);

  if (photos.length === 0) return null;

  return (
    <div className="mt-2 flex flex-col gap-1.5">
      <span className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">
        Ảnh bác sĩ gửi kèm ({photos.length})
      </span>
      <div className="flex gap-2 overflow-x-auto pb-1">
        {photos.map(p => (
          <button
            key={p.id}
            type="button"
            onClick={() => setPreview(p)}
            title={p.note ?? "Xem ảnh"}
            className="shrink-0 w-16 h-16 rounded-lg overflow-hidden border border-slate-200 hover:border-primary transition-colors cursor-pointer"
          >
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={resolveAssetUrl(p.url)} alt="Ảnh đính kèm" className="w-full h-full object-cover" />
          </button>
        ))}
      </div>

      {preview && typeof document !== "undefined" && createPortal(
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-[9999] bg-black/80 flex items-center justify-center p-6"
          onClick={() => setPreview(null)}
        >
          <div className="max-w-2xl max-h-[85vh] flex flex-col items-center gap-3" onClick={e => e.stopPropagation()}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={resolveAssetUrl(preview.url)} alt="Ảnh đính kèm" className="max-w-full max-h-[75vh] rounded-xl object-contain" />
            {preview.note && <p className="text-white text-[13px] font-semibold text-center">{preview.note}</p>}
            <button onClick={() => setPreview(null)} className="text-white/70 hover:text-white text-[13px] font-bold cursor-pointer">
              Đóng
            </button>
          </div>
        </div>,
        document.body
      )}
    </div>
  );
}
