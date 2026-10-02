using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    [SerializeField] private LivesManager livesManager;
    [SerializeField] private Image[] shields;
    [SerializeField] private Sprite fullShield;
    [SerializeField] private Sprite emptyShield;

    private void OnEnable()
    {
        if (livesManager == null) return;
        livesManager.LivesChanged += Refresh;
        Refresh(livesManager.CurrentLives);
    }

    private void OnDisable()
    {
        if (livesManager != null) livesManager.LivesChanged -= Refresh;
    }

    private void Refresh(int currentLives)
    {
        if (shields == null) return;
        for (int i = 0; i < shields.Length; i++)
        {
            if (shields[i] != null)
                shields[i].sprite = i < currentLives ? fullShield : emptyShield;
        }
    }
}
