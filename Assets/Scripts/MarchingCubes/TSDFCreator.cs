using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TSDFCreator : MonoBehaviour
{
    // TSDF Grid Parameters 
    [SerializeField] protected internal float resolution;
    [SerializeField] protected internal float tsdfThreshold;
    [SerializeField] protected internal float truncation;
    [SerializeField] private PointCloudSubscriber pointCloudSubscriber;

    // Managed TSDF (for your existing MC code)
    protected internal float[,,] tsdf;
    protected internal Color[,,] tsdfColor;

    // Internal helpers
    protected internal Vector3 tsdfOrigin;
    protected internal int sizeX, sizeY, sizeZ;
    private int radius;
    private int strideYZ;

    void Start()
    {
        radius = Mathf.CeilToInt(truncation / resolution);
    }

    protected internal void CreateTSDFGrid()
    {
        // 1) Pull point cloud
        Vector3[] pcVertices = pointCloudSubscriber.getPC_vertices();
        Color[] pcColors = pointCloudSubscriber.getPC_colors();
        if (pcVertices == null || pcVertices.Length == 0) return;
        if (pcColors == null || pcColors.Length == 0) return;

        // 2) Compute bounds & grid dimensions
        Vector3 minP = pcVertices[0], maxP = pcVertices[0];
        foreach (var p in pcVertices)
        {
            minP = Vector3.Min(minP, p);
            maxP = Vector3.Max(maxP, p);
        }
        tsdfOrigin = minP;
        Vector3 span = maxP - minP;
        sizeX = Mathf.CeilToInt(span.x / resolution) + 1;
        sizeY = Mathf.CeilToInt(span.y / resolution) + 1;
        sizeZ = Mathf.CeilToInt(span.z / resolution) + 1;
        strideYZ = sizeY * sizeZ;

        int totalVoxels = sizeX * sizeY * sizeZ;

        // 3) Allocate flat NativeArrays
        var tsdfFlat = new NativeArray<float>(totalVoxels, Allocator.TempJob);
        var tsdfColorFlat = new NativeArray<Color32>(totalVoxels, Allocator.TempJob);

        // 4) Parallel reset to +truncation
        var resetJob = new TSDFResetJob
        {
            tsdf = tsdfFlat,
            truncation = truncation
        };
        JobHandle hReset = resetJob.Schedule(totalVoxels, 64);

        // 5) Copy point cloud into NativeArrays
        var naPts = new NativeArray<float3>(pcVertices.Length, Allocator.TempJob);
        var naCols = new NativeArray<Color32>(pcColors.Length, Allocator.TempJob);
        for (int i = 0; i < pcVertices.Length; i++)
        {
            naPts[i] = pcVertices[i];
            Color c = pcColors[i];
            naCols[i] = new Color32(
                (byte)(c.r * 255f),
                (byte)(c.g * 255f),
                (byte)(c.b * 255f),
                (byte)(c.a * 255f)
            );
        }

        // 6) Burst-compiled TSDF integration (one iteration per point)
        var integJob = new TSDFIntegrationJob
        {
            points = naPts,
            colors = naCols,
            tsdf = tsdfFlat,
            tsdfColor = tsdfColorFlat,
            origin = tsdfOrigin,
            resolution = resolution,
            truncation = truncation,
            radius = radius,
            sizeX = sizeX,
            sizeY = sizeY,
            sizeZ = sizeZ,
            strideYZ = strideYZ
        };
        var hInt = integJob.Schedule(pcVertices.Length, 64, hReset);

        // 7) Wait for completion
        hInt.Complete();

        // 8) Unflatten back into managed 3D arrays for your current MC code
        tsdf = new float[sizeX, sizeY, sizeZ];
        tsdfColor = new Color[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    int idx = x * strideYZ + y * sizeZ + z;
                    tsdf[x, y, z] = tsdfFlat[idx];
                    Color32 c32 = tsdfColorFlat[idx];
                    tsdfColor[x, y, z] = new Color(
                        c32.r / 255f,
                        c32.g / 255f,
                        c32.b / 255f,
                        c32.a / 255f
                    );
                }

        // 9) Clean up NativeArrays
        naPts.Dispose();
        naCols.Dispose();
        tsdfFlat.Dispose();
        tsdfColorFlat.Dispose();
    }

    public float[,,] GetTSDF() => tsdf;

    void OnDestroy()
    {
        // nothing to dispose here since we only used TempJob allocators
    }

    // === Burst Jobs ===

    [BurstCompile]
    struct TSDFResetJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float> tsdf;
        public float truncation;
        public void Execute(int i) => tsdf[i] = truncation;
    }

    [BurstCompile]
    struct TSDFIntegrationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> points;
        [ReadOnly] public NativeArray<Color32> colors;

        // Disable the parallel-for safety restriction so we can write arbitrary indices
        [NativeDisableParallelForRestriction]
        public NativeArray<float> tsdf;
        [NativeDisableParallelForRestriction]
        public NativeArray<Color32> tsdfColor;

        public float3 origin;
        public float resolution;
        public float truncation;
        public int radius;
        public int sizeX, sizeY, sizeZ;
        public int strideYZ;

        public void Execute(int i)
        {
            float3 p = points[i];
            Color32 c = colors[i];

            float3 rel = (p - origin) / resolution;
            int vx = (int)math.floor(rel.x);
            int vy = (int)math.floor(rel.y);
            int vz = (int)math.floor(rel.z);

            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        int x = vx + dx, y = vy + dy, z = vz + dz;
                        if (x < 0 || y < 0 || z < 0 || x >= sizeX || y >= sizeY || z >= sizeZ)
                            continue;

                        int idx = x * strideYZ + y * sizeZ + z;
                        float3 voxelPos = origin + new float3(x, y, z) * resolution;
                        float dist = math.distance(voxelPos, p);
                        float sdf = dist - resolution;
                        float v = math.clamp(sdf, -truncation, truncation);

                        if (v < tsdf[idx])
                        {
                            tsdf[idx] = v;
                            tsdfColor[idx] = c;
                        }
                    }
        }
    }
}
