using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StealthDangerZone : MonoBehaviour
{
    SneakBarrel barrel = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Barrel"))
        {
            if (!barrel)
            {
                GetBarrel(other);
            }

            barrel.inSafeZone = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Barrel"))
        {
            if (!barrel)
            {
                GetBarrel(other);
            }

            barrel.inSafeZone = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Barrel"))
        {
            if (!barrel)
            {
                GetBarrel(other);
            }

            barrel.inSafeZone = true;
        }
    }


    private void GetBarrel(Collider other)
    {
        barrel = other.GetComponentInParent<SneakBarrel>();
    }
}
