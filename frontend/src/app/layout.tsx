import type { Metadata } from "next";
import { Providers } from "./providers";
import { AuthGate } from "@/components/AuthGate";
import { ro } from "@/lib/strings";
import "./globals.css";

export const metadata: Metadata = {
  title: ro.appName,
  description: "Gestionează clase, acordă puncte de comportament și condu-ți ziua la clasă.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ro" className="h-full">
      <body className="min-h-full">
        <Providers>
          <AuthGate>{children}</AuthGate>
        </Providers>
      </body>
    </html>
  );
}
