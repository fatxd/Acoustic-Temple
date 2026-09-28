using System.Globalization;
using UnityEngine;

public class VolumeManager : MonoBehaviour
{
    private int currentLevel = 5;

    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        SetVolume(currentLevel);
    }

    public void SetVolume(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 10);
        AudioListener.volume = currentLevel / 10f;
        Debug.Log($"[VOLUME] Nivel: {currentLevel} | AudioListener: {AudioListener.volume.ToString("0.0", CultureInfo.InvariantCulture)}");
    }
}
