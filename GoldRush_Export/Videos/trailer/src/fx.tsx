import React from "react";
import {
  AbsoluteFill,
  Easing,
  Img,
  interpolate,
  random,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { loadFont as loadPoppins } from "@remotion/google-fonts/Poppins";
import { loadFont as loadLilita } from "@remotion/google-fonts/LilitaOne";

export const { fontFamily: POPPINS } = loadPoppins("normal", {
  weights: ["600", "800", "900"],
  subsets: ["latin"],
});
export const { fontFamily: LILITA } = loadLilita("normal", {
  weights: ["400"],
  subsets: ["latin"],
});

export const GOLD = "#FFC21A";
export const GOLD_DARK = "#C9790A";
export const CYAN = "#3FD6F0";
export const RED = "#FF4D5E";
export const GREEN = "#4ADE80";
export const INK = "#0B0F1E";

const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;

/** Screenshot with a slow Ken Burns push. */
export const KenBurns: React.FC<{
  src: string;
  from?: number;
  to?: number;
  x?: [number, number];
  y?: [number, number];
  dur?: number;
  style?: React.CSSProperties;
}> = ({ src, from = 1.05, to = 1.18, x = [0, 0], y = [0, 0], dur, style }) => {
  const frame = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  const d = dur ?? durationInFrames;
  const t = interpolate(frame, [0, d], [0, 1], {
    ...clamp,
    easing: Easing.bezier(0.33, 0, 0.2, 1),
  });
  const s = from + (to - from) * t;
  const tx = x[0] + (x[1] - x[0]) * t;
  const ty = y[0] + (y[1] - y[0]) * t;
  return (
    <AbsoluteFill style={{ overflow: "hidden", ...style }}>
      <Img
        src={staticFile(`img/${src}`)}
        style={{
          width: "100%",
          height: "100%",
          objectFit: "cover",
          transform: `translate(${tx}%, ${ty}%) scale(${s})`,
        }}
      />
    </AbsoluteFill>
  );
};

/** Deterministic camera shake that decays. */
export const useShake = (start: number, length = 12, power = 18) => {
  const frame = useCurrentFrame();
  const t = frame - start;
  if (t < 0 || t > length) return "translate(0px,0px)";
  const k = (1 - t / length) ** 2 * power;
  const dx = (random(`sx${frame}`) - 0.5) * 2 * k;
  const dy = (random(`sy${frame}`) - 0.5) * 2 * k;
  return `translate(${dx}px, ${dy}px) rotate(${dx * 0.04}deg)`;
};

/** White flash peaking at `at`. */
export const Flash: React.FC<{ at: number; color?: string; length?: number; peak?: number }> = ({
  at,
  color = "white",
  length = 10,
  peak = 0.9,
}) => {
  const frame = useCurrentFrame();
  const o = interpolate(frame, [at - 1, at, at + length], [0, peak, 0], clamp);
  return <AbsoluteFill style={{ background: color, opacity: o, pointerEvents: "none" }} />;
};

export const Vignette: React.FC<{ strength?: number; color?: string }> = ({
  strength = 0.75,
  color = "0,0,0",
}) => (
  <AbsoluteFill
    style={{
      background: `radial-gradient(ellipse at center, rgba(${color},0) 45%, rgba(${color},${strength}) 100%)`,
    }}
  />
);

/** Cinematic letterbox bars. */
export const Letterbox: React.FC<{ size?: number }> = ({ size = 70 }) => (
  <>
    <div style={{ position: "absolute", left: 0, right: 0, top: 0, height: size, background: "black" }} />
    <div style={{ position: "absolute", left: 0, right: 0, bottom: 0, height: size, background: "black" }} />
  </>
);

/** Animated film grain. */
export const Grain: React.FC<{ opacity?: number }> = ({ opacity = 0.07 }) => {
  const frame = useCurrentFrame();
  const seed = Math.floor(frame / 2) % 8;
  return (
    <AbsoluteFill style={{ opacity, mixBlendMode: "overlay", pointerEvents: "none" }}>
      <svg width="100%" height="100%">
        <filter id={`g${seed}`}>
          <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" seed={seed} />
          <feColorMatrix type="saturate" values="0" />
        </filter>
        <rect width="100%" height="100%" filter={`url(#g${seed})`} />
      </svg>
    </AbsoluteFill>
  );
};

/** The Gold Rush coin: gold disc, inner ring, diamond. `spin` 0..1 turns it around Y. */
export const Coin: React.FC<{ size: number; spin?: number; style?: React.CSSProperties }> = ({
  size,
  spin = 0,
  style,
}) => {
  const sx = Math.cos(spin * Math.PI * 2);
  const edge = Math.max(0.12, Math.abs(sx));
  return (
    <div
      style={{
        width: size,
        height: size,
        transform: `scaleX(${edge})`,
        filter: `drop-shadow(0 0 ${size * 0.18}px rgba(255,194,26,0.65))`,
        ...style,
      }}
    >
      <svg viewBox="0 0 100 100" width={size} height={size}>
        <defs>
          <radialGradient id="cg" cx="35%" cy="30%" r="80%">
            <stop offset="0" stopColor="#FFE58A" />
            <stop offset="0.55" stopColor={GOLD} />
            <stop offset="1" stopColor={GOLD_DARK} />
          </radialGradient>
        </defs>
        <circle cx="50" cy="50" r="48" fill={GOLD_DARK} />
        <circle cx="50" cy="48" r="46" fill="url(#cg)" />
        <circle cx="50" cy="48" r="32" fill="none" stroke={GOLD_DARK} strokeWidth="5" opacity="0.7" />
        <path d="M50 28 L68 48 L50 68 L32 48 Z" fill={GOLD_DARK} opacity="0.85" />
        <path d="M50 34 L62 48 L50 62 L38 48 Z" fill="#FFD84D" />
        <ellipse cx="36" cy="28" rx="12" ry="6" fill="white" opacity="0.35" transform="rotate(-30 36 28)" />
      </svg>
    </div>
  );
};

/** Floating golden sparkle dust. */
export const Sparkles: React.FC<{ count?: number; color?: string; seed?: string; rise?: number }> = ({
  count = 60,
  color = GOLD,
  seed = "sp",
  rise = 2,
}) => {
  const frame = useCurrentFrame();
  const { width, height } = useVideoConfig();
  return (
    <AbsoluteFill style={{ pointerEvents: "none" }}>
      {new Array(count).fill(0).map((_, i) => {
        const x0 = random(`${seed}x${i}`) * width;
        const y0 = random(`${seed}y${i}`) * height;
        const sp = 0.4 + random(`${seed}s${i}`) * rise;
        const sz = 2 + random(`${seed}z${i}`) * 6;
        const y = (((y0 - frame * sp) % height) + height) % height;
        const x = x0 + Math.sin(frame / 20 + i) * 12;
        const tw = 0.35 + 0.65 * Math.abs(Math.sin(frame / (6 + (i % 7)) + i));
        return (
          <div
            key={i}
            style={{
              position: "absolute",
              left: x,
              top: y,
              width: sz,
              height: sz,
              borderRadius: "50%",
              background: color,
              opacity: tw,
              boxShadow: `0 0 ${sz * 3}px ${color}`,
            }}
          />
        );
      })}
    </AbsoluteFill>
  );
};

/** Coins exploding outward from a point at frame `at`. */
export const CoinBurst: React.FC<{ at: number; x: number; y: number; count?: number; seed?: string }> = ({
  at,
  x,
  y,
  count = 14,
  seed = "cb",
}) => {
  const frame = useCurrentFrame();
  const t = frame - at;
  if (t < 0 || t > 45) return null;
  return (
    <AbsoluteFill style={{ pointerEvents: "none" }}>
      {new Array(count).fill(0).map((_, i) => {
        const ang = (i / count) * Math.PI * 2 + random(`${seed}a${i}`) * 0.5;
        const v = 14 + random(`${seed}v${i}`) * 16;
        const px = x + Math.cos(ang) * v * t;
        const py = y + Math.sin(ang) * v * t + 0.6 * t * t;
        const sz = 34 + random(`${seed}z${i}`) * 40;
        const o = interpolate(t, [0, 30, 45], [1, 1, 0], clamp);
        return (
          <div key={i} style={{ position: "absolute", left: px - sz / 2, top: py - sz / 2, opacity: o }}>
            <Coin size={sz} spin={t / 18 + i * 0.13} />
          </div>
        );
      })}
    </AbsoluteFill>
  );
};

/** Coins raining from the top. */
export const CoinRain: React.FC<{ count?: number; seed?: string; speed?: number }> = ({
  count = 26,
  seed = "cr",
  speed = 1,
}) => {
  const frame = useCurrentFrame();
  const { width, height } = useVideoConfig();
  return (
    <AbsoluteFill style={{ pointerEvents: "none" }}>
      {new Array(count).fill(0).map((_, i) => {
        const x = random(`${seed}x${i}`) * width;
        const delay = random(`${seed}d${i}`) * 60;
        const v = (10 + random(`${seed}v${i}`) * 10) * speed;
        const sz = 40 + random(`${seed}z${i}`) * 60;
        const y = -150 + (frame - delay) * v;
        if (y > height + 100 || frame < delay) return null;
        const blur = sz > 85 ? 3 : 0;
        return (
          <div key={i} style={{ position: "absolute", left: x, top: y, filter: `blur(${blur}px)` }}>
            <Coin size={sz} spin={frame / 22 + i * 0.17} />
          </div>
        );
      })}
    </AbsoluteFill>
  );
};

/** Big punchy word that slams in with overshoot and a chromatic split. */
export const SlamWord: React.FC<{
  text: string;
  color: string;
  delay?: number;
  size?: number;
  glitch?: boolean;
  style?: React.CSSProperties;
}> = ({ text, color, delay = 0, size = 260, glitch = false, style }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: frame - delay, fps, config: { damping: 11, stiffness: 180, mass: 0.8 } });
  const scale = interpolate(s, [0, 1], [2.6, 1]);
  const op = interpolate(frame - delay, [0, 4], [0, 1], clamp);
  const split = glitch
    ? (random(`gl${Math.floor(frame / 2)}`) - 0.5) * 18 * interpolate(frame - delay, [0, 25], [1, 0.25], clamp)
    : interpolate(frame - delay, [0, 10], [14, 0], clamp);
  return (
    <div
      style={{
        fontFamily: LILITA,
        fontSize: size,
        lineHeight: 1,
        color,
        letterSpacing: 4,
        transform: `scale(${scale})`,
        opacity: op,
        textShadow: `${split}px 0 0 rgba(255,40,80,0.75), ${-split}px 0 0 rgba(40,220,255,0.75), 0 10px 0 rgba(0,0,0,0.45), 0 0 60px ${color}88`,
        WebkitTextStroke: "4px rgba(0,0,0,0.35)",
        ...style,
      }}
    >
      {text}
    </div>
  );
};

