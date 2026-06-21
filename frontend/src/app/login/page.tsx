"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Sparkles, Star } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { demoLogin, fetchMe, login } from "@/lib/api";
import { ro } from "@/lib/strings";

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

  const loginMutation = useMutation({ mutationFn: () => login(email, password), onSuccess: onAuthed });
  const demoMutation = useMutation({ mutationFn: demoLogin, onSuccess: onAuthed });

  return (
    <main className="grid min-h-screen place-items-center p-6">
      <form
        className="w-full max-w-sm rounded-3xl bg-white p-8 shadow-xl shadow-purple-900/10 ring-1 ring-violet-100"
        onSubmit={(e) => {
          e.preventDefault();
          loginMutation.mutate();
        }}
      >
        <div className="mb-6 flex items-center gap-3">
          <div className="flex h-12 w-12 rotate-3 items-center justify-center rounded-2xl bg-yellow-400 shadow-md">
            <Star size={26} className="fill-yellow-900 text-yellow-900" />
          </div>
          <div>
            <h1 className="text-2xl font-extrabold tracking-tight">{ro.auth.title}</h1>
            <p className="text-sm text-slate-500">{ro.auth.subtitle}</p>
          </div>
        </div>

        <label htmlFor="email" className="mb-1 block text-sm font-semibold">{ro.auth.email}</label>
        <input
          id="email"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="mb-4 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-lg outline-none focus:border-violet-400"
        />

        <label htmlFor="password" className="mb-1 block text-sm font-semibold">{ro.auth.password}</label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          className="mb-2 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-lg outline-none focus:border-violet-400"
        />

        <button
          type="submit"
          disabled={loginMutation.isPending}
          className="mt-4 w-full rounded-2xl bg-violet-600 py-3 text-lg font-bold text-white shadow-lg shadow-violet-600/30 transition hover:bg-violet-700 disabled:opacity-50"
        >
          {loginMutation.isPending ? ro.auth.signingIn : ro.auth.signIn}
        </button>
        {loginMutation.isError && (
          <p className="mt-3 text-sm font-medium text-rose-600">{(loginMutation.error as Error).message}</p>
        )}

        <div className="my-5 flex items-center gap-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
          <span className="h-px flex-1 bg-slate-200" />
          {ro.auth.or}
          <span className="h-px flex-1 bg-slate-200" />
        </div>

        <button
          type="button"
          onClick={() => demoMutation.mutate()}
          disabled={demoMutation.isPending}
          className="flex w-full items-center justify-center gap-2 rounded-2xl border border-violet-200 bg-violet-50 py-3 text-lg font-bold text-violet-700 transition hover:bg-violet-100 disabled:opacity-50"
        >
          <Sparkles size={20} />
          {demoMutation.isPending ? ro.auth.startingDemo : ro.auth.tryDemo}
        </button>
        <p className="mt-2 text-center text-sm text-slate-500">{ro.auth.demoHint}</p>
        {demoMutation.isError && (
          <p className="mt-3 text-sm font-medium text-rose-600">{(demoMutation.error as Error).message}</p>
        )}
      </form>
    </main>
  );
}
