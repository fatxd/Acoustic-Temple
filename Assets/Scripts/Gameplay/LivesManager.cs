using UnityEngine;

public class LivesManager : MonoBehaviour
{
    private const int InitialLives = 3;
    private int currentLives = InitialLives;

    public int CurrentLives => currentLives;
    public event System.Action<int> LivesChanged;

    public void ResetLives()
    {
        currentLives = InitialLives;
        LivesChanged?.Invoke(currentLives);
        Debug.Log($"[LIVES] Vidas restantes: {currentLives}");
    }

    public void LoseLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            LivesChanged?.Invoke(currentLives);
        }

        Debug.Log($"[LIVES] Vidas restantes: {currentLives}");
    }
}
