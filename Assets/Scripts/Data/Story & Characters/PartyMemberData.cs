using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Pary Member", menuName = "Story/Party Member", order = 5)]
public class PartyMemberData : ScriptableObject
{
    [Title("Name")]
    public string memberName;
    [Title("Art")]
    public Sprite portrait;
}
