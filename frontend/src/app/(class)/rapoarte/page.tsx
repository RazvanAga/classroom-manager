import { BarChart3 } from "lucide-react";
import { SectionPlaceholder } from "@/components/SectionPlaceholder";
import { ro } from "@/lib/strings";

// Rapoarte + student profile is built in slice #25.
export default function RapoartePage() {
  return <SectionPlaceholder title={ro.nav.reports} icon={BarChart3} />;
}
