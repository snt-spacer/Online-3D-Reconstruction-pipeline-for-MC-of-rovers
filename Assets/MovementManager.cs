using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;


public class MovementManager : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject teleporting;
    public GameObject snapTurn;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void toggleTeleporting(bool status)
    {
        teleporting.SetActive(status);
    }

    public void toggleSnapTurn(bool status)
    {
        snapTurn.SetActive(status);
    }
}
