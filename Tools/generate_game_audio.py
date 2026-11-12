import wave
import struct
import math
import random

SAMPLE_RATE = 44100

def write_wav(filename, samples):
    with wave.open(filename, 'w') as wav:
        wav.setnchannels(1) # mono
        wav.setsampwidth(2) # 16-bit
        wav.setframerate(SAMPLE_RATE)
        # Normalize to avoid clipping
        max_amp = max(abs(s) for s in samples) if samples else 1.0
        scale = 32000.0 / max(max_amp, 1.0)
        data = bytearray()
        for s in samples:
            val = int(max(-32767, min(32767, s * scale)))
            data.extend(struct.pack('<h', val))
        wav.writeframes(data)
    print(f"Generated: {filename} ({len(samples)/SAMPLE_RATE:.2f}s)")

def generate_bgm(filename):
    # Vietnamese Folk Pentatonic scale (C4, D4, F4, G4, A4, C5, D5, F5, G5)
    scale = [261.63, 293.66, 349.23, 392.00, 440.00, 523.25, 587.33, 698.46, 783.99]
    bpm = 120.0
    beat_dur = 60.0 / bpm # 0.5s per beat
    total_beats = 32 # 8 measures of 4/4 = 16 seconds loop
    total_samples = int(total_beats * beat_dur * SAMPLE_RATE)
    
    buf = [0.0] * total_samples
    
    # Melodic motif (Rong Ran Len May folk motif)
    # Measures: 8 measures, 4 beats each
    melody_notes = [
        # Bar 1: C5, D5, C5, A4
        (0.0, 5, 0.45), (1.0, 6, 0.45), (2.0, 5, 0.45), (3.0, 4, 0.45),
        # Bar 2: G4, A4, C5, G4
        (4.0, 3, 0.45), (5.0, 4, 0.45), (6.0, 5, 0.9),
        # Bar 3: D5, F5, D5, C5
        (8.0, 6, 0.45), (9.0, 7, 0.45), (10.0, 6, 0.45), (11.0, 5, 0.45),
        # Bar 4: G4, A4, G4 (rest)
        (12.0, 3, 0.45), (13.0, 4, 0.45), (14.0, 3, 0.9),
        # Bar 5: F4, G4, A4, C5
        (16.0, 2, 0.45), (17.0, 3, 0.45), (18.0, 4, 0.45), (19.0, 5, 0.45),
        # Bar 6: D5, C5, A4, G4
        (20.0, 6, 0.45), (21.0, 5, 0.45), (22.0, 4, 0.45), (23.0, 3, 0.45),
        # Bar 7: A4, C5, D5, C5
        (24.0, 4, 0.45), (25.0, 5, 0.45), (26.0, 6, 0.45), (27.0, 5, 0.45),
        # Bar 8: G4, A4, C5
        (28.0, 3, 0.45), (29.0, 4, 0.45), (30.0, 5, 0.9)
    ]
    
    # Synthesize Flute / Bell Melody
    for start_beat, note_idx, dur_beats in melody_notes:
        freq = scale[note_idx]
        start_samp = int(start_beat * beat_dur * SAMPLE_RATE)
        dur_samp = int(dur_beats * beat_dur * SAMPLE_RATE)
        for i in range(dur_samp):
            idx = start_samp + i
            if idx >= total_samples: break
            t = i / SAMPLE_RATE
            # Envelope (soft attack, decay, sustain)
            env = math.exp(-2.5 * (t / (dur_beats * beat_dur))) * (1.0 - math.exp(-50.0 * t))
            # Harmonics for bamboo flute / xylophone timbre
            sig = (0.6 * math.sin(2 * math.pi * freq * t) +
                   0.3 * math.sin(4 * math.pi * freq * t) +
                   0.15 * math.sin(6 * math.pi * freq * t))
            buf[idx] += sig * env * 0.45

    # Bassline (warm acoustic bass)
    bass_notes = [
        (0, 0), (2, 3), (4, 3), (6, 0),
        (8, 1), (10, 3), (12, 0), (14, 3),
        (16, 2), (18, 3), (20, 1), (22, 3),
        (24, 0), (26, 3), (28, 4), (30, 0)
    ]
    for start_beat, note_idx in bass_notes:
        freq = scale[note_idx] * 0.5 # 1 octave lower
        start_samp = int(start_beat * beat_dur * SAMPLE_RATE)
        dur_samp = int(1.8 * beat_dur * SAMPLE_RATE)
        for i in range(dur_samp):
            idx = start_samp + i
            if idx >= total_samples: break
            t = i / SAMPLE_RATE
            env = math.exp(-3.0 * t) * (1.0 - math.exp(-80.0 * t))
            sig = math.sin(2 * math.pi * freq * t) + 0.4 * math.sin(4 * math.pi * freq * t)
            buf[idx] += sig * env * 0.35

    # Folk Percussion (Trong Com & Song Lang)
    for b in range(total_beats):
        beat_start = int(b * beat_dur * SAMPLE_RATE)
        # Bass drum on beat 0 and 2 of each bar
        if b % 2 == 0:
            for i in range(int(0.25 * SAMPLE_RATE)):
                idx = beat_start + i
                if idx >= total_samples: break
                t = i / SAMPLE_RATE
                f = 110.0 * math.exp(-15.0 * t) + 45.0
                env = math.exp(-12.0 * t)
                buf[idx] += math.sin(2 * math.pi * f * t) * env * 0.50
        # High woodblock / clap on beat 1 and 3 of each bar
        if b % 2 == 1:
            for i in range(int(0.12 * SAMPLE_RATE)):
                idx = beat_start + i
                if idx >= total_samples: break
                t = i / SAMPLE_RATE
                env = math.exp(-35.0 * t)
                # Filtered noise + high sine
                sig = math.sin(2 * math.pi * 1250.0 * t) + (random.random() * 2 - 1) * 0.5
                buf[idx] += sig * env * 0.30

    write_wav(filename, buf)

