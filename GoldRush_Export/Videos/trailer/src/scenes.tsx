import React from "react";
import { Audio, Video } from "@remotion/media";
import {
  AbsoluteFill,
  Easing,
  interpolate,
  Sequence,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { EVENTS } from "./events";
import {
  Coin,
  CoinRain,
  CYAN,
  Flash,
  Glint,
  GOLD,
  GREEN,
  INK,
  LILITA,
  Letterbox,
  Pill,
  POPPINS,
  RED,
  SlamWord,
  Sparkles,
  Tagline,
  useShake,
  Vignette,
} from "./fx";

const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;

/** Length on the timeline of a clip section. */
export const len = (from: number, to: number, rate = 1) => Math.round((to - from) / rate);

export const Sfx: React.FC<{ at: number; name: string; volume?: number }> = ({ at, name, volume = 0.9 }) => (
  <Sequence from={Math.max(0, at)} layout="none">
    <Audio src={staticFile(`sfx/${name}.wav`)} volume={volume} />
  </Sequence>
);

const Center: React.FC<{ children: React.ReactNode; style?: React.CSSProperties }> = ({ children, style }) => (
  <AbsoluteFill style={{ alignItems: "center", justifyContent: "center", flexDirection: "column", ...style }}>
    {children}
  </AbsoluteFill>
);

const SFX_FOR: Record<string, [string, number]> = {
  coin: ["coin", 0.55],
  gate: ["gate", 0.9],
  win: ["win", 0.9],
  power: ["power", 0.8],
  hit: ["hit", 0.9],
  go: ["go", 0.8],
};

/**
 * A piece of real gameplay recorded in Unity (public/clips/<name>.mp4, 30 fps).
 * Plays source frames [from, to) at `rate`, with a slow camera push, a zoom "punch" on every
 * coin pickup and the matching sound effect on every recorded game event.
 */
export const Clip: React.FC<{
  name: string;
  from: number;
  to: number;
  rate?: number;
  zoom?: [number, number];
  origin?: string;
  filter?: string;
  sfx?: boolean;
  skip?: string[];
}> = ({ name, from, to, rate = 1, zoom = [1.04, 1.12], origin = "50% 50%", filter, sfx = true, skip = [] }) => {
  const frame = useCurrentFrame();
  const d = len(from, to, rate);
  const events = (EVENTS[name] ?? [])
    .filter(([f, t]) => f >= from && f < to && !skip.includes(t))
    .map(([f, t]) => [Math.round((f - from) / rate), t] as [number, string]);
  let punch = 0;
  for (const [at, t] of events) {
    if (t !== "coin" && t !== "power") continue;
    const k = frame - at;
    if (k >= 0 && k < 8) punch = Math.max(punch, interpolate(k, [0, 2, 8], [0, 1, 0]) * (t === "power" ? 0.06 : 0.035));
  }
  const s = interpolate(frame, [0, d], zoom, clamp) + punch;
  return (
    <AbsoluteFill style={{ background: "black", overflow: "hidden" }}>
      <AbsoluteFill style={{ transform: `scale(${s})`, transformOrigin: origin, filter }}>
        <Video
          src={staticFile(`clips/${name}.mp4`)}
          trimBefore={from}
          trimAfter={to}
          playbackRate={rate}
          muted
          style={{ width: "100%", height: "100%", objectFit: "cover" }}
        />
      </AbsoluteFill>
      {sfx &&
        events.map(([at, t], i) =>
          SFX_FOR[t] ? <Sfx key={i} at={at} name={SFX_FOR[t][0]} volume={SFX_FOR[t][1]} /> : null,
        )}
    </AbsoluteFill>
  );
};

/** Section title at the bottom of the frame over a dark gradient. */
const Caption: React.FC<{ word: string; sub: string; color: string; glitch?: boolean; delay?: number; out?: number }> = ({
  word,
  sub,
  color,
  glitch,
  delay = 2,
  out = 9999,
}) => {
  const frame = useCurrentFrame();
  const o = interpolate(frame, [out, out + 8], [1, 0], clamp);
  return (
    <AbsoluteFill style={{ opacity: o }}>
      <AbsoluteFill style={{ background: "linear-gradient(0deg, rgba(0,0,0,0.65) 0%, rgba(0,0,0,0) 38%)" }} />
      <Center style={{ justifyContent: "flex-end", paddingBottom: 120 }}>
        <SlamWord text={word} color={color} delay={delay} size={210} glitch={glitch} />
        <div style={{ marginTop: 6 }}>
          <Tagline text={sub} delay={delay + 8} size={40} />
        </div>
      </Center>
    </AbsoluteFill>
  );
};

/* ───────────── 1. Cold open ───────────── */
export const INTRO = 80;
export const Intro: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: frame - 6, fps, config: { damping: 18, stiffness: 60 } });
  const glow = interpolate(frame, [0, 50], [0, 1], clamp);
  const zoom = interpolate(frame, [52, 80], [1, 9], { ...clamp, easing: Easing.in(Easing.cubic) });
  return (
    <AbsoluteFill style={{ background: INK }}>
      <AbsoluteFill
        style={{ background: `radial-gradient(circle at 50% 50%, rgba(255,194,26,${0.35 * glow}) 0%, rgba(11,15,30,0) 45%)` }}
      />
      <Sparkles count={70} seed="intro" rise={1.2} />
      <Center style={{ transform: `scale(${zoom})` }}>
        <Coin size={320 * s} spin={frame / 40} />
      </Center>
      <Center style={{ top: 360 }}>
        <div style={{ opacity: interpolate(frame, [16, 28, 46, 54], [0, 1, 1, 0], clamp) }}>
          <Tagline text="There's gold in the city..." delay={16} size={48} color="#FFE7A3" />
        </div>
      </Center>
      <Letterbox />
      <Sfx at={6} name="whoosh" volume={0.7} />
      <Sfx at={18} name="coin" volume={0.6} />
      <Sfx at={58} name="whoosh" />
    </AbsoluteFill>
  );
};

