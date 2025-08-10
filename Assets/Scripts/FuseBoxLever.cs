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
        if (other.CompareTag("PlayerReference") || other.CompareTag("Player"))
        {
            TogglePower();
        }
    }

    private void TogglePower()
    {
        powerOn = !powerOn;
        //SetLeverAngle(powerOn ? onAngle : offAngle);

        if (leverAnimator != null)
            leverAnimator.SetBool("isOn", powerOn);

        if (leverAudio != null)
            leverAudio.Play();
        

        if (fuseBoxController == null) return;
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
