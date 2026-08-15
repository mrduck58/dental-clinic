"use client";

export type SortDir = "asc" | "desc";
type Align = "left" | "right" | "center";

const TH_CLASS = "py-3 font-extrabold text-slate-400 text-[11px] uppercase tracking-wider";
const ALIGN_CLASS: Record<Align, string> = {
  left: "text-left", right: "text-right", center: "text-center",
};

interface ThProps {
  children: React.ReactNode;
  align?: Align;
  /** Padding ngang — mặc định "px-4", truyền "px-6" v.v. để khớp với padding của <td> trong cùng bảng. */
  className?: string;
}

/** Ô tiêu đề bảng không sắp xếp được (vd. cột "Thao tác") — cùng kiểu chữ với SortableTh. */
export function Th({ children, align = "left", className = "px-4" }: ThProps) {
  return <th className={`${className} ${TH_CLASS} ${ALIGN_CLASS[align]}`}>{children}</th>;
}

interface SortableThProps<K extends string> {
  column: K;
  label: string;
  sortKey: K;
  sortDir: SortDir;
  onSort: (column: K) => void;
  align?: Align;
  /** Padding ngang — mặc định "px-4", truyền "px-6" v.v. để khớp với padding của <td> trong cùng bảng. */
  className?: string;
}

/** Ô tiêu đề bảng có thể bấm để sắp xếp tăng/giảm — mũi tên trung tính khi chưa chọn, đổi màu primary khi đang sắp xếp theo cột đó. */
export function SortableTh<K extends string>({
  column, label, sortKey, sortDir, onSort, align = "left", className = "px-4",
}: SortableThProps<K>) {
  const active = sortKey === column;
  return (
    <th className={`${className} ${TH_CLASS} ${ALIGN_CLASS[align]}`}>
      <button
        onClick={() => onSort(column)}
        className={`inline-flex items-center gap-1 uppercase cursor-pointer hover:text-slate-600 transition-colors ${
          active ? "text-slate-600" : ""
        }`}
        title={`Sắp xếp theo ${label}`}
      >
        {label}
        <span className={`inline-flex items-center ml-0.5 ${active ? "text-primary" : "text-slate-300"}`}>
          {active ? (
            sortDir === "asc" ? (
              <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M4.5 15.75l7.5-7.5 7.5 7.5" /></svg>
            ) : (
              <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" /></svg>
            )
          ) : (
            <svg className="w-3 h-3" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M8.25 15L12 18.75 15.75 15m-7.5-6L12 5.25 15.75 9" /></svg>
          )}
        </span>
      </button>
    </th>
  );
}

/**
 * Toggle sort tiêu chuẩn: bấm cột đang sort thì đảo chiều; bấm cột khác thì đổi cột và
 * áp chiều mặc định (chữ/ngày → tăng dần A→Z hợp lý hơn; số/tiền/trạng thái → giảm dần
 * để thấy giá trị lớn nhất trước — truyền qua `descendingByDefault`).
 */
export function toggleSortState<K extends string>(
  current: { key: K; dir: SortDir },
  column: K,
  descendingByDefault: (column: K) => boolean,
): { key: K; dir: SortDir } {
  if (current.key === column) {
    return { key: column, dir: current.dir === "asc" ? "desc" : "asc" };
  }
  return { key: column, dir: descendingByDefault(column) ? "desc" : "asc" };
}
