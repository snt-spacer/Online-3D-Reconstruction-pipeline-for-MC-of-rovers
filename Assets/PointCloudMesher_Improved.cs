using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Smoothing;
using System;

public class PointCloudMesher_Improved : MonoBehaviour
{

    // Public Variables
    public PointCloudSubscriber pc_sub;


    // Private Variables

    private Vector3 planeNormal;
    private Vector3 planeCenter;
    private Vector3 basisU;
    private Vector3 basisV;

    private TriangleNet.Mesh trianglenet_mesh;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private List<MyVertex> pc_vertices_2D;


    // Data used to create the mesh
    private Triangle triangle; // Mesh Triangle 
    private Vector3 v0; // vector 0 of a triangle
    private Vector3 v1; // vector 1 of a triangle
    private Vector3 v2; // vector 2 of a triangle
    private Vector3 normal; // Vector to initialise Mesh Normals
    private Vector2 uv_vector = new Vector2(0.0f, 0.0f); // UV vector for mesh UVs
    private int chunkEnd; // end of a mesh chunk
    // Triangles in each chunk. 
    public int trianglesInChunk = 50000;
    // Mesh to be rendered from PCL
    private Mesh chunkMesh;
    // Prefab which is generated for each chunk of the mesh.
    public Transform chunkPrefab;
    private bool first;


    private int minimumNewPoints = 1000;
    private int existingPoints;
    private List<GameObject> generatedChunks = new List<GameObject>();
    private List<Mesh> generatedMeshes = new List<Mesh>();

    public class MyVertex : TriangleNet.Geometry.Vertex
    {
        public float height;
        public Color color;

        public MyVertex(double x, double y, float height) : base(x, y)
        {
            this.height = height;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        existingPoints = 0;

        pc_vertices_2D = new List<MyVertex>();
        first = true;
    }



    // Update is called once per frame
    void Update()
    {
        if (pc_sub.getPC_vertices() == null)
        {
            //print("Empty rtabmap pointcloud"); 
            return;
        }


        List<Vector3> pointCloudVertices = new List<Vector3>();
        // List<Vector2> pcVertices_2D;

        var start = DateTime.Now;
        var end = DateTime.Now;
        Debug.Log($"Elapsed 1: {(end - start).TotalMilliseconds} ms");

        foreach (var vert in pc_sub.getPC_vertices())
        {
            pointCloudVertices.Add(vert);

        }

        ComputePlaneFromPointCloud(pointCloudVertices);

        pc_vertices_2D = project3Dto2D(pointCloudVertices);

        if (pc_vertices_2D.Count > 0 && existingPoints + minimumNewPoints <= pointCloudVertices.Count)
        {

            foreach (GameObject go in generatedChunks)
            {
                UnityEngine.GameObject.Destroy(go);
            }

            foreach (Mesh mesh in generatedMeshes)
            {
                UnityEngine.Mesh.Destroy(mesh);
            }
            existingPoints = pointCloudVertices.Count;
            GenerateMesh(pc_vertices_2D);
            end = DateTime.Now;
            Debug.Log($"Elapsed 2: {(end - start).TotalMilliseconds} ms");
        }
    }

    void ComputePlaneFromPointCloud(List<Vector3> pointCloud)
    {
        // Compute centroid
        planeCenter = Vector3.zero;
        foreach (var pt in pointCloud)
        {
            planeCenter += pt;
        }
        planeCenter /= pointCloud.Count;

        // For simplicity, assume a dominant upward facing plane.
        // In practice, you may compute this using PCA to get the normal.
        planeNormal = Vector3.up;

        // Project Unity's right vector onto the plane to define the in-plane x-axis.
        Vector3 projectedRight = Vector3.right - Vector3.Dot(Vector3.right, planeNormal) * planeNormal;
        if (projectedRight.sqrMagnitude < 1e-6)
        {
            // Fallback in case the normal is collinear with Vector3.right.
            projectedRight = Vector3.forward - Vector3.Dot(Vector3.forward, planeNormal) * planeNormal;
        }
        basisU = projectedRight.normalized;

        // Compute the second basis vector (in-plane y-axis) using a right-handed coordinate system.
        basisV = Vector3.Cross(planeNormal, basisU).normalized;
    }


    List<MyVertex> project3Dto2D(List<Vector3> pc_vertices)
    {
        List<MyVertex> projected2DPoints = new List<MyVertex>();
        foreach (var vert in pc_vertices)
        {
            Vector3 diff = vert - planeCenter;
            float height = Vector3.Dot(diff, planeNormal);
            float u = Vector3.Dot(diff, basisU);
            float v = Vector3.Dot(diff, basisV);
            bool flipBasisV = false;
            if (flipBasisV)
            {
                v = -v;
                projected2DPoints.Add(new MyVertex(u, v, height));
            }
            else
            {
                projected2DPoints.Add(new MyVertex(u, v, height));
            }

        }

        return projected2DPoints;

    }

    Vector3 project2Dto3D(Vertex vert, float height)
    {
        /*        Vector3 pt3D = planeCenter + (basisU * (float)vert.x) + (basisV * (float)vert.y);
        */
        Vector3 pt3D = planeCenter + (basisU * (float)vert.x) + (basisV * (float)vert.y) + (planeNormal * height);
        return pt3D;
    }

