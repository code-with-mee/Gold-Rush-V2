import React from "react";
import { Audio } from "@remotion/media";
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
import {
  CoinBurst,
  CoinRain,
  Coin,
  CYAN,
  Flash,
  Glint,
  GOLD,
  GREEN,
  INK,
  KenBurns,
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

export const Sfx: React.FC<{ at: number; name: string; volume?: number }> = ({ at, name, volume = 0.9 }) => (
  <Sequence from={at} layout="none">
    <Audio src={staticFile(`sfx/${name}.wav`)} volume={volume} />
  </Sequence>
);

const Center: React.FC<{ children: React.ReactNode; style?: React.CSSProperties }> = ({ children, style }) => (
  <AbsoluteFill style={{ alignItems: "center", justifyContent: "center", flexDirection: "column", ...style }}>
    {children}
  </AbsoluteFill>
);

/* ───────────── 1. Cold open: a single coin spins out of the dark ───────────── */
export const Intro: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: frame - 8, fps, config: { damping: 18, stiffness: 60 } });
  const glow = interpolate(frame, [0, 60], [0, 1], clamp);
  const zoom = interpolate(frame, [60, 90], [1, 9], { ...clamp, easing: Easing.in(Easing.cubic) });
  return (
    <AbsoluteFill style={{ background: INK }}>
      <AbsoluteFill
        style={{
          background: `radial-gradient(circle at 50% 50%, rgba(255,194,26,${0.35 * glow}) 0%, rgba(11,15,30,0) 45%)`,
        }}
      />
      <Sparkles count={70} seed="intro" rise={1.2} />
      <Center style={{ transform: `scale(${zoom})` }}>
        <Coin size={320 * s} spin={frame / 40} />
      </Center>
      <Center style={{ top: 360 }}>
        <div style={{ opacity: interpolate(frame, [20, 32, 55, 64], [0, 1, 1, 0], clamp) }}>
          <Tagline text="There's gold in the city..." delay={20} size={48} color="#FFE7A3" />
        </div>
      </Center>
      <Letterbox />
      <Sfx at={8} name="whoosh" volume={0.7} />
      <Sfx at={22} name="coin" volume={0.6} />
      <Sfx at={66} name="whoosh" />
    </AbsoluteFill>
  );
};

/* ───────────── 2. Title slam ───────────── */
export const Title: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const shake = useShake(6, 16, 26);
  const gold = spring({ frame: frame - 4, fps, config: { damping: 10, stiffness: 200 } });
  const rush = spring({ frame: frame - 10, fps, config: { damping: 10, stiffness: 200 } });
  const word = (t: string, c: string, shadow: string, s: number, dx: number) => (
    <span
      style={{
        display: "inline-block",
        color: c,
        transform: `translateX(${(1 - s) * dx}px) scale(${interpolate(s, [0, 1], [3, 1])})`,
        opacity: Math.min(1, s * 2),
        textShadow: `0 14px 0 ${shadow}, 0 22px 0 rgba(0,0,0,0.55), 0 0 80px ${c}88`,
      }}
    >
      {t}
    </span>
  );
  return (
    <AbsoluteFill style={{ background: "black", transform: shake }}>
      <KenBurns src="05b_level1_night.jpg" from={1.3} to={1.1} y={[0, 0]} style={{ filter: "blur(9px) brightness(0.55) saturate(1.3)" }} />
      <Vignette strength={0.8} />
      <Sparkles count={40} seed="title" />
      <Center style={{ top: -120 }}>
        <div style={{ fontFamily: LILITA, fontSize: 250, letterSpacing: 6, display: "flex", gap: 50 }}>
          {word("GOLD", GOLD, "#B86A00", gold, -600)}
          {word("RUSH", CYAN, "#11788C", rush, 600)}
        </div>
        <div style={{ marginTop: 40, display: "flex", gap: 40 }}>
          {["GRAB", "DODGE", "ESCAPE"].map((w, i) => (
            <Pill key={w} delay={30 + i * 8} accent={[GOLD, RED, GREEN][i]}>
              {w}
            </Pill>
          ))}
        </div>
      </Center>
      <Glint at={40} length={28} />
      <Flash at={6} />
      <Letterbox />
      <Sfx at={4} name="hit" />
      <Sfx at={10} name="go" />
      <Sfx at={30} name="click" />
      <Sfx at={38} name="click" />
      <Sfx at={46} name="click" />
    </AbsoluteFill>
  );
};

