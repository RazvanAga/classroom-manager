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

// --- Kiosk ---

// A reduced-scope kiosk session scoped to one class (design.md §5.4). Students shop/equip; teacher
// admin actions are rejected server-side. Exiting needs the teacher's PIN.
export type KioskSession = { classId: string; className: string };

// Returns the current kiosk session, or null when not in kiosk mode (403/404).
export async function fetchKioskSession(): Promise<KioskSession | null> {
  const res = await fetch("/api/kiosk/me", { credentials: "include" });
  if (res.status === 401 || res.status === 403 || res.status === 404) return null;
  if (!res.ok) throw new Error("Failed to load the kiosk session.");
  return res.json();
}

export async function enterKiosk(classId: string): Promise<KioskSession> {
  const token = await antiforgeryToken();
  const res = await fetch("/api/kiosk/enter", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ classId }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not enter kiosk mode."));
  return res.json();
}

// Exit kiosk mode. A wrong PIN throws (the session stays in kiosk); a correct PIN restores the
// full teacher session server-side.
export async function exitKiosk(pin: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch("/api/kiosk/exit", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ pin }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "That PIN is incorrect."));
}

export async function setKioskPin(pin: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch("/api/kiosk/pin", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ pin }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not set the PIN."));
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

// --- Points ---

export type Balance = { studentId: string; wallet: number; lifetimeEarned: number };

export type LeaderboardEntry = {
  studentId: string;
  displayName: string;
  wallet: number;
  lifetimeEarned: number;
};

// Award (or deduct) a behavior to one or many students. The selected behavior's signed default
// points decide the amount; selecting several students writes one row each sharing a batch id.
export async function awardBehavior(
  classId: string,
  studentIds: string[],
  behaviorId: string,
): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/points/award`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ studentIds, behaviorId }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not award points."));
}

export async function fetchLeaderboard(classId: string): Promise<LeaderboardEntry[]> {
  const res = await fetch(`/api/classes/${classId}/leaderboard`, { credentials: "include" });
  if (!res.ok) throw new Error("Failed to load the leaderboard.");
  return res.json();
}

export type PointTransaction = {
  id: string;
  studentId: string;
  studentName: string;
  amount: number;
  type: "Award" | "Deduction" | "Purchase" | "Adjustment";
  behaviorId: string | null;
  behaviorName: string | null;
  batchId: string | null;
  reason: string | null;
  createdAt: string;
  voidedAt: string | null;
};

export async function fetchTransactions(classId: string): Promise<PointTransaction[]> {
  const res = await fetch(`/api/classes/${classId}/points/transactions`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error("Failed to load recent activity.");
  return res.json();
}

// Undo a single award/deduction by soft-voiding it (retained for audit, excluded from totals).
export async function voidTransaction(classId: string, transactionId: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(
    `/api/classes/${classId}/points/transactions/${transactionId}/void`,
    { method: "POST", credentials: "include", headers: { "X-XSRF-TOKEN": token } },
  );
  if (!res.ok) throw new Error(await problemMessage(res, "Could not undo the transaction."));
}

// Undo a whole bulk award by voiding every row sharing its batch id.
export async function voidBatch(classId: string, batchId: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/points/batches/${batchId}/void`, {
    method: "POST",
    credentials: "include",
    headers: { "X-XSRF-TOKEN": token },
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not undo the bulk award."));
}

// --- Picker ---

// One fair pick. The picker is stateless server-side (no DB table): the client carries the
// already-picked set for the current cycle and sends it back each turn. `cycleReset` is true when
// everyone had been picked, so the cycle reset and this pick starts a fresh one.
export type PickResult = {
  pickedStudentId: string;
  pickedDisplayName: string;
  cycleReset: boolean;
  alreadyPickedIds: string[];
};

export async function pickStudent(
  classId: string,
  alreadyPickedIds: string[],
): Promise<PickResult> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/picker/pick`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ alreadyPickedIds }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not pick a student."));
  return res.json();
}

// --- Avatars ---

// The always-on layers of the locked DiceBear style (mirrors the backend AvatarSlot enum).
export type AvatarSlot = "Hair" | "HairColor" | "SkinColor" | "Eyes" | "Mouth";
export type AvatarRarity = "Common" | "Rare" | "Epic" | "Legendary";

export type AvatarItem = {
  id: string;
  slot: AvatarSlot;
  optionValue: string;
  displayName: string;
  cost: number;
  rarity: AvatarRarity | null;
  isDefault: boolean;
};

export type EquippedSlot = { slot: AvatarSlot; itemId: string; optionValue: string };

export type StudentAvatar = {
  studentId: string;
  style: string;
  equipped: EquippedSlot[];
  owned: AvatarItem[];
};

export async function fetchStudentAvatar(
  classId: string,
  studentId: string,
): Promise<StudentAvatar> {
  const res = await fetch(`/api/classes/${classId}/students/${studentId}/avatar`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error("Failed to load the avatar.");
  return res.json();
}

// --- Store ---

// One buyable catalog option, with affordability computed against the student's wallet by the server.
export type StoreItem = {
  id: string;
  slot: AvatarSlot;
  optionValue: string;
  displayName: string;
  cost: number;
  rarity: AvatarRarity | null;
  affordable: boolean;
};

export type Store = {
  studentId: string;
  wallet: number;
  style: string;
  items: StoreItem[];
};

export async function fetchStore(classId: string, studentId: string): Promise<Store> {
  const res = await fetch(`/api/classes/${classId}/students/${studentId}/store`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error("Failed to load the store.");
  return res.json();
}

// Buy an option for a student. The server runs a serializable transaction (no overspend, no
// double-buy); the cost is the catalog's, never sent by the client.
export async function purchaseItem(
  classId: string,
  studentId: string,
  itemId: string,
): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(
    `/api/classes/${classId}/students/${studentId}/store/purchase`,
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
      body: JSON.stringify({ itemId }),
    },
  );
  if (!res.ok) throw new Error(await problemMessage(res, "Could not complete the purchase."));
}

// Equip an option the student already owns in a slot. Free; switching is unrestricted.
export async function equipItem(
  classId: string,
  studentId: string,
  slot: AvatarSlot,
  itemId: string,
): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(
    `/api/classes/${classId}/students/${studentId}/avatar/${slot}`,
    {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
      body: JSON.stringify({ itemId }),
    },
  );
  if (!res.ok) throw new Error(await problemMessage(res, "Could not equip the option."));
}
