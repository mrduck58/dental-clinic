"use client";

import React from "react";
import Sidebar from "../../components/Sidebar";
import Header from "../../components/Header";

export default function ManagerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="animate-fade-in flex min-h-screen bg-slate-50 font-sans text-slate-800">
      <Sidebar />
      <main className="flex-1 flex flex-col min-w-0">
        <Header />
        <div className="p-8 flex-1 overflow-y-auto flex flex-col gap-8">
          {children}
        </div>
      </main>
    </div>
  );
}
