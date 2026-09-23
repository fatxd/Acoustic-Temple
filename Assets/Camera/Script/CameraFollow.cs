using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0f, 5f, -7f);

    void LateUpdate()
    {
        if (objetivo == null)
            return;

        transform.position = objetivo.position + offset;

        transform.LookAt(objetivo);
    }
}