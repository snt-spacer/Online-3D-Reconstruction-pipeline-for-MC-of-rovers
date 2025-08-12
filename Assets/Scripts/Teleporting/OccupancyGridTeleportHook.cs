using UnityEngine;
using Valve.VR.InteractionSystem;

public class OccupancyGridTeleportHook : MonoBehaviour
{
    public GameObject drawing3DManager;
    public Material occupancyGridMaterial;
    public Material teleportMaterial;
    private bool attached = false;

    void Start()
    {
    
    }

    private void Update()
    {
        if (!attached)
        {
            TryAttachTeleportArea();
        }
    }

    void TryAttachTeleportArea()
    {
        if (!drawing3DManager || !occupancyGridMaterial)
        {
            Debug.LogWarning("Missing drawing3DManager or occupancyGridMaterial.");
            return;
        }

        foreach (Transform ts in drawing3DManager.transform)
        {
            if (ts.childCount > 0)
            {

                foreach (MeshRenderer renderer in ts.GetComponentsInChildren<MeshRenderer>())
                {

                    GameObject child = renderer.gameObject;

                    if (child.GetComponent<MeshRenderer>().sharedMaterial == occupancyGridMaterial
                        ||
                        child.GetComponent<MeshRenderer>().sharedMaterial.shader == occupancyGridMaterial.shader)
                    {

                        if (child.GetComponent<MeshCollider>() == null)
                        {
                            var col = child.AddComponent<MeshCollider>();
                            col.sharedMesh = child.GetComponent<MeshFilter>()?.sharedMesh;
                            col.convex = false;
                        }

                        if (child.GetComponent<TeleportArea>() == null)
                        {
                            var ta = child.AddComponent<TeleportArea>();
                            child.layer = LayerMask.NameToLayer("Default");
                            Debug.Log("Teleport area added to occupancy grid.");
                            attached = true;
                        }
                    }
                }
            }

        }
        }
    }

