using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerData
{
    public NetworkObject ownedObject;
    public int score = 0;
    public PlayerVisuals visuals;

    public struct PlayerVisuals : INetworkStruct
    {
        public NetworkString<_16> name;
        public Color color;
    }
}
