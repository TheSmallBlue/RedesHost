using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawnpoint : MonoBehaviour
{
    [SerializeField] Mesh spawnPreviewMesh;
    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(transform.position, new Vector3(1,1.8f,1));
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
