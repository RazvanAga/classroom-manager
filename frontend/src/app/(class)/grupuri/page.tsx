import { Shuffle } from "lucide-react";
import { SectionPlaceholder } from "@/components/SectionPlaceholder";
import { ro } from "@/lib/strings";

// Grupuri (picker + groups + timer) is built in slice #24.
export default function GrupuriPage() {
  return <SectionPlaceholder title={ro.nav.groups} icon={Shuffle} />;
}
