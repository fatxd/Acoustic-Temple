using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Builds Bach's screen-space menus; all buttons call the same actions as voice commands.</summary>
public sealed class BachMenuUI : MonoBehaviour
{
    [SerializeField] private BachMenuController menu;

    private static readonly Color Backdrop = new Color(0.025f, 0.045f, 0.075f, 0.88f);
    private static readonly Color Card = new Color(0.055f, 0.09f, 0.14f, 0.97f);
    private static readonly Color Gold = new Color(0.88f, 0.71f, 0.39f, 1f);
    private static readonly Color White = new Color(0.96f, 0.96f, 0.94f, 1f);
    private static readonly Color Muted = new Color(0.69f, 0.75f, 0.78f, 1f);

    private Font font;
    private GameObject mainPanel;
    private GameObject pausePanel;
    private GameObject volumePanel;
    private GameObject resultPanel;
    private GameObject pauseButton;
    private Text volumeValue;
    private Text volumeSubtitle;
    private Text resultTitle;
    private Slider volumeSlider;
    private GameState shownState = (GameState)(-1);

    private void Awake()
    {
        if (menu == null) menu = GetComponent<BachMenuController>();
        if (menu == null)
        {
            Debug.LogError("[MENU UI] Falta BachMenuController.", this);
            enabled = false;
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        BuildCanvas();
        menu.GameManager.StateChanged += ShowState;
    }

    private void Start()
    {
        ShowState(menu.GameManager.CurrentState);
    }

    private void OnDestroy()
    {
        if (menu != null && menu.GameManager != null)
            menu.GameManager.StateChanged -= ShowState;
    }

    private void ShowState(GameState state)
    {
        if (mainPanel == null) return;

        mainPanel.SetActive(state == GameState.MainMenu);
        pausePanel.SetActive(state == GameState.Paused);
        volumePanel.SetActive(state == GameState.VolumeMenu || state == GameState.PausedVolumeMenu);
        resultPanel.SetActive(state == GameState.Victory || state == GameState.GameOver);
        pauseButton.SetActive(state == GameState.Playing);

        if (state == GameState.VolumeMenu || state == GameState.PausedVolumeMenu)
        {
            volumeSubtitle.text = state == GameState.PausedVolumeMenu
                ? "Ajustá el sonido sin salir de la partida"
                : "Elegí el nivel de sonido del juego";
            RefreshVolume();
        }

        if (state == GameState.Victory || state == GameState.GameOver)
            resultTitle.text = state == GameState.Victory ? "¡TEMPLO COMPLETADO!" : "FIN DE LA PARTIDA";

        shownState = state;
    }

    private void Update()
    {
        if (shownState == GameState.VolumeMenu || shownState == GameState.PausedVolumeMenu)
            RefreshVolume();
    }

    private void RefreshVolume()
    {
        int level = menu.VolumeManager.CurrentLevel;
        volumeSlider.SetValueWithoutNotify(level);
        volumeValue.text = $"{level} / 10";
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("BachMenuCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject events = new GameObject("BachUIEventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }

        Transform root = canvasObject.transform;
        mainPanel = CreatePanel(root, "MainMenu");
        pausePanel = CreatePanel(root, "PauseMenu");
        volumePanel = CreatePanel(root, "VolumeMenu");
        resultPanel = CreatePanel(root, "ResultMenu");

        BuildMain(mainPanel.transform.GetChild(0));
        BuildPause(pausePanel.transform.GetChild(0));
        BuildVolume(volumePanel.transform.GetChild(0));
        BuildResult(resultPanel.transform.GetChild(0));

        pauseButton = CreateButton(root, "PauseButton", "PAUSA", new Vector2(-145, -64),
            new Vector2(220, 64), menu.PauseRound, false, true);
    }

    private void BuildMain(Transform panel)
    {
        CreateText(panel, "Eyebrow", "UNA EXPERIENCIA DE VOZ Y SONIDO",
            23, Gold, new Vector2(0, 296), new Vector2(660, 45));
        CreateText(panel, "Title", "TEMPLO ACÚSTICO",
            58, White, new Vector2(0, 213), new Vector2(680, 100));
        CreateText(panel, "Subtitle", "Elegí una opción o decila en voz alta",
            26, Muted, new Vector2(0, 146), new Vector2(650, 50));
        CreateButton(panel, "Start", "EMPEZAR", new Vector2(0, 32),
            new Vector2(520, 78), menu.StartRound, true);
        CreateButton(panel, "Volume", "VOLUMEN", new Vector2(0, -68),
            new Vector2(520, 78), menu.OpenVolume);
        CreateButton(panel, "Exit", "SALIR", new Vector2(0, -168),
            new Vector2(520, 78), menu.QuitGame);
        CreateText(panel, "Hint", "También podés usar el mouse",
            22, Muted, new Vector2(0, -288), new Vector2(610, 40));
    }

    private void BuildPause(Transform panel)
    {
        CreateText(panel, "Eyebrow", "PARTIDA DETENIDA",
            23, Gold, new Vector2(0, 296), new Vector2(660, 45));
        CreateText(panel, "Title", "PAUSA",
            66, White, new Vector2(0, 218), new Vector2(660, 100));
        CreateText(panel, "Subtitle", "El tiempo y la música están en pausa",
            26, Muted, new Vector2(0, 149), new Vector2(650, 50));
        CreateButton(panel, "Continue", "CONTINUAR", new Vector2(0, 56),
            new Vector2(520, 70), menu.ResumeRound, true);
        CreateButton(panel, "Return", "VOLVER AL MENÚ", new Vector2(0, -31),
            new Vector2(520, 70), menu.ReturnToMainMenu);
        CreateButton(panel, "Volume", "VOLUMEN", new Vector2(0, -118),
            new Vector2(520, 70), menu.OpenVolume);
        CreateButton(panel, "Exit", "SALIR", new Vector2(0, -205),
            new Vector2(520, 70), menu.QuitGame);
        CreateText(panel, "Hint", "Decí continuar, volver, volumen o salir",
            22, Muted, new Vector2(0, -298), new Vector2(650, 40));
    }

    private void BuildVolume(Transform panel)
    {
        CreateText(panel, "Eyebrow", "CONFIGURACIÓN DE AUDIO",
            23, Gold, new Vector2(0, 296), new Vector2(660, 45));
        CreateText(panel, "Title", "VOLUMEN",
            60, White, new Vector2(0, 220), new Vector2(660, 100));
        volumeSubtitle = CreateText(panel, "Subtitle", "",
            25, Muted, new Vector2(0, 153), new Vector2(650, 50));

        volumeSlider = CreateSlider(panel, new Vector2(0, 31));
        volumeValue = CreateText(panel, "Value", "5 / 10",
            40, Gold, new Vector2(0, -66), new Vector2(350, 60));
        CreateButton(panel, "Less", "−", new Vector2(-270, 31),
            new Vector2(65, 65), () => menu.SetVolumeFromUI(menu.VolumeManager.CurrentLevel - 1));
        CreateButton(panel, "More", "+", new Vector2(270, 31),
            new Vector2(65, 65), () => menu.SetVolumeFromUI(menu.VolumeManager.CurrentLevel + 1));
        CreateButton(panel, "Back", "VOLVER", new Vector2(0, -166),
            new Vector2(520, 78), menu.CloseVolume, true);
        CreateText(panel, "Hint", "Decí un número del uno al diez, o usá el control",
            21, Muted, new Vector2(0, -282), new Vector2(660, 50));
    }

    private void BuildResult(Transform panel)
    {
        CreateText(panel, "Eyebrow", "ACOUSTIC TEMPLE",
            23, Gold, new Vector2(0, 286), new Vector2(660, 45));
        resultTitle = CreateText(panel, "Title", "FIN DE LA PARTIDA",
            51, White, new Vector2(0, 185), new Vector2(680, 140));
        CreateButton(panel, "Return", "VOLVER AL MENÚ", new Vector2(0, 21),
            new Vector2(520, 78), menu.ReturnToMainMenu, true);
        CreateButton(panel, "Exit", "SALIR", new Vector2(0, -85),
            new Vector2(520, 78), menu.QuitGame);
        CreateText(panel, "Hint", "Decí volver o salir",
            22, Muted, new Vector2(0, -224), new Vector2(610, 40));
    }

    private GameObject CreatePanel(Transform root, string name)
    {
        RectTransform screen = CreateRect(root, name, Vector2.zero, Vector2.zero);
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = Vector2.zero;
        screen.offsetMax = Vector2.zero;
        screen.gameObject.AddComponent<Image>().color = Backdrop;

        RectTransform card = CreateRect(screen, "Card", Vector2.zero, new Vector2(760, 780));
        card.gameObject.AddComponent<Image>().color = Card;
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(2, -2);
        return screen.gameObject;
    }

    private static RectTransform CreateRect(Transform parent, string name,
        Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private Text CreateText(Transform parent, string name, string content, int size,
        Color color, Vector2 position, Vector2 dimensions)
    {
        RectTransform rect = CreateRect(parent, name, position, dimensions);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.text = content;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        return label;
    }

    private GameObject CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action,
        bool primary = false, bool topRight = false)
    {
        RectTransform rect = CreateRect(parent, name, position, size);
        if (topRight)
        {
            rect.anchorMin = rect.anchorMax = Vector2.one;
        }

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = primary ? Gold : new Color(0.13f, 0.20f, 0.27f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        Text text = CreateText(rect, "Label", label, size.x < 100 ? 40 : 30,
            primary ? new Color(0.06f, 0.09f, 0.12f, 1f) : White,
            Vector2.zero, size);
        text.raycastTarget = false;
        return rect.gameObject;
    }

    private Slider CreateSlider(Transform parent, Vector2 position)
    {
        RectTransform root = CreateRect(parent, "VolumeSlider", position, new Vector2(430, 70));
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 1f;
        slider.maxValue = 10f;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform track = CreateRect(root, "Track", Vector2.zero, new Vector2(430, 20));
        track.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.28f, 0.34f, 1f);
        RectTransform fill = CreateRect(track, "Fill", Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = Gold;
        RectTransform handle = CreateRect(root, "Handle", Vector2.zero, new Vector2(35, 48));
        handle.gameObject.AddComponent<Image>().color = White;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.SetValueWithoutNotify(menu.VolumeManager.CurrentLevel);
        slider.onValueChanged.AddListener(value => menu.SetVolumeFromUI(Mathf.RoundToInt(value)));
        return slider;
    }
}
