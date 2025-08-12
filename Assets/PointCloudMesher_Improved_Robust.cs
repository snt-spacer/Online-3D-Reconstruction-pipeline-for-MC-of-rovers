// TSDF + Marching Cubes using real-time point cloud input from PointCloudSubscriber

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TSDFMesher : MonoBehaviour
{
	[SerializeField] private float resolution = 0.1f;
	[SerializeField] private float tsdfThreshold = 0.01f;
	[SerializeField] private float truncation = 0.2f;
	[SerializeField] private bool visualizeTSDF = false;

	private float[,,] tsdf;
	private Vector3 minBounds;
	private int sizeX, sizeY, sizeZ;
	private MeshFilter meshFilter;
	private List<Vector3> vertices = new List<Vector3>();
	private List<Color> vertexColors = new List<Color>();
	private List<int> triangles = new List<int>();
	private Color[,,] colorGrid;

	private PointCloudSubscriber pointCloudSubscriber;

	void Start()
	{
		meshFilter = GetComponent<MeshFilter>();
		pointCloudSubscriber = FindObjectOfType<PointCloudSubscriber>();
		InvokeRepeating("UpdateMeshFromPointCloud", 0f, 3f);
	}

	void UpdateMeshFromPointCloud()
	{
		if (pointCloudSubscriber == null || pointCloudSubscriber.getPC_vertices() == null)
			return;

		Vector3[] points = pointCloudSubscriber.getPC_vertices();

		if (points.Length == 0) return;

		// Compute bounds
		Vector3 min = points[0], max = points[0];
		foreach (var p in points)
		{
			min = Vector3.Min(min, p);
			max = Vector3.Max(max, p);
		}
		minBounds = min;
		Vector3 size = max - min;
		sizeX = Mathf.CeilToInt(size.x / resolution) + 1;
		sizeY = Mathf.CeilToInt(size.y / resolution) + 1;
		sizeZ = Mathf.CeilToInt(size.z / resolution) + 1;
		tsdf = new float[sizeX, sizeY, sizeZ];
		colorGrid = new Color[sizeX, sizeY, sizeZ];

		// Initialize TSDF to truncation
		for (int x = 0; x < sizeX; x++)
			for (int y = 0; y < sizeY; y++)
				for (int z = 0; z < sizeZ; z++)
					tsdf[x, y, z] = truncation;

		// Fill TSDF
		foreach (var p in points)
		{
			Vector3 relPos = (p - minBounds) / resolution;
			int vx = Mathf.FloorToInt(relPos.x);
			int vy = Mathf.FloorToInt(relPos.y);
			int vz = Mathf.FloorToInt(relPos.z);

			for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
					for (int dz = -1; dz <= 1; dz++)
					{
						int x = vx + dx;
						int y = vy + dy;
						int z = vz + dz;
						if (x < 0 || y < 0 || z < 0 || x >= sizeX || y >= sizeY || z >= sizeZ) continue;
						Vector3 voxelPos = minBounds + new Vector3(x, y, z) * resolution;
						float dist = Vector3.Distance(voxelPos, p);
						tsdf[x, y, z] = Mathf.Min(tsdf[x, y, z], Mathf.Clamp(dist - resolution, -truncation, truncation));
					}
		}

		Color[] colors = pointCloudSubscriber.getPC_colors();
		for (int i = 0; i < points.Length; i++)
		{
			Vector3 p = points[i];
			Color c = colors[i];
			Vector3 relPos = (p - minBounds) / resolution;
			int vx = Mathf.FloorToInt(relPos.x);
			int vy = Mathf.FloorToInt(relPos.y);
			int vz = Mathf.FloorToInt(relPos.z);

			for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
					for (int dz = -1; dz <= 1; dz++)
					{
						int x = vx + dx;
						int y = vy + dy;
						int z = vz + dz;
						if (x < 0 || y < 0 || z < 0 || x >= sizeX || y >= sizeY || z >= sizeZ) continue;
						Vector3 voxelPos = minBounds + new Vector3(x, y, z) * resolution;
						float dist = Vector3.Distance(voxelPos, p);
						float tsdfValue = Mathf.Clamp(dist - resolution, -truncation, truncation);

						if (tsdfValue < tsdf[x, y, z])
						{
							tsdf[x, y, z] = tsdfValue;
							colorGrid[x, y, z] = c; // save color of closest surface
						}
					}
		}

		MarchCubes();
		SetMesh();
	}

	private void SetMesh()
	{
		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.colors = vertexColors.ToArray();
		mesh.RecalculateNormals();
		meshFilter.mesh = mesh;
	}

	private void MarchCubes()
	{
		vertices.Clear();
		triangles.Clear();
		vertexColors.Clear();

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
				Vector3 world = minBounds + (pos + interpolated) * resolution;

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

	private void OnDrawGizmosSelected()
	{
		if (!visualizeTSDF || tsdf == null) return;
		for (int x = 0; x < sizeX; x++)
			for (int y = 0; y < sizeY; y++)
				for (int z = 0; z < sizeZ; z++)
				{
					float val = Mathf.Clamp01((tsdf[x, y, z] + truncation) / (2f * truncation));
					Gizmos.color = new Color(val, val, val);
					Gizmos.DrawSphere(minBounds + new Vector3(x, y, z) * resolution, 0.05f * resolution);
				}
	}
}