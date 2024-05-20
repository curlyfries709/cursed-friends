using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitTrigger : MonoBehaviour
{
    [SerializeField] BoxCollider myCollider;
    [SerializeField] GameObject hitVFX;
    public bool triggerer = false;

    Animator animator;
    

    private void OnTriggerEnter(Collider other)
    {
        HitTrigger otherTrigger = other.GetComponent<HitTrigger>();

        if (!triggerer && animator && otherTrigger && otherTrigger.triggerer)
        {
            float randomYOffset = Random.Range(0.25f, myCollider.size.y - 0.25f);
            float centerZWorldPos = transform.position.z + myCollider.center.z;
            float randomZOffset = Random.Range(centerZWorldPos - 0.25f, centerZWorldPos + 0.25f);
            Vector3 spawnPos = new Vector3(transform.position.x, randomYOffset, randomZOffset);
            Instantiate(hitVFX, spawnPos, Quaternion.identity);

            animator.SetTrigger("Hit");
        }
    }


    public void Setup(BoxCollider collider, bool triggerer, Animator animator = null)
    {
        this.animator = animator;
        this.triggerer = triggerer;

        if (animator)
        {
            animator.SetLayerWeight(1, 1);
        }

        myCollider.center = collider.center;
        myCollider.size = collider.size;
    }
}