    void GenerateMesh(List<MyVertex> vertices_2D)
    {
        Polygon polygon = new Polygon();
        foreach (var vert in vertices_2D)
        {
            polygon.Add(vert);
        }

        TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions()
        {
            ConformingDelaunay = true,
            Convex = false,
            SegmentSplitting = 2
        };

        // Quality options to triangulate the mesh
        TriangleNet.Meshing.QualityOptions quality = new TriangleNet.Meshing.QualityOptions()
        {
            //MinimumAngle = 20,
            //MaximumArea = (Math.Sqrt(3) / 4 * 0.2 * 0.2) * 1.5
            //MaximumAngle = 100,
            //SteinerPoints = 5,
            //MaximumArea = (Math.Sqrt(3) / 4 * 0.2 * 0.2 * 1.45) // to be removed
        };

        TriangleNet.Smoothing.SimpleSmoother smoother = new TriangleNet.Smoothing.SimpleSmoother();


        trianglenet_mesh = (TriangleNet.Mesh)polygon.Triangulate(options, quality);
        smoother.Smooth(trianglenet_mesh, 45, 1.5);

        // print("Size of polygon: " + trianglenet_mesh.vertices.Count);
        // print("Size of vertices2D: " + pc_vertices_2D.Count);
        // Enumerate over all the triangles of the mesh
        IEnumerator<Triangle> triangleEnumerator = trianglenet_mesh.Triangles.GetEnumerator(); ; // Enumerate over all the triangles of the mesh 

        // Split the mesh into chunks (at most 65000 vertices per chunk)
        for (int chunkStart = 0; chunkStart < trianglenet_mesh.Triangles.Count; chunkStart += trianglesInChunk)
        {

            // Define vertices, normals, uvs, triangles for each chunk's mesh
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();

            // Define chunkEnd (?)
            chunkEnd = chunkStart + trianglesInChunk;

            for (int i = chunkStart; i < chunkEnd; i++)
            {
                if (!triangleEnumerator.MoveNext())
                {
                    break;
                }

                triangle = triangleEnumerator.Current;

                // For the triangles to be right-side up, they need
                // to be wound in the opposite direction
                v0 = project2Dto3D(triangle.vertices[2], vertices_2D[triangle.vertices[2].id].height);
                v1 = project2Dto3D(triangle.vertices[1], vertices_2D[triangle.vertices[1].id].height);
                v2 = project2Dto3D(triangle.vertices[0], vertices_2D[triangle.vertices[0].id].height);

                float edge1 = Vector3.Distance(v0, v1);
                float edge2 = Vector3.Distance(v1, v2);
                float edge3 = Vector3.Distance(v2, v0);
                float maxLength = 0.6f;

                if (edge1 > maxLength || edge2 > maxLength || edge3 > maxLength)
                {
                    continue;
                }


                // Add Normals

                normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                if (Vector3.Dot(normal, Vector3.up) < 0f)
                {
                    // Flip triangle (reverse vertex order)
                    Vector3 temp = v1;
                    v1 = v2;
                    v2 = temp;
                    normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                }

                /*                float dotUp = Mathf.Abs(Vector3.Dot(normal, Vector3.up)); // dot product with Vector3.up
                                if (dotUp < 0.1f) // tweak this threshold (e.g., 0.7 = ~45 degrees)
                                    continue; // Skip triangle*/

                // Add triangles
                triangles.Add(vertices.Count);
                triangles.Add(vertices.Count + 1);
                triangles.Add(vertices.Count + 2);

                // Add vertices
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);


                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                // Add UVs
                uvs.Add(uv_vector);
                uvs.Add(uv_vector);
                uvs.Add(uv_vector);

                // Add the Vertex Colors
                colors.Add(pc_sub.getPC_colors()[triangle.vertices[2].id]);
                colors.Add(pc_sub.getPC_colors()[triangle.vertices[1].id]);
                colors.Add(pc_sub.getPC_colors()[triangle.vertices[0].id]);

            }

            // Mesh gameObject the Unity can render 
            // Converted from TriangleNet.mesh
            // ToArray() is used to convert them such that Unity can use them

            Mesh chunkMesh = new Mesh();
            chunkMesh.vertices = vertices.ToArray();
            chunkMesh.uv = uvs.ToArray();
            chunkMesh.triangles = triangles.ToArray();
            chunkMesh.normals = normals.ToArray();
            chunkMesh.colors = colors.ToArray();

            chunkMesh.RecalculateNormals();

            /*            smoother.Smooth(chunkMesh, 25, 0.5);
            */

            // Instantiate the GameObject which will display this chunk mesh 
            Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation);

            // Give the instantiated mesh (chunk) a filter and a collider object
            chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
            chunk.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
            chunk.transform.parent = transform;

            generatedChunks.Add(chunk.gameObject);

            // Store each chunk instance by ID to keep track of all generated GameObjects
            // generatedChunks.Add(chunk.GetInstanceID(),chunk.gameObject);
            // generatedChunks.Add(chunk.gameObject);
            // generatedMeshes.Add(chunkMesh);

/*            // Free up the memory
            vertices.Clear();
            uvs.Clear();
            triangles.Clear();
            normals.Clear();
            colors.Clear();*/
        }

    }

/*    public void OnDrawGizmos()
    {
        if (trianglenet_mesh == null)
        {
            // We're probably in the editor
            return;
        }

        Gizmos.color = Color.red;
        foreach (Edge edge in trianglenet_mesh.Edges)
        {
            Vertex v0 = trianglenet_mesh.vertices[edge.P0];
            Vertex v1 = trianglenet_mesh.vertices[edge.P1];
            Vector3 p0 = planeCenter + basisU * (float)v0.x + basisV * (float)v0.y;
            Vector3 p1 = planeCenter + basisU * (float)v1.x + basisV * (float)v1.y;
            Gizmos.DrawLine(p0, p1);
        }
    }*/
}


// Custom vertex that stores an extra "height" value.



