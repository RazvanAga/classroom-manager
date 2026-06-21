"use client";

import { useQuery } from "@tanstack/react-query";
import { fetchStudentAvatar } from "@/lib/api";
import { avatarDataUri } from "@/lib/avatar";

// Renders a student's composed DiceBear avatar. Each avatar is its own query so the cache dedupes the
// roster-wide fan-out and the award modal reuses the same entry without refetching. A neutral disc is
// shown while the config loads so cards never jump.
export function StudentAvatar({
  classId,
  studentId,
  size = 56,
  className = "",
}: {
  classId: string;
  studentId: string;
  size?: number;
  className?: string;
}) {
  const { data } = useQuery({
    queryKey: ["avatar", classId, studentId],
    queryFn: () => fetchStudentAvatar(classId, studentId),
    staleTime: 5 * 60 * 1000,
  });

  if (!data) {
    return (
      <div
        className={`rounded-full bg-gradient-to-br from-violet-100 to-indigo-100 ${className}`}
        style={{ width: size, height: size }}
      />
    );
  }

  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={avatarDataUri(studentId, data.equipped)}
      alt=""
      width={size}
      height={size}
      className={className}
      style={{ width: size, height: size }}
    />
  );
}
