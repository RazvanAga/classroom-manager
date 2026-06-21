import { Settings } from "lucide-react";
import { SectionPlaceholder } from "@/components/SectionPlaceholder";
import { ro } from "@/lib/strings";

// Setări (roster, behaviors, currency icon, kiosk PIN, purge) is built in slice #26.
export default function SetariPage() {
  return <SectionPlaceholder title={ro.nav.settings} icon={Settings} />;
}
