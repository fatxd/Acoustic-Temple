using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        public string text;
        public string option1;
        public string option2;
        public string option3;
        public string option4;
        [Range(1, 4)] public int correctOption = 1;
    }
    [SerializeField] private QuestionUI questionUI;

    [SerializeField] private TTSManager ttsManager;
    [SerializeField] private LivesManager livesManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private AdaptiveMusicManager adaptiveMusicManager;
    [SerializeField] private GameSfxManager gameSfxManager;
    [SerializeField] private List<QuestionData> questions = new List<QuestionData>
    {
        new QuestionData
        {
            text = "¿Cuál es el planeta más cercano al Sol?",
            option1 = "Venus",
            option2 = "Mercurio",
            option3 = "Marte",
            option4 = "Tierra",
            correctOption = 2
        },
        new QuestionData
        {
            text = "¿Cuántos días tiene una semana?",
            option1 = "Cinco",
            option2 = "Seis",
            option3 = "Siete",
            option4 = "Ocho",
            correctOption = 3
        },
        new QuestionData
        {
            text = "¿Qué animal maúlla?",
            option1 = "Gato",
            option2 = "Perro",
            option3 = "Vaca",
            option4 = "Caballo",
            correctOption = 1
        }
    };

    private int currentQuestionIndex;
    private bool questionActive;
    private bool acceptingAnswer;
    private bool doorMode;
    private bool waitingForGoal;
    private bool currentDoorPrompted;

    public bool IsQuestionActive => questionActive;
    public event Action<int> CorrectAnswerAtDoor;
    public event Action<int> IncorrectAnswerAtDoor;

    public void EnableDoorMode() => doorMode = true;

    private void Awake()
    {
        if (ttsManager == null)
        {
            ttsManager = GetComponent<TTSManager>();
        }

        if (livesManager == null)
        {
            livesManager = GetComponent<LivesManager>();
        }

        if (adaptiveMusicManager == null)
        {
            adaptiveMusicManager = GetComponent<AdaptiveMusicManager>();
        }

        if (gameSfxManager == null)
        {
            gameSfxManager = GetComponent<GameSfxManager>();
        }
    }

    public void StartQuestion()
    {
        if (ttsManager == null)
        {
            Debug.LogError("[QUESTION] Asigná TTSManager en el Inspector.", this);
            return;
        }

        if (livesManager == null || gameManager == null)
        {
            Debug.LogError("[QUESTION] Asigná LivesManager y GameManager en el Inspector.", this);
            return;
        }

        livesManager.ResetLives();
        if (adaptiveMusicManager != null) adaptiveMusicManager.ResetForRound();
        currentQuestionIndex = 0;
        questionActive = questions != null && questions.Count > 0;
        acceptingAnswer = false;
        waitingForGoal = false;
        currentDoorPrompted = false;

        if (!questionActive)
        {
            CompleteQuestions("");
            return;
        }

        if (!doorMode) ReadCurrentQuestion("");
    }

    public void BeginQuestionAtDoor(int doorIndex)
    {
        if (!doorMode || !questionActive || waitingForGoal || currentDoorPrompted
            || gameManager.CurrentState != GameState.Playing || doorIndex != currentQuestionIndex)
            return;

        currentDoorPrompted = true;
        ReadCurrentQuestion("");
    }

    public void ReachGoal()
    {
        if (doorMode && questionActive && waitingForGoal
            && gameManager.CurrentState == GameState.Playing)
            CompleteQuestions("", "Has ganado.");
    }

    public void AnswerQuestion(int option)
    {
        if (!questionActive || !acceptingAnswer || gameManager.CurrentState != GameState.Playing
            || option < 1 || option > 4 || ttsManager.IsSpeaking)
        {
            return;
        }

        acceptingAnswer = false;

        if (option == questions[currentQuestionIndex].correctOption)
        {
            HandleCorrectAnswer();
        }
        else
        {
            HandleIncorrectAnswer();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Probar: respuesta correcta")]
    private void TestCorrectAnswer()
    {
        if (!Application.isPlaying || !questionActive || ttsManager.IsSpeaking)
        {
            Debug.LogWarning("[QUESTION] Esperá a que termine la voz durante Play para probar una respuesta.", this);
            return;
        }

        AnswerQuestion(questions[currentQuestionIndex].correctOption);
    }

    [ContextMenu("Probar: respuesta incorrecta")]
    private void TestIncorrectAnswer()
    {
        if (!Application.isPlaying || !questionActive || ttsManager.IsSpeaking)
        {
            Debug.LogWarning("[QUESTION] Esperá a que termine la voz durante Play para probar una respuesta.", this);
            return;
        }

        AnswerQuestion(questions[currentQuestionIndex].correctOption % 4 + 1);
    }
#endif

    private void HandleCorrectAnswer()
    {
        Debug.Log("[QUESTION] Respuesta correcta");
        if (adaptiveMusicManager != null) adaptiveMusicManager.ReportCorrectAnswer();
        if (gameSfxManager != null) gameSfxManager.PlayCorrectAnswer();

        if (questionUI != null)
        {
            questionUI.ShowFeedback("¡Respuesta Correcta!", true);
        }

        int completedQuestionIndex = currentQuestionIndex;
        currentQuestionIndex++;
        if (doorMode)
        {
            currentDoorPrompted = false;
            waitingForGoal = currentQuestionIndex >= questions.Count;
            ttsManager.Speak(waitingForGoal
                ? "Respuesta correcta. La última puerta se abre. Avanza hasta el final."
                : "Respuesta correcta. La puerta se abre.");
            CorrectAnswerAtDoor?.Invoke(completedQuestionIndex);
            return;
        }

        if (currentQuestionIndex >= questions.Count)
        {
            CompleteQuestions("Respuesta correcta. ");
            return;
        }

        ReadCurrentQuestion("Respuesta correcta. ");
    }

    private void HandleIncorrectAnswer()
    {
        Debug.Log("[QUESTION] Respuesta incorrecta");
        livesManager.LoseLife();
        if (adaptiveMusicManager != null)
            adaptiveMusicManager.ReportLifeLost(livesManager.CurrentLives);

        if (questionUI != null)
        {
            questionUI.ShowFeedback("Respuesta Incorrecta", false);
        }

        if (livesManager.CurrentLives == 0)
        {
            StopQuestion();
            gameManager.ChangeState(GameState.GameOver);
            Debug.Log("[GAME] Game Over por vidas");
            ttsManager.Speak("Respuesta incorrecta. Te has quedado sin vidas. Has perdido.");
            if (gameSfxManager != null) gameSfxManager.PlayDefeatAfterSpeech(ttsManager);
            return;
        }

        if (gameSfxManager != null) gameSfxManager.PlayLifeLost();
        if (doorMode) IncorrectAnswerAtDoor?.Invoke(currentQuestionIndex);
        string feedback = livesManager.CurrentLives == 1
            ? "Respuesta incorrecta. Te queda una vida."
            : "Respuesta incorrecta. Te quedan dos vidas.";

        SpeakOptions(feedback + " Intenta nuevamente. ", questions[currentQuestionIndex]);
    }

    private void CompleteQuestions(string introduction, string closingText = "Has completado todas las preguntas.")
    {
        StopQuestion();
        gameManager.ChangeState(GameState.Victory);
        Debug.Log("[QUESTION] Todas las preguntas completadas");
        ttsManager.Speak(introduction + closingText);
        if (gameSfxManager != null) gameSfxManager.PlayVictoryAfterSpeech(ttsManager);
    }

    public void StopQuestion()
    {
        questionActive = false;
        acceptingAnswer = false;
        waitingForGoal = false;
        currentDoorPrompted = false;

        if (questionUI != null)
            questionUI.HideUI();
    }

    private void ReadCurrentQuestion(string introduction)
    {
        QuestionData question = questions[currentQuestionIndex];

        if (questionUI != null)
        {
            questionUI.DisplayQuestion(question, currentQuestionIndex);
        }

        SpeakOptions(introduction + $"Pregunta {currentQuestionIndex + 1}. {question.text} ", question);
    }

    private void SpeakOptions(string introduction, QuestionData question)
    {
        string text = introduction
            + $"Opción uno, {question.option1}. "
            + $"Opción dos, {question.option2}. "
            + $"Opción tres, {question.option3}. "
            + $"Opción cuatro, {question.option4}. "
            + "Di uno, dos, tres o cuatro.";

        ttsManager.Speak(text);
        acceptingAnswer = true;
    }
}
