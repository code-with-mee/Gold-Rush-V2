import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, interpolate, staticFile, useVideoConfig } from "remotion";
import { linearTiming, TransitionSeries } from "@remotion/transitions";
import { fade } from "@remotion/transitions/fade";
import { slide } from "@remotion/transitions/slide";
import { wipe } from "@remotion/transitions/wipe";
import { LightLeak } from "@remotion/light-leaks";
import { Grain } from "./fx";
import * as S from "./scenes";

const T = 10;
const TRANSITIONS = 4;

export const TRAILER_FRAMES =
  S.INTRO + S.TITLE + S.COUNT + S.GRAB + S.DODGE + S.POWER + S.DAYNIGHT + S.ESCAPE + S.LEVELS + S.SHOP + S.FINALE -
  TRANSITIONS * T;

const Music: React.FC = () => {
  const { durationInFrames } = useVideoConfig();
  return (
    <Audio
      src={staticFile("sfx/music.wav")}
      loop
      volume={(f) =>
        interpolate(f, [0, 20, durationInFrames - 50, durationInFrames], [0, 0.5, 0.5, 0], {
          extrapolateLeft: "clamp",
          extrapolateRight: "clamp",
        })
      }
    />
  );
};

const tr = (p: Parameters<typeof TransitionSeries.Transition>[0]["presentation"]) => (
  <TransitionSeries.Transition presentation={p} timing={linearTiming({ durationInFrames: T })} />
);

export const Trailer: React.FC = () => (
  <AbsoluteFill style={{ background: "black" }}>
    <TransitionSeries>
      <TransitionSeries.Sequence durationInFrames={S.INTRO}><S.Intro /></TransitionSeries.Sequence>
      <TransitionSeries.Sequence durationInFrames={S.TITLE}><S.Title /></TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={24}><LightLeak seed={3} /></TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={S.COUNT}><S.Countdown /></TransitionSeries.Sequence>
      <TransitionSeries.Sequence durationInFrames={S.GRAB}><S.Grab /></TransitionSeries.Sequence>
      {tr(slide({ direction: "from-right" }))}
      <TransitionSeries.Sequence durationInFrames={S.DODGE}><S.Dodge /></TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={20}><LightLeak seed={11} hueShift={260} /></TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={S.POWER}><S.PowerUps /></TransitionSeries.Sequence>
      {tr(wipe({ direction: "from-left" }))}
      <TransitionSeries.Sequence durationInFrames={S.DAYNIGHT}><S.DayNight /></TransitionSeries.Sequence>
      {tr(fade())}
      <TransitionSeries.Sequence durationInFrames={S.ESCAPE}><S.Escape /></TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={24}><LightLeak seed={7} hueShift={180} /></TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={S.LEVELS}><S.Levels /></TransitionSeries.Sequence>
      {tr(slide({ direction: "from-bottom" }))}
      <TransitionSeries.Sequence durationInFrames={S.SHOP}><S.Shop /></TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={30}><LightLeak seed={9} /></TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={S.FINALE}><S.Finale /></TransitionSeries.Sequence>
    </TransitionSeries>
    <Grain opacity={0.05} />
    <Music />
  </AbsoluteFill>
);
