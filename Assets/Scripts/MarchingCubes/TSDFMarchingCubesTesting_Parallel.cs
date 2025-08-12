using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;

public class TSDFMarchingCubesTesting_Parallel: MonoBehaviour
{
    // Class Variables (editable from Unity Editor)
    [SerializeField] private TSDFCreator tsdf_creator;
    [SerializeField] private float invokeInterval = 5f;

    // Mesh variables
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private Mesh mesh;

    // Lookup Marching Tables
    private NativeArray<float3> edgeVertexPositions;
    private NativeArray<int> triangleTable;

    // Burst Arrays
    NativeArray<float> tsdfFlat;
    NativeArray<Color32> tsdfColorsFlat;

    public void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        // Create and initialize 3D mesh
        mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        meshFilter.mesh = mesh;
        meshFilter.sharedMesh = mesh;

        // Initialize lookup table for Edge Positions
        edgeVertexPositions = new NativeArray<float3>(12 * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int e = 0; e < 12; e++)
        {
            edgeVertexPositions[e * 2 + 0] = MarchingTable.Edges[e, 0];
            edgeVertexPositions[e * 2 + 1] = MarchingTable.Edges[e, 1];
        }

        // Initialize lookip table for Triangle Positions (configurations)
        triangleTable = new NativeArray<int>(256 * 16, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                triangleTable[i * 16 + j] = MarchingTable.Triangles[i, j];
            }
        }

        InvokeRepeating("MarchCubes", 0f, invokeInterval);
        Debug.Log($"after invoke of MC");
    }

    private void OnDestroy()
    {
        if(edgeVertexPositions.IsCreated) edgeVertexPositions.Dispose();
        if(triangleTable.IsCreated) triangleTable.Dispose();
    }

    private void MarchCubes()
    {

        //Debug.Log($"Beginning of MC");

        //var start = DateTime.Now;
        tsdf_creator.CreateTSDFGrid();
        //var end = DateTime.Now;
        //Debug.Log($"Elapsed 1: {(end - start).TotalMilliseconds} ms");
        
        //start = DateTime.Now;
        
        // Extract TSDF and Color Grids
        var tsdf = tsdf_creator.tsdf;
        var tsdfColor = tsdf_creator.tsdfColor;

        // Check if data is available
        if (tsdfColor == null) return;

        // Define Grid sizes for flatenning TSDF and Color grids to use in Burst
        int sizeX = tsdf_creator.sizeX;
        int sizeY = tsdf_creator.sizeY;
        int sizeZ = tsdf_creator.sizeZ;
        int strideYZ = sizeY * sizeZ;
        int totalVoxels = (sizeX) * (sizeY) * (sizeZ);

        // Initialize Flat arrays
        // We flatten the TSDF from 3D to 1D, such that each index corresponds to a signed distance
        tsdfFlat = new NativeArray<float>(totalVoxels, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        tsdfColorsFlat = new NativeArray<Color32>(totalVoxels,Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        // Copy over 3D voxel grids into Flat TSDF and Color Arrays for Burst
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    // index conversion from 3D to 1D 
                    int idx = x * strideYZ + y * sizeZ + z;
                    Color c = tsdf_creator.tsdfColor[x, y, z];

                    // Copy Data
                    tsdfFlat[idx] = tsdf[x, y, z];
                    // tsdfColorsFlat[idx] = tsdfColor[x, y, z];

                    tsdfColorsFlat[idx] = new Color32(
     (byte)(c.r * 255f),
     (byte)(c.g * 255f),
     (byte)(c.b * 255f),
     (byte)(c.a * 255f));
                }
            }
        }

        // Prepare the Marching Cube Corners into a Flat 1D Array for Burst
        var cornerOffsets = new NativeArray<int>(8, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < 8; i++)
        {
            //var c = MarchingTable.Corners[i];

            int cx = (int)MarchingTable.Corners[i].x;
            int cy = (int)MarchingTable.Corners[i].y;
            int cz = (int)MarchingTable.Corners[i].z;

            //cornerOffsets[i] = c.x * strideYZ + c.y * sizeZ + c.z;
            cornerOffsets[i] = cx * strideYZ + cy * sizeZ + cz;

        }

        // Create NativeLists for vertices, indices, and colors to be used for Burst
        // Contain the vertices, triangles and colors of the mesh
        // var vertices = new NativeList<float3>(totalVoxels * 5 * 3, Allocator.TempJob);
        // var colors = new NativeList<Color>(totalVoxels * 5 * 3, Allocator.TempJob);
        // var triangles = new NativeList<int>(totalVoxels * 5 * 3, Allocator.TempJob);


        var outVerts = new NativeArray<float3>(totalVoxels * 5 * 3, Allocator.TempJob);
        var outCols = new NativeArray<Color32>(totalVoxels * 5 * 3, Allocator.TempJob);


        // Define the amount of cubes we have in total to pass over the grid
        // The number of cubes is essentially the size of the Flat TSDF grid - 1
        
        int cubeCount = (sizeX - 1) * (sizeY - 1) * (sizeZ - 1);

        var triangle_counter = new NativeArray<int>(1, Allocator.TempJob);
        triangle_counter[0] = 0;

    // Create the Marching Cubes Job and pass the relevant data
    var marchingCubesJob = new MarchCubeJob
        {
            tsdf = tsdfFlat,
            tsdfColors = tsdfColorsFlat,
            cornerOffsets = cornerOffsets,
            edgeVertexPos = edgeVertexPositions,
            triangleTable = triangleTable,
            sizeX = sizeX,
            sizeY = sizeY,
            sizeZ = sizeZ,
            strideYZ = strideYZ,
            threshold = tsdf_creator.tsdfThreshold,
            tsdfOrigin = tsdf_creator.tsdfOrigin,
            resolution = tsdf_creator.resolution,
            //vertices = vertices.AsParallelWriter(),
            //vertexColors = colors.AsParallelWriter(),
            //triangles = triangles.AsParallelWriter(),
            outVerts = outVerts,
            outCols = outCols,
            counter = triangle_counter,
        };

        // Schedule the Marching Cubes job and wait for it to complete
        var handleMarchingCubesJob = marchingCubesJob.Schedule(cubeCount, 64);
        handleMarchingCubesJob.Complete();

        // Convert native lists to managed arrays
        //var vCount = vertices.Length;
        //var cCount = colors.Length;

        int vCount = triangle_counter[0];

        Vector3[] meshVerts = new Vector3[vCount];
        //Color[] meshCols = new Color[cCount];

        Color[] meshCols = new Color[vCount];

        // Extract the vertices and the colors computed by the Job
        for (int i = 0; i < vCount; i++)
        {
            //meshVerts[i] = vertices[i];
            //meshCols[i] = colors[i];

            meshVerts[i] = outVerts[i];
            meshCols[i] = outCols[i];
        }

        // Define the mesh triangles. Each mesh triangle is formed by 3 vertices in order.
        int triangleCount = triangle_counter[0];
        int[] meshTris = new int[triangleCount];

        for (int i = 0; i < triangleCount; i++)
        {
            meshTris[i] = i;
        }

        //for (int t = 0; t < meshTris.Length; t += 3)
        //{
        //    int tmp = meshTris[t + 1];
        //    meshTris[t + 1] = meshTris[t + 2];
        //    meshTris[t + 2] = tmp;
        //}

        // Clear the previous mesh and update it with new data
        mesh.Clear();
        mesh.vertices = meshVerts;
        mesh.colors = meshCols;
        mesh.triangles = meshTris;
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        // Dispose NativeArrays and NativeLists to free up CPU memory buffers
        tsdfFlat.Dispose();
        tsdfColorsFlat.Dispose();
        cornerOffsets.Dispose();
        //vertices.Dispose();
        //colors.Dispose();
        //triangles.Dispose();
        
        outVerts.Dispose();
        outCols.Dispose();  
        triangle_counter.Dispose();

        //end = DateTime.Now;
        //Debug.Log($"Elapsed 2: {(end - start).TotalMilliseconds} ms");
    }

    // ==== Burst Jobs ===
    [BurstCompile]
    unsafe struct MarchCubeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> tsdf; // Flat TSDF 
        [ReadOnly] public NativeArray<Color32> tsdfColors; // Flat TSDF colors
        [ReadOnly] public NativeArray<int> cornerOffsets;
        [ReadOnly] public NativeArray<float3> edgeVertexPos;
        [ReadOnly] public NativeArray<int> triangleTable;

        [ReadOnly] public int sizeX, sizeY, sizeZ, strideYZ;
        [ReadOnly] public float threshold, resolution;
        [ReadOnly] public float3 tsdfOrigin;

        //public NativeList<float3>.ParallelWriter vertices;
        //public NativeList<Color>.ParallelWriter vertexColors;
        //public NativeList<int>.ParallelWriter triangles;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> outVerts;  // pre-sized to worst-case

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<Color32> outCols;   // same

        [NativeDisableParallelForRestriction]
        public NativeArray<int> counter;


        public void Execute(int cubeIdx)
        {

            int* counterPtr = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(counter);

            int zCount = sizeZ - 1;
            int yCount = sizeY - 1;
            int z = cubeIdx % zCount;
            int y = (cubeIdx / zCount) % yCount;
            int x = cubeIdx / (zCount * yCount);
            int tsdfSignedDistFlat = x * strideYZ + y * sizeZ + z;

            // Compute the 8 corners of the Cubes based on the current signed distance value
            float v0 = tsdf[tsdfSignedDistFlat + cornerOffsets[0]];
            float v1 = tsdf[tsdfSignedDistFlat + cornerOffsets[1]];
            float v2 = tsdf[tsdfSignedDistFlat + cornerOffsets[2]];
            float v3 = tsdf[tsdfSignedDistFlat + cornerOffsets[3]];
            float v4 = tsdf[tsdfSignedDistFlat + cornerOffsets[4]];
            float v5 = tsdf[tsdfSignedDistFlat + cornerOffsets[5]];
            float v6 = tsdf[tsdfSignedDistFlat + cornerOffsets[6]];
            float v7 = tsdf[tsdfSignedDistFlat + cornerOffsets[7]];

            // Compute the configuation of the cube to determine how to generate the triangles inside it
            int config = 0;
            if (v0 > threshold) config |= 1 << 0;
            if (v1 > threshold) config |= 1 << 1;
            if (v2 > threshold) config |= 1 << 2;
            if (v3 > threshold) config |= 1 << 3;
            if (v4 > threshold) config |= 1 << 4;
            if (v5 > threshold) config |= 1 << 5;
            if (v6 > threshold) config |= 1 << 6;
            if (v7 > threshold) config |= 1 << 7;
            if (config == 0 || config == 255) return;

            // The Triangle Table is flattened as a 1D array of length 256 × 16.
            // triBase = config * 16 picks the start of the Triangle 16-entry row.
            float3 cubePos = new float3(x, y, z);
            int triBase = config * 16;

            // **step through the triangleTable in groups of 3 edges**  
            for (int t = 0; t < 16; t += 3)
            {
                int e0 = triangleTable[triBase + t + 0];
                int e1 = triangleTable[triBase + t + 1];
                int e2 = triangleTable[triBase + t + 2];

                if (e0 < 0 || e1 < 0 || e2 < 0) break;              // no more triangles for this cube

                // interpolate three vertices + colors exactly as before…
                float3 wp0 = InterpolateEdge(tsdfSignedDistFlat, e0, out Color32 c0);
                float3 wp1 = InterpolateEdge(tsdfSignedDistFlat, e1, out Color32 c1);
                float3 wp2 = InterpolateEdge(tsdfSignedDistFlat, e2, out Color32 c2);

                // **atomically reserve 3 slots at once**
                // Interlocked.Add returns the *new* value, so subtract 3
                int baseIdx3 = Interlocked.Add(ref counterPtr[0], 3) - 3;

                outVerts[baseIdx3 + 0] = wp0;
                outCols[baseIdx3 + 0] = c0;

                outVerts[baseIdx3 + 1] = wp1;
                outCols[baseIdx3 + 1] = c1;

                outVerts[baseIdx3 + 2] = wp2;
                outCols[baseIdx3 + 2] = c2;
            }
        }
        // helper you can add inside the job to keep it clean:
        float3 InterpolateEdge(int baseIdx, int edge, out Color32 outColor)
        {
            float3 p1 = edgeVertexPos[edge * 2 + 0];
            float3 p2 = edgeVertexPos[edge * 2 + 1];
            int off1 = (int)p1.x * strideYZ + (int)p1.y * sizeZ + (int)p1.z;
            int off2 = (int)p2.x * strideYZ + (int)p2.y * sizeZ + (int)p2.z;
            float s1 = tsdf[baseIdx + off1], s2 = tsdf[baseIdx + off2];
            float alpha = math.clamp(s1 / (s1 - s2), 0f, 1f);
            float3 local = math.lerp(p1, p2, alpha);
            float3 world = tsdfOrigin + (new float3(baseIdx / strideYZ, (baseIdx % strideYZ) / sizeZ, baseIdx % sizeZ) + local) * resolution;
            // color
            Color32 c1 = tsdfColors[baseIdx + off1];
            Color32 c2 = tsdfColors[baseIdx + off2];
            outColor = new Color32(
              (byte)math.lerp(c1.r, c2.r, alpha),
              (byte)math.lerp(c1.g, c2.g, alpha),
              (byte)math.lerp(c1.b, c2.b, alpha),
              (byte)math.lerp(c1.a, c2.a, alpha)
            );
            return world;
        }
    }
}