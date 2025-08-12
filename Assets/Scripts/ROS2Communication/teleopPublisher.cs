using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using Twist = RosMessageTypes.Geometry.TwistMsg;
using Valve.VR;

public class teleopPublisher : MonoBehaviour
{

    // Public Variables
    public string topicName;
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean deadTriggerAction;
    public SteamVR_Action_Vector2 touchPadAction;

    public float maxLinearVelocity;
    public float maxAngularVelocity;

    // Private Variables
    private ROSConnection vel_pub;

    // Start is called before the first frame update
    void Start()
    {
        // Create ROS2 publisher instance
        vel_pub = ROSConnection.GetOrCreateInstance();
        vel_pub.RegisterPublisher<Twist>(topicName);
    }

    // Update is called once per frame
    void Update()
    {

        // Check if the deadTrigger is active. This ensures that the robot will not move by accidently touching the D-Pad on the controller
        if (isDeadTriggerActive())
        {
            // Move to a direction
            move();
        }
        else
        {

            // Stop if deadtrigger is not pressed
            stop();
        }
    }

    private bool isDeadTriggerActive()
    {
        // Get the state of the Action currently being issued by the user through the controller
        return deadTriggerAction.GetState(handType);
    }

    private void move()
    {

        // Get position of thumb on D-pad
        Vector2 velocityVector = touchPadAction.GetAxis(handType);

        // Define linear and angular velocities based on handposition. This allows for more "flexible" velocity control
        float linear_vel = velocityVector.y * maxLinearVelocity;
        float angular_vel = -velocityVector.x * maxAngularVelocity;

        // Generate the command
        Twist teleop_command = generateVelMessage(linear_vel, angular_vel);

        vel_pub.Publish(topicName, teleop_command);
    }

    private void stop()
    {
        Twist teleop_command = generateVelMessage(0.0f, 0.0f);
        vel_pub.Publish(topicName, teleop_command);
    }

    private Twist generateVelMessage(float l_vel, float a_vel)
    {
        Twist vel_msg = new Twist(
            new Vector3Msg(l_vel, 0.0f, 0.0f),
            new Vector3Msg(0.0f, 0.0f, a_vel)
            );

        return vel_msg;
    }

}
