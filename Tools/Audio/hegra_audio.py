"""
Secret of Hegra — procedural audio generator.

Synthesizes every new sound of the game (no recordings, no downloads) and writes 16-bit WAV files into
Assets/SecretOfHegra/Audio, where Unity imports them:

  ambience (seamless loops): amb_wind (night wind with gusts), amb_crickets, amb_fire (crackle + roar),
                             amb_cloth (tent cloth flapping)
  music (seamless loops):    mus_hijaz  — maqam Hijaz on D: plucked oud (Karplus-Strong, double course),
                                          low drone, light frame drum (maqsum)
                             mus_legend — slower, darker variation for when a legend is told
  stingers:                  sting_legend (mysterious rise), sting_reveal (dramatic hit)
  effects:                   sfx_footstep (sand), sfx_click (select), ui_open, ui_confirm

Run:  python Tools/Audio/hegra_audio.py      (numpy + scipy). Output is deterministic (fixed seeds).
"""
import os
import wave

import numpy as np
from scipy import signal

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "SecretOfHegra", "Audio"))
SR_AMB = 22050
SR_MUS = 32000

# maqam Hijaz on D: D Eb F# G A Bb C D (semitones from the tonic)
HIJAZ = [0, 1, 4, 5, 7, 8, 10, 12]
D3 = 146.83


def rng(seed):
    return np.random.default_rng(seed)


# ───────────────────────── helpers ─────────────────────────

def write(name, data, sr, peak=0.89):
    data = np.asarray(data, dtype=np.float64)
    m = np.max(np.abs(data))
    if m > 0:
        data = data / m * peak
    pcm = np.clip(data * 32767, -32768, 32767).astype("<i2")
    ch = 1 if data.ndim == 1 else data.shape[1]
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(ch)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())
    print(f"  {name}.wav  {len(data) / sr:5.1f}s  {'stereo' if ch == 2 else 'mono'}")


def butter(x, kind, f, sr, order=2):
    nyq = sr / 2
    f = np.clip(np.asarray(f, dtype=float) / nyq, 1e-4, 0.999)
    b, a = signal.butter(order, f, btype=kind)
    return signal.lfilter(b, a, x, axis=0)


def butter_loop(x, kind, f, sr, order=2):
    """Filters a loop circularly (no start-up transient, so the end flows into the start)."""
    y = butter(np.concatenate([x, x]), kind, f, sr, order)
    return y[len(x):]


def seamless(x, sr, fade=1.0):
    """Crossfades the end into the start so the file loops without a click (length shrinks by `fade`)."""
    n = int(fade * sr)
    head, body, tail = x[:n], x[n:-n] if n else x, x[-n:]
    ramp = np.linspace(0, 1, n)
    if x.ndim == 2:
        ramp = ramp[:, None]
    mixed = tail * (1 - ramp) + head * ramp
    return np.concatenate([body, mixed]) if n else x


def wrap_tail(buf, length):
    """Folds whatever rings past `length` back onto the start (for loops rendered with long tails)."""
    out = buf[:length].copy()
    extra = buf[length:]
    while len(extra):
        k = min(len(extra), length)
        out[:k] += extra[:k]
        extra = extra[k:]
    return out


def reverb(x, sr, t60=2.2, mix=0.3, seed=1, loop=False, bright=5000):
    """Convolution with a synthetic, slightly diffuse exponential-noise room (stereo when the input is stereo)."""
    n = int(t60 * sr)
    t = np.arange(n) / sr
    r = rng(seed)
    chans = 1 if x.ndim == 1 else x.shape[1]
    out = np.zeros_like(x)
    for c in range(chans):
        ir = r.standard_normal(n) * np.exp(-6.9 * t / t60)
        ir = butter(ir, "low", bright, sr)
        ir[: int(0.012 * sr)] *= np.linspace(0, 1, int(0.012 * sr))   # pre-delay feel
        ir /= np.sqrt(np.sum(ir ** 2))
        src = x if chans == 1 else x[:, c]
        if loop:
            wet = signal.fftconvolve(np.concatenate([src, src]), ir)[len(src): 2 * len(src)]
        else:
            wet = signal.fftconvolve(src, ir)[: len(src)]
        if chans == 1:
            out = src * (1 - mix) + wet * mix * 0.6
        else:
            out[:, c] = src * (1 - mix) + wet * mix * 0.6
    return out


