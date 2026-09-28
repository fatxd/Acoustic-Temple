using UnityEngine;

public class LivesManager : MonoBehaviour
{
    private const int InitialLives = 3;
    private int currentLives = InitialLives;

    public int CurrentLives => currentLives;

    public void ResetLives()
    {
        currentLives = InitialLives;
        Debug.Log($"[LIVES] Vidas restantes: {currentLives}");
    }

    public void LoseLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
        }

        Debug.Log($"[LIVES] Vidas restantes: {currentLives}");
    }
}
