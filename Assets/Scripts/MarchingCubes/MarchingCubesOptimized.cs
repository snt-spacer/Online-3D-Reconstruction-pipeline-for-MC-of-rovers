using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MarchingCubesOptimized : MonoBehaviour
{
    // Class Variables (editable from Unity Editor)
    [SerializeField] private float resolution = 0.1f;       // voxel size
    [SerializeField] private float tsdfThreshold = 0.01f;   // 
    [SerializeField] private float truncation = 0.2f;
    [SerializeField] private bool visualizeTSDF = false;
    [SerializeField] private PointCloudSubscriber pointCloudSubscriber;
    // Private variables
    private float[,,] tsdf;
    private Vector3 tsdf_origin;
    private int sizeX, sizeY, sizeZ;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private List<Vector3> vertices = new List<Vector3>();
    private List<Color> vertexColors = new List<Color>();
    private List<int> triangles = new List<int>();
    private Color[,,] colorGrid;

    private float[,,] weightGrid;
    private bool tsdfInitialized = false;

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // First initialization of TSDF grid to fixed size (subject to change if tsdf grid becomes bigger)
        initializeTSDF();
        
        // TSDF Grid update and Meshing with MC
        InvokeRepeating("CreateTSDF", 0f, 5f);
    }

    private void initializeTSDF()
    {
        // Fixed Sizes for TSDF
        sizeX = Mathf.CeilToInt(10 / resolution) + 1;
        sizeY = Mathf.CeilToInt(10 / resolution) + 1;
        sizeZ = Mathf.CeilToInt(10 / resolution) + 1;

        // Initialize TSDF and Color Grids size
        tsdf = new float[sizeX, sizeY, sizeZ];
        colorGrid = new Color[sizeX, sizeY, sizeZ];
        weightGrid = new float[sizeX, sizeY, sizeZ];

        // Initialize TSDF to Truncation
        // Truncation is the maximum signed-distance from the surface 
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    tsdf[x, y, z] = truncation;
                    colorGrid[x, y, z] = new Color(0, 0, 1);
                    weightGrid[x, y, z] = 0f;
                }
            }
        }
    }

    private void copyTSDF(Vector3[] pc_vertices)
    {

        // Compute World-axis aligned bounding box
        Vector3 min_point = pc_vertices[0];
        Vector3 max_point = pc_vertices[0];

        foreach (var p in pc_vertices)
        {
            min_point = Vector3.Min(min_point, p);
            max_point = Vector3.Max(max_point, p);
        }

        // Compute origin of TSDF grid
        tsdf_origin = min_point;

        // Compute the the amount of voxels required in each dimension x,y,z
        Vector3 size = max_point - min_point;

        // Allocate size to TSDF grids
        // Resolution is the size of each voxel
        // + 1 is added to make sure we have enough voxels to cover the full range
        int sizeX_new = Mathf.CeilToInt(size.x / resolution) + 1;
        int sizeY_new = Mathf.CeilToInt(size.y / resolution) + 1;
        int sizeZ_new = Mathf.CeilToInt(size.z / resolution) + 1;

        if (sizeX_new > sizeX + 10 || sizeY_new > sizeY + 10 || sizeZ_new > sizeZ + 10)
        {

            float[,,] tsdf_new;
            float[,,] weightGrid_new;
            Color[,,] colorGrid_new;

            tsdf_new = new float[sizeX_new, sizeY_new, sizeZ_new];
            weightGrid_new = new float[sizeX_new, sizeY_new, sizeZ_new];
            colorGrid_new = new Color[sizeX_new, sizeY_new, sizeZ_new];

            for (int x = 0; x < sizeX_new; x++)
            {
                for (int y = 0; y < sizeY_new; y++)
                {
                    for (int z = 0; z < sizeZ_new; z++)
                    {
                        if(x >= sizeX || y >= sizeY || z >= sizeZ)
                        {
                            tsdf_new[x, y, z] = truncation;
                            colorGrid_new[x, y, z] = Color.black;
                            weightGrid[x, y, z] = 0f;
                        }
                        else
                        {
                            tsdf_new[x, y, z] = tsdf[x, y, z];
                            colorGrid_new[x, y, z] = colorGrid[x, y, z];
                            weightGrid_new[x, y, z] = weightGrid[x, y, z];
                        }

                    }
                }
            }

            tsdf = tsdf_new;
            colorGrid = colorGrid_new;
            weightGrid = weightGrid_new;

            sizeX = sizeX_new;
            sizeY = sizeY_new;
            sizeZ = sizeZ_new;
        }
    }

    private void CreateTSDF()
    {
        // Extract points from Point Cloud
        Vector3[] pc_vertices = pointCloudSubscriber.getPC_vertices();
        Color[] pc_colors = pointCloudSubscriber.getPC_colors();

        // Check if there exist 3D vertices from input point cloud
        if (pc_vertices == null || pc_colors == null) return;


        // copyTSDF(pc_vertices);

        if (!tsdfInitialized) {

            // Compute World-axis aligned bounding box
            Vector3 min_point = pc_vertices[0];
            Vector3 max_point = pc_vertices[0];

            foreach (var p in pc_vertices)
            {
                min_point = Vector3.Min(min_point, p);
                max_point = Vector3.Max(max_point, p);
            }

            tsdf_origin = min_point;
            tsdfInitialized = true;
        }
        // Compute origin of TSDF grid
        

        // Compute the the amount of voxels required in each dimension x,y,z
        // Vector3 size = max_point - min_point;

        // Allocate size to TSDF grids
        // Resolution is the size of each voxel
        // + 1 is added to make sure we have enough voxels to cover the full range
        /*        sizeX = Mathf.CeilToInt(size.x / resolution) + 1;
                sizeY = Mathf.CeilToInt(size.y / resolution) + 1;
                sizeZ = Mathf.CeilToInt(size.z / resolution) + 1;*/

        // Initialize TSDF and Color Grids size
        /*        tsdf = new float[sizeX, sizeY, sizeZ];
                colorGrid = new Color[sizeX, sizeY, sizeZ];*/

        // Initialize TSDF to Truncation
        // Truncation is the maximum signed-distance from the surface 
        /*        for (int x = 0; x < sizeX; x++)
                    for (int y = 0; y < sizeY; y++)
                        for (int z = 0; z < sizeZ; z++)
                            tsdf[x, y, z] = truncation;*/


        // Fill TSDF (Geometry and Color pass)
        // Check which vertices fall closer to the truncation value (e.g the distance from the surface
        // Assign color to the vertices based on the input point cloud



        for (int i = 0; i < pc_vertices.Length; i++)
        {
            // Extract a 3D point and its Color
            Vector3 p = pc_vertices[i];
            Color c = pc_colors[i];

            // Map world-space point into voxel-grid coordinates
            Vector3 relPos = (p - tsdf_origin) / resolution;
            int vx = Mathf.FloorToInt(relPos.x);
            int vy = Mathf.FloorToInt(relPos.y);
            int vz = Mathf.FloorToInt(relPos.z);

            // Sweep the n x n x n neighbourhood. This depends on your radius
            // For each point we check its 27 neighbours
            int radius = Mathf.CeilToInt(truncation / resolution);

            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        int x = vx + dx;
                        int y = vy + dy;
                        int z = vz + dz;
                        if (x < 0 || y < 0 || z < 0 || x >= sizeX || y >= sizeY || z >= sizeZ) continue;

                        // Compute signed distance and clamp to truncation
                        Vector3 voxelPos = tsdf_origin + new Vector3(x, y, z) * resolution;
                        float dist = Vector3.Distance(voxelPos, p);
                        float sdf = dist - resolution;
                        float tsdfValue = Mathf.Clamp(sdf, -truncation, truncation);

                        // If this point is closer to the surface than any we've seen
                        // update both TSDF and assign color
                        // tsdf[x, y, z] = Mathf.Min(tsdf[x, y, z], tsdfValue);

                        float w_old = weightGrid[x, y, z];
                        float w_new = 1f;

                        if (tsdfValue < tsdf[x, z, y])
                        {
                            float fused_tsdf = (tsdf[x, y, z] * w_old + tsdfValue * w_new) / (w_old + w_new);
                            tsdf[x, y, z] = fused_tsdf;
                            colorGrid[x, y, z] = Color.Lerp(colorGrid[x, y, z], c, 1f / (w_old + w_new));
                            weightGrid[x, y, z] = w_old + w_new;
                        }
                        
                        /*                       if (tsdfValue < tsdf[x, y, z])
                                               {
                                                   tsdf[x, y, z] = tsdfValue;
                                                   colorGrid[x, y, z] = c;
                                               }*/

                    }
        }

        MarchCubes();
        setMesh();
    }

    private void MarchCubes()
    {
        clearMesh();

        for (int x = 0; x < sizeX - 1; x++)
            for (int y = 0; y < sizeY - 1; y++)
                for (int z = 0; z < sizeZ - 1; z++)
                {
                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cube[i] = tsdf[corner.x, corner.y, corner.z];
                    }
                    MarchCube(new Vector3(x, y, z), cube);
                }
    }

    private int GetConfigIndex(float[] cube)
    {
        int idx = 0;
        for (int i = 0; i < 8; i++)
            if (cube[i] > tsdfThreshold) idx |= 1 << i;
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

                float v1 = tsdf[corner1.x, corner1.y, corner1.z];
                float v2 = tsdf[corner2.x, corner2.y, corner2.z];

                // Avoid divide by zero and invalid interpolation
                if (Mathf.Abs(v1 - v2) < 1e-6f) continue;

                // Interpolate to find approximate zero-crossing
                float alpha = v1 / (v1 - v2);
                alpha = Mathf.Clamp01(alpha); // prevent overshoot

                Vector3 interpolated = Vector3.Lerp(p1, p2, alpha);
                Vector3 world = tsdf_origin + (pos + interpolated) * resolution;

                vertices.Add(world);
                triangles.Add(vertices.Count - 1);

                // Use same alpha for interpolating color
                Color c1 = colorGrid[corner1.x, corner1.y, corner1.z];
                Color c2 = colorGrid[corner2.x, corner2.y, corner2.z];
                vertexColors.Add(Color.Lerp(c1, c2, alpha));


            }
    }

    private bool InBounds(Vector3Int idx)
    {
        return idx.x >= 0 && idx.x < sizeX &&
               idx.y >= 0 && idx.y < sizeY &&
               idx.z >= 0 && idx.z < sizeZ;
    }

    private void setMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
        if (!visualizeTSDF || tsdf == null) return;
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    float val = Mathf.Clamp01((tsdf[x, y, z] + truncation) / (2f * truncation));
                    Gizmos.color = new Color(val, val, val);
                    Gizmos.DrawSphere(tsdf_origin + new Vector3(x, y, z) * resolution, 0.05f * resolution);
                }
    }

}
