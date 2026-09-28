"""Render seven phase-aligned music stems and two archived cue sketches.

Requires NumPy. Run with the bundled Codex Python or any Python with NumPy.
"""

from pathlib import Path
import math
import wave
import uuid

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Resources" / "AdaptiveMusic"
SR = 48000
BPM = 96
BEATS = 32  # Eight bars of 4/4.
SECONDS = BEATS * 60 / BPM
FRAMES = int(SECONDS * SR)
RNG = np.random.default_rng(20260928)


def note(number):
    return 440.0 * 2 ** ((number - 69) / 12)


def stereo(seconds=SECONDS):
    return np.zeros((int(round(seconds * SR)), 2), dtype=np.float64)


def add(buffer, start_seconds, samples, pan=0.0, gain=1.0):
    """Wrap tails into the next cycle, preserving exact stem alignment."""
    left = math.sqrt((1 - pan) / 2) * gain
    right = math.sqrt((1 + pan) / 2) * gain
    start = int(round(start_seconds * SR)) % FRAMES
    samples = np.asarray(samples, dtype=np.float64)
    for first in range(0, len(samples), FRAMES):
        chunk = samples[first:first + FRAMES]
        pos = (start + first) % FRAMES
        count = min(len(chunk), FRAMES - pos)
        buffer[pos:pos + count, 0] += chunk[:count] * left
        buffer[pos:pos + count, 1] += chunk[:count] * right
        if count < len(chunk):
            buffer[:len(chunk) - count, 0] += chunk[count:] * left
            buffer[:len(chunk) - count, 1] += chunk[count:] * right


def bell(pitch, seconds=2.1):
    t = np.arange(int(seconds * SR)) / SR
    f = note(pitch)
    partials = (np.sin(2 * np.pi * f * t) +
                0.42 * np.sin(2 * np.pi * 2.013 * f * t) +
                0.21 * np.sin(2 * np.pi * 4.09 * f * t))
    attack = np.minimum(1.0, t / 0.008)
    return partials * attack * np.exp(-2.65 * t)


def dark_metal(seconds=0.72):
    """A low inharmonic strike that sounds unlike the correct-answer bell."""
    t = np.arange(int(seconds * SR)) / SR
    f = note(51)
    sound = (np.sin(2 * np.pi * f * t) +
             0.55 * np.sin(2 * np.pi * 2.67 * f * t) +
             0.24 * np.sin(2 * np.pi * 4.38 * f * t))
    return sound * np.minimum(1, t / 0.006) * np.exp(-7.5 * t)


def hand_drum(strength=1.0):
    t = np.arange(int(0.34 * SR)) / SR
    phase = 2 * np.pi * (71 * t + 50 * (1 - np.exp(-t * 37)) / 37)
    body = np.sin(phase) * np.exp(-19 * t)
    noise = RNG.normal(0, 1, len(t))
    attack = (noise - np.roll(noise, 1)) * np.exp(-67 * t) * 0.19
    return strength * (body + attack) * np.minimum(1, t / 0.002)


def rim():
    t = np.arange(int(0.13 * SR)) / SR
    n = RNG.normal(0, 1, len(t))
    return (n - np.roll(n, 1)) * np.exp(-42 * t) * np.minimum(1, t / 0.002) * 0.18


def shaker():
    t = np.arange(int(0.085 * SR)) / SR
    n = RNG.normal(0, 1, len(t))
    return (n - np.roll(n, 1)) * np.exp(-60 * t) * np.minimum(1, t / 0.002) * 0.11


def pluck(pitch, seconds=0.46):
    t = np.arange(int(seconds * SR)) / SR
    f = note(pitch)
    return (np.sin(2 * np.pi * f * t) + 0.38 * np.sin(2 * np.pi * 2 * f * t)
            + 0.1 * np.sin(2 * np.pi * 3.02 * f * t)) * np.exp(-9 * t) * np.minimum(1, t / 0.005)


