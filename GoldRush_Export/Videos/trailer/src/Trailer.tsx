import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, interpolate, staticFile, useVideoConfig } from "remotion";
import { linearTiming, TransitionSeries } from "@remotion/transitions";
import { fade } from "@remotion/transitions/fade";
import { slide } from "@remotion/transitions/slide";
import { wipe } from "@remotion/transitions/wipe";
import { LightLeak } from "@remotion/light-leaks";
import { Grain } from "./fx";
import { DayNight, Dodge, Escape, Finale, Grab, Intro, Levels, PowerUps, Style, Title } from "./scenes";

const T = 12;

export const SCENES = {
  intro: 90,
  title: 110,
  grab: 85,
  dodge: 85,
  escape: 140,
  daynight: 90,
  levels: 75,
  power: 80,
  style: 100,
  finale: 180,
};
// 3 transitions of T frames; overlays do not change length.
export const TRAILER_FRAMES = Object.values(SCENES).reduce((a, b) => a + b, 0) - 3 * T;

const Music: React.FC = () => {
  const { durationInFrames } = useVideoConfig();
  return (
    <Audio
      src={staticFile("sfx/music.wav")}
      loop
      volume={(f) =>
        interpolate(f, [0, 20, durationInFrames - 50, durationInFrames], [0, 0.55, 0.55, 0], {
          extrapolateLeft: "clamp",
          extrapolateRight: "clamp",
        })
      }
    />
  );
};

export const Trailer: React.FC = () => (
  <AbsoluteFill style={{ background: "black" }}>
    <TransitionSeries>
      <TransitionSeries.Sequence durationInFrames={SCENES.intro}>
        <Intro />
      </TransitionSeries.Sequence>
      <TransitionSeries.Sequence durationInFrames={SCENES.title}>
        <Title />
      </TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={24}>
        <LightLeak seed={3} />
      </TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={SCENES.grab}>
        <Grab />
      </TransitionSeries.Sequence>
      <TransitionSeries.Transition presentation={slide({ direction: "from-right" })} timing={linearTiming({ durationInFrames: T })} />
      <TransitionSeries.Sequence durationInFrames={SCENES.dodge}>
        <Dodge />
      </TransitionSeries.Sequence>
      <TransitionSeries.Transition presentation={wipe({ direction: "from-left" })} timing={linearTiming({ durationInFrames: T })} />
      <TransitionSeries.Sequence durationInFrames={SCENES.escape}>
        <Escape />
      </TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={24}>
        <LightLeak seed={7} hueShift={180} />
      </TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={SCENES.daynight}>
        <DayNight />
      </TransitionSeries.Sequence>
      <TransitionSeries.Transition presentation={fade()} timing={linearTiming({ durationInFrames: T })} />
      <TransitionSeries.Sequence durationInFrames={SCENES.levels}>
        <Levels />
      </TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={20}>
        <LightLeak seed={11} hueShift={260} />
      </TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={SCENES.power}>
        <PowerUps />
      </TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={20}>
        <LightLeak seed={5} />
      </TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={SCENES.style}>
        <Style />
      </TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={30}>
        <LightLeak seed={9} />
      </TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={SCENES.finale}>
        <Finale />
      </TransitionSeries.Sequence>
    </TransitionSeries>
    <Grain />
    <Music />
  </AbsoluteFill>
);
