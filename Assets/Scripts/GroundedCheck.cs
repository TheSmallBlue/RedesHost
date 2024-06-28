using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GroundedCheck : MonoBehaviour
{
    [SerializeField] float lengthToFloor = 1.3f;

    [HideInInspector] public bool isGrounded = true;
    bool wasGrounded = true;
    [HideInInspector] public RaycastHit groundedHitInfo;

    public event Action<bool, RaycastHit> onGroundedStateChanged;

    void Update()
    {
        wasGrounded = isGrounded;

        isGrounded = Physics.Raycast(transform.position, -Vector3.up, out RaycastHit info, lengthToFloor, ~(1 << 6));
        groundedHitInfo = info;

        if(wasGrounded != isGrounded)
        {
            onGroundedStateChanged?.Invoke(isGrounded, info);
        }
    }

    public bool GetInfoIfGrounded(ref RaycastHit hit)
    {
        if(!isGrounded) return false;

        hit = groundedHitInfo;
        return true;
    }
}
