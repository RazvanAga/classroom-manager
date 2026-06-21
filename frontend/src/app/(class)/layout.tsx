import { Suspense } from "react";
import { ClassShell } from "@/components/ClassShell";
import { Loader } from "@/components/Loader";

// Shared layout for every class-scoped section. ClassShell reads the `?class=` param via
// useSearchParams, which Next requires to sit under a Suspense boundary for the static export.
export default function ClassLayout({ children }: { children: React.ReactNode }) {
  return (
    <Suspense fallback={<Loader />}>
      <ClassShell>{children}</ClassShell>
    </Suspense>
  );
}