def generate_sfx_pickup(filename):
    # Sweet sparkling major arpeggio
    dur = 0.45
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    notes = [523.25, 659.25, 783.99, 1046.50] # C5, E5, G5, C6
    for i, f in enumerate(notes):
        offset = int(i * 0.08 * SAMPLE_RATE)
        for s in range(int(0.25 * SAMPLE_RATE)):
            idx = offset + s
            if idx >= total_samples: break
            t = s / SAMPLE_RATE
            env = math.exp(-12.0 * t)
            buf[idx] += (math.sin(2 * math.pi * f * t) + 0.25 * math.sin(4 * math.pi * f * t)) * env * 0.35
    write_wav(filename, buf)

def generate_sfx_shield(filename):
    # Resonant bronze chime + low thump
    dur = 0.6
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    for s in range(total_samples):
        t = s / SAMPLE_RATE
        env = math.exp(-7.0 * t)
        thump_env = math.exp(-25.0 * t)
        thump = math.sin(2 * math.pi * (160.0 * math.exp(-15.0 * t) + 60.0) * t) * thump_env * 0.6
        chime = (math.sin(2 * math.pi * 587.33 * t) + 
                 0.6 * math.sin(2 * math.pi * 880.00 * t) + 
                 0.3 * math.sin(2 * math.pi * 1174.66 * t)) * env * 0.4
        buf[s] = thump + chime
    write_wav(filename, buf)

def generate_sfx_whoosh(filename):
    # Filtered sweep noise
    dur = 0.30
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    for s in range(total_samples):
        t = s / SAMPLE_RATE
        # Envelope rises and falls
        env = math.sin(math.pi * (t / dur)) ** 2
        f = 300.0 + 800.0 * math.sin(math.pi * (t / dur))
        noise = (random.random() * 2 - 1)
        buf[s] = (noise * 0.6 + math.sin(2 * math.pi * f * t) * 0.4) * env * 0.5
    write_wav(filename, buf)

