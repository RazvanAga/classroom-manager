"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { demoLogin, fetchMe, login } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("teacher@classroom.local");
  const [password, setPassword] = useState("");

  const onAuthed = async () => {
    const me = await fetchMe();
    queryClient.setQueryData(["me"], me);
    router.replace("/");
  };

  const loginMutation = useMutation({
    mutationFn: () => login(email, password),
    onSuccess: onAuthed,
  });

  const demoMutation = useMutation({
    mutationFn: demoLogin,
    onSuccess: onAuthed,
  });

  return (
    <main className="shell">
      <form
        className="card"
        onSubmit={(e) => {
          e.preventDefault();
          loginMutation.mutate();
        }}
      >
        <h1>Classroom Manager</h1>
        <p className="subtitle">Sign in to manage your classes.</p>

        <label htmlFor="email">Email</label>
        <input
          id="email"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />

        <button className="btn-primary" type="submit" disabled={loginMutation.isPending}>
          {loginMutation.isPending ? "Signing in…" : "Sign in"}
        </button>

        {loginMutation.isError && (
          <p className="error">{(loginMutation.error as Error).message}</p>
        )}

        <div className="divider">or</div>

        <button
          className="btn-ghost full"
          type="button"
          onClick={() => demoMutation.mutate()}
          disabled={demoMutation.isPending}
        >
          {demoMutation.isPending ? "Starting demo…" : "Try the demo"}
        </button>
        <p className="subtitle demo-hint">
          Jump into a populated sample class — no account needed.
        </p>

        {demoMutation.isError && (
          <p className="error">{(demoMutation.error as Error).message}</p>
        )}
      </form>
    </main>
  );
}
