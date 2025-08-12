using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceVisibleTeleportObject : MonoBehaviour
{
    public Material objectMaterial;

    void Start()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer && objectMaterial)
        {
            renderer.enabled = true;
            renderer.material = objectMaterial;
        }

        gameObject.SetActive(true);
    }
}