/* ───────────── 2. The real main menu ───────────── */
export const TITLE = len(0, 150, 1.25);
export const Title: React.FC = () => {
  const shake = useShake(0, 14, 24);
  return (
    <AbsoluteFill style={{ transform: shake }}>
      <Clip name="menu_title" from={0} to={150} rate={1.25} zoom={[1.18, 1.02]} origin="50% 30%" />
      <Sparkles count={30} seed="title" />
      <Vignette strength={0.5} />
      <Glint at={30} length={26} />
      <Flash at={0} />
      <Letterbox />
      <Sfx at={0} name="hit" />
      <Sfx at={4} name="go" volume={0.7} />
    </AbsoluteFill>
  );
};

/* ───────────── 3. Level 1 intro swoop + countdown ───────────── */
export const COUNT = len(18, 100);
export const Countdown: React.FC = () => (
  <AbsoluteFill>
    <Clip name="l1_run" from={18} to={100} zoom={[1.0, 1.06]} />
    <Sfx at={92 - 18 - 60} name="count" volume={0.7} />
    <Sfx at={92 - 18 - 30} name="count" volume={0.7} />
    <Flash at={92 - 18} peak={0.45} length={8} />
    <Letterbox />
  </AbsoluteFill>
);

/* ───────────── 4. GRAB ───────────── */
export const GRAB = len(100, 310, 1.3);
export const Grab: React.FC = () => (
  <AbsoluteFill>
    <Clip name="l1_run" from={100} to={310} rate={1.3} zoom={[1.02, 1.12]} />
    <Caption word="GRAB" sub="every coin in the arena" color={GOLD} out={GRAB - 40} />
    <Letterbox />
    <Sfx at={2} name="whoosh" volume={0.5} />
  </AbsoluteFill>
);

/* ───────────── 5. DODGE (slow-mo close call) → CAUGHT ───────────── */
export const DODGE = len(470, 560, 0.75) + 72;
export const Dodge: React.FC = () => {
  const frame = useCurrentFrame();
  const slow = len(470, 560, 0.75);
  const shake = useShake(slow, 16, 36);
  const pulse = 0.18 + 0.14 * Math.abs(Math.sin(frame / 5));
  return (
    <AbsoluteFill style={{ transform: shake }}>
      <Sequence durationInFrames={slow}>
        <Clip name="l2_chase" from={470} to={560} rate={0.75} zoom={[1.1, 1.22]} filter="saturate(1.15) contrast(1.05)" />
        <AbsoluteFill style={{ background: `rgba(255,30,60,${pulse})`, mixBlendMode: "multiply" }} />
        <Vignette strength={0.85} color="110,0,20" />
        <Caption word="DODGE" sub="the monsters on patrol" color={RED} glitch />
        <Sfx at={2} name="alert" />
        <Sfx at={40} name="alert" volume={0.6} />
      </Sequence>
      <Sequence from={slow}>
        <Clip name="lose" from={0} to={72} zoom={[1.15, 1.0]} sfx={false} />
        <Vignette strength={0.8} color="110,0,20" />
        <Center style={{ justifyContent: "flex-start", paddingTop: 100 }}>
          <Tagline text="...or get caught" delay={4} color="#FFB3BC" size={54} />
        </Center>
        <Sfx at={0} name="hit" />
        <Sfx at={3} name="lose" volume={0.6} />
      </Sequence>
      <Flash at={slow} color={RED} peak={0.75} />
      <Letterbox />
    </AbsoluteFill>
  );
};

