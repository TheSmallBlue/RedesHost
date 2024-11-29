using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerHead : NetworkBehaviour
{
    [Networked] public Color Color { get; set; }
    [Networked] public string Name { get; set; }

    public override void Spawned()
    {
        
    }
}
