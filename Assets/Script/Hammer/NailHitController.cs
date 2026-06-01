using UnityEngine;

public class NailHitController : MonoBehaviour
{
    private enum NailState
    {
        Holding,
        Fixed,
        Driven
    }

    [Header("Nail Size")]
    [SerializeField] private float nailHeight = 1.0f;

    [Header("Nail Movement Settings")]
    [SerializeField] private float sinkAmount = 0.04f;
    [SerializeField] private float fixedDepth = 0.08f;
    [SerializeField] private float maxSinkDepth = 0.4f;

    [Header("Hit Judge Settings")]
    [SerializeField] private float centerHitThreshold = 0.08f;

    [Header("Nail Tilt Recovery Settings")]
    [SerializeField] private float tiltStepAngle = 8f;
    [SerializeField] private float maxRecoverableTiltAngle = 35f;
    [SerializeField] private float straightThreshold = 2f;

    [Header("Toast")]
    [SerializeField] private ToastMessageController toastController;
    [SerializeField] private float toastShowTime = 3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private NailState currentState = NailState.Holding;

    private Quaternion initialRotation;

    private float currentSinkDepth = 0f;
    private float currentTiltX = 0f;
    private float currentTiltZ = 0f;

    private HammerFragmentEyeAccident fragmentAccident;

    private bool hasShownStartMessage = false;
    private bool hasShownFixedMessage = false;

    private void Start()
    {
        initialRotation = transform.rotation;
        fragmentAccident = GetComponent<HammerFragmentEyeAccident>();
    }

    public void OnHitByHammer(Transform hammerHead)
    {
        if (hammerHead == null)
            return;

        if (SafetyPracticeManager.Instance != null &&
            SafetyPracticeManager.Instance.TryFailIfNoGlove())
        {
            return;
        }

        Vector3 nailAxis = -transform.forward;
        Vector3 nailTopCenter = transform.position + nailAxis * (nailHeight * 0.5f);

        Vector3 hammerFlatPosition = new Vector3(
            hammerHead.position.x,
            nailTopCenter.y,
            hammerHead.position.z
        );

        float distanceFromCenter = Vector3.Distance(hammerFlatPosition, nailTopCenter);

        Vector3 hitDirection = (transform.position - hammerHead.position).normalized;

        if (showDebugLog)
        {
            Debug.Log(
                "[NailHitController] 타격 거리: " + distanceFromCenter.ToString("F3")
                + " / 박힘 깊이: " + currentSinkDepth.ToString("F3")
                + " / 기울기: " + GetTotalTiltAngle().ToString("F1")
                + " / 상태: " + currentState
            );
        }

        if (distanceFromCenter <= centerHitThreshold)
        {
            HandleCenterHit();
        }
        else
        {
            HandleSideHit(hitDirection);
        }
    }

    public void ShowStartMessage()
    {
        if (hasShownStartMessage)
            return;

        hasShownStartMessage = true;

        if (toastController != null)
        {
            toastController.ShowNormalToast("못을 손으로 고정한 뒤 가볍게 타격하십시오.", 3f);
        }
    }

    private void ShowFixedMessage()
    {
        if (hasShownFixedMessage)
            return;

        hasShownFixedMessage = true;

        if (toastController != null)
        {
            toastController.ShowNormalToast("못이 고정되었습니다. 손을 떼고 타격하십시오.", toastShowTime);
        }
    }

    private void HandleCenterHit()
    {
        SinkNail();

        if (currentState == NailState.Holding && currentSinkDepth >= fixedDepth)
        {
            currentState = NailState.Fixed;
            ShowFixedMessage();
        }
        else if (currentState == NailState.Fixed)
        {
            currentState = NailState.Driven;
        }

        if (showDebugLog)
        {
            Debug.Log("[NailHitController] 정중앙 타격: 못이 아래로 박힘");
        }
    }

    private void HandleSideHit(Vector3 hitDirection)
    {
        TiltOrRecoverNail(hitDirection);

        if (currentState == NailState.Holding && currentSinkDepth < fixedDepth)
        {
            SinkNail();

            if (currentSinkDepth >= fixedDepth)
            {
                currentState = NailState.Fixed;
                ShowFixedMessage();
            }
        }
        else if (currentState == NailState.Fixed || currentState == NailState.Driven)
        {
            currentState = NailState.Driven;

            if (IsAlmostStraight())
            {
                SinkNail();

                if (showDebugLog)
                {
                    Debug.Log("[NailHitController] 못이 거의 펴진 상태라 다시 정상적으로 박힘");
                }
            }
        }

        fragmentAccident?.TryTriggerAccident(GetTotalTiltAngle());

        if (showDebugLog)
        {
            Debug.Log("[NailHitController] 비스듬한 타격 처리 완료 / 현재 기울기: " + GetTotalTiltAngle().ToString("F1"));
        }
    }

    private void SinkNail()
    {
        if (currentSinkDepth >= maxSinkDepth)
        {
            if (showDebugLog)
            {
                Debug.Log("[NailHitController] 최대 박힘 깊이에 도달했습니다.");
            }

            return;
        }

        Vector3 nailAxis = -transform.forward;
        transform.position += -nailAxis * sinkAmount;
        currentSinkDepth += sinkAmount;
    }

    private void TiltOrRecoverNail(Vector3 hitDirection)
    {
        Vector2 hitTiltDirection = new Vector2(
            hitDirection.z,
            -hitDirection.x
        );

        if (hitTiltDirection.sqrMagnitude <= 0.001f)
            return;

        hitTiltDirection.Normalize();

        Vector2 currentTiltDirection = new Vector2(
            currentTiltX,
            currentTiltZ
        );

        bool isAlreadyTilted = currentTiltDirection.magnitude > straightThreshold;

        if (!isAlreadyTilted)
        {
            currentTiltX += hitTiltDirection.x * tiltStepAngle;
            currentTiltZ += hitTiltDirection.y * tiltStepAngle;

            ApplyTiltRotation();

            if (showDebugLog)
            {
                Debug.Log("[NailHitController] 못이 처음 기울어짐");
            }

            return;
        }

        float directionDot = Vector2.Dot(currentTiltDirection.normalized, hitTiltDirection);

        currentTiltX += hitTiltDirection.x * tiltStepAngle;
        currentTiltZ += hitTiltDirection.y * tiltStepAngle;

        if (Mathf.Abs(currentTiltX) < straightThreshold)
            currentTiltX = 0f;

        if (Mathf.Abs(currentTiltZ) < straightThreshold)
            currentTiltZ = 0f;

        ApplyTiltRotation();

        if (showDebugLog)
        {
            if (directionDot > 0f)
            {
                Debug.Log("[NailHitController] 같은 방향 타격: 못이 더 휘어짐");
            }
            else
            {
                Debug.Log("[NailHitController] 반대 방향 타격: 못이 다시 펴지는 중");
            }
        }
    }

    private void ApplyTiltRotation()
    {
        float totalTilt = GetTotalTiltAngle();

        if (totalTilt > maxRecoverableTiltAngle)
        {
            float scale = maxRecoverableTiltAngle / totalTilt;

            currentTiltX *= scale;
            currentTiltZ *= scale;
        }

        Quaternion tiltRotation = Quaternion.Euler(currentTiltX, 0f, currentTiltZ);
        transform.rotation = initialRotation * tiltRotation;
    }

    private float GetTotalTiltAngle()
    {
        return Mathf.Sqrt(currentTiltX * currentTiltX + currentTiltZ * currentTiltZ);
    }

    private bool IsAlmostStraight()
    {
        return GetTotalTiltAngle() <= straightThreshold;
    }
}