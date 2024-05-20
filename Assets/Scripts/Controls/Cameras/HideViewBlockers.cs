using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideViewBlockers : MonoBehaviour
{
    [SerializeField] bool activateOnEnable = false;
    [Space(5)]
    [SerializeField] bool onlyShowActiveUnit = false;
    [SerializeField] bool onlyShowTargetsWithoutActiveUnit = false;

    List<GridUnitAnimator> unitAnimators = new List<GridUnitAnimator>();

    private void OnEnable()
    {
        if (activateOnEnable)
        {
            HideViewIntruders();
        }
    }

    //CALLED VIA VCAM ON LIVE FUNCTION.
    public void HideViewIntruders()
    {
        unitAnimators.Clear();

        foreach (CharacterGridUnit unit in FantasyCombatManager.Instance.GetAllCharacterCombatUnits(false))
        {
            if (onlyShowActiveUnit || (onlyShowTargetsWithoutActiveUnit && FantasyCombatManager.Instance.GetUnitsToShow().Contains(unit)))
            {
                if((onlyShowActiveUnit && unit != FantasyCombatManager.Instance.GetActiveUnit()) || (onlyShowTargetsWithoutActiveUnit && unit == FantasyCombatManager.Instance.GetActiveUnit()))
                {
                    unitAnimators.Add(unit.unitAnimator);
                    unit.unitAnimator.ShowModel(false);
                }
                else
                {
                    unit.unitAnimator.ShowModel(true);
                }
            }
            else if (!FantasyCombatManager.Instance.GetUnitsToShow().Contains(unit))
            {
                unitAnimators.Add(unit.unitAnimator);
                unit.unitAnimator.ShowModel(false);
            }
            else
            {
                unit.unitAnimator.ShowModel(true);
            }
        }
    }

    public void ShowAll()
    {
        foreach (GridUnitAnimator unitAnimator in unitAnimators)
        {
            unitAnimator.ShowModel(true);
        }
    }

 
    private void OnDisable()
    {
        ShowAll();
    }
}