/* ───────────── 3. GRAB ───────────── */
export const Grab: React.FC = () => {
  const frame = useCurrentFrame();
  const count = Math.min(6, Math.max(0, Math.floor((frame - 18) / 9) + 1));
  const bump = (() => {
    const t = (frame - 18) % 9;
    return frame >= 18 && count <= 6 ? interpolate(t, [0, 3, 9], [1.35, 1.05, 1], clamp) : 1;
  })();
  return (
    <AbsoluteFill style={{ background: "black" }}>
      <KenBurns src="05_level1_play.jpg" from={1.1} to={1.45} x={[0, -4]} y={[0, 6]} />
      <AbsoluteFill style={{ background: "linear-gradient(0deg, rgba(255,170,0,0.25), transparent 60%)" }} />
      <Vignette />
      <CoinBurst at={18} x={1020} y={330} seed="g1" />
      <CoinBurst at={45} x={720} y={420} seed="g2" count={10} />
      <Center style={{ justifyContent: "flex-end", paddingBottom: 150 }}>
        <SlamWord text="GRAB" color={GOLD} delay={2} size={280} />
        <Tagline text="every coin in the arena" delay={12} />
      </Center>
      <div
        style={{
          position: "absolute",
          left: 110,
          top: 120,
          display: "flex",
          alignItems: "center",
          gap: 20,
          padding: "16px 34px 16px 20px",
          borderRadius: 999,
          background: "rgba(20,24,40,0.7)",
          border: `3px solid ${GOLD}`,
          transform: `scale(${bump * 1.25})`,
          transformOrigin: "left center",
          fontFamily: POPPINS,
          fontWeight: 800,
          fontSize: 60,
          color: "white",
        }}
      >
        <Coin size={70} spin={frame / 30} />
        {count} / 6
      </div>
      <Letterbox />
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <Sfx key={i} at={18 + i * 9} name="coin" volume={0.6} />
      ))}
      <Sfx at={2} name="whoosh" volume={0.6} />
    </AbsoluteFill>
  );
};

/* ───────────── 4. DODGE ───────────── */
export const Dodge: React.FC = () => {
  const frame = useCurrentFrame();
  const shake = useShake(40, 14, 34);
  const showCaught = frame >= 40;
  const pulse = 0.25 + 0.2 * Math.abs(Math.sin(frame / 5));
  const zoom = interpolate(frame, [40, 46], [1.15, 1.0], clamp);
  return (
    <AbsoluteFill style={{ background: "black", transform: shake }}>
      {!showCaught ? (
        <KenBurns src="sunset.jpg" from={1.05} to={1.25} x={[2, -3]} style={{ filter: "saturate(1.15)" }} />
      ) : (
        <AbsoluteFill style={{ transform: `scale(${zoom})` }}>
          <KenBurns src="10_lose.jpg" from={1.0} to={1.08} style={{ filter: "saturate(0.8)" }} />
        </AbsoluteFill>
      )}
      <Vignette strength={0.9} color="120,0,20" />
      <AbsoluteFill style={{ background: `rgba(255,30,60,${showCaught ? 0.12 : pulse * 0.25})`, mixBlendMode: "multiply" }} />
      {!showCaught && (
        <Center style={{ justifyContent: "flex-end", paddingBottom: 140 }}>
          <SlamWord text="DODGE" color={RED} delay={2} size={280} glitch />
          <Tagline text="the monsters on patrol" delay={10} />
        </Center>
      )}
      {showCaught && (
        <Center style={{ justifyContent: "flex-start", paddingTop: 110 }}>
          <Tagline text="...or get caught" delay={44} color="#FFB3BC" size={52} />
        </Center>
      )}
      <Flash at={40} color={RED} peak={0.7} />
      <Letterbox />
      <Sfx at={2} name="alert" />
      <Sfx at={16} name="alert" volume={0.6} />
      <Sfx at={40} name="hit" />
      <Sfx at={44} name="lose" volume={0.6} />
    </AbsoluteFill>
  );
};