/** Small caps tagline that types upward. */
export const Tagline: React.FC<{ text: string; delay?: number; size?: number; color?: string }> = ({
  text,
  delay = 0,
  size = 44,
  color = "white",
}) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: frame - delay, fps, config: { damping: 200 } });
  return (
    <div
      style={{
        fontFamily: POPPINS,
        fontWeight: 800,
        fontSize: size,
        color,
        letterSpacing: 6,
        textTransform: "uppercase",
        opacity: s,
        transform: `translateY(${(1 - s) * 30}px)`,
        textShadow: "0 4px 20px rgba(0,0,0,0.8)",
      }}
    >
      {text}
    </div>
  );
};

/** Glass pill label like the in-game HUD. */
export const Pill: React.FC<{ children: React.ReactNode; delay?: number; accent?: string; style?: React.CSSProperties }> = ({
  children,
  delay = 0,
  accent = CYAN,
  style,
}) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: frame - delay, fps, config: { damping: 14, stiffness: 160 } });
  return (
    <div
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 18,
        padding: "18px 36px",
        borderRadius: 999,
        background: "rgba(20,24,40,0.62)",
        border: `3px solid ${accent}`,
        boxShadow: `0 0 30px ${accent}66, inset 0 0 20px ${accent}22`,
        backdropFilter: "blur(8px)",
        fontFamily: POPPINS,
        fontWeight: 800,
        fontSize: 46,
        color: "white",
        transform: `scale(${s})`,
        opacity: s,
        ...style,
      }}
    >
      {children}
    </div>
  );
};

/** Sweeping light streak across the frame. */
export const Glint: React.FC<{ at: number; length?: number }> = ({ at, length = 22 }) => {
  const frame = useCurrentFrame();
  const t = interpolate(frame, [at, at + length], [-40, 140], clamp);
  if (frame < at || frame > at + length) return null;
  return (
    <AbsoluteFill
      style={{
        background: `linear-gradient(105deg, transparent ${t - 12}%, rgba(255,255,255,0.55) ${t}%, transparent ${t + 12}%)`,
        mixBlendMode: "overlay",
        pointerEvents: "none",
      }}
    />
  );
};
