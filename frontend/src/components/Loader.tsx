import { Loader2 } from "lucide-react";
import { ro } from "@/lib/strings";

// Full-viewport centered spinner, used while auth/kiosk state is resolving.
export function Loader() {
  return (
    <div className="grid min-h-screen place-items-center">
      <div className="flex items-center gap-3 text-indigo-900/70">
        <Loader2 className="animate-spin" size={22} />
        <span className="font-semibold">{ro.common.loading}</span>
      </div>
    </div>
  );
}
