using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SpectatorController : PlayerAvatar
{
    [SerializeField] float moveSpeed;

    Rigidbody _rb;

    private void Awake() 
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void SetupVisuals(string desiredName, Color desiredColor)
    {
        GetComponentInChildren<Renderer>().material.color = desiredColor;
    }

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        var cameraRoot = Camera.main.transform.root;
        cameraRoot.GetComponent<CameraController>().SetTarget(transform, false).enabled = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!HasInputAuthority) return;

        var cameraRoot = Camera.main.transform.root;
        cameraRoot.GetComponent<CameraController>().enabled = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData inputData)) return; // If there is no input for us, do nothing

        Vector3 desiredVelocity = Vector3.zero;

        desiredVelocity += GetForward(inputData);
        if(inputData.buttons.IsSet(PlayerButtons.Jump)) desiredVelocity += Vector3.up;
        if(inputData.buttons.IsSet(PlayerButtons.Crouch)) desiredVelocity -= Vector3.up;

        _rb.velocity = desiredVelocity.normalized * moveSpeed;
    }

    Vector3 GetForward(NetworkInputData input)
    {
        if (input.direction.magnitude == 0) return default;

        Vector3 inputForward = input.cameraForward.CollapseAxis(1) * input.direction.y + input.cameraRight.CollapseAxis(1) * input.direction.x;
        inputForward.Normalize();

        transform.forward = inputForward;

        return inputForward;
    }
}
