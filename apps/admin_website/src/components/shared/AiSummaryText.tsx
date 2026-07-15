/**
 * Render nhẹ cho văn bản do AI (Gemini) trả về ở dạng markdown tự do — chỉ hỗ trợ
 * đúng tập cú pháp AI hay dùng: tiêu đề "### ", in đậm "**...**", gạch đầu dòng
 * "* "/"- " (có thể lồng nhau qua thụt lề), và dòng phân cách "---". Không dùng thư
 * viện markdown đầy đủ vì input luôn là tóm tắt ngắn theo đúng khuôn mẫu prompt, không
 * cần hỗ trợ bảng/link/code block.
 */
function renderInline(text: string, keyPrefix: string) {
  const parts = text.split(/(\*\*[^*]+\*\*)/g).filter((p) => p.length > 0);
  return parts.map((part, i) => {
    if (part.startsWith("**") && part.endsWith("**")) {
      return (
        <strong key={`${keyPrefix}-${i}`} className="font-bold text-slate-900">
          {part.slice(2, -2)}
        </strong>
      );
    }
    return <span key={`${keyPrefix}-${i}`}>{part}</span>;
  });
}

export default function AiSummaryText({ text }: { text: string }) {
  const lines = text.replace(/\r\n/g, "\n").split("\n");
  const blocks: React.ReactNode[] = [];
  let listBuffer: { depth: number; content: string }[] = [];

  const flushList = () => {
    if (listBuffer.length === 0) return;
    const key = `list-${blocks.length}`;
    blocks.push(
      <ul key={key} className="flex flex-col gap-1.5">
        {listBuffer.map((item, i) => (
          <li
            key={`${key}-${i}`}
            className="flex gap-2 text-[12.5px] text-slate-700 leading-relaxed"
            style={{ marginLeft: item.depth * 16 }}
          >
            <span className="text-slate-400 mt-0.5">•</span>
            <span>{renderInline(item.content, `${key}-${i}`)}</span>
          </li>
        ))}
      </ul>,
    );
    listBuffer = [];
  };

  lines.forEach((rawLine, idx) => {
    const line = rawLine.trimEnd();
    const trimmed = line.trim();

    if (trimmed.length === 0) {
      flushList();
      return;
    }

    if (/^-{3,}$/.test(trimmed) || /^\*{3,}$/.test(trimmed)) {
      flushList();
      blocks.push(<hr key={`hr-${idx}`} className="border-slate-200" />);
      return;
    }

    const headingMatch = trimmed.match(/^#{1,6}\s+(.*)$/);
    if (headingMatch) {
      flushList();
      blocks.push(
        <p key={`h-${idx}`} className="text-[13px] font-black text-slate-900">
          {renderInline(headingMatch[1], `h-${idx}`)}
        </p>,
      );
      return;
    }

    const bulletMatch = line.match(/^(\s*)[*-]\s+(.*)$/);
    if (bulletMatch) {
      const depth = Math.floor(bulletMatch[1].length / 2);
      listBuffer.push({ depth, content: bulletMatch[2] });
      return;
    }

    flushList();
    blocks.push(
      <p key={`p-${idx}`} className="text-[12.5px] font-semibold text-slate-700 leading-relaxed">
        {renderInline(trimmed, `p-${idx}`)}
      </p>,
    );
  });

  flushList();

  return <div className="flex flex-col gap-2">{blocks}</div>;
}
