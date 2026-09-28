using System.Collections;
using UnityEngine;

/// <summary>Runs the three door encounters inside Main Scene.</summary>
public sealed class MainSceneFlow : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform spawn;
    [SerializeField] private Transform[] questionDoors = new Transform[3];
    [SerializeField] private MainSceneTrigger[] questionTriggers = new MainSceneTrigger[3];
    [SerializeField] private MainSceneTrigger goalTrigger;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private QuestionManager questionManager;
    [SerializeField] private TTSManager ttsManager;
    [SerializeField] private BachMenuController menuController;
    [SerializeField, Min(0f)] private float knockbackDistance = 0.8f;
    [SerializeField, Min(0.1f)] private float knockbackSeconds = 0.35f;
    [SerializeField, Range(0.5f, 1f)] private float finalSpeedMultiplier = 0.84f;

    private DoorState[] doors;
    private bool traversing;
    private bool waitingForAnswer;
    private bool goalReached;
    private int nextDoor;
    private float knockbackTimeLeft;

    public PlayerMovement Player => player;

    private sealed class DoorState
    {
        public Transform leftHinge;
        public Transform rightHinge;
        public Quaternion leftClosed;
        public Quaternion rightClosed;
        public Collider[] colliders;
        public bool[] colliderEnabled;
    }

    private void Awake()
    {
        if (player == null || spawn == null || questionDoors == null
            || questionDoors.Length != 3 || System.Array.Exists(questionDoors, door => door == null)
            || questionTriggers == null || questionTriggers.Length != questionDoors.Length
            || System.Array.Exists(questionTriggers, trigger => trigger == null)
            || goalTrigger == null
            || gameManager == null || questionManager == null || ttsManager == null
            || menuController == null)
        {
            Debug.LogError("[MAIN] Faltan referencias del nivel o de los sistemas de juego.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < questionTriggers.Length; i++)
        {
            if (questionTriggers[i].Flow != this || questionTriggers[i].DoorIndex != i)
            {
                Debug.LogError($"[MAIN] Trigger de puerta {i + 1} mal configurado.", this);
                enabled = false;
                return;
            }
        }
        if (goalTrigger.Flow != this || goalTrigger.DoorIndex != -1)
        {
            Debug.LogError("[MAIN] Trigger de Meta mal configurado.", this);
            enabled = false;
            return;
        }

        player.EnableAutomaticMode();
        player.Teleport(spawn.position);
        CacheDoors();
        questionManager.EnableDoorMode();
        questionManager.CorrectAnswerAtDoor += OnCorrectAnswer;
        questionManager.IncorrectAnswerAtDoor += OnIncorrectAnswer;
        gameManager.StateChanged += OnGameStateChanged;
        menuController.AttachLevelFlow(this);
        Debug.Log("[MAIN] Sistemas de juego conectados en Main Scene.", this);
    }

    private void CacheDoors()
    {
        doors = new DoorState[questionDoors.Length];
        for (int i = 0; i < questionDoors.Length; i++)
        {
            Transform door = questionDoors[i];
            DoorState state = new DoorState();
            state.colliders = door.GetComponentsInChildren<Collider>(true);
            state.colliderEnabled = new bool[state.colliders.Length];
            for (int j = 0; j < state.colliders.Length; j++)
                state.colliderEnabled[j] = state.colliders[j].enabled;

            Transform first = FindChild(door, "SM_Bld_Castle_Door_L");
            Transform second = FindChild(door, "SM_Bld_Castle_Door_R");
            if (first != null && second != null)
            {
                Renderer firstRenderer = first.GetComponentInChildren<Renderer>(true);
                Renderer secondRenderer = second.GetComponentInChildren<Renderer>(true);
                if (firstRenderer != null && secondRenderer != null)
                {
                    // The FBX names its leaves from the opposite viewing direction.
                    // Determine their physical side from the meshes instead.
                    bool firstIsLeft = Vector3.Dot(
                        firstRenderer.bounds.center - secondRenderer.bounds.center,
                        door.right) < 0f;
                    state.leftHinge = CreateHinge(door,
                        firstIsLeft ? first : second,
                        firstIsLeft ? firstRenderer : secondRenderer, true);
                    state.rightHinge = CreateHinge(door,
                        firstIsLeft ? second : first,
                        firstIsLeft ? secondRenderer : firstRenderer, false);
                }
                if (state.leftHinge != null) state.leftClosed = state.leftHinge.localRotation;
                if (state.rightHinge != null) state.rightClosed = state.rightHinge.localRotation;
            }
            if (state.leftHinge == null || state.rightHinge == null)
                Debug.LogWarning($"[MAIN] Puerta {i + 1} sin hojas separadas; se liberará el paso al acertar.", door);

            doors[i] = state;
        }
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static Transform CreateHinge(Transform door, Transform leaf,
        Renderer renderer, bool left)
    {
        Bounds bounds = LeafBoundsInDoorSpace(door, leaf, renderer);
        Vector3 edge = new Vector3(left ? bounds.min.x : bounds.max.x,
            bounds.center.y, bounds.center.z);
        GameObject pivot = new GameObject(left ? "LeftDoorHinge" : "RightDoorHinge");
        Transform hinge = pivot.transform;
        hinge.SetParent(door, false);
        hinge.localPosition = edge;
        hinge.localRotation = Quaternion.identity;
        leaf.SetParent(hinge, true);
        return hinge;
    }

    private static Bounds LeafBoundsInDoorSpace(Transform door, Transform leaf,
        Renderer renderer)
    {
        MeshFilter mesh = leaf.GetComponentInChildren<MeshFilter>(true);
        bool useMesh = mesh != null && mesh.sharedMesh != null;
        Bounds source = useMesh ? mesh.sharedMesh.bounds : renderer.bounds;
        Vector3 minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity,
            float.PositiveInfinity);
        Vector3 maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity,
            float.NegativeInfinity);

        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector3 corner = new Vector3(x == 0 ? source.min.x : source.max.x,
                y == 0 ? source.min.y : source.max.y,
                z == 0 ? source.min.z : source.max.z);
            Vector3 world = useMesh ? mesh.transform.TransformPoint(corner) : corner;
            Vector3 local = door.InverseTransformPoint(world);
            minimum = Vector3.Min(minimum, local);
            maximum = Vector3.Max(maximum, local);
        }

        return new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
    }

    public void PrepareRound()
    {
        traversing = false;
        waitingForAnswer = false;
        goalReached = false;
        nextDoor = 0;
        knockbackTimeLeft = 0f;
        player.SetAutomaticInput(Vector2.zero);
        player.Teleport(spawn.position);
        StopAllCoroutines();
        foreach (DoorState door in doors)
        {
            if (door.leftHinge != null) door.leftHinge.localRotation = door.leftClosed;
            if (door.rightHinge != null) door.rightHinge.localRotation = door.rightClosed;
            for (int i = 0; i < door.colliders.Length; i++)
                door.colliders[i].enabled = door.colliderEnabled[i];
        }
    }

    public void BeginTraversal() => traversing = true;

    public void ResetForMenu() => PrepareRound();

    private void Update()
    {
        if (gameManager == null || gameManager.CurrentState != GameState.Playing || !traversing)
        {
            player.SetAutomaticInput(Vector2.zero);
            return;
        }

        if (knockbackTimeLeft > 0f)
        {
            knockbackTimeLeft = Mathf.Max(0f, knockbackTimeLeft - Time.deltaTime);
            float input = Mathf.Min(1f, knockbackDistance / (knockbackSeconds * player.velocidad));
            player.SetAutomaticInput(new Vector2(0f, -input));
            return;
        }

        if (waitingForAnswer || ttsManager.IsSpeaking)
        {
            player.SetAutomaticInput(Vector2.zero);
            return;
        }

        // CharacterController normally raises OnTriggerEnter; this also catches a skipped
        // trigger when the frame rate drops while crossing its narrow volume.
        if (nextDoor < questionTriggers.Length
            && player.transform.position.z >= questionTriggers[nextDoor].transform.position.z)
        {
            EnterDoorTrigger(nextDoor);
            return;
        }

        if (nextDoor >= questionDoors.Length
            && player.transform.position.z >= goalTrigger.transform.position.z)
        {
            EnterGoalTrigger();
            return;
        }

        player.SetAutomaticInput(Vector2.up * (nextDoor == questionDoors.Length
            ? finalSpeedMultiplier : 1f));
    }

    public void EnterDoorTrigger(int index)
    {
        if (!traversing || gameManager == null || gameManager.CurrentState != GameState.Playing
            || waitingForAnswer || index != nextDoor) return;

        waitingForAnswer = true;
        player.SetAutomaticInput(Vector2.zero);
        questionManager.BeginQuestionAtDoor(index);
        Debug.Log($"[MAIN] Trigger puerta {index + 1}: empieza la pregunta.", this);
    }

    public void EnterGoalTrigger()
    {
        if (!traversing || goalReached || nextDoor != questionDoors.Length
            || gameManager.CurrentState != GameState.Playing) return;

        goalReached = true;
        traversing = false;
        player.SetAutomaticInput(Vector2.zero);
        questionManager.ReachGoal();
    }

    private void OnCorrectAnswer(int index)
    {
        if (index != nextDoor) return;
        OpenDoor(index);
        nextDoor++;
        waitingForAnswer = false;
        knockbackTimeLeft = 0f;
        Debug.Log($"[MAIN] Puerta {index + 1} abierta.", this);
    }

    private void OpenDoor(int index)
    {
        DoorState door = doors[index];
        for (int i = 0; i < door.colliders.Length; i++) door.colliders[i].enabled = false;
        if (door.leftHinge != null && door.rightHinge != null)
            StartCoroutine(AnimateDoor(door));
    }

    private static IEnumerator AnimateDoor(DoorState door)
    {
        const float duration = 0.8f;
        float elapsed = 0f;
        Quaternion leftOpen = door.leftClosed * Quaternion.Euler(0f, -90f, 0f);
        Quaternion rightOpen = door.rightClosed * Quaternion.Euler(0f, 90f, 0f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            door.leftHinge.localRotation = Quaternion.Slerp(door.leftClosed, leftOpen, t);
            door.rightHinge.localRotation = Quaternion.Slerp(door.rightClosed, rightOpen, t);
            yield return null;
        }
        door.leftHinge.localRotation = leftOpen;
        door.rightHinge.localRotation = rightOpen;
    }

    private void OnIncorrectAnswer(int index)
    {
        if (index != nextDoor) return;
        knockbackTimeLeft = knockbackSeconds;
        Debug.Log($"[MAIN] Puerta {index + 1}: retroceso por error.", this);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Playing) player.SetAutomaticInput(Vector2.zero);
    }

    private void OnDestroy()
    {
        if (questionManager != null)
        {
            questionManager.CorrectAnswerAtDoor -= OnCorrectAnswer;
            questionManager.IncorrectAnswerAtDoor -= OnIncorrectAnswer;
        }
        if (gameManager != null) gameManager.StateChanged -= OnGameStateChanged;
        if (player != null) player.SetAutomaticInput(Vector2.zero);
    }
}
