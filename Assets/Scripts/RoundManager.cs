using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;

    [SerializeField] Transform[] spawnPoints;

    [Space]
    public UnityEvent onRoundStarted;
    public UnityEvent<PlayerHead> onRoundEnded;
    

    //---

    List<PlayerAvatar> alivePlayers = new();

    //---

    private void Awake() 
    {
        Instance = this;
    }

    public void StartRound()
    {
        if (!HasStateAuthority) return;

        // Link events
        RunnerManager.instance.onPlayerJoined.AddListener(RPC_OnPlayerConnected);
        RunnerManager.instance.onPlayerLeft.AddListener(RPC_OnPlayerDisconnected);

        // Spawn players
        alivePlayers.Clear();
        foreach (var player in Runner.ActivePlayers)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            PlayerHead head = Runner.GetPlayerObject(player).GetComponent<PlayerHead>();
            var avatar = head.SpawnAvatar(spawnPoint.position, spawnPoint.rotation, true);

            alivePlayers.Add(avatar);
        }

        // Start round
        StartCoroutine(Round());
    }

    IEnumerator Round()
    {
        // -- Start round --
        RPC_OnRoundStart();

        // Round
        while (alivePlayers.Count > 1)
        {
            yield return null;
        }

        var winningPlayer = alivePlayers[0];

        // -- End round --

        RPC_OnRoundEnd(winningPlayer.Head);

        // Wait a single network tick
        // (To make sure all remaining RPCs and methos are run)
        yield return new WaitForSeconds(Runner.DeltaTime);

        // Unlink runner methods
        RunnerManager.instance.onPlayerJoined.RemoveListener(RPC_OnPlayerConnected);
        RunnerManager.instance.onPlayerLeft.RemoveListener(RPC_OnPlayerDisconnected);

        // Despawn all players
        foreach (var playerHead in Runner.ActivePlayers.Select(x => Runner.GetPlayerObject(x).GetComponent<PlayerHead>()))
        {
            Runner.Despawn(playerHead.ControlledObject);
        }
    }

    [Rpc]
    public void RPC_OnRoundStart()
    {
        onRoundStarted.Invoke();
    }

    [Rpc]
    public void RPC_OnRoundEnd(PlayerHead winner)
    {
        onRoundEnded.Invoke(winner);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_OnPlayerDead(PlayerAvatar player)
    {
        var head = player.Head;
        var deathPosition = player.transform.position;
        var deathRotation = player.transform.rotation;

        alivePlayers.Remove(player);
        Runner.Despawn(player.Object);

        // If the round is over there's no need to spawn a spectator, since there's nothing to spectate
        if(alivePlayers.Count <= 1) return;

        // Spawn spectator object
        var newAvatar = head.SpawnAvatar(deathPosition, deathRotation, false);

        // Make spectator object invisible to other players
        foreach (var alivePlayer in alivePlayers)
        {
            alivePlayer.RPC_HideAvatarFromMe(newAvatar);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_OnPlayerConnected(PlayerRef playerRef)
    {
        // The round started already! This player cannot join...
        // So we make them a spectator instead!
        var head = Runner.GetPlayerObject(playerRef).GetComponent<PlayerHead>();
        var avatar = head.SpawnAvatar(Vector3.zero, Quaternion.identity, false);

        // Hide spectator from other players
        foreach (var alivePlayer in alivePlayers)
        {
            alivePlayer.RPC_HideAvatarFromMe(avatar);
        }

        Invoke("RefreshAvatarVisuals", 0.5f);
        Invoke("RPC_OnRoundStart", 0.5f);
    }

    void RefreshAvatarVisuals()
    {
        foreach (var playerHead in Runner.ActivePlayers.Select(x => Runner.GetPlayerObject(x).GetComponent<PlayerHead>()))
        {
            playerHead.RPC_ConfigureAvatar();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_OnPlayerDisconnected()
    {
        Invoke("UpdateAlivePlayers", 0.1f);
    }

    void UpdateAlivePlayers()
    {
        var playersNotNull = alivePlayers.Where(x => x != null);
        alivePlayers = playersNotNull.ToList();
    }
}
