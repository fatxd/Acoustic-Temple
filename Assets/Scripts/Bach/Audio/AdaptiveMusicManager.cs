using UnityEngine;

/// <summary>
/// Plays seven equal-length Reaper stems in sync. Progress changes rhythm;
/// lost lives and low time increase danger without slowing the music.
/// Loop stems must be exported from bar 1 to bar 9 at 96 BPM, including silence.
/// </summary>
public class AdaptiveMusicManager : MonoBehaviour
{
    private const float TempoBpm = 96f;
    private const float BeatSeconds = 60f / TempoBpm;
    private const double BarSeconds = 4d * 60d / TempoBpm;

    private static readonly string[] ClipNames =
    {
        "01_drone", "02_pad", "03_bells", "04_slow_drums",
        "05_eighth_pulse", "06_fast_percussion", "07_danger_texture"
    };

    [SerializeField] private GameManager gameManager;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private QuestionManager questionManager;
    [SerializeField] private TTSManager ttsManager;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.48f;
    [SerializeField, Min(0.1f)] private float layerFadeSeconds = 0.8f;

    private readonly AudioSource[] layers = new AudioSource[7];
    private readonly float[] layerLevels = new float[7];
    private int correctAnswers;
    private int remainingLives = 3;
    private int appliedRhythmStage;
    private int pendingRhythmStage;
    private int lastBarIndex = -1;
    private double loopStartDspTime;
    private float rhythmDropUntil;
    private bool playing;
    private bool clipsReady;
    private bool paused;
    private double pausedAtDspTime;

    private void Awake()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (timerManager == null) timerManager = GetComponent<TimerManager>();
        if (questionManager == null) questionManager = GetComponent<QuestionManager>();
        if (ttsManager == null) ttsManager = GetComponent<TTSManager>();

        clipsReady = true;
        for (int i = 0; i < layers.Length; i++)
        {
            AudioClip clip = Resources.Load<AudioClip>("AdaptiveMusic/" + ClipNames[i]);
            if (clip == null)
            {
                Debug.LogError("[MUSIC] Falta el stem " + ClipNames[i] + ".", this);
                clipsReady = false;
                continue;
            }

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            layers[i] = source;
        }

