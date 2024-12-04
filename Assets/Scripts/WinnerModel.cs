using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WinnerModel : MonoBehaviour
{
    [SerializeField] Renderer mesh;
    [SerializeField] TMP_Text letterCap;

    public void SetColor(Color color, string name)
    {
        mesh.materials[1].color = color;

        letterCap.text = name[0].ToString().ToUpper();
        letterCap.color = color;
    }
}
