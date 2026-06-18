"use client";

import { useState } from "react";
import StaffSidebar from "../../../components/shared/StaffSidebar";
import StaffPageHeader from "../../../components/shared/StaffPageHeader";
import { useRequireStaff } from "../../../hooks/useRequireStaff";

const REVIEWS = [
  { id: 1,  name: "Nguyễn Văn An",   date: "13/06/2026", rating: 5, service: "Nhổ răng khôn",       dentist: "BS. Thảo", comment: "Bác sĩ rất nhẹ nhàng và tận tình, không đau chút nào. Cơ sở sạch sẽ, nhân viên thân thiện. Sẽ quay lại lần sau!", recommend: true  },
  { id: 2,  name: "Trần Thị Bích",   date: "13/06/2026", rating: 4, service: "Trám răng số 6",      dentist: "BS. Minh", comment: "Dịch vụ tốt, bác sĩ giải thích rõ ràng trước khi làm. Chờ hơi lâu một chút nhưng chấp nhận được.", recommend: true  },
  { id: 3,  name: "Phạm Minh Cường", date: "12/06/2026", rating: 5, service: "Kiểm tra định kỳ",   dentist: "BS. Thảo", comment: "Phòng khám hiện đại, trang thiết bị mới. Bác sĩ nhiệt tình tư vấn kỹ về sức khỏe răng miệng.", recommend: true  },
  { id: 4,  name: "Lê Thu Hà",       date: "12/06/2026", rating: 3, service: "Tẩy trắng Zoom",     dentist: "BS. Linh", comment: "Kết quả khá tốt, tuy nhiên quy trình mất nhiều thời gian hơn dự kiến. Giá cũng hơi cao.", recommend: false },
  { id: 5,  name: "Hoàng Văn Đức",   date: "11/06/2026", rating: 5, service: "Cấy ghép Implant",   dentist: "BS. Minh", comment: "Cực kỳ hài lòng với kết quả implant. Bác sĩ Minh có tay nghề rất cao. Tôi đã giới thiệu cho bạn bè rồi.", recommend: true  },
  { id: 6,  name: "Vũ Thị Ngọc",     date: "11/06/2026", rating: 4, service: "Bọc răng sứ",        dentist: "BS. Linh", comment: "Màu răng sứ rất tự nhiên và khớp với răng thật. Hài lòng với kết quả thẩm mỹ.", recommend: true  },
  { id: 7,  name: "Đỗ Quang Huy",    date: "10/06/2026", rating: 2, service: "Lấy cao răng",       dentist: "BS. Thảo", comment: "Vẫn còn đau sau khi lấy cao răng được 2 ngày, không biết có bình thường không. Mong phòng khám tư vấn kỹ hơn.", recommend: false },
  { id: 8,  name: "Nguyễn Thị Mai",  date: "10/06/2026", rating: 5, service: "Chỉnh nha",          dentist: "BS. Minh", comment: "Phòng khám rất chuyên nghiệp. Quá trình chỉnh nha được theo dõi sát sao. Nhân viên lễ tân vui vẻ và nhiệt tình.", recommend: true  },
];

function Stars({ rating, size = "sm" }: { rating: number; size?: "sm"|"lg" }) {
  const w = size === "lg" ? "w-5 h-5" : "w-4 h-4";
  return (
    <div className="flex gap-0.5">
      {[1,2,3,4,5].map(i => (
        <svg key={i} className={`${w} ${i <= rating ? "text-amber-400" : "text-slate-200"}`} fill="currentColor" viewBox="0 0 24 24">
          <path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
        </svg>
      ))}
    </div>
  );
}