/* ───────────── 6. Power-ups (three real pickups) ───────────── */
const PW = [
  { from: 30, to: 125, label: "MAGNET", desc: "gold flies to you", c: "#FF5A7A" },
  { from: 362, to: 445, label: "GHOST", desc: "monsters can't see you", c: "#B48CFF" },
  { from: 538, to: 630, label: "SPEED", desc: "outrun everything", c: "#3FD6F0" },
];
const PW_RATE = 1.25;
export const POWER = PW.reduce((a, p) => a + len(p.from, p.to, PW_RATE), 0);
export const PowerUps: React.FC = () => {
  let at = 0;
  return (
    <AbsoluteFill>
      {PW.map((p, i) => {
        const d = len(p.from, p.to, PW_RATE);
        const start = at;
        at += d;
        return (
          <Sequence key={p.label} from={start} durationInFrames={d}>
            <Clip name="l4_power" from={p.from} to={p.to} rate={PW_RATE} zoom={[1.06, 1.14]} />
            <Vignette strength={0.6} />
            <Center style={{ justifyContent: "flex-end", paddingBottom: 130, gap: 18 }}>
              {i === 0 && <Tagline text="Power-ups" size={46} color={GOLD} />}
              <Pill delay={3} accent={p.c}>
                <span style={{ color: p.c, fontFamily: LILITA, fontSize: 64, fontWeight: 400 }}>{p.label}</span>
                <span style={{ fontSize: 36, opacity: 0.9 }}>{p.desc}</span>
              </Pill>
            </Center>
            <Flash at={0} peak={0.35} length={6} />
            <Sfx at={0} name="whoosh" volume={0.5} />
          </Sequence>
        );
      })}
      <Letterbox />
    </AbsoluteFill>
  );
};

/* ───────────── 7. Day & night (orbit around the hero) ───────────── */
const DN_FROM = 70, DN_TO = 300, DN_RATE = 1.5;
export const DAYNIGHT = len(DN_FROM, DN_TO, DN_RATE);
export const DayNight: React.FC = () => {
  const frame = useCurrentFrame();
  const t1 = Math.round((100 - DN_FROM) / DN_RATE), t2 = Math.round((220 - DN_FROM) / DN_RATE);
  const day = frame >= t1 + 20 && frame < t2 + 20;
  return (
    <AbsoluteFill>
      <Clip name="l3_orbit" from={DN_FROM} to={DN_TO} rate={DN_RATE} zoom={[1.05, 1.15]} />
      <Vignette strength={0.55} />
      <Center style={{ justifyContent: "flex-end", paddingBottom: 130 }}>
        <Pill delay={4} accent={day ? GOLD : CYAN}>
          <span style={{ fontSize: 54 }}>{day ? "☀" : "☾"}</span>
          DAY &amp; NIGHT
        </Pill>
        <div style={{ marginTop: 18 }}>
          <Tagline text="switch any time — night monsters glow" delay={14} size={34} color="#BDF3FF" />
        </div>
      </Center>
      <Flash at={t1 + 4} peak={0.25} length={14} />
      <Flash at={t2 + 4} peak={0.25} length={14} color="#1b2a6b" />
      <Letterbox />
      <Sfx at={t1} name="click" />
      <Sfx at={t1 + 2} name="whoosh" volume={0.6} />
      <Sfx at={t2} name="click" />
      <Sfx at={t2 + 2} name="whoosh" volume={0.6} />
    </AbsoluteFill>
  );
};

/* ───────────── 8. ESCAPE: gate opens, run out, 3 stars ───────────── */
const ES_FROM = 455, ES_TO = 716, ES_RATE = 1.2;
export const ESCAPE = len(ES_FROM, ES_TO, ES_RATE);
export const Escape: React.FC = () => {
  const gate = Math.round((495 - ES_FROM) / ES_RATE);
  const win = Math.round((566 - ES_FROM) / ES_RATE);
  const stars = Math.round((598 - ES_FROM) / ES_RATE);
  return (
    <AbsoluteFill>
      <Clip name="l1_run" from={ES_FROM} to={ES_TO} rate={ES_RATE} zoom={[1.04, 1.12]} />
      <Sequence from={gate - 4} durationInFrames={win - gate + 4} layout="none">
        <Caption word="ESCAPE" sub="the gate opens when the gold is yours" color={GREEN} delay={0} />
      </Sequence>
      <Sequence from={win}>
        <CoinRain count={22} seed="winrain" speed={0.8} />
        <Sparkles count={40} seed="win" rise={3} />
      </Sequence>
      <Flash at={win} peak={0.6} />
      <Letterbox />
      {[0, 1, 2].map((i) => (
        <Sfx key={i} at={stars + i * 6} name="star" volume={0.7} />
      ))}
    </AbsoluteFill>
  );
};

