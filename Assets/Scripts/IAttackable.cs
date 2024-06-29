using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackable
{
    public void OnAttack(AttackType type, Transform source);

    enum AttackType
    {
        Forward,
        Up,
        Down
    }
}
