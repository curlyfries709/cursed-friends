using System.Collections;
using System.Collections.Generic;

public abstract class State
{

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}
