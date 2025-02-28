using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideViewBlockers : MonoBehaviour
{
    [SerializeField] bool activateOnEnable = false;
    [Space(5)]
    [SerializeField] bool onlyShowActiveUnit = false;
    [SerializeField] bool onlyShowTargetsWithoutActiveUnit = false;

    List<GridUnit> units = new List<GridUnit>();

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
        units.Clear();

        foreach (GridUnit unit in FantasyCombatManager.Instance.GetAllCombatUnits(false))
        {
            if (onlyShowActiveUnit || (onlyShowTargetsWithoutActiveUnit && FantasyCombatManager.Instance.GetUnitsToShow().Contains(unit)))
            {
                if((onlyShowActiveUnit && unit != FantasyCombatManager.Instance.GetActiveUnit()) || (onlyShowTargetsWithoutActiveUnit && unit == FantasyCombatManager.Instance.GetActiveUnit()))
                {
                    units.Add(unit);
                    unit.ShowModel(false);
                }
                else
                {
                    unit.ShowModel(true);
                }
            }
            else if (!FantasyCombatManager.Instance.GetUnitsToShow().Contains(unit))
            {
                units.Add(unit);
                unit.ShowModel(false);
            }
            else
            {
                unit.ShowModel(true);
            }
        }
    }

    public void ShowAll()
    {
        foreach (GridUnit unit in units)
        {
            unit.ShowModel(true);
        }
    }
 
    private void OnDisable()
    {
        ShowAll();
    }
}
