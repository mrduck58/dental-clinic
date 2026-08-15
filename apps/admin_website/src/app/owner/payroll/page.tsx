"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function OwnerPayrollRedirectPage() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/owner/payroll/dentists");
  }, [router]);

  return null;
}
