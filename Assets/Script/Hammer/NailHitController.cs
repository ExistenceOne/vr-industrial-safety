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

    [Header("Nail Complete By Contact Settings")]
    [SerializeField] private bool useHeadContactCompleteLimit = true;
    [SerializeField] private Collider nailHeadContactCollider;
    [SerializeField] private Collider completeTargetCollider;
    [SerializeField] private float contactCheckPadding = 0.001f;

    [Header("Nail Complete By Y Settings")]
    [SerializeField] private bool useHeadYCompleteLimit = false;
    [SerializeField] private float completeHeadY = 0.7301f;

    [Header("Hit Judge Settings")]
    [SerializeField] private float centerHitThreshold = 0.08f;

    [Header("Nail Tilt Recovery Settings")]
    [SerializeField] private float tiltStepAngle = 8f;
    [SerializeField] private float maxRecoverableTiltAngle = 35f;
    [SerializeField] private float straightThreshold = 2f;

    [Header("Complete Visual Settings")]
    [SerializeField] private bool changeColorOnComplete = true;
    [SerializeField] private Renderer[] nailRenderers;
    [SerializeField] private Color completeColor = new Color(0.35f, 1.0f, 0.35f, 1f);


    [SerializeField] private float sideHitSinkMultiplier = 0.6f;


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
    private bool isCompleted = false;

    private void Start()
    {
        initialRotation = transform.rotation;
        fragmentAccident = GetComponent<HammerFragmentEyeAccident>();
    }

    public void OnHitByHammer(Transform hammerHead)
    {
        if (isCompleted)
            return;

        if (hammerHead == null)
            return;

        if (SafetyPracticeManager.Instance != null &&
            SafetyPracticeManager.Instance.TryFailIfNoGlove())
        {
            return;
        }

        Vector3 nailAxis = -transform.forward;
        Vector3 nailTopCenter = GetNailTopCenter();

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
                + " / 헤드 Y: " + nailTopCenter.y.ToString("F4")
                + " / 접촉 완료 여부: " + IsHeadTouchingCompleteTarget()
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

    private void ShowCompleteMessage()
    {
        if (toastController != null)
        {
            toastController.ShowSuccessToast("못 박기가 완료되었습니다.", toastShowTime);
        }
    }

    private void HandleCenterHit()
    {
        if (isCompleted)
            return;

        SinkNail();

        if (isCompleted)
            return;

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
        if (isCompleted)
            return;

        TiltOrRecoverNail(hitDirection);

        SinkNail();

        if (isCompleted)
            return;

        if (currentState == NailState.Holding && currentSinkDepth >= fixedDepth)
        {
            currentState = NailState.Fixed;
            ShowFixedMessage();
        }
        else if (currentState == NailState.Fixed || currentState == NailState.Driven)
        {
            currentState = NailState.Driven;
        }

        fragmentAccident?.TryTriggerAccident(GetTotalTiltAngle());

        if (showDebugLog)
        {
            Debug.Log(
                "[NailHitController] 비스듬한 타격: 못이 기울어지고 아래로도 박힘"
                + " / 현재 기울기: " + GetTotalTiltAngle().ToString("F1")
                + " / 박힘 깊이: " + currentSinkDepth.ToString("F3")
            );
        }
    }

    private void SinkNail()
    {
        if (isCompleted)
            return;

        if (IsCompleteConditionMet())
        {
            CompleteNail();
            return;
        }

        if (currentSinkDepth >= maxSinkDepth)
        {
            if (showDebugLog)
            {
                Debug.Log("[NailHitController] 최대 박힘 깊이에 도달했습니다.");
            }

            return;
        }

        Vector3 nailAxis = -transform.forward;
        Vector3 moveDelta = -nailAxis * sinkAmount;

        Vector3 previousPosition = transform.position;
        float previousSinkDepth = currentSinkDepth;

        transform.position += moveDelta;
        currentSinkDepth += sinkAmount;

        if (IsCompleteConditionMet())
        {
            ClampToContactPosition(previousPosition, moveDelta);

            currentSinkDepth = previousSinkDepth + sinkAmount;

            CompleteNail();
        }
    }

    private bool IsCompleteConditionMet()
    {
        if (useHeadContactCompleteLimit && IsHeadTouchingCompleteTarget())
            return true;

        if (useHeadYCompleteLimit)
        {
            Vector3 currentTopCenter = GetNailTopCenter();

            if (currentTopCenter.y <= completeHeadY)
                return true;
        }

        return false;
    }

    private bool IsHeadTouchingCompleteTarget()
    {
        if (nailHeadContactCollider == null || completeTargetCollider == null)
            return false;

        Bounds headBounds = nailHeadContactCollider.bounds;
        Bounds targetBounds = completeTargetCollider.bounds;

        headBounds.Expand(contactCheckPadding);
        targetBounds.Expand(contactCheckPadding);

        return headBounds.Intersects(targetBounds);
    }

    private void ClampToContactPosition(Vector3 startPosition, Vector3 moveDelta)
    {
        if (!useHeadContactCompleteLimit)
            return;

        if (nailHeadContactCollider == null || completeTargetCollider == null)
            return;

        float low = 0f;
        float high = 1f;

        for (int i = 0; i < 12; i++)
        {
            float mid = (low + high) * 0.5f;

            transform.position = startPosition + moveDelta * mid;

            if (IsHeadTouchingCompleteTarget())
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        transform.position = startPosition + moveDelta * high;

        if (showDebugLog)
        {
            Debug.Log("[NailHitController] 네일 헤드 Plane이 완료 오브젝트에 닿은 위치로 보정됨");
        }
    }

    private Vector3 GetNailTopCenter()
    {
        if (nailHeadContactCollider != null)
        {
            return nailHeadContactCollider.bounds.center;
        }

        Vector3 nailAxis = -transform.forward;
        return transform.position + nailAxis * (nailHeight * 0.5f);
    }

    private void ApplyCompleteVisual()
    {
        if (!changeColorOnComplete)
            return;

        if (nailRenderers == null || nailRenderers.Length == 0)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("[NailHitController] 완료 색상 변경 대상 Renderer가 연결되지 않았습니다.");
            }

            return;
        }

        for (int i = 0; i < nailRenderers.Length; i++)
        {
            if (nailRenderers[i] == null)
                continue;

            Material materialInstance = nailRenderers[i].material;
            materialInstance.color = completeColor;
        }

        if (showDebugLog)
        {
            Debug.Log("[NailHitController] 완료된 못 색상이 초록색으로 변경되었습니다.");
        }
    }


    private void CompleteNail()
    {
        if (isCompleted)
            return;

        isCompleted = true;
        currentState = NailState.Driven;

        ApplyCompleteVisual();
        ShowCompleteMessage();

        if (SafetyPracticeManager.Instance != null)
        {
            SafetyPracticeManager.Instance.AddHammerNailProgress();
            Debug.Log("[NailHitController] 못 완료 progress 증가 호출");
        }
        else
        {
            Debug.LogWarning("[NailHitController] SafetyPracticeManager.Instance가 null이라 progress 증가 실패");
        }

        if (showDebugLog)
        {
            Vector3 nailTopCenter = GetNailTopCenter();

            Debug.Log(
                "[NailHitController] 못 박기 완료"
                + " / 헤드 위치: " + nailTopCenter
                + " / 박힘 깊이: " + currentSinkDepth.ToString("F3")
            );
        }
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