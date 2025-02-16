
using UnityEngine;
using MoreMountains.Feedbacks;

public enum Element
{
    None,
    Silver,
    Steel,
    Gold,
    Fire,
    Ice,
    Air,
    Lightning,
    Earth,
    Holy,
    Curse
}

public enum Affinity
{
    None,
    Absorb,
    Immune,
    Reflect,
    Resist,
    Weak,
    Evade
}

[System.Serializable]
public class ElementAffinity
{
    public Element element;
    public Affinity affinity;
}


[System.Serializable]
public class ItemAffinity
{
    public Item item;
    public Affinity affinity;
}

[System.Serializable]
public class AffinityFeedback
{
    [Tooltip("Triggered when Affinity is None, Resist or Weak")]
    public MMF_Player attackConnectedFeedback;
    public MMF_Player attackEvadedFeedback;
    [Space(10)]
    public MMF_Player attackReflectedFeedback;
    public MMF_Player attackNulledFeedback;
    public MMF_Player attackAbsorbedFeedback;
    [Space(10)]
    [Tooltip("This is only for DAMAGE Feedbacks esthablished in an IDAMAGEABLE class")]
    public Transform spawnVFXHeader;
}


