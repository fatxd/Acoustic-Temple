using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0f, 5f, -7f);

    public float suavizado = 5f;

    void LateUpdate()
    {
        if (objetivo == null)
            return;

        Vector3 posicionDeseada = objetivo.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            posicionDeseada,
            suavizado * Time.deltaTime
        );

        transform.LookAt(objetivo);
    }
}