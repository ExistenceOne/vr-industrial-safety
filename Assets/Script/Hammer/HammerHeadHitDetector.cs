// HammerHeadHitDetector.cs
// 해머 헤드가 충돌을 감지하여 HammerMotionHapticController에 전달하는 스크립트

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
        HandleHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider other)
    {
        if (hammerController == null)
            return;

        bool isValidHit = hammerController.TryHammerHit(other);

        if (!isValidHit)
            return;

        NailHitController nail = other.GetComponent<NailHitController>();

        if (nail == null)
        {
            nail = other.GetComponentInParent<NailHitController>();
        }

        if (nail != null)
        {
            nail.OnHitByHammer(transform);
        }
    }
}