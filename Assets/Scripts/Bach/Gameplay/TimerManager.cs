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

    public float CurrentTime => currentTime;

    private void Awake()
    {
        if (gameSfxManager == null)
        {
            gameSfxManager = GetComponent<GameSfxManager>();
        }

        timerText.color = new Color(0.83f, 0.69f, 0.22f);
        timerText.transform.localScale = Vector3.one;
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

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
 
        timerText.text = $"{minutes:00}:{seconds:00}";

        float previousTime = currentTime;
        currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);

        if (!warnedAt30 && previousTime > 30f && currentTime <= 30f)
        {
            timerText.color = new Color(1f, 0.65f, 0f);
            warnedAt30 = true;
            PlayWarning();
        }

        if (!warnedAt10 && previousTime > 10f && currentTime <= 10f)
        {
            timerText.color = Color.red;
            float scale = 1f + Mathf.Sin(Time.time * 8f) * 0.15f;
            timerText.transform.localScale = Vector3.one * scale;
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

    private void PlayWarning()
    {
        if (warningAudioSource != null && warningClip != null)
        {
            warningAudioSource.PlayOneShot(warningClip);
        }
    }
}
