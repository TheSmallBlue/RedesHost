using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public abstract class PlayerAvatar : NetworkBehaviour
{
    [Networked] public PlayerHead Head { get; set; }

    public NetworkObject Initialize(PlayerHead source)
    {
        Head = source;
        return Object;
    }

    public abstract void SetupVisuals(string desiredName, Color desiredColor);

    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    public void RPC_HideObjectFromMe(NetworkObject netObject)
    {
        foreach (var renderer in netObject.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
    }
}
