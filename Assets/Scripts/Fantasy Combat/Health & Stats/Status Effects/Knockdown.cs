
public class Knockdown : StatusEffect
{
    public override void OnEffectApplied()
    {
        SpawnVisual();

        //Lose Fired Up! Status
        StatusEffectManager.Instance.LoseFiredUp(myUnit);

        //Display Knockdown Text
        myUnit.CharacterHealth().ShowKnockdownText();

        myUnit.CharacterHealth().SetKnockDown(true);

        myUnit.unitAnimator.SetBool(myUnit.unitAnimator.animIDKnockdown, true);

        //Lose FP
        myUnit.CharacterHealth().LoseFP();

        //Subscribe to turn Start
        myUnit.BeginTurn += OnTurnStart;
    }

    public override void IncreaseTurns(int numOfTurns, int buffChange)
    {
        //Method Overridden because this SE cannot stack turns.
    }

    protected override void EffectEnded()
    {
        myUnit.unitAnimator.SetBool(myUnit.unitAnimator.animIDKnockdown, false);
        myUnit.CharacterHealth().SetKnockDown(false);
        RemoveStatusEffect();
        myUnit.BeginTurn -= OnTurnStart;
    }



    protected override void OnTurnStart()
    {
        //On Turn Start. Get Up.
        myUnit.unitAnimator.SetBool(myUnit.unitAnimator.animIDKnockdown, false);
        Invoke("EffectEnded", 0.5f); //Remove Effect After Delay as EnemyCombatWaitingState has OnTurnStart Method to check if unit disabled....which sometimes gets called after this' OnTurnStart.
    }

    protected override void OnStatusStacked() { }
    protected override void CalculateNewStatValue(bool resetValues) { }
    protected override void OnTurnEnd() { }
}
