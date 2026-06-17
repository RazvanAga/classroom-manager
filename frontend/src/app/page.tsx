"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { fetchMe, logout } from "@/lib/api";

export default function HomePage() {
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: me, isLoading } = useQuery({ queryKey: ["me"], queryFn: fetchMe });

  const logoutMutation = useMutation({
    mutationFn: logout,
    onSuccess: () => queryClient.setQueryData(["me"], null),
  });

  useEffect(() => {
    if (!isLoading && me === null) {
      router.replace("/login");
    }
  }, [isLoading, me, router]);

  if (isLoading || !me) {
    return (
      <main className="shell">
        <div className="card greeting">Loading…</div>
      </main>
    );
  }

  return (
    <main className="shell">
      <div className="card greeting">
        <h1>Welcome back</h1>
        <p className="subtitle">
          Logged in as <span className="name">{me.displayName}</span>
        </p>
        <button
          className="btn-ghost"
          onClick={() => logoutMutation.mutate()}
          disabled={logoutMutation.isPending}
        >
          {logoutMutation.isPending ? "Signing out…" : "Sign out"}
        </button>
      </div>
    </main>
  );
}