/* ───────────── 9. Levels + shop (real menus) ───────────── */
export const LEVELS = 80;
export const Levels: React.FC = () => (
  <AbsoluteFill>
    <Clip name="menu_levels" from={5} to={85} zoom={[1.12, 1.0]} />
    <Center style={{ justifyContent: "flex-end", paddingBottom: 120 }}>
      <Pill delay={6} accent={GOLD}>
        4 LEVELS&nbsp;+&nbsp;<span style={{ color: "#C4A6FF" }}>A NEW DAILY RUSH</span>
      </Pill>
    </Center>
    <Glint at={16} />
    <Letterbox />
    <Sfx at={0} name="whoosh" volume={0.6} />
    <Sfx at={6} name="click" />
  </AbsoluteFill>
);

export const SHOP = 85;
export const Shop: React.FC = () => (
  <AbsoluteFill>
    <Clip name="menu_shop" from={12} to={97} zoom={[1.0, 1.12]} origin="30% 55%" />
    <Center style={{ justifyContent: "flex-end", paddingBottom: 120 }}>
      <Pill delay={6} accent={GOLD}>
        SPEND YOUR GOLD ON&nbsp;<span style={{ color: GOLD }}>COLOURS</span>&nbsp;&amp;&nbsp;<span style={{ color: CYAN }}>HATS</span>
      </Pill>
    </Center>
    <Letterbox />
    <Sfx at={6} name="buy" volume={0.7} />
  </AbsoluteFill>
);

/* ───────────── 10. Finale ───────────── */
export const FINALE = 180;
export const Finale: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const shake = useShake(14, 16, 22);
  const logo = spring({ frame: frame - 10, fps, config: { damping: 9, stiffness: 160 } });
  const btn = spring({ frame: frame - 45, fps, config: { damping: 12 } });
  const pulse = 1 + 0.04 * Math.sin(frame / 5);
  const fadeOut = interpolate(frame, [150, 175], [1, 0], clamp);
  return (
    <AbsoluteFill style={{ background: "black" }}>
      <AbsoluteFill style={{ opacity: fadeOut, transform: shake }}>
        <Clip name="l4_power" from={430} to={520} rate={0.5} zoom={[1.2, 1.05]} filter="blur(7px) brightness(0.45)" sfx={false} />
        <AbsoluteFill style={{ background: "radial-gradient(circle at 50% 45%, rgba(255,194,26,0.35), transparent 55%)" }} />
        <CoinRain count={30} seed="fin" />
        <Sparkles count={70} seed="fin2" rise={2.5} />
        <Center style={{ top: -60 }}>
          <div
            style={{
              fontFamily: LILITA,
              fontSize: 230,
              letterSpacing: 6,
              display: "flex",
              gap: 46,
              transform: `scale(${interpolate(logo, [0, 1], [0.2, 1])}) rotate(${(1 - logo) * -8}deg)`,
              opacity: Math.min(1, logo * 2),
            }}
          >
            <span style={{ color: GOLD, textShadow: "0 14px 0 #B86A00, 0 24px 0 rgba(0,0,0,0.55), 0 0 90px #FFC21A99" }}>GOLD</span>
            <span style={{ color: CYAN, textShadow: "0 14px 0 #11788C, 0 24px 0 rgba(0,0,0,0.55), 0 0 90px #3FD6F099" }}>RUSH</span>
          </div>
          <div style={{ marginTop: 30 }}>
            <Tagline text="Grab the gold. Beat the clock. Get out alive." delay={28} size={40} color="#FFE7A3" />
          </div>
          <div
            style={{
              marginTop: 60,
              transform: `scale(${btn * pulse})`,
              opacity: btn,
              padding: "26px 80px",
              borderRadius: 36,
              background: "linear-gradient(180deg, #5BE3F6, #1A9FC0)",
              border: "4px solid rgba(255,255,255,0.85)",
              boxShadow: "0 12px 0 #0E6A82, 0 0 60px rgba(63,214,240,0.7)",
              fontFamily: POPPINS,
              fontWeight: 900,
              fontSize: 64,
              color: "white",
              letterSpacing: 4,
              display: "flex",
              alignItems: "center",
              gap: 24,
            }}
          >
            <span style={{ fontSize: 50 }}>▶</span> PLAY NOW
          </div>
          <div style={{ marginTop: 50 }}>
            <Tagline text="Phone · Tablet · PC" delay={60} size={30} color="rgba(255,255,255,0.75)" />
          </div>
        </Center>
        <Glint at={70} length={30} />
        <Flash at={14} />
      </AbsoluteFill>
      <Letterbox />
      <Sfx at={10} name="whoosh" />
      <Sfx at={14} name="hit" />
      <Sfx at={16} name="win" volume={0.8} />
      <Sfx at={45} name="go" />
    </AbsoluteFill>
  );
};
