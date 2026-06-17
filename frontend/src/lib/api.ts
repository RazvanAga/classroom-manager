// Thin API client. Same-origin (dev proxy / prod Nginx), so the auth cookie rides along and
// mutations carry the double-submit antiforgery token from /api/antiforgery/token.

export type Me = { id: string; email: string; displayName: string };

type ProblemDetails = { title?: string; detail?: string };

async function antiforgeryToken(): Promise<string> {
  const res = await fetch("/api/antiforgery/token", { credentials: "include" });
  if (!res.ok) throw new Error("Could not obtain an antiforgery token.");
  const data: { token: string } = await res.json();
  return data.token;
}

export async function fetchMe(): Promise<Me | null> {
  const res = await fetch("/api/auth/me", { credentials: "include" });
  if (res.status === 401) return null;
  if (!res.ok) throw new Error("Failed to load the current teacher.");
  return res.json();
}

export async function login(email: string, password: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch("/api/auth/login", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) {
    const problem: ProblemDetails | null = await res.json().catch(() => null);
    throw new Error(problem?.detail ?? problem?.title ?? "Login failed.");
  }
}

export async function logout(): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch("/api/auth/logout", {
    method: "POST",
    credentials: "include",
    headers: { "X-XSRF-TOKEN": token },
  });
  if (!res.ok) throw new Error("Logout failed.");
}
