using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterStateMachine : StateMachine
{
    public abstract void BeginCombat();
    public abstract void ShowWeapon(bool show);

}
