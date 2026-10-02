using System.Text;
using UnityEngine;

public class TTSManager : MonoBehaviour
{
    private const float EndQuietSeconds = 0.75f;

    [SerializeField] private VolumeManager volumeManager;

    private readonly StringBuilder statusBuffer = new StringBuilder(256);
    private string spanishLanguageId;
    private bool voiceLookupComplete;
    private bool speaking;
    private bool blocksTraversal = true;
    private bool nativeSpeechObserved;
    private float minimumSpeechEndAt;
    private float fallbackSpeechEndAt;
    private float lastNativeSpeakingAt;
    private int lastStatusFrame = -1;

    private void Awake()
    {
        if (volumeManager == null)
        {
            volumeManager = GetComponent<VolumeManager>();
        }

        if (volumeManager == null)
        {
            Debug.LogWarning("[TTS] Falta VolumeManager: la voz usará el volumen máximo.", this);
        }
    }

    private void Start()
    {
        FindSpanishVoice();
    }

    private void FindSpanishVoice()
    {
        if (voiceLookupComplete)
        {
            return;
        }

        voiceLookupComplete = true;
        if (SpanishSapiVoice.TryFind(out spanishLanguageId, out string voiceName))
        {
            Debug.Log($"[TTS] Voz española disponible: {voiceName} ({spanishLanguageId}).", this);
        }
        else
        {
            Debug.LogWarning("[TTS] No se encontró una voz SAPI en español; se usará la voz predeterminada de Windows.", this);
        }
    }

    public bool IsSpeaking
    {
        get
        {
            RefreshSpeakingState();
            return speaking;
        }
    }

    public bool IsBlockingTraversal => IsSpeaking && blocksTraversal;

    private void RefreshSpeakingState()
    {
        // Several systems ask in one frame. They must all receive the same answer.
        if (lastStatusFrame == Time.frameCount)
        {
            return;
        }

        lastStatusFrame = Time.frameCount;
        if (WindowsVoice.theVoice == null)
        {
            SetSpeaking(false);
            return;
        }

        statusBuffer.Clear();
        WindowsVoice.statusMessage(statusBuffer, statusBuffer.Capacity);
        bool nativeSpeaking = statusBuffer.ToString().StartsWith(
            "Speaking:", System.StringComparison.OrdinalIgnoreCase);
        float now = Time.realtimeSinceStartup;

        if (nativeSpeaking)
        {
            nativeSpeechObserved = true;
            lastNativeSpeakingAt = now;
            SetSpeaking(true);
            return;
        }

        if (!speaking)
        {
            return;
        }

        // The plugin can miss the start altogether. Hold the request for an
        // estimated utterance length until native activity is observed.
        bool finished = nativeSpeechObserved
            ? now >= minimumSpeechEndAt && now - lastNativeSpeakingAt >= EndQuietSeconds
            : now >= fallbackSpeechEndAt;
        if (finished)
        {
            SetSpeaking(false);
        }
    }

    private void SetSpeaking(bool value)
    {
        if (speaking == value)
        {
            return;
        }

        speaking = value;
#if UNITY_EDITOR
        Debug.Log($"[TTS] IsSpeaking = {value}", this);
#endif
    }

    public void Speak(string text, bool blocksTraversal = true)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (WindowsVoice.theVoice == null)
        {
            Debug.LogWarning("TTSManager: agregá y activá el componente WindowsVoice antes de hablar.");
            return;
        }

        FindSpanishVoice();
        int speechVolume = (volumeManager != null ? volumeManager.CurrentLevel : 10) * 10;
        string speechText = $"<volume level=\"{speechVolume}\">{EscapeForSpeechXml(text)}</volume>";
        if (spanishLanguageId != null)
        {
            speechText = $"<lang langid=\"{spanishLanguageId}\">{speechText}</lang>";
        }

        long requiredCapacity = (long)speechText.Length * 4 + 64;
        if (requiredCapacity > int.MaxValue)
        {
            Debug.LogWarning("TTSManager: el texto es demasiado largo para consultar el estado de la voz.");
            return;
        }

        statusBuffer.EnsureCapacity((int)requiredCapacity);
        this.blocksTraversal = blocksTraversal;
        WindowsVoice.speak(speechText);
        float now = Time.realtimeSinceStartup;
        float estimatedSeconds = EstimateSpeechSeconds(text);
        minimumSpeechEndAt = now + estimatedSeconds * 0.6f;
        fallbackSpeechEndAt = now + estimatedSeconds;
        lastNativeSpeakingAt = now;
        nativeSpeechObserved = false;
        SetSpeaking(true);
    }

    private static float EstimateSpeechSeconds(string text)
    {
        int words = 0;
        int pauses = 0;
        bool insideWord = false;
        foreach (char character in text)
        {
            bool isWordCharacter = char.IsLetterOrDigit(character);
            if (isWordCharacter && !insideWord)
            {
                words++;
            }

            insideWord = isWordCharacter;
            if (character == '.' || character == ',' || character == '?' || character == '!' || character == ';')
            {
                pauses++;
            }
        }

        return Mathf.Clamp(0.4f + words * 0.42f + pauses * 0.18f, 1f, 30f);
    }

    private static string EscapeForSpeechXml(string text)
    {
        return System.Security.SecurityElement.Escape(text);
    }

#if UNITY_EDITOR
    [ContextMenu("Probar: bienvenida hablada")]
    private void TestWelcome()
    {
        Speak("Bienvenido a Acoustic Sound");
    }
#endif
}
