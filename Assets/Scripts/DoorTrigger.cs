using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class DoorTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    public string openParam = "isOpen"; // Animator param

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggerred: " + other.gameObject.name);
        if (other.CompareTag("Ghost"))
        {
            Debug.Log("Ghost Detected!");
            doorAnimator.SetBool(openParam, true);
        }
    }
}