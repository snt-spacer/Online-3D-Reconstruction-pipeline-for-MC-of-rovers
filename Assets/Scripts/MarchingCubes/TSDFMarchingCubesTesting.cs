using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

public class TSDFMarchingCubesTesting: MonoBehaviour
{
    // Class Variables (editable from Unity Editor)
    [SerializeField] private bool visualizeTSDF = false;
    [SerializeField] private TSDFCreator tsdf_creator;

    // Private variables
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private List<Vector3> vertices = new List<Vector3>();
    private List<Color> vertexColors = new List<Color>();
    private List<int> triangles = new List<int>();

    // Mesh Variables
    private Mesh mesh;


    public void Awake()
    {
        // Create and initialize 3D mesh
        mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        meshFilter = new MeshFilter();
        meshFilter.mesh = mesh;
        meshFilter.sharedMesh = mesh;

    }

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        InvokeRepeating("MarchCubes", 0f, 5f);
    }

    private void MarchCubes()
    {
        var start = DateTime.Now;
        tsdf_creator.CreateTSDFGrid();
        var end = DateTime.Now;
        Debug.Log($"Elapsed 1: {(end - start).TotalMilliseconds} ms");
        
        start = DateTime.Now;
        if (tsdf_creator.GetTSDF() == null)
        {
            return;
        }

        clearMesh();

        for (int x = 0; x < tsdf_creator.sizeX - 1; x++)
            for (int y = 0; y < tsdf_creator.sizeY - 1; y++)
                for (int z = 0; z < tsdf_creator.sizeZ - 1; z++)
                {
                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cube[i] = tsdf_creator.tsdf[corner.x, corner.y, corner.z];
                    }
                    MarchCube(new Vector3(x, y, z), cube);
                }

        updateMesh();
        end = DateTime.Now;
        Debug.Log($"Elapsed 2: {(end - start).TotalMilliseconds} ms");
    }

    private int GetConfigIndex(float[] cube)
    {
        int idx = 0;
        for (int i = 0; i < 8; i++)
            if (cube[i] > tsdf_creator.tsdfThreshold) idx |= 1 << i;
        return idx;
    }

    private void MarchCube(Vector3 pos, float[] cube)
    {
        int config = GetConfigIndex(cube);
        if (config == 0 || config == 255) return;

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
            for (int v = 0; v < 3; v++)
            {
                int edge = MarchingTable.Triangles[config, edgeIndex++];
                if (edge == -1) return;

                /*				
				Vector3 p1 = MarchingTable.Edges[edge, 0];
				Vector3 p2 = MarchingTable.Edges[edge, 1];
				Vector3 world = minBounds + (pos + (p1 + p2) / 2f) * resolution;
				vertices.Add(world);
				triangles.Add(vertices.Count - 1);
				Vector3Int corner1 = Vector3Int.FloorToInt(pos + p1);
				Vector3Int corner2 = Vector3Int.FloorToInt(pos + p2);
				Color c1 = colorGrid[corner1.x, corner1.y, corner1.z];
				Color c2 = colorGrid[corner2.x, corner2.y, corner2.z];
				vertexColors.Add(Color.Lerp(c1, c2, 0.5f));
				 */

                Vector3 p1 = MarchingTable.Edges[edge, 0];
                Vector3 p2 = MarchingTable.Edges[edge, 1];

                Vector3Int corner1 = Vector3Int.FloorToInt(pos + p1);
                Vector3Int corner2 = Vector3Int.FloorToInt(pos + p2);

                // Check bounds
                if (!InBounds(corner1) || !InBounds(corner2)) continue;

                float v1 = tsdf_creator.tsdf[corner1.x, corner1.y, corner1.z];
                float v2 = tsdf_creator.tsdf[corner2.x, corner2.y, corner2.z];

                // Avoid divide by zero and invalid interpolation
                if (Mathf.Abs(v1 - v2) < 1e-6f) continue;

                // Interpolate to find approximate zero-crossing
                float alpha = v1 / (v1 - v2);
                alpha = Mathf.Clamp01(alpha); // prevent overshoot

                Vector3 interpolated = Vector3.Lerp(p1, p2, alpha);
                Vector3 world = tsdf_creator.tsdfOrigin + (pos + interpolated) * tsdf_creator.resolution;

                vertices.Add(world);
                triangles.Add(vertices.Count - 1);

                // Use same alpha for interpolating color
                Color c1 = tsdf_creator.tsdfColor[corner1.x, corner1.y, corner1.z];
                Color c2 = tsdf_creator.tsdfColor[corner2.x, corner2.y, corner2.z];
                vertexColors.Add(Color.Lerp(c1, c2, alpha));


            }
    }

    private bool InBounds(Vector3Int idx)
    {
        return idx.x >= 0 && idx.x < tsdf_creator.sizeX &&
               idx.y >= 0 && idx.y < tsdf_creator.sizeY &&
               idx.z >= 0 && idx.z < tsdf_creator.sizeZ;
    }

    private void updateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = vertexColors.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void clearMesh()
    {
        triangles.Clear();
        vertices.Clear();
        vertexColors.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!visualizeTSDF || tsdf_creator.tsdf == null) return;
        for (int x = 0; x < tsdf_creator.sizeX; x++)
            for (int y = 0; y < tsdf_creator.sizeY; y++)
                for (int z = 0; z < tsdf_creator.sizeZ; z++)
                {
                    float val = Mathf.Clamp01((tsdf_creator.tsdf[x, y, z] + tsdf_creator.truncation) / (2f * tsdf_creator.truncation));
                    Gizmos.color = new Color(val, val, val);
                    Gizmos.DrawSphere(tsdf_creator.tsdfOrigin + new Vector3(x, y, z) * tsdf_creator.resolution, 0.05f * tsdf_creator.resolution);
                }
    }

}
