using MoreMountains.Feedbacks;
using UnityEngine;

public abstract class MMFeedbackValueSetter : MonoBehaviour
{
    [SerializeField] protected BaseSkill associatedSkill;
    [SerializeField] protected MMF_Player feedbackToUpdate;
    public abstract void SetValue(CharacterGridUnit myCharacter);

    private void OnEnable()
    {
        associatedSkill.SkillOwnerSet += SetValue;
    }

    private void OnDisable()
    {
        associatedSkill.SkillOwnerSet -= SetValue;
    }
}
