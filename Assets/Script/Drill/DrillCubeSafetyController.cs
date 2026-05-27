using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class DrillCubeSafetyCheckController : MonoBehaviour
{
    [Header("Target Cube Settings")]
    [SerializeField] private string targetCubeTag = "DrillCube";

    [Header("Success Toast")]
    [SerializeField] private ToastMessageController toastController;
    [SerializeField] private string successMessage = "보호구 착용 확인! 안전수칙을 준수했습니다.";
    [SerializeField] private float successToastShowTime = 2f;

    [Header("Judge Settings")]
    [SerializeField] private bool judgeOnlyOnce = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private XRGrabInteractable grabInteractable;

    private bool isGrabbed = false;
    private bool isDrillActive = false;
    private bool hasJudged = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        grabInteractable.activated.AddListener(OnDrillActivated);
        grabInteractable.deactivated.AddListener(OnDrillDeactivated);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);

        grabInteractable.activated.RemoveListener(OnDrillActivated);
        grabInteractable.deactivated.RemoveListener(OnDrillDeactivated);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        if (!judgeOnlyOnce)
        {
            hasJudged = false;
        }

        if (showDebugLog)
        {
            Debug.Log("[DrillCubeSafetyCheckController] 드릴 잡음");
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        isDrillActive = false;

        if (!judgeOnlyOnce)
        {
            hasJudged = false;
        }

        if (showDebugLog)
        {
            Debug.Log("[DrillCubeSafetyCheckController] 드릴 놓음");
        }
    }

    private void OnDrillActivated(ActivateEventArgs args)
    {
        if (!isGrabbed)
            return;

        isDrillActive = true;

        if (showDebugLog)
        {
            Debug.Log("[DrillCubeSafetyCheckController] 드릴 작동 시작");
        }
    }

    private void OnDrillDeactivated(DeactivateEventArgs args)
    {
        isDrillActive = false;

        if (showDebugLog)
        {
            Debug.Log("[DrillCubeSafetyCheckController] 드릴 작동 정지");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckCubeContact(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckCubeContact(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckCubeContact(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckCubeContact(other.gameObject);
    }

    private void CheckCubeContact(GameObject contactObject)
    {
        if (!isGrabbed)
            return;

        if (!isDrillActive)
            return;

        if (judgeOnlyOnce && hasJudged)
            return;

        if (contactObject == null)
            return;

        if (!contactObject.CompareTag(targetCubeTag))
            return;

        hasJudged = true;

        if (showDebugLog)
        {
            Debug.Log("[DrillCubeSafetyCheckController] 드릴 작동 중 큐브 접촉 감지: " + contactObject.name);
        }

        if (SafetyPracticeManager.Instance != null &&
            SafetyPracticeManager.Instance.TryFailIfNoGlove())
        {
            if (showDebugLog)
            {
                Debug.Log("[DrillCubeSafetyCheckController] 보호구 미착용: 기존 실패 로직 실행");
            }

            return;
        }

        ShowSuccessToast();
    }

    private void ShowSuccessToast()
    {
        if (toastController != null)
        {
            toastController.ShowSuccessToast(successMessage, successToastShowTime);
        }

        if (showDebugLog)
        {
            Debug.Log("[DrillCubeSafetyCheckController] 보호구 착용 상태: 안전수칙 준수 성공");
        }
    }
}