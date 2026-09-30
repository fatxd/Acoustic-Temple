using UnityEngine;
using UnityEngine.UI;
 
public class LivesUI : MonoBehaviour
{
[SerializeField] private LivesManager livesManager;
 
[SerializeField] private Image[] shields;
 
[SerializeField] private Sprite fullShield;
[SerializeField] private Sprite emptyShield;
 
private void Update()
{
UpdateLivesUI();
}
 
private void UpdateLivesUI()
{
int currentLives = livesManager.CurrentLives;
 
for (int i = 0; i < shields.Length; i++)
{
shields[i].sprite = i < currentLives
? fullShield
: emptyShield;
}
}
}