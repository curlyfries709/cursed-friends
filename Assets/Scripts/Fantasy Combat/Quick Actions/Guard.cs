using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : PlayerBaseSkill
{
    [Header("GUARD ACTION DATA")]
    [SerializeField] float actionDisplayTime = 1f;
    [Header("Orbit Point Rotation Settings")]
    [SerializeField] Transform orbitPoint;
    [SerializeField] float orbitPointRotationSpeed;
    [SerializeField] Vector3 orbitPointStartingRotation;


    public override void SkillSelected()
    {
        if (skillTriggered)
        {
            orbitPoint.Rotate(Vector3.down * orbitPointRotationSpeed * Time.deltaTime);
        }
        else
        {
            GridVisual();
        }
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(false))
        {
            BeginSkill(0, actionDisplayTime, false);//Unit Position Updated here

            myCharacter.CharacterHealth().Guard(true);

            //Setup Cam
            orbitPoint.localRotation = Quaternion.Euler(orbitPointStartingRotation);
            ActivateVisuals(true);

            Invoke("GuardComplete", actionDisplayTime);

            return true;
        }
        else
        {
            return false;
        }
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }
        ActivateVisuals(false);
    }

    private void GuardComplete()
    {
        EndAction();
        ActivateVisuals(false);
        //skillTriggered = false;
    }

    protected override bool ListenForUnitHealthUIComplete()
    {
        return false;
    }

}
