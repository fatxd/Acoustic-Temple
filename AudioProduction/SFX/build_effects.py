"""Generate four separate game effects for Bach (48 kHz, stereo, 24-bit PCM)."""

from pathlib import Path
import math
import uuid
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Resources" / "GameSFX"
PROJECT = Path(__file__).resolve().parent / "Bach_Efectos.RPP"
SR = 48_000
RNG = np.random.default_rng(20260928)


def silence(seconds):
    return np.zeros((round(seconds * SR), 2), dtype=np.float64)


def add(buffer, sound, start=0, pan=0, level=1):
    first = round(start * SR)
    count = min(len(sound), len(buffer) - first)
    if count <= 0:
        return
    buffer[first:first + count, 0] += sound[:count] * math.sqrt((1 - pan) / 2) * level
    buffer[first:first + count, 1] += sound[:count] * math.sqrt((1 + pan) / 2) * level


def hit(seconds=0.48):
    t = np.arange(round(seconds * SR)) / SR
    # One quick, tactile impact: a falling drum body and a short noisy edge.
    phase = 2 * np.pi * (69 * t + 115 * (1 - np.exp(-30 * t)) / 30)
    body = np.sin(phase) * np.exp(-12 * t)
    noise = RNG.normal(size=len(t))
    edge = (noise - np.roll(noise, 1)) * np.exp(-85 * t)
    knock = np.sin(2 * np.pi * 290 * t) * np.exp(-36 * t)
    return (0.76 * body + 0.12 * edge + 0.16 * knock) * np.minimum(1, t / 0.0015)


def bell(seconds=1.05):
    t = np.arange(round(seconds * SR)) / SR
    f = 1174.66  # D6: a small, bright bell above the darker game music.
    partials = (
        np.sin(2 * np.pi * f * t)
        + 0.48 * np.sin(2 * np.pi * 2.01 * f * t)
        + 0.25 * np.sin(2 * np.pi * 3.93 * f * t)
        + 0.10 * np.sin(2 * np.pi * 5.44 * f * t)
    )
    attack = np.minimum(1, t / 0.002)
    return partials * attack * np.exp(-5.2 * t)


def brass(frequency, seconds):
    t = np.arange(round(seconds * SR)) / SR
    vibrato_depth = np.clip((t - 0.10) * 0.001, 0, 0.00022)
    phase = 2 * np.pi * frequency * (t + vibrato_depth * np.sin(2 * np.pi * 5.2 * t))
    # Harmonics emphasize the brass formant; staggered attacks keep the fanfare articulated.
    sound = np.zeros_like(t)
    for harmonic in range(1, min(17, int(19_000 / frequency))):
        band = math.exp(-((harmonic * frequency - 1100) / 1050) ** 2)
        sound += (0.22 + band) * np.sin(harmonic * phase + harmonic * 0.14) / harmonic
    attack = np.minimum(1, t / 0.038) ** 1.6
    release = np.minimum(1, np.maximum(0, seconds - t) / 0.11)
    flutter = 1 + 0.035 * np.sin(2 * np.pi * 7.3 * t)
    breath = RNG.normal(size=len(t)) * np.exp(-65 * t) * 0.018
    return (sound * attack * release * flutter + breath) * 0.53


def make_effects():
    life = silence(0.50)
    add(life, hit(), level=0.72)

    correct = silence(1.08)
    add(correct, bell(), pan=-0.10, level=0.25)
    add(correct, bell(0.80), start=0.085, pan=0.25, level=0.075)

    defeat = silence(2.05)
    # The same impact starts the longer defeat cue, followed by a descending tail.
    add(defeat, hit(), level=0.74)
    for start, frequency, level in [(0.22, 146.83, 0.19),
                                    (0.58, 130.81, 0.15),
                                    (0.94, 110.00, 0.13)]:
        t = np.arange(round(0.95 * SR)) / SR
        tone = (np.sin(2 * np.pi * frequency * t)
                + 0.23 * np.sin(2 * np.pi * frequency * 2.015 * t))
        tone *= np.minimum(1, t / 0.018) * np.exp(-3.5 * t)
        add(defeat, tone, start, pan=-0.15 if start == 0.58 else 0.12, level=level)
    # A restrained rumble gives the defeat a longer ending without turning it into music.
    t = np.arange(len(defeat)) / SR
    rumble = np.sin(2 * np.pi * (54 * t + 34 * (1 - np.exp(-4 * t)) / 4))
    add(defeat, rumble * np.exp(-2.8 * t), start=0.0, level=0.10)

    victory = silence(2.85)
    # Two short trumpet lines answer each other and land on a D-major chord.
    notes = [
        (0.00, 587.33, 0.21, -0.28, 0.34),  # D5
        (0.00, 440.00, 0.21, 0.22, 0.23),  # A4
        (0.34, 587.33, 0.21, -0.28, 0.34),
        (0.34, 440.00, 0.21, 0.22, 0.23),
        (0.70, 880.00, 0.35, -0.28, 0.33),  # A5
        (0.70, 659.25, 0.35, 0.22, 0.22),  # E5
        (1.12, 739.99, 0.30, -0.28, 0.31),  # F#5
        (1.12, 587.33, 0.30, 0.22, 0.22),
        (1.49, 1174.66, 0.95, -0.25, 0.31),  # D6
        (1.49, 880.00, 0.95, 0.25, 0.23),  # A5
        (1.49, 739.99, 0.95, 0.02, 0.15),  # F#5
    ]
    for start, frequency, duration, pan, level in notes:
        add(victory, brass(frequency, duration), start, pan, level)
    # Small room reflections, still leaving each note clear.
    dry = victory.copy()
    victory[round(0.095 * SR):] += dry[:-round(0.095 * SR)] * 0.09
    victory[round(0.19 * SR):] += dry[:-round(0.19 * SR)] * 0.06

    return {
        "01_Pierde_vida.wav": life,
        "02_Respuesta_correcta.wav": correct,
        "03_Derrota.wav": defeat,
        "04_Victoria_trompetas.wav": victory,
    }


