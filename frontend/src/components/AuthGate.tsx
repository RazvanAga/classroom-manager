"use client";

import { useQuery } from "@tanstack/react-query";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { fetchKioskSession, fetchMe } from "@/lib/api";
import { Loader } from "./Loader";
import { KioskScreen } from "./KioskScreen";

// Global auth/kiosk gate (slice #20). Wraps the whole app so kiosk and sign-in state are resolved in
// one place regardless of route:
//   - a kiosk session takes over the entire app (a reduced-scope principal — design.md §5.4);
//   - an unauthenticated visitor is sent to /login (except when already there);
//   - a signed-in teacher who lands on /login is bounced back to the class selector.
export function AuthGate({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const onLogin = pathname === "/login";

  // A kiosk session is checked first; while it loads we hold the teacher query to avoid a flash.
  const { data: kiosk, isLoading: kioskLoading } = useQuery({
    queryKey: ["kioskSession"],
    queryFn: fetchKioskSession,
  });

  const { data: me, isLoading: meLoading } = useQuery({
    queryKey: ["me"],
    queryFn: fetchMe,
    enabled: !kioskLoading && !kiosk,
  });

  const loading = kioskLoading || (!kiosk && meLoading);

  useEffect(() => {
    if (loading || kiosk) return;
    if (me === null && !onLogin) router.replace("/login");
    if (me && onLogin) router.replace("/");
  }, [loading, kiosk, me, onLogin, router]);

  if (loading) return <Loader />;
  if (kiosk) return <KioskScreen session={kiosk} />;

  // Mid-redirect: don't flash the wrong page while the effect navigates.
  if (me === null && !onLogin) return <Loader />;
  if (me && onLogin) return <Loader />;

  return <>{children}</>;
}
