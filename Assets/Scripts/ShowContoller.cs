using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem; 

public class ShowContoller : MonoBehaviour
{
    // Start is called before the first frame update

    public bool show_controller = true;
    
    // Update is called once per frame
    void Update()
    {
        if (show_controller)
        {
            foreach (var hand in Player.instance.hands)
            {
                hand.ShowController();
                hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithController);
            }
        }

        else
        {
            foreach (var hand in Player.instance.hands)
            {
                hand.HideController();
                hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithoutController);
            }
        }
    }
}
