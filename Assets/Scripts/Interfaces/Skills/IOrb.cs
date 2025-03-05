using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IOrb
{
    public void Setup(PlayerBaseSkill orbSkill, PlayerGridUnit user, FantasyCombatCollectionManager collectionManager)
    {
        orbSkill.ExternalSetup(user, "", collectionManager);   
    }

    public void UseOrb(Orb orb, PlayerGridUnit user, FantasyCombatCollectionManager collectionManager)
    {
        collectionManager.BeginOrbCooldown(orb, user);
    }
}
