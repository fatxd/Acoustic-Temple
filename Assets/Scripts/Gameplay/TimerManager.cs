using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    private const float InitialTime = 180f;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private QuestionManager questionManager;
    [SerializeField] private TTSManager ttsManager;
    [SerializeField] private AudioSource warningAudioSource;
    [SerializeField] private AudioClip warningClip;
    [SerializeField] private GameSfxManager gameSfxManager;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField, Min(0f)] private float currentTime;
    

    private bool timerActive;
    private bool warnedAt30;
    private bool warnedAt10;
    private bool spokeThree;
    private bool spokeTwo;
    private bool spokeOne;
    private int displayedSeconds = -1;

    public float CurrentTime => currentTime;

    private void Awake()
    {
        if (gameSfxManager == null)
        {
            gameSfxManager = GetComponent<GameSfxManager>();
        }

        currentTime = InitialTime;
        RefreshTimerUI();
    }

    public void StartTimer()
    {
        if (gameManager == null || questionManager == null || ttsManager == null)
        {
            Debug.LogError("[TIMER] Asigná GameManager, QuestionManager y TTSManager en el Inspector.", this);
            return;
        }

        currentTime = InitialTime;
        timerActive = true;
        warnedAt30 = false;
        warnedAt10 = false;
        spokeThree = false;
        spokeTwo = false;
        spokeOne = false;
        RefreshTimerUI();
    }

    public void StopTimer()
    {
        timerActive = false;
    }

    private void Update()
    {
        if (!timerActive)
        {
            return;
        }

        if (gameManager.IsPaused)
        {
            return;
        }

        if (gameManager.CurrentState != GameState.Playing || !questionManager.IsQuestionActive)
        {
            StopTimer();
            return;
        }

        float previousTime = currentTime;
        currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
        RefreshTimerUI();

        if (!warnedAt30 && previousTime > 30f && currentTime <= 30f)
        {
            warnedAt30 = true;
            PlayWarning();
        }

        if (!warnedAt10 && previousTime > 10f && currentTime <= 10f)
        {
            warnedAt10 = true;
            PlayWarning();
        }

        if (!spokeThree && previousTime > 3f && currentTime <= 3f)
        {
            spokeThree = true;
            ttsManager.Speak("Tres.");
        }

        if (!spokeTwo && previousTime > 2f && currentTime <= 2f)
        {
            spokeTwo = true;
            ttsManager.Speak("Dos.");
        }

        if (!spokeOne && previousTime > 1f && currentTime <= 1f)
        {
            spokeOne = true;
            ttsManager.Speak("Uno.");
        }

        if (currentTime > 0f)
        {
            return;
        }

        StopTimer();
        questionManager.StopQuestion();
        gameManager.ChangeState(GameState.GameOver);
        Debug.Log("[TIMER] Tiempo agotado");
        ttsManager.Speak("Se terminó el tiempo. Has perdido.");
        if (gameSfxManager != null) gameSfxManager.PlayDefeatAfterSpeech(ttsManager);
    }

    private void RefreshTimerUI()
    {
        if (timerText == null) return;
        int remaining = Mathf.CeilToInt(currentTime);
        if (remaining != displayedSeconds)
        {
            displayedSeconds = remaining;
            timerText.text = $"<size=24><color=#D9B76F>TIEMPO RESTANTE</color></size>\n{remaining / 60:00}:{remaining % 60:00}";
        }
        timerText.color = currentTime <= 10f && timerActive
            ? new Color(1f, 0.38f, 0.28f)
            : currentTime <= 30f && timerActive
                ? new Color(1f, 0.72f, 0.3f)
                : new Color(1f, 0.91f, 0.72f);
        float scale = timerActive && currentTime <= 10f
            ? 1f + Mathf.Sin(Time.time * 6f) * 0.04f : 1f;
        timerText.transform.localScale = Vector3.one * scale;
    }

    private void PlayWarning()
    {
        if (warningAudioSource != null && warningClip != null)
        {
            warningAudioSource.PlayOneShot(warningClip);
        }
    }
}
