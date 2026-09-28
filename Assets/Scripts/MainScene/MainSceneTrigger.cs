using UnityEngine;

/// <summary>Invisible trigger before a question door or at the final goal.</summary>
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class MainSceneTrigger : MonoBehaviour
{
    [SerializeField] private MainSceneFlow flow;
    [SerializeField] private int doorIndex;

    public MainSceneFlow Flow => flow;
    public int DoorIndex => doorIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (flow == null || other.GetComponentInParent<PlayerMovement>() != flow.Player) return;
        if (doorIndex >= 0) flow.EnterDoorTrigger(doorIndex);
        else flow.EnterGoalTrigger();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = doorIndex < 0 ? Color.yellow : Color.cyan;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = previousMatrix;
    }
#endif
}
