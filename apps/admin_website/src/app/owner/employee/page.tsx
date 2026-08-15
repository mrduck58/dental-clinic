"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function OwnerEmployeeRedirectPage() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/owner/employee/dentists");
  }, [router]);

  return null;
}
