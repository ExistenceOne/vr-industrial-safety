using UnityEngine;

public class VoxelFullCarveFailTrigger : MonoBehaviour
{
    [Header("감지할 복셀 오브젝트")]
    [SerializeField] private VoxelObject targetVoxel;

    [Header("관통 감지 설정")]
    [SerializeField] [Range(0f, 1f)] private float depthThreshold = 1f;
    [SerializeField] private int minCarvedPerLayer = 4;

    [Header("실패 메시지")]
    [SerializeField] private string failMessage = "드릴이 완전히 관통되었습니다!";

    private bool triggered = false;

    private void Update()
    {
        if (triggered || targetVoxel == null) return;

        float depth = targetVoxel.GetCarvedDepthRatio(-targetVoxel.transform.forward, minCarvedPerLayer);
        if (depth >= depthThreshold)
        {
            triggered = true;
            if (SafetyPracticeManager.Instance != null)
                SafetyPracticeManager.Instance.TryFailAlways(failMessage);
        }
    }
}
