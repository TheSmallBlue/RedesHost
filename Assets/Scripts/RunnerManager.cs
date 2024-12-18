using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using TMPro;
using Fusion.Addons.Physics;
using System.Collections;
using UnityEngine.Events;
using System.Linq;

public class RunnerManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static RunnerManager instance;

    [SerializeField] NetworkPrefabRef _playerHead;

    [Space]
    public UnityEvent<PlayerRef> onPlayerJoined;
    public UnityEvent onPlayerLeft;
    public UnityEvent onShutdown;

    [HideInInspector] public NetworkRunner Runner;

    private void Awake() 
    {
        instance = this;
    }

    #region UI Methods
    public void StartGameUI(bool isHost)
    {
        StartGame(isHost ? GameMode.Host : GameMode.Client);
    }

    public void DisconnectUI()
    {
        StopGame();
    }

    #endregion

    async void StartGame(GameMode mode)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        Runner = gameObject.AddComponent<NetworkRunner>();
        if(GetComponent<RunnerSimulatePhysics3D>() == null) gameObject.AddComponent<RunnerSimulatePhysics3D>();
        Runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = TryGetComponent(out NetworkSceneManagerDefault sceneManager) ? sceneManager : gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    void StopGame()
    {
        Runner.Shutdown(true);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
    {
        if(runner.IsServer)
        {
            var headObject = runner.Spawn(_playerHead, inputAuthority: player);
            var head = headObject.GetComponent<PlayerHead>();
            head.Color = UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f);
            head.Name = NameGenerator.GetName();

            runner.SetPlayerObject(player, headObject);
        }

        onPlayerJoined.Invoke(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {   
        if(!runner.IsServer) return;

        var playerObject = runner.GetPlayerObject(player);
        var controlledObject = playerObject.GetComponent<PlayerHead>().ControlledObject;

        runner.Despawn(controlledObject);
        runner.Despawn(playerObject);

        onPlayerLeft.Invoke();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        onShutdown.Invoke();
    }

    #region Input

    #endregion

    public void OnInput(NetworkRunner runner, NetworkInput input) 
    { 
        var data = new NetworkInputData();

        data.direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        data.cameraForward = Camera.main.transform.forward;
        data.cameraRight = Camera.main.transform.right;

        data.buttons.Set(PlayerButtons.Jump, Input.GetKey(KeyCode.Space));
        data.buttons.Set(PlayerButtons.Crouch, Input.GetKey(KeyCode.LeftControl));
        data.buttons.Set(PlayerButtons.Dash, Input.GetKey(KeyCode.LeftShift));

        data.buttons.Set(PlayerButtons.Attack, Input.GetMouseButton(0));

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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

    /*
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
    */
}