using UnityEngine;

public class VoxelObject : MonoBehaviour
{
    [Header("복셀 한 칸 크기 (월드 단위)")]
    public float voxelSize = 0.025f;

    [Header("복셀 개수 (실제 크기 = voxelSize * res)")]
    public int resX = 4;
    public int resY = 40;
    public int resZ = 40;

    [HideInInspector] public float sizeX;
    [HideInInspector] public float sizeY;
    [HideInInspector] public float sizeZ;

    [Header("재질 강도 (0.1 = 물렁물렁, 1 = 보통, 클수록 단단함)")]
    [Range(0.1f, 5f)]
    public float hardness = 1f;

    [Header("깎일 때 재생할 파티클 (객체별로 지정)")]
    public ParticleSystem carveParticle;

    [Header("한 번에 방출할 파티클 수")]
    public int particleEmitCount = 10;

    [Header("등록할 GrinderController")]
    public GrinderController blade;

    private float[,,] voxels;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private float voxelSizeX, voxelSizeY, voxelSizeZ;

    private float colliderUpdateTimer = 0f;
    private const float ColliderUpdateInterval = 0.2f;
    private bool meshDirty = false;

    // 최초 복셀 수 (초기화 시 1회 계산)
    private int initialVoxelCount = 0;

    private void Awake()
    {
        if (GetComponent<MeshFilter>() == null)   gameObject.AddComponent<MeshFilter>();
        if (GetComponent<MeshRenderer>() == null)  gameObject.AddComponent<MeshRenderer>();
        if (GetComponent<MeshCollider>() == null)  gameObject.AddComponent<MeshCollider>();

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        meshFilter   = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.convex = false;

        sizeX = voxelSize * resX;
        sizeY = voxelSize * resY;
        sizeZ = voxelSize * resZ;

        voxelSizeX = voxelSize;
        voxelSizeY = voxelSize;
        voxelSizeZ = voxelSize;

        voxels = new float[resX + 1, resY + 1, resZ + 1];
        for (int x = 0; x <= resX; x++)
        for (int y = 0; y <= resY; y++)
        for (int z = 0; z <= resZ; z++)
        {
            bool isBorder = x == 0 || x == resX || y == 0 || y == resY || z == 0 || z == resZ;
            voxels[x, y, z] = isBorder ? 0f : 1f;
        }

        initialVoxelCount = CountActiveVoxels();

        RebuildMesh();

        if (blade != null)
            blade.RegisterTarget(this);
        else
            Debug.LogWarning("VoxelObject: Blade가 연결되지 않았습니다.");

        Debug.Log($"VoxelObject 초기화 완료. Mesh vertexCount: {meshFilter.sharedMesh?.vertexCount}");
    }

    private void Update()
    {
        if (!meshDirty) return;

        colliderUpdateTimer -= Time.deltaTime;
        if (colliderUpdateTimer > 0f) return;

        colliderUpdateTimer = ColliderUpdateInterval;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshDirty = false;
    }

    /// <summary>현재 활성 복셀 수를 반환합니다.</summary>
    public int GetCurrentVoxelCount()
    {
        return CountActiveVoxels();
    }

    /// <summary>최초 복셀 수를 반환합니다.</summary>
    public int GetInitialVoxelCount()
    {
        return initialVoxelCount;
    }

    /// <summary>현재 깎인 비율 (0~1)을 반환합니다. 1 = 100% 깎임</summary>
    public float GetCarvedRatio()
    {
        if (initialVoxelCount == 0) return 0f;
        int current = CountActiveVoxels();
        return 1f - (float)current / initialVoxelCount;
    }

    /// <summary>지정 방향으로 입구면부터 연속으로 깎인 깊이 비율 (0~1)을 반환합니다.</summary>
    /// <param name="minCarvedPerLayer">레이어당 깎인 것으로 인정할 최소 복셀 수</param>
    public float GetCarvedDepthRatio(Vector3 worldDirection, int minCarvedPerLayer = 1)
    {
        Vector3 localDir = transform.InverseTransformDirection(worldDirection).normalized;

        float ax = Mathf.Abs(localDir.x);
        float ay = Mathf.Abs(localDir.y);
        float az = Mathf.Abs(localDir.z);

        int carved = 0;
        int total;

        if (az >= ax && az >= ay)
        {
            total = resZ - 1;
            bool forward = localDir.z > 0;
            for (int i = 1; i < resZ; i++)
            {
                int z = forward ? i : resZ - i;
                if (IsLayerCarved(z, axis: 2, minCarvedPerLayer)) carved = i;
                else break;
            }
        }
        else if (ay >= ax)
        {
            total = resY - 1;
            bool forward = localDir.y > 0;
            for (int i = 1; i < resY; i++)
            {
                int y = forward ? i : resY - i;
                if (IsLayerCarved(y, axis: 1, minCarvedPerLayer)) carved = i;
                else break;
            }
        }
        else
        {
            total = resX - 1;
            bool forward = localDir.x > 0;
            for (int i = 1; i < resX; i++)
            {
                int x = forward ? i : resX - i;
                if (IsLayerCarved(x, axis: 0, minCarvedPerLayer)) carved = i;
                else break;
            }
        }

        return total > 0 ? (float)carved / total : 0f;
    }

