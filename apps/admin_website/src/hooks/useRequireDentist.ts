"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { getToken, getUser } from "../lib/apiClient";

export function useRequireDentist() {
  const router = useRouter();

  useEffect(() => {
    const token = getToken();
    const user = getUser();
    if (!token || !user) { router.replace("/auth/login"); return; }
    if (user.role !== "Dentist" && user.role !== "Admin") { router.replace("/auth/login"); }
  }, [router]);
}
