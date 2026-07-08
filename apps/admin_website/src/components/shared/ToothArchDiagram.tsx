"use client";

export type ToothStatus = "normal" | "decay" | "filled" | "missing" | "crown" | "implant";
export type ToothState  = Record<string, ToothStatus>;

const ATW = 26, ATH = 32, ACURVE = 34, AVPAD = 8, AGAP = 12;
export const ARCH_H = AVPAD + ACURVE + ATH + AGAP + ACURVE + ATH + AVPAD; // 160px
const U_BASE = AVPAD;
const L_BASE = AVPAD + ACURVE + ATH + AGAP + ACURVE;

export const UPPER_TEETH = ["18","17","16","15","14","13","12","11","21","22","23","24","25","26","27","28"];
export const LOWER_TEETH = ["48","47","46","45","44","43","42","41","31","32","33","34","35","36","37","38"];

export const TOOTH_COLOR: Record<ToothStatus | "selected", string> = {
  normal:   "bg-white border-slate-300 text-slate-600",
  decay:    "bg-red-100 border-red-400 text-red-700",
  filled:   "bg-amber-100 border-amber-400 text-amber-700",
  missing:  "bg-slate-200 border-slate-300 text-slate-400",
  crown:    "bg-sky-100 border-sky-400 text-sky-700",
  implant:  "bg-violet-100 border-violet-400 text-violet-700",
  selected: "bg-primary border-primary text-white shadow-sm shadow-primary/40",
};

export const TOOTH_LEGEND = [
  { status: "normal",  label: "Bình thường" },
  { status: "decay",   label: "Sâu răng"    },
  { status: "filled",  label: "Đã trám"     },
  { status: "missing", label: "Mất răng"    },
  { status: "crown",   label: "Bọc sứ"      },
  { status: "implant", label: "Implant"     },
] as const;

function archPos(idx: number, baseY: number, upper: boolean, count: number) {
  const t = idx / (count - 1);
  const dip = 4 * t * (1 - t) * ACURVE;
  return { xPct: 4 + t * 92, y: upper ? baseY + dip : baseY - dip, rot: (0.5 - t) * 26 };
}

interface Props {
  teeth?:        ToothState;
  selected?:     Set<string>;
  onToothClick?: (tooth: string) => void;
  showLegend?:   boolean;
  readonly?:     boolean;
}

export default function ToothArchDiagram({
  teeth       = {},
  selected    = new Set(),
  onToothClick,
  showLegend  = true,
  readonly    = false,
}: Props) {

  const renderTooth = (num: string, idx: number, upper: boolean) => {
    const count = upper ? UPPER_TEETH.length : LOWER_TEETH.length;
    const { xPct, y, rot } = archPos(idx, upper ? U_BASE : L_BASE, upper, count);
    const isSelected = selected.has(num);
    const status     = isSelected ? "selected" : (teeth[num] ?? "normal");
    const origin     = upper ? "center top" : "center bottom";
    return (
      <button
        key={num}
        type="button"
        disabled={readonly}
        onClick={() => onToothClick?.(num)}
        style={{
          position: "absolute",
          left: `${xPct}%`,
          top: y,
          width: ATW,
          height: ATH,
          transform: `translateX(-50%) rotate(${rot}deg)`,
          transformOrigin: origin,
        }}
        className={`rounded-md border-2 flex flex-col items-center justify-center transition-all z-0
          ${readonly ? "cursor-default" : "cursor-pointer hover:z-10 hover:scale-125"}
          ${TOOTH_COLOR[status]}`}
        title={`Răng ${num}`}
      >
        <span className="font-extrabold text-[11px] leading-none">{num}</span>
      </button>
    );
  };

  return (
    <div className="flex flex-col gap-2.5">
      {showLegend && (
        <div className="flex items-center gap-3 flex-wrap">
          {TOOTH_LEGEND.map(({ status, label }) => (
            <span key={status} className="flex items-center gap-1.5 text-[11.5px] font-semibold text-slate-500">
              <span className={`w-3 h-3 rounded border ${TOOTH_COLOR[status].split(" ")[0]} inline-block`} />
              {label}
            </span>
          ))}
        </div>
      )}

      <div className="relative w-full select-none" style={{ height: ARCH_H }}>
        <svg
          className="absolute inset-0 w-full pointer-events-none"
          style={{ height: ARCH_H }}
          viewBox={`0 0 100 ${ARCH_H}`}
          preserveAspectRatio="none"
        >
          {/* upper gumline */}
          <path d={`M 4 ${U_BASE + ATH * 0.55} Q 50 ${U_BASE + ACURVE + ATH * 0.55} 96 ${U_BASE + ATH * 0.55}`}
            fill="none" stroke="#fda4af" strokeWidth="1.4" opacity="0.5" />
          {/* lower gumline */}
          <path d={`M 4 ${L_BASE + ATH * 0.45} Q 50 ${L_BASE - ACURVE + ATH * 0.45} 96 ${L_BASE + ATH * 0.45}`}
            fill="none" stroke="#fda4af" strokeWidth="1.4" opacity="0.5" />
          {/* midline */}
          <line x1="50" y1={AVPAD} x2="50" y2={ARCH_H - AVPAD}
            stroke="#cbd5e1" strokeWidth="0.6" strokeDasharray="3 2" />
          {/* arch labels */}
          <text x="1" y={U_BASE + ATH * 0.5 + 4} fontSize="5" fill="#94a3b8" fontWeight="700">HT</text>
          <text x="1" y={L_BASE + ATH * 0.5 + 4} fontSize="5" fill="#94a3b8" fontWeight="700">HD</text>
        </svg>

        {UPPER_TEETH.map((num, i) => renderTooth(num, i, true))}
        {LOWER_TEETH.map((num, i) => renderTooth(num, i, false))}
      </div>
    </div>
  );
}