def write_wav(path, data, target_peak):
    # Leave generous headroom; bright bell and sustained brass need extra reduction.
    peak = float(np.max(np.abs(data)))
    if peak > 0:
        data = data * (target_peak / peak)
    pcm = np.rint(np.clip(data, -1, 1) * 8_388_607).astype(np.int32)
    packed = (pcm[..., None] >> np.array([0, 8, 16]) & 255).astype(np.uint8)
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(3)
        wav.setframerate(SR)
        wav.writeframes(packed.tobytes())
    print(f"{path.name}: {len(data) / SR:.2f}s, peak {20 * math.log10(target_peak):.2f} dBFS")


def ensure_metas(names):
    folder_meta = OUT.with_name(OUT.name + ".meta")
    if not folder_meta.exists():
        folder_meta.write_text(
            "fileFormatVersion: 2\n"
            f"guid: {uuid.uuid5(uuid.NAMESPACE_URL, str(OUT.relative_to(ROOT))).hex}\n"
            "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
            "  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
            encoding="utf-8",
        )
    for name in names:
        meta = OUT / (name + ".meta")
        if not meta.exists():
            meta.write_text(
                "fileFormatVersion: 2\n"
                f"guid: {uuid.uuid5(uuid.NAMESPACE_URL, str(meta.relative_to(ROOT))).hex}\n"
                "AudioImporter:\n  externalObjects: {}\n  serializedVersion: 6\n"
                "  defaultSettings:\n    loadType: 0\n    sampleRateSetting: 0\n"
                "    sampleRateOverride: 48000\n    compressionFormat: 0\n"
                "    quality: 1\n    conversionMode: 0\n  platformSettingOverrides: {}\n"
                "  forceToMono: 0\n  normalize: 0\n  preloadAudioData: 1\n"
                "  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n"
                "  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
                encoding="utf-8",
            )


def write_reaper_project(effects):
    lines = [
        '<REAPER_PROJECT 0.1 "7.0/x64" 0',
        ' RIPPLE 0', ' AUTOXFADE 1', ' PANLAW 1', ' PROJOFFS 0 0 0',
        ' TIMEMODE 1 5 -1 30 0 0 -1', ' CURSOR 0', ' LOOP 0',
        ' TEMPO 120 4 4', ' SAMPLERATE 48000 1 0',
    ]
    names = ["01 Pierde vida - golpe", "02 Respuesta correcta - campanita",
             "03 Derrota - golpe extendido", "04 Victoria - trompetas"]
    positions = [0.0, 1.5, 3.5, 6.5]
    for (filename, data), track_name, position in zip(effects.items(), names, positions):
        track_id = str(uuid.uuid5(uuid.NAMESPACE_URL, track_name))
        item_id = str(uuid.uuid5(uuid.NAMESPACE_URL, filename))
        lines.extend([
            f' <TRACK {{{track_id}}}', f'  NAME "{track_name}"',
            '  VOLPAN 1 0 -1 -1 1', '  MUTESOLO 0 0 0',
            '  <ITEM', f'   POSITION {position:.3f}',
            f'   LENGTH {len(data) / SR:.6f}', '   LOOP 0',
            '   ALLTAKES 0', '   FADEIN 1 0 0 1 0 0 0',
            '   FADEOUT 1 0 0 1 0 0 0', '   MUTE 0 0', '   SEL 0',
            f'   IGUID {{{item_id}}}', f'   NAME "{filename}"',
            '   VOLPAN 1 0 1 -1', '   SOFFS 0',
            '   PLAYRATE 1 1 0 -1 0 0.0025', '   CHANMODE 0',
            '   <SOURCE WAVE', f'    FILE "../../Assets/Resources/GameSFX/{filename}"',
            '   >', '  >', ' >',
        ])
    lines.append('>')
    PROJECT.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    effects = make_effects()
    target_peaks = {
        "01_Pierde_vida.wav": 0.25,
        "02_Respuesta_correcta.wav": 0.20,
        "03_Derrota.wav": 0.25,
        "04_Victoria_trompetas.wav": 0.185,
    }
    for name, data in effects.items():
        write_wav(OUT / name, data, target_peaks[name])
    ensure_metas(effects)
    write_reaper_project(effects)


if __name__ == "__main__":
    main()
