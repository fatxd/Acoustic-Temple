using System.Collections;
using UnityEngine;

/// <summary>Plays the four independent game effects in Resources/GameSFX.</summary>
public class GameSfxManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.55f;

    private AudioSource source;
    private AudioClip lifeLost;
    private AudioClip correctAnswer;
    private AudioClip defeat;
    private AudioClip victory;
    private Coroutine pendingFinalCue;

    public event System.Action VictoryCueFinished;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = effectsVolume;

        lifeLost = Load("01_Pierde_vida");
        correctAnswer = Load("02_Respuesta_correcta");
        defeat = Load("03_Derrota");
        victory = Load("04_Victoria_trompetas");
    }

    public void PlayLifeLost() => Play(lifeLost);
    public void PlayCorrectAnswer() => Play(correctAnswer);

    public void StopAll()
    {
        if (pendingFinalCue != null)
        {
            StopCoroutine(pendingFinalCue);
            pendingFinalCue = null;
        }
        source.Stop();
    }

    public void PlayDefeatAfterSpeech(TTSManager ttsManager)
    {
        PlayAfterSpeech(ttsManager, defeat);
    }

    public void PlayVictoryAfterSpeech(TTSManager ttsManager)
    {
        PlayAfterSpeech(ttsManager, victory, true);
    }

    private void PlayAfterSpeech(TTSManager ttsManager, AudioClip clip, bool notifyVictory = false)
    {
        if (pendingFinalCue != null)
        {
            StopCoroutine(pendingFinalCue);
        }

        pendingFinalCue = StartCoroutine(WaitForSpeechThenPlay(ttsManager, clip, notifyVictory));
    }

    private IEnumerator WaitForSpeechThenPlay(TTSManager ttsManager, AudioClip clip, bool notifyVictory)
    {
        // TTSManager marks speech busy as soon as Speak queues it and releases it
        // only after the native voice has stayed quiet.
        while (ttsManager != null && ttsManager.IsSpeaking)
        {
            yield return null;
        }

        if (notifyVictory)
        {
            CameraFollow victoryCamera = FindAnyObjectByType<CameraFollow>();
            while (victoryCamera != null && victoryCamera.isActiveAndEnabled && victoryCamera.IsVictoryTransitioning)
                yield return null;
        }

        Play(clip);
        // Wait for actual playback to finish, including when the volume is muted.
        yield return null;
        while (source.isPlaying)
        {
            yield return null;
        }
        pendingFinalCue = null;
        if (notifyVictory) VictoryCueFinished?.Invoke();
    }

    private AudioClip Load(string name)
    {
        AudioClip clip = Resources.Load<AudioClip>("GameSFX/" + name);
        if (clip == null)
        {
            Debug.LogError("[SFX] Falta el efecto " + name + ".", this);
        }

        return clip;
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
        {
            source.PlayOneShot(clip);
        }
    }
}
