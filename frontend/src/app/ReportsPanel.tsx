"use client";

import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import {
  fetchBehaviorBreakdown,
  fetchStudentTimeline,
  fetchStudents,
} from "@/lib/api";

// Lightweight analytics for the selected class (design.md §9.3): a per-class behavior breakdown and a
// per-student point timeline over a selectable date range. Both aggregate the non-voided ledger
// server-side, bucketed by the teacher's local day; this panel just charts the result.
const pad = (n: number) => String(n).padStart(2, "0");
const isoDate = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

function defaultRange(): { from: string; to: string } {
  const to = new Date();
  const from = new Date();
  from.setDate(to.getDate() - 29); // a 30-day window ending today
  return { from: isoDate(from), to: isoDate(to) };
}

export function ReportsPanel({ classId }: { classId: string }) {
  const initial = useMemo(defaultRange, []);
  const [from, setFrom] = useState(initial.from);
  const [to, setTo] = useState(initial.to);
  const [studentId, setStudentId] = useState<string>("");

  const rangeValid = from <= to;

  const { data: students } = useQuery({
    queryKey: ["students", classId],
    queryFn: () => fetchStudents(classId),
  });

  const { data: breakdown, isError: breakdownError } = useQuery({
    queryKey: ["behaviorBreakdown", classId, from, to],
    queryFn: () => fetchBehaviorBreakdown(classId, from, to),
    enabled: rangeValid,
  });

  const { data: timeline, isError: timelineError } = useQuery({
    queryKey: ["studentTimeline", classId, studentId, from, to],
    queryFn: () => fetchStudentTimeline(classId, studentId, from, to),
    enabled: rangeValid && !!studentId,
  });

  // The breakdown is sorted most-common-first; the first of each sign is the headline behavior.
  const mostCommonPositive = breakdown?.behaviors.find((b) => b.totalPoints > 0) ?? null;
  const mostCommonNegative = breakdown?.behaviors.find((b) => b.totalPoints < 0) ?? null;
  const maxCount = Math.max(1, ...(breakdown?.behaviors.map((b) => b.count) ?? [1]));

  // Scale the timeline bars against the largest single-day swing in the window.
  const maxNet = Math.max(1, ...(timeline?.days.map((d) => Math.abs(d.net)) ?? [1]));
  const closing =
    timeline && timeline.days.length > 0
      ? timeline.days[timeline.days.length - 1].balance
      : (timeline?.openingBalance ?? 0);

  return (
    <section className="roster">
      <h2>Reports</h2>

      <div className="report-controls">
        <label className="report-field">
          From
          <input type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label className="report-field">
          To
          <input type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
        </label>
      </div>
      {!rangeValid && <p className="error">The “from” date must be on or before the “to” date.</p>}

      {/* --- Per-class behavior breakdown --- */}
      <h3 className="leaderboard-title">Behavior breakdown</h3>
      {breakdownError ? (
        <p className="error">Couldn’t load the breakdown.</p>
      ) : breakdown && breakdown.behaviors.length > 0 ? (
        <>
          <div className="report-summary">
            <span className="points-tag">{breakdown.totalAwarded} awarded</span>
            <span className="points-tag negative">{breakdown.totalDeducted} deducted</span>
            <span className="points-tag wallet">{breakdown.netPoints} net</span>
            <span className="empty selected-count">{breakdown.awardCount} entries</span>
          </div>

          <div className="report-headlines">
            {mostCommonPositive && (
              <p className="muted">
                Most common positive:{" "}
                <span className="name">{mostCommonPositive.name}</span> ×{mostCommonPositive.count}
              </p>
            )}
            {mostCommonNegative && (
              <p className="muted">
                Most common negative:{" "}
                <span className="name">{mostCommonNegative.name}</span> ×{mostCommonNegative.count}
              </p>
            )}
          </div>

          <ul className="breakdown-list">
            {breakdown.behaviors.map((b) => (
              <li key={b.behaviorId} className="breakdown-row">
                <span className="breakdown-name">{b.name}</span>
                <span className="breakdown-track">
                  <span
                    className={`breakdown-bar${b.totalPoints < 0 ? " negative" : ""}`}
                    style={{ width: `${Math.round((b.count / maxCount) * 100)}%` }}
                  />
                </span>
                <span className="breakdown-meta">
                  <span className="muted">×{b.count}</span>
                  <span className={`points-tag${b.totalPoints < 0 ? " negative" : ""}`}>
                    {b.totalPoints > 0 ? `+${b.totalPoints}` : b.totalPoints}
                  </span>
                </span>
              </li>
            ))}
          </ul>
        </>
      ) : (
        <p className="empty">No behavior points in this range yet.</p>
      )}

      {/* --- Per-student point timeline --- */}
      <h3 className="leaderboard-title">Student timeline</h3>
      <select
        className="gender-select report-student"
        aria-label="Student for the timeline"
        value={studentId}
        onChange={(e) => setStudentId(e.target.value)}
      >
        <option value="">Choose a student…</option>
        {(students ?? []).map((s) => (
          <option key={s.id} value={s.id}>
            {s.displayName}
          </option>
        ))}
      </select>

      {timelineError ? (
        <p className="error">Couldn’t load the timeline.</p>
      ) : studentId && timeline ? (
        <>
          <div className="report-summary">
            <span className="points-tag">{timeline.totalEarned} earned</span>
            <span className="points-tag negative">{timeline.totalSpent} spent</span>
            <span className="points-tag wallet">{closing} balance</span>
          </div>
          <div className="timeline-chart" role="img" aria-label="Daily net points over the range">
            {timeline.days.map((d) => (
              <div
                key={d.date}
                className="timeline-col"
                title={`${d.date}: ${d.net > 0 ? "+" : ""}${d.net} (balance ${d.balance})`}
              >
                <span className="timeline-top">
                  {d.net > 0 && (
                    <span
                      className="timeline-bar pos"
                      style={{ height: `${Math.round((d.net / maxNet) * 100)}%` }}
                    />
                  )}
                </span>
                <span className="timeline-bot">
                  {d.net < 0 && (
                    <span
                      className="timeline-bar neg"
                      style={{ height: `${Math.round((-d.net / maxNet) * 100)}%` }}
                    />
                  )}
                </span>
              </div>
            ))}
          </div>
          <p className="muted timeline-axis">
            <span>{timeline.from}</span>
            <span>{timeline.to}</span>
          </p>
        </>
      ) : (
        studentId && <p className="empty">Loading timeline…</p>
      )}
    </section>
  );
}
