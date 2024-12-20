using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    bool startScreen = true;

    [SerializeField] RunnerManager runnerPrefab;
    [SerializeField] RoundManager roundPrefab;

    [Space]
    [SerializeField] GameObject modeScreen;
    [SerializeField] GameObject connectingScreen, lobbyScreen;

    [Space]
    [SerializeField] Button startButton;

    [Space]
    [SerializeField] Transform playerListParent;
    [SerializeField] PlayerListItem playerListItemPrefab;

    [Space]
    [SerializeField] WinnerModel winnerModel;
    [SerializeField] GameObject winnerScreen;

    [Space]
    [SerializeField] UnityEvent onAnyKey;

    private void Update() 
    {
        if(Input.anyKeyDown && startScreen)
        {
            onAnyKey.Invoke();
            startScreen = false;
        }
    }

    public void Connect(bool isHost)
    {
        startButton.onClick.RemoveAllListeners();

        // Create and setup NetworkRunner if it doesn't exist
        if(RunnerManager.instance == null)
        {
            var runner = Instantiate(runnerPrefab);
            runner.onShutdown.AddListener(OnShutdown);
        }

        RunnerManager.instance.StartGameUI(isHost);
        StartCoroutine(WaitForConnection());
    }

    IEnumerator WaitForConnection()
    {
        connectingScreen.SetActive(true);

        while (!RunnerManager.instance.Runner.IsRunning)
        {
            yield return null;
        }

        // Create and setup RoundManager if one doesn't exist already
        if (RoundManager.Instance == null)
        {
            Instantiate(roundPrefab);
        }

        // Link RoundManager events
        RoundManager.Instance.onRoundStarted.AddListener(OnRoundStart);
        RoundManager.Instance.onRoundEnded.AddListener(OnRoundEnd);
        startButton.onClick.AddListener(() => RoundManager.Instance.StartRound());

        connectingScreen.SetActive(false);
        lobbyScreen.SetActive(true);

        StartCoroutine(UpdatePlayerList());
    }

    public void OnRoundStart()
    {
        lobbyScreen.SetActive(false);
    }

    public void OnRoundEnd(PlayerHead winner)
    {
        lobbyScreen.SetActive(true);

        winnerScreen.SetActive(true);
        winnerModel.SetColor(winner.Color, winner.name);
    }

    public void Disconnect()
    {
        RunnerManager.instance.DisconnectUI();
    }

    public void OnShutdown()
    {
        lobbyScreen.SetActive(false);
        connectingScreen.SetActive(false);

        modeScreen.SetActive(true);
    }

    IEnumerator UpdatePlayerList()
    {
        while (true)
        {
            var activePlayers = RunnerManager.instance.Runner.ActivePlayers;

            if(activePlayers.Count() != playerListParent.childCount)
            {
                UpdatePlayerList(RunnerManager.instance.Runner.ActivePlayers);
            }            

            yield return null;
        }
    }

    public void UpdatePlayerList(IEnumerable<PlayerRef> players)
    {
        if(players.Count() >= 2 && RunnerManager.instance.Runner.IsServer)
        {
            startButton.gameObject.SetActive(true);
        } else 
        {
            startButton.gameObject.SetActive(false);
        }

        for (int i = 0; i < playerListParent.childCount; i++)
        {
            Destroy(playerListParent.GetChild(i).gameObject);
        }

        foreach (var player in players)
        {
            if(!RunnerManager.instance.Runner.TryGetPlayerObject(player, out NetworkObject networkObject)) continue;

            PlayerHead head = networkObject.GetComponent<PlayerHead>();

            var listItem = Instantiate(playerListItemPrefab, playerListParent);
            listItem.color.color = head.Color;
            listItem.nameText.text = head.Name;

            listItem.selfIndicator.SetActive(RunnerManager.instance.Runner.LocalPlayer == player);
            listItem.hostIndicator.SetActive(player.PlayerId == 1);
        }
    }
}
