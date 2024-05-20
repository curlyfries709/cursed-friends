using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChainSelectionEvent : MonoBehaviour
{
    public List<PlayerBaseChainAttack> GetChainAttacks()
    {
        return GetComponentsInChildren<PlayerBaseChainAttack>().ToList();
    }

}
