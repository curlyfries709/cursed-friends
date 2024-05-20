
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using System.Linq;

public enum SFXType
{
    OpenChest,
    Loot,
    ScrollForward,
    InventorySwitch,
    ActionDenied,
    TabForward,
    TabBack,
    CombatMenuSelect,
    OpenPouch,
    EnemyAlert,
    EnemySearch,
    GrassStep,
    WoodStep,
    EnemySeeing,
    DefaultMenuSelect,
    OpenCombatMenu,
    TaDa,
    EquipItem,
    InventoryTransfer,
    InventoryDiscard,
    PotionPowerUp,
    ShopCoin,
    RealmTransition
}

public class SFXPlayer : MonoBehaviour
{
    Dictionary<SFXType, MMF_Player> sfxDict = new Dictionary<SFXType, MMF_Player>();

    private void Awake()
    {
        foreach(SFXComponent sfx in GetComponentsInChildren<SFXComponent>())
        {
            sfxDict[sfx.sfxType] = sfx.GetComponent<MMF_Player>();
        }
    }

    public void PlaySFX(SFXType sfxToPlay)
    {
        if(sfxDict.ContainsKey(sfxToPlay))
            sfxDict[sfxToPlay]?.PlayFeedbacks();
    }
}
