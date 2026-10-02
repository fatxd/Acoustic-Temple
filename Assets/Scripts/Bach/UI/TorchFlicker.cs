using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private Light torchLight;

    [Header("Intensidad")]
    [SerializeField] private float intensidadMin = 2.5f;
    [SerializeField] private float intensidadMax = 4f;

    [Header("Velocidad")]
    [SerializeField] private float velocidad = 5f;

    private float intensidadBase;
    private float offset;

    private void Start()
    {
        if (torchLight == null)
            torchLight = GetComponent<Light>();

        if (torchLight == null)
        {
            Debug.LogWarning("TorchFlicker: No se encontró una Light.", this);
            enabled = false;
            return;
        }

        intensidadBase = torchLight.intensity;
        offset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float ruido = Mathf.PerlinNoise(
            offset,
            Time.time * velocidad
        );

        torchLight.intensity = Mathf.Lerp(
            intensidadMin,
            intensidadMax,
            ruido
        );
    }
}