using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class QuestionUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI[] optionTexts; // Asigna 4 elementos en el Inspector
    [SerializeField] private Button[] optionButtons;       // Asigna 4 botones en el Inspector
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Colores de Feedback")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color normalOptionColor = Color.white;

    [Header("Referencias del Sistema")]
    [SerializeField] private QuestionManager questionManager;

    private Coroutine feedbackCoroutine;
    private UnityAction[] answerActions;
    private Vector3 feedbackScale = Vector3.one;
    private static readonly Color ReadingColor = new Color(1f, 0.82f, 0.43f);

    public void SetReadingSegment(int optionIndex)
    {
        if (!enabled) return;
        if (questionText != null)
            questionText.color = optionIndex == -1 ? ReadingColor : normalOptionColor;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = false;
            if (i < optionTexts.Length)
                optionTexts[i].color = i == optionIndex ? ReadingColor : normalOptionColor;
        }
    }

    public void FinishReading()
    {
        if (!enabled) return;
        SetReadingSegment(4);
        foreach (Button button in optionButtons) button.interactable = true;
    }

    private void Awake()
    {
        if (questionManager == null || optionButtons == null || optionTexts == null
            || optionButtons.Length != 4 || optionTexts.Length != 4)
        {
            Debug.LogError("[UI] Asigná QuestionManager y las cuatro respuestas y textos.", this);
            enabled = false;
            return;
        }
        answerActions = new UnityAction[optionButtons.Length];
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null || optionTexts[i] == null)
            {
                Debug.LogError("[UI] Falta una referencia de respuesta.", this);
                enabled = false;
                return;
            }
            int optionNumber = i + 1;
            answerActions[i] = () => OnOptionClicked(optionNumber);
            optionButtons[i].onClick.AddListener(answerActions[i]);
        }
        if (feedbackText != null) feedbackScale = feedbackText.transform.localScale;
        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Muestra la pregunta actual y sus 4 opciones en la UI.
    /// </summary>
    public void DisplayQuestion(QuestionManager.QuestionData question, int questionIndex)
    {
        if (!enabled) return;
        ClearFeedback();
        if (questionPanel != null) questionPanel.SetActive(true);

        // Resetear texto de feedback previo
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        // Asignar texto de la pregunta
        if (questionText != null)
            questionText.text = $"Pregunta {questionIndex + 1}: {question.text}";

        // Asignar texto a cada opción
        string[] options = { question.option1, question.option2, question.option3, question.option4 };

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                
                // Resetear color del botón/texto
                if (optionTexts != null && i < optionTexts.Length)
                {
                    optionTexts[i].text = $"{i + 1}. {options[i]}";
                    optionTexts[i].color = normalOptionColor;
                }

            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
        SetReadingSegment(-1);
    }

    private void OnOptionClicked(int optionIndex)
    {
        if (questionManager != null)
        {
            questionManager.AnswerQuestion(optionIndex);
        }
    }

    /// <summary>
    /// Muestra un mensaje flotante de feedback (Correcto / Incorrecto).
    /// </summary>
    public void ShowFeedback(string message, bool isCorrect, float duration = 2.5f)
    {
        if (!enabled) return;
        SetReadingSegment(4);
        if (feedbackText == null) return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(message, isCorrect, duration));
    }

    private IEnumerator ShowFeedbackRoutine(string message, bool isCorrect, float duration)
    {
        feedbackText.text = message;
        feedbackText.color = isCorrect ? correctColor : incorrectColor;
        feedbackText.gameObject.SetActive(true);

        Color feedbackColor = feedbackText.color;
        float elapsed = 0f;
        duration = Mathf.Max(0.3f, duration);
        while (elapsed < duration)
        {
            float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.16f));
            feedbackText.transform.localScale = feedbackScale * Mathf.Lerp(0.92f, 1f, entrance);
            feedbackColor.a = Mathf.Min(entrance, Mathf.Clamp01((duration - elapsed) / 0.25f));
            feedbackText.color = feedbackColor;
            elapsed += Time.deltaTime;
            yield return null;
        }
        feedbackText.transform.localScale = feedbackScale;
        feedbackText.gameObject.SetActive(false);
        feedbackCoroutine = null;
    }

    public void HideUI()
    {
        ClearFeedback();
        if (questionPanel != null)
            questionPanel.SetActive(false);
    }

    private void ClearFeedback()
    {
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = null;
        if (feedbackText == null) return;
        feedbackText.transform.localScale = feedbackScale;
        feedbackText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (answerActions == null) return;
        for (int i = 0; i < answerActions.Length; i++)
            if (optionButtons[i] != null && answerActions[i] != null)
                optionButtons[i].onClick.RemoveListener(answerActions[i]);
    }
}
