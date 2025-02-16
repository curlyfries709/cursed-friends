using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistableObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
