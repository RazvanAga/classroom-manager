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

export type ClassRole = "Owner" | "Collaborator";

export type ClassSummary = {
  id: string;
  name: string;
  createdAt: string;
  isArchived: boolean;
  role: ClassRole;
};

export async function fetchClasses(): Promise<ClassSummary[]> {
  const res = await fetch("/api/classes", { credentials: "include" });
  if (!res.ok) throw new Error("Failed to load classes.");
  return res.json();
}

export async function createClass(name: string): Promise<ClassSummary> {
  const token = await antiforgeryToken();
  const res = await fetch("/api/classes", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ name }),
  });
  if (!res.ok) {
    const problem: ProblemDetails | null = await res.json().catch(() => null);
    throw new Error(problem?.detail ?? problem?.title ?? "Could not create the class.");
  }
  return res.json();
}

// --- Roster ---

export type Gender = "Female" | "Male";

export type Student = {
  id: string;
  classId: string;
  displayName: string;
  gender: Gender | null;
  createdAt: string;
};

async function problemMessage(res: Response, fallback: string): Promise<string> {
  const problem: ProblemDetails | null = await res.json().catch(() => null);
  return problem?.detail ?? problem?.title ?? fallback;
}

export async function fetchStudents(classId: string): Promise<Student[]> {
  const res = await fetch(`/api/classes/${classId}/students`, { credentials: "include" });
  if (!res.ok) throw new Error("Failed to load the roster.");
  return res.json();
}

export async function addStudent(
  classId: string,
  displayName: string,
  gender: Gender | null,
): Promise<Student> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/students`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ displayName, gender }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not add the student."));
  return res.json();
}

export async function bulkAddStudents(classId: string, text: string): Promise<Student[]> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/students/bulk`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ text }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not add the students."));
  return res.json();
}

export async function removeStudent(classId: string, studentId: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/students/${studentId}`, {
    method: "DELETE",
    credentials: "include",
    headers: { "X-XSRF-TOKEN": token },
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not remove the student."));
}

// --- Behaviors ---

export type Behavior = {
  id: string;
  classId: string;
  name: string;
  defaultPoints: number;
  createdAt: string;
};

export async function fetchBehaviors(classId: string): Promise<Behavior[]> {
  const res = await fetch(`/api/classes/${classId}/behaviors`, { credentials: "include" });
  if (!res.ok) throw new Error("Failed to load the behavior catalog.");
  return res.json();
}

export async function addBehavior(
  classId: string,
  name: string,
  defaultPoints: number,
): Promise<Behavior> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/behaviors`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ name, defaultPoints }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not add the behavior."));
  return res.json();
}

export async function updateBehavior(
  classId: string,
  behaviorId: string,
  name: string,
  defaultPoints: number,
): Promise<Behavior> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/behaviors/${behaviorId}`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ name, defaultPoints }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not update the behavior."));
  return res.json();
}

export async function removeBehavior(classId: string, behaviorId: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/behaviors/${behaviorId}`, {
    method: "DELETE",
    credentials: "include",
    headers: { "X-XSRF-TOKEN": token },
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not remove the behavior."));
}
