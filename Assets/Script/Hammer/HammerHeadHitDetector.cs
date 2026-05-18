using UnityEngine;

public class HammerHeadHitDetector : MonoBehaviour
{
    private HammerMotionHapticController hammerController;

    private void Awake()
    {
        hammerController = GetComponentInParent<HammerMotionHapticController>();

        if (hammerController == null)
        {
            Debug.LogWarning("[HammerHeadHitDetector] 부모 오브젝트에서 HammerMotionHapticController를 찾지 못했습니다.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hammerController != null)
        {
            hammerController.TryHammerHit(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hammerController != null)
        {
            hammerController.TryHammerHit(collision.collider);
        }
    }
}