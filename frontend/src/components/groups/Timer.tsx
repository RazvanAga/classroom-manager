"use client";

import { Pause, Play, RotateCcw, Timer as TimerIcon } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { ro } from "@/lib/strings";

// Activity timer (issue #24). Client-only by design (design.md §6.1): no backend, just a deadline-based
// countdown with a circular SVG progress ring, presets + a custom duration, start/pause/reset, and a
// Web Audio chime at zero. Ticking is deadline-based (compute remaining from a target timestamp) so it
// stays accurate even if the tab is throttled, rather than decrementing a counter each interval.

const PRESETS = [1, 3, 5, 10];
const RADIUS = 90;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

export function Timer() {
  const [minutes, setMinutes] = useState(5);
  const [seconds, setSeconds] = useState(0);
  const [remaining, setRemaining] = useState<number | null>(null); // null = idle, showing the dialed time
  const [running, setRunning] = useState(false);

  const totalRef = useRef(0); // the duration the current run started from, for the ring fraction
  const deadlineRef = useRef(0);
  const audioRef = useRef<AudioContext | null>(null);
  const chimeLoopRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    if (!running) return;
    const tick = () => {
      const left = Math.max(0, Math.round((deadlineRef.current - Date.now()) / 1000));
      setRemaining(left);
      if (left <= 0) {
        setRunning(false);
        playChime();
      }
    };
    tick();
    const id = setInterval(tick, 250);
    return () => clearInterval(id);
  }, [running]);

  // Stop any chime when the component unmounts (navigating away).
  useEffect(() => () => stopChime(), []);

  function playChime() {
    try {
      const ctx = new AudioContext();
      audioRef.current = ctx;
      const sequence = () => {
        const beep = (start: number, freq: number, duration: number) => {
          const osc = ctx.createOscillator();
          const gain = ctx.createGain();
          osc.connect(gain);
          gain.connect(ctx.destination);
          osc.type = "sine";
          osc.frequency.value = freq;
          gain.gain.setValueAtTime(0.5, ctx.currentTime + start);
          gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + start + duration);
          osc.start(ctx.currentTime + start);
          osc.stop(ctx.currentTime + start + duration);
        };
        beep(0, 880, 0.3);
        beep(0.5, 880, 0.3);
      };
      sequence();
      chimeLoopRef.current = setInterval(sequence, 2000);
    } catch {
      // Web Audio unavailable (e.g. no user gesture yet) — the visual flash still signals time-up.
    }
  }

  function stopChime() {
    if (chimeLoopRef.current) clearInterval(chimeLoopRef.current);
    chimeLoopRef.current = null;
    audioRef.current?.close().catch(() => {});
    audioRef.current = null;
  }

  function start() {
    const dialed = minutes * 60 + seconds;
    const left = remaining ?? dialed; // resume from a pause, or start fresh from the dial
    if (left <= 0) return;
    if (remaining === null) totalRef.current = dialed;
    deadlineRef.current = Date.now() + left * 1000;
    setRemaining(left);
    setRunning(true);
  }

  function pause() {
    setRunning(false);
  }

  function reset() {
    setRunning(false);
    setRemaining(null);
    totalRef.current = 0;
    stopChime();
  }

  function dialPreset(m: number) {
    if (running) return;
    setMinutes(m);
    setSeconds(0);
    setRemaining(null);
  }

  const display = remaining ?? minutes * 60 + seconds;
  const total = totalRef.current || minutes * 60 + seconds;
  const fraction = total > 0 ? display / total : 1;
  const expired = remaining === 0;
  const ringColor = expired
    ? "stroke-rose-500"
    : display <= 30 && remaining !== null
      ? "stroke-rose-500"
      : display <= total * 0.25 && remaining !== null
        ? "stroke-amber-400"
        : "stroke-violet-500";

  const mm = Math.floor(display / 60).toString().padStart(2, "0");
  const ss = (display % 60).toString().padStart(2, "0");
  const idle = remaining === null;

  return (
    <section className="rounded-3xl border border-purple-100 bg-white p-6 shadow-sm">
      <h2 className="mb-6 flex items-center gap-2 text-lg font-extrabold text-slate-800">
        <TimerIcon size={20} className="text-violet-500" /> {ro.groups.timer.title}
      </h2>

      <div className="flex flex-col gap-6 md:flex-row">
        {/* Presets + custom dial. */}
        <div className="flex flex-shrink-0 flex-col gap-4 md:w-56">
          <div className="rounded-2xl border border-violet-100 bg-violet-50 p-4">
            <p className="mb-3 text-center text-xs font-extrabold uppercase tracking-wide text-violet-700">
              {ro.groups.timer.presets}
            </p>
            <div className="flex flex-wrap justify-center gap-2">
              {PRESETS.map((m) => (
                <button
                  key={m}
                  type="button"
                  onClick={() => dialPreset(m)}
                  className={`rounded-xl border px-3 py-1.5 text-sm font-bold transition ${
                    idle && minutes === m && seconds === 0
                      ? "border-violet-600 bg-violet-600 text-white shadow shadow-violet-200"
                      : "border-violet-200 bg-white text-violet-700 hover:bg-violet-100"
                  }`}
                >
                  {ro.groups.timer.preset(m)}
                </button>
              ))}
            </div>
          </div>

          <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
            <p className="mb-3 text-center text-xs font-extrabold uppercase tracking-wide text-slate-500">
              {ro.groups.timer.custom}
            </p>
            <div className="flex items-center justify-center gap-2">
              <DialColumn
                value={minutes}
                label={ro.groups.timer.minutes}
                disabled={!idle}
                onInc={() => setMinutes((m) => Math.min(99, m + 1))}
                onDec={() => setMinutes((m) => Math.max(0, m - 1))}
              />
              <span className="mb-4 text-2xl font-extrabold text-slate-300">:</span>
              <DialColumn
                value={seconds}
                label={ro.groups.timer.seconds}
                disabled={!idle}
                onInc={() => setSeconds((s) => (s === 55 ? 0 : s + 5))}
                onDec={() => setSeconds((s) => (s === 0 ? 55 : s - 5))}
              />
            </div>
          </div>
        </div>

        {/* Ring + controls. */}
        <div className="flex flex-1 flex-col items-center justify-center gap-6">
          <div className="relative" style={{ width: 220, height: 220 }}>
            <svg width="220" height="220" style={{ transform: "rotate(-90deg)" }}>
              <circle cx="110" cy="110" r={RADIUS} fill="none" stroke="#ede9fe" strokeWidth="14" />
              <circle
                cx="110"
                cy="110"
                r={RADIUS}
                fill="none"
                strokeWidth="14"
                strokeLinecap="round"
                strokeDasharray={CIRCUMFERENCE}
                strokeDashoffset={CIRCUMFERENCE * (1 - fraction)}
                className={`transition-all duration-300 ${ringColor}`}
              />
            </svg>
            <div className="absolute inset-0 flex flex-col items-center justify-center">
              <span
                className={`text-5xl font-extrabold leading-none tabular-nums ${
                  expired ? "animate-pulse text-rose-500" : "text-slate-800"
                }`}
              >
                {mm}:{ss}
              </span>
              {expired && (
                <span className="mt-1 animate-bounce text-sm font-bold text-rose-500">
                  {ro.groups.timer.expired}
                </span>
              )}
            </div>
          </div>

          <div className="flex items-center gap-3">
            {running ? (
              <button
                type="button"
                onClick={pause}
                className="flex items-center gap-2 rounded-2xl bg-amber-400 px-6 py-3 font-extrabold text-white shadow shadow-amber-200 transition hover:bg-amber-500 active:scale-95"
              >
                <Pause size={18} /> {ro.groups.timer.pause}
              </button>
            ) : (
              <button
                type="button"
                onClick={start}
                disabled={expired}
                className="flex items-center gap-2 rounded-2xl bg-gradient-to-r from-violet-600 to-indigo-500 px-6 py-3 font-extrabold text-white shadow shadow-purple-200 transition hover:opacity-90 active:scale-95 disabled:opacity-40"
              >
                <Play size={18} /> {idle ? ro.groups.timer.start : ro.groups.timer.resume}
              </button>
            )}
            <button
              type="button"
              onClick={reset}
              className="flex items-center gap-2 rounded-2xl bg-slate-100 px-5 py-3 font-extrabold text-slate-600 transition hover:bg-slate-200 active:scale-95"
            >
              <RotateCcw size={18} /> {ro.groups.timer.reset}
            </button>
          </div>
        </div>
      </div>
    </section>
  );
}

function DialColumn({
  value,
  label,
  disabled,
  onInc,
  onDec,
}: {
  value: number;
  label: string;
  disabled: boolean;
  onInc: () => void;
  onDec: () => void;
}) {
  return (
    <div className="flex flex-col items-center gap-1">
      <button
        type="button"
        onClick={onInc}
        disabled={disabled}
        className="h-8 w-8 rounded-xl border border-slate-200 bg-white text-sm font-extrabold text-violet-700 shadow-sm transition hover:bg-violet-50 disabled:opacity-40"
      >
        +
      </button>
      <span className="w-10 text-center text-xl font-extrabold tabular-nums text-slate-800">
        {value.toString().padStart(2, "0")}
      </span>
      <button
        type="button"
        onClick={onDec}
        disabled={disabled}
        className="h-8 w-8 rounded-xl border border-slate-200 bg-white text-sm font-extrabold text-violet-700 shadow-sm transition hover:bg-violet-50 disabled:opacity-40"
      >
        −
      </button>
      <span className="text-xs font-medium text-slate-400">{label}</span>
    </div>
  );
}
