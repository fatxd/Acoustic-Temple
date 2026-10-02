using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0f, 5f, -7f);

    public float suavizado = 5f;

    [Header("Vista de victoria")]
    [SerializeField, Min(0.1f)] private float victoryHeight = 0.35f;
    [SerializeField, Range(0f, 60f)] private float victoryUpwardAngle = 25f;
    [SerializeField, Min(0.1f)] private float victoryTransitionSeconds = 1.6f;
    private GameManager gameManager;
    private bool victoryView;
    private float transitionElapsed;
    private Vector3 shotStart;
    private Vector3 shotPosition;
    private Quaternion shotRotation;
    private Quaternion shotTargetRotation;

    public bool IsVictoryTransitioning => victoryView && transitionElapsed < victoryTransitionSeconds;

    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null) gameManager.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Victory && objetivo != null)
        {
            victoryView = true;
            transitionElapsed = 0f;
            shotStart = transform.position;
            shotRotation = transform.rotation;
            shotPosition = new Vector3(shotStart.x, objetivo.position.y + victoryHeight, shotStart.z);
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            shotTargetRotation = Quaternion.LookRotation(forward, Vector3.up)
                * Quaternion.Euler(-victoryUpwardAngle, 0f, 0f);
        }
        else
        {
            victoryView = false;
            if (state == GameState.MainMenu && objetivo != null)
            {
                transform.position = objetivo.position + offset;
                transform.LookAt(objetivo);
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null) gameManager.StateChanged -= OnStateChanged;
    }

    void LateUpdate()
    {
        if (objetivo == null)
            return;

        if (victoryView)
        {
            transitionElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transitionElapsed / victoryTransitionSeconds));
            transform.position = Vector3.Lerp(shotStart, shotPosition, t);
            transform.rotation = Quaternion.Slerp(shotRotation, shotTargetRotation, t);
            return;
        }

        Vector3 posicionDeseada = objetivo.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            posicionDeseada,
            1f - Mathf.Exp(-suavizado * Time.deltaTime)
        );

        transform.LookAt(objetivo);
    }
}