export default function FeedbackPage() {
  useRequireStaff();

  const [filter, setFilter] = useState(0);
  const [search, setSearch] = useState("");

  const avgRating = REVIEWS.reduce((sum, r) => sum + r.rating, 0) / REVIEWS.length;
  const recommend = REVIEWS.filter(r => r.recommend).length;
  const distribution = [5,4,3,2,1].map(s => ({ star: s, count: REVIEWS.filter(r => r.rating === s).length }));

  const filtered = REVIEWS.filter(r => {
    const matchStar   = filter === 0 || r.rating === filter;
    const matchSearch = !search || r.name.toLowerCase().includes(search.toLowerCase()) || r.service.toLowerCase().includes(search.toLowerCase());
    return matchStar && matchSearch;
  });

  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <StaffSidebar activeMenu="feedback" />
      <main className="flex-1 flex flex-col min-w-0">
        <StaffPageHeader title="Phản Hồi & Đánh Giá" subtitle="Xem đánh giá của bệnh nhân về dịch vụ" />

        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-5">
          {/* Stats row */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5 shrink-0">
            {/* Average rating */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex items-center gap-6">
              <div className="text-center shrink-0">
                <div className="text-5xl font-black text-slate-900">{avgRating.toFixed(1)}</div>
                <Stars rating={Math.round(avgRating)} size="lg" />
                <div className="text-[12px] text-slate-400 font-semibold mt-1">{REVIEWS.length} đánh giá</div>
              </div>
              <div className="flex-1 flex flex-col gap-1.5">
                {distribution.map(({ star, count }) => (
                  <div key={star} className="flex items-center gap-2">
                    <span className="text-[12px] font-bold text-slate-500 w-3 shrink-0">{star}</span>
                    <svg className="w-3 h-3 text-amber-400 shrink-0" fill="currentColor" viewBox="0 0 24 24"><path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" /></svg>
                    <div className="flex-1 bg-slate-100 rounded-full h-2 overflow-hidden">
                      <div className="h-full bg-amber-400 rounded-full transition-all" style={{ width: `${(count / REVIEWS.length) * 100}%` }} />
                    </div>
                    <span className="text-[12px] font-semibold text-slate-400 w-4 text-right">{count}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Recommend rate */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col items-center justify-center gap-2">
              <div className="w-16 h-16 rounded-2xl bg-emerald-50 border border-emerald-100 flex items-center justify-center">
                <svg className="w-8 h-8 text-emerald-600" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6.633 10.5c.806 0 1.533-.446 2.031-1.08a9.041 9.041 0 012.861-2.4c.723-.384 1.35-.956 1.653-1.715a4.498 4.498 0 00.322-1.672V3a.75.75 0 01.75-.75A2.25 2.25 0 0116.5 4.5c0 1.152-.26 2.243-.723 3.218-.266.558.107 1.282.725 1.282h3.126c1.026 0 1.945.694 2.054 1.715.045.422.068.85.068 1.285a11.95 11.95 0 01-2.649 7.521c-.388.482-.987.729-1.605.729H13.48c-.483 0-.964-.078-1.423-.23l-3.114-1.04a4.501 4.501 0 00-1.423-.23H5.904M14.25 9h2.25M5.904 18.75c.083.205.173.405.27.602.197.4-.078.898-.523.898h-.908c-.889 0-1.713-.518-1.972-1.368a12 12 0 01-.521-3.507c0-1.553.295-3.036.831-4.398C3.387 10.203 4.167 9.75 5 9.75h1.053c.472 0 .745.556.5.96a8.958 8.958 0 00-1.302 4.665c0 1.194.232 2.333.654 3.375z" />
                </svg>
              </div>
              <div className="text-3xl font-black text-emerald-600">{Math.round((recommend / REVIEWS.length) * 100)}%</div>
              <div className="text-[13px] font-bold text-slate-700 text-center">Sẵn sàng giới thiệu</div>
              <div className="text-[12px] text-slate-400 font-medium">{recommend}/{REVIEWS.length} bệnh nhân</div>
            </div>

            {/* Total this month */}
            <div className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-6 flex flex-col justify-between">
              <div className="text-[12px] font-extrabold text-slate-400 uppercase tracking-wider">Tháng này</div>
              <div className="text-4xl font-black text-slate-900 mt-2">{REVIEWS.length}</div>
              <div className="text-[13px] text-slate-500 font-semibold">đánh giá mới</div>
              <div className="flex items-center gap-1.5 mt-3 text-[12.5px] font-bold text-emerald-600">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M2.25 18L9 11.25l4.306 4.307a11.95 11.95 0 015.814-5.519l2.74-1.22m0 0l-5.94-2.28m5.94 2.28l-2.28 5.941" /></svg>
                +12% so với tháng trước
              </div>
            </div>
          </div>

          {/* Filters */}
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative min-w-[200px]">
              <svg className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" /></svg>
              <input value={search} onChange={e => setSearch(e.target.value)}
                placeholder="Tìm tên, dịch vụ..."
                className="w-full pl-9 pr-3 py-2 text-[13px] border border-slate-200 rounded-xl bg-white focus:outline-none focus:border-primary/50" />
            </div>
            <div className="flex gap-2">
              {[0,5,4,3,2,1].map(s => (
                <button key={s} onClick={() => setFilter(s)}
                  className={`px-3 py-1.5 rounded-xl text-[12.5px] font-bold border transition-all cursor-pointer flex items-center gap-1 ${
                    filter === s ? "bg-primary text-white border-primary" : "bg-white text-slate-500 border-slate-200 hover:border-primary/40 hover:text-primary"
                  }`}>
                  {s === 0 ? "Tất cả" : <><svg className="w-3 h-3" fill="currentColor" viewBox="0 0 24 24"><path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" /></svg>{s} sao</>}
                </button>
              ))}
            </div>
          </div>

          {/* Reviews */}
          <div className="flex flex-col gap-3">
            {filtered.map(r => (
              <div key={r.id} className="bg-white rounded-2xl border border-slate-200/60 shadow-sm p-5">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-full bg-sky-50 border border-sky-100 flex items-center justify-center font-black text-secondary shrink-0">
                      {r.name.split(" ").slice(-1)[0][0]}
                    </div>
                    <div>
                      <div className="text-[14px] font-bold text-slate-900">{r.name}</div>
                      <div className="text-[12px] text-slate-400 font-medium">{r.date} · {r.service} · {r.dentist}</div>
                    </div>
                  </div>
                  <div className="flex flex-col items-end gap-1.5 shrink-0">
                    <Stars rating={r.rating} />
                    {r.recommend ? (
                      <span className="px-2 py-0.5 bg-emerald-50 text-emerald-700 border border-emerald-100 rounded-full text-[11px] font-black">👍 Đề xuất</span>
                    ) : (
                      <span className="px-2 py-0.5 bg-slate-100 text-slate-500 border border-slate-200 rounded-full text-[11px] font-semibold">Không đề xuất</span>
                    )}
                  </div>
                </div>
                {r.comment && (
                  <p className="mt-3 text-[13.5px] text-slate-600 font-medium leading-relaxed pl-[52px]">{r.comment}</p>
                )}
              </div>
            ))}
            {filtered.length === 0 && (
              <div className="bg-white rounded-2xl border border-slate-200/60 p-12 text-center text-slate-400 font-semibold text-[13px]">
                Không tìm thấy đánh giá nào
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
