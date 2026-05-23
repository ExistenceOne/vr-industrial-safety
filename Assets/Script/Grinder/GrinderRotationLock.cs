using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrinderRotationLock : MonoBehaviour
{
    public float detectRadius = 0.1f;
    public Vector3 centerOffset = Vector3.zero;

    private Rigidbody rb;
    private Quaternion lockedRotation;
    private bool isNearVoxel = false;

    private Vector3 WorldCenter => transform.TransformPoint(centerOffset);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lockedRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(WorldCenter, detectRadius);

        bool found = false;
        foreach (var hit in hits)
        {
            if (hit.GetComponent<VoxelObject>() != null)
            {
                found = true;
                break;
            }
        }

        if (!isNearVoxel && found)
            lockedRotation = transform.rotation;

        isNearVoxel = found;

        if (isNearVoxel)
            rb.MoveRotation(lockedRotation);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(WorldCenter, detectRadius);
    }
}