/* ───────────── 5. ESCAPE ───────────── */
export const Escape: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const cut = 58;
  const push = interpolate(frame, [0, cut], [1.05, 2.4], { ...clamp, easing: Easing.in(Easing.quad) });
  const blur = interpolate(frame, [cut - 16, cut], [0, 10], clamp);
  return (
    <AbsoluteFill style={{ background: "black" }}>
      {frame < cut ? (
        <>
          <AbsoluteFill style={{ transform: `scale(${push})`, transformOrigin: "23% 18%", filter: `blur(${blur}px)` }}>
            <KenBurns src="06_gate_open.jpg" from={1} to={1} />
          </AbsoluteFill>
          <AbsoluteFill
            style={{
              background: `radial-gradient(circle at 23% 18%, rgba(74,222,128,${interpolate(frame, [0, cut], [0.1, 0.7], clamp)}) 0%, transparent 40%)`,
              mixBlendMode: "screen",
            }}
          />
          <Vignette />
          <Center style={{ justifyContent: "flex-end", paddingBottom: 140 }}>
            <SlamWord text="ESCAPE" color={GREEN} delay={4} size={270} />
            <Tagline text="the gate opens when the gold is yours" delay={14} />
          </Center>
        </>
      ) : (
        <WinCard start={cut} />
      )}
      <Flash at={cut} />
      <Letterbox />
      <Sfx at={4} name="gate" />
      <Sfx at={cut - 14} name="whoosh" />
      <Sfx at={cut} name="win" />
      {[0, 1, 2].map((i) => (
        <Sfx key={i} at={cut + 14 + i * 7} name="star" volume={0.7} />
      ))}
    </AbsoluteFill>
  );

  function WinCard({ start }: { start: number }) {
    const f = frame - start;
    return (
      <AbsoluteFill>
        <KenBurns src="07_win.jpg" from={1.18} to={1.08} dur={80} />
        <Sparkles count={50} seed="win" rise={3} />
        {[0, 1, 2].map((i) => {
          const s = spring({ frame: f - 14 - i * 7, fps, config: { damping: 8, stiffness: 220 } });
          const x = [740, 960, 1180][i];
          const y = i === 1 ? 360 : 395;
          return (
            <div
              key={i}
              style={{
                position: "absolute",
                left: x - 110,
                top: y - 110,
                width: 220,
                height: 220,
                borderRadius: "50%",
                background: "radial-gradient(circle, rgba(255,210,60,0.75), transparent 65%)",
                transform: `scale(${s * 1.4})`,
                opacity: interpolate(f - 14 - i * 7, [0, 4, 20], [0, 1, 0], clamp),
              }}
            />
          );
        })}
        <CoinRain count={18} seed="winrain" speed={0.8} />
      </AbsoluteFill>
    );
  }
};

/* ───────────── 6. Day & Night wipe ───────────── */
export const DayNight: React.FC = () => {
  const frame = useCurrentFrame();
  const p = interpolate(frame, [18, 62], [100, 0], { ...clamp, easing: Easing.inOut(Easing.cubic) });
  return (
    <AbsoluteFill style={{ background: "black" }}>
      <KenBurns src="05b_level1_night.jpg" from={1.08} to={1.2} />
      <AbsoluteFill style={{ clipPath: `inset(0 ${100 - p}% 0 0)` }}>
        <KenBurns src="05_level1_play.jpg" from={1.08} to={1.2} />
      </AbsoluteFill>
      <div
        style={{
          position: "absolute",
          top: 0,
          bottom: 0,
          left: `${p}%`,
          width: 6,
          marginLeft: -3,
          background: "white",
          boxShadow: `0 0 40px 12px ${CYAN}`,
          opacity: p > 0 && p < 100 ? 1 : 0,
        }}
      />
      <Vignette strength={0.6} />
      <Center style={{ justifyContent: "flex-start", paddingTop: 130 }}>
        <Pill delay={4} accent={frame < 40 ? GOLD : CYAN}>
          <span style={{ fontSize: 54 }}>{frame < 40 ? "☀" : "☾"}</span>
          DAY &amp; NIGHT MODE
        </Pill>
      </Center>
      <Center style={{ justifyContent: "flex-end", paddingBottom: 140 }}>
        <Tagline text="night levels — watch for glowing monsters" delay={50} size={40} color="#BDF3FF" />
      </Center>
      <Letterbox />
      <Sfx at={4} name="click" />
      <Sfx at={20} name="whoosh" />
    </AbsoluteFill>
  );
};

