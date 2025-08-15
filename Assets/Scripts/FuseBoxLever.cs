using System.Collections;
using UnityEngine;

public class FuseBoxLever : MonoBehaviour
{
    public FuseBoxController fuseBoxController;
    public Transform leverHandle;
    public float onAngle = -105f;
    public float offAngle = 55f;
    private bool powerOn = false;

    public AudioSource leverAudio;

    public Animator leverAnimator;

    private void Start()
    {
        powerOn = false;
        //SetLeverAngle(offAngle);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Use the tag of your VR hand/controller
        if ((other.CompareTag("PlayerReference") || other.CompareTag("Player")) &&  !GameDirector.Instance.ghost.gameObject.activeInHierarchy)
        {
            TogglePower();
        }
    }

    public void TogglePower()
    {
        powerOn = !powerOn;

        if (leverAnimator != null)
            leverAnimator.SetBool("isOn", powerOn);

        StartCoroutine(DelayedLeverActions());
    }
    private IEnumerator DelayedLeverActions()
    {
        yield return new WaitForSeconds(1f); // Wait for animation

        if (leverAudio != null)
            leverAudio.Play();

        if (fuseBoxController == null) yield break;

        if (powerOn)
        {
            fuseBoxController.ActivateFuseBox();
        }
        else
        {
            fuseBoxController.TurnOffAllRooms();
        }
    }

}
