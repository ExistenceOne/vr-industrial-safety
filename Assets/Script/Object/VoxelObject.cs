using UnityEngine;

public class VoxelObject : MonoBehaviour
{
    [Header("복셀 해상도")]
    public int resX = 20;
    public int resY = 40;
    public int resZ = 10;

    [Header("크기 (월드 단위)")]
    public float sizeX = 0.1f;
    public float sizeY = 1f;
    public float sizeZ = 1f;

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

        voxelSizeX = sizeX / resX;
        voxelSizeY = sizeY / resY;
        voxelSizeZ = sizeZ / resZ;

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
        meshCollider.sharedMesh = mesh;
    }
}
