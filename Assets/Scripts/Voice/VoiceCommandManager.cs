using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class VoiceCommandManager : MonoBehaviour
{
    [SerializeField] private TTSManager ttsManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private VolumeManager volumeManager;
    [SerializeField] private QuestionManager questionManager;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private MenuController menuController;

#if UNITY_EDITOR
    [ContextMenu("Probar: iniciar preguntas y música")]
    private void StartRoundForTesting()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[VOICE] Iniciá Play para probar la ronda.", this);
            return;
        }

        menuController.StartRound();
    }
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static readonly string[] Commands =
    {
        "empezar", "volumen", "salir", "pausa", "continuar", "reanudar",
        "uno", "dos", "tres", "cuatro", "cinco",
        "seis", "siete", "ocho", "nueve", "diez",
        "volver"
    };

    private static readonly string[] AnswerCommands = { "uno", "dos", "tres", "cuatro" };

    private KeywordRecognizer keywordRecognizer;
    private MenuVoiceActions menuActions;
    private bool wasSpeaking;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (ttsManager == null)
        {
            Debug.LogError("[VOICE] Asigná TTSManager en el Inspector.", this);
            enabled = false;
            return;
        }

        if (gameManager == null)
        {
            Debug.LogError("[VOICE] Asigná GameManager en el Inspector.", this);
            enabled = false;
            return;
        }

        if (volumeManager == null)
        {
            Debug.LogError("[VOICE] Asigná VolumeManager en el Inspector.", this);
            enabled = false;
            return;
        }

        if (questionManager == null)
        {
            Debug.LogError("[VOICE] Asigná QuestionManager en el Inspector.", this);
            enabled = false;
            return;
        }

        if (timerManager == null)
        {
            Debug.LogError("[VOICE] Asigná TimerManager en el Inspector.", this);
            enabled = false;
            return;
        }

        if (menuController == null) menuController = GetComponent<MenuController>();
        if (menuController == null)
        {
            Debug.LogError("[VOICE] Asigná MenuController en el Inspector.", this);
            enabled = false;
            return;
        }

        if (!PhraseRecognitionSystem.isSupported)
        {
            Debug.LogError("[VOICE] El reconocimiento de voz no está disponible en este equipo.", this);
            enabled = false;
            return;
        }

        try
        {
            menuActions = new MenuVoiceActions(menuController);
            keywordRecognizer = new KeywordRecognizer(Commands);
            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
            wasSpeaking = ttsManager.IsSpeaking;

            if (!wasSpeaking)
            {
                keywordRecognizer.Start();
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[VOICE] No se pudo iniciar el reconocedor: {exception.Message}", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (keywordRecognizer == null)
        {
            return;
        }

        bool isSpeaking = ttsManager.IsSpeaking;
        if (isSpeaking == wasSpeaking)
        {
            return;
        }

        wasSpeaking = isSpeaking;

        if (isSpeaking)
        {
            if (keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }
        }
        else
        {
            try
            {
                if (!keywordRecognizer.IsRunning)
                {
                    keywordRecognizer.Start();
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[VOICE] No se pudo reanudar el reconocedor: {exception.Message}", this);
                enabled = false;
            }
        }
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (wasSpeaking || ttsManager.IsSpeaking)
        {
            return;
        }

        string command = args.text.ToLowerInvariant();

        if (gameManager.CurrentState == GameState.Playing)
        {
            if (command == "pausa")
            {
                menuActions.Handle(command);
                return;
            }

            int answerIndex = System.Array.IndexOf(AnswerCommands, command);
            if (answerIndex >= 0)
            {
                questionManager.AnswerQuestion(answerIndex + 1);
            }

            return;
        }

        menuActions.Handle(command);
    }

    private void OnDisable()
    {
        if (keywordRecognizer == null)
        {
            return;
        }

        keywordRecognizer.OnPhraseRecognized -= OnPhraseRecognized;

        if (keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
        }

        keywordRecognizer.Dispose();
        keywordRecognizer = null;
    }
#else
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[VOICE] KeywordRecognizer solo está disponible en Windows.", this);
        }
    }
#endif
}
