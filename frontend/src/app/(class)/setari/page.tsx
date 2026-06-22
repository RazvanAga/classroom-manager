"use client";

import { useQuery } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { useSearchParams } from "next/navigation";
import { fetchClasses } from "@/lib/api";
import type { CurrencyIcon } from "@/lib/currency";
import { BehaviorsSettings } from "@/components/settings/BehaviorsSettings";
import { CurrencySettings } from "@/components/settings/CurrencySettings";
import { DangerZone } from "@/components/settings/DangerZone";
import { KioskPinSettings } from "@/components/settings/KioskPinSettings";
import { RosterSettings } from "@/components/settings/RosterSettings";

// Setări (issue #26): the unified per-class administration page — roster, behavior catalog, currency
// icon, kiosk PIN, student purge (Owner only) and archiving, each in its own section. Reuses the
// existing endpoints; the only new wiring is the currency-icon picker (#19). Class-scoped via ?class=.
export default function SetariPage() {
  const classId = useSearchParams().get("class") ?? "";

  const classes = useQuery({ queryKey: ["classes"], queryFn: fetchClasses });
  const klass = classes.data?.find((c) => c.id === classId) ?? null;

  if (classes.isLoading || !klass) {
    return (
      <div className="grid place-items-center py-24 text-indigo-900/60">
        <Loader2 className="animate-spin" size={28} />
      </div>
    );
  }

  const currencyIcon: CurrencyIcon = klass.currencyIcon;
  const isOwner = klass.role === "Owner";

  return (
    <div className="space-y-6">
      <RosterSettings classId={classId} isOwner={isOwner} />
      <BehaviorsSettings classId={classId} currencyIcon={currencyIcon} />
      <div className="grid gap-6 lg:grid-cols-2">
        <CurrencySettings classId={classId} current={currencyIcon} />
        <KioskPinSettings />
      </div>
      <DangerZone classId={classId} />
    </div>
  );
}
