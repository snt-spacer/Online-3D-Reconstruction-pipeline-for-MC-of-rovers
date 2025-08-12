using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Odom = RosMessageTypes.Nav.OdometryMsg;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class OdometrySubscriber : MonoBehaviour
{

    // Public variables
    public string TopicName;
    public Transform PublishedTransform;
    public Vector3 rover_position;
    public Quaternion rover_rotation;

    // Private variables
    private Vector3 rover_last_position;
    private Quaternion rover_last_rotation;
    private double[] covariance;
    private bool isMessageReceived;
    private Vector3 base_footprint_link_offset = new Vector3(0,0.19F,0);

    // Start is called before the first frame update
    void Start()
    {
        // Initialize ROS2 connection
        ROSConnection.GetOrCreateInstance().Subscribe<Odom>(TopicName, ReceiveMessage);

    }

    // Update is called once per frame
    void Update()
    {
        if (isMessageReceived)
        {
            ProcessMessage();
        } 
    }

    void ReceiveMessage(Odom msg)
    {
        rover_position = extractPosition(msg);
        rover_rotation = extractRotation(msg);
        covariance = extractCovariance(msg);
        isMessageReceived = true;
    }

    void ProcessMessage()
    {
        // Check if rover odometry has been lost or not
        if(covariance[0] < 9000)
        {
            PublishedTransform.position = rover_position + base_footprint_link_offset;
            PublishedTransform.rotation = rover_rotation;
            rover_last_position = rover_position;
            rover_last_rotation = rover_rotation;
        }

        // Retain last known position of the rover in Unity scene
        else
        {
            PublishedTransform.position = rover_last_position;
            PublishedTransform.rotation = rover_last_rotation;
        }
    }

    // Odom msg data extractors
    // The From<FLU>() function call converts the ROS coordinate frame to Unity's coordinate frame
    public Vector3 extractPosition(Odom message)
    {
        return message.pose.pose.position.From<FLU>();
    }

    public Quaternion extractRotation(Odom message)
    {
        return message.pose.pose.orientation.From<FLU>();
    }

    public double[] extractCovariance(Odom message)
    {
        return message.pose.covariance;
    }

}
