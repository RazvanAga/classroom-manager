import { LayoutDashboard } from "lucide-react";
import { SectionPlaceholder } from "@/components/SectionPlaceholder";
import { ro } from "@/lib/strings";

// Dashboard (grid + leaderboard + award + undo) is built in slice #22.
export default function DashboardPage() {
  return <SectionPlaceholder title={ro.nav.dashboard} icon={LayoutDashboard} />;
}
