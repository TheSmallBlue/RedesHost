using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NameGenerator
{
    static string[] postfixes = new string[] {"ario", "uigi"};
    static string letters = "abcdefghijklmnñopqrstuvwxyz";

    public static string GetName()
    {
        return letters[Random.Range(0, letters.Length - 1)] + postfixes[Random.Range(0, postfixes.Length - 1)];
    }
}