def stereo(mono, pan=0.0, haas_ms=0.0, sr=SR_MUS):
    """Equal-power pan (-1 left … 1 right) with an optional tiny inter-channel delay for width."""
    a = (pan + 1) * np.pi / 4
    l, r = mono * np.cos(a), mono * np.sin(a)
    d = int(abs(haas_ms) * sr / 1000)
    if d:   # circular shift: keeps loops seamless
        if haas_ms > 0:
            r = np.roll(r, d)
        else:
            l = np.roll(l, d)
    return np.stack([l, r], axis=1)


# ───────────────────────── instruments ─────────────────────────

def ks_pluck(freq, dur, sr, seed, bright=0.6, decay=0.996, pluck_pos=0.13):
    """Karplus-Strong string: noise burst through a delay line with a gentle low-pass in the loop."""
    r = rng(seed)
    n = max(2, int(round(sr / freq)))
    total = int(dur * sr)
    buf = np.zeros(total + n + 1)
    exc = r.uniform(-1, 1, n)
    exc = bright * exc + (1 - bright) * np.convolve(exc, np.ones(4) / 4, mode="same")   # plectrum hardness
    exc = exc - np.roll(exc, max(1, int(n * pluck_pos)))                                # plucking position comb
    buf[1:n + 1] = exc
    s = n + 1
    while s < total + n + 1:
        prev = buf[s - n:s]
        before = np.concatenate(([buf[s - n - 1]], prev[:-1]))
        end = min(s + n, len(buf))
        buf[s:end] = (decay * 0.5 * (prev + before))[: end - s]
        s += n
    return buf[1:total + 1]


def oud(freq, dur, sr, seed, vel=1.0):
    """Two slightly detuned courses + a woody body resonance: a plucked oud-like tone."""
    a = ks_pluck(freq, dur, sr, seed, bright=0.55, decay=0.9965)
    b = ks_pluck(freq * 2 ** (3.5 / 1200), dur, sr, seed + 999, bright=0.5, decay=0.996)
    x = 0.55 * a + 0.45 * b
    body = butter(x, "band", [110, 420], sr) * 0.6 + butter(x, "band", [900, 2400], sr) * 0.25
    x = x * 0.7 + body
    attack = int(0.003 * sr)
    x[:attack] *= np.linspace(0, 1, attack)
    rel = int(0.08 * sr)
    x[-rel:] *= np.linspace(1, 0, rel)
    return x * vel


def frame_drum(kind, sr, seed, vel=1.0):
    """Daf/riq strokes: 'dum' (low centre) and 'tak' (rim + small jingles)."""
    r = rng(seed)
    if kind == "dum":
        n = int(0.55 * sr)
        t = np.arange(n) / sr
        f = 62 + 38 * np.exp(-t * 30)
        tone = np.sin(2 * np.pi * np.cumsum(f) / sr) * np.exp(-t * 7.5)
        skin = butter(r.standard_normal(n), "low", 900, sr) * np.exp(-t * 40) * 0.35
        return (tone + skin) * vel
    n = int(0.22 * sr)
    t = np.arange(n) / sr
    rim = butter(r.standard_normal(n), "band", [1800, 5200], sr) * np.exp(-t * 70)
    jingle = butter(r.standard_normal(n), "band", [6000, 9500], sr) * np.exp(-t * 22) * 0.35
    return (rim + jingle) * 0.55 * vel


def place(buf, x, start):
    end = min(len(buf), start + len(x))
    if end > start:
        buf[start:end] += x[: end - start]


# ───────────────────────── music ─────────────────────────

def hijaz_note(deg, octave=0):
    octs, d = divmod(deg, 7)
    return D3 * 2 ** ((HIJAZ[d] + 12 * (octs + octave)) / 12)


