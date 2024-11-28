using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkedUI : NetworkBehaviour
{
    public static NetworkedUI instance;

    private void Awake() 
    {
        instance = this;
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_ShowWinUI(PlayerRef winningPlayer)
    {
        GetComponent<GameUI>().ShowRoundEnd(winningPlayer == Object.InputAuthority);
    }
}
