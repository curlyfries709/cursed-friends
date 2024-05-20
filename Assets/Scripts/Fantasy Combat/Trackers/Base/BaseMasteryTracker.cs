using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseMasteryTracker : MonoBehaviour
{
    public Mastery myMastery;

    protected Dictionary<PlayerGridUnit, int> playerCombatProgression = new Dictionary<PlayerGridUnit, int>();
    protected Dictionary<PlayerGridUnit, MasteryProgression.ProgressionType> allPlayersProgressionType = new Dictionary<PlayerGridUnit, MasteryProgression.ProgressionType>();

    protected abstract void ListenToEvents(bool listen);

    public void OnCombatBegin(Dictionary<PlayerGridUnit, MasteryProgression> allPlayersCurrentMasteryProgression)
    {
        SetDictionaries(allPlayersCurrentMasteryProgression);
        ListenToEvents(true);
    }

    public void OnCombatEnd()
    {
        ClearData();
        ListenToEvents(false);
    }

    public Dictionary<PlayerGridUnit, int> GetAllPlayersCombatProgression()
    {
        return playerCombatProgression;
    }

    protected void SetDictionaries(Dictionary<PlayerGridUnit, MasteryProgression> allPlayersCurrentMasteryProgression)
    {
        foreach (var item in allPlayersCurrentMasteryProgression)
        {
            playerCombatProgression[item.Key] = 0;
            allPlayersProgressionType[item.Key] = allPlayersCurrentMasteryProgression[item.Key].progressionType;
        }
    }

    protected void ClearData()
    {
        playerCombatProgression.Clear();
        allPlayersProgressionType.Clear();
    }
}