def render_melody(events, bpm, sr, seed, total_beats, octave=0, vel=0.9):
    """events: (degree or None, beats, tremolo). Long notes may be played as a tremolo (riša), like an oud."""
    beat = 60 / bpm
    length = int(total_beats * beat * sr)
    buf = np.zeros(length + int(4 * sr))
    pos, k = 0.0, 0
    for deg, beats, trem in events:
        if deg is not None:
            f = hijaz_note(deg, octave)
            start = int(pos * beat * sr)
            if trem:
                step = beat / 4
                count = int(beats * 4)
                for i in range(count):
                    v = vel * (0.55 + 0.25 * (i % 2 == 0)) * (1 - 0.3 * i / count)
                    place(buf, oud(f, step * 2.2, sr, seed + k * 31 + i, v), start + int(i * step * sr))
            else:
                place(buf, oud(f, max(0.35, beats * beat * 1.6), sr, seed + k * 31, vel), start)
        pos += beats
        k += 1
    return wrap_tail(buf, length)


def drone(sr, length, notes, lfo_cycles=2, level=1.0, seed=3):
    t = np.arange(length) / sr
    out = np.zeros(length)
    loop_s = length / sr
    for f, amp in notes:
        f = round(f * loop_s) / loop_s   # whole cycles per loop: no click at the loop point
        f2 = round(f * 1.003 * loop_s) / loop_s
        saw = signal.sawtooth(2 * np.pi * f * t) + signal.sawtooth(2 * np.pi * f2 * t)
        out += saw * amp
    out = butter_loop(out, "low", 380, sr)
    lfo = 0.8 + 0.2 * np.sin(2 * np.pi * lfo_cycles * t / (length / sr))   # whole cycles: loops seamlessly
    return out * lfo * level


