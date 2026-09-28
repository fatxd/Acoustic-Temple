using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameState currentState = GameState.MainMenu;

    public GameState CurrentState => currentState;
    public bool IsPaused => currentState == GameState.Paused
        || currentState == GameState.PausedVolumeMenu;
    public event Action<GameState> StateChanged;

    private void Awake()
    {
        currentState = GameState.MainMenu;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Debug.Log($"Estado actual: {currentState}");
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        bool paused = IsPaused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        Debug.Log($"Nuevo estado: {currentState}");
        StateChanged?.Invoke(currentState);
    }

    private void OnDestroy()
    {
        // Play mode can stop while the game is paused.
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

#if UNITY_EDITOR
    [ContextMenu("Probar: siguiente estado")]
    private void AdvanceStateForTesting()
    {
        GameState nextState = currentState == GameState.GameOver
            ? GameState.MainMenu
            : (GameState)((int)currentState + 1);

        ChangeState(nextState);
    }
#endif
}