/* ───────────── 7. Levels: cards fly in ───────────── */
export const Levels: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const tilt = interpolate(frame, [0, 80], [14, 0], { ...clamp, easing: Easing.out(Easing.cubic) });
  const s = spring({ frame, fps, config: { damping: 16 } });
  return (
    <AbsoluteFill style={{ background: INK, perspective: 1600 }}>
      <AbsoluteFill
        style={{
          transform: `rotateY(${-tilt}deg) rotateX(${tilt * 0.4}deg) scale(${0.8 + 0.2 * s})`,
          borderRadius: 30,
          overflow: "hidden",
          boxShadow: `0 30px 120px rgba(63,214,240,0.35)`,
        }}
      >
        <KenBurns src="02_levels.jpg" from={1.0} to={1.08} />
      </AbsoluteFill>
      <Center style={{ justifyContent: "flex-end", paddingBottom: 130 }}>
        <Pill delay={10} accent={GOLD}>
          4 LEVELS&nbsp;+&nbsp;<span style={{ color: "#C4A6FF" }}>A NEW DAILY RUSH</span>
        </Pill>
      </Center>
      <Glint at={22} />
      <Letterbox />
      <Sfx at={2} name="whoosh" />
      <Sfx at={10} name="click" />
    </AbsoluteFill>
  );
};

/* ───────────── 8. Power-ups ───────────── */
export const PowerUps: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const items = [
    { t: "SPEED", c: "#FFB020", icon: "⚡" },
    { t: "GHOST", c: "#B48CFF", icon: "👻" },
    { t: "MAGNET", c: "#FF5A7A", icon: "🧲" },
  ];
  return (
    <AbsoluteFill style={{ background: "black" }}>
      <KenBurns src="11_level4.jpg" from={1.15} to={1.35} y={[0, 4]} style={{ filter: "brightness(0.65)" }} />
      <Vignette strength={0.85} />
      <Sparkles count={40} color="#B48CFF" seed="pw" />
      <Center style={{ gap: 50 }}>
        <Tagline text="Power-ups" delay={0} size={56} color={GOLD} />
        <div style={{ display: "flex", gap: 60 }}>
          {items.map((it, i) => {
            const d = 8 + i * 9;
            const s = spring({ frame: frame - d, fps, config: { damping: 9, stiffness: 170 } });
            const bob = Math.sin((frame - d) / 8) * 8;
            return (
              <div
                key={it.t}
                style={{
                  width: 330,
                  height: 330,
                  borderRadius: 44,
                  background: `linear-gradient(160deg, ${it.c}EE, ${it.c}66)`,
                  border: "4px solid rgba(255,255,255,0.7)",
                  boxShadow: `0 0 70px ${it.c}AA`,
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "center",
                  justifyContent: "center",
                  gap: 10,
                  transform: `translateY(${(1 - s) * 300 + bob}px) rotate(${(1 - s) * (i - 1) * 25}deg) scale(${s})`,
                  opacity: Math.min(1, s * 1.5),
                }}
              >
                <div style={{ fontSize: 150 }}>{it.icon}</div>
                <div style={{ fontFamily: LILITA, fontSize: 70, color: "white", textShadow: "0 5px 0 rgba(0,0,0,0.35)" }}>
                  {it.t}
                </div>
              </div>
            );
          })}
        </div>
      </Center>
      <Letterbox />
      {[0, 1, 2].map((i) => (
        <Sfx key={i} at={8 + i * 9} name="power" volume={0.7} />
      ))}
    </AbsoluteFill>
  );
};

/* ───────────── 9. Style your hero ───────────── */
export const Style: React.FC = () => {
  const frame = useCurrentFrame();
  const cut = 50;
  return (
    <AbsoluteFill style={{ background: "black" }}>
      {frame < cut ? (
        <KenBurns src="hats.jpg" from={1.25} to={1.25} x={[9, -9]} dur={cut} />
      ) : (
        <KenBurns src="03_shop.jpg" from={1.2} to={1.05} x={[-4, 0]} dur={50} />
      )}
      <Vignette strength={0.55} />
      <Center style={{ justifyContent: "flex-end", paddingBottom: 130 }}>
        <Pill delay={6} accent={GOLD}>
          SPEND YOUR GOLD ON&nbsp;<span style={{ color: GOLD }}>COLOURS</span>&nbsp;&amp;&nbsp;<span style={{ color: CYAN }}>HATS</span>
        </Pill>
      </Center>
      <Flash at={cut} peak={0.5} length={8} />
      <Letterbox />
      <Sfx at={6} name="click" />
      <Sfx at={cut} name="buy" />
    </AbsoluteFill>
  );
};

/* ───────────── 10. Finale ───────────── */
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
        <KenBurns src="night.jpg" from={1.2} to={1.05} style={{ filter: "blur(6px) brightness(0.45)" }} />
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
