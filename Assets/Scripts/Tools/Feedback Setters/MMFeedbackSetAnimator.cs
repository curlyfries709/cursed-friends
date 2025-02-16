using MoreMountains.Feedbacks;
using UnityEngine;

public class MMFeedbackSetAnimator : MMFeedbackValueSetter
{
    public override void SetValue(CharacterGridUnit myCharacter)
    {
        MMF_AnimatorSpeed animatorFeedback = feedbackToUpdate.GetFeedbackOfType<MMF_AnimatorSpeed>();
        animatorFeedback.BoundAnimator = myCharacter.unitAnimator.GetAnimator();
    }
}
