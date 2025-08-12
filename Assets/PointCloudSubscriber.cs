using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using PointCloud2Msg = RosMessageTypes.Sensor.PointCloud2Msg;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;


public class PointCloudSubscriber : MonoBehaviour
{
    // Public Variables
    public string topicName;
    public ParticleSystem pointCloudParticleSystem;
    public Material particleMaterial;
    public bool showPointCloud = false;

    // Private Variables

    private bool isMessageReceived = false;
    private PointCloud2Msg pointcloud;
    private Renderer particleRenderer;
    
    private byte[] byteArray;
    private int pc_size;
    private Vector3[] pc_vertices;
    private Color[] pc_colors;
    private int point_step;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<PointCloud2Msg>(topicName, ReceiveMessage);
        
        // Reset the particle system and clear default values
        pointCloudParticleSystem.Stop();
        pointCloudParticleSystem.Clear();

        // Attach a new material to the system
        particleRenderer = pointCloudParticleSystem.GetComponent<Renderer>();
        particleRenderer.material = particleMaterial;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMessageReceived)
        {
            ProcessMessage(pointcloud);
            isMessageReceived = false;

            if (showPointCloud)
            {
                renderPointCloud(getPC_vertices(), getPC_colors());
            }
            else
            {
                pointCloudParticleSystem.Stop();
                pointCloudParticleSystem.Clear();
            }
        }
    }

    void ReceiveMessage(PointCloud2Msg msg)
    {
        if (msg != null)
        {
            pointcloud = msg;
            isMessageReceived = true;
        }
        
    }

    // Converts the PointCloud data into Unity accessible format
    // Stores the vertices and the colors into respective lists
    void ProcessMessage(PointCloud2Msg msg)
    {
        pc_size = msg.data.GetLength(0);

        // Initialize byte array to store Point Cloud data
        // PointCloud2 messages have their data stored into byte format
        byteArray = new byte[pc_size];
        byteArray = msg.data;

        // Define the offset of byte values. Each data inside the PointCloud is represented by 4 bytes
        point_step = (int)msg.point_step;
        pc_size = pc_size / point_step;

        // Initialize the arrays of PointCloud vertices and colors
        pc_vertices = new Vector3[pc_size];
        pc_colors = new Color[pc_size];

        // Initialize x,y,z and r,g,b values in byte format
        int x_byte;
        int y_byte;
        int z_byte;

        int rgb_byte;
        int rgb_max = 255;

        // Initialize x,y,z and r,g,b real values
        float x_real;
        float y_real;
        float z_real;

        float r_real;
        float g_real;
        float b_real;

        // Convert byte values into real values
        for (int i = 0; i < pc_size; i++)
        {
            x_byte = i * point_step + 0;
            y_byte = i * point_step + 4;
            z_byte = i * point_step + 8;

            x_real = System.BitConverter.ToSingle(byteArray, x_byte);
            y_real = System.BitConverter.ToSingle(byteArray, y_byte);
            z_real = System.BitConverter.ToSingle(byteArray, z_byte);

            rgb_byte = i * point_step + 16;

            // Extract the real values of r,g,b. They are stored as int
            r_real = byteArray[rgb_byte + 2];
            g_real = byteArray[rgb_byte + 1];
            b_real = byteArray[rgb_byte + 0];

            // Normalize the colors to be in 
            r_real = r_real / rgb_max;
            g_real = g_real / rgb_max;
            b_real = b_real / rgb_max;

            pc_vertices[i] = new Vector3(-y_real, z_real, x_real);
            pc_colors[i] = new Color(r_real, g_real, b_real);

        }
    }

    void renderPointCloud(Vector3[] vertices, Color[] colors)
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            ParticleSystem.Particle particle = new ParticleSystem.Particle();
            particle.position = vertices[i];
            particle.startColor = colors[i];
            particle.startSize = 0.1f; // Adjust size as needed for your visualization
            particle.remainingLifetime = float.MaxValue; // Particles remain until replaced

            particles[i] = particle;
        }

        pointCloudParticleSystem.SetParticles(particles, particles.Length);

    }

    public Vector3[] getPC_vertices()
    {
        return pc_vertices;
    }

    public Color[] getPC_colors()
    {
        return pc_colors;
    }

}
