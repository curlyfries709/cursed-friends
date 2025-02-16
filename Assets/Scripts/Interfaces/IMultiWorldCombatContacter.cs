
/*
 * Implemented by classes that also exist outside the fantasy world and need to contact the Combat Manager. 
 */
public interface IMultiWorldCombatContacter 
{
    public void SubscribeToCombatManagerEvents(bool subscribe);

    public void ListenForCombatManagerSet();
}
