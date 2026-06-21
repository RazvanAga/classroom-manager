import { ShoppingBag } from "lucide-react";
import { SectionPlaceholder } from "@/components/SectionPlaceholder";
import { ro } from "@/lib/strings";

// Magazin (store redesign) is built in slice #23.
export default function MagazinPage() {
  return <SectionPlaceholder title={ro.nav.shop} icon={ShoppingBag} />;
}
