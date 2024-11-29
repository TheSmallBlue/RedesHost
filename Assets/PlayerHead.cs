using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerHead : NetworkBehaviour
{
    public PlayerController alivePlayerPrefab;

    [Networked] public NetworkObject ControlledObject {get; set; }
    [Networked] public Color Color { get; set; }
    [Networked] public string Name { get; set; }

    public void SpawnAliveAvatar()
    {
        ControlledObject = Runner.Spawn(alivePlayerPrefab, inputAuthority: Object.InputAuthority).Object;

        RPC_ConfigureAvatar();
    }

    [Rpc]
    public void RPC_ConfigureAvatar()
    {
        ControlledObject.GetComponent<PlayerController>().SetupVisuals(Name, Color);
    }
}