def mus_hijaz():
    sr, bpm = SR_MUS, 84
    # (degree, beats, tremolo) — degree 0 = D3, 7 = D4
    A = [(4, 1, 0), (3, 0.5, 0), (2, 0.5, 0), (1, 1, 0), (2, 1, 0), (0, 4, 1)]
    B = [(0, 0.5, 0), (1, 0.5, 0), (2, 1, 0), (3, 1, 0), (4, 1, 0), (5, 1, 0), (4, 1, 0), (3, 2, 1)]
    C = [(4, 0.5, 0), (5, 0.5, 0), (6, 1, 0), (7, 2, 1), (6, 1, 0), (5, 1, 0), (4, 2, 0)]
    D = [(3, 1, 0), (2, 0.5, 0), (1, 0.5, 0), (2, 1, 0), (1, 1, 0), (0, 4, 1)]
    E = [(7, 1, 0), (6, 0.5, 0), (5, 0.5, 0), (4, 1, 0), (5, 1, 0), (4, 1, 0), (3, 1, 0), (2, 2, 1)]
    rest = [(None, 8, 0)]
    events = rest + A + B + A + D + C + E + B + D
    total_beats = sum(e[1] for e in events)
    length = int(total_beats * 60 / bpm * sr)

    melody = render_melody(events, bpm, sr, 100, total_beats)
    # a quiet answering voice an octave down at phrase ends
    answer = render_melody([(None, 12, 0), (0, 4, 0), (None, 12, 0), (4, 4, 0), (None, 12, 0), (0, 4, 0),
                            (None, 12, 0), (2, 4, 0), (None, total_beats - 64, 0)], bpm, sr, 400, total_beats, -1, 0.5)

    dr = drone(sr, length, [(73.42, 0.5), (110.0, 0.25), (146.83, 0.12)], lfo_cycles=3)

    # maqsum (Dum Tak _ Tak Dum _ Tak _) in eighths, from bar 3, softly
    perc = np.zeros(length + sr)
    eighth = 60 / bpm / 2
    pattern = ["dum", "tak", None, "tak", "dum", None, "tak", None]
    bars = int(total_beats // 4)
    for bar in range(2, bars):
        for i, p in enumerate(pattern):
            if p:
                v = (0.8 if p == "dum" else 0.5) * (0.85 + 0.15 * ((bar + i) % 3 == 0))
                place(perc, frame_drum(p, sr, bar * 8 + i, v), int((bar * 8 + i) * eighth * sr))
    perc = wrap_tail(perc, length)

    mix = (stereo(melody, -0.15, 6) * 0.62 + stereo(answer, 0.25, -5) * 0.32 +
           stereo(dr, 0.0) * 0.32 + stereo(perc, 0.2, 3) * 0.3)
    mix = reverb(mix, sr, t60=2.4, mix=0.32, seed=11, loop=True)
    write("mus_hijaz", mix, sr, 0.85)


def mus_legend():
    sr, bpm = SR_MUS, 58
    events = [(None, 4, 0), (0, 4, 1), (1, 2, 0), (0, 2, 0), (None, 2, 0), (2, 2, 0), (3, 4, 1), (2, 2, 0),
              (1, 2, 0), (0, 6, 1), (None, 2, 0), (4, 2, 0), (5, 4, 1), (4, 2, 0), (2, 2, 0), (1, 4, 1), (0, 6, 1)]
    total_beats = sum(e[1] for e in events)
    length = int(total_beats * 60 / bpm * sr)
    melody = render_melody(events, bpm, sr, 700, total_beats, octave=-1, vel=0.85)

    t = np.arange(length) / sr
    dr = drone(sr, length, [(73.42, 0.55), (116.54, 0.18), (110.0, 0.2)], lfo_cycles=2, seed=5)
    # breathing pad: band-limited noise swelling twice per loop
    pad = butter(rng(77).standard_normal(length), "band", [180, 900], sr)
    pad *= (0.5 - 0.5 * np.cos(2 * np.pi * 2 * t / (length / sr))) * 0.5

    # a slow heartbeat on the frame drum
    perc = np.zeros(length + sr)
    beat = 60 / bpm
    for b in range(0, int(total_beats), 2):
        place(perc, frame_drum("dum", sr, 900 + b, 0.55), int(b * beat * sr))
        place(perc, frame_drum("dum", sr, 950 + b, 0.3), int((b + 0.35) * beat * sr))
    perc = wrap_tail(perc, length)

    mix = (stereo(melody, -0.1, 7) * 0.6 + stereo(dr, 0) * 0.38 + stereo(pad, 0.3, -9) * 0.22 + stereo(perc, 0) * 0.32)
    mix = reverb(mix, sr, t60=3.6, mix=0.45, seed=12, loop=True, bright=3500)
    write("mus_legend", mix, sr, 0.8)


# ───────────────────────── stingers ─────────────────────────

def sting_legend():
    sr = SR_MUS
    dur = 5.0
    n = int(dur * sr)
    t = np.arange(n) / sr
    env = np.clip(t / 3.2, 0, 1) ** 2 * np.clip((dur - t) / 1.2, 0, 1)
    # rising filtered noise (processed in blocks with a moving band)
    noise = rng(5).standard_normal(n)
    sweep = np.zeros(n)
    blk = 1024
    for i in range(0, n, blk):
        c = 250 * (14 ** (i / n))
        sweep[i:i + blk] = butter(noise[i:i + blk + 256], "band", [c * 0.7, c * 1.4], sr)[:blk][: len(sweep[i:i + blk])]
    # rising Hijaz glissando voices
    f = D3 * 2 ** (t / dur * 19 / 12)
    gl = np.sin(2 * np.pi * np.cumsum(f) / sr) + 0.5 * np.sin(2 * np.pi * np.cumsum(f * 1.5) / sr)
    # shimmering plucks climbing the scale
    bells = np.zeros(n + sr)
    for i, deg in enumerate([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]):
        place(bells, oud(hijaz_note(deg, 1), 1.6, sr, 300 + i, 0.5), int((0.6 + i * 0.32) * sr))
    x = sweep * env * 0.35 + gl * env * 0.18 + bells[:n] * np.clip(t / 1.0, 0, 1) * 0.6
    x = reverb(stereo(x, 0, 9), sr, t60=3.0, mix=0.5, seed=21)
    write("sting_legend", x, sr, 0.8)


def sting_reveal():
    sr = SR_MUS
    dur = 4.0
    n = int(dur * sr)
    t = np.arange(n) / sr
    f = 35 + 70 * np.exp(-t * 6)
    boom = np.sin(2 * np.pi * np.cumsum(f) / sr) * np.exp(-t * 1.6)
    hit = np.zeros(n)
    place(hit, frame_drum("dum", sr, 41, 1.0), 0)
    place(hit, frame_drum("tak", sr, 42, 0.8), int(0.01 * sr))
    metal = sum(np.sin(2 * np.pi * p * t + k) * np.exp(-t * (1.5 + k * 0.4)) / (k + 1)
                for k, p in enumerate([523, 1311, 1873, 2633, 3497]))
    x = boom * 0.9 + hit * 0.7 + metal * 0.18
    x = reverb(stereo(x, 0, 5), sr, t60=3.2, mix=0.4, seed=22)
    write("sting_reveal", x, sr, 0.9)


# ───────────────────────── ambience ─────────────────────────

def amb_wind():
    sr, dur = SR_AMB, 31.0
    n = int(dur * sr)
    t = np.arange(n) / sr
    r = rng(8)
    gust = (0.55 + 0.25 * np.sin(2 * np.pi * t / 13.1) + 0.2 * np.sin(2 * np.pi * t / 5.3 + 1.3)) ** 2
    low = butter(r.standard_normal(n), "low", 260, sr, 2) * 1.2
    mid = np.zeros(n)
    noise = r.standard_normal(n + 2048)
    blk = 2048
    zi = None
    for i in range(0, n, blk):
        c = 380 + 520 * gust[i]
        b, a = signal.butter(2, [c * 0.75 / (sr / 2), c * 1.3 / (sr / 2)], btype="band")
        if zi is None:
            zi = signal.lfilter_zi(b, a) * 0
        seg, zi = signal.lfilter(b, a, noise[i:i + blk], zi=zi)
        mid[i:i + blk] = seg[: len(mid[i:i + blk])]
    whistle = butter(r.standard_normal(n), "band", [1150, 1350], sr, 2) * 0.25
    x = low * (0.6 + 0.4 * gust) + mid * gust * 0.9 + whistle * gust ** 2
    write("amb_wind", seamless(x, sr, 1.5), sr, 0.7)


def amb_crickets():
    sr, dur = SR_AMB, 21.0
    n = int(dur * sr)
    t = np.arange(n) / sr
    r = rng(9)
    x = np.zeros(n)
    for c in range(7):
        carrier = r.uniform(3900, 5200)
        rate = r.uniform(1.6, 3.2)
        dist = r.uniform(0.25, 1.0)
        pulse_n = int(0.018 * sr)
        pt = np.arange(pulse_n) / sr
        pulse = np.sin(2 * np.pi * carrier * pt) * np.sin(np.pi * pt / pt[-1]) ** 2
        tt = r.uniform(0, 1)
        while tt < dur - 0.3:
            if r.uniform() < 0.88:
                for s in range(r.integers(2, 5)):
                    place(x, pulse * dist, int((tt + s * 0.032) * sr))
            tt += 1 / rate * r.uniform(0.85, 1.15)
            if r.uniform() < 0.05:
                tt += r.uniform(1, 3)   # a pause now and then
    x = butter(x, "low", 7000, sr)
    x += butter(r.standard_normal(n), "high", 3000, sr) * 0.004   # distant insect bed
    write("amb_crickets", seamless(x, sr, 1.0), sr, 0.55)


def amb_fire():
    sr, dur = SR_AMB, 13.0
    n = int(dur * sr)
    t = np.arange(n) / sr
    r = rng(10)
    roar = butter(np.cumsum(r.standard_normal(n)) * 0.02, "band", [40, 320], sr)
    roar *= 0.7 + 0.3 * np.sin(2 * np.pi * t * 0.37) * np.sin(2 * np.pi * t * 0.11)
    crack = np.zeros(n)
    count = int(dur * 14)
    for _ in range(count):
        s = int(r.uniform(0, dur - 0.1) * sr)
        ln = int(r.uniform(0.004, 0.02) * sr)
        burst = r.standard_normal(ln) * np.exp(-np.arange(ln) / (ln / 4))
        place(crack, burst * r.uniform(0.2, 1.0) ** 2, s)
    for _ in range(int(dur * 0.8)):   # occasional louder pops
        s = int(r.uniform(0, dur - 0.1) * sr)
        ln = int(0.03 * sr)
        place(crack, r.standard_normal(ln) * np.exp(-np.arange(ln) / (ln / 6)) * 1.6, s)
    crack = butter(crack, "band", [900, 6000], sr)
    hiss = butter(r.standard_normal(n), "high", 4000, sr) * 0.02
    x = roar * 1.4 + crack * 0.9 + hiss
    write("amb_fire", seamless(x, sr, 1.0), sr, 0.75)


def amb_cloth():
    sr, dur = SR_AMB, 11.0
    n = int(dur * sr)
    t = np.arange(n) / sr
    r = rng(12)
    gust = np.clip(np.sin(2 * np.pi * t / 5.5) * 0.6 + np.sin(2 * np.pi * t / 2.2 + 0.7) * 0.4, 0, 1) ** 1.5
    flutter = 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * (9 + 4 * gust) * t)) * (0.6 + 0.4 * r.uniform(size=n))
    x = butter(r.standard_normal(n), "band", [180, 1600], sr) * gust * butter(flutter, "low", 60, sr)
    x += butter(r.standard_normal(n), "low", 300, sr) * gust * 0.3
    write("amb_cloth", seamless(x, sr, 1.0), sr, 0.6)


