using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    /// <summary>Procedural sound synthesis: renders every game sound into AudioClips at runtime.</summary>
    public static class Synth
    {
        public const int SR = 22050;
        const double TwoPi = Math.PI * 2.0;

        enum W { Sine, Square, SoftSq, Tri, Saw }

        // ------------------------------------------------------------------ public

        public static Dictionary<string, AudioClip> BuildAll()
        {
            noiseState = 0x9E3779B9u;
            var d = new Dictionary<string, AudioClip>();
            d["coin"] = Make("coin", Coin(), 0.9f);
            d["step"] = Make("step", Step(), 0.6f);
            d["alert"] = Make("alert", Alert(), 0.85f);
            d["hit"] = Make("hit", Hit(), 0.9f);
            d["win"] = Make("win", Win(), 0.9f);
            d["lose"] = Make("lose", Lose(), 0.9f);
            d["power"] = Make("power", Power(), 0.85f);
            d["click"] = Make("click", Click(), 0.6f);
            d["gate"] = Make("gate", Gate(), 0.9f);
            d["star"] = Make("star", Star(), 0.85f);
            d["buy"] = Make("buy", Buy(), 0.9f);
            d["error"] = Make("error", Error(), 0.8f);
            d["count"] = Make("count", Count(), 0.8f);
            d["go"] = Make("go", Go(), 0.9f);
            d["whoosh"] = Make("whoosh", Whoosh(), 0.55f);
            d["music"] = Make("music", Music(), 0.9f);
            return d;
        }

        // ------------------------------------------------------------------ helpers

        static float[] Buf(double seconds) => new float[Math.Max(1, (int)(seconds * SR))];

        static double Hz(double midi) => 440.0 * Math.Pow(2.0, (midi - 69.0) / 12.0);

        static uint noiseState = 0x9E3779B9u;

        static float NoiseSample()
        {
            uint x = noiseState;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            noiseState = x;
            return (float)(x * (1.0 / 4294967295.0) * 2.0 - 1.0);
        }

        static double Osc(W w, double p)
        {
            switch (w)
            {
                case W.Sine: return Math.Sin(TwoPi * p);
                case W.Square: return p < 0.5 ? 0.6 : -0.6;
                case W.SoftSq:
                {
                    double x = TwoPi * p;
                    return (Math.Sin(x) + Math.Sin(3.0 * x) / 3.0 + Math.Sin(5.0 * x) / 5.0) * 0.8;
                }
                case W.Tri: return 4.0 * Math.Abs(p - 0.5) - 1.0;
                case W.Saw: return (2.0 * p - 1.0) * 0.7;
            }
            return 0.0;
        }

        /// <summary>
        /// Renders an oscillator note additively into buffer b.
        /// Envelope: linear attack, exponential decay (rate per second), linear release over the last `release` seconds.
        /// Frequency glides exponentially from f0 to f1 over `glide` seconds (default whole note).
        /// </summary>
        static void Tone(float[] b, double start, double dur, double f0, double f1, float vol, W w,
            double attack = 0.005, double decay = 0.0, double release = 0.02,
            double vibDepth = 0.0, double vibRate = 0.0, double glide = -1.0, bool wrap = false)
        {
            int len = b.Length;
            int s0 = (int)(start * SR);
            int n = (int)(dur * SR);
            if (s0 < 0) s0 = 0;
            if (!wrap)
            {
                if (s0 >= len) return;
                if (s0 + n > len) n = len - s0;
            }
            else if (n > len) n = len;

            double phase = 0.0;
            double gt = glide > 0.0 ? glide : dur;
            bool gliding = Math.Abs(f1 - f0) > 1e-6;
            double logRatio = gliding ? Math.Log(f1 / f0) : 0.0;
            double decMul = decay > 0.0 ? Math.Exp(-decay / SR) : 1.0;
            double decEnv = 1.0;
            double invSR = 1.0 / SR;
            if (attack < 1e-4) attack = 1e-4;
            if (release < 1e-4) release = 1e-4;

            for (int i = 0; i < n; i++)
            {
                double t = i * invSR;
                double f = f0;
                if (gliding)
                {
                    double k = t < gt ? t / gt : 1.0;
                    f = f0 * Math.Exp(logRatio * k);
                }
                if (vibDepth > 0.0) f *= 1.0 + vibDepth * Math.Sin(TwoPi * vibRate * t);

                double env;
                if (t < attack) env = t / attack;
                else { env = decEnv; decEnv *= decMul; }
                double rem = dur - t;
                if (rem < release) env *= rem / release;

                int idx = s0 + i;
                if (idx >= len) { if (!wrap) break; idx -= len; }
                b[idx] += (float)(vol * env * Osc(w, phase));

                phase += f * invSR;
                if (phase >= 1.0) phase -= Math.Floor(phase);
            }
        }

        /// <summary>
        /// Filtered noise burst. Low-pass cutoff sweeps linearly lp0 -> lp1, optional one-pole high-pass at hp Hz.
        /// </summary>
        static void Noise(float[] b, double start, double dur, float vol, double lp0, double lp1, double hp,
            double attack = 0.002, double decay = 0.0, double release = 0.01, bool wrap = false)
        {
            int len = b.Length;
            int s0 = (int)(start * SR);
            int n = (int)(dur * SR);
            if (s0 < 0) s0 = 0;
            if (!wrap)
            {
                if (s0 >= len) return;
                if (s0 + n > len) n = len - s0;
            }
            else if (n > len) n = len;

            double lpY = 0.0, hpY = 0.0;
            double wHp = TwoPi * hp / SR;
            double aHp = hp > 0.0 ? wHp / (1.0 + wHp) : 0.0;
            double decMul = decay > 0.0 ? Math.Exp(-decay / SR) : 1.0;
            double decEnv = 1.0;
            double invSR = 1.0 / SR;
            if (attack < 1e-4) attack = 1e-4;
            if (release < 1e-4) release = 1e-4;

            for (int i = 0; i < n; i++)
            {
                double t = i * invSR;
                double k = n > 1 ? (double)i / (n - 1) : 0.0;
                double fc = lp0 + (lp1 - lp0) * k;
                double wl = TwoPi * fc / SR;
                double aLp = wl / (1.0 + wl);

                double x = NoiseSample();
                lpY += aLp * (x - lpY);
                double y = lpY;
                if (hp > 0.0)
                {
                    hpY += aHp * (y - hpY);
                    y -= hpY;
                }

                double env;
                if (t < attack) env = t / attack;
                else { env = decEnv; decEnv *= decMul; }
                double rem = dur - t;
                if (rem < release) env *= rem / release;

                int idx = s0 + i;
                if (idx >= len) { if (!wrap) break; idx -= len; }
                b[idx] += (float)(vol * env * y);
            }
        }

        static void Normalize(float[] b, float peak)
        {
            float max = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float a = b[i] < 0f ? -b[i] : b[i];
                if (a > max) max = a;
            }
            if (max < 1e-6f) return;
            float g = peak / max;
            for (int i = 0; i < b.Length; i++) b[i] *= g;
        }

        static AudioClip Make(string name, float[] b, float peak)
        {
            Normalize(b, peak);
            var clip = AudioClip.Create("gr_" + name, b.Length, 1, SR, false);
            clip.SetData(b, 0);
            return clip;
        }

        // ------------------------------------------------------------------ sfx

        static float[] Coin()
        {
            var b = Buf(0.36);
            double b5 = Hz(83), e6 = Hz(88);
            Tone(b, 0.0, 0.085, b5, b5, 0.5f, W.Sine, 0.002, 6, 0.01);
            Tone(b, 0.0, 0.085, b5, b5, 0.15f, W.Tri, 0.002, 6, 0.01);
            Tone(b, 0.075, 0.28, e6, e6, 0.55f, W.Sine, 0.002, 9, 0.05);
            Tone(b, 0.075, 0.28, e6, e6, 0.15f, W.Tri, 0.002, 11, 0.05);
            Tone(b, 0.075, 0.28, e6, e6, 0.06f, W.SoftSq, 0.002, 14, 0.05);
            Tone(b, 0.075, 0.2, e6 * 2.0, e6 * 2.0, 0.07f, W.Sine, 0.002, 16, 0.05);
            return b;
        }

        static float[] Step()
        {
            var b = Buf(0.07);
            Noise(b, 0.0, 0.07, 1.0f, 900, 250, 50, 0.003, 45, 0.015);
            Tone(b, 0.0, 0.07, 120, 60, 0.35f, W.Sine, 0.002, 50, 0.015);
            return b;
        }

        static float[] Alert()
        {
            var b = Buf(0.26);
            Tone(b, 0.0, 0.1, 660, 880, 0.45f, W.SoftSq, 0.003, 8, 0.02, 0, 0, 0.06);
            Tone(b, 0.0, 0.1, 660, 880, 0.25f, W.Sine, 0.003, 8, 0.02, 0, 0, 0.06);
            Tone(b, 0.11, 0.15, 880, 1320, 0.45f, W.SoftSq, 0.003, 8, 0.04, 0, 0, 0.06);
            Tone(b, 0.11, 0.15, 880, 1320, 0.25f, W.Sine, 0.003, 8, 0.04, 0, 0, 0.06);
            return b;
        }

        static float[] Hit()
        {
            var b = Buf(0.36);
            Noise(b, 0.0, 0.25, 0.9f, 3500, 300, 40, 0.001, 14, 0.05);
            Tone(b, 0.0, 0.35, 300, 70, 0.55f, W.SoftSq, 0.002, 7, 0.06);
            Tone(b, 0.0, 0.35, 300, 70, 0.45f, W.Sine, 0.002, 6, 0.06);
            return b;
        }

        static float[] Win()
        {
            var b = Buf(1.22);
            int[] arp = { 72, 76, 79, 84 };
            for (int i = 0; i < arp.Length; i++)
            {
                double f = Hz(arp[i]);
                double st = i * 0.11;
                Tone(b, st, 0.28, f, f, 0.35f, W.Tri, 0.004, 7, 0.06);
                Tone(b, st, 0.28, f, f, 0.25f, W.Sine, 0.004, 6, 0.06);
            }
            int[] chord = { 72, 76, 79, 84 };
            for (int i = 0; i < chord.Length; i++)
            {
                double f = Hz(chord[i]);
                Tone(b, 0.46, 0.75, f, f, 0.22f, W.Tri, 0.01, 2.5, 0.3, 0.004, 5.5);
                Tone(b, 0.46, 0.75, f, f, 0.15f, W.Sine, 0.01, 2.2, 0.3, 0.004, 5.5);
            }
            Tone(b, 0.46, 0.5, Hz(96), Hz(96), 0.05f, W.Sine, 0.005, 6, 0.2);
            return b;
        }

        static float[] Lose()
        {
            var b = Buf(1.32);
            int[] notes = { 67, 66, 65, 64 };
            for (int i = 0; i < 3; i++)
            {
                double f = Hz(notes[i]);
                double st = i * 0.26;
                Tone(b, st, 0.25, f, f, 0.4f, W.Tri, 0.01, 3, 0.05);
                Tone(b, st, 0.25, f, f, 0.18f, W.SoftSq, 0.01, 4, 0.05);
            }
            double e4 = Hz(64);
            Tone(b, 0.78, 0.54, e4, e4 * 0.97, 0.42f, W.Tri, 0.015, 1.8, 0.2, 0.018, 5.5);
            Tone(b, 0.78, 0.54, e4, e4 * 0.97, 0.2f, W.SoftSq, 0.015, 2.2, 0.2, 0.018, 5.5);
            return b;
        }

        static float[] Power()
        {
            var b = Buf(0.46);
            Tone(b, 0.0, 0.45, 300, 1200, 0.45f, W.Tri, 0.01, 2, 0.1, 0.03, 14);
            Tone(b, 0.0, 0.45, 600, 2400, 0.15f, W.Sine, 0.01, 3, 0.1, 0.03, 14);
            Tone(b, 0.0, 0.45, 300, 1200, 0.12f, W.SoftSq, 0.01, 3, 0.1, 0.03, 14);
            return b;
        }

        static float[] Click()
        {
            var b = Buf(0.05);
            Tone(b, 0.0, 0.05, 1300, 900, 0.6f, W.Sine, 0.001, 50, 0.01);
            Tone(b, 0.0, 0.05, 1300, 900, 0.15f, W.Tri, 0.001, 60, 0.01);
            return b;
        }

        static float[] Gate()
        {
            var b = Buf(1.0);
            Noise(b, 0.0, 0.85, 1.4f, 260, 160, 25, 0.12, 1.5, 0.2);
            Tone(b, 0.0, 0.85, 58, 46, 0.35f, W.Sine, 0.08, 1.2, 0.2, 0.06, 9);
            Tone(b, 0.0, 0.85, 58, 46, 0.12f, W.Tri, 0.08, 1.5, 0.2, 0.06, 9);
            int[] chime = { 81, 85, 88 };
            for (int i = 0; i < chime.Length; i++)
            {
                double f = Hz(chime[i]);
                double st = 0.58 + i * 0.09;
                double dur = 1.0 - st;
                Tone(b, st, dur, f, f, 0.22f, W.Sine, 0.003, 6, 0.08);
                Tone(b, st, dur, f, f, 0.07f, W.Tri, 0.003, 8, 0.08);
            }
            return b;
        }

        static float[] Star()
        {
            var b = Buf(0.36);
            double a6 = Hz(93), e7 = Hz(100);
            Tone(b, 0.0, 0.35, a6, a6, 0.5f, W.Sine, 0.002, 9, 0.06, 0.006, 9);
            Tone(b, 0.0, 0.35, a6, a6, 0.1f, W.Tri, 0.002, 12, 0.06);
            Tone(b, 0.04, 0.31, e7, e7, 0.22f, W.Sine, 0.002, 12, 0.06, 0.008, 11);
            Tone(b, 0.08, 0.2, a6 * 2.0, a6 * 2.0, 0.08f, W.Sine, 0.002, 16, 0.05);
            Noise(b, 0.0, 0.3, 0.12f, 9000, 9000, 6000, 0.002, 14, 0.05);
            return b;
        }

        static float[] Buy()
        {
            var b = Buf(0.5);
            double b5 = Hz(83), e6 = Hz(88), b6 = Hz(95);
            Tone(b, 0.0, 0.05, b5, b5, 0.4f, W.Sine, 0.001, 10, 0.008);
            Tone(b, 0.045, 0.14, e6, e6, 0.45f, W.Sine, 0.001, 14, 0.03);
            Tone(b, 0.045, 0.14, e6, e6, 0.12f, W.Tri, 0.001, 16, 0.03);
            Tone(b, 0.12, 0.05, e6, e6, 0.4f, W.Sine, 0.001, 10, 0.008);
            Tone(b, 0.165, 0.33, b6, b6, 0.45f, W.Sine, 0.001, 8, 0.08);
            Tone(b, 0.165, 0.33, b6, b6, 0.1f, W.Tri, 0.001, 10, 0.08);
            Noise(b, 0.1, 0.4, 0.25f, 10000, 10000, 5000, 0.01, 7, 0.1);
            // sparkles
            double[] sp = { 2900, 3500, 3100, 4200, 3800 };
            for (int i = 0; i < sp.Length; i++)
                Tone(b, 0.18 + i * 0.05, 0.08, sp[i], sp[i], 0.08f, W.Sine, 0.001, 30, 0.02);
            return b;
        }

        static float[] Error()
        {
            var b = Buf(0.26);
            Tone(b, 0.0, 0.1, 150, 140, 0.5f, W.SoftSq, 0.004, 6, 0.02);
            Tone(b, 0.0, 0.1, 150, 140, 0.3f, W.Tri, 0.004, 6, 0.02);
            Tone(b, 0.13, 0.12, 132, 118, 0.5f, W.SoftSq, 0.004, 6, 0.03);
            Tone(b, 0.13, 0.12, 132, 118, 0.3f, W.Tri, 0.004, 6, 0.03);
            return b;
        }

        static float[] Count()
        {
            var b = Buf(0.15);
            double a5 = Hz(81);
            Tone(b, 0.0, 0.15, a5, a5, 0.55f, W.Sine, 0.002, 10, 0.03);
            Tone(b, 0.0, 0.15, a5, a5, 0.18f, W.Tri, 0.002, 12, 0.03);
            return b;
        }

        static float[] Go()
        {
            var b = Buf(0.4);
            double a6 = Hz(93), a5 = Hz(81);
            Tone(b, 0.0, 0.4, a6, a6, 0.5f, W.Sine, 0.002, 4.5, 0.08);
            Tone(b, 0.0, 0.4, a6, a6, 0.15f, W.Tri, 0.002, 5.5, 0.08);
            Tone(b, 0.0, 0.4, a5, a5, 0.25f, W.Sine, 0.002, 4.5, 0.08);
            return b;
        }

        static float[] Whoosh()
        {
            var b = Buf(0.35);
            Noise(b, 0.0, 0.35, 1.0f, 500, 2800, 180, 0.15, 0, 0.19);
            return b;
        }

        // ------------------------------------------------------------------ music

        static float[] Music()
        {
            const double bpm = 112.0;
            double beat = 60.0 / bpm;
            const int bars = 8;
            int total = (int)Math.Round(SR * beat * 4 * bars);
            var b = new float[total];
            double loopLen = (double)total / SR;

            // Am Am F G Am Am F E
            int[] roots = { 45, 45, 41, 43, 45, 45, 41, 40 };
            int[][] triads =
            {
                new[] { 57, 60, 64 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
                new[] { 57, 60, 64 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 52, 56, 59 },
            };
            // chord tones within A-minor pentatonic (pitch classes)
            int[][] chordPcs =
            {
                new[] { 9, 0, 4 }, new[] { 9, 0, 4 }, new[] { 9, 0 }, new[] { 7, 2 },
                new[] { 9, 0, 4 }, new[] { 9, 0, 4 }, new[] { 9, 0 }, new[] { 4 },
            };

            for (int bar = 0; bar < bars; bar++)
            {
                double bs = bar * 4 * beat;
                double root = Hz(roots[bar]);
                double fifth = Hz(roots[bar] + 7);

                // boom (beats 1 & 3)
                Tone(b, bs, beat * 0.9, root, root, 0.34f, W.Tri, 0.005, 3.5, 0.06, 0, 0, -1, true);
                Tone(b, bs, beat * 0.9, root, root, 0.22f, W.Sine, 0.005, 3.0, 0.06, 0, 0, -1, true);
                Tone(b, bs + 2 * beat, beat * 0.9, fifth, fifth, 0.30f, W.Tri, 0.005, 3.5, 0.06, 0, 0, -1, true);
                Tone(b, bs + 2 * beat, beat * 0.9, fifth, fifth, 0.2f, W.Sine, 0.005, 3.0, 0.06, 0, 0, -1, true);

                // chick (beats 2 & 4): muted chord stab + soft noise
                for (int c = 1; c <= 3; c += 2)
                {
                    double st = bs + c * beat;
                    int[] tri = triads[bar];
                    for (int k = 0; k < tri.Length; k++)
                    {
                        double f = Hz(tri[k]);
                        Tone(b, st, 0.13, f, f, 0.055f, W.Tri, 0.003, 22, 0.03, 0, 0, -1, true);
                    }
                    Noise(b, st, 0.06, 0.12f, 6000, 4000, 1500, 0.001, 50, 0.02, true);
                }

                // hi-hat on off-beats
                for (int q = 0; q < 4; q++)
                {
                    double st = bs + (q + 0.5) * beat;
                    Noise(b, st, 0.045, 0.09f, 10000, 10000, 6000, 0.001, 70, 0.01, true);
                }
            }

            // ---- melody: A-minor pentatonic, deterministic
            int[] scale = { 64, 67, 69, 72, 74, 76, 79, 81, 84 }; // E4 G4 A4 C5 D5 E5 G5 A5 C6
            var rnd = new System.Random(1849);
            const int slotsPerBar = 8;
            int nSlots = bars * slotsPerBar;
            int[] mel = new int[nSlots];      // scale index, -1 rest, -2 hold
            int cur = 2; // A4
            for (int s = 0; s < nSlots; s++)
            {
                int bar = s / slotsPerBar, pos = s % slotsPerBar;
                bool strong = pos == 0 || pos == 4;

                // last bar: land on E (dominant), hold, then rest to leave room for clean loop
                if (bar == bars - 1)
                {
                    if (pos == 0) { cur = NearestChordTone(scale, cur, chordPcs[bar]); mel[s] = cur; }
                    else if (pos < 4) mel[s] = -2;
                    else mel[s] = -1;
                    continue;
                }

                if (strong)
                {
                    if (pos == 4 && rnd.NextDouble() < 0.15) { mel[s] = -2; continue; }
                    cur = NearestChordTone(scale, cur + (rnd.NextDouble() < 0.5 ? -1 : 1), chordPcs[bar]);
                    mel[s] = cur;
                    continue;
                }

                double r = rnd.NextDouble();
                if (r < 0.2) { mel[s] = -1; continue; }
                if (r < 0.38 && s > 0 && mel[s - 1] >= 0) { mel[s] = -2; continue; }

                double m = rnd.NextDouble();
                int step;
                if (m < 0.75) step = rnd.NextDouble() < 0.5 ? -1 : 1;
                else if (m < 0.88) step = 0;
                else step = rnd.NextDouble() < 0.5 ? -2 : 2;
                cur += step;
                if (cur < 1) cur = 2;
                if (cur > scale.Length - 2) cur = scale.Length - 3;
                mel[s] = cur;
            }

            double slot = beat * 0.5;
            for (int s = 0; s < nSlots; s++)
            {
                if (mel[s] < 0) continue;
                int len = 1;
                while (s + len < nSlots && mel[s + len] == -2) len++;
                double st = s * slot;
                double dur = len * slot * 0.95;
                if (st + dur > loopLen - 0.01) dur = loopLen - 0.01 - st;
                if (dur <= 0.02) continue;
                double f = Hz(scale[mel[s]]);
                bool accent = (s % slotsPerBar) == 0;
                float v = accent ? 0.2f : 0.16f;
                Tone(b, st, dur, f, f, v, W.Tri, 0.012, 2.2, Math.Min(0.08, dur * 0.4), len > 1 ? 0.006 : 0.0, 5.0);
                Tone(b, st, dur, f, f, v * 0.45f, W.Sine, 0.012, 2.6, Math.Min(0.08, dur * 0.4), len > 1 ? 0.006 : 0.0, 5.0);
                Tone(b, st, dur, f * 2.0, f * 2.0, v * 0.08f, W.Sine, 0.012, 5.0, Math.Min(0.08, dur * 0.4));
            }

            return b;
        }

        static int NearestChordTone(int[] scale, int from, int[] pcs)
        {
            if (from < 0) from = 0;
            if (from > scale.Length - 1) from = scale.Length - 1;
            int best = from, bestDist = int.MaxValue;
            for (int i = 0; i < scale.Length; i++)
            {
                int pc = scale[i] % 12;
                bool ok = false;
                for (int k = 0; k < pcs.Length; k++) if (pcs[k] == pc) { ok = true; break; }
                if (!ok) continue;
                int dist = Math.Abs(i - from);
                // prefer the middle of the range slightly
                if (i == 0 || i == scale.Length - 1) dist += 1;
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            return best;
        }
    }
}
