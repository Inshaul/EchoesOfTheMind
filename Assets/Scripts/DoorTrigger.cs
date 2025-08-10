using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class DoorTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    public string openParam = "isOpen"; // Animator param

    public AudioSource audioSource;
    public AudioClip closeSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ghost") || other.CompareTag("PlayerReference"))
        {
            doorAnimator.SetBool(openParam, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ghost") || other.CompareTag("PlayerReference"))
        {
            doorAnimator.SetBool(openParam, false);
            StartCoroutine(PlayDoorSoundWithDelay(1f));
        }
    }
    

    private IEnumerator PlayDoorSoundWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayDoorSound();
    }
    private void PlayDoorSound()
    {
        if (audioSource == null) return;
        //audioSource.clip = open ? openSound : closeSound;
        audioSource.clip = closeSound;
        audioSource.Play();
    }
}