def write(name, data, peak_limit):
    peak = float(np.max(np.abs(data)))
    if peak > peak_limit:
        data *= peak_limit / peak
    pcm = np.rint(np.clip(data, -1, 1) * 32767).astype("<i2")
    with wave.open(str(OUT / name), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(pcm.tobytes())
    print(f"{name}: {len(data) / SR:.3f}s, peak={np.max(np.abs(pcm)) / 32767:.3f}")


def ensure_unity_metas(names):
    """Keep stable GUIDs and import lossless PCM for sample-accurate looping."""
    resources = ROOT / "Assets" / "Resources"
    for folder in (resources, OUT):
        meta = folder.with_name(folder.name + ".meta")
        if not meta.exists():
            guid = uuid.uuid5(uuid.NAMESPACE_URL, str(folder.relative_to(ROOT))).hex
            meta.write_text(
                f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\n"
                "DefaultImporter:\n  externalObjects: {}\n  userData: \n"
                "  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8")
    for name in names:
        meta = OUT / (name + ".meta")
        if not meta.exists():
            guid = uuid.uuid5(uuid.NAMESPACE_URL, str(meta.relative_to(ROOT))).hex
            meta.write_text(
                f"fileFormatVersion: 2\nguid: {guid}\nAudioImporter:\n"
                "  externalObjects: {}\n  serializedVersion: 6\n"
                "  defaultSettings:\n    loadType: 0\n    sampleRateSetting: 0\n"
                "    sampleRateOverride: 48000\n    compressionFormat: 0\n"
                "    quality: 1\n    conversionMode: 0\n"
                "  platformSettingOverrides: {}\n  forceToMono: 0\n"
                "  normalize: 0\n  preloadAudioData: 1\n"
                "  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n"
                "  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
                encoding="utf-8")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    beat = 60 / BPM
    t = np.arange(FRAMES) / SR

    # D Phrygian. Keep energy mostly below the speech band.
    drone = stereo()
    for midi, amp, pan in [(38, 0.11, 0), (45, 0.055, -0.25), (50, 0.03, 0.25)]:
        f = note(midi)
        # Integer cycles over the whole loop prevent a click at the join.
        f = round(f * SECONDS) / SECONDS
        vibration = np.sin(2 * np.pi * f * t) + 0.26 * np.sin(2 * np.pi * 2 * f * t)
        drift = 0.92 + 0.08 * np.sin(2 * np.pi * 2 * t / SECONDS + pan)
        drone[:, 0] += amp * vibration * drift * math.sqrt((1 - pan) / 2)
        drone[:, 1] += amp * vibration * drift * math.sqrt((1 + pan) / 2)
    write("01_drone.wav", drone, 0.27)

    pad = stereo()
    # Minor seconds in the voicings create suspense without crowding the voice.
    chords = [[50, 53, 57, 63], [51, 55, 58, 62],
              [48, 51, 55, 62], [50, 53, 57, 63]]
    for chord_index, chord in enumerate(chords):
        start = chord_index * 8 * beat
        duration = 8 * beat + 1.0
        local = np.arange(int(duration * SR)) / SR
        env = np.minimum(1, local / 0.6) * np.minimum(1, (duration - local) / 1.1)
        for i, midi in enumerate(chord):
            f = note(midi)
            tone = (np.sin(2 * np.pi * f * local) +
                    0.3 * np.sin(2 * np.pi * 2.003 * f * local))
            shimmer = 0.78 + 0.22 * np.sin(2 * np.pi * 0.19 * local + i)
            add(pad, start, tone * env * shimmer, pan=(-0.5 + i / 3), gain=0.03)
    # A short edge fade avoids a click where the chord cycles.
    edge = int(0.08 * SR)
    pad[:edge] *= np.linspace(0, 1, edge)[:, None]
    pad[-edge:] *= np.linspace(1, 0, edge)[:, None]
    write("02_pad.wav", pad, 0.22)

    motif = stereo()
    # Leave space for the separate correct-answer chime.
    melody = [(3.5, 75), (8, 72), (14, 74),
              (19.5, 69), (24, 74), (30, 75)]
    for i, (b, midi) in enumerate(melody):
        sound = bell(midi)
        add(motif, b * beat, sound, pan=(-0.38 if i % 2 else 0.38), gain=0.03)
        add(motif, (b + 0.75) * beat, sound, pan=(0.38 if i % 2 else -0.38), gain=0.006)
    write("03_bells.wav", motif, 0.11)

    slow = stereo()
    for b in range(BEATS):
        if b % 4 in (0, 2):
            add(slow, b * beat, hand_drum(0.75 if b % 4 == 2 else 1), pan=-0.08, gain=0.24)
        if b % 4 == 3:
            add(slow, (b + 0.5) * beat, rim(), pan=0.12, gain=0.14)
    write("04_slow_drums.wav", slow, 0.27)

    pulse = stereo()
    pitches = [50, 57, 51, 57, 48, 55, 51, 55,
               50, 57, 53, 57, 48, 55, 51, 57]
    for half_beat in range(BEATS * 2):
        if half_beat % 8 == 7:
            continue
        pitch = pitches[(half_beat // 4) % len(pitches)]
        add(pulse, half_beat * beat / 2, pluck(pitch),
            pan=(-0.27 if half_beat % 2 else 0.27),
            gain=(0.092 if half_beat % 2 else 0.12))
        if half_beat % 8 == 0:
            add(pulse, half_beat * beat / 2, pluck(pitch - 12, 0.62),
                pan=0, gain=0.055)
    write("05_eighth_pulse.wav", pulse, 0.21)

    fast = stereo()
    for step in range(BEATS * 4):
        b = step / 4
        if step % 4 in (0, 2, 3):
            add(fast, b * beat, shaker(),
                pan=(-0.42 if step % 2 else 0.42),
                gain=(0.12 if step % 4 == 0 else 0.065))
        if step % 16 in (0, 6, 10):
            add(fast, b * beat, hand_drum(0.55), pan=0.06, gain=0.13)
    write("06_fast_percussion.wav", fast, 0.22)

    danger = stereo()
    # D and E-flat rub against each other; the midrange tremolo rises with risk.
    for midi, amp, pan in [(39, 0.078, -0.35), (50, 0.048, 0.35),
                           (51, 0.033, -0.10), (63, 0.018, 0.20)]:
        f = round(note(midi) * SECONDS) / SECONDS
        vibration = (np.sin(2 * np.pi * f * t) +
                     0.36 * np.sin(2 * np.pi * 2 * f * t) +
                     0.12 * np.sin(2 * np.pi * 3 * f * t))
        tremolo = 0.32 + 0.68 * np.maximum(0, np.sin(2 * np.pi * 2 * t / beat))
        danger[:, 0] += amp * vibration * tremolo * math.sqrt((1 - pan) / 2)
        danger[:, 1] += amp * vibration * tremolo * math.sqrt((1 + pan) / 2)
    for b in range(0, BEATS, 4):
        add(danger, (b + 3.5) * beat, dark_metal(), pan=0.35, gain=0.065)
        add(danger, (b + 2) * beat, hand_drum(0.35), pan=-0.12, gain=0.07)
    write("07_danger_texture.wav", danger, 0.24)

    life = stereo(1.35)
    cue_t = np.arange(len(life)) / SR
    falling = np.sin(2 * np.pi * (62 * cue_t + 65 * (1 - np.exp(-24 * cue_t)) / 24))
    noise = RNG.normal(0, 1, len(cue_t))
    transient = (noise - np.roll(noise, 1)) * np.exp(-34 * cue_t)
    clash = (np.sin(2 * np.pi * note(62) * cue_t) +
             np.sin(2 * np.pi * note(63) * cue_t)) * np.exp(-6 * cue_t)
    sound = (0.52 * falling * np.exp(-5 * cue_t) +
             0.08 * transient + 0.1 * clash) * np.minimum(1, cue_t / 0.003)
    life[:, 0] = sound
    life[:, 1] = 0.92 * sound
    write("08_life_lost.wav", life, 0.36)

    passed = stereo(1.05)
    for start, pitch, pan, gain in [(0, 74, -0.3, 0.12), (0.19, 77, 0.3, 0.095)]:
        sound = bell(pitch, 0.85) * gain
        first = int(start * SR)
        count = min(len(sound), len(passed) - first)
        passed[first:first + count, 0] += sound[:count] * math.sqrt((1 - pan) / 2)
        passed[first:first + count, 1] += sound[:count] * math.sqrt((1 + pan) / 2)
    write("09_question_passed.wav", passed, 0.2)

    # Reference mix: compress a full round's progression into one loop.
    names = ["01_drone.wav", "02_pad.wav", "03_bells.wav",
             "04_slow_drums.wav", "05_eighth_pulse.wav", "06_fast_percussion.wav",
             "07_danger_texture.wav"]
    cue_names = ["08_life_lost.wav", "09_question_passed.wav"]
    ensure_unity_metas(names + cue_names)
    mix = stereo()
    def rise(at, duration=0.8):
        return np.clip((t - at) / duration, 0, 1)

    samples_by_name = {}
    for layer_index, name in enumerate(names):
        with wave.open(str(OUT / name), "rb") as wav:
            samples = np.frombuffer(wav.readframes(FRAMES), dtype="<i2").reshape(-1, 2) / 32768.0
        samples_by_name[name] = samples
        if layer_index == 0:
            gain = np.full(FRAMES, 0.78)
        elif layer_index == 1:
            gain = np.full(FRAMES, 0.82)
        elif layer_index == 2:
            gain = np.full(FRAMES, 0.25)
        elif layer_index == 3:
            gain = 0.38 + 0.47 * rise(5)
        elif layer_index == 4:
            gain = 0.60 * rise(5) + 0.18 * rise(10)
        elif layer_index == 5:
            gain = 0.50 * rise(10) + 0.26 * rise(15)
        else:
            gain = 0.08 + 0.3 * rise(7) + 0.3 * rise(13) + 0.27 * rise(17)
        mix += samples * gain[:, None]
    write_preview = Path(__file__).resolve().parent / "preview_escalada.wav"
    peak = np.max(np.abs(mix))
    mix *= min(1, 0.62 / peak)
    with wave.open(str(write_preview), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(np.rint(mix * 32767).astype("<i2").tobytes())
    print(f"Preview: {write_preview}")

    # A second preview shows the answer cues without needing live gameplay.
    event_mix = (samples_by_name[names[0]] + samples_by_name[names[1]] +
                 samples_by_name[names[2]] * 0.6).copy()
    event_mix += samples_by_name[names[3]] * np.clip((t - 9) / 1.0, 0, 1)[:, None]
    event_mix += samples_by_name[names[4]] * np.clip((t - 12) / 1.0, 0, 1)[:, None]
    event_mix += samples_by_name[names[5]] * np.clip((t - 17) / 1.0, 0, 1)[:, None]
    event_mix += samples_by_name[names[6]] * np.clip((t - 4.8) / 1.2, 0, 0.85)[:, None]
    for cue_name, when in [(cue_names[0], 4.0), (cue_names[1], 11.0)]:
        with wave.open(str(OUT / cue_name), "rb") as wav:
            cue = np.frombuffer(wav.readframes(wav.getnframes()), dtype="<i2").reshape(-1, 2) / 32768.0
        first = int(when * SR)
        event_mix[first:first + len(cue)] += cue * 0.7
    event_mix *= min(1, 0.65 / np.max(np.abs(event_mix)))
    event_preview = Path(__file__).resolve().parent / "preview_eventos.wav"
    with wave.open(str(event_preview), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(np.rint(event_mix * 32767).astype("<i2").tobytes())
    print(f"Preview: {event_preview}")

    track_labels = [
        "01 Base - drone D frigio",
        "02 Base - armonía oscura",
        "03 Base - campanas discretas",
        "04 Nivel 1 - tambor pesado",
        "05 Nivel 2 - pulso en corcheas",
        "06 Nivel 3 - percusión urgente",
        "07 Peligro - vidas y tiempo",
        "08 Señal - vida perdida",
        "09 Señal - pregunta superada",
    ]
    project = [
        '<REAPER_PROJECT 0.1 "7.0/x64" 0',
        ' RIPPLE 0',
        ' AUTOXFADE 1',
        ' PANLAW 1',
        ' PROJOFFS 0 0 0',
        ' MAXPROJLEN 0 600',
        ' TIMEMODE 1 5 -1 30 0 0 -1',
        ' CURSOR 0',
        ' LOOP 0',
        f' TEMPO {BPM} 4 4',
    ]
    for index, (name, label) in enumerate(zip(names + cue_names, track_labels)):
        track_guid = "{" + str(uuid.uuid5(uuid.NAMESPACE_URL, name + "-track")) + "}"
        item_guid = "{" + str(uuid.uuid5(uuid.NAMESPACE_URL, name + "-item")) + "}"
        is_cue = index >= len(names)
        with wave.open(str(OUT / name), "rb") as wav:
            item_length = wav.getnframes() / wav.getframerate()
        item_position = 0 if not is_cue else 21 + (index - len(names)) * 2
        project += [
            f' <TRACK {track_guid}',
            f'  NAME "{label}"',
            f'  VOLPAN {0.72 if index < 3 else 0.8} 0 -1 -1 1',
            '  MUTESOLO 0 0 0',
            '  IPHASE 0',
            '  PLAYOFFS 0 1',
            '  ISBUS 0 0',
            '  <ITEM',
            f'   POSITION {item_position}',
            f'   LENGTH {item_length:.9f}',
            f'   LOOP {0 if is_cue else 1}',
            '   ALLTAKES 0',
            '   FADEIN 1 0 0 1 0 0 0',
            '   FADEOUT 1 0 0 1 0 0 0',
            '   MUTE 0 0',
            '   SEL 0',
            f'   IGUID {item_guid}',
            f'   NAME "{name}"',
            '   VOLPAN 1 0 1 -1',
            '   SOFFS 0',
            '   PLAYRATE 1 1 0 -1 0 0.0025',
            '   CHANMODE 0',
            f'   <SOURCE WAVE',
            f'    FILE "../../Assets/Resources/AdaptiveMusic/{name}"',
            '   >',
            '  >',
            ' >',
        ]
    project.append('>')
    (Path(__file__).resolve().parent / 'Templo_Adaptativo.RPP').write_text(
        '\n'.join(project) + '\n', encoding='utf-8')


if __name__ == "__main__":
    main()
