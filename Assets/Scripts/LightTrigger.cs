using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class LightTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            FearManager.Instance.SetLit(true);
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            FearManager.Instance.SetLit(false);
    }
}
