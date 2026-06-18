"use client";

import { useEffect, useRef, useState } from "react";

// Client-only activity countdown (design.md §6.1 — no backend, no server round-trips for a timer).
// Presets + a custom duration, start/pause/reset, and a visual + optional sound alert at zero.

const PRESETS = [
  { label: "1 min", seconds: 60 },
  { label: "2 min", seconds: 120 },
  { label: "5 min", seconds: 300 },
  { label: "10 min", seconds: 600 },
];

function format(ms: number): string {
  const total = Math.max(0, Math.ceil(ms / 1000));
  const minutes = Math.floor(total / 60);
  const seconds = total % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

// A short triple beep via the Web Audio API — no asset to ship, and degrades silently if the browser
// blocks audio (the visual alert still fires).
function playChime() {
  try {
    const Ctx =
      window.AudioContext ??
      (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctx) return;

    const ctx = new Ctx();
    const start = ctx.currentTime;
    for (const offset of [0, 0.25, 0.5]) {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.frequency.value = 880;
      osc.connect(gain);
      gain.connect(ctx.destination);
      gain.gain.setValueAtTime(0.0001, start + offset);
      gain.gain.exponentialRampToValueAtTime(0.3, start + offset + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, start + offset + 0.2);
      osc.start(start + offset);
      osc.stop(start + offset + 0.22);
    }
    setTimeout(() => ctx.close(), 1200);
  } catch {
    // Sound is optional; ignore any audio failure.
  }
}

export function TimerPanel() {
  const [durationMs, setDurationMs] = useState(300_000); // the configured total, restored on reset
  const [remainingMs, setRemainingMs] = useState(300_000);
  const [running, setRunning] = useState(false);
  const [finished, setFinished] = useState(false);
  const [soundOn, setSoundOn] = useState(true);

  const [mins, setMins] = useState("5");
  const [secs, setSecs] = useState("0");

  // Deadline-based ticking so the countdown stays accurate regardless of interval jitter.
  const deadlineRef = useRef(0);
  const soundOnRef = useRef(soundOn);
  soundOnRef.current = soundOn;

  useEffect(() => {
    if (!running) return;

    deadlineRef.current = Date.now() + remainingMs;
    const id = setInterval(() => {
      const left = deadlineRef.current - Date.now();
      if (left <= 0) {
        setRemainingMs(0);
        setRunning(false);
        setFinished(true);
        if (soundOnRef.current) playChime();
      } else {
        setRemainingMs(left);
      }
    }, 200);

    return () => clearInterval(id);
    // Re-armed only when start/pause toggles; the deadline captures the current remaining time.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [running]);

  const applyDuration = (totalMs: number) => {
    setRunning(false);
    setFinished(false);
    setDurationMs(totalMs);
    setRemainingMs(totalMs);
  };

  const applyCustom = () => {
    const totalMs = ((Number(mins) || 0) * 60 + (Number(secs) || 0)) * 1000;
    if (totalMs > 0) applyDuration(totalMs);
  };

  const startPause = () => {
    if (running) {
      // Freeze the exact remaining time on pause.
      setRemainingMs(Math.max(0, deadlineRef.current - Date.now()));
      setRunning(false);
    } else if (remainingMs > 0) {
      setFinished(false);
      setRunning(true);
    }
  };

  const reset = () => {
    setRunning(false);
    setFinished(false);
    setRemainingMs(durationMs);
  };

  return (
    <section className="roster">
      <h2>Timer</h2>

      <div className={`timer-display${finished ? " finished" : ""}`}>
        {finished ? "Time's up!" : format(remainingMs)}
      </div>

      <div className="timer-presets">
        {PRESETS.map((p) => (
          <button
            key={p.seconds}
            className="chip timer-preset"
            onClick={() => applyDuration(p.seconds * 1000)}
          >
            {p.label}
          </button>
        ))}
      </div>

      <div className="timer-custom">
        <label className="timer-field">
          Min
          <input
            aria-label="Minutes"
            className="points-input"
            type="number"
            min={0}
            value={mins}
            onChange={(e) => setMins(e.target.value)}
          />
        </label>
        <label className="timer-field">
          Sec
          <input
            aria-label="Seconds"
            className="points-input"
            type="number"
            min={0}
            max={59}
            value={secs}
            onChange={(e) => setSecs(e.target.value)}
          />
        </label>
        <button className="btn-ghost" onClick={applyCustom}>
          Set
        </button>
      </div>

      <div className="timer-controls">
        <button className="btn-primary inline" onClick={startPause} disabled={remainingMs <= 0}>
          {running ? "Pause" : "Start"}
        </button>
        <button className="btn-ghost" onClick={reset}>
          Reset
        </button>
        <label className="timer-sound">
          <input
            type="checkbox"
            className="pick-box"
            checked={soundOn}
            onChange={(e) => setSoundOn(e.target.checked)}
          />
          Sound
        </label>
      </div>
    </section>
  );
}
