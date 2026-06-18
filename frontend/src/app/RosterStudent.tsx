"use client";

import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { fetchStudentAvatar, type Student } from "@/lib/api";
import { StudentShop, avatarUri } from "./StudentShop";

// One roster row: the student's composed avatar, name, a customize toggle, and remove. The avatar
// is fetched per student (small classes, so N light queries are fine); the customizer + store live
// in the shared StudentShop, mounted only while open.
export function RosterStudent({
  classId,
  student,
  onRemove,
  removing,
  canPurge,
  onPurge,
  purging,
}: {
  classId: string;
  student: Student;
  onRemove: () => void;
  removing: boolean;
  canPurge: boolean;
  onPurge: () => void;
  purging: boolean;
}) {
  const [open, setOpen] = useState(false);
  // Purge is irreversible, so it takes a second deliberate click to confirm rather than firing outright.
  const [confirmingPurge, setConfirmingPurge] = useState(false);

  const { data: avatar } = useQuery({
    queryKey: ["avatar", classId, student.id],
    queryFn: () => fetchStudentAvatar(classId, student.id),
  });

  const uri = useMemo(
    () => (avatar ? avatarUri(student.id, avatar.equipped, 96) : null),
    [avatar, student.id],
  );

  return (
    <li className="class-item roster-student">
      <div className="roster-student-head">
        {uri ? (
          <img className="avatar-thumb" src={uri} width={48} height={48} alt="" />
        ) : (
          <span className="avatar-thumb placeholder" aria-hidden />
        )}
        <span className="class-name">{student.displayName}</span>
        <span className="roster-right">
          {student.gender && (
            <span className="role-tag">{student.gender === "Female" ? "F" : "M"}</span>
          )}
          <button
            className="btn-ghost"
            aria-expanded={open}
            onClick={() => setOpen((v) => !v)}
            disabled={!avatar}
          >
            {open ? "Done" : "Customize"}
          </button>
          <button
            className="btn-ghost danger"
            aria-label={`Remove ${student.displayName}`}
            onClick={onRemove}
            disabled={removing}
          >
            Remove
          </button>
          {canPurge &&
            (confirmingPurge ? (
              <button
                className="btn-ghost danger"
                aria-label={`Confirm permanent erase of ${student.displayName}`}
                onClick={onPurge}
                disabled={purging}
              >
                {purging ? "Erasing…" : "Confirm erase"}
              </button>
            ) : (
              <button
                className="btn-ghost danger"
                aria-label={`Permanently erase ${student.displayName}'s data`}
                title="Permanently erase this student's data (cannot be undone)"
                onClick={() => setConfirmingPurge(true)}
                disabled={purging}
              >
                Erase
              </button>
            ))}
        </span>
      </div>

      {confirmingPurge && (
        <p className="muted purge-warning">
          Permanently erase {student.displayName}’s name, avatar and items? This can’t be undone.{" "}
          <button className="btn-ghost" onClick={() => setConfirmingPurge(false)}>
            Cancel
          </button>
        </p>
      )}

      {open && <StudentShop classId={classId} studentId={student.id} />}
    </li>
  );
}
