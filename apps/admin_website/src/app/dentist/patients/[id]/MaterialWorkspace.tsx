"use client";

import { useState, useEffect, useCallback } from "react";
import {
  getExaminationApi,
  createMaterialRequestApi,
  getMaterialRequestsByPatientApi,
  type ExaminationDto,
  type MaterialRequestDto,
} from "../../../../lib/apiClient";

interface MaterialWorkspaceProps {
  appointmentId: string;
  /** Cho phép gửi yêu cầu dù buổi hẹn đã kết thúc (chế độ chỉnh sửa đơn hoàn thành). */
  editMode?: boolean;
}

const fmtDateTime = (iso: string) => {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()} ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
};

function CardHeader({ title, icon, color, action }: {
  title: string; icon: string; color: string; action?: React.ReactNode;
}) {
  return (
    <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-2">
      <svg className={`w-5 h-5 ${color}`} fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
      </svg>
      <span className="text-[14px] font-black text-slate-900">{title}</span>
      {action && <div className="ml-auto">{action}</div>}
    </div>
  );
}

const BOX_ICON = "M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z";

export default function MaterialWorkspace({ appointmentId, editMode = false }: MaterialWorkspaceProps) {
  const [examination, setExamination] = useState<ExaminationDto | null>(null);
  const [requests, setRequests] = useState<MaterialRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [content, setContent] = useState("");
  const [saving, setSaving] = useState(false);

  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);
  const showToast = (message: string, type: "success" | "error" = "success") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  const canEdit = examination?.status === "InProgress" || editMode;

  const loadRequests = useCallback(async (patientId: string, patientName: string) => {
    setRequests(await getMaterialRequestsByPatientApi(patientId, patientName).catch(() => []));
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const exam = await getExaminationApi(appointmentId);
        if (cancelled) return;
        setExamination(exam);
        setError(null);
        await loadRequests(exam.patient.id, exam.patient.fullName);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Không thể tải thông tin bệnh nhân");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [appointmentId, loadRequests]);

  const handleSubmit = async () => {
    if (!content.trim() || !examination) return;
    try {
      setSaving(true);
      await createMaterialRequestApi({ appointmentId, content: content.trim() });
      showToast("Đã gửi yêu cầu vật tư sang kho");
      setContent("");
      await loadRequests(examination.patient.id, examination.patient.fullName);
    } catch (err) {
      showToast(err instanceof Error ? err.message : "Không thể gửi yêu cầu", "error");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center py-24">
        <div className="w-8 h-8 border-3 border-primary/20 border-t-primary rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !examination) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-4 py-24">
        <p className="text-[14px] font-semibold text-red-500">{error ?? "Đã xảy ra lỗi hệ thống."}</p>
      </div>
    );
  }

  const pt = examination.patient;
  const initials = pt.fullName.trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();

  return (
    <div>
      <div className="grid gap-6 items-start" style={{ gridTemplateColumns: "2fr 1fr" }}>
        {/* ══════════ LEFT 2/3: PATIENT + REQUEST HISTORY ══════════ */}
        <div className="flex flex-col gap-6">
          {/* Thông tin bệnh nhân */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-sky-600"
              title="Thông tin bệnh nhân"
              icon="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"
            />
            <div className="p-5 flex items-start gap-4">
              <div className={`w-16 h-16 rounded-2xl flex items-center justify-center font-black text-xl border-2 shrink-0 ${pt.gender === "Nữ" ? "bg-rose-50 text-rose-500 border-rose-100" : "bg-sky-50 text-sky-600 border-sky-100"}`}>
                {initials}
              </div>
              <div className="flex-1 grid grid-cols-3 gap-3">
                {[
                  ["Họ tên", pt.fullName],
                  ["Giới tính", pt.gender ?? "—"],
                  ["Điện thoại", pt.phoneNumber ?? "—"],
                  ["Dịch vụ", examination.serviceName ?? "Khám tổng quát"],
                ].map(([label, value]) => (
                  <div key={label}>
                    <div className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider">{label}</div>
                    <div className="text-[13px] font-bold text-slate-800 mt-0.5 break-words">{value}</div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Lịch sử yêu cầu vật tư */}
          <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
            <CardHeader
              color="text-amber-600"
              title="Yêu cầu vật tư đã gửi"
              icon={BOX_ICON}
              action={
                <span className="text-[11px] font-bold px-2.5 py-1 rounded-lg bg-slate-100 text-slate-500">{requests.length} yêu cầu</span>
              }
            />
            <div className="px-5 py-2">
              {requests.length === 0 ? (
                <p className="text-[13px] font-semibold text-slate-400 text-center py-8">Chưa gửi yêu cầu vật tư nào cho bệnh nhân này.</p>
              ) : (
                <div className="divide-y divide-slate-100">
                  {requests.map(r => (
                    <div key={r.id} className="py-3.5">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-[13px] font-black text-slate-800">{r.courseName || "Yêu cầu vật tư"}</span>
                        <span className={`text-[10.5px] font-black px-2 py-0.5 rounded-md uppercase tracking-wide ${r.status === "Done" ? "bg-emerald-50 text-emerald-700 border border-emerald-200" : "bg-amber-50 text-amber-700 border border-amber-200"}`}>
                          {r.status === "Done" ? "Đã xử lý" : "Chờ kho xử lý"}
                        </span>
                        <span className="ml-auto text-[11.5px] font-semibold text-slate-400 font-mono">{fmtDateTime(r.createdAt)}</span>
                      </div>
                      <p className="text-[13px] font-semibold text-slate-600 whitespace-pre-wrap bg-slate-50 border border-slate-100 rounded-xl px-3.5 py-2.5">{r.content}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* ══════════ RIGHT 1/3: NEW REQUEST FORM ══════════ */}
        <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm overflow-hidden">
          <CardHeader color="text-emerald-600" title="Gửi yêu cầu vật tư" icon={BOX_ICON} />
          <div className="p-5 flex flex-col gap-4">
            <div>
              <label className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Danh sách vật tư cần dùng</label>
              <textarea
                value={content}
                onChange={e => setContent(e.target.value)}
                rows={7}
                placeholder={"VD:\n- Composite A2 x 2\n- Kim tiêm nha khoa x 5\n- Chỉ khâu 4/0 x 1"}
                disabled={!canEdit}
                className="w-full mt-1.5 px-3.5 py-2.5 text-[13px] bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:border-primary focus:outline-none font-semibold text-slate-700 resize-none disabled:opacity-60 disabled:cursor-not-allowed"
              />
            </div>

            <button
              onClick={() => void handleSubmit()}
              disabled={!canEdit || !content.trim() || saving}
              title={canEdit ? undefined : "Chỉ gửi yêu cầu khi buổi hẹn đang khám"}
              className="w-full py-3 bg-emerald-600 text-white text-[14px] font-black rounded-xl hover:bg-emerald-700 transition-all shadow-sm shadow-emerald-500/25 disabled:bg-slate-200 disabled:text-slate-400 disabled:shadow-none disabled:cursor-not-allowed cursor-pointer"
            >
              {saving ? "Đang gửi..." : "Gửi sang kho"}
            </button>

            <p className="text-[11.5px] font-semibold text-slate-400 leading-relaxed">
              Yêu cầu sẽ hiện ở tab <span className="font-black text-slate-500">&quot;Yêu cầu vật tư&quot;</span> trong trang Nhập–xuất vật tư của nhân viên để xử lý.
            </p>
            {!canEdit && (
              <p className="text-[11.5px] font-semibold text-amber-600 text-center">
                Buổi hẹn chưa ở trạng thái đang khám — chỉ xem.
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Toast */}
      {toast && (
        <div className={`fixed bottom-6 right-6 z-[60] px-5 py-3.5 rounded-xl shadow-lg text-[13.5px] font-bold text-white ${toast.type === "success" ? "bg-emerald-600" : "bg-red-600"}`}>
          {toast.message}
        </div>
      )}
    </div>
  );
}
