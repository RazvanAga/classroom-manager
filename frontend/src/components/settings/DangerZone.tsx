"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Archive, AlertTriangle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { archiveClass } from "@/lib/api";
import { ro } from "@/lib/strings";
import { ConfirmDialog } from "./ConfirmDialog";
import { SettingsCard } from "./SettingsCard";

// Class-level destructive actions (issue #26). Archiving drops the class from the active list (any
// member); history is kept. Once archived the class can't be opened from the app, so it sits behind a
// confirmation and bounces back to the class selector.
export function DangerZone({ classId }: { classId: string }) {
  const queryClient = useQueryClient();
  const router = useRouter();
  const [confirming, setConfirming] = useState(false);

  const archive = useMutation({
    mutationFn: () => archiveClass(classId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["classes"] });
      router.replace("/");
    },
  });

  return (
    <SettingsCard
      icon={AlertTriangle}
      title={ro.settings.danger.title}
      tone="danger"
    >
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border-2 border-rose-100 bg-rose-50/60 px-4 py-3">
        <div>
          <p className="text-sm font-bold text-slate-700">{ro.settings.danger.archiveTitle}</p>
          <p className="text-xs text-slate-500">{ro.settings.danger.archiveHint}</p>
        </div>
        <button
          type="button"
          onClick={() => setConfirming(true)}
          className="flex items-center gap-1.5 rounded-2xl border-2 border-rose-200 bg-white px-4 py-2 text-sm font-bold text-rose-600 transition hover:bg-rose-100"
        >
          <Archive size={16} /> {ro.settings.danger.archive}
        </button>
      </div>

      {confirming && (
        <ConfirmDialog
          title={ro.settings.danger.archiveConfirmTitle}
          message={ro.settings.danger.archiveConfirmMessage}
          confirmLabel={ro.settings.danger.archiveConfirm}
          pendingLabel={ro.settings.danger.archiving}
          isPending={archive.isPending}
          error={archive.isError ? (archive.error as Error).message : null}
          onConfirm={() => archive.mutate()}
          onClose={() => setConfirming(false)}
        />
      )}
    </SettingsCard>
  );
}
