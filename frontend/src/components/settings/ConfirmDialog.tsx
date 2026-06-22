"use client";

import { AlertTriangle, X } from "lucide-react";
import { useState } from "react";
import { ro } from "@/lib/strings";

// A modal confirmation for destructive actions (soft-remove, archive, purge). When `confirmWord` is
// set, the confirm button stays disabled until the teacher types that exact word — the deliberate,
// irreversible-action gate the purge flow needs (issue #26). The caller owns the mutation and passes
// `isPending` / `error`; the dialog only gathers intent.
export function ConfirmDialog({
  title,
  message,
  confirmLabel,
  pendingLabel,
  confirmWord,
  confirmWordHint,
  isPending,
  error,
  onConfirm,
  onClose,
}: {
  title: string;
  message: string;
  confirmLabel: string;
  pendingLabel: string;
  confirmWord?: string;
  confirmWordHint?: string;
  isPending: boolean;
  error?: string | null;
  onConfirm: () => void;
  onClose: () => void;
}) {
  const [typed, setTyped] = useState("");
  const gated = confirmWord != null && typed.trim() !== confirmWord;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-indigo-950/50 p-4 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="w-full max-w-md overflow-hidden rounded-3xl bg-white shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start gap-3 bg-gradient-to-r from-rose-500 to-pink-500 px-5 py-4">
          <AlertTriangle size={24} className="mt-0.5 flex-shrink-0 text-white" />
          <h2 className="flex-1 text-lg font-extrabold leading-tight text-white">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="text-white/70 transition hover:text-white"
            aria-label={ro.common.cancel}
          >
            <X size={22} />
          </button>
        </div>

        <div className="space-y-4 p-5">
          <p className="text-sm leading-relaxed text-slate-600">{message}</p>

          {confirmWord != null && (
            <div>
              {confirmWordHint && (
                <label className="mb-1.5 block text-xs font-semibold text-slate-500">
                  {confirmWordHint}
                </label>
              )}
              <input
                type="text"
                value={typed}
                onChange={(e) => setTyped(e.target.value)}
                autoFocus
                className="w-full rounded-2xl border-2 border-slate-100 bg-slate-50 px-4 py-2.5 text-sm font-bold tracking-wide transition focus:border-rose-300 focus:bg-white focus:outline-none"
              />
            </div>
          )}

          {error && <p className="text-sm font-semibold text-rose-600">{error}</p>}

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-2xl px-4 py-2.5 text-sm font-bold text-slate-500 transition hover:bg-slate-100"
            >
              {ro.common.cancel}
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={isPending || gated}
              className="rounded-2xl bg-gradient-to-r from-rose-500 to-pink-500 px-5 py-2.5 text-sm font-bold text-white shadow transition hover:opacity-90 disabled:opacity-40"
            >
              {isPending ? pendingLabel : confirmLabel}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
