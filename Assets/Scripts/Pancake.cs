using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pancake : MonoBehaviour
{
    [SerializeField] float despawnTime;

    public void Initialize(Color color)
    {
        GetComponentInChildren<Renderer>().materials[1].color = color;

        Invoke("Despawn", despawnTime);
    }

    void Despawn()
    {
        Destroy(gameObject);
    }
}
