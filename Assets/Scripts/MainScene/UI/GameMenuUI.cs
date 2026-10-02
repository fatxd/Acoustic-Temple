using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>Connects the editable Main Scene panels to the game menu actions.</summary>
public sealed class GameMenuUI : MonoBehaviour
{
    [Header("Game logic")]
    [SerializeField] private MenuController menu;

    [Header("Screens")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject volumePanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Main menu")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button mainVolumeButton;
    [SerializeField] private Button mainExitButton;

    [Header("Pause menu")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button pauseReturnButton;
    [SerializeField] private Button pauseVolumeButton;
    [SerializeField] private Button pauseExitButton;

    [Header("Volume menu")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValue;
    [SerializeField] private TextMeshProUGUI volumeSubtitle;
    [SerializeField] private Button volumeLessButton;
    [SerializeField] private Button volumeMoreButton;
    [SerializeField] private Button volumeBackButton;

    [Header("Result and HUD")]

    [SerializeField] private Image resultImage;

    [SerializeField] private Sprite victorySprite;

    [SerializeField] private Sprite gameOverSprite;
    [SerializeField] private TextMeshProUGUI resultTitle;
    [SerializeField] private Button resultReturnButton;
    [SerializeField] private Button resultExitButton;
    [SerializeField] private Button pauseButton;

    private GameState shownState = (GameState)(-1);
    private GameSfxManager sfxManager;

    private void Awake()
    {
        if (menu == null || mainMenuPanel == null || pausePanel == null
            || volumePanel == null || resultPanel == null || hudPanel == null
            || startButton == null || mainVolumeButton == null || mainExitButton == null
            || continueButton == null || pauseReturnButton == null
            || pauseVolumeButton == null || pauseExitButton == null
            || volumeSlider == null || volumeValue == null || volumeSubtitle == null
            || volumeLessButton == null || volumeMoreButton == null || volumeBackButton == null
            || resultTitle == null || resultReturnButton == null || resultExitButton == null
            || pauseButton == null || resultImage == null || victorySprite == null || gameOverSprite == null)
        {
            Debug.LogError("[UI] Faltan referencias en los paneles de Main Scene.", this);
            enabled = false;
            return;
        }

        startButton.onClick.AddListener(menu.StartRound);
        mainVolumeButton.onClick.AddListener(menu.OpenVolume);
        mainExitButton.onClick.AddListener(menu.QuitGame);
        continueButton.onClick.AddListener(menu.ResumeRound);
        pauseReturnButton.onClick.AddListener(menu.ReturnToMainMenu);
        pauseVolumeButton.onClick.AddListener(menu.OpenVolume);
        pauseExitButton.onClick.AddListener(menu.QuitGame);
        volumeLessButton.onClick.AddListener(() => ChangeVolume(-1));
        volumeMoreButton.onClick.AddListener(() => ChangeVolume(1));
        volumeBackButton.onClick.AddListener(menu.CloseVolume);
        volumeSlider.onValueChanged.AddListener(value =>
            menu.SetVolumeFromUI(Mathf.RoundToInt(value)));
        resultReturnButton.onClick.AddListener(menu.ReturnToMainMenu);
        resultExitButton.onClick.AddListener(menu.QuitGame);
        pauseButton.onClick.AddListener(menu.PauseRound);

        menu.GameManager.StateChanged += ShowState;
    }

    private void Start()
    {
        if (!enabled) return;
        sfxManager = menu.SfxManager;
        if (sfxManager != null) sfxManager.VictoryCueFinished += ShowVictoryResult;
        ShowState(menu.GameManager.CurrentState);
    }

    private void Update()
    {
        if (shownState == GameState.VolumeMenu || shownState == GameState.PausedVolumeMenu)
            RefreshVolume();
    }

    private void OnDestroy()
    {
        if (sfxManager != null) sfxManager.VictoryCueFinished -= ShowVictoryResult;
        if (menu != null && menu.GameManager != null)
            menu.GameManager.StateChanged -= ShowState;
    }

    private void ShowState(GameState state)
    {
        mainMenuPanel.SetActive(state == GameState.MainMenu);
        pausePanel.SetActive(state == GameState.Paused);
        volumePanel.SetActive(state == GameState.VolumeMenu
            || state == GameState.PausedVolumeMenu);
        resultPanel.SetActive(state == GameState.GameOver
            || (state == GameState.Victory && sfxManager == null));
        hudPanel.SetActive(state == GameState.Playing);

        if (state == GameState.VolumeMenu || state == GameState.PausedVolumeMenu)
        {
            volumeSubtitle.text = state == GameState.PausedVolumeMenu
                ? "Ajustá el sonido sin salir de la partida"
                : "Elegí el nivel de sonido del juego";
            RefreshVolume();
        }

        if (state == GameState.Victory || state == GameState.GameOver)
            resultTitle.text = state == GameState.Victory
                ? "¡TEMPLO COMPLETADO!" : "FIN DE LA PARTIDA";

        if (state == GameState.Victory || state == GameState.GameOver)
            resultImage.sprite = state == GameState.Victory 
                ? victorySprite : gameOverSprite;     
        shownState = state;
        if (EventSystem.current != null)
        {
            GameObject selected = state == GameState.MainMenu ? startButton.gameObject
                : state == GameState.Paused ? continueButton.gameObject
                : state == GameState.VolumeMenu || state == GameState.PausedVolumeMenu ? volumeSlider.gameObject
                : state == GameState.GameOver ? resultReturnButton.gameObject : null;
            EventSystem.current.SetSelectedGameObject(selected);
        }
    }

    private void ShowVictoryResult()
    {
        if (menu.GameManager.CurrentState == GameState.Victory)
        {
            resultPanel.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resultReturnButton.gameObject);
        }
    }

    private void ChangeVolume(int step)
    {
        menu.SetVolumeFromUI(menu.VolumeManager.CurrentLevel + step);
        RefreshVolume();
    }

    private void RefreshVolume()
    {
        int level = menu.VolumeManager.CurrentLevel;
        volumeSlider.SetValueWithoutNotify(level);
        volumeValue.text = $"{level} / 10";
    }
}
