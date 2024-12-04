using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerHead : NetworkBehaviour
{
    public PlayerAvatar alivePlayerPrefab, deadPlayerPrefab;

    [Networked] public NetworkObject ControlledObject {get; set; }
    [Networked] public Color Color { get; set; }
    [Networked] public string Name { get; set; }

    public PlayerAvatar SpawnAvatar(Vector3 position, Quaternion rotation, bool alive)
    {
        var avatar = Runner.Spawn(alive ? alivePlayerPrefab : deadPlayerPrefab, position, inputAuthority: Object.InputAuthority);
        ControlledObject = avatar.Initialize(this);

        RPC_ConfigureAvatar();

        return avatar;
    }

    [Rpc]
    public void RPC_ConfigureAvatar()
    {
        ControlledObject.GetComponent<PlayerAvatar>().SetupVisuals(Name, Color);
    }
}
