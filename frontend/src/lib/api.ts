// Thin API client. Same-origin (dev proxy / prod Nginx), so the auth cookie rides along and
// mutations carry the double-submit antiforgery token from /api/antiforgery/token.

import type { CurrencyIcon } from "./currency";

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

// One-click sign-in to the shared, writable demo account (design.md §9.2). No credentials: the server
// signs in its seeded demo teacher. The account is periodically re-seeded to a rich, populated state.
export async function demoLogin(): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch("/api/auth/demo-login", {
    method: "POST",
    credentials: "include",
    headers: { "X-XSRF-TOKEN": token },
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not start the demo."));
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
  currencyIcon: CurrencyIcon;
  createdAt: string;
  isArchived: boolean;
  role: ClassRole;
};

export async function fetchClasses(): Promise<ClassSummary[]> {
  const res = await fetch("/api/classes", { credentials: "include" });
  if (!res.ok) throw new Error("Failed to load classes.");
  return res.json();
}

// Archive a class (any member): it leaves the active list but stays in the database (history kept).
export async function archiveClass(classId: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/archive`, {
    method: "POST",
    credentials: "include",
    headers: { "X-XSRF-TOKEN": token },
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not archive the class."));
}

// Change a class's reward-currency icon (any member); the value must be in the fixed allowed set.
export async function setCurrencyIcon(classId: string, icon: CurrencyIcon): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/currency-icon`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ icon }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not change the currency icon."));
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

// Permanently erase a student's data (Owner only): PII, avatar and inventory are destroyed and their
// ledger rows anonymized. Irreversible — distinct from and stronger than removeStudent's soft-delete.
export async function purgeStudent(classId: string, studentId: string): Promise<void> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/students/${studentId}/purge`, {
    method: "POST",
    credentials: "include",
    headers: { "X-XSRF-TOKEN": token },
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not purge the student."));
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
// points decide the amount; selecting several students writes one row each sharing a batch id. An
// optional note is stored verbatim on every row written.
export async function awardBehavior(
  classId: string,
  studentIds: string[],
  behaviorId: string,
  reason?: string,
): Promise<void> {
  const token = await antiforgeryToken();
  const trimmed = reason?.trim();
  const res = await fetch(`/api/classes/${classId}/points/award`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ studentIds, behaviorId, reason: trimmed || null }),
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

// --- Groups ---

export type GroupMember = { studentId: string; displayName: string; gender: Gender | null };
export type FormedGroup = { groupNumber: number; members: GroupMember[] };

// An ephemeral, formed-but-unsaved grouping. `seed` reproduces this exact arrangement on save.
export type FormedGrouping = {
  groupSize: number;
  balancedByGender: boolean;
  seed: number;
  groups: FormedGroup[];
};

export type SavedGrouping = {
  id: string;
  classId: string;
  name: string | null;
  groupSize: number;
  balancedByGender: boolean;
  createdAt: string;
  groups: FormedGroup[];
};

export type GroupingSummary = {
  id: string;
  name: string | null;
  groupSize: number;
  balancedByGender: boolean;
  createdAt: string;
  groupCount: number;
  studentCount: number;
};

export async function previewGrouping(
  classId: string,
  groupSize: number,
  balanceByGender: boolean,
): Promise<FormedGrouping> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/groupings/preview`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ groupSize, balanceByGender }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not form groups."));
  return res.json();
}

export async function saveGrouping(
  classId: string,
  name: string | null,
  groupSize: number,
  balanceByGender: boolean,
  seed: number,
): Promise<SavedGrouping> {
  const token = await antiforgeryToken();
  const res = await fetch(`/api/classes/${classId}/groupings`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-XSRF-TOKEN": token },
    body: JSON.stringify({ name, groupSize, balanceByGender, seed }),
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Could not save the grouping."));
  return res.json();
}

export async function fetchGroupings(classId: string): Promise<GroupingSummary[]> {
  const res = await fetch(`/api/classes/${classId}/groupings`, { credentials: "include" });
  if (!res.ok) throw new Error("Failed to load saved groupings.");
  return res.json();
}

export async function fetchGrouping(classId: string, groupingId: string): Promise<SavedGrouping> {
  const res = await fetch(`/api/classes/${classId}/groupings/${groupingId}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error("Failed to load the grouping.");
  return res.json();
}

// --- Reporting ---

// The teacher's local UTC offset in minutes (minutes to add to UTC to reach local time). The server
// buckets the ledger by local day, so it needs this to place a late-evening award on the right day.
function localOffsetMinutes(): number {
  return -new Date().getTimezoneOffset();
}

export type TimelineDay = {
  date: string; // yyyy-MM-dd
  earned: number;
  spent: number; // <= 0
  net: number;
  balance: number; // running wallet at end of day
};

export type StudentTimeline = {
  studentId: string;
  displayName: string;
  from: string;
  to: string;
  openingBalance: number;
  totalEarned: number;
  totalSpent: number;
  days: TimelineDay[];
};

export async function fetchStudentTimeline(
  classId: string,
  studentId: string,
  from: string,
  to: string,
): Promise<StudentTimeline> {
  const params = new URLSearchParams({ from, to, tzOffsetMinutes: String(localOffsetMinutes()) });
  const res = await fetch(
    `/api/classes/${classId}/reports/students/${studentId}/timeline?${params}`,
    { credentials: "include" },
  );
  if (!res.ok) throw new Error(await problemMessage(res, "Failed to load the timeline."));
  return res.json();
}

export type BehaviorStat = {
  behaviorId: string;
  name: string;
  count: number;
  totalPoints: number; // signed
};

export type BehaviorBreakdown = {
  from: string;
  to: string;
  totalAwarded: number;
  totalDeducted: number; // <= 0
  netPoints: number;
  awardCount: number;
  behaviors: BehaviorStat[]; // most common first
};

export async function fetchBehaviorBreakdown(
  classId: string,
  from: string,
  to: string,
): Promise<BehaviorBreakdown> {
  const params = new URLSearchParams({ from, to, tzOffsetMinutes: String(localOffsetMinutes()) });
  const res = await fetch(`/api/classes/${classId}/reports/behaviors?${params}`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await problemMessage(res, "Failed to load the breakdown."));
  return res.json();
}

export type PointTransactionType = "Award" | "Deduction" | "Purchase" | "Adjustment";

// One row of a student's history, including its note (reason) so a teacher sees *why* points moved.
// Voided rows are included (the list is an audit trail, not an aggregation) and marked via voidedAt.
export type StudentHistoryItem = {
  id: string;
  amount: number;
  type: PointTransactionType;
  behaviorId: string | null;
  behaviorName: string | null;
  reason: string | null;
  createdAt: string;
  voidedAt: string | null;
};

export type StudentHistory = {
  studentId: string;
  displayName: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: StudentHistoryItem[];
};

export async function fetchStudentHistory(
  classId: string,
  studentId: string,
  page = 1,
  pageSize = 20,
): Promise<StudentHistory> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  const res = await fetch(
    `/api/classes/${classId}/reports/students/${studentId}/history?${params}`,
    { credentials: "include" },
  );
  if (!res.ok) throw new Error(await problemMessage(res, "Failed to load the history."));
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
