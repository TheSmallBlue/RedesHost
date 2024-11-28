using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using TMPro;
using Fusion.Addons.Physics;
using System.Collections;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public static BasicSpawner instance;

    [SerializeField] NetworkPrefabRef _playerHead, _playerPrefab;
    [SerializeField] public Dictionary<PlayerRef, PlayerData> playerObjects = new Dictionary<PlayerRef, PlayerData>();

    [SerializeField] Transform[] spawnPoints;
    [SerializeField] int amountOfPointsToWin;

    [Networked] public RoundState roundState {get; set;}

    public enum RoundState
    {
        NotStarted,
        Started,
        Ended
    }

    NetworkRunner _runner;

    private void Awake() 
    {
        instance = this;
    }

    async void StartGame(GameMode mode)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        gameObject.AddComponent<RunnerSimulatePhysics3D>();
        _runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
    { 
        if(!runner.IsServer) return;

        var head = _runner.Spawn(_playerHead).GetComponent<PlayerHead>();
        head.Color = UnityEngine.Random.ColorHSV();
        head.Name = NameGenerator.GetName();

        /*

        var newVisuals = new PlayerData.PlayerVisuals()
        {
            name = NameGenerator.GetName(),
            color = UnityEngine.Random.ColorHSV()
        };

        var newPlayerData = new PlayerData()
        {
            visuals = newVisuals
        };

        playerObjects.Add(player, newPlayerData);

        if (roundState == RoundState.Started) playerObjects[player].ownedObject = SpawnPlayer(player);
        else if (playerObjects.Count >= 2) StartRound();
        */
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    { 
        if(playerObjects.TryGetValue(player, out PlayerData playerToDespawn))
        {
            runner.Despawn(playerToDespawn.ownedObject);
            playerObjects.Remove(player);
        }
    }

    void StartRound()
    {
        roundState = RoundState.Started;

        foreach (var player in playerObjects)
        {
            playerObjects[player.Key].ownedObject = SpawnPlayer(player.Key);
        }
    }

    NetworkObject SpawnPlayer(PlayerRef playerToSpawn)
    {
        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length - 1)];

        var playerNetworkObject = _runner.Spawn(_playerPrefab, spawnPoint.position, spawnPoint.rotation, playerToSpawn);

        return playerNetworkObject;
    }

    Queue<Tuple<PlayerRef, float>> playersToRespawn = new Queue<Tuple<PlayerRef, float>>();
    public void RespawnIn(PlayerRef player, float seconds)
    {
        if(!_runner.IsServer) return;

        playersToRespawn.Enqueue(Tuple.Create(player, Time.time + seconds));

    }

    public void AddPoints(PlayerRef sourcePlayer, int amount)
    {
        playerObjects[sourcePlayer].score += amount;

        if (playerObjects[sourcePlayer].score >= amountOfPointsToWin)
        {
            roundState = RoundState.Ended;
            
            playerObjects[sourcePlayer].ownedObject.GetComponent<PlayerController>().RPC_ShowWinUI(sourcePlayer);
        }
    }

    #region Input

    bool _jump, _dash, _crouch, _attack;

    private void Update() 
    {
        // Input
        _jump = _jump | Input.GetKey(KeyCode.Space);
        _dash = _dash | Input.GetKey(KeyCode.LeftShift);
        _crouch = _crouch | Input.GetKey(KeyCode.LeftControl);
        _attack = _attack | Input.GetMouseButton(0);

        // Respawns
        if(playersToRespawn.TryPeek(out var playerRespawn) && Time.time > playerRespawn.Item2)
        {
            PlayerRef player = playersToRespawn.Dequeue().Item1;

            if(!playerObjects.ContainsKey(player)) return;

            NetworkObject newPlayer = SpawnPlayer(player);
            playerObjects[player].ownedObject = newPlayer;
        }
    }

    #endregion

    public void OnInput(NetworkRunner runner, NetworkInput input) 
    { 
        var data = new NetworkInputData();

        data.direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        data.cameraForward = Camera.main.transform.forward;
        data.cameraRight = Camera.main.transform.right;

        data.buttons.Set(PlayerButtons.Jump, _jump);
        data.buttons.Set(PlayerButtons.Crouch, _crouch);
        data.buttons.Set(PlayerButtons.Dash, _dash);

        data.buttons.Set(PlayerButtons.Attack, _attack);

        _jump = false;
        _crouch = false;
        _dash = false;
        _attack = false;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
    }
}