        if (!clipsReady || gameManager == null || timerManager == null || questionManager == null)
        {
            Debug.LogError("[MUSIC] Faltan clips o referencias para la música adaptativa.", this);
            enabled = false;
        }
    }

    public void ResetForRound()
    {
        correctAnswers = 0;
        remainingLives = 3;
        appliedRhythmStage = 0;
        pendingRhythmStage = 0;
        lastBarIndex = -1;
        rhythmDropUntil = 0f;
        foreach (AudioSource layer in layers)
        {
            if (layer != null) layer.Stop();
        }
        System.Array.Clear(layerLevels, 0, layerLevels.Length);
        playing = false;
        paused = false;
    }

    public void StopForMenu()
    {
        foreach (AudioSource layer in layers)
        {
            if (layer != null) layer.Stop();
        }
        System.Array.Clear(layerLevels, 0, layerLevels.Length);
        playing = false;
        paused = false;
    }

    public void ReportCorrectAnswer()
    {
        correctAnswers++;
        Debug.Log("[MUSIC] Pregunta superada: aumenta el pulso.", this);
    }

    public void ReportLifeLost(int livesLeft)
    {
        remainingLives = Mathf.Clamp(livesLeft, 0, 3);
        rhythmDropUntil = Time.time + BeatSeconds;
        Debug.Log($"[MUSIC] Vida perdida: {remainingLives} restantes; sube la tensión.", this);
    }

    private void Update()
    {
        if (gameManager.IsPaused)
        {
            if (!paused)
            {
                paused = true;
                pausedAtDspTime = AudioSettings.dspTime;
            }
            return;
        }

        if (paused)
        {
            loopStartDspTime += AudioSettings.dspTime - pausedAtDspTime;
            paused = false;
        }

        bool active = gameManager.CurrentState == GameState.Playing && questionManager.IsQuestionActive;
        if (active && !playing)
        {
            // All stems start on the same DSP sample, so fades never shift the beat.
            loopStartDspTime = AudioSettings.dspTime + 0.1;
            foreach (AudioSource layer in layers) layer.PlayScheduled(loopStartDspTime);
            appliedRhythmStage = 0;
            pendingRhythmStage = 0;
            lastBarIndex = -1;
            playing = true;
        }

        if (active)
        {
            pendingRhythmStage = Mathf.Max(pendingRhythmStage,
                DesiredRhythmStage(timerManager.CurrentTime));
            // Percussion changes on a downbeat; danger and voice ducking react immediately.
            if (AudioSettings.dspTime >= loopStartDspTime)
            {
                int barIndex = (int)System.Math.Floor(
                    (AudioSettings.dspTime - loopStartDspTime) / BarSeconds);
                if (barIndex > lastBarIndex)
                {
                    if (barIndex > 0 && pendingRhythmStage > appliedRhythmStage)
                    {
                        appliedRhythmStage = pendingRhythmStage;
                        Debug.Log($"[MUSIC] Ritmo nivel {appliedRhythmStage}.", this);
                    }
                    lastBarIndex = barIndex;
                }
            }
        }

        bool voiceSpeaking = ttsManager != null && ttsManager.IsSpeaking;
        float danger = DangerLevel(timerManager.CurrentTime);
        bool allSilent = true;
        for (int i = 0; i < layers.Length; i++)
        {
            float target = 0f;
            if (active)
            {
                target = LayerTarget(i, appliedRhythmStage, danger);
                if (i >= 3 && i <= 5 && Time.time < rhythmDropUntil) target *= 0.12f;
                if (voiceSpeaking) target *= i == 2 ? 0.14f : i >= 3 ? 0.28f : 0.42f;
            }

            float fadeSeconds = i >= 3 && i <= 5 && Time.time < rhythmDropUntil
                ? 0.15f : voiceSpeaking ? 0.2f : layerFadeSeconds;
            layerLevels[i] = Mathf.MoveTowards(layerLevels[i], target,
                Time.deltaTime / fadeSeconds);
            layers[i].volume = layerLevels[i] * musicVolume;
            if (layerLevels[i] > 0.001f) allSilent = false;
        }

        if (!active && playing && allSilent)
        {
            foreach (AudioSource layer in layers) layer.Stop();
            playing = false;
        }
    }

    private int DesiredRhythmStage(float secondsLeft)
    {
        int progress = Mathf.Clamp(correctAnswers, 0, 2);
        int time = secondsLeft <= 25f ? 3 : secondsLeft <= 60f ? 2
            : secondsLeft <= 120f ? 1 : 0;
        int lastLife = remainingLives <= 1 ? 2 : 0;
        return Mathf.Max(progress, time, lastLife);
    }

    private float DangerLevel(float secondsLeft)
    {
        float time = secondsLeft <= 25f ? 0.35f : secondsLeft <= 60f ? 0.22f
            : secondsLeft <= 120f ? 0.1f : 0f;
        return Mathf.Clamp01(0.08f + (3 - remainingLives) * 0.35f + time);
    }

    private static float LayerTarget(int index, int rhythmStage, float danger)
    {
        switch (index)
        {
            case 0: return 0.78f; // Drone.
            case 1: return 0.82f; // Dark harmony.
            case 2: return 0.25f; // Sparse background bells.
            case 3: return rhythmStage == 0 ? 0.38f : rhythmStage == 1 ? 0.65f : 0.85f;
            case 4: return rhythmStage == 0 ? 0f : rhythmStage == 1 ? 0.60f : 0.78f;
            case 5: return rhythmStage < 2 ? 0f : rhythmStage == 2 ? 0.50f : 0.76f;
            case 6: return danger;
            default: return 0f;
        }
    }

    private void OnDisable()
    {
        foreach (AudioSource layer in layers)
        {
            if (layer != null) layer.Stop();
        }
        System.Array.Clear(layerLevels, 0, layerLevels.Length);
        playing = false;
    }
}
