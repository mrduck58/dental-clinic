"use client";

import React, { useRef, useEffect, useState } from "react";
import { uploadFileApi, resolveAssetUrl } from "../../lib/apiClient";

interface RichTextEditorProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  minHeight?: string;
  maxHeight?: string;
}

export default function RichTextEditor({
  value,
  onChange,
  placeholder = "Viết bài giới thiệu / mô tả chi tiết về dịch vụ...",
  minHeight = "450px",
  maxHeight = "600px",
}: RichTextEditorProps) {
  const editorRef = useRef<HTMLDivElement>(null);
  const imageInputRef = useRef<HTMLInputElement>(null);
  const colorInputRef = useRef<HTMLInputElement>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [currentColor, setCurrentColor] = useState("#1E293B");
  const [activeFormats, setActiveFormats] = useState<Record<string, boolean>>({});

  // Sync value into innerHTML when value changes externally (and on mount)
  useEffect(() => {
    if (editorRef.current && editorRef.current.innerHTML !== value) {
      editorRef.current.innerHTML = value || "";
    }
  }, [value]);

  const handleInput = () => {
    if (editorRef.current) {
      onChange(editorRef.current.innerHTML);
      checkActiveFormats();
    }
  };

  const execCommand = (command: string, valueArg: string | undefined = undefined) => {
    if (editorRef.current) {
      editorRef.current.focus();
    }
    document.execCommand(command, false, valueArg);
    if (editorRef.current) {
      onChange(editorRef.current.innerHTML);
    }
    checkActiveFormats();
  };

  const checkActiveFormats = () => {
    try {
      setActiveFormats({
        bold: document.queryCommandState("bold"),
        italic: document.queryCommandState("italic"),
        underline: document.queryCommandState("underline"),
        strikeThrough: document.queryCommandState("strikeThrough"),
        insertUnorderedList: document.queryCommandState("insertUnorderedList"),
        insertOrderedList: document.queryCommandState("insertOrderedList"),
        justifyLeft: document.queryCommandState("justifyLeft"),
        justifyCenter: document.queryCommandState("justifyCenter"),
        justifyRight: document.queryCommandState("justifyRight"),
      });
    } catch {
      // ignore
    }
  };

  const handleHeading = (tag: string) => {
    if (tag === "p") {
      execCommand("formatBlock", "<p>");
    } else {
      execCommand("formatBlock", `<${tag}>`);
    }
  };

  const handleFontSize = (sizeVal: string) => {
    execCommand("fontSize", sizeVal);
  };

  const handleTextColor = (hexColor: string) => {
    setCurrentColor(hexColor);
    execCommand("foreColor", hexColor);
  };

  const handleAddLink = () => {
    const url = prompt("Nhập đường dẫn URL:");
    if (url) {
      execCommand("createLink", url);
    }
  };

  const handleInsertTable = () => {
    const rowsInput = prompt("Số hàng (rows):", "3");
    const colsInput = prompt("Số cột (columns):", "3");
    if (rowsInput === null || colsInput === null) return;
    const rows = Math.min(Math.max(parseInt(rowsInput) || 3, 1), 20);
    const cols = Math.min(Math.max(parseInt(colsInput) || 3, 1), 10);

    let tableHtml = '<div class="rich-table-wrapper" style="overflow-x:auto; margin: 16px 0;"><table style="width:100%; border-collapse: collapse; border: 1px solid #cbd5e1; font-size: 14px;"><thead><tr style="background-color: #f1f5f9;">';
    for (let c = 1; c <= cols; c++) {
      tableHtml += `<th style="padding: 10px 12px; border: 1px solid #cbd5e1; text-align: left; font-weight: 700;">Tiêu đề ${c}</th>`;
    }
    tableHtml += '</tr></thead><tbody>';
    for (let r = 1; r <= rows; r++) {
      const bg = r % 2 === 0 ? ' style="background-color: #f8fafc;"' : '';
      tableHtml += `<tr${bg}>`;
      for (let c = 1; c <= cols; c++) {
        tableHtml += `<td style="padding: 10px 12px; border: 1px solid #cbd5e1;">Nội dung ${r}.${c}</td>`;
      }
      tableHtml += '</tr>';
    }
    tableHtml += '</tbody></table></div><p><br></p>';

    if (editorRef.current) {
      editorRef.current.focus();
      execCommand("insertHTML", tableHtml);
    }
  };

  const handleDeleteTable = () => {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
      alert("Vui lòng nhấp con trỏ vào bên trong một ô của bảng bạn muốn xóa!");
      return;
    }

    let node: Node | null = selection.anchorNode;
    let targetElement: HTMLElement | null = null;

    while (node && node !== editorRef.current) {
      if (node.nodeType === Node.ELEMENT_NODE) {
        const el = node as HTMLElement;
        if (el.tagName === "TABLE" || el.classList?.contains("rich-table-wrapper")) {
          targetElement = el;
          break;
        }
      }
      node = node.parentNode;
    }

    if (targetElement) {
      // If target is <table> inside wrapper div, delete the wrapper
      if (targetElement.parentElement && targetElement.parentElement.classList.contains("rich-table-wrapper")) {
        targetElement = targetElement.parentElement;
      }
      targetElement.remove();
      if (editorRef.current) {
        onChange(editorRef.current.innerHTML);
      }
    } else {
      alert("Vui lòng đặt con trỏ vào bên trong ô của bảng bạn muốn xóa rồi bấm lại nút 'Xóa bảng'!");
    }
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploadingImage(true);
    try {
      const result = await uploadFileApi(file);
      const fullUrl = resolveAssetUrl(result.url);
      
      // Focus editor and insert image
      if (editorRef.current) {
        editorRef.current.focus();
        execCommand("insertImage", fullUrl);

        // Style the inserted image nicely
        const imgs = editorRef.current.querySelectorAll("img");
        imgs.forEach((img) => {
          if (!img.classList.contains("rounded-xl")) {
            img.className = "max-w-full h-auto rounded-xl my-4 shadow-sm border border-slate-200 block mx-auto";
          }
        });
        onChange(editorRef.current.innerHTML);
      }
    } catch (err) {
      alert(err instanceof Error ? err.message : "Tải hình ảnh thất bại");
    } finally {
      setIsUploadingImage(false);
      if (imageInputRef.current) imageInputRef.current.value = "";
    }
  };

  const PRESET_COLORS = [
    { label: "Đen xám", color: "#1E293B" },
    { label: "Đỏ Primary", color: "#E11D48" },
    { label: "Xanh Dương", color: "#2563EB" },
    { label: "Xanh Lá", color: "#059669" },
    { label: "Cam", color: "#D97706" },
    { label: "Tím", color: "#7C3AED" },
  ];

  return (
    <div className="flex flex-col border border-slate-200 rounded-xl overflow-hidden bg-white shadow-sm transition-all focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20">
      {/* Hidden file input for image upload */}
      <input
        ref={imageInputRef}
        type="file"
        accept="image/*"
        onChange={handleImageUpload}
        className="hidden"
      />

      {/* Hidden color input for custom color picker */}
      <input
        ref={colorInputRef}
        type="color"
        value={currentColor}
        onChange={(e) => handleTextColor(e.target.value)}
        className="hidden"
      />

      {/* Toolbar — onMouseDown preventDefault prevents editor focus loss */}
      <div className="flex flex-wrap items-center gap-1 p-2 bg-slate-50 border-b border-slate-200 select-none shrink-0">
        {/* Headings */}
        <select
          onChange={(e) => handleHeading(e.target.value)}
          className="h-8 px-2 text-xs font-bold bg-white border border-slate-200 rounded-lg text-slate-700 focus:outline-none cursor-pointer"
        >
          <option value="p">Kiểu chữ: Đoạn văn</option>
          <option value="h2">Tiêu đề 1 (H2)</option>
          <option value="h3">Tiêu đề 2 (H3)</option>
          <option value="h4">Tiêu đề 3 (H4)</option>
        </select>

        {/* Font Size */}
        <select
          onChange={(e) => handleFontSize(e.target.value)}
          defaultValue="3"
          className="h-8 px-2 text-xs font-bold bg-white border border-slate-200 rounded-lg text-slate-700 focus:outline-none cursor-pointer"
          title="Cỡ chữ"
        >
          <option value="1">Cỡ chữ: Rất nhỏ (10px)</option>
          <option value="2">Cỡ chữ: Nhỏ (13px)</option>
          <option value="3">Cỡ chữ: Vừa (16px)</option>
          <option value="4">Cỡ chữ: Lớn (18px)</option>
          <option value="5">Cỡ chữ: Rất lớn (24px)</option>
          <option value="6">Cỡ chữ: Khổng lồ (32px)</option>
        </select>

        <div className="w-[1px] h-5 bg-slate-200 mx-1" />

        {/* Color Picker & Preset Palette */}
        <div className="flex items-center gap-1">
          <button
            type="button"
            onMouseDown={(e) => e.preventDefault()}
            onClick={() => colorInputRef.current?.click()}
            title="Màu chữ tùy chọn (Color Picker)"
            className="h-8 px-2 rounded-lg bg-white border border-slate-200 hover:bg-slate-100 transition-colors flex items-center gap-1.5 cursor-pointer"
          >
            <span className="font-extrabold text-xs text-slate-800">A</span>
            <span
              className="w-3.5 h-3.5 rounded-full border border-slate-300 shrink-0"
              style={{ backgroundColor: currentColor }}
            />
          </button>

          <div className="hidden sm:flex items-center gap-1 bg-white border border-slate-200 rounded-lg p-1">
            {PRESET_COLORS.map((c) => (
              <button
                key={c.color}
                type="button"
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => handleTextColor(c.color)}
                title={`Màu ${c.label}`}
                className="w-5 h-5 rounded-md transition-transform hover:scale-115 cursor-pointer border border-slate-200 shrink-0"
                style={{ backgroundColor: c.color }}
              />
            ))}
          </div>
        </div>

        <div className="w-[1px] h-5 bg-slate-200 mx-1" />

        {/* Text Formats */}
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("bold")}
          title="In đậm (Ctrl+B)"
          className={`w-8 h-8 rounded-lg flex items-center justify-center font-black text-xs transition-colors ${
            activeFormats.bold ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          B
        </button>
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("italic")}
          title="In nghiêng (Ctrl+I)"
          className={`w-8 h-8 rounded-lg flex items-center justify-center font-serif italic text-sm transition-colors ${
            activeFormats.italic ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          I
        </button>
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("underline")}
          title="Gạch chân (Ctrl+U)"
          className={`w-8 h-8 rounded-lg flex items-center justify-center underline font-bold text-xs transition-colors ${
            activeFormats.underline ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          U
        </button>
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("strikeThrough")}
          title="Gạch ngang"
          className={`w-8 h-8 rounded-lg flex items-center justify-center line-through text-xs font-bold transition-colors ${
            activeFormats.strikeThrough ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          S
        </button>

        <div className="w-[1px] h-5 bg-slate-200 mx-1" />

        {/* Alignment */}
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("justifyLeft")}
          title="Căn trái"
          className={`w-8 h-8 rounded-lg flex items-center justify-center transition-colors ${
            activeFormats.justifyLeft ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h10.5m-10.5 5.25h16.5" />
          </svg>
        </button>
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("justifyCenter")}
          title="Căn giữa"
          className={`w-8 h-8 rounded-lg flex items-center justify-center transition-colors ${
            activeFormats.justifyCenter ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M6.75 12h10.5m-10.5 5.25h16.5" />
          </svg>
        </button>
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("justifyRight")}
          title="Căn phải"
          className={`w-8 h-8 rounded-lg flex items-center justify-center transition-colors ${
            activeFormats.justifyRight ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M9.75 12h10.5m-10.5 5.25h16.5" />
          </svg>
        </button>

        <div className="w-[1px] h-5 bg-slate-200 mx-1" />

        {/* Lists */}
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("insertUnorderedList")}
          title="Danh sách dấu chấm"
          className={`w-8 h-8 rounded-lg flex items-center justify-center transition-colors ${
            activeFormats.insertUnorderedList ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75zm0 5.25h.007v.008H3.75V12zm0 5.25h.007v.008H3.75v-.008z" />
          </svg>
        </button>
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => execCommand("insertOrderedList")}
          title="Danh sách số"
          className={`w-8 h-8 rounded-lg flex items-center justify-center transition-colors ${
            activeFormats.insertOrderedList ? "bg-primary text-white" : "text-slate-600 hover:bg-slate-200/70"
          }`}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75v.008H3.75V6.75zm0 5.25v.008H3.75V12zm0 5.25v.008H3.75v-.008z" />
          </svg>
        </button>

        <div className="w-[1px] h-5 bg-slate-200 mx-1" />

        {/* Link, Table & Image */}
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={handleAddLink}
          title="Chèn liên kết URL"
          className="w-8 h-8 rounded-lg flex items-center justify-center text-slate-600 hover:bg-slate-200/70 transition-colors"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M13.19 8.688a4.5 4.5 0 011.242 7.244l-4.5 4.5a4.5 4.5 0 01-6.364-6.364l1.757-1.757m13.35-.622l1.757-1.757a4.5 4.5 0 00-6.364-6.364l-4.5 4.5a4.5 4.5 0 001.242 7.244" />
          </svg>
        </button>

        {/* Table insert & Table delete */}
        <div className="flex items-center gap-1">
          <button
            type="button"
            onMouseDown={(e) => e.preventDefault()}
            onClick={handleInsertTable}
            title="Chèn bảng biểu dữ liệu mới"
            className="flex items-center gap-1 px-2.5 h-8 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 text-xs font-bold transition-all cursor-pointer border border-emerald-200/80"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6A2.25 2.25 0 016 3.75h12A2.25 2.25 0 0120.25 6v12A2.25 2.25 0 0118 20.25H6A2.25 2.25 0 013.75 18V6zM3.75 9h16.5m-16.5 6h16.5M9 3.75v16.5m6-16.5v16.5" />
            </svg>
            <span>+ Bảng</span>
          </button>
          <button
            type="button"
            onMouseDown={(e) => e.preventDefault()}
            onClick={handleDeleteTable}
            title="Xóa bảng hiện tại (Đặt con trỏ vào ô trong bảng rồi bấm nút này)"
            className="flex items-center gap-1 px-2.5 h-8 rounded-lg bg-rose-50 text-rose-700 hover:bg-rose-100 text-xs font-bold transition-all cursor-pointer border border-rose-200/80"
          >
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
            </svg>
            <span>Xóa bảng</span>
          </button>
        </div>

        {/* Upload image */}
        <button
          type="button"
          onMouseDown={(e) => e.preventDefault()}
          onClick={() => imageInputRef.current?.click()}
          disabled={isUploadingImage}
          title="Chèn hình ảnh vào bài viết"
          className="flex items-center gap-1 px-2.5 h-8 rounded-lg bg-primary/10 text-primary hover:bg-primary/20 text-xs font-bold transition-all disabled:opacity-50 cursor-pointer"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
          </svg>
          <span>{isUploadingImage ? "Đang tải..." : "+ Ảnh"}</span>
        </button>
      </div>

      {/* Editable Area — with strict max-height and internal scrollbar */}
      <div
        ref={editorRef}
        contentEditable
        onInput={handleInput}
        onKeyUp={checkActiveFormats}
        onMouseUp={checkActiveFormats}
        style={{ minHeight, maxHeight: maxHeight || "600px" }}
        className="p-4 text-[14px] leading-relaxed text-slate-800 focus:outline-none overflow-y-auto prose max-w-none font-sans rich-editor-content"
        data-placeholder={placeholder}
      />
    </div>
  );
}
