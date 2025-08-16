using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class FinalEscapeManager : MonoBehaviour
{
    public GameObject finalEscapeDoor;
    public AudioSource audioSource;
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerReference"))
        {
            GameDirector.Instance.OnEscape(()=>
            {
                PlayEscapeSound();
            });
            
        }
    }
    private void PlayEscapeSound()
    {
        if (audioSource == null) return;
        audioSource.Play();
    }
}