    private bool IsLayerCarved(int sliceIndex, int axis, int minCount)
    {
        int count = 0;
        if (axis == 2)
        {
            for (int x = 1; x < resX; x++)
            for (int y = 1; y < resY; y++)
                if (voxels[x, y, sliceIndex] < 0.5f && ++count >= minCount) return true;
        }
        else if (axis == 1)
        {
            for (int x = 1; x < resX; x++)
            for (int z = 1; z < resZ; z++)
                if (voxels[x, sliceIndex, z] < 0.5f && ++count >= minCount) return true;
        }
        else
        {
            for (int y = 1; y < resY; y++)
            for (int z = 1; z < resZ; z++)
                if (voxels[sliceIndex, y, z] < 0.5f && ++count >= minCount) return true;
        }
        return false;
    }

    private int CountActiveVoxels()
    {
        int count = 0;
        for (int x = 0; x <= resX; x++)
        for (int y = 0; y <= resY; y++)
        for (int z = 0; z <= resZ; z++)
        {
            if (voxels[x, y, z] > 0f) count++;
        }
        return count;
    }

    public void Carve(Vector3 worldCenter, float worldRadius, float depth, Vector3 worldCarveAxis)
    {
        Vector3 localCenter    = transform.InverseTransformPoint(worldCenter);
        Vector3 localCarveAxis = transform.InverseTransformDirection(worldCarveAxis).normalized;

        float localRadius     = worldRadius / transform.lossyScale.x;
        float localHalfHeight = blade.cylinderHalfHeight / transform.lossyScale.y;

        float effectiveDepth = depth / Mathf.Max(0.01f, hardness);

        bool changed = false;

        for (int x = 0; x <= resX; x++)
        for (int y = 0; y <= resY; y++)
        for (int z = 0; z <= resZ; z++)
        {
            Vector3 voxelLocalPos = VoxelToLocal(x, y, z);
            Vector3 diff = voxelLocalPos - localCenter;

            float alongAxis = Vector3.Dot(diff, localCarveAxis);
            if (Mathf.Abs(alongAxis) > localHalfHeight) continue;

            Vector3 radial = diff - alongAxis * localCarveAxis;
            float dist     = radial.magnitude / localRadius;
            if (dist > 1f) continue;

            float carveAmount = effectiveDepth * (1f - dist);
            voxels[x, y, z] = Mathf.Max(0f, voxels[x, y, z] - carveAmount);
            changed = true;
        }

        if (!changed) return;

        RebuildMesh();
        meshDirty = true;

        EmitParticle(worldCenter, worldCarveAxis);
    }

    private void EmitParticle(Vector3 worldCenter, Vector3 worldCarveAxis)
    {
        if (carveParticle == null) return;

        carveParticle.transform.position = worldCenter;
        carveParticle.transform.rotation = Quaternion.LookRotation(-worldCarveAxis);
        carveParticle.Emit(new ParticleSystem.EmitParams(), particleEmitCount);
    }

    private Vector3 VoxelToLocal(int x, int y, int z)
    {
        return new Vector3(
            x * voxelSizeX - sizeX / 2f,
            y * voxelSizeY - sizeY / 2f,
            z * voxelSizeZ - sizeZ / 2f
        );
    }

    private void RebuildMesh()
    {
        Mesh mesh = MarchingCubes.Generate(voxels, resX, resY, resZ, voxelSizeX, voxelSizeY, voxelSizeZ, sizeX, sizeY, sizeZ);
        meshFilter.sharedMesh = mesh;
    }

    private void OnDrawGizmosSelected()
    {
        float sx = voxelSize * resX;
        float sy = voxelSize * resY;
        float sz = voxelSize * resZ;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(sx, sy, sz));
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(sx, sy, sz));
    }
}
