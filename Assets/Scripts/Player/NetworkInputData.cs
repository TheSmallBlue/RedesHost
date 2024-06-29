using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 direction;
    public Vector3 cameraForward, cameraRight;

    public NetworkButtons buttons;
}

public enum PlayerButtons
{
    Jump,
    Crouch,
    Dash
}
