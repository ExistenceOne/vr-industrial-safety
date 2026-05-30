using System.Collections.Generic;
using UnityEngine;
using TMPro;

// AngleGrinder 오브젝트(GrinderController와 같은 GameObject)에 붙인다.
// 블레이드 원통 영역이 VoxelObject와 2초 이상 접촉하면 킥백 사고 발생.
public class GrinderKickbackDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GrinderController grinderController;

    [Header("Cylinder Zone")]
    [SerializeField] private Vector3 centerOffset = Vector3.zero;
    [SerializeField] private float cylinderRadius = 0.05f;
    [SerializeField] private float cylinderHalfHeight = 0.05f;

    [Header("Detection Targets (비워두면 씬 내 VoxelObject 자동 감지)")]
    [SerializeField] private List<VoxelObject> targets = new();

    [Header("Kickback Settings")]
    [SerializeField] private float failTime = 2f;
    [SerializeField] private string kickbackMessage = "킥백 사고";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    [Header("Contact Grace Period")]
    [SerializeField] private float contactGracePeriod = 0.3f;

    private float contactTimer = 0f;
    private float noContactTimer = 0f;
    private bool hasTriggeredAccident = false;

    private Vector3 WorldCenter => transform.position + transform.TransformDirection(centerOffset);

    private void Awake()
    {
        if (grinderController == null)
            grinderController = GetComponent<GrinderController>();

        if (grinderController == null)
            Debug.LogWarning("[GrinderKickbackDetector] GrinderController를 찾을 수 없습니다.");
    }

    private void Update()
    {
        if (hasTriggeredAccident) return;
        if (grinderController == null) return;

        if (!grinderController.IsGrabbed || !grinderController.IsActive)
        {
            ResetContact();
            return;
        }

        if (CheckVoxelContact())
        {
            noContactTimer = 0f;
            contactTimer += Time.deltaTime;
            UpdateCountdownText();

            if (contactTimer >= failTime)
                TriggerKickback();
        }
        else
        {
            noContactTimer += Time.deltaTime;
            if (noContactTimer >= contactGracePeriod)
                ResetContact();
        }
    }

    private bool CheckVoxelContact()
    {
        if (targets.Count > 0)
        {
            foreach (var voxel in targets)
            {
                if (voxel == null) continue;
                var col = voxel.GetComponent<Collider>();
                if (col == null) continue;
                if (IsInsideCylinder(col.bounds.ClosestPoint(WorldCenter))) return true;
            }
        }
        else
        {
            float broadRadius = Mathf.Max(cylinderRadius, cylinderHalfHeight) * 2f;
            Collider[] hits = Physics.OverlapSphere(WorldCenter, broadRadius);
            foreach (var col in hits)
            {
                if (col.GetComponent<VoxelObject>() == null) continue;
                if (IsInsideCylinder(col.bounds.ClosestPoint(WorldCenter))) return true;
            }
        }
        return false;
    }

    private bool IsInsideCylinder(Vector3 worldPos)
    {
        Vector3 diff = worldPos - WorldCenter;
        float alongAxis = Vector3.Dot(diff, transform.up);
        Vector3 radialVec = diff - alongAxis * transform.up;
        return Mathf.Abs(alongAxis) < cylinderHalfHeight && radialVec.magnitude < cylinderRadius;
    }

    private void UpdateCountdownText()
    {
        if (countdownText == null) return;
        countdownText.text = "킥백 사고 위험";
    }

    private void ResetContact()
    {
        contactTimer = 0f;
        noContactTimer = 0f;
        if (countdownText != null)
            countdownText.text = "";
    }

    private void TriggerKickback()
    {
        hasTriggeredAccident = true;

        if (showDebugLog)
            Debug.Log("[GrinderKickbackDetector] 킥백 사고 발생");

        if (countdownText != null)
            countdownText.text = "";

        if (SafetyPracticeManager.Instance != null)
            SafetyPracticeManager.Instance.TryFailAlways(kickbackMessage);
        else
            Debug.LogWarning("[GrinderKickbackDetector] SafetyPracticeManager 인스턴스 없음.");
    }

    // ─── Gizmos ───────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);

        Vector3 center = transform.position + transform.TransformDirection(centerOffset);
        Vector3 axis = transform.up;

        Vector3 perp1 = Vector3.Cross(axis, Vector3.up).normalized;
        if (perp1.sqrMagnitude < 0.001f)
            perp1 = Vector3.Cross(axis, Vector3.right).normalized;
        Vector3 perp2 = Vector3.Cross(axis, perp1).normalized;

        Vector3 top = center + axis * cylinderHalfHeight;
        Vector3 bot = center - axis * cylinderHalfHeight;

        DrawCircle(top, perp1, perp2, cylinderRadius);
        DrawCircle(bot, perp1, perp2, cylinderRadius);

        int lines = 8;
        for (int i = 0; i < lines; i++)
        {
            float angle = i * 2f * Mathf.PI / lines;
            Vector3 offset = (Mathf.Cos(angle) * perp1 + Mathf.Sin(angle) * perp2) * cylinderRadius;
            Gizmos.DrawLine(top + offset, bot + offset);
        }
    }

    private void DrawCircle(Vector3 center, Vector3 perp1, Vector3 perp2, float radius, int segments = 32)
    {
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * 2f * Mathf.PI / segments;
            float a2 = (i + 1) * 2f * Mathf.PI / segments;
            Vector3 p1 = center + (Mathf.Cos(a1) * perp1 + Mathf.Sin(a1) * perp2) * radius;
            Vector3 p2 = center + (Mathf.Cos(a2) * perp1 + Mathf.Sin(a2) * perp2) * radius;
            Gizmos.DrawLine(p1, p2);
        }
    }
}