def generate_sfx_victory(filename):
    # Fanfare jingle
    dur = 1.8
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    # C5, E5, G5, C6 (held)
    notes = [
        (0.00, 523.25, 0.20),
        (0.18, 659.25, 0.20),
        (0.36, 783.99, 0.25),
        (0.60, 1046.50, 1.10)
    ]
    for start_t, f, d in notes:
        offset = int(start_t * SAMPLE_RATE)
        dur_s = int(d * SAMPLE_RATE)
        for s in range(dur_s):
            idx = offset + s
            if idx >= total_samples: break
            t = s / SAMPLE_RATE
            env = math.exp(-2.5 * (t / d)) * (1.0 - math.exp(-60.0 * t))
            sig = (math.sin(2 * math.pi * f * t) + 
                   0.4 * math.sin(4 * math.pi * f * t) +
                   0.2 * math.sin(6 * math.pi * f * t))
            buf[idx] += sig * env * 0.45
    write_wav(filename, buf)

def generate_sfx_defeat(filename):
    # Sad descending trombone/sigh
    dur = 1.4
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    notes = [
        (0.00, 392.00, 0.35), # G4
        (0.35, 349.23, 0.35), # F4
        (0.70, 311.13, 0.35), # Eb4
        (1.05, 261.63, 0.35)  # C4
    ]
    for start_t, f, d in notes:
        offset = int(start_t * SAMPLE_RATE)
        dur_s = int(d * SAMPLE_RATE)
        for s in range(dur_s):
            idx = offset + s
            if idx >= total_samples: break
            t = s / SAMPLE_RATE
            env = math.exp(-3.0 * (t / d))
            sig = math.sin(2 * math.pi * f * t) + 0.3 * math.sin(3 * math.pi * f * t)
            buf[idx] += sig * env * 0.4
    write_wav(filename, buf)

def generate_sfx_cham_beat(filename):
    # Festive drum pulse
    dur = 0.4
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    for s in range(total_samples):
        t = s / SAMPLE_RATE
        env = math.exp(-14.0 * t)
        f = 180.0 * math.exp(-20.0 * t) + 70.0
        buf[s] = math.sin(2 * math.pi * f * t) * env * 0.75
    write_wav(filename, buf)

def generate_sfx_dodge(filename):
    # Quick nimble slide whistle
    dur = 0.30
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    for s in range(total_samples):
        t = s / SAMPLE_RATE
        env = math.sin(math.pi * (t / dur))
        f = 600.0 + 800.0 * (t / dur)
        buf[s] = math.sin(2 * math.pi * f * t) * env * 0.4
    write_wav(filename, buf)

def generate_sfx_ui_click(filename):
    # Clean subtle bubble pop
    dur = 0.08
    total_samples = int(dur * SAMPLE_RATE)
    buf = [0.0] * total_samples
    for s in range(total_samples):
        t = s / SAMPLE_RATE
        env = math.exp(-45.0 * t)
        f = 850.0 * math.exp(-25.0 * t)
        buf[s] = math.sin(2 * math.pi * f * t) * env * 0.35
    write_wav(filename, buf)

if __name__ == '__main__':
    generate_bgm("Assets/_Game/Audio/BGM/BGM_Gameplay.wav")
    generate_sfx_pickup("Assets/_Game/Audio/SFX/SFX_Pickup.wav")
    generate_sfx_shield("Assets/_Game/Audio/SFX/SFX_Shield_Block.wav")
    generate_sfx_whoosh("Assets/_Game/Audio/SFX/SFX_Whoosh.wav")
    generate_sfx_victory("Assets/_Game/Audio/SFX/SFX_Victory.wav")
    generate_sfx_defeat("Assets/_Game/Audio/SFX/SFX_Defeat.wav")
    generate_sfx_cham_beat("Assets/_Game/Audio/SFX/SFX_Cham_Beat.wav")
    generate_sfx_dodge("Assets/_Game/Audio/SFX/SFX_Dodge.wav")
    generate_sfx_ui_click("Assets/_Game/Audio/SFX/SFX_UI_Click.wav")
    print("All audio generated successfully!")
