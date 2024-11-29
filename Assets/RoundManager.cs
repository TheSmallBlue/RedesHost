using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;

    [SerializeField] Transform[] spawnPoints;

    [Space]
    [SerializeField] UnityEvent onRoundStarted, onRoundEnded;

    private void Awake() 
    {
        Instance = this;
    }

    public void StartRound()
    {
        if(!HasStateAuthority) return;

        foreach (var player in Runner.ActivePlayers)
        {
            PlayerHead head = Runner.GetPlayerObject(player).GetComponent<PlayerHead>();
            head.SpawnAliveAvatar();
        }

        RPC_OnRoundStart();
    }

    [Rpc]
    public void RPC_OnRoundStart()
    {
        onRoundStarted.Invoke();
    }
}
