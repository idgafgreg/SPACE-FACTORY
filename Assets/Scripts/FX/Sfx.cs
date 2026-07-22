using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural audio service — every clip is synthesized at first use
/// (AudioClip.Create + math), so the game gets a full SFX pass with zero
/// asset files, matching the primitives-only art direction.
///
/// Producers call the static one-liners (Sfx.Shot(), Sfx.Pickup()…).
/// A hidden persistent GameObject hosts a small pool of AudioSources; calls
/// are pitch-jittered so rapid repeats don't machine-gun. Volume knob:
/// <see cref="masterVolume"/>. Costs nothing until the first call.
/// </summary>
public class Sfx : MonoBehaviour
{
    [Range(0f, 1f)] public float masterVolume = 0.5f;

    const int SampleRate = 44100;
    const int PoolSize   = 12;

    static Sfx _instance;
    readonly List<AudioSource> _pool = new();
    int _next;
    readonly Dictionary<string, AudioClip> _clips = new();
    AudioSource _ambient;

    // B2: shift-end radio silence state
    float _userAmbient;
    float _silenceUntil;

    static Sfx Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Sfx>();
            for (int i = 0; i < PoolSize; i++)
            {
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;   // 2D — camera is far above the field
                _instance._pool.Add(src);
            }
            _instance._ambient = go.AddComponent<AudioSource>();
            _instance._ambient.playOnAwake = false;
            _instance._ambient.loop        = true;
            _instance._ambient.spatialBlend = 0f;
            _instance._ambient.clip        = MakeAmbientHum();
            return _instance;
        }
    }

    void ApplyAmbient(float volume01)
    {
        if (_ambient == null) return;
        _userAmbient = Mathf.Clamp01(volume01);
    }

    void Update()
    {
        if (_ambient == null) return;

        float v = _userAmbient * masterVolume * 0.35f;
        if (Time.time < _silenceUntil)
        {
            // B2: radio silence — ambient drops to zero, smooth ramp out/in.
            v = 0f;
        }

        _ambient.volume = v;
        if (v > 0.001f && !_ambient.isPlaying) _ambient.Play();
        if (v <= 0.001f && _ambient.isPlaying) _ambient.Stop();
    }

    /// <summary>B2: drop the ambient ship hum to silence for the given duration.</summary>
    public static void RadioSilence(float seconds)
    {
        var inst = Instance;
        inst._silenceUntil = Time.time + seconds;
        if (inst._ambient != null) inst._ambient.Stop();
    }

    // ── Public one-liners ─────────────────────────────────────────────────────

    public static void Shot()       => Play("shot",      MakeShot,      0.35f, 0.12f);
    public static void TurretShot() => Play("turretShot", MakeTurretShot, 0.18f, 0.10f);
    public static void Impact()    => Play("impact",    MakeImpact,    0.30f, 0.15f);
    public static void EnemyDie()  => Play("enemyDie",  MakeEnemyDie,  0.45f, 0.10f);
    public static void Pickup()    => Play("pickup",    MakePickup,    0.50f, 0.05f);
    public static void Place()     => Play("place",     MakePlace,     0.55f, 0.06f);
    public static void Demolish()  => Play("demolish",  MakeDemolish,  0.50f, 0.06f);
    public static void WaveHorn()  => Play("waveHorn",  MakeWaveHorn,  0.65f, 0.00f);
    public static void Unlock()    => Play("unlock",    MakeUnlock,    0.55f, 0.00f);
    public static void HubHit()    => Play("hubHit",    MakeHubHit,    0.55f, 0.08f);
    public static void UIClick()   => Play("uiClick",   MakeUIClick,   0.40f, 0.04f);
    public static void Warning()   => Play("warning",   MakeWarning,   0.50f, 0.00f);
    public static void Scan()      => Play("scan",      MakeScan,      0.45f, 0.00f);
    public static void DryFire()   => Play("dryFire",   MakeDryFire,   0.35f, 0.05f);
    public static void Skitter()   => Play("skitter",   MakeSkitter,   0.25f, 0.18f);
    public static void Alarm()     => Play("alarm",     MakeAlarm,     0.55f, 0.00f);

    /// <summary>Starts/stops the looping ship-hum AmbientSource (volume 0–1).</summary>
    public static void SetAmbient(float volume01) => Instance.ApplyAmbient(volume01);

    // ── Playback ─────────────────────────────────────────────────────────────

    static void Play(string key, System.Func<AudioClip> make, float volume, float pitchJitter)
    {
        var inst = Instance;
        if (!inst._clips.TryGetValue(key, out var clip) || clip == null)
        {
            clip = make();
            inst._clips[key] = clip;
        }

        var src = inst._pool[inst._next];
        inst._next = (inst._next + 1) % PoolSize;
        src.pitch  = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.volume = volume * inst.masterVolume;
        src.clip   = clip;
        src.Play();
    }

    // ── Synthesis helpers ─────────────────────────────────────────────────────

    static AudioClip Bake(string name, float seconds, System.Func<float, float> wave)
    {
        int n = Mathf.CeilToInt(seconds * SampleRate);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            data[i] = Mathf.Clamp(wave(t), -1f, 1f);
        }
        // Short fade at both ends kills clicks.
        int fade = Mathf.Min(128, n / 8);
        for (int i = 0; i < fade; i++)
        {
            float f = i / (float)fade;
            data[i] *= f;
            data[n - 1 - i] *= f;
        }
        var clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Noise() => Random.value * 2f - 1f;
    static float Env(float t, float dur, float power = 2f) =>
        Mathf.Pow(Mathf.Clamp01(1f - t / dur), power);

    // Zap: fast downward square sweep + noise tail.
    static AudioClip MakeShot() => Bake("shot", 0.09f, t =>
    {
        float f = Mathf.Lerp(1400f, 500f, t / 0.09f);
        float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t));
        return (square * 0.7f + Noise() * 0.3f) * Env(t, 0.09f, 3f);
    });

    // Soft high tick for auto-turrets (quieter / shorter than player shot).
    static AudioClip MakeTurretShot() => Bake("turretShot", 0.05f, t =>
    {
        float f = Mathf.Lerp(2200f, 900f, t / 0.05f);
        return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.05f, 4f) * 0.55f;
    });

    // Spark: filtered-ish noise snap.
    static AudioClip MakeImpact() => Bake("impact", 0.1f, t =>
        Noise() * Env(t, 0.1f, 4f) * 0.9f);

    // Death: descending saw croak.
    static AudioClip MakeEnemyDie() => Bake("enemyDie", 0.28f, t =>
    {
        float f = Mathf.Lerp(300f, 70f, t / 0.28f);
        float saw = 2f * (f * t - Mathf.Floor(f * t + 0.5f));
        return (saw * 0.8f + Noise() * 0.2f) * Env(t, 0.28f, 2f);
    });

    // Pickup: two rising sine notes.
    static AudioClip MakePickup() => Bake("pickup", 0.16f, t =>
    {
        float f = t < 0.08f ? 660f : 990f;
        return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t % 0.08f, 0.08f, 1.5f) * 0.8f;
    });

    // Place: low thunk (sine) + tiny click.
    static AudioClip MakePlace() => Bake("place", 0.12f, t =>
    {
        float thunk = Mathf.Sin(2f * Mathf.PI * 120f * t) * Env(t, 0.12f, 2f);
        float click = t < 0.01f ? Noise() * 0.5f : 0f;
        return thunk * 0.9f + click;
    });

    // Demolish: crumble noise, slower decay.
    static AudioClip MakeDemolish() => Bake("demolish", 0.22f, t =>
        Noise() * Env(t, 0.22f, 1.6f) * Mathf.Sin(2f * Mathf.PI * 9f * t + 1f) * 0.9f);

    // Wave horn: two detuned saws swelling then dying — the "they're coming" call.
    static AudioClip MakeWaveHorn() => Bake("waveHorn", 0.9f, t =>
    {
        float swell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.9f));
        float a = 2f * (98f  * t - Mathf.Floor(98f  * t + 0.5f));
        float b = 2f * (103f * t - Mathf.Floor(103f * t + 0.5f));
        return (a + b) * 0.4f * swell;
    });

    // Unlock: quick major arpeggio.
    static AudioClip MakeUnlock() => Bake("unlock", 0.42f, t =>
    {
        float f = t < 0.14f ? 523f : t < 0.28f ? 659f : 784f;   // C5 E5 G5
        float lt = t % 0.14f;
        return Mathf.Sin(2f * Mathf.PI * f * t) * Env(lt, 0.14f, 1.4f) * 0.7f;
    });

    // Hub hit: deep boom.
    static AudioClip MakeHubHit() => Bake("hubHit", 0.3f, t =>
    {
        float f = Mathf.Lerp(90f, 45f, t / 0.3f);
        return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.85f + Noise() * 0.15f) * Env(t, 0.3f, 2f);
    });

    // UI tick.
    static AudioClip MakeUIClick() => Bake("uiClick", 0.035f, t =>
        Mathf.Sin(2f * Mathf.PI * 1800f * t) * Env(t, 0.035f, 2f) * 0.7f);

    // Warning: short siren chirp used in the prep countdown.
    static AudioClip MakeWarning() => Bake("warning", 0.35f, t =>
    {
        float f = Mathf.Lerp(420f, 780f, Mathf.PingPong(t * 6f, 1f));
        return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.35f, 1.2f) * 0.75f;
    });

    // Scan: rising sonar ping.
    static AudioClip MakeScan() => Bake("scan", 0.45f, t =>
    {
        float f = Mathf.Lerp(380f, 1400f, t / 0.45f);
        return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.45f, 1.8f) * 0.7f;
    });

    // Dry fire: empty click.
    static AudioClip MakeDryFire() => Bake("dryFire", 0.06f, t =>
        Noise() * Env(t, 0.06f, 5f) * 0.55f);

    // Distant alien skitter: filtered noise bursts.
    static AudioClip MakeSkitter() => Bake("skitter", 0.18f, t =>
    {
        float burst = Mathf.Sin(2f * Mathf.PI * 28f * t);
        return Noise() * Env(t, 0.18f, 2.5f) * Mathf.Abs(burst) * 0.7f;
    });

    // Alarm: harsher two-tone for final warning seconds.
    static AudioClip MakeAlarm() => Bake("alarm", 0.5f, t =>
    {
        float f = t % 0.25f < 0.125f ? 880f : 660f;
        return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.5f, 1.1f) * 0.65f;
    });

    // Looping ship-hull hum (low detuned sines + soft noise bed).
    static AudioClip MakeAmbientHum() => Bake("ambientHum", 2.0f, t =>
    {
        float a = Mathf.Sin(2f * Mathf.PI * 55f * t);
        float b = Mathf.Sin(2f * Mathf.PI * 58.5f * t);
        float c = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f;
        float bed = Noise() * 0.04f;
        return (a + b) * 0.22f + c + bed;
    });
}