# ───────────────────────── effects ─────────────────────────

def sfx_footstep():
    sr = SR_AMB
    n = int(0.36 * sr)
    t = np.arange(n) / sr
    r = rng(13)
    grains = np.zeros(n)
    density = np.exp(-((t - 0.05) / 0.06) ** 2) + 0.4 * np.exp(-((t - 0.13) / 0.05) ** 2)
    for i in range(n):
        if r.uniform() < density[i] * 0.06:
            ln = int(r.uniform(0.0008, 0.003) * sr)
            place(grains, r.standard_normal(ln) * r.uniform(0.3, 1), i)
    thud = butter(r.standard_normal(n), "low", 180, sr) * np.exp(-t * 40) * 0.8
    x = butter(grains, "band", [500, 4200], sr) + thud
    write("sfx_footstep", x, sr, 0.85)


def sfx_click():
    sr = SR_AMB
    x = ks_pluck(1318.5, 0.12, sr, 14, bright=0.8, decay=0.985)
    n = len(x)
    x += butter(rng(15).standard_normal(n), "band", [2500, 6000], sr) * np.exp(-np.arange(n) / sr * 120) * 0.4
    write("sfx_click", x, sr, 0.7)


def ui_open():
    sr = SR_AMB
    x = np.zeros(int(0.7 * sr))
    place(x, oud(hijaz_note(4, 1), 0.6, sr, 16, 0.7), 0)
    place(x, oud(hijaz_note(7, 1), 0.6, sr, 17, 0.6), int(0.07 * sr))
    write("ui_open", reverb(x, sr, 0.9, 0.25, 31), sr, 0.6)


def ui_confirm():
    sr = SR_AMB
    x = np.zeros(int(1.0 * sr))
    for i, deg in enumerate([7, 9, 11]):
        place(x, oud(hijaz_note(deg, 0), 0.7, sr, 18 + i, 0.7), int(i * 0.075 * sr))
    write("ui_confirm", reverb(x, sr, 1.1, 0.3, 32), sr, 0.65)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    print("Writing to", OUT)
    for fn in (amb_wind, amb_crickets, amb_fire, amb_cloth, sfx_footstep, sfx_click, ui_open, ui_confirm,
               sting_legend, sting_reveal, mus_hijaz, mus_legend):
        fn()
    print("done")
