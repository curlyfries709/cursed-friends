using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachine : MonoBehaviour
{
    protected State currentState;

    private void Update()
    {
        currentState?.UpdateState();
    }

    public void SwitchState(State newState)
    {
        if (newState != currentState)
        {
            currentState?.ExitState();
            currentState = newState;
            currentState?.EnterState();
        }
    }
}
