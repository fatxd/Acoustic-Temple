using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asegúrate de usar TextMeshPro

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

    private void Awake()
    {
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

                // Configurar el click del botón hacia el QuestionManager
                int optionNumber = i + 1; // Las opciones en QuestionManager van de 1 a 4
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(optionNumber));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
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

        yield return new WaitForSeconds(duration);

        feedbackText.gameObject.SetActive(false);
    }

    public void HideUI()
    {
        if (questionPanel != null)
            questionPanel.SetActive(false);
    }
}