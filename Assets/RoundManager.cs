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

    //---

    List<PlayerAvatar> alivePlayers = new();

    //---

    [Networked] public RoundState State { get; set; }

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
            var avatar = head.SpawnAvatar(Vector3.zero, Quaternion.identity, true);

            alivePlayers.Add(avatar);
        }

        RPC_OnRoundStart();
    }

    [Rpc]
    public void RPC_OnRoundStart()
    {
        onRoundStarted.Invoke();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_OnPlayerDead(PlayerAvatar player)
    {
        var head = player.Head;
        var deathPosition = player.transform.position;
        var deathRotation = player.transform.rotation;

        alivePlayers.Remove(player);
        Runner.Despawn(player.Object);

        var newAvatar = head.SpawnAvatar(deathPosition, deathRotation, false);

        foreach (var alivePlayer in alivePlayers)
        {
            alivePlayer.RPC_HideObjectFromMe(newAvatar.Object);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_OnPlayerDisconnect(PlayerAvatar player)
    {
        var head = player.Head;
        var deathPosition = player.transform.position;
        var deathRotation = player.transform.rotation;

        alivePlayers.Remove(player);
        Runner.Despawn(player.Object);

        var newAvatar = head.SpawnAvatar(deathPosition, deathRotation, false);

        foreach (var alivePlayer in alivePlayers)
        {
            alivePlayer.RPC_HideObjectFromMe(newAvatar.Object);
        }
    }

    public enum RoundState
    {
        NotStarted,
        Started,
        Ended
    }
}
