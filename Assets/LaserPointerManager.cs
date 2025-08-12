using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LaserPointerManager : MonoBehaviour
{

    public SteamVR_Action_Boolean grabGripAction;
    public SteamVR_Input_Sources handType;

    private GameObject laserPointer;
    
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        if(laserPointer == null)
        {
            laserPointer = GameObject.Find("VR Pointer");

            if (laserPointer != null)
            {
                laserPointer.SetActive(false);
            }

            return;
        }

        if (grabGripAction.GetStateUp(handType))
        {
            toggleLaserPointer();
        }
        
    }

    public void toggleLaserPointer()
    {

        if (laserPointer != null && laserPointer.activeSelf)
        {
            laserPointer.SetActive(false);
        }
        else
        {
            laserPointer.SetActive(true);
        }
    }
}
