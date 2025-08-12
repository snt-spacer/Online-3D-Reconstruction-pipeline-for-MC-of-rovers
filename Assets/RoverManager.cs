using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoverManager : MonoBehaviour
{
    public GameObject[] lunarRovers_go;


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Initialized ELYSIUM-XR with " + lunarRovers_go.Length + " amount of Rovers available");
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
