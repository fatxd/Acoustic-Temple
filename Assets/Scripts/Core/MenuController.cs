using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Owns the transitions between the main menu, round, pause and volume screens.</summary>
public sealed class MenuController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private QuestionManager questionManager;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private AdaptiveMusicManager musicManager;
    [SerializeField] private GameSfxManager sfxManager;
    [SerializeField] private VolumeManager volumeManager;
    [SerializeField] private TTSManager ttsManager;
    private MainSceneFlow mainSceneFlow;

    public GameManager GameManager => gameManager;
    public VolumeManager VolumeManager => volumeManager;
    public GameSfxManager SfxManager => sfxManager;

    public void AttachLevelFlow(MainSceneFlow flow) => mainSceneFlow = flow;

    private void Awake()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (questionManager == null) questionManager = GetComponent<QuestionManager>();
        if (timerManager == null) timerManager = GetComponent<TimerManager>();
        if (musicManager == null) musicManager = GetComponent<AdaptiveMusicManager>();
        if (sfxManager == null) sfxManager = GetComponent<GameSfxManager>();
        if (volumeManager == null) volumeManager = GetComponent<VolumeManager>();
        if (ttsManager == null) ttsManager = GetComponent<TTSManager>();

        if (gameManager == null || questionManager == null || timerManager == null
            || volumeManager == null || ttsManager == null)
        {
            Debug.LogError("[MENU] Faltan referencias del controlador de menú.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (enabled) ttsManager.Speak("Menú principal. Di empezar, volumen o salir.");
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        switch (gameManager.CurrentState)
        {
            case GameState.Playing: PauseRound(); break;
            case GameState.Paused: ResumeRound(); break;
            case GameState.VolumeMenu:
            case GameState.PausedVolumeMenu: CloseVolume(); break;
        }
    }

    public void StartRound()
    {
        if (gameManager.CurrentState != GameState.MainMenu) return;
        if (sfxManager != null) sfxManager.StopAll();
        if (mainSceneFlow != null) mainSceneFlow.PrepareRound();
        gameManager.ChangeState(GameState.Playing);
        timerManager.StartTimer();
        questionManager.StartQuestion();
        if (mainSceneFlow != null) mainSceneFlow.BeginTraversal();
    }

    public void PauseRound()
    {
        if (gameManager.CurrentState != GameState.Playing) return;
        gameManager.ChangeState(GameState.Paused);
        ttsManager.Speak("Juego en pausa. Di continuar, volver, volumen o salir.", false);
    }

    public void ResumeRound()
    {
        if (gameManager.CurrentState != GameState.Paused) return;
        gameManager.ChangeState(GameState.Playing);
    }

    public void ReturnToMainMenu()
    {
        GameState state = gameManager.CurrentState;
        if (state != GameState.Paused && state != GameState.PausedVolumeMenu
            && state != GameState.Victory && state != GameState.GameOver) return;

        questionManager.StopQuestion();
        timerManager.StopTimer();
        if (musicManager != null) musicManager.StopForMenu();
        if (sfxManager != null) sfxManager.StopAll();
        if (mainSceneFlow != null) mainSceneFlow.ResetForMenu();
        gameManager.ChangeState(GameState.MainMenu);
        ttsManager.Speak("Menú principal. Di empezar, volumen o salir.");
    }

    public void OpenVolume()
    {
        GameState state = gameManager.CurrentState;
        if (state == GameState.MainMenu)
            gameManager.ChangeState(GameState.VolumeMenu);
        else if (state == GameState.Paused)
            gameManager.ChangeState(GameState.PausedVolumeMenu);
        else
            return;

        ttsManager.Speak("Di un número del uno al diez para cambiar el volumen. Di volver para regresar.", false);
    }

    public void CloseVolume()
    {
        if (gameManager.CurrentState == GameState.VolumeMenu)
        {
            gameManager.ChangeState(GameState.MainMenu);
            ttsManager.Speak("Menú principal. Di empezar, volumen o salir.");
        }
        else if (gameManager.CurrentState == GameState.PausedVolumeMenu)
        {
            gameManager.ChangeState(GameState.Paused);
            ttsManager.Speak("Juego en pausa. Di continuar, volver, volumen o salir.", false);
        }
    }

    public void SetVolume(int level)
    {
        if (gameManager.CurrentState != GameState.VolumeMenu
            && gameManager.CurrentState != GameState.PausedVolumeMenu) return;

        volumeManager.SetVolume(level);
        ttsManager.Speak($"Volumen {volumeManager.CurrentLevel} de diez.", false);
    }

    public void SetVolumeFromUI(int level)
    {
        if (gameManager.CurrentState == GameState.VolumeMenu
            || gameManager.CurrentState == GameState.PausedVolumeMenu)
            volumeManager.SetVolume(level);
    }

    public void QuitGame()
    {
        if (sfxManager != null) sfxManager.StopAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Debug.Log("[MENU] Salir del juego.", this);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
