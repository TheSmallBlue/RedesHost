using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI instance;

    [SerializeField] GameObject win, lose;

    private void Awake() 
    {
        instance = this;
    }

    public void ShowRoundEnd(bool won)
    {
        if(won) win.SetActive(true);
        else lose.SetActive(true);
